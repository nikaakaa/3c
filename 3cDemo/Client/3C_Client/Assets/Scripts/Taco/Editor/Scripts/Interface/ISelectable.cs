using UnityEngine;
using UnityEngine.UIElements;

namespace Taco.Editor
{
    public interface ISelectable
    {
        public ISelection SelectionContainer { get; set; }
        //
        // 摘要:
        //     Check if element is selectable.
        //
        // 返回结果:
        //     True if selectable. False otherwise.
        bool IsSelectable();

        //
        // 摘要:
        //     Check if selection overlaps rectangle.
        //
        // 参数:
        //   rectangle:
        //     Rectangle to check.
        //
        // 返回结果:
        //     True if it overlaps. False otherwise.
        bool Overlaps(Rect rectangle);

        //
        // 摘要:
        //     Select element.
        //
        // 参数:
        //   selectionContainer:
        //     Container in which selection is tracked.
        //
        //   additive:
        //     True if selection is additive. False otherwise.
        void Select();

        //
        // 摘要:
        //     Deselect element.
        //
        // 参数:
        //   selectionContainer:
        //     Container in which selection is tracked.
        void Unselect();

        //
        // 摘要:
        //     Check if element is selected.
        //
        // 参数:
        //   selectionContainer:
        //     Container in which the selection is tracked.
        //
        // 返回结果:
        //     True if selected. False otherwise.
        bool IsSelected();
    }
}