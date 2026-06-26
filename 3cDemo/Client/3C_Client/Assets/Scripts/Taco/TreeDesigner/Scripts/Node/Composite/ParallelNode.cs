using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [NodeName("Parallel")]
    [NodePath("Base/Composite/Parallel")]
    public class ParallelNode : CompositeNode
    {
        public enum ParallelType { JumpComplete, UpdateAll }

        [SerializeField, ShowInPanel("ParallelType")]
        ParallelType m_ParallelType;

        List<BaseNode> m_CompletedChildren = new List<BaseNode>();

        protected override void OnStart()
        {
            base.OnStart();
            m_CompletedChildren.Clear();
        }
        protected override State OnUpdate()
        {
            if (m_Parent.State != State.Running)
                return State.None;

            bool running = false;
            foreach (var child in m_Children)
            {
                if (m_ParallelType == ParallelType.JumpComplete && m_CompletedChildren.Contains(child))
                    continue;

                State childState = child.UpdateNode();
                if ((childState == State.Success || childState == State.Failure) && 
                    m_ParallelType == ParallelType.JumpComplete)
                    m_CompletedChildren.Add(child);

                if (childState == State.Running)
                    running = true;
            }

            if (running)
                return State.Running;
            else
                return State.Success;
        }
    }
}