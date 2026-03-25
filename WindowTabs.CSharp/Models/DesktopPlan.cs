using System;
using System.Collections.Generic;

namespace WindowTabs.CSharp.Models
{
    internal sealed class DesktopPlan
    {
        public List<IntPtr> WindowsToSubscribe { get; } = [];

        public List<WindowGroupingDecision> WindowsToGroup { get; } = [];

        public List<WindowGroupingDecision> WindowsToRegroup { get; } = [];

        public List<WindowGroupingDecision> WindowsToReorder { get; } = [];

        public List<(IntPtr GroupHandle, IntPtr WindowHandle)> WindowsToRemoveFromGroups { get; } = [];

        public List<IntPtr> GroupsToDestroy { get; } = [];
    }
}
