using System;
namespace WindowTabs.CSharp.Services
{
    internal static partial class NativeWindowHookApi
    {
        internal const int HshellWindowCreated = 1;
        internal const int HshellWindowDestroyed = 2;
        internal const int HshellWindowActivated = 4;
        internal const int HshellRudeAppActivated = 32772;

        [Flags]
        private enum WinEventHookFlags : uint
        {
            None = 0
        }

        internal delegate void WinEventProc(
            IntPtr hWinEventHook,
            uint evt,
            IntPtr hwnd,
            IntPtr idObject,
            IntPtr idChild,
            uint dwEventThread,
            uint dwmsEventTime);

        public static int RegisterWindowMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("A message name is required.", nameof(message));
            }

            return unchecked((int)RegisterWindowMessageCore(message));
        }

        public static bool RegisterShellHookWindow(IntPtr windowHandle)
        {
            return windowHandle != IntPtr.Zero && RegisterShellHookWindowCore(windowHandle);
        }

        public static IntPtr SetWinEventHook(
            uint minEvent,
            uint maxEvent,
            WinEventProc callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            return SetWinEventHookCore(
                minEvent,
                maxEvent,
                IntPtr.Zero,
                callback,
                0,
                0,
                WinEventHookFlags.None);
        }

        public static bool UnhookWinEvent(IntPtr hookHandle)
        {
            return hookHandle != IntPtr.Zero && UnhookWinEventCore(hookHandle);
        }
    }
}
