using System;
using System.Drawing;
using System.Windows.Forms;
using WindowTabs.CSharp.Models;
using WindowTabs.CSharp.Services;

namespace WindowTabs.CSharp.UI
{
    internal sealed class MigrationShellForm : Form
    {
        private readonly SettingsStore settingsStore;
        private readonly SettingsSession settingsSession;
        private readonly FilterService filterService;
        private readonly LauncherService launcherService;
        private readonly NewWindowLaunchSupport launchSupport;
        private readonly PendingWindowLaunchTracker pendingLaunchTracker;
        private readonly ProcessSettingsService processSettingsService;
        private readonly DesktopSnapshotService desktopSnapshotService;
        private readonly DesktopPlannerService desktopPlannerService;
        private readonly Label summaryLabel;

        public MigrationShellForm(
            SettingsStore settingsStore,
            SettingsSession settingsSession,
            FilterService filterService,
            LauncherService launcherService,
            NewWindowLaunchSupport launchSupport,
            PendingWindowLaunchTracker pendingLaunchTracker,
            ProcessSettingsService processSettingsService,
            DesktopSnapshotService desktopSnapshotService,
            DesktopPlannerService desktopPlannerService)
        {
            this.settingsStore = settingsStore;
            this.settingsSession = settingsSession;
            this.filterService = filterService;
            this.launcherService = launcherService;
            this.launchSupport = launchSupport;
            this.pendingLaunchTracker = pendingLaunchTracker;
            this.processSettingsService = processSettingsService;
            this.desktopSnapshotService = desktopSnapshotService;
            this.desktopPlannerService = desktopPlannerService;

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
                Text = BuildSummary(),
                AutoEllipsis = false
            };

            var languageButton = new Button
            {
                Text = "Reload Localization",
                Dock = DockStyle.Bottom,
                Height = 36
            };
            languageButton.Click += OnReloadLocalization;

            Controls.Add(summaryLabel);
            Controls.Add(languageButton);
            Controls.Add(header);
        }

        private void OnReloadLocalization(object sender, EventArgs e)
        {
            LocalizationService.Initialize(settingsSession.Current.LanguageName);
            summaryLabel.Text = BuildSummary();
        }

        private string BuildSummary()
        {
            var settings = settingsSession.Current;
            var screenRegion = desktopSnapshotService.GetScreenRegion();
            var windows = desktopSnapshotService.EnumerateWindowsInZOrder();
            desktopPlannerService.UpdateTrackedProcessPaths(windows);
            var plan = desktopPlannerService.BuildPlan(
                windows,
                new GroupSnapshot[0],
                screenRegion,
                new HashSet<IntPtr>(),
                new HashSet<IntPtr>());
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
                string.Empty,
                "Loaded values:",
                "- Language: " + LocalizationService.CurrentLanguage,
                "- Settings path: " + settingsStore.SettingsPath,
                "- Run at startup: " + settings.RunAtStartup,
                "- Enable tabbing by default: " + settings.EnableTabbingByDefault,
                "- Included path count: " + settings.IncludedPaths.Count,
                "- Auto grouping path count: " + settings.AutoGroupingPaths.Count,
                "- Filter default mode: " + filterService.IsTabbingEnabledForAllProcessesByDefault,
                "- Pending launches tracked: " + launcherService.IsLaunching(IntPtr.Zero),
                "- Pending launch matcher entries: " + pendingLaunchTracker.Count,
                "- UWP launch fallback: " + (launchSupport.ResolveLaunchCommand(@"C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_foo\WindowsTerminal.exe")?.FileName ?? "none"),
                "- Configured process paths: " + processSettingsService.GetAllConfiguredProcessPaths().Count,
                "- Windows in Z order: " + windows.Count,
                "- Tabbable windows now: " + tabbableCount,
                "- Subscribe candidates: " + plan.WindowsToSubscribe.Count,
                "- Group candidates: " + plan.WindowsToGroup.Count,
                string.Empty,
                "Next migration targets:",
                "- Shared program types / service contracts",
                "- Window snapshot adapter over Win32 layer",
                "- Desktop / WindowGroup / TabStrip");
        }
    }
}
