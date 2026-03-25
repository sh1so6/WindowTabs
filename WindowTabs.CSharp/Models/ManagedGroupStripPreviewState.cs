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
            CurrentGroupWindowHandles = currentGroupWindowHandles == null
                ? Array.Empty<IntPtr>()
                : new List<IntPtr>(currentGroupWindowHandles).AsReadOnly();
            PreviewGroupWindowHandles = previewGroupWindowHandles == null
                ? null
                : new List<IntPtr>(previewGroupWindowHandles).AsReadOnly();
        }

        public IReadOnlyList<IntPtr> CurrentGroupWindowHandles { get; }

        public IReadOnlyList<IntPtr> PreviewGroupWindowHandles { get; }
    }
}
