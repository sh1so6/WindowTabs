using System;
using System.Drawing;
using System.Windows.Forms;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;
using WindowTabs.CSharp.Services;

namespace WindowTabs.CSharp.UI
{
    internal sealed class MigrationShellForm : Form
    {
        private readonly SettingsStore settingsStore;
        private readonly SettingsSession settingsSession;
        private readonly ILocalizationContext localizationContext;
        private readonly FilterService filterService;
        private readonly LauncherService launcherService;
        private readonly NewWindowLaunchSupport launchSupport;
        private readonly PendingWindowLaunchTracker pendingLaunchTracker;
        private readonly ProcessSettingsService processSettingsService;
        private readonly DesktopMonitoringService desktopMonitoringService;
        private readonly DesktopRuntimeSelection desktopRuntimeSelection;
        private readonly ManagerViewRequestDispatcher managerViewRequestDispatcher;
        private readonly IDesktopRuntime desktopRuntime;
        private readonly IDragDrop dragDrop;
        private readonly AppLifecycleState appLifecycleState;
        private readonly HotKeySettingsStore hotKeySettingsStore;
        private readonly ProgramsSettingsControl programsSettingsControl;
        private readonly WorkspaceSettingsControl workspaceSettingsControl;
        private readonly AppearanceSettingsControl appearanceSettingsControl;
        private readonly BehaviorSettingsControl behaviorSettingsControl;
        private readonly DiagnosticsSettingsControl diagnosticsSettingsControl;
        private readonly Label summaryLabel;
        private readonly TabControl mainTabs;
        private readonly TabPage statusTabPage;
        private readonly TabPage programsTabPage;
        private readonly TabPage workspaceTabPage;
        private readonly TabPage appearanceTabPage;
        private readonly TabPage behaviorTabPage;
        private readonly TabPage diagnosticsTabPage;
        private string lastRequestedSettingsView = "none";

        public MigrationShellForm(
            SettingsStore settingsStore,
            SettingsSession settingsSession,
            ILocalizationContext localizationContext,
            FilterService filterService,
            LauncherService launcherService,
            NewWindowLaunchSupport launchSupport,
            PendingWindowLaunchTracker pendingLaunchTracker,
            ProcessSettingsService processSettingsService,
            DesktopMonitoringService desktopMonitoringService,
            DesktopRuntimeSelection desktopRuntimeSelection,
            ManagerViewRequestDispatcher managerViewRequestDispatcher,
            IDesktopRuntime desktopRuntime,
            IDragDrop dragDrop,
            AppLifecycleState appLifecycleState,
            HotKeySettingsStore hotKeySettingsStore,
            ProgramsSettingsControl programsSettingsControl,
            WorkspaceSettingsControl workspaceSettingsControl,
            AppearanceSettingsControl appearanceSettingsControl,
            BehaviorSettingsControl behaviorSettingsControl,
            DiagnosticsSettingsControl diagnosticsSettingsControl)
        {
            this.settingsStore = settingsStore;
            this.settingsSession = settingsSession;
            this.localizationContext = localizationContext;
            this.filterService = filterService;
            this.launcherService = launcherService;
            this.launchSupport = launchSupport;
            this.pendingLaunchTracker = pendingLaunchTracker;
            this.processSettingsService = processSettingsService;
            this.desktopMonitoringService = desktopMonitoringService;
            this.desktopRuntimeSelection = desktopRuntimeSelection;
            this.managerViewRequestDispatcher = managerViewRequestDispatcher;
            this.desktopRuntime = desktopRuntime;
            this.dragDrop = dragDrop;
            this.appLifecycleState = appLifecycleState;
            this.hotKeySettingsStore = hotKeySettingsStore;
            this.programsSettingsControl = programsSettingsControl;
            this.workspaceSettingsControl = workspaceSettingsControl;
            this.appearanceSettingsControl = appearanceSettingsControl;
            this.behaviorSettingsControl = behaviorSettingsControl;
            this.diagnosticsSettingsControl = diagnosticsSettingsControl;

            Text = "WindowTabs C# Migration";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(720, 420);
            Size = new Size(820, 520);

            var header = new Label
            {
                Dock = DockStyle.Top,
                Height = 52,
                Text = "C# Migration Workspace",
                Font = new Font(Font.FontFamily, 18, FontStyle.Bold),
                Padding = new Padding(16, 16, 16, 8)
            };

            summaryLabel = new Label
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                Font = new Font("Consolas", 10),
                Text = BuildSummary(this.desktopMonitoringService.CurrentState),
                AutoEllipsis = false
            };

