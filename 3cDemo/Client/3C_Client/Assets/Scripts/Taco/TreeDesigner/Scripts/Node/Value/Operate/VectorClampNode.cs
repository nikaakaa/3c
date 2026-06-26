using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("VectorClamp")]
    [NodePath("Base/Value/Operate/VectorClamp")]
    [NodeView("VariablePropertyNodeView")]
    public class VectorClampNode : ValueNode
    {
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "Value", typeof(Vector2), typeof(Vector3))]
        protected PropertyPort m_InputValue = new PropertyPort();
        [SerializeField, PropertyPort(PortDirection.Input, "ClampMagnitude")]
        protected FloatPropertyPort m_ClampMagnitude = new FloatPropertyPort();
        [SerializeReference, VariablePropertyPort(PortDirection.Output, "Result", typeof(Vector2), typeof(Vector3)), ReadOnly]
        protected PropertyPort m_OutputValue = new PropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            switch (m_InputValue)
            {
                case Vector2PropertyPort inputVector2:
                    (m_OutputValue as Vector2PropertyPort).Value = Vector2.ClampMagnitude(inputVector2.Value, m_ClampMagnitude.Value);
                    break;
                case Vector3PropertyPort inputVector3:
                    (m_OutputValue as Vector3PropertyPort).Value = Vector3.ClampMagnitude(inputVector3.Value, m_ClampMagnitude.Value);
                    break;
            }
        }

#if UNITY_EDITOR
        public override void OnInputPropertyLinked(PropertyEdge propertyEdge)
        {
            base.OnInputPropertyLinked(propertyEdge);
            switch (propertyEdge.EndPortName)
            {
                case "m_InputValue":
                    if (!IsConnected("m_OutputValue"))
                        SetPropertyPort("m_OutputValue", propertyEdge.EndPort.GetType(), PortDirection.Output);
                    break;
            }
        }
        public override void OnInputPropertyUnLinked(PropertyEdge propertyEdge)
        {
            base.OnInputPropertyUnLinked(propertyEdge);
            switch (propertyEdge.EndPortName)
            {
                case "m_InputValue":
                    if (!IsConnected("m_OutputValue"))
                    {
                        SetPropertyPort("m_InputValue", typeof(PropertyPort), PortDirection.Input);
                        SetPropertyPort("m_OutputValue", typeof(PropertyPort), PortDirection.Output);
                    }
                    break;
            }
        }
        public override void OnOutputPropertyLinked(PropertyEdge propertyEdge)
        {
            base.OnOutputPropertyLinked(propertyEdge);
            if (propertyEdge.StartPortName == "m_OutputValue")
            {
                if (!IsConnected("m_InputValue"))
                    SetPropertyPort("m_InputValue", propertyEdge.EndPort.GetType(), PortDirection.Input);
            }
        }
        public override void OnOutputPropertyUnLinked(PropertyEdge propertyEdge)
        {
            base.OnOutputPropertyUnLinked(propertyEdge);
            if (propertyEdge.StartPortName == "m_OutputValue")
            {
                if (!IsConnected("m_InputValue") && !IsConnected("m_OutputValue"))
                {
                    SetPropertyPort("m_InputValue", typeof(PropertyPort), PortDirection.Input);
                    SetPropertyPort("m_OutputValue", typeof(PropertyPort), PortDirection.Output);
                }
            }
        }
#endif
    }
}