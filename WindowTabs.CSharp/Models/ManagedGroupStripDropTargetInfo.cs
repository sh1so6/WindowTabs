using System;

namespace WindowTabs.CSharp.Models
{
    internal readonly struct ManagedGroupStripDropTargetInfo
    {
        public ManagedGroupStripDropTargetInfo(IntPtr targetWindowHandle, IntPtr? insertAfterWindowHandle)
        {
            TargetWindowHandle = targetWindowHandle;
            InsertAfterWindowHandle = insertAfterWindowHandle;
        }

        public IntPtr TargetWindowHandle { get; }

        public IntPtr? InsertAfterWindowHandle { get; }

        public bool InsertAfterTarget =>
            InsertAfterWindowHandle.HasValue
            && InsertAfterWindowHandle.Value == TargetWindowHandle;
    }
}
