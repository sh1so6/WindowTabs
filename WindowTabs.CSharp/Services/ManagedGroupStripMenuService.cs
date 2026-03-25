using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripMenuService
    {
        private readonly ManagedGroupStripWindowSetService windowSetService;
        private readonly WindowPresentationStateStore windowPresentationStateStore;
        private readonly PresentationMutationService presentationMutationService;
        private readonly PresentationDialogService presentationDialogService;
        private readonly GroupMutationService groupMutationService;
        private readonly WindowCloseService windowCloseService;
        private readonly IDesktopRuntime desktopRuntime;

        public ManagedGroupStripMenuService(
            ManagedGroupStripWindowSetService windowSetService,
            WindowPresentationStateStore windowPresentationStateStore,
            PresentationMutationService presentationMutationService,
            PresentationDialogService presentationDialogService,
            GroupMutationService groupMutationService,
            WindowCloseService windowCloseService,
            IDesktopRuntime desktopRuntime)
        {
            this.windowSetService = windowSetService ?? throw new ArgumentNullException(nameof(windowSetService));
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
            var context = BuildWindowMenuContext(windowHandle);
            AddItems(menu,
                CreatePinItem(windowHandle, context.IsPinned),
                CreateAlignItem(windowHandle, "Align Left", TabAlign.TopLeft, context.Alignment),
                CreateAlignItem(windowHandle, "Align Right", TabAlign.TopRight, context.Alignment));
            AddSeparator(menu);
            AddItems(menu,
                CreateActionItem("Detach Tab", (_, __) => groupMutationService.UngroupWindow(windowHandle)),
                CreateMoveToNewGroupItem("Split Left Tabs To New Group", context.LeftSplitWindowHandles, context.OrderedWindowHandles),
                CreateMoveToNewGroupItem("Split Right Tabs To New Group", context.RightSplitWindowHandles, context.OrderedWindowHandles));
            AddSeparator(menu);
            AddItems(menu,
                CreateActionItem("Close Tab", (_, __) => windowCloseService.CloseWindow(windowHandle)),
                CreateCloseWindowsItem("Close Left Tabs", context.LeftWindowHandles),
                CreateCloseWindowsItem("Close Right Tabs", context.RightWindowHandles),
                CreateCloseWindowsItem("Close Other Tabs", context.OtherWindowHandles),
                CreateCloseAllItem(context.OrderedWindowHandles));
            AddSeparator(menu);
            AddItems(menu,
                CreateRenameItem(owner, windowHandle),
                CreateActionItem("Clear Custom Name", (_, __) => presentationMutationService.ClearWindowName(windowHandle)));
            AddSeparator(menu);
            AddItems(menu,
                CreateColorItem(owner, windowHandle, "Fill Color...", windowPresentationStateStore.TryGetFillColor, presentationMutationService.SetFillColor),
                CreateColorItem(owner, windowHandle, "Underline Color...", windowPresentationStateStore.TryGetUnderlineColor, presentationMutationService.SetUnderlineColor),
                CreateColorItem(owner, windowHandle, "Border Color...", windowPresentationStateStore.TryGetBorderColor, presentationMutationService.SetBorderColor),
                CreateActionItem("Clear Custom Colors", (_, __) => presentationMutationService.ClearCustomColors(windowHandle)));
            ShowMenu(menu, control, clientPoint);
        }

        public void ShowGroupMenu(Control control, Point clientPoint, IntPtr groupHandle, IReadOnlyCollection<IntPtr> windowHandles)
        {
            var group = desktopRuntime.FindGroup(groupHandle);
            if (group == null)
            {
                return;
            }

            var menu = new ContextMenuStrip();
            AddItems(menu,
                CreateGroupPositionItem(group.GroupHandle, "Group Position Left", "TopLeft", string.Equals(group.TabPosition, "TopLeft", StringComparison.OrdinalIgnoreCase)),
                CreateGroupPositionItem(group.GroupHandle, "Group Position Right", "TopRight", !string.Equals(group.TabPosition, "TopLeft", StringComparison.OrdinalIgnoreCase)),
                CreateGroupSnapMarginItem(group));
            AddSeparator(menu);
            AddItems(menu,
                CreateActionItem("Align All Left", (_, __) => presentationMutationService.SetAlignment(windowHandles, TabAlign.TopLeft)),
                CreateActionItem("Align All Right", (_, __) => presentationMutationService.SetAlignment(windowHandles, TabAlign.TopRight)));
            AddSeparator(menu);
            AddItems(menu,
                CreateActionItem("Pin All Tabs", (_, __) => presentationMutationService.SetPinned(windowHandles, true)),
                CreateActionItem("Unpin All Tabs", (_, __) => presentationMutationService.SetPinned(windowHandles, false)));
            AddSeparator(menu);
            AddItems(menu,
                CreateActionItem("Ungroup All Tabs", (_, __) => groupMutationService.UngroupWindows(windowHandles)),
                CreateActionItem("Close Group", (_, __) => windowCloseService.CloseWindows(windowHandles)));
            ShowMenu(menu, control, clientPoint);
        }

        private static Color? GetCurrentColor(IntPtr windowHandle, TryGetColor tryGetColor)
        {
            return tryGetColor(windowHandle, out var existingColor)
                ? existingColor
                : (Color?)null;
        }

        private WindowMenuContext BuildWindowMenuContext(IntPtr windowHandle)
        {
            var alignment = windowPresentationStateStore.TryGetAlignment(windowHandle, out var storedAlignment)
                ? storedAlignment
                : TabAlign.TopRight;
            var hasWindowContext = windowSetService.TryGetWindowContext(windowHandle, out _, out var orderedWindowHandles, out var currentIndex);
            return new WindowMenuContext(
                windowPresentationStateStore.IsPinned(windowHandle),
                alignment,
                hasWindowContext ? orderedWindowHandles : Array.Empty<IntPtr>(),
                hasWindowContext ? windowSetService.GetLeftWindowHandles(orderedWindowHandles, currentIndex) : Array.Empty<IntPtr>(),
                hasWindowContext ? windowSetService.GetRightWindowHandles(orderedWindowHandles, currentIndex) : Array.Empty<IntPtr>(),
                hasWindowContext ? windowSetService.GetOtherWindowHandles(orderedWindowHandles, windowHandle) : Array.Empty<IntPtr>(),
                hasWindowContext ? windowSetService.GetLeftSplitWindowHandles(orderedWindowHandles, currentIndex) : Array.Empty<IntPtr>(),
                hasWindowContext ? windowSetService.GetRightSplitWindowHandles(orderedWindowHandles, currentIndex) : Array.Empty<IntPtr>());
        }

        private ToolStripMenuItem CreatePinItem(IntPtr windowHandle, bool isPinned)
        {
            return CreateActionItem(
                isPinned ? "Unpin Tab" : "Pin Tab",
                (_, __) => presentationMutationService.SetPinned(windowHandle, !isPinned));
        }

        private ToolStripMenuItem CreateAlignItem(IntPtr windowHandle, string text, TabAlign alignment, TabAlign currentAlignment)
        {
            var item = CreateActionItem(text, (_, __) => presentationMutationService.SetAlignment(windowHandle, alignment));
            item.Checked = currentAlignment == alignment;
            return item;
        }

        private ToolStripMenuItem CreateMoveToNewGroupItem(string text, IReadOnlyCollection<IntPtr> handles, IReadOnlyCollection<IntPtr> orderedWindowHandles)
        {
            var item = CreateActionItem(text, (_, __) => groupMutationService.MoveWindowsToNewGroup(handles));
            item.Enabled = handles.Count > 0 && handles.Count < orderedWindowHandles.Count;
            return item;
        }

        private ToolStripMenuItem CreateCloseWindowsItem(string text, IReadOnlyCollection<IntPtr> handles)
        {
            var item = CreateActionItem(text, (_, __) => windowCloseService.CloseWindows(handles));
            item.Enabled = handles.Count > 0;
            return item;
        }

        private ToolStripMenuItem CreateCloseAllItem(IReadOnlyCollection<IntPtr> handles)
        {
            var item = CreateActionItem("Close All Tabs", (_, __) => windowCloseService.CloseWindows(handles));
            item.Enabled = handles.Count > 1;
            return item;
        }

        private ToolStripMenuItem CreateRenameItem(IWin32Window owner, IntPtr windowHandle)
        {
            return CreateActionItem("Rename Tab...", (_, __) =>
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
            });
        }

        private ToolStripMenuItem CreateColorItem(
            IWin32Window owner,
            IntPtr windowHandle,
            string text,
            TryGetColor tryGetColor,
            Action<IntPtr, Color?> setColor)
        {
            return CreateActionItem(text, (_, __) =>
            {
                var color = presentationDialogService.ShowColorPicker(owner, GetCurrentColor(windowHandle, tryGetColor));
                if (color.HasValue)
                {
                    setColor(windowHandle, color);
                }
            });
        }

        private ToolStripMenuItem CreateGroupPositionItem(IntPtr groupHandle, string text, string position, bool isChecked)
        {
            var item = CreateActionItem(text, (_, __) => presentationMutationService.SetGroupTabPosition(groupHandle, position));
            item.Checked = isChecked;
            return item;
        }

        private ToolStripMenuItem CreateGroupSnapMarginItem(IWindowGroupRuntime group)
        {
            var item = CreateActionItem(
                "Snap Tab Height Margin",
                (_, __) => presentationMutationService.SetGroupSnapMargin(group.GroupHandle, !group.SnapTabHeightMargin));
            item.Checked = group.SnapTabHeightMargin;
            return item;
        }

        private static ToolStripMenuItem CreateActionItem(string text, EventHandler onClick)
        {
            var item = new ToolStripMenuItem(text);
            item.Click += onClick;
            return item;
        }

        private static void AddItems(ContextMenuStrip menu, params ToolStripItem[] items)
        {
            foreach (var item in items)
            {
                menu.Items.Add(item);
            }
        }

        private static void AddSeparator(ContextMenuStrip menu)
        {
            menu.Items.Add(new ToolStripSeparator());
        }

        private static void ShowMenu(ContextMenuStrip menu, Control control, Point clientPoint)
        {
            menu.Closed += (_, __) => menu.Dispose();
            menu.Show(control, clientPoint);
        }

        private delegate bool TryGetColor(IntPtr hwnd, out Color color);

        private sealed class WindowMenuContext
        {
            public WindowMenuContext(
                bool isPinned,
                TabAlign alignment,
                IReadOnlyCollection<IntPtr> orderedWindowHandles,
                IReadOnlyCollection<IntPtr> leftWindowHandles,
                IReadOnlyCollection<IntPtr> rightWindowHandles,
                IReadOnlyCollection<IntPtr> otherWindowHandles,
                IReadOnlyCollection<IntPtr> leftSplitWindowHandles,
                IReadOnlyCollection<IntPtr> rightSplitWindowHandles)
            {
                IsPinned = isPinned;
                Alignment = alignment;
                OrderedWindowHandles = orderedWindowHandles ?? Array.Empty<IntPtr>();
                LeftWindowHandles = leftWindowHandles ?? Array.Empty<IntPtr>();
                RightWindowHandles = rightWindowHandles ?? Array.Empty<IntPtr>();
                OtherWindowHandles = otherWindowHandles ?? Array.Empty<IntPtr>();
                LeftSplitWindowHandles = leftSplitWindowHandles ?? Array.Empty<IntPtr>();
                RightSplitWindowHandles = rightSplitWindowHandles ?? Array.Empty<IntPtr>();
            }

            public bool IsPinned { get; }

            public TabAlign Alignment { get; }

            public IReadOnlyCollection<IntPtr> OrderedWindowHandles { get; }

            public IReadOnlyCollection<IntPtr> LeftWindowHandles { get; }

            public IReadOnlyCollection<IntPtr> RightWindowHandles { get; }

            public IReadOnlyCollection<IntPtr> OtherWindowHandles { get; }

            public IReadOnlyCollection<IntPtr> LeftSplitWindowHandles { get; }

            public IReadOnlyCollection<IntPtr> RightSplitWindowHandles { get; }
        }
    }
}
