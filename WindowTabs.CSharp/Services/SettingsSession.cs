using System;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class SettingsSession
    {
        private readonly SettingsStore store;

        public SettingsSession(SettingsStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            Current = store.Load();
        }

        public SettingsSnapshot Current { get; private set; }

        public event EventHandler Changed;

        public void Update(Action<SettingsSnapshot> update)
        {
            if (update == null)
            {
                throw new ArgumentNullException(nameof(update));
            }

            update(Current);
            store.Save(Current);
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public void Reload()
        {
            Current = store.Load();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
