using System;
using System.Collections.Generic;

namespace WindowTabs.CSharp.Models
{
    internal sealed class ManagedGroupStripDropTargetChange
    {
        public ManagedGroupStripDropTargetChange(bool hasChanged, IntPtr targetWindowHandle, bool insertAfterTarget, IReadOnlyList<IntPtr> invalidatedWindowHandles)
        {
            HasChanged = hasChanged;
            TargetWindowHandle = targetWindowHandle;
            InsertAfterTarget = insertAfterTarget;
            InvalidatedWindowHandles = invalidatedWindowHandles ?? Array.Empty<IntPtr>();
        }

        public bool HasChanged { get; }

        public IntPtr TargetWindowHandle { get; }

        public bool InsertAfterTarget { get; }

        public IReadOnlyList<IntPtr> InvalidatedWindowHandles { get; }
    }
}
