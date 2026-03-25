using System;

namespace WindowTabs.CSharp.Services
{
    internal sealed class RefreshCoordinator
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
