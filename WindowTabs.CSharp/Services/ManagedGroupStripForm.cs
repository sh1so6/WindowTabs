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
        private ManagedGroupStripFormState formState = ManagedGroupStripFormState.Empty;

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
            controlBindingService.WireTabPanel(tabPanel, () => formState.CurrentGroupHandle, () => formState.DragSessionState.CurrentGroupWindowHandles);
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

            var stripSize = GetPreferredSize(Size.Empty);
            if (!stripPlacementService.TryResolveLocation(
                group,
                windowsByHandle,
                displayState.Settings,
                displayState.ActiveWindowHandle,
                stripSize,
                displayState.Appearance,
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
                tabPanel.DisplayRectangle,
                out dropTargetInfo);
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
