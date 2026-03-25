using System;
using System.Drawing;
using System.Windows.Forms;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripVisualService
    {
        private readonly WindowPresentationStateStore windowPresentationStateStore;
        private readonly GroupVisualOrderService groupVisualOrderService;
        private readonly IDesktopRuntime desktopRuntime;

        public ManagedGroupStripVisualService(
            WindowPresentationStateStore windowPresentationStateStore,
            GroupVisualOrderService groupVisualOrderService,
            IDesktopRuntime desktopRuntime)
        {
            this.windowPresentationStateStore = windowPresentationStateStore ?? throw new ArgumentNullException(nameof(windowPresentationStateStore));
            this.groupVisualOrderService = groupVisualOrderService ?? throw new ArgumentNullException(nameof(groupVisualOrderService));
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
        }

        public ManagedGroupStripTabVisualInfo CreateTabVisual(
            IntPtr windowHandle,
            string fallbackText,
            GroupSnapshot group,
            TabAppearanceInfo appearance,
            Font font,
            bool isActive,
            bool isHovered,
            bool isDropTarget)
        {
            var isPinned = windowPresentationStateStore.IsPinned(windowHandle);
            var text = ResolveTabText(windowHandle, fallbackText, isPinned);
            var fillColor = ResolveTabFillColor(windowHandle, appearance, isActive, isHovered);
            var borderColor = ResolveTabBorderColor(windowHandle, appearance, isActive, isHovered, isDropTarget);
            var textColor = ResolveTabTextColor(windowHandle, appearance, fillColor, isActive, isHovered);
            var maxWidth = Math.Max(90, appearance.TabMaxWidth);
            var textWidth = TextRenderer.MeasureText(text, font).Width;
            var resolvedWidth = isPinned
                ? Math.Min(maxWidth, Math.Max(90, appearance.TabPinnedTabWidth))
                : Math.Max(90, Math.Min(maxWidth, textWidth + 24));

            return new ManagedGroupStripTabVisualInfo
            {
                Text = text,
                FillColor = fillColor,
                BorderColor = borderColor,
                TextColor = textColor,
                TextAlign = ResolveTextAlign(windowHandle, group),
                Size = new Size(
                    resolvedWidth,
                    Math.Max(22, appearance.TabHeight))
            };
        }

        private string ResolveTabText(IntPtr windowHandle, string fallbackText, bool isPinned)
        {
            var baseText = string.IsNullOrWhiteSpace(fallbackText) ? "[untitled]" : fallbackText;
            if (windowPresentationStateStore.TryGetWindowNameOverride(windowHandle, out var nameOverride))
            {
                baseText = nameOverride;
            }

            return isPinned
                ? "* " + baseText
                : baseText;
        }

        private TabAlign ResolveAlignment(IntPtr windowHandle, GroupSnapshot group)
        {
            var runtimeGroup = desktopRuntime.FindGroup(group.GroupHandle);
            if (runtimeGroup != null)
            {
                return groupVisualOrderService.ResolveAlignment(windowHandle, runtimeGroup);
            }

            if (windowPresentationStateStore.TryGetAlignment(windowHandle, out var alignment))
            {
                return alignment;
            }

            return string.Equals(group.TabPosition, "TopLeft", StringComparison.OrdinalIgnoreCase)
                ? TabAlign.TopLeft
                : TabAlign.TopRight;
        }

        private ContentAlignment ResolveTextAlign(IntPtr windowHandle, GroupSnapshot group)
        {
            return ResolveAlignment(windowHandle, group) == TabAlign.TopRight
                ? ContentAlignment.MiddleRight
                : ContentAlignment.MiddleLeft;
        }

        private Color ResolveTabFillColor(IntPtr windowHandle, TabAppearanceInfo appearance, bool isActive, bool isHovered)
        {
            if (windowPresentationStateStore.TryGetFillColor(windowHandle, out var fillColor))
            {
                return fillColor;
            }

            if (isHovered)
            {
                return Blend(fillColor: appearance.TabMouseOverTabColor, mixColor: Color.White, amount: 0.20f);
            }

            return isActive
                ? Blend(appearance.TabActiveTabColor, Color.White, 0.10f)
                : Blend(appearance.TabInactiveTabColor, Color.White, 0.22f);
        }

        private Color ResolveTabBorderColor(IntPtr windowHandle, TabAppearanceInfo appearance, bool isActive, bool isHovered, bool isDropTarget)
        {
            if (isDropTarget)
            {
                return ColorSerialization.FromRgb(0xD28A18);
            }

            if (windowPresentationStateStore.TryGetBorderColor(windowHandle, out var borderColor))
            {
                return borderColor;
            }

            if (isHovered)
            {
                return Blend(appearance.TabMouseOverBorderColor, Color.Black, 0.12f);
            }

            return isActive
                ? Blend(appearance.TabActiveBorderColor, Color.Black, 0.08f)
                : Blend(appearance.TabInactiveBorderColor, Color.White, 0.22f);
        }

        private Color ResolveTabTextColor(IntPtr windowHandle, TabAppearanceInfo appearance, Color fillColor, bool isActive, bool isHovered)
        {
            if (windowPresentationStateStore.TryGetFillColor(windowHandle, out _))
            {
                return GetContrastColor(fillColor);
            }

            if (isHovered)
            {
                return appearance.TabMouseOverTextColor;
            }

            return isActive ? appearance.TabActiveTextColor : appearance.TabInactiveTextColor;
        }

        private static Color GetContrastColor(Color background)
        {
            var brightness = (background.R * 299) + (background.G * 587) + (background.B * 114);
            return brightness >= 140000 ? Color.Black : Color.White;
        }

        private static Color Blend(Color fillColor, Color mixColor, float amount)
        {
            amount = Math.Max(0f, Math.Min(1f, amount));
            return Color.FromArgb(
                255,
                (int)(fillColor.R + ((mixColor.R - fillColor.R) * amount)),
                (int)(fillColor.G + ((mixColor.G - fillColor.G) * amount)),
                (int)(fillColor.B + ((mixColor.B - fillColor.B) * amount)));
        }
    }
}
