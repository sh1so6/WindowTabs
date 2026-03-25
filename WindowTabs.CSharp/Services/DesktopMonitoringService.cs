using System;
using System.Windows.Forms;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class DesktopMonitoringService : IDisposable
    {
        private readonly DesktopRefreshWorkflowService refreshWorkflowService;
        private readonly AppBehaviorState appBehaviorState;
        private readonly DesktopMonitorStateFactory monitorStateFactory;
        private readonly Timer refreshTimer;
        private readonly WindowEventSubscriptionService windowEventSubscriptionService;
        private ShellHookWindow shellHookWindow;
        private int suspensionDepth;
        private bool isShellHookAvailable = true;
        private bool isWinEventMonitoringAvailable = true;
        private string shellHookError = string.Empty;
        private string winEventMonitoringError = string.Empty;
        private bool isStarted;
        private bool isDisposed;

        public DesktopMonitoringService(
            DesktopRefreshWorkflowService refreshWorkflowService,
            DesktopMonitorStateFactory monitorStateFactory,
            AppBehaviorState appBehaviorState)
        {
            this.refreshWorkflowService = refreshWorkflowService ?? throw new ArgumentNullException(nameof(refreshWorkflowService));
            this.monitorStateFactory = monitorStateFactory ?? throw new ArgumentNullException(nameof(monitorStateFactory));
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
            TryStartShellHookWindow();
            refreshTimer.Start();
            RefreshNow("startup");
        }

        public void RefreshNow(string trigger)
        {
            HandleTrigger(trigger, refreshWorkflowService.RefreshDesktop);
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
            var usedFastDestroyPath = shellEvent == ShellEventKind.WindowDestroyed;
            HandleTrigger(
                usedFastDestroyPath ? "shell-hook-fast-destroy" : "shell-hook",
                () => usedFastDestroyPath
                    ? refreshWorkflowService.HandleDestroyedWindow(windowHandle, CurrentState.RefreshResult)
                    : refreshWorkflowService.RefreshDesktop(),
                usedFastDestroyPath,
                shellEvent: shellEvent,
                shellWindowHandle: windowHandle);
        }

        private void OnWinObjectEvent(IntPtr windowHandle, WinObjectEventKind winObjectEvent)
        {
            HandleTrigger(
                "win-event",
                refreshWorkflowService.RefreshDesktop,
                shellEvent: null,
                shellWindowHandle: default,
                winEvent: winObjectEvent,
                winEventWindowHandle: windowHandle);
        }

        private void OnRefreshTimerTick(object sender, EventArgs e)
        {
            RefreshNow("timer");
        }

        private void PublishState(DesktopMonitorStateUpdate update)
        {
            CurrentState = monitorStateFactory.Create(CurrentState, update);
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void HandleTrigger(
            string trigger,
            Func<DesktopRefreshResult> refreshOperation,
            bool usedFastDestroyPath = false,
            ShellEventKind? shellEvent = null,
            IntPtr shellWindowHandle = default,
            WinObjectEventKind? winEvent = null,
            IntPtr winEventWindowHandle = default)
        {
            if (suspensionDepth > 0)
            {
                return;
            }

            if (appBehaviorState.IsDisabled)
            {
                PublishState(CreateStateUpdate(
                    trigger,
                    CurrentState?.RefreshResult,
                    isDisabled: true,
                    usedFastDestroyPath: false,
                    shellEvent: shellEvent,
                    shellWindowHandle: shellWindowHandle,
                    winEvent: winEvent,
                    winEventWindowHandle: winEventWindowHandle));
                return;
            }

            var refreshResult = refreshOperation?.Invoke();
            TrySyncWinEventSubscriptions(refreshResult);
            PublishState(CreateStateUpdate(
                trigger,
                refreshResult,
                isDisabled: false,
                usedFastDestroyPath: usedFastDestroyPath,
                shellEvent: shellEvent,
                shellWindowHandle: shellWindowHandle,
                winEvent: winEvent,
                winEventWindowHandle: winEventWindowHandle,
                activeWinEventSubscriptions: windowEventSubscriptionService.SubscriptionCount));
        }

        private DesktopMonitorStateUpdate CreateStateUpdate(
            string trigger,
            DesktopRefreshResult refreshResult,
            bool isDisabled,
            bool usedFastDestroyPath,
            ShellEventKind? shellEvent = null,
            IntPtr shellWindowHandle = default,
            WinObjectEventKind? winEvent = null,
            IntPtr winEventWindowHandle = default,
            int activeWinEventSubscriptions = 0)
        {
            return new DesktopMonitorStateUpdate
            {
                RefreshResult = refreshResult,
                LastTrigger = trigger,
                LastShellEvent = shellEvent,
                LastShellWindowHandle = shellWindowHandle,
                LastWinEvent = winEvent,
                LastWinEventWindowHandle = winEventWindowHandle,
                ActiveWinEventSubscriptions = activeWinEventSubscriptions,
                UsedFastDestroyPath = usedFastDestroyPath,
                IsShellHookAvailable = isShellHookAvailable,
                ShellHookError = shellHookError,
                IsWinEventMonitoringAvailable = isWinEventMonitoringAvailable,
                WinEventMonitoringError = winEventMonitoringError,
                IsDisabled = isDisabled
            };
        }

        private void TryStartShellHookWindow()
        {
            try
            {
                shellHookWindow = new ShellHookWindow(OnShellEvent);
                isShellHookAvailable = true;
                shellHookError = string.Empty;
            }
            catch (Exception exception)
            {
                isShellHookAvailable = false;
                shellHookError = exception.GetType().Name + ": " + exception.Message;
                UnhandledExceptionLogger.Log(exception, "DesktopMonitoringService.TryStartShellHookWindow");
            }
        }

        private void TrySyncWinEventSubscriptions(DesktopRefreshResult refreshResult)
        {
            if (!isWinEventMonitoringAvailable || refreshResult == null)
            {
                return;
            }

            try
            {
                windowEventSubscriptionService.SyncSubscriptions(refreshResult.Windows, refreshResult.Plan.WindowsToSubscribe);
                winEventMonitoringError = string.Empty;
            }
            catch (Exception exception)
            {
                isWinEventMonitoringAvailable = false;
                winEventMonitoringError = exception.GetType().Name + ": " + exception.Message;
                windowEventSubscriptionService.Dispose();
                UnhandledExceptionLogger.Log(exception, "DesktopMonitoringService.TrySyncWinEventSubscriptions");
            }
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
