using Microsoft.Extensions.DependencyInjection;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.UI;

namespace WindowTabs.CSharp.Services
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddWindowTabsMigrationServices(this IServiceCollection services)
        {
            services.AddSingleton(new SettingsStore(isStandalone: false));
            services.AddSingleton<SettingsSession>();
            services.AddSingleton<ILocalizationContext, LocalizationContext>();
            services.AddSingleton<AppLifecycleState>();
            services.AddSingleton<AppBehaviorState>();
            services.AddSingleton<AppRestartService>();
            services.AddSingleton<RefreshCoordinator>();
            services.AddSingleton<IProgramRefresher>(serviceProvider => serviceProvider.GetRequiredService<RefreshCoordinator>());
            services.AddSingleton<FilterService>();
            services.AddSingleton<LauncherService>();
            services.AddSingleton<NewWindowLaunchSupport>();
            services.AddSingleton<PendingWindowLaunchTracker>();
            services.AddSingleton<ProcessSettingsService>();
            services.AddSingleton<WindowPresentationStateStore>();
            services.AddSingleton<GroupVisualOrderService>();
            services.AddSingleton<GroupWindowActivationService>();
            services.AddSingleton<GroupMutationService>();
            services.AddSingleton<WindowCloseService>();
            services.AddSingleton<WindowDetachService>();
            services.AddSingleton<HotKeySettingsStore>();
            services.AddSingleton<DesktopSnapshotService>();
            services.AddSingleton<ManagedDesktopInteractionState>();
            services.AddSingleton<IDragDropParent, ManagedDesktopDragDropParent>();
            services.AddSingleton<ManagedDragDropController>();
            services.AddSingleton<IDragDrop>(serviceProvider => serviceProvider.GetRequiredService<ManagedDragDropController>());
            services.AddSingleton<LegacyProgramLifecycle>();
            services.AddSingleton<LegacyAppWindowCatalog>();
            services.AddSingleton<LegacyNewWindowLauncher>();
            services.AddSingleton<LegacyProgramSettingsFacade>();
            services.AddSingleton<LegacyWindowPresentationAdapter>();
            services.AddSingleton<TabAppearancePresetCatalog>();
            services.AddSingleton<BemoSettingsValueConverter>();
            services.AddSingleton<LegacySettingsBridge>();
            services.AddSingleton<LegacyFilterServiceBridge>();
            services.AddSingleton<ManagerViewRequestDispatcher>();
            services.AddSingleton<LegacyManagerViewBridge>();
            services.AddSingleton<LegacyTabAppearanceCatalog>();
            services.AddSingleton<LegacyDesktopServiceBundleFactory>();
            services.AddSingleton<LegacyDesktopServiceRegistrar>();
            services.AddSingleton<LegacyDesktopInstanceFactory>();
            services.AddSingleton<LegacyDesktopHostFactory>();
            services.AddSingleton<LegacyGroupTabOrderService>();
            services.AddSingleton<LegacyWindowGroupRuntimeFactory>();
            services.AddSingleton<ManagedWindowGroupRuntimeFactory>();
            services.AddSingleton<ManagedGroupDefaultsService>();
            services.AddSingleton<ManagedDesktopStateStore>();
            services.AddSingleton<ManagedTabGroupPersistenceService>();
            services.AddSingleton<ManagedDesktopRuntime>();
            services.AddSingleton<ManagedGroupDragDropTargetRegistry>();
            services.AddSingleton<ManagedGroupStripRegistry>();
            services.AddSingleton<ManagedDesktopRuntimeFactory>();
            services.AddSingleton<LegacyDesktopRuntimeFactory>();
            services.AddSingleton<DesktopPlannerService>();
            services.AddSingleton<DesktopRuntimeOptions>();
            services.AddSingleton<DesktopRuntimeSelection>();
            services.AddSingleton<DesktopRuntimeFactory>();

            services.AddSingleton<IDesktopRuntime>(serviceProvider =>
                serviceProvider.GetRequiredService<DesktopRuntimeFactory>().Create());

            services.AddSingleton<DesktopSessionCoordinator>();
            services.AddSingleton<DesktopMonitoringService>();
            services.AddSingleton<WorkspaceLayoutsService>();
            services.AddSingleton<NotifyIconService>();
            services.AddSingleton<GlobalHotKeyService>();
            services.AddSingleton<NumericTabHotKeyService>();
            services.AddSingleton<AppBootstrapper>();
            services.AddSingleton<ProgramsSettingsControl>();
            services.AddSingleton<WorkspaceSettingsControl>();
            services.AddSingleton<AppearanceSettingsControl>();
            services.AddSingleton<BehaviorSettingsControl>();
            services.AddSingleton<DiagnosticsSettingsControl>();
            services.AddSingleton<MigrationShellForm>();
            return services;
        }
    }
}
