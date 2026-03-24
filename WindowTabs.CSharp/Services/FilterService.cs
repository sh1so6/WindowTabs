using System;
using System.Collections.Generic;
using System.IO;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class FilterService
    {
        private readonly SettingsSession settingsSession;
        private readonly IProgramRefresher refresher;
        private readonly HashSet<string> blackListedExeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "taskmgr.exe"
        };

        public FilterService(SettingsSession settingsSession, IProgramRefresher refresher)
        {
            this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
            this.refresher = refresher ?? throw new ArgumentNullException(nameof(refresher));
        }

        public IReadOnlyCollection<string> IncludedPaths => settingsSession.Current.IncludedPaths;

        public IReadOnlyCollection<string> ExcludedPaths => settingsSession.Current.ExcludedPaths;

        public bool IsTabbingEnabledForAllProcessesByDefault
        {
            get => settingsSession.Current.EnableTabbingByDefault;
            set
            {
                settingsSession.Update(snapshot => snapshot.EnableTabbingByDefault = value);
                refresher.Refresh();
            }
        }

        public bool IsAppWindowStyle(WindowSnapshot window)
        {
            if (window == null)
            {
                return false;
            }

            var style = NativeWindowApi.WsOverlappedWindow
                        & ~NativeWindowApi.WsCaption
                        & ~NativeWindowApi.WsThickFrame
                        & ~NativeWindowApi.WsSysMenu;

            return (window.ExtendedStyle & NativeWindowApi.WsExToolWindow) == 0
                   && (window.Style & style) == style;
        }

        public bool IsAppWindow(WindowSnapshot window, RectValue screenRegion)
        {
            if (window == null || window.Process == null)
            {
                return false;
            }

            return window.Process.CanQueryProcess
                   && IsAppWindowStyle(window)
                   && !window.Process.IsCurrentProcess
                   && window.IsWindow
                   && window.IsVisibleOnScreen
                   && !window.IsTopMost
                   && IsValidOwner(window)
                   && !string.Equals(window.ClassName, "#32770", StringComparison.Ordinal)
                   && (!string.Equals(window.ClassName, "ApplicationFrameWindow", StringComparison.Ordinal) || !string.IsNullOrEmpty(window.Text))
                   && !IsBanned(window)
                   && IsOnScreenOrMinimized(window, screenRegion);
        }

        public bool IsTabbableWindow(WindowSnapshot window, RectValue screenRegion)
        {
            return window?.Process != null
                   && GetIsTabbingEnabledForProcess(window.Process.ProcessPath)
                   && IsAppWindow(window, screenRegion);
        }

        public bool GetIsTabbingEnabledForProcess(string processPath)
        {
            if (string.IsNullOrWhiteSpace(processPath))
            {
                return false;
            }

            if (IsTabbingEnabledForAllProcessesByDefault)
            {
                return !ContainsPath(settingsSession.Current.ExcludedPaths, processPath);
            }

            return ContainsPath(settingsSession.Current.IncludedPaths, processPath);
        }

        public void SetIsTabbingEnabledForProcess(string processPath, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(processPath))
            {
                return;
            }

            settingsSession.Update(snapshot =>
            {
                var included = new HashSet<string>(snapshot.IncludedPaths, StringComparer.OrdinalIgnoreCase);
                var excluded = new HashSet<string>(snapshot.ExcludedPaths, StringComparer.OrdinalIgnoreCase);

                if (snapshot.EnableTabbingByDefault)
                {
                    if (enabled)
                    {
                        excluded.Remove(processPath);
                    }
                    else
                    {
                        excluded.Add(processPath);
                    }
                }
                else
                {
                    if (enabled)
                    {
                        included.Add(processPath);
                    }
                    else
                    {
                        included.Remove(processPath);
                    }
                }

                snapshot.IncludedPaths = new List<string>(included);
                snapshot.ExcludedPaths = new List<string>(excluded);
            });

            refresher.Refresh();
        }

        private bool IsBanned(WindowSnapshot window)
        {
            return blackListedExeNames.Contains(window.Process.ExeName)
                   || string.Equals(Path.GetFileName(window.Process.ProcessPath), "taskmgr.exe", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsValidOwner(WindowSnapshot window)
        {
            return window.ParentBounds == null || window.ParentBounds.Width == 0;
        }

        private static bool IsOnScreenOrMinimized(WindowSnapshot window, RectValue screenRegion)
        {
            return window.IsMinimized || (screenRegion?.ContainsRect(window.Bounds) ?? false);
        }

        private static bool ContainsPath(IEnumerable<string> paths, string processPath)
        {
            foreach (var path in paths)
            {
                if (string.Equals(path, processPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
