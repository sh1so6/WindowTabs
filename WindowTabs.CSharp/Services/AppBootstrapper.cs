using System;
using System.Windows.Forms;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class AppBootstrapper
    {
        private readonly SettingsSession settingsSession;
        private readonly ILocalizationContext localizationContext;
        private readonly IDesktopRuntime desktopRuntime;
        private readonly DesktopSessionCoordinator desktopSessionCoordinator;
        private readonly DesktopMonitoringService desktopMonitoringService;
        private readonly RefreshCoordinator refreshCoordinator;
        private readonly NotifyIconService notifyIconService;
        private readonly GlobalHotKeyService globalHotKeyService;
        private readonly ManagedGroupDragDropTargetRegistry managedGroupDragDropTargetRegistry;
        private readonly ManagedGroupStripRegistry managedGroupStripRegistry;
        private bool initialized;

        public AppBootstrapper(
            SettingsSession settingsSession,
            ILocalizationContext localizationContext,
            IDesktopRuntime desktopRuntime,
            DesktopSessionCoordinator desktopSessionCoordinator,
            DesktopMonitoringService desktopMonitoringService,
            RefreshCoordinator refreshCoordinator,
            NotifyIconService notifyIconService,
            GlobalHotKeyService globalHotKeyService,
            ManagedGroupDragDropTargetRegistry managedGroupDragDropTargetRegistry,
            ManagedGroupStripRegistry managedGroupStripRegistry)
        {
            this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
            this.localizationContext = localizationContext ?? throw new ArgumentNullException(nameof(localizationContext));
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
            this.desktopSessionCoordinator = desktopSessionCoordinator ?? throw new ArgumentNullException(nameof(desktopSessionCoordinator));
            this.desktopMonitoringService = desktopMonitoringService ?? throw new ArgumentNullException(nameof(desktopMonitoringService));
            this.refreshCoordinator = refreshCoordinator ?? throw new ArgumentNullException(nameof(refreshCoordinator));
            this.notifyIconService = notifyIconService ?? throw new ArgumentNullException(nameof(notifyIconService));
            this.globalHotKeyService = globalHotKeyService ?? throw new ArgumentNullException(nameof(globalHotKeyService));
            this.managedGroupDragDropTargetRegistry = managedGroupDragDropTargetRegistry ?? throw new ArgumentNullException(nameof(managedGroupDragDropTargetRegistry));
            this.managedGroupStripRegistry = managedGroupStripRegistry ?? throw new ArgumentNullException(nameof(managedGroupStripRegistry));
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            localizationContext.Initialize(settingsSession.Current.LanguageName);
            refreshCoordinator.SetRefreshAction(() => desktopMonitoringService.RefreshNow("program-refresher"));
            Application.ApplicationExit += OnApplicationExit;
            notifyIconService.Initialize();
            globalHotKeyService.Initialize();
            managedGroupDragDropTargetRegistry.Initialize();
            managedGroupStripRegistry.Initialize();

            if (desktopRuntime is IDesktopRuntimeBootstrapper runtimeBootstrapper)
            {
                runtimeBootstrapper.Initialize(desktopSessionCoordinator, desktopMonitoringService);
            }
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            if (desktopRuntime is IDesktopRuntimeStatePersistence statePersistence)
            {
                statePersistence.SaveState();
            }

            notifyIconService.Dispose();
            globalHotKeyService.Dispose();
            managedGroupDragDropTargetRegistry.Dispose();
            managedGroupStripRegistry.Dispose();
        }
    }
}
