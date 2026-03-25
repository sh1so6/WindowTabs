namespace WindowTabs.CSharp.Models
{
    internal sealed class WorkspaceWindowLayout(
        string name,
        string title,
        int zOrder,
        WorkspaceWindowMatchType matchType)
    {
        public WorkspaceWindowLayout()
            : this(string.Empty, string.Empty, 0, WorkspaceWindowMatchType.ExactMatch)
        {
        }

        public string Name { get; } = name ?? string.Empty;

        public string Title { get; } = title ?? string.Empty;

        public int ZOrder { get; } = zOrder;

        public WorkspaceWindowMatchType MatchType { get; } = matchType;
    }
}
