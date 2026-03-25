using System;
using System.Text;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class WindowTabsStatusSummaryBuilder
    {
        private readonly SettingsStore settingsStore;
        private readonly SettingsSession settingsSession;
        private readonly FilterService filterService;
        private readonly LauncherService launcherService;
        private readonly NewWindowLaunchSupport launchSupport;
        private readonly PendingWindowLaunchTracker pendingLaunchTracker;
        private readonly ProcessSettingsService processSettingsService;
        private readonly IDesktopRuntime desktopRuntime;
        private readonly IDragDrop dragDrop;
        private readonly HotKeySettingsStore hotKeySettingsStore;
        private readonly StartupComponentStatusService startupComponentStatusService;

        public WindowTabsStatusSummaryBuilder(
            SettingsStore settingsStore,
            SettingsSession settingsSession,
            FilterService filterService,
            LauncherService launcherService,
            NewWindowLaunchSupport launchSupport,
            PendingWindowLaunchTracker pendingLaunchTracker,
            ProcessSettingsService processSettingsService,
            IDesktopRuntime desktopRuntime,
            IDragDrop dragDrop,
            HotKeySettingsStore hotKeySettingsStore,
            StartupComponentStatusService startupComponentStatusService)
        {
            this.settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
            this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
            this.filterService = filterService ?? throw new ArgumentNullException(nameof(filterService));
            this.launcherService = launcherService ?? throw new ArgumentNullException(nameof(launcherService));
            this.launchSupport = launchSupport ?? throw new ArgumentNullException(nameof(launchSupport));
            this.pendingLaunchTracker = pendingLaunchTracker ?? throw new ArgumentNullException(nameof(pendingLaunchTracker));
            this.processSettingsService = processSettingsService ?? throw new ArgumentNullException(nameof(processSettingsService));
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
            this.dragDrop = dragDrop ?? throw new ArgumentNullException(nameof(dragDrop));
            this.hotKeySettingsStore = hotKeySettingsStore ?? throw new ArgumentNullException(nameof(hotKeySettingsStore));
            this.startupComponentStatusService = startupComponentStatusService ?? throw new ArgumentNullException(nameof(startupComponentStatusService));
        }

        public string Build(DesktopMonitorState monitorState, string lastRequestedSettingsView)
        {
            var settings = settingsSession.Current;
            var refresh = monitorState?.RefreshResult ?? new DesktopRefreshResult();
            var windows = refresh.Windows;
            var screenRegion = refresh.ScreenRegion;
            var plan = refresh.Plan;
            var tabbableCount = 0;
            foreach (var window in windows)
            {
                if (filterService.IsTabbableWindow(window, screenRegion))
                {
                    tabbableCount++;
                }
            }

            var builder = new StringBuilder();
            AppendLine(builder, "This executable is the managed WindowTabs entry point.");
            AppendLine(builder);
            AppendLine(builder, "Current runtime coverage:");
            AppendLine(builder, "- JSONC parsing");
            AppendLine(builder, "- Localization loading from Language/*.json");
            AppendLine(builder, "- Settings file load/save and tab appearance defaults");
            AppendLine(builder, "- FilterService and LauncherService logic");
            AppendLine(builder, "- Shell hook and timer-driven monitoring loop");
            AppendLine(builder);
            AppendLine(builder, "Loaded values:");
            AppendLine(builder, "- Language: " + LocalizationService.CurrentLanguage);
            AppendLine(builder, "- Settings path: " + settingsStore.SettingsPath);
            AppendLine(builder, "- Run at startup: " + settings.RunAtStartup);
            AppendLine(builder, "- Enable tabbing by default: " + settings.EnableTabbingByDefault);
            AppendLine(builder, "- Included path count: " + settings.IncludedPaths.Count);
            AppendLine(builder, "- Auto grouping path count: " + settings.AutoGroupingPaths.Count);
            AppendLine(builder, "- Filter default mode: " + filterService.IsTabbingEnabledForAllProcessesByDefault);
            AppendLine(builder, "- Pending launches tracked: " + launcherService.IsLaunching(IntPtr.Zero));
            AppendLine(builder, "- Pending launch matcher entries: " + pendingLaunchTracker.Count);
            AppendLine(builder, "- UWP launch fallback: " + (launchSupport.ResolveLaunchCommand(@"C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_foo\WindowsTerminal.exe")?.FileName ?? "none"));
            AppendLine(builder, "- Runtime: " + (monitorState?.RuntimeKind ?? "unknown"));
            AppendLine(builder, "- Runtime dragging: " + desktopRuntime.IsDragging);
            AppendLine(builder, "- App disabled: " + (monitorState?.IsDisabled ?? false));
            AppendLine(builder, "- Shell hook available: " + monitorState?.IsShellHookAvailable);
            AppendLine(builder, "- Shell hook error: " + (string.IsNullOrWhiteSpace(monitorState?.ShellHookError) ? "none" : monitorState.ShellHookError));
            AppendLine(builder, "- WinEvent monitoring available: " + monitorState?.IsWinEventMonitoringAvailable);
            AppendLine(builder, "- WinEvent monitoring error: " + (string.IsNullOrWhiteSpace(monitorState?.WinEventMonitoringError) ? "none" : monitorState.WinEventMonitoringError));
            AppendLine(builder, "- Managed drag/drop service: " + dragDrop.GetType().Name);
            AppendLine(builder, "- HotKey prevTab: " + hotKeySettingsStore.Get("prevTab"));
            AppendLine(builder, "- HotKey nextTab: " + hotKeySettingsStore.Get("nextTab"));
            AppendLine(builder, "- Startup degraded: " + startupComponentStatusService.HasFailures);
            AppendLine(builder, "- Startup errors: " + startupComponentStatusService.BuildSummary());
            AppendLine(builder, "- Last requested settings view: " + lastRequestedSettingsView);
            AppendLine(builder, "- Configured process paths: " + processSettingsService.GetAllConfiguredProcessPaths().Count);
            AppendLine(builder, "- Windows in Z order: " + windows.Count);
            AppendLine(builder, "- Tabbable windows now: " + tabbableCount);
            AppendLine(builder, "- Subscribe candidates: " + plan.WindowsToSubscribe.Count);
            AppendLine(builder, "- Group candidates: " + plan.WindowsToGroup.Count);
            AppendLine(builder, "- Regroup candidates: " + plan.WindowsToRegroup.Count);
            AppendLine(builder, "- Reorder candidates: " + plan.WindowsToReorder.Count);
            AppendLine(builder, "- Tracked groups after refresh: " + refresh.Groups.Count);
            AppendLine(builder, "- WinEvent subscriptions: " + monitorState?.ActiveWinEventSubscriptions);
            AppendLine(builder, "- Last trigger: " + monitorState?.LastTrigger);
            AppendLine(builder, "- Fast destroy path: " + monitorState?.UsedFastDestroyPath);
            AppendLine(builder, "- Last shell event: " + (monitorState?.LastShellEvent?.ToString() ?? "none"));
            AppendLine(builder, "- Last shell hwnd: " + (monitorState?.LastShellWindowHandle.ToString() ?? "0"));
            AppendLine(builder, "- Last WinEvent: " + (monitorState?.LastWinEvent?.ToString() ?? "none"));
            AppendLine(builder, "- Last WinEvent hwnd: " + (monitorState?.LastWinEventWindowHandle.ToString() ?? "0"));
            AppendLine(builder, "- Last refresh at: " + (monitorState?.LastUpdatedLocal == DateTime.MinValue ? "n/a" : monitorState.LastUpdatedLocal.ToString("yyyy-MM-dd HH:mm:ss")));
            AppendLine(builder);
            AppendLine(builder, "Current watchpoints:");
            AppendLine(builder, "- Managed strip / drag-drop parity and regression coverage");
            AppendLine(builder, "- Runtime refresh / monitoring behavior on degraded shell-hook paths");
            return builder.ToString();
        }

        private static void AppendLine(StringBuilder builder, string line = "")
        {
            builder.AppendLine(line);
        }
    }
}
