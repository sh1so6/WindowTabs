using System;
using System.Collections.Generic;
using Bemo;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class InMemoryWindowGroupRuntime : IWindowGroupRuntime
    {
        private readonly List<IntPtr> windowHandles = new List<IntPtr>();

        public InMemoryWindowGroupRuntime(IntPtr groupHandle)
        {
            GroupHandle = groupHandle;
        }

        public IntPtr GroupHandle { get; }

        public IReadOnlyList<IntPtr> WindowHandles => windowHandles;

        public string TabPosition { get; set; } = "TopRight";

        public bool SnapTabHeightMargin { get; set; }

        public void AddWindow(IntPtr windowHandle, IntPtr? insertAfterWindowHandle)
        {
            if (windowHandle == IntPtr.Zero || windowHandles.Contains(windowHandle))
            {
                return;
            }

            InsertWindow(windowHandle, insertAfterWindowHandle);
        }

        public void MoveWindowAfter(IntPtr windowHandle, IntPtr? insertAfterWindowHandle)
        {
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            if (!windowHandles.Remove(windowHandle))
            {
                return;
            }

            if (!insertAfterWindowHandle.HasValue)
            {
                windowHandles.Insert(0, windowHandle);
                return;
            }

            InsertWindow(windowHandle, insertAfterWindowHandle);
        }

        public void RemoveWindow(IntPtr windowHandle)
        {
            windowHandles.RemoveAll(handle => handle == windowHandle);
        }

        public void SwitchWindow(bool next, bool force)
        {
            if (windowHandles.Count == 0)
            {
                return;
            }

            var foregroundHandle = WinUserApi.GetForegroundWindow();
            var currentIndex = windowHandles.IndexOf(foregroundHandle);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }
            else if (windowHandles.Count == 1 && !force)
            {
                return;
            }

            var targetIndex = next
                ? (currentIndex + 1) % windowHandles.Count
                : (currentIndex - 1 + windowHandles.Count) % windowHandles.Count;
            WinUserApi.SetForegroundWindow(windowHandles[targetIndex]);
        }

        public void ActivateWindowAt(int index, bool force)
        {
            if (index < 0 || index >= windowHandles.Count)
            {
                return;
            }

            if (windowHandles.Count == 1 && !force)
            {
                return;
            }

            var targetHandle = windowHandles[index];
            if (targetHandle != IntPtr.Zero)
            {
                WinUserApi.SetForegroundWindow(targetHandle);
            }
        }

        private void InsertWindow(IntPtr windowHandle, IntPtr? insertAfterWindowHandle)
        {
            if (insertAfterWindowHandle.HasValue)
            {
                var insertAfterIndex = windowHandles.IndexOf(insertAfterWindowHandle.Value);
                if (insertAfterIndex >= 0 && insertAfterIndex < windowHandles.Count)
                {
                    windowHandles.Insert(insertAfterIndex + 1, windowHandle);
                    return;
                }
            }

            windowHandles.Add(windowHandle);
        }
    }
}
