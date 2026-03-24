using System;

namespace WindowTabs.CSharp.Services
{
    internal sealed class DesktopRuntimeOptions
    {
        public DesktopRuntimeOptions()
        {
            RequestedRuntime = ReadRequestedRuntime();
        }

        public string RequestedRuntime { get; }

        public bool UseManagedRuntime =>
            string.Equals(RequestedRuntime, "managed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(RequestedRuntime, "csharp", StringComparison.OrdinalIgnoreCase);

        private static string ReadRequestedRuntime()
        {
            var value = Environment.GetEnvironmentVariable("WINDOWTABS_RUNTIME");
            return string.IsNullOrWhiteSpace(value) ? "managed" : value.Trim();
        }
    }
}
