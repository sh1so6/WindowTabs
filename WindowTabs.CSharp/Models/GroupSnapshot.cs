using System;
using System.Collections.Generic;

namespace WindowTabs.CSharp.Models
{
    internal sealed class GroupSnapshot
    {
        public IntPtr GroupHandle { get; set; }

        public List<IntPtr> WindowHandles { get; set; } = new List<IntPtr>();
    }
}
