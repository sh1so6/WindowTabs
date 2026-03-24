using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Bemo;
using Microsoft.FSharp.Core;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacySettingsBridge : ISettings
    {
        private readonly SettingsStore settingsStore;
        private readonly SettingsSession settingsSession;
        private readonly BemoSettingsValueConverter valueConverter;
        private readonly Dictionary<string, List<FSharpFunc<object, Unit>>> subscribers =
            new Dictionary<string, List<FSharpFunc<object, Unit>>>(StringComparer.Ordinal);

        public LegacySettingsBridge(
            SettingsStore settingsStore,
            SettingsSession settingsSession,
            BemoSettingsValueConverter valueConverter)
        {
            this.settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
            this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
            this.valueConverter = valueConverter ?? throw new ArgumentNullException(nameof(valueConverter));
        }

        public void setValue(Tuple<string, object> tuple)
        {
            if (tuple == null || string.IsNullOrWhiteSpace(tuple.Item1))
            {
                return;
            }

            SetValue(tuple.Item1, tuple.Item2);
        }

        public object getValue(string key)
        {
            var settings = settingsSession.Current;
            switch (key)
            {
                case "licenseKey":
                    return string.Empty;
                case "ticket":
                    return null;
                case "includedPaths":
                    return valueConverter.ToSet2(settings.IncludedPaths);
                case "excludedPaths":
                    return valueConverter.ToSet2(settings.ExcludedPaths);
                case "autoGroupingPaths":
                    return valueConverter.ToSet2(settings.AutoGroupingPaths);
                case "version":
                    return settings.Version;
                case "tabAppearance":
                    return valueConverter.ToBemoTabAppearance(settings.TabAppearance);
                case "runAtStartup":
                    return settings.RunAtStartup;
                case "hideInactiveTabs":
                    return settings.HideInactiveTabs;
                case "enableTabbingByDefault":
                    return settings.EnableTabbingByDefault;
                case "enableCtrlNumberHotKey":
                    return settings.EnableCtrlNumberHotKey;
                case "enableHoverActivate":
                    return settings.EnableHoverActivate;
                case "tabPositionByDefault":
                    return settings.TabPositionByDefault;
                case "hideTabsWhenDownByDefault":
                    return settings.HideTabsWhenDownByDefault;
                case "hideTabsDelayMilliseconds":
                    return settings.HideTabsDelayMilliseconds;
                case "hideTabsOnFullscreen":
                    return settings.HideTabsOnFullscreen;
                case "snapTabHeightMargin":
                    return settings.SnapTabHeightMargin;
                default:
                    return null;
            }
        }

        public void notifyValue(string key, FSharpFunc<object, Unit> callback)
        {
            if (string.IsNullOrWhiteSpace(key) || callback == null)
            {
                return;
            }

            if (!subscribers.TryGetValue(key, out var callbacks))
            {
                callbacks = new List<FSharpFunc<object, Unit>>();
                subscribers[key] = callbacks;
            }

            callbacks.Add(callback);
        }

        public JObject root
        {
            get => settingsStore.LoadRawRoot();
            set
            {
                settingsStore.SaveRawRoot(value ?? new JObject());
                settingsSession.Reload();
            }
        }

        private void SetValue(string key, object value)
        {
            settingsSession.Update(snapshot =>
            {
                switch (key)
                {
                    case "licenseKey":
                        break;
                    case "ticket":
                        break;
                    case "includedPaths":
                        snapshot.IncludedPaths = valueConverter.ToStringList(value as Set2<string>);
                        break;
                    case "excludedPaths":
                        snapshot.ExcludedPaths = valueConverter.ToStringList(value as Set2<string>);
                        break;
                    case "autoGroupingPaths":
                        snapshot.AutoGroupingPaths = valueConverter.ToStringList(value as Set2<string>);
                        break;
                    case "version":
                        snapshot.Version = value as string ?? string.Empty;
                        break;
                    case "tabAppearance":
                        snapshot.TabAppearance = valueConverter.ToManagedTabAppearance(value as Bemo.TabAppearanceInfo);
                        break;
                    case "runAtStartup":
                        snapshot.RunAtStartup = value is bool runAtStartup && runAtStartup;
                        break;
                    case "hideInactiveTabs":
                        snapshot.HideInactiveTabs = value is bool hideInactiveTabs && hideInactiveTabs;
                        break;
                    case "enableTabbingByDefault":
                        snapshot.EnableTabbingByDefault = value is bool enableTabbingByDefault && enableTabbingByDefault;
                        break;
                    case "enableCtrlNumberHotKey":
                        snapshot.EnableCtrlNumberHotKey = value is bool enableCtrlNumberHotKey && enableCtrlNumberHotKey;
                        break;
                    case "enableHoverActivate":
                        snapshot.EnableHoverActivate = value is bool enableHoverActivate && enableHoverActivate;
                        break;
                    case "tabPositionByDefault":
                        snapshot.TabPositionByDefault = value as string ?? snapshot.TabPositionByDefault;
                        break;
                    case "hideTabsWhenDownByDefault":
                        snapshot.HideTabsWhenDownByDefault = value as string ?? snapshot.HideTabsWhenDownByDefault;
                        break;
                    case "hideTabsDelayMilliseconds":
                        snapshot.HideTabsDelayMilliseconds = value is int delay ? delay : snapshot.HideTabsDelayMilliseconds;
                        break;
                    case "hideTabsOnFullscreen":
                        snapshot.HideTabsOnFullscreen = value is bool hideTabsOnFullscreen && hideTabsOnFullscreen;
                        break;
                    case "snapTabHeightMargin":
                        snapshot.SnapTabHeightMargin = value is bool snapTabHeightMargin && snapTabHeightMargin;
                        break;
                }
            });

            if (subscribers.TryGetValue(key, out var callbacks))
            {
                var resolvedValue = getValue(key);
                foreach (var callback in callbacks)
                {
                    callback.Invoke(resolvedValue);
                }
            }
        }
    }
}
