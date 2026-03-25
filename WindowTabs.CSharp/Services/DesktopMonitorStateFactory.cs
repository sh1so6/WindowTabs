using System;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class DesktopMonitorStateFactory
    {
        private readonly IDesktopRuntime desktopRuntime;

        public DesktopMonitorStateFactory(IDesktopRuntime desktopRuntime)
        {
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
        }

        public DesktopMonitorState Create(DesktopMonitorState previousState, DesktopMonitorStateUpdate update)
        {
            var priorState = previousState ?? new DesktopMonitorState();
            var stateUpdate = update ?? new DesktopMonitorStateUpdate();
            var hasShellEvent = stateUpdate.LastShellEvent.HasValue;
            var hasWinEvent = stateUpdate.LastWinEvent.HasValue;

            return new DesktopMonitorState
            {
                RefreshResult = stateUpdate.RefreshResult ?? priorState.RefreshResult ?? new DesktopRefreshResult(),
                LastTrigger = string.IsNullOrWhiteSpace(stateUpdate.LastTrigger)
                    ? (stateUpdate.IsDisabled ? "disabled" : "manual")
                    : stateUpdate.LastTrigger,
                LastUpdatedLocal = DateTime.Now,
                LastShellEvent = hasShellEvent ? stateUpdate.LastShellEvent : priorState.LastShellEvent,
                LastShellWindowHandle = hasShellEvent ? stateUpdate.LastShellWindowHandle : priorState.LastShellWindowHandle,
                LastWinEvent = hasWinEvent ? stateUpdate.LastWinEvent : priorState.LastWinEvent,
                LastWinEventWindowHandle = hasWinEvent ? stateUpdate.LastWinEventWindowHandle : priorState.LastWinEventWindowHandle,
                ActiveWinEventSubscriptions = stateUpdate.ActiveWinEventSubscriptions,
                UsedFastDestroyPath = stateUpdate.UsedFastDestroyPath,
                RuntimeKind = desktopRuntime.GetType().Name,
                IsShellHookAvailable = stateUpdate.IsShellHookAvailable,
                ShellHookError = stateUpdate.ShellHookError ?? string.Empty,
                IsWinEventMonitoringAvailable = stateUpdate.IsWinEventMonitoringAvailable,
                WinEventMonitoringError = stateUpdate.WinEventMonitoringError ?? string.Empty,
                IsDisabled = stateUpdate.IsDisabled
            };
        }
    }
}
