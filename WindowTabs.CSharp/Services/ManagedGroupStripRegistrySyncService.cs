using System;
using System.Collections.Generic;
using System.Linq;
using WindowTabs.CSharp.Contracts;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Services
{
    internal sealed class ManagedGroupStripRegistrySyncService
    {
        public void SyncForms(
            IDesktopRuntime desktopRuntime,
            DesktopMonitorState monitorState,
            IDictionary<IntPtr, ManagedGroupStripForm> forms,
            ManagedGroupStripFormFactory formFactory)
        {
            if (desktopRuntime == null)
            {
                throw new ArgumentNullException(nameof(desktopRuntime));
            }

            if (forms == null)
            {
                throw new ArgumentNullException(nameof(forms));
            }

            if (formFactory == null)
            {
                throw new ArgumentNullException(nameof(formFactory));
            }

            if (!(desktopRuntime is ManagedDesktopRuntime) || monitorState?.IsDisabled == true)
            {
                ClearForms(forms);
                return;
            }

            var refreshResult = monitorState?.RefreshResult ?? new DesktopRefreshResult();
            var windowsByHandle = refreshResult.Windows.ToDictionary(window => window.Handle, window => window);
            var activeGroupHandles = new HashSet<IntPtr>(refreshResult.Groups.Select(group => group.GroupHandle));

            foreach (var staleGroupHandle in forms.Keys.Where(handle => !activeGroupHandles.Contains(handle)).ToArray())
            {
                forms[staleGroupHandle].Dispose();
                forms.Remove(staleGroupHandle);
            }

            foreach (var group in refreshResult.Groups)
            {
                if (group.WindowHandles.Count == 0)
                {
                    continue;
                }

                if (!forms.TryGetValue(group.GroupHandle, out var form))
                {
                    form = formFactory.Create();
                    forms.Add(group.GroupHandle, form);
                }

                form.UpdateGroup(group, windowsByHandle);
            }
        }

        public void ClearForms(IDictionary<IntPtr, ManagedGroupStripForm> forms)
        {
            if (forms == null)
            {
                throw new ArgumentNullException(nameof(forms));
            }

            foreach (var form in forms.Values.ToArray())
            {
                form.Dispose();
            }

            forms.Clear();
        }
    }
}
