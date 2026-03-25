using System;
using System.Collections.Generic;
using System.Linq;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupDragDropTargetRegistrySyncService
    {
        public void SyncTargets(
            IDragDrop dragDrop,
            IDesktopRuntime desktopRuntime,
            GroupMutationService groupMutationService,
            GroupMembershipService groupMembershipService,
            IDictionary<IntPtr, IDragDropTarget> targets)
        {
            if (dragDrop == null)
            {
                throw new ArgumentNullException(nameof(dragDrop));
            }

            if (desktopRuntime == null)
            {
                throw new ArgumentNullException(nameof(desktopRuntime));
            }

            if (groupMutationService == null)
            {
                throw new ArgumentNullException(nameof(groupMutationService));
            }

            if (groupMembershipService == null)
            {
                throw new ArgumentNullException(nameof(groupMembershipService));
            }

            if (targets == null)
            {
                throw new ArgumentNullException(nameof(targets));
            }

            var desiredHandles = new HashSet<IntPtr>(
                desktopRuntime.Groups.SelectMany(group => group.WindowHandles).Where(hwnd => hwnd != IntPtr.Zero));

            foreach (var staleHandle in targets.Keys.Where(hwnd => !desiredHandles.Contains(hwnd)).ToArray())
            {
                dragDrop.UnregisterTarget(staleHandle);
                targets.Remove(staleHandle);
            }

            foreach (var hwnd in desiredHandles)
            {
                if (targets.ContainsKey(hwnd))
                {
                    continue;
                }

                var target = new ManagedGroupDragDropTarget(hwnd, groupMutationService, groupMembershipService);
                targets.Add(hwnd, target);
                dragDrop.RegisterTarget(hwnd, target);
            }
        }

        public void ClearTargets(IDragDrop dragDrop, IDictionary<IntPtr, IDragDropTarget> targets)
        {
            if (dragDrop == null)
            {
                throw new ArgumentNullException(nameof(dragDrop));
            }

            if (targets == null)
            {
                throw new ArgumentNullException(nameof(targets));
            }

            foreach (var hwnd in targets.Keys.ToArray())
            {
                dragDrop.UnregisterTarget(hwnd);
            }

            targets.Clear();
        }
    }
}
