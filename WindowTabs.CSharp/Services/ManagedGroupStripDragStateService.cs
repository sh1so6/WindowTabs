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
            IntPtr currentTargetWindowHandle,
            bool currentInsertAfterTarget,
            IntPtr nextTargetWindowHandle,
            bool nextInsertAfterTarget)
        {
            if (currentTargetWindowHandle == nextTargetWindowHandle
                && currentInsertAfterTarget == nextInsertAfterTarget)
            {
                return new ManagedGroupStripDropTargetChange(
                    hasChanged: false,
                    targetWindowHandle: currentTargetWindowHandle,
                    insertAfterTarget: currentInsertAfterTarget,
                    invalidatedWindowHandles: Array.Empty<IntPtr>());
            }

            var invalidatedHandles = new[] { currentTargetWindowHandle, nextTargetWindowHandle }
                .Where(handle => handle != IntPtr.Zero)
                .Distinct()
                .ToArray();

            return new ManagedGroupStripDropTargetChange(
                hasChanged: true,
                targetWindowHandle: nextTargetWindowHandle,
                insertAfterTarget: nextInsertAfterTarget,
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
