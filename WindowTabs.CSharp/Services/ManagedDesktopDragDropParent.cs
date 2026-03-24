using System;
using System.Drawing;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedDesktopDragDropParent : IDragDropParent
    {
        private readonly ManagedDesktopInteractionState interactionState;
        private readonly WindowDetachService windowDetachService;

        public ManagedDesktopDragDropParent(
            ManagedDesktopInteractionState interactionState,
            WindowDetachService windowDetachService)
        {
            this.interactionState = interactionState ?? throw new ArgumentNullException(nameof(interactionState));
            this.windowDetachService = windowDetachService ?? throw new ArgumentNullException(nameof(windowDetachService));
        }

        public void OnDragBegin()
        {
            interactionState.SetDragging(true);
        }

        public void OnDragDrop(Point screenPoint, object data)
        {
            windowDetachService.DetachWindowToPoint(screenPoint, data as TabDragInfo);
        }

        public void OnDragEnd()
        {
            interactionState.SetDragging(false);
        }
    }
}
