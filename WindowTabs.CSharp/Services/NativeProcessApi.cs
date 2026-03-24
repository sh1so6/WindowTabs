using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowTabs.CSharp.Services
{
    internal static class NativeProcessApi
    {
        private const uint ProcessQueryInformation = 0x0400;
        private const uint ProcessQueryLimitedInformation = 0x1000;

        public static int GetCurrentProcessIdValue()
        {
            return GetCurrentProcessId();
        }

        public static IntPtr OpenQueryProcessHandle(int processId)
        {
            return OpenProcess(
                ProcessQueryInformation | ProcessQueryLimitedInformation,
                false,
                processId);
        }

        public static bool CloseHandleValue(IntPtr handle)
        {
            return handle != IntPtr.Zero && CloseHandle(handle);
        }

        public static string GetProcessImagePath(IntPtr processHandle)
        {
            if (processHandle == IntPtr.Zero)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(1024);
            return GetProcessImageFileName(processHandle, builder, builder.Capacity) > 0
                ? builder.ToString()
                : string.Empty;
        }

        public static bool TryQueryDosDevice(string driveName, StringBuilder targetPath)
        {
            return QueryDosDevice(driveName, targetPath, targetPath.Capacity) != 0;
        }

        [DllImport("kernel32.dll")]
        private static extern int GetCurrentProcessId();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("psapi.dll", CharSet = CharSet.Auto)]
        private static extern uint GetProcessImageFileName(IntPtr hProcess, StringBuilder lpImageFileName, int nSize);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);
    }
}
