using System;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedDesktopLifecycleService
    {
        private readonly ManagedDesktopStateStore stateStore;
        private readonly AppBehaviorState appBehaviorState;
        private readonly SettingsSession settingsSession;
        private readonly ManagedTabGroupPersistenceService persistenceService;
        private readonly WindowPresentationStateStore windowPresentationStateStore;
        private bool initialized;

        public ManagedDesktopLifecycleService(
            ManagedDesktopStateStore stateStore,
            AppBehaviorState appBehaviorState,
            SettingsSession settingsSession,
            ManagedTabGroupPersistenceService persistenceService,
            WindowPresentationStateStore windowPresentationStateStore)
        {
            this.stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
            this.appBehaviorState = appBehaviorState ?? throw new ArgumentNullException(nameof(appBehaviorState));
            this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
            this.persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
            this.windowPresentationStateStore = windowPresentationStateStore ?? throw new ArgumentNullException(nameof(windowPresentationStateStore));
        }

        public void Initialize()
        {
            if (!initialized)
            {
                initialized = true;
                settingsSession.Changed += OnSettingsChanged;
                appBehaviorState.DisabledChanged += OnDisabledChanged;
            }

            ResetAndRestoreState();
        }

        public void SaveState()
        {
            if (!appBehaviorState.IsDisabled)
            {
                persistenceService.SaveGroups(stateStore.Groups);
            }
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            stateStore.SyncGroupDefaults();
        }

        private void OnDisabledChanged(object sender, EventArgs e)
        {
            if (appBehaviorState.IsDisabled)
            {
                SaveState();
                ResetRuntimeState();
                return;
            }

            ResetAndRestoreState();
        }

        private void ResetAndRestoreState()
        {
            ResetRuntimeState();

            if (!appBehaviorState.IsDisabled)
            {
                persistenceService.RestoreGroups(stateStore);
            }
        }

        private void ResetRuntimeState()
        {
            stateStore.Reset();
            windowPresentationStateStore.Reset();
        }
    }
}
