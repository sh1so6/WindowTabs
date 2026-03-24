using System;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class WindowEventHookSubscription : IDisposable
    {
        private readonly IntPtr windowHandle;
        private readonly Action<IntPtr, WinObjectEventKind> eventHandler;
        private readonly NativeWindowHookApi.WinEventProc winEventProc;
        private readonly IntPtr showHook;
        private readonly IntPtr hideHook;
        private bool isDisposed;

        public WindowEventHookSubscription(IntPtr windowHandle, Action<IntPtr, WinObjectEventKind> eventHandler)
        {
            this.windowHandle = windowHandle;
            this.eventHandler = eventHandler ?? throw new ArgumentNullException(nameof(eventHandler));
            winEventProc = OnWinEvent;
            showHook = NativeWindowHookApi.SetWinEventHook(
                (uint)WinObjectEventKind.ObjectShow,
                (uint)WinObjectEventKind.ObjectShow,
                winEventProc);
            hideHook = NativeWindowHookApi.SetWinEventHook(
                (uint)WinObjectEventKind.ObjectHide,
                (uint)WinObjectEventKind.ObjectHide,
                winEventProc);
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            if (showHook != IntPtr.Zero)
            {
                NativeWindowHookApi.UnhookWinEvent(showHook);
            }

            if (hideHook != IntPtr.Zero)
            {
                NativeWindowHookApi.UnhookWinEvent(hideHook);
            }
        }

        private void OnWinEvent(
            IntPtr hWinEventHook,
            uint evt,
            IntPtr hwnd,
            IntPtr idObject,
            IntPtr idChild,
            uint dwEventThread,
            uint dwmsEventTime)
        {
            if (hwnd != windowHandle || idObject != IntPtr.Zero)
            {
                return;
            }

            switch (evt)
            {
                case (int)WinObjectEventKind.ObjectShow:
                    eventHandler(hwnd, WinObjectEventKind.ObjectShow);
                    break;
                case (int)WinObjectEventKind.ObjectHide:
                    eventHandler(hwnd, WinObjectEventKind.ObjectHide);
                    break;
            }
        }
    }
}
