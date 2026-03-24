using System;

namespace WindowTabs.CSharp.Models
{
    internal sealed class WindowGroupingDecision
    {
        public IntPtr WindowHandle { get; set; }

        public IntPtr? TargetGroupHandle { get; set; }

        public IntPtr? InsertAfterWindowHandle { get; set; }

        public GroupAssignmentReason Reason { get; set; }

        public bool CreateNewGroup => !TargetGroupHandle.HasValue;
    }
}
