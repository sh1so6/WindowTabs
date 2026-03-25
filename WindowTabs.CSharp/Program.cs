using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using WindowTabs.CSharp.Services;
using WindowTabs.CSharp.UI;

namespace WindowTabs.CSharp
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            UnhandledExceptionLogger.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                using var serviceProvider = new ServiceCollection()
                    .AddWindowTabsServices()
                    .BuildServiceProvider();

                serviceProvider.GetRequiredService<AppBootstrapper>().Initialize();

                using var form = serviceProvider.GetRequiredService<WindowTabsShellForm>();
                Application.Run(form);
            }
            catch (Exception exception)
            {
                UnhandledExceptionLogger.Log(exception, "Program.Main");
                MessageBox.Show(
                    "WindowTabs crashed.\r\n\r\nLog: " + UnhandledExceptionLogger.LogFilePath,
                    "WindowTabs",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                throw;
            }
        }
    }
}
