using System;
using Bemo;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyProgramSettingsFacade
    {
        private readonly ProcessSettingsService processSettingsService;
        private readonly HotKeySettingsStore hotKeySettingsStore;

        public LegacyProgramSettingsFacade(
            ProcessSettingsService processSettingsService,
            HotKeySettingsStore hotKeySettingsStore)
        {
            this.processSettingsService = processSettingsService ?? throw new ArgumentNullException(nameof(processSettingsService));
            this.hotKeySettingsStore = hotKeySettingsStore ?? throw new ArgumentNullException(nameof(hotKeySettingsStore));
        }

        public bool GetAutoGroupingEnabled(string processPath)
        {
            return processSettingsService.GetAutoGroupingEnabled(processPath);
        }

        public void SetAutoGroupingEnabled(string processPath, bool enabled)
        {
            processSettingsService.SetAutoGroupingEnabled(processPath, enabled);
        }

        public bool GetCategoryEnabled(string processPath, int categoryNumber)
        {
            return processSettingsService.GetCategoryEnabled(processPath, categoryNumber);
        }

        public void SetCategoryEnabled(string processPath, int categoryNumber, bool enabled)
        {
            processSettingsService.SetCategoryEnabled(processPath, categoryNumber, enabled);
        }

        public void SetHotKey(string key, int value)
        {
            hotKeySettingsStore.Set(key, value);
        }

        public int GetHotKey(string key)
        {
            return hotKeySettingsStore.Get(key);
        }

        public List2<string> GetAllConfiguredProcessPaths()
        {
            return FSharpListAdapter.ToList2(processSettingsService.GetAllConfiguredProcessPaths());
        }

        public void RemoveProcessSettings(string processPath)
        {
            processSettingsService.RemoveProcessSettings(processPath);
        }
    }
}
