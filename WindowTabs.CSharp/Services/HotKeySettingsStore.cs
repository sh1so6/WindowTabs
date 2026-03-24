using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace WindowTabs.CSharp.Services
{
    internal sealed class HotKeySettingsStore
    {
        private readonly SettingsStore settingsStore;
        private readonly Dictionary<string, int> hotKeys = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["prevTab"] = 3621,
            ["nextTab"] = 3623
        };

        public HotKeySettingsStore(SettingsStore settingsStore)
        {
            this.settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
            Load();
        }

        public event EventHandler Changed;

        public void Set(string key, int value)
        {
            hotKeys[key] = value;
            Save();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public int Get(string key)
        {
            return hotKeys.TryGetValue(key, out var value) ? value : 0;
        }

        private void Load()
        {
            var root = settingsStore.LoadRawRoot();
            if (!(root["HotKeys"] is JObject hotKeysObject))
            {
                return;
            }

            foreach (var pair in new[] { "prevTab", "nextTab" })
            {
                if (hotKeysObject[pair]?.Type == JTokenType.Integer)
                {
                    hotKeys[pair] = hotKeysObject[pair].Value<int>();
                }
            }
        }

        private void Save()
        {
            var root = settingsStore.LoadRawRoot();
            var hotKeysObject = root["HotKeys"] as JObject ?? new JObject();
            foreach (var pair in hotKeys)
            {
                hotKeysObject[pair.Key] = pair.Value;
            }

            root["HotKeys"] = hotKeysObject;
            settingsStore.SaveRawRoot(root);
        }
    }
}
