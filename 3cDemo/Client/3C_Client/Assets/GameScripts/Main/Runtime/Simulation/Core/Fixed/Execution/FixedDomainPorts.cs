using ThirdPersonSimulation;
using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation.Fixed
{
    internal interface IFixedInputPort
    {
        bool HasRequest(string requestId, out FixedInputRequestState state);
        void ClearRequest(string requestId);
        SimulationInputValue ReadValue(string inputId, SimulationInputValueKind kind);
    }

    internal interface IFixedValueInputReader
    {
        FixedValueInputLease ReadInputs<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation)
            where TTarget : struct, IOperationControlTarget<TTarget>;
    }

    internal interface IFixedMotionContributionSink
    {
        void Submit(SimulationMotionContribution contribution);
    }

    internal interface IFixedMotionModifierSampleSink
    {
        void Submit(MotionWarpSample<FixedScalar, FixedActionInstanceState> sample);
    }

    internal interface IFixedActivationReader
    {
        ulong ReadGeneration(OperationHandle operation);
    }

    internal interface IFixedActionContextReader
    {
        bool IsContextActive(string contextId);
        int FindActive(string contextId, out FixedActionInstanceState state);
        FixedActionInstanceState FindOnlyActive();
        FixedActionInstanceState RequireActive(FixedActionInstanceState expected);
        FixedActionInstanceState RequireActive(FixedActionInstanceReference reference);
        bool ContainsInstance(ulong instanceId);
    }

    internal interface IFixedActionAdmissionQuery
    {
        ActionAdmissionDecision PreviewActivation<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation)
            where TTarget : struct, IOperationControlTarget<TTarget>;
    }

    internal interface IFixedBlackboardPort
    {
        CharacterStateValue Read<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation,
            int valueSlot)
            where TTarget : struct, IOperationControlTarget<TTarget>;

        void Write<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation,
            int valueSlot,
            CharacterStateValue value)
            where TTarget : struct, IOperationControlTarget<TTarget>;

        void ClearActionInstanceScopes(ulong actionInstanceId);

        void ProjectInputDerived(InputDerivedStateBinding binding, SimulationInputValue value);

        bool IsActionWindowActive(SimulationOperation operation);

        IDisposable PushTimelineContext(
            SimulationOperation timeline,
            SimulationOperation clip,
            int cycle,
            FixedActionInstanceState action);
    }

    internal interface IFixedGameplayTagQuery
    {
        IEnumerable<string> OwnedTags { get; }
        bool HasTag(string tag);
        bool Matches(PortableTagQuery query);
        CharacterStateValue ReadAttribute(SimulationOperation operation, string outputPort);
    }

    internal interface IFixedGameplayEffectActionPort
    {
        void SetActionTags(ulong actionInstanceId, IEnumerable<string> tags);
        void RemoveActionTags(ulong actionInstanceId);
        void ClearConfirmedAction(ulong actionInstanceId);
    }
}

