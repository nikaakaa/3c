using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("VectorProjectOnPlane")]
    [NodePath("Base/Value/Operate/VectorProjectOnPlane")]
    [NodeView("VariablePropertyNodeView")]
    public class VectorProjectOnPlaneNode : TwoVectorNode
    {
        protected override void OutputValue()
        {
            base.OutputValue();
            bool isV3 = m_InputValue1 is Vector3PropertyPort;
            if (isV3)
            {
                Vector3PropertyPort vector = m_InputValue1 as Vector3PropertyPort;
                Vector3PropertyPort planeNormal = m_InputValue2 as Vector3PropertyPort;
                (m_OutputValue as Vector3PropertyPort).Value = Vector3.ProjectOnPlane(vector.Value, planeNormal.Value);
            }
            else
            {
                Vector2PropertyPort vector = m_InputValue1 as Vector2PropertyPort;
                Vector2PropertyPort planeNormal = m_InputValue2 as Vector2PropertyPort;
                (m_OutputValue as Vector2PropertyPort).Value = Vector3.ProjectOnPlane(vector.Value, planeNormal.Value);
            }
        }
    }

    [Serializable]
    [NodeName("RandomVectorInCircle")]
    [NodePath("Base/Value/Operate/RandomVectorInCircle")]
    [NodeView("VariablePropertyNodeView")]
    public class RandomVectorInCircleNode : ValueNode
    {
        [SerializeReference, PropertyPort(PortDirection.Output, "OutputVector"), ReadOnly]
        protected Vector2PropertyPort m_OutputValue = new Vector2PropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            (m_OutputValue).Value = UnityEngine.Random.insideUnitCircle;
        }
    }

    [Serializable]
    [NodeName("RandomVectorInSphere")]
    [NodePath("Base/Value/Operate/RandomVectorInSphere")]
    [NodeView("VariablePropertyNodeView")]
    public class RandomVectorInSphereNode : ValueNode
    {
        [SerializeReference, PropertyPort(PortDirection.Output, "OutputVector"), ReadOnly]
        protected Vector3PropertyPort m_OutputValue = new Vector3PropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            (m_OutputValue).Value = UnityEngine.Random.insideUnitSphere;
        }
    }

    [Serializable]
    [NodeName("Vector3Multiply")]
    [NodePath("Base/Value/Operate/Vector3Multiply")]
    [NodeView("VariablePropertyNodeView")]
    public class Vector3MultiplyNode : ValueNode
    {
        [SerializeReference, PropertyPort(PortDirection.Input, "Vector1")]
        protected Vector3PropertyPort m_InputValue1 = new Vector3PropertyPort();
        [SerializeReference, PropertyPort(PortDirection.Input, "Float")]
        protected FloatPropertyPort m_InputValue2 = new FloatPropertyPort();
        [SerializeReference, PropertyPort(PortDirection.Output, "OutputVector"), ReadOnly]
        protected Vector3PropertyPort m_OutputValue = new Vector3PropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            (m_OutputValue).Value = m_InputValue1.Value * m_InputValue2.Value;
        }
    }


    public class TwoVectorNode : ValueNode
    {
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "Vector1", typeof(Vector2), typeof(Vector3))]
        protected PropertyPort m_InputValue1 = new PropertyPort();
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "Vector2", typeof(Vector2), typeof(Vector3))]
        protected PropertyPort m_InputValue2 = new PropertyPort();
        [SerializeReference, VariablePropertyPort(PortDirection.Output, "OutputVector", typeof(Vector2), typeof(Vector3)), ReadOnly]
        protected PropertyPort m_OutputValue = new PropertyPort();
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