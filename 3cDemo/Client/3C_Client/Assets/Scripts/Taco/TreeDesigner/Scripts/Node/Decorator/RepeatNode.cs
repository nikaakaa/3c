using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Repeat")]
    [NodePath("Base/Decorator/Repeat")]
    public class RepeatNode : DecoratorNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Int")]
        IntPropertyPort m_Count = new IntPropertyPort();

        int m_CurrentIndex;

        protected override void OnStart()
        {
            base.OnStart();
            m_CurrentIndex = 0;
        }
        protected override State OnUpdate()
        {
            if (m_Parent.State != State.Running)
                return State.None;

            State childState = m_Child.UpdateNode();
            if(childState == State.Running)
                return State.Running;
            else
            {
                m_CurrentIndex++;
                if (m_CurrentIndex < m_Count.Value)
                    return OnUpdate();
                else
                    return State.Success;
            }
        }
    }
}