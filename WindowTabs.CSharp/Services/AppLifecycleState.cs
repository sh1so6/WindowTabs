using System.Windows.Forms;

namespace WindowTabs.CSharp.Services
{
    internal sealed class AppLifecycleState
    {
        public bool AllowExit { get; private set; }

        public void RequestExit()
        {
            AllowExit = true;
            Application.Exit();
        }
    }
}
