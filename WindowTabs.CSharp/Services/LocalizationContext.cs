using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LocalizationContext : ILocalizationContext
    {
        public string CurrentLanguage => LocalizationService.CurrentLanguage;

        public void Initialize(string languageName)
        {
            LocalizationService.Initialize(languageName);
        }

        public void SetLanguage(string languageName)
        {
            LocalizationService.SetLanguage(languageName);
        }

        public string GetString(string key)
        {
            return LocalizationService.GetString(key);
        }
    }
}
