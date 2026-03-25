using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowTabs.CSharp.Models
{
    internal sealed class GroupSnapshot
    {
        public GroupSnapshot()
            : this(IntPtr.Zero, Array.Empty<IntPtr>(), "TopRight", false)
        {
        }

        public GroupSnapshot(
            IntPtr groupHandle,
            IEnumerable<IntPtr> windowHandles,
            string tabPosition,
            bool snapTabHeightMargin)
        {
            GroupHandle = groupHandle;
            WindowHandles = (windowHandles ?? Array.Empty<IntPtr>()).ToArray();
            TabPosition = string.IsNullOrWhiteSpace(tabPosition) ? "TopRight" : tabPosition;
            SnapTabHeightMargin = snapTabHeightMargin;
        }

        public IntPtr GroupHandle { get; }

        public IReadOnlyList<IntPtr> WindowHandles { get; }

        public string TabPosition { get; }

        public bool SnapTabHeightMargin { get; }
    }
}
