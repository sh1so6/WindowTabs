using System.Collections.Generic;
using System.Linq;

namespace WindowTabs.CSharp.Models
{
    internal sealed class WorkspaceLayout
    {
        public WorkspaceLayout()
            : this(string.Empty, new List<WorkspaceGroupLayout>())
        {
        }

        public WorkspaceLayout(string name, IEnumerable<WorkspaceGroupLayout> groups)
        {
            Name = name ?? string.Empty;
            Groups = (groups ?? new List<WorkspaceGroupLayout>()).ToArray();
        }

        public string Name { get; }

        public IReadOnlyList<WorkspaceGroupLayout> Groups { get; }
    }
}
