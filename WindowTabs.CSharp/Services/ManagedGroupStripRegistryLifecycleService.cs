using System;
using System.Collections.Generic;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripRegistryLifecycleService
    {
        private readonly DesktopMonitoringService desktopMonitoringService;
        private readonly SettingsSession settingsSession;
        private readonly ManagedGroupStripFormFactory formFactory;
        private readonly ManagedGroupStripRegistrySyncService registrySyncService;
        private readonly IDesktopRuntime desktopRuntime;
        private readonly Dictionary<IntPtr, ManagedGroupStripForm> forms = new Dictionary<IntPtr, ManagedGroupStripForm>();
        private bool initialized;
        private bool disposed;

        public ManagedGroupStripRegistryLifecycleService(
            DesktopMonitoringService desktopMonitoringService,
            SettingsSession settingsSession,
            ManagedGroupStripFormFactory formFactory,
            ManagedGroupStripRegistrySyncService registrySyncService,
            IDesktopRuntime desktopRuntime)
        {
            this.desktopMonitoringService = desktopMonitoringService ?? throw new ArgumentNullException(nameof(desktopMonitoringService));
            this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
            this.formFactory = formFactory ?? throw new ArgumentNullException(nameof(formFactory));
            this.registrySyncService = registrySyncService ?? throw new ArgumentNullException(nameof(registrySyncService));
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            desktopMonitoringService.StateChanged += OnInputsChanged;
            settingsSession.Changed += OnInputsChanged;
            SyncForms();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            desktopMonitoringService.StateChanged -= OnInputsChanged;
            settingsSession.Changed -= OnInputsChanged;
            registrySyncService.ClearForms(forms);
        }

        private void OnInputsChanged(object sender, EventArgs e)
        {
            SyncForms();
        }

        private void SyncForms()
        {
            registrySyncService.SyncForms(desktopRuntime, desktopMonitoringService.CurrentState, forms, formFactory);
        }
    }
}
