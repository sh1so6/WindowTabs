using System;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class RefreshCoordinator : IProgramRefresher
    {
        private Action refreshAction = () => { };

        public void SetRefreshAction(Action refreshAction)
        {
            this.refreshAction = refreshAction ?? (() => { });
        }

        public void Refresh()
        {
            refreshAction();
        }
    }
}
