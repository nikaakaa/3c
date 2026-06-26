using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using Taco.Editor;
using Taco;

namespace TreeDesigner.Editor
{
    public class TreeValueNodeView : BaseNodeView
    {
        Label m_ExposedPropertyDropdownButton;
        EnumMenuView m_NodeTypeDropdownMenuView;

        public TreeValueNode TreeValueNode => m_Node as TreeValueNode;
        public BaseTree TargetTree => TreeValueNode.Tree ?? TreeValueNode.PreviewTree;

        public TreeValueNodeView(BaseNode node, BaseTreeWindow treeWindow) : base(node, treeWindow, AssetDatabase.GUIDToAssetPath("e6aa85bab195b9b4b812bce8e95e2742"))
        {
            m_ExposedPropertyDropdownButton = this.Q<Label>("property-dropdown-button");
            m_ExposedPropertyDropdownButton.AddManipulator(new DropdownMenuManipulator(ChangeExposedPropertyMenu, MouseButton.LeftMouse));

            m_NodeTypeDropdownMenuView = this.Q<EnumMenuView>("nodeType-field");
            m_NodeTypeDropdownMenuView.Init(TreeValueNode.NodeType, string.Empty, ChangeNodeTypeMenu);

            OnNodeTypeChanged();
            OnExposedPropertyChanged();
        }

        public override void Refresh()
        {            
            base.Refresh();
            UpdateTreeValeProperty();
        }
        public override void SyncSerializedPropertyPathes()
        {
            base.SyncSerializedPropertyPathes();
            UpdateTreeValeProperty();
            SortPropertyPorts();
        }

        protected override void RefreshCollapseButton()
        {
            m_CollapseButton.SetEnabled(!m_CollapseButton.enabledSelf);
            m_CollapseButton.SetEnabled(true);
        }
        protected override void GeneratePorts()
        {
            base.GeneratePorts();
            switch (TreeValueNode.NodeType)
            {
                case TreeValueNodeType.Get:

                    break;
                case TreeValueNodeType.Set:
                    m_InputPortContainer.AddPort("Input", Direction.Input, Port.Capacity.Single);
                    break;
            }
        }
        protected override void GeneratePropertyPorts()
        {
            base.GeneratePropertyPorts();
            switch (TreeValueNode.NodeType)
            {
                case TreeValueNodeType.Get:
                    m_OutputPortContainer.AddPropertyPort(TreeValueNode.Value, "Value", Port.Capacity.Multi);
                    break;
                case TreeValueNodeType.Set:
                    m_InputPortContainer.AddPropertyPort(TreeValueNode.Value, "Value", Port.Capacity.Single);
                    break;
            }
        }


