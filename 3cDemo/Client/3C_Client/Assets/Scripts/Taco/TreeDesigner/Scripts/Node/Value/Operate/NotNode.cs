using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Not")]
    [NodePath("Base/Value/Operate/Not")]
    public sealed class NotNode : ValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Input")]
        BoolPropertyPort m_Input = new BoolPropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Output"), ReadOnly]
        BoolPropertyPort m_Output = new BoolPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            m_Output.Value = !m_Input.Value;
        }
    }
}
