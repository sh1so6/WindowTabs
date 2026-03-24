using System;
using System.Collections.Generic;
using System.Linq;
using Bemo;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyDesktopRuntime : IDesktopRuntime, IDesktopRuntimeBootstrapper, IDisposable
    {
        private readonly LegacyDesktopAdapter desktopAdapter;
        private readonly LegacyDesktopNotificationRouter notificationRouter;
        private readonly LegacyProgramBridge programBridge;

        public LegacyDesktopRuntime(
            LegacyDesktopHostFactory hostFactory,
            LegacyWindowGroupRuntimeFactory groupRuntimeFactory)
        {
            if (hostFactory == null)
            {
                throw new ArgumentNullException(nameof(hostFactory));
            }

            if (groupRuntimeFactory == null)
            {
                throw new ArgumentNullException(nameof(groupRuntimeFactory));
            }

            var host = hostFactory.Create();
            programBridge = host.Services.ProgramBridge;
            notificationRouter = host.NotificationRouter;
            desktopAdapter = new LegacyDesktopAdapter(host.Desktop, groupRuntimeFactory);
        }

        public bool IsDragging => desktopAdapter.IsDragging;

        public IReadOnlyList<IWindowGroupRuntime> Groups => desktopAdapter.GetGroups();

        public IWindowGroupRuntime CreateGroup(IntPtr? preferredHandle)
        {
            return desktopAdapter.CreateGroup();
        }

        public IWindowGroupRuntime FindGroup(IntPtr groupHandle)
        {
            return desktopAdapter.FindGroup(groupHandle);
        }

        public IWindowGroupRuntime FindGroupContainingWindow(IntPtr windowHandle)
        {
            return desktopAdapter.FindGroupContainingWindow(windowHandle);
        }

        public bool IsWindowGrouped(IntPtr windowHandle)
        {
            return desktopAdapter.IsWindowGrouped(windowHandle);
        }

        public void DestroyGroup(IntPtr groupHandle)
        {
            desktopAdapter.DestroyGroup(groupHandle);
        }

        public IntPtr? RemoveWindow(IntPtr windowHandle)
        {
            return desktopAdapter.RemoveWindow(windowHandle);
        }

        public void RemoveClosedWindows(ISet<IntPtr> activeWindowHandles)
        {
            desktopAdapter.RemoveClosedWindows(activeWindowHandles);
        }

        public void SetDroppedWindowHandler(Action<IntPtr> handler)
        {
            notificationRouter.SetDroppedWindowHandler(handler);
        }

        public void Initialize(DesktopSessionCoordinator desktopSessionCoordinator, DesktopMonitoringService desktopMonitoringService)
        {
            if (desktopSessionCoordinator == null)
            {
                throw new ArgumentNullException(nameof(desktopSessionCoordinator));
            }

            if (desktopMonitoringService == null)
            {
                throw new ArgumentNullException(nameof(desktopMonitoringService));
            }

            programBridge.SetRefreshAction(trigger => desktopMonitoringService.RefreshNow(trigger));
            SetDroppedWindowHandler(desktopSessionCoordinator.MarkDropped);
        }

        public void Dispose()
        {
            desktopAdapter.DestroyAllGroups();
        }
    }
}
