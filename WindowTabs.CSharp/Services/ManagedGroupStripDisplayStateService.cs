using System;
using System.Collections.Generic;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripDisplayStateService
    {
        private readonly DesktopSnapshotService desktopSnapshotService;
        private readonly SettingsSession settingsSession;
        private readonly ManagedGroupStripGroupOrderService groupOrderService;
        private readonly ManagedGroupStripDragSessionStateService dragSessionStateService;

        public ManagedGroupStripDisplayStateService(
            DesktopSnapshotService desktopSnapshotService,
            SettingsSession settingsSession,
            ManagedGroupStripGroupOrderService groupOrderService,
            ManagedGroupStripDragSessionStateService dragSessionStateService)
        {
            this.desktopSnapshotService = desktopSnapshotService ?? throw new ArgumentNullException(nameof(desktopSnapshotService));
            this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
            this.groupOrderService = groupOrderService ?? throw new ArgumentNullException(nameof(groupOrderService));
            this.dragSessionStateService = dragSessionStateService ?? throw new ArgumentNullException(nameof(dragSessionStateService));
        }

        public ManagedGroupStripDisplayState BuildDisplayState(
            GroupSnapshot group,
            ManagedGroupStripDragSessionState currentDragSessionState)
        {
            if (group == null)
            {
                return new ManagedGroupStripDisplayState(
                    IntPtr.Zero,
                    Array.Empty<IntPtr>(),
                    ManagedGroupStripDragSessionState.Empty,
                    new SettingsSnapshot(),
                    SettingsDefaults.CreateDefaultTabAppearance(),
                    IntPtr.Zero);
            }

            var activeWindowHandle = desktopSnapshotService.GetForegroundWindowHandle();
            var settings = settingsSession.Current;
            var appearance = settings.TabAppearance ?? SettingsDefaults.CreateDefaultTabAppearance();
            var orderedHandles = groupOrderService.OrderWindowHandles(group);
            var resolvedDragSession = dragSessionStateService.ResolveDisplayState(orderedHandles, currentDragSessionState);

            return new ManagedGroupStripDisplayState(
                group.GroupHandle,
                orderedHandles,
                resolvedDragSession,
                settings,
                appearance,
                activeWindowHandle);
        }
    }
}