        void ChangeExposedPropertyMenu(DropdownMenu menu)
        {
            if (TargetTree)
            {
                BaseExposedProperty selectedExposedProperty = TargetTree.GetExposedProperty(TreeValueNode.ExposedPropertyName);
                foreach (var exposedProperty in TargetTree.ExposedProperties.OrderBy(i => i.Index))
                {
                    menu.AppendAction(exposedProperty.Name, (a) =>
                    {
                        if (exposedProperty != selectedExposedProperty)
                        {
                            m_Node.ApplyModify("Change ExposedProperty", () =>
                            {
                                if (!selectedExposedProperty || exposedProperty.GetType() != selectedExposedProperty.GetType())
                                {
                                    switch (TreeValueNode.NodeType)
                                    {
                                        case TreeValueNodeType.Get:
                                            TreeView.DeleteElements(OutputPropertyPorts["m_Value"].connections);
                                            TreeValueNode.SetExposedProperty(exposedProperty);
                                            OutputPropertyPorts["m_Value"].SetPropertyPort(TreeValueNode.Value);
                                            break;
                                        case TreeValueNodeType.Set:
                                            TreeView.DeleteElements(InputPropertyPorts["m_Value"].connections);
                                            TreeValueNode.SetExposedProperty(exposedProperty);
                                            InputPropertyPorts["m_Value"].SetPropertyPort(TreeValueNode.Value);
                                            break;
                                    }
                                }
                                else
                                {
                                    switch (TreeValueNode.NodeType)
                                    {
                                        case TreeValueNodeType.Get:
                                            TreeValueNode.SetExposedPropertyWithoutChangePropertyPort(exposedProperty);
                                            break;
                                        case TreeValueNodeType.Set:
                                            TreeValueNode.SetExposedPropertyWithoutChangePropertyPort(exposedProperty);
                                            break;
                                    }
                                }
                                m_Node.GetNewSerializedTree();
                                OnExposedPropertyChanged();
                                SortPropertyPorts();
                                Refresh();
                            });
                        }
                    }, (DropdownMenuAction a) => exposedProperty != selectedExposedProperty ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Checked);
                }

            }
            menu.AppendAction("Empty", (a) =>
            {
                if (!string.IsNullOrEmpty(TreeValueNode.ExposedPropertyName))
                {
                    m_Node.ApplyModify("Change ExposedProperty", () =>
                    {
                        OnExposedPropertyRemoved();
                    });
                }
            }, (DropdownMenuAction a) => string.IsNullOrEmpty(TreeValueNode.ExposedPropertyName) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        }
        void ChangeNodeTypeMenu(object value)
        {
            TreeValueNode.ApplyModify("ChangeNodeType", () =>
            {
                TreeValueNodeType selectedNodeType = (TreeValueNodeType)value;
                if (TreeValueNode.NodeType != selectedNodeType)
                {
                    switch (TreeValueNode.NodeType)
                    {
                        case TreeValueNodeType.Get:
                            m_OutputPortContainer.RemovePropertyPort(TreeValueNode.Value);
                            break;
                        case TreeValueNodeType.Set:
                            m_InputPortContainer.RemovePort("Input");
                            m_InputPortContainer.RemovePropertyPort(TreeValueNode.Value);
                            break;
                    }
                    switch (selectedNodeType)
                    {
                        case TreeValueNodeType.Get:
                            TreeValueNode.Value.Direction = PortDirection.Output;
                            break;
                        case TreeValueNodeType.Set:
                            TreeValueNode.Value.Direction = PortDirection.Input;
                            break;
                    }
                    switch (selectedNodeType)
                    {
                        case TreeValueNodeType.Get:
                            m_OutputPortContainer.AddPropertyPort(TreeValueNode.Value, "Value", Port.Capacity.Multi);
                            break;
                        case TreeValueNodeType.Set:
                            m_InputPortContainer.AddPort("Input", Direction.Input, Port.Capacity.Single);
                            m_InputPortContainer.AddPropertyPort(TreeValueNode.Value, "Value", Port.Capacity.Single);
                            break;
                    }
                    TreeValueNode.SetNodeType(selectedNodeType);
                    Refresh();
                    RefreshPorts();
                    OnNodeTypeChanged();
                }
            });
        }
        void UpdateTreeValeProperty()
        {
            if (TreeValueNode.Value.GetType() == typeof(PropertyPort))
            {

            }
            else
            {
                SerializedProperty serializedProperty = m_Node.GetNodeSerializedProperty("m_Value").FindPropertyRelative("m_Value");
                m_NodePanel.AddPropertyPortField(serializedProperty, TreeValueNode.Value, "Value");
                if (TreeValueNode.NodeType == TreeValueNodeType.Get)
                    m_NodePanel.SetPropertyPortFieldEnable(TreeValueNode.Value.PortId, false);

                if (TreeValueNode.NodeType == TreeValueNodeType.Set)
                {
                    if (TreeValueNode.Value.ValueType.IsSubClassOfRawGeneric(typeof(List<>)))
                        m_NodeInputFieldContainer.AddEmptyField(TreeValueNode.Value.PortId);
                    else
                    {
                        m_NodeInputFieldContainer.AddPropertyPortField(serializedProperty, TreeValueNode.Value);
                        m_NodeInputFieldContainer.SetPropertyPortFieldEnable(TreeValueNode.Value.PortId, !TreeValueNode.IsConnected(TreeValueNode.Value.PortId));
                    }
                }
                RefreshShowPanelState();
            }
        }

        void OnNodeTypeChanged()
        {
            RemoveFromClassList("get");
            RemoveFromClassList("set");
            switch (TreeValueNode.NodeType)
            {
                case TreeValueNodeType.Get:
                    AddToClassList("get");
                    titleContainer.style.backgroundColor = new Color(74, 42, 192, 255) / 255;
                    break;
                case TreeValueNodeType.Set:
                    AddToClassList("set");
                    titleContainer.style.backgroundColor = new Color(239, 71, 111, 255) / 255;
                    break;
            }
        }
        void OnExposedPropertyChanged()
        {
            if (string.IsNullOrEmpty(TreeValueNode.ExposedPropertyName))
            {
                m_ExposedPropertyDropdownButton.text = "Empty";
            }
            else
            {
                m_ExposedPropertyDropdownButton.text = TreeValueNode.ExposedPropertyName;

            }
        }
        void OnExposedPropertyRemoved()
        {
            switch (TreeValueNode.NodeType)
            {
                case TreeValueNodeType.Get:
                    TreeView.DeleteElements(OutputPropertyPorts["m_Value"].connections);
                    TreeValueNode.RemoveExposedProperty();
                    OutputPropertyPorts["m_Value"].SetPropertyPort(TreeValueNode.Value);
                    break;
                case TreeValueNodeType.Set:
                    TreeView.DeleteElements(InputPropertyPorts["m_Value"].connections);
                    TreeValueNode.RemoveExposedProperty();
                    InputPropertyPorts["m_Value"].SetPropertyPort(TreeValueNode.Value);
                    break;
            }
            OnExposedPropertyChanged();
            Refresh();
        }
    }
}
