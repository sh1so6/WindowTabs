using System;
using System.Collections.Generic;

namespace WindowTabs.CSharp.Contracts
{
    internal interface IDesktopRuntime
    {
        bool IsDragging { get; }

        IReadOnlyList<IWindowGroupRuntime> Groups { get; }

        IWindowGroupRuntime CreateGroup(IntPtr? preferredHandle);

        IWindowGroupRuntime FindGroup(IntPtr groupHandle);

        IWindowGroupRuntime FindGroupContainingWindow(IntPtr windowHandle);

        bool IsWindowGrouped(IntPtr windowHandle);

        void DestroyGroup(IntPtr groupHandle);

        IntPtr? RemoveWindow(IntPtr windowHandle);

        void RemoveClosedWindows(ISet<IntPtr> activeWindowHandles);
    }
}
