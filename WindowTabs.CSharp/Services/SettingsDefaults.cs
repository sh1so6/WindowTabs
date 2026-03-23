using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal static class SettingsDefaults
    {
        public static SettingsSnapshot Create(bool hasExistingSettings)
        {
            return new SettingsSnapshot
            {
                RunAtStartup = !hasExistingSettings,
                EnableTabbingByDefault = !hasExistingSettings,
                HideInactiveTabs = false,
                EnableCtrlNumberHotKey = false,
                EnableHoverActivate = false,
                TabPositionByDefault = "TopRight",
                HideTabsWhenDownByDefault = "never",
                HideTabsDelayMilliseconds = 3000,
                HideTabsOnFullscreen = true,
                SnapTabHeightMargin = false,
                TabAppearance = CreateDefaultTabAppearance(),
                LanguageName = "English"
            };
        }

        public static TabAppearanceInfo CreateDefaultTabAppearance()
        {
            return new TabAppearanceInfo
            {
                TabHeight = 25,
                TabMaxWidth = 200,
                TabPinnedTabWidth = 90,
                TabPinnedTabWidthIcon = true,
                TabOverlap = 20,
                TabHeightOffset = 1,
                TabIndentFlipped = 150,
                TabIndentNormal = 4,
                TabInactiveTextColor = ColorSerialization.FromRgb(0x000000),
                TabMouseOverTextColor = ColorSerialization.FromRgb(0x000000),
                TabActiveTextColor = ColorSerialization.FromRgb(0x000000),
                TabFlashTextColor = ColorSerialization.FromRgb(0x000000),
                TabInactiveTabColor = ColorSerialization.FromRgb(0x9FC4F0),
                TabMouseOverTabColor = ColorSerialization.FromRgb(0xBDD5F4),
                TabActiveTabColor = ColorSerialization.FromRgb(0xFAFCFE),
                TabFlashTabColor = ColorSerialization.FromRgb(0xFFBBBB),
                TabInactiveBorderColor = ColorSerialization.FromRgb(0x3A70B1),
                TabMouseOverBorderColor = ColorSerialization.FromRgb(0x3A70B1),
                TabActiveBorderColor = ColorSerialization.FromRgb(0x3A70B1),
                TabFlashBorderColor = ColorSerialization.FromRgb(0x3A70B1)
            };
        }
    }
}
