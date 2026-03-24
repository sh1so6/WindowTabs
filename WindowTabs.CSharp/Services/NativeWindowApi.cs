using System;
using System.Runtime.InteropServices;

namespace WindowTabs.CSharp.Services
{
    internal static class NativeWindowApi
    {
        private const uint WM_SYSCOMMAND = 0x0112;
        private static readonly IntPtr ScClose = new IntPtr(0xF060);

        public static IntPtr GetForegroundWindowHandle()
        {
            return GetForegroundWindow();
        }

        public static bool ActivateWindow(IntPtr windowHandle)
        {
            return windowHandle != IntPtr.Zero && SetForegroundWindow(windowHandle);
        }

        public static bool CloseWindow(IntPtr windowHandle)
        {
            return windowHandle != IntPtr.Zero
                && PostMessage(windowHandle, WM_SYSCOMMAND, ScClose, IntPtr.Zero);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    }
}
