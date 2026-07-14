using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("VectorNormalize")]
    [NodePath("Base/Value/Operate/VectorNormalize")]
    [NodeView("VariablePropertyNodeView")]
    public class VectorNormalizeNode : ValueNode
    {
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "Vector", typeof(Vector2), typeof(Vector3))]
        PropertyPort m_Vector = new PropertyPort();
        [SerializeReference, VariablePropertyPort(PortDirection.Output, "NormalizedVector", typeof(Vector2), typeof(Vector3)), ReadOnly]
        PropertyPort m_NormalizedVector = new PropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            switch (m_Vector)
            {
                case Vector2PropertyPort vector2PropertyPort:
                    (m_NormalizedVector as Vector2PropertyPort).Value = vector2PropertyPort.Value.normalized;
                    break;
                case Vector3PropertyPort vector3PropertyPort:
                    (m_NormalizedVector as Vector3PropertyPort).Value = vector3PropertyPort.Value.normalized;
                    break;
            }
        }

#if UNITY_EDITOR
        public override void OnInputPropertyLinked(PropertyEdge propertyEdge)
        {
            base.OnInputPropertyLinked(propertyEdge);
            if (!IsConnected("m_NormalizedVector"))
                SetPropertyPort("m_NormalizedVector", propertyEdge.EndPort.GetType(), PortDirection.Output);
        }
        public override void OnInputPropertyUnLinked(PropertyEdge propertyEdge)
        {
            base.OnInputPropertyUnLinked(propertyEdge);
            if (!IsConnected("m_NormalizedVector"))
            {
                SetPropertyPort("m_Vector", typeof(PropertyPort), PortDirection.Input);
                SetPropertyPort("m_NormalizedVector", typeof(PropertyPort), PortDirection.Output);
            }
        }
        public override void OnOutputPropertyLinked(PropertyEdge propertyEdge)
        {
            base.OnOutputPropertyLinked(propertyEdge);
            if (!IsConnected("m_Vector"))
                SetPropertyPort("m_Vector", propertyEdge.EndPort.GetType(), PortDirection.Input);
        }
        public override void OnOutputPropertyUnLinked(PropertyEdge propertyEdge)
        {
            base.OnOutputPropertyUnLinked(propertyEdge);
            if (!IsConnected("m_Vector") && !IsConnected("m_NormalizedVector"))
            {
                SetPropertyPort("m_Vector", typeof(PropertyPort), PortDirection.Input);
                SetPropertyPort("m_NormalizedVector", typeof(PropertyPort), PortDirection.Output);
            }
        }
#endif
    }
}