using System;
using System.Collections.Generic;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedDesktopRuntime : IDesktopRuntime, IDesktopRuntimeBootstrapper, IDesktopRuntimeStatePersistence
    {
        private readonly ManagedDesktopStateStore stateStore;
        private readonly ManagedDesktopInteractionState interactionState;
        private readonly AppBehaviorState appBehaviorState;
        private readonly SettingsSession settingsSession;
        private readonly ManagedTabGroupPersistenceService persistenceService;
        private readonly WindowPresentationStateStore windowPresentationStateStore;
        private bool isInitialized;

        public ManagedDesktopRuntime(
            ManagedDesktopStateStore stateStore,
            ManagedDesktopInteractionState interactionState,
            AppBehaviorState appBehaviorState,
            SettingsSession settingsSession,
            ManagedTabGroupPersistenceService persistenceService,
            WindowPresentationStateStore windowPresentationStateStore)
        {
            this.stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
            this.interactionState = interactionState ?? throw new ArgumentNullException(nameof(interactionState));
            this.appBehaviorState = appBehaviorState ?? throw new ArgumentNullException(nameof(appBehaviorState));
            this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
            this.persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
            this.windowPresentationStateStore = windowPresentationStateStore ?? throw new ArgumentNullException(nameof(windowPresentationStateStore));
        }

        public bool IsDragging => interactionState.IsDragging;

        public IReadOnlyList<IWindowGroupRuntime> Groups => stateStore.Groups;

        public IWindowGroupRuntime CreateGroup(IntPtr? preferredHandle)
        {
            return stateStore.CreateGroup(preferredHandle);
        }

        public IWindowGroupRuntime FindGroup(IntPtr groupHandle)
        {
            return stateStore.FindGroup(groupHandle);
        }

        public IWindowGroupRuntime FindGroupContainingWindow(IntPtr windowHandle)
        {
            return stateStore.FindGroupContainingWindow(windowHandle);
        }

        public bool IsWindowGrouped(IntPtr windowHandle)
        {
            return stateStore.IsWindowGrouped(windowHandle);
        }

        public void DestroyGroup(IntPtr groupHandle)
        {
            stateStore.DestroyGroup(groupHandle);
        }

        public IntPtr? RemoveWindow(IntPtr windowHandle)
        {
            return stateStore.RemoveWindow(windowHandle);
        }

        public void RemoveClosedWindows(ISet<IntPtr> activeWindowHandles)
        {
            stateStore.RemoveClosedWindows(activeWindowHandles);
        }

        public void Initialize(DesktopSessionCoordinator desktopSessionCoordinator, DesktopMonitoringService desktopMonitoringService)
        {
            if (!isInitialized)
            {
                isInitialized = true;
                settingsSession.Changed += OnSettingsChanged;
                appBehaviorState.DisabledChanged += OnDisabledChanged;
            }

            stateStore.Reset();
            windowPresentationStateStore.Reset();
            if (!appBehaviorState.IsDisabled)
            {
                persistenceService.RestoreGroups(stateStore);
            }
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            stateStore.SyncGroupDefaults();
        }

        public void SaveState()
        {
            if (!appBehaviorState.IsDisabled)
            {
                persistenceService.SaveGroups(stateStore.Groups);
            }
        }

        private void OnDisabledChanged(object sender, EventArgs e)
        {
            if (appBehaviorState.IsDisabled)
            {
                persistenceService.SaveGroups(stateStore.Groups);
                stateStore.Reset();
                windowPresentationStateStore.Reset();
                return;
            }

            stateStore.Reset();
            windowPresentationStateStore.Reset();
            persistenceService.RestoreGroups(stateStore);
        }
    }
}
