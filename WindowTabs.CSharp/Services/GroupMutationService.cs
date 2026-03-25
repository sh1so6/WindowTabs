using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowTabs.CSharp.Services
{
    internal sealed class GroupMutationService
    {
        private readonly GroupMembershipService groupMembershipService;
        private readonly RefreshCoordinator refreshCoordinator;

        public GroupMutationService(
            GroupMembershipService groupMembershipService,
            RefreshCoordinator refreshCoordinator)
        {
            this.groupMembershipService = groupMembershipService ?? throw new ArgumentNullException(nameof(groupMembershipService));
            this.refreshCoordinator = refreshCoordinator ?? throw new ArgumentNullException(nameof(refreshCoordinator));
        }

        public bool UngroupWindow(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return false;
            }

            if (!groupMembershipService.RemoveWindow(windowHandle))
            {
                return false;
            }

            refreshCoordinator.Refresh();
            return true;
        }

        public bool UngroupWindows(IEnumerable<IntPtr> windowHandles)
        {
            if (windowHandles == null)
            {
                return false;
            }

            var changed = false;
            foreach (var windowHandle in windowHandles.Where(handle => handle != IntPtr.Zero).Distinct().ToArray())
            {
                changed |= groupMembershipService.RemoveWindow(windowHandle);
            }

            if (changed)
            {
                refreshCoordinator.Refresh();
            }

            return changed;
        }

        public bool MoveWindowWithinGroup(IntPtr windowHandle, IntPtr groupHandle, IntPtr? insertAfterWindowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return false;
            }

            if (!groupMembershipService.MoveWindowWithinGroup(windowHandle, groupHandle, insertAfterWindowHandle))
            {
                return false;
            }
            refreshCoordinator.Refresh();
            return true;
        }

        public bool MoveWindowToGroup(IntPtr windowHandle, IntPtr targetGroupHandle, IntPtr? insertAfterWindowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return false;
            }

            if (!groupMembershipService.MoveWindowToGroup(windowHandle, targetGroupHandle, insertAfterWindowHandle))
            {
                return false;
            }

            refreshCoordinator.Refresh();
            return true;
        }

        public bool MoveWindowRelativeToWindow(IntPtr windowHandle, IntPtr targetWindowHandle, IntPtr? insertAfterWindowHandle)
        {
            if (windowHandle == IntPtr.Zero || targetWindowHandle == IntPtr.Zero || windowHandle == targetWindowHandle)
            {
                return false;
            }

            var targetGroupHandle = groupMembershipService.GetGroupHandleContainingWindow(targetWindowHandle);
            if (!targetGroupHandle.HasValue)
            {
                return false;
            }

            var sourceGroupHandle = groupMembershipService.GetGroupHandleContainingWindow(windowHandle);
            return sourceGroupHandle.HasValue && sourceGroupHandle.Value == targetGroupHandle.Value
                ? MoveWindowWithinGroup(windowHandle, targetGroupHandle.Value, insertAfterWindowHandle)
                : MoveWindowToGroup(windowHandle, targetGroupHandle.Value, insertAfterWindowHandle);
        }

        public bool MoveWindowsToNewGroup(IEnumerable<IntPtr> windowHandles, IntPtr? preferredHandle = null)
        {
            if (windowHandles == null)
            {
                return false;
            }

            var group = groupMembershipService.CreateGroupWithWindows(windowHandles, preferredHandle);
            if (group == null)
            {
                return false;
            }

            refreshCoordinator.Refresh();
            return true;
        }
    }
}
