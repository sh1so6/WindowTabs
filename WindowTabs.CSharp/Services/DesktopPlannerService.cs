using System;
using System.Collections.Generic;
using System.Linq;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class DesktopPlannerService
    {
        private readonly FilterService filterService;
        private readonly LauncherService launcherService;
        private readonly PendingWindowLaunchTracker pendingLaunchTracker;
        private readonly ProcessSettingsService processSettingsService;
        private readonly Dictionary<IntPtr, string> trackedProcessPaths = new Dictionary<IntPtr, string>();

        public DesktopPlannerService(
            FilterService filterService,
            LauncherService launcherService,
            PendingWindowLaunchTracker pendingLaunchTracker,
            ProcessSettingsService processSettingsService)
        {
            this.filterService = filterService ?? throw new ArgumentNullException(nameof(filterService));
            this.launcherService = launcherService ?? throw new ArgumentNullException(nameof(launcherService));
            this.pendingLaunchTracker = pendingLaunchTracker ?? throw new ArgumentNullException(nameof(pendingLaunchTracker));
            this.processSettingsService = processSettingsService ?? throw new ArgumentNullException(nameof(processSettingsService));
        }

        public DesktopPlan BuildPlan(
            IReadOnlyList<WindowSnapshot> windowsInZOrder,
            IReadOnlyList<GroupSnapshot> groups,
            RectValue screenRegion,
            ISet<IntPtr> subscribedHandles,
            ISet<IntPtr> droppedWindowHandles)
        {
            var plan = new DesktopPlan();
            var zOrderMap = BuildZOrderMap(windowsInZOrder);
            var groupedWindows = BuildGroupedWindowSet(groups);

            foreach (var window in windowsInZOrder)
            {
                if (!subscribedHandles.Contains(window.Handle)
                    && !window.Process.IsCurrentProcess
                    && filterService.IsAppWindowStyle(window))
                {
                    plan.WindowsToSubscribe.Add(window.Handle);
                }

                if (!window.IsOnCurrentVirtualDesktop)
                {
                    continue;
                }

                if (filterService.IsTabbableWindow(window, screenRegion) && !groupedWindows.Contains(window.Handle))
                {
                    plan.WindowsToGroup.Add(FindGroupForWindow(window, groups, zOrderMap, droppedWindowHandles));
                }
            }

            foreach (var group in groups)
            {
                foreach (var windowHandle in group.WindowHandles)
                {
                    var window = windowsInZOrder.FirstOrDefault(candidate => candidate.Handle == windowHandle);
                    if (window == null)
                    {
                        continue;
                    }

                    if (window.IsOnCurrentVirtualDesktop && !filterService.IsTabbableWindow(window, screenRegion))
                    {
                        plan.WindowsToRemoveFromGroups.Add((group.GroupHandle, windowHandle));
                    }
                }

                if (group.WindowHandles.Count == 0 && !launcherService.IsLaunching(group.GroupHandle))
                {
                    plan.GroupsToDestroy.Add(group.GroupHandle);
                }
            }

            return plan;
        }

        private WindowGroupingDecision FindGroupForWindow(
            WindowSnapshot window,
            IReadOnlyList<GroupSnapshot> groups,
            IReadOnlyDictionary<IntPtr, int> zOrderMap,
            ISet<IntPtr> droppedWindowHandles)
        {
            if (droppedWindowHandles.Contains(window.Handle))
            {
                return new WindowGroupingDecision
                {
                    WindowHandle = window.Handle,
                    Reason = GroupAssignmentReason.Dropped
                };
            }

            var pendingLaunch = pendingLaunchTracker.TryConsume(window.Process.ProcessPath);
            if (pendingLaunch != null)
            {
                return new WindowGroupingDecision
                {
                    WindowHandle = window.Handle,
                    TargetGroupHandle = pendingLaunch.GroupHandle,
                    InsertAfterWindowHandle = pendingLaunch.InvokerHandle,
                    Reason = GroupAssignmentReason.PendingLaunch
                };
            }

            var autoGroup = FindAutoGroup(window, groups, zOrderMap);
            if (autoGroup.HasValue)
            {
                return new WindowGroupingDecision
                {
                    WindowHandle = window.Handle,
                    TargetGroupHandle = autoGroup.Value,
                    InsertAfterWindowHandle = FindInsertAfterWindowHandle(window.Process.ProcessPath, autoGroup.Value, groups),
                    Reason = GroupAssignmentReason.AutoGroup
                };
            }

            return new WindowGroupingDecision
            {
                WindowHandle = window.Handle,
                Reason = GroupAssignmentReason.NewGroup
            };
        }

        private static IReadOnlyDictionary<IntPtr, int> BuildZOrderMap(IReadOnlyList<WindowSnapshot> windowsInZOrder)
        {
            var map = new Dictionary<IntPtr, int>();
            for (var index = 0; index < windowsInZOrder.Count; index++)
            {
                map[windowsInZOrder[index].Handle] = index;
            }

            return map;
        }

        private static HashSet<IntPtr> BuildGroupedWindowSet(IReadOnlyList<GroupSnapshot> groups)
        {
            var set = new HashSet<IntPtr>();
            foreach (var group in groups)
            {
                foreach (var windowHandle in group.WindowHandles)
                {
                    set.Add(windowHandle);
                }
            }

            return set;
        }

        private IntPtr? FindAutoGroup(WindowSnapshot window, IReadOnlyList<GroupSnapshot> groups, IReadOnlyDictionary<IntPtr, int> zOrderMap)
        {
            if (!processSettingsService.GetAutoGroupingEnabled(window.Process.ProcessPath))
            {
                return null;
            }

            var categoryNumber = processSettingsService.GetCategoryForProcess(window.Process.ProcessPath);
            if (categoryNumber > 0)
            {
                return FindGroupByCategory(categoryNumber, groups, zOrderMap);
            }

            return FindGroupByProcessPath(window.Process.ProcessPath, groups, zOrderMap);
        }

        private IntPtr? FindInsertAfterWindowHandle(string processPath, IntPtr groupHandle, IReadOnlyList<GroupSnapshot> groups)
        {
            if (string.IsNullOrWhiteSpace(processPath))
            {
                return null;
            }

            var group = groups.FirstOrDefault(candidate => candidate.GroupHandle == groupHandle);
            if (group == null)
            {
                return null;
            }

            IntPtr? insertAfterWindowHandle = null;
            foreach (var windowHandle in group.WindowHandles)
            {
                if (trackedProcessPaths.TryGetValue(windowHandle, out var candidatePath)
                    && string.Equals(candidatePath, processPath, StringComparison.OrdinalIgnoreCase))
                {
                    insertAfterWindowHandle = windowHandle;
                }
            }

            return insertAfterWindowHandle;
        }

        private IntPtr? FindGroupByCategory(int categoryNumber, IReadOnlyList<GroupSnapshot> groups, IReadOnlyDictionary<IntPtr, int> zOrderMap)
        {
            foreach (var group in groups
                         .Where(candidate => candidate.WindowHandles.Count > 0)
                         .OrderBy(candidate => candidate.WindowHandles.Min(handle => zOrderMap.TryGetValue(handle, out var order) ? order : int.MaxValue)))
            {
                foreach (var windowHandle in group.WindowHandles)
                {
                    if (!trackedProcessPaths.TryGetValue(windowHandle, out var processPath))
                    {
                        continue;
                    }

                    if (processSettingsService.GetCategoryForProcess(processPath) == categoryNumber)
                    {
                        return group.GroupHandle;
                    }
                }
            }

            return null;
        }

        private IntPtr? FindGroupByProcessPath(string processPath, IReadOnlyList<GroupSnapshot> groups, IReadOnlyDictionary<IntPtr, int> zOrderMap)
        {
            if (string.IsNullOrWhiteSpace(processPath))
            {
                return null;
            }

            foreach (var group in groups
                         .Where(group => group.WindowHandles.Count > 0)
                         .OrderBy(group => group.WindowHandles.Min(handle => zOrderMap.TryGetValue(handle, out var order) ? order : int.MaxValue)))
            {
                foreach (var windowHandle in group.WindowHandles)
                {
                    if (trackedProcessPaths.TryGetValue(windowHandle, out var candidatePath)
                        && string.Equals(candidatePath, processPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return group.GroupHandle;
                    }
                }
            }

            return null;
        }

        public void UpdateTrackedProcessPaths(IEnumerable<WindowSnapshot> windows)
        {
            trackedProcessPaths.Clear();
            foreach (var window in windows)
            {
                trackedProcessPaths[window.Handle] = window.Process.ProcessPath ?? string.Empty;
            }
        }
    }
}
