using System.Drawing;

namespace WindowTabs.CSharp.Contracts
{
    internal interface IDragDropParent
    {
        void OnDragBegin();

        void OnDragDrop(Point screenPoint, object data);

        void OnDragEnd();
    }
}
