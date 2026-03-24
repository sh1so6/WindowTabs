using System;
using System.Windows.Forms;
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
            shellHookMessage = NativeWindowHookApi.RegisterWindowMessage("SHELLHOOK");
            CreateHandle(new CreateParams
            {
                Caption = "WindowTabs.CSharp.ShellHookWindow"
            });

            NativeWindowHookApi.RegisterShellHookWindow(Handle);
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
                case NativeWindowHookApi.HshellWindowCreated:
                    return ShellEventKind.WindowCreated;
                case NativeWindowHookApi.HshellWindowDestroyed:
                    return ShellEventKind.WindowDestroyed;
                case NativeWindowHookApi.HshellWindowActivated:
                    return ShellEventKind.WindowActivated;
                case NativeWindowHookApi.HshellRudeAppActivated:
                    return ShellEventKind.RudeAppActivated;
                default:
                    return null;
            }
        }
    }
}
