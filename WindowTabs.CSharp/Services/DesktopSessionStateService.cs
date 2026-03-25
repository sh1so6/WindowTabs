using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowTabs.CSharp.Services
{
    internal sealed class DesktopSessionStateService
    {
        private static readonly TimeSpan DroppedWindowCooldown = TimeSpan.FromSeconds(2);
        private readonly HashSet<IntPtr> subscribedHandles = new HashSet<IntPtr>();
        private readonly Dictionary<IntPtr, DateTime> droppedWindowHandles = new Dictionary<IntPtr, DateTime>();

        public ISet<IntPtr> GetSubscribedHandles()
        {
            return new HashSet<IntPtr>(subscribedHandles);
        }

        public ISet<IntPtr> GetActiveDroppedWindowHandles()
        {
            return new HashSet<IntPtr>(droppedWindowHandles.Keys);
        }

        public void MarkDropped(IntPtr windowHandle)
        {
            if (windowHandle != IntPtr.Zero)
            {
                droppedWindowHandles[windowHandle] = DateTime.UtcNow.Add(DroppedWindowCooldown);
            }
        }

        public void RegisterSubscriptions(IEnumerable<IntPtr> windowHandles)
        {
            foreach (var handle in windowHandles ?? Array.Empty<IntPtr>())
            {
                if (handle != IntPtr.Zero)
                {
                    subscribedHandles.Add(handle);
                }
            }
        }

        public void ClearDropped(IEnumerable<IntPtr> windowHandles)
        {
            foreach (var handle in windowHandles ?? Array.Empty<IntPtr>())
            {
                if (handle != IntPtr.Zero)
                {
                    droppedWindowHandles.Remove(handle);
                }
            }
        }

        public void ForgetWindow(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            subscribedHandles.Remove(windowHandle);
            droppedWindowHandles.Remove(windowHandle);
        }

        public void RemoveClosedWindows(ISet<IntPtr> activeHandles)
        {
            if (activeHandles == null)
            {
                return;
            }

            subscribedHandles.RemoveWhere(handle => !activeHandles.Contains(handle));
            foreach (var staleHandle in droppedWindowHandles.Keys.Where(handle => !activeHandles.Contains(handle)).ToArray())
            {
                droppedWindowHandles.Remove(staleHandle);
            }
        }

        public void CleanupExpiredDroppedWindows()
        {
            var now = DateTime.UtcNow;
            foreach (var expiredHandle in droppedWindowHandles
                         .Where(pair => pair.Value <= now)
                         .Select(pair => pair.Key)
                         .ToArray())
            {
                droppedWindowHandles.Remove(expiredHandle);
            }
        }
    }
}
