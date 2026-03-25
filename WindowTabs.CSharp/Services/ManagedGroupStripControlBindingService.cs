using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripControlBindingService
    {
        private readonly ManagedGroupStripButtonInteractionService buttonInteractionService;
        private readonly ManagedGroupStripMenuService stripMenuService;
        private readonly ManagedGroupStripPaintService stripPaintService;

        public ManagedGroupStripControlBindingService(
            ManagedGroupStripButtonInteractionService buttonInteractionService,
            ManagedGroupStripMenuService stripMenuService,
            ManagedGroupStripPaintService stripPaintService)
        {
            this.buttonInteractionService = buttonInteractionService ?? throw new ArgumentNullException(nameof(buttonInteractionService));
            this.stripMenuService = stripMenuService ?? throw new ArgumentNullException(nameof(stripMenuService));
            this.stripPaintService = stripPaintService ?? throw new ArgumentNullException(nameof(stripPaintService));
        }

        public void WireTabPanel(
            Control control,
            Func<IntPtr> getCurrentGroupHandle,
            Func<IReadOnlyCollection<IntPtr>> getCurrentWindowHandles)
        {
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            if (getCurrentGroupHandle == null)
            {
                throw new ArgumentNullException(nameof(getCurrentGroupHandle));
            }

            if (getCurrentWindowHandles == null)
            {
                throw new ArgumentNullException(nameof(getCurrentWindowHandles));
            }

            control.MouseUp += (_, e) =>
            {
                if (e.Button != MouseButtons.Right)
                {
                    return;
                }

                var currentGroupHandle = getCurrentGroupHandle();
                if (currentGroupHandle == IntPtr.Zero)
                {
                    return;
                }

                stripMenuService.ShowGroupMenu(control, e.Location, currentGroupHandle, getCurrentWindowHandles());
            };
        }

        public Button CreateTabButton()
        {
            var button = new StripTabButton
            {
                FlatStyle = FlatStyle.Flat,
                AutoEllipsis = true,
                UseVisualStyleBackColor = false,
                TabStop = false,
                Margin = Padding.Empty,
                Padding = new Padding(12, 1, 12, 1),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point)
            };

            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseDownBackColor = button.BackColor;
            button.FlatAppearance.MouseOverBackColor = button.BackColor;
            return button;
        }

        public void WireTabButton(
            IWin32Window owner,
            Button button,
            ManagedGroupStripButtonState state,
            IntPtr windowHandle,
            Func<ManagedGroupStripDropState> getDropState,
            Action invalidateStrip)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            if (button == null)
            {
                throw new ArgumentNullException(nameof(button));
            }

            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (getDropState == null)
            {
                throw new ArgumentNullException(nameof(getDropState));
            }

            button.Click += (_, __) => buttonInteractionService.HandleClick(windowHandle);
            button.MouseDown += (_, e) => buttonInteractionService.HandleLeftMouseDown(button, state, e);
            button.MouseMove += (_, e) =>
                buttonInteractionService.HandleMouseMove(button, state, windowHandle, () => CaptureButtonImage(button), e);
            button.MouseUp += (_, __) => buttonInteractionService.HandleMouseUp(state);
            button.MouseDown += (_, e) => buttonInteractionService.HandleMiddleMouseDown(e, windowHandle);
            button.MouseEnter += (_, __) => buttonInteractionService.HandleMouseEnter(button, state, windowHandle, invalidateStrip);
            button.MouseLeave += (_, __) => buttonInteractionService.HandleMouseLeave(button, state, invalidateStrip);
            button.MouseUp += (_, e) =>
            {
                if (!buttonInteractionService.HandleRightMouseUp(e, state))
                {
                    return;
                }

                stripMenuService.ShowWindowMenu(owner, button, e.Location, windowHandle);
            };
            button.Paint += (_, e) =>
            {
                var dropState = getDropState();
                stripPaintService.PaintTabOverlay(
                    e.Graphics,
                    windowHandle,
                    button.Size,
                    dropState.IsTargetWindow(windowHandle),
                    dropState.InsertAfterTarget);
            };
        }

        private static Bitmap CaptureButtonImage(Control control)
        {
            var bitmap = new Bitmap(control.Width, control.Height);
            control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, control.Size));
            return bitmap;
        }

        private sealed class StripTabButton : Button
        {
            protected override bool ShowFocusCues => false;
        }
    }
}
