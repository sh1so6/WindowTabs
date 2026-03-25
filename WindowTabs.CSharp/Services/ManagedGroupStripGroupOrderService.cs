using System;
using System.Collections.Generic;
using System.Linq;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripGroupOrderService
    {
        private readonly GroupVisualOrderService groupVisualOrderService;
        private readonly IDesktopRuntime desktopRuntime;

        public ManagedGroupStripGroupOrderService(
            GroupVisualOrderService groupVisualOrderService,
            IDesktopRuntime desktopRuntime)
        {
            this.groupVisualOrderService = groupVisualOrderService ?? throw new ArgumentNullException(nameof(groupVisualOrderService));
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
        }

        public List<IntPtr> OrderWindowHandles(GroupSnapshot group)
        {
            if (group == null)
            {
                return new List<IntPtr>();
            }

            var runtimeGroup = desktopRuntime.FindGroup(group.GroupHandle);
            return runtimeGroup != null
                ? groupVisualOrderService.OrderWindowHandles(runtimeGroup)
                : group.WindowHandles.ToList();
        }
    }
}
