using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using BTSMTL.Editor;

namespace TreeDesigner.Editor
{
    public class SubTreeNodeView : BaseNodeView
    {
        VisualElement m_InputPortControlContainer;
        VisualElement m_OutputPortControlContainer;
        Label m_AddInputPortButton;
        Label m_RemoveInputPortButton;
        Label m_AddOutputPortButton;
        Label m_RemoveOutputPortButton;

        public SubTreeNode SubTreeNode => m_Node as SubTreeNode;
        public SubTree SubTree => SubTreeNode.SubTree;
        public SubTreeNodeView(BaseNode node, BaseTreeWindow treeWindow) : base(node, treeWindow, AssetDatabase.GUIDToAssetPath("8d935ecb420b3ef4094ee19c709db8d7"))
        {
            m_InputPortControlContainer = this.Q("inputPort-control-container");
            m_OutputPortControlContainer = this.Q("outputPort-control-container");
            m_AddInputPortButton = m_InputPortControlContainer.Q<Label>("add-port-button");
            m_RemoveInputPortButton = m_InputPortControlContainer.Q<Label>("remove-port-button");
            m_AddOutputPortButton = m_OutputPortControlContainer.Q<Label>("add-port-button");
            m_RemoveOutputPortButton = m_OutputPortControlContainer.Q<Label>("remove-port-button");

            m_AddInputPortButton.AddManipulator(new DropdownMenuManipulator((e) =>
            {
                if (SubTree)
                {
                    var exposedProperties = SubTree.ExposedProperties.OrderBy(i => i.Index).ToList();
                    foreach (var exposedProperty in exposedProperties)
                    {
                        if (SubTreeNode.InputPropertyPorts.Find(i => i.Name == $"{exposedProperty.Name}_Input") == null)
                        {
                            e.AppendAction($"{exposedProperty.Name}", (s) =>
                            {
                                foreach (var targetTypePair in PropertyPortUtility.TargetTypeMap)
                                {
                                    if (targetTypePair.Value == ExposedPropertyUtility.TargetType(exposedProperty.GetType()))
                                    {
                                        SubTreeNode.ApplyModify("Add InputPropertyPort", () =>
                                        {
                                            PropertyPort propertyPort = SubTreeNode.AddPropertyPort("m_InputPropertyPorts", $"{exposedProperty.Name}_Input", targetTypePair.Key, PortDirection.Input);
                                            m_InputPortContainer.AddPropertyPort(propertyPort, exposedProperty.Name, Port.Capacity.Single);
                                            m_Node.GetNewSerializedTree();
                                            Refresh();
                                            RefreshPorts();
                                            SortPropertyPorts();
                                        });
                                        break;
                                    }
                                }
                            });
                        }
                    }
                }
            }, MouseButton.LeftMouse));
            m_RemoveInputPortButton.AddManipulator(new DropdownMenuManipulator((e) =>
            {
                foreach (var propertyPort in SubTreeNode.InputPropertyPorts)
                {
                    string portName = propertyPort.Name;
                    portName = portName.Substring(0, portName.Length - "_Input".Length);
                    e.AppendAction($"{portName}", (s) =>
                    {
                        SubTreeNode.ApplyModify("RemoveTagWithChildren InputPropertyPort", () =>
                        {
                            m_InputPortContainer.RemovePropertyPort(propertyPort);
                            SubTreeNode.RemovePropertyPort("m_InputPropertyPorts", propertyPort);
                            m_Node.GetNewSerializedTree();
                            Refresh();
                            RefreshPorts();
                            SortPropertyPorts();
                        });
                    });
                }
            }, MouseButton.LeftMouse));
            m_AddOutputPortButton.AddManipulator(new DropdownMenuManipulator((e) =>
            {
                var exposedProperties = SubTree.ExposedProperties.OrderBy(i => i.Index).ToList();
                foreach (var exposedProperty in exposedProperties)
                {
                    if (SubTreeNode.OutputPropertyPorts.Find(i => i.Name == $"{exposedProperty.Name}_Output") == null)
                    {
                        e.AppendAction($"{exposedProperty.Name}", (s) =>
                        {
                            foreach (var targetTypePair in PropertyPortUtility.TargetTypeMap)
                            {
                                if (targetTypePair.Value == ExposedPropertyUtility.TargetType(exposedProperty.GetType()))
                                {
                                    SubTreeNode.ApplyModify("Add OutputPropertyPort", () =>
                                    {
                                        PropertyPort propertyPort = SubTreeNode.AddPropertyPort("m_OutputPropertyPorts", $"{exposedProperty.Name}_Output", targetTypePair.Key, PortDirection.Output);
                                        m_OutputPortContainer.AddPropertyPort(propertyPort, exposedProperty.Name, Port.Capacity.Multi);
                                        m_Node.GetNewSerializedTree();
                                        Refresh();
                                        RefreshPorts();
                                        SortPropertyPorts();
                                    });
                                    break;
                                }
                            }
                        });
                    }
                }
            }, MouseButton.LeftMouse));
            m_RemoveOutputPortButton.AddManipulator(new DropdownMenuManipulator((e) =>
            {
                foreach (var propertyPort in SubTreeNode.OutputPropertyPorts)
                {
                    string portName = propertyPort.Name;
                    portName = portName.Substring(0, portName.Length - "_Output".Length);
                    e.AppendAction($"{portName}", (s) =>
                    {
                        SubTreeNode.ApplyModify("RemoveTagWithChildren OutputPropertyPort", () =>
                        {
                            m_OutputPortContainer.RemovePropertyPort(propertyPort);
                            SubTreeNode.RemovePropertyPort("m_OutputPropertyPorts", propertyPort);
                            m_Node.GetNewSerializedTree();
                            Refresh();
                            RefreshPorts();
                            SortPropertyPorts();
                        });
                    });
                }
            }, MouseButton.LeftMouse));
        }
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            if (evt.target is BaseNodeView)
            {
                evt.menu.AppendAction("Open SubTree", (s) =>
                {
                    TreeWindowUtility.TreeWindowUtilityInstance.OpenSubTreeWindow(SubTree);
                }, (DropdownMenuAction a) => SubTree ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                evt.menu.AppendSeparator();
            }
        }
        public override void Update()
        {
            base.Update();
            if (SubTree)
                title = SubTree.name;
            else
                title = "SubTreeNode";

            m_InputPortControlContainer.style.width = m_InputPortContainer.layout.width;
            m_OutputPortControlContainer.style.width = m_OutputPortContainer.layout.width;
        }
        public override void Refresh()
        {
            base.Refresh();
            UpdateSubTreeProperty();
        }
        public override void SyncSerializedPropertyPathes()
        {
            base.SyncSerializedPropertyPathes();
            UpdateSubTreeProperty();
            SortPropertyPorts();
        }
        protected override void RefreshCollapseButton()
        {
            m_CollapseButton.SetEnabled(!m_CollapseButton.enabledSelf);
            m_CollapseButton.SetEnabled(true);
        }
        protected override void GeneratePropertyPorts()
        {
            base.GeneratePropertyPorts();
            foreach (var propertyPort in SubTreeNode.InputPropertyPorts)
            {
                string valueLabel = propertyPort.Name;
                valueLabel = valueLabel.Substring(0, valueLabel.Length - "_Input".Length);
                m_InputPortContainer.AddPropertyPort(propertyPort, valueLabel, Port.Capacity.Single);
            }
            foreach (var propertyPort in SubTreeNode.OutputPropertyPorts)
            {
                string valueLabel = propertyPort.Name;
                valueLabel = valueLabel.Substring(0, valueLabel.Length - "_Output".Length);
                m_OutputPortContainer.AddPropertyPort(propertyPort, valueLabel, Port.Capacity.Multi);
            }
        }

