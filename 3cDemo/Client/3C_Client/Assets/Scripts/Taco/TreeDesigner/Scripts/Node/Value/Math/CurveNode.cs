using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Curve")]
    [NodePath("Base/Value/Curve")]
    public class CurveNode : ValueNode
    {
        [SerializeField, ShowInPanel(), OnValueChanged("OnNodeChangedCallback")]
        bool m_OutSide;
        [SerializeField, ShowInPanel("Curve"), ShowIf("m_OutSide", false)]
        AnimationCurve m_Curve;
        [SerializeField, PropertyPort(PortDirection.Input, "Curve"), ShowIf("m_OutSide", true)]
        AnimationCurvePropertyPort m_OutSideCurve = new AnimationCurvePropertyPort();
        [SerializeField, PropertyPort(PortDirection.Input, "Input")]
        FloatPropertyPort m_Input = new FloatPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Output, "Output"), ReadOnly]
        FloatPropertyPort m_Output = new FloatPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            if(m_OutSide)
                m_Output.Value = m_OutSideCurve.Value.Evaluate(m_Input.Value);
            else
                m_Output.Value = m_Curve.Evaluate(m_Input.Value);
        }
    }
}