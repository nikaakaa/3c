using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("ToString")]
    [NodePath("Base/Value/Operate/ToString")]
    [NodeView("VariablePropertyNodeView")]
    public partial class ToStringNode : ValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Value")]
        objectPropertyPort m_Value = new objectPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Output, "String"), ReadOnly]
        StringPropertyPort m_String = new StringPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            m_String.Value = m_Value.GetValue().ToString();
        }
    }
}