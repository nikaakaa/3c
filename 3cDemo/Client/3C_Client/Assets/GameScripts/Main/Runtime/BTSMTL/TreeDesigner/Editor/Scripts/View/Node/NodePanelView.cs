using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using BTSMTL;
using BTSMTL.Editor;

namespace TreeDesigner.Editor
{
    public class NodePanelView : GraphElement
    {
        public new class UxmlFactory : UxmlFactory<NodePanelView, UxmlTraits> { }

        protected BaseNode m_Node;
        protected BaseNodeView m_NodeView;
        protected VisualElement m_Container;
        protected Dictionary<string, VisualElement> m_FieldMap = new Dictionary<string, VisualElement>();
        protected Dictionary<string, object> m_ValueMap = new Dictionary<string, object>();

        protected bool m_Expanded;

        public int PropertyCount => m_Container.childCount;

        public NodePanelView()
        {
            VisualTreeAsset template = Resources.Load<VisualTreeAsset>("VisualTree/NodePanel");
            template.CloneTree(this);
            AddToClassList("nodePanel");
            m_Container = this.Q("container");
            this.AddManipulator(new Clickable(() => 
            {
                m_Expanded = !m_Expanded;
                foreach (var fieldPair in m_FieldMap)
                {
                    if (fieldPair.Value.Q<Foldout>() is Foldout foldout)
                        foldout.SetValueWithoutNotify(m_Expanded);
                }
            }));
            this.AddManipulator(new DragLineManipulator(DragLineDirection.Right,(f) => 
            {
                style.width = Mathf.Max(style.width.value.value + f.x, style.minWidth.value.value);
            }));

            style.width = 0;
        }


