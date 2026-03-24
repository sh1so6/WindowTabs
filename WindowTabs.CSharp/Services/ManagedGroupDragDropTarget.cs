using System;
using System.Drawing;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupDragDropTarget : IDragDropTarget
    {
        private readonly IntPtr targetWindowHandle;
        private readonly IDesktopRuntime desktopRuntime;
        private readonly IProgramRefresher refresher;

        public ManagedGroupDragDropTarget(
            IntPtr targetWindowHandle,
            IDesktopRuntime desktopRuntime,
            IProgramRefresher refresher)
        {
            this.targetWindowHandle = targetWindowHandle;
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
            this.refresher = refresher ?? throw new ArgumentNullException(nameof(refresher));
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

            var targetGroup = desktopRuntime.FindGroupContainingWindow(targetWindowHandle);
            if (targetGroup == null)
            {
                return;
            }

            var sourceGroup = desktopRuntime.FindGroupContainingWindow(dragInfo.WindowHandle);
            if (sourceGroup != null && sourceGroup.GroupHandle == targetGroup.GroupHandle)
            {
                targetGroup.MoveWindowAfter(dragInfo.WindowHandle, targetWindowHandle);
                refresher.Refresh();
                return;
            }

            desktopRuntime.RemoveWindow(dragInfo.WindowHandle);
            targetGroup.AddWindow(dragInfo.WindowHandle, targetWindowHandle);
            refresher.Refresh();
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

            return desktopRuntime.FindGroupContainingWindow(targetWindowHandle) != null;
        }
    }
}
