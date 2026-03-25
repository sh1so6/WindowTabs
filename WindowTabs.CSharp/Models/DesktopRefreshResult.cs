using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowTabs.CSharp.Models
{
    internal sealed class DesktopRefreshResult
    {
        public DesktopRefreshResult()
            : this(Array.Empty<WindowSnapshot>(), new RectValue(), Array.Empty<GroupSnapshot>(), new DesktopPlan())
        {
        }

        public DesktopRefreshResult(
            IEnumerable<WindowSnapshot> windows,
            RectValue screenRegion,
            IEnumerable<GroupSnapshot> groups,
            DesktopPlan plan)
        {
            Windows = (windows ?? Array.Empty<WindowSnapshot>()).ToArray();
            ScreenRegion = screenRegion ?? new RectValue();
            Groups = (groups ?? Array.Empty<GroupSnapshot>()).ToArray();
            Plan = plan ?? new DesktopPlan();
        }

        public IReadOnlyList<WindowSnapshot> Windows { get; }

        public RectValue ScreenRegion { get; }

        public IReadOnlyList<GroupSnapshot> Groups { get; }

        public DesktopPlan Plan { get; }
    }
}
