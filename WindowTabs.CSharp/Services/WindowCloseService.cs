using System;
using System.Collections.Generic;
using System.Linq;
using Bemo;

namespace WindowTabs.CSharp.Services
{
    internal sealed class WindowCloseService
    {
        public bool CloseWindow(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return false;
            }

            WinUserApi.PostMessage(
                windowHandle,
                WindowMessages.WM_SYSCOMMAND,
                new IntPtr(SystemMenuCommandValues.SC_CLOSE),
                IntPtr.Zero);
            return true;
        }

        public int CloseWindows(IEnumerable<IntPtr> windowHandles)
        {
            if (windowHandles == null)
            {
                return 0;
            }

            var count = 0;
            foreach (var windowHandle in windowHandles.Where(handle => handle != IntPtr.Zero).Distinct().ToArray())
            {
                if (CloseWindow(windowHandle))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
