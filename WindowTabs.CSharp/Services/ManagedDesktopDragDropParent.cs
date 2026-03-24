using System;
using System.Drawing;
using System.Windows.Forms;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;
using BemoWin32Helper = Bemo.Win32Helper;
using BemoWinUserApi = Bemo.WinUserApi;
using BemoShowWindowCommands = Bemo.ShowWindowCommands;
using BemoWindowHandleTypes = Bemo.WindowHandleTypes;
using BemoSetWindowPosFlags = Bemo.SetWindowPosFlags;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedDesktopDragDropParent : IDragDropParent
    {
        private readonly ManagedDesktopInteractionState interactionState;
        private readonly SettingsSession settingsSession;
        private readonly DesktopSessionCoordinator desktopSessionCoordinator;
        private readonly IProgramRefresher refresher;

        public ManagedDesktopDragDropParent(
            ManagedDesktopInteractionState interactionState,
            SettingsSession settingsSession,
            DesktopSessionCoordinator desktopSessionCoordinator,
            IProgramRefresher refresher)
        {
            this.interactionState = interactionState ?? throw new ArgumentNullException(nameof(interactionState));
            this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
            this.desktopSessionCoordinator = desktopSessionCoordinator ?? throw new ArgumentNullException(nameof(desktopSessionCoordinator));
            this.refresher = refresher ?? throw new ArgumentNullException(nameof(refresher));
        }

        public void OnDragBegin()
        {
            interactionState.SetDragging(true);
        }

        public void OnDragDrop(Point screenPoint, object data)
        {
            if (!(data is TabDragInfo dragInfo) || dragInfo.WindowHandle == IntPtr.Zero)
            {
                return;
            }

            var handle = dragInfo.WindowHandle;
            var previewWindowOffset = GetPreviewWindowOffset();
            var windowPoint = new Point(
                screenPoint.X - dragInfo.ImageOffset.X + previewWindowOffset.X,
                screenPoint.Y - dragInfo.ImageOffset.Y + previewWindowOffset.Y);

            windowPoint = BemoWin32Helper.ScreenToWorkspace(windowPoint);

            if (BemoWinUserApi.IsIconic(handle) || BemoWinUserApi.IsZoomed(handle))
            {
                BemoWinUserApi.ShowWindow(handle, BemoShowWindowCommands.SW_RESTORE);
            }

            var bounds = BemoWin32Helper.GetWindowRectangle(handle);
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
                BemoWinUserApi.SetWindowPos(
                    handle,
                    BemoWindowHandleTypes.HWND_TOP,
                    adjustedX,
                    adjustedY,
                    finalWidth,
                    finalHeight,
                    BemoSetWindowPosFlags.SWP_NOACTIVATE | BemoSetWindowPosFlags.SWP_NOZORDER);
            }
            else
            {
                BemoWinUserApi.SetWindowPos(
                    handle,
                    BemoWindowHandleTypes.HWND_TOP,
                    adjustedX,
                    adjustedY,
                    0,
                    0,
                    BemoSetWindowPosFlags.SWP_NOSIZE | BemoSetWindowPosFlags.SWP_NOACTIVATE | BemoSetWindowPosFlags.SWP_NOZORDER);
            }

            desktopSessionCoordinator.MarkDropped(handle);
            refresher.Refresh();
        }

        public void OnDragEnd()
        {
            interactionState.SetDragging(false);
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
