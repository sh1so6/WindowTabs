namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyDesktopServiceBundle
    {
        public LegacyDesktopServiceBundle(
            LegacyProgramBridge programBridge,
            LegacySettingsBridge settingsBridge,
            LegacyFilterServiceBridge filterServiceBridge,
            LegacyManagerViewBridge managerViewBridge)
        {
            ProgramBridge = programBridge;
            SettingsBridge = settingsBridge;
            FilterServiceBridge = filterServiceBridge;
            ManagerViewBridge = managerViewBridge;
        }

        public LegacyProgramBridge ProgramBridge { get; }

        public LegacySettingsBridge SettingsBridge { get; }

        public LegacyFilterServiceBridge FilterServiceBridge { get; }

        public LegacyManagerViewBridge ManagerViewBridge { get; }
    }
}
