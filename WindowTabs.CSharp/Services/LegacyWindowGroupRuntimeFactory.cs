using System;
using Bemo;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyWindowGroupRuntimeFactory
    {
        private readonly LegacyGroupTabOrderService tabOrderService;

        public LegacyWindowGroupRuntimeFactory(LegacyGroupTabOrderService tabOrderService)
        {
            this.tabOrderService = tabOrderService ?? throw new ArgumentNullException(nameof(tabOrderService));
        }

        public IWindowGroupRuntime Create(IGroup group)
        {
            if (group == null)
            {
                return null;
            }

            return new LegacyWindowGroupRuntime(group, tabOrderService);
        }
    }
}
