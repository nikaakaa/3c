using System.Collections.Generic;
using UnityEngine.UIElements;

namespace BTSMTL.Editor
{
    public interface ISelection
    {
        public VisualElement ContentContainer { get; }
        public List<ISelectable> Elements { get; }
        //
        // ժҪ:
        //     Get the selection.
        List<ISelectable> Selections { get; }

        //
        // ժҪ:
        //     Add element to selection.
        //
        // ����:
        //   selectable:
        //     Selectable element to add.
        void AddToSelection(ISelectable selectable);

        //
        // ժҪ:
        //     Remove element from selection.
        //
        // ����:
        //   selectable:
        //     Selectable element to remove.
        void RemoveFromSelection(ISelectable selectable);

        //
        // ժҪ:
        //     Clear selection.
        void ClearSelection();
    }
}