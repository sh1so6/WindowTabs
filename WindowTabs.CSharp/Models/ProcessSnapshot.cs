namespace WindowTabs.CSharp.Models
{
    internal sealed class ProcessSnapshot
    {
        public ProcessSnapshot()
            : this(0, false, false, string.Empty, string.Empty)
        {
        }

        public ProcessSnapshot(
            int processId,
            bool canQueryProcess,
            bool isCurrentProcess,
            string processPath,
            string exeName)
        {
            ProcessId = processId;
            CanQueryProcess = canQueryProcess;
            IsCurrentProcess = isCurrentProcess;
            ProcessPath = processPath ?? string.Empty;
            ExeName = exeName ?? string.Empty;
        }

        public int ProcessId { get; }

        public bool CanQueryProcess { get; }

        public bool IsCurrentProcess { get; }

        public string ProcessPath { get; }

        public string ExeName { get; }
    }
}
