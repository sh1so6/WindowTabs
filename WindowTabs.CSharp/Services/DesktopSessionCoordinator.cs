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
        private readonly HashSet<IntPtr> droppedWindowHandles = new HashSet<IntPtr>();

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

            var plan = desktopPlannerService.BuildPlan(
                windows,
                ToGroupSnapshots(),
                screenRegion,
                subscribedHandles,
                droppedWindowHandles);

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
            var plan = desktopPlannerService.BuildPlan(
                windows,
                groups,
                previousRefresh.ScreenRegion,
                subscribedHandles,
                droppedWindowHandles);

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
                droppedWindowHandles.Add(windowHandle);
            }
        }

        private void RemoveClosedWindows(IReadOnlyList<WindowSnapshot> windows)
        {
            var activeHandles = new HashSet<IntPtr>(windows.Select(window => window.Handle));

            subscribedHandles.RemoveWhere(handle => !activeHandles.Contains(handle));
            droppedWindowHandles.RemoveWhere(handle => !activeHandles.Contains(handle));
            desktopRuntime.RemoveClosedWindows(activeHandles);
            windowPresentationStateStore.RemoveClosedWindows(activeHandles);
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
