using System.Collections.Generic;

namespace WindowTabs.CSharp.Models
{
    internal sealed class SettingsSnapshot
    {
        public string LicenseKey { get; set; } = string.Empty;
        public string Ticket { get; set; }
        public List<string> IncludedPaths { get; set; } = new List<string>();
        public List<string> ExcludedPaths { get; set; } = new List<string>();
        public List<string> AutoGroupingPaths { get; set; } = new List<string>();
        public string Version { get; set; } = string.Empty;
        public TabAppearanceInfo TabAppearance { get; set; }
        public bool RunAtStartup { get; set; }
        public bool HideInactiveTabs { get; set; }
        public bool EnableTabbingByDefault { get; set; }
        public bool EnableCtrlNumberHotKey { get; set; }
        public bool EnableHoverActivate { get; set; }
        public string TabPositionByDefault { get; set; } = "TopRight";
        public string HideTabsWhenDownByDefault { get; set; } = "never";
        public int HideTabsDelayMilliseconds { get; set; } = 3000;
        public bool HideTabsOnFullscreen { get; set; } = true;
        public bool SnapTabHeightMargin { get; set; }
        public string LanguageName { get; set; } = "English";
    }
}
