using System;
using System.Collections.Generic;
using System.Linq;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class GroupMutationService
    {
        private readonly IDesktopRuntime desktopRuntime;
        private readonly IProgramRefresher refresher;

        public GroupMutationService(
            IDesktopRuntime desktopRuntime,
            IProgramRefresher refresher)
        {
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
            this.refresher = refresher ?? throw new ArgumentNullException(nameof(refresher));
        }

        public bool UngroupWindow(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return false;
            }

            var removedGroup = desktopRuntime.RemoveWindow(windowHandle);
            if (!removedGroup.HasValue)
            {
                return false;
            }

            refresher.Refresh();
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
                changed |= desktopRuntime.RemoveWindow(windowHandle).HasValue;
            }

            if (changed)
            {
                refresher.Refresh();
            }

            return changed;
        }

        public bool MoveWindowWithinGroup(IntPtr windowHandle, IntPtr groupHandle, IntPtr? insertAfterWindowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return false;
            }

            var group = desktopRuntime.FindGroup(groupHandle);
            if (group == null)
            {
                return false;
            }

            group.MoveWindowAfter(windowHandle, insertAfterWindowHandle);
            refresher.Refresh();
            return true;
        }

        public bool MoveWindowToGroup(IntPtr windowHandle, IntPtr targetGroupHandle, IntPtr? insertAfterWindowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return false;
            }

            var targetGroup = desktopRuntime.FindGroup(targetGroupHandle);
            if (targetGroup == null)
            {
                return false;
            }

            desktopRuntime.RemoveWindow(windowHandle);
            targetGroup.AddWindow(windowHandle, insertAfterWindowHandle);
            if (!insertAfterWindowHandle.HasValue)
            {
                targetGroup.MoveWindowAfter(windowHandle, null);
            }

            refresher.Refresh();
            return true;
        }
    }
}
