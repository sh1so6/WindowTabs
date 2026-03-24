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
        private readonly IProgramRefresher refresher;

        public PresentationMutationService(
            WindowPresentationStateStore windowPresentationStateStore,
            IDesktopRuntime desktopRuntime,
            IProgramRefresher refresher)
        {
            this.windowPresentationStateStore = windowPresentationStateStore ?? throw new ArgumentNullException(nameof(windowPresentationStateStore));
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
            this.refresher = refresher ?? throw new ArgumentNullException(nameof(refresher));
        }

        public void SetPinned(IntPtr windowHandle, bool pinned)
        {
            windowPresentationStateStore.SetPinned(windowHandle, pinned);
            refresher.Refresh();
        }

        public void SetPinned(IEnumerable<IntPtr> windowHandles, bool pinned)
        {
            foreach (var windowHandle in EnumerateHandles(windowHandles))
            {
                windowPresentationStateStore.SetPinned(windowHandle, pinned);
            }

            refresher.Refresh();
        }

        public void SetAlignment(IntPtr windowHandle, TabAlign alignment)
        {
            windowPresentationStateStore.SetAlignment(windowHandle, alignment);
            refresher.Refresh();
        }

        public void SetAlignment(IEnumerable<IntPtr> windowHandles, TabAlign alignment)
        {
            foreach (var windowHandle in EnumerateHandles(windowHandles))
            {
                windowPresentationStateStore.SetAlignment(windowHandle, alignment);
            }

            refresher.Refresh();
        }

        public void SetWindowName(IntPtr windowHandle, string name)
        {
            windowPresentationStateStore.SetWindowNameOverride(windowHandle, name);
            refresher.Refresh();
        }

        public void ClearWindowName(IntPtr windowHandle)
        {
            windowPresentationStateStore.SetWindowNameOverride(windowHandle, null);
            refresher.Refresh();
        }

        public void SetFillColor(IntPtr windowHandle, Color? color)
        {
            windowPresentationStateStore.SetFillColor(windowHandle, color);
            refresher.Refresh();
        }

        public void SetUnderlineColor(IntPtr windowHandle, Color? color)
        {
            windowPresentationStateStore.SetUnderlineColor(windowHandle, color);
            refresher.Refresh();
        }

        public void SetBorderColor(IntPtr windowHandle, Color? color)
        {
            windowPresentationStateStore.SetBorderColor(windowHandle, color);
            refresher.Refresh();
        }

        public void ClearCustomColors(IntPtr windowHandle)
        {
            windowPresentationStateStore.SetFillColor(windowHandle, null);
            windowPresentationStateStore.SetUnderlineColor(windowHandle, null);
            windowPresentationStateStore.SetBorderColor(windowHandle, null);
            refresher.Refresh();
        }

        public void SetGroupTabPosition(IntPtr groupHandle, string tabPosition)
        {
            var group = desktopRuntime.FindGroup(groupHandle);
            if (group == null)
            {
                return;
            }

            group.TabPosition = tabPosition;
            refresher.Refresh();
        }

        public void SetGroupSnapMargin(IntPtr groupHandle, bool snapTabHeightMargin)
        {
            var group = desktopRuntime.FindGroup(groupHandle);
            if (group == null)
            {
                return;
            }

            group.SnapTabHeightMargin = snapTabHeightMargin;
            refresher.Refresh();
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
