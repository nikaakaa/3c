using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Taco;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("RandomElement")]
    [NodePath("Base/Value/Operate/RandomElement")]
    [NodeView("VariablePropertyNodeView")]
    public class RandomElementNode : ValueNode
    {
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "List", "AcceptableTypes")]
        PropertyPort m_List = new PropertyPort();
        [SerializeReference, VariablePropertyPort(PortDirection.Output, "Element", "AcceptableTypes"), ReadOnly]
        PropertyPort m_Element = new PropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            IList list = m_List.GetValue() as IList;
            m_Element.SetValue(list[UnityEngine.Random.Range(0, list.Count)]);
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
                        SetPropertyPort("m_Element", propertyEdge.EndPort.GetType().GetElementPropertyPortType(), PortDirection.Output);
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
                        SetPropertyPort("m_List", typeof(PropertyPort), PortDirection.Output);
                        SetPropertyPort("m_Element", typeof(PropertyPort), PortDirection.Input);
                    }
                    break;
            }
        }
        public override void OnOutputPropertyLinked(PropertyEdge propertyEdge)
        {
            base.OnOutputPropertyLinked(propertyEdge);
            if (propertyEdge.StartPortName == "m_Element")
            {
                if (!IsConnected("m_List"))
                {
                    SetPropertyPort("m_List", propertyEdge.StartPort.GetType().GetListPropertyPortType(), PortDirection.Input);
                }
            }
        }
        public override void OnOutputPropertyUnLinked(PropertyEdge propertyEdge)
        {
            base.OnOutputPropertyUnLinked(propertyEdge);
            if (propertyEdge.StartPortName == "m_Element")
            {
                if (!IsConnected("m_List") && !IsConnected("m_Element"))
                {
                    SetPropertyPort("m_List", typeof(PropertyPort), PortDirection.Output);
                    SetPropertyPort("m_Element", typeof(PropertyPort), PortDirection.Input);
                }
            }
        }

        List<Type> AcceptableTypes(string name)
        {
            switch (name)
            {
                case "m_List":
                    return new List<Type> { typeof(List<>) };
                case "m_Element":
                    List<Type> acceptableTypes = new List<Type>();
                    foreach (var item in PropertyPortUtility.PropertyPortTypeMap)
                    {
                        if (!item.Value.ValueType.IsSubClassOfRawGeneric(typeof(List<>)))
                            acceptableTypes.Add(item.Value.ValueType);
                    }
                    return acceptableTypes;
                default:
                    return null;
            }
        }
#endif
    }
}