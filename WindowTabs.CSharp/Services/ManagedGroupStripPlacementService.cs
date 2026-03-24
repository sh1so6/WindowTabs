using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripPlacementService
    {
        public bool TryResolveLocation(
            GroupSnapshot group,
            IReadOnlyDictionary<IntPtr, WindowSnapshot> windowsByHandle,
            SettingsSnapshot settings,
            IntPtr activeWindowHandle,
            Size stripSize,
            TabAppearanceInfo appearance,
            out Point location)
        {
            location = Point.Empty;

            var anchorWindow = group.WindowHandles
                .Select(hwnd => windowsByHandle.TryGetValue(hwnd, out var window) ? window : null)
                .FirstOrDefault(window => window != null);

            if (anchorWindow == null || anchorWindow.Bounds.Width <= 0)
            {
                return false;
            }

            if (anchorWindow.IsMinimized
                || !anchorWindow.IsVisibleOnScreen
                || !anchorWindow.IsOnCurrentVirtualDesktop)
            {
                return false;
            }

            if (settings.HideInactiveTabs && !group.WindowHandles.Contains(activeWindowHandle))
            {
                return false;
            }

            if (settings.HideTabsOnFullscreen && IsFullscreen(anchorWindow))
            {
                return false;
            }

            var offsetY = group.SnapTabHeightMargin
                ? Math.Max(1, appearance.TabHeight - 1)
                : Math.Max(22, appearance.TabHeight);
            var stripX = string.Equals(group.TabPosition, "TopLeft", StringComparison.OrdinalIgnoreCase)
                ? anchorWindow.Bounds.X
                : Math.Max(anchorWindow.Bounds.X, anchorWindow.Bounds.Right - stripSize.Width);

            location = new Point(stripX, Math.Max(0, anchorWindow.Bounds.Y - offsetY - 4));
            return true;
        }

        private static bool IsFullscreen(WindowSnapshot window)
        {
            if (window == null || window.Handle == IntPtr.Zero || window.IsMinimized)
            {
                return false;
            }

            var screenBounds = Screen.FromHandle(window.Handle).Bounds;
            return window.Bounds.X <= screenBounds.X
                && window.Bounds.Y <= screenBounds.Y
                && window.Bounds.Right >= screenBounds.Right
                && window.Bounds.Bottom >= screenBounds.Bottom;
        }
    }
}
