using System;
using System.Text.RegularExpressions;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class WorkspaceWindowMatchService
    {
        public bool IsMatch(WindowSnapshot window, WorkspaceWindowLayout windowLayout)
        {
            if (window == null || windowLayout == null)
            {
                return false;
            }

            if (!MatchesProcessName(window.Process?.ExeName, windowLayout.Name))
            {
                return false;
            }

            return MatchesTitle(window.Text ?? string.Empty, windowLayout);
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
            switch (windowLayout.MatchType)
            {
                case WorkspaceWindowMatchType.ExactMatch:
                    if (string.IsNullOrEmpty(expectedTitle))
                    {
                        return true;
                    }

                    return string.Equals(title, expectedTitle, StringComparison.Ordinal);
                case WorkspaceWindowMatchType.StartsWith:
                    return title.StartsWith(expectedTitle, StringComparison.Ordinal);
                case WorkspaceWindowMatchType.EndsWith:
                    return title.EndsWith(expectedTitle, StringComparison.Ordinal);
                case WorkspaceWindowMatchType.Contains:
                    return title.Contains(expectedTitle);
                case WorkspaceWindowMatchType.RegEx:
                    try
                    {
                        return Regex.IsMatch(title, expectedTitle);
                    }
                    catch
                    {
                        return false;
                    }
                default:
                    return false;
            }
        }
    }
}
