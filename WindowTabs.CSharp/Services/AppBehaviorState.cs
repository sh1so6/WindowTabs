using System;

namespace WindowTabs.CSharp.Services
{
    internal sealed class AppBehaviorState
    {
        private readonly SettingsSession settingsSession;

        public AppBehaviorState(SettingsSession settingsSession)
        {
            this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
            IsDisabled = settingsSession.Current.IsDisabled;
        }

        public event EventHandler DisabledChanged;

        public bool IsDisabled { get; private set; }

        public void SetDisabled(bool value)
        {
            if (IsDisabled == value)
            {
                return;
            }

            IsDisabled = value;
            settingsSession.Update(snapshot => snapshot.IsDisabled = value);
            DisabledChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ToggleDisabled()
        {
            SetDisabled(!IsDisabled);
        }
    }
}
