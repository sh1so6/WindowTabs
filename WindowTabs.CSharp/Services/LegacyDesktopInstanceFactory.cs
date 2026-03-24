using System;
using Bemo;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyDesktopInstanceFactory
    {
        public LegacyDesktopInstance Create()
        {
            var notificationRouter = new LegacyDesktopNotificationRouter();
            var notificationBridge = new LegacyDesktopNotificationBridge(notificationRouter);
            var desktop = (IDesktop)new Desktop(notificationBridge);
            return new LegacyDesktopInstance(desktop, notificationRouter);
        }
    }
}
