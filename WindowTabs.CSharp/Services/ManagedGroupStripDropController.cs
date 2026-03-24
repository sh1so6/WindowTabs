using System;
using System.Drawing;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripDropController
    {
        private readonly IDesktopRuntime desktopRuntime;
        private readonly GroupMutationService groupMutationService;

        public ManagedGroupStripDropController(
            IDesktopRuntime desktopRuntime,
            GroupMutationService groupMutationService)
        {
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
            this.groupMutationService = groupMutationService ?? throw new ArgumentNullException(nameof(groupMutationService));
        }

        public bool HandleDragEnter(
            object data,
            Point clientPoint,
            TryResolveDropTarget tryResolveDropTarget,
            Action<IntPtr, bool> setDropTarget,
            Action<TabDragInfo, IntPtr?> updatePreviewForDrag)
        {
            var dragInfo = data as TabDragInfo;
            var dropTargetInfo = default(ManagedGroupStripDropTargetInfo);
            var canEnter = dragInfo != null
                && dragInfo.WindowHandle != IntPtr.Zero
                && tryResolveDropTarget(clientPoint, out dropTargetInfo);

            setDropTarget(
                canEnter ? dropTargetInfo.TargetWindowHandle : IntPtr.Zero,
                canEnter && dropTargetInfo.InsertAfterTarget);
            updatePreviewForDrag(canEnter ? dragInfo : null, canEnter ? dropTargetInfo.InsertAfterWindowHandle : null);
            return canEnter;
        }

        public void HandleDragMove(
            Point clientPoint,
            TryResolveDropTarget tryResolveDropTarget,
            Action<IntPtr, bool> setDropTarget,
            Action<IntPtr?> updatePreviewForCurrentDrag,
            Action clearDragState)
        {
            if (tryResolveDropTarget(clientPoint, out var dropTargetInfo))
            {
                setDropTarget(dropTargetInfo.TargetWindowHandle, dropTargetInfo.InsertAfterTarget);
                updatePreviewForCurrentDrag(dropTargetInfo.InsertAfterWindowHandle);
                return;
            }

            clearDragState();
        }

        public void HandleDrop(
            object data,
            Point clientPoint,
            TryResolveDropTarget tryResolveDropTarget,
            Action clearDragState)
        {
            if (!(data is TabDragInfo dragInfo)
                || dragInfo.WindowHandle == IntPtr.Zero
                || !tryResolveDropTarget(clientPoint, out var dropTargetInfo)
                || dropTargetInfo.TargetWindowHandle == dragInfo.WindowHandle)
            {
                clearDragState();
                return;
            }

            var targetGroup = desktopRuntime.FindGroupContainingWindow(dropTargetInfo.TargetWindowHandle);
            if (targetGroup == null)
            {
                clearDragState();
                return;
            }

            var sourceGroup = desktopRuntime.FindGroupContainingWindow(dragInfo.WindowHandle);
            if (sourceGroup != null && sourceGroup.GroupHandle == targetGroup.GroupHandle)
            {
                groupMutationService.MoveWindowWithinGroup(
                    dragInfo.WindowHandle,
                    targetGroup.GroupHandle,
                    dropTargetInfo.InsertAfterWindowHandle);
                clearDragState();
                return;
            }

            groupMutationService.MoveWindowToGroup(
                dragInfo.WindowHandle,
                targetGroup.GroupHandle,
                dropTargetInfo.InsertAfterWindowHandle);
            clearDragState();
        }

        public void HandleDragExit(Action clearDragState)
        {
            clearDragState();
        }

        public delegate bool TryResolveDropTarget(Point clientPoint, out ManagedGroupStripDropTargetInfo dropTargetInfo);
    }
}
