using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace Taco.Editor
{
    public class EnumMenuView : VisualElement
    {
        protected const string m_VisualTreeName = "EnumMenu";

        public new class UxmlFactory : UxmlFactory<EnumMenuView, UxmlTraits> { }

        Label m_Label;
        public Label Label => m_Label;
        
        Label m_SelectedLabel;
        public Label SelectedLabel => m_SelectedLabel;
        public string SelectedElement => m_SelectedLabel.text;

        public event Action<object> OnSelected;
        
        List<object> m_Elements = new List<object>();

        const string m_VisualTreeAssetGUID = "734909a76aa4fc741ab924dbe7871a21";

        public EnumMenuView()
        {
            VisualTreeAsset template = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(m_VisualTreeAssetGUID), typeof(VisualTreeAsset)) as VisualTreeAsset;
            template.CloneTree(this);
            AddToClassList("dropdownMenu");

            m_Label = this.Q<Label>("label");
            m_SelectedLabel = this.Q<Label>("title");
        }

        public void Init(object selectedType, string label = null, Action<object> onSelectedCallback = null)
        {
            Array array = Enum.GetValues(selectedType.GetType());
            List<object> elements = new List<object>();
            for (int i = 0; i < array.Length; i++)
            {
                elements.Add(array.GetValue(i));
            }
            Init(elements, selectedType.ToString(), label, onSelectedCallback);
        }
        public void Init(List<object> elements, string selectedElement, string label = null, Action<object> onSelectedCallback = null)
        {
            m_Elements = elements;
            m_SelectedLabel.text = selectedElement;
            m_Label.text = label;
            OnSelected += onSelectedCallback;

            Action<DropdownMenu> dropDownMenuBuilder =
            (menu) =>
            {
                foreach (var element in m_Elements)
                {
                    menu.AppendAction(element.ToString(), (s) =>
                    {
                        m_SelectedLabel.text = element.ToString();
                        OnSelected?.Invoke(element);
                    }, (DropdownMenuAction a) => SelectedElement == element.ToString() ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
                }
            };
            m_SelectedLabel.AddManipulator(new DropdownMenuManipulator(dropDownMenuBuilder, MouseButton.LeftMouse));
        }
    }
}