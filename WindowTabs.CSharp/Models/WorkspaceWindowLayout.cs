namespace WindowTabs.CSharp.Models
{
    internal sealed class WorkspaceWindowLayout
    {
        public string Name { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public int ZOrder { get; set; }

        public WorkspaceWindowMatchType MatchType { get; set; } = WorkspaceWindowMatchType.ExactMatch;
    }
}
