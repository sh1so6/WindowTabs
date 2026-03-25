using System;
using System.Collections.Generic;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class DesktopPlannerService
    {
        private readonly FilterService filterService;
        private readonly LauncherService launcherService;
        private readonly DesktopGroupingRuleService groupingRuleService;

        public DesktopPlannerService(
            FilterService filterService,
            LauncherService launcherService,
            DesktopGroupingRuleService groupingRuleService)
        {
            this.filterService = filterService ?? throw new ArgumentNullException(nameof(filterService));
            this.launcherService = launcherService ?? throw new ArgumentNullException(nameof(launcherService));
            this.groupingRuleService = groupingRuleService ?? throw new ArgumentNullException(nameof(groupingRuleService));
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
            var windowsByHandle = BuildWindowMap(windowsInZOrder);
            var groupedWindows = BuildGroupedWindowSet(groups);
            var currentGroupsByWindow = BuildCurrentGroupMap(groups);
            var tabbableByHandle = BuildTabbableMap(windowsInZOrder, screenRegion);

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

                var isTabbable = tabbableByHandle.TryGetValue(window.Handle, out var cachedIsTabbable)
                    && cachedIsTabbable;
                if (isTabbable && !groupedWindows.Contains(window.Handle))
                {
                    plan.WindowsToGroup.Add(groupingRuleService.BuildGroupingDecision(window, groups, zOrderMap, droppedWindowHandles));
                    continue;
                }

                if (!isTabbable || !currentGroupsByWindow.TryGetValue(window.Handle, out var currentGroupHandle))
                {
                    continue;
                }

                var regroupDecision = groupingRuleService.BuildGroupingDecision(window, groups, zOrderMap, droppedWindowHandles);
                if (regroupDecision.TargetGroupHandle.HasValue
                    && regroupDecision.TargetGroupHandle.Value != currentGroupHandle)
                {
                    plan.WindowsToRegroup.Add(regroupDecision);
                    continue;
                }

                if (regroupDecision.TargetGroupHandle.HasValue
                    && regroupDecision.TargetGroupHandle.Value == currentGroupHandle
                    && groupingRuleService.ShouldReorderWindow(window.Handle, currentGroupHandle, regroupDecision.InsertAfterWindowHandle, groups))
                {
                    plan.WindowsToReorder.Add(regroupDecision);
                }
            }

            foreach (var group in groups)
            {
                foreach (var windowHandle in group.WindowHandles)
                {
                    if (!windowsByHandle.TryGetValue(windowHandle, out var window))
                    {
                        continue;
                    }

                    if (window.IsOnCurrentVirtualDesktop
                        && (!tabbableByHandle.TryGetValue(windowHandle, out var isTabbable) || !isTabbable))
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

        private static IReadOnlyDictionary<IntPtr, int> BuildZOrderMap(IReadOnlyList<WindowSnapshot> windowsInZOrder)
        {
            var map = new Dictionary<IntPtr, int>();
            for (var index = 0; index < windowsInZOrder.Count; index++)
            {
                map[windowsInZOrder[index].Handle] = index;
            }

            return map;
        }

        private static Dictionary<IntPtr, WindowSnapshot> BuildWindowMap(IReadOnlyList<WindowSnapshot> windowsInZOrder)
        {
            var map = new Dictionary<IntPtr, WindowSnapshot>();
            foreach (var window in windowsInZOrder)
            {
                map[window.Handle] = window;
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

        private static Dictionary<IntPtr, IntPtr> BuildCurrentGroupMap(IReadOnlyList<GroupSnapshot> groups)
        {
            var map = new Dictionary<IntPtr, IntPtr>();
            foreach (var group in groups)
            {
                foreach (var windowHandle in group.WindowHandles)
                {
                    map[windowHandle] = group.GroupHandle;
                }
            }

            return map;
        }

        private Dictionary<IntPtr, bool> BuildTabbableMap(IReadOnlyList<WindowSnapshot> windowsInZOrder, RectValue screenRegion)
        {
            var map = new Dictionary<IntPtr, bool>();
            foreach (var window in windowsInZOrder)
            {
                map[window.Handle] = filterService.IsTabbableWindow(window, screenRegion);
            }

            return map;
        }


        public void UpdateTrackedProcessPaths(IEnumerable<WindowSnapshot> windows)
        {
            groupingRuleService.UpdateTrackedProcessPaths(windows);
        }
    }
}
