using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowTabs.CSharp.Services
{
    internal sealed class StartupComponentStatusService
    {
        private readonly Dictionary<string, string> componentErrors =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, string> ComponentErrors => componentErrors;

        public bool HasFailures => componentErrors.Count > 0;

        public void Clear()
        {
            componentErrors.Clear();
        }

        public void MarkHealthy(string componentName)
        {
            if (string.IsNullOrWhiteSpace(componentName))
            {
                return;
            }

            componentErrors.Remove(componentName);
        }

        public void MarkFailed(string componentName, Exception exception)
        {
            if (string.IsNullOrWhiteSpace(componentName) || exception == null)
            {
                return;
            }

            componentErrors[componentName] = exception.GetType().Name + ": " + exception.Message;
        }

        public string BuildSummary()
        {
            if (componentErrors.Count == 0)
            {
                return "none";
            }

            return string.Join(
                "; ",
                componentErrors.OrderBy(pair => pair.Key).Select(pair => pair.Key + "=" + pair.Value));
        }
    }
}
