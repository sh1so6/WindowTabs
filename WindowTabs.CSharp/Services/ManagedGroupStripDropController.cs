using System;
using System.Drawing;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripDropController
    {
        private readonly GroupMutationService groupMutationService;

        public ManagedGroupStripDropController(GroupMutationService groupMutationService)
        {
            this.groupMutationService = groupMutationService ?? throw new ArgumentNullException(nameof(groupMutationService));
        }

        public bool HandleDragEnter(
            object data,
            Point clientPoint,
            IDropSurface dropSurface)
        {
            ArgumentNullException.ThrowIfNull(dropSurface);

            var dragInfo = data as TabDragInfo;
            var dropTargetInfo = default(ManagedGroupStripDropTargetInfo);
            var canEnter = dragInfo is not null
                && dragInfo.WindowHandle != IntPtr.Zero
                && dropSurface.TryResolveDropTarget(clientPoint, out dropTargetInfo);

            dropSurface.SetDropTarget(
                canEnter ? dropTargetInfo.TargetWindowHandle : IntPtr.Zero,
                canEnter && dropTargetInfo.InsertAfterTarget);
            dropSurface.UpdatePreviewForDrag(
                canEnter ? dragInfo : null,
                canEnter ? dropTargetInfo.InsertAfterWindowHandle : null);
            return canEnter;
        }

        public void HandleDragMove(
            Point clientPoint,
            IDropSurface dropSurface)
        {
            ArgumentNullException.ThrowIfNull(dropSurface);

            if (dropSurface.TryResolveDropTarget(clientPoint, out var dropTargetInfo))
            {
                dropSurface.SetDropTarget(dropTargetInfo.TargetWindowHandle, dropTargetInfo.InsertAfterTarget);
                dropSurface.UpdatePreviewForCurrentDrag(dropTargetInfo.InsertAfterWindowHandle);
                return;
            }

            dropSurface.ClearDragState();
        }

        public void HandleDrop(
            object data,
            Point clientPoint,
            IDropSurface dropSurface)
        {
            ArgumentNullException.ThrowIfNull(dropSurface);

            if (data is not TabDragInfo dragInfo
                || dragInfo.WindowHandle == IntPtr.Zero
                || !dropSurface.TryResolveDropTarget(clientPoint, out var dropTargetInfo)
                || dropTargetInfo.TargetWindowHandle == dragInfo.WindowHandle)
            {
                dropSurface.ClearDragState();
                return;
            }

            groupMutationService.MoveWindowRelativeToWindow(
                dragInfo.WindowHandle,
                dropTargetInfo.TargetWindowHandle,
                dropTargetInfo.InsertAfterWindowHandle);
            dropSurface.ClearDragState();
        }

        public void HandleDragExit(IDropSurface dropSurface)
        {
            ArgumentNullException.ThrowIfNull(dropSurface);
            dropSurface.ClearDragState();
        }

        public interface IDropSurface
        {
            bool TryResolveDropTarget(Point clientPoint, out ManagedGroupStripDropTargetInfo dropTargetInfo);

            void SetDropTarget(IntPtr windowHandle, bool insertAfter);

            void UpdatePreviewForDrag(TabDragInfo dragInfo, IntPtr? insertAfterWindowHandle);

            void UpdatePreviewForCurrentDrag(IntPtr? insertAfterWindowHandle);

            void ClearDragState();
        }
    }
}
