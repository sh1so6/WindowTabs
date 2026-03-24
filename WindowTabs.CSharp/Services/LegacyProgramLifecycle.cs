using System;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyProgramLifecycle
    {
        private Action<string> refreshAction = _ => { };
        private int suspendDepth;

        public bool IsDisabled { get; private set; }

        public bool IsShuttingDown { get; private set; }

        public void SetRefreshAction(Action<string> action)
        {
            refreshAction = action ?? (_ => { });
        }

        public void Refresh()
        {
            if (suspendDepth == 0)
            {
                refreshAction("legacy-program-refresh");
            }
        }

        public void RequestShutdown(Action shutdownAction)
        {
            IsShuttingDown = true;
            shutdownAction?.Invoke();
        }

        public void SuspendTabMonitoring()
        {
            suspendDepth++;
        }

        public void ResumeTabMonitoring()
        {
            suspendDepth = Math.Max(0, suspendDepth - 1);
            if (suspendDepth == 0)
            {
                Refresh();
            }
        }

        public void SetDisabled(bool value)
        {
            IsDisabled = value;
        }
    }
}
