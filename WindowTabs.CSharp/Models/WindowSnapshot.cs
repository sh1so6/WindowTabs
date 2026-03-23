using System;

namespace WindowTabs.CSharp.Models
{
    internal sealed class WindowSnapshot
    {
        public IntPtr Handle { get; set; }
        public ProcessSnapshot Process { get; set; } = new ProcessSnapshot();
        public long Style { get; set; }
        public long ExtendedStyle { get; set; }
        public bool IsWindow { get; set; }
        public bool IsVisibleOnScreen { get; set; }
        public bool IsOnCurrentVirtualDesktop { get; set; }
        public bool IsTopMost { get; set; }
        public bool IsMinimized { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public RectValue Bounds { get; set; } = new RectValue();
        public RectValue ParentBounds { get; set; } = new RectValue();
    }
}
