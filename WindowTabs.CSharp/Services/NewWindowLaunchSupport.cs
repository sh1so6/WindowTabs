using System;
using System.ComponentModel;
using System.IO;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class NewWindowLaunchSupport
    {
        public LaunchCommand ResolveLaunchCommand(string processPath)
        {
            if (string.IsNullOrWhiteSpace(processPath))
            {
                return null;
            }

            if (processPath.IndexOf("WindowsApps", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var alternative = GetAlternativeLaunchCommand(processPath);
                if (alternative != null)
                {
                    return new LaunchCommand(alternative);
                }
            }

            return new LaunchCommand(processPath);
        }

        public string BuildLaunchFailureMessage(string processPath, Exception exception)
        {
            if (exception is Win32Exception && processPath.IndexOf("WindowsApps", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return BuildUwpLaunchFailureMessage(processPath);
            }

            return "Failed to start new tab:"
                   + Environment.NewLine
                   + LocalizationService.GetString("NewTab")
                   + Environment.NewLine
                   + Environment.NewLine
                   + "Path: " + processPath
                   + Environment.NewLine
                   + "Error: " + exception.Message;
        }

        private static string GetAlternativeLaunchCommand(string processPath)
        {
            var fileName = Path.GetFileName(processPath)?.ToLowerInvariant() ?? string.Empty;
            if (fileName.Contains("windowsterminal"))
            {
                return "wt.exe";
            }

            return null;
        }

        private string BuildUwpLaunchFailureMessage(string processPath)
        {
            var appName = Path.GetFileNameWithoutExtension(processPath);
            if (string.Equals(LocalizationService.CurrentLanguage, "Japanese", StringComparison.OrdinalIgnoreCase))
            {
                return "新規ウィンドウの起動に失敗しました。"
                       + Environment.NewLine
                       + Environment.NewLine
                       + $"このアプリケーション ({appName}) はUWPアプリのため、"
                       + Environment.NewLine
                       + "直接起動できません。"
                       + Environment.NewLine
                       + Environment.NewLine
                       + "代わりにスタートメニューから起動してください。";
            }

            return "Failed to start new window."
                   + Environment.NewLine
                   + Environment.NewLine
                   + $"This application ({appName}) is a UWP app and"
                   + Environment.NewLine
                   + "cannot be launched directly."
                   + Environment.NewLine
                   + Environment.NewLine
                   + "Please launch it from the Start menu instead.";
        }
    }
}
