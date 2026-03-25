using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripForm : Form
    {
        private readonly ManagedGroupStripDisplayStateService displayStateService;
        private readonly ManagedGroupStripFormStateService formStateService;
        private readonly ManagedGroupStripDropController stripDropController;
        private readonly ManagedGroupStripButtonVisualService buttonVisualService;
        private readonly ManagedGroupStripControlBindingService controlBindingService;
        private readonly ManagedGroupStripPlacementService stripPlacementService;
        private readonly IDragDrop dragDrop;
        private readonly FlowLayoutPanel tabPanel;
        private readonly Dictionary<IntPtr, ManagedGroupStripButtonState> buttonStates = new Dictionary<IntPtr, ManagedGroupStripButtonState>();
        private readonly StripDropTarget stripDropTarget;
        private readonly StripDropSurface stripDropSurface;
        private readonly Timer autoHideTimer;
        private Color chromeBorderColor = ColorSerialization.FromRgb(0x8FA6C4);
        private Color chromeTopFillColor = ColorSerialization.FromRgb(0xF4F8FC);
        private Color chromeBottomBorderColor = ColorSerialization.FromRgb(0xC4D0DF);
        private ManagedGroupStripFormState formState = ManagedGroupStripFormState.Empty;
        private int currentTabOverlap = 20;
        private string currentHideTabsMode = "never";
        private int currentHideTabsDelayMilliseconds = 3000;
        private bool currentShowInside;
        private bool isAutoHidden;
        private DateTime? autoHideDeadlineUtc;

        public ManagedGroupStripForm(
            ManagedGroupStripDisplayStateService displayStateService,
            ManagedGroupStripFormStateService formStateService,
            ManagedGroupStripDropController stripDropController,
            ManagedGroupStripButtonVisualService buttonVisualService,
            ManagedGroupStripControlBindingService controlBindingService,
            ManagedGroupStripPlacementService stripPlacementService,
            IDragDrop dragDrop)
        {
            this.displayStateService = displayStateService ?? throw new ArgumentNullException(nameof(displayStateService));
            this.formStateService = formStateService ?? throw new ArgumentNullException(nameof(formStateService));
            this.stripDropController = stripDropController ?? throw new ArgumentNullException(nameof(stripDropController));
            this.buttonVisualService = buttonVisualService ?? throw new ArgumentNullException(nameof(buttonVisualService));
            this.controlBindingService = controlBindingService ?? throw new ArgumentNullException(nameof(controlBindingService));
            this.stripPlacementService = stripPlacementService ?? throw new ArgumentNullException(nameof(stripPlacementService));
            this.dragDrop = dragDrop ?? throw new ArgumentNullException(nameof(dragDrop));
            stripDropTarget = new StripDropTarget(this);
            stripDropSurface = new StripDropSurface(this);

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = ColorSerialization.FromRgb(0xE6EDF6);
            Padding = new Padding(1, 1, 1, 1);

            tabPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = Padding.Empty,
                Padding = new Padding(3, 2, 3, 1),
                BackColor = ColorSerialization.FromRgb(0xF0F5FB)
            };

            Controls.Add(tabPanel);
            Paint += OnPaintChrome;
            HandleCreated += (_, __) => dragDrop.RegisterTarget(Handle, stripDropTarget);
            HandleDestroyed += (_, __) =>
            {
                if (Handle != IntPtr.Zero)
                {
                    dragDrop.UnregisterTarget(Handle);
                }
            };
            controlBindingService.WireTabPanel(tabPanel, () => formState.CurrentGroupHandle, () => formState.DragSessionState.CurrentGroupWindowHandles);
            autoHideTimer = new Timer
            {
                Interval = 100
            };
            autoHideTimer.Tick += OnAutoHideTimerTick;
            autoHideTimer.Start();
        }

        protected override bool ShowWithoutActivation => true;

        public void UpdateGroup(GroupSnapshot group, IReadOnlyDictionary<IntPtr, WindowSnapshot> windowsByHandle)
        {
            var displayState = displayStateService.BuildDisplayState(group, formState.DragSessionState);
            formState = formStateService.ApplyDisplayState(displayState, tabPanel, buttonStates, this, () => formState, Invalidate);
            buttonVisualService.ApplyVisuals(
                formState.DragSessionState.CurrentGroupWindowHandles,
                buttonStates,
                windowsByHandle,
                group,
                displayState.Appearance,
                displayState.ActiveWindowHandle,
                formState.DragSessionState.DropState);
            currentTabOverlap = Math.Max(0, displayState.Appearance?.TabOverlap ?? 0);
            currentHideTabsMode = displayState.Settings?.HideTabsWhenDownByDefault ?? "never";
            currentHideTabsDelayMilliseconds = Math.Max(0, displayState.Settings?.HideTabsDelayMilliseconds ?? 3000);

            var stripSize = GetPreferredSize(Size.Empty);
            if (!stripPlacementService.TryResolveLocation(
                group,
                windowsByHandle,
                displayState.Settings,
                displayState.ActiveWindowHandle,
                stripSize,
                displayState.Appearance,
                out var location,
                out var showInside))
            {
                currentShowInside = false;
                ClearAutoHideRegion();
                Hide();
                return;
            }

            currentShowInside = showInside;
            Location = location;
            ApplyChrome(displayState.Appearance);
            if (!Visible)
            {
                Show();
            }

            RefreshAutoHideState();
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                autoHideTimer.Stop();
                autoHideTimer.Tick -= OnAutoHideTimerTick;
                autoHideTimer.Dispose();
                ClearAutoHideRegion();
                foreach (var state in buttonStates.Values.ToArray())
                {
                    state.Dispose();
                }

                buttonStates.Clear();
            }

            base.Dispose(disposing);
        }

        private void ApplyChrome(TabAppearanceInfo appearance)
        {
            if (appearance == null)
            {
                return;
            }

            chromeBorderColor = BlendWithWhite(appearance.TabInactiveBorderColor, 0.34f);
            chromeBottomBorderColor = BlendWithWhite(appearance.TabInactiveBorderColor, 0.52f);
            chromeTopFillColor = BlendWithWhite(appearance.TabActiveTabColor, 0.16f);
            BackColor = BlendWithWhite(appearance.TabInactiveBorderColor, 0.78f);
            tabPanel.BackColor = BlendWithWhite(appearance.TabInactiveTabColor, 0.70f);
            Invalidate();
        }

        private void OnPaintChrome(object sender, PaintEventArgs e)
        {
            var bounds = ClientRectangle;
            if (bounds.Width <= 2 || bounds.Height <= 2)
            {
                return;
            }

            var topFillHeight = Math.Max(1, Math.Min(5, bounds.Height - 2));
            using (var topBrush = new SolidBrush(chromeTopFillColor))
            {
                e.Graphics.FillRectangle(topBrush, 1, 1, bounds.Width - 2, topFillHeight);
            }

            using (var borderPen = new Pen(chromeBorderColor))
            {
                e.Graphics.DrawRectangle(borderPen, 0, 0, bounds.Width - 1, bounds.Height - 1);
            }

            using (var bottomPen = new Pen(chromeBottomBorderColor))
            {
                e.Graphics.DrawLine(bottomPen, 1, bounds.Height - 1, bounds.Width - 2, bounds.Height - 1);
            }
        }

        private static Color BlendWithWhite(Color color, float amount)
        {
            amount = Math.Max(0f, Math.Min(1f, amount));
            return Color.FromArgb(
                255,
                (int)(color.R + ((255 - color.R) * amount)),
                (int)(color.G + ((255 - color.G) * amount)),
                (int)(color.B + ((255 - color.B) * amount)));
        }

        private void ClearPreviewWindowOrder()
        {
            if (formState.DragSessionState.PreviewGroupWindowHandles == null)
            {
                return;
            }

            formState = formStateService.ClearDragState(formState, tabPanel, buttonStates, this, () => formState, Invalidate);
        }

        private void UpdatePreviewForDrag(TabDragInfo dragInfo, IntPtr? insertAfterWindowHandle)
        {
            formState = formStateService.StartDragPreview(
                formState,
                dragInfo,
                insertAfterWindowHandle,
                tabPanel,
                buttonStates,
                this,
                () => formState,
                Invalidate);
        }

        private void UpdatePreviewForCurrentDrag(IntPtr? insertAfterWindowHandle)
        {
            formState = formStateService.UpdateDragPreview(
                formState,
                insertAfterWindowHandle,
                tabPanel,
                buttonStates,
                this,
                () => formState,
                Invalidate);
        }

        private void SetDropTargetWindowHandle(IntPtr windowHandle)
        {
            SetDropTargetWindowHandle(windowHandle, true);
        }

        private void SetDropTargetWindowHandle(IntPtr windowHandle, bool insertAfter)
        {
            formState = formStateService.UpdateDropTarget(
                formState,
                windowHandle,
                insertAfter,
                buttonStates);
        }

        private void ClearDragState()
        {
            SetDropTargetWindowHandle(IntPtr.Zero);
            ClearPreviewWindowOrder();
        }

        private bool TryResolveDropTarget(Point clientPoint, out ManagedGroupStripDropTargetInfo dropTargetInfo)
        {
            return formStateService.TryResolveDropTarget(
                clientPoint,
                formState,
                buttonStates,
                currentTabOverlap,
                tabPanel.DisplayRectangle,
                out dropTargetInfo);
        }

        private void OnAutoHideTimerTick(object sender, EventArgs e)
        {
            RefreshAutoHideState();
        }

        private void RefreshAutoHideState()
        {
            if (!Visible)
            {
                autoHideDeadlineUtc = null;
                return;
            }

            if (!ShouldAutoHideInsideTabs())
            {
                autoHideDeadlineUtc = null;
                SetAutoHidden(false);
                return;
            }

            if (IsPointerOverVisibleStrip())
            {
                autoHideDeadlineUtc = null;
                SetAutoHidden(false);
                return;
            }

            autoHideDeadlineUtc ??= DateTime.UtcNow.AddMilliseconds(currentHideTabsDelayMilliseconds);
            if (DateTime.UtcNow >= autoHideDeadlineUtc.Value)
            {
                SetAutoHidden(true);
            }
        }

        private bool ShouldAutoHideInsideTabs()
        {
            return currentShowInside
                && string.Equals(currentHideTabsMode, "down", StringComparison.OrdinalIgnoreCase)
                && formState.DragSessionState.DraggedWindowHandle == IntPtr.Zero;
        }

        private bool IsPointerOverVisibleStrip()
        {
            var clientPoint = PointToClient(Control.MousePosition);
            if (Region != null)
            {
                return Region.IsVisible(clientPoint);
            }

            return ClientRectangle.Contains(clientPoint);
        }

        private void SetAutoHidden(bool autoHidden)
        {
            if (isAutoHidden == autoHidden)
            {
                return;
            }

            isAutoHidden = autoHidden;
            ApplyAutoHideRegion();
        }

        private void ApplyAutoHideRegion()
        {
            if (!isAutoHidden || !currentShowInside || Height <= 0 || Width <= 0)
            {
                ClearAutoHideRegion();
                return;
            }

            var visibleHeight = Math.Min(7, Math.Max(1, Height));
            var top = Math.Max(0, Height - visibleHeight);
            var previousRegion = Region;
            Region = new Region(new Rectangle(0, top, Width, visibleHeight));
            previousRegion?.Dispose();
        }

        private void ClearAutoHideRegion()
        {
            var previousRegion = Region;
            Region = null;
            previousRegion?.Dispose();
            isAutoHidden = false;
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
                    owner.stripDropSurface);
            }

            public void OnDragMove(Point clientPoint)
            {
                owner.stripDropController.HandleDragMove(
                    clientPoint,
                    owner.stripDropSurface);
            }

            public void OnDrop(object data, Point clientPoint)
            {
                owner.stripDropController.HandleDrop(
                    data,
                    clientPoint,
                    owner.stripDropSurface);
            }

            public void OnDragExit()
            {
                owner.stripDropController.HandleDragExit(owner.stripDropSurface);
            }

            public void OnDragBegin()
            {
            }

            public void OnDragEnd()
            {
                owner.stripDropController.HandleDragExit(owner.stripDropSurface);
            }
        }

        private sealed class StripDropSurface : ManagedGroupStripDropController.IDropSurface
        {
            private readonly ManagedGroupStripForm owner;

            public StripDropSurface(ManagedGroupStripForm owner)
            {
                this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
            }

            public bool TryResolveDropTarget(Point clientPoint, out ManagedGroupStripDropTargetInfo dropTargetInfo)
            {
                return owner.TryResolveDropTarget(clientPoint, out dropTargetInfo);
            }

            public void SetDropTarget(IntPtr windowHandle, bool insertAfter)
            {
                owner.SetDropTargetWindowHandle(windowHandle, insertAfter);
            }

            public void UpdatePreviewForDrag(TabDragInfo dragInfo, IntPtr? insertAfterWindowHandle)
            {
                owner.UpdatePreviewForDrag(dragInfo, insertAfterWindowHandle);
            }

            public void UpdatePreviewForCurrentDrag(IntPtr? insertAfterWindowHandle)
            {
                owner.UpdatePreviewForCurrentDrag(insertAfterWindowHandle);
            }

            public void ClearDragState()
            {
                owner.ClearDragState();
            }
        }
    }
}
