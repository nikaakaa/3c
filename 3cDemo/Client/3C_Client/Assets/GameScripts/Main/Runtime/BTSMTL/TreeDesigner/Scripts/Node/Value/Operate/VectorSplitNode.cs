using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("VectorSplit")]
    [NodePath("Base/Value/Operate/VectorSplit")]
    [NodeView("VariablePropertyNodeView")]
    public partial class VectorSplitNode : ValueNode
    {
        public enum VectorType { Vector3, Vector2 }

        [SerializeField, EnumMenu("VectorType", "OnNodeChangedCallback")]
        VectorType m_VectorType;

        [SerializeField, PropertyPort(PortDirection.Input, "Vector3"), ShowIf("m_VectorType", VectorType.Vector3)]
        Vector3PropertyPort m_Vector3 = new Vector3PropertyPort();
        [SerializeField, PropertyPort(PortDirection.Input, "Vector2"), ShowIf("m_VectorType", VectorType.Vector2)]
        Vector2PropertyPort m_Vector2 = new Vector2PropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "X"), ReadOnly]
        FloatPropertyPort m_X = new FloatPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Output, "Y"), ReadOnly]
        FloatPropertyPort m_Y = new FloatPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Output, "Z"), ShowIf("m_VectorType", VectorType.Vector3), ReadOnly]
        FloatPropertyPort m_Z = new FloatPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            switch (m_VectorType)
            {
                case VectorType.Vector3:
                    m_X.Value = m_Vector3.Value.x;
                    m_Y.Value = m_Vector3.Value.y;
                    m_Z.Value = m_Vector3.Value.z;
                    break;
                case VectorType.Vector2:
                    m_X.Value = m_Vector2.Value.x;
                    m_Y.Value = m_Vector2.Value.y;
                    break;
                default:
                    break;
            }
        }
    }
}