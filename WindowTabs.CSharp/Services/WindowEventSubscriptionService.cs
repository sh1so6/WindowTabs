using System;
using System.Collections.Generic;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class WindowEventSubscriptionService : IDisposable
    {
        private readonly Dictionary<IntPtr, WindowEventHookSubscription> subscriptions =
            new Dictionary<IntPtr, WindowEventHookSubscription>();
        private readonly Action<IntPtr, WinObjectEventKind> eventHandler;
        private bool isDisposed;

        public WindowEventSubscriptionService(Action<IntPtr, WinObjectEventKind> eventHandler)
        {
            this.eventHandler = eventHandler ?? throw new ArgumentNullException(nameof(eventHandler));
        }

        public int SubscriptionCount => subscriptions.Count;

        public void SyncSubscriptions(IReadOnlyList<WindowSnapshot> windows, IEnumerable<IntPtr> handlesToSubscribe)
        {
            var activeHandles = new HashSet<IntPtr>();
            foreach (var window in windows)
            {
                activeHandles.Add(window.Handle);
            }

            var handlesToKeep = new HashSet<IntPtr>(activeHandles);
            foreach (var handle in handlesToSubscribe)
            {
                if (handle == IntPtr.Zero || subscriptions.ContainsKey(handle))
                {
                    continue;
                }

                subscriptions[handle] = new WindowEventHookSubscription(handle, eventHandler);
                handlesToKeep.Add(handle);
            }

            var staleHandles = new List<IntPtr>();
            foreach (var handle in subscriptions.Keys)
            {
                if (!handlesToKeep.Contains(handle))
                {
                    staleHandles.Add(handle);
                }
            }

            foreach (var handle in staleHandles)
            {
                subscriptions[handle].Dispose();
                subscriptions.Remove(handle);
            }
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            foreach (var subscription in subscriptions.Values)
            {
                subscription.Dispose();
            }

            subscriptions.Clear();
        }
    }
}
