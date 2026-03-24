using System;
using System.Collections.Generic;
using System.Linq;
using Bemo;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyDesktopAdapter
    {
        private readonly IDesktop desktop;
        private readonly LegacyWindowGroupRuntimeFactory groupRuntimeFactory;

        public LegacyDesktopAdapter(IDesktop desktop, LegacyWindowGroupRuntimeFactory groupRuntimeFactory)
        {
            this.desktop = desktop ?? throw new ArgumentNullException(nameof(desktop));
            this.groupRuntimeFactory = groupRuntimeFactory ?? throw new ArgumentNullException(nameof(groupRuntimeFactory));
        }

        public bool IsDragging => desktop.isDragging;

        public IReadOnlyList<IWindowGroupRuntime> GetGroups()
        {
            return desktop.groups.list
                .Select(groupRuntimeFactory.Create)
                .Where(group => group != null)
                .ToList();
        }

        public IWindowGroupRuntime CreateGroup()
        {
            return groupRuntimeFactory.Create(desktop.createGroup(false));
        }

        public IWindowGroupRuntime FindGroup(IntPtr groupHandle)
        {
            return groupRuntimeFactory.Create(
                desktop.groups.list.FirstOrDefault(candidate => candidate.hwnd == groupHandle));
        }

        public IWindowGroupRuntime FindGroupContainingWindow(IntPtr windowHandle)
        {
            return groupRuntimeFactory.Create(
                desktop.groups.list.FirstOrDefault(candidate => candidate.windows.list.Contains(windowHandle)));
        }

        public bool IsWindowGrouped(IntPtr windowHandle)
        {
            return desktop.groups.list.Any(group => group.windows.list.Contains(windowHandle));
        }

        public void DestroyGroup(IntPtr groupHandle)
        {
            desktop.groups.list
                .FirstOrDefault(candidate => candidate.hwnd == groupHandle)
                ?.destroy();
        }

        public IntPtr? RemoveWindow(IntPtr windowHandle)
        {
            var group = desktop.groups.list.FirstOrDefault(candidate => candidate.windows.list.Contains(windowHandle));
            if (group == null)
            {
                return null;
            }

            group.removeWindow(windowHandle);
            return group.hwnd;
        }

        public void RemoveClosedWindows(ISet<IntPtr> activeWindowHandles)
        {
            foreach (var group in desktop.groups.list.ToList())
            {
                foreach (var windowHandle in group.windows.list.ToList())
                {
                    if (!activeWindowHandles.Contains(windowHandle))
                    {
                        group.removeWindow(windowHandle);
                    }
                }
            }
        }

        public void DestroyAllGroups()
        {
            foreach (var group in desktop.groups.list.ToList())
            {
                group.destroy();
            }
        }
    }
}
