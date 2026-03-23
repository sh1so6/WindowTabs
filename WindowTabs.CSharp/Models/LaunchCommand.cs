namespace WindowTabs.CSharp.Models
{
    internal sealed class LaunchCommand
    {
        public LaunchCommand(string fileName, string arguments = null)
        {
            FileName = fileName;
            Arguments = arguments;
        }

        public string FileName { get; }

        public string Arguments { get; }
    }
}
