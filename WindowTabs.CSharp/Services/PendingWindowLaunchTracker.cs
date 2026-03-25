using System;
using System.Collections.Generic;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class PendingWindowLaunchTracker
    {
        private static readonly TimeSpan MatchWindow = TimeSpan.FromSeconds(30);
        private readonly object syncRoot = new object();
        private readonly Dictionary<string, PendingWindowLaunch> pendingByProcessPath =
            new(StringComparer.OrdinalIgnoreCase);

        public void Register(string processPath, IntPtr groupHandle, IntPtr invokerHandle)
        {
            if (string.IsNullOrWhiteSpace(processPath))
            {
                return;
            }

            lock (syncRoot)
            {
                pendingByProcessPath[processPath] = new PendingWindowLaunch
                {
                    GroupHandle = groupHandle,
                    InvokerHandle = invokerHandle,
                    CreatedAtUtc = DateTime.UtcNow
                };
            }
        }

        public PendingWindowLaunch TryConsume(string processPath)
        {
            if (string.IsNullOrWhiteSpace(processPath))
            {
                return null;
            }

            lock (syncRoot)
            {
                if (!pendingByProcessPath.TryGetValue(processPath, out var pending))
                {
                    return null;
                }

                pendingByProcessPath.Remove(processPath);
                if (DateTime.UtcNow - pending.CreatedAtUtc > MatchWindow)
                {
                    return null;
                }

                return pending;
            }
        }

        public int Count
        {
            get
            {
                lock (syncRoot)
                {
                    return pendingByProcessPath.Count;
                }
            }
        }
    }
}
