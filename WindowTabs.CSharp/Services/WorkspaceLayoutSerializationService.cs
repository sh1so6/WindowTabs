using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class WorkspaceLayoutSerializationService
    {
        public IReadOnlyList<WorkspaceLayout> DeserializeWorkspaces(JToken workspacesToken)
        {
            if (!(workspacesToken is JArray workspacesArray))
            {
                return new List<WorkspaceLayout>();
            }

            return workspacesArray
                .OfType<JObject>()
                .Select(DeserializeWorkspace)
                .ToList();
        }

        public JArray SerializeWorkspaces(IReadOnlyList<WorkspaceLayout> layouts)
        {
            return new JArray((layouts ?? new List<WorkspaceLayout>()).Select(SerializeWorkspace));
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
            return new WorkspaceLayout(
                workspace["name"]?.ToString() ?? string.Empty,
                workspace["groups"] is JArray groups
                    ? groups.OfType<JObject>().Select(DeserializeGroup).ToList()
                    : new List<WorkspaceGroupLayout>());
        }

        private static WorkspaceGroupLayout DeserializeGroup(JObject group)
        {
            return new WorkspaceGroupLayout(
                group["name"]?.ToString() ?? string.Empty,
                group["placement"] is JObject placement ? DeserializePlacement(placement) : new WindowPlacementValue(),
                group["windows"] is JArray windows
                    ? windows.OfType<JObject>().Select(DeserializeWindow).ToList()
                    : new List<WorkspaceWindowLayout>());
        }

        private static WindowPlacementValue DeserializePlacement(JObject placement)
        {
            return new WindowPlacementValue
            {
                Flags = 0,
                ShowCommand = placement["showCmd"]?.Value<int>() ?? NativeWindowApi.SwRestore,
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
            return new WorkspaceWindowLayout(
                window["name"]?.ToString() ?? string.Empty,
                window["title"]?.ToString() ?? string.Empty,
                window["zorder"]?.Value<int>() ?? 0,
                (WorkspaceWindowMatchType)(window["matchType"]?.Value<int>() ?? 0));
        }
    }
}
