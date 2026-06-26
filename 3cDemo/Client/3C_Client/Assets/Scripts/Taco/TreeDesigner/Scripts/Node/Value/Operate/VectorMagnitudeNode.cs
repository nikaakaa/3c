using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("VectorMagnitude")]
    [NodePath("Base/Value/Operate/VectorMagnitude")]
    [NodeView("VariablePropertyNodeView")]
    public class VectorMagnitudeNode : ValueNode
    {
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "Vector", typeof(Vector2), typeof(Vector3))]
        PropertyPort m_Vector = new PropertyPort();
        [SerializeField, PropertyPort(PortDirection.Output, "Magnitude"), ReadOnly]
        FloatPropertyPort m_Magnitude = new FloatPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            switch (m_Vector)
            {
                case Vector2PropertyPort vector2PropertyPort:
                    m_Magnitude.Value = vector2PropertyPort.Value.magnitude;
                    break;
                case Vector3PropertyPort vector3PropertyPort:
                    m_Magnitude.Value = vector3PropertyPort.Value.magnitude;
                    break;
            }
        }

#if UNITY_EDITOR
        public override void OnInputPropertyUnLinked(PropertyEdge propertyEdge)
        {
            base.OnInputPropertyUnLinked(propertyEdge);
            SetPropertyPort("m_Vector", typeof(PropertyPort), PortDirection.Input);
        }
#endif
    }
}