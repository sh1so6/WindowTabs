using System;
using System.Collections.Generic;

namespace WindowTabs.CSharp.Models
{
    internal sealed class ManagedGroupStripFormState
    {
        public ManagedGroupStripFormState(
            IntPtr currentGroupHandle,
            IReadOnlyList<IntPtr> actualGroupWindowHandles,
            ManagedGroupStripDragSessionState dragSessionState)
        {
            CurrentGroupHandle = currentGroupHandle;
            ActualGroupWindowHandles = actualGroupWindowHandles ?? Array.Empty<IntPtr>();
            DragSessionState = dragSessionState ?? ManagedGroupStripDragSessionState.Empty;
        }

        public static ManagedGroupStripFormState Empty { get; } =
            new ManagedGroupStripFormState(IntPtr.Zero, Array.Empty<IntPtr>(), ManagedGroupStripDragSessionState.Empty);

        public IntPtr CurrentGroupHandle { get; }

        public IReadOnlyList<IntPtr> ActualGroupWindowHandles { get; }

        public ManagedGroupStripDragSessionState DragSessionState { get; }

        public ManagedGroupStripFormState WithDragSessionState(ManagedGroupStripDragSessionState dragSessionState)
        {
            return new ManagedGroupStripFormState(CurrentGroupHandle, ActualGroupWindowHandles, dragSessionState);
        }
    }
}
