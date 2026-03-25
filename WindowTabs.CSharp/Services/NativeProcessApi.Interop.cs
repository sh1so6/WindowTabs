using System;
using System.Runtime.InteropServices;

namespace WindowTabs.CSharp.Services
{
    internal static partial class NativeProcessApi
    {
        [LibraryImport("kernel32.dll")]
        private static partial int GetCurrentProcessId();

        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial IntPtr OpenProcess(uint processAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, int processId);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CloseHandle(IntPtr hObject);

        [LibraryImport("psapi.dll", EntryPoint = "GetProcessImageFileNameW")]
        private static unsafe partial uint GetProcessImageFileName(IntPtr hProcess, char* lpImageFileName, int nSize);

        [LibraryImport("kernel32.dll", EntryPoint = "QueryDosDeviceW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        private static unsafe partial uint QueryDosDevice(string lpDeviceName, char* lpTargetPath, int ucchMax);
    }
}
