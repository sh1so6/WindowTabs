using System;
using System.Text;

namespace WindowTabs.CSharp.Services
{
    internal static partial class NativeProcessApi
    {
        private const uint ProcessQueryInformation = 0x0400;
        private const uint ProcessQueryLimitedInformation = 0x1000;
        private const int ProcessImagePathBufferLength = 1024;

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

        public static unsafe string GetProcessImagePath(IntPtr processHandle)
        {
            if (processHandle == IntPtr.Zero)
            {
                return string.Empty;
            }

            Span<char> buffer = stackalloc char[ProcessImagePathBufferLength];
            fixed (char* bufferPtr = buffer)
            {
                var length = (int)GetProcessImageFileName(processHandle, bufferPtr, buffer.Length);
                return length > 0 ? new string(bufferPtr, 0, length) : string.Empty;
            }
        }

        public static unsafe bool TryQueryDosDevice(string driveName, StringBuilder targetPath)
        {
            if (string.IsNullOrWhiteSpace(driveName) || targetPath == null || targetPath.Capacity == 0)
            {
                return false;
            }

            var buffer = new char[targetPath.Capacity];
            fixed (char* bufferPtr = buffer)
            {
                var length = (int)QueryDosDevice(driveName, bufferPtr, buffer.Length);
                if (length == 0)
                {
                    return false;
                }

                var stringLength = 0;
                while (stringLength < buffer.Length && buffer[stringLength] != '\0')
                {
                    stringLength++;
                }

                targetPath.Clear();
                targetPath.Append(buffer, 0, stringLength);
                return true;
            }
        }
    }
}
