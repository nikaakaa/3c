using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal interface IFloat32ValueInputReader
    {
        Float32ValueInputLease ReadInputs<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation)
            where TTarget : struct, IOperationControlTarget<TTarget>;
    }

    internal interface IFloat32MotionContributionSink
    {
        void Submit(SimulationMotionContribution contribution);
    }

    internal interface IFloat32ActionContextReader
    {
        bool IsContextActive(string contextId);
        bool IsActivationEntry(string entryId);
        bool IsCurrentExecutionContextActive();
        bool TryGetActiveAbilityInstanceId(CharacterSkillId abilityId, out ulong instanceId);
        int FindActive(string contextId, out Float32ActionInstanceState state);
        int FindActive(CharacterSkillId skillId, out Float32ActionInstanceState state);
        Float32ActionInstanceState FindOnlyActive();
        Float32ActionInstanceState RequireActive(Float32ActionInstanceState expected);
        Float32ActionInstanceState RequireActive(Float32ActionInstanceReference reference);
        bool ContainsInstance(ulong instanceId);
        IDisposable PushSkillExecution(Float32ActionInstanceState action);
        bool TryGetCurrentSkillExecution(out Float32ActionInstanceState action);
    }

    internal interface IFloat32ActionAdmissionQuery
    {
        ActionAdmissionDecision PreviewActivation<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation)
            where TTarget : struct, IOperationControlTarget<TTarget>;
    }

    internal interface IFloat32BlackboardPort
    {
        void WriteGraphCallParameter(int valueSlot, AbilityStateValue value);
        void ResetGraphCallParameter(int valueSlot);
        AbilityStateValue ReadGraphCallParameter(int valueSlot);

        AbilityStateValue Read<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation,
            int valueSlot)
            where TTarget : struct, IOperationControlTarget<TTarget>;

        void Write<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation,
            int valueSlot,
            AbilityStateValue value)
            where TTarget : struct, IOperationControlTarget<TTarget>;

        void ClearActionInstanceScopes(ulong actionInstanceId);

        void ProjectBlackboardInput(BlackboardInputStateBinding binding, SimulationInputValue value);

        bool IsActionWindowActive(SimulationOperation operation);

        IDisposable PushTimelineContext(
            SimulationOperation timeline,
            SimulationOperation clip,
            int cycle,
            Float32ActionInstanceState action);
    }

    internal interface IFloat32GameplayTagQuery
    {
        IEnumerable<string> OwnedTags { get; }
        bool HasTag(string tag);
        bool Matches(PortableTagQuery query);
        AbilityStateValue ReadAttribute(SimulationOperation operation, string outputPort);
    }

    internal interface IFloat32GameplayEffectActionPort
    {
        void SetActionTags(ulong actionInstanceId, IEnumerable<string> tags);
        void RemoveActionTags(ulong actionInstanceId);
        void ClearConfirmedAction(ulong actionInstanceId);
    }
}
