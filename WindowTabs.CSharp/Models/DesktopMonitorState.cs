using System;

namespace WindowTabs.CSharp.Models
{
    internal sealed class DesktopMonitorState
    {
        public DesktopRefreshResult RefreshResult { get; set; } = new DesktopRefreshResult();

        public string LastTrigger { get; set; } = "not-started";

        public DateTime LastUpdatedLocal { get; set; } = DateTime.MinValue;

        public IntPtr LastShellWindowHandle { get; set; } = IntPtr.Zero;

        public ShellEventKind? LastShellEvent { get; set; }

        public IntPtr LastWinEventWindowHandle { get; set; } = IntPtr.Zero;

        public WinObjectEventKind? LastWinEvent { get; set; }

        public int ActiveWinEventSubscriptions { get; set; }

        public bool UsedFastDestroyPath { get; set; }

        public string RuntimeKind { get; set; } = string.Empty;

        public bool IsShellHookAvailable { get; set; } = true;

        public string ShellHookError { get; set; } = string.Empty;

        public bool IsWinEventMonitoringAvailable { get; set; } = true;

        public string WinEventMonitoringError { get; set; } = string.Empty;

        public bool IsDisabled { get; set; }
    }
}
