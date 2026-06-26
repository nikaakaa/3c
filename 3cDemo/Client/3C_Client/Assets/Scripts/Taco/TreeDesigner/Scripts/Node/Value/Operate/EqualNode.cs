using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Equal")]
    [NodePath("Base/Value/Operate/Equal")]
    [NodeView("VariablePropertyNodeView")]
    public partial class EqualNode : ValueNode
    {
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "Value1", "AcceptableTypes")]
        protected PropertyPort m_InputValue1 = new PropertyPort();
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "Value2", "AcceptableTypes")]
        protected PropertyPort m_InputValue2 = new PropertyPort();
        [SerializeField, PropertyPort(PortDirection.Output, "Result"), ReadOnly]
        protected BoolPropertyPort m_Result = new BoolPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            m_Result.Value = m_InputValue1.GetValue().Equals(m_InputValue2.GetValue());
        }
    }
}