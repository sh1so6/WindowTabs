using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class WindowPresentationStateStore
    {
        private readonly Dictionary<IntPtr, string> nameOverrides = new Dictionary<IntPtr, string>();
        private readonly Dictionary<IntPtr, Color> fillColors = new Dictionary<IntPtr, Color>();
        private readonly Dictionary<IntPtr, Color> underlineColors = new Dictionary<IntPtr, Color>();
        private readonly Dictionary<IntPtr, Color> borderColors = new Dictionary<IntPtr, Color>();
        private readonly HashSet<IntPtr> pinnedWindows = new HashSet<IntPtr>();
        private readonly Dictionary<IntPtr, TabAlign> alignments = new Dictionary<IntPtr, TabAlign>();

        public void SetWindowNameOverride(IntPtr hwnd, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                nameOverrides.Remove(hwnd);
                return;
            }

            nameOverrides[hwnd] = name;
        }

        public bool TryGetWindowNameOverride(IntPtr hwnd, out string name)
        {
            return nameOverrides.TryGetValue(hwnd, out name);
        }

        public void SetFillColor(IntPtr hwnd, Color? color)
        {
            SetColor(fillColors, hwnd, color);
        }

        public bool TryGetFillColor(IntPtr hwnd, out Color color)
        {
            return fillColors.TryGetValue(hwnd, out color);
        }

        public void SetUnderlineColor(IntPtr hwnd, Color? color)
        {
            SetColor(underlineColors, hwnd, color);
        }

        public bool TryGetUnderlineColor(IntPtr hwnd, out Color color)
        {
            return underlineColors.TryGetValue(hwnd, out color);
        }

        public void SetBorderColor(IntPtr hwnd, Color? color)
        {
            SetColor(borderColors, hwnd, color);
        }

        public bool TryGetBorderColor(IntPtr hwnd, out Color color)
        {
            return borderColors.TryGetValue(hwnd, out color);
        }

        public void SetPinned(IntPtr hwnd, bool pinned)
        {
            if (pinned)
            {
                pinnedWindows.Add(hwnd);
            }
            else
            {
                pinnedWindows.Remove(hwnd);
            }
        }

        public bool IsPinned(IntPtr hwnd)
        {
            return pinnedWindows.Contains(hwnd);
        }

        public void SetAlignment(IntPtr hwnd, TabAlign? alignment)
        {
            if (!alignment.HasValue)
            {
                alignments.Remove(hwnd);
                return;
            }

            alignments[hwnd] = alignment.Value;
        }

        public bool TryGetAlignment(IntPtr hwnd, out TabAlign alignment)
        {
            return alignments.TryGetValue(hwnd, out alignment);
        }

        public void RemoveWindow(IntPtr hwnd)
        {
            nameOverrides.Remove(hwnd);
            fillColors.Remove(hwnd);
            underlineColors.Remove(hwnd);
            borderColors.Remove(hwnd);
            pinnedWindows.Remove(hwnd);
            alignments.Remove(hwnd);
        }

        public void RemoveClosedWindows(ISet<IntPtr> activeWindowHandles)
        {
            RemoveMissingKeys(nameOverrides.Keys, activeWindowHandles, hwnd => nameOverrides.Remove(hwnd));
            RemoveMissingKeys(fillColors.Keys, activeWindowHandles, hwnd => fillColors.Remove(hwnd));
            RemoveMissingKeys(underlineColors.Keys, activeWindowHandles, hwnd => underlineColors.Remove(hwnd));
            RemoveMissingKeys(borderColors.Keys, activeWindowHandles, hwnd => borderColors.Remove(hwnd));
            RemoveMissingKeys(pinnedWindows, activeWindowHandles, hwnd => pinnedWindows.Remove(hwnd));
            RemoveMissingKeys(alignments.Keys, activeWindowHandles, hwnd => alignments.Remove(hwnd));
        }

        public void Reset()
        {
            nameOverrides.Clear();
            fillColors.Clear();
            underlineColors.Clear();
            borderColors.Clear();
            pinnedWindows.Clear();
            alignments.Clear();
        }

        private static void SetColor(IDictionary<IntPtr, Color> colors, IntPtr hwnd, Color? color)
        {
            if (!color.HasValue)
            {
                colors.Remove(hwnd);
            }
            else
            {
                colors[hwnd] = color.Value;
            }
        }

        private static void RemoveMissingKeys(IEnumerable<IntPtr> keys, ISet<IntPtr> activeWindowHandles, Action<IntPtr> remove)
        {
            foreach (var hwnd in keys.Where(hwnd => !activeWindowHandles.Contains(hwnd)).ToArray())
            {
                remove(hwnd);
            }
        }
    }
}
