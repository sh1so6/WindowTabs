using System;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagerViewRequestDispatcher
    {
        public event EventHandler<SettingsViewType> ViewRequested;

        public void RequestShow()
        {
            RequestShow(SettingsViewType.ProgramSettings);
        }

        public void RequestShow(SettingsViewType view)
        {
            ViewRequested?.Invoke(this, view);
        }
    }
}
