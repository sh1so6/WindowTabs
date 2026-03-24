using System;
using System.Collections.Generic;
using System.Linq;
using Bemo;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyWindowGroupRuntime : Contracts.IWindowGroupRuntime
    {
        private readonly IGroup group;
        private readonly LegacyGroupTabOrderService tabOrderService;

        public LegacyWindowGroupRuntime(IGroup group, LegacyGroupTabOrderService tabOrderService)
        {
            this.group = group ?? throw new ArgumentNullException(nameof(group));
            this.tabOrderService = tabOrderService ?? throw new ArgumentNullException(nameof(tabOrderService));
        }

        public IntPtr GroupHandle => group.hwnd;

        public IReadOnlyList<IntPtr> WindowHandles => group.lorder.list.ToList();

        public string TabPosition
        {
            get => group.perGroupTabPositionValue;
            set => group.perGroupTabPositionValue = value;
        }

        public bool SnapTabHeightMargin
        {
            get => group.snapTabHeightMargin;
            set => group.snapTabHeightMargin = value;
        }

        public void AddWindow(IntPtr windowHandle, IntPtr? insertAfterWindowHandle)
        {
            group.addWindow(windowHandle, false);
            if (!insertAfterWindowHandle.HasValue)
            {
                return;
            }

            tabOrderService.MoveWindowAfter(group, windowHandle, insertAfterWindowHandle.Value);
        }

        public void MoveWindowAfter(IntPtr windowHandle, IntPtr? insertAfterWindowHandle)
        {
            tabOrderService.MoveWindowAfter(group, windowHandle, insertAfterWindowHandle);
        }

        public void RemoveWindow(IntPtr windowHandle)
        {
            group.removeWindow(windowHandle);
        }

        public void SwitchWindow(bool next, bool force)
        {
            group.switchWindow(next, force);
        }

        public void ActivateWindowAt(int index, bool force)
        {
            var handles = WindowHandles;
            if (index < 0 || index >= handles.Count)
            {
                return;
            }

            if (handles.Count == 1 && !force)
            {
                return;
            }

            var targetHandle = handles[index];
            if (targetHandle != IntPtr.Zero)
            {
                NativeWindowApi.ActivateWindow(targetHandle);
            }
        }
    }
}
