using System;
using System.Drawing;

namespace WindowTabs.CSharp.Models
{
    internal sealed class DragBeginRequest
    {
        public IntPtr InitialWindowHandle { get; set; }

        public Func<Bitmap> CreateImage { get; set; }

        public Point ImageOffset { get; set; }

        public Point InitialScreenPoint { get; set; }

        public object Data { get; set; }
    }
}
