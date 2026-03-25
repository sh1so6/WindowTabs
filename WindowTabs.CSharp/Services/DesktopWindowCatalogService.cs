using System;
using System.Collections.Generic;
using System.Linq;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class DesktopWindowCatalogService
    {
        private readonly DesktopSnapshotService desktopSnapshotService;
        private readonly FilterService filterService;

        public DesktopWindowCatalogService(
            DesktopSnapshotService desktopSnapshotService,
            FilterService filterService)
        {
            this.desktopSnapshotService = desktopSnapshotService ?? throw new ArgumentNullException(nameof(desktopSnapshotService));
            this.filterService = filterService ?? throw new ArgumentNullException(nameof(filterService));
        }

        public IReadOnlyList<WindowSnapshot> GetAppWindowsInZOrder()
        {
            var screenRegion = desktopSnapshotService.GetScreenRegion();
            return desktopSnapshotService
                .EnumerateWindowsInZOrder()
                .Where(window => filterService.IsAppWindow(window, screenRegion))
                .ToList();
        }

        public IReadOnlyList<WindowSnapshot> GetTabbableWindowsInZOrder()
        {
            var screenRegion = desktopSnapshotService.GetScreenRegion();
            return desktopSnapshotService
                .EnumerateWindowsInZOrder()
                .Where(window => window.IsOnCurrentVirtualDesktop)
                .Where(window => filterService.IsTabbableWindow(window, screenRegion))
                .ToList();
        }

        public IReadOnlyDictionary<IntPtr, WindowSnapshot> GetTabbableWindowsByHandle()
        {
            return GetTabbableWindowsInZOrder()
                .ToDictionary(window => window.Handle, window => window);
        }

        public IReadOnlyList<WindowSnapshot> GetRestorableWindowsInZOrder()
        {
            return GetTabbableWindowsInZOrder();
        }

        public IReadOnlyDictionary<IntPtr, WindowSnapshot> GetRestorableWindowsByHandle()
        {
            return GetRestorableWindowsInZOrder()
                .ToDictionary(window => window.Handle, window => window);
        }
    }
}
