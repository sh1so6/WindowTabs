using System;
using WindowTabs.CSharp.Models;

namespace WindowTabs.CSharp.Contracts
{
    internal interface IDragDrop
    {
        void RegisterNotification(IDragDropNotification notification);

        void UnregisterNotification(IDragDropNotification notification);

        void RegisterTarget(IntPtr hwnd, IDragDropTarget target);

        void UnregisterTarget(IntPtr hwnd);

        void BeginDrag(DragBeginRequest request);
    }
}
