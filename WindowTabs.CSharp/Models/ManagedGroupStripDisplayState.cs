using System;
using System.Collections.Generic;

namespace WindowTabs.CSharp.Models
{
    internal sealed class ManagedGroupStripDisplayState
    {
        public ManagedGroupStripDisplayState(
            IntPtr groupHandle,
            IReadOnlyList<IntPtr> actualGroupWindowHandles,
            ManagedGroupStripDragSessionState dragSessionState,
            SettingsSnapshot settings,
            TabAppearanceInfo appearance,
            IntPtr activeWindowHandle)
        {
            GroupHandle = groupHandle;
            ActualGroupWindowHandles = actualGroupWindowHandles ?? Array.Empty<IntPtr>();
            DragSessionState = dragSessionState ?? ManagedGroupStripDragSessionState.Empty;
            Settings = settings ?? new SettingsSnapshot();
            Appearance = appearance ?? new TabAppearanceInfo();
            ActiveWindowHandle = activeWindowHandle;
        }

        public IntPtr GroupHandle { get; }

        public IReadOnlyList<IntPtr> ActualGroupWindowHandles { get; }

        public ManagedGroupStripDragSessionState DragSessionState { get; }

        public SettingsSnapshot Settings { get; }

        public TabAppearanceInfo Appearance { get; }

        public IntPtr ActiveWindowHandle { get; }
    }
}
