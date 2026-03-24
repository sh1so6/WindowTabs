using System.Drawing;

namespace WindowTabs.CSharp.Contracts
{
    internal interface IDragDropTarget : IDragDropNotification
    {
        bool OnDragEnter(object data, Point clientPoint);

        void OnDragMove(Point clientPoint);

        void OnDrop(object data, Point clientPoint);

        void OnDragExit();
    }
}
