using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace TreeDesigner
{
    [Serializable]
    [NodeName("PingPong")]
    [NodePath("Base/Value/Math/PingPong")]
    public class PingPongNode : ValueNode
    {
        [SerializeReference, PropertyPort(PortDirection.Input, "Value1")]
        protected FloatPropertyPort m_InputValue1 = new FloatPropertyPort();
        [SerializeReference, PropertyPort(PortDirection.Input, "Value2")]
        protected FloatPropertyPort m_InputValue2 = new FloatPropertyPort();
        [SerializeReference, PropertyPort(PortDirection.Output, "Result"), ReadOnly]
        protected FloatPropertyPort m_OutputValue = new FloatPropertyPort();
        protected override void OutputValue()
        {
            base.OutputValue();

            m_OutputValue.Value = Mathf.PingPong(m_InputValue1.Value, m_InputValue2.Value);
        }
    }
}