using System;
using Bemo;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyDesktopHostFactory
    {
        private readonly LegacyDesktopServiceBundleFactory serviceBundleFactory;
        private readonly LegacyDesktopServiceRegistrar serviceRegistrar;
        private readonly LegacyDesktopInstanceFactory desktopInstanceFactory;

        public LegacyDesktopHostFactory(
            LegacyDesktopServiceBundleFactory serviceBundleFactory,
            LegacyDesktopServiceRegistrar serviceRegistrar,
            LegacyDesktopInstanceFactory desktopInstanceFactory)
        {
            this.serviceBundleFactory = serviceBundleFactory ?? throw new ArgumentNullException(nameof(serviceBundleFactory));
            this.serviceRegistrar = serviceRegistrar ?? throw new ArgumentNullException(nameof(serviceRegistrar));
            this.desktopInstanceFactory = desktopInstanceFactory ?? throw new ArgumentNullException(nameof(desktopInstanceFactory));
        }

        public LegacyDesktopHost Create()
        {
            var services = serviceBundleFactory.Create();
            serviceRegistrar.Register(services);
            var desktopInstance = desktopInstanceFactory.Create();
            return new LegacyDesktopHost(desktopInstance, services, desktopInstance.NotificationRouter);
        }
    }
}
