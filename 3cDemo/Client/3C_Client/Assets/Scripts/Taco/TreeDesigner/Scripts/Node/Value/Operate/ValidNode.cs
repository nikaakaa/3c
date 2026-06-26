using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Valid")]
    [NodePath("Base/Value/Operate/Valid")]
    [NodeView("VariablePropertyNodeView")]
    public partial class ValidNode : ValueNode
    {
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "Value", "AcceptableTypes"), ReadOnly]
        protected PropertyPort m_InputValue = new PropertyPort();
        [SerializeField, PropertyPort(PortDirection.Output, "Result"), ReadOnly]
        protected BoolPropertyPort m_Result = new BoolPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            m_Result.Value = m_InputValue.GetValue() != null;
        }
    }
}