            statusTabPage = new TabPage("Status");
            statusTabPage.Controls.Add(summaryLabel);

            programsTabPage = new TabPage("Programs");
            programsTabPage.Controls.Add(programsSettingsControl);

            workspaceTabPage = new TabPage("Workspace");
            workspaceTabPage.Controls.Add(workspaceSettingsControl);

            appearanceTabPage = new TabPage("Appearance");
            appearanceTabPage.Controls.Add(appearanceSettingsControl);

            behaviorTabPage = new TabPage("Behavior");
            behaviorTabPage.Controls.Add(behaviorSettingsControl);

            diagnosticsTabPage = new TabPage("Diagnostics");
            diagnosticsTabPage.Controls.Add(diagnosticsSettingsControl);

            mainTabs = new TabControl
            {
                Dock = DockStyle.Fill
            };
            mainTabs.TabPages.Add(statusTabPage);
            mainTabs.TabPages.Add(programsTabPage);
            mainTabs.TabPages.Add(workspaceTabPage);
            mainTabs.TabPages.Add(appearanceTabPage);
            mainTabs.TabPages.Add(behaviorTabPage);
            mainTabs.TabPages.Add(diagnosticsTabPage);

            var languageButton = new Button
            {
                Text = "Reload Localization",
                Dock = DockStyle.Bottom,
                Height = 36
            };
            languageButton.Click += OnReloadLocalization;

            Shown += OnShown;
            FormClosed += OnFormClosed;
            FormClosing += OnFormClosing;
            Resize += OnResize;
            this.desktopMonitoringService.StateChanged += OnMonitoringStateChanged;
            this.managerViewRequestDispatcher.ViewRequested += OnManagerViewRequested;

            Controls.Add(mainTabs);
            Controls.Add(languageButton);
            Controls.Add(header);
        }

        private void OnShown(object sender, EventArgs e)
        {
            desktopMonitoringService.Start();
            summaryLabel.Text = BuildSummary(desktopMonitoringService.CurrentState);
            programsSettingsControl.ReloadRows();
            workspaceSettingsControl.ReloadTree();
            appearanceSettingsControl.ReloadValues();
            diagnosticsSettingsControl.RefreshDiagnostics();
        }

