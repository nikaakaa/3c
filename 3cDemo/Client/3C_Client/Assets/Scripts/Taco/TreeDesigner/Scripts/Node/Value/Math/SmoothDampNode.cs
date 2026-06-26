using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("SmoothDamp")]
    [NodePath("Base/Value/SmoothDamp")]
    public class SmoothDampNode : ValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Current")]
        FloatPropertyPort m_Current = new FloatPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Input, "Target")]
        FloatPropertyPort m_Target = new FloatPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Input, "SmoothTime")]
        FloatPropertyPort m_SmoothTime = new FloatPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Output, "Result"), ReadOnly]
        FloatPropertyPort m_Result = new FloatPropertyPort();

        float m_CurrentVelocity;

        protected override void OutputValue()
        {
            base.OutputValue();
            m_Result.Value = Mathf.SmoothDamp(m_Current.Value, m_Target.Value, ref m_CurrentVelocity, m_SmoothTime.Value);
        }
    }
}