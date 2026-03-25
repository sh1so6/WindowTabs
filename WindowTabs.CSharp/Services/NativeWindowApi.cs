using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal static partial class NativeWindowApi
    {
        internal const int GwlHwndParent = -8;
        internal const int GwlStyle = -16;
        internal const int GwlExStyle = -20;

        internal const int WmMouseMove = 0x0200;
        internal const int WmLButtonUp = 0x0202;
        internal const int WmMouseLeave = 0x02A3;
        internal const int WsExToolWindow = 0x00000080;
        internal const long WsExTopMost = 0x00000008;
        internal const long WsOverlappedWindow = 0x00CF0000;
        internal const long WsCaption = 0x00C00000;
        internal const long WsThickFrame = 0x00040000;
        internal const long WsSysMenu = 0x00080000;

        internal const int SwRestore = 9;

        internal static readonly IntPtr HwndTop = IntPtr.Zero;

        internal const int SwpNoSize = 0x0001;
        internal const int SwpNoMove = 0x0002;
        internal const int SwpNoZOrder = 0x0004;
        internal const int SwpNoActivate = 0x0010;
        internal const int SwpNoOwnerZOrder = 0x0200;
        private const int WindowTextBufferLength = 256;

        private const uint GaRoot = 2;
        private const uint WM_SYSCOMMAND = 0x0112;
        private static readonly IntPtr ScClose = new IntPtr(0xF060);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;

            public NativePoint(Point point)
            {
                X = point.X;
                Y = point.Y;
            }

            public Point ToPoint()
            {
                return new Point(X, Y);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public Rectangle ToRectangle()
            {
                return Rectangle.FromLTRB(Left, Top, Right, Bottom);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeWindowPlacement
        {
            public int Length;
            public int Flags;
            public int ShowCommand;
            public NativePoint MinPosition;
            public NativePoint MaxPosition;
            public NativeRect NormalPosition;
        }

        private delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        public static IntPtr GetForegroundWindowHandle()
        {
            return GetForegroundWindow();
        }

        public static bool ActivateWindow(IntPtr windowHandle)
        {
            return windowHandle != IntPtr.Zero && SetForegroundWindow(windowHandle);
        }

        public static bool CloseWindow(IntPtr windowHandle)
        {
            return windowHandle != IntPtr.Zero
                && PostMessage(windowHandle, WM_SYSCOMMAND, ScClose, IntPtr.Zero);
        }

        public static IReadOnlyList<IntPtr> EnumerateWindowsInZOrder()
        {
            var handles = new List<IntPtr>();
            EnumWindows(
                (hwnd, lParam) =>
                {
                    handles.Add(hwnd);
                    return true;
                },
                IntPtr.Zero);
            return handles;
        }

        public static int GetWindowProcessId(IntPtr windowHandle)
        {
            GetWindowThreadProcessId(windowHandle, out var processId);
            return processId;
        }

        public static unsafe string GetWindowClassName(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return string.Empty;
            }

            Span<char> buffer = stackalloc char[WindowTextBufferLength];
            fixed (char* bufferPtr = buffer)
            {
                var length = GetClassName(windowHandle, bufferPtr, buffer.Length);
                return length > 0 ? new string(bufferPtr, 0, length) : string.Empty;
            }
        }

        public static unsafe string GetWindowTextValue(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return string.Empty;
            }

            Span<char> buffer = stackalloc char[WindowTextBufferLength];
            fixed (char* bufferPtr = buffer)
            {
                var length = GetWindowText(windowHandle, bufferPtr, buffer.Length);
                return length > 0 ? new string(bufferPtr, 0, length) : string.Empty;
            }
        }

        public static IntPtr GetWindowLongPtr(IntPtr windowHandle, int index)
        {
            return IntPtr.Size == 4
                ? GetWindowLong32(windowHandle, index)
                : GetWindowLongPtr64(windowHandle, index);
        }

        public static bool IsWindowHandle(IntPtr windowHandle)
        {
            return windowHandle != IntPtr.Zero && IsWindow(windowHandle);
        }

        public static bool IsWindowVisibleOnDesktop(IntPtr windowHandle)
        {
            return windowHandle != IntPtr.Zero && IsWindowVisible(windowHandle);
        }

        public static bool IsWindowMinimized(IntPtr windowHandle)
        {
            return windowHandle != IntPtr.Zero && IsIconic(windowHandle);
        }

        public static bool IsWindowMaximized(IntPtr windowHandle)
        {
            return windowHandle != IntPtr.Zero && IsZoomed(windowHandle);
        }

        public static bool RestoreWindow(IntPtr windowHandle)
        {
            return windowHandle != IntPtr.Zero && ShowWindow(windowHandle, SwRestore);
        }

        public static bool SetWindowPosition(
            IntPtr windowHandle,
            IntPtr insertAfter,
            int x,
            int y,
            int width,
            int height,
            int flags)
        {
            return windowHandle != IntPtr.Zero
                && SetWindowPos(windowHandle, insertAfter, x, y, width, height, flags);
        }

        public static WindowPlacementValue GetWindowPlacementValue(IntPtr windowHandle)
        {
            var placement = new NativeWindowPlacement
            {
                Length = Marshal.SizeOf(typeof(NativeWindowPlacement))
            };

            if (windowHandle == IntPtr.Zero || !GetWindowPlacement(windowHandle, ref placement))
            {
                return new WindowPlacementValue();
            }

            return new WindowPlacementValue
            {
                Flags = placement.Flags,
                ShowCommand = placement.ShowCommand,
                MaxPosition = new PointValue
                {
                    X = placement.MaxPosition.X,
                    Y = placement.MaxPosition.Y
                },
                MinPosition = new PointValue
                {
                    X = placement.MinPosition.X,
                    Y = placement.MinPosition.Y
                },
                NormalPosition = new RectValue
                {
                    X = placement.NormalPosition.Left,
                    Y = placement.NormalPosition.Top,
                    Width = placement.NormalPosition.Right - placement.NormalPosition.Left,
                    Height = placement.NormalPosition.Bottom - placement.NormalPosition.Top
                }
            };
        }

        public static bool SetWindowPlacementValue(IntPtr windowHandle, WindowPlacementValue placement)
        {
            if (windowHandle == IntPtr.Zero || placement == null)
            {
                return false;
            }

            var nativePlacement = new NativeWindowPlacement
            {
                Length = Marshal.SizeOf(typeof(NativeWindowPlacement)),
                Flags = placement.Flags,
                ShowCommand = placement.ShowCommand,
                MaxPosition = new NativePoint(new Point(placement.MaxPosition.X, placement.MaxPosition.Y)),
                MinPosition = new NativePoint(new Point(placement.MinPosition.X, placement.MinPosition.Y)),
                NormalPosition = new NativeRect
                {
                    Left = placement.NormalPosition.X,
                    Top = placement.NormalPosition.Y,
                    Right = placement.NormalPosition.X + placement.NormalPosition.Width,
                    Bottom = placement.NormalPosition.Y + placement.NormalPosition.Height
                }
            };

            return SetWindowPlacement(windowHandle, ref nativePlacement);
        }

        public static void ApplyZOrder(IReadOnlyList<IntPtr> handles)
        {
            if (handles == null || handles.Count < 2)
            {
                return;
            }

            var deferHandle = BeginDeferWindowPos(handles.Count);
            if (deferHandle == IntPtr.Zero)
            {
                return;
            }

            var current = deferHandle;
            for (var index = 1; index < handles.Count; index++)
            {
                current = DeferWindowPos(
                    current,
                    handles[index],
                    handles[index - 1],
                    0,
                    0,
                    0,
                    0,
                    SwpNoOwnerZOrder | SwpNoMove | SwpNoSize | SwpNoActivate);
            }

            EndDeferWindowPos(current);
        }

        public static Rectangle GetWindowRectangle(IntPtr windowHandle)
        {
            return windowHandle != IntPtr.Zero && GetWindowRect(windowHandle, out var rect)
                ? rect.ToRectangle()
                : Rectangle.Empty;
        }

        public static IntPtr GetTopLevelWindowFromPoint(Point screenPoint)
        {
            var handle = WindowFromPoint(new NativePoint(screenPoint));
            return handle == IntPtr.Zero ? IntPtr.Zero : GetAncestor(handle, GaRoot);
        }

        public static Point ScreenToClient(IntPtr windowHandle, Point screenPoint)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return screenPoint;
            }

            var nativePoint = new NativePoint(screenPoint);
            return ScreenToClientCore(windowHandle, ref nativePoint)
                ? nativePoint.ToPoint()
                : screenPoint;
        }

        public static Point ScreenToWorkspace(Point screenPoint)
        {
            var screen = Screen.FromPoint(screenPoint);
            var workspaceOffset = new Point(
                screen.WorkingArea.X - screen.Bounds.X,
                screen.WorkingArea.Y - screen.Bounds.Y);
            return new Point(
                screenPoint.X - workspaceOffset.X,
                screenPoint.Y - workspaceOffset.Y);
        }

        public static Bitmap ScaleImage(Bitmap image, double xScale, double yScale)
        {
            if (image == null)
            {
                return null;
            }

            var scaledWidth = Math.Max(1, (int)(image.Width / xScale));
            var scaledHeight = Math.Max(1, (int)(image.Height / yScale));
            var scaled = new Bitmap(scaledWidth, scaledHeight, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(scaled))
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(image, 0, 0, scaledWidth, scaledHeight);
            }

            return scaled;
        }

        public static IntPtr GetCaptureHandle()
        {
            return GetCapture();
        }

        public static IntPtr SetCaptureHandle(IntPtr windowHandle)
        {
            return windowHandle == IntPtr.Zero ? IntPtr.Zero : SetCapture(windowHandle);
        }

        public static bool ReleaseCaptureHandle()
        {
            return ReleaseCapture();
        }
    }
}
