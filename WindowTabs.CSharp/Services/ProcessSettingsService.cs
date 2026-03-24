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

        public bool GetAutoGroupingEnabled(string processPath)
        {
            if (string.IsNullOrWhiteSpace(processPath))
            {
                return false;
            }

            foreach (var path in settingsSession.Current.AutoGroupingPaths)
            {
                if (string.Equals(path, processPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public void SetAutoGroupingEnabled(string processPath, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(processPath))
            {
                return;
            }

            settingsSession.Update(snapshot =>
            {
                var autoGroupingPaths = new HashSet<string>(snapshot.AutoGroupingPaths, StringComparer.OrdinalIgnoreCase);
                if (enabled)
                {
                    autoGroupingPaths.Add(processPath);
                }
                else
                {
                    autoGroupingPaths.Remove(processPath);
                }

                snapshot.AutoGroupingPaths = new List<string>(autoGroupingPaths);
            });
        }

        public bool GetCategoryEnabled(string processPath, int categoryNumber)
        {
            if (string.IsNullOrWhiteSpace(processPath) || categoryNumber < 1 || categoryNumber > 10)
            {
                return false;
            }

            foreach (var path in ReadStringArray(settingsStore.LoadRawRoot(), GetCategoryKey(categoryNumber)))
            {
                if (string.Equals(path, processPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public void SetCategoryEnabled(string processPath, int categoryNumber, bool enabled)
        {
            if (string.IsNullOrWhiteSpace(processPath) || categoryNumber < 1 || categoryNumber > 10)
            {
                return;
            }

            var root = settingsStore.LoadRawRoot();
            var categoryKey = GetCategoryKey(categoryNumber);
            var categoryPaths = new HashSet<string>(ReadStringArray(root, categoryKey), StringComparer.OrdinalIgnoreCase);
            if (enabled)
            {
                categoryPaths.Add(processPath);
            }
            else
            {
                categoryPaths.Remove(processPath);
            }

            root[categoryKey] = new JArray(categoryPaths);
            settingsStore.SaveRawRoot(root);
        }

        public int GetCategoryForProcess(string processPath)
        {
            if (string.IsNullOrWhiteSpace(processPath))
            {
                return 0;
            }

            for (var index = 1; index <= 10; index++)
            {
                if (GetCategoryEnabled(processPath, index))
                {
                    return index;
                }
            }

            return 0;
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
                var key = GetCategoryKey(index);
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

        public bool HasProcessSettings(string processPath)
        {
            if (string.IsNullOrWhiteSpace(processPath))
            {
                return false;
            }

            foreach (var configuredPath in GetAllConfiguredProcessPaths())
            {
                if (string.Equals(configuredPath, processPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public void SetCategoryForProcess(string processPath, int categoryNumber)
        {
            if (string.IsNullOrWhiteSpace(processPath))
            {
                return;
            }

            var root = settingsStore.LoadRawRoot();
            for (var index = 1; index <= 10; index++)
            {
                var key = GetCategoryKey(index);
                var categoryPaths = new HashSet<string>(ReadStringArray(root, key), StringComparer.OrdinalIgnoreCase);
                categoryPaths.Remove(processPath);
                if (index == categoryNumber)
                {
                    categoryPaths.Add(processPath);
                }

                root[key] = new JArray(categoryPaths);
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

        private static string GetCategoryKey(int categoryNumber)
        {
            return $"Category{categoryNumber}Paths";
        }
    }
}
