using System;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class DesktopRuntimeFactory
    {
        private readonly DesktopRuntimeOptions runtimeOptions;
        private readonly ManagedDesktopRuntimeFactory managedRuntimeFactory;
        private readonly LegacyDesktopRuntimeFactory legacyRuntimeFactory;
        private readonly DesktopRuntimeSelection runtimeSelection;

        public DesktopRuntimeFactory(
            DesktopRuntimeOptions runtimeOptions,
            ManagedDesktopRuntimeFactory managedRuntimeFactory,
            LegacyDesktopRuntimeFactory legacyRuntimeFactory,
            DesktopRuntimeSelection runtimeSelection)
        {
            this.runtimeOptions = runtimeOptions;
            this.managedRuntimeFactory = managedRuntimeFactory;
            this.legacyRuntimeFactory = legacyRuntimeFactory;
            this.runtimeSelection = runtimeSelection;
        }

        public IDesktopRuntime Create()
        {
            runtimeSelection.SetRequestedRuntime(runtimeOptions.RequestedRuntime);

            if (runtimeOptions.UseManagedRuntime)
            {
                var managedRuntime = managedRuntimeFactory.Create();
                runtimeSelection.SetActiveRuntime(managedRuntime.GetType().Name);
                return managedRuntime;
            }

            try
            {
                var runtime = legacyRuntimeFactory.Create();
                runtimeSelection.SetActiveRuntime(runtime.GetType().Name);
                return runtime;
            }
            catch (Exception exception)
            {
                var fallbackRuntime = managedRuntimeFactory.Create();
                runtimeSelection.SetFallback(exception, fallbackRuntime.GetType().Name);
                return fallbackRuntime;
            }
        }
    }
}
