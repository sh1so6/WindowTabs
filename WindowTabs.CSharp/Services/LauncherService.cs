using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LauncherService
    {
        private readonly object syncRoot = new object();
        private readonly Dictionary<IntPtr, HashSet<int>> pendingByGroup = new Dictionary<IntPtr, HashSet<int>>();

        public void Launch(IntPtr groupHandle, IEnumerable<LaunchCommand> commands)
        {
            if (commands == null)
            {
                return;
            }

            var launchedPids = new HashSet<int>();
            foreach (var command in commands)
            {
                if (command == null || string.IsNullOrWhiteSpace(command.FileName))
                {
                    continue;
                }

                var startInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = command.FileName
                };

                if (!string.IsNullOrWhiteSpace(command.Arguments))
                {
                    startInfo.Arguments = command.Arguments;
                }

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        continue;
                    }

                    process.Refresh();
                    launchedPids.Add(process.Id);
                }
            }

            lock (syncRoot)
            {
                pendingByGroup[groupHandle] = launchedPids;
            }
        }

        public bool IsLaunching(IntPtr groupHandle)
        {
            lock (syncRoot)
            {
                return pendingByGroup.ContainsKey(groupHandle);
            }
        }

        public IntPtr? TryMatchLaunchedProcess(int processId)
        {
            lock (syncRoot)
            {
                foreach (var pair in pendingByGroup.ToList())
                {
                    if (!pair.Value.Remove(processId))
                    {
                        continue;
                    }

                    if (pair.Value.Count == 0)
                    {
                        pendingByGroup.Remove(pair.Key);
                    }

                    return pair.Key;
                }
            }

            return null;
        }
    }
}
