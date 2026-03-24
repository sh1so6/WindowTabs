using System;
using Bemo;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyFilterServiceBridge : IFilterService
    {
        private readonly FilterService filterService;
        private readonly DesktopSnapshotService desktopSnapshotService;

        public LegacyFilterServiceBridge(
            FilterService filterService,
            DesktopSnapshotService desktopSnapshotService)
        {
            this.filterService = filterService ?? throw new ArgumentNullException(nameof(filterService));
            this.desktopSnapshotService = desktopSnapshotService ?? throw new ArgumentNullException(nameof(desktopSnapshotService));
        }

        public bool isAppWindow(IntPtr hwnd)
        {
            return filterService.IsAppWindow(
                desktopSnapshotService.CreateWindowSnapshot(hwnd),
                desktopSnapshotService.GetScreenRegion());
        }

        public bool isAppWindowStyle(IntPtr hwnd)
        {
            return filterService.IsAppWindowStyle(desktopSnapshotService.CreateWindowSnapshot(hwnd));
        }

        public bool isTabbableWindow(IntPtr hwnd)
        {
            return filterService.IsTabbableWindow(
                desktopSnapshotService.CreateWindowSnapshot(hwnd),
                desktopSnapshotService.GetScreenRegion());
        }

        public bool isTabbingEnabledForAllProcessesByDefault
        {
            get => filterService.IsTabbingEnabledForAllProcessesByDefault;
            set => filterService.IsTabbingEnabledForAllProcessesByDefault = value;
        }

        public void setIsTabbingEnabledForProcess(string processPath, bool enabled)
        {
            filterService.SetIsTabbingEnabledForProcess(processPath, enabled);
        }

        public bool getIsTabbingEnabledForProcess(string processPath)
        {
            return filterService.GetIsTabbingEnabledForProcess(processPath);
        }
    }
}
