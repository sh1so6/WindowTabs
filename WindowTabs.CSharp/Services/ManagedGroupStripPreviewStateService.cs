using System;
using System.Collections.Generic;
using System.Linq;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripPreviewStateService
    {
        private readonly ManagedGroupStripLayoutService stripLayoutService;
        private readonly ManagedGroupStripDragStateService stripDragStateService;

        public ManagedGroupStripPreviewStateService(
            ManagedGroupStripLayoutService stripLayoutService,
            ManagedGroupStripDragStateService stripDragStateService)
        {
            this.stripLayoutService = stripLayoutService ?? throw new ArgumentNullException(nameof(stripLayoutService));
            this.stripDragStateService = stripDragStateService ?? throw new ArgumentNullException(nameof(stripDragStateService));
        }

        public ManagedGroupStripPreviewState ResolveDisplayState(
            IReadOnlyList<IntPtr> orderedHandles,
            IReadOnlyList<IntPtr> previewGroupWindowHandles)
        {
            var canUsePreviewWindowHandles = stripLayoutService.CanUsePreviewWindowHandles(orderedHandles, previewGroupWindowHandles);
            return canUsePreviewWindowHandles
                ? new ManagedGroupStripPreviewState(previewGroupWindowHandles, previewGroupWindowHandles)
                : new ManagedGroupStripPreviewState(orderedHandles, null);
        }

        public ManagedGroupStripPreviewState BuildPreviewState(
            IntPtr draggedWindowHandle,
            bool groupExists,
            IReadOnlyList<IntPtr> actualGroupWindowHandles,
            IntPtr? insertAfterWindowHandle)
        {
            var previewOrder = stripDragStateService.BuildPreviewWindowHandles(
                draggedWindowHandle,
                groupExists,
                actualGroupWindowHandles,
                insertAfterWindowHandle);

            return previewOrder == null
                ? ClearPreviewState(actualGroupWindowHandles)
                : new ManagedGroupStripPreviewState(previewOrder, previewOrder);
        }

        public ManagedGroupStripPreviewState ClearPreviewState(IReadOnlyList<IntPtr> actualGroupWindowHandles)
        {
            return new ManagedGroupStripPreviewState(actualGroupWindowHandles, null);
        }
    }
}
