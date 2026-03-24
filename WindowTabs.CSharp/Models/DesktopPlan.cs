using System;
using System.Collections.Generic;

namespace WindowTabs.CSharp.Models
{
    internal sealed class DesktopPlan
    {
        public List<IntPtr> WindowsToSubscribe { get; } = new List<IntPtr>();

        public List<WindowGroupingDecision> WindowsToGroup { get; } = new List<WindowGroupingDecision>();

        public List<WindowGroupingDecision> WindowsToRegroup { get; } = new List<WindowGroupingDecision>();

        public List<WindowGroupingDecision> WindowsToReorder { get; } = new List<WindowGroupingDecision>();

        public List<(IntPtr GroupHandle, IntPtr WindowHandle)> WindowsToRemoveFromGroups { get; } = new List<(IntPtr GroupHandle, IntPtr WindowHandle)>();

        public List<IntPtr> GroupsToDestroy { get; } = new List<IntPtr>();
    }
}
