using System;
using System.Drawing;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripPaintService
    {
        private static readonly Color DropMarkerColor = ColorSerialization.FromRgb(0xCF8D27);
        private static readonly Color DropOutlineColor = ColorSerialization.FromRgb(0xE2B66F);
        private readonly WindowPresentationStateStore windowPresentationStateStore;

        public ManagedGroupStripPaintService(WindowPresentationStateStore windowPresentationStateStore)
        {
            this.windowPresentationStateStore = windowPresentationStateStore ?? throw new ArgumentNullException(nameof(windowPresentationStateStore));
        }

        public void PaintTabOverlay(Graphics graphics, IntPtr windowHandle, Size buttonSize, bool isDropTarget, bool insertAfterTarget)
        {
            if (graphics == null)
            {
                throw new ArgumentNullException(nameof(graphics));
            }

            if (isDropTarget)
            {
                using (var brush = new SolidBrush(DropMarkerColor))
                {
                    var markerX = insertAfterTarget ? buttonSize.Width - 2 : 1;
                    graphics.FillRectangle(brush, markerX, 2, 1, Math.Max(1, buttonSize.Height - 4));
                }
            }

            if (!windowPresentationStateStore.TryGetUnderlineColor(windowHandle, out var underlineColor))
            {
                if (isDropTarget)
                {
                    DrawDropTargetOutline(graphics, buttonSize);
                }

                return;
            }

            using (var brush = new SolidBrush(underlineColor))
            {
                graphics.FillRectangle(brush, 0, buttonSize.Height - 3, buttonSize.Width, 3);
            }

            if (isDropTarget)
            {
                DrawDropTargetOutline(graphics, buttonSize);
            }
        }

        private static void DrawDropTargetOutline(Graphics graphics, Size buttonSize)
        {
            using (var pen = new Pen(DropOutlineColor, 1))
            {
                graphics.DrawRectangle(pen, 1, 1, buttonSize.Width - 3, buttonSize.Height - 3);
            }
        }
    }
}
