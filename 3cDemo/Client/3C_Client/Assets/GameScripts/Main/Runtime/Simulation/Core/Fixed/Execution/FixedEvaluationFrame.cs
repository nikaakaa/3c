using ThirdPersonSimulation;
using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation.Fixed
{
    internal interface IFixedSkillExecutionStateAccess
    {
        bool TryGet(int slotIndex, out CharacterStateValue value);
        bool TrySet(int slotIndex, CharacterStateValue value);
        bool TryReset(int slotIndex);
    }

    internal readonly struct FixedEvaluationOutputSavepoint
    {
        public FixedEvaluationOutputSavepoint(int factCount, int presentationCount)
        {
            FactCount = factCount;
            PresentationCount = presentationCount;
        }

        public int FactCount { get; }
        public int PresentationCount { get; }
    }

    internal readonly struct CharacterOperationEvaluation
    {
        public CharacterOperationEvaluation(
            FixedCharacterStateTransaction transaction,
            CharacterControlRuntimeStateTransaction controlStateTransaction,
            ResolvedGameplayMotion gameplayMotion)
        {
            Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            ControlStateTransaction = controlStateTransaction;
            GameplayMotion = gameplayMotion;
        }

        internal FixedCharacterStateTransaction Transaction { get; }
        internal CharacterControlRuntimeStateTransaction ControlStateTransaction { get; }
        public ResolvedGameplayMotion GameplayMotion { get; }
    }

    internal sealed class FixedEvaluationFrame
    {
        readonly List<GameplayFact> m_Facts;
        readonly List<PresentationCommand> m_Presentation;
        readonly List<SimulationTraceRecord> m_Trace;
        readonly FixedCharacterStateTransactionWorkspace m_StateTransactions;
        IFixedSkillExecutionStateAccess m_SkillExecutionStateAccess;
        ulong m_ActionTraceInstanceId;
        string m_ActionTraceSkillId = string.Empty;
        OperationHandle m_ActionTraceEntryOperation = OperationHandle.Invalid;

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
            ControlStateTransaction = request.ControlState == null
                ? null
                : CharacterControlRuntimeStateTransaction.Begin(
                    request.ControlState,
                    request.ControlState.Schema,
                    request.Tick);
            Trace.Begin(request.DiagnosticsEnabled, request.ValueTraceEnabled, request.ControlTraceEnabled);
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
        internal CharacterControlRuntimeStateTransaction ControlStateTransaction { get; private set; }

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
            if (Program.Manifest.Root.IsCharacter && ControlStateTransaction == null)
                throw new InvalidOperationException("Fixed evaluation has no active Control state transaction.");
            CharacterControlRuntimeStateTransaction controlStateTransaction = ControlStateTransaction;
            Transaction = null;
            ControlStateTransaction = null;
            return new CharacterOperationEvaluation(
                transaction,
                controlStateTransaction,
                gameplayMotion);
        }

        public void End()
        {
            if (Transaction != null)
            {
                Transaction.Dispose();
                Transaction = null;
            }
            if (ControlStateTransaction != null)
            {
                ControlStateTransaction.Dispose();
                ControlStateTransaction = null;
            }
            Tick = default;
            Input = null;
            Ingress = Array.Empty<SimulationIngress>();
            Body = default;
            m_ActionTraceInstanceId = 0;
            m_ActionTraceSkillId = string.Empty;
            m_ActionTraceEntryOperation = OperationHandle.Invalid;
            Trace.End();
        }

        internal void AddFact(GameplayFact value) => m_Facts.Add(value);
        internal void AddPresentation(PresentationCommand value) => m_Presentation.Add(value);
        internal void AddTrace(SimulationTraceRecord value) => m_Trace.Add(value);

        internal ulong CurrentActionTraceInstanceId => m_ActionTraceInstanceId;
        internal string CurrentActionTraceSkillId => m_ActionTraceSkillId;
        internal bool HasActionTraceContext => m_ActionTraceEntryOperation.IsValid;

        internal ulong CurrentSkillTraceGeneration
        {
            get
            {
                if (!m_ActionTraceEntryOperation.IsValid)
                    return 0;
                int slot = Layout.FindOperationStateSlot(m_ActionTraceEntryOperation, ProgramStateSemantic.RunnableActivationGeneration);
                if (slot < 0)
                    throw new InvalidOperationException("Skill trace entry has no activation generation state.");
                return ReadState(slot).UInt64;
            }
        }

        internal IDisposable PushActionTraceContext(ulong actionInstanceId, CharacterSkillId skillId, OperationHandle entryOperation)
        {
            ulong previousInstanceId = m_ActionTraceInstanceId;
            string previousSkillId = m_ActionTraceSkillId;
            OperationHandle previousEntryOperation = m_ActionTraceEntryOperation;
            m_ActionTraceInstanceId = actionInstanceId;
            m_ActionTraceSkillId = skillId.IsValid ? skillId.Value : string.Empty;
            m_ActionTraceEntryOperation = entryOperation;
            return new ActionTraceContextScope(this, previousInstanceId, previousSkillId, previousEntryOperation);
        }

        internal void BindSkillExecutionStateAccess(IFixedSkillExecutionStateAccess access)
        {
            if (access == null)
                throw new ArgumentNullException(nameof(access));
            if (m_SkillExecutionStateAccess != null && !ReferenceEquals(m_SkillExecutionStateAccess, access))
                throw new InvalidOperationException("Fixed evaluation frame is already bound to another Skill execution state owner.");
            m_SkillExecutionStateAccess = access;
        }

        internal bool TryGetSkillExecutionState(int slotIndex, out CharacterStateValue value)
        {
            if (m_SkillExecutionStateAccess == null)
            {
                value = default;
                return false;
            }
            return m_SkillExecutionStateAccess.TryGet(slotIndex, out value);
        }

        internal bool TrySetSkillExecutionState(int slotIndex, CharacterStateValue value) =>
            m_SkillExecutionStateAccess != null && m_SkillExecutionStateAccess.TrySet(slotIndex, value);

        internal bool TryResetSkillExecutionState(int slotIndex) =>
            m_SkillExecutionStateAccess != null && m_SkillExecutionStateAccess.TryReset(slotIndex);

        internal void ResetState(int slotIndex)
        {
            if (TryResetSkillExecutionState(slotIndex))
                return;
            Transaction.Reset(slotIndex);
        }

        internal CharacterStateValue ReadState(int slotIndex)
        {
            return TryGetSkillExecutionState(slotIndex, out CharacterStateValue value)
                ? value
                : Transaction.Get(slotIndex);
        }

        internal FixedEvaluationOutputSavepoint CreateOutputSavepoint() =>
            new FixedEvaluationOutputSavepoint(m_Facts.Count, m_Presentation.Count);

        internal void RestoreOutput(FixedEvaluationOutputSavepoint savepoint)
        {
            if (savepoint.FactCount < 0 || savepoint.FactCount > m_Facts.Count ||
                savepoint.PresentationCount < 0 || savepoint.PresentationCount > m_Presentation.Count)
            {
                throw new InvalidOperationException("Fixed evaluation output savepoint is invalid.");
            }
            m_Facts.RemoveRange(savepoint.FactCount, m_Facts.Count - savepoint.FactCount);
            m_Presentation.RemoveRange(savepoint.PresentationCount, m_Presentation.Count - savepoint.PresentationCount);
        }

        sealed class ActionTraceContextScope : IDisposable
        {
            readonly FixedEvaluationFrame m_Owner;
            readonly ulong m_PreviousInstanceId;
            readonly string m_PreviousSkillId;
            readonly OperationHandle m_PreviousEntryOperation;
            bool m_Disposed;

            public ActionTraceContextScope(FixedEvaluationFrame owner, ulong previousInstanceId, string previousSkillId, OperationHandle previousEntryOperation)
            {
                m_Owner = owner;
                m_PreviousInstanceId = previousInstanceId;
                m_PreviousSkillId = previousSkillId;
                m_PreviousEntryOperation = previousEntryOperation;
            }

            public void Dispose()
            {
                if (m_Disposed)
                    return;
                m_Disposed = true;
                m_Owner.m_ActionTraceInstanceId = m_PreviousInstanceId;
                m_Owner.m_ActionTraceSkillId = m_PreviousSkillId;
                m_Owner.m_ActionTraceEntryOperation = m_PreviousEntryOperation;
            }
        }
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
                m_Frame.ResetState(slotIndex);
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
            if (m_Frame.TryGetSkillExecutionState(slotIndex, out CharacterStateValue value))
                return value;
            return m_Frame.Transaction.Get(slotIndex);
        }

        public void Set(int slotIndex, CharacterStateValue value)
        {
            Require(slotIndex);
            if (m_Frame.TrySetSkillExecutionState(slotIndex, value))
                return;
            m_Frame.Transaction.Set(slotIndex, value);
        }

        public void Reset(int slotIndex)
        {
            Require(slotIndex);
            if (m_Frame.TryResetSkillExecutionState(slotIndex))
                return;
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
            int generationSlot = m_Frame.Layout.FindOperationStateSlot(
                operation.Handle,
                ProgramStateSemantic.RunnableActivationGeneration);
            if (generationSlot < 0)
                throw new InvalidOperationException(
                    $"Operation '{SourcePath(operation)}' has no activation generation state.");
            ulong generation = m_Frame.ReadState(generationSlot).UInt64;
            if (generation == 0)
                throw new InvalidOperationException(
                    $"Operation '{SourcePath(operation)}' has no active activation generation.");
            return Next(
                SimulationExecutionSource.FromSkillOperation(operation.Handle, SourcePath(operation)),
                generation,
                channel);
        }

        public SimulationEventHeader Next(SimulationExecutionSource source, ulong generation, string channel)
        {
            ulong sequence = checked(m_State.Get(m_SequenceSlot).UInt64 + 1);
            if (sequence == 0)
                throw new OverflowException("Simulation event sequence overflowed.");
            m_State.Set(m_SequenceSlot, CharacterStateValue.FromUInt64(sequence));
            if (generation == 0)
                throw new ArgumentOutOfRangeException(nameof(generation));
            var activation = new ActivationId(source, generation);
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
        public SimulationEventHeader Next(SimulationExecutionSource source, ulong generation = 1) => m_Sequence.Next(source, generation, "Gameplay");
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

        public SimulationEventHeader Next(SimulationOperation operation) =>
            m_Sequence.Next(operation, "Presentation");
        public SimulationEventHeader Next(SimulationExecutionSource source, ulong generation = 1) => m_Sequence.Next(source, generation, "Presentation");
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
            int generationSlot = m_Frame.Layout.FindOperationStateSlot(
                operation.Handle,
                ProgramStateSemantic.RunnableActivationGeneration);
            ulong generation = generationSlot < 0 ? 1UL : m_Frame.ReadState(generationSlot).UInt64;
            return Next(
                SimulationExecutionSource.FromSkillOperation(
                    operation.Handle,
                    m_Frame.Services.SourcePath(operation.Handle)),
                generation);
        }

        public SimulationEventHeader Next(SimulationExecutionSource source, ulong generation = 1)
        {
            ulong sequence = checked(++m_Sequence);
            if (generation == 0)
                generation = 1;
            var activation = new ActivationId(source, generation);
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
        readonly HashSet<(int Operation, string Port, ProgramValuePortDirection Direction)> m_ValuePorts = new();
        readonly HashSet<string> m_EdgeIds = new(StringComparer.Ordinal);
        readonly ProgramGraphInvocationLayout m_Invocations;
        readonly Dictionary<(int Target, string Port), ProgramControlFlowEdge> m_ValueEdges = new();
        int m_ValueSampleCount;
        readonly FixedEvaluationFrame m_Frame;
        readonly FixedDiagnosticSequence m_Sequence;
        bool m_Enabled;

        public FixedTraceSink(FixedEvaluationFrame frame, FixedDiagnosticSequence sequence)
        {
            m_Frame = frame;
            m_Sequence = sequence;
            m_Invocations = new ProgramGraphInvocationLayout(frame.Program.SourceMap, frame.Program.Operations.Count,
                owner => frame.Layout.FindOperationStateSlot(owner, ProgramStateSemantic.RunnableActivationGeneration));
            foreach (ProgramSourceMapEntry source in frame.Program.SourceMap)
            {
                if (source.TargetKind == ProgramSourceTargetKind.OperationPort && source.ValuePortDirection != ProgramValuePortDirection.None)
                    m_ValuePorts.Add((source.TargetIndex, source.CompiledPortId, source.ValuePortDirection));
                if (source.TargetKind == ProgramSourceTargetKind.Reference && !string.IsNullOrEmpty(source.EdgeId))
                {
                    ProgramControlFlowEdge edge = frame.Program.ControlFlow[source.TargetIndex];
                    m_EdgeIds.Add(edge.Identity);
                    if (edge.Kind == ProgramControlFlowKind.Value)
                        m_ValueEdges.Add((edge.Target.Value, edge.TargetPort), edge);
                }
            }
        }

        public void Begin(bool enabled, bool captureValues = false, bool captureControlFlow = false)
        {
            m_Enabled = enabled;
            CaptureValues = enabled && captureValues;
            CaptureControlFlow = enabled && captureControlFlow;
            m_ValueSampleCount = 0;
            m_Sequence.Reset();
        }

        public bool Enabled => m_Enabled;
        public bool CaptureValues { get; private set; }
        public bool CaptureControlFlow { get; private set; }

        public void AddValueEdge(SimulationOperation target, string port)
        {
            if (CaptureControlFlow && m_ValueEdges.TryGetValue((target.Handle.Value, port), out ProgramControlFlowEdge edge))
                AddControlFlow(edge, true, true);
        }

        public void AddControlFlow(ProgramControlFlowEdge edge, bool selected, bool passed)
        {
            if (!CaptureControlFlow || !m_EdgeIds.Contains(edge.Identity))
                return;
            m_Frame.AddTrace(new SimulationTraceRecord(
                m_Sequence.Next(m_Frame.Program.Operations[edge.Source.Value]), SimulationTraceSeverity.Detail, "Kernel.Flow",
                selected ? "edge_selected" : "edge_evaluated", string.Empty,
                m_Frame.CurrentActionTraceInstanceId, m_Frame.CurrentActionTraceSkillId, m_Frame.CurrentSkillTraceGeneration,
                controlFlow: edge, controlFlowSelected: selected, controlFlowPassed: passed,
                graphInvocationGeneration: InvocationGeneration(edge.Source),
                parentInvocationGeneration: ParentGeneration(edge.Source)));
        }

        public void End()
        {
            m_Enabled = false;
            CaptureValues = false;
            CaptureControlFlow = false;
        }

        public void AddValue(SimulationOperation operation, string portId, ProgramValuePortDirection direction, in CharacterStateValue value)
        {
            if (!CaptureValues || string.IsNullOrEmpty(portId) || !m_ValuePorts.Contains((operation.Handle.Value, portId, direction)))
                return;
            if (m_ValueSampleCount >= SimulationValueTraceLimits.MaxSamplesPerEvaluation)
            {
                if (m_ValueSampleCount == SimulationValueTraceLimits.MaxSamplesPerEvaluation)
                {
                    m_ValueSampleCount++;
                    Add(operation, "value_sampling_limit", SimulationTraceSeverity.Detail,
                        "本次求值的端口采样达到上限，后续值未采集。");
                }
                return;
            }
            m_ValueSampleCount++;
            m_Frame.AddTrace(new SimulationTraceRecord(
                m_Sequence.Next(operation), SimulationTraceSeverity.Detail, "Kernel.Value", "value_sampled", string.Empty,
                m_Frame.CurrentActionTraceInstanceId, m_Frame.CurrentActionTraceSkillId, m_Frame.CurrentSkillTraceGeneration,
                new SimulationValueTrace(portId, direction, value),
                graphInvocationGeneration: InvocationGeneration(operation.Handle),
                parentInvocationGeneration: ParentGeneration(operation.Handle)));
        }

        public void Add(SimulationOperation operation, string code, SimulationTraceSeverity severity, string detail)
        {
            if (!m_Enabled)
                return;
            SimulationEventHeader header = m_Sequence.Next(operation);
            m_Frame.AddTrace(new SimulationTraceRecord(
                header,
                severity,
                "Kernel.Operation",
                code,
                detail,
                m_Frame.CurrentActionTraceInstanceId,
                m_Frame.CurrentActionTraceSkillId,
                m_Frame.CurrentSkillTraceGeneration,
                graphInvocationGeneration: InvocationGeneration(operation.Handle),
                parentInvocationGeneration: ParentGeneration(operation.Handle)));
        }

        public void Add(
            SimulationExecutionSource source,
            string code,
            SimulationTraceSeverity severity,
            string detail,
            ulong generation = 0,
            ulong parentInvocationGeneration = 0)
        {
            if (!m_Enabled)
                return;
            SimulationEventHeader header = m_Sequence.Next(source, generation);
            ulong graphInvocationGeneration = source.IsSkillOperation
                ? generation != 0
                    ? generation
                    : m_Frame.HasActionTraceContext
                        ? InvocationGeneration(source.Operation)
                        : 0
                : 0;
            ulong resolvedParentInvocationGeneration = source.IsSkillOperation
                ? parentInvocationGeneration != 0
                    ? parentInvocationGeneration
                    : m_Frame.HasActionTraceContext
                        ? ParentGeneration(source.Operation)
                        : 0
                : 0;
            m_Frame.AddTrace(new SimulationTraceRecord(
                header,
                severity,
                "Kernel.Operation",
                code,
                detail,
                m_Frame.CurrentActionTraceInstanceId,
                m_Frame.CurrentActionTraceSkillId,
                m_Frame.CurrentSkillTraceGeneration,
                graphInvocationGeneration: graphInvocationGeneration,
                parentInvocationGeneration: resolvedParentInvocationGeneration));
        }

        public void AddActionResult(
            SimulationOperation operation,
            string actionId,
            CharacterSkillId skillId,
            ulong actionInstanceId,
            ulong inputSequence,
            SimulationActionResultKind result,
            string reason)
        {
            if (!m_Enabled)
                return;
            SimulationEventHeader header = m_Sequence.Next(operation);
            m_Frame.AddTrace(new SimulationTraceRecord(
                header,
                SimulationTraceSeverity.Information,
                "Kernel.Action",
                "action_result",
                reason,
                actionInstanceId,
                skillId.Value,
                m_Frame.CurrentSkillTraceGeneration,
                graphInvocationGeneration: InvocationGeneration(operation.Handle),
                parentInvocationGeneration: ParentGeneration(operation.Handle),
                actionId: actionId,
                inputSequence: inputSequence,
                actionResult: result));
        }

        public void AddActionResult(
            SimulationExecutionSource source,
            string actionId,
            CharacterSkillId skillId,
            ulong actionInstanceId,
            ulong inputSequence,
            SimulationActionResultKind result,
            string reason,
            ulong generation = 0,
            ulong parentInvocationGeneration = 0)
        {
            if (!m_Enabled)
                return;
            SimulationEventHeader header = m_Sequence.Next(source, generation);
            ulong graphInvocationGeneration = source.IsSkillOperation
                ? generation != 0
                    ? generation
                    : m_Frame.HasActionTraceContext
                        ? InvocationGeneration(source.Operation)
                        : 0
                : 0;
            ulong resolvedParentInvocationGeneration = source.IsSkillOperation
                ? parentInvocationGeneration != 0
                    ? parentInvocationGeneration
                    : m_Frame.HasActionTraceContext
                        ? ParentGeneration(source.Operation)
                        : 0
                : 0;
            m_Frame.AddTrace(new SimulationTraceRecord(
                header,
                SimulationTraceSeverity.Information,
                "Kernel.Action",
                "action_result",
                reason,
                actionInstanceId,
                skillId.Value,
                m_Frame.CurrentSkillTraceGeneration,
                graphInvocationGeneration: graphInvocationGeneration,
                parentInvocationGeneration: resolvedParentInvocationGeneration,
                actionId: actionId,
                inputSequence: inputSequence,
                actionResult: result));
        }

        public void Add(TimelineTraceOutput output)
        {
            if (!m_Enabled)
                return;
            SimulationTraceSeverity severity = output.Severity switch
            {
                TimelineTraceSeverity.Detail => SimulationTraceSeverity.Detail,
                TimelineTraceSeverity.Information => SimulationTraceSeverity.Information,
                TimelineTraceSeverity.Warning => SimulationTraceSeverity.Warning,
                TimelineTraceSeverity.Error => SimulationTraceSeverity.Error,
                _ => throw new ArgumentOutOfRangeException(nameof(output))
            };
            ulong actionInstanceId = output.ActionContext.IsValid
                ? output.ActionContext.InstanceId
                : m_Frame.CurrentActionTraceInstanceId;
            string skillId = output.ActionContext.HasSkillExecution
                ? output.ActionContext.SkillId.Value
                : m_Frame.CurrentActionTraceSkillId;
            ulong skillExecutionGeneration = output.ActionContext.HasSkillExecution
                ? output.ActionContext.SkillExecutionGeneration
                : m_Frame.CurrentSkillTraceGeneration;
            SimulationOperation operation = m_Frame.Program.Operations[output.Operation.Value];
            m_Frame.AddTrace(new SimulationTraceRecord(
                m_Sequence.Next(operation),
                severity,
                "Kernel.Timeline",
                output.Code,
                output.Detail,
                actionInstanceId,
                skillId,
                skillExecutionGeneration,
                graphInvocationGeneration: InvocationGeneration(output.Operation),
                parentInvocationGeneration: ParentGeneration(output.Operation),
                timelineOperation: output.TimelineOperation,
                timelinePlaybackGeneration: output.PlaybackGeneration,
                timelineTime: output.Time,
                timelineCycle: output.Cycle));
        }

        internal ulong ReadInvocationGeneration(OperationHandle operation) => InvocationGeneration(operation);

        internal ulong ReadParentInvocationGeneration(OperationHandle operation) => ParentGeneration(operation);

        ulong InvocationGeneration(OperationHandle operation)
        {
            if (!m_Frame.HasActionTraceContext)
                return 0;
            int slot = m_Invocations.GenerationSlot(operation);
            return slot >= 0 ? m_Frame.ReadState(slot).UInt64 : 0;
        }

        ulong ParentGeneration(OperationHandle operation)
        {
            if (!m_Frame.HasActionTraceContext)
                return 0;
            int slot = m_Invocations.ParentGenerationSlot(operation);
            return slot >= 0 ? m_Frame.ReadState(slot).UInt64 : 0;
        }
    }
}

