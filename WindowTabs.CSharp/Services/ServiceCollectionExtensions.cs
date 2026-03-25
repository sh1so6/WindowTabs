using Microsoft.Extensions.DependencyInjection;

namespace WindowTabs.CSharp.Services
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWindowTabsServices(this IServiceCollection services)
        {
            return services
                .AddWindowTabsCoreServices()
                .AddWindowTabsDesktopServices()
                .AddWindowTabsManagedGroupServices()
                .AddWindowTabsUiServices();
        }
    }
}