        public void Init(BaseNode node, BaseNodeView nodeView)
        {
            m_Node = node;
            m_NodeView = nodeView;
        }
        public void Refresh()
        {
            ClearFields();
            foreach (var accessor in m_Node.GetFieldAccessors())
            {
                string fieldKey = accessor.FieldKey;

                if (!accessor.IsShow())
                    continue;

                if (accessor.TryGetPropertyPortList(out List<PropertyPort> propertyPorts))
                {
                    foreach (var propertyPort in propertyPorts)
                    {
                        if (propertyPort != null)
                            AddPropertyPortPanelField(propertyPort, propertyPort.DisplayName, accessor.IsReadOnly());
                    }
                    continue;
                }

                PropertyPortAttribute propertyPortAttribute = accessor.GetAttribute<PropertyPortAttribute>();
                VariablePropertyPortAttribute variablePropertyPortAttribute = accessor.GetAttribute<VariablePropertyPortAttribute>();
                ShowInPanelAttribute showInPanelAttribute = accessor.GetAttribute<ShowInPanelAttribute>();
                EnumMenuAttribute enumMenuAttribute = accessor.GetAttribute<EnumMenuAttribute>();
                ToggleAttribute toggleAttribute = accessor.GetAttribute<ToggleAttribute>();

                if (propertyPortAttribute != null)
                {
                    PropertyPort propertyPort = accessor.GetValue() as PropertyPort;
                    if (propertyPort == null)
                        continue;

                    if (propertyPort.GetType() == typeof(PropertyPort))
                    {

                    }
                    else
                    {
                        SerializedProperty serializedProperty = accessor.GetSerializedProperty()?.FindPropertyRelative("m_Value");
                        if(serializedProperty != null)
                        {
                            PropertyField propertyField = AddPropertyPortField(serializedProperty, propertyPort, propertyPortAttribute.Name);

                            OnValueChangedAttribute onValueChangedAttribute = accessor.GetAttribute<OnValueChangedAttribute>();
                            if (onValueChangedAttribute != null)
                            {
                                FieldInfo valueFieldInfo = propertyPort.GetField("m_Value");
                                string valueKey = $"{propertyPort.PortId}.m_Value";
                                m_ValueMap.Add(valueKey, valueFieldInfo.GetValue(propertyPort));

                                propertyField.RegisterValueChangeCallback(i =>
                                {
                                    object value = valueFieldInfo.GetValue(propertyPort);
                                    if (!Equals(value, m_ValueMap[valueKey]))
                                    {
                                        m_ValueMap[valueKey] = value;
                                        MethodInfo methodInfo = accessor.TargetObject.GetMethod(onValueChangedAttribute.CallbackName);
                                        methodInfo?.Invoke(accessor.TargetObject, null);
                                    }
                                });
                            }
                        }
                    }
                    if (propertyPort.Direction == PortDirection.Input)
                        SetPropertyPortFieldEnable(propertyPort.PortId, !m_Node.IsConnected(propertyPort.PortId));
                    else
                        SetPropertyPortFieldEnable(propertyPort.PortId, true);
                }
                else if (variablePropertyPortAttribute != null)
                {
                    PropertyPort propertyPort = accessor.GetValue() as PropertyPort;
                    if (propertyPort == null)
                        continue;

                    if (propertyPort.GetType() == typeof(PropertyPort))
                    {

                    }
                    else
                    {
                        SerializedProperty serializedProperty = accessor.GetSerializedProperty()?.FindPropertyRelative("m_Value");
                        if (serializedProperty != null)
                        {
                            PropertyField propertyField = AddPropertyPortField(serializedProperty, propertyPort, variablePropertyPortAttribute.Name);

                            OnValueChangedAttribute onValueChangedAttribute = accessor.GetAttribute<OnValueChangedAttribute>();
                            if (onValueChangedAttribute != null)
                            {
                                FieldInfo valueFieldInfo = propertyPort.GetField("m_Value");
                                string valueKey = $"{propertyPort.PortId}.m_Value";
                                m_ValueMap.Add(valueKey, valueFieldInfo.GetValue(propertyPort));

                                propertyField.RegisterValueChangeCallback(i =>
                                {
                                    object value = valueFieldInfo.GetValue(propertyPort);
                                    if (!Equals(value, m_ValueMap[valueKey]))
                                    {
                                        m_ValueMap[valueKey] = value;
                                        MethodInfo methodInfo = accessor.TargetObject.GetMethod(onValueChangedAttribute.CallbackName);
                                        methodInfo?.Invoke(accessor.TargetObject, null);
                                    }
                                });
                            }
                        }
                    }
                    if (propertyPort.Direction == PortDirection.Input)
                        SetPropertyPortFieldEnable(propertyPort.PortId, !m_Node.IsConnected(propertyPort.PortId));
                    else
                        SetPropertyPortFieldEnable(propertyPort.PortId, true);
                }
                else if (showInPanelAttribute != null)
                {
                    PropertyField propertyField = AddBaseField(accessor, showInPanelAttribute.Label);
                    if (propertyField == null)
                        continue;

                    OnValueChangedAttribute onValueChangedAttribute = accessor.GetAttribute<OnValueChangedAttribute>();
                    if (onValueChangedAttribute != null)
                    {
                        m_ValueMap.Add(fieldKey, accessor.GetValue());

                        propertyField.RegisterValueChangeCallback(i =>
                        {
                            object value = accessor.GetValue();
                            if (!Equals(value, m_ValueMap[fieldKey]))
                            {
                                m_ValueMap[fieldKey] = value;
                                MethodInfo methodInfo = accessor.TargetObject.GetMethod(onValueChangedAttribute.CallbackName);
                                methodInfo?.Invoke(accessor.TargetObject, null);
                            }
                        });
                    }
                }
                else if (enumMenuAttribute != null)
                {
                    EnumMenuView enumMenuView = new EnumMenuView();
                    enumMenuView.Init(accessor.GetValue(), enumMenuAttribute.Label, (o) =>
                    {
                        m_Node.ApplyModify("ChangeNodeValue", () =>
                        {
                            accessor.SetValue(o);
                            MethodInfo methodInfo = accessor.TargetObject.GetMethod(enumMenuAttribute.CallbackName);
                            methodInfo?.Invoke(accessor.TargetObject, null);
                        });
                    });
                    AddField(fieldKey, enumMenuView);
                }
                else if (toggleAttribute != null)
                {
                    Toggle toggle = new Toggle(toggleAttribute.Label);
                    toggle.style.height = 21;
                    toggle.style.marginTop = toggle.style.marginBottom = 2;
                    toggle.labelElement.style.minWidth = 50;
                    toggle.labelElement.style.unityTextAlign = TextAnchor.MiddleLeft;
                    toggle.value = (bool)accessor.GetValue();
                    toggle.RegisterValueChangedCallback((i) =>
                    {
                        m_Node.ApplyModify("ChangeNodeValue", () =>
                        {
                            accessor.SetValue(i.newValue);
                            MethodInfo methodInfo = accessor.TargetObject.GetMethod(toggleAttribute.CallbackName);
                            methodInfo?.Invoke(accessor.TargetObject, null);
                        });
                    });
                    AddField(fieldKey, toggle);
                }
            }
        }
        public void Rebind()
        {
            foreach (var item in m_FieldMap)
            {
                if(item.Value is PropertyField propertyField)
                {
                    propertyField.Unbind();
                    SerializedProperty serializedProperty = GetBoundSerializedProperty(item.Key);
                    if (serializedProperty == null)
                        continue;
                    propertyField.BindProperty(serializedProperty);
                }
            }
        }

