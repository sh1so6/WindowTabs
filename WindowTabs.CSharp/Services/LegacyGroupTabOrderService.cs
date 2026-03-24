using System;
using System.Linq;
using Bemo;
using Microsoft.FSharp.Core;

namespace WindowTabs.CSharp.Services
{
    internal sealed class LegacyGroupTabOrderService
    {
        public void MoveWindowAfter(IGroup group, IntPtr windowHandle, IntPtr? insertAfterWindowHandle)
        {
            if (!(group is GroupInfo groupInfo))
            {
                return;
            }

            groupInfo.group.invokeAsync(FuncConvert.ToFSharpFunc<Unit, Unit>(_ =>
            {
                var lorder = groupInfo.group.ts.lorder.list.ToList();
                var newTab = Tab.NewTab(windowHandle);
                var currentIndex = lorder.FindIndex(tab => tab.Equals(newTab));
                if (currentIndex < 0)
                {
                    return default(Unit);
                }

                var destinationIndex = 0;
                if (insertAfterWindowHandle.HasValue)
                {
                    var insertAfterTab = Tab.NewTab(insertAfterWindowHandle.Value);
                    var insertAfterIndex = lorder.FindIndex(tab => tab.Equals(insertAfterTab));
                    if (insertAfterIndex < 0)
                    {
                        return default(Unit);
                    }

                    destinationIndex = insertAfterIndex + 1;
                }

                if (currentIndex != destinationIndex)
                {
                    groupInfo.group.ts.moveTab(newTab, destinationIndex, null);
                }

                return default(Unit);
            }));
        }
    }
}
