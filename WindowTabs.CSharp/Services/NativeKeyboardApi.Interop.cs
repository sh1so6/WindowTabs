using System;
using System.Runtime.InteropServices;

namespace WindowTabs.CSharp.Services
{
    internal static partial class NativeKeyboardApi
    {
        [LibraryImport("user32.dll", SetLastError = true)]
        private static partial IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, uint threadId);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool UnhookWindowsHookEx(IntPtr hhk);

        [LibraryImport("user32.dll")]
        private static partial int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "RegisterHotKey")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool RegisterHotKeyCore(IntPtr hWnd, int id, int fsModifiers, int vk);

        [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "UnregisterHotKey")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool UnregisterHotKeyCore(IntPtr hWnd, int id);

        [LibraryImport("user32.dll")]
        private static partial short GetAsyncKeyState(int vKey);

        [LibraryImport("user32.dll")]
        private static partial int GetSystemMetrics(int nIndex);

        [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        private static partial IntPtr GetModuleHandle(string lpModuleName);
    }
}
