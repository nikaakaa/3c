using System;

namespace TreeDesigner
{
    [NodeName("Selector")]
    [NodePath("Base/Composite/Selector")]
    [NodeAuthoringCapability(NodeAuthoringCapability.SharedFlow)]
    public class SelectorNode : CompositeNode
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
            m_CurrentIndex = -1;
            m_PendingStopSlot = null;
        }
        protected override State OnUpdate()
        {
            if (m_Parent.State != State.Running || ChildSlotCount == 0)
                return State.None;

            if (m_PendingStopSlot != null)
                return ContinuePendingStop();

            if (TryGetChildSlot(m_CurrentIndex, out ChildSlot currentSlot))
            {
                if (UsesSelfAbort(currentSlot.AbortPolicy) && !IsSlotConditionMet(currentSlot))
                {
                    return BeginPendingStop(
                        currentSlot,
                        CreateSlotStopContext(NodeStopOriginCause.SelfAbort, currentSlot, currentSlot));
                }
                else if (currentSlot.Child.State == State.Running)
                {
                    int interruptIndex = FindLowerPriorityAbortTarget(m_CurrentIndex);
                    if (interruptIndex >= 0)
                    {
                        TryGetChildSlot(interruptIndex, out ChildSlot replacementSlot);
                        return BeginPendingStop(
                            currentSlot,
                            CreateSlotStopContext(
                                NodeStopOriginCause.LowerPriorityAbort,
                                replacementSlot,
                                currentSlot,
                                replacementSlot));
                    }

                    State currentState = UpdateSlot(currentSlot);
                    return currentState == State.Failure
                        ? TickFrom(m_CurrentIndex + 1)
                        : ResolveChildState(currentState, m_CurrentIndex);
                }
            }

            return TickFrom(m_CurrentIndex < 0 ? 0 : m_CurrentIndex);
        }

        State BeginPendingStop(ChildSlot slot, NodeStopContext context)
        {
            m_PendingStopSlot = slot;
            m_PendingStopContext = context;
            return ContinuePendingStop();
        }

        State ContinuePendingStop()
        {
            NodeStopStatus status = StopSlot(m_PendingStopSlot, m_PendingStopContext);
            if (status == NodeStopStatus.Running)
                return State.Running;

            m_PendingStopSlot = null;
            m_CurrentIndex = -1;
            return status == NodeStopStatus.Failed ? State.Failure : TickFrom(0);
        }

        protected override void OnReset()
        {
            base.OnReset();
            m_CurrentIndex = -1;
            m_PendingStopSlot = null;
            m_PendingStopContext = default;
        }

        int FindLowerPriorityAbortTarget(int currentIndex)
        {
            for (int i = 0; i < currentIndex; i++)
            {
                if (!TryGetChildSlot(i, out ChildSlot slot))
                    continue;

                if (UsesLowerPriorityAbort(slot.AbortPolicy) && IsSlotConditionMet(slot))
                    return i;
            }

            return -1;
        }

        State TickFrom(int startIndex)
        {
            for (int i = startIndex; i < ChildSlotCount; i++)
            {
                if (!TryGetChildSlot(i, out ChildSlot slot) || !IsSlotConditionMet(slot))
                    continue;

                m_CurrentIndex = i;
                State state = ResolveChildState(UpdateSlot(slot), i);
                if (state != State.Failure)
                    return state;
            }

            m_CurrentIndex = -1;
            return State.Failure;
        }

        State ResolveChildState(State childState, int childIndex)
        {
            switch (childState)
            {
                case State.Running:
                    m_CurrentIndex = childIndex;
                    return State.Running;
                case State.Success:
                    return State.Success;
                case State.Failure:
                    return State.Failure;
                default:
                    return State.None;
            }
        }
    }
}
