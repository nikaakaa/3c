using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal interface IFloat32AbilityExecutionSavepoint
    {
        int Depth { get; }
    }

    internal interface IFloat32AbilityExecutionSavepointPort
    {
        IFloat32AbilityExecutionSavepoint CreateSavepoint();
        void Restore(IFloat32AbilityExecutionSavepoint savepoint);
        void Release(IFloat32AbilityExecutionSavepoint savepoint);
        int SavepointDepth { get; }
    }

    internal sealed class Float32AbilityExecutionInput
    {
        public Float32AbilityExecutionInput(
            ulong sequence,
            IReadOnlyList<SimulationInputValue> values,
            IReadOnlyList<SimulationInputRequest> requests)
        {
            if (sequence == 0)
                throw new ArgumentOutOfRangeException(nameof(sequence));
            Sequence = sequence;
            Values = values ?? throw new ArgumentNullException(nameof(values));
            Requests = requests ?? throw new ArgumentNullException(nameof(requests));
        }

        public ulong Sequence { get; }
        public IReadOnlyList<SimulationInputValue> Values { get; }
        public IReadOnlyList<SimulationInputRequest> Requests { get; }
    }

    internal readonly struct Float32AbilityBodyFacts
    {
        public Float32AbilityBodyFacts(ActorId actorId, WorldBodyState body)
        {
            if (!actorId.IsValid || body.ActorId != actorId)
                throw new ArgumentException("Float32 Ability body facts identity is incomplete.", nameof(body));
            IsValid = true;
            Position = body.Position;
            Yaw = body.Yaw;
            Velocity = body.Velocity;
            VerticalVelocity = body.VerticalVelocity;
            Grounded = body.Grounded;
        }

        public bool IsValid { get; }
        public Float32Vector3 Position { get; }
        public Float32Yaw Yaw { get; }
        public Float32Vector3 Velocity { get; }
        public Float32Scalar VerticalVelocity { get; }
        public bool Grounded { get; }
    }

    internal interface IFloat32SkillExecutionStateAccess
    {
        bool TryGet(int slotIndex, out CharacterStateValue value);
        bool TrySet(int slotIndex, CharacterStateValue value);
        bool TryReset(int slotIndex);
    }

    internal readonly struct Float32AbilityOutputSavepoint
    {
        public Float32AbilityOutputSavepoint(int factCount, int presentationCount)
        {
            FactCount = factCount;
            PresentationCount = presentationCount;
        }

        public int FactCount { get; }
        public int PresentationCount { get; }
    }

    internal sealed class Float32AbilityExecutionFrame
    {
        readonly List<GameplayFact> m_Facts;
        readonly List<PresentationCommand> m_Presentation;
        readonly List<SimulationTraceRecord> m_Trace;
        readonly Float32AbilityBodyFacts m_BodyFacts;
        readonly IFloat32AbilityExecutionSavepointPort m_SavepointPort;
        readonly IFloat32GameplayEffectStatePort m_GameplayEffectState;
        readonly IFloat32EquipmentStatePort m_EquipmentState;
        IFloat32SkillExecutionStateAccess m_SkillExecutionStateAccess;
        ulong m_ActionTraceInstanceId;
        string m_ActionTraceSkillId = string.Empty;
        OperationHandle m_ActionTraceEntryOperation = OperationHandle.Invalid;

        public Float32AbilityExecutionFrame(
            Float32GameplayAbilityExecutionInstallation installation,
            ActorId actorId,
            SimulationTick tick,
            Float32AbilityExecutionInput input,
            IReadOnlyList<SimulationIngress> ingress,
            Float32AbilityBodyFacts bodyFacts,
            IFloat32SkillExecutionState skillState,
            IFloat32AbilityExecutionSavepointPort savepointPort,
            IFloat32InputRequestStatePort inputRequests,
            IFloat32ActionRuntimeStatePort actionState,
            IFloat32HandleAllocatorStatePort handleAllocatorState,
            IFloat32EventSequenceStatePort eventSequenceState,
            IFloat32GameplayEffectStatePort gameplayEffectState,
            IFloat32EquipmentStatePort equipmentState,
            Float32AbilityExecutionWorkspace workspace)
        {
            Installation = installation ?? throw new ArgumentNullException(nameof(installation));
            Data = installation.Data;
            Layout = installation.Layout;
            Services = installation.Services;
            ActorId = actorId;
            Tick = tick;
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Ingress = ingress ?? Array.Empty<SimulationIngress>();
            m_BodyFacts = bodyFacts;
            SkillState = skillState ?? throw new ArgumentNullException(nameof(skillState));
            m_SavepointPort = savepointPort;
            InputRequests = inputRequests ?? throw new ArgumentNullException(nameof(inputRequests));
            ActionState = actionState ?? throw new ArgumentNullException(nameof(actionState));
            HandleAllocatorState = handleAllocatorState ?? throw new ArgumentNullException(nameof(handleAllocatorState));
            EventSequenceState = eventSequenceState ?? throw new ArgumentNullException(nameof(eventSequenceState));
            m_GameplayEffectState = gameplayEffectState;
            m_EquipmentState = equipmentState;
            workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            m_Facts = workspace.Facts;
            m_Presentation = workspace.Presentation;
            m_Trace = workspace.Trace;
            EventSequence = new Float32EventSequence(this);
            Facts = new Float32FactSink(this, EventSequence);
            Presentation = new Float32PresentationSink(this, EventSequence);
            Trace = new Float32TraceSink(this, new Float32DiagnosticSequence(this));
        }

        public Float32GameplayAbilityExecutionInstallation Installation { get; }
        public Float32GameplayAbilityExecutionData Data { get; }
        public GameplayAbilityExecutionLayout Layout { get; }
        internal Float32GameplayAbilityExecutionServices Services { get; }
        public GameplayAbilityExecutionIdentity Identity => Services.Identity;
        public SimulationNumericProfile NumericProfile => Data.NumericProfile;
        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public Float32AbilityExecutionInput Input { get; }
        public IReadOnlyList<SimulationIngress> Ingress { get; }
        public Float32AbilityBodyFacts BodyFacts => m_BodyFacts.IsValid
            ? m_BodyFacts
            : throw new InvalidOperationException("Float32 Ability invocation has no Body Facts service.");
        internal IFloat32SkillExecutionState SkillState { get; }
        internal IFloat32AbilityExecutionSavepointPort SavepointPort => m_SavepointPort ??
            throw new InvalidOperationException("Float32 Ability invocation has no execution savepoint service.");
        internal IFloat32InputRequestStatePort InputRequests { get; }
        internal IFloat32ActionRuntimeStatePort ActionState { get; }
        internal IFloat32HandleAllocatorStatePort HandleAllocatorState { get; }
        internal IFloat32EventSequenceStatePort EventSequenceState { get; }
        internal IFloat32GameplayEffectStatePort GameplayEffectState => m_GameplayEffectState ??
            throw new InvalidOperationException("Float32 Ability invocation has no Gameplay Effect state service.");
        internal IFloat32EquipmentStatePort EquipmentState => m_EquipmentState ??
            throw new InvalidOperationException("Float32 Ability invocation has no Equipment state service.");
        internal Float32EventSequence EventSequence { get; }
        internal Float32FactSink Facts { get; }
        internal Float32PresentationSink Presentation { get; }
        internal Float32TraceSink Trace { get; }

        internal Float32StatePort CreateStatePort(string owner, Float32StateAccessPolicy policy) =>
            new Float32StatePort(this, owner, policy);

        internal Float32OperationStateReset CreateOperationStateReset() =>
            new Float32OperationStateReset(this);

        internal ulong CurrentSkillTraceGeneration
        {
            get
            {
                if (!m_ActionTraceEntryOperation.IsValid)
                    return 0;
                int slot = Layout.FindOperationStateSlot(
                    m_ActionTraceEntryOperation,
                    ProgramStateSemantic.RunnableActivationGeneration);
                if (slot < 0)
                    throw new InvalidOperationException("Skill trace entry has no activation generation state.");
                return ReadState(slot).UInt64;
            }
        }

        internal IDisposable PushActionTraceContext(
            ulong actionInstanceId,
            CharacterSkillId skillId,
            OperationHandle entryOperation)
        {
            ulong previousInstanceId = m_ActionTraceInstanceId;
            string previousSkillId = m_ActionTraceSkillId;
            OperationHandle previousEntryOperation = m_ActionTraceEntryOperation;
            m_ActionTraceInstanceId = actionInstanceId;
            m_ActionTraceSkillId = skillId.IsValid ? skillId.Value : string.Empty;
            m_ActionTraceEntryOperation = entryOperation;
            return new ActionTraceContextScope(
                this,
                previousInstanceId,
                previousSkillId,
                previousEntryOperation);
        }

        internal void BindSkillExecutionStateAccess(IFloat32SkillExecutionStateAccess access)
        {
            if (access == null)
                throw new ArgumentNullException(nameof(access));
            if (m_SkillExecutionStateAccess != null && !ReferenceEquals(m_SkillExecutionStateAccess, access))
                throw new InvalidOperationException("Float32 ability frame is already bound to another Skill execution state owner.");
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
            m_SkillExecutionStateAccess != null &&
            m_SkillExecutionStateAccess.TrySet(slotIndex, value);

        internal bool TryResetSkillExecutionState(int slotIndex) =>
            m_SkillExecutionStateAccess != null &&
            m_SkillExecutionStateAccess.TryReset(slotIndex);

        internal ulong CurrentActionTraceInstanceId => m_ActionTraceInstanceId;
        internal string CurrentActionTraceSkillId => m_ActionTraceSkillId;
        internal bool HasActionTraceContext => m_ActionTraceInstanceId != 0;

        internal Float32AbilityOutputSavepoint CreateOutputSavepoint() =>
            new Float32AbilityOutputSavepoint(m_Facts.Count, m_Presentation.Count);

        internal void RestoreOutput(Float32AbilityOutputSavepoint savepoint)
        {
            if (savepoint.FactCount < 0 || savepoint.FactCount > m_Facts.Count ||
                savepoint.PresentationCount < 0 || savepoint.PresentationCount > m_Presentation.Count)
                throw new InvalidOperationException("Float32 Ability output savepoint is stale.");
            m_Facts.RemoveRange(savepoint.FactCount, m_Facts.Count - savepoint.FactCount);
            m_Presentation.RemoveRange(savepoint.PresentationCount, m_Presentation.Count - savepoint.PresentationCount);
        }

        internal void ResetState(int slotIndex)
        {
            if (TryResetSkillExecutionState(slotIndex))
                return;
            Transaction.Reset(slotIndex);
        }

        internal CharacterStateValue ReadState(int slotIndex) =>
            TryGetSkillExecutionState(slotIndex, out CharacterStateValue value)
                ? value
                : Transaction.Get(slotIndex);

        internal void End()
        {
            Trace.End();
            m_Facts.Clear();
            m_Presentation.Clear();
            m_Trace.Clear();
            m_ActionTraceInstanceId = 0;
            m_ActionTraceSkillId = string.Empty;
            m_ActionTraceEntryOperation = OperationHandle.Invalid;
            m_SkillExecutionStateAccess = null;
        }

        internal void AddFact(GameplayFact value) => m_Facts.Add(value);
        internal void AddPresentation(PresentationCommand value) => m_Presentation.Add(value);
        internal void AddTrace(SimulationTraceRecord value) => m_Trace.Add(value);

        sealed class ActionTraceContextScope : IDisposable
        {
            readonly Float32AbilityExecutionFrame m_Owner;
            readonly ulong m_PreviousInstanceId;
            readonly string m_PreviousSkillId;
            readonly OperationHandle m_PreviousEntryOperation;
            bool m_Disposed;

            public ActionTraceContextScope(
                Float32AbilityExecutionFrame owner,
                ulong previousInstanceId,
                string previousSkillId,
                OperationHandle previousEntryOperation)
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

    internal sealed class Float32OperationStateReset
    {
        readonly Float32AbilityExecutionFrame m_Frame;

        public Float32OperationStateReset(Float32AbilityExecutionFrame frame)
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
                if (slotIndex < 0 || slotIndex >= m_Frame.Layout.StateSlots.Count)
                    throw new InvalidOperationException($"Operation '{operation.Handle}' owns invalid state slot '{slotIndex}'.");
                ProgramStateSlot slot = m_Frame.Layout.StateSlots[slotIndex];
                if (slot.Semantic == ProgramStateSemantic.RunnableActivationGeneration)
                    continue;
                m_Frame.ResetState(slotIndex);
            }
        }
    }

    internal sealed class Float32StatePort
    {
        readonly Float32AbilityExecutionFrame m_Frame;
        readonly Float32StateAccessPolicy m_Policy;
        readonly string m_Owner;

        public Float32StatePort(
            Float32AbilityExecutionFrame frame,
            string owner,
            Float32StateAccessPolicy policy)
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
            return m_Frame.SkillState.Get(slotIndex);
        }

        public void Set(int slotIndex, CharacterStateValue value)
        {
            Require(slotIndex);
            if (m_Frame.TrySetSkillExecutionState(slotIndex, value))
                return;
            m_Frame.SkillState.Set(slotIndex, value);
        }

        public void Reset(int slotIndex)
        {
            Require(slotIndex);
            if (m_Frame.TryResetSkillExecutionState(slotIndex))
                return;
            m_Frame.SkillState.Reset(slotIndex);
        }

        void Require(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= m_Frame.Layout.StateSlots.Count)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            ProgramStateSemantic semantic = m_Frame.Layout.StateSlots[slotIndex].Semantic;
            if (!m_Policy.Allows(semantic))
            {
                throw new InvalidOperationException(
                    $"State port '{m_Owner}' cannot access '{semantic}' slot '{slotIndex}'.");
            }
        }
    }

    internal sealed class Float32EventSequence
    {
        readonly Float32AbilityExecutionFrame m_Frame;

        public Float32EventSequence(Float32AbilityExecutionFrame frame)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
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
            ulong sequence = m_Frame.EventSequenceState.NextEventSequence();
            if (generation == 0)
                throw new ArgumentOutOfRangeException(nameof(generation));
            var activation = new ActivationId(source, generation);
            var eventId = EventId.Create(
                new ProgramHash(m_Frame.Identity.ContentHash),
                m_Frame.ActorId,
                activation,
                m_Frame.Tick,
                sequence,
                channel);
            return new SimulationEventHeader(
                m_Frame.NumericProfile,
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

    internal sealed class Float32FactSink
    {
        readonly Float32AbilityExecutionFrame m_Frame;
        readonly Float32EventSequence m_Sequence;

        public Float32FactSink(Float32AbilityExecutionFrame frame, Float32EventSequence sequence)
        {
            m_Frame = frame;
            m_Sequence = sequence;
        }

        public SimulationEventHeader Next(SimulationOperation operation) => m_Sequence.Next(operation, "Gameplay");
        public SimulationEventHeader Next(SimulationExecutionSource source, ulong generation = 1) => m_Sequence.Next(source, generation, "Gameplay");
        public void Add(GameplayFact value) => m_Frame.AddFact(value);
    }

    internal sealed class Float32PresentationSink
    {
        readonly Float32AbilityExecutionFrame m_Frame;
        readonly Float32EventSequence m_Sequence;

        public Float32PresentationSink(Float32AbilityExecutionFrame frame, Float32EventSequence sequence)
        {
            m_Frame = frame;
            m_Sequence = sequence;
        }

        public SimulationEventHeader Next(SimulationOperation operation) =>
            m_Sequence.Next(operation, "Presentation");
        public SimulationEventHeader Next(SimulationExecutionSource source, ulong generation = 1) => m_Sequence.Next(source, generation, "Presentation");
        public void Add(PresentationCommand value) => m_Frame.AddPresentation(value);
    }

    internal sealed class Float32DiagnosticSequence
    {
        readonly Float32AbilityExecutionFrame m_Frame;
        ulong m_Sequence;

        public Float32DiagnosticSequence(Float32AbilityExecutionFrame frame)
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
                new ProgramHash(m_Frame.Identity.ContentHash),
                m_Frame.ActorId,
                activation,
                m_Frame.Tick,
                sequence,
                "Trace");
            return new SimulationEventHeader(
                m_Frame.NumericProfile,
                eventId,
                m_Frame.ActorId,
                m_Frame.Tick,
                activation,
                sequence,
                "Trace");
        }
    }

    internal sealed class Float32TraceSink
    {
        readonly HashSet<(int Operation, string Port, ProgramValuePortDirection Direction)> m_ValuePorts = new();
        readonly HashSet<string> m_EdgeIds = new(StringComparer.Ordinal);
        readonly ProgramGraphInvocationLayout m_Invocations;
        readonly Dictionary<(int Target, string Port), ProgramControlFlowEdge> m_ValueEdges = new();
        int m_ValueSampleCount;
        readonly Float32AbilityExecutionFrame m_Frame;
        readonly Float32DiagnosticSequence m_Sequence;
        bool m_Enabled;

        public Float32TraceSink(Float32AbilityExecutionFrame frame, Float32DiagnosticSequence sequence)
        {
            m_Frame = frame;
            m_Sequence = sequence;
            m_Invocations = new ProgramGraphInvocationLayout(frame.Data.SourceMap, frame.Layout.Operations.Count,
                owner => frame.Layout.FindOperationStateSlot(owner, ProgramStateSemantic.RunnableActivationGeneration));
            foreach (ProgramSourceMapEntry source in frame.Data.SourceMap)
            {
                if (source.TargetKind == ProgramSourceTargetKind.OperationPort && source.ValuePortDirection != ProgramValuePortDirection.None)
                    m_ValuePorts.Add((source.TargetIndex, source.CompiledPortId, source.ValuePortDirection));
                if (source.TargetKind == ProgramSourceTargetKind.Reference && !string.IsNullOrEmpty(source.EdgeId))
                {
                    ProgramControlFlowEdge edge = frame.Data.ControlFlow[source.TargetIndex];
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
                m_Sequence.Next(m_Frame.Layout.Operations[edge.Source.Value]), SimulationTraceSeverity.Detail, "Ability.Flow",
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
                m_Sequence.Next(operation), SimulationTraceSeverity.Detail, "Ability.Value", "value_sampled", string.Empty,
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
                "Ability.Operation",
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
                "Ability.Operation",
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
                "Ability.Action",
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
                "Ability.Action",
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
            SimulationOperation operation = m_Frame.Layout.Operations[output.Operation.Value];
            m_Frame.AddTrace(new SimulationTraceRecord(
                m_Sequence.Next(operation),
                severity,
                "Ability.Timeline",
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
