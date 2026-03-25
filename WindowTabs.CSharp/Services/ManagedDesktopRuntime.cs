using System;
using System.Collections.Generic;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedDesktopRuntime : IDesktopRuntime
    {
        private readonly ManagedDesktopStateStore stateStore;
        private readonly ManagedDesktopInteractionState interactionState;
        private readonly ManagedDesktopLifecycleService lifecycleService;
        private bool isInitialized;

        public ManagedDesktopRuntime(
            ManagedDesktopStateStore stateStore,
            ManagedDesktopInteractionState interactionState,
            ManagedDesktopLifecycleService lifecycleService)
        {
            this.stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
            this.interactionState = interactionState ?? throw new ArgumentNullException(nameof(interactionState));
            this.lifecycleService = lifecycleService ?? throw new ArgumentNullException(nameof(lifecycleService));
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

        public void Initialize()
        {
            if (!isInitialized)
            {
                isInitialized = true;
            }

            lifecycleService.Initialize();
        }

        public void SaveState()
        {
            lifecycleService.SaveState();
        }
    }
}
