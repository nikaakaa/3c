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
                        if (!PropertyPortAuthoringService.TryGetByDeclaration(
                                SubTreeNode,
                                exposedProperty.DeclarationId,
                                PortDirection.Input,
                                out _))
                        {
                            e.AppendAction($"{exposedProperty.Name}", (s) =>
                            {
                                if (!PropertyPortAuthoringService.TryGetPropertyPortType(
                                        ExposedPropertyUtility.TargetType(exposedProperty.GetType()),
                                        out System.Type propertyPortType))
                                    throw new System.InvalidOperationException($"Exposed property '{exposedProperty.DeclarationId}' has no PropertyPort implementation.");
                                SubTreeNode.ApplyModify("Add InputPropertyPort", () =>
                                {
                                    PropertyPort propertyPort = PropertyPortAuthoringService.AddByDeclaration(
                                        SubTreeNode,
                                        "m_InputPropertyPorts",
                                        exposedProperty.DeclarationId,
                                        exposedProperty.Name,
                                        propertyPortType,
                                        PortDirection.Input,
                                        index: exposedProperty.Index);
                                    m_InputPortContainer.AddPropertyPort(propertyPort, exposedProperty.Name, Port.Capacity.Single);
                                    m_Node.GetNewSerializedTree();
                                    Refresh();
                                    RefreshPorts();
                                    SortPropertyPorts();
                                });
                            });
                        }
                    }
                }
            }, MouseButton.LeftMouse));
            m_RemoveInputPortButton.AddManipulator(new DropdownMenuManipulator((e) =>
            {
                foreach (var propertyPort in SubTreeNode.InputPropertyPorts)
                {
                        string declarationId = propertyPort.DeclarationId;
                        e.AppendAction(propertyPort.DisplayName, (s) =>
                        {
                        SubTreeNode.ApplyModify("Remove InputPropertyPort", () =>
                        {
                            m_InputPortContainer.RemovePropertyPort(propertyPort);
                            PropertyPortAuthoringService.RemoveByDeclaration(SubTreeNode, declarationId, PortDirection.Input);
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
                if (!SubTree)
                    return;
                var exposedProperties = SubTree.ExposedProperties.OrderBy(i => i.Index).ToList();
                foreach (var exposedProperty in exposedProperties)
                {
                    if (!PropertyPortAuthoringService.TryGetByDeclaration(
                            SubTreeNode,
                            exposedProperty.DeclarationId,
                            PortDirection.Output,
                            out _))
                    {
                        e.AppendAction($"{exposedProperty.Name}", (s) =>
                        {
                            if (!PropertyPortAuthoringService.TryGetPropertyPortType(
                                    ExposedPropertyUtility.TargetType(exposedProperty.GetType()),
                                    out System.Type propertyPortType))
                                throw new System.InvalidOperationException($"Exposed property '{exposedProperty.DeclarationId}' has no PropertyPort implementation.");
                            SubTreeNode.ApplyModify("Add OutputPropertyPort", () =>
                            {
                                PropertyPort propertyPort = PropertyPortAuthoringService.AddByDeclaration(
                                        SubTreeNode,
                                        "m_OutputPropertyPorts",
                                        exposedProperty.DeclarationId,
                                        exposedProperty.Name,
                                        propertyPortType,
                                        PortDirection.Output,
                                        index: exposedProperty.Index);
                                m_OutputPortContainer.AddPropertyPort(propertyPort, exposedProperty.Name, Port.Capacity.Multi);
                                m_Node.GetNewSerializedTree();
                                Refresh();
                                RefreshPorts();
                                SortPropertyPorts();
                            });
                        });
                    }
                }
            }, MouseButton.LeftMouse));
            m_RemoveOutputPortButton.AddManipulator(new DropdownMenuManipulator((e) =>
            {
                foreach (var propertyPort in SubTreeNode.OutputPropertyPorts)
                {
                        string declarationId = propertyPort.DeclarationId;
                        e.AppendAction(propertyPort.DisplayName, (s) =>
                        {
                        SubTreeNode.ApplyModify("Remove OutputPropertyPort", () =>
                        {
                            m_OutputPortContainer.RemovePropertyPort(propertyPort);
                            PropertyPortAuthoringService.RemoveByDeclaration(SubTreeNode, declarationId, PortDirection.Output);
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
