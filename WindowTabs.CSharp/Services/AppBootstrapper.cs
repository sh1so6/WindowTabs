using System;
using System.Windows.Forms;

namespace WindowTabs.CSharp.Services
{
    internal sealed class AppBootstrapper
    {
        private readonly SettingsSession settingsSession;
        private readonly ManagedDesktopRuntime managedDesktopRuntime;
        private readonly DesktopMonitoringService desktopMonitoringService;
        private readonly RefreshCoordinator refreshCoordinator;
        private readonly StartupComponentStatusService startupComponentStatusService;
        private readonly BootstrapParticipant[] failSoftShellParticipants;
        private readonly BootstrapParticipant[] failSoftUiRegistryParticipants;
        private readonly ShutdownParticipant[] shutdownParticipants;
        private bool initialized;

        public AppBootstrapper(
            SettingsSession settingsSession,
            ManagedDesktopRuntime managedDesktopRuntime,
            DesktopMonitoringService desktopMonitoringService,
            RefreshCoordinator refreshCoordinator,
            NotifyIconService notifyIconService,
            GlobalHotKeyService globalHotKeyService,
            NumericTabHotKeyService numericTabHotKeyService,
            ManagedGroupDragDropTargetRegistryLifecycleService managedGroupDragDropTargetRegistryLifecycleService,
            ManagedGroupStripRegistryLifecycleService managedGroupStripRegistryLifecycleService,
            StartupComponentStatusService startupComponentStatusService)
        {
            this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
            this.managedDesktopRuntime = managedDesktopRuntime ?? throw new ArgumentNullException(nameof(managedDesktopRuntime));
            this.desktopMonitoringService = desktopMonitoringService ?? throw new ArgumentNullException(nameof(desktopMonitoringService));
            this.refreshCoordinator = refreshCoordinator ?? throw new ArgumentNullException(nameof(refreshCoordinator));
            this.startupComponentStatusService = startupComponentStatusService ?? throw new ArgumentNullException(nameof(startupComponentStatusService));
            var checkedNotifyIconService = notifyIconService ?? throw new ArgumentNullException(nameof(notifyIconService));
            var checkedGlobalHotKeyService = globalHotKeyService ?? throw new ArgumentNullException(nameof(globalHotKeyService));
            var checkedNumericTabHotKeyService = numericTabHotKeyService ?? throw new ArgumentNullException(nameof(numericTabHotKeyService));
            var checkedManagedGroupDragDropTargetRegistry =
                managedGroupDragDropTargetRegistryLifecycleService ?? throw new ArgumentNullException(nameof(managedGroupDragDropTargetRegistryLifecycleService));
            var checkedManagedGroupStripRegistry =
                managedGroupStripRegistryLifecycleService ?? throw new ArgumentNullException(nameof(managedGroupStripRegistryLifecycleService));

            failSoftShellParticipants = new[]
            {
                BootstrapParticipant.Create("NotifyIconService", checkedNotifyIconService.Initialize),
                BootstrapParticipant.Create("GlobalHotKeyService", checkedGlobalHotKeyService.Initialize),
                BootstrapParticipant.Create("NumericTabHotKeyService", checkedNumericTabHotKeyService.Initialize),
            };
            failSoftUiRegistryParticipants = new[]
            {
                BootstrapParticipant.Create("ManagedGroupDragDropTargetRegistry", checkedManagedGroupDragDropTargetRegistry.Initialize),
                BootstrapParticipant.Create("ManagedGroupStripRegistry", checkedManagedGroupStripRegistry.Initialize),
            };
            shutdownParticipants = new[]
            {
                ShutdownParticipant.Create("ManagedDesktopRuntime", SaveRuntimeState),
                ShutdownParticipant.Create("ManagedGroupDragDropTargetRegistry", checkedManagedGroupDragDropTargetRegistry.Dispose),
                ShutdownParticipant.Create("ManagedGroupStripRegistry", checkedManagedGroupStripRegistry.Dispose),
                ShutdownParticipant.Create("NotifyIconService", checkedNotifyIconService.Dispose),
                ShutdownParticipant.Create("GlobalHotKeyService", checkedGlobalHotKeyService.Dispose),
                ShutdownParticipant.Create("NumericTabHotKeyService", checkedNumericTabHotKeyService.Dispose),
            };
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            startupComponentStatusService.Clear();
            // Core state is required before any shell/UI participant can run.
            InitializeCoreState();
            initialized = true;
            InitializeFailSoftParticipants(failSoftShellParticipants);
            InitializeFailSoftParticipants(failSoftUiRegistryParticipants);
            InitializeDesktopRuntime();
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            // Shutdown is best-effort: persist first, then release UI/shell resources even if one step fails.
            RunShutdownParticipants();
        }

        private void InitializeCoreState()
        {
            LocalizationService.Initialize(settingsSession.Current.LanguageName);
            refreshCoordinator.SetRefreshAction(() => desktopMonitoringService.RefreshNow("program-refresher"));
            Application.ApplicationExit += OnApplicationExit;
        }

        private void InitializeDesktopRuntime()
        {
            TryInitialize(
                "ManagedDesktopRuntime",
                managedDesktopRuntime.Initialize);
        }

        private void InitializeFailSoftParticipants(BootstrapParticipant[] participants)
        {
            foreach (var participant in participants)
            {
                TryInitialize(participant.Name, participant.Execute);
            }
        }

        private void SaveRuntimeState()
        {
            managedDesktopRuntime.SaveState();
        }

        private void RunShutdownParticipants()
        {
            foreach (var participant in shutdownParticipants)
            {
                TryRunOnExit(participant.Name, participant.Execute);
            }
        }

        private void TryRunOnExit(string componentName, Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                UnhandledExceptionLogger.Log(exception, "AppBootstrapper.OnApplicationExit." + componentName);
            }
        }

        private void TryInitialize(string componentName, Action initialize)
        {
            try
            {
                initialize();
                startupComponentStatusService.MarkHealthy(componentName);
            }
            catch (Exception exception)
            {
                startupComponentStatusService.MarkFailed(componentName, exception);
                UnhandledExceptionLogger.Log(exception, "AppBootstrapper." + componentName);
            }
        }

        private sealed class BootstrapParticipant
        {
            private BootstrapParticipant(string name, Action execute)
            {
                Name = name;
                Execute = execute;
            }

            public string Name { get; }

            public Action Execute { get; }

            public static BootstrapParticipant Create(string name, Action execute)
            {
                return new BootstrapParticipant(name, execute);
            }
        }

        private sealed class ShutdownParticipant
        {
            private ShutdownParticipant(string name, Action execute)
            {
                Name = name;
                Execute = execute;
            }

            public string Name { get; }

            public Action Execute { get; }

            public static ShutdownParticipant Create(string name, Action execute)
            {
                return new ShutdownParticipant(name, execute);
            }
        }
    }
}
