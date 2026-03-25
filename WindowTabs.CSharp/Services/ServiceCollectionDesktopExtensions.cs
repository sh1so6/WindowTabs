using Microsoft.Extensions.DependencyInjection;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal static class ServiceCollectionDesktopExtensions
    {
        public static IServiceCollection AddWindowTabsDesktopServices(this IServiceCollection services)
        {
            return services
                .AddDesktopStateMutationServices()
                .AddDesktopRefreshPipelineServices()
                .AddManagedDesktopRuntimeServices()
                .AddDesktopMonitoringServices();
        }

        private static IServiceCollection AddDesktopStateMutationServices(this IServiceCollection services)
        {
            services.AddSingleton<WindowPresentationStateStore>();
            services.AddSingleton<GroupVisualOrderService>();
            services.AddSingleton<GroupWindowActivationService>();
            services.AddSingleton<GroupMembershipService>();
            services.AddSingleton<GroupMutationService>();
            services.AddSingleton<PresentationMutationService>();
            services.AddSingleton<PresentationDialogService>();
            services.AddSingleton<WindowCloseService>();
            services.AddSingleton<WindowDetachService>();
            return services;
        }

        private static IServiceCollection AddDesktopRefreshPipelineServices(this IServiceCollection services)
        {
            services.AddSingleton<DesktopSnapshotService>();
            services.AddSingleton<DesktopWindowCatalogService>();
            services.AddSingleton<DesktopGroupingRuleService>();
            services.AddSingleton<DesktopGroupSnapshotService>();
            services.AddSingleton<DesktopRefreshResultFactory>();
            services.AddSingleton<DesktopWindowCleanupService>();
            services.AddSingleton<DesktopRefreshWorkflowService>();
            services.AddSingleton<DesktopPlanExecutionService>();
            services.AddSingleton<DesktopSessionStateService>();
            return services;
        }

        private static IServiceCollection AddManagedDesktopRuntimeServices(this IServiceCollection services)
        {
            return services
                .AddSingleton<ManagedWindowGroupRuntimeFactory>()
                .AddSingleton<ManagedGroupDefaultsService>()
                .AddSingleton<ManagedDesktopStateStore>()
                .AddSingleton<ManagedTabGroupPersistenceService>()
                .AddSingleton<ManagedDesktopLifecycleService>()
                .AddSingleton<ManagedDesktopRuntime>()
                .AddSingleton<DesktopPlannerService>()
                .AddSingleton<IDesktopRuntime>(serviceProvider => serviceProvider.GetRequiredService<ManagedDesktopRuntime>());
        }

        private static IServiceCollection AddDesktopMonitoringServices(this IServiceCollection services)
        {
            services.AddSingleton<DesktopMonitorStateFactory>();
            services.AddSingleton<DesktopMonitoringService>();
            return services;
        }
    }
}
