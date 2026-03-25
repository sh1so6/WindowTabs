using System;
using System.Collections.Generic;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripDragSessionStateService
    {
        private readonly ManagedGroupStripDragStateService stripDragStateService;
        private readonly ManagedGroupStripPreviewStateService previewStateService;

        public ManagedGroupStripDragSessionStateService(
            ManagedGroupStripDragStateService stripDragStateService,
            ManagedGroupStripPreviewStateService previewStateService)
        {
            this.stripDragStateService = stripDragStateService ?? throw new ArgumentNullException(nameof(stripDragStateService));
            this.previewStateService = previewStateService ?? throw new ArgumentNullException(nameof(previewStateService));
        }

        public ManagedGroupStripDragSessionState ResolveDisplayState(
            IReadOnlyList<IntPtr> orderedHandles,
            ManagedGroupStripDragSessionState currentSession)
        {
            currentSession ??= ManagedGroupStripDragSessionState.Empty;
            var previewState = previewStateService.ResolveDisplayState(orderedHandles, currentSession.PreviewGroupWindowHandles);
            return currentSession.WithPreviewState(previewState);
        }

        public ManagedGroupStripDragSessionState StartDrag(
            TabDragInfo dragInfo,
            bool groupExists,
            IReadOnlyList<IntPtr> actualGroupWindowHandles,
            IntPtr? insertAfterWindowHandle,
            ManagedGroupStripDragSessionState currentSession)
        {
            currentSession ??= ManagedGroupStripDragSessionState.Empty;
            return UpdatePreview(
                dragInfo?.WindowHandle ?? IntPtr.Zero,
                groupExists,
                actualGroupWindowHandles,
                insertAfterWindowHandle,
                currentSession);
        }

        public ManagedGroupStripDragSessionState UpdatePreview(
            bool groupExists,
            IReadOnlyList<IntPtr> actualGroupWindowHandles,
            IntPtr? insertAfterWindowHandle,
            ManagedGroupStripDragSessionState currentSession)
        {
            currentSession ??= ManagedGroupStripDragSessionState.Empty;
            return UpdatePreview(
                currentSession.DraggedWindowHandle,
                groupExists,
                actualGroupWindowHandles,
                insertAfterWindowHandle,
                currentSession);
        }

        public ManagedGroupStripDragSessionState UpdateDropTarget(
            ManagedGroupStripDragSessionState currentSession,
            IntPtr targetWindowHandle,
            bool insertAfterTarget,
            out IReadOnlyList<IntPtr> invalidatedWindowHandles)
        {
            currentSession ??= ManagedGroupStripDragSessionState.Empty;
            var change = stripDragStateService.UpdateDropTarget(currentSession.DropState, targetWindowHandle, insertAfterTarget);
            invalidatedWindowHandles = change.InvalidatedWindowHandles;
            return currentSession.WithDropState(change.DropState);
        }

        public ManagedGroupStripDragSessionState ClearDragState(IReadOnlyList<IntPtr> actualGroupWindowHandles)
        {
            return ManagedGroupStripDragSessionState.Empty
                .WithPreviewState(previewStateService.ClearPreviewState(actualGroupWindowHandles));
        }

        private ManagedGroupStripDragSessionState UpdatePreview(
            IntPtr draggedWindowHandle,
            bool groupExists,
            IReadOnlyList<IntPtr> actualGroupWindowHandles,
            IntPtr? insertAfterWindowHandle,
            ManagedGroupStripDragSessionState currentSession)
        {
            var previewState = previewStateService.BuildPreviewState(
                draggedWindowHandle,
                groupExists,
                actualGroupWindowHandles,
                insertAfterWindowHandle);

            return currentSession
                .WithDraggedWindowHandle(draggedWindowHandle)
                .WithPreviewState(previewState);
        }
    }
}
