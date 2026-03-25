using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class PresentationMutationService
    {
        private readonly WindowPresentationStateStore windowPresentationStateStore;
        private readonly IDesktopRuntime desktopRuntime;
        private readonly RefreshCoordinator refreshCoordinator;

        public PresentationMutationService(
            WindowPresentationStateStore windowPresentationStateStore,
            IDesktopRuntime desktopRuntime,
            RefreshCoordinator refreshCoordinator)
        {
            this.windowPresentationStateStore = windowPresentationStateStore ?? throw new ArgumentNullException(nameof(windowPresentationStateStore));
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
            this.refreshCoordinator = refreshCoordinator ?? throw new ArgumentNullException(nameof(refreshCoordinator));
        }

        public void SetPinned(IntPtr windowHandle, bool pinned)
        {
            windowPresentationStateStore.SetPinned(windowHandle, pinned);
            refreshCoordinator.Refresh();
        }

        public void SetPinned(IEnumerable<IntPtr> windowHandles, bool pinned)
        {
            foreach (var windowHandle in EnumerateHandles(windowHandles))
            {
                windowPresentationStateStore.SetPinned(windowHandle, pinned);
            }

            refreshCoordinator.Refresh();
        }

        public void SetAlignment(IntPtr windowHandle, TabAlign alignment)
        {
            windowPresentationStateStore.SetAlignment(windowHandle, alignment);
            refreshCoordinator.Refresh();
        }

        public void SetAlignment(IEnumerable<IntPtr> windowHandles, TabAlign alignment)
        {
            foreach (var windowHandle in EnumerateHandles(windowHandles))
            {
                windowPresentationStateStore.SetAlignment(windowHandle, alignment);
            }

            refreshCoordinator.Refresh();
        }

        public void SetWindowName(IntPtr windowHandle, string name)
        {
            windowPresentationStateStore.SetWindowNameOverride(windowHandle, name);
            refreshCoordinator.Refresh();
        }

        public void ClearWindowName(IntPtr windowHandle)
        {
            windowPresentationStateStore.SetWindowNameOverride(windowHandle, null);
            refreshCoordinator.Refresh();
        }

        public void SetFillColor(IntPtr windowHandle, Color? color)
        {
            windowPresentationStateStore.SetFillColor(windowHandle, color);
            refreshCoordinator.Refresh();
        }

        public void SetUnderlineColor(IntPtr windowHandle, Color? color)
        {
            windowPresentationStateStore.SetUnderlineColor(windowHandle, color);
            refreshCoordinator.Refresh();
        }

        public void SetBorderColor(IntPtr windowHandle, Color? color)
        {
            windowPresentationStateStore.SetBorderColor(windowHandle, color);
            refreshCoordinator.Refresh();
        }

        public void ClearCustomColors(IntPtr windowHandle)
        {
            windowPresentationStateStore.SetFillColor(windowHandle, null);
            windowPresentationStateStore.SetUnderlineColor(windowHandle, null);
            windowPresentationStateStore.SetBorderColor(windowHandle, null);
            refreshCoordinator.Refresh();
        }

        public void SetGroupTabPosition(IntPtr groupHandle, string tabPosition)
        {
            var group = desktopRuntime.FindGroup(groupHandle);
            if (group == null)
            {
                return;
            }

            group.TabPosition = tabPosition;
            refreshCoordinator.Refresh();
        }

        public void SetGroupSnapMargin(IntPtr groupHandle, bool snapTabHeightMargin)
        {
            var group = desktopRuntime.FindGroup(groupHandle);
            if (group == null)
            {
                return;
            }

            group.SnapTabHeightMargin = snapTabHeightMargin;
            refreshCoordinator.Refresh();
        }

        private static IEnumerable<IntPtr> EnumerateHandles(IEnumerable<IntPtr> windowHandles)
        {
            return windowHandles?
                .Where(handle => handle != IntPtr.Zero)
                .Distinct()
                .ToArray()
                ?? Array.Empty<IntPtr>();
        }
    }
}
