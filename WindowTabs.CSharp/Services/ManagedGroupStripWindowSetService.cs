using System;
using System.Collections.Generic;
using System.Linq;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripWindowSetService
    {
        private readonly IDesktopRuntime desktopRuntime;
        private readonly GroupVisualOrderService groupVisualOrderService;

        public ManagedGroupStripWindowSetService(
            IDesktopRuntime desktopRuntime,
            GroupVisualOrderService groupVisualOrderService)
        {
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
            this.groupVisualOrderService = groupVisualOrderService ?? throw new ArgumentNullException(nameof(groupVisualOrderService));
        }

        public bool TryGetWindowContext(
            IntPtr windowHandle,
            out IntPtr groupHandle,
            out IReadOnlyList<IntPtr> orderedWindowHandles,
            out int currentIndex)
        {
            groupHandle = IntPtr.Zero;
            orderedWindowHandles = Array.Empty<IntPtr>();
            currentIndex = -1;

            if (windowHandle == IntPtr.Zero)
            {
                return false;
            }

            var group = desktopRuntime.FindGroupContainingWindow(windowHandle);
            if (group == null)
            {
                return false;
            }

            var orderedHandles = groupVisualOrderService.OrderWindowHandles(group);
            currentIndex = orderedHandles.IndexOf(windowHandle);
            if (currentIndex < 0)
            {
                return false;
            }

            groupHandle = group.GroupHandle;
            orderedWindowHandles = orderedHandles;
            return true;
        }

        public IReadOnlyList<IntPtr> GetLeftWindowHandles(IReadOnlyList<IntPtr> orderedWindowHandles, int currentIndex)
        {
            return TakeRange(orderedWindowHandles, 0, currentIndex);
        }

        public IReadOnlyList<IntPtr> GetRightWindowHandles(IReadOnlyList<IntPtr> orderedWindowHandles, int currentIndex)
        {
            return TakeRange(
                orderedWindowHandles,
                currentIndex + 1,
                Math.Max(0, (orderedWindowHandles?.Count ?? 0) - currentIndex - 1));
        }

        public IReadOnlyList<IntPtr> GetOtherWindowHandles(IReadOnlyList<IntPtr> orderedWindowHandles, IntPtr currentWindowHandle)
        {
            return orderedWindowHandles?
                .Where(handle => handle != IntPtr.Zero && handle != currentWindowHandle)
                .ToList()
                ?? new List<IntPtr>();
        }

        public IReadOnlyList<IntPtr> GetLeftSplitWindowHandles(IReadOnlyList<IntPtr> orderedWindowHandles, int currentIndex)
        {
            return TakeRange(orderedWindowHandles, 0, currentIndex + 1);
        }

        public IReadOnlyList<IntPtr> GetRightSplitWindowHandles(IReadOnlyList<IntPtr> orderedWindowHandles, int currentIndex)
        {
            return TakeRange(
                orderedWindowHandles,
                currentIndex,
                Math.Max(0, (orderedWindowHandles?.Count ?? 0) - currentIndex));
        }

        private static IReadOnlyList<IntPtr> TakeRange(IReadOnlyList<IntPtr> orderedWindowHandles, int startIndex, int count)
        {
            if (orderedWindowHandles == null
                || count <= 0
                || startIndex < 0
                || startIndex >= orderedWindowHandles.Count)
            {
                return Array.Empty<IntPtr>();
            }

            return orderedWindowHandles
                .Skip(startIndex)
                .Take(count)
                .Where(handle => handle != IntPtr.Zero)
                .ToList();
        }
    }
}
