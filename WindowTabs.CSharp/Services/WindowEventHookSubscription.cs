using System;
using Bemo;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class WindowEventHookSubscription : IDisposable
    {
        private readonly IntPtr windowHandle;
        private readonly Action<IntPtr, WinObjectEventKind> eventHandler;
        private readonly WINEVENTPROC winEventProc;
        private readonly IntPtr showHook;
        private readonly IntPtr hideHook;
        private bool isDisposed;

        public WindowEventHookSubscription(IntPtr windowHandle, Action<IntPtr, WinObjectEventKind> eventHandler)
        {
            this.windowHandle = windowHandle;
            this.eventHandler = eventHandler ?? throw new ArgumentNullException(nameof(eventHandler));
            winEventProc = OnWinEvent;
            showHook = WinUserApi.SetWinEventHook(
                (int)WinObjectEventKind.ObjectShow,
                (int)WinObjectEventKind.ObjectShow,
                IntPtr.Zero,
                winEventProc,
                0,
                0,
                0);
            hideHook = WinUserApi.SetWinEventHook(
                (int)WinObjectEventKind.ObjectHide,
                (int)WinObjectEventKind.ObjectHide,
                IntPtr.Zero,
                winEventProc,
                0,
                0,
                0);
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
                WinUserApi.UnhookWinEvent(showHook);
            }

            if (hideHook != IntPtr.Zero)
            {
                WinUserApi.UnhookWinEvent(hideHook);
            }
        }

        private void OnWinEvent(
            IntPtr hWinEventHook,
            int evt,
            IntPtr hwnd,
            IntPtr idObject,
            IntPtr idChild,
            int dwEventThread,
            int dwmsEventTime)
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
