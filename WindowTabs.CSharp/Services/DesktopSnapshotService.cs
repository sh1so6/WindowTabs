using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Bemo;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class DesktopSnapshotService
    {
        private readonly List<(string DevicePath, string DrivePrefix)> dosDevices;

        public DesktopSnapshotService()
        {
            dosDevices = LoadDosDevices();
        }

        public RectValue GetScreenRegion()
        {
            var region = new RectValue();
            var initialized = false;

            foreach (var screen in Screen.AllScreens)
            {
                var bounds = screen.Bounds;
                if (!initialized)
                {
                    region.X = bounds.X;
                    region.Y = bounds.Y;
                    region.Width = bounds.Width;
                    region.Height = bounds.Height;
                    initialized = true;
                    continue;
                }

                var left = Math.Min(region.X, bounds.X);
                var top = Math.Min(region.Y, bounds.Y);
                var right = Math.Max(region.Right, bounds.Right);
                var bottom = Math.Max(region.Bottom, bounds.Bottom);
                region.X = left;
                region.Y = top;
                region.Width = right - left;
                region.Height = bottom - top;
            }

            return region;
        }

        public IReadOnlyList<WindowSnapshot> EnumerateWindowsInZOrder()
        {
            var windows = new List<WindowSnapshot>();
            foreach (var handle in Win32Helper.GetWindowsInZOrder())
            {
                windows.Add(CreateWindowSnapshot(handle));
            }

            return windows;
        }

        public IntPtr GetForegroundWindowHandle()
        {
            return NativeWindowApi.GetForegroundWindowHandle();
        }

        public WindowSnapshot CreateWindowSnapshot(IntPtr handle)
        {
            var processId = Win32Helper.GetWindowProcessId(handle);
            var parentHandle = WinUserApi.GetWindowLong(handle, WindowLongFieldOffset.GWL_HWNDPARENT);
            var extendedStyle = WinUserApi.GetWindowLong(handle, WindowLongFieldOffset.GWL_EXSTYLE);

            return new WindowSnapshot
            {
                Handle = handle,
                Process = CreateProcessSnapshot(processId),
                Style = WinUserApi.GetWindowLong(handle, WindowLongFieldOffset.GWL_STYLE).ToInt64(),
                ExtendedStyle = extendedStyle.ToInt64(),
                IsWindow = WinUserApi.IsWindow(handle),
                IsVisibleOnScreen = WinUserApi.IsWindowVisible(handle) && !IsCloaked(handle),
                IsOnCurrentVirtualDesktop = !IsCloaked(handle),
                IsTopMost = (extendedStyle.ToInt64() & WindowsExtendedStyles.WS_EX_TOPMOST) == WindowsExtendedStyles.WS_EX_TOPMOST,
                IsMinimized = WinUserApi.IsIconic(handle),
                ClassName = Win32Helper.GetClassName(handle),
                Text = Win32Helper.GetWindowText(handle),
                Bounds = ToRectValue(Win32Helper.GetWindowRectangle(handle)),
                ParentBounds = parentHandle == IntPtr.Zero ? new RectValue() : ToRectValue(Win32Helper.GetWindowRectangle(parentHandle))
            };
        }

        private ProcessSnapshot CreateProcessSnapshot(int processId)
        {
            var snapshot = new ProcessSnapshot
            {
                ProcessId = processId,
                IsCurrentProcess = processId == WinBaseApi.GetCurrentProcessId()
            };

            var processHandle = WinBaseApi.OpenProcess(
                ProcessAccessRights.PROCESS_QUERY_LIMITED_INFORMATION | ProcessAccessRights.PROCESS_QUERY_INFORMATION,
                false,
                processId);

            snapshot.CanQueryProcess = processHandle != IntPtr.Zero;
            if (!snapshot.CanQueryProcess)
            {
                return snapshot;
            }

            try
            {
                var builder = new StringBuilder(1024);
                Psapi.GetProcessImageFileName(processHandle, builder, builder.Capacity);
                var kernelPath = builder.ToString();
                snapshot.ProcessPath = NormalizeProcessPath(kernelPath);
                snapshot.ExeName = System.IO.Path.GetFileName(snapshot.ProcessPath)?.ToLowerInvariant() ?? string.Empty;
                return snapshot;
            }
            finally
            {
                WinBaseApi.CloseHandle(processHandle);
            }
        }

        private string NormalizeProcessPath(string kernelPath)
        {
            if (string.IsNullOrWhiteSpace(kernelPath))
            {
                return string.Empty;
            }

            foreach (var (devicePath, drivePrefix) in dosDevices)
            {
                if (kernelPath.StartsWith(devicePath, StringComparison.OrdinalIgnoreCase))
                {
                    return kernelPath.Replace(devicePath, drivePrefix);
                }
            }

            return kernelPath;
        }

        private static bool IsCloaked(IntPtr handle)
        {
            try
            {
                var result = DwmApi.DwmGetWindowAttribute(handle, DWMWINDOWATTRIBUTE.DWMWA_CLOAKED, out var cloaked, sizeof(int));
                return result == 0 && cloaked != 0;
            }
            catch
            {
                return false;
            }
        }

        private static RectValue ToRectValue(Rectangle rectangle)
        {
            return new RectValue
            {
                X = rectangle.X,
                Y = rectangle.Y,
                Width = rectangle.Width,
                Height = rectangle.Height
            };
        }

        private static List<(string DevicePath, string DrivePrefix)> LoadDosDevices()
        {
            var mappings = new List<(string DevicePath, string DrivePrefix)>();
            for (var drive = 'A'; drive <= 'Z'; drive++)
            {
                var builder = new StringBuilder(512);
                if (!WinBaseApi.QueryDosDevice(drive + ":", builder, builder.Capacity))
                {
                    continue;
                }

                mappings.Add((builder.ToString(), drive + ":"));
            }

            return mappings;
        }
    }
}
