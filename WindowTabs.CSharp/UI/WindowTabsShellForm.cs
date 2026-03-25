using System;
using System.Drawing;
using System.Windows.Forms;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;
using WindowTabs.CSharp.Services;

namespace WindowTabs.CSharp.UI
{
    internal sealed class WindowTabsShellForm : Form
    {
        private readonly SettingsSession settingsSession;
        private readonly DesktopMonitoringService desktopMonitoringService;
        private readonly ManagerViewRequestDispatcher managerViewRequestDispatcher;
        private readonly AppLifecycleState appLifecycleState;
        private readonly WindowTabsStatusSummaryBuilder statusSummaryBuilder;
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
        private bool shouldHideOnFirstShow = true;
        private string lastRequestedSettingsView = "none";

        public WindowTabsShellForm(
            SettingsSession settingsSession,
            DesktopMonitoringService desktopMonitoringService,
            ManagerViewRequestDispatcher managerViewRequestDispatcher,
            AppLifecycleState appLifecycleState,
            WindowTabsStatusSummaryBuilder statusSummaryBuilder,
            ProgramsSettingsControl programsSettingsControl,
            WorkspaceSettingsControl workspaceSettingsControl,
            AppearanceSettingsControl appearanceSettingsControl,
            BehaviorSettingsControl behaviorSettingsControl,
            DiagnosticsSettingsControl diagnosticsSettingsControl)
        {
            this.settingsSession = settingsSession;
            this.desktopMonitoringService = desktopMonitoringService;
            this.managerViewRequestDispatcher = managerViewRequestDispatcher;
            this.appLifecycleState = appLifecycleState;
            this.statusSummaryBuilder = statusSummaryBuilder;
            this.programsSettingsControl = programsSettingsControl;
            this.workspaceSettingsControl = workspaceSettingsControl;
            this.appearanceSettingsControl = appearanceSettingsControl;
            this.behaviorSettingsControl = behaviorSettingsControl;
            this.diagnosticsSettingsControl = diagnosticsSettingsControl;

            Text = "WindowTabs Settings";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(720, 420);
            Size = new Size(820, 520);
            ShowInTaskbar = false;
            ShowIcon = false;
            WindowState = FormWindowState.Minimized;
            BackColor = Color.White;

            var header = new Label
            {
                Dock = DockStyle.Top,
                Height = 52,
                Text = "WindowTabs Settings",
                Font = new Font(Font.FontFamily, 18, FontStyle.Bold),
                Padding = new Padding(16, 16, 16, 8),
                BackColor = Color.White
            };

            summaryLabel = new Label
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                Text = BuildSummary(this.desktopMonitoringService.CurrentState),
                AutoEllipsis = false,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(48, 64, 80)
            };

            statusTabPage = new TabPage("Overview");
            statusTabPage.Controls.Add(summaryLabel);

            programsTabPage = new TabPage("Programs");
            programsTabPage.Controls.Add(programsSettingsControl);

            workspaceTabPage = new TabPage("Workspace");
            workspaceTabPage.Controls.Add(workspaceSettingsControl);

            appearanceTabPage = new TabPage("Appearance");
            appearanceTabPage.Controls.Add(appearanceSettingsControl);

            behaviorTabPage = new TabPage("Behavior");
            behaviorTabPage.Controls.Add(behaviorSettingsControl);

            diagnosticsTabPage = new TabPage("Advanced");
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
                Text = "Reload Language",
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
            RefreshSummary();
            RefreshAllTabs();

            if (shouldHideOnFirstShow)
            {
                BeginInvoke((MethodInvoker)HideInitialShell);
            }
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
            RefreshSummary();
            programsSettingsControl.ReloadRows();
        }

        private void OnReloadLocalization(object sender, EventArgs e)
        {
            LocalizationService.Initialize(settingsSession.Current.LanguageName);
            desktopMonitoringService.RefreshNow("localization-reload");
        }

        private void OnManagerViewRequested(object sender, SettingsViewType view)
        {
            shouldHideOnFirstShow = false;
            lastRequestedSettingsView = view.ToString();
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }

            SelectRequestedView(view);
            Show();
            Activate();
            BringToFront();
            RefreshSummary();
            RefreshAllTabs();
        }

        private void HideInitialShell()
        {
            if (!shouldHideOnFirstShow)
            {
                return;
            }

            shouldHideOnFirstShow = false;
            WindowState = FormWindowState.Normal;
            Hide();
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

        private void RefreshAllTabs()
        {
            programsSettingsControl.ReloadRows();
            workspaceSettingsControl.ReloadTree();
            appearanceSettingsControl.ReloadValues();
            diagnosticsSettingsControl.RefreshDiagnostics();
        }

        private void RefreshSummary()
        {
            summaryLabel.Text = BuildSummary(desktopMonitoringService.CurrentState);
        }

        private string BuildSummary(DesktopMonitorState monitorState)
        {
            return statusSummaryBuilder.Build(monitorState, lastRequestedSettingsView);
        }
    }
}
