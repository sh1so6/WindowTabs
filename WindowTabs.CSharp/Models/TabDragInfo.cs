using System;
using System.Drawing;

namespace WindowTabs.CSharp.Models
{
    internal sealed class TabDragInfo
    {
        public IntPtr WindowHandle { get; set; }

        public Point TabOffset { get; set; }

        public Point ImageOffset { get; set; }
    }
}
