using System;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyDesktopServiceBundleFactory
    {
        private readonly LegacyProgramLifecycle lifecycle;
        private readonly LegacyAppWindowCatalog appWindowCatalog;
        private readonly LegacyNewWindowLauncher windowLauncher;
        private readonly LegacyProgramSettingsFacade programSettingsFacade;
        private readonly LegacyWindowPresentationAdapter windowPresentationAdapter;
        private readonly LegacyTabAppearanceCatalog tabAppearanceCatalog;
        private readonly LegacySettingsBridge legacySettingsBridge;
        private readonly LegacyFilterServiceBridge legacyFilterServiceBridge;
        private readonly LegacyManagerViewBridge legacyManagerViewBridge;

        public LegacyDesktopServiceBundleFactory(
            LegacyProgramLifecycle lifecycle,
            LegacyAppWindowCatalog appWindowCatalog,
            LegacyNewWindowLauncher windowLauncher,
            LegacyProgramSettingsFacade programSettingsFacade,
            LegacyWindowPresentationAdapter windowPresentationAdapter,
            LegacyTabAppearanceCatalog tabAppearanceCatalog,
            LegacySettingsBridge legacySettingsBridge,
            LegacyFilterServiceBridge legacyFilterServiceBridge,
            LegacyManagerViewBridge legacyManagerViewBridge)
        {
            this.lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            this.appWindowCatalog = appWindowCatalog ?? throw new ArgumentNullException(nameof(appWindowCatalog));
            this.windowLauncher = windowLauncher ?? throw new ArgumentNullException(nameof(windowLauncher));
            this.programSettingsFacade = programSettingsFacade ?? throw new ArgumentNullException(nameof(programSettingsFacade));
            this.windowPresentationAdapter = windowPresentationAdapter ?? throw new ArgumentNullException(nameof(windowPresentationAdapter));
            this.tabAppearanceCatalog = tabAppearanceCatalog ?? throw new ArgumentNullException(nameof(tabAppearanceCatalog));
            this.legacySettingsBridge = legacySettingsBridge ?? throw new ArgumentNullException(nameof(legacySettingsBridge));
            this.legacyFilterServiceBridge = legacyFilterServiceBridge ?? throw new ArgumentNullException(nameof(legacyFilterServiceBridge));
            this.legacyManagerViewBridge = legacyManagerViewBridge ?? throw new ArgumentNullException(nameof(legacyManagerViewBridge));
        }

        public LegacyDesktopServiceBundle Create()
        {
            var programBridge = new LegacyProgramBridge(
                lifecycle,
                appWindowCatalog,
                tabAppearanceCatalog,
                windowLauncher,
                programSettingsFacade,
                windowPresentationAdapter);

            return new LegacyDesktopServiceBundle(
                programBridge,
                legacySettingsBridge,
                legacyFilterServiceBridge,
                legacyManagerViewBridge);
        }
    }
}
