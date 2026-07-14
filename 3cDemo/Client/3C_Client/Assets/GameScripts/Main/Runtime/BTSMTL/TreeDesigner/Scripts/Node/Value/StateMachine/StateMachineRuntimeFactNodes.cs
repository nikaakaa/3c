using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    public abstract class StateMachineRuntimeFactNode : ValueNode
    {
        [NonSerialized]
        bool m_ReportedMissingContext;

        protected bool TryGetFacts(out IStateMachineRuntimeFacts facts)
        {
            facts = null;
            if (Owner != null &&
                Owner.TryGetEvaluationContext(out ConditionRuleEvaluationContext context) &&
                context?.StateMachineFacts != null)
            {
                facts = context.StateMachineFacts;
                return true;
            }

            if (!m_ReportedMissingContext)
            {
                m_ReportedMissingContext = true;
                Debug.LogError($"{GetType().Name}: State machine runtime facts are missing from condition rule context.", Owner?.SerializedOwner);
            }
            return false;
        }
    }

    [Serializable]
    [NodeName("State Elapsed Seconds")]
    [NodePath("Base/Value/StateMachine/ElapsedSeconds")]
    public sealed class StateElapsedSecondsNode : StateMachineRuntimeFactNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Seconds"), ReadOnly]
        FloatPropertyPort m_Output = new FloatPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            m_Output.Value = TryGetFacts(out IStateMachineRuntimeFacts facts) ? facts.StateElapsedSeconds : 0f;
        }
    }

    [Serializable]
    [NodeName("State Elapsed Ticks")]
    [NodePath("Base/Value/StateMachine/ElapsedTicks")]
    public sealed class StateElapsedTicksNode : StateMachineRuntimeFactNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Ticks"), ReadOnly]
        IntPropertyPort m_Output = new IntPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            m_Output.Value = TryGetFacts(out IStateMachineRuntimeFacts facts) ? facts.StateElapsedTicks : 0;
        }
    }

    [Serializable]
    [NodeName("State Root Completed")]
    [NodePath("Base/Value/StateMachine/RootCompleted")]
    public sealed class StateRootCompletedNode : StateMachineRuntimeFactNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Completed"), ReadOnly]
        BoolPropertyPort m_Output = new BoolPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            m_Output.Value = TryGetFacts(out IStateMachineRuntimeFacts facts) && facts.StateRootCompleted;
        }
    }
}
