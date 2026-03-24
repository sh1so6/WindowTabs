using System;
using System.Drawing;
using Bemo;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Reflection;
using ContractsTabAlign = WindowTabs.CSharp.Contracts.TabAlign;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyWindowPresentationAdapter
    {
        private readonly WindowPresentationStateStore windowPresentationStateStore;

        public LegacyWindowPresentationAdapter(WindowPresentationStateStore windowPresentationStateStore)
        {
            this.windowPresentationStateStore = windowPresentationStateStore ?? throw new ArgumentNullException(nameof(windowPresentationStateStore));
        }

        public void SetWindowNameOverride(Tuple<IntPtr, FSharpOption<string>> value)
        {
            if (value == null)
            {
                return;
            }

            windowPresentationStateStore.SetWindowNameOverride(value.Item1, value.Item2?.Value);
        }

        public FSharpOption<string> GetWindowNameOverride(IntPtr hwnd)
        {
            return windowPresentationStateStore.TryGetWindowNameOverride(hwnd, out var value)
                ? FSharpOption<string>.Some(value)
                : null;
        }

        public void SetWindowFillColor(IntPtr hwnd, FSharpOption<Color> color)
        {
            windowPresentationStateStore.SetFillColor(hwnd, color?.Value);
        }

        public FSharpOption<Color> GetWindowFillColor(IntPtr hwnd)
        {
            return windowPresentationStateStore.TryGetFillColor(hwnd, out var color)
                ? FSharpOption<Color>.Some(color)
                : null;
        }

        public void SetWindowUnderlineColor(IntPtr hwnd, FSharpOption<Color> color)
        {
            windowPresentationStateStore.SetUnderlineColor(hwnd, color?.Value);
        }

        public FSharpOption<Color> GetWindowUnderlineColor(IntPtr hwnd)
        {
            return windowPresentationStateStore.TryGetUnderlineColor(hwnd, out var color)
                ? FSharpOption<Color>.Some(color)
                : null;
        }

        public void SetWindowBorderColor(IntPtr hwnd, FSharpOption<Color> color)
        {
            windowPresentationStateStore.SetBorderColor(hwnd, color?.Value);
        }

        public FSharpOption<Color> GetWindowBorderColor(IntPtr hwnd)
        {
            return windowPresentationStateStore.TryGetBorderColor(hwnd, out var color)
                ? FSharpOption<Color>.Some(color)
                : null;
        }

        public void SetWindowPinned(IntPtr hwnd, bool pinned)
        {
            windowPresentationStateStore.SetPinned(hwnd, pinned);
        }

        public bool IsWindowPinned(IntPtr hwnd)
        {
            return windowPresentationStateStore.IsPinned(hwnd);
        }

        public void SetWindowAlignment(IntPtr hwnd, FSharpOption<TabAlign> alignment)
        {
            windowPresentationStateStore.SetAlignment(hwnd, ToContractsTabAlign(alignment?.Value));
        }

        public FSharpOption<TabAlign> GetWindowAlignment(IntPtr hwnd)
        {
            return windowPresentationStateStore.TryGetAlignment(hwnd, out var alignment)
                ? FSharpOption<TabAlign>.Some(ToBemoTabAlign(alignment))
                : null;
        }

        private static ContractsTabAlign? ToContractsTabAlign(TabAlign alignment)
        {
            if (alignment == null)
            {
                return null;
            }

            var union = FSharpValue.GetUnionFields(alignment, typeof(TabAlign), null);
            return string.Equals(union.Item1.Name, "TopLeft", StringComparison.Ordinal)
                ? ContractsTabAlign.TopLeft
                : ContractsTabAlign.TopRight;
        }

        private static TabAlign ToBemoTabAlign(ContractsTabAlign alignment)
        {
            var caseName = alignment == ContractsTabAlign.TopLeft ? "TopLeft" : "TopRight";
            var unionCase = Array.Find(
                FSharpType.GetUnionCases(typeof(TabAlign), null),
                candidate => string.Equals(candidate.Name, caseName, StringComparison.Ordinal));
            return (TabAlign)FSharpValue.MakeUnion(unionCase, Array.Empty<object>(), null);
        }
    }
}
