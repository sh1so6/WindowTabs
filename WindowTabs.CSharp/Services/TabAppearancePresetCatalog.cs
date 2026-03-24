using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class TabAppearancePresetCatalog
    {
        public TabAppearanceInfo Default => SettingsDefaults.CreateDefaultTabAppearance();

        public TabAppearanceInfo DarkMode => new TabAppearanceInfo
        {
            TabHeight = -1,
            TabMaxWidth = -1,
            TabPinnedTabWidth = -1,
            TabPinnedTabWidthIcon = false,
            TabOverlap = -1,
            TabHeightOffset = -1,
            TabIndentFlipped = -1,
            TabIndentNormal = -1,
            TabInactiveTextColor = ColorSerialization.FromRgb(0xFFFFFF),
            TabMouseOverTextColor = ColorSerialization.FromRgb(0xFFFFFF),
            TabActiveTextColor = ColorSerialization.FromRgb(0xFFFFFF),
            TabFlashTextColor = ColorSerialization.FromRgb(0xFFFFFF),
            TabInactiveTabColor = ColorSerialization.FromRgb(0x0D0D0D),
            TabMouseOverTabColor = ColorSerialization.FromRgb(0x1E1E1E),
            TabActiveTabColor = ColorSerialization.FromRgb(0x2D2D2D),
            TabFlashTabColor = ColorSerialization.FromRgb(0x772222),
            TabInactiveBorderColor = ColorSerialization.FromRgb(0x333333),
            TabMouseOverBorderColor = ColorSerialization.FromRgb(0x333333),
            TabActiveBorderColor = ColorSerialization.FromRgb(0x333333),
            TabFlashBorderColor = ColorSerialization.FromRgb(0x333333)
        };

        public TabAppearanceInfo DarkModeBlue => new TabAppearanceInfo
        {
            TabHeight = -1,
            TabMaxWidth = -1,
            TabPinnedTabWidth = -1,
            TabPinnedTabWidthIcon = false,
            TabOverlap = -1,
            TabHeightOffset = -1,
            TabIndentFlipped = -1,
            TabIndentNormal = -1,
            TabInactiveTextColor = ColorSerialization.FromRgb(0xE0E0E0),
            TabMouseOverTextColor = ColorSerialization.FromRgb(0xE0E0E0),
            TabActiveTextColor = ColorSerialization.FromRgb(0xE0E0E0),
            TabFlashTextColor = ColorSerialization.FromRgb(0xE0E0E0),
            TabInactiveTabColor = ColorSerialization.FromRgb(0x111827),
            TabMouseOverTabColor = ColorSerialization.FromRgb(0x4B5970),
            TabActiveTabColor = ColorSerialization.FromRgb(0x273548),
            TabFlashTabColor = ColorSerialization.FromRgb(0x991B1B),
            TabInactiveBorderColor = ColorSerialization.FromRgb(0x374151),
            TabMouseOverBorderColor = ColorSerialization.FromRgb(0x374151),
            TabActiveBorderColor = ColorSerialization.FromRgb(0x374151),
            TabFlashBorderColor = ColorSerialization.FromRgb(0x374151)
        };

        public TabAppearanceInfo LightMono => new TabAppearanceInfo
        {
            TabHeight = -1,
            TabMaxWidth = -1,
            TabPinnedTabWidth = -1,
            TabPinnedTabWidthIcon = false,
            TabOverlap = -1,
            TabHeightOffset = -1,
            TabIndentFlipped = -1,
            TabIndentNormal = -1,
            TabInactiveTextColor = ColorSerialization.FromRgb(0x000000),
            TabMouseOverTextColor = ColorSerialization.FromRgb(0x000000),
            TabActiveTextColor = ColorSerialization.FromRgb(0x000000),
            TabFlashTextColor = ColorSerialization.FromRgb(0x000000),
            TabInactiveTabColor = ColorSerialization.FromRgb(0xA0A0A0),
            TabMouseOverTabColor = ColorSerialization.FromRgb(0xD0D0D0),
            TabActiveTabColor = ColorSerialization.FromRgb(0xFFFFFF),
            TabFlashTabColor = ColorSerialization.FromRgb(0xD4D4D4),
            TabInactiveBorderColor = ColorSerialization.FromRgb(0x252525),
            TabMouseOverBorderColor = ColorSerialization.FromRgb(0x252525),
            TabActiveBorderColor = ColorSerialization.FromRgb(0x252525),
            TabFlashBorderColor = ColorSerialization.FromRgb(0x252525)
        };

        public TabAppearanceInfo DarkMono => new TabAppearanceInfo
        {
            TabHeight = -1,
            TabMaxWidth = -1,
            TabPinnedTabWidth = -1,
            TabPinnedTabWidthIcon = false,
            TabOverlap = -1,
            TabHeightOffset = -1,
            TabIndentFlipped = -1,
            TabIndentNormal = -1,
            TabInactiveTextColor = ColorSerialization.FromRgb(0xFFFFFF),
            TabMouseOverTextColor = ColorSerialization.FromRgb(0x111111),
            TabActiveTextColor = ColorSerialization.FromRgb(0xFFFFFF),
            TabFlashTextColor = ColorSerialization.FromRgb(0xFFFFFF),
            TabInactiveTabColor = ColorSerialization.FromRgb(0x0D0D0D),
            TabMouseOverTabColor = ColorSerialization.FromRgb(0xDDDDDD),
            TabActiveTabColor = ColorSerialization.FromRgb(0x616161),
            TabFlashTabColor = ColorSerialization.FromRgb(0x808080),
            TabInactiveBorderColor = ColorSerialization.FromRgb(0x787878),
            TabMouseOverBorderColor = ColorSerialization.FromRgb(0xF2F2F2),
            TabActiveBorderColor = ColorSerialization.FromRgb(0x6B6B6B),
            TabFlashBorderColor = ColorSerialization.FromRgb(0x787878)
        };

        public TabAppearanceInfo DarkRedFrame => new TabAppearanceInfo
        {
            TabHeight = -1,
            TabMaxWidth = -1,
            TabPinnedTabWidth = -1,
            TabPinnedTabWidthIcon = false,
            TabOverlap = -1,
            TabHeightOffset = -1,
            TabIndentFlipped = -1,
            TabIndentNormal = -1,
            TabInactiveTextColor = ColorSerialization.FromRgb(0xFFFFFF),
            TabMouseOverTextColor = ColorSerialization.FromRgb(0x111111),
            TabActiveTextColor = ColorSerialization.FromRgb(0xB13A3A),
            TabFlashTextColor = ColorSerialization.FromRgb(0xFFFFFF),
            TabInactiveTabColor = ColorSerialization.FromRgb(0x0D0D0D),
            TabMouseOverTabColor = ColorSerialization.FromRgb(0xB13A3A),
            TabActiveTabColor = ColorSerialization.FromRgb(0x250A0B),
            TabFlashTabColor = ColorSerialization.FromRgb(0x808080),
            TabInactiveBorderColor = ColorSerialization.FromRgb(0xB13A3A),
            TabMouseOverBorderColor = ColorSerialization.FromRgb(0xFF6666),
            TabActiveBorderColor = ColorSerialization.FromRgb(0xCC4444),
            TabFlashBorderColor = ColorSerialization.FromRgb(0xB13A3A)
        };

        public string[] GetPresetNames()
        {
            return new[]
            {
                "Default",
                "Dark Mode",
                "Dark Mode Blue",
                "Light Mono",
                "Dark Mono",
                "Dark Red Frame"
            };
        }

        public bool TryGetPreset(string name, out TabAppearanceInfo preset)
        {
            switch (name)
            {
                case "Default":
                    preset = Default;
                    return true;
                case "Dark Mode":
                    preset = DarkMode;
                    return true;
                case "Dark Mode Blue":
                    preset = DarkModeBlue;
                    return true;
                case "Light Mono":
                    preset = LightMono;
                    return true;
                case "Dark Mono":
                    preset = DarkMono;
                    return true;
                case "Dark Red Frame":
                    preset = DarkRedFrame;
                    return true;
                default:
                    preset = null;
                    return false;
            }
        }
    }
}
