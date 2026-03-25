using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripLayoutService
    {
        public bool CanUsePreviewWindowHandles(IReadOnlyList<IntPtr> orderedHandles, IReadOnlyList<IntPtr> previewHandles)
        {
            return previewHandles != null
                && orderedHandles != null
                && orderedHandles.All(previewHandles.Contains)
                && previewHandles.Count >= orderedHandles.Count
                && previewHandles.Count <= orderedHandles.Count + 1;
        }

        public List<IntPtr> BuildPreviewOrder(IReadOnlyList<IntPtr> actualWindowHandles, IntPtr draggedWindowHandle, IntPtr? insertAfterWindowHandle)
        {
            if (draggedWindowHandle == IntPtr.Zero)
            {
                return actualWindowHandles?.ToList() ?? new List<IntPtr>();
            }

            var previewOrder = actualWindowHandles?
                .Where(handle => handle != draggedWindowHandle)
                .ToList()
                ?? new List<IntPtr>();

            if (insertAfterWindowHandle.HasValue)
            {
                var insertAfterIndex = previewOrder.IndexOf(insertAfterWindowHandle.Value);
                if (insertAfterIndex >= 0)
                {
                    previewOrder.Insert(insertAfterIndex + 1, draggedWindowHandle);
                }
                else
                {
                    previewOrder.Add(draggedWindowHandle);
                }
            }
            else
            {
                previewOrder.Insert(0, draggedWindowHandle);
            }

            return previewOrder;
        }

        public bool TryResolveDropTarget(
            Point clientPoint,
            IReadOnlyList<IntPtr> currentGroupWindowHandles,
            IReadOnlyDictionary<IntPtr, ManagedGroupStripButtonState> buttonStates,
            Rectangle displayRectangle,
            out ManagedGroupStripDropTargetInfo dropTargetInfo)
        {
            if (currentGroupWindowHandles != null)
            {
                for (var index = 0; index < currentGroupWindowHandles.Count; index++)
                {
                    var windowHandle = currentGroupWindowHandles[index];
                    if (buttonStates == null
                        || !buttonStates.TryGetValue(windowHandle, out var buttonState)
                        || !buttonState.Button.Bounds.Contains(clientPoint))
                    {
                        continue;
                    }

                    var bounds = buttonState.Button.Bounds;
                    var insertAfter = clientPoint.X >= bounds.Left + (bounds.Width / 2);
                    if (insertAfter)
                    {
                        dropTargetInfo = new ManagedGroupStripDropTargetInfo(windowHandle, windowHandle);
                        return true;
                    }

                    dropTargetInfo = new ManagedGroupStripDropTargetInfo(
                        windowHandle,
                        index > 0 ? currentGroupWindowHandles[index - 1] : (IntPtr?)null);
                    return true;
                }
            }

            if (currentGroupWindowHandles != null
                && currentGroupWindowHandles.Count > 0
                && displayRectangle.Contains(clientPoint))
            {
                if (!TryGetButtonBounds(currentGroupWindowHandles, buttonStates, 0, out var firstHandle, out var firstButton))
                {
                    dropTargetInfo = default;
                    return false;
                }

                if (clientPoint.X < firstButton.Left)
                {
                    dropTargetInfo = new ManagedGroupStripDropTargetInfo(firstHandle, null);
                    return true;
                }

                for (var index = 1; index < currentGroupWindowHandles.Count; index++)
                {
                    if (!TryGetButtonBounds(currentGroupWindowHandles, buttonStates, index - 1, out var previousHandle, out var previousButton)
                        || !TryGetButtonBounds(currentGroupWindowHandles, buttonStates, index, out var nextHandle, out var nextButton))
                    {
                        continue;
                    }

                    if (clientPoint.X <= previousButton.Right || clientPoint.X >= nextButton.Left)
                    {
                        continue;
                    }

                    var midpoint = previousButton.Right + ((nextButton.Left - previousButton.Right) / 2);
                    dropTargetInfo = clientPoint.X <= midpoint
                        ? new ManagedGroupStripDropTargetInfo(previousHandle, previousHandle)
                        : new ManagedGroupStripDropTargetInfo(nextHandle, previousHandle);
                    return true;
                }

                if (TryGetButtonBounds(
                        currentGroupWindowHandles,
                        buttonStates,
                        currentGroupWindowHandles.Count - 1,
                        out var lastHandle,
                        out var lastButton)
                    && clientPoint.X > lastButton.Right)
                {
                    dropTargetInfo = new ManagedGroupStripDropTargetInfo(
                        lastHandle,
                        lastHandle);
                    return true;
                }
            }

            dropTargetInfo = default;
            return false;
        }

        private static bool TryGetButtonBounds(
            IReadOnlyList<IntPtr> currentGroupWindowHandles,
            IReadOnlyDictionary<IntPtr, ManagedGroupStripButtonState> buttonStates,
            int index,
            out IntPtr windowHandle,
            out Rectangle bounds)
        {
            windowHandle = IntPtr.Zero;
            bounds = default;
            if (currentGroupWindowHandles == null
                || buttonStates == null
                || index < 0
                || index >= currentGroupWindowHandles.Count)
            {
                return false;
            }

            windowHandle = currentGroupWindowHandles[index];
            if (!buttonStates.TryGetValue(windowHandle, out var buttonState))
            {
                windowHandle = IntPtr.Zero;
                return false;
            }

            bounds = buttonState.Button.Bounds;
            return true;
        }
    }
}
