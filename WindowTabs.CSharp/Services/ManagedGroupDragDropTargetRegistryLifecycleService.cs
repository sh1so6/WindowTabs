using System;
using System.Collections.Generic;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupDragDropTargetRegistryLifecycleService
    {
        private readonly IDragDrop dragDrop;
        private readonly IDesktopRuntime desktopRuntime;
        private readonly GroupMutationService groupMutationService;
        private readonly GroupMembershipService groupMembershipService;
        private readonly DesktopMonitoringService desktopMonitoringService;
        private readonly ManagedGroupDragDropTargetRegistrySyncService registrySyncService;
        private readonly Dictionary<IntPtr, IDragDropTarget> targets = new Dictionary<IntPtr, IDragDropTarget>();
        private bool initialized;
        private bool disposed;

        public ManagedGroupDragDropTargetRegistryLifecycleService(
            IDragDrop dragDrop,
            IDesktopRuntime desktopRuntime,
            GroupMutationService groupMutationService,
            GroupMembershipService groupMembershipService,
            DesktopMonitoringService desktopMonitoringService,
            ManagedGroupDragDropTargetRegistrySyncService registrySyncService)
        {
            this.dragDrop = dragDrop ?? throw new ArgumentNullException(nameof(dragDrop));
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
            this.groupMutationService = groupMutationService ?? throw new ArgumentNullException(nameof(groupMutationService));
            this.groupMembershipService = groupMembershipService ?? throw new ArgumentNullException(nameof(groupMembershipService));
            this.desktopMonitoringService = desktopMonitoringService ?? throw new ArgumentNullException(nameof(desktopMonitoringService));
            this.registrySyncService = registrySyncService ?? throw new ArgumentNullException(nameof(registrySyncService));
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
            registrySyncService.ClearTargets(dragDrop, targets);
        }

        private void OnStateChanged(object sender, EventArgs e)
        {
            SyncTargets();
        }

        private void SyncTargets()
        {
            registrySyncService.SyncTargets(
                dragDrop,
                desktopRuntime,
                groupMutationService,
                groupMembershipService,
                targets);
        }
    }
}
