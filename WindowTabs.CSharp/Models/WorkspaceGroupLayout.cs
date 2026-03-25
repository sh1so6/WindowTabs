using System.Collections.Generic;
using System.Linq;

namespace WindowTabs.CSharp.Models
{
    internal sealed class WorkspaceGroupLayout
    {
        public WorkspaceGroupLayout()
            : this(string.Empty, new WindowPlacementValue(), new List<WorkspaceWindowLayout>())
        {
        }

        public WorkspaceGroupLayout(
            string name,
            WindowPlacementValue placement,
            IEnumerable<WorkspaceWindowLayout> windows)
        {
            Name = name ?? string.Empty;
            Placement = placement ?? new WindowPlacementValue();
            Windows = (windows ?? new List<WorkspaceWindowLayout>()).ToArray();
        }

        public string Name { get; }

        public WindowPlacementValue Placement { get; }

        public IReadOnlyList<WorkspaceWindowLayout> Windows { get; }
    }
}
