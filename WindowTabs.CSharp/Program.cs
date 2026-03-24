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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using var serviceProvider = new ServiceCollection()
                .AddWindowTabsMigrationServices()
                .BuildServiceProvider();

            serviceProvider.GetRequiredService<AppBootstrapper>().Initialize();

            using (var form = serviceProvider.GetRequiredService<MigrationShellForm>())
            {
                Application.Run(form);
            }
        }
    }
}
