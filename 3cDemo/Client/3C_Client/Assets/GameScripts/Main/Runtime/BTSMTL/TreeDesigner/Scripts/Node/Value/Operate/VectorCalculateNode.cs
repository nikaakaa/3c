using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("VectorCalculate")]
    [NodePath("Base/Value/Operate/VectorCalculate")]
    [NodeView("VariablePropertyNodeView")]
    public class VectorCalculateNode : ValueNode
    {
        public enum CalculateType
        {
            Add,
            Sub,
        }

        [SerializeField, ShowInPanel("CalculateType")]
        CalculateType m_CalculateType;
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "Value1", typeof(Vector2), typeof(Vector3))]
        protected PropertyPort m_InputValue1 = new PropertyPort();
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "Value2", typeof(Vector2), typeof(Vector3))]
        protected PropertyPort m_InputValue2 = new PropertyPort();
        [SerializeReference, VariablePropertyPort(PortDirection.Output, "Result", typeof(Vector2), typeof(Vector3)), ReadOnly]
        protected PropertyPort m_OutputValue = new PropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            switch (m_CalculateType)
            {
                case CalculateType.Add:
                    switch (m_InputValue1)
                    {
                        case Vector2PropertyPort inputVector2:
                            (m_OutputValue as Vector2PropertyPort).Value = inputVector2.Value + (m_InputValue2 as Vector2PropertyPort).Value;
                            break;
                        case Vector3PropertyPort inputVector3:
                            (m_OutputValue as Vector3PropertyPort).Value = inputVector3.Value + (m_InputValue2 as Vector3PropertyPort).Value;
                            break;
                    }
                    break;
                case CalculateType.Sub:
                    switch (m_InputValue1)
                    {
                        case Vector2PropertyPort inputVector2:
                            (m_OutputValue as Vector2PropertyPort).Value = inputVector2.Value - (m_InputValue2 as Vector2PropertyPort).Value;
                            break;
                        case Vector3PropertyPort inputVector3:
                            (m_OutputValue as Vector3PropertyPort).Value = inputVector3.Value - (m_InputValue2 as Vector3PropertyPort).Value;
                            break;
                    }
                    break;
            }
        }

#if UNITY_EDITOR
        public override void OnInputPropertyLinked(PropertyEdge propertyEdge)
        {
            base.OnInputPropertyLinked(propertyEdge);
            switch (propertyEdge.EndPortName)
            {
                case "m_InputValue1":
                    if (!IsConnected("m_InputValue2"))
                        SetPropertyPort("m_InputValue2", propertyEdge.EndPort.GetType(), PortDirection.Input);
                    if (!IsConnected("m_OutputValue"))
                        SetPropertyPort("m_OutputValue", propertyEdge.EndPort.GetType(), PortDirection.Output);
                    break;
                case "m_InputValue2":
                    if (!IsConnected("m_InputValue1"))
                        SetPropertyPort("m_InputValue1", propertyEdge.EndPort.GetType(), PortDirection.Input);
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
                case "m_InputValue1":
                    if (!IsConnected("m_InputValue2") && !IsConnected("m_OutputValue"))
                    {
                        SetPropertyPort("m_InputValue1", typeof(PropertyPort), PortDirection.Input);
                        SetPropertyPort("m_InputValue2", typeof(PropertyPort), PortDirection.Input);
                        SetPropertyPort("m_OutputValue", typeof(PropertyPort), PortDirection.Output);
                    }
                    break;
                case "m_InputValue2":
                    if (!IsConnected("m_InputValue1") && !IsConnected("m_OutputValue"))
                    {
                        SetPropertyPort("m_InputValue1", typeof(PropertyPort), PortDirection.Input);
                        SetPropertyPort("m_InputValue2", typeof(PropertyPort), PortDirection.Input);
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
                if (!IsConnected("m_InputValue1"))
                    SetPropertyPort("m_InputValue1", propertyEdge.EndPort.GetType(), PortDirection.Input);
                if (!IsConnected("m_InputValue2"))
                    SetPropertyPort("m_InputValue2", propertyEdge.EndPort.GetType(), PortDirection.Input);
            }
        }
        public override void OnOutputPropertyUnLinked(PropertyEdge propertyEdge)
        {
            base.OnOutputPropertyUnLinked(propertyEdge);
            if (propertyEdge.StartPortName == "m_OutputValue")
            {
                if (!IsConnected("m_InputValue1") && !IsConnected("m_InputValue2") && !IsConnected("m_OutputValue"))
                {
                    SetPropertyPort("m_InputValue1", typeof(PropertyPort), PortDirection.Input);
                    SetPropertyPort("m_InputValue2", typeof(PropertyPort), PortDirection.Input);
                    SetPropertyPort("m_OutputValue", typeof(PropertyPort), PortDirection.Output);
                }
            }
        }
#endif
    }
}