using System;
using System.Drawing;
using System.Windows.Forms;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripButtonInteractionService
    {
        private readonly SettingsSession settingsSession;
        private readonly GroupWindowActivationService groupWindowActivationService;
        private readonly GroupMutationService groupMutationService;
        private readonly Contracts.IDragDrop dragDrop;

        public ManagedGroupStripButtonInteractionService(
            SettingsSession settingsSession,
            GroupWindowActivationService groupWindowActivationService,
            GroupMutationService groupMutationService,
            Contracts.IDragDrop dragDrop)
        {
            this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
            this.groupWindowActivationService = groupWindowActivationService ?? throw new ArgumentNullException(nameof(groupWindowActivationService));
            this.groupMutationService = groupMutationService ?? throw new ArgumentNullException(nameof(groupMutationService));
            this.dragDrop = dragDrop ?? throw new ArgumentNullException(nameof(dragDrop));
        }

        public void HandleClick(IntPtr windowHandle)
        {
            groupWindowActivationService.ActivateWindow(windowHandle);
        }

        public void HandleLeftMouseDown(Button button, ManagedGroupStripButtonState state, MouseEventArgs e)
        {
            if (button == null)
            {
                throw new ArgumentNullException(nameof(button));
            }

            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (e == null || e.Button != MouseButtons.Left)
            {
                return;
            }

            state.MouseDownClientPoint = e.Location;
            state.MouseDownScreenPoint = button.PointToScreen(e.Location);
            state.IsMouseDown = true;
            state.HasStartedDrag = false;
        }

        public void HandleMouseMove(Button button, ManagedGroupStripButtonState state, IntPtr windowHandle, Func<Bitmap> captureImage, MouseEventArgs e)
        {
            if (button == null)
            {
                throw new ArgumentNullException(nameof(button));
            }

            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (captureImage == null)
            {
                throw new ArgumentNullException(nameof(captureImage));
            }

            if (e == null
                || !state.IsMouseDown
                || state.HasStartedDrag
                || (Control.MouseButtons & MouseButtons.Left) == 0)
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
                CreateImage = captureImage,
                Data = new TabDragInfo
                {
                    WindowHandle = windowHandle,
                    TabOffset = state.MouseDownClientPoint,
                    ImageOffset = state.MouseDownClientPoint
                }
            });
        }

        public void HandleMouseUp(ManagedGroupStripButtonState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            state.IsMouseDown = false;
            state.HasStartedDrag = false;
        }

        public void HandleMiddleMouseDown(MouseEventArgs e, IntPtr windowHandle)
        {
            if (e == null || e.Button != MouseButtons.Middle)
            {
                return;
            }

            groupMutationService.UngroupWindow(windowHandle);
        }

        public void HandleMouseEnter(Button button, ManagedGroupStripButtonState state, IntPtr windowHandle, Action invalidateStrip)
        {
            if (button == null)
            {
                throw new ArgumentNullException(nameof(button));
            }

            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            state.IsHovered = true;
            button.Invalidate();
            invalidateStrip?.Invoke();
            if (settingsSession.Current.EnableHoverActivate)
            {
                state.StopHoverActivate();
                groupWindowActivationService.ActivateWindow(windowHandle);
            }
        }

        public void HandleMouseLeave(Button button, ManagedGroupStripButtonState state, Action invalidateStrip)
        {
            if (button == null)
            {
                throw new ArgumentNullException(nameof(button));
            }

            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            state.IsHovered = false;
            button.Invalidate();
            invalidateStrip?.Invoke();
            state.StopHoverActivate();
        }

        public bool HandleRightMouseUp(MouseEventArgs e, ManagedGroupStripButtonState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (e == null || e.Button != MouseButtons.Right)
            {
                return false;
            }

            state.StopHoverActivate();
            return true;
        }
    }
}
