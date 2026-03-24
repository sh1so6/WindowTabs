using Bemo;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyDesktopInstance
    {
        public LegacyDesktopInstance(
            IDesktop desktop,
            LegacyDesktopNotificationRouter notificationRouter)
        {
            Desktop = desktop;
            NotificationRouter = notificationRouter;
        }

        public IDesktop Desktop { get; }

        public LegacyDesktopNotificationRouter NotificationRouter { get; }
    }
}
