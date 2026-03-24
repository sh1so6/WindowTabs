using System;

namespace WindowTabs.CSharp.Services
{
    internal sealed class DesktopRuntimeSelection
    {
        public string RequestedRuntime { get; private set; } = "legacy";

        public string ActiveRuntime { get; private set; } = string.Empty;

        public string FallbackReason { get; private set; } = string.Empty;

        public string ExceptionType { get; private set; } = string.Empty;

        public bool UsedFallback => !string.IsNullOrWhiteSpace(FallbackReason);

        public void SetRequestedRuntime(string requestedRuntime)
        {
            RequestedRuntime = string.IsNullOrWhiteSpace(requestedRuntime)
                ? "legacy"
                : requestedRuntime.Trim();
        }

        public void SetActiveRuntime(string activeRuntime)
        {
            ActiveRuntime = activeRuntime ?? string.Empty;
        }

        public void SetFallback(Exception exception, string fallbackRuntime)
        {
            ActiveRuntime = fallbackRuntime ?? string.Empty;
            FallbackReason = exception?.Message ?? "Unknown runtime activation failure.";
            ExceptionType = exception?.GetType().Name ?? string.Empty;
        }
    }
}
