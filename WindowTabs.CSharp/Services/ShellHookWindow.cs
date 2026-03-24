using System;
using System.Windows.Forms;
using Bemo;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ShellHookWindow : NativeWindow, IDisposable
    {
        private readonly Action<IntPtr, ShellEventKind> eventHandler;
        private readonly int shellHookMessage;
        private bool isDisposed;

        public ShellHookWindow(Action<IntPtr, ShellEventKind> eventHandler)
        {
            this.eventHandler = eventHandler ?? throw new ArgumentNullException(nameof(eventHandler));
            shellHookMessage = WinUserApi.RegisterWindowMessage("SHELLHOOK");
            CreateHandle(new CreateParams
            {
                Caption = "WindowTabs.CSharp.ShellHookWindow"
            });

            WinUserApi.RegisterShellHookWindow(Handle);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == shellHookMessage)
            {
                var shellEvent = ToShellEventKind(m.WParam.ToInt32());
                if (shellEvent.HasValue)
                {
                    eventHandler(m.LParam, shellEvent.Value);
                }
            }

            base.WndProc(ref m);
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            DestroyHandle();
        }

        private static ShellEventKind? ToShellEventKind(int shellEventCode)
        {
            switch (shellEventCode)
            {
                case WindowsHookShellEvents.HSHELL_WINDOWCREATED:
                    return ShellEventKind.WindowCreated;
                case WindowsHookShellEvents.HSHELL_WINDOWDESTROYED:
                    return ShellEventKind.WindowDestroyed;
                case WindowsHookShellEvents.HSHELL_WINDOWACTIVATED:
                    return ShellEventKind.WindowActivated;
                case WindowsHookShellEvents.HSHELL_RUDEAPPACTIVATED:
                    return ShellEventKind.RudeAppActivated;
                default:
                    return null;
            }
        }
    }
}
