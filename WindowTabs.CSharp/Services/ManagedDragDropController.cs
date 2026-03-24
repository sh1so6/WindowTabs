using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;
using BemoPoint = Bemo.POINT;
using BemoVirtualKeyCodes = Bemo.VirtualKeyCodes;
using BemoWin32Helper = Bemo.Win32Helper;
using BemoWinUserApi = Bemo.WinUserApi;
using BemoWindowMessages = Bemo.WindowMessages;
using BemoWindowsExtendedStyles = Bemo.WindowsExtendedStyles;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedDragDropController : IDragDrop, IDisposable
    {
        private readonly object syncRoot = new object();
        private readonly IDragDropParent parent;
        private readonly Dictionary<IntPtr, IDragDropTarget> targets = new Dictionary<IntPtr, IDragDropTarget>();
        private readonly HashSet<IDragDropNotification> notifications = new HashSet<IDragDropNotification>();
        private DragAction activeDragAction;
        private bool isDisposed;

        public ManagedDragDropController(IDragDropParent parent)
        {
            this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
        }

        public void RegisterNotification(IDragDropNotification notification)
        {
            if (notification == null)
            {
                return;
            }

            lock (syncRoot)
            {
                notifications.Add(notification);
            }
        }

        public void UnregisterNotification(IDragDropNotification notification)
        {
            if (notification == null)
            {
                return;
            }

            lock (syncRoot)
            {
                notifications.Remove(notification);
            }
        }

        public void RegisterTarget(IntPtr hwnd, IDragDropTarget target)
        {
            if (hwnd == IntPtr.Zero || target == null)
            {
                return;
            }

            lock (syncRoot)
            {
                targets[hwnd] = target;
            }
        }

        public void UnregisterTarget(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            lock (syncRoot)
            {
                targets.Remove(hwnd);
            }
        }

        public void BeginDrag(DragBeginRequest request)
        {
            if (request == null || request.InitialWindowHandle == IntPtr.Zero)
            {
                return;
            }

            DragAction actionToStart = null;
            lock (syncRoot)
            {
                ThrowIfDisposed();
                if (activeDragAction != null)
                {
                    return;
                }

                actionToStart = new DragAction(
                    parent,
                    request,
                    new Dictionary<IntPtr, IDragDropTarget>(targets),
                    new List<IDragDropNotification>(notifications),
                    OnDragActionCompleted);
                activeDragAction = actionToStart;
            }

            actionToStart.Start();
        }

        public void Dispose()
        {
            DragAction actionToDispose = null;

            lock (syncRoot)
            {
                if (isDisposed)
                {
                    return;
                }

                isDisposed = true;
                actionToDispose = activeDragAction;
                activeDragAction = null;
                targets.Clear();
                notifications.Clear();
            }

            actionToDispose?.Dispose();
        }

        private void OnDragActionCompleted(DragAction completedAction)
        {
            lock (syncRoot)
            {
                if (ReferenceEquals(activeDragAction, completedAction))
                {
                    activeDragAction = null;
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(ManagedDragDropController));
            }
        }

        private sealed class DragAction : IDisposable
        {
            private readonly IDragDropParent parent;
            private readonly DragBeginRequest request;
            private readonly IReadOnlyDictionary<IntPtr, IDragDropTarget> targets;
            private readonly IReadOnlyCollection<IDragDropNotification> notifications;
            private readonly Action<DragAction> onCompleted;
            private readonly Timer timer;
            private readonly DragCaptureWindow captureWindow;
            private DragPreviewWindow previewWindow;
            private IDragState state;
            private Point currentScreenPoint;
            private bool isDisposed;

            public DragAction(
                IDragDropParent parent,
                DragBeginRequest request,
                IReadOnlyDictionary<IntPtr, IDragDropTarget> targets,
                IReadOnlyCollection<IDragDropNotification> notifications,
                Action<DragAction> onCompleted)
            {
                this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
                this.request = request ?? throw new ArgumentNullException(nameof(request));
                this.targets = targets ?? throw new ArgumentNullException(nameof(targets));
                this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
                this.onCompleted = onCompleted ?? throw new ArgumentNullException(nameof(onCompleted));
                timer = new Timer
                {
                    Interval = 50
                };
                timer.Tick += OnTimerTick;
                captureWindow = new DragCaptureWindow(OnWindowMessage);
                currentScreenPoint = request.InitialScreenPoint;
            }

            public void Start()
            {
                captureWindow.SetCapture();
                timer.Start();
                SetState(new DragDetectingState(request.InitialScreenPoint, BeginDragCapture));
            }

            public void Dispose()
            {
                if (isDisposed)
                {
                    return;
                }

                isDisposed = true;
                timer.Stop();
                timer.Tick -= OnTimerTick;
                timer.Dispose();
                captureWindow.Dispose();
                previewWindow?.Dispose();
                state?.Dispose();
            }

            private void OnTimerTick(object sender, EventArgs e)
            {
                if (!captureWindow.HasCapture || !IsLeftMouseButtonDown())
                {
                    CompleteDrag(currentScreenPoint);
                }
            }

            private void OnWindowMessage(int message)
            {
                var point = Control.MousePosition;
                currentScreenPoint = point;

                switch (message)
                {
                    case BemoWindowMessages.WM_MOUSEMOVE:
                        if (!IsLeftMouseButtonDown())
                        {
                            CompleteDrag(point);
                            return;
                        }

                        state?.OnMouseMove(point);
                        break;
                    case BemoWindowMessages.WM_MOUSELEAVE:
                    case BemoWindowMessages.WM_LBUTTONUP:
                        CompleteDrag(point);
                        break;
                }
            }

            private void BeginDragCapture()
            {
                previewWindow = CreatePreviewWindow();

                foreach (var notification in notifications)
                {
                    notification.OnDragBegin();
                }

                parent.OnDragBegin();
                EnterTarget(request.InitialWindowHandle, request.InitialScreenPoint, true);
            }

            private void EnterTarget(IntPtr hwnd, Point screenPoint, bool isInitial)
            {
                if (!targets.TryGetValue(hwnd, out var target))
                {
                    EnterFloatingState();
                    return;
                }

                var clientPoint = ScreenToClient(hwnd, screenPoint);
                if (target.OnDragEnter(request.Data, clientPoint))
                {
                    SetState(new DragCapturedState(target, hwnd, screenPoint, MoveWithinCapturedTarget));
                    return;
                }

                if (isInitial)
                {
                    target.OnDragExit();
                }

                EnterFloatingState();
            }

            private void MoveWithinCapturedTarget(IDragDropTarget target, IntPtr hwnd, Point screenPoint)
            {
                var dragBounds = BemoWin32Helper.GetWindowRectangle(hwnd);
                dragBounds.Inflate(0, 20);
                if (dragBounds.Contains(screenPoint))
                {
                    target.OnDragMove(ScreenToClient(hwnd, screenPoint));
                    return;
                }

                target.OnDragExit();
                var nextTarget = BemoWin32Helper.GetTopLevelWindowFromPoint(screenPoint);
                if (targets.ContainsKey(nextTarget))
                {
                    EnterTarget(nextTarget, screenPoint, false);
                    return;
                }

                EnterFloatingState();
            }

            private void EnterFloatingState()
            {
                if (previewWindow == null)
                {
                    previewWindow = CreatePreviewWindow();
                }

                SetState(new DragFloatingState(request.ImageOffset, MoveWhileFloating));
            }

            private void MoveWhileFloating(Point screenPoint)
            {
                if (previewWindow == null)
                {
                    return;
                }

                previewWindow.SetVisible(false);
                var targetHwnd = BemoWin32Helper.GetTopLevelWindowFromPoint(screenPoint);
                if (targets.ContainsKey(targetHwnd))
                {
                    EnterTarget(targetHwnd, screenPoint, false);
                    return;
                }

                previewWindow.SetLocation(screenPoint);
                previewWindow.SetVisible(true);
            }

            private void CompleteDrag(Point screenPoint)
            {
                if (isDisposed)
                {
                    return;
                }

                var finalState = state;
                Dispose();

                switch (finalState)
                {
                    case DragDetectingState _:
                        break;
                    case DragCapturedState _:
                        ((DragCapturedState)finalState).Drop(request.Data, screenPoint);
                        foreach (var target in targets.Values)
                        {
                            target.OnDragEnd();
                        }

                        foreach (var notification in notifications)
                        {
                            notification.OnDragEnd();
                        }

                        parent.OnDragEnd();
                        break;
                    case DragFloatingState _:
                        foreach (var target in targets.Values)
                        {
                            target.OnDragEnd();
                        }

                        foreach (var notification in notifications)
                        {
                            notification.OnDragEnd();
                        }

                        parent.OnDragDrop(screenPoint, request.Data);
                        parent.OnDragEnd();
                        break;
                }

                onCompleted(this);
            }

            private void SetState(IDragState nextState)
            {
                state?.Dispose();
                state = nextState;
            }

            private DragPreviewWindow CreatePreviewWindow()
            {
                var image = request.CreateImage?.Invoke();
                if (image == null)
                {
                    return new DragPreviewWindow();
                }

                return new DragPreviewWindow(ScalePreviewImage(image));
            }

            private static Bitmap ScalePreviewImage(Bitmap image)
            {
                return BemoWin32Helper.ScaleImage(image, 2.0, 2.0);
            }

            private static bool IsLeftMouseButtonDown()
            {
                return BemoWin32Helper.IsKeyPressed(BemoVirtualKeyCodes.VK_LBUTTON);
            }

            private static Point ScreenToClient(IntPtr hwnd, Point point)
            {
                var nativePoint = BemoWin32Helper.ScreenToClient(hwnd, BemoPoint.FromPoint(point));
                return nativePoint.ToPoint();
            }
        }

        private interface IDragState : IDisposable
        {
            void OnMouseMove(Point screenPoint);
        }

        private sealed class DragDetectingState : IDragState
        {
            private readonly Point initialPoint;
            private readonly Action onBegin;

            public DragDetectingState(Point initialPoint, Action onBegin)
            {
                this.initialPoint = initialPoint;
                this.onBegin = onBegin ?? throw new ArgumentNullException(nameof(onBegin));
            }

            public void OnMouseMove(Point screenPoint)
            {
                var deltaX = screenPoint.X - initialPoint.X;
                var deltaY = screenPoint.Y - initialPoint.Y;
                var distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
                if (distance > 5.0)
                {
                    onBegin();
                }
            }

            public void Dispose()
            {
            }
        }

        private sealed class DragCapturedState : IDragState
        {
            private readonly IDragDropTarget target;
            private readonly IntPtr targetWindowHandle;
            private readonly Action<IDragDropTarget, IntPtr, Point> onMove;

            public DragCapturedState(
                IDragDropTarget target,
                IntPtr targetWindowHandle,
                Point initialPoint,
                Action<IDragDropTarget, IntPtr, Point> onMove)
            {
                this.target = target ?? throw new ArgumentNullException(nameof(target));
                this.targetWindowHandle = targetWindowHandle;
                this.onMove = onMove ?? throw new ArgumentNullException(nameof(onMove));
            }

            public void OnMouseMove(Point screenPoint)
            {
                onMove(target, targetWindowHandle, screenPoint);
            }

            public void Drop(object data, Point screenPoint)
            {
                var nativePoint = BemoWin32Helper.ScreenToClient(targetWindowHandle, BemoPoint.FromPoint(screenPoint));
                target.OnDrop(data, nativePoint.ToPoint());
            }

            public void Dispose()
            {
            }
        }

        private sealed class DragFloatingState : IDragState
        {
            private readonly Point imageOffset;
            private readonly Action<Point> onMove;

            public DragFloatingState(Point imageOffset, Action<Point> onMove)
            {
                this.imageOffset = imageOffset;
                this.onMove = onMove ?? throw new ArgumentNullException(nameof(onMove));
            }

            public void OnMouseMove(Point screenPoint)
            {
                onMove(new Point(
                    screenPoint.X - (imageOffset.X / 2),
                    screenPoint.Y - (imageOffset.Y / 2)));
            }

            public void Dispose()
            {
            }
        }

        private sealed class DragCaptureWindow : NativeWindow, IDisposable
        {
            private readonly Action<int> messageHandler;
            private bool isDisposed;

            public DragCaptureWindow(Action<int> messageHandler)
            {
                this.messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
                CreateHandle(new CreateParams
                {
                    Caption = "WindowTabs.CSharp.DragCaptureWindow"
                });
            }

            public bool HasCapture => BemoWinUserApi.GetCapture() == Handle;

            public void SetCapture()
            {
                BemoWinUserApi.SetCapture(Handle);
            }

            public void Dispose()
            {
                if (isDisposed)
                {
                    return;
                }

                isDisposed = true;
                if (HasCapture)
                {
                    BemoWinUserApi.ReleaseCapture();
                }

                DestroyHandle();
            }

            protected override void WndProc(ref Message m)
            {
                messageHandler(m.Msg);
                base.WndProc(ref m);
            }
        }

        private sealed class DragPreviewWindow : Form
        {
            private readonly PictureBox pictureBox;

            public DragPreviewWindow()
            {
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.Manual;
                TopMost = true;
                Opacity = 0.66d;

                pictureBox = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    SizeMode = PictureBoxSizeMode.StretchImage
                };

                Controls.Add(pictureBox);
            }

            public DragPreviewWindow(Bitmap image)
                : this()
            {
                SetImage(image);
            }

            protected override CreateParams CreateParams
            {
                get
                {
                    var createParams = base.CreateParams;
                    createParams.ExStyle |= BemoWindowsExtendedStyles.WS_EX_TOOLWINDOW;
                    return createParams;
                }
            }

            protected override bool ShowWithoutActivation => true;

            public void SetImage(Bitmap image)
            {
                pictureBox.Image = image;
                ClientSize = image?.Size ?? Size.Empty;
            }

            public void SetLocation(Point screenPoint)
            {
                Location = screenPoint;
            }

            public void SetVisible(bool isVisible)
            {
                if (isVisible)
                {
                    if (!Visible)
                    {
                        Show();
                    }
                }
                else if (Visible)
                {
                    Hide();
                }
            }
        }
    }
}
