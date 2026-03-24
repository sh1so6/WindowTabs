using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Bemo;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class WorkspaceLayoutsService
    {
        private readonly SettingsStore settingsStore;
        private readonly DesktopSnapshotService desktopSnapshotService;
        private readonly FilterService filterService;
        private readonly IDesktopRuntime desktopRuntime;
        private readonly DesktopMonitoringService desktopMonitoringService;

        public WorkspaceLayoutsService(
            SettingsStore settingsStore,
            DesktopSnapshotService desktopSnapshotService,
            FilterService filterService,
            IDesktopRuntime desktopRuntime,
            DesktopMonitoringService desktopMonitoringService)
        {
            this.settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
            this.desktopSnapshotService = desktopSnapshotService ?? throw new ArgumentNullException(nameof(desktopSnapshotService));
            this.filterService = filterService ?? throw new ArgumentNullException(nameof(filterService));
            this.desktopRuntime = desktopRuntime ?? throw new ArgumentNullException(nameof(desktopRuntime));
            this.desktopMonitoringService = desktopMonitoringService ?? throw new ArgumentNullException(nameof(desktopMonitoringService));
        }

        public IReadOnlyList<WorkspaceLayout> LoadLayouts()
        {
            var root = settingsStore.LoadRawRoot();
            if (!(root["workspaces"] is JArray workspacesArray))
            {
                return Array.Empty<WorkspaceLayout>();
            }

            return workspacesArray
                .OfType<JObject>()
                .Select(DeserializeWorkspace)
                .ToList();
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

            var layoutToAdd = new WorkspaceLayout
            {
                Name = "Workspace " + nextNumber
            };

            var groups = desktopRuntime.Groups.ToList();
            for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                var runtimeGroup = groups[groupIndex];
                if (runtimeGroup.WindowHandles.Count == 0)
                {
                    continue;
                }

                var firstWindowHandle = runtimeGroup.WindowHandles[0];
                var placement = ToPlacementValue(Win32Helper.GetWindowPlacement(firstWindowHandle));
                var groupLayout = new WorkspaceGroupLayout
                {
                    Name = "Group " + (groupIndex + 1),
                    Placement = placement
                };

                for (var windowIndex = 0; windowIndex < runtimeGroup.WindowHandles.Count; windowIndex++)
                {
                    var handle = runtimeGroup.WindowHandles[windowIndex];
                    var snapshot = desktopSnapshotService.CreateWindowSnapshot(handle);
                    groupLayout.Windows.Add(new WorkspaceWindowLayout
                    {
                        Name = snapshot.Process.ExeName,
                        Title = snapshot.Text ?? string.Empty,
                        ZOrder = windowIndex,
                        MatchType = WorkspaceWindowMatchType.ExactMatch
                    });
                }

                layoutToAdd.Groups.Add(groupLayout);
            }

            var updatedLayouts = layouts.ToList();
            updatedLayouts.Insert(0, layoutToAdd);
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
            if (workspace == null)
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
                        desktopRuntime.RemoveWindow(handle);
                        WinUserApi.ShowWindow(handle, ShowWindowCommands.SW_RESTORE);
                        ApplyPlacement(handle, groupLayout.Placement);
                    }

                    ApplyZOrder(handles);

                    var runtimeGroup = desktopRuntime.CreateGroup(null);
                    foreach (var handle in handles)
                    {
                        runtimeGroup.AddWindow(handle, null);
                    }
                }
            }

            desktopMonitoringService.RefreshNow("workspace-restore");
        }

        private IReadOnlyList<WindowSnapshot> GetRestorableWindows()
        {
            var screenRegion = desktopSnapshotService.GetScreenRegion();
            return desktopSnapshotService
                .EnumerateWindowsInZOrder()
                .Where(window => filterService.IsAppWindow(window, screenRegion))
                .ToList();
        }

        private List<IntPtr> ResolveWindowHandles(WorkspaceGroupLayout groupLayout, IReadOnlyList<WindowSnapshot> windowsInZOrder)
        {
            var availableHandles = new HashSet<IntPtr>(windowsInZOrder.Select(window => window.Handle));
            var result = new List<IntPtr>();

            foreach (var windowLayout in groupLayout.Windows.OrderBy(window => window.ZOrder))
            {
                var handle = windowsInZOrder
                    .Where(window => availableHandles.Contains(window.Handle))
                    .FirstOrDefault(window => IsMatch(window.Text ?? string.Empty, windowLayout))
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

        private static bool IsMatch(string title, WorkspaceWindowLayout windowLayout)
        {
            switch (windowLayout.MatchType)
            {
                case WorkspaceWindowMatchType.ExactMatch:
                    return string.Equals(title, windowLayout.Title, StringComparison.Ordinal);
                case WorkspaceWindowMatchType.StartsWith:
                    return title.StartsWith(windowLayout.Title ?? string.Empty, StringComparison.Ordinal);
                case WorkspaceWindowMatchType.EndsWith:
                    return title.EndsWith(windowLayout.Title ?? string.Empty, StringComparison.Ordinal);
                case WorkspaceWindowMatchType.Contains:
                    return title.Contains(windowLayout.Title ?? string.Empty);
                case WorkspaceWindowMatchType.RegEx:
                    try
                    {
                        return Regex.IsMatch(title, windowLayout.Title ?? string.Empty);
                    }
                    catch
                    {
                        return false;
                    }
                default:
                    return false;
            }
        }

        private static void ApplyPlacement(IntPtr handle, WindowPlacementValue placement)
        {
            if (handle == IntPtr.Zero || placement == null)
            {
                return;
            }

            var nativePlacement = new WINDOWPLACEMENT
            {
                length = System.Runtime.InteropServices.Marshal.SizeOf(typeof(WINDOWPLACEMENT)),
                flags = placement.Flags,
                showCmd = placement.ShowCommand,
                ptMaxPosition = new POINT(placement.MaxPosition.X, placement.MaxPosition.Y),
                ptMinPosition = new POINT(placement.MinPosition.X, placement.MinPosition.Y),
                rcNormalPosition = RECT.FromRectangle(new System.Drawing.Rectangle(
                    placement.NormalPosition.X,
                    placement.NormalPosition.Y,
                    placement.NormalPosition.Width,
                    placement.NormalPosition.Height))
            };

            WinUserApi.SetWindowPlacement(handle, ref nativePlacement);
        }

        private static void ApplyZOrder(IReadOnlyList<IntPtr> handles)
        {
            if (handles == null || handles.Count < 2)
            {
                return;
            }

            var deferHandle = WinUserApi.BeginDeferWindowPos(handles.Count);
            if (deferHandle == IntPtr.Zero)
            {
                return;
            }

            var current = deferHandle;
            for (var index = 1; index < handles.Count; index++)
            {
                current = WinUserApi.DeferWindowPos(
                    current,
                    handles[index],
                    handles[index - 1],
                    0,
                    0,
                    0,
                    0,
                    SetWindowPosFlags.SWP_NOOWNERZORDER |
                    SetWindowPosFlags.SWP_NOMOVE |
                    SetWindowPosFlags.SWP_NOSIZE |
                    SetWindowPosFlags.SWP_NOACTIVATE);
            }

            WinUserApi.EndDeferWindowPos(current);
        }

        private void SaveLayouts(IReadOnlyList<WorkspaceLayout> layouts)
        {
            var root = settingsStore.LoadRawRoot();
            root["workspaces"] = new JArray(layouts.Select(SerializeWorkspace));
            settingsStore.SaveRawRoot(root);
        }

        private static JObject SerializeWorkspace(WorkspaceLayout workspace)
        {
            return new JObject
            {
                ["name"] = workspace.Name,
                ["groups"] = new JArray(workspace.Groups.Select(SerializeGroup))
            };
        }

        private static JObject SerializeGroup(WorkspaceGroupLayout group)
        {
            return new JObject
            {
                ["name"] = group.Name,
                ["placement"] = SerializePlacement(group.Placement),
                ["windows"] = new JArray(group.Windows.Select(SerializeWindow))
            };
        }

        private static JObject SerializePlacement(WindowPlacementValue placement)
        {
            return new JObject
            {
                ["showCmd"] = placement.ShowCommand,
                ["ptMaxPosition"] = SerializePoint(placement.MaxPosition),
                ["ptMinPosition"] = SerializePoint(placement.MinPosition),
                ["rcNormalPosition"] = SerializeRect(placement.NormalPosition)
            };
        }

        private static JObject SerializePoint(PointValue point)
        {
            return new JObject
            {
                ["x"] = point.X,
                ["y"] = point.Y
            };
        }

        private static JObject SerializeRect(RectValue rect)
        {
            return new JObject
            {
                ["x"] = rect.X,
                ["y"] = rect.Y,
                ["width"] = rect.Width,
                ["height"] = rect.Height
            };
        }

        private static JObject SerializeWindow(WorkspaceWindowLayout window)
        {
            return new JObject
            {
                ["name"] = window.Name,
                ["title"] = window.Title,
                ["zorder"] = window.ZOrder,
                ["matchType"] = (int)window.MatchType
            };
        }

        private static WorkspaceLayout DeserializeWorkspace(JObject workspace)
        {
            return new WorkspaceLayout
            {
                Name = workspace["name"]?.ToString() ?? string.Empty,
                Groups = workspace["groups"] is JArray groups
                    ? groups.OfType<JObject>().Select(DeserializeGroup).ToList()
                    : new List<WorkspaceGroupLayout>()
            };
        }

        private static WorkspaceGroupLayout DeserializeGroup(JObject group)
        {
            return new WorkspaceGroupLayout
            {
                Name = group["name"]?.ToString() ?? string.Empty,
                Placement = group["placement"] is JObject placement ? DeserializePlacement(placement) : new WindowPlacementValue(),
                Windows = group["windows"] is JArray windows
                    ? windows.OfType<JObject>().Select(DeserializeWindow).ToList()
                    : new List<WorkspaceWindowLayout>()
            };
        }

        private static WindowPlacementValue DeserializePlacement(JObject placement)
        {
            return new WindowPlacementValue
            {
                Flags = 0,
                ShowCommand = placement["showCmd"]?.Value<int>() ?? ShowWindowCommands.SW_RESTORE,
                MaxPosition = DeserializePoint(placement["ptMaxPosition"] as JObject),
                MinPosition = DeserializePoint(placement["ptMinPosition"] as JObject),
                NormalPosition = DeserializeRect(placement["rcNormalPosition"] as JObject)
            };
        }

        private static PointValue DeserializePoint(JObject point)
        {
            if (point == null)
            {
                return new PointValue();
            }

            return new PointValue
            {
                X = point["x"]?.Value<int>() ?? 0,
                Y = point["y"]?.Value<int>() ?? 0
            };
        }

        private static RectValue DeserializeRect(JObject rect)
        {
            if (rect == null)
            {
                return new RectValue();
            }

            return new RectValue
            {
                X = rect["x"]?.Value<int>() ?? 0,
                Y = rect["y"]?.Value<int>() ?? 0,
                Width = rect["width"]?.Value<int>() ?? 0,
                Height = rect["height"]?.Value<int>() ?? 0
            };
        }

        private static WorkspaceWindowLayout DeserializeWindow(JObject window)
        {
            return new WorkspaceWindowLayout
            {
                Name = window["name"]?.ToString() ?? string.Empty,
                Title = window["title"]?.ToString() ?? string.Empty,
                ZOrder = window["zorder"]?.Value<int>() ?? 0,
                MatchType = (WorkspaceWindowMatchType)(window["matchType"]?.Value<int>() ?? 0)
            };
        }

        private static WindowPlacementValue ToPlacementValue(WINDOWPLACEMENT placement)
        {
            return new WindowPlacementValue
            {
                Flags = placement.flags,
                ShowCommand = placement.showCmd,
                MaxPosition = new PointValue
                {
                    X = placement.ptMaxPosition.X,
                    Y = placement.ptMaxPosition.Y
                },
                MinPosition = new PointValue
                {
                    X = placement.ptMinPosition.X,
                    Y = placement.ptMinPosition.Y
                },
                NormalPosition = new RectValue
                {
                    X = placement.rcNormalPosition.Left,
                    Y = placement.rcNormalPosition.Top,
                    Width = placement.rcNormalPosition.Right - placement.rcNormalPosition.Left,
                    Height = placement.rcNormalPosition.Bottom - placement.rcNormalPosition.Top
                }
            };
        }
    }
}
