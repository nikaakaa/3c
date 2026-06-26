using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Taco;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Contains")]
    [NodePath("Base/Value/Operate/Contains")]
    [NodeView("VariablePropertyNodeView")]
    public class ContainsNode : ValueNode
    {
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "List", "AcceptableTypes")]
        PropertyPort m_List = new PropertyPort();
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "Element", "AcceptableTypes")]
        PropertyPort m_Element = new PropertyPort();
        [SerializeField, PropertyPort(PortDirection.Output, "Result")]
        BoolPropertyPort m_Result = new BoolPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            m_Result.Value = (m_List.GetValue() as IList).Contains(m_Element.GetValue());
        }

#if UNITY_EDITOR
        public override void OnInputPropertyLinked(PropertyEdge propertyEdge)
        {
            base.OnInputPropertyLinked(propertyEdge);
            switch (propertyEdge.EndPortName)
            {
                case "m_List":
                    if (!IsConnected("m_Element"))
                    {
                        SetPropertyPort("m_Element", propertyEdge.EndPort.GetType().GetElementPropertyPortType(), PortDirection.Input);
                    }
                    break;
                case "m_Element":
                    if (!IsConnected("m_List"))
                    {
                        SetPropertyPort("m_List", propertyEdge.EndPort.GetType().GetListPropertyPortType(), PortDirection.Input);
                    }
                    break;
            }
        }
        public override void OnInputPropertyUnLinked(PropertyEdge propertyEdge)
        {
            base.OnInputPropertyUnLinked(propertyEdge);
            switch (propertyEdge.EndPortName)
            {
                case "m_List":
                    if (!IsConnected("m_Element"))
                    {
                        SetPropertyPort("m_List", typeof(PropertyPort), PortDirection.Input);
                        SetPropertyPort("m_Element", typeof(PropertyPort), PortDirection.Input);
                    }
                    break;
                case "m_Element":
                    if (!IsConnected("m_List"))
                    {
                        SetPropertyPort("m_List", typeof(PropertyPort), PortDirection.Input);
                        SetPropertyPort("m_Element", typeof(PropertyPort), PortDirection.Input);
                    }
                    break;
            }
        }

        List<Type> AcceptableTypes(string name)
        {
            switch (name)
            {
                case "m_Element":
                    List<Type> acceptableTypes = new List<Type>();
                    foreach (var item in PropertyPortUtility.PropertyPortTypeMap)
                    {
                        if (!item.Value.ValueType.IsSubClassOfRawGeneric(typeof(List<>)))
                            acceptableTypes.Add(item.Value.ValueType);
                    }
                    return acceptableTypes;
                case "m_List":
                    return new List<Type> { typeof(List<>) };
                default:
                    return null;
            }
        }
#endif
    }
}