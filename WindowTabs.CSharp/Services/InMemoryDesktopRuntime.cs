using System;
using System.Collections.Generic;
using System.Linq;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class InMemoryDesktopRuntime : IDesktopRuntime
    {
        private readonly List<IWindowGroupRuntime> groups = new List<IWindowGroupRuntime>();
        private long nextSyntheticGroupHandle = -1;

        public bool IsDragging => false;

        public IReadOnlyList<IWindowGroupRuntime> Groups => groups;

        public IWindowGroupRuntime CreateGroup(IntPtr? preferredHandle)
        {
            var groupHandle = preferredHandle.HasValue && FindGroup(preferredHandle.Value) == null
                ? preferredHandle.Value
                : new IntPtr(nextSyntheticGroupHandle--);
            var group = new InMemoryWindowGroupRuntime(groupHandle);
            groups.Add(group);
            return group;
        }

        public IWindowGroupRuntime FindGroup(IntPtr groupHandle)
        {
            return groups.FirstOrDefault(group => group.GroupHandle == groupHandle);
        }

        public IWindowGroupRuntime FindGroupContainingWindow(IntPtr windowHandle)
        {
            return groups.FirstOrDefault(group => group.WindowHandles.Contains(windowHandle));
        }

        public bool IsWindowGrouped(IntPtr windowHandle)
        {
            return groups.Any(group => group.WindowHandles.Contains(windowHandle));
        }

        public void DestroyGroup(IntPtr groupHandle)
        {
            groups.RemoveAll(group => group.GroupHandle == groupHandle);
        }

        public IntPtr? RemoveWindow(IntPtr windowHandle)
        {
            var group = FindGroupContainingWindow(windowHandle);
            if (group == null)
            {
                return null;
            }

            var groupHandle = group.GroupHandle;
            group.RemoveWindow(windowHandle);
            if (group.WindowHandles.Count == 0)
            {
                DestroyGroup(groupHandle);
            }

            return groupHandle;
        }

        public void RemoveClosedWindows(ISet<IntPtr> activeWindowHandles)
        {
            foreach (var group in groups)
            {
                var staleHandles = new List<IntPtr>();
                foreach (var windowHandle in group.WindowHandles)
                {
                    if (!activeWindowHandles.Contains(windowHandle))
                    {
                        staleHandles.Add(windowHandle);
                    }
                }

                foreach (var staleHandle in staleHandles)
                {
                    group.RemoveWindow(staleHandle);
                }
            }

            groups.RemoveAll(group => group.WindowHandles.Count == 0);
        }
    }
}
