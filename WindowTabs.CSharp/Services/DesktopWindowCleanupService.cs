using System;
using System.Collections.Generic;
using System.Linq;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class DesktopWindowCleanupService
    {
        private readonly IDesktopRuntime desktopRuntime;
        private readonly DesktopSessionStateService sessionStateService;
        private readonly GroupMembershipService groupMembershipService;
        private readonly WindowPresentationStateStore windowPresentationStateStore;

        public DesktopWindowCleanupService(
            IDesktopRuntime desktopRuntime,
            DesktopSessionStateService sessionStateService,
            GroupMembershipService groupMembershipService,
            WindowPresentationStateStore windowPresentationStateStore)
        {
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
            this.sessionStateService = sessionStateService ?? throw new ArgumentNullException(nameof(sessionStateService));
            this.groupMembershipService = groupMembershipService ?? throw new ArgumentNullException(nameof(groupMembershipService));
            this.windowPresentationStateStore = windowPresentationStateStore ?? throw new ArgumentNullException(nameof(windowPresentationStateStore));
        }

        public void CleanupDestroyedWindow(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            sessionStateService.ForgetWindow(windowHandle);
            groupMembershipService.RemoveWindow(windowHandle);
            windowPresentationStateStore.RemoveWindow(windowHandle);
        }

        public void CleanupClosedWindows(IReadOnlyList<WindowSnapshot> windows)
        {
            var activeHandles = new HashSet<IntPtr>(
                (windows ?? Array.Empty<WindowSnapshot>()).Select(window => window.Handle));

            sessionStateService.RemoveClosedWindows(activeHandles);
            desktopRuntime.RemoveClosedWindows(activeHandles);
            windowPresentationStateStore.RemoveClosedWindows(activeHandles);
        }
    }
}
