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
            var text = ResolveTabText(windowHandle, fallbackText);
            var fillColor = ResolveTabFillColor(windowHandle, appearance, isActive, isHovered);

            return new ManagedGroupStripTabVisualInfo
            {
                Text = text,
                FillColor = fillColor,
                BorderColor = ResolveTabBorderColor(windowHandle, appearance, isActive, isHovered, isDropTarget),
                TextColor = ResolveTabTextColor(windowHandle, appearance, fillColor, isActive, isHovered),
                TextAlign = ResolveTextAlign(windowHandle, group),
                Size = new Size(
                    Math.Max(90, Math.Min(appearance.TabMaxWidth, TextRenderer.MeasureText(text, font).Width + 24)),
                    Math.Max(22, appearance.TabHeight))
            };
        }

        private string ResolveTabText(IntPtr windowHandle, string fallbackText)
        {
            var baseText = string.IsNullOrWhiteSpace(fallbackText) ? "[untitled]" : fallbackText;
            if (windowPresentationStateStore.TryGetWindowNameOverride(windowHandle, out var nameOverride))
            {
                baseText = nameOverride;
            }

            return windowPresentationStateStore.IsPinned(windowHandle)
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
                return appearance.TabMouseOverTabColor;
            }

            return isActive ? appearance.TabActiveTabColor : appearance.TabInactiveTabColor;
        }

        private Color ResolveTabBorderColor(IntPtr windowHandle, TabAppearanceInfo appearance, bool isActive, bool isHovered, bool isDropTarget)
        {
            if (isDropTarget)
            {
                return Color.Gold;
            }

            if (windowPresentationStateStore.TryGetBorderColor(windowHandle, out var borderColor))
            {
                return borderColor;
            }

            if (isHovered)
            {
                return appearance.TabMouseOverBorderColor;
            }

            return isActive ? appearance.TabActiveBorderColor : appearance.TabInactiveBorderColor;
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
    }
}
