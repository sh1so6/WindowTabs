using System;
using Bemo;
using Microsoft.FSharp.Reflection;
using ContractsSettingsViewType = WindowTabs.CSharp.Contracts.SettingsViewType;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyManagerViewBridge : IManagerView
    {
        private readonly ManagerViewRequestDispatcher dispatcher;

        public LegacyManagerViewBridge(ManagerViewRequestDispatcher dispatcher)
        {
            this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public void show()
        {
            dispatcher.RequestShow();
        }

        public void show(SettingsViewType view)
        {
            dispatcher.RequestShow(ToContractsSettingsViewType(view));
        }

        private static ContractsSettingsViewType ToContractsSettingsViewType(SettingsViewType view)
        {
            var union = FSharpValue.GetUnionFields(view, typeof(SettingsViewType), null);
            switch (union.Item1.Name)
            {
                case "AppearanceSettings":
                    return ContractsSettingsViewType.AppearanceSettings;
                case "DiagnosticsSettings":
                    return ContractsSettingsViewType.DiagnosticsSettings;
                case "LayoutSettings":
                    return ContractsSettingsViewType.LayoutSettings;
                case "HotKeySettings":
                    return ContractsSettingsViewType.HotKeySettings;
                default:
                    return ContractsSettingsViewType.ProgramSettings;
            }
        }
    }
}
