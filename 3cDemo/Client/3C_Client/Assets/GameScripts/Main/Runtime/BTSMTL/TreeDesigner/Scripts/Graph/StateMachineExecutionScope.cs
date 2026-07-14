using System;
using System.Collections.Generic;

namespace TreeDesigner
{
    public enum StateExitCause
    {
        StateTransition,
        TreeSelfAbort,
        TreeLowerPriorityAbort,
        TreeParentStop
    }

    public readonly struct StateExitContext
    {
        public StateExitContext(
            StateExitCause cause,
            string sourceStateGuid,
            string targetStateGuid,
            string transitionEdgeGuid,
            string parentSourceEdgeGuid,
            string parentSourceNodeGuid,
            string replacementEdgeGuid,
            string replacementNodeGuid,
            ulong localLogicTick)
        {
            Cause = cause;
            SourceStateGuid = sourceStateGuid ?? string.Empty;
            TargetStateGuid = targetStateGuid ?? string.Empty;
            TransitionEdgeGuid = transitionEdgeGuid ?? string.Empty;
            ParentSourceEdgeGuid = parentSourceEdgeGuid ?? string.Empty;
            ParentSourceNodeGuid = parentSourceNodeGuid ?? string.Empty;
            ReplacementEdgeGuid = replacementEdgeGuid ?? string.Empty;
            ReplacementNodeGuid = replacementNodeGuid ?? string.Empty;
            LocalLogicTick = localLogicTick;
        }

        public StateExitCause Cause { get; }
        public string SourceStateGuid { get; }
        public string TargetStateGuid { get; }
        public string TransitionEdgeGuid { get; }
        public string ParentSourceEdgeGuid { get; }
        public string ParentSourceNodeGuid { get; }
        public string ReplacementEdgeGuid { get; }
        public string ReplacementNodeGuid { get; }
        public ulong LocalLogicTick { get; }
        public bool IsValid => !string.IsNullOrEmpty(SourceStateGuid);
    }

    public readonly struct StateMachineExecutionScope : IEquatable<StateMachineExecutionScope>
    {
        public StateMachineExecutionScope(
            Guid runtimeId,
            string stateId,
            ulong activationGeneration,
            string stateMachineGraphOwnerId,
            Guid stateMachineGraphRuntimeId,
            string stateBodyGraphOwnerId,
            Guid stateBodyGraphRuntimeId)
        {
            RuntimeId = runtimeId;
            StateId = stateId ?? string.Empty;
            ActivationGeneration = activationGeneration;
            StateMachineGraphOwnerId = stateMachineGraphOwnerId ?? string.Empty;
            StateMachineGraphRuntimeId = stateMachineGraphRuntimeId;
            StateBodyGraphOwnerId = stateBodyGraphOwnerId ?? string.Empty;
            StateBodyGraphRuntimeId = stateBodyGraphRuntimeId;
        }

        public Guid RuntimeId { get; }
        public string StateId { get; }
        public ulong ActivationGeneration { get; }
        public string StateMachineGraphOwnerId { get; }
        public Guid StateMachineGraphRuntimeId { get; }
        public string StateBodyGraphOwnerId { get; }
        public Guid StateBodyGraphRuntimeId { get; }
        public bool IsValid => RuntimeId != Guid.Empty &&
                               !string.IsNullOrEmpty(StateId) &&
                               ActivationGeneration != 0 &&
                               !string.IsNullOrEmpty(StateMachineGraphOwnerId) &&
                               StateMachineGraphRuntimeId != Guid.Empty &&
                               !string.IsNullOrEmpty(StateBodyGraphOwnerId) &&
                               StateBodyGraphRuntimeId != Guid.Empty;

        public bool Equals(StateMachineExecutionScope other)
        {
            return RuntimeId.Equals(other.RuntimeId) &&
                   ActivationGeneration == other.ActivationGeneration &&
                   StateMachineGraphRuntimeId.Equals(other.StateMachineGraphRuntimeId) &&
                   StateBodyGraphRuntimeId.Equals(other.StateBodyGraphRuntimeId) &&
                   string.Equals(StateId, other.StateId, StringComparison.Ordinal) &&
                   string.Equals(StateMachineGraphOwnerId, other.StateMachineGraphOwnerId, StringComparison.Ordinal) &&
                   string.Equals(StateBodyGraphOwnerId, other.StateBodyGraphOwnerId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is StateMachineExecutionScope other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = RuntimeId.GetHashCode();
                hash = hash * 31 + ActivationGeneration.GetHashCode();
                hash = hash * 31 + (StateId ?? string.Empty).GetHashCode();
                hash = hash * 31 + (StateMachineGraphOwnerId ?? string.Empty).GetHashCode();
                hash = hash * 31 + StateMachineGraphRuntimeId.GetHashCode();
                hash = hash * 31 + (StateBodyGraphOwnerId ?? string.Empty).GetHashCode();
                hash = hash * 31 + StateBodyGraphRuntimeId.GetHashCode();
                return hash;
            }
        }
    }

    public readonly struct StateMachineExecutionPath
    {
        readonly StateMachineExecutionScope[] m_Frames;

        public StateMachineExecutionPath(IReadOnlyList<StateMachineExecutionScope> frames)
        {
            if (frames == null || frames.Count == 0)
            {
                m_Frames = Array.Empty<StateMachineExecutionScope>();
                return;
            }

            m_Frames = new StateMachineExecutionScope[frames.Count];
            for (int i = 0; i < frames.Count; i++)
                m_Frames[i] = frames[i];
        }

        public int Count => m_Frames?.Length ?? 0;
        public StateMachineExecutionScope this[int index] => m_Frames[index];
        public StateMachineExecutionScope Leaf => Count > 0 ? m_Frames[Count - 1] : default;

        public bool Contains(StateMachineExecutionScope scope)
        {
            for (int i = 0; i < Count; i++)
            {
                if (m_Frames[i].Equals(scope))
                    return true;
            }
            return false;
        }
    }

    public interface IStateMachineExecutionScopeSink
    {
        void PushStateMachineExecutionScope(StateMachineExecutionScope scope);
        void PopStateMachineExecutionScope(StateMachineExecutionScope scope);
    }

    public interface IStateExitContextRuntimeAccess
    {
        void PushStateExitContext(StateExitContext context);
        void PopStateExitContext(StateExitContext context);
        bool TryGetStateExitContext(out StateExitContext context);
    }
}
