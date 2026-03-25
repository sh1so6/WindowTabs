using System.Collections.Generic;
using System.Linq;

namespace WindowTabs.CSharp.Models
{
    internal sealed class WorkspaceGroupLayout(
        string name,
        WindowPlacementValue placement,
        IEnumerable<WorkspaceWindowLayout> windows)
    {
        public WorkspaceGroupLayout()
            : this(string.Empty, new WindowPlacementValue(), [])
        {
        }

        public string Name { get; } = name ?? string.Empty;

        public WindowPlacementValue Placement { get; } = placement ?? new WindowPlacementValue();

        public IReadOnlyList<WorkspaceWindowLayout> Windows { get; } = windows?.ToArray() ?? [];
    }
}
