using System;
using System.Collections.Generic;

namespace WindowTabs.CSharp.Models
{
    internal sealed class ManagedGroupStripDragSessionState
    {
        public ManagedGroupStripDragSessionState(
            IntPtr draggedWindowHandle,
            ManagedGroupStripPreviewState previewState,
            ManagedGroupStripDropState dropState)
        {
            DraggedWindowHandle = draggedWindowHandle;
            PreviewState = previewState ?? new ManagedGroupStripPreviewState(null, null);
            DropState = dropState;
        }

        public static ManagedGroupStripDragSessionState Empty { get; } =
            new ManagedGroupStripDragSessionState(IntPtr.Zero, new ManagedGroupStripPreviewState(null, null), ManagedGroupStripDropState.Empty);

        public IntPtr DraggedWindowHandle { get; }

        public ManagedGroupStripPreviewState PreviewState { get; }

        public ManagedGroupStripDropState DropState { get; }

        public IReadOnlyList<IntPtr> CurrentGroupWindowHandles => PreviewState.CurrentGroupWindowHandles;

        public IReadOnlyList<IntPtr> PreviewGroupWindowHandles => PreviewState.PreviewGroupWindowHandles;

        public ManagedGroupStripDragSessionState WithDraggedWindowHandle(IntPtr draggedWindowHandle)
        {
            return new ManagedGroupStripDragSessionState(draggedWindowHandle, PreviewState, DropState);
        }

        public ManagedGroupStripDragSessionState WithPreviewState(ManagedGroupStripPreviewState previewState)
        {
            return new ManagedGroupStripDragSessionState(DraggedWindowHandle, previewState, DropState);
        }

        public ManagedGroupStripDragSessionState WithDropState(ManagedGroupStripDropState dropState)
        {
            return new ManagedGroupStripDragSessionState(DraggedWindowHandle, PreviewState, dropState);
        }
    }
}
