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
        public ParallelType Mode => m_ParallelType;

        List<RunnableNode> m_CompletedChildren = new List<RunnableNode>();
        readonly List<ChildSlot> m_PendingStopSlots = new List<ChildSlot>();
        readonly Dictionary<ChildSlot, NodeStopContext> m_PendingStopContexts = new Dictionary<ChildSlot, NodeStopContext>();

        protected override void OnStart()
        {
            base.OnStart();
            m_CompletedChildren.Clear();
            m_PendingStopSlots.Clear();
            m_PendingStopContexts.Clear();
        }
        protected override State OnUpdate()
        {
            if (m_Parent.State != State.Running)
                return State.None;

            bool running = false;
            foreach (var slot in m_ChildSlots)
            {
                if (m_PendingStopContexts.TryGetValue(slot, out NodeStopContext pendingContext))
                {
                    NodeStopStatus pendingStatus = StopSlot(slot, pendingContext);
                    if (pendingStatus == NodeStopStatus.Failed)
                        return State.Failure;
                    if (pendingStatus == NodeStopStatus.Running)
                    {
                        running = true;
                        continue;
                    }

                    m_PendingStopSlots.Remove(slot);
                    m_PendingStopContexts.Remove(slot);
                    continue;
                }

                bool conditionMet = IsSlotConditionMet(slot);
                if (!conditionMet)
                {
                    m_CompletedChildren.Remove(slot.Child);
                    if (slot.Child.State == State.Running && UsesSelfAbort(slot.AbortPolicy))
                    {
                        NodeStopContext context = CreateSlotStopContext(NodeStopOriginCause.SelfAbort, slot, slot);
                        NodeStopStatus status = StopSlot(slot, context);
                        if (status == NodeStopStatus.Failed)
                            return State.Failure;
                        if (status == NodeStopStatus.Running)
                        {
                            m_PendingStopSlots.Add(slot);
                            m_PendingStopContexts.Add(slot, context);
                            running = true;
                        }
                    }
                    continue;
                }

                if (m_ParallelType == ParallelType.JumpComplete && m_CompletedChildren.Contains(slot.Child))
                    continue;

                State childState = UpdateSlot(slot);
                if ((childState == State.Success || childState == State.Failure) && 
                    m_ParallelType == ParallelType.JumpComplete)
                    m_CompletedChildren.Add(slot.Child);

                if (childState == State.Running)
                    running = true;
            }

            if (running)
                return State.Running;
            else
                return State.Success;
        }

        protected override void OnReset()
        {
            base.OnReset();
            m_CompletedChildren.Clear();
            m_PendingStopSlots.Clear();
            m_PendingStopContexts.Clear();
        }
    }
}
