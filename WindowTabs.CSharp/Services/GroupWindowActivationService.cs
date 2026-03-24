using System;
using Bemo;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class GroupWindowActivationService
    {
        private readonly DesktopSnapshotService desktopSnapshotService;
        private readonly IDesktopRuntime desktopRuntime;
        private readonly GroupVisualOrderService groupVisualOrderService;

        public GroupWindowActivationService(
            DesktopSnapshotService desktopSnapshotService,
            IDesktopRuntime desktopRuntime,
            GroupVisualOrderService groupVisualOrderService)
        {
            this.desktopSnapshotService = desktopSnapshotService ?? throw new ArgumentNullException(nameof(desktopSnapshotService));
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
            this.groupVisualOrderService = groupVisualOrderService ?? throw new ArgumentNullException(nameof(groupVisualOrderService));
        }

        public bool ActivateWindow(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return false;
            }

            WinUserApi.SetForegroundWindow(windowHandle);
            return true;
        }

        public bool TryActivateForegroundRelative(bool moveNext)
        {
            var foregroundWindowHandle = desktopSnapshotService.GetForegroundWindowHandle();
            var group = desktopRuntime.FindGroupContainingWindow(foregroundWindowHandle);
            if (group == null)
            {
                return false;
            }

            var orderedHandles = groupVisualOrderService.OrderWindowHandles(group);
            if (orderedHandles.Count <= 1)
            {
                return false;
            }

            var currentIndex = orderedHandles.IndexOf(foregroundWindowHandle);
            if (currentIndex < 0)
            {
                return false;
            }

            var targetIndex = moveNext
                ? (currentIndex + 1) % orderedHandles.Count
                : (currentIndex - 1 + orderedHandles.Count) % orderedHandles.Count;
            return ActivateWindow(orderedHandles[targetIndex]);
        }

        public bool TryActivateForegroundIndex(int index)
        {
            if (index < 0)
            {
                return false;
            }

            var foregroundWindowHandle = desktopSnapshotService.GetForegroundWindowHandle();
            var group = desktopRuntime.FindGroupContainingWindow(foregroundWindowHandle);
            if (group == null)
            {
                return false;
            }

            var orderedHandles = groupVisualOrderService.OrderWindowHandles(group);
            if (index >= orderedHandles.Count)
            {
                return false;
            }

            return ActivateWindow(orderedHandles[index]);
        }
    }
}
