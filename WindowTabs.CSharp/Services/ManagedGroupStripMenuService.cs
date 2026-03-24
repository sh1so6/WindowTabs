using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripMenuService
    {
        private readonly WindowPresentationStateStore windowPresentationStateStore;
        private readonly PresentationMutationService presentationMutationService;
        private readonly PresentationDialogService presentationDialogService;
        private readonly GroupMutationService groupMutationService;
        private readonly WindowCloseService windowCloseService;
        private readonly IDesktopRuntime desktopRuntime;

        public ManagedGroupStripMenuService(
            WindowPresentationStateStore windowPresentationStateStore,
            PresentationMutationService presentationMutationService,
            PresentationDialogService presentationDialogService,
            GroupMutationService groupMutationService,
            WindowCloseService windowCloseService,
            IDesktopRuntime desktopRuntime)
        {
            this.windowPresentationStateStore = windowPresentationStateStore ?? throw new ArgumentNullException(nameof(windowPresentationStateStore));
            this.presentationMutationService = presentationMutationService ?? throw new ArgumentNullException(nameof(presentationMutationService));
            this.presentationDialogService = presentationDialogService ?? throw new ArgumentNullException(nameof(presentationDialogService));
            this.groupMutationService = groupMutationService ?? throw new ArgumentNullException(nameof(groupMutationService));
            this.windowCloseService = windowCloseService ?? throw new ArgumentNullException(nameof(windowCloseService));
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
        }

        public void ShowWindowMenu(IWin32Window owner, Control control, Point clientPoint, IntPtr windowHandle)
        {
            var menu = new ContextMenuStrip();
            var isPinned = windowPresentationStateStore.IsPinned(windowHandle);
            var alignment = windowPresentationStateStore.TryGetAlignment(windowHandle, out var storedAlignment)
                ? storedAlignment
                : TabAlign.TopRight;

            var pinItem = new ToolStripMenuItem(isPinned ? "Unpin Tab" : "Pin Tab");
            pinItem.Click += (_, __) => presentationMutationService.SetPinned(windowHandle, !isPinned);

            var alignLeftItem = new ToolStripMenuItem("Align Left")
            {
                Checked = alignment == TabAlign.TopLeft
            };
            alignLeftItem.Click += (_, __) => presentationMutationService.SetAlignment(windowHandle, TabAlign.TopLeft);

            var alignRightItem = new ToolStripMenuItem("Align Right")
            {
                Checked = alignment == TabAlign.TopRight
            };
            alignRightItem.Click += (_, __) => presentationMutationService.SetAlignment(windowHandle, TabAlign.TopRight);

            var fillColorItem = new ToolStripMenuItem("Fill Color...");
            fillColorItem.Click += (_, __) =>
            {
                var color = presentationDialogService.ShowColorPicker(owner, GetCurrentColor(windowHandle, windowPresentationStateStore.TryGetFillColor));
                if (color.HasValue)
                {
                    presentationMutationService.SetFillColor(windowHandle, color);
                }
            };

            var underlineColorItem = new ToolStripMenuItem("Underline Color...");
            underlineColorItem.Click += (_, __) =>
            {
                var color = presentationDialogService.ShowColorPicker(owner, GetCurrentColor(windowHandle, windowPresentationStateStore.TryGetUnderlineColor));
                if (color.HasValue)
                {
                    presentationMutationService.SetUnderlineColor(windowHandle, color);
                }
            };

            var borderColorItem = new ToolStripMenuItem("Border Color...");
            borderColorItem.Click += (_, __) =>
            {
                var color = presentationDialogService.ShowColorPicker(owner, GetCurrentColor(windowHandle, windowPresentationStateStore.TryGetBorderColor));
                if (color.HasValue)
                {
                    presentationMutationService.SetBorderColor(windowHandle, color);
                }
            };

            var renameItem = new ToolStripMenuItem("Rename Tab...");
            renameItem.Click += (_, __) =>
            {
                var currentName = windowPresentationStateStore.TryGetWindowNameOverride(windowHandle, out var nameOverride)
                    ? nameOverride
                    : string.Empty;
                var renamed = presentationDialogService.ShowRenamePrompt(owner, "Rename Tab", currentName);
                if (renamed == null)
                {
                    return;
                }

                presentationMutationService.SetWindowName(windowHandle, renamed);
            };

            var closeItem = new ToolStripMenuItem("Close Tab");
            closeItem.Click += (_, __) => windowCloseService.CloseWindow(windowHandle);

            var ungroupItem = new ToolStripMenuItem("Ungroup Tab");
            ungroupItem.Click += (_, __) => groupMutationService.UngroupWindow(windowHandle);

            var clearRenameItem = new ToolStripMenuItem("Clear Custom Name");
            clearRenameItem.Click += (_, __) => presentationMutationService.ClearWindowName(windowHandle);

            var clearColorsItem = new ToolStripMenuItem("Clear Custom Colors");
            clearColorsItem.Click += (_, __) => presentationMutationService.ClearCustomColors(windowHandle);

            menu.Items.Add(pinItem);
            menu.Items.Add(alignLeftItem);
            menu.Items.Add(alignRightItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(closeItem);
            menu.Items.Add(ungroupItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(renameItem);
            menu.Items.Add(clearRenameItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(fillColorItem);
            menu.Items.Add(underlineColorItem);
            menu.Items.Add(borderColorItem);
            menu.Items.Add(clearColorsItem);
            menu.Closed += (_, __) => menu.Dispose();
            menu.Show(control, clientPoint);
        }

        public void ShowGroupMenu(Control control, Point clientPoint, IntPtr groupHandle, IReadOnlyCollection<IntPtr> windowHandles)
        {
            var group = desktopRuntime.FindGroup(groupHandle);
            if (group == null)
            {
                return;
            }

            var menu = new ContextMenuStrip();
            var topLeftItem = new ToolStripMenuItem("Group Position Left")
            {
                Checked = string.Equals(group.TabPosition, "TopLeft", StringComparison.OrdinalIgnoreCase)
            };
            topLeftItem.Click += (_, __) => presentationMutationService.SetGroupTabPosition(group.GroupHandle, "TopLeft");

            var topRightItem = new ToolStripMenuItem("Group Position Right")
            {
                Checked = !string.Equals(group.TabPosition, "TopLeft", StringComparison.OrdinalIgnoreCase)
            };
            topRightItem.Click += (_, __) => presentationMutationService.SetGroupTabPosition(group.GroupHandle, "TopRight");

            var snapMarginItem = new ToolStripMenuItem("Snap Tab Height Margin")
            {
                Checked = group.SnapTabHeightMargin
            };
            snapMarginItem.Click += (_, __) => presentationMutationService.SetGroupSnapMargin(group.GroupHandle, !group.SnapTabHeightMargin);

            var alignAllLeftItem = new ToolStripMenuItem("Align All Left");
            alignAllLeftItem.Click += (_, __) => presentationMutationService.SetAlignment(windowHandles, TabAlign.TopLeft);

            var alignAllRightItem = new ToolStripMenuItem("Align All Right");
            alignAllRightItem.Click += (_, __) => presentationMutationService.SetAlignment(windowHandles, TabAlign.TopRight);

            var pinAllItem = new ToolStripMenuItem("Pin All Tabs");
            pinAllItem.Click += (_, __) => presentationMutationService.SetPinned(windowHandles, true);

            var unpinAllItem = new ToolStripMenuItem("Unpin All Tabs");
            unpinAllItem.Click += (_, __) => presentationMutationService.SetPinned(windowHandles, false);

            var ungroupAllItem = new ToolStripMenuItem("Ungroup All Tabs");
            ungroupAllItem.Click += (_, __) => groupMutationService.UngroupWindows(windowHandles);

            var closeGroupItem = new ToolStripMenuItem("Close Group");
            closeGroupItem.Click += (_, __) => windowCloseService.CloseWindows(windowHandles);

            menu.Items.Add(topLeftItem);
            menu.Items.Add(topRightItem);
            menu.Items.Add(snapMarginItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(alignAllLeftItem);
            menu.Items.Add(alignAllRightItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(pinAllItem);
            menu.Items.Add(unpinAllItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(ungroupAllItem);
            menu.Items.Add(closeGroupItem);
            menu.Closed += (_, __) => menu.Dispose();
            menu.Show(control, clientPoint);
        }

        private static Color? GetCurrentColor(IntPtr windowHandle, TryGetColor tryGetColor)
        {
            return tryGetColor(windowHandle, out var existingColor)
                ? existingColor
                : (Color?)null;
        }

        private delegate bool TryGetColor(IntPtr hwnd, out Color color);
    }
}
