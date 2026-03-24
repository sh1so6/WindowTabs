using System;
using System.Diagnostics;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyNewWindowLauncher
    {
        private readonly PendingWindowLaunchTracker pendingWindowLaunchTracker;
        private readonly NewWindowLaunchSupport launchSupport;

        public LegacyNewWindowLauncher(
            PendingWindowLaunchTracker pendingWindowLaunchTracker,
            NewWindowLaunchSupport launchSupport)
        {
            this.pendingWindowLaunchTracker = pendingWindowLaunchTracker ?? throw new ArgumentNullException(nameof(pendingWindowLaunchTracker));
            this.launchSupport = launchSupport ?? throw new ArgumentNullException(nameof(launchSupport));
        }

        public void Launch(IntPtr groupHwnd, IntPtr invokerHwnd, string processPath)
        {
            if (string.IsNullOrWhiteSpace(processPath))
            {
                return;
            }

            pendingWindowLaunchTracker.Register(processPath, groupHwnd, invokerHwnd);
            try
            {
                var command = launchSupport.ResolveLaunchCommand(processPath);
                var startInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = command?.FileName ?? processPath,
                    Arguments = command?.Arguments ?? string.Empty
                };
                Process.Start(startInfo);
            }
            catch
            {
            }
        }
    }
}
