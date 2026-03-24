using System;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyDesktopNotificationRouter
    {
        private Action<IntPtr> droppedWindowHandler = _ => { };

        public void SetDroppedWindowHandler(Action<IntPtr> handler)
        {
            droppedWindowHandler = handler ?? (_ => { });
        }

        public void NotifyDroppedWindow(IntPtr hwnd)
        {
            droppedWindowHandler(hwnd);
        }

        public void NotifyDragEnd()
        {
        }
    }
}
