using System;
using System.Drawing;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupDragDropTarget : IDragDropTarget
    {
        private readonly IntPtr targetWindowHandle;
        private readonly GroupMutationService groupMutationService;
        private readonly GroupMembershipService groupMembershipService;

        public ManagedGroupDragDropTarget(
            IntPtr targetWindowHandle,
            GroupMutationService groupMutationService,
            GroupMembershipService groupMembershipService)
        {
            this.targetWindowHandle = targetWindowHandle;
            this.groupMutationService = groupMutationService ?? throw new ArgumentNullException(nameof(groupMutationService));
            this.groupMembershipService = groupMembershipService ?? throw new ArgumentNullException(nameof(groupMembershipService));
        }

        public bool OnDragEnter(object data, Point clientPoint)
        {
            return CanHandle(data);
        }

        public void OnDragMove(Point clientPoint)
        {
        }

        public void OnDrop(object data, Point clientPoint)
        {
            if (!(data is TabDragInfo dragInfo) || dragInfo.WindowHandle == IntPtr.Zero)
            {
                return;
            }

            if (dragInfo.WindowHandle == targetWindowHandle)
            {
                return;
            }

            groupMutationService.MoveWindowRelativeToWindow(dragInfo.WindowHandle, targetWindowHandle, targetWindowHandle);
        }

        public void OnDragExit()
        {
        }

        public void OnDragBegin()
        {
        }

        public void OnDragEnd()
        {
        }

        private bool CanHandle(object data)
        {
            if (!(data is TabDragInfo dragInfo) || dragInfo.WindowHandle == IntPtr.Zero)
            {
                return false;
            }

            if (dragInfo.WindowHandle == targetWindowHandle)
            {
                return false;
            }

            return groupMembershipService.GetGroupHandleContainingWindow(targetWindowHandle).HasValue;
        }
    }
}
