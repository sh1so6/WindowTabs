using System;
using System.Runtime.InteropServices;

namespace WindowTabs.CSharp.Services
{
    internal static partial class NativeWindowHookApi
    {
        [LibraryImport("user32.dll", EntryPoint = "RegisterWindowMessageW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        private static partial uint RegisterWindowMessageCore(string lpString);

        [LibraryImport("user32.dll", EntryPoint = "RegisterShellHookWindow", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool RegisterShellHookWindowCore(IntPtr hWnd);

        [LibraryImport("user32.dll", EntryPoint = "SetWinEventHook", SetLastError = true)]
        private static partial IntPtr SetWinEventHookCore(
            uint eventMin,
            uint eventMax,
            IntPtr hmodWinEventProc,
            WinEventProc lpfnWinEventProc,
            uint idProcess,
            uint idThread,
            WinEventHookFlags dwFlags);

        [LibraryImport("user32.dll", EntryPoint = "UnhookWinEvent", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool UnhookWinEventCore(IntPtr hWinEventHook);
    }
}
