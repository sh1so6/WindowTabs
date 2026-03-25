using System;
using System.Text.RegularExpressions;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class WorkspaceWindowMatchService
    {
        public bool IsMatch(WindowSnapshot window, WorkspaceWindowLayout windowLayout)
        {
            return window is not null
                && windowLayout is not null
                && MatchesProcessName(window.Process?.ExeName, windowLayout.Name)
                && MatchesTitle(window.Text ?? string.Empty, windowLayout);
        }

        private static bool MatchesProcessName(string processName, string expectedProcessName)
        {
            if (string.IsNullOrWhiteSpace(expectedProcessName))
            {
                return true;
            }

            return string.Equals(
                processName ?? string.Empty,
                expectedProcessName,
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesTitle(string title, WorkspaceWindowLayout windowLayout)
        {
            var expectedTitle = windowLayout.Title ?? string.Empty;
            return windowLayout.MatchType switch
            {
                WorkspaceWindowMatchType.ExactMatch when string.IsNullOrEmpty(expectedTitle) => true,
                WorkspaceWindowMatchType.ExactMatch => string.Equals(title, expectedTitle, StringComparison.Ordinal),
                WorkspaceWindowMatchType.StartsWith => title.StartsWith(expectedTitle, StringComparison.Ordinal),
                WorkspaceWindowMatchType.EndsWith => title.EndsWith(expectedTitle, StringComparison.Ordinal),
                WorkspaceWindowMatchType.Contains => title.Contains(expectedTitle),
                WorkspaceWindowMatchType.RegEx => MatchesRegularExpression(title, expectedTitle),
                _ => false
            };
        }

        private static bool MatchesRegularExpression(string title, string expectedTitle)
        {
            try
            {
                return Regex.IsMatch(title, expectedTitle);
            }
            catch
            {
                return false;
            }
        }
    }
}
