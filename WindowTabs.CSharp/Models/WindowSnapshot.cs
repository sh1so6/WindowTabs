using System;

namespace WindowTabs.CSharp.Models
{
    internal sealed class WindowSnapshot
    {
        public WindowSnapshot()
            : this(
                IntPtr.Zero,
                new ProcessSnapshot(),
                0,
                0,
                false,
                false,
                false,
                false,
                false,
                string.Empty,
                string.Empty,
                new RectValue(),
                new RectValue())
        {
        }

        public WindowSnapshot(
            IntPtr handle,
            ProcessSnapshot process,
            long style,
            long extendedStyle,
            bool isWindow,
            bool isVisibleOnScreen,
            bool isOnCurrentVirtualDesktop,
            bool isTopMost,
            bool isMinimized,
            string className,
            string text,
            RectValue bounds,
            RectValue parentBounds)
        {
            Handle = handle;
            Process = process ?? new ProcessSnapshot();
            Style = style;
            ExtendedStyle = extendedStyle;
            IsWindow = isWindow;
            IsVisibleOnScreen = isVisibleOnScreen;
            IsOnCurrentVirtualDesktop = isOnCurrentVirtualDesktop;
            IsTopMost = isTopMost;
            IsMinimized = isMinimized;
            ClassName = className ?? string.Empty;
            Text = text ?? string.Empty;
            Bounds = bounds ?? new RectValue();
            ParentBounds = parentBounds ?? new RectValue();
        }

        public IntPtr Handle { get; }

        public ProcessSnapshot Process { get; }

        public long Style { get; }

        public long ExtendedStyle { get; }

        public bool IsWindow { get; }

        public bool IsVisibleOnScreen { get; }

        public bool IsOnCurrentVirtualDesktop { get; }

        public bool IsTopMost { get; }

        public bool IsMinimized { get; }

        public string ClassName { get; }

        public string Text { get; }

        public RectValue Bounds { get; }

        public RectValue ParentBounds { get; }
    }
}
