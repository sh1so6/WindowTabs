using System;

namespace WindowTabs.CSharp.Models
{
    internal sealed class DesktopMonitorStateUpdate
    {
        public DesktopRefreshResult RefreshResult { get; init; }

        public string LastTrigger { get; init; } = string.Empty;

        public ShellEventKind? LastShellEvent { get; init; }

        public IntPtr LastShellWindowHandle { get; init; } = IntPtr.Zero;

        public WinObjectEventKind? LastWinEvent { get; init; }

        public IntPtr LastWinEventWindowHandle { get; init; } = IntPtr.Zero;

        public int ActiveWinEventSubscriptions { get; init; }

        public bool UsedFastDestroyPath { get; init; }

        public bool IsShellHookAvailable { get; init; } = true;

        public string ShellHookError { get; init; } = string.Empty;

        public bool IsWinEventMonitoringAvailable { get; init; } = true;

        public string WinEventMonitoringError { get; init; } = string.Empty;

        public bool IsDisabled { get; init; }
    }
}
