using System;
using System.Collections.Generic;
using System.Linq;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class DesktopSessionCoordinator
    {
        private readonly DesktopSnapshotService desktopSnapshotService;
        private readonly DesktopPlannerService desktopPlannerService;
        private readonly IDesktopRuntime desktopRuntime;
        private readonly WindowPresentationStateStore windowPresentationStateStore;
        private readonly HashSet<IntPtr> subscribedHandles = new HashSet<IntPtr>();
        private readonly Dictionary<IntPtr, DateTime> droppedWindowHandles = new Dictionary<IntPtr, DateTime>();
        private static readonly TimeSpan DroppedWindowCooldown = TimeSpan.FromSeconds(2);

        public DesktopSessionCoordinator(
            DesktopSnapshotService desktopSnapshotService,
            DesktopPlannerService desktopPlannerService,
            IDesktopRuntime desktopRuntime,
            WindowPresentationStateStore windowPresentationStateStore)
        {
            this.desktopSnapshotService = desktopSnapshotService ?? throw new ArgumentNullException(nameof(desktopSnapshotService));
            this.desktopPlannerService = desktopPlannerService ?? throw new ArgumentNullException(nameof(desktopPlannerService));
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
            this.windowPresentationStateStore = windowPresentationStateStore ?? throw new ArgumentNullException(nameof(windowPresentationStateStore));
        }

        public string RuntimeKind => desktopRuntime.GetType().Name;

        public DesktopRefreshResult RefreshDesktop()
        {
            var screenRegion = desktopSnapshotService.GetScreenRegion();
            var windows = desktopSnapshotService.EnumerateWindowsInZOrder();
            desktopPlannerService.UpdateTrackedProcessPaths(windows);
            RemoveClosedWindows(windows);
            CleanupExpiredDroppedWindows();

            var plan = desktopPlannerService.BuildPlan(
                windows,
                ToGroupSnapshots(),
                screenRegion,
                subscribedHandles,
                GetActiveDroppedWindowHandles());

            ApplyPlan(plan);

            return new DesktopRefreshResult
            {
                Windows = windows,
                ScreenRegion = screenRegion,
                Groups = ToGroupSnapshots(),
                Plan = plan
            };
        }

        public DesktopRefreshResult HandleDestroyedWindow(IntPtr windowHandle, DesktopRefreshResult previousRefresh)
        {
            if (windowHandle == IntPtr.Zero || previousRefresh == null)
            {
                return RefreshDesktop();
            }

            subscribedHandles.Remove(windowHandle);
            droppedWindowHandles.Remove(windowHandle);
            desktopRuntime.RemoveWindow(windowHandle);
            windowPresentationStateStore.RemoveWindow(windowHandle);

            var windows = previousRefresh.Windows
                .Where(window => window.Handle != windowHandle)
                .ToList();

            desktopPlannerService.UpdateTrackedProcessPaths(windows);
            var groups = ToGroupSnapshots();
            CleanupExpiredDroppedWindows();
            var plan = desktopPlannerService.BuildPlan(
                windows,
                groups,
                previousRefresh.ScreenRegion,
                subscribedHandles,
                GetActiveDroppedWindowHandles());

            ApplyPlan(plan);

            return new DesktopRefreshResult
            {
                Windows = windows,
                ScreenRegion = previousRefresh.ScreenRegion,
                Groups = ToGroupSnapshots(),
                Plan = plan
            };
        }

        public void MarkDropped(IntPtr windowHandle)
        {
            if (windowHandle != IntPtr.Zero)
            {
                droppedWindowHandles[windowHandle] = DateTime.UtcNow.Add(DroppedWindowCooldown);
            }
        }

        private void RemoveClosedWindows(IReadOnlyList<WindowSnapshot> windows)
        {
            var activeHandles = new HashSet<IntPtr>(windows.Select(window => window.Handle));

            subscribedHandles.RemoveWhere(handle => !activeHandles.Contains(handle));
            foreach (var staleHandle in droppedWindowHandles.Keys.Where(handle => !activeHandles.Contains(handle)).ToArray())
            {
                droppedWindowHandles.Remove(staleHandle);
            }
            desktopRuntime.RemoveClosedWindows(activeHandles);
            windowPresentationStateStore.RemoveClosedWindows(activeHandles);
        }

        private void CleanupExpiredDroppedWindows()
        {
            var now = DateTime.UtcNow;
            foreach (var expiredHandle in droppedWindowHandles
                         .Where(pair => pair.Value <= now)
                         .Select(pair => pair.Key)
                         .ToArray())
            {
                droppedWindowHandles.Remove(expiredHandle);
            }
        }

        private ISet<IntPtr> GetActiveDroppedWindowHandles()
        {
            return new HashSet<IntPtr>(droppedWindowHandles.Keys);
        }

        private void ApplyPlan(DesktopPlan plan)
        {
            foreach (var handle in plan.WindowsToSubscribe)
            {
                subscribedHandles.Add(handle);
            }

            foreach (var (groupHandle, windowHandle) in plan.WindowsToRemoveFromGroups)
            {
                var group = desktopRuntime.FindGroup(groupHandle);
                if (group != null)
                {
                    group.RemoveWindow(windowHandle);
                }
            }

            foreach (var decision in plan.WindowsToGroup)
            {
                if (desktopRuntime.IsWindowGrouped(decision.WindowHandle))
                {
                    droppedWindowHandles.Remove(decision.WindowHandle);
                    continue;
                }

                var targetGroup = decision.TargetGroupHandle.HasValue
                    ? desktopRuntime.FindGroup(decision.TargetGroupHandle.Value)
                    : null;

                if (targetGroup == null)
                {
                    targetGroup = desktopRuntime.CreateGroup(decision.TargetGroupHandle);
                }

                targetGroup.AddWindow(decision.WindowHandle, decision.InsertAfterWindowHandle);
                droppedWindowHandles.Remove(decision.WindowHandle);
            }

            foreach (var decision in plan.WindowsToRegroup)
            {
                var currentGroup = desktopRuntime.FindGroupContainingWindow(decision.WindowHandle);
                if (currentGroup == null)
                {
                    continue;
                }

                var targetGroup = decision.TargetGroupHandle.HasValue
                    ? desktopRuntime.FindGroup(decision.TargetGroupHandle.Value)
                    : null;
                if (targetGroup == null)
                {
                    targetGroup = desktopRuntime.CreateGroup(decision.TargetGroupHandle);
                }

                if (currentGroup.GroupHandle == targetGroup.GroupHandle)
                {
                    droppedWindowHandles.Remove(decision.WindowHandle);
                    continue;
                }

                desktopRuntime.RemoveWindow(decision.WindowHandle);
                targetGroup.AddWindow(decision.WindowHandle, decision.InsertAfterWindowHandle);
                droppedWindowHandles.Remove(decision.WindowHandle);
            }

            foreach (var decision in plan.WindowsToReorder)
            {
                var currentGroup = desktopRuntime.FindGroupContainingWindow(decision.WindowHandle);
                if (currentGroup == null)
                {
                    continue;
                }

                currentGroup.MoveWindowAfter(decision.WindowHandle, decision.InsertAfterWindowHandle);
                droppedWindowHandles.Remove(decision.WindowHandle);
            }

            foreach (var groupHandle in plan.GroupsToDestroy)
            {
                desktopRuntime.DestroyGroup(groupHandle);
            }
        }

        private IReadOnlyList<GroupSnapshot> ToGroupSnapshots()
        {
            return desktopRuntime.Groups
                .Select(group => new GroupSnapshot
                {
                    GroupHandle = group.GroupHandle,
                    WindowHandles = group.WindowHandles.ToList(),
                    TabPosition = group.TabPosition,
                    SnapTabHeightMargin = group.SnapTabHeightMargin
                })
                .ToList();
        }
    }
}
