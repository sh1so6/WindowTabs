using System;

namespace WindowTabs.CSharp.Models
{
    internal readonly struct ManagedGroupStripDropState
    {
        public ManagedGroupStripDropState(IntPtr targetWindowHandle, bool insertAfterTarget)
        {
            TargetWindowHandle = targetWindowHandle;
            InsertAfterTarget = insertAfterTarget;
        }

        public static ManagedGroupStripDropState Empty { get; } = new ManagedGroupStripDropState(IntPtr.Zero, true);

        public IntPtr TargetWindowHandle { get; }

        public bool InsertAfterTarget { get; }

        public bool IsTargetWindow(IntPtr windowHandle)
        {
            return windowHandle != IntPtr.Zero && TargetWindowHandle == windowHandle;
        }
    }
}
