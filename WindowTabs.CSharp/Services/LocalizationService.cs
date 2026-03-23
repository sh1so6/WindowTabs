using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace WindowTabs.CSharp.Services
{
    internal static class LocalizationService
    {
        private static readonly object SyncRoot = new object();
        private static string currentLanguage = "English";
        private static Dictionary<string, string> loadedStrings;
        private static Dictionary<string, string> englishFallback;

        public static event EventHandler LanguageChanged;

        public static string CurrentLanguage
        {
            get
            {
                lock (SyncRoot)
                {
                    return currentLanguage;
                }
            }
        }

        public static void Initialize(string languageName)
        {
            lock (SyncRoot)
            {
                currentLanguage = NormalizeLanguageString(languageName);
                englishFallback = LoadLanguageMap("English");
                loadedStrings = LoadLanguageMap(currentLanguage);
            }
        }

        public static void SetLanguage(string languageName)
        {
            var normalized = NormalizeLanguageString(languageName);
            var changed = false;

            lock (SyncRoot)
            {
                if (string.Equals(currentLanguage, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                currentLanguage = normalized;
                loadedStrings = LoadLanguageMap(normalized);
                changed = true;
            }

            if (changed)
            {
                LanguageChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static string GetString(string key)
        {
            lock (SyncRoot)
            {
                if (loadedStrings != null && loadedStrings.TryGetValue(key, out var value))
                {
                    return value;
                }

                if (englishFallback != null && englishFallback.TryGetValue(key, out value))
                {
                    return value;
                }

                return key;
            }
        }

        private static string NormalizeLanguageString(string languageName)
        {
            switch (languageName)
            {
                case "en":
                    return "English";
                case "ja":
                    return "Japanese";
                default:
                    return string.IsNullOrWhiteSpace(languageName) ? "English" : languageName;
            }
        }

        private static string GetLanguageFolder()
        {
            var exePath = Assembly.GetExecutingAssembly().Location;
            var exeDir = Path.GetDirectoryName(exePath) ?? ".";
            return Path.Combine(exeDir, "Language");
        }

        private static Dictionary<string, string> LoadLanguageMap(string languageName)
        {
            try
            {
                var path = Path.Combine(GetLanguageFolder(), languageName + ".json");
                if (!File.Exists(path))
                {
                    return null;
                }

                var json = File.ReadAllText(path);
                var root = JsoncHelper.ParseObject(json);
                var results = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Flatten(root, string.Empty, results);
                return results;
            }
            catch
            {
                return null;
            }
        }

        private static void Flatten(JObject obj, string prefix, IDictionary<string, string> results)
        {
            foreach (var property in obj.Properties())
            {
                var key = string.IsNullOrEmpty(prefix) ? property.Name : prefix + "." + property.Name;
                if (property.Value is JObject nested)
                {
                    Flatten(nested, key, results);
                    continue;
                }

                results[key] = property.Value.ToString();
            }
        }
    }
}
