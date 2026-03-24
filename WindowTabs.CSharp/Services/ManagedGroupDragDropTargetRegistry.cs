using System;
using System.Collections.Generic;
using System.Linq;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupDragDropTargetRegistry : IDisposable
    {
        private readonly IDragDrop dragDrop;
        private readonly IDesktopRuntime desktopRuntime;
        private readonly DesktopMonitoringService desktopMonitoringService;
        private readonly IProgramRefresher refresher;
        private readonly Dictionary<IntPtr, IDragDropTarget> targets = new Dictionary<IntPtr, IDragDropTarget>();
        private bool initialized;
        private bool disposed;

        public ManagedGroupDragDropTargetRegistry(
            IDragDrop dragDrop,
            IDesktopRuntime desktopRuntime,
            DesktopMonitoringService desktopMonitoringService,
            IProgramRefresher refresher)
        {
            this.dragDrop = dragDrop ?? throw new ArgumentNullException(nameof(dragDrop));
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
            this.desktopMonitoringService = desktopMonitoringService ?? throw new ArgumentNullException(nameof(desktopMonitoringService));
            this.refresher = refresher ?? throw new ArgumentNullException(nameof(refresher));
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            desktopMonitoringService.StateChanged += OnStateChanged;
            SyncTargets();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            desktopMonitoringService.StateChanged -= OnStateChanged;
            foreach (var hwnd in targets.Keys.ToArray())
            {
                dragDrop.UnregisterTarget(hwnd);
            }

            targets.Clear();
        }

        private void OnStateChanged(object sender, EventArgs e)
        {
            SyncTargets();
        }

        private void SyncTargets()
        {
            var desiredHandles = new HashSet<IntPtr>(
                desktopRuntime.Groups.SelectMany(group => group.WindowHandles).Where(hwnd => hwnd != IntPtr.Zero));

            foreach (var staleHandle in targets.Keys.Where(hwnd => !desiredHandles.Contains(hwnd)).ToArray())
            {
                dragDrop.UnregisterTarget(staleHandle);
                targets.Remove(staleHandle);
            }

            foreach (var hwnd in desiredHandles)
            {
                if (targets.ContainsKey(hwnd))
                {
                    continue;
                }

                var target = new ManagedGroupDragDropTarget(hwnd, desktopRuntime, refresher);
                targets.Add(hwnd, target);
                dragDrop.RegisterTarget(hwnd, target);
            }
        }
    }
}
