using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class SettingsStore
    {
        private readonly bool isStandalone;

        public SettingsStore(bool isStandalone)
        {
            this.isStandalone = isStandalone;
        }

        public string SettingsPath
        {
            get
            {
                var basePath = isStandalone
                    ? "."
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WindowTabs");
                return Path.Combine(basePath, "WindowTabsSettings.txt");
            }
        }

        public SettingsSnapshot Load()
        {
            var hasExistingSettings = File.Exists(SettingsPath);
            var defaults = SettingsDefaults.Create(hasExistingSettings);

            if (!hasExistingSettings)
            {
                return defaults;
            }

            try
            {
                var root = JsoncHelper.ParseObject(File.ReadAllText(SettingsPath));
                var settings = SettingsDefaults.Create(hasExistingSettings);
                settings.Version = GetString(root, "Version") ?? string.Empty;
                settings.RunAtStartup = GetBool(root, "RunAtStartup") ?? settings.RunAtStartup;
                settings.HideInactiveTabs = GetBool(root, "HideInactiveTabs") ?? settings.HideInactiveTabs;
                settings.EnableTabbingByDefault = GetBool(root, "EnableTabbingByDefault") ?? settings.EnableTabbingByDefault;
                settings.EnableCtrlNumberHotKey = GetBool(root, "EnableCtrlNumberHotKey") ?? settings.EnableCtrlNumberHotKey;
                settings.EnableHoverActivate = GetBool(root, "EnableHoverActivate") ?? settings.EnableHoverActivate;
                settings.IsDisabled = GetBool(root, "IsDisabled") ?? settings.IsDisabled;
                settings.TabPositionByDefault = NormalizeTabPosition(GetString(root, "TabPositionByDefault"));
                settings.HideTabsWhenDownByDefault = NormalizeHideTabsMode(root, settings.HideTabsWhenDownByDefault);
                settings.HideTabsDelayMilliseconds = GetInt(root, "HideTabsDelayMilliseconds") ?? settings.HideTabsDelayMilliseconds;
                settings.HideTabsOnFullscreen = GetBool(root, "HideTabsOnFullscreen") ?? settings.HideTabsOnFullscreen;
                settings.SnapTabHeightMargin = GetBool(root, "SnapTabHeightMargin") ?? settings.SnapTabHeightMargin;
                settings.LanguageName = NormalizeLanguage(GetString(root, "Language"));
                settings.IncludedPaths = GetStringList(root, "IncludedPaths");
                settings.ExcludedPaths = GetStringList(root, "ExcludedPaths");
                settings.AutoGroupingPaths = GetStringList(root, "AutoGroupingPaths");
                settings.TabAppearance = ReadTabAppearance(root["TabAppearance"] as JObject, settings.TabAppearance);
                return settings;
            }
            catch
            {
                return defaults;
            }
        }

        public JObject LoadRawRoot()
        {
            if (!File.Exists(SettingsPath))
            {
                return new JObject();
            }

            try
            {
                return JsoncHelper.ParseObject(File.ReadAllText(SettingsPath));
            }
            catch
            {
                return new JObject();
            }
        }

        public void Save(SettingsSnapshot settings)
        {
            var root = new JObject
            {
                ["Version"] = settings.Version,
                ["RunAtStartup"] = settings.RunAtStartup,
                ["HideInactiveTabs"] = settings.HideInactiveTabs,
                ["EnableTabbingByDefault"] = settings.EnableTabbingByDefault,
                ["EnableCtrlNumberHotKey"] = settings.EnableCtrlNumberHotKey,
                ["EnableHoverActivate"] = settings.EnableHoverActivate,
                ["IsDisabled"] = settings.IsDisabled,
                ["TabPositionByDefault"] = settings.TabPositionByDefault,
                ["HideTabsWhenDownByDefault"] = settings.HideTabsWhenDownByDefault,
                ["HideTabsDelayMilliseconds"] = settings.HideTabsDelayMilliseconds,
                ["HideTabsOnFullscreen"] = settings.HideTabsOnFullscreen,
                ["SnapTabHeightMargin"] = settings.SnapTabHeightMargin,
                ["Language"] = settings.LanguageName,
                ["IncludedPaths"] = new JArray(settings.IncludedPaths),
                ["ExcludedPaths"] = new JArray(settings.ExcludedPaths),
                ["AutoGroupingPaths"] = new JArray(settings.AutoGroupingPaths),
                ["TabAppearance"] = WriteTabAppearance(settings.TabAppearance)
            };

            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(SettingsPath, root.ToString());
        }

        public void SaveRawRoot(JObject root)
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(SettingsPath, (root ?? new JObject()).ToString());
        }

        private static TabAppearanceInfo ReadTabAppearance(JObject json, TabAppearanceInfo fallback)
        {
            var appearance = fallback.Clone();
            if (json == null)
            {
                return appearance;
            }

            appearance.TabHeight = GetInt(json, "tabHeight") ?? appearance.TabHeight;
            appearance.TabMaxWidth = GetInt(json, "tabMaxWidth") ?? appearance.TabMaxWidth;
            appearance.TabPinnedTabWidth = GetInt(json, "tabPinnedTabWidth") ?? appearance.TabPinnedTabWidth;
            appearance.TabPinnedTabWidthIcon = GetBool(json, "tabPinnedTabWidthIcon") ?? appearance.TabPinnedTabWidthIcon;
            appearance.TabOverlap = GetInt(json, "tabOverlap") ?? appearance.TabOverlap;
            appearance.TabHeightOffset = GetInt(json, "tabHeightOffset") ?? appearance.TabHeightOffset;
            appearance.TabIndentFlipped = GetInt(json, "tabIndentFlipped") ?? appearance.TabIndentFlipped;
            appearance.TabIndentNormal = GetInt(json, "tabIndentNormal") ?? appearance.TabIndentNormal;
            appearance.TabInactiveTextColor = GetColor(json, "tabInactiveTextColor", appearance.TabInactiveTextColor);
            appearance.TabMouseOverTextColor = GetColor(json, "tabMouseOverTextColor", appearance.TabMouseOverTextColor);
            appearance.TabActiveTextColor = GetColor(json, "tabActiveTextColor", appearance.TabActiveTextColor);
            appearance.TabFlashTextColor = GetColor(json, "tabFlashTextColor", appearance.TabFlashTextColor);
            appearance.TabInactiveTabColor = GetColor(json, "tabInactiveTabColor", appearance.TabInactiveTabColor);
            appearance.TabMouseOverTabColor = GetColor(json, "tabMouseOverTabColor", appearance.TabMouseOverTabColor);
            appearance.TabActiveTabColor = GetColor(json, "tabActiveTabColor", appearance.TabActiveTabColor);
            appearance.TabFlashTabColor = GetColor(json, "tabFlashTabColor", appearance.TabFlashTabColor);
            appearance.TabInactiveBorderColor = GetColor(json, "tabInactiveBorderColor", appearance.TabInactiveBorderColor);
            appearance.TabMouseOverBorderColor = GetColor(json, "tabMouseOverBorderColor", appearance.TabMouseOverBorderColor);
            appearance.TabActiveBorderColor = GetColor(json, "tabActiveBorderColor", appearance.TabActiveBorderColor);
            appearance.TabFlashBorderColor = GetColor(json, "tabFlashBorderColor", appearance.TabFlashBorderColor);
            return appearance;
        }

        private static JObject WriteTabAppearance(TabAppearanceInfo appearance)
        {
            return new JObject
            {
                ["tabHeight"] = appearance.TabHeight,
                ["tabMaxWidth"] = appearance.TabMaxWidth,
                ["tabPinnedTabWidth"] = appearance.TabPinnedTabWidth,
                ["tabPinnedTabWidthIcon"] = appearance.TabPinnedTabWidthIcon,
                ["tabOverlap"] = appearance.TabOverlap,
                ["tabHeightOffset"] = appearance.TabHeightOffset,
                ["tabIndentFlipped"] = appearance.TabIndentFlipped,
                ["tabIndentNormal"] = appearance.TabIndentNormal,
                ["tabInactiveTextColor"] = ColorSerialization.ToHexString(appearance.TabInactiveTextColor),
                ["tabMouseOverTextColor"] = ColorSerialization.ToHexString(appearance.TabMouseOverTextColor),
                ["tabActiveTextColor"] = ColorSerialization.ToHexString(appearance.TabActiveTextColor),
                ["tabFlashTextColor"] = ColorSerialization.ToHexString(appearance.TabFlashTextColor),
                ["tabInactiveTabColor"] = ColorSerialization.ToHexString(appearance.TabInactiveTabColor),
                ["tabMouseOverTabColor"] = ColorSerialization.ToHexString(appearance.TabMouseOverTabColor),
                ["tabActiveTabColor"] = ColorSerialization.ToHexString(appearance.TabActiveTabColor),
                ["tabFlashTabColor"] = ColorSerialization.ToHexString(appearance.TabFlashTabColor),
                ["tabInactiveBorderColor"] = ColorSerialization.ToHexString(appearance.TabInactiveBorderColor),
                ["tabMouseOverBorderColor"] = ColorSerialization.ToHexString(appearance.TabMouseOverBorderColor),
                ["tabActiveBorderColor"] = ColorSerialization.ToHexString(appearance.TabActiveBorderColor),
                ["tabFlashBorderColor"] = ColorSerialization.ToHexString(appearance.TabFlashBorderColor)
            };
        }

        private static List<string> GetStringList(JObject root, string key)
        {
            if (!(root[key] is JArray array))
            {
                return new List<string>();
            }

            return array
                .Select(token => token?.ToString())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string GetString(JObject root, string key)
        {
            return root[key]?.Type == JTokenType.Null ? null : root[key]?.ToString();
        }

        private static bool? GetBool(JObject root, string key)
        {
            return root[key]?.Type == JTokenType.Boolean ? (bool?)root[key] : null;
        }

        private static int? GetInt(JObject root, string key)
        {
            return root[key]?.Type == JTokenType.Integer ? (int?)root[key] : null;
        }

        private static System.Drawing.Color GetColor(JObject root, string key, System.Drawing.Color fallback)
        {
            var value = GetString(root, key);
            return string.IsNullOrWhiteSpace(value) ? fallback : ColorSerialization.FromHexString(value);
        }

        private static string NormalizeTabPosition(string value)
        {
            switch (value)
            {
                case "left":
                case "center":
                case "TopCenter":
                    return "TopLeft";
                case "right":
                    return "TopRight";
                default:
                    return string.IsNullOrWhiteSpace(value) ? "TopRight" : value;
            }
        }

        private static string NormalizeHideTabsMode(JObject root, string fallback)
        {
            var stringValue = GetString(root, "HideTabsWhenDownByDefault");
            if (!string.IsNullOrWhiteSpace(stringValue))
            {
                return stringValue;
            }

            var boolValue = GetBool(root, "HideTabsWhenDownByDefault");
            if (boolValue.HasValue)
            {
                return boolValue.Value ? "down" : "never";
            }

            return fallback;
        }

        private static string NormalizeLanguage(string value)
        {
            switch (value)
            {
                case "ja":
                    return "Japanese";
                case "en":
                    return "English";
                default:
                    return string.IsNullOrWhiteSpace(value) ? "English" : value;
            }
        }
    }
}
