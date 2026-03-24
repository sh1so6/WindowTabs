using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowTabs.CSharp.Services
{
    internal sealed class WindowCloseService
    {
        public bool CloseWindow(IntPtr windowHandle)
        {
            return NativeWindowApi.CloseWindow(windowHandle);
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
