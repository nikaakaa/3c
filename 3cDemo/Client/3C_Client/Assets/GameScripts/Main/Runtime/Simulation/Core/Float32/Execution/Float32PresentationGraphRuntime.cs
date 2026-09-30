using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public readonly struct Float32PresentationGraphFacts
    {
        public Float32PresentationGraphFacts(ulong renderFrame, Float32Vector3 position,
            Float32Vector3 velocity, Float32Yaw yaw, bool grounded, ulong branchRevision)
        {
            if (renderFrame == 0)
                throw new ArgumentOutOfRangeException(nameof(renderFrame));
            if (branchRevision == 0)
                throw new ArgumentOutOfRangeException(nameof(branchRevision));
            RenderFrame = renderFrame;
            Position = position;
            Velocity = velocity;
            Yaw = yaw;
            Grounded = grounded;
            BranchRevision = branchRevision;
        }

        public ulong RenderFrame { get; }
        public Float32Vector3 Position { get; }
        public Float32Vector3 Velocity { get; }
        public Float32Yaw Yaw { get; }
        public bool Grounded { get; }
        public ulong BranchRevision { get; }

        internal AbilityStateValue ReadCharacterState(string field) => field switch
        {
            CharacterStateProviderFields.Position => AbilityStateValue.FromVector3(Position),
            CharacterStateProviderFields.Velocity => AbilityStateValue.FromVector3(Velocity),
            CharacterStateProviderFields.VerticalVelocity => AbilityStateValue.FromScalar(Velocity.Y),
            CharacterStateProviderFields.BodyYaw => AbilityStateValue.FromYaw(Yaw),
            CharacterStateProviderFields.Grounded => AbilityStateValue.FromBoolean(Grounded),
            _ => throw new InvalidOperationException($"Presentation Character State field '{field}' is unsupported.")
        };
    }

    public readonly struct Float32PresentationGraphOutputIdentity
    {
        public Float32PresentationGraphOutputIdentity(
            EventId eventId,
            string treeGraphId,
            string treeGraphRevision,
            string nodeAuthoringId,
            ulong playbackGeneration,
            ulong branchRevision)
        {
            if (!eventId.IsValid)
                throw new ArgumentException("Presentation output EventId is invalid.", nameof(eventId));
            TreeGraphId = SimulationIdentity.Require(treeGraphId, nameof(treeGraphId));
            TreeGraphRevision = SimulationIdentity.Require(treeGraphRevision, nameof(treeGraphRevision));
            NodeAuthoringId = SimulationIdentity.Require(nodeAuthoringId, nameof(nodeAuthoringId));
            if (playbackGeneration == 0)
                throw new ArgumentOutOfRangeException(nameof(playbackGeneration));
            if (branchRevision == 0)
                throw new ArgumentOutOfRangeException(nameof(branchRevision));
            EventId = eventId;
            PlaybackGeneration = playbackGeneration;
            BranchRevision = branchRevision;
        }

        public EventId EventId { get; }
        public string TreeGraphId { get; }
        public string TreeGraphRevision { get; }
        public string NodeAuthoringId { get; }
        public ulong PlaybackGeneration { get; }
        public ulong BranchRevision { get; }
    }

    public interface IFloat32PresentationGraphOutput
    {
        bool CaptureGraphTrace { get; }
        bool CaptureValueTrace { get; }
        void TraceOperation(OperationHandle operation, string code, string detail, ulong generation);
        void TraceEdge(ProgramControlFlowEdge edge, bool selected, bool passed);
        void TraceValue(OperationHandle operation, string port, AbilityStateValue value, bool input);
        void SubmitCamera(in Float32PresentationGraphOutputIdentity identity, string producer,
            in PresentationCameraRequest activation,
            in PresentationCameraRequest retirement, bool retiring);
    }

    public sealed class Float32PresentationGraphRuntime
    {
        readonly Float32GameplayAbilityExecutionData m_Data;
        readonly Dictionary<string, string> m_InvocationSources = new(StringComparer.Ordinal);
        readonly GameplayAbilityExecutionLayout m_Layout;
        readonly ProgramSourceMapEntry[] m_Entries;
        readonly string[] m_GraphIds;
        readonly AbilityStateValue[] m_Defaults;
        readonly AbilityStateValue[] m_State;
        readonly int[] m_ResetStateSlots;
        readonly ProgramSourceMapEntry[] m_CameraSources;
        readonly string[][] m_InputPortNames;
        readonly string[] m_DefaultOutputPortNames;
        readonly bool[] m_Parameters;
        readonly Values m_Values;
        readonly OperationControlRuntime<Target> m_Control;
        Float32PresentationGraphFacts m_Facts;
        bool m_Executing;
        bool m_CaptureGraphTrace;
        bool m_CaptureValueTrace;
        IFloat32PresentationGraphOutput m_Output;
        bool m_Retiring;
        bool m_ExitRequested;
        bool m_CanRequestExit;
        ulong m_PlaybackGeneration;
        ulong m_BranchRevision;
        readonly string[] m_Callers;
        readonly AbilityTreeClipHook[] m_Hooks;
        readonly string[] m_Producers;
        readonly PresentationCameraRequest[] m_Activations;
        readonly PresentationCameraRequest[] m_Retirements;

        public Float32PresentationGraphRuntime(Float32GameplayAbilityExecutionData data)
        {
            m_Data = data ?? throw new ArgumentNullException(nameof(data));
            m_Layout = Float32GameplayAbilityExecutionLayoutFactory.Create(data, AbilityTimelineMotionWarpCatalog.Empty);
            var entries = new List<ProgramSourceMapEntry>();
            foreach (ProgramSourceMapEntry source in data.SourceMap)
            {
                if (source.TargetKind != ProgramSourceTargetKind.GraphInvocation)
                    continue;
                m_InvocationSources.Add(source.GraphInvocationPath, source.SourceInvocationPath);
                if (source.InvocationCallerKind == ProgramInvocationCallerKind.PresentationMarker ||
                    source.InvocationCallerKind == ProgramInvocationCallerKind.PresentationTreeClip)
                    entries.Add(source);
            }
            if (entries.Count == 0)
                throw new InvalidOperationException("The program contains no Presentation graph entry.");
            m_Entries = entries.ToArray();
            m_GraphIds = new string[m_Entries.Length];
            m_Callers = new string[m_Entries.Length];
            m_Hooks = new AbilityTreeClipHook[m_Entries.Length];
            m_Producers = new string[data.Operations.Count];
            m_CameraSources = new ProgramSourceMapEntry[data.Operations.Count];
            m_InputPortNames = new string[data.Operations.Count][];
            m_DefaultOutputPortNames = new string[data.Operations.Count];
            m_Activations = new PresentationCameraRequest[data.Operations.Count];
            m_Retirements = new PresentationCameraRequest[data.Operations.Count];
            m_Defaults = new AbilityStateValue[data.StateSlots.Count];
            m_State = new AbilityStateValue[data.StateSlots.Count];
            m_Parameters = new bool[data.StateSlots.Count];
            foreach (ProgramGraphCallFrame frame in data.GraphCallFrames)
            {
                foreach (ProgramGraphParameterBinding parameter in frame.Inputs)
                    PrepareParameter(parameter.StateSlot);
                foreach (ProgramGraphParameterBinding parameter in frame.Outputs)
                    PrepareParameter(parameter.StateSlot);
            }
            var visited = new byte[data.Operations.Count];
            for (int index = 0; index < m_Entries.Length; index++)
            {
                ProgramSourceMapEntry entry = m_Entries[index];
                SimulationOperation entryOperation = m_Layout.Operation(new OperationHandle(entry.TargetIndex));
                m_Hooks[index] = entryOperation.Code == SimulationOperationCode.Root
                    ? AbilityTreeClipHook.Root : entryOperation.Code == SimulationOperationCode.TimelineEnter
                        ? (AbilityTreeClipHook)entryOperation.Integer0
                        : throw new InvalidOperationException("Presentation graph binding must target a lifecycle entry.");
                bool marker = entry.InvocationCallerKind == ProgramInvocationCallerKind.PresentationMarker;
                if (marker && m_Hooks[index] != AbilityTreeClipHook.OnEnable)
                    throw new InvalidOperationException("Presentation Marker requires OnEnable.");
                m_Callers[index] = marker ? entry.InvocationCallerId
                    : entry.InvocationCallerId.Substring(0, entry.InvocationCallerId.LastIndexOf('/'));
                m_GraphIds[index] = "tree:" + entry.GraphId;
                PrepareOperation(new OperationHandle(entry.TargetIndex), visited);
            }
            var resetSlots = new HashSet<int>();
            for (int slot = 0; slot < m_Parameters.Length; slot++)
                if (m_Parameters[slot])
                    resetSlots.Add(slot);
            for (int operation = 0; operation < visited.Length; operation++)
                if (visited[operation] == 2)
                    foreach (int slot in data.Operations[operation].StateSlots)
                        resetSlots.Add(slot);
            m_ResetStateSlots = new int[resetSlots.Count];
            resetSlots.CopyTo(m_ResetStateSlots);
            Array.Sort(m_ResetStateSlots);
            m_Values = new Values(this, new Float32GraphValueWorkspace(data, m_Layout));
            m_Control = new OperationControlRuntime<Target>(data.Topology, new Target(this),
                checked(Math.Max(1024, data.Operations.Count * 128)));
        }

        public CharacterSkillId AbilityId => m_Data.AbilityId;
        public int EntryCount => m_Entries.Length;
        public ProgramSourceMapEntry Entry(int binding) => m_Entries[binding];
        public bool TryBind(string parentInvocationPath, string timelineNodeId, string markerId, string graphId, string revision,
            ProgramInvocationCallerKind callerKind, AbilityTreeClipHook hook, out int binding)
        {
            binding = -1;
            if (!m_InvocationSources.TryGetValue(parentInvocationPath, out string parentSourcePath))
                return false;
            int match = -1;
            for (int index = 0; index < m_Entries.Length; index++)
            {
                ProgramSourceMapEntry entry = m_Entries[index];
                if (entry.ParentInvocationPath != parentSourcePath || m_Callers[index] != timelineNodeId ||
                    entry.InvocationCallerKind != callerKind || m_Hooks[index] != hook ||
                    entry.InvocationCallerClipId != markerId || m_GraphIds[index] != graphId || entry.ContentHash != revision)
                    continue;
                if (match >= 0)
                    throw new InvalidOperationException("Presentation Marker graph binding is ambiguous.");
                match = index;
            }
            binding = match;
            return match >= 0;
        }

        public bool Evaluate(int binding, in Float32PresentationGraphFacts facts, ulong playbackGeneration,
            IFloat32PresentationGraphOutput output)
        {
            if (m_Executing)
                throw new InvalidOperationException("Presentation graph evaluation is already active.");
            if (facts.RenderFrame == 0)
                throw new InvalidOperationException("Presentation Marker requires its current read-only fact frame.");
            ProgramSourceMapEntry entry = m_Entries[binding];
            m_Facts = facts;
            m_Output = output ?? throw new ArgumentNullException(nameof(output));
            m_CaptureGraphTrace = output.CaptureGraphTrace;
            m_CaptureValueTrace = output.CaptureValueTrace;
            m_Retiring = m_Hooks[binding] == AbilityTreeClipHook.OnDisable || m_Hooks[binding] == AbilityTreeClipHook.OnDestroy;
            m_ExitRequested = false;
            m_CanRequestExit = entry.InvocationCallerKind == ProgramInvocationCallerKind.PresentationTreeClip && !m_Retiring;
            m_PlaybackGeneration = playbackGeneration;
            m_BranchRevision = facts.BranchRevision;
            m_Executing = true;
            for (int i = 0; i < m_ResetStateSlots.Length; i++)
            {
                int slot = m_ResetStateSlots[i];
                m_State[slot] = m_Defaults[slot];
            }
            try
            {
                m_Values.BeginEvaluation();
                m_Control.BeginEvaluation();
                OperationExecutionResult result = m_Control.Tick(new OperationHandle(entry.TargetIndex));
                if (result != OperationExecutionResult.Success && result != OperationExecutionResult.Failure)
                    throw new InvalidOperationException($"Presentation Marker '{entry.InvocationCallerClipId}' did not complete its OnEnable graph: {result}.");
                return m_ExitRequested;
            }
            finally
            {
                m_Output = null;
                m_CaptureGraphTrace = false;
                m_CaptureValueTrace = false;
                m_Facts = default;
                m_PlaybackGeneration = 0;
                m_BranchRevision = 0;
                m_Executing = false;
            }
        }

        void PrepareParameter(int slot)
        {
            m_Parameters[slot] = true;
            m_Defaults[slot] = InitialValue(m_Data.StateSlots[slot]);
        }

        AbilityStateValue InitialValue(ProgramStateSlot slot) => slot.DefaultConstantIndex >= 0
            ? Values.FromConstant(m_Data.Constants[slot.DefaultConstantIndex])
            : AbilityStateValue.Default(slot.ValueKind);

        void PrepareOperation(OperationHandle handle, byte[] visited)
        {
            if (visited[handle.Value] == 1)
                throw new InvalidOperationException("Presentation graph contains recursive operations.");
            if (visited[handle.Value] == 2)
                return;
            visited[handle.Value] = 1;
            SimulationOperation operation = m_Layout.Operation(handle);
            switch (operation.Code)
            {
                case SimulationOperationCode.TimelineClipExitRequest:
                case SimulationOperationCode.Root:
                case SimulationOperationCode.Sequence:
                case SimulationOperationCode.Selector:
                case SimulationOperationCode.Parallel:
                case SimulationOperationCode.Succeed:
                case SimulationOperationCode.SubGraph:
                case SimulationOperationCode.ConditionResult:
                case SimulationOperationCode.Compare:
                case SimulationOperationCode.And:
                case SimulationOperationCode.Or:
                case SimulationOperationCode.Not:
                case SimulationOperationCode.Constant:
                    break;
                case SimulationOperationCode.TimelineEnter:
                    break;
                case SimulationOperationCode.CameraStateRequest:
                case SimulationOperationCode.CameraEffectRequest:
                case SimulationOperationCode.CameraResponse:
                case SimulationOperationCode.CameraTarget:
                    ProgramReference producer = m_Layout.Topology.FirstReference(handle, ProgramReferenceKind.Producer);
                    if (producer == null || string.IsNullOrWhiteSpace(producer.ExternalIdentity))
                        throw new InvalidOperationException("Presentation Camera operation requires its compiled producer identity.");
                    m_CameraSources[handle.Value] = RequireOperationSource(handle);
                    m_Producers[handle.Value] = producer.ExternalIdentity;
                    m_Activations[handle.Value] = CameraProgramRequestFactory.Build(operation.Code, operation.Integer1,
                        operation.Flags, PresentationCameraRequestLifecycle.Activate, new Float32CameraProgramConstantReader(m_Layout, handle));
                    m_Retirements[handle.Value] = CameraProgramRequestFactory.Build(operation.Code, operation.Integer1,
                        operation.Flags, PresentationCameraRequestLifecycle.Retire, new Float32CameraProgramConstantReader(m_Layout, handle));
                    break;
                case SimulationOperationCode.CharacterStateRead:
                    if (!CharacterStateProviderFields.IsValid(operation.Text0))
                        throw new InvalidOperationException($"Presentation Character State field '{operation.Text0}' is unsupported.");
                    break;
                case SimulationOperationCode.BlackboardGet:
                case SimulationOperationCode.BlackboardSet:
                    ParameterSlot(operation);
                    break;
                default:
                    throw new InvalidOperationException($"Operation '{m_Layout.SourcePath(handle)}' ({operation.Code}) cannot execute in a Presentation graph.");
            }
            OperationValuePortContract ports = GameplayAbilityValuePortContracts.Require(operation.Code, handle, m_Data.GraphCallFrames);
            var inputNames = ports.Inputs.Count == 0 ? Array.Empty<string>() : new string[ports.Inputs.Count];
            for (int i = 0; i < inputNames.Length; i++)
                inputNames[i] = ports.Inputs[i].Identity;
            m_InputPortNames[handle.Value] = inputNames;
            m_DefaultOutputPortNames[handle.Value] = ports.Outputs.Count == 1 ? ports.Outputs[0].Identity : string.Empty;
            foreach (int slot in operation.StateSlots)
                m_Defaults[slot] = InitialValue(m_Data.StateSlots[slot]);
            ReadOnlySpan<CompiledValueInputBinding> inputs = m_Layout.ValueInputs(handle);
            for (int index = 0; index < inputs.Length; index++)
                if (inputs[index].SourceKind == CompiledValueInputSourceKind.Operation)
                    PrepareOperation(inputs[index].SourceOperation, visited);
            PrepareEdges(handle, ProgramControlFlowKind.Child, visited);
            PrepareEdges(handle, ProgramControlFlowKind.Enter, visited);
            visited[handle.Value] = 2;
        }

        void PrepareEdges(OperationHandle operation, ProgramControlFlowKind kind, byte[] visited)
        {
            foreach (ProgramControlFlowEdge edge in m_Layout.Outgoing(operation, kind))
            {
                if (edge.HasCondition)
                    PrepareOperation(edge.Condition, visited);
                PrepareOperation(edge.Target, visited);
            }
        }

        int ParameterSlot(SimulationOperation operation)
        {
            ProgramReference reference = m_Layout.Topology.FirstReference(operation.Handle, ProgramReferenceKind.StateSlot);
            if (reference == null || !m_Parameters[reference.TargetIndex])
                throw new InvalidOperationException($"Presentation graph '{m_Layout.SourcePath(operation.Handle)}' cannot access Gameplay blackboard state.");
            return reference.TargetIndex;
        }

        ProgramSourceMapEntry RequireOperationSource(OperationHandle operation)
        {
            ProgramSourceMapEntry source = null;
            for (int index = 0; index < m_Data.SourceMap.Count; index++)
            {
                ProgramSourceMapEntry candidate = m_Data.SourceMap[index];
                if (candidate.TargetKind == ProgramSourceTargetKind.Operation &&
                    candidate.TargetIndex == operation.Value)
                {
                    if (source != null)
                        throw new InvalidOperationException($"Presentation operation '{operation.Value}' has multiple source entries.");
                    source = candidate;
                }
            }
            if (source == null)
                throw new InvalidOperationException($"Presentation operation '{operation.Value}' has no source entry.");

            return source;
        }

        Float32PresentationGraphOutputIdentity CreateOutputIdentity(SimulationOperation operation)
        {
            ProgramSourceMapEntry source = m_CameraSources[operation.Handle.Value];
            Span<byte> block = stackalloc byte[64];
            var builder = new EventIdBuilder(block);
            builder.Append("presentation-treeclip-output");
            builder.Append(source.GraphId);
            builder.Append(source.ContentHash);
            builder.Append(source.NodeId);
            builder.Append(m_PlaybackGeneration);
            builder.Append(m_BranchRevision);
            return new Float32PresentationGraphOutputIdentity(
                builder.Build(),
                source.GraphId,
                source.ContentHash,
                source.NodeId,
                m_PlaybackGeneration,
                m_BranchRevision);
        }

        void ResetOperation(OperationExecutionDescriptor operation)
        {
            for (int index = 0; index < operation.StateSlots.Count; index++)
            {
                int slot = operation.StateSlots[index];
                if (m_Data.StateSlots[slot].Semantic != ProgramStateSemantic.RunnableActivationGeneration)
                    m_State[slot] = m_Defaults[slot];
            }
        }

        sealed class Values : Float32GraphValueRuntime
        {
            readonly Float32PresentationGraphRuntime m_Owner;

            internal Values(Float32PresentationGraphRuntime owner, Float32GraphValueWorkspace workspace)
                : base(owner.m_Data, owner.m_Layout, workspace) => m_Owner = owner;

            internal static AbilityStateValue FromConstant(ProgramConstant constant) => ValueFromConstant(constant);

            protected override AbilityStateValue EvaluateDomainValue<TTarget>(OperationControlCursor<TTarget> cursor,
                SimulationOperation operation, string outputPort, Float32ValueInputLease inputs)
            {
                return operation.Code switch
                {
                    SimulationOperationCode.BlackboardGet => m_Owner.m_State[m_Owner.ParameterSlot(operation)],
                    SimulationOperationCode.CharacterStateRead => m_Owner.m_Facts.ReadCharacterState(operation.Text0),
                    _ => throw new InvalidOperationException($"Presentation value operation '{operation.Code}' has no bound read capability.")
                };
            }

            internal void WriteParameter<TTarget>(OperationControlCursor<TTarget> cursor, SimulationOperation operation)
                where TTarget : struct, IOperationControlTarget<TTarget>
            {
                int slot = m_Owner.ParameterSlot(operation);
                using Float32ValueInputLease inputs = ReadInputs(cursor, operation);
                if (inputs.Count != 1)
                    throw new InvalidOperationException("Macro parameter write requires exactly one value.");
                m_Owner.m_State[slot] = ConvertValue(inputs[0], m_Ability.StateSlots[slot].ValueKind);
            }

            protected override void ResetGraphCallParameter(int slot) => m_Owner.m_State[slot] = m_Owner.m_Defaults[slot];
            protected override void WriteGraphCallParameter(int slot, AbilityStateValue value) => m_Owner.m_State[slot] = value;
            protected override AbilityStateValue ReadGraphCallParameter(int slot) => m_Owner.m_State[slot];
            protected override void TraceResult(SimulationOperation operation, string port, AbilityStateValue value, bool predictive)
            {
                if (!predictive && m_Owner.m_CaptureValueTrace)
                    m_Owner.m_Output.TraceValue(operation.Handle, string.IsNullOrEmpty(port)
                        ? m_Owner.m_DefaultOutputPortNames[operation.Handle.Value] : port, value, false);
            }
            protected override void TraceInput(SimulationOperation operation, CompiledValueInputBinding input, AbilityStateValue value, bool predictive)
            {
                if (!predictive && m_Owner.m_CaptureValueTrace)
                    m_Owner.m_Output.TraceValue(operation.Handle, m_Owner.m_InputPortNames[operation.Handle.Value][input.TargetPortIndex], value, true);
            }
        }

        readonly struct Target : IOperationControlTarget<Target>, IOperationControlEdgeTraceTarget
        {
            readonly Float32PresentationGraphRuntime m_Owner;
            internal Target(Float32PresentationGraphRuntime owner) => m_Owner = owner;
            public bool DiagnosticsEnabled => m_Owner.m_CaptureGraphTrace || m_Owner.m_CaptureValueTrace;
            public bool ControlTraceEnabled => m_Owner.m_CaptureGraphTrace;
            public void TraceEdge(ProgramControlFlowEdge edge, bool selected, bool passed)
            {
                if (m_Owner.m_CaptureGraphTrace)
                    m_Owner.m_Output.TraceEdge(edge, selected, passed);
            }
            public int ReadInt32(int slot) => m_Owner.m_State[slot].Int32;
            public void WriteInt32(int slot, int value) => m_Owner.m_State[slot] = AbilityStateValue.FromInt32(value);
            public ulong ReadUInt64(int slot) => m_Owner.m_State[slot].UInt64;
            public void WriteUInt64(int slot, ulong value) => m_Owner.m_State[slot] = AbilityStateValue.FromUInt64(value);
            public string ReadIdentity(int slot) => m_Owner.m_State[slot].Identity;
            public void WriteIdentity(int slot, string value) => m_Owner.m_State[slot] = AbilityStateValue.FromIdentity(value);
            public bool EvaluateCondition(OperationControlCursor<Target> cursor, ProgramControlFlowEdge edge) => m_Owner.m_Values.EvaluateCondition(cursor, edge);
            public void PrepareSubGraph(OperationControlCursor<Target> cursor, OperationExecutionDescriptor operation) =>
                m_Owner.m_Values.PrepareSubGraph(cursor, m_Owner.m_Layout.Operation(operation.Handle));
            public void ResetOperationState(OperationExecutionDescriptor operation) => m_Owner.ResetOperation(operation);

            public OperationExecutionResult ExecuteLeaf(OperationControlCursor<Target> cursor, OperationExecutionDescriptor descriptor)
            {
                SimulationOperation operation = m_Owner.m_Layout.Operation(descriptor.Handle);
                switch (operation.Code)
                {
                    case SimulationOperationCode.CameraStateRequest:
                    case SimulationOperationCode.CameraEffectRequest:
                    case SimulationOperationCode.CameraResponse:
                    case SimulationOperationCode.CameraTarget:
                        m_Owner.m_Output.SubmitCamera(m_Owner.CreateOutputIdentity(operation),
                            m_Owner.m_Producers[operation.Handle.Value],
                            m_Owner.m_Activations[operation.Handle.Value], m_Owner.m_Retirements[operation.Handle.Value], m_Owner.m_Retiring);
                        return OperationExecutionResult.Success;
                    case SimulationOperationCode.TimelineClipExitRequest:
                        if (!m_Owner.m_CanRequestExit)
                            throw new InvalidOperationException("Only an active Presentation TreeClip can request its own exit.");
                        m_Owner.m_ExitRequested = true;
                        return OperationExecutionResult.Success;
                    case SimulationOperationCode.BlackboardSet:
                        m_Owner.m_Values.WriteParameter(cursor, operation);
                        return OperationExecutionResult.Success;
                    default:
                        return Float32GraphValueRuntime.ToBoolean(m_Owner.m_Values.Evaluate(cursor, operation.Handle))
                            ? OperationExecutionResult.Success : OperationExecutionResult.Failure;
                }
            }

            public void PrepareActivation(OperationExecutionDescriptor operation) { }
            public void ActivateScopes(OperationControlCursor<Target> cursor, OperationExecutionDescriptor operation, ulong generation) { }
            public void CompleteScopes(OperationExecutionDescriptor operation) { }
            public void ClearStateScope(OperationExecutionDescriptor state) => throw UnsupportedState();
            public OperationStopStatus ContinueLeafStop(OperationControlCursor<Target> cursor, OperationExecutionDescriptor operation, OperationStopContext context) => throw UnsupportedState();
            public void ForceStopLeaf(OperationControlCursor<Target> cursor, OperationExecutionDescriptor operation, OperationStopContext context) => throw UnsupportedState();
            public void EmitTrace(OperationExecutionDescriptor operation, string code, OperationControlTraceSeverity severity, string detail) =>
                m_Owner.m_Output.TraceOperation(operation.Handle, code, detail, m_Owner.m_Control.Cursor.ReadGeneration(operation.Handle));
            public void NotifyStateLifecycle(OperationExecutionDescriptor machine, OperationHandle state, OperationStateLifecyclePhase phase) => throw UnsupportedState();
            public void NotifyStateTransition(OperationExecutionDescriptor machine, OperationHandle exitingState, OperationHandle targetState) => throw UnsupportedState();
            static InvalidOperationException UnsupportedState() => new("Presentation Marker graphs cannot retain a Simulation state lifecycle.");
        }
    }
}
