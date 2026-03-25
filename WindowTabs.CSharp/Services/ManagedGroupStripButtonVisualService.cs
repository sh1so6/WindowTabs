using System;
using System.Collections.Generic;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripButtonVisualService
    {
        private readonly ManagedGroupStripVisualService stripVisualService;

        public ManagedGroupStripButtonVisualService(ManagedGroupStripVisualService stripVisualService)
        {
            this.stripVisualService = stripVisualService ?? throw new ArgumentNullException(nameof(stripVisualService));
        }

        public void ApplyVisuals(
            IReadOnlyList<IntPtr> windowHandles,
            IReadOnlyDictionary<IntPtr, ManagedGroupStripButtonState> buttonStates,
            IReadOnlyDictionary<IntPtr, WindowSnapshot> windowsByHandle,
            GroupSnapshot group,
            TabAppearanceInfo appearance,
            IntPtr activeWindowHandle,
            ManagedGroupStripDropState dropState)
        {
            if (windowHandles == null || buttonStates == null || windowsByHandle == null || group == null || appearance == null)
            {
                return;
            }

            foreach (var windowHandle in windowHandles)
            {
                if (!buttonStates.TryGetValue(windowHandle, out var state)
                    || !windowsByHandle.TryGetValue(windowHandle, out var window))
                {
                    continue;
                }

                var visual = stripVisualService.CreateTabVisual(
                    windowHandle,
                    window.Text,
                    group,
                    appearance,
                    state.Button.Font,
                    activeWindowHandle == windowHandle,
                    state.IsHovered,
                    dropState.IsTargetWindow(windowHandle));

                state.Button.Text = visual.Text;
                state.Button.BackColor = visual.FillColor;
                state.Button.ForeColor = visual.TextColor;
                state.Button.FlatAppearance.BorderColor = visual.BorderColor;
                state.Button.FlatAppearance.MouseOverBackColor = visual.FillColor;
                state.Button.FlatAppearance.MouseDownBackColor = visual.FillColor;
                state.Button.TextAlign = visual.TextAlign;
                state.Button.Size = visual.Size;
                state.Button.Tag = windowHandle;
                state.Button.Invalidate();
            }
        }
    }
}
