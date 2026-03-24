using System.Collections.Generic;

namespace WindowTabs.CSharp.Models
{
    internal sealed class WorkspaceGroupLayout
    {
        public string Name { get; set; } = string.Empty;

        public WindowPlacementValue Placement { get; set; } = new WindowPlacementValue();

        public List<WorkspaceWindowLayout> Windows { get; set; } = new List<WorkspaceWindowLayout>();
    }
}
