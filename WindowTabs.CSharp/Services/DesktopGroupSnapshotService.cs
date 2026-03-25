using System;
using System.Collections.Generic;
using System.Linq;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class DesktopGroupSnapshotService
    {
        private readonly IDesktopRuntime desktopRuntime;

        public DesktopGroupSnapshotService(IDesktopRuntime desktopRuntime)
        {
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
        }

        public IReadOnlyList<GroupSnapshot> GetGroupSnapshots()
        {
            return desktopRuntime.Groups
                .Select(group => new GroupSnapshot(
                    group.GroupHandle,
                    group.WindowHandles,
                    group.TabPosition,
                    group.SnapTabHeightMargin))
                .ToList();
        }
    }
}
