using System;
using System.Collections.Generic;

namespace WindowTabs.CSharp.Models
{
    internal sealed class ManagedGroupStripPreviewState
    {
        public ManagedGroupStripPreviewState(
            IEnumerable<IntPtr> currentGroupWindowHandles,
            IEnumerable<IntPtr> previewGroupWindowHandles)
        {
            CurrentGroupWindowHandles = currentGroupWindowHandles is null
                ? []
                : [.. currentGroupWindowHandles];
            PreviewGroupWindowHandles = previewGroupWindowHandles is null
                ? null
                : [.. previewGroupWindowHandles];
        }

        public IReadOnlyList<IntPtr> CurrentGroupWindowHandles { get; }

        public IReadOnlyList<IntPtr> PreviewGroupWindowHandles { get; }
    }
}
