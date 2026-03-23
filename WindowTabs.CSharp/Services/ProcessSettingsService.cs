using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ProcessSettingsService
    {
        private readonly SettingsSession settingsSession;
        private readonly SettingsStore settingsStore;

        public ProcessSettingsService(SettingsSession settingsSession, SettingsStore settingsStore)
        {
            this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
            this.settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        }

        public IReadOnlyCollection<string> GetAllConfiguredProcessPaths()
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in settingsSession.Current.IncludedPaths)
            {
                paths.Add(path);
            }

            foreach (var path in settingsSession.Current.ExcludedPaths)
            {
                paths.Add(path);
            }

            foreach (var path in settingsSession.Current.AutoGroupingPaths)
            {
                paths.Add(path);
            }

            var root = settingsStore.LoadRawRoot();
            for (var index = 1; index <= 10; index++)
            {
                foreach (var path in ReadStringArray(root, $"Category{index}Paths"))
                {
                    paths.Add(path);
                }
            }

            return new List<string>(paths);
        }

        public void RemoveProcessSettings(string processPath)
        {
            if (string.IsNullOrWhiteSpace(processPath))
            {
                return;
            }

            settingsSession.Update(snapshot =>
            {
                snapshot.IncludedPaths.RemoveAll(path => string.Equals(path, processPath, StringComparison.OrdinalIgnoreCase));
                snapshot.ExcludedPaths.RemoveAll(path => string.Equals(path, processPath, StringComparison.OrdinalIgnoreCase));
                snapshot.AutoGroupingPaths.RemoveAll(path => string.Equals(path, processPath, StringComparison.OrdinalIgnoreCase));
            });

            var root = settingsStore.LoadRawRoot();
            for (var index = 1; index <= 10; index++)
            {
                var key = $"Category{index}Paths";
                var filtered = new JArray();
                foreach (var path in ReadStringArray(root, key))
                {
                    if (!string.Equals(path, processPath, StringComparison.OrdinalIgnoreCase))
                    {
                        filtered.Add(path);
                    }
                }

                root[key] = filtered;
            }

            settingsStore.SaveRawRoot(root);
        }

        private static IEnumerable<string> ReadStringArray(JObject root, string key)
        {
            if (!(root[key] is JArray array))
            {
                yield break;
            }

            foreach (var token in array)
            {
                var value = token?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    yield return value;
                }
            }
        }
    }
}
