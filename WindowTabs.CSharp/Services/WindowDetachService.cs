using System;
using System.Drawing;
using System.Windows.Forms;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class WindowDetachService
    {
        private readonly SettingsSession settingsSession;
        private readonly DesktopSessionCoordinator desktopSessionCoordinator;
        private readonly IProgramRefresher refresher;

        public WindowDetachService(
            SettingsSession settingsSession,
            DesktopSessionCoordinator desktopSessionCoordinator,
            IProgramRefresher refresher)
        {
            this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
            this.desktopSessionCoordinator = desktopSessionCoordinator ?? throw new ArgumentNullException(nameof(desktopSessionCoordinator));
            this.refresher = refresher ?? throw new ArgumentNullException(nameof(refresher));
        }

        public bool DetachWindowToPoint(Point screenPoint, WindowTabs.CSharp.Models.TabDragInfo dragInfo)
        {
            if (dragInfo == null || dragInfo.WindowHandle == IntPtr.Zero)
            {
                return false;
            }

            var handle = dragInfo.WindowHandle;
            var previewWindowOffset = GetPreviewWindowOffset();
            var windowPoint = new Point(
                screenPoint.X - dragInfo.ImageOffset.X + previewWindowOffset.X,
                screenPoint.Y - dragInfo.ImageOffset.Y + previewWindowOffset.Y);

            windowPoint = NativeWindowApi.ScreenToWorkspace(windowPoint);

            if (NativeWindowApi.IsWindowMinimized(handle) || NativeWindowApi.IsWindowMaximized(handle))
            {
                NativeWindowApi.RestoreWindow(handle);
            }

            var bounds = NativeWindowApi.GetWindowRectangle(handle);
            var screen = Screen.FromPoint(new Point(
                windowPoint.X + (bounds.Width / 2),
                windowPoint.Y + (bounds.Height / 2)));
            var workingArea = screen.WorkingArea;

            var finalWidth = Math.Min(bounds.Width, workingArea.Width);
            var finalHeight = Math.Min(bounds.Height, workingArea.Height);
            var adjustedX = Math.Max(workingArea.Left, Math.Min(windowPoint.X, workingArea.Right - finalWidth));
            var adjustedY = Math.Max(workingArea.Top, Math.Min(windowPoint.Y, workingArea.Bottom - finalHeight));

            if (bounds.Width > workingArea.Width || bounds.Height > workingArea.Height)
            {
                NativeWindowApi.SetWindowPosition(
                    handle,
                    NativeWindowApi.HwndTop,
                    adjustedX,
                    adjustedY,
                    finalWidth,
                    finalHeight,
                    NativeWindowApi.SwpNoActivate | NativeWindowApi.SwpNoZOrder);
            }
            else
            {
                NativeWindowApi.SetWindowPosition(
                    handle,
                    NativeWindowApi.HwndTop,
                    adjustedX,
                    adjustedY,
                    0,
                    0,
                    NativeWindowApi.SwpNoSize | NativeWindowApi.SwpNoActivate | NativeWindowApi.SwpNoZOrder);
            }

            desktopSessionCoordinator.MarkDropped(handle);
            refresher.Refresh();
            return true;
        }

        private Point GetPreviewWindowOffset()
        {
            var tabAppearance = settingsSession.Current.TabAppearance;
            if (tabAppearance == null)
            {
                return Point.Empty;
            }

            return new Point(0, tabAppearance.TabHeight - (tabAppearance.TabHeightOffset + 1));
        }
    }
}
