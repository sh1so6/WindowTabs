using System;
using System.Windows.Forms;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class DesktopMonitoringService : IDisposable
    {
        private readonly DesktopSessionCoordinator desktopSessionCoordinator;
        private readonly DesktopRuntimeSelection desktopRuntimeSelection;
        private readonly AppBehaviorState appBehaviorState;
        private readonly Timer refreshTimer;
        private readonly WindowEventSubscriptionService windowEventSubscriptionService;
        private ShellHookWindow shellHookWindow;
        private int suspensionDepth;
        private bool isStarted;
        private bool isDisposed;

        public DesktopMonitoringService(
            DesktopSessionCoordinator desktopSessionCoordinator,
            DesktopRuntimeSelection desktopRuntimeSelection,
            AppBehaviorState appBehaviorState)
        {
            this.desktopSessionCoordinator = desktopSessionCoordinator ?? throw new ArgumentNullException(nameof(desktopSessionCoordinator));
            this.desktopRuntimeSelection = desktopRuntimeSelection ?? throw new ArgumentNullException(nameof(desktopRuntimeSelection));
            this.appBehaviorState = appBehaviorState ?? throw new ArgumentNullException(nameof(appBehaviorState));
            windowEventSubscriptionService = new WindowEventSubscriptionService(OnWinObjectEvent);
            refreshTimer = new Timer
            {
                Interval = 1500
            };
            refreshTimer.Tick += OnRefreshTimerTick;
        }

        public event EventHandler StateChanged;

        public DesktopMonitorState CurrentState { get; private set; } = new DesktopMonitorState();

        public void Start()
        {
            if (isStarted)
            {
                return;
            }

            isStarted = true;
            shellHookWindow = new ShellHookWindow(OnShellEvent);
            refreshTimer.Start();
            RefreshNow("startup");
        }

        public void RefreshNow(string trigger)
        {
            if (suspensionDepth > 0)
            {
                return;
            }

            if (appBehaviorState.IsDisabled)
            {
                CurrentState = BuildDisabledState(trigger);
                StateChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            var refreshResult = desktopSessionCoordinator.RefreshDesktop();
            windowEventSubscriptionService.SyncSubscriptions(refreshResult.Windows, refreshResult.Plan.WindowsToSubscribe);
            CurrentState = new DesktopMonitorState
            {
                RefreshResult = refreshResult,
                LastTrigger = string.IsNullOrWhiteSpace(trigger) ? "manual" : trigger,
                LastUpdatedLocal = DateTime.Now,
                LastShellEvent = CurrentState.LastShellEvent,
                LastShellWindowHandle = CurrentState.LastShellWindowHandle,
                LastWinEvent = CurrentState.LastWinEvent,
                LastWinEventWindowHandle = CurrentState.LastWinEventWindowHandle,
                ActiveWinEventSubscriptions = windowEventSubscriptionService.SubscriptionCount,
                UsedFastDestroyPath = false,
                RuntimeKind = desktopSessionCoordinator.RuntimeKind,
                UsedRuntimeFallback = desktopRuntimeSelection.UsedFallback,
                RuntimeFallbackReason = desktopRuntimeSelection.FallbackReason,
                RuntimeFallbackExceptionType = desktopRuntimeSelection.ExceptionType,
                IsDisabled = false
            };

            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            refreshTimer.Stop();
            refreshTimer.Tick -= OnRefreshTimerTick;
            refreshTimer.Dispose();
            windowEventSubscriptionService.Dispose();
            shellHookWindow?.Dispose();
        }

        public IDisposable SuspendRefresh()
        {
            suspensionDepth++;
            return new RefreshSuspension(this);
        }

        private void OnShellEvent(IntPtr windowHandle, ShellEventKind shellEvent)
        {
            if (suspensionDepth > 0)
            {
                return;
            }

            if (appBehaviorState.IsDisabled)
            {
                CurrentState = BuildDisabledState("shell-hook-disabled");
                CurrentState.LastShellEvent = shellEvent;
                CurrentState.LastShellWindowHandle = windowHandle;
                StateChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            var refreshResult = shellEvent == ShellEventKind.WindowDestroyed
                ? desktopSessionCoordinator.HandleDestroyedWindow(windowHandle, CurrentState.RefreshResult)
                : desktopSessionCoordinator.RefreshDesktop();
            windowEventSubscriptionService.SyncSubscriptions(refreshResult.Windows, refreshResult.Plan.WindowsToSubscribe);
            CurrentState = new DesktopMonitorState
            {
                RefreshResult = refreshResult,
                LastTrigger = shellEvent == ShellEventKind.WindowDestroyed ? "shell-hook-fast-destroy" : "shell-hook",
                LastUpdatedLocal = DateTime.Now,
                LastShellEvent = shellEvent,
                LastShellWindowHandle = windowHandle,
                LastWinEvent = CurrentState.LastWinEvent,
                LastWinEventWindowHandle = CurrentState.LastWinEventWindowHandle,
                ActiveWinEventSubscriptions = windowEventSubscriptionService.SubscriptionCount,
                UsedFastDestroyPath = shellEvent == ShellEventKind.WindowDestroyed,
                RuntimeKind = desktopSessionCoordinator.RuntimeKind,
                UsedRuntimeFallback = desktopRuntimeSelection.UsedFallback,
                RuntimeFallbackReason = desktopRuntimeSelection.FallbackReason,
                RuntimeFallbackExceptionType = desktopRuntimeSelection.ExceptionType,
                IsDisabled = false
            };

            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnWinObjectEvent(IntPtr windowHandle, WinObjectEventKind winObjectEvent)
        {
            if (suspensionDepth > 0)
            {
                return;
            }

            if (appBehaviorState.IsDisabled)
            {
                CurrentState = BuildDisabledState("win-event-disabled");
                CurrentState.LastWinEvent = winObjectEvent;
                CurrentState.LastWinEventWindowHandle = windowHandle;
                StateChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            var refreshResult = desktopSessionCoordinator.RefreshDesktop();
            windowEventSubscriptionService.SyncSubscriptions(refreshResult.Windows, refreshResult.Plan.WindowsToSubscribe);
            CurrentState = new DesktopMonitorState
            {
                RefreshResult = refreshResult,
                LastTrigger = "win-event",
                LastUpdatedLocal = DateTime.Now,
                LastShellEvent = CurrentState.LastShellEvent,
                LastShellWindowHandle = CurrentState.LastShellWindowHandle,
                LastWinEvent = winObjectEvent,
                LastWinEventWindowHandle = windowHandle,
                ActiveWinEventSubscriptions = windowEventSubscriptionService.SubscriptionCount,
                UsedFastDestroyPath = false,
                RuntimeKind = desktopSessionCoordinator.RuntimeKind,
                UsedRuntimeFallback = desktopRuntimeSelection.UsedFallback,
                RuntimeFallbackReason = desktopRuntimeSelection.FallbackReason,
                RuntimeFallbackExceptionType = desktopRuntimeSelection.ExceptionType,
                IsDisabled = false
            };

            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnRefreshTimerTick(object sender, EventArgs e)
        {
            RefreshNow("timer");
        }

        private DesktopMonitorState BuildDisabledState(string trigger)
        {
            return new DesktopMonitorState
            {
                RefreshResult = CurrentState?.RefreshResult ?? new DesktopRefreshResult(),
                LastTrigger = string.IsNullOrWhiteSpace(trigger) ? "disabled" : trigger,
                LastUpdatedLocal = DateTime.Now,
                LastShellEvent = CurrentState?.LastShellEvent,
                LastShellWindowHandle = CurrentState?.LastShellWindowHandle ?? IntPtr.Zero,
                LastWinEvent = CurrentState?.LastWinEvent,
                LastWinEventWindowHandle = CurrentState?.LastWinEventWindowHandle ?? IntPtr.Zero,
                ActiveWinEventSubscriptions = 0,
                UsedFastDestroyPath = false,
                RuntimeKind = desktopSessionCoordinator.RuntimeKind,
                UsedRuntimeFallback = desktopRuntimeSelection.UsedFallback,
                RuntimeFallbackReason = desktopRuntimeSelection.FallbackReason,
                RuntimeFallbackExceptionType = desktopRuntimeSelection.ExceptionType,
                IsDisabled = true
            };
        }

        private sealed class RefreshSuspension : IDisposable
        {
            private readonly DesktopMonitoringService owner;
            private bool isDisposed;

            public RefreshSuspension(DesktopMonitoringService owner)
            {
                this.owner = owner;
            }

            public void Dispose()
            {
                if (isDisposed)
                {
                    return;
                }

                isDisposed = true;
                owner.suspensionDepth = Math.Max(0, owner.suspensionDepth - 1);
            }
        }
    }
}
