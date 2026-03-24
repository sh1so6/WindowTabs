using Bemo;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyDesktopHost
    {
        public LegacyDesktopHost(
            LegacyDesktopInstance desktopInstance,
            LegacyDesktopServiceBundle services,
            LegacyDesktopNotificationRouter notificationRouter)
        {
            Desktop = desktopInstance.Desktop;
            Services = services;
            NotificationRouter = notificationRouter;
        }

        public IDesktop Desktop { get; }

        public LegacyDesktopServiceBundle Services { get; }

        public LegacyDesktopNotificationRouter NotificationRouter { get; }
    }
}
