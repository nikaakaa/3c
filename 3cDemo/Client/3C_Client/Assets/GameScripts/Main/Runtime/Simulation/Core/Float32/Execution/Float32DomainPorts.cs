using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal interface IFloat32InputPort
    {
        bool HasRequest(string requestId, out Float32InputRequestState state);
        void ClearRequest(string requestId);
        SimulationInputValue ReadValue(string inputId, SimulationInputValueKind kind);
    }

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

    internal interface IFloat32MotionModifierSampleSink
    {
        void Submit(MotionWarpSample<Float32Scalar> sample);
    }

    internal interface IFloat32ActivationReader
    {
        ulong ReadGeneration(OperationHandle operation);
    }

    internal interface IFloat32ActionContextReader
    {
        bool IsContextActive(string contextId);
        int FindActive(string contextId, out Float32ActionInstanceState state);
        Float32ActionInstanceState FindOnlyActive();
        Float32ActionInstanceState RequireActive(Float32ActionInstanceState expected);
        Float32ActionInstanceState RequireActive(Float32ActionInstanceReference reference);
        bool ContainsInstance(ulong instanceId);
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
            Float32ActionInstanceState action);
    }

    internal interface IFloat32GameplayTagQuery
    {
        IEnumerable<string> OwnedTags { get; }
        bool HasTag(string tag);
        bool Matches(PortableTagQuery query);
        CharacterStateValue ReadAttribute(SimulationOperation operation, string outputPort);
    }

    internal interface IFloat32GameplayEffectActionPort
    {
        void SetActionTags(ulong actionInstanceId, IEnumerable<string> tags);
        void RemoveActionTags(ulong actionInstanceId);
        void ClearConfirmedAction(ulong actionInstanceId);
    }
}
