using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;
using BemoSystemMenuCommandValues = Bemo.SystemMenuCommandValues;
using BemoWinUserApi = Bemo.WinUserApi;
using BemoWindowMessages = Bemo.WindowMessages;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripRegistry : IDisposable
    {
        private readonly DesktopMonitoringService desktopMonitoringService;
        private readonly DesktopSnapshotService desktopSnapshotService;
        private readonly SettingsSession settingsSession;
        private readonly WindowPresentationStateStore windowPresentationStateStore;
        private readonly Contracts.IDragDrop dragDrop;
        private readonly IDesktopRuntime desktopRuntime;
        private readonly IProgramRefresher refresher;
        private readonly Dictionary<IntPtr, ManagedGroupStripForm> forms = new Dictionary<IntPtr, ManagedGroupStripForm>();
        private bool initialized;
        private bool disposed;

        public ManagedGroupStripRegistry(
            DesktopMonitoringService desktopMonitoringService,
            DesktopSnapshotService desktopSnapshotService,
            SettingsSession settingsSession,
            WindowPresentationStateStore windowPresentationStateStore,
            Contracts.IDragDrop dragDrop,
            IDesktopRuntime desktopRuntime,
            IProgramRefresher refresher)
        {
            this.desktopMonitoringService = desktopMonitoringService ?? throw new ArgumentNullException(nameof(desktopMonitoringService));
            this.desktopSnapshotService = desktopSnapshotService ?? throw new ArgumentNullException(nameof(desktopSnapshotService));
            this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
            this.windowPresentationStateStore = windowPresentationStateStore ?? throw new ArgumentNullException(nameof(windowPresentationStateStore));
            this.dragDrop = dragDrop ?? throw new ArgumentNullException(nameof(dragDrop));
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
            this.refresher = refresher ?? throw new ArgumentNullException(nameof(refresher));
        }

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            desktopMonitoringService.StateChanged += OnStateChanged;
            settingsSession.Changed += OnSettingsChanged;
            SyncForms();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            desktopMonitoringService.StateChanged -= OnStateChanged;
            settingsSession.Changed -= OnSettingsChanged;
            foreach (var form in forms.Values.ToArray())
            {
                form.Dispose();
            }

            forms.Clear();
        }

        private void OnStateChanged(object sender, EventArgs e)
        {
            SyncForms();
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            SyncForms();
        }

        private void SyncForms()
        {
            if (!(desktopRuntime is ManagedDesktopRuntime)
                || desktopMonitoringService.CurrentState?.IsDisabled == true)
            {
                ClearForms();
                return;
            }

            var refreshResult = desktopMonitoringService.CurrentState?.RefreshResult ?? new DesktopRefreshResult();
            var windowsByHandle = refreshResult.Windows.ToDictionary(window => window.Handle, window => window);
            var activeGroupHandles = new HashSet<IntPtr>(refreshResult.Groups.Select(group => group.GroupHandle));

            foreach (var staleGroupHandle in forms.Keys.Where(handle => !activeGroupHandles.Contains(handle)).ToArray())
            {
                forms[staleGroupHandle].Dispose();
                forms.Remove(staleGroupHandle);
            }

            foreach (var group in refreshResult.Groups)
            {
                if (group.WindowHandles.Count == 0)
                {
                    continue;
                }

                if (!forms.TryGetValue(group.GroupHandle, out var form))
                {
                    form = new ManagedGroupStripForm(
                        desktopSnapshotService,
                        settingsSession,
                        windowPresentationStateStore,
                        dragDrop,
                        desktopRuntime,
                        refresher);
                    forms.Add(group.GroupHandle, form);
                }

                form.UpdateGroup(group, windowsByHandle);
            }
        }

        private void ClearForms()
        {
            foreach (var form in forms.Values.ToArray())
            {
                form.Dispose();
            }

            forms.Clear();
        }

        private sealed class ManagedGroupStripForm : Form
        {
            private readonly DesktopSnapshotService desktopSnapshotService;
            private readonly SettingsSession settingsSession;
            private readonly WindowPresentationStateStore windowPresentationStateStore;
            private readonly Contracts.IDragDrop dragDrop;
            private readonly IDesktopRuntime desktopRuntime;
            private readonly IProgramRefresher refresher;
            private readonly FlowLayoutPanel tabPanel;
            private readonly Dictionary<IntPtr, TabButtonState> buttonStates = new Dictionary<IntPtr, TabButtonState>();
            private readonly StripDropTarget stripDropTarget;
            private IntPtr currentGroupHandle;
            private List<IntPtr> currentGroupWindowHandles = new List<IntPtr>();

            public ManagedGroupStripForm(
                DesktopSnapshotService desktopSnapshotService,
                SettingsSession settingsSession,
                WindowPresentationStateStore windowPresentationStateStore,
                Contracts.IDragDrop dragDrop,
                IDesktopRuntime desktopRuntime,
                IProgramRefresher refresher)
            {
                this.desktopSnapshotService = desktopSnapshotService ?? throw new ArgumentNullException(nameof(desktopSnapshotService));
                this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
                this.windowPresentationStateStore = windowPresentationStateStore ?? throw new ArgumentNullException(nameof(windowPresentationStateStore));
                this.dragDrop = dragDrop ?? throw new ArgumentNullException(nameof(dragDrop));
                this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
                this.refresher = refresher ?? throw new ArgumentNullException(nameof(refresher));
                stripDropTarget = new StripDropTarget(this);

                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.Manual;
                TopMost = true;
                AutoSize = true;
                AutoSizeMode = AutoSizeMode.GrowAndShrink;
                BackColor = Color.Black;

                tabPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    WrapContents = false,
                    Margin = Padding.Empty,
                    Padding = new Padding(2),
                    BackColor = Color.Black
                };

                Controls.Add(tabPanel);
                HandleCreated += (_, __) => dragDrop.RegisterTarget(Handle, stripDropTarget);
                HandleDestroyed += (_, __) =>
                {
                    if (Handle != IntPtr.Zero)
                    {
                        dragDrop.UnregisterTarget(Handle);
                    }
                };
                tabPanel.MouseUp += OnPanelMouseUp;
            }

            protected override bool ShowWithoutActivation => true;

            public void UpdateGroup(GroupSnapshot group, IReadOnlyDictionary<IntPtr, WindowSnapshot> windowsByHandle)
            {
                var activeWindowHandle = desktopSnapshotService.GetForegroundWindowHandle();
                var settings = settingsSession.Current;
                var appearance = settingsSession.Current.TabAppearance ?? SettingsDefaults.CreateDefaultTabAppearance();
                var orderedHandles = OrderWindowHandles(group);
                currentGroupHandle = group.GroupHandle;
                currentGroupWindowHandles = group.WindowHandles.ToList();
                var anchorWindow = group.WindowHandles
                    .Select(hwnd => windowsByHandle.TryGetValue(hwnd, out var window) ? window : null)
                    .FirstOrDefault(window => window != null);

                if (anchorWindow == null || anchorWindow.Bounds.Width <= 0)
                {
                    Hide();
                    return;
                }

                if (anchorWindow.IsMinimized
                    || !anchorWindow.IsVisibleOnScreen
                    || !anchorWindow.IsOnCurrentVirtualDesktop)
                {
                    Hide();
                    return;
                }

                if (settings.HideInactiveTabs && !group.WindowHandles.Contains(activeWindowHandle))
                {
                    Hide();
                    return;
                }

                if (ShouldHideForFullscreen(anchorWindow, settings))
                {
                    Hide();
                    return;
                }

                foreach (var staleHandle in buttonStates.Keys.Where(hwnd => !orderedHandles.Contains(hwnd)).ToArray())
                {
                    var state = buttonStates[staleHandle];
                    tabPanel.Controls.Remove(state.Button);
                    state.Dispose();
                    buttonStates.Remove(staleHandle);
                }

                for (var index = 0; index < orderedHandles.Count; index++)
                {
                    var windowHandle = orderedHandles[index];
                    if (!buttonStates.TryGetValue(windowHandle, out var state))
                    {
                        var button = CreateTabButton();
                        state = new TabButtonState(button);
                        buttonStates.Add(windowHandle, state);
                        tabPanel.Controls.Add(button);
                        WireButton(button, state, windowHandle);
                    }

                    if (windowsByHandle.TryGetValue(windowHandle, out var window))
                    {
                        var isActive = activeWindowHandle == windowHandle;
                        var text = ResolveTabText(windowHandle, window.Text);
                        var fillColor = ResolveTabFillColor(windowHandle, appearance, isActive, state.IsHovered);
                        var borderColor = ResolveTabBorderColor(windowHandle, appearance, isActive, state.IsHovered);
                        var textColor = ResolveTabTextColor(windowHandle, appearance, fillColor, isActive, state.IsHovered);
                        var width = Math.Max(90, Math.Min(appearance.TabMaxWidth, TextRenderer.MeasureText(text, state.Button.Font).Width + 24));
                        var height = Math.Max(22, appearance.TabHeight);

                        state.Button.Text = text;
                        state.Button.BackColor = fillColor;
                        state.Button.ForeColor = textColor;
                        state.Button.FlatAppearance.BorderColor = borderColor;
                        state.Button.TextAlign = ResolveTextAlign(windowHandle, group);
                        state.Button.Size = new Size(width, height);
                        state.Button.Tag = windowHandle;
                        state.Button.Invalidate();
                    }

                    tabPanel.Controls.SetChildIndex(state.Button, index);
                }

                var stripSize = GetPreferredSize(Size.Empty);
                var offsetY = group.SnapTabHeightMargin
                    ? Math.Max(1, appearance.TabHeight - 1)
                    : Math.Max(22, appearance.TabHeight);
                var stripX = string.Equals(group.TabPosition, "TopLeft", StringComparison.OrdinalIgnoreCase)
                    ? anchorWindow.Bounds.X
                    : Math.Max(anchorWindow.Bounds.X, anchorWindow.Bounds.Right - stripSize.Width);
                var newLocation = new Point(stripX, Math.Max(0, anchorWindow.Bounds.Y - offsetY - 4));
                Location = newLocation;

                if (!Visible)
                {
                    Show();
                }

                Invalidate();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    foreach (var state in buttonStates.Values.ToArray())
                    {
                        state.Dispose();
                    }

                    buttonStates.Clear();
                }

                base.Dispose(disposing);
            }

            private void OnPanelMouseUp(object sender, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Right || currentGroupHandle == IntPtr.Zero)
                {
                    return;
                }

                ShowGroupContextMenu(tabPanel, e.Location);
            }

            private List<IntPtr> OrderWindowHandles(GroupSnapshot group)
            {
                var orderedHandles = new List<IntPtr>();
                var rightAlignedHandles = new List<IntPtr>();
                foreach (var hwnd in group.WindowHandles)
                {
                    if (ResolveAlignment(hwnd, group) == TabAlign.TopRight)
                    {
                        rightAlignedHandles.Add(hwnd);
                    }
                    else
                    {
                        orderedHandles.Add(hwnd);
                    }
                }

                orderedHandles.AddRange(rightAlignedHandles);
                return orderedHandles;
            }

            private void WireButton(Button button, TabButtonState state, IntPtr windowHandle)
            {
                button.Click += (_, __) =>
                {
                    if (windowHandle != IntPtr.Zero)
                    {
                        BemoWinUserApi.SetForegroundWindow(windowHandle);
                    }
                };

                button.MouseDown += (_, e) =>
                {
                    if (e.Button != MouseButtons.Left)
                    {
                        return;
                    }

                    state.MouseDownClientPoint = e.Location;
                    state.MouseDownScreenPoint = button.PointToScreen(e.Location);
                    state.IsMouseDown = true;
                    state.HasStartedDrag = false;
                };

                button.MouseMove += (_, e) =>
                {
                    if (!state.IsMouseDown || state.HasStartedDrag || (Control.MouseButtons & MouseButtons.Left) == 0)
                    {
                        return;
                    }

                    var screenPoint = button.PointToScreen(e.Location);
                    var deltaX = screenPoint.X - state.MouseDownScreenPoint.X;
                    var deltaY = screenPoint.Y - state.MouseDownScreenPoint.Y;
                    if ((deltaX * deltaX) + (deltaY * deltaY) < 25)
                    {
                        return;
                    }

                    state.HasStartedDrag = true;
                    dragDrop.BeginDrag(new DragBeginRequest
                    {
                        InitialWindowHandle = windowHandle,
                        InitialScreenPoint = screenPoint,
                        ImageOffset = state.MouseDownClientPoint,
                        CreateImage = () => CaptureButtonImage(button),
                        Data = new TabDragInfo
                        {
                            WindowHandle = windowHandle,
                            TabOffset = state.MouseDownClientPoint,
                            ImageOffset = state.MouseDownClientPoint
                        }
                    });
                };

                button.MouseUp += (_, __) =>
                {
                    state.IsMouseDown = false;
                    state.HasStartedDrag = false;
                };

                button.MouseDown += (_, e) =>
                {
                    if (e.Button != MouseButtons.Middle)
                    {
                        return;
                    }

                    desktopRuntime.RemoveWindow(windowHandle);
                    refresher.Refresh();
                };

                button.MouseEnter += (_, __) =>
                {
                    state.IsHovered = true;
                    button.Invalidate();
                    Invalidate();
                    if (settingsSession.Current.EnableHoverActivate)
                    {
                        state.StartHoverActivate(() => BemoWinUserApi.SetForegroundWindow(windowHandle));
                    }
                };

                button.MouseLeave += (_, __) =>
                {
                    state.IsHovered = false;
                    button.Invalidate();
                    Invalidate();
                    state.StopHoverActivate();
                };

                button.MouseUp += (_, e) =>
                {
                    if (e.Button != MouseButtons.Right)
                    {
                        return;
                    }

                    state.StopHoverActivate();
                    ShowContextMenu(button, windowHandle, e.Location);
                };

                button.Paint += (_, e) =>
                {
                    if (!windowPresentationStateStore.TryGetUnderlineColor(windowHandle, out var underlineColor))
                    {
                        return;
                    }

                    using (var brush = new SolidBrush(underlineColor))
                    {
                        e.Graphics.FillRectangle(brush, 0, button.Height - 3, button.Width, 3);
                    }
                };
            }

            private static Button CreateTabButton()
            {
                return new Button
                {
                    FlatStyle = FlatStyle.Flat,
                    AutoEllipsis = true,
                    Margin = new Padding(1),
                    Padding = new Padding(6, 2, 6, 2),
                    TextAlign = ContentAlignment.MiddleLeft
                };
            }

            private static Bitmap CaptureButtonImage(Control control)
            {
                var bitmap = new Bitmap(control.Width, control.Height);
                control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, control.Size));
                return bitmap;
            }

            private string ResolveTabText(IntPtr windowHandle, string fallbackText)
            {
                var baseText = string.IsNullOrWhiteSpace(fallbackText) ? "[untitled]" : fallbackText;
                if (windowPresentationStateStore.TryGetWindowNameOverride(windowHandle, out var nameOverride))
                {
                    baseText = nameOverride;
                }

                if (windowPresentationStateStore.IsPinned(windowHandle))
                {
                    return "* " + baseText;
                }

                return baseText;
            }

            private static bool ShouldHideForFullscreen(WindowSnapshot anchorWindow, SettingsSnapshot settings)
            {
                return settings.HideTabsOnFullscreen && IsFullscreen(anchorWindow);
            }

            private static bool IsFullscreen(WindowSnapshot window)
            {
                if (window == null || window.Handle == IntPtr.Zero || window.IsMinimized)
                {
                    return false;
                }

                var screenBounds = Screen.FromHandle(window.Handle).Bounds;
                return window.Bounds.X <= screenBounds.X
                    && window.Bounds.Y <= screenBounds.Y
                    && window.Bounds.Right >= screenBounds.Right
                    && window.Bounds.Bottom >= screenBounds.Bottom;
            }

            private TabAlign ResolveAlignment(IntPtr windowHandle, GroupSnapshot group)
            {
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

            private Color ResolveTabBorderColor(IntPtr windowHandle, TabAppearanceInfo appearance, bool isActive, bool isHovered)
            {
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

            private void ShowContextMenu(Control button, IntPtr windowHandle, Point clientPoint)
            {
                var menu = new ContextMenuStrip();
                var isPinned = windowPresentationStateStore.IsPinned(windowHandle);
                var alignment = windowPresentationStateStore.TryGetAlignment(windowHandle, out var storedAlignment)
                    ? storedAlignment
                    : TabAlign.TopRight;

                var pinItem = new ToolStripMenuItem(isPinned ? "Unpin Tab" : "Pin Tab");
                pinItem.Click += (_, __) =>
                {
                    windowPresentationStateStore.SetPinned(windowHandle, !isPinned);
                    refresher.Refresh();
                };

                var alignLeftItem = new ToolStripMenuItem("Align Left")
                {
                    Checked = alignment == TabAlign.TopLeft
                };
                alignLeftItem.Click += (_, __) =>
                {
                    windowPresentationStateStore.SetAlignment(windowHandle, TabAlign.TopLeft);
                    refresher.Refresh();
                };

                var alignRightItem = new ToolStripMenuItem("Align Right")
                {
                    Checked = alignment == TabAlign.TopRight
                };
                alignRightItem.Click += (_, __) =>
                {
                    windowPresentationStateStore.SetAlignment(windowHandle, TabAlign.TopRight);
                    refresher.Refresh();
                };

                var fillColorItem = new ToolStripMenuItem("Fill Color...");
                fillColorItem.Click += (_, __) =>
                {
                    ChooseColor(windowHandle, windowPresentationStateStore.TryGetFillColor, windowPresentationStateStore.SetFillColor);
                };

                var underlineColorItem = new ToolStripMenuItem("Underline Color...");
                underlineColorItem.Click += (_, __) =>
                {
                    ChooseColor(windowHandle, windowPresentationStateStore.TryGetUnderlineColor, windowPresentationStateStore.SetUnderlineColor);
                };

                var borderColorItem = new ToolStripMenuItem("Border Color...");
                borderColorItem.Click += (_, __) =>
                {
                    ChooseColor(windowHandle, windowPresentationStateStore.TryGetBorderColor, windowPresentationStateStore.SetBorderColor);
                };

                var renameItem = new ToolStripMenuItem("Rename Tab...");
                renameItem.Click += (_, __) =>
                {
                    var currentName = windowPresentationStateStore.TryGetWindowNameOverride(windowHandle, out var nameOverride)
                        ? nameOverride
                        : string.Empty;
                    var renamed = ShowTextPrompt(this, "Rename Tab", currentName);
                    if (renamed == null)
                    {
                        return;
                    }

                    windowPresentationStateStore.SetWindowNameOverride(windowHandle, renamed);
                    refresher.Refresh();
                };

                var closeItem = new ToolStripMenuItem("Close Tab");
                closeItem.Click += (_, __) => CloseWindow(windowHandle);

                var ungroupItem = new ToolStripMenuItem("Ungroup Tab");
                ungroupItem.Click += (_, __) =>
                {
                    desktopRuntime.RemoveWindow(windowHandle);
                    refresher.Refresh();
                };

                var clearRenameItem = new ToolStripMenuItem("Clear Custom Name");
                clearRenameItem.Click += (_, __) =>
                {
                    windowPresentationStateStore.SetWindowNameOverride(windowHandle, null);
                    refresher.Refresh();
                };

                var clearColorsItem = new ToolStripMenuItem("Clear Custom Colors");
                clearColorsItem.Click += (_, __) =>
                {
                    windowPresentationStateStore.SetFillColor(windowHandle, null);
                    windowPresentationStateStore.SetUnderlineColor(windowHandle, null);
                    windowPresentationStateStore.SetBorderColor(windowHandle, null);
                    refresher.Refresh();
                };

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
                menu.Show(button, clientPoint);
            }

            private void ShowGroupContextMenu(Control control, Point clientPoint)
            {
                var group = desktopRuntime.FindGroup(currentGroupHandle);
                if (group == null)
                {
                    return;
                }

                var menu = new ContextMenuStrip();
                var topLeftItem = new ToolStripMenuItem("Group Position Left")
                {
                    Checked = string.Equals(group.TabPosition, "TopLeft", StringComparison.OrdinalIgnoreCase)
                };
                topLeftItem.Click += (_, __) =>
                {
                    group.TabPosition = "TopLeft";
                    refresher.Refresh();
                };

                var topRightItem = new ToolStripMenuItem("Group Position Right")
                {
                    Checked = !string.Equals(group.TabPosition, "TopLeft", StringComparison.OrdinalIgnoreCase)
                };
                topRightItem.Click += (_, __) =>
                {
                    group.TabPosition = "TopRight";
                    refresher.Refresh();
                };

                var snapMarginItem = new ToolStripMenuItem("Snap Tab Height Margin")
                {
                    Checked = group.SnapTabHeightMargin
                };
                snapMarginItem.Click += (_, __) =>
                {
                    group.SnapTabHeightMargin = !group.SnapTabHeightMargin;
                    refresher.Refresh();
                };

                var alignAllLeftItem = new ToolStripMenuItem("Align All Left");
                alignAllLeftItem.Click += (_, __) =>
                {
                    foreach (var hwnd in currentGroupWindowHandles)
                    {
                        windowPresentationStateStore.SetAlignment(hwnd, TabAlign.TopLeft);
                    }

                    refresher.Refresh();
                };

                var alignAllRightItem = new ToolStripMenuItem("Align All Right");
                alignAllRightItem.Click += (_, __) =>
                {
                    foreach (var hwnd in currentGroupWindowHandles)
                    {
                        windowPresentationStateStore.SetAlignment(hwnd, TabAlign.TopRight);
                    }

                    refresher.Refresh();
                };

                var pinAllItem = new ToolStripMenuItem("Pin All Tabs");
                pinAllItem.Click += (_, __) =>
                {
                    foreach (var hwnd in currentGroupWindowHandles)
                    {
                        windowPresentationStateStore.SetPinned(hwnd, true);
                    }

                    refresher.Refresh();
                };

                var unpinAllItem = new ToolStripMenuItem("Unpin All Tabs");
                unpinAllItem.Click += (_, __) =>
                {
                    foreach (var hwnd in currentGroupWindowHandles)
                    {
                        windowPresentationStateStore.SetPinned(hwnd, false);
                    }

                    refresher.Refresh();
                };

                var ungroupAllItem = new ToolStripMenuItem("Ungroup All Tabs");
                ungroupAllItem.Click += (_, __) =>
                {
                    foreach (var hwnd in currentGroupWindowHandles.ToArray())
                    {
                        desktopRuntime.RemoveWindow(hwnd);
                    }

                    refresher.Refresh();
                };

                var closeGroupItem = new ToolStripMenuItem("Close Group");
                closeGroupItem.Click += (_, __) =>
                {
                    foreach (var hwnd in currentGroupWindowHandles.ToArray())
                    {
                        CloseWindow(hwnd);
                    }
                };

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

            private static void CloseWindow(IntPtr windowHandle)
            {
                if (windowHandle == IntPtr.Zero)
                {
                    return;
                }

                BemoWinUserApi.PostMessage(
                    windowHandle,
                    BemoWindowMessages.WM_SYSCOMMAND,
                    new IntPtr(BemoSystemMenuCommandValues.SC_CLOSE),
                    IntPtr.Zero);
            }

            private void ChooseColor(
                IntPtr windowHandle,
                TryGetColor tryGetColor,
                Action<IntPtr, Color?> setColor)
            {
                using (var dialog = new ColorDialog())
                {
                    dialog.FullOpen = true;
                    if (tryGetColor(windowHandle, out var existingColor))
                    {
                        dialog.Color = existingColor;
                    }

                    if (dialog.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    setColor(windowHandle, dialog.Color);
                    refresher.Refresh();
                }
            }

            private sealed class TabButtonState : IDisposable
            {
                private readonly Timer hoverTimer;
                private Action hoverActivateAction;

                public TabButtonState(Button button)
                {
                    Button = button ?? throw new ArgumentNullException(nameof(button));
                    hoverTimer = new Timer
                    {
                        Interval = 350
                    };
                    hoverTimer.Tick += OnHoverTimerTick;
                }

                public Button Button { get; }

                public bool IsMouseDown { get; set; }

                public bool HasStartedDrag { get; set; }

                public bool IsHovered { get; set; }

                public Point MouseDownClientPoint { get; set; }

                public Point MouseDownScreenPoint { get; set; }

                public void StartHoverActivate(Action activateAction)
                {
                    hoverActivateAction = activateAction;
                    hoverTimer.Stop();
                    hoverTimer.Start();
                }

                public void StopHoverActivate()
                {
                    hoverTimer.Stop();
                    hoverActivateAction = null;
                }

                public void Dispose()
                {
                    StopHoverActivate();
                    hoverTimer.Tick -= OnHoverTimerTick;
                    hoverTimer.Dispose();
                    Button.Dispose();
                }

                private void OnHoverTimerTick(object sender, EventArgs e)
                {
                    hoverTimer.Stop();
                    hoverActivateAction?.Invoke();
                }
            }

            private delegate bool TryGetColor(IntPtr hwnd, out Color color);

            private static string ShowTextPrompt(IWin32Window owner, string title, string initialValue)
            {
                using (var dialog = new Form())
                using (var textBox = new TextBox())
                using (var okButton = new Button())
                using (var cancelButton = new Button())
                using (var buttons = new FlowLayoutPanel())
                using (var layout = new TableLayoutPanel())
                {
                    dialog.Text = title;
                    dialog.StartPosition = FormStartPosition.CenterParent;
                    dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                    dialog.MinimizeBox = false;
                    dialog.MaximizeBox = false;
                    dialog.ShowInTaskbar = false;
                    dialog.ClientSize = new Size(360, 110);

                    textBox.Dock = DockStyle.Top;
                    textBox.Text = initialValue ?? string.Empty;

                    okButton.Text = "OK";
                    okButton.DialogResult = DialogResult.OK;
                    cancelButton.Text = "Cancel";
                    cancelButton.DialogResult = DialogResult.Cancel;

                    buttons.Dock = DockStyle.Fill;
                    buttons.FlowDirection = FlowDirection.RightToLeft;
                    buttons.Controls.Add(cancelButton);
                    buttons.Controls.Add(okButton);

                    layout.Dock = DockStyle.Fill;
                    layout.Padding = new Padding(12);
                    layout.RowCount = 3;
                    layout.ColumnCount = 1;
                    layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                    layout.Controls.Add(new Label
                    {
                        Text = "Tab name",
                        AutoSize = true,
                        Margin = new Padding(0, 0, 0, 6)
                    }, 0, 0);
                    layout.Controls.Add(textBox, 0, 1);
                    layout.Controls.Add(buttons, 0, 2);

                    dialog.Controls.Add(layout);
                    dialog.AcceptButton = okButton;
                    dialog.CancelButton = cancelButton;

                    return dialog.ShowDialog(owner) == DialogResult.OK
                        ? textBox.Text?.Trim()
                        : null;
                }
            }

            private sealed class StripDropTarget : IDragDropTarget
            {
                private readonly ManagedGroupStripForm owner;

                public StripDropTarget(ManagedGroupStripForm owner)
                {
                    this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
                }

                public bool OnDragEnter(object data, Point clientPoint)
                {
                    return owner.TryGetTargetWindowHandle(clientPoint, out _)
                           && data is TabDragInfo dragInfo
                           && dragInfo.WindowHandle != IntPtr.Zero;
                }

                public void OnDragMove(Point clientPoint)
                {
                }

                public void OnDrop(object data, Point clientPoint)
                {
                    if (!(data is TabDragInfo dragInfo)
                        || dragInfo.WindowHandle == IntPtr.Zero
                        || !owner.TryGetTargetWindowHandle(clientPoint, out var targetWindowHandle)
                        || targetWindowHandle == dragInfo.WindowHandle)
                    {
                        return;
                    }

                    var targetGroup = owner.desktopRuntime.FindGroupContainingWindow(targetWindowHandle);
                    if (targetGroup == null)
                    {
                        return;
                    }

                    var sourceGroup = owner.desktopRuntime.FindGroupContainingWindow(dragInfo.WindowHandle);
                    if (sourceGroup != null && sourceGroup.GroupHandle == targetGroup.GroupHandle)
                    {
                        targetGroup.MoveWindowAfter(dragInfo.WindowHandle, targetWindowHandle);
                        owner.refresher.Refresh();
                        return;
                    }

                    owner.desktopRuntime.RemoveWindow(dragInfo.WindowHandle);
                    targetGroup.AddWindow(dragInfo.WindowHandle, targetWindowHandle);
                    owner.refresher.Refresh();
                }

                public void OnDragExit()
                {
                }

                public void OnDragBegin()
                {
                }

                public void OnDragEnd()
                {
                }
            }

            private bool TryGetTargetWindowHandle(Point clientPoint, out IntPtr windowHandle)
            {
                foreach (var pair in buttonStates)
                {
                    if (pair.Value.Button.Bounds.Contains(clientPoint))
                    {
                        windowHandle = pair.Key;
                        return true;
                    }
                }

                windowHandle = IntPtr.Zero;
                return false;
            }
        }
    }
}
