using Microsoft.Extensions.DependencyInjection;

namespace WindowTabs.CSharp.Services
{
    internal static class ServiceCollectionCoreExtensions
    {
        public static IServiceCollection AddWindowTabsCoreServices(this IServiceCollection services)
        {
            return services
                .AddSharedSettingsServices()
                .AddApplicationLifecycleServices()
                .AddPersistedUserActionServices();
        }

        private static IServiceCollection AddSharedSettingsServices(this IServiceCollection services)
        {
            services.AddSingleton(new SettingsStore(isStandalone: false));
            services.AddSingleton<SettingsSession>();
            return services;
        }

        private static IServiceCollection AddApplicationLifecycleServices(this IServiceCollection services)
        {
            services.AddSingleton<AppLifecycleState>();
            services.AddSingleton<AppBehaviorState>();
            services.AddSingleton<AppRestartService>();
            services.AddSingleton<StartupComponentStatusService>();
            services.AddSingleton<RefreshCoordinator>();
            return services;
        }

        private static IServiceCollection AddPersistedUserActionServices(this IServiceCollection services)
        {
            services.AddSingleton<FilterService>();
            services.AddSingleton<LauncherService>();
            services.AddSingleton<NewWindowLaunchSupport>();
            services.AddSingleton<PendingWindowLaunchTracker>();
            services.AddSingleton<ProcessSettingsService>();
            services.AddSingleton<HotKeySettingsStore>();
            services.AddSingleton<TabAppearancePresetCatalog>();
            services.AddSingleton<ManagerViewRequestDispatcher>();
            services.AddSingleton<WorkspaceWindowMatchService>();
            services.AddSingleton<WorkspaceLayoutSerializationService>();
            services.AddSingleton<WorkspaceLayoutsService>();
            return services;
        }
    }
}
