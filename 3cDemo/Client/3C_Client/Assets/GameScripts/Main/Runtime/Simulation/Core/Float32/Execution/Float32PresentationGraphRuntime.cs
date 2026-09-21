using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public readonly struct Float32PresentationGraphFacts
    {
        public Float32PresentationGraphFacts(ulong renderFrame, Float32Vector3 position,
            Float32Vector3 velocity, Float32Yaw yaw, bool grounded)
        {
            if (renderFrame == 0)
                throw new ArgumentOutOfRangeException(nameof(renderFrame));
            RenderFrame = renderFrame;
            Position = position;
            Velocity = velocity;
            Yaw = yaw;
            Grounded = grounded;
        }

        public ulong RenderFrame { get; }
        public Float32Vector3 Position { get; }
        public Float32Vector3 Velocity { get; }
        public Float32Yaw Yaw { get; }
        public bool Grounded { get; }

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

    public interface IFloat32PresentationGraphOutput
    {
        void SubmitCamera(string producer, in PresentationCameraRequest activation,
            in PresentationCameraRequest retirement, bool retiring);
    }

    public sealed class Float32PresentationGraphRuntime
    {
        readonly Float32GameplayAbilityExecutionData m_Data;
        readonly GameplayAbilityExecutionLayout m_Layout;
        readonly ProgramSourceMapEntry[] m_Entries;
        readonly string[] m_GraphIds;
        readonly AbilityStateValue[] m_Defaults;
        readonly AbilityStateValue[] m_State;
        readonly bool[] m_Parameters;
        readonly Values m_Values;
        readonly OperationControlRuntime<Target> m_Control;
        Float32PresentationGraphFacts m_Facts;
        bool m_Executing;
        IFloat32PresentationGraphOutput m_Output;
        bool m_Retiring;
        bool m_ExitRequested;
        bool m_CanRequestExit;
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
                if (source.TargetKind == ProgramSourceTargetKind.GraphInvocation &&
                    (source.InvocationCallerKind == ProgramInvocationCallerKind.PresentationMarker ||
                     source.InvocationCallerKind == ProgramInvocationCallerKind.PresentationTreeClip))
                    entries.Add(source);
            if (entries.Count == 0)
                throw new InvalidOperationException("The program contains no Presentation graph entry.");
            m_Entries = entries.ToArray();
            m_GraphIds = new string[m_Entries.Length];
            m_Callers = new string[m_Entries.Length];
            m_Hooks = new AbilityTreeClipHook[m_Entries.Length];
            m_Producers = new string[data.Operations.Count];
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
            var capacities = new int[data.Operations.Count];
            int capacity = 0;
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
                capacity = Math.Max(capacity, PrepareOperation(new OperationHandle(entry.TargetIndex), visited, capacities));
            }
            m_Values = new Values(this, new Float32GraphValueWorkspace(data, m_Layout));
            m_Control = new OperationControlRuntime<Target>(data.Topology, new Target(this),
                checked(Math.Max(1024, data.Operations.Count * 128)));
        }

        public CharacterSkillId AbilityId => m_Data.AbilityId;
        public bool TryBind(string parentInvocationPath, string timelineNodeId, string markerId, string graphId, string revision,
            ProgramInvocationCallerKind callerKind, AbilityTreeClipHook hook, out int binding)
        {
            int match = -1;
            for (int index = 0; index < m_Entries.Length; index++)
            {
                ProgramSourceMapEntry entry = m_Entries[index];
                if (entry.ParentInvocationPath != parentInvocationPath || m_Callers[index] != timelineNodeId ||
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

        public bool Evaluate(int binding, in Float32PresentationGraphFacts facts, IFloat32PresentationGraphOutput output)
        {
            if (m_Executing)
                throw new InvalidOperationException("Presentation graph evaluation is already active.");
            if (facts.RenderFrame == 0)
                throw new InvalidOperationException("Presentation Marker requires its current read-only fact frame.");
            ProgramSourceMapEntry entry = m_Entries[binding];
            m_Facts = facts;
            m_Output = output;
            m_Retiring = m_Hooks[binding] == AbilityTreeClipHook.OnDisable || m_Hooks[binding] == AbilityTreeClipHook.OnDestroy;
            m_ExitRequested = false;
            m_CanRequestExit = entry.InvocationCallerKind == ProgramInvocationCallerKind.PresentationTreeClip && !m_Retiring;
            m_Executing = true;
            Array.Copy(m_Defaults, m_State, m_State.Length);
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
                m_Facts = default;
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

        int PrepareOperation(OperationHandle handle, byte[] visited, int[] capacities)
        {
            if (visited[handle.Value] == 1)
                throw new InvalidOperationException("Presentation graph contains recursive operations.");
            if (visited[handle.Value] == 2)
                return capacities[handle.Value];
            visited[handle.Value] = 1;
            SimulationOperation operation = m_Layout.Operation(handle);
            int capacity = 0;
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
            foreach (int slot in operation.StateSlots)
                m_Defaults[slot] = InitialValue(m_Data.StateSlots[slot]);
            ReadOnlySpan<CompiledValueInputBinding> inputs = m_Layout.ValueInputs(handle);
            for (int index = 0; index < inputs.Length; index++)
                if (inputs[index].SourceKind == CompiledValueInputSourceKind.Operation)
                    PrepareOperation(inputs[index].SourceOperation, visited, capacities);
            capacity = checked(capacity + PrepareEdges(handle, ProgramControlFlowKind.Child, visited, capacities));
            capacity = checked(capacity + PrepareEdges(handle, ProgramControlFlowKind.Enter, visited, capacities));
            visited[handle.Value] = 2;
            capacities[handle.Value] = capacity;
            return capacity;
        }

        int PrepareEdges(OperationHandle operation, ProgramControlFlowKind kind, byte[] visited, int[] capacities)
        {
            int capacity = 0;
            foreach (ProgramControlFlowEdge edge in m_Layout.Outgoing(operation, kind))
            {
                if (edge.HasCondition)
                    PrepareOperation(edge.Condition, visited, capacities);
                capacity = checked(capacity + PrepareOperation(edge.Target, visited, capacities));
            }
            return capacity;
        }

        int ParameterSlot(SimulationOperation operation)
        {
            ProgramReference reference = m_Layout.Topology.FirstReference(operation.Handle, ProgramReferenceKind.StateSlot);
            if (reference == null || !m_Parameters[reference.TargetIndex])
                throw new InvalidOperationException($"Presentation graph '{m_Layout.SourcePath(operation.Handle)}' cannot access Gameplay blackboard state.");
            return reference.TargetIndex;
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
            protected override void TraceResult(SimulationOperation operation, string port, AbilityStateValue value, bool predictive) { }
            protected override void TraceInput(SimulationOperation operation, CompiledValueInputBinding input, AbilityStateValue value, bool predictive) { }
        }

        readonly struct Target : IOperationControlTarget<Target>
        {
            readonly Float32PresentationGraphRuntime m_Owner;
            internal Target(Float32PresentationGraphRuntime owner) => m_Owner = owner;
            public bool DiagnosticsEnabled => false;
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
                        if (m_Owner.m_Output == null)
                            throw new InvalidOperationException("Presentation graph has no Camera output consumer.");
                        m_Owner.m_Output.SubmitCamera(m_Owner.m_Producers[operation.Handle.Value],
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
            public void EmitTrace(OperationExecutionDescriptor operation, string code, OperationControlTraceSeverity severity, string detail) { }
            public void NotifyStateLifecycle(OperationExecutionDescriptor machine, OperationHandle state, OperationStateLifecyclePhase phase) => throw UnsupportedState();
            public void NotifyStateTransition(OperationExecutionDescriptor machine, OperationHandle exitingState, OperationHandle targetState) => throw UnsupportedState();
            static InvalidOperationException UnsupportedState() => new("Presentation Marker graphs cannot retain a Simulation state lifecycle.");
        }
    }
}
