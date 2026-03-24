namespace WindowTabs.CSharp.Contracts
{
    internal interface ILocalizationContext
    {
        string CurrentLanguage { get; }

        void Initialize(string languageName);

        void SetLanguage(string languageName);

        string GetString(string key);
    }
}
