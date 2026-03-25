using Microsoft.Extensions.DependencyInjection;
using WindowTabs.CSharp.UI;

namespace WindowTabs.CSharp.Services
{
    internal static class ServiceCollectionUiExtensions
    {
        public static IServiceCollection AddWindowTabsUiServices(this IServiceCollection services)
        {
            return services
                .AddShellLifecycleServices()
                .AddWinFormsSurfaceServices();
        }

        private static IServiceCollection AddShellLifecycleServices(this IServiceCollection services)
        {
            services.AddSingleton<NotifyIconService>();
            services.AddSingleton<GlobalHotKeyService>();
            services.AddSingleton<NumericTabHotKeyService>();
            services.AddSingleton<AppBootstrapper>();
            return services;
        }

        private static IServiceCollection AddWinFormsSurfaceServices(this IServiceCollection services)
        {
            services.AddSingleton<WindowTabsStatusSummaryBuilder>();
            services.AddSingleton<ProgramsSettingsControl>();
            services.AddSingleton<WorkspaceSettingsControl>();
            services.AddSingleton<AppearanceSettingsControl>();
            services.AddSingleton<BehaviorSettingsControl>();
            services.AddSingleton<DiagnosticsSettingsControl>();
            services.AddSingleton<WindowTabsShellForm>();
            return services;
        }
    }
}
