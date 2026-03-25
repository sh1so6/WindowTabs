using System;
namespace WindowTabs.CSharp.Services
{
    internal static partial class NativeDwmApi
    {
        private const int DwmwaCloaked = 14;

        public static bool IsWindowCloaked(IntPtr windowHandle)
        {
            try
            {
                var result = DwmGetWindowAttribute(windowHandle, DwmwaCloaked, out var cloaked, sizeof(int));
                return result == 0 && cloaked != 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
