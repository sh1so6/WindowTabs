using System;
using System.Collections.Generic;
using System.Linq;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class GroupMembershipService
    {
        private readonly IDesktopRuntime desktopRuntime;

        public GroupMembershipService(IDesktopRuntime desktopRuntime)
        {
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
        }

        public IWindowGroupRuntime GetOrCreateGroup(IntPtr? preferredHandle)
        {
            var existingGroup = preferredHandle.HasValue
                ? desktopRuntime.FindGroup(preferredHandle.Value)
                : null;
            return existingGroup ?? desktopRuntime.CreateGroup(preferredHandle);
        }

        public IWindowGroupRuntime FindGroupContainingWindow(IntPtr windowHandle)
        {
            return desktopRuntime.FindGroupContainingWindow(windowHandle);
        }

        public IntPtr? GetGroupHandleContainingWindow(IntPtr windowHandle)
        {
            return FindGroupContainingWindow(windowHandle)?.GroupHandle;
        }

        public bool RemoveWindow(IntPtr windowHandle)
        {
            return windowHandle != IntPtr.Zero
                && desktopRuntime.RemoveWindow(windowHandle).HasValue;
        }

        public bool IsWindowGrouped(IntPtr windowHandle)
        {
            return windowHandle != IntPtr.Zero && desktopRuntime.IsWindowGrouped(windowHandle);
        }

        public bool RemoveWindowFromGroup(IntPtr groupHandle, IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return false;
            }

            var group = desktopRuntime.FindGroup(groupHandle);
            if (group == null || !group.WindowHandles.Contains(windowHandle))
            {
                return false;
            }

            return desktopRuntime.RemoveWindow(windowHandle).HasValue;
        }

        public bool MoveWindowWithinGroup(IntPtr windowHandle, IntPtr groupHandle, IntPtr? insertAfterWindowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return false;
            }

            var group = desktopRuntime.FindGroup(groupHandle);
            if (group == null || !group.WindowHandles.Contains(windowHandle))
            {
                return false;
            }

            group.MoveWindowAfter(windowHandle, insertAfterWindowHandle);
            return true;
        }

        public bool MoveWindowToGroup(IntPtr windowHandle, IntPtr? targetGroupHandle, IntPtr? insertAfterWindowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return false;
            }

            var currentGroup = FindGroupContainingWindow(windowHandle);
            var targetGroup = GetOrCreateGroup(targetGroupHandle);
            if (currentGroup != null && currentGroup.GroupHandle == targetGroup.GroupHandle)
            {
                currentGroup.MoveWindowAfter(windowHandle, insertAfterWindowHandle);
                return true;
            }

            desktopRuntime.RemoveWindow(windowHandle);
            targetGroup.AddWindow(windowHandle, insertAfterWindowHandle);
            if (!insertAfterWindowHandle.HasValue)
            {
                targetGroup.MoveWindowAfter(windowHandle, null);
            }

            return true;
        }

        public bool MoveWindowToGroupContainingWindow(IntPtr windowHandle, IntPtr targetWindowHandle)
        {
            if (targetWindowHandle == IntPtr.Zero)
            {
                return false;
            }

            var targetGroup = FindGroupContainingWindow(targetWindowHandle);
            return targetGroup != null
                && MoveWindowToGroup(windowHandle, targetGroup.GroupHandle, targetWindowHandle);
        }

        public bool ApplyGroupingDecision(WindowGroupingDecision decision)
        {
            if (decision == null || decision.WindowHandle == IntPtr.Zero || IsWindowGrouped(decision.WindowHandle))
            {
                return false;
            }

            return MoveWindowToGroup(
                decision.WindowHandle,
                decision.TargetGroupHandle,
                decision.InsertAfterWindowHandle);
        }

        public bool ApplyRegroupingDecision(WindowGroupingDecision decision)
        {
            if (decision == null || decision.WindowHandle == IntPtr.Zero)
            {
                return false;
            }

            var currentGroup = FindGroupContainingWindow(decision.WindowHandle);
            if (currentGroup == null)
            {
                return false;
            }

            if (decision.TargetGroupHandle.HasValue
                && currentGroup.GroupHandle == decision.TargetGroupHandle.Value)
            {
                return false;
            }

            return MoveWindowToGroup(
                decision.WindowHandle,
                decision.TargetGroupHandle,
                decision.InsertAfterWindowHandle);
        }

        public bool ApplyReorderingDecision(WindowGroupingDecision decision)
        {
            if (decision == null || decision.WindowHandle == IntPtr.Zero)
            {
                return false;
            }

            var currentGroup = FindGroupContainingWindow(decision.WindowHandle);
            return currentGroup != null
                && MoveWindowWithinGroup(
                    decision.WindowHandle,
                    currentGroup.GroupHandle,
                    decision.InsertAfterWindowHandle);
        }

        public IWindowGroupRuntime CreateGroupWithWindows(IEnumerable<IntPtr> windowHandles, IntPtr? preferredHandle)
        {
            if (windowHandles == null)
            {
                return null;
            }

            var handles = windowHandles.Where(handle => handle != IntPtr.Zero).Distinct().ToList();
            if (handles.Count == 0)
            {
                return null;
            }

            var group = GetOrCreateGroup(preferredHandle);
            IntPtr? insertAfterWindowHandle = null;
            foreach (var handle in handles)
            {
                MoveWindowToGroup(handle, group.GroupHandle, insertAfterWindowHandle);
                insertAfterWindowHandle = handle;
            }

            return group;
        }

        public void DestroyGroup(IntPtr groupHandle)
        {
            if (groupHandle != IntPtr.Zero)
            {
                desktopRuntime.DestroyGroup(groupHandle);
            }
        }
    }
}
