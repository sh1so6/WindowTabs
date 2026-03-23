namespace WindowTabs.CSharp.Models
{
    internal sealed class ProcessSnapshot
    {
        public int ProcessId { get; set; }
        public bool CanQueryProcess { get; set; }
        public bool IsCurrentProcess { get; set; }
        public string ProcessPath { get; set; } = string.Empty;
        public string ExeName { get; set; } = string.Empty;
    }
}
