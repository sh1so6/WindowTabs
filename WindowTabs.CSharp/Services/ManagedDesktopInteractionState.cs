namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedDesktopInteractionState
    {
        public bool IsDragging { get; private set; }

        public void SetDragging(bool isDragging)
        {
            IsDragging = isDragging;
        }
    }
}
