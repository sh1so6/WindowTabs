using System;
using System.Runtime.InteropServices;

namespace WindowTabs.CSharp.Services
{
    internal static partial class NativeWindowApi
    {
        [LibraryImport("user32.dll")]
        private static partial IntPtr GetForegroundWindow();

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetForegroundWindow(IntPtr hWnd);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [LibraryImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetWindowRect(IntPtr hWnd, out NativeRect lpRect);

        [LibraryImport("user32.dll")]
        private static partial IntPtr WindowFromPoint(NativePoint point);

        [LibraryImport("user32.dll")]
        private static partial IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [LibraryImport("user32.dll", EntryPoint = "GetClassNameW")]
        private static unsafe partial int GetClassName(IntPtr hWnd, char* className, int classNameSize);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW")]
        private static unsafe partial int GetWindowText(IntPtr hWnd, char* lpString, int nMaxCount);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")]
        private static partial int GetWindowThreadProcessId(IntPtr handle, out int lpdwProcessId);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static partial IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static partial IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool IsWindow(IntPtr hWnd);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool IsWindowVisible(IntPtr hWnd);

        [LibraryImport("user32.dll", EntryPoint = "IsIconic")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool IsIconic(IntPtr hWnd);

        [LibraryImport("user32.dll", EntryPoint = "IsZoomed")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool IsZoomed(IntPtr hWnd);

        [LibraryImport("user32.dll", EntryPoint = "ShowWindow")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, int uFlags);

        [LibraryImport("user32.dll")]
        private static partial IntPtr BeginDeferWindowPos(int nNumWindows);

        [LibraryImport("user32.dll")]
        private static partial IntPtr DeferWindowPos(
            IntPtr hWinPosInfo,
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            int uFlags);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool EndDeferWindowPos(IntPtr hWinPosInfo);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowPlacement")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetWindowPlacement(IntPtr hWnd, ref NativeWindowPlacement placement);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowPlacement")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetWindowPlacement(IntPtr hWnd, ref NativeWindowPlacement placement);

        [LibraryImport("user32.dll", EntryPoint = "ScreenToClient", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool ScreenToClientCore(IntPtr hWnd, ref NativePoint lpPoint);

        [LibraryImport("user32.dll")]
        private static partial IntPtr GetCapture();

        [LibraryImport("user32.dll")]
        private static partial IntPtr SetCapture(IntPtr hWnd);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool ReleaseCapture();
    }
}
