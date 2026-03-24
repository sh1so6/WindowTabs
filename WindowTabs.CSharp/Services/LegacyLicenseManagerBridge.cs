using Bemo;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyLicenseManagerBridge : ILicenseManager
    {
        public bool isLicensed => true;

        public string licenseKey
        {
            get => string.Empty;
            set { }
        }

        public void setTicketString(string ticket)
        {
        }
    }
}
