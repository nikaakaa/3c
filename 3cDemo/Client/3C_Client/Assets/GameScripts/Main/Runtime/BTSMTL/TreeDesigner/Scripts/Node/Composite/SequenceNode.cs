using System;

namespace TreeDesigner 
{
    [NodeName("Sequence")]
    [NodePath("Base/Composite/Sequence")]
    [NodeAuthoringCapability(NodeAuthoringCapability.SharedFlow)]
    public class SequenceNode : CompositeNode
    {
        [NonSerialized]
        int m_CurrentIndex;

        [NonSerialized]
        ChildSlot m_PendingStopSlot;

        [NonSerialized]
        NodeStopContext m_PendingStopContext;

        protected override void OnStart()
        {
            base.OnStart();
            m_CurrentIndex = 0;
            m_PendingStopSlot = null;
        }
        protected override State OnUpdate()
        {
            if (m_Parent.State != State.Running || m_CurrentIndex >= ChildSlotCount)
                return State.None;

            if (m_PendingStopSlot != null)
                return ContinuePendingStop();

            while (m_CurrentIndex < ChildSlotCount)
            {
                if (!TryGetChildSlot(m_CurrentIndex, out ChildSlot slot))
                    return State.Failure;

                bool conditionMet = IsSlotConditionMet(slot);
                if (!conditionMet)
                {
                    if (slot.Child.State == State.Running && UsesSelfAbort(slot.AbortPolicy))
                    {
                        m_PendingStopSlot = slot;
                        m_PendingStopContext = CreateSlotStopContext(NodeStopOriginCause.SelfAbort, slot, slot);
                        return ContinuePendingStop();
                    }
                    return State.Failure;
                }

                State childState = UpdateSlot(slot);
                switch (childState)
                {
                    case State.Running:
                        return State.Running;
                    case State.Success:
                        m_CurrentIndex++;
                        break;
                    case State.Failure:
                        return State.Failure;
                    default:
                        return State.None;
                }
            }

            return State.Success;
        }

        State ContinuePendingStop()
        {
            NodeStopStatus status = StopSlot(m_PendingStopSlot, m_PendingStopContext);
            if (status == NodeStopStatus.Running)
                return State.Running;

            m_PendingStopSlot = null;
            return State.Failure;
        }

        protected override void OnReset()
        {
            base.OnReset();
            m_CurrentIndex = 0;
            m_PendingStopSlot = null;
            m_PendingStopContext = default;
        }
    }
}
