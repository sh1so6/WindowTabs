using System;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupDefaultsService
    {
        private readonly SettingsSession settingsSession;

        public ManagedGroupDefaultsService(SettingsSession settingsSession)
        {
            this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
        }

        public void Apply(IWindowGroupRuntime group)
        {
            if (group == null)
            {
                return;
            }

            var settings = settingsSession.Current;
            group.TabPosition = string.IsNullOrWhiteSpace(settings.TabPositionByDefault)
                ? "TopRight"
                : settings.TabPositionByDefault;
            group.SnapTabHeightMargin = settings.SnapTabHeightMargin;
        }
    }
}
