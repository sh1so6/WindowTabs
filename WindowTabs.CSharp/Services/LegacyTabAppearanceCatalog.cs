using System;
using System.IO;
using Bemo;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyTabAppearanceCatalog
    {
        private readonly SettingsStore settingsStore;
        private readonly SettingsSession settingsSession;
        private readonly TabAppearancePresetCatalog presetCatalog;
        private readonly BemoSettingsValueConverter valueConverter;

        public LegacyTabAppearanceCatalog(
            SettingsStore settingsStore,
            SettingsSession settingsSession,
            TabAppearancePresetCatalog presetCatalog,
            BemoSettingsValueConverter valueConverter)
        {
            this.settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
            this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
            this.presetCatalog = presetCatalog ?? throw new ArgumentNullException(nameof(presetCatalog));
            this.valueConverter = valueConverter ?? throw new ArgumentNullException(nameof(valueConverter));
        }

        public TabAppearanceInfo Current => valueConverter.ToBemoTabAppearance(settingsSession.Current.TabAppearance);

        public TabAppearanceInfo Default => valueConverter.ToBemoTabAppearance(presetCatalog.Default);

        public TabAppearanceInfo DarkMode => valueConverter.ToBemoTabAppearance(presetCatalog.DarkMode);

        public TabAppearanceInfo DarkModeBlue => valueConverter.ToBemoTabAppearance(presetCatalog.DarkModeBlue);

        public TabAppearanceInfo LightMono => valueConverter.ToBemoTabAppearance(presetCatalog.LightMono);

        public TabAppearanceInfo DarkMono => valueConverter.ToBemoTabAppearance(presetCatalog.DarkMono);

        public TabAppearanceInfo DarkRedFrame => valueConverter.ToBemoTabAppearance(presetCatalog.DarkRedFrame);

        public bool IsFirstRun => !File.Exists(settingsStore.SettingsPath);
    }
}