        protected override bool CanShowPanel()
        {
            return true;
        }
        void UpdateSubTreeProperty()
        {
            


            //IMGUIContainer subTreeGUIContainer = new IMGUIContainer(() =>
            //{
            //    EditorGUI.BeginChangeCheck();
            //    EditorGUILayout.ObjectField(SubTree, typeof(BaseTree), false);
            //    if (EditorGUI.EndChangeCheck())
            //    {

            //    }
            //});
            //m_NodePanel.AddField("subTreeGUIContainer", subTreeGUIContainer);

            List<(PropertyPort, SerializedProperty)> propertyPort_SerilizedPropertyPairs = new List<(PropertyPort, SerializedProperty)>();
            for (int i = 0; i < SubTreeNode.InputPropertyPorts.Count; i++)
            {
                PropertyPort propertyPort = SubTreeNode.InputPropertyPorts[i];
                SerializedProperty serializedProperty = m_Node.GetNodeSerializedProperty("m_InputPropertyPorts");
                serializedProperty = serializedProperty.GetArrayElementAtIndex(i);
                serializedProperty = serializedProperty.FindPropertyRelative("m_Value");
                propertyPort_SerilizedPropertyPairs.Add((propertyPort, serializedProperty));

                m_NodeInputFieldContainer.AddPropertyPortField(serializedProperty, propertyPort);
                m_NodeInputFieldContainer.SetPropertyPortFieldEnable(propertyPort.PortId, !SubTreeNode.IsConnected(propertyPort.PortId));
            }
            propertyPort_SerilizedPropertyPairs = propertyPort_SerilizedPropertyPairs.OrderBy(i => i.Item1.Index).ToList();
            foreach (var propertyPort_SerilizedPropertyPair in propertyPort_SerilizedPropertyPairs)
            {
                m_NodePanel.AddPropertyPortField(propertyPort_SerilizedPropertyPair.Item2, propertyPort_SerilizedPropertyPair.Item1, propertyPort_SerilizedPropertyPair.Item1.Name);
                m_NodePanel.SetPropertyPortFieldEnable(propertyPort_SerilizedPropertyPair.Item1.PortId, !SubTreeNode.IsConnected(propertyPort_SerilizedPropertyPair.Item1.PortId));
            }

            propertyPort_SerilizedPropertyPairs.Clear();
            for (int i = 0; i < SubTreeNode.OutputPropertyPorts.Count; i++)
            {
                PropertyPort propertyPort = SubTreeNode.OutputPropertyPorts[i];
                SerializedProperty serializedProperty = m_Node.GetNodeSerializedProperty("m_OutputPropertyPorts");
                serializedProperty = serializedProperty.GetArrayElementAtIndex(i);
                serializedProperty = serializedProperty.FindPropertyRelative("m_Value");
                propertyPort_SerilizedPropertyPairs.Add((propertyPort, serializedProperty));
            }
            propertyPort_SerilizedPropertyPairs = propertyPort_SerilizedPropertyPairs.OrderBy(i => i.Item1.Index).ToList();
            foreach (var propertyPort_SerilizedPropertyPair in propertyPort_SerilizedPropertyPairs)
            {
                m_NodePanel.AddPropertyPortField(propertyPort_SerilizedPropertyPair.Item2, propertyPort_SerilizedPropertyPair.Item1, propertyPort_SerilizedPropertyPair.Item1.Name);
                m_NodePanel.SetPropertyPortFieldEnable(propertyPort_SerilizedPropertyPair.Item1.PortId, !SubTreeNode.IsConnected(propertyPort_SerilizedPropertyPair.Item1.PortId));
            }
        }
    }
}
