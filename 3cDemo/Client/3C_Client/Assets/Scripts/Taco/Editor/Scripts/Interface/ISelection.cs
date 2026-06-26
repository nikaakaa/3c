using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Taco.Editor
{
    public interface ISelection
    {
        public VisualElement ContentContainer { get; }
        public List<ISelectable> Elements { get; }
        //
        // 摘要:
        //     Get the selection.
        List<ISelectable> Selections { get; }

        //
        // 摘要:
        //     Add element to selection.
        //
        // 参数:
        //   selectable:
        //     Selectable element to add.
        void AddToSelection(ISelectable selectable);

        //
        // 摘要:
        //     Remove element from selection.
        //
        // 参数:
        //   selectable:
        //     Selectable element to remove.
        void RemoveFromSelection(ISelectable selectable);

        //
        // 摘要:
        //     Clear selection.
        void ClearSelection();
    }
}