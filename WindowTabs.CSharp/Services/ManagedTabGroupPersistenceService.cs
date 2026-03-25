using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Newtonsoft.Json.Linq;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedTabGroupPersistenceService
    {
        private readonly SettingsStore settingsStore;
        private readonly DesktopWindowCatalogService desktopWindowCatalogService;
        private readonly WindowPresentationStateStore windowPresentationStateStore;

        public ManagedTabGroupPersistenceService(
            SettingsStore settingsStore,
            DesktopWindowCatalogService desktopWindowCatalogService,
            WindowPresentationStateStore windowPresentationStateStore)
        {
            this.settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
            this.desktopWindowCatalogService = desktopWindowCatalogService ?? throw new ArgumentNullException(nameof(desktopWindowCatalogService));
            this.windowPresentationStateStore = windowPresentationStateStore ?? throw new ArgumentNullException(nameof(windowPresentationStateStore));
        }

        public void RestoreGroups(ManagedDesktopStateStore stateStore)
        {
            if (stateStore == null)
            {
                return;
            }

            var root = settingsStore.LoadRawRoot();
            if (!(root["SavedTabGroupsForRestart"] is JArray groupsArray) || groupsArray.Count == 0)
            {
                return;
            }

            var windowsByHandle = desktopWindowCatalogService.GetRestorableWindowsByHandle();
            var assignedWindowHandles = new HashSet<IntPtr>();

            foreach (var groupToken in groupsArray.OfType<JToken>())
            {
                var windowsArray = groupToken["windows"] as JArray ?? groupToken as JArray;
                if (windowsArray == null || windowsArray.Count == 0)
                {
                    continue;
                }

                var validHandles = CollectRestorableWindowHandles(
                    windowsArray.OfType<JObject>(),
                    windowsByHandle,
                    assignedWindowHandles);
                if (validHandles.Count == 0)
                {
                    continue;
                }

                var preferredGroupHandle = TryGetPreferredGroupHandle(groupToken);
                var group = stateStore.CreateGroupWithWindows(validHandles, preferredGroupHandle);
                ApplySavedGroupState(groupToken as JObject, group);
            }
        }

        public void SaveGroups(IReadOnlyList<IWindowGroupRuntime> groups)
        {
            var root = settingsStore.LoadRawRoot();
            var groupsArray = new JArray();

            foreach (var group in groups ?? Array.Empty<IWindowGroupRuntime>())
            {
                if (group == null || group.WindowHandles.Count == 0)
                {
                    continue;
                }

                var windowsArray = new JArray();
                foreach (var hwnd in group.WindowHandles)
                {
                    var windowObject = new JObject
                    {
                        ["hwnd"] = hwnd.ToInt64()
                    };

                    if (windowPresentationStateStore.TryGetWindowNameOverride(hwnd, out var renamedTabName))
                    {
                        windowObject["renamedTabName"] = renamedTabName;
                    }

                    if (windowPresentationStateStore.IsPinned(hwnd))
                    {
                        windowObject["isPinned"] = true;
                    }

                    if (windowPresentationStateStore.TryGetFillColor(hwnd, out var fillColor))
                    {
                        windowObject["tabFillColor"] = ToRrgGBbaa(fillColor);
                    }

                    if (windowPresentationStateStore.TryGetUnderlineColor(hwnd, out var underlineColor))
                    {
                        windowObject["tabUnderlineColor"] = ToRrgGBbaa(underlineColor);
                    }

                    if (windowPresentationStateStore.TryGetBorderColor(hwnd, out var borderColor))
                    {
                        windowObject["tabBorderColor"] = ToRrgGBbaa(borderColor);
                    }

                    if (windowPresentationStateStore.TryGetAlignment(hwnd, out var alignment))
                    {
                        windowObject["tabAlignment"] = alignment.ToString();
                    }

                    windowsArray.Add(windowObject);
                }

                if (windowsArray.Count == 0)
                {
                    continue;
                }

                groupsArray.Add(new JObject
                {
                    ["groupHandle"] = group.GroupHandle.ToInt64(),
                    ["windows"] = windowsArray,
                    ["tabPosition"] = group.TabPosition,
                    ["snapTabHeightMargin"] = group.SnapTabHeightMargin
                });
            }

            root["SavedTabGroupsForRestart"] = groupsArray;
            settingsStore.SaveRawRoot(root);
        }

        private static IntPtr? TryGetPreferredGroupHandle(JToken groupToken)
        {
            if (!(groupToken is JObject groupObject))
            {
                return null;
            }

            var value = groupObject["groupHandle"]?.Type == JTokenType.Integer
                ? (long?)groupObject["groupHandle"]
                : null;
            return value.HasValue ? new IntPtr(value.Value) : (IntPtr?)null;
        }

        private static void ApplySavedGroupState(JObject groupObject, IWindowGroupRuntime group)
        {
            if (groupObject == null || group == null)
            {
                return;
            }

            var savedTabPosition = groupObject["tabPosition"]?.ToString();
            if (!string.IsNullOrWhiteSpace(savedTabPosition))
            {
                group.TabPosition = savedTabPosition;
            }

            if (groupObject["snapTabHeightMargin"]?.Type == JTokenType.Boolean)
            {
                group.SnapTabHeightMargin = (bool)groupObject["snapTabHeightMargin"];
            }
        }

        private List<IntPtr> CollectRestorableWindowHandles(
            IEnumerable<JObject> windows,
            IReadOnlyDictionary<IntPtr, WindowSnapshot> windowsByHandle,
            ISet<IntPtr> assignedWindowHandles)
        {
            var validHandles = new List<IntPtr>();
            foreach (var windowToken in windows ?? Enumerable.Empty<JObject>())
            {
                var hwndValue = windowToken["hwnd"]?.Type == JTokenType.Integer
                    ? (long?)windowToken["hwnd"]
                    : null;
                if (!hwndValue.HasValue)
                {
                    continue;
                }

                var hwnd = new IntPtr(hwndValue.Value);
                if (!windowsByHandle.ContainsKey(hwnd) || assignedWindowHandles.Contains(hwnd))
                {
                    continue;
                }

                RestoreWindowState(hwnd, windowToken);
                assignedWindowHandles.Add(hwnd);
                validHandles.Add(hwnd);
            }

            return validHandles;
        }

        private void RestoreWindowState(IntPtr hwnd, JObject windowToken)
        {
            windowPresentationStateStore.SetWindowNameOverride(hwnd, windowToken["renamedTabName"]?.ToString());

            if (windowToken["isPinned"]?.Type == JTokenType.Boolean)
            {
                windowPresentationStateStore.SetPinned(hwnd, (bool)windowToken["isPinned"]);
            }

            if (TryParseColor(windowToken["tabFillColor"]?.ToString(), out var fillColor))
            {
                windowPresentationStateStore.SetFillColor(hwnd, fillColor);
            }

            if (TryParseColor(windowToken["tabUnderlineColor"]?.ToString(), out var underlineColor))
            {
                windowPresentationStateStore.SetUnderlineColor(hwnd, underlineColor);
            }

            if (TryParseColor(windowToken["tabBorderColor"]?.ToString(), out var borderColor))
            {
                windowPresentationStateStore.SetBorderColor(hwnd, borderColor);
            }

            var alignment = windowToken["tabAlignment"]?.ToString();
            if (string.Equals(alignment, "TopLeft", StringComparison.OrdinalIgnoreCase)
                || string.Equals(alignment, "Left", StringComparison.OrdinalIgnoreCase))
            {
                windowPresentationStateStore.SetAlignment(hwnd, TabAlign.TopLeft);
            }
            else if (string.Equals(alignment, "TopRight", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(alignment, "Right", StringComparison.OrdinalIgnoreCase))
            {
                windowPresentationStateStore.SetAlignment(hwnd, TabAlign.TopRight);
            }
        }

        private static string ToRrgGBbaa(Color color)
        {
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0:X2}{1:X2}{2:X2}{3:X2}",
                color.R,
                color.G,
                color.B,
                color.A);
        }

        private static bool TryParseColor(string value, out Color color)
        {
            color = default(Color);
            if (string.IsNullOrWhiteSpace(value) || value.Length != 8)
            {
                return false;
            }

            try
            {
                var r = Convert.ToInt32(value.Substring(0, 2), 16);
                var g = Convert.ToInt32(value.Substring(2, 2), 16);
                var b = Convert.ToInt32(value.Substring(4, 2), 16);
                var a = Convert.ToInt32(value.Substring(6, 2), 16);
                color = Color.FromArgb(a, r, g, b);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
