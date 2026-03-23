using System;

namespace WindowTabs.CSharp.Models
{
    internal sealed class PendingWindowLaunch
    {
        public IntPtr GroupHandle { get; set; }

        public IntPtr InvokerHandle { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
