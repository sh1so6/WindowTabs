using System;
using System.Collections.Generic;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class GroupVisualOrderService
    {
        private readonly WindowPresentationStateStore windowPresentationStateStore;

        public GroupVisualOrderService(WindowPresentationStateStore windowPresentationStateStore)
        {
            this.windowPresentationStateStore = windowPresentationStateStore ?? throw new ArgumentNullException(nameof(windowPresentationStateStore));
        }

        public List<IntPtr> OrderWindowHandles(IWindowGroupRuntime group)
        {
            if (group == null)
            {
                throw new ArgumentNullException(nameof(group));
            }

            var orderedHandles = new List<IntPtr>();
            var rightAlignedHandles = new List<IntPtr>();
            foreach (var hwnd in group.WindowHandles)
            {
                if (ResolveAlignment(hwnd, group) == TabAlign.TopRight)
                {
                    rightAlignedHandles.Add(hwnd);
                }
                else
                {
                    orderedHandles.Add(hwnd);
                }
            }

            orderedHandles.AddRange(rightAlignedHandles);
            return orderedHandles;
        }

        public TabAlign ResolveAlignment(IntPtr windowHandle, IWindowGroupRuntime group)
        {
            if (windowPresentationStateStore.TryGetAlignment(windowHandle, out var alignment))
            {
                return alignment;
            }

            return string.Equals(group.TabPosition, "TopLeft", StringComparison.OrdinalIgnoreCase)
                ? TabAlign.TopLeft
                : TabAlign.TopRight;
        }
    }
}
