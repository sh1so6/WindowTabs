using System;
using System.Runtime.InteropServices;

namespace WindowTabs.CSharp.Services
{
    internal static class NativeKeyboardApi
    {
        internal const int ModAlt = 0x0001;
        internal const int ModControl = 0x0002;
        internal const int ModShift = 0x0004;
        internal const int ModNoRepeat = 0x4000;

        internal const int WmKeyDown = 0x0100;
        internal const int WmSysKeyDown = 0x0104;
        internal const int WmHotKey = 0x0312;

        internal const int WhKeyboardLl = 13;
        internal const int VkControl = 0x11;
        internal const int VkLButton = 0x01;
        internal const int VkRButton = 0x02;

        internal const int HotKeyfShift = 0x01;
        internal const int HotKeyfControl = 0x02;
        internal const int HotKeyfAlt = 0x04;
        private const int SmSwapButton = 23;

        internal delegate int HookProc(int code, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        internal struct KeyboardHookStruct
        {
            public int VirtualKeyCode;
            public int ScanCode;
            public int Flags;
            public int Time;
            public IntPtr ExtraInfo;
        }

        public static IntPtr SetLowLevelKeyboardHook(HookProc callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            return SetWindowsHookEx(
                WhKeyboardLl,
                callback,
                GetModuleHandle(null),
                0);
        }

        public static bool UnhookWindowsHook(IntPtr hookHandle)
        {
            return hookHandle != IntPtr.Zero && UnhookWindowsHookEx(hookHandle);
        }

        public static int CallNextHook(IntPtr hookHandle, int code, IntPtr wParam, IntPtr lParam)
        {
            return CallNextHookEx(hookHandle, code, wParam, lParam);
        }

        public static bool RegisterHotKey(IntPtr windowHandle, int id, int modifiers, int virtualKey)
        {
            return windowHandle != IntPtr.Zero
                && virtualKey != 0
                && RegisterHotKeyCore(windowHandle, id, modifiers, virtualKey);
        }

        public static bool UnregisterHotKey(IntPtr windowHandle, int id)
        {
            return windowHandle != IntPtr.Zero && UnregisterHotKeyCore(windowHandle, id);
        }

        public static bool IsKeyPressed(int virtualKey)
        {
            return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
        }

        public static bool IsLeftMouseButtonPressed()
        {
            var virtualKey = GetSystemMetrics(SmSwapButton) != 0 ? VkRButton : VkLButton;
            return IsKeyPressed(virtualKey);
        }

        public static void DecodeHotKeyControlCode(short controlCode, out int modifiers, out int virtualKey)
        {
            var lowByte = controlCode & 0x00FF;
            var highByte = (controlCode >> 8) & 0x00FF;

            modifiers = 0;
            if ((highByte & HotKeyfControl) != 0)
            {
                modifiers |= ModControl;
            }

            if ((highByte & HotKeyfAlt) != 0)
            {
                modifiers |= ModAlt;
            }

            if ((highByte & HotKeyfShift) != 0)
            {
                modifiers |= ModShift;
            }

            virtualKey = lowByte;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, uint threadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "RegisterHotKey")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterHotKeyCore(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "UnregisterHotKey")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterHotKeyCore(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
