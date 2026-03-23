using System.Drawing;

namespace WindowTabs.CSharp.Models
{
    internal sealed class TabAppearanceInfo
    {
        public int TabHeight { get; set; }
        public int TabMaxWidth { get; set; }
        public int TabPinnedTabWidth { get; set; }
        public bool TabPinnedTabWidthIcon { get; set; }
        public int TabOverlap { get; set; }
        public int TabHeightOffset { get; set; }
        public int TabIndentFlipped { get; set; }
        public int TabIndentNormal { get; set; }
        public Color TabInactiveTextColor { get; set; }
        public Color TabMouseOverTextColor { get; set; }
        public Color TabActiveTextColor { get; set; }
        public Color TabFlashTextColor { get; set; }
        public Color TabInactiveTabColor { get; set; }
        public Color TabMouseOverTabColor { get; set; }
        public Color TabActiveTabColor { get; set; }
        public Color TabFlashTabColor { get; set; }
        public Color TabInactiveBorderColor { get; set; }
        public Color TabMouseOverBorderColor { get; set; }
        public Color TabActiveBorderColor { get; set; }
        public Color TabFlashBorderColor { get; set; }

        public TabAppearanceInfo Clone()
        {
            return (TabAppearanceInfo)MemberwiseClone();
        }
    }
}
