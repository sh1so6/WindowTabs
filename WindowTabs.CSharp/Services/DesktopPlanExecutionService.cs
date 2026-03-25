using System;
using System.Linq;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class DesktopPlanExecutionService
    {
        private readonly GroupMembershipService groupMembershipService;
        private readonly DesktopSessionStateService sessionStateService;

        public DesktopPlanExecutionService(
            GroupMembershipService groupMembershipService,
            DesktopSessionStateService sessionStateService)
        {
            this.groupMembershipService = groupMembershipService ?? throw new ArgumentNullException(nameof(groupMembershipService));
            this.sessionStateService = sessionStateService ?? throw new ArgumentNullException(nameof(sessionStateService));
        }

        public void ApplyPlan(DesktopPlan plan)
        {
            if (plan == null)
            {
                return;
            }

            sessionStateService.RegisterSubscriptions(plan.WindowsToSubscribe);

            foreach (var (groupHandle, windowHandle) in plan.WindowsToRemoveFromGroups)
            {
                groupMembershipService.RemoveWindowFromGroup(groupHandle, windowHandle);
            }

            foreach (var decision in plan.WindowsToGroup)
            {
                groupMembershipService.ApplyGroupingDecision(decision);
            }
            sessionStateService.ClearDropped(plan.WindowsToGroup.Select(decision => decision.WindowHandle));

            foreach (var decision in plan.WindowsToRegroup)
            {
                groupMembershipService.ApplyRegroupingDecision(decision);
            }
            sessionStateService.ClearDropped(plan.WindowsToRegroup.Select(decision => decision.WindowHandle));

            foreach (var decision in plan.WindowsToReorder)
            {
                groupMembershipService.ApplyReorderingDecision(decision);
            }
            sessionStateService.ClearDropped(plan.WindowsToReorder.Select(decision => decision.WindowHandle));

            foreach (var groupHandle in plan.GroupsToDestroy)
            {
                groupMembershipService.DestroyGroup(groupHandle);
            }
        }
    }
}
