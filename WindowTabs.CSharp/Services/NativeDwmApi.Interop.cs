using System;
using System.Runtime.InteropServices;

namespace WindowTabs.CSharp.Services
{
    internal static partial class NativeDwmApi
    {
        [LibraryImport("dwmapi.dll")]
        private static partial int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);
    }
}
