using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public readonly struct PresentationGraphCameraOutput
    {
        public PresentationGraphCameraOutput(OperationHandle operation, string producer, PresentationCameraRequest request, PresentationCameraRequest retirement)
        {
            Operation = operation;
            Producer = producer;
            Request = request;
            Retirement = retirement;
        }

        public OperationHandle Operation { get; }
        public string Producer { get; }
        public PresentationCameraRequest Request { get; }
        public PresentationCameraRequest Retirement { get; }
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
        readonly PresentationGraphCameraOutput[] m_CameraOperations;
        readonly PresentationGraphCameraOutput[] m_Outputs;
        readonly Values m_Values;
        readonly OperationControlRuntime<Target> m_Control;
        int m_OutputCount;
        bool m_Executing;

        public Float32PresentationGraphRuntime(Float32GameplayAbilityExecutionData data)
        {
            m_Data = data ?? throw new ArgumentNullException(nameof(data));
            m_Layout = Float32GameplayAbilityExecutionLayoutFactory.Create(data);
            var entries = new List<ProgramSourceMapEntry>();
            foreach (ProgramSourceMapEntry source in data.SourceMap)
                if (source.TargetKind == ProgramSourceTargetKind.GraphInvocation &&
                    source.InvocationCallerKind == ProgramInvocationCallerKind.PresentationMarker)
                    entries.Add(source);
            if (entries.Count == 0)
                throw new InvalidOperationException("The program contains no Presentation Marker entry.");
            m_Entries = entries.ToArray();
            m_GraphIds = new string[m_Entries.Length];
            m_Defaults = new AbilityStateValue[data.StateSlots.Count];
            m_State = new AbilityStateValue[data.StateSlots.Count];
            m_Parameters = new bool[data.StateSlots.Count];
            m_CameraOperations = new PresentationGraphCameraOutput[data.Operations.Count];
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
                if (entryOperation.Code != SimulationOperationCode.TimelineEnter || entryOperation.Integer0 != (int)AbilityTreeClipHook.OnEnable)
                    throw new InvalidOperationException("Presentation Marker binding must target its compiled OnEnable operation.");
                m_GraphIds[index] = "tree:" + entry.GraphId;
                capacity = Math.Max(capacity, PrepareOperation(new OperationHandle(entry.TargetIndex), visited, capacities));
            }
            m_Outputs = new PresentationGraphCameraOutput[capacity];
            m_Values = new Values(this, new Float32GraphValueWorkspace(data, m_Layout));
            m_Control = new OperationControlRuntime<Target>(data.Topology, new Target(this),
                checked(Math.Max(1024, data.Operations.Count * 128)));
        }

        public CharacterSkillId AbilityId => m_Data.AbilityId;
        public int CameraOutputCapacity => m_Outputs.Length;

        public void ValidateCameraResources(Action<PresentationCameraRequest> validate)
        {
            for (int index = 0; index < m_CameraOperations.Length; index++)
                if (m_CameraOperations[index].Producer != null)
                    validate(m_CameraOperations[index].Request);
        }

        public bool TryBind(string parentInvocationPath, string timelineNodeId, string markerId, string graphId, string revision, out int binding)
        {
            int match = -1;
            for (int index = 0; index < m_Entries.Length; index++)
            {
                ProgramSourceMapEntry entry = m_Entries[index];
                if (entry.ParentInvocationPath != parentInvocationPath || entry.InvocationCallerId != timelineNodeId ||
                    entry.InvocationCallerClipId != markerId || m_GraphIds[index] != graphId || entry.ContentHash != revision)
                    continue;
                if (match >= 0)
                    throw new InvalidOperationException("Presentation Marker graph binding is ambiguous.");
                match = index;
            }
            binding = match;
            return match >= 0;
        }

        public ReadOnlySpan<PresentationGraphCameraOutput> Evaluate(int binding)
        {
            if (m_Executing)
                throw new InvalidOperationException("Presentation graph evaluation is already active.");
            ProgramSourceMapEntry entry = m_Entries[binding];
            m_Executing = true;
            m_OutputCount = 0;
            Array.Copy(m_Defaults, m_State, m_State.Length);
            try
            {
                m_Values.BeginEvaluation();
                m_Control.BeginEvaluation();
                OperationExecutionResult result = m_Control.Tick(new OperationHandle(entry.TargetIndex));
                if (result != OperationExecutionResult.Success)
                    throw new InvalidOperationException($"Presentation Marker '{entry.InvocationCallerClipId}' did not complete its OnEnable graph: {result}.");
                return new ReadOnlySpan<PresentationGraphCameraOutput>(m_Outputs, 0, m_OutputCount);
            }
            catch
            {
                m_OutputCount = 0;
                throw;
            }
            finally
            {
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
                    if (operation.Integer0 != (int)AbilityTreeClipHook.OnEnable)
                        throw new InvalidOperationException("Presentation Marker only supports the OnEnable hook.");
                    break;
                case SimulationOperationCode.BlackboardGet:
                case SimulationOperationCode.BlackboardSet:
                    ParameterSlot(operation);
                    break;
                case SimulationOperationCode.CameraStateRequest:
                case SimulationOperationCode.CameraEffectRequest:
                case SimulationOperationCode.CameraResponse:
                case SimulationOperationCode.CameraTarget:
                    IReadOnlyList<ProgramReference> producers = m_Layout.References(handle, ProgramReferenceKind.Producer);
                    if (producers.Count != 1 || string.IsNullOrWhiteSpace(producers[0].ExternalIdentity))
                        throw new InvalidOperationException($"Presentation Camera operation '{m_Layout.SourcePath(handle)}' requires one producer.");
                    m_CameraOperations[handle.Value] = new PresentationGraphCameraOutput(handle, producers[0].ExternalIdentity,
                        CameraProgramRequestFactory.Build(operation.Code, operation.Integer1, operation.Flags,
                            PresentationCameraRequestLifecycle.Activate, new Float32CameraProgramConstantReader(m_Layout, handle)),
                        CameraProgramRequestFactory.Build(operation.Code, operation.Integer1, operation.Flags,
                            PresentationCameraRequestLifecycle.Retire, new Float32CameraProgramConstantReader(m_Layout, handle)));
                    capacity = 1;
                    break;
                default:
                    throw new InvalidOperationException($"Operation '{m_Layout.SourcePath(handle)}' ({operation.Code}) cannot execute in a Presentation Marker.");
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
                if (operation.Code != SimulationOperationCode.BlackboardGet)
                    throw new InvalidOperationException($"Presentation value operation '{operation.Code}' has no bound read capability.");
                return m_Owner.m_State[m_Owner.ParameterSlot(operation)];
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
                        if (m_Owner.m_OutputCount == m_Owner.m_Outputs.Length)
                            throw new InvalidOperationException("Presentation graph exceeded its prepared Camera output bound.");
                        m_Owner.m_Outputs[m_Owner.m_OutputCount++] = m_Owner.m_CameraOperations[operation.Handle.Value];
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
