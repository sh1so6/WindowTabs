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
            out Point location,
            out bool showInside)
        {
            location = Point.Empty;
            showInside = false;

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

            var stripX = string.Equals(group.TabPosition, "TopLeft", StringComparison.OrdinalIgnoreCase)
                ? anchorWindow.Bounds.X
                : Math.Max(anchorWindow.Bounds.X, anchorWindow.Bounds.Right - stripSize.Width);
            showInside = ShouldShowInside(anchorWindow, stripX, stripSize, appearance);

            location = showInside
                ? new Point(stripX, anchorWindow.Bounds.Y - 1)
                : new Point(stripX, Math.Max(0, anchorWindow.Bounds.Y - appearance.TabHeight + appearance.TabHeightOffset));
            return true;
        }

        private static bool ShouldShowInside(WindowSnapshot anchorWindow, int stripX, Size stripSize, TabAppearanceInfo appearance)
        {
            if (anchorWindow == null || appearance == null)
            {
                return false;
            }

            var stripHeight = Math.Max(1, appearance.TabHeight);
            var stripWidth = Math.Max(1, stripSize.Width);
            var outsideBounds = new Rectangle(
                stripX,
                anchorWindow.Bounds.Y - appearance.TabHeight + appearance.TabHeightOffset,
                stripWidth,
                stripHeight);
            var insideBounds = new Rectangle(
                stripX,
                anchorWindow.Bounds.Y - 1,
                stripWidth,
                stripHeight);

            foreach (var screen in Screen.AllScreens)
            {
                var insideHeight = Rectangle.Intersect(screen.Bounds, insideBounds).Height;
                var outsideHeight = Rectangle.Intersect(screen.Bounds, outsideBounds).Height;
                if (insideHeight > outsideHeight)
                {
                    return true;
                }
            }

            return false;
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
