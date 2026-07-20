using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation
{
    public sealed class SimulationEvaluateRequest
    {
        readonly ReadOnlyCollection<SimulationIngress> m_Ingress;

        public SimulationEvaluateRequest(
            KernelProgramBinding binding,
            ActorId actorId,
            SimulationTick tick,
            CharacterSimulationInput input,
            IEnumerable<SimulationIngress> ingress,
            CharacterSimulationState currentState,
            WorldBodyState previousBody,
            bool diagnosticsEnabled,
            ISimulationPerformanceSink performance = null)
        {
            Binding = binding ?? throw new ArgumentNullException(nameof(binding));
            Program = binding.Program;
            ExecutionLayout = binding.Layout;
            if (!actorId.IsValid || !tick.IsValid)
                throw new ArgumentException("Evaluate identity is incomplete.");
            Input = input ?? throw new ArgumentNullException(nameof(input));
            CurrentState = currentState ?? throw new ArgumentNullException(nameof(currentState));
            if (previousBody.ActorId != actorId)
                throw new ArgumentException("Evaluate body observation does not match ActorId.", nameof(previousBody));
            if (currentState.NumericProfile != Program.Manifest.NumericProfile || currentState.ProgramId != Program.Manifest.ProgramId || !currentState.ProgramHash.Equals(Program.ProgramHash) || !currentState.LayoutHash.Equals(Program.LayoutHash))
                throw new ArgumentException("Evaluate Character state does not match Program.", nameof(currentState));
            if (input.NumericProfile != Program.Manifest.NumericProfile)
                throw new ArgumentException("Evaluate input Numeric Profile does not match Program.", nameof(input));
            if (tick.Value != checked(currentState.LastCompletedTick + 1))
                throw new ArgumentException("Evaluate Tick must immediately follow Character state.", nameof(tick));
            ActorId = actorId;
            Tick = tick;
            PreviousBody = previousBody;
            DiagnosticsEnabled = diagnosticsEnabled;
            Performance = performance ?? NullSimulationPerformanceSink.Instance;
            var copied = ingress == null ? new List<SimulationIngress>() : new List<SimulationIngress>(ingress);
            copied.Sort(CompareIngress);
            for (int i = 0; i < copied.Count; i++)
            {
                if (copied[i].Header.ActorId != actorId)
                    throw new ArgumentException("Evaluate ingress contains another ActorId.", nameof(ingress));
                if (copied[i].Header.NumericProfile != Program.Manifest.NumericProfile)
                    throw new ArgumentException("Evaluate ingress Numeric Profile does not match Program.", nameof(ingress));
                if (i > 0 && copied[i - 1].Header.SourceTick == copied[i].Header.SourceTick &&
                    copied[i - 1].Header.Sequence == copied[i].Header.Sequence &&
                    copied[i - 1].Header.FactIdentity == copied[i].Header.FactIdentity)
                    throw new ArgumentException("Evaluate ingress contains a duplicate fact.", nameof(ingress));
            }
            m_Ingress = copied.AsReadOnly();
        }

        public CharacterSimulationProgram Program { get; }
        public ProgramExecutionLayout ExecutionLayout { get; }
        public KernelProgramBinding Binding { get; }
        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public CharacterSimulationInput Input { get; }
        public IReadOnlyList<SimulationIngress> Ingress => m_Ingress;
        public CharacterSimulationState CurrentState { get; }
        public WorldBodyState PreviousBody { get; }
        public bool DiagnosticsEnabled { get; }
        public ISimulationPerformanceSink Performance { get; }

        internal static int CompareIngress(SimulationIngress left, SimulationIngress right)
        {
            int byActor = left.Header.ActorId.CompareTo(right.Header.ActorId);
            if (byActor != 0)
                return byActor;
            int byTick = left.Header.SourceTick.CompareTo(right.Header.SourceTick);
            if (byTick != 0)
                return byTick;
            int bySequence = left.Header.Sequence.CompareTo(right.Header.Sequence);
            return bySequence != 0 ? bySequence : left.Header.FactIdentity.CompareTo(right.Header.FactIdentity);
        }
    }

    internal readonly struct ActorOutputWorkspaceLease
    {
        public ActorOutputWorkspaceLease(
            ActorId actorId,
            SimulationTick tick,
            KernelProgramBinding binding,
            ExecutionWorkspaceLease workspaceLease)
        {
            if (!actorId.IsValid || !tick.IsValid || binding == null || !workspaceLease.IsValid)
                throw new ArgumentException("Actor output workspace lease is incomplete.");
            ActorId = actorId;
            Tick = tick;
            Binding = binding;
            WorkspaceLease = workspaceLease;
        }

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public KernelProgramBinding Binding { get; }
        public ExecutionWorkspaceLease WorkspaceLease { get; }
        public ulong Generation => WorkspaceLease.Generation;
        public bool IsValid => ActorId.IsValid && Tick.IsValid && Binding != null && WorkspaceLease.IsValid;
    }

    public sealed class PendingCharacterEvaluation
    {
        readonly Float32CharacterStateTransaction m_Transaction;
        bool m_Consumed;

        internal PendingCharacterEvaluation(
            KernelProgramBinding binding,
            ActorId actorId,
            SimulationTick tick,
            CharacterSimulationState sourceState,
            Float32CharacterStateTransaction transaction,
            ActorOutputWorkspaceLease outputLease,
            CharacterWorldSolveRequest worldRequest,
            bool diagnosticsEnabled)
        {
            Binding = binding ?? throw new ArgumentNullException(nameof(binding));
            Program = binding.Program;
            ExecutionLayout = binding.Layout;
            ActorId = actorId;
            Tick = tick;
            SourceState = sourceState ?? throw new ArgumentNullException(nameof(sourceState));
            m_Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            OutputLease = outputLease;
            if (!ReferenceEquals(transaction.Program, Program) ||
                !ReferenceEquals(transaction.Layout, ExecutionLayout) ||
                !ReferenceEquals(transaction.BaseState, sourceState) ||
                transaction.ActorId != actorId ||
                transaction.Tick != tick ||
                !outputLease.IsValid || outputLease.ActorId != actorId || outputLease.Tick != tick ||
                !ReferenceEquals(outputLease.Binding, binding) ||
                transaction.Status != Float32CharacterStateTransactionStatus.Active)
            {
                throw new InvalidOperationException("Pending evaluation transaction binding is invalid.");
            }
            WorldRequest = worldRequest ?? throw new ArgumentNullException(nameof(worldRequest));
            DiagnosticsEnabled = diagnosticsEnabled;
        }

        public CharacterSimulationProgram Program { get; }
        public KernelProgramBinding Binding { get; }
        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public CharacterWorldSolveRequest WorldRequest { get; }
        public bool DiagnosticsEnabled { get; }
        internal CharacterSimulationState SourceState { get; }
        internal ProgramExecutionLayout ExecutionLayout { get; }
        internal ActorOutputWorkspaceLease OutputLease { get; }

        internal Float32CharacterStateTransaction ClaimForFinalize(
            SimulationKernelSpecializationManifest specialization)
        {
            if (specialization == null)
                throw new ArgumentNullException(nameof(specialization));
            if (m_Consumed)
                throw new InvalidOperationException("Pending Character evaluation has already been consumed.");
            m_Consumed = true;
            if (m_Transaction.Status != Float32CharacterStateTransactionStatus.Active)
            {
                m_Transaction.Dispose();
                throw new InvalidOperationException("Pending Character evaluation does not match the active Kernel specialization.");
            }
            try
            {
                Binding.Require(Program, ExecutionLayout, specialization);
            }
            catch
            {
                m_Transaction.Dispose();
                throw;
            }
            return m_Transaction;
        }

        internal void AbortUnconsumed()
        {
            Binding.Kernel.Abort(this);
        }

        internal bool TryClaimForAbort(
            SimulationKernelSpecializationManifest specialization,
            out Float32CharacterStateTransaction transaction)
        {
            transaction = null;
            if (m_Consumed)
                return false;
            m_Consumed = true;
            try
            {
                Binding.Require(Program, ExecutionLayout, specialization);
                transaction = m_Transaction;
                return true;
            }
            catch
            {
                m_Transaction.Dispose();
                throw;
            }
        }
    }

    public sealed class SimulationFinalizeRequest
    {
        public SimulationFinalizeRequest(
            PendingCharacterEvaluation pending,
            CharacterWorldSolveResult worldResult,
            SolverImplementationId expectedSolverId,
            ISimulationPerformanceSink performance = null)
        {
            Pending = pending ?? throw new ArgumentNullException(nameof(pending));
            WorldResult = worldResult ?? throw new ArgumentNullException(nameof(worldResult));
            if (string.IsNullOrEmpty(expectedSolverId.Value))
                throw new ArgumentException("Expected Solver identity is missing.", nameof(expectedSolverId));
            ExpectedSolverId = expectedSolverId;
            Performance = performance ?? NullSimulationPerformanceSink.Instance;
        }

        public PendingCharacterEvaluation Pending { get; }
        public CharacterWorldSolveResult WorldResult { get; }
        public SolverImplementationId ExpectedSolverId { get; }
        public ISimulationPerformanceSink Performance { get; }
    }

    public readonly struct CharacterBodySample
    {
        public CharacterBodySample(
            ActorId actorId,
            SimulationTick tick,
            WorldBodyState beforeBody,
            WorldBodyState finalBody,
            Float32Vector3 appliedDisplacement,
            Float32Scalar appliedYawDegrees)
        {
            if (!actorId.IsValid || !tick.IsValid || beforeBody.ActorId != actorId || finalBody.ActorId != actorId)
                throw new ArgumentException("Body sample identity is incomplete.");
            ActorId = actorId;
            Tick = tick;
            BeforeBody = beforeBody;
            FinalBody = finalBody;
            AppliedDisplacement = appliedDisplacement;
            AppliedYawDegrees = appliedYawDegrees;
        }

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public WorldBodyState BeforeBody { get; }
        public WorldBodyState FinalBody { get; }
        public Float32Vector3 AppliedDisplacement { get; }
        public Float32Scalar AppliedYawDegrees { get; }
    }

    public sealed class SimulationActorTickResult
    {
        readonly ReadOnlyCollection<GameplayFact> m_GameplayFacts;
        readonly ReadOnlyCollection<PresentationCommand> m_PresentationCommands;
        readonly ReadOnlyCollection<SimulationTraceRecord> m_TraceRecords;
        CharacterStateHash m_StateHash;

        public SimulationActorTickResult(
            ActorId actorId,
            SimulationTick tick,
            CharacterSimulationState state,
            CharacterBodySample bodySample,
            IEnumerable<GameplayFact> gameplayFacts,
            IEnumerable<PresentationCommand> presentationCommands,
            IEnumerable<SimulationTraceRecord> traceRecords)
        {
            if (!actorId.IsValid || !tick.IsValid || bodySample.ActorId != actorId || bodySample.Tick != tick)
                throw new ArgumentException("Actor Tick result identity is incomplete.");
            State = state ?? throw new ArgumentNullException(nameof(state));
            if (state.LastCompletedTick != tick.Value)
                throw new ArgumentException("Actor state Tick does not match result Tick.", nameof(state));
            ActorId = actorId;
            Tick = tick;
            BodySample = bodySample;
            m_GameplayFacts = Copy(gameplayFacts).AsReadOnly();
            m_PresentationCommands = Copy(presentationCommands).AsReadOnly();
            m_TraceRecords = Copy(traceRecords).AsReadOnly();
            ValidateHeaders(m_GameplayFacts, value => value.Header, state.NumericProfile, actorId, tick, "Gameplay fact");
            ValidateHeaders(m_PresentationCommands, value => value.Header, state.NumericProfile, actorId, tick, "Presentation command");
            ValidateHeaders(m_TraceRecords, value => value.Header, state.NumericProfile, actorId, tick, "Trace record");
        }

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public CharacterSimulationState State { get; }
        public CharacterStateHash StateHash
        {
            get
            {
                if (!m_StateHash.IsValid)
                    m_StateHash = CharacterSimulationStateCodec.ComputeHash(State);
                return m_StateHash;
            }
        }
        public CharacterBodySample BodySample { get; }
        public IReadOnlyList<GameplayFact> GameplayFacts => m_GameplayFacts;
        public IReadOnlyList<PresentationCommand> PresentationCommands => m_PresentationCommands;
        public IReadOnlyList<SimulationTraceRecord> TraceRecords => m_TraceRecords;

        static List<T> Copy<T>(IEnumerable<T> values)
        {
            return values == null ? new List<T>() : new List<T>(values);
        }

        static void ValidateHeaders<T>(
            IReadOnlyList<T> values,
            Func<T, SimulationEventHeader> headerSelector,
            SimulationNumericProfile numericProfile,
            ActorId actorId,
            SimulationTick tick,
            string label)
        {
            for (int i = 0; i < values.Count; i++)
            {
                SimulationEventHeader header = headerSelector(values[i]);
                if (header.NumericProfile != numericProfile || header.ActorId != actorId || header.Tick != tick)
                    throw new ArgumentException($"{label} header does not match Actor result identity.");
            }
        }
    }
}
