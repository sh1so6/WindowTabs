using System;
using System.Collections.Generic;
using System.Linq;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class WorkspaceLayoutsService
    {
        private readonly SettingsStore settingsStore;
        private readonly DesktopWindowCatalogService desktopWindowCatalogService;
        private readonly DesktopSnapshotService desktopSnapshotService;
        private readonly IDesktopRuntime desktopRuntime;
        private readonly GroupMembershipService groupMembershipService;
        private readonly DesktopMonitoringService desktopMonitoringService;
        private readonly WorkspaceWindowMatchService workspaceWindowMatchService;
        private readonly WorkspaceLayoutSerializationService workspaceLayoutSerializationService;

        public WorkspaceLayoutsService(
            SettingsStore settingsStore,
            DesktopWindowCatalogService desktopWindowCatalogService,
            DesktopSnapshotService desktopSnapshotService,
            IDesktopRuntime desktopRuntime,
            GroupMembershipService groupMembershipService,
            DesktopMonitoringService desktopMonitoringService,
            WorkspaceWindowMatchService workspaceWindowMatchService,
            WorkspaceLayoutSerializationService workspaceLayoutSerializationService)
        {
            this.settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
            this.desktopWindowCatalogService = desktopWindowCatalogService ?? throw new ArgumentNullException(nameof(desktopWindowCatalogService));
            this.desktopSnapshotService = desktopSnapshotService ?? throw new ArgumentNullException(nameof(desktopSnapshotService));
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
            this.groupMembershipService = groupMembershipService ?? throw new ArgumentNullException(nameof(groupMembershipService));
            this.desktopMonitoringService = desktopMonitoringService ?? throw new ArgumentNullException(nameof(desktopMonitoringService));
            this.workspaceWindowMatchService = workspaceWindowMatchService ?? throw new ArgumentNullException(nameof(workspaceWindowMatchService));
            this.workspaceLayoutSerializationService = workspaceLayoutSerializationService ?? throw new ArgumentNullException(nameof(workspaceLayoutSerializationService));
        }

        public IReadOnlyList<WorkspaceLayout> LoadLayouts()
        {
            var root = settingsStore.LoadRawRoot();
            return workspaceLayoutSerializationService.DeserializeWorkspaces(root["workspaces"]);
        }

        public WorkspaceLayout CreateFromCurrentDesktop()
        {
            var layouts = LoadLayouts();
            var nextNumber = 1;
            foreach (var layout in layouts)
            {
                if (layout.Name.StartsWith("Workspace ", StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(layout.Name.Substring("Workspace ".Length), out var number))
                {
                    nextNumber = Math.Max(nextNumber, number + 1);
                }
            }

            List<WorkspaceGroupLayout> groupLayouts = [];
            var groups = desktopRuntime.Groups.ToList();
            for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                var runtimeGroup = groups[groupIndex];
                if (runtimeGroup.WindowHandles.Count == 0)
                {
                    continue;
                }

                var firstWindowHandle = runtimeGroup.WindowHandles[0];
                var placement = NativeWindowApi.GetWindowPlacementValue(firstWindowHandle);
                List<WorkspaceWindowLayout> windowLayouts = [];

                for (var windowIndex = 0; windowIndex < runtimeGroup.WindowHandles.Count; windowIndex++)
                {
                    var handle = runtimeGroup.WindowHandles[windowIndex];
                    var snapshot = desktopSnapshotService.CreateWindowSnapshot(handle);
                    windowLayouts.Add(new WorkspaceWindowLayout(
                        snapshot.Process.ExeName,
                        snapshot.Text ?? string.Empty,
                        windowIndex,
                        WorkspaceWindowMatchType.ExactMatch));
                }

                groupLayouts.Add(new WorkspaceGroupLayout(
                    "Group " + (groupIndex + 1),
                    placement,
                    windowLayouts));
            }

            var layoutToAdd = new WorkspaceLayout(
                "Workspace " + nextNumber,
                groupLayouts);

            List<WorkspaceLayout> updatedLayouts = [layoutToAdd, .. layouts];
            SaveLayouts(updatedLayouts);
            return layoutToAdd;
        }

        public void RemoveWorkspace(string workspaceName)
        {
            if (string.IsNullOrWhiteSpace(workspaceName))
            {
                return;
            }

            var updatedLayouts = LoadLayouts()
                .Where(layout => !string.Equals(layout.Name, workspaceName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            SaveLayouts(updatedLayouts);
        }

        public void RestoreWorkspace(WorkspaceLayout workspace)
        {
            if (workspace is null)
            {
                return;
            }

            var windowsInZOrder = GetRestorableWindows();
            using (desktopMonitoringService.SuspendRefresh())
            {
                foreach (var groupLayout in workspace.Groups)
                {
                    var handles = ResolveWindowHandles(groupLayout, windowsInZOrder);
                    if (handles.Count == 0)
                    {
                        continue;
                    }

                    foreach (var handle in handles)
                    {
                        groupMembershipService.RemoveWindow(handle);
                        NativeWindowApi.RestoreWindow(handle);
                        ApplyPlacement(handle, groupLayout.Placement);
                    }

                    ApplyZOrder(handles);
                    groupMembershipService.CreateGroupWithWindows(handles, null);
                }
            }

            desktopMonitoringService.RefreshNow("workspace-restore");
        }

        private IReadOnlyList<WindowSnapshot> GetRestorableWindows() => desktopWindowCatalogService.GetRestorableWindowsInZOrder();

        private List<IntPtr> ResolveWindowHandles(WorkspaceGroupLayout groupLayout, IReadOnlyList<WindowSnapshot> windowsInZOrder)
        {
            var availableHandles = new HashSet<IntPtr>(windowsInZOrder.Select(window => window.Handle));
            List<IntPtr> result = [];

            foreach (var windowLayout in groupLayout.Windows.OrderBy(window => window.ZOrder))
            {
                var handle = windowsInZOrder
                    .Where(window => availableHandles.Contains(window.Handle))
                    .FirstOrDefault(window => workspaceWindowMatchService.IsMatch(window, windowLayout))
                    ?.Handle ?? IntPtr.Zero;
                if (handle == IntPtr.Zero)
                {
                    continue;
                }

                availableHandles.Remove(handle);
                result.Add(handle);
            }

            return result;
        }

        private static void ApplyPlacement(IntPtr handle, WindowPlacementValue placement)
        {
            if (handle == IntPtr.Zero || placement is null)
            {
                return;
            }

            NativeWindowApi.SetWindowPlacementValue(handle, placement);
        }

        private static void ApplyZOrder(IReadOnlyList<IntPtr> handles) => NativeWindowApi.ApplyZOrder(handles);

        private void SaveLayouts(IReadOnlyList<WorkspaceLayout> layouts)
        {
            var root = settingsStore.LoadRawRoot();
            root["workspaces"] = workspaceLayoutSerializationService.SerializeWorkspaces(layouts);
            settingsStore.SaveRawRoot(root);
        }
    }
}