        private void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            desktopMonitoringService.StateChanged -= OnMonitoringStateChanged;
            managerViewRequestDispatcher.ViewRequested -= OnManagerViewRequested;
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (appLifecycleState.AllowExit)
            {
                return;
            }

            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void OnResize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
            }
        }

        private void OnMonitoringStateChanged(object sender, EventArgs e)
        {
            summaryLabel.Text = BuildSummary(desktopMonitoringService.CurrentState);
            programsSettingsControl.ReloadRows();
        }

        private void OnReloadLocalization(object sender, EventArgs e)
        {
            localizationContext.Initialize(settingsSession.Current.LanguageName);
            desktopMonitoringService.RefreshNow("localization-reload");
        }

        private void OnManagerViewRequested(object sender, SettingsViewType view)
        {
            lastRequestedSettingsView = view.ToString();
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }

            SelectRequestedView(view);
            Show();
            Activate();
            BringToFront();
            summaryLabel.Text = BuildSummary(desktopMonitoringService.CurrentState);
            programsSettingsControl.ReloadRows();
            workspaceSettingsControl.ReloadTree();
            appearanceSettingsControl.ReloadValues();
            diagnosticsSettingsControl.RefreshDiagnostics();
        }

        private void SelectRequestedView(SettingsViewType view)
        {
            switch (view)
            {
                case SettingsViewType.ProgramSettings:
                    mainTabs.SelectedTab = programsTabPage;
                    break;
                case SettingsViewType.LayoutSettings:
                    mainTabs.SelectedTab = workspaceTabPage;
                    break;
                case SettingsViewType.AppearanceSettings:
                    mainTabs.SelectedTab = appearanceTabPage;
                    break;
                case SettingsViewType.HotKeySettings:
                    mainTabs.SelectedTab = behaviorTabPage;
                    break;
                case SettingsViewType.DiagnosticsSettings:
                    mainTabs.SelectedTab = diagnosticsTabPage;
                    break;
                default:
                    mainTabs.SelectedTab = statusTabPage;
                    break;
            }
        }

        private string BuildSummary(DesktopMonitorState monitorState)
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

            return string.Join(
                Environment.NewLine,
                "This executable is the parallel C# migration entry point.",
                string.Empty,
                "Current slice:",
                "- JSONC parsing",
                "- Localization loading from Language/*.json",
                "- Settings file load/save and tab appearance defaults",
                "- FilterService and LauncherService logic",
                "- Shell hook and timer-driven monitoring loop",
                string.Empty,
                "Loaded values:",
                "- Language: " + localizationContext.CurrentLanguage,
                "- Settings path: " + settingsStore.SettingsPath,
                "- Run at startup: " + settings.RunAtStartup,
                "- Enable tabbing by default: " + settings.EnableTabbingByDefault,
                "- Included path count: " + settings.IncludedPaths.Count,
                "- Auto grouping path count: " + settings.AutoGroupingPaths.Count,
                "- Filter default mode: " + filterService.IsTabbingEnabledForAllProcessesByDefault,
                "- Pending launches tracked: " + launcherService.IsLaunching(IntPtr.Zero),
                "- Pending launch matcher entries: " + pendingLaunchTracker.Count,
                "- UWP launch fallback: " + (launchSupport.ResolveLaunchCommand(@"C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_foo\WindowsTerminal.exe")?.FileName ?? "none"),
                "- Requested runtime: " + desktopRuntimeSelection.RequestedRuntime,
                "- Runtime: " + (monitorState?.RuntimeKind ?? "unknown"),
                "- Runtime dragging: " + desktopRuntime.IsDragging,
                "- App disabled: " + (monitorState?.IsDisabled ?? false),
                "- Runtime fallback used: " + monitorState?.UsedRuntimeFallback,
                "- Runtime fallback type: " + (string.IsNullOrWhiteSpace(monitorState?.RuntimeFallbackExceptionType) ? "none" : monitorState.RuntimeFallbackExceptionType),
                "- Runtime fallback reason: " + (string.IsNullOrWhiteSpace(monitorState?.RuntimeFallbackReason) ? "none" : monitorState.RuntimeFallbackReason),
                "- Managed drag/drop service: " + dragDrop.GetType().Name,
                "- HotKey prevTab: " + hotKeySettingsStore.Get("prevTab"),
                "- HotKey nextTab: " + hotKeySettingsStore.Get("nextTab"),
                "- Runtime mode hint: set WINDOWTABS_RUNTIME=legacy to force F# runtime",
                "- Last requested settings view: " + lastRequestedSettingsView,
                "- Configured process paths: " + processSettingsService.GetAllConfiguredProcessPaths().Count,
                "- Windows in Z order: " + windows.Count,
                "- Tabbable windows now: " + tabbableCount,
                "- Subscribe candidates: " + plan.WindowsToSubscribe.Count,
                "- Group candidates: " + plan.WindowsToGroup.Count,
                "- Tracked groups after refresh: " + refresh.Groups.Count,
                "- WinEvent subscriptions: " + monitorState?.ActiveWinEventSubscriptions,
                "- Last trigger: " + monitorState?.LastTrigger,
                "- Fast destroy path: " + monitorState?.UsedFastDestroyPath,
                "- Last shell event: " + (monitorState?.LastShellEvent?.ToString() ?? "none"),
                "- Last shell hwnd: " + (monitorState?.LastShellWindowHandle.ToString() ?? "0"),
                "- Last WinEvent: " + (monitorState?.LastWinEvent?.ToString() ?? "none"),
                "- Last WinEvent hwnd: " + (monitorState?.LastWinEventWindowHandle.ToString() ?? "0"),
                "- Last refresh at: " + (monitorState?.LastUpdatedLocal == DateTime.MinValue ? "n/a" : monitorState.LastUpdatedLocal.ToString("yyyy-MM-dd HH:mm:ss")),
                string.Empty,
                "Next migration targets:",
                "- Managed DesktopRuntime group behavior beyond persistence/defaults",
                "- TabStrip target registration onto C# drag/drop runtime",
                "- WindowGroup / TabStripDecorator physical C# port");
        }
    }
}
