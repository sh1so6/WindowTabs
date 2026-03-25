using System;
using System.Collections.Generic;
using System.Linq;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class DesktopGroupingRuleService
    {
        private readonly PendingWindowLaunchTracker pendingLaunchTracker;
        private readonly ProcessSettingsService processSettingsService;
        private readonly Dictionary<IntPtr, string> trackedProcessPaths = new Dictionary<IntPtr, string>();

        public DesktopGroupingRuleService(
            PendingWindowLaunchTracker pendingLaunchTracker,
            ProcessSettingsService processSettingsService)
        {
            this.pendingLaunchTracker = pendingLaunchTracker ?? throw new ArgumentNullException(nameof(pendingLaunchTracker));
            this.processSettingsService = processSettingsService ?? throw new ArgumentNullException(nameof(processSettingsService));
        }

        public void UpdateTrackedProcessPaths(IEnumerable<WindowSnapshot> windows)
        {
            trackedProcessPaths.Clear();
            foreach (var window in windows ?? Array.Empty<WindowSnapshot>())
            {
                trackedProcessPaths[window.Handle] = window.Process?.ProcessPath ?? string.Empty;
            }
        }

        public WindowGroupingDecision BuildGroupingDecision(
            WindowSnapshot window,
            IReadOnlyList<GroupSnapshot> groups,
            IReadOnlyDictionary<IntPtr, int> zOrderMap,
            ISet<IntPtr> droppedWindowHandles)
        {
            if (window == null)
            {
                return new WindowGroupingDecision();
            }

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

        public bool ShouldReorderWindow(
            IntPtr windowHandle,
            IntPtr currentGroupHandle,
            IntPtr? desiredInsertAfterWindowHandle,
            IReadOnlyList<GroupSnapshot> groups)
        {
            var group = groups.FirstOrDefault(candidate => candidate.GroupHandle == currentGroupHandle);
            if (group == null)
            {
                return false;
            }

            return FindCurrentInsertAfterWindowHandle(group, windowHandle) != desiredInsertAfterWindowHandle;
        }

        private IntPtr? FindAutoGroup(
            WindowSnapshot window,
            IReadOnlyList<GroupSnapshot> groups,
            IReadOnlyDictionary<IntPtr, int> zOrderMap)
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

        private IntPtr? FindGroupByCategory(
            int categoryNumber,
            IReadOnlyList<GroupSnapshot> groups,
            IReadOnlyDictionary<IntPtr, int> zOrderMap)
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

        private IntPtr? FindGroupByProcessPath(
            string processPath,
            IReadOnlyList<GroupSnapshot> groups,
            IReadOnlyDictionary<IntPtr, int> zOrderMap)
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

        private static IntPtr? FindCurrentInsertAfterWindowHandle(GroupSnapshot group, IntPtr windowHandle)
        {
            if (group?.WindowHandles == null)
            {
                return null;
            }

            for (var index = 0; index < group.WindowHandles.Count; index++)
            {
                if (group.WindowHandles[index] != windowHandle)
                {
                    continue;
                }

                return index > 0
                    ? group.WindowHandles[index - 1]
                    : (IntPtr?)null;
            }

            return null;
        }
    }
}
