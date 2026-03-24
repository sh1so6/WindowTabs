using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripRegistry : IDisposable
    {
        private readonly DesktopMonitoringService desktopMonitoringService;
        private readonly DesktopSnapshotService desktopSnapshotService;
        private readonly SettingsSession settingsSession;
        private readonly GroupVisualOrderService groupVisualOrderService;
        private readonly ManagedGroupStripLayoutService stripLayoutService;
        private readonly ManagedGroupStripDragStateService stripDragStateService;
        private readonly ManagedGroupStripDropController stripDropController;
        private readonly ManagedGroupStripButtonInteractionService buttonInteractionService;
        private readonly ManagedGroupStripVisualService stripVisualService;
        private readonly ManagedGroupStripPlacementService stripPlacementService;
        private readonly ManagedGroupStripPaintService stripPaintService;
        private readonly GroupWindowActivationService groupWindowActivationService;
        private readonly GroupMutationService groupMutationService;
        private readonly ManagedGroupStripMenuService stripMenuService;
        private readonly Contracts.IDragDrop dragDrop;
        private readonly IDesktopRuntime desktopRuntime;
        private readonly Dictionary<IntPtr, ManagedGroupStripForm> forms = new Dictionary<IntPtr, ManagedGroupStripForm>();
        private bool initialized;
        private bool disposed;

        public ManagedGroupStripRegistry(
            DesktopMonitoringService desktopMonitoringService,
            DesktopSnapshotService desktopSnapshotService,
            SettingsSession settingsSession,
            GroupVisualOrderService groupVisualOrderService,
            ManagedGroupStripLayoutService stripLayoutService,
            ManagedGroupStripDragStateService stripDragStateService,
            ManagedGroupStripDropController stripDropController,
            ManagedGroupStripButtonInteractionService buttonInteractionService,
            ManagedGroupStripVisualService stripVisualService,
            ManagedGroupStripPlacementService stripPlacementService,
            ManagedGroupStripPaintService stripPaintService,
            GroupWindowActivationService groupWindowActivationService,
            GroupMutationService groupMutationService,
            ManagedGroupStripMenuService stripMenuService,
            Contracts.IDragDrop dragDrop,
            IDesktopRuntime desktopRuntime)
        {
            this.desktopMonitoringService = desktopMonitoringService ?? throw new ArgumentNullException(nameof(desktopMonitoringService));
            this.desktopSnapshotService = desktopSnapshotService ?? throw new ArgumentNullException(nameof(desktopSnapshotService));
            this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
            this.groupVisualOrderService = groupVisualOrderService ?? throw new ArgumentNullException(nameof(groupVisualOrderService));
            this.stripLayoutService = stripLayoutService ?? throw new ArgumentNullException(nameof(stripLayoutService));
            this.stripDragStateService = stripDragStateService ?? throw new ArgumentNullException(nameof(stripDragStateService));
            this.stripDropController = stripDropController ?? throw new ArgumentNullException(nameof(stripDropController));
            this.buttonInteractionService = buttonInteractionService ?? throw new ArgumentNullException(nameof(buttonInteractionService));
            this.stripVisualService = stripVisualService ?? throw new ArgumentNullException(nameof(stripVisualService));
            this.stripPlacementService = stripPlacementService ?? throw new ArgumentNullException(nameof(stripPlacementService));
            this.stripPaintService = stripPaintService ?? throw new ArgumentNullException(nameof(stripPaintService));
            this.groupWindowActivationService = groupWindowActivationService ?? throw new ArgumentNullException(nameof(groupWindowActivationService));
            this.groupMutationService = groupMutationService ?? throw new ArgumentNullException(nameof(groupMutationService));
            this.stripMenuService = stripMenuService ?? throw new ArgumentNullException(nameof(stripMenuService));
            this.dragDrop = dragDrop ?? throw new ArgumentNullException(nameof(dragDrop));
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
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
                        groupVisualOrderService,
                        stripLayoutService,
                        stripDragStateService,
                        stripDropController,
                        buttonInteractionService,
                        stripVisualService,
                        stripPlacementService,
                        stripPaintService,
                        groupWindowActivationService,
                        groupMutationService,
                        stripMenuService,
                        dragDrop,
                        desktopRuntime);
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
            private readonly GroupVisualOrderService groupVisualOrderService;
            private readonly ManagedGroupStripLayoutService stripLayoutService;
            private readonly ManagedGroupStripDragStateService stripDragStateService;
            private readonly ManagedGroupStripDropController stripDropController;
            private readonly ManagedGroupStripButtonInteractionService buttonInteractionService;
            private readonly ManagedGroupStripVisualService stripVisualService;
            private readonly ManagedGroupStripPlacementService stripPlacementService;
            private readonly ManagedGroupStripPaintService stripPaintService;
            private readonly GroupWindowActivationService groupWindowActivationService;
            private readonly GroupMutationService groupMutationService;
            private readonly ManagedGroupStripMenuService stripMenuService;
            private readonly Contracts.IDragDrop dragDrop;
            private readonly IDesktopRuntime desktopRuntime;
            private readonly FlowLayoutPanel tabPanel;
            private readonly Dictionary<IntPtr, ManagedGroupStripButtonState> buttonStates = new Dictionary<IntPtr, ManagedGroupStripButtonState>();
            private readonly StripDropTarget stripDropTarget;
            private IntPtr currentGroupHandle;
            private List<IntPtr> actualGroupWindowHandles = new List<IntPtr>();
            private List<IntPtr> currentGroupWindowHandles = new List<IntPtr>();
            private List<IntPtr> previewGroupWindowHandles;
            private IntPtr currentDraggedWindowHandle;
            private IntPtr currentDropTargetWindowHandle;
            private bool currentDropTargetInsertAfter = true;

            public ManagedGroupStripForm(
                DesktopSnapshotService desktopSnapshotService,
                SettingsSession settingsSession,
                GroupVisualOrderService groupVisualOrderService,
                ManagedGroupStripLayoutService stripLayoutService,
                ManagedGroupStripDragStateService stripDragStateService,
                ManagedGroupStripDropController stripDropController,
                ManagedGroupStripButtonInteractionService buttonInteractionService,
                ManagedGroupStripVisualService stripVisualService,
                ManagedGroupStripPlacementService stripPlacementService,
                ManagedGroupStripPaintService stripPaintService,
                GroupWindowActivationService groupWindowActivationService,
                GroupMutationService groupMutationService,
                ManagedGroupStripMenuService stripMenuService,
                Contracts.IDragDrop dragDrop,
                IDesktopRuntime desktopRuntime)
            {
                this.desktopSnapshotService = desktopSnapshotService ?? throw new ArgumentNullException(nameof(desktopSnapshotService));
                this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
                this.groupVisualOrderService = groupVisualOrderService ?? throw new ArgumentNullException(nameof(groupVisualOrderService));
                this.stripLayoutService = stripLayoutService ?? throw new ArgumentNullException(nameof(stripLayoutService));
                this.stripDragStateService = stripDragStateService ?? throw new ArgumentNullException(nameof(stripDragStateService));
                this.stripDropController = stripDropController ?? throw new ArgumentNullException(nameof(stripDropController));
                this.buttonInteractionService = buttonInteractionService ?? throw new ArgumentNullException(nameof(buttonInteractionService));
                this.stripVisualService = stripVisualService ?? throw new ArgumentNullException(nameof(stripVisualService));
                this.stripPlacementService = stripPlacementService ?? throw new ArgumentNullException(nameof(stripPlacementService));
                this.stripPaintService = stripPaintService ?? throw new ArgumentNullException(nameof(stripPaintService));
                this.groupWindowActivationService = groupWindowActivationService ?? throw new ArgumentNullException(nameof(groupWindowActivationService));
                this.groupMutationService = groupMutationService ?? throw new ArgumentNullException(nameof(groupMutationService));
                this.stripMenuService = stripMenuService ?? throw new ArgumentNullException(nameof(stripMenuService));
                this.dragDrop = dragDrop ?? throw new ArgumentNullException(nameof(dragDrop));
                this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
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
                actualGroupWindowHandles = orderedHandles.ToList();
                var canUsePreviewWindowHandles = stripLayoutService.CanUsePreviewWindowHandles(orderedHandles, previewGroupWindowHandles);
                var displayHandles = canUsePreviewWindowHandles
                    ? previewGroupWindowHandles.ToList()
                    : orderedHandles.ToList();
                if (!canUsePreviewWindowHandles)
                {
                    previewGroupWindowHandles = null;
                }

                currentGroupWindowHandles = displayHandles.ToList();
                foreach (var staleHandle in buttonStates.Keys.Where(hwnd => !displayHandles.Contains(hwnd)).ToArray())
                {
                    var state = buttonStates[staleHandle];
                    tabPanel.Controls.Remove(state.Button);
                    state.Dispose();
                    buttonStates.Remove(staleHandle);
                }

                for (var index = 0; index < displayHandles.Count; index++)
                {
                    var windowHandle = displayHandles[index];
                    if (!buttonStates.TryGetValue(windowHandle, out var state))
                    {
                        var button = CreateTabButton();
                        state = new ManagedGroupStripButtonState(button);
                        buttonStates.Add(windowHandle, state);
                        tabPanel.Controls.Add(button);
                        WireButton(button, state, windowHandle);
                    }

                    if (windowsByHandle.TryGetValue(windowHandle, out var window))
                    {
                        var isActive = activeWindowHandle == windowHandle;
                        var visual = stripVisualService.CreateTabVisual(
                            windowHandle,
                            window.Text,
                            group,
                            appearance,
                            state.Button.Font,
                            isActive,
                            state.IsHovered,
                            currentDropTargetWindowHandle == windowHandle);

                        state.Button.Text = visual.Text;
                        state.Button.BackColor = visual.FillColor;
                        state.Button.ForeColor = visual.TextColor;
                        state.Button.FlatAppearance.BorderColor = visual.BorderColor;
                        state.Button.TextAlign = visual.TextAlign;
                        state.Button.Size = visual.Size;
                        state.Button.Tag = windowHandle;
                        state.Button.Invalidate();
                    }
                }

                ApplyDisplayedButtonOrder(displayHandles);

                var stripSize = GetPreferredSize(Size.Empty);
                if (!stripPlacementService.TryResolveLocation(
                    group,
                    windowsByHandle,
                    settings,
                    activeWindowHandle,
                    stripSize,
                    appearance,
                    out var location))
                {
                    Hide();
                    return;
                }

                Location = location;

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
                var runtimeGroup = desktopRuntime.FindGroup(group.GroupHandle);
                if (runtimeGroup != null)
                {
                    return groupVisualOrderService.OrderWindowHandles(runtimeGroup);
                }

                return group.WindowHandles.ToList();
            }

            private void WireButton(Button button, ManagedGroupStripButtonState state, IntPtr windowHandle)
            {
                button.Click += (_, __) => buttonInteractionService.HandleClick(windowHandle);

                button.MouseDown += (_, e) => buttonInteractionService.HandleLeftMouseDown(button, state, e);

                button.MouseMove += (_, e) =>
                    buttonInteractionService.HandleMouseMove(button, state, windowHandle, () => CaptureButtonImage(button), e);

                button.MouseUp += (_, __) => buttonInteractionService.HandleMouseUp(state);

                button.MouseDown += (_, e) => buttonInteractionService.HandleMiddleMouseDown(e, windowHandle);

                button.MouseEnter += (_, __) =>
                    buttonInteractionService.HandleMouseEnter(button, state, windowHandle, Invalidate);

                button.MouseLeave += (_, __) =>
                    buttonInteractionService.HandleMouseLeave(button, state, Invalidate);

                button.MouseUp += (_, e) =>
                {
                    if (!buttonInteractionService.HandleRightMouseUp(e, state))
                    {
                        return;
                    }

                    ShowContextMenu(button, windowHandle, e.Location);
                };

                button.Paint += (_, e) =>
                {
                    stripPaintService.PaintTabOverlay(
                        e.Graphics,
                        windowHandle,
                        button.Size,
                        currentDropTargetWindowHandle == windowHandle,
                        currentDropTargetInsertAfter);
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

            private void ApplyDisplayedButtonOrder(IReadOnlyList<IntPtr> orderedHandles)
            {
                for (var index = 0; index < orderedHandles.Count; index++)
                {
                    if (buttonStates.TryGetValue(orderedHandles[index], out var state))
                    {
                        tabPanel.Controls.SetChildIndex(state.Button, index);
                    }
                }
            }

            private void ClearPreviewWindowOrder()
            {
                if (previewGroupWindowHandles == null)
                {
                    return;
                }

                previewGroupWindowHandles = null;
                currentGroupWindowHandles = actualGroupWindowHandles.ToList();
                ApplyDisplayedButtonOrder(currentGroupWindowHandles);
            }

            private void UpdatePreviewForDrag(TabDragInfo dragInfo, IntPtr? insertAfterWindowHandle)
            {
                currentDraggedWindowHandle = dragInfo?.WindowHandle ?? IntPtr.Zero;
                UpdatePreviewForCurrentDrag(insertAfterWindowHandle);
            }

            private void UpdatePreviewForCurrentDrag(IntPtr? insertAfterWindowHandle)
            {
                var previewOrder = stripDragStateService.BuildPreviewWindowHandles(
                    currentDraggedWindowHandle,
                    desktopRuntime.FindGroup(currentGroupHandle) != null,
                    actualGroupWindowHandles,
                    insertAfterWindowHandle);
                if (previewOrder == null)
                {
                    ClearPreviewWindowOrder();
                    return;
                }

                previewGroupWindowHandles = previewOrder;
                currentGroupWindowHandles = previewOrder.ToList();
                ApplyDisplayedButtonOrder(previewOrder);
            }

            private void SetDropTargetWindowHandle(IntPtr windowHandle)
            {
                SetDropTargetWindowHandle(windowHandle, true);
            }

            private void SetDropTargetWindowHandle(IntPtr windowHandle, bool insertAfter)
            {
                var change = stripDragStateService.UpdateDropTarget(
                    currentDropTargetWindowHandle,
                    currentDropTargetInsertAfter,
                    windowHandle,
                    insertAfter);
                if (!change.HasChanged)
                {
                    return;
                }

                foreach (var invalidatedWindowHandle in change.InvalidatedWindowHandles)
                {
                    InvalidateDropTargetWindowHandle(invalidatedWindowHandle);
                }

                currentDropTargetWindowHandle = change.TargetWindowHandle;
                currentDropTargetInsertAfter = change.InsertAfterTarget;
            }

            private void ClearDropTargetWindowHandle()
            {
                SetDropTargetWindowHandle(IntPtr.Zero);
            }

            private void ClearDragState()
            {
                ClearDropTargetWindowHandle();
                ClearPreviewWindowOrder();
            }

            private void InvalidateDropTargetWindowHandle(IntPtr windowHandle)
            {
                if (windowHandle != IntPtr.Zero && buttonStates.TryGetValue(windowHandle, out var state))
                {
                    state.Button.Invalidate();
                }
            }

            private bool TryGetDropTargetInfo(Point clientPoint, out IntPtr targetWindowHandle, out IntPtr? insertAfterWindowHandle)
            {
                if (TryResolveDropTarget(clientPoint, out var dropTargetInfo))
                {
                    targetWindowHandle = dropTargetInfo.TargetWindowHandle;
                    insertAfterWindowHandle = dropTargetInfo.InsertAfterWindowHandle;
                    return true;
                }

                targetWindowHandle = IntPtr.Zero;
                insertAfterWindowHandle = null;
                return false;
            }

            private bool TryResolveDropTarget(Point clientPoint, out ManagedGroupStripDropTargetInfo dropTargetInfo)
            {
                var buttonBoundsByHandle = currentGroupWindowHandles
                    .Where(buttonStates.ContainsKey)
                    .ToDictionary(handle => handle, handle => buttonStates[handle].Button.Bounds);
                return stripLayoutService.TryResolveDropTarget(
                    clientPoint,
                    currentGroupWindowHandles,
                    buttonBoundsByHandle,
                    tabPanel.DisplayRectangle,
                    out dropTargetInfo);
            }

            private void ShowContextMenu(Control button, IntPtr windowHandle, Point clientPoint)
            {
                stripMenuService.ShowWindowMenu(this, button, clientPoint, windowHandle);
            }

            private void ShowGroupContextMenu(Control control, Point clientPoint)
            {
                stripMenuService.ShowGroupMenu(control, clientPoint, currentGroupHandle, currentGroupWindowHandles);
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
                    return owner.stripDropController.HandleDragEnter(
                        data,
                        clientPoint,
                        owner.TryResolveDropTarget,
                        owner.SetDropTargetWindowHandle,
                        owner.UpdatePreviewForDrag);
                }

                public void OnDragMove(Point clientPoint)
                {
                    owner.stripDropController.HandleDragMove(
                        clientPoint,
                        owner.TryResolveDropTarget,
                        owner.SetDropTargetWindowHandle,
                        owner.UpdatePreviewForCurrentDrag,
                        owner.ClearDragState);
                }

                public void OnDrop(object data, Point clientPoint)
                {
                    owner.stripDropController.HandleDrop(
                        data,
                        clientPoint,
                        owner.TryResolveDropTarget,
                        owner.ClearDragState);
                }

                public void OnDragExit()
                {
                    owner.stripDropController.HandleDragExit(owner.ClearDragState);
                }

                public void OnDragBegin()
                {
                }

                public void OnDragEnd()
                {
                    owner.stripDropController.HandleDragExit(owner.ClearDragState);
                }
            }

            private bool TryGetTargetWindowHandle(Point clientPoint, out IntPtr windowHandle)
            {
                return TryGetDropTargetInfo(clientPoint, out windowHandle, out _);
            }
        }
    }
}
