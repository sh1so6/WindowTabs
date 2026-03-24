using System.Collections.Generic;

namespace WindowTabs.CSharp.Models
{
    internal sealed class WorkspaceLayout
    {
        public string Name { get; set; } = string.Empty;

        public List<WorkspaceGroupLayout> Groups { get; set; } = new List<WorkspaceGroupLayout>();
    }
}
