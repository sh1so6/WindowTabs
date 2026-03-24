using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripButtonState : IDisposable
    {
        private readonly Timer hoverTimer;
        private Action hoverActivateAction;

        public ManagedGroupStripButtonState(Button button)
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
}
