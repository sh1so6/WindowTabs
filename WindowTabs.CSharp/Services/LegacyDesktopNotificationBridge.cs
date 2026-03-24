using System;
using Bemo;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyDesktopNotificationBridge : IDesktopNotification
    {
        private readonly LegacyDesktopNotificationRouter router;

        public LegacyDesktopNotificationBridge(LegacyDesktopNotificationRouter router)
        {
            this.router = router ?? throw new ArgumentNullException(nameof(router));
        }

        public void dragDrop(IntPtr hwnd)
        {
            router.NotifyDroppedWindow(hwnd);
        }

        public void dragEnd()
        {
            router.NotifyDragEnd();
        }
    }
}
