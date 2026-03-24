using System;
using System.Linq;
using Bemo;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyAppWindowCatalog
    {
        private readonly FilterService filterService;
        private readonly DesktopSnapshotService desktopSnapshotService;

        public LegacyAppWindowCatalog(FilterService filterService, DesktopSnapshotService desktopSnapshotService)
        {
            this.filterService = filterService ?? throw new ArgumentNullException(nameof(filterService));
            this.desktopSnapshotService = desktopSnapshotService ?? throw new ArgumentNullException(nameof(desktopSnapshotService));
        }

        public List2<IntPtr> GetAppWindows()
        {
            var screenRegion = desktopSnapshotService.GetScreenRegion();
            var handles = desktopSnapshotService
                .EnumerateWindowsInZOrder()
                .Where(window => filterService.IsAppWindow(window, screenRegion))
                .Select(window => window.Handle);
            return FSharpListAdapter.ToList2(handles);
        }
    }
}
