using System;
using Bemo;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyDesktopServiceRegistrar
    {
        public void Register(LegacyDesktopServiceBundle bundle)
        {
            if (bundle == null)
            {
                throw new ArgumentNullException(nameof(bundle));
            }

            GS.Services.register<IProgram>(bundle.ProgramBridge);
            GS.Services.register<ISettings>(bundle.SettingsBridge);
            GS.Services.register<IFilterService>(bundle.FilterServiceBridge);
            GS.Services.register<IManagerView>(bundle.ManagerViewBridge);
        }
    }
}
