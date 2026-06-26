using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Reverse")]
    [NodePath("Base/Value/Operate/Reverse")]
    public class ReverseNode : ValueNode
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