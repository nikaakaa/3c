using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("If")]
    [NodePath("Base/Decorator/If")]
    public class IfNode : DecoratorNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Condition")]
        BoolPropertyPort m_Condition = new BoolPropertyPort();

        protected override State OnUpdate()
        {
            if (m_Parent.State != State.Running)
                return State.None;

            if (m_Condition.Value && m_Child)
                return m_Child.UpdateNode();
            else
                return State.Failure;
        }
    }
}