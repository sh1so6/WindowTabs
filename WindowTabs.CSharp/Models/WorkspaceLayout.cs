using System.Collections.Generic;
using System.Linq;

namespace WindowTabs.CSharp.Models
{
    internal sealed class WorkspaceLayout(string name, IEnumerable<WorkspaceGroupLayout> groups)
    {
        public WorkspaceLayout()
            : this(string.Empty, [])
        {
        }

        public string Name { get; } = name ?? string.Empty;

        public IReadOnlyList<WorkspaceGroupLayout> Groups { get; } = groups?.ToArray() ?? [];
    }
}
