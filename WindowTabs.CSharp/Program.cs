using System;
using System.Windows.Forms;
using WindowTabs.CSharp.Services;
using WindowTabs.CSharp.UI;

namespace WindowTabs.CSharp
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var settingsStore = new SettingsStore(isStandalone: false);
            var settingsSession = new SettingsSession(settingsStore);
            LocalizationService.Initialize(settingsSession.Current.LanguageName);
            var filterService = new FilterService(settingsSession, new NullProgramRefresher());
            var launcherService = new LauncherService();
            var launchSupport = new NewWindowLaunchSupport();
            var pendingLaunchTracker = new PendingWindowLaunchTracker();
            var processSettingsService = new ProcessSettingsService(settingsSession, settingsStore);
            var desktopSnapshotService = new DesktopSnapshotService();
            var desktopPlannerService = new DesktopPlannerService(filterService, launcherService, pendingLaunchTracker);

            Application.Run(new MigrationShellForm(
                settingsStore,
                settingsSession,
                filterService,
                launcherService,
                launchSupport,
                pendingLaunchTracker,
                processSettingsService,
                desktopSnapshotService,
                desktopPlannerService));
        }
    }
}
