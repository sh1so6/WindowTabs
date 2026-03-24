using System;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyDesktopRuntimeFactory
    {
        private readonly LegacyDesktopHostFactory hostFactory;
        private readonly LegacyWindowGroupRuntimeFactory groupRuntimeFactory;

        public LegacyDesktopRuntimeFactory(
            LegacyDesktopHostFactory hostFactory,
            LegacyWindowGroupRuntimeFactory groupRuntimeFactory)
        {
            this.hostFactory = hostFactory ?? throw new ArgumentNullException(nameof(hostFactory));
            this.groupRuntimeFactory = groupRuntimeFactory ?? throw new ArgumentNullException(nameof(groupRuntimeFactory));
        }

        public IDesktopRuntime Create()
        {
            return new LegacyDesktopRuntime(hostFactory, groupRuntimeFactory);
        }
    }
}
