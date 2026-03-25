using System;
using System.Collections.Generic;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class DesktopRefreshResultFactory
    {
        private readonly DesktopGroupSnapshotService groupSnapshotService;

        public DesktopRefreshResultFactory(DesktopGroupSnapshotService groupSnapshotService)
        {
            this.groupSnapshotService = groupSnapshotService ?? throw new ArgumentNullException(nameof(groupSnapshotService));
        }

        public DesktopRefreshResult Create(
            IReadOnlyList<WindowSnapshot> windows,
            RectValue screenRegion,
            DesktopPlan plan)
        {
            return new DesktopRefreshResult(
                windows ?? Array.Empty<WindowSnapshot>(),
                screenRegion ?? new RectValue(),
                groupSnapshotService.GetGroupSnapshots(),
                plan ?? new DesktopPlan());
        }
    }
}