        public PropertyField AddBaseField(string fieldName, string labelName)
        {
            NodeFieldAccessor? accessor = m_Node.FindFieldAccessor(fieldName);
            if (!accessor.HasValue)
                return null;
            return AddBaseField(accessor.Value, labelName);
        }
        public PropertyField AddBaseField(NodeFieldAccessor accessor, string labelName)
        {
            SerializedProperty serializedProperty = accessor.GetSerializedProperty();
            if (serializedProperty == null)
                return null;
            PropertyField field = new PropertyField(serializedProperty, labelName);
            field.BindProperty(serializedProperty);
            AddField(accessor.FieldKey, field);
            field.SetEnabled(!accessor.IsReadOnly());
            return field;
        }
        public PropertyField AddPropertyPortField(SerializedProperty serializedProperty, PropertyPort propertyPort, string labelName)
        {
            PropertyField field = new PropertyField(serializedProperty, labelName);
            field.BindProperty(serializedProperty);
            AddField(propertyPort.PortId, field);
            return field;
        }
        PropertyField AddPropertyPortPanelField(PropertyPort propertyPort, string labelName, bool readOnly)
        {
            if (propertyPort.GetType() == typeof(PropertyPort) || (propertyPort.ValueType != null && propertyPort.ValueType.IsSubClassOfRawGeneric(typeof(List<>))))
                return null;

            SerializedProperty serializedProperty = GetPortValueSerializedProperty(propertyPort.PortId);
            if (serializedProperty == null)
                return null;

            PropertyField field = AddPropertyPortField(serializedProperty, propertyPort, labelName);
            if (propertyPort.Direction == PortDirection.Input)
                field.SetEnabled(!m_Node.IsConnected(propertyPort.PortId) && !readOnly);
            else
                field.SetEnabled(!readOnly);
            return field;
        }
        public void SetPropertyPortFieldEnable(string name, bool enable)
        {
            if (m_FieldMap.TryGetValue(name, out VisualElement field))
            {
                field.SetEnabled(enable && !IsReadOnly(name));
            }
        }

        protected SerializedProperty GetBoundSerializedProperty(string key)
        {
            if (m_Node.PropertyPortMap.TryGetValue(key, out PropertyPort propertyPort))
                return GetPortValueSerializedProperty(propertyPort.PortId);

            NodeFieldAccessor? accessor = m_Node.FindFieldAccessor(key);
            return accessor.HasValue ? accessor.Value.GetSerializedProperty() : null;
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

        protected bool IsReadOnly(string key)
        {
            if (m_Node.PropertyPortMap.TryGetValue(key, out PropertyPort propertyPort))
            {
                foreach (var fieldAccessor in m_Node.GetFieldAccessors())
                {
                    if (fieldAccessor.TryGetPropertyPort(out PropertyPort fieldPort) && fieldPort == propertyPort)
                        return fieldAccessor.IsReadOnly();

                    if (fieldAccessor.TryGetPropertyPortList(out List<PropertyPort> propertyPorts) && propertyPorts.Any(i => i != null && (i == propertyPort || i.PortId == propertyPort.PortId)))
                        return fieldAccessor.IsReadOnly();
                }
                return false;
            }

            NodeFieldAccessor? accessor = m_Node.FindFieldAccessor(key);
            return accessor.HasValue && accessor.Value.IsReadOnly();
        }

        public void AddField(string name, VisualElement field)
        {
            m_Container.Add(field);
            m_FieldMap.Add(name, field);
            field.name = "nodePanelField";
            field.SetEnabled(true);
        }
        void ClearFields()
        {
            m_Container.Clear();
            m_FieldMap.Clear();
            m_ValueMap.Clear();
        }
    }
}
