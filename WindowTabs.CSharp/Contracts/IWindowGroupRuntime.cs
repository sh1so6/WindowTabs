using System;
using System.Collections.Generic;

namespace WindowTabs.CSharp.Contracts
{
    internal interface IWindowGroupRuntime
    {
        IntPtr GroupHandle { get; }

        IReadOnlyList<IntPtr> WindowHandles { get; }

        string TabPosition { get; set; }

        bool SnapTabHeightMargin { get; set; }

        void AddWindow(IntPtr windowHandle, IntPtr? insertAfterWindowHandle);

        void MoveWindowAfter(IntPtr windowHandle, IntPtr? insertAfterWindowHandle);

        void RemoveWindow(IntPtr windowHandle);

        void SwitchWindow(bool next, bool force);
    }
}
