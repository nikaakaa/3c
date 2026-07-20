using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using BTSMTL;

namespace TreeDesigner.Editor
{
    public class NodeInputFieldContainerView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<NodeInputFieldContainerView, UxmlTraits> { }

        protected BaseNode m_Node;
        protected BaseNodeView m_NodeView;
        protected Dictionary<string, VisualElement> m_FieldContainerMap = new Dictionary<string, VisualElement>();

        public NodeInputFieldContainerView()
        {
            VisualTreeAsset template = Resources.Load<VisualTreeAsset>("VisualTree/NodeInputFieldContainer");
            template.CloneTree(this);
            AddToClassList("nodeInputFieldContainer");
        }

        public void Init(BaseNode node, BaseNodeView nodeView)
        {
            m_Node = node;
            m_NodeView = nodeView;
            style.display = m_Node.Expanded ? DisplayStyle.Flex : DisplayStyle.None;
        }
        public void Refresh()
        {
            style.top = m_NodeView.InputPorts.Count > 0 ? 53 : 28;
            Clear();
            m_FieldContainerMap.Clear();
            foreach (var accessor in m_Node.GetFieldAccessors())
            {
                if (accessor.TryGetPropertyPortList(out List<PropertyPort> propertyPorts))
                {
                    foreach (var listPropertyPort in propertyPorts)
                    {
                        if (listPropertyPort == null || listPropertyPort.Direction != PortDirection.Input || !m_NodeView.InputPropertyPorts.ContainsKey(listPropertyPort.PortId))
                            continue;

                        AddInputField(listPropertyPort, accessor.IsReadOnly());
                    }
                    continue;
                }

                if (accessor.TryGetPropertyPort(out PropertyPort propertyPort) && m_NodeView.InputPropertyPorts.ContainsKey(propertyPort.PortId))
                {
                    PropertyPortAttribute propertyPortAttribute = accessor.GetAttribute<PropertyPortAttribute>();
                    if (propertyPortAttribute != null)
                    {
                        if (propertyPortAttribute.Direction == PortDirection.Input)
                        {
                            AddInputField(propertyPort, accessor.IsReadOnly());
                        }
                        SetPropertyPortFieldEnable(propertyPort.PortId, !m_Node.IsConnected(propertyPort.PortId) && !accessor.IsReadOnly());
                    }

                    VariablePropertyPortAttribute variablePropertyPortAttribute = accessor.GetAttribute<VariablePropertyPortAttribute>();
                    if (variablePropertyPortAttribute != null)
                    {
                        if (variablePropertyPortAttribute.Direction == PortDirection.Input)
                        {
                            AddInputField(propertyPort, accessor.IsReadOnly());
                            SetPropertyPortFieldEnable(propertyPort.PortId, !m_Node.IsConnected(propertyPort.PortId) && !accessor.IsReadOnly());
                        }
                    }
                }
            }
            Sort();
        }
        public void Rebind()
        {
            foreach (var item in m_FieldContainerMap)
            {
                if (item.Value.Q<PropertyField>() is PropertyField propertyField)
                {
                    propertyField.Unbind();
                    SerializedProperty serializedProperty = GetPortValueSerializedProperty(item.Key);
                    if (serializedProperty == null)
                        continue;
                    propertyField.BindProperty(serializedProperty);
                }
            }
        }
        public void Sort()
        {
            foreach (var fieldPair in m_FieldContainerMap)
            {
                Remove(fieldPair.Value);
            }
            m_FieldContainerMap = m_FieldContainerMap.OrderBy(i => m_NodeView.InputPropertyPorts.Keys.ToList().IndexOf(i.Key)).ToDictionary(i => i.Key, i => i.Value);
            foreach (var fieldPair in m_FieldContainerMap)
            {
                Add(fieldPair.Value);
            }
        }
        public void AddEmptyField(string fieldName)
        {
            VisualElement container = new VisualElement();
            container.name = "propertyFieldContainer";
            container.pickingMode = PickingMode.Ignore;
            container.style.width = 0;
            Add(container);
            m_FieldContainerMap.Add(fieldName, container);
        }
        public void AddPropertyPortField(SerializedProperty serializedProperty, PropertyPort propertyPort)
        {
            VisualElement container = new VisualElement();
            container.name = "propertyFieldContainer";
            container.pickingMode = PickingMode.Ignore;
            container.RegisterCallback<MouseDownEvent>((e) => e.StopPropagation());

            PropertyField propertyField = new PropertyField(serializedProperty, string.Empty);
            propertyField.BindProperty(serializedProperty);
            propertyField.style.borderTopColor = propertyField.style.borderBottomColor = propertyField.style.borderLeftColor = propertyField.style.borderRightColor = propertyPort.Color();
            container.Add(propertyField);

            Add(container);
            m_FieldContainerMap.Add(propertyPort.PortId, container);
        }
        void AddInputField(PropertyPort propertyPort, bool readOnly)
        {
            if (propertyPort.GetType() == typeof(PropertyPort) || (propertyPort.ValueType != null && propertyPort.ValueType.IsSubClassOfRawGeneric(typeof(List<>))))
            {
                AddEmptyField(propertyPort.PortId);
                return;
            }

            SerializedProperty serializedProperty = GetPortValueSerializedProperty(propertyPort.PortId);
            if (serializedProperty != null)
                AddPropertyPortField(serializedProperty, propertyPort);
            else
                AddEmptyField(propertyPort.PortId);

            SetPropertyPortFieldEnable(propertyPort.PortId, !m_Node.IsConnected(propertyPort.PortId) && !readOnly);
        }
        public void SetPropertyPortFieldEnable(string name, bool enable)
        {
            if (m_FieldContainerMap.TryGetValue(name, out VisualElement propertyField))
            {
                if (enable)
                {
                    if (propertyField.childCount > 0)
                        propertyField.Children().ElementAt(0).style.display = DisplayStyle.Flex;
                }
                else
                {
                    if (propertyField.childCount > 0)
                        propertyField.Children().ElementAt(0).style.display = DisplayStyle.None;
                }
            }
        }

        protected SerializedProperty GetPortValueSerializedProperty(string portId)
        {
            if (!m_Node.PropertyPortMap.TryGetValue(portId, out PropertyPort propertyPort))
                return null;

            foreach (var accessor in m_Node.GetFieldAccessors())
            {
                if (accessor.TryGetPropertyPort(out PropertyPort fieldPort) && fieldPort == propertyPort)
                    return accessor.GetSerializedProperty()?.FindPropertyRelative("m_Value");

                if (accessor.TryGetPropertyPortList(out List<PropertyPort> propertyPorts))
                {
                    int index = propertyPorts.FindIndex(i => i != null && (i == propertyPort || i.PortId == propertyPort.PortId));
                    if (index >= 0)
                        return accessor.GetSerializedProperty()?.GetArrayElementAtIndex(index)?.FindPropertyRelative("m_Value");
                }
            }
            return null;
        }
    }
}
