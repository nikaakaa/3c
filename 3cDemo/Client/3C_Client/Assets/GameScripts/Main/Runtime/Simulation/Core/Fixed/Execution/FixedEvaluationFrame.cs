using ThirdPersonSimulation;
using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation.Fixed
{
    internal readonly struct CharacterOperationEvaluation
    {
        public CharacterOperationEvaluation(
            FixedCharacterStateTransaction transaction,
            ResolvedGameplayMotion gameplayMotion)
        {
            Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            GameplayMotion = gameplayMotion;
        }

        internal FixedCharacterStateTransaction Transaction { get; }
        public ResolvedGameplayMotion GameplayMotion { get; }
    }

    internal sealed class FixedEvaluationFrame
    {
        readonly List<GameplayFact> m_Facts;
        readonly List<PresentationCommand> m_Presentation;
        readonly List<SimulationTraceRecord> m_Trace;
        readonly FixedCharacterStateTransactionWorkspace m_StateTransactions;

        public FixedEvaluationFrame(
            CharacterSimulationProgram program,
            ProgramExecutionLayout layout,
            ActorId actorId,
            FixedEvaluationWorkspace workspace)
        {
            Program = program ?? throw new ArgumentNullException(nameof(program));
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            Layout.RequireProgram(Program);
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));
            Services = Layout.Services;
            ActorId = actorId;
            m_Facts = workspace.Facts;
            m_Presentation = workspace.Presentation;
            m_Trace = workspace.Trace;
            m_StateTransactions = workspace.StateTransactions;
            EventSequence = new FixedEventSequence(this);
            Facts = new FixedFactSink(this, EventSequence);
            Presentation = new FixedPresentationSink(this, EventSequence);
            Trace = new FixedTraceSink(this, new FixedDiagnosticSequence(this));
        }

        public void Begin(SimulationEvaluateRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (!ReferenceEquals(request.Program, Program) ||
                !ReferenceEquals(request.ExecutionLayout, Layout) ||
                request.ActorId != ActorId)
            {
                throw new InvalidOperationException("Fixed evaluation request does not match its Actor evaluator binding.");
            }
            Tick = request.Tick;
            Input = request.Input;
            Ingress = request.Ingress;
            Body = request.PreviousBody;
            Transaction = FixedCharacterStateTransaction.Begin(
                request.Program,
                request.ExecutionLayout,
                request.CurrentState,
                request.ActorId,
                request.Tick,
                m_StateTransactions);
            Trace.Begin(request.DiagnosticsEnabled);
        }

        public CharacterSimulationProgram Program { get; }
        public ProgramExecutionLayout Layout { get; }
        public FixedProgramExecutionServices Services { get; }
        public ActorId ActorId { get; }
        public SimulationTick Tick { get; private set; }
        public CharacterSimulationInput Input { get; private set; }
        public IReadOnlyList<SimulationIngress> Ingress { get; private set; }
        public WorldBodyState Body { get; private set; }
        public FixedEventSequence EventSequence { get; }
        public FixedFactSink Facts { get; }
        public FixedPresentationSink Presentation { get; }
        public FixedTraceSink Trace { get; }
        internal FixedCharacterStateTransaction Transaction { get; private set; }

        public FixedStatePort CreateStatePort(string owner, FixedStateAccessPolicy policy)
        {
            return new FixedStatePort(this, owner, policy);
        }

        public FixedOperationStateReset CreateOperationStateReset()
        {
            return new FixedOperationStateReset(this);
        }

        public CharacterOperationEvaluation Complete(ResolvedGameplayMotion gameplayMotion)
        {
            FixedCharacterStateTransaction transaction = Transaction ??
                throw new InvalidOperationException("Fixed evaluation has no active state transaction.");
            Transaction = null;
            return new CharacterOperationEvaluation(
                transaction,
                gameplayMotion);
        }

        public void End()
        {
            if (Transaction != null)
            {
                Transaction.Dispose();
                Transaction = null;
            }
            Tick = default;
            Input = null;
            Ingress = Array.Empty<SimulationIngress>();
            Body = default;
            Trace.End();
        }

        internal void AddFact(GameplayFact value) => m_Facts.Add(value);
        internal void AddPresentation(PresentationCommand value) => m_Presentation.Add(value);
        internal void AddTrace(SimulationTraceRecord value) => m_Trace.Add(value);
    }

    internal sealed class FixedOperationStateReset
    {
        readonly FixedEvaluationFrame m_Frame;

        public FixedOperationStateReset(FixedEvaluationFrame frame)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
        }

        public void Reset(SimulationOperation operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            for (int i = 0; i < operation.StateSlots.Count; i++)
            {
                int slotIndex = operation.StateSlots[i];
                if (slotIndex < 0 || slotIndex >= m_Frame.Program.StateSlots.Count)
                    throw new InvalidOperationException($"Operation '{operation.Handle}' owns invalid state slot '{slotIndex}'.");
                ProgramStateSlot slot = m_Frame.Program.StateSlots[slotIndex];
                if (slot.Semantic == ProgramStateSemantic.RunnableActivationGeneration)
                    continue;
                m_Frame.Transaction.Reset(slotIndex);
            }
        }
    }

    internal sealed class FixedStatePort
    {
        readonly FixedEvaluationFrame m_Frame;
        readonly FixedStateAccessPolicy m_Policy;
        readonly string m_Owner;

        public FixedStatePort(
            FixedEvaluationFrame frame,
            string owner,
            FixedStateAccessPolicy policy)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            m_Owner = SimulationIdentity.Require(owner, nameof(owner));
            m_Policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }

        public CharacterStateValue Get(int slotIndex)
        {
            Require(slotIndex);
            return m_Frame.Transaction.Get(slotIndex);
        }

        public void Set(int slotIndex, CharacterStateValue value)
        {
            Require(slotIndex);
            m_Frame.Transaction.Set(slotIndex, value);
        }

        public void Reset(int slotIndex)
        {
            Require(slotIndex);
            m_Frame.Transaction.Reset(slotIndex);
        }

        void Require(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= m_Frame.Program.StateSlots.Count)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            ProgramStateSemantic semantic = m_Frame.Program.StateSlots[slotIndex].Semantic;
            if (!m_Policy.Allows(semantic))
            {
                throw new InvalidOperationException(
                    $"State port '{m_Owner}' cannot access '{semantic}' slot '{slotIndex}'.");
            }
        }
    }

    internal sealed class FixedEventSequence
    {
        readonly FixedEvaluationFrame m_Frame;
        readonly FixedStatePort m_State;
        readonly int m_SequenceSlot;

        public FixedEventSequence(FixedEvaluationFrame frame)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            m_State = frame.CreateStatePort("EventSequence", frame.Services.EventSequencePolicy);
            m_SequenceSlot = frame.Layout.FindStateSlot(ProgramStateSemantic.FactSequence, null);
            if (m_SequenceSlot < 0)
                throw new InvalidOperationException("Program has no FactSequence state slot.");
        }

        public SimulationEventHeader Next(SimulationOperation operation, string channel)
        {
            ulong sequence = checked(m_State.Get(m_SequenceSlot).UInt64 + 1);
            if (sequence == 0)
                throw new OverflowException("Simulation event sequence overflowed.");
            m_State.Set(m_SequenceSlot, CharacterStateValue.FromUInt64(sequence));
            int generationSlot = m_Frame.Layout.FindOperationStateSlot(
                operation.Handle,
                ProgramStateSemantic.RunnableActivationGeneration);
            ulong generation = generationSlot < 0 ? 1UL : m_Frame.Transaction.Get(generationSlot).UInt64;
            if (generation == 0)
                generation = 1;
            var activation = new ActivationId(operation.Handle, generation, SourcePath(operation));
            var eventId = EventId.Create(
                m_Frame.Program.ProgramHash,
                m_Frame.ActorId,
                activation,
                m_Frame.Tick,
                sequence,
                channel);
            return new SimulationEventHeader(
                m_Frame.Program.Manifest.NumericProfile,
                eventId,
                m_Frame.ActorId,
                m_Frame.Tick,
                activation,
                sequence,
                channel);
        }

        public string SourcePath(SimulationOperation operation)
        {
            return m_Frame.Services.SourcePath(operation.Handle);
        }
    }

    internal sealed class FixedFactSink
    {
        readonly FixedEvaluationFrame m_Frame;
        readonly FixedEventSequence m_Sequence;

        public FixedFactSink(FixedEvaluationFrame frame, FixedEventSequence sequence)
        {
            m_Frame = frame;
            m_Sequence = sequence;
        }

        public SimulationEventHeader Next(SimulationOperation operation) => m_Sequence.Next(operation, "Gameplay");
        public void Add(GameplayFact value) => m_Frame.AddFact(value);
    }

    internal sealed class FixedPresentationSink
    {
        readonly FixedEvaluationFrame m_Frame;
        readonly FixedEventSequence m_Sequence;

        public FixedPresentationSink(FixedEvaluationFrame frame, FixedEventSequence sequence)
        {
            m_Frame = frame;
            m_Sequence = sequence;
        }

        public SimulationEventHeader Next(SimulationOperation operation) => m_Sequence.Next(operation, "Presentation");
        public void Add(PresentationCommand value) => m_Frame.AddPresentation(value);
    }

    internal sealed class FixedDiagnosticSequence
    {
        readonly FixedEvaluationFrame m_Frame;
        ulong m_Sequence;

        public FixedDiagnosticSequence(FixedEvaluationFrame frame)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
        }

        public void Reset()
        {
            m_Sequence = 0;
        }

        public SimulationEventHeader Next(SimulationOperation operation)
        {
            ulong sequence = checked(++m_Sequence);
            int generationSlot = m_Frame.Layout.FindOperationStateSlot(
                operation.Handle,
                ProgramStateSemantic.RunnableActivationGeneration);
            ulong generation = generationSlot < 0 ? 1UL : m_Frame.Transaction.Get(generationSlot).UInt64;
            if (generation == 0)
                generation = 1;
            var activation = new ActivationId(
                operation.Handle,
                generation,
                m_Frame.Services.SourcePath(operation.Handle));
            var eventId = EventId.Create(
                m_Frame.Program.ProgramHash,
                m_Frame.ActorId,
                activation,
                m_Frame.Tick,
                sequence,
                "Trace");
            return new SimulationEventHeader(
                m_Frame.Program.Manifest.NumericProfile,
                eventId,
                m_Frame.ActorId,
                m_Frame.Tick,
                activation,
                sequence,
                "Trace");
        }
    }

    internal sealed class FixedTraceSink
    {
        readonly FixedEvaluationFrame m_Frame;
        readonly FixedDiagnosticSequence m_Sequence;
        bool m_Enabled;

        public FixedTraceSink(FixedEvaluationFrame frame, FixedDiagnosticSequence sequence)
        {
            m_Frame = frame;
            m_Sequence = sequence;
        }

        public void Begin(bool enabled)
        {
            m_Enabled = enabled;
            m_Sequence.Reset();
        }

        public bool Enabled => m_Enabled;

        public void End() => m_Enabled = false;

        public void Add(SimulationOperation operation, string code, SimulationTraceSeverity severity, string detail)
        {
            if (!m_Enabled)
                return;
            SimulationEventHeader header = m_Sequence.Next(operation);
            m_Frame.AddTrace(new SimulationTraceRecord(header, severity, "Kernel.Operation", code, detail));
        }
    }
}

