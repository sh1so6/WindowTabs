using System;
using System.Collections.Generic;
using System.Linq;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class DesktopRefreshWorkflowService
    {
        private readonly DesktopSnapshotService desktopSnapshotService;
        private readonly DesktopPlannerService desktopPlannerService;
        private readonly DesktopWindowCleanupService windowCleanupService;
        private readonly DesktopSessionStateService sessionStateService;
        private readonly DesktopGroupSnapshotService groupSnapshotService;
        private readonly DesktopPlanExecutionService planExecutionService;
        private readonly DesktopRefreshResultFactory refreshResultFactory;

        public DesktopRefreshWorkflowService(
            DesktopSnapshotService desktopSnapshotService,
            DesktopPlannerService desktopPlannerService,
            DesktopWindowCleanupService windowCleanupService,
            DesktopSessionStateService sessionStateService,
            DesktopGroupSnapshotService groupSnapshotService,
            DesktopPlanExecutionService planExecutionService,
            DesktopRefreshResultFactory refreshResultFactory)
        {
            this.desktopSnapshotService = desktopSnapshotService ?? throw new ArgumentNullException(nameof(desktopSnapshotService));
            this.desktopPlannerService = desktopPlannerService ?? throw new ArgumentNullException(nameof(desktopPlannerService));
            this.windowCleanupService = windowCleanupService ?? throw new ArgumentNullException(nameof(windowCleanupService));
            this.sessionStateService = sessionStateService ?? throw new ArgumentNullException(nameof(sessionStateService));
            this.groupSnapshotService = groupSnapshotService ?? throw new ArgumentNullException(nameof(groupSnapshotService));
            this.planExecutionService = planExecutionService ?? throw new ArgumentNullException(nameof(planExecutionService));
            this.refreshResultFactory = refreshResultFactory ?? throw new ArgumentNullException(nameof(refreshResultFactory));
        }

        public DesktopRefreshResult RefreshDesktop()
        {
            var screenRegion = desktopSnapshotService.GetScreenRegion();
            var windows = desktopSnapshotService.EnumerateWindowsInZOrder();
            desktopPlannerService.UpdateTrackedProcessPaths(windows);
            windowCleanupService.CleanupClosedWindows(windows);

            return ExecutePlannedRefresh(windows, screenRegion);
        }

        public DesktopRefreshResult HandleDestroyedWindow(IntPtr windowHandle, DesktopRefreshResult previousRefresh)
        {
            if (windowHandle == IntPtr.Zero || previousRefresh == null)
            {
                return RefreshDesktop();
            }

            windowCleanupService.CleanupDestroyedWindow(windowHandle);

            var windows = previousRefresh.Windows
                .Where(window => window.Handle != windowHandle)
                .ToList();

            desktopPlannerService.UpdateTrackedProcessPaths(windows);

            return ExecutePlannedRefresh(windows, previousRefresh.ScreenRegion);
        }

        private DesktopRefreshResult ExecutePlannedRefresh(
            IReadOnlyList<WindowSnapshot> windows,
            RectValue screenRegion)
        {
            sessionStateService.CleanupExpiredDroppedWindows();
            var groups = groupSnapshotService.GetGroupSnapshots();

            var plan = desktopPlannerService.BuildPlan(
                windows,
                groups,
                screenRegion,
                sessionStateService.GetSubscribedHandles(),
                sessionStateService.GetActiveDroppedWindowHandles());

            planExecutionService.ApplyPlan(plan);

            return refreshResultFactory.Create(windows, screenRegion, plan);
        }
    }
}
