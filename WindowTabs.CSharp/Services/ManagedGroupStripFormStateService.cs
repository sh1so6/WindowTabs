using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripFormStateService
    {
        private readonly IDesktopRuntime desktopRuntime;
        private readonly ManagedGroupStripDragSessionStateService dragSessionStateService;
        private readonly ManagedGroupStripLayoutService stripLayoutService;
        private readonly ManagedGroupStripButtonCollectionService buttonCollectionService;
        private readonly ManagedGroupStripControlBindingService controlBindingService;

        public ManagedGroupStripFormStateService(
            IDesktopRuntime desktopRuntime,
            ManagedGroupStripDragSessionStateService dragSessionStateService,
            ManagedGroupStripLayoutService stripLayoutService,
            ManagedGroupStripButtonCollectionService buttonCollectionService,
            ManagedGroupStripControlBindingService controlBindingService)
        {
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
            this.dragSessionStateService = dragSessionStateService ?? throw new ArgumentNullException(nameof(dragSessionStateService));
            this.stripLayoutService = stripLayoutService ?? throw new ArgumentNullException(nameof(stripLayoutService));
            this.buttonCollectionService = buttonCollectionService ?? throw new ArgumentNullException(nameof(buttonCollectionService));
            this.controlBindingService = controlBindingService ?? throw new ArgumentNullException(nameof(controlBindingService));
        }

        public ManagedGroupStripFormState ApplyDisplayState(
            ManagedGroupStripDisplayState displayState,
            FlowLayoutPanel tabPanel,
            IDictionary<IntPtr, ManagedGroupStripButtonState> buttonStates,
            IWin32Window owner,
            Func<ManagedGroupStripFormState> getCurrentState,
            Action invalidateStrip)
        {
            if (displayState == null)
            {
                throw new ArgumentNullException(nameof(displayState));
            }

            var nextState = new ManagedGroupStripFormState(
                displayState.GroupHandle,
                displayState.ActualGroupWindowHandles,
                displayState.DragSessionState);

            SyncButtons(nextState, tabPanel, buttonStates, owner, getCurrentState, invalidateStrip);
            return nextState;
        }

        public ManagedGroupStripFormState StartDragPreview(
            ManagedGroupStripFormState currentState,
            TabDragInfo dragInfo,
            IntPtr? insertAfterWindowHandle,
            FlowLayoutPanel tabPanel,
            IDictionary<IntPtr, ManagedGroupStripButtonState> buttonStates,
            IWin32Window owner,
            Func<ManagedGroupStripFormState> getCurrentState,
            Action invalidateStrip)
        {
            currentState ??= ManagedGroupStripFormState.Empty;
            var nextDragSessionState = dragSessionStateService.StartDrag(
                dragInfo,
                GroupExists(currentState.CurrentGroupHandle),
                currentState.ActualGroupWindowHandles,
                insertAfterWindowHandle,
                currentState.DragSessionState);

            return ApplyDragSessionState(
                currentState,
                nextDragSessionState,
                tabPanel,
                buttonStates,
                owner,
                getCurrentState,
                invalidateStrip);
        }

        public ManagedGroupStripFormState UpdateDragPreview(
            ManagedGroupStripFormState currentState,
            IntPtr? insertAfterWindowHandle,
            FlowLayoutPanel tabPanel,
            IDictionary<IntPtr, ManagedGroupStripButtonState> buttonStates,
            IWin32Window owner,
            Func<ManagedGroupStripFormState> getCurrentState,
            Action invalidateStrip)
        {
            currentState ??= ManagedGroupStripFormState.Empty;
            var nextDragSessionState = dragSessionStateService.UpdatePreview(
                GroupExists(currentState.CurrentGroupHandle),
                currentState.ActualGroupWindowHandles,
                insertAfterWindowHandle,
                currentState.DragSessionState);

            return ApplyDragSessionState(
                currentState,
                nextDragSessionState,
                tabPanel,
                buttonStates,
                owner,
                getCurrentState,
                invalidateStrip);
        }

        public ManagedGroupStripFormState ClearDragState(
            ManagedGroupStripFormState currentState,
            FlowLayoutPanel tabPanel,
            IDictionary<IntPtr, ManagedGroupStripButtonState> buttonStates,
            IWin32Window owner,
            Func<ManagedGroupStripFormState> getCurrentState,
            Action invalidateStrip)
        {
            currentState ??= ManagedGroupStripFormState.Empty;
            var nextDragSessionState = dragSessionStateService.ClearDragState(currentState.ActualGroupWindowHandles);
            return ApplyDragSessionState(
                currentState,
                nextDragSessionState,
                tabPanel,
                buttonStates,
                owner,
                getCurrentState,
                invalidateStrip);
        }

        public ManagedGroupStripFormState UpdateDropTarget(
            ManagedGroupStripFormState currentState,
            IntPtr targetWindowHandle,
            bool insertAfterTarget,
            IReadOnlyDictionary<IntPtr, ManagedGroupStripButtonState> buttonStates)
        {
            currentState ??= ManagedGroupStripFormState.Empty;
            var nextDragSessionState = dragSessionStateService.UpdateDropTarget(
                currentState.DragSessionState,
                targetWindowHandle,
                insertAfterTarget,
                out var invalidatedWindowHandles);

            foreach (var invalidatedWindowHandle in invalidatedWindowHandles)
            {
                if (invalidatedWindowHandle != IntPtr.Zero
                    && buttonStates != null
                    && buttonStates.TryGetValue(invalidatedWindowHandle, out var buttonState))
                {
                    buttonState.Button.Invalidate();
                }
            }

            return currentState.WithDragSessionState(nextDragSessionState);
        }

        public bool TryResolveDropTarget(
            Point clientPoint,
            ManagedGroupStripFormState currentState,
            IReadOnlyDictionary<IntPtr, ManagedGroupStripButtonState> buttonStates,
            int tabOverlap,
            Rectangle displayRectangle,
            out ManagedGroupStripDropTargetInfo dropTargetInfo)
        {
            currentState ??= ManagedGroupStripFormState.Empty;
            return stripLayoutService.TryResolveDropTarget(
                clientPoint,
                currentState.DragSessionState.CurrentGroupWindowHandles,
                buttonStates,
                tabOverlap,
                displayRectangle,
                out dropTargetInfo);
        }

        private ManagedGroupStripFormState ApplyDragSessionState(
            ManagedGroupStripFormState currentState,
            ManagedGroupStripDragSessionState dragSessionState,
            FlowLayoutPanel tabPanel,
            IDictionary<IntPtr, ManagedGroupStripButtonState> buttonStates,
            IWin32Window owner,
            Func<ManagedGroupStripFormState> getCurrentState,
            Action invalidateStrip)
        {
            var nextState = (currentState ?? ManagedGroupStripFormState.Empty).WithDragSessionState(dragSessionState);
            SyncButtons(nextState, tabPanel, buttonStates, owner, getCurrentState, invalidateStrip);
            return nextState;
        }

        private void SyncButtons(
            ManagedGroupStripFormState state,
            FlowLayoutPanel tabPanel,
            IDictionary<IntPtr, ManagedGroupStripButtonState> buttonStates,
            IWin32Window owner,
            Func<ManagedGroupStripFormState> getCurrentState,
            Action invalidateStrip)
        {
            if (tabPanel == null)
            {
                throw new ArgumentNullException(nameof(tabPanel));
            }

            if (buttonStates == null)
            {
                throw new ArgumentNullException(nameof(buttonStates));
            }

            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            if (getCurrentState == null)
            {
                throw new ArgumentNullException(nameof(getCurrentState));
            }

            buttonCollectionService.SyncButtons(
                tabPanel,
                buttonStates,
                state?.DragSessionState?.CurrentGroupWindowHandles ?? Array.Empty<IntPtr>(),
                controlBindingService.CreateTabButton,
                (button, buttonState, windowHandle) => controlBindingService.WireTabButton(
                    owner,
                    button,
                    buttonState,
                    windowHandle,
                    () => getCurrentState()?.DragSessionState?.DropState ?? ManagedGroupStripDropState.Empty,
                    invalidateStrip));
        }

        private bool GroupExists(IntPtr groupHandle)
        {
            return groupHandle != IntPtr.Zero && desktopRuntime.FindGroup(groupHandle) != null;
        }
    }
}
