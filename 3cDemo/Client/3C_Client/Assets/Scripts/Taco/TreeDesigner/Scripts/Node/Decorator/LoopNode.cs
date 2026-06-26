using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Loop")]
    [NodePath("Base/Decorator/Loop")]
    public class LoopNode : DecoratorNode
    {
        public enum StopType { None, Success, Failure }

        [SerializeField, ShowInPanel("StopType")]
        StopType m_StopType;

        protected override State OnUpdate()
        {
            if (m_Parent.State != State.Running || !m_Child)
                return State.None;

            State childState = m_Child.UpdateNode();
            switch (m_StopType)
            {
                case StopType.Success:
                    if(childState == State.Success)
                        return State.Success;
                    break;
                case StopType.Failure:
                    if (childState == State.Failure)
                        return State.Failure;
                    break;
            }
            return State.Running;
        }
    }
}