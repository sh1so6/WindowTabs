using System;
using System.Collections.Generic;
using System.Linq;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripDragStateService
    {
        private readonly ManagedGroupStripLayoutService stripLayoutService;

        public ManagedGroupStripDragStateService(ManagedGroupStripLayoutService stripLayoutService)
        {
            this.stripLayoutService = stripLayoutService ?? throw new ArgumentNullException(nameof(stripLayoutService));
        }

        public ManagedGroupStripDropTargetChange UpdateDropTarget(
            ManagedGroupStripDropState currentState,
            IntPtr nextTargetWindowHandle,
            bool nextInsertAfterTarget)
        {
            var nextState = new ManagedGroupStripDropState(nextTargetWindowHandle, nextInsertAfterTarget);
            if (currentState.TargetWindowHandle == nextState.TargetWindowHandle
                && currentState.InsertAfterTarget == nextState.InsertAfterTarget)
            {
                return new ManagedGroupStripDropTargetChange(
                    hasChanged: false,
                    dropState: currentState,
                    invalidatedWindowHandles: Array.Empty<IntPtr>());
            }

            var invalidatedHandles = new[] { currentState.TargetWindowHandle, nextState.TargetWindowHandle }
                .Where(handle => handle != IntPtr.Zero)
                .Distinct()
                .ToArray();

            return new ManagedGroupStripDropTargetChange(
                hasChanged: true,
                dropState: nextState,
                invalidatedWindowHandles: invalidatedHandles);
        }

        public List<IntPtr> BuildPreviewWindowHandles(
            IntPtr draggedWindowHandle,
            bool groupExists,
            IReadOnlyList<IntPtr> actualWindowHandles,
            IntPtr? insertAfterWindowHandle)
        {
            if (draggedWindowHandle == IntPtr.Zero || !groupExists)
            {
                return null;
            }

            return stripLayoutService.BuildPreviewOrder(actualWindowHandles, draggedWindowHandle, insertAfterWindowHandle);
        }
    }
}
