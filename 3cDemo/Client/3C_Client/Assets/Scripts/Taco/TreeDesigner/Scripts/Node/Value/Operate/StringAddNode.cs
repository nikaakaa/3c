using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("StringAdd")]
    [NodePath("Base/Value/Operate/StringAdd")]
    public class StringAddNode : ValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Input1")]
        StringPropertyPort m_Input1 = new StringPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Input, "Input2")]
        StringPropertyPort m_Input2 = new StringPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Output, "m_Output")]
        StringPropertyPort m_Output = new StringPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            m_Output.Value = m_Input1.Value + m_Input2.Value;
        }
    }
}