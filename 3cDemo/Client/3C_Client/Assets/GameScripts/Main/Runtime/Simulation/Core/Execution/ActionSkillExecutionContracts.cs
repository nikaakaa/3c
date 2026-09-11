using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public enum SimulationActionResultKind : byte
    {
        None = 0,
        Accepted = 1,
        Rejected = 2,
        Completed = 3,
        Cancelled = 4,
        Interrupted = 5,
        Aborted = 6,
        Corrected = 7
    }

    internal enum ActionSkillTraceSeverity : byte
    {
        Detail = 1,
        Information = 2
    }

    internal readonly struct ActionSkillExecutionIdentity
    {
        public ActionSkillExecutionIdentity(
            CharacterSkillId skillId,
            OperationHandle entryOperation,
            ulong actionInstanceId,
            ulong predictionKey,
            ulong generation)
        {
            SkillId = skillId;
            EntryOperation = entryOperation;
            ActionInstanceId = actionInstanceId;
            PredictionKey = predictionKey;
            Generation = generation;
        }

        public CharacterSkillId SkillId { get; }
        public OperationHandle EntryOperation { get; }
        public ulong ActionInstanceId { get; }
        public ulong PredictionKey { get; }
        public ulong Generation { get; }
        public bool IsValid => SkillId.IsValid && EntryOperation.IsValid && ActionInstanceId != 0 && PredictionKey != 0;
    }

    internal interface IActionSkillExecutionStorage<TValue>
        where TValue : struct
    {
        bool IsSkillStateSlot(int slotIndex);
        bool IsValueValid(int slotIndex, TValue value);
        TValue DefaultValue(int slotIndex);
        ActionSkillExecutionAggregate<TValue> ReadAggregate();
        void WriteAggregate(ActionSkillExecutionAggregate<TValue> aggregate);
    }

    internal readonly struct ActionSkillActivationCandidate<TTargetSnapshot, TOperation>
        where TTargetSnapshot : struct
        where TOperation : class
    {
        public ActionSkillActivationCandidate(
            CharacterSkillId skillId,
            OperationHandle skillEntryOperation,
            string contextId,
            string sourceInputRequestId,
            bool consumeSourceInputRequest,
            string targetKey,
            TTargetSnapshot targetSnapshot,
            SimulationExecutionSource source,
            EquipmentActionContext equipmentContext,
            TOperation operation = null,
            ulong replacementActionInstanceId = 0)
        {
            if (!source.IsValid)
                throw new ArgumentException("Action activation source is invalid.", nameof(source));
            SkillId = skillId;
            SkillEntryOperation = skillEntryOperation;
            ContextId = contextId ?? string.Empty;
            SourceInputRequestId = sourceInputRequestId ?? string.Empty;
            ConsumeSourceInputRequest = consumeSourceInputRequest;
            TargetKey = targetKey ?? string.Empty;
            TargetSnapshot = targetSnapshot;
            Source = source;
            EquipmentContext = equipmentContext;
            Operation = operation;
            ReplacementActionInstanceId = replacementActionInstanceId;
        }

        public CharacterSkillId SkillId { get; }
        public OperationHandle SkillEntryOperation { get; }
        public string ContextId { get; }
        public string SourceInputRequestId { get; }
        public bool ConsumeSourceInputRequest { get; }
        public string TargetKey { get; }
        public TTargetSnapshot TargetSnapshot { get; }
        public SimulationExecutionSource Source { get; }
        public EquipmentActionContext EquipmentContext { get; }
        public TOperation Operation { get; }
        public ulong ReplacementActionInstanceId { get; }
    }

    internal readonly struct ActionSkillActivationRequest<TTargetSnapshot>
        where TTargetSnapshot : struct
    {
        public ActionSkillActivationRequest(
            string actionId,
            CharacterSkillId skillId,
            OperationHandle skillEntryOperation,
            string contextId,
            string sourceInputRequestId,
            ulong inputSequence,
            ulong startTick,
            string targetKey,
            TTargetSnapshot targetSnapshot,
            SimulationExecutionSource source,
            EquipmentActionContext equipmentContext,
            ulong replacementActionInstanceId = 0)
        {
            ActionId = SimulationIdentity.Require(actionId, nameof(actionId));
            ContextId = SimulationIdentity.Require(contextId, nameof(contextId));
            if (inputSequence == 0 || startTick == 0 || !source.IsValid ||
                source.IsCharacterControl && (!skillId.IsValid || !skillEntryOperation.IsValid))
            {
                throw new ArgumentException("Action activation request identity is incomplete.");
            }
            SkillId = skillId;
            SkillEntryOperation = skillEntryOperation;
            SourceInputRequestId = sourceInputRequestId ?? string.Empty;
            InputSequence = inputSequence;
            StartTick = startTick;
            TargetKey = targetKey ?? string.Empty;
            TargetSnapshot = targetSnapshot;
            Source = source;
            EquipmentContext = equipmentContext;
            ReplacementActionInstanceId = replacementActionInstanceId;
        }

        public string ActionId { get; }
        public CharacterSkillId SkillId { get; }
        public OperationHandle SkillEntryOperation { get; }
        public string ContextId { get; }
        public string SourceInputRequestId { get; }
        public ulong InputSequence { get; }
        public ulong StartTick { get; }
        public string TargetKey { get; }
        public TTargetSnapshot TargetSnapshot { get; }
        public SimulationExecutionSource Source { get; }
        public EquipmentActionContext EquipmentContext { get; }
        public ulong ReplacementActionInstanceId { get; }
    }

    internal interface IActionSkillCommitPort<TTargetSnapshot, TActionState>
        where TTargetSnapshot : struct
        where TActionState : struct
    {
        ulong NextActionInstanceId();
        ulong NextPredictionKey();
        TActionState CreatePredictedAction(
            ActionSkillActivationRequest<TTargetSnapshot> request,
            ulong instanceId,
            ulong predictionKey);
        void WriteAction(TActionState action);
        void SetActionTags(ulong actionInstanceId, IEnumerable<string> tags);
        void ClearRequest(ActionSkillActivationRequest<TTargetSnapshot> request);
        void EmitActionFact(SimulationExecutionSource source, TActionState action);
    }

    internal interface IActionSkillActivationPort<TTargetSnapshot, TOperation>
        where TTargetSnapshot : struct
        where TOperation : class
    {
        ulong InputSequence { get; }
        ulong Tick { get; }
        bool TraceEnabled { get; }
        bool TryReadInputSequence(string requestId, out ulong sequence);
        void ClearInputRequest(string requestId);
        TTargetSnapshot NoneTarget { get; }
        TTargetSnapshot ReadTargetSnapshot(string inputValueId);
        string TargetId(TTargetSnapshot targetSnapshot);
        string FormatTarget(TTargetSnapshot targetSnapshot);
        bool HasPendingRequest(string actionId);
        void StageRequest(ActionSkillActivationRequest<TTargetSnapshot> request);
        bool TryReadPendingRequest(CharacterSkillId skillId, out ActionSkillActivationRequest<TTargetSnapshot> request);
        void ClearPendingRequest(ActionSkillActivationRequest<TTargetSnapshot> request);
        void InterruptActive(ulong actionInstanceId, SimulationExecutionSource source, string reason);
        bool IsActionInstanceStopComplete(ulong actionInstanceId);
        void Trace(TOperation operation, string code, ActionSkillTraceSeverity severity, string detail);
        void Trace(SimulationExecutionSource source, string code, ActionSkillTraceSeverity severity, string detail);
        void TraceActionResult(
            TOperation operation,
            string actionId,
            CharacterSkillId skillId,
            ulong actionInstanceId,
            ulong inputSequence,
            SimulationActionResultKind result,
            string reason);
        void TraceActionResult(
            SimulationExecutionSource source,
            string actionId,
            CharacterSkillId skillId,
            ulong actionInstanceId,
            ulong inputSequence,
            SimulationActionResultKind result,
            string reason);
    }

    internal enum ActionSkillLifecyclePhase : byte
    {
        Startup = 0,
        Active = 1,
        Recovery = 2,
        Cancel = 3,
        Ended = 4
    }

    internal enum ActionSkillLifecycleState : byte
    {
        Requested = 0,
        Predicted = 1,
        Confirmed = 2,
        Rejected = 3,
        Cancelled = 4,
        Interrupted = 5,
        Aborted = 6,
        Ended = 7,
        Corrected = 8
    }

    internal enum ActionSkillLifecycleTransition : byte
    {
        None = 0,
        Confirm = 1,
        Complete = 2,
        Cancel = 3,
        Interrupt = 4,
        Reject = 5,
        Correct = 6,
        Abort = 7
    }

    internal readonly struct ActionSkillLifecycleIngress
    {
        public ActionSkillLifecycleIngress(
            string identity,
            ulong actionInstanceId,
            ulong predictionKey,
            ulong inputSequence,
            int transitionValue,
            ulong sourceTick,
            string reason)
        {
            if (actionInstanceId == 0 && predictionKey == 0 && inputSequence == 0)
                throw new ArgumentException("Action lifecycle ingress requires an instance, prediction, or input identity.");
            if (transitionValue == 0)
                throw new ArgumentOutOfRangeException(nameof(transitionValue));
            Identity = identity ?? string.Empty;
            ActionInstanceId = actionInstanceId;
            PredictionKey = predictionKey;
            InputSequence = inputSequence;
            TransitionValue = transitionValue;
            SourceTick = sourceTick;
            Reason = reason ?? string.Empty;
        }

        public string Identity { get; }
        public ulong ActionInstanceId { get; }
        public ulong PredictionKey { get; }
        public ulong InputSequence { get; }
        public int TransitionValue { get; }
        public ulong SourceTick { get; }
        public string Reason { get; }
    }

    internal readonly struct ActionSkillLifecycleUpdate
    {
        public ActionSkillLifecycleUpdate(
            ActionSkillLifecyclePhase phase,
            ActionSkillLifecycleState state,
            ActionSkillLifecycleTransition transition,
            ulong transitionTick,
            ulong sourceTick,
            string reason)
        {
            Phase = phase;
            State = state;
            Transition = transition;
            TransitionTick = transitionTick;
            SourceTick = sourceTick;
            Reason = reason ?? string.Empty;
        }

        public ActionSkillLifecyclePhase Phase { get; }
        public ActionSkillLifecycleState State { get; }
        public ActionSkillLifecycleTransition Transition { get; }
        public ulong TransitionTick { get; }
        public ulong SourceTick { get; }
        public string Reason { get; }
    }

    internal interface IActionSkillLifecyclePort<TActionState>
        where TActionState : struct
    {
        ulong Tick { get; }
        IEnumerable<TActionState> ActionStates { get; }
        bool TryFindActive(string contextId, out TActionState action);
        bool TryFindActive(CharacterSkillId skillId, out TActionState action);
        bool IsActive(TActionState action);
        string ActionId(TActionState action);
        CharacterSkillId SkillId(TActionState action);
        ulong InstanceId(TActionState action);
        ulong PredictionKey(TActionState action);
        ulong InputSequence(TActionState action);
        string Reason(TActionState action);
        SimulationExecutionSource Source(TActionState action);
        EquipmentActionContext EquipmentContext(TActionState action);
        ActionSkillLifecyclePhase Phase(TActionState action);
        ActionSkillLifecycleState State(TActionState action);
        IDisposable EnterExecution(TActionState action);
        TActionState WithLifecycle(TActionState action, ActionSkillLifecycleUpdate update);
        void WriteState(TActionState action);
        void EmitActionFact(SimulationExecutionSource source, TActionState action);
        void ClearTerminalResources(ulong actionInstanceId);
        ulong SourceGeneration(SimulationExecutionSource source);
        bool TraceEnabled { get; }
        void Trace(SimulationExecutionSource source, string code, ActionSkillTraceSeverity severity, string detail, ulong generation);
        void TraceActionResult(
            SimulationExecutionSource source,
            string actionId,
            CharacterSkillId skillId,
            ulong actionInstanceId,
            ulong inputSequence,
            SimulationActionResultKind result,
            string reason);
    }
}
