using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowTabs.CSharp.Services
{
    internal static class UnhandledExceptionLogger
    {
        private static readonly object SyncRoot = new object();
        private static bool initialized;

        public static string LogFilePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs", "unhandled-exceptions.log");

        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath) ?? AppDomain.CurrentDomain.BaseDirectory);

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        public static void Log(Exception exception, string source)
        {
            if (exception == null)
            {
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine("=== WindowTabs.CSharp Unhandled Exception ===");
            builder.AppendLine("Timestamp: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            builder.AppendLine("Source: " + source);
            builder.AppendLine("Runtime: " + Environment.Version);
            builder.AppendLine("OS: " + Environment.OSVersion);
            builder.AppendLine();
            builder.AppendLine(exception.ToString());
            builder.AppendLine();

            lock (SyncRoot)
            {
                File.AppendAllText(LogFilePath, builder.ToString(), Encoding.UTF8);
            }
        }

        private static void OnThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Log(e.Exception, "Application.ThreadException");
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log(e.ExceptionObject as Exception, "AppDomain.UnhandledException");
        }

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Log(e.Exception, "TaskScheduler.UnobservedTaskException");
        }
    }
}
