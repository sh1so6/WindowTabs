using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripButtonCollectionService
    {
        public void SyncButtons(
            FlowLayoutPanel tabPanel,
            IDictionary<IntPtr, ManagedGroupStripButtonState> buttonStates,
            IReadOnlyList<IntPtr> displayHandles,
            Func<Button> createButton,
            Action<Button, ManagedGroupStripButtonState, IntPtr> wireButton)
        {
            if (tabPanel == null)
            {
                throw new ArgumentNullException(nameof(tabPanel));
            }

            if (buttonStates == null)
            {
                throw new ArgumentNullException(nameof(buttonStates));
            }

            if (displayHandles == null)
            {
                throw new ArgumentNullException(nameof(displayHandles));
            }

            if (createButton == null)
            {
                throw new ArgumentNullException(nameof(createButton));
            }

            if (wireButton == null)
            {
                throw new ArgumentNullException(nameof(wireButton));
            }

            foreach (var staleHandle in buttonStates.Keys.Where(hwnd => !displayHandles.Contains(hwnd)).ToArray())
            {
                var staleState = buttonStates[staleHandle];
                tabPanel.Controls.Remove(staleState.Button);
                staleState.Dispose();
                buttonStates.Remove(staleHandle);
            }

            foreach (var windowHandle in displayHandles)
            {
                if (buttonStates.ContainsKey(windowHandle))
                {
                    continue;
                }

                var button = createButton();
                var state = new ManagedGroupStripButtonState(button);
                buttonStates.Add(windowHandle, state);
                tabPanel.Controls.Add(button);
                wireButton(button, state, windowHandle);
            }

            ApplyDisplayedButtonOrder(tabPanel, buttonStates, displayHandles);
        }

        private static void ApplyDisplayedButtonOrder(
            FlowLayoutPanel tabPanel,
            IDictionary<IntPtr, ManagedGroupStripButtonState> buttonStates,
            IReadOnlyList<IntPtr> orderedHandles)
        {
            for (var index = 0; index < orderedHandles.Count; index++)
            {
                if (buttonStates.TryGetValue(orderedHandles[index], out var state))
                {
                    tabPanel.Controls.SetChildIndex(state.Button, index);
                }
            }
        }
    }
}
