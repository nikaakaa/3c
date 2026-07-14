using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("VectorCombine")]
    [NodePath("Base/Value/Operate/VectorCombine")]
    [NodeView("VariablePropertyNodeView")]
    public partial class VectorCombineNode : ValueNode
    {
        public enum VectorType { Vector3, Vector2 }

        [SerializeField, EnumMenu("VectorType", "OnNodeChangedCallback")]
        VectorType m_VectorType;

        [SerializeField, PropertyPort(PortDirection.Input, "X")]
        FloatPropertyPort m_X = new FloatPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Input, "Y")]
        FloatPropertyPort m_Y = new FloatPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Input, "Z"), ShowIf("m_VectorType", VectorType.Vector3)]
        FloatPropertyPort m_Z = new FloatPropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Vector3"), ShowIf("m_VectorType", VectorType.Vector3), ReadOnly]
        Vector3PropertyPort m_Vector3 = new Vector3PropertyPort();
        [SerializeField, PropertyPort(PortDirection.Output, "Vector2"), ShowIf("m_VectorType", VectorType.Vector2), ReadOnly]
        Vector2PropertyPort m_Vector2 = new Vector2PropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            switch (m_VectorType)
            {
                case VectorType.Vector3:
                    m_Vector3.Value = new Vector3(m_X.Value, m_Y.Value, m_Z.Value);
                    break;
                case VectorType.Vector2:
                    m_Vector2.Value = new Vector2(m_X.Value, m_Y.Value);
                    break;
                default:
                    break;
            }
        }
    }
}