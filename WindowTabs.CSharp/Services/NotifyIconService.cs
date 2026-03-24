using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class NotifyIconService : IDisposable
    {
        private readonly ILocalizationContext localizationContext;
        private readonly SettingsSession settingsSession;
        private readonly ManagerViewRequestDispatcher managerViewRequestDispatcher;
        private readonly RefreshCoordinator refreshCoordinator;
        private readonly AppLifecycleState appLifecycleState;
        private readonly AppBehaviorState appBehaviorState;
        private readonly AppRestartService appRestartService;
        private NotifyIcon notifyIcon;
        private ContextMenu contextMenu;
        private bool isInitialized;
        private bool isDisposed;

        public NotifyIconService(
            ILocalizationContext localizationContext,
            SettingsSession settingsSession,
            ManagerViewRequestDispatcher managerViewRequestDispatcher,
            RefreshCoordinator refreshCoordinator,
            AppLifecycleState appLifecycleState,
            AppBehaviorState appBehaviorState,
            AppRestartService appRestartService)
        {
            this.localizationContext = localizationContext ?? throw new ArgumentNullException(nameof(localizationContext));
            this.settingsSession = settingsSession ?? throw new ArgumentNullException(nameof(settingsSession));
            this.managerViewRequestDispatcher = managerViewRequestDispatcher ?? throw new ArgumentNullException(nameof(managerViewRequestDispatcher));
            this.refreshCoordinator = refreshCoordinator ?? throw new ArgumentNullException(nameof(refreshCoordinator));
            this.appLifecycleState = appLifecycleState ?? throw new ArgumentNullException(nameof(appLifecycleState));
            this.appBehaviorState = appBehaviorState ?? throw new ArgumentNullException(nameof(appBehaviorState));
            this.appRestartService = appRestartService ?? throw new ArgumentNullException(nameof(appRestartService));
        }

        public void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            isInitialized = true;
            notifyIcon = new NotifyIcon
            {
                Visible = true,
                Text = "WindowTabs C# Migration",
                Icon = LoadApplicationIcon()
            };
            notifyIcon.DoubleClick += OnNotifyIconDoubleClick;

            contextMenu = new ContextMenu();
            contextMenu.Popup += OnContextMenuPopup;
            notifyIcon.ContextMenu = contextMenu;

            RebuildMenu();
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            if (contextMenu != null)
            {
                contextMenu.Popup -= OnContextMenuPopup;
            }

            if (notifyIcon != null)
            {
                notifyIcon.DoubleClick -= OnNotifyIconDoubleClick;
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
            }
        }

        private void OnNotifyIconDoubleClick(object sender, EventArgs e)
        {
            managerViewRequestDispatcher.RequestShow();
        }

        private void OnContextMenuPopup(object sender, EventArgs e)
        {
            RebuildMenu();
        }

        private void RebuildMenu()
        {
            if (contextMenu == null)
            {
                return;
            }

            contextMenu.MenuItems.Clear();
            var settingsItem = CreateMenuItem(localizationContext.GetString("Settings"), (_, __) => managerViewRequestDispatcher.RequestShow());
            settingsItem.Enabled = !appBehaviorState.IsDisabled;
            contextMenu.MenuItems.Add(settingsItem);
            contextMenu.MenuItems.Add(CreateLanguageMenu());
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(CreateMenuItem("Refresh", (_, __) => refreshCoordinator.Refresh()));
            var disableItem = CreateMenuItem(localizationContext.GetString("Disable"), (_, __) => ToggleDisabled());
            disableItem.Checked = appBehaviorState.IsDisabled;
            contextMenu.MenuItems.Add(disableItem);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(CreateMenuItem(localizationContext.GetString("RestartWindowTabs"), (_, __) => appRestartService.Restart()));
            contextMenu.MenuItems.Add(CreateMenuItem(localizationContext.GetString("CloseWindowTabs"), (_, __) => appLifecycleState.RequestExit()));
        }

        private MenuItem CreateLanguageMenu()
        {
            var languageMenu = new MenuItem(localizationContext.GetString("Language"));
            foreach (var language in LoadLanguageList())
            {
                var item = new MenuItem(language.DisplayName)
                {
                    Checked = string.Equals(localizationContext.CurrentLanguage, language.FileName, StringComparison.OrdinalIgnoreCase),
                    Enabled = !string.Equals(localizationContext.CurrentLanguage, language.FileName, StringComparison.OrdinalIgnoreCase)
                };
                item.Click += (_, __) => ChangeLanguage(language.FileName);
                languageMenu.MenuItems.Add(item);
            }

            languageMenu.Enabled = languageMenu.MenuItems.Count > 0;
            return languageMenu;
        }

        private void ChangeLanguage(string languageName)
        {
            settingsSession.Update(snapshot => snapshot.LanguageName = languageName);
            localizationContext.SetLanguage(languageName);
            refreshCoordinator.Refresh();
            managerViewRequestDispatcher.RequestShow();
        }

        private void ToggleDisabled()
        {
            appBehaviorState.ToggleDisabled();
            refreshCoordinator.Refresh();
        }

        private static MenuItem CreateMenuItem(string text, EventHandler onClick)
        {
            var item = new MenuItem(text);
            item.Click += onClick;
            return item;
        }

        private static IEnumerable<LanguageEntry> LoadLanguageList()
        {
            var fileListPath = Path.Combine(GetLanguageFolder(), "FileList.json");
            if (!File.Exists(fileListPath))
            {
                yield break;
            }

            JArray fileList;
            try
            {
                fileList = JsoncHelper.ParseArray(File.ReadAllText(fileListPath));
            }
            catch
            {
                yield break;
            }

            foreach (var token in fileList)
            {
                if (!(token is JObject entry))
                {
                    continue;
                }

                var displayName = entry["name"]?.ToString();
                var fileName = entry["fileName"]?.ToString();
                if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(fileName))
                {
                    continue;
                }

                yield return new LanguageEntry(
                    displayName,
                    Path.GetFileNameWithoutExtension(fileName));
            }
        }

        private static string GetLanguageFolder()
        {
            var exePath = Assembly.GetExecutingAssembly().Location;
            var exeDirectory = Path.GetDirectoryName(exePath) ?? ".";
            return Path.Combine(exeDirectory, "Language");
        }

        private static Icon LoadApplicationIcon()
        {
            try
            {
                return Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location) ?? SystemIcons.Application;
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        private sealed class LanguageEntry
        {
            public LanguageEntry(string displayName, string fileName)
            {
                DisplayName = displayName;
                FileName = fileName;
            }

            public string DisplayName { get; }

            public string FileName { get; }
        }
    }
}
