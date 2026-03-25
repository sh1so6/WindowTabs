using System;
using System.Collections.Generic;

namespace WindowTabs.CSharp.Models
{
    internal sealed class ManagedGroupStripDropTargetChange
    {
        public ManagedGroupStripDropTargetChange(bool hasChanged, ManagedGroupStripDropState dropState, IReadOnlyList<IntPtr> invalidatedWindowHandles)
        {
            HasChanged = hasChanged;
            DropState = dropState;
            InvalidatedWindowHandles = invalidatedWindowHandles ?? Array.Empty<IntPtr>();
        }

        public bool HasChanged { get; }

        public ManagedGroupStripDropState DropState { get; }

        public IReadOnlyList<IntPtr> InvalidatedWindowHandles { get; }
    }
}
