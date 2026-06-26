using System;

namespace TreeDesigner 
{
    [NodeName("Sequence")]
    [NodePath("Base/Composite/Sequence")]
    public class SequenceNode : CompositeNode
    {
        [NonSerialized]
        int m_CurrentIndex;

        protected override void OnStart()
        {
            base.OnStart();
            m_CurrentIndex = 0;
        }
        protected override State OnUpdate()
        {
            if (m_Parent.State != State.Running || m_CurrentIndex >= m_Children.Count)
                return State.None;

            State childState = m_Children[m_CurrentIndex].UpdateNode();
            switch (childState)
            {
                case State.Running:
                    return State.Running;
                case State.Success:
                    m_CurrentIndex++;
                    if (m_CurrentIndex < m_Children.Count)
                        return OnUpdate();
                    else
                        return State.Success;
                case State.Failure:
                    return State.Failure;          
            }
            return State.None;
        }
    }
}