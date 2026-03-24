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
            IReadOnlyDictionary<IntPtr, Rectangle> buttonBoundsByHandle,
            Rectangle displayRectangle,
            out ManagedGroupStripDropTargetInfo dropTargetInfo)
        {
            if (currentGroupWindowHandles != null)
            {
                foreach (var windowHandle in currentGroupWindowHandles)
                {
                    if (!buttonBoundsByHandle.TryGetValue(windowHandle, out var bounds) || !bounds.Contains(clientPoint))
                    {
                        continue;
                    }

                    var insertAfter = clientPoint.X >= bounds.Left + (bounds.Width / 2);
                    if (insertAfter)
                    {
                        dropTargetInfo = new ManagedGroupStripDropTargetInfo(windowHandle, windowHandle);
                        return true;
                    }

                    var targetIndex = FindIndex(currentGroupWindowHandles, windowHandle);
                    dropTargetInfo = new ManagedGroupStripDropTargetInfo(
                        windowHandle,
                        targetIndex > 0 ? currentGroupWindowHandles[targetIndex - 1] : (IntPtr?)null);
                    return true;
                }
            }

            var orderedButtons = currentGroupWindowHandles?
                .Where(buttonBoundsByHandle.ContainsKey)
                .Select(handle => new KeyValuePair<IntPtr, Rectangle>(handle, buttonBoundsByHandle[handle]))
                .ToList()
                ?? new List<KeyValuePair<IntPtr, Rectangle>>();

            if (orderedButtons.Count > 0 && displayRectangle.Contains(clientPoint))
            {
                var firstButton = orderedButtons[0].Value;
                if (clientPoint.X < firstButton.Left)
                {
                    dropTargetInfo = new ManagedGroupStripDropTargetInfo(orderedButtons[0].Key, null);
                    return true;
                }

                for (var index = 1; index < orderedButtons.Count; index++)
                {
                    var previousButton = orderedButtons[index - 1].Value;
                    var nextButton = orderedButtons[index].Value;
                    if (clientPoint.X <= previousButton.Right || clientPoint.X >= nextButton.Left)
                    {
                        continue;
                    }

                    var midpoint = previousButton.Right + ((nextButton.Left - previousButton.Right) / 2);
                    dropTargetInfo = clientPoint.X <= midpoint
                        ? new ManagedGroupStripDropTargetInfo(orderedButtons[index - 1].Key, orderedButtons[index - 1].Key)
                        : new ManagedGroupStripDropTargetInfo(orderedButtons[index].Key, orderedButtons[index - 1].Key);
                    return true;
                }

                var lastButton = orderedButtons[orderedButtons.Count - 1].Value;
                if (clientPoint.X > lastButton.Right)
                {
                    dropTargetInfo = new ManagedGroupStripDropTargetInfo(
                        orderedButtons[orderedButtons.Count - 1].Key,
                        orderedButtons[orderedButtons.Count - 1].Key);
                    return true;
                }
            }

            dropTargetInfo = default;
            return false;
        }

        private static int FindIndex(IReadOnlyList<IntPtr> handles, IntPtr targetHandle)
        {
            if (handles == null)
            {
                return -1;
            }

            for (var index = 0; index < handles.Count; index++)
            {
                if (handles[index] == targetHandle)
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
