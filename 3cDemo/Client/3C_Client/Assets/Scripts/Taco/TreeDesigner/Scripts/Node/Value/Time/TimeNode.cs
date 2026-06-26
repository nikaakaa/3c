using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Time")]
    [NodePath("Base/Value/Time")]
    public class TimeNode : ValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Time"), ReadOnly]
        FloatPropertyPort m_Time = new FloatPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Output, "DeltaTime"), ReadOnly]
        FloatPropertyPort m_DeltaTime = new FloatPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            m_Time.Value = Time.time;
            m_DeltaTime.Value = Time.deltaTime;
        }
    }
}