using System;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedDesktopRuntimeFactory
    {
        private readonly ManagedDesktopRuntime managedDesktopRuntime;

        public ManagedDesktopRuntimeFactory(ManagedDesktopRuntime managedDesktopRuntime)
        {
            this.managedDesktopRuntime = managedDesktopRuntime ?? throw new ArgumentNullException(nameof(managedDesktopRuntime));
        }

        public IDesktopRuntime Create()
        {
            return managedDesktopRuntime;
        }
    }
}
