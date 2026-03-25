using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
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
            foreach (var handle in NativeWindowApi.EnumerateWindowsInZOrder())
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
            var processId = NativeWindowApi.GetWindowProcessId(handle);
            var parentHandle = NativeWindowApi.GetWindowLongPtr(handle, NativeWindowApi.GwlHwndParent);
            var extendedStyle = NativeWindowApi.GetWindowLongPtr(handle, NativeWindowApi.GwlExStyle);

            return new WindowSnapshot(
                handle,
                CreateProcessSnapshot(processId),
                NativeWindowApi.GetWindowLongPtr(handle, NativeWindowApi.GwlStyle).ToInt64(),
                extendedStyle.ToInt64(),
                NativeWindowApi.IsWindowHandle(handle),
                NativeWindowApi.IsWindowVisibleOnDesktop(handle) && !IsCloaked(handle),
                !IsCloaked(handle),
                (extendedStyle.ToInt64() & NativeWindowApi.WsExTopMost) == NativeWindowApi.WsExTopMost,
                NativeWindowApi.IsWindowMinimized(handle),
                NativeWindowApi.GetWindowClassName(handle),
                NativeWindowApi.GetWindowTextValue(handle),
                ToRectValue(NativeWindowApi.GetWindowRectangle(handle)),
                parentHandle == IntPtr.Zero ? new RectValue() : ToRectValue(NativeWindowApi.GetWindowRectangle(parentHandle)));
        }

        private ProcessSnapshot CreateProcessSnapshot(int processId)
        {
            var isCurrentProcess = processId == NativeProcessApi.GetCurrentProcessIdValue();

            var processHandle = NativeProcessApi.OpenQueryProcessHandle(processId);

            if (processHandle == IntPtr.Zero)
            {
                return new ProcessSnapshot(processId, false, isCurrentProcess, string.Empty, string.Empty);
            }

            try
            {
                var kernelPath = NativeProcessApi.GetProcessImagePath(processHandle);
                var processPath = NormalizeProcessPath(kernelPath);
                var exeName = System.IO.Path.GetFileName(processPath)?.ToLowerInvariant() ?? string.Empty;
                return new ProcessSnapshot(processId, true, isCurrentProcess, processPath, exeName);
            }
            finally
            {
                NativeProcessApi.CloseHandleValue(processHandle);
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
                return NativeDwmApi.IsWindowCloaked(handle);
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
                if (!NativeProcessApi.TryQueryDosDevice(drive + ":", builder))
                {
                    continue;
                }

                mappings.Add((builder.ToString(), drive + ":"));
            }

            return mappings;
        }
    }
}
