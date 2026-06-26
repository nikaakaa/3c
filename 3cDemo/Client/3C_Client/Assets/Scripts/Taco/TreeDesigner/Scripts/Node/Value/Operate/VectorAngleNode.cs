using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("VectorAngle")]
    [NodePath("Base/Value/Operate/VectorAngle")]
    [NodeView("VariablePropertyNodeView")]
    public class VectorAngleNode : ValueNode
    {
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "VectorA", typeof(Vector2), typeof(Vector3))]
        PropertyPort m_VectorA = new PropertyPort();
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "VectorB", typeof(Vector2), typeof(Vector3))]
        PropertyPort m_VectorB = new PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Magnitude"), ReadOnly]
        FloatPropertyPort m_Magnitude = new FloatPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            switch (m_VectorA)
            {
                case Vector2PropertyPort vector2PropertyPort:
                    m_Magnitude.Value = Vector2.SignedAngle(vector2PropertyPort.Value, (m_VectorB as Vector2PropertyPort).Value);
                    break;
                case Vector3PropertyPort vector3PropertyPort:
                    m_Magnitude.Value = Vector3.SignedAngle(vector3PropertyPort.Value, (m_VectorB as Vector3PropertyPort).Value, Vector3.up);
                    break;
            }
        }

#if UNITY_EDITOR
        public override void OnInputPropertyLinked(PropertyEdge propertyEdge)
        {
            base.OnInputPropertyLinked(propertyEdge);
            switch (propertyEdge.EndPortName)
            {
                case "m_VectorA":
                    if (!IsConnected("m_VectorB"))
                        SetPropertyPort("m_VectorB", propertyEdge.EndPort.GetType(), PortDirection.Input);
                    break;
                case "m_VectorB":
                    if (!IsConnected("m_VectorA"))
                        SetPropertyPort("m_VectorA", propertyEdge.EndPort.GetType(), PortDirection.Input);
                    break;
            }
        }
        public override void OnInputPropertyUnLinked(PropertyEdge propertyEdge)
        {
            base.OnInputPropertyUnLinked(propertyEdge);
            switch (propertyEdge.EndPortName)
            {
                case "m_VectorA":
                    if (!IsConnected("m_VectorB"))
                    {
                        SetPropertyPort("m_VectorA", typeof(PropertyPort), PortDirection.Input);
                        SetPropertyPort("m_VectorB", typeof(PropertyPort), PortDirection.Input);
                    }
                    break;
                case "m_VectorB":
                    if (!IsConnected("m_VectorA"))
                    {
                        SetPropertyPort("m_VectorA", typeof(PropertyPort), PortDirection.Input);
                        SetPropertyPort("m_VectorB", typeof(PropertyPort), PortDirection.Input);
                    }
                    break;
            }
        }
#endif
    }
}