using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace WindowTabs.CSharp.Services
{
    internal sealed class AppRestartService
    {
        private readonly AppLifecycleState appLifecycleState;

        public AppRestartService(AppLifecycleState appLifecycleState)
        {
            this.appLifecycleState = appLifecycleState ?? throw new ArgumentNullException(nameof(appLifecycleState));
        }

        public void Restart()
        {
            var exePath = Assembly.GetExecutingAssembly().Location;
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c timeout /t 2 /nobreak >nul && start \"\" \"" + exePath + "\"",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(exePath) ?? "."
            };

            Process.Start(startInfo);
            appLifecycleState.RequestExit();
        }
    }
}
