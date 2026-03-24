using System.Collections.Generic;

namespace WindowTabs.CSharp.Models
{
    internal sealed class DesktopRefreshResult
    {
        public IReadOnlyList<WindowSnapshot> Windows { get; set; } = new WindowSnapshot[0];

        public RectValue ScreenRegion { get; set; } = new RectValue();

        public IReadOnlyList<GroupSnapshot> Groups { get; set; } = new GroupSnapshot[0];

        public DesktopPlan Plan { get; set; } = new DesktopPlan();
    }
}
