using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using Taco.Editor;

namespace TreeDesigner.Editor
{
    public class ExposedPropertyView : VisualElement, IDragableVisualElement
    {
        protected virtual string m_VisualTreeName => "ExposedProperty";
        
        protected BaseExposedProperty m_ExposedProperty;
        public BaseExposedProperty ExposedProperty => m_ExposedProperty;

        protected BaseTreeInspectorView m_TreeInspectorView;
        protected VisualElement m_Handle;
        protected Button m_TitleButton;
        protected Button m_RemoveButton;
        protected VisualElement m_Content;
        protected DragHandle m_DragHandle;

        public BaseTree Tree => m_ExposedProperty.Owner as BaseTree;

        public ExposedPropertyView(BaseExposedProperty exposedProperty, BaseTreeInspectorView treeInspectorView)
        {
            VisualTreeAsset template = Resources.Load<VisualTreeAsset>($"VisualTree/{m_VisualTreeName}");
            template.CloneTree(this);
            AddToClassList("property");

            m_ExposedProperty = exposedProperty;
            m_TreeInspectorView = treeInspectorView;

            m_Handle = this.Q("property-handle");
            m_Handle.style.backgroundColor = m_ExposedProperty.Color();
            m_Handle.AddManipulator(new DropdownMenuManipulator(BuildContextualMenu, MouseButton.RightMouse));

            m_TitleButton = this.Q<Button>("property-title-button");
            m_TitleButton.text = m_ExposedProperty.Name;
            m_TitleButton.clicked += ToggleExpaned;

            m_RemoveButton = this.Q<Button>("property-remove-button");
            m_RemoveButton.clicked += () => 
            {                
                m_TreeInspectorView.RemoveExposedProperty(m_ExposedProperty);
            };
            if (exposedProperty.Internal)
                m_RemoveButton.style.display = DisplayStyle.None;

            m_Content = this.Q("property-content");

            m_DragHandle = new DragHandle();
            m_DragHandle.Init(m_Handle, this);


            DrawProperty();
            RefreshExpanedState();
        }

        public void DrawProperty()
        {
            m_Content.Clear();

            SerializedProperty serializedNameProperty = m_ExposedProperty.GetExposedPropertySerializedProperty("m_Name");
            PropertyField nameField = new PropertyField(serializedNameProperty, "Name");
            nameField.name = "property-name-field";
            nameField.Bind(serializedNameProperty.serializedObject);
            nameField.RegisterValueChangeCallback(i => OnNameChanged());
            m_Content.Add(nameField);
            if (m_ExposedProperty.Internal || !m_ExposedProperty.CanEdit)
                nameField.SetEnabled(false);

            string valueLabel = m_ExposedProperty.GetType().Name;
            valueLabel = valueLabel.Substring(0, valueLabel.Length - "ExposedProperty".Length);
            SerializedProperty serializedValueProperty = m_ExposedProperty.GetExposedPropertySerializedProperty("m_Value");
            PropertyField valueField = new PropertyField(serializedValueProperty, valueLabel);
            valueField.name = "property-value-field";
            valueField.Bind(serializedValueProperty.serializedObject);
            m_Content.Add(valueField);
            if (!m_ExposedProperty.CanEdit)
                valueField.SetEnabled(false);
        }
        public void Rebind()
        {
            PropertyField nameField = this.Q<PropertyField>("property-name-field");
            nameField.Unbind();
            SerializedProperty serializedNameProperty = m_ExposedProperty.GetExposedPropertySerializedProperty("m_Name");
            nameField.BindProperty(serializedNameProperty);

            PropertyField valueField = this.Q<PropertyField>("property-value-field");
            valueField.Unbind();
            SerializedProperty serializedValueProperty = m_ExposedProperty.GetExposedPropertySerializedProperty("m_Value");
            valueField.BindProperty(serializedValueProperty);
        }

        void BuildContextualMenu(DropdownMenu menu)
        {
            if (!m_ExposedProperty.Internal)
            {
                if (m_ExposedProperty.ShowOutside)
                {
                    menu.AppendAction("HideOutside", (a) =>
                    {
                        m_ExposedProperty.ShowOutside = false;
                        Tree.OnExposedPropertyChanged?.Invoke();
                    });
                }
                else
                {
                    menu.AppendAction("ShowOutside", (a) =>
                    {
                        m_ExposedProperty.ShowOutside = true;
                        Tree.OnExposedPropertyChanged?.Invoke();
                    });
                }
            }

            menu.AppendAction("Select", (a) =>
            {
                m_ExposedProperty.OnSelected?.Invoke();
            });
        }
        void ToggleExpaned()
        {
            m_TreeInspectorView.Tree.ApplyModify("SetExpandedState ExposedProperty", () =>
            {
                m_ExposedProperty.Expanded = !m_ExposedProperty.Expanded;
            });
            RefreshExpanedState();
        }
        void RefreshExpanedState()
        {
            if (m_ExposedProperty.Expanded)
                m_Content.style.display = DisplayStyle.Flex;
            else
                m_Content.style.display = DisplayStyle.None;
        }
        void OnNameChanged()
        {
            m_ExposedProperty.Name = m_TreeInspectorView.GetName(m_ExposedProperty);
            m_TitleButton.text = m_ExposedProperty.Name;
            m_ExposedProperty.OnNameChanged?.Invoke();
        }

        public void StartDrag()
        {
            AddToClassList("dragged");
        }
        public void StopDrag()
        {
            RemoveFromClassList("dragged");
        }
        public void UpdateDrag(DragUpdatedEvent e, VisualElement dragArea)
        {
            if (dragArea is BaseTreeView treeView && treeView.Tree == Tree)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
            }
        }
        public void PerformDrag(DragPerformEvent e, VisualElement dragArea)
        {
            if(dragArea is BaseTreeView treeView && treeView.Tree == Tree)
            {
                Tree.ApplyModify("Create ExposedPropertyNode", () =>
                {
                    var windowMousePosition = treeView.TreeWindow.rootVisualElement.ChangeCoordinatesTo(treeView.TreeWindow.rootVisualElement, e.originalMousePosition);
                    Vector2 localMousePosition = treeView.contentViewContainer.WorldToLocal(windowMousePosition);
                    ExposedPropertyNode exposedPropertyNode = ExposedPropertyNode.Create(m_ExposedProperty);
                    exposedPropertyNode.Position = localMousePosition;
                    treeView.CreateNodeView(exposedPropertyNode);
                });
            }
        }
    }
}
