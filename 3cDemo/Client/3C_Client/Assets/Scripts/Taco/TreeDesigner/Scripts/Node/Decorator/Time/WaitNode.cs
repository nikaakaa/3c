using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Wait")]
    [NodePath("Base/Decorator/Time/Wait")]
    public class WaitNode : DecoratorNode
    {
        public enum WaitType { Time, Frame }

        [SerializeField, EnumMenu("WaitType", "OnNodeChangedCallback")]
        WaitType m_WaitType;
        [SerializeField, PropertyPort(PortDirection.Input, "Time"), ShowIf("m_WaitType", WaitType.Time)]
        FloatPropertyPort m_Time = new FloatPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Input, "Frame"), ShowIf("m_WaitType", WaitType.Frame)]
        IntPropertyPort m_Frame = new IntPropertyPort();

        float m_CurrentTime;
        int m_CurrentFrame;

        protected override void OnStart()
        {
            base.OnStart();
            m_CurrentTime = 0;
            m_CurrentFrame = 0;
        }
        protected override State OnUpdate()
        {
            if (m_Parent.State != State.Running)
                return State.None;

            if (m_WaitType == WaitType.Time && m_CurrentTime < m_Time.Value)
            {
                m_CurrentTime += Time.deltaTime;
                return State.Running;
            }
            else if (m_WaitType == WaitType.Frame && m_CurrentFrame < m_Frame.Value)
            {
                m_CurrentFrame++;
                return State.Running;
            }
            return m_Child?.UpdateNode() ?? State.Success;
        }
    }
}