namespace WindowTabs.CSharp.Models
{
    internal sealed class WorkspaceWindowLayout
    {
        public WorkspaceWindowLayout()
            : this(string.Empty, string.Empty, 0, WorkspaceWindowMatchType.ExactMatch)
        {
        }

        public WorkspaceWindowLayout(
            string name,
            string title,
            int zOrder,
            WorkspaceWindowMatchType matchType)
        {
            Name = name ?? string.Empty;
            Title = title ?? string.Empty;
            ZOrder = zOrder;
            MatchType = matchType;
        }

        public string Name { get; }

        public string Title { get; }

        public int ZOrder { get; }

        public WorkspaceWindowMatchType MatchType { get; }
    }
}
