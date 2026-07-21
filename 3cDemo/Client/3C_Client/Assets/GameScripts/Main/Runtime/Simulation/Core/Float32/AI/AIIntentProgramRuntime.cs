using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace ThirdPersonSimulation
{
    public sealed class CharacterInputCatalogRuntime
    {
        const string ValuePrefix = "input:value:";
        const string RequestPrefix = "input:request:";
        readonly ReadOnlyCollection<SimulationInputValue> m_NeutralValues;
        readonly Dictionary<string, SimulationInputValueKind> m_ValueKinds;
        readonly Dictionary<string, int> m_RequestTimingClasses;

        public CharacterInputCatalogRuntime(CharacterSimulationProgram program)
        {
            Program = program ?? throw new ArgumentNullException(nameof(program));
            var neutral = new List<SimulationInputValue>();
            m_ValueKinds = new Dictionary<string, SimulationInputValueKind>(StringComparer.Ordinal);
            m_RequestTimingClasses = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < program.CatalogEntries.Count; i++)
            {
                ProgramCatalogEntry entry = program.CatalogEntries[i];
                if (entry.Kind == ProgramCatalogEntryKind.InputValue)
                {
                    string inputId = Strip(entry.Identity, ValuePrefix);
                    SimulationInputValue value = CreateNeutralValue(program, entry, inputId);
                    m_ValueKinds.Add(inputId, value.Kind);
                    neutral.Add(value);
                }
                else if (entry.Kind == ProgramCatalogEntryKind.InputRequest)
                {
                    string requestId = Strip(entry.Identity, RequestPrefix);
                    m_RequestTimingClasses.Add(requestId, ReadInt32Field(program, entry, "TimingClass"));
                }
            }
            neutral.Sort((left, right) => string.CompareOrdinal(left.InputId, right.InputId));
            m_NeutralValues = neutral.AsReadOnly();
        }

        public CharacterSimulationProgram Program { get; }
        public IReadOnlyList<SimulationInputValue> NeutralValues => m_NeutralValues;

        public SimulationInputValueKind RequireValueKind(string inputId)
        {
            if (!m_ValueKinds.TryGetValue(inputId ?? string.Empty, out SimulationInputValueKind kind))
                throw new InvalidOperationException($"Character Program has no input value '{inputId}'.");
            return kind;
        }

        public void RequireRequest(string requestId)
        {
            if (!m_RequestTimingClasses.ContainsKey(requestId ?? string.Empty))
                throw new InvalidOperationException($"Character Program has no input request '{requestId}'.");
        }

        public int RequireRequestTimingClass(string requestId)
        {
            if (!m_RequestTimingClasses.TryGetValue(requestId ?? string.Empty, out int timingClass) || timingClass <= 0)
                throw new InvalidOperationException($"Character Program input request '{requestId}' has no valid TimingClass.");
            return timingClass;
        }

        static int ReadInt32Field(CharacterSimulationProgram program, ProgramCatalogEntry entry, string fieldName)
        {
            for (int i = 0; i < entry.Fields.Count; i++)
            {
                ProgramCatalogField field = entry.Fields[i];
                if (!string.Equals(field.Name, fieldName, StringComparison.Ordinal))
                    continue;
                if (field.Kind != ProgramCatalogFieldKind.Constant)
                    break;
                ProgramConstant value = program.Constants[field.ConstantIndex];
                if (value.Kind == ProgramConstantKind.Int32)
                    return value.Int32;
                break;
            }
            throw new InvalidOperationException($"Character Program catalog entry '{entry.Identity}' has no Int32 field '{fieldName}'.");
        }

        static SimulationInputValue CreateNeutralValue(
            CharacterSimulationProgram program,
            ProgramCatalogEntry entry,
            string inputId)
        {
            ProgramCatalogField typeField = null;
            for (int i = 0; i < entry.Fields.Count; i++)
            {
                if (string.Equals(entry.Fields[i].Name, "ValueType", StringComparison.Ordinal))
                {
                    typeField = entry.Fields[i];
                    break;
                }
            }
            if (typeField == null || typeField.Kind != ProgramCatalogFieldKind.Constant)
                throw new InvalidOperationException($"Program input '{inputId}' has no ValueType field.");
            ProgramConstant type = program.Constants[typeField.ConstantIndex];
            if (type.Kind != ProgramConstantKind.Int32)
                throw new InvalidOperationException($"Program input '{inputId}' ValueType is not Int32.");
            return (ProgramInputValueKind)type.Int32 switch
            {
                ProgramInputValueKind.Boolean => SimulationInputValue.FromBoolean(inputId, false),
                ProgramInputValueKind.Scalar => SimulationInputValue.FromScalar(inputId, Float32Scalar.Zero),
                ProgramInputValueKind.Vector2 => SimulationInputValue.FromVector2(inputId, Float32Vector2.Zero),
                ProgramInputValueKind.Vector3 => SimulationInputValue.FromVector3(inputId, Float32Vector3.Zero),
                ProgramInputValueKind.Yaw => SimulationInputValue.FromYaw(inputId, Float32Yaw.Zero),
                ProgramInputValueKind.ActionTargetSnapshot => SimulationInputValue.FromActionTargetSnapshot(inputId, SimulationActionTargetSnapshot.None),
                _ => throw new InvalidOperationException($"Program input '{inputId}' has an unsupported value kind.")
            };
        }

        static string Strip(string identity, string prefix)
        {
            if (string.IsNullOrEmpty(identity) || !identity.StartsWith(prefix, StringComparison.Ordinal))
                throw new InvalidOperationException($"Character input catalog identity '{identity}' is invalid.");
            return identity.Substring(prefix.Length);
        }
    }

    public sealed class AIIntentOutputBuilder
    {
        readonly CharacterInputCatalogRuntime m_Catalog;
        readonly Dictionary<string, SimulationInputValue> m_Values;
        readonly List<SimulationInputRequest> m_Requests = new List<SimulationInputRequest>();
        readonly HashSet<string> m_WrittenInputs = new HashSet<string>(StringComparer.Ordinal);

        public AIIntentOutputBuilder(CharacterInputCatalogRuntime catalog)
        {
            m_Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            m_Values = new Dictionary<string, SimulationInputValue>(StringComparer.Ordinal);
            Reset();
        }

        public void Reset()
        {
            m_Values.Clear();
            m_Requests.Clear();
            m_WrittenInputs.Clear();
            for (int i = 0; i < m_Catalog.NeutralValues.Count; i++)
                m_Values.Add(m_Catalog.NeutralValues[i].InputId, m_Catalog.NeutralValues[i]);
        }

        public void Write(string inputId, AIIntentValue value)
        {
            SimulationInputValueKind expected = m_Catalog.RequireValueKind(inputId);
            SimulationInputValue converted = value.Kind switch
            {
                AIIntentValueKind.Boolean when expected == SimulationInputValueKind.Boolean => SimulationInputValue.FromBoolean(inputId, value.Boolean),
                AIIntentValueKind.Scalar when expected == SimulationInputValueKind.Scalar => SimulationInputValue.FromScalar(inputId, value.Scalar),
                AIIntentValueKind.Vector2 when expected == SimulationInputValueKind.Vector2 => SimulationInputValue.FromVector2(inputId, value.Vector2),
                AIIntentValueKind.Vector3 when expected == SimulationInputValueKind.Vector3 => SimulationInputValue.FromVector3(inputId, value.Vector3),
                AIIntentValueKind.ActionTargetSnapshot when expected == SimulationInputValueKind.ActionTargetSnapshot => SimulationInputValue.FromActionTargetSnapshot(inputId, value.ActionTarget),
                _ => throw new InvalidOperationException($"AI value '{value.Kind}' is incompatible with Character input '{inputId}' kind '{expected}'.")
            };
            m_Values[inputId] = converted;
            m_WrittenInputs.Add(inputId);
        }

        public void SubmitRequest(SimulationInputRequest request)
        {
            m_Catalog.RequireRequest(request.RequestId);
            m_Requests.Add(request);
        }

        public string DescribeWrittenInputs()
        {
            var values = new List<string>(m_WrittenInputs);
            values.Sort(StringComparer.Ordinal);
            return string.Join(",", values);
        }

        public string DescribeRequests()
        {
            var values = new List<string>(m_Requests.Count);
            for (int i = 0; i < m_Requests.Count; i++)
                values.Add($"{m_Requests[i].RequestId}#{m_Requests[i].Sequence}");
            values.Sort(StringComparer.Ordinal);
            return string.Join(",", values);
        }

        public CharacterSimulationInput Freeze(
            SimulationInputBuildContext context,
            string sourceIdentity)
        {
            var values = new List<SimulationInputValue>(m_Values.Values);
            values.Sort((left, right) => string.CompareOrdinal(left.InputId, right.InputId));
            m_Requests.Sort((left, right) =>
            {
                int id = string.CompareOrdinal(left.RequestId, right.RequestId);
                return id != 0 ? id : left.Sequence.CompareTo(right.Sequence);
            });
            return new CharacterSimulationInput(
                context.NumericProfile,
                context.Source,
                sourceIdentity,
                context.InputSequence,
                values,
                m_Requests);
        }
    }

    sealed class AIIntentProgramExecutionLayout
    {
        readonly Dictionary<string, ProgramControlFlowEdge> m_IncomingValues =
            new Dictionary<string, ProgramControlFlowEdge>(StringComparer.Ordinal);

        public AIIntentProgramExecutionLayout(AIIntentProgram program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            for (int i = 0; i < program.Edges.Count; i++)
            {
                ProgramControlFlowEdge edge = program.Edges[i];
                if (edge.Kind != ProgramControlFlowKind.Value)
                    continue;
                string key = InputKey(edge.Target, edge.TargetPort);
                if (!m_IncomingValues.TryAdd(key, edge))
                    throw new InvalidOperationException($"AI value input '{key}' has multiple sources.");
            }
        }

        public bool TryGetInput(OperationHandle operation, string port, out ProgramControlFlowEdge edge) =>
            m_IncomingValues.TryGetValue(InputKey(operation, port), out edge);

        static string InputKey(OperationHandle operation, string port) => $"{operation.Value}:{port}";
    }

    sealed class AIIntentEvaluationWorkspace
    {
        public readonly Dictionary<string, AIIntentValue> ValueCache =
            new Dictionary<string, AIIntentValue>(StringComparer.Ordinal);
        public readonly HashSet<string> ValueStack = new HashSet<string>(StringComparer.Ordinal);
        public readonly List<string> MemoryReads = new List<string>();
        public readonly List<string> MemoryWrites = new List<string>();
        public readonly AIIntentValue[] TickMemory;

        public AIIntentEvaluationWorkspace(AIIntentProgram program)
        {
            TickMemory = new AIIntentValue[program?.Memory.Count ?? throw new ArgumentNullException(nameof(program))];
            Reset(program);
        }

        public void Reset(AIIntentProgram program)
        {
            ValueCache.Clear();
            ValueStack.Clear();
            MemoryReads.Clear();
            MemoryWrites.Clear();
            for (int i = 0; i < TickMemory.Length; i++)
                TickMemory[i] = AIControllerState.DefaultValue(program.Memory[i]);
        }
    }

    sealed class AIIntentEvaluationSlot
    {
        public AIIntentEvaluationContext Context { get; set; }

        public AIIntentEvaluationContext RequireContext() => Context ??
            throw new InvalidOperationException("AI operation control was used outside an active evaluation.");
    }

    public sealed class AIDiagnosticsSnapshot
    {
        public AIDiagnosticsSnapshot(
            ulong observationTick,
            ActorId actorId,
            int activeOperation,
            string activeNodePath,
            string selectedTarget,
            string candidateSummary,
            string memoryReads,
            string memoryWrites,
            string writtenInputs,
            string submittedRequests,
            ulong inputSequence,
            ulong sourceTick,
            string stateDisposition)
        {
            ObservationTick = observationTick;
            ActorId = actorId;
            ActiveOperation = activeOperation;
            ActiveNodePath = activeNodePath ?? string.Empty;
            SelectedTarget = selectedTarget ?? string.Empty;
            CandidateSummary = candidateSummary ?? string.Empty;
            MemoryReads = memoryReads ?? string.Empty;
            MemoryWrites = memoryWrites ?? string.Empty;
            WrittenInputs = writtenInputs ?? string.Empty;
            SubmittedRequests = submittedRequests ?? string.Empty;
            InputSequence = inputSequence;
            SourceTick = sourceTick;
            StateDisposition = stateDisposition ?? string.Empty;
        }

        public ulong ObservationTick { get; }
        public ActorId ActorId { get; }
        public int ActiveOperation { get; }
        public string ActiveNodePath { get; }
        public string SelectedTarget { get; }
        public string CandidateSummary { get; }
        public string MemoryReads { get; }
        public string MemoryWrites { get; }
        public string WrittenInputs { get; }
        public string SubmittedRequests { get; }
        public ulong InputSequence { get; }
        public ulong SourceTick { get; }
        public string StateDisposition { get; }

        public AIDiagnosticsSnapshot WithDisposition(string disposition) => new AIDiagnosticsSnapshot(
            ObservationTick,
            ActorId,
            ActiveOperation,
            ActiveNodePath,
            SelectedTarget,
            CandidateSummary,
            MemoryReads,
            MemoryWrites,
            WrittenInputs,
            SubmittedRequests,
            InputSequence,
            SourceTick,
            disposition);
    }

    public sealed class Float32AIControlSourceRuntime :
        ICharacterControlSourceRuntime,
        ICharacterControlSourceStateRuntime,
        ICharacterControlSourceTransactionObserver,
        ICharacterControlSourceRosterRuntime
    {
        readonly ActorId m_ActorId;
        readonly AIIntentProgram m_AIProgram;
        readonly CharacterSimulationProgram m_CharacterProgram;
        readonly AIPerceptionDescriptor m_Perception;
        readonly CharacterInputCatalogRuntime m_InputCatalog;
        readonly AIIntentProgramExecutionLayout m_ExecutionLayout;
        readonly AIIntentEvaluationWorkspace m_Workspace;
        readonly AIIntentOutputBuilder m_Output;
        readonly AIIntentEvaluationSlot m_EvaluationSlot;
        readonly OperationControlRuntime<AIIntentOperationTarget> m_Control;
        AIControllerState m_State;

        public Float32AIControlSourceRuntime(
            ActorId actorId,
            AIIntentProgram aiProgram,
            CharacterSimulationProgram characterProgram,
            AIPerceptionDescriptor perception)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("AI Control Source ActorId is invalid.", nameof(actorId));
            m_AIProgram = aiProgram ?? throw new ArgumentNullException(nameof(aiProgram));
            m_CharacterProgram = characterProgram ?? throw new ArgumentNullException(nameof(characterProgram));
            m_Perception = perception ?? throw new ArgumentNullException(nameof(perception));
            if (aiProgram.NumericProfile != Float32SimulationNumericProfile.Value ||
                !aiProgram.CharacterProgramId.Equals(characterProgram.Manifest.ProgramId) ||
                !aiProgram.CharacterProgramHash.Equals(characterProgram.ProgramHash) ||
                !aiProgram.PerceptionSchemaHash.Equals(perception.SchemaHash))
            {
                throw new InvalidOperationException("AI Control Source Program, Character Program, Numeric ABI or Perception binding is incompatible.");
            }
            m_ActorId = actorId;
            m_InputCatalog = new CharacterInputCatalogRuntime(characterProgram);
            m_ExecutionLayout = new AIIntentProgramExecutionLayout(aiProgram);
            m_Workspace = new AIIntentEvaluationWorkspace(aiProgram);
            m_Output = new AIIntentOutputBuilder(m_InputCatalog);
            m_EvaluationSlot = new AIIntentEvaluationSlot();
            m_Control = new OperationControlRuntime<AIIntentOperationTarget>(
                aiProgram.Topology,
                new AIIntentOperationTarget(m_EvaluationSlot),
                Math.Max(256, aiProgram.Operations.Count * 64));
            m_State = new AIControllerState(aiProgram);
            SourceIdentity = $"AIIntent/Float32/{aiProgram.ProgramId}/{aiProgram.ProgramHash}/{actorId}";
        }

        public string SourceIdentity { get; }
        public SimulationNumericProfile NumericProfile => Float32SimulationNumericProfile.Value;
        public ProgramId CharacterProgramId => m_CharacterProgram.Manifest.ProgramId;
        public ProgramHash CharacterProgramHash => m_CharacterProgram.ProgramHash;
        public CharacterControlSourceCapability Capabilities =>
            CharacterControlSourceCapability.CommittedObservation |
            CharacterControlSourceCapability.TransactionalState;
        public string StateSchemaId => AIControllerStateCodec.SchemaId;
        public int StateSchemaVersion => AIControllerStateCodec.SchemaVersion;
        public AIDiagnosticsSnapshot LatestDiagnostics { get; private set; }

        public CharacterSimulationInput BuildInput(SimulationInputBuildContext context)
        {
            if (context.ActorId != m_ActorId || context.NumericProfile != NumericProfile)
                throw new InvalidOperationException("AI Control Source received an incompatible input build context.");
            AIPerceptionFrame perception = AIPerceptionFrame.Create(m_ActorId, m_Perception, context.CommittedObservation);
            AIControllerState candidate = m_State.Clone();
            m_Workspace.Reset(m_AIProgram);
            m_Output.Reset();
            var evaluation = new AIIntentEvaluationContext(
                m_AIProgram,
                m_ExecutionLayout,
                m_Workspace,
                candidate,
                perception,
                m_Output,
                context);
            m_EvaluationSlot.Context = evaluation;
            try
            {
                m_Control.BeginEvaluation();
                OperationExecutionResult result = m_Control.TickPersistent(m_AIProgram.SemanticIr.RootOperation);
                if (result == OperationExecutionResult.Failure)
                    throw new InvalidOperationException($"AI Controller '{m_AIProgram.SemanticIr.ControllerId}' failed at '{evaluation.ActiveNodePath}'.");
                CharacterSimulationInput input = m_Output.Freeze(context, SourceIdentity);
                m_State = candidate;
                LatestDiagnostics = new AIDiagnosticsSnapshot(
                    perception.Snapshot.ObservationTick,
                    m_ActorId,
                    evaluation.ActiveOperation.Value,
                    evaluation.ActiveNodePath,
                    evaluation.SelectedTarget.ActorId.Value,
                    DescribeCandidates(perception),
                    string.Join(",", m_Workspace.MemoryReads),
                    string.Join(",", m_Workspace.MemoryWrites),
                    m_Output.DescribeWrittenInputs(),
                    m_Output.DescribeRequests(),
                    context.InputSequence,
                    context.Source.SourceTick,
                    CharacterControlSourceStateDisposition.Prepared.ToString());
                return input;
            }
            finally
            {
                m_EvaluationSlot.Context = null;
            }
        }

        public byte[] CaptureState() => AIControllerStateCodec.Write(m_State);
        public void RestoreState(byte[] state)
        {
            m_State = AIControllerStateCodec.Read(m_AIProgram, state);
            NotifyStateDisposition(CharacterControlSourceStateDisposition.Restored);
        }

        public void NotifyStateDisposition(CharacterControlSourceStateDisposition disposition)
        {
            if (!Enum.IsDefined(typeof(CharacterControlSourceStateDisposition), disposition))
                throw new ArgumentOutOfRangeException(nameof(disposition));
            if (LatestDiagnostics != null)
                LatestDiagnostics = LatestDiagnostics.WithDisposition(disposition.ToString());
        }

        public void ValidateRoster(
            ActorId actorId,
            IReadOnlyList<ActorId> roster,
            StableHash committedObservationCapability)
        {
            if (actorId != m_ActorId || roster == null ||
                !committedObservationCapability.Equals(CommittedActorObservationSchema.CapabilityHash))
            {
                throw new InvalidOperationException("AI Control Source roster or committed observation capability is incompatible.");
            }
            var available = new HashSet<ActorId>(roster);
            if (!available.Contains(m_ActorId))
                throw new InvalidOperationException($"AI Control Source roster has no controlled Actor '{m_ActorId}'.");
            for (int i = 0; i < m_Perception.CandidateActorIds.Count; i++)
            {
                ActorId candidate = m_Perception.CandidateActorIds[i];
                if (candidate == m_ActorId || !available.Contains(candidate))
                    throw new InvalidOperationException($"AI Control Source candidate Actor '{candidate}' is absent from its locked roster or targets self.");
            }
        }

        static string DescribeCandidates(AIPerceptionFrame perception)
        {
            var values = new string[perception.Candidates.Count];
            for (int i = 0; i < values.Length; i++)
                values[i] = perception.Candidates[i].ActorId.Value;
            return string.Join(",", values);
        }
    }

    sealed class AIIntentEvaluationContext
    {
        readonly AIIntentProgramExecutionLayout m_Layout;
        readonly AIIntentEvaluationWorkspace m_Workspace;

        public AIIntentEvaluationContext(
            AIIntentProgram program,
            AIIntentProgramExecutionLayout layout,
            AIIntentEvaluationWorkspace workspace,
            AIControllerState state,
            AIPerceptionFrame perception,
            AIIntentOutputBuilder output,
            SimulationInputBuildContext inputContext)
        {
            Program = program ?? throw new ArgumentNullException(nameof(program));
            m_Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            m_Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            State = state ?? throw new ArgumentNullException(nameof(state));
            Perception = perception ?? throw new ArgumentNullException(nameof(perception));
            Output = output ?? throw new ArgumentNullException(nameof(output));
            InputContext = inputContext;
        }

        public AIIntentProgram Program { get; }
        public AIControllerState State { get; }
        public AIPerceptionFrame Perception { get; }
        public AIIntentOutputBuilder Output { get; }
        public SimulationInputBuildContext InputContext { get; }
        public CommittedActorObservation SelectedTarget { get; private set; }
        public OperationHandle ActiveOperation { get; private set; } = OperationHandle.Invalid;
        public string ActiveNodePath { get; private set; } = string.Empty;

        public void SetActive(AIIntentSemanticOperation operation)
        {
            ActiveOperation = operation.Handle;
            ActiveNodePath = operation.NodePath;
        }

        public bool SelectNearest()
        {
            if (!Perception.TrySelectNearest(out CommittedActorObservation selected))
            {
                SelectedTarget = default;
                return false;
            }
            SelectedTarget = selected;
            return true;
        }

        public AIIntentValue ReadInput(AIIntentSemanticOperation target, string port)
        {
            if (m_Layout.TryGetInput(target.Handle, port, out ProgramControlFlowEdge edge))
                return EvaluateValue(Program.Operation(edge.Source), edge.SourcePort);
            return ConstantValue(target, port);
        }

        public AIIntentValue ReadMemory(AIIntentMemoryDeclaration declaration)
        {
            AIIntentValue value = declaration.Scope == AIMemoryScope.Tick
                ? m_Workspace.TickMemory[declaration.Address]
                : State.ReadMemory(declaration.Address);
            m_Workspace.MemoryReads.Add($"{declaration.Identity}={DescribeValue(value)}");
            return value;
        }

        public void WriteMemory(AIIntentMemoryDeclaration declaration, AIIntentValue value)
        {
            if (value.Kind != declaration.ValueKind)
                throw new InvalidOperationException($"AI memory '{declaration.Identity}' value kind does not match its Program layout.");
            if (declaration.Scope == AIMemoryScope.Tick)
                m_Workspace.TickMemory[declaration.Address] = value;
            else
                State.WriteMemory(declaration.Address, value);
            m_Workspace.MemoryWrites.Add($"{declaration.Identity}={DescribeValue(value)}");
        }

        static string DescribeValue(AIIntentValue value) => value.Kind switch
        {
            AIIntentValueKind.Boolean => value.Boolean.ToString(),
            AIIntentValueKind.Integer => value.Integer.ToString(CultureInfo.InvariantCulture),
            AIIntentValueKind.Scalar => value.Scalar.Value.ToString("R", CultureInfo.InvariantCulture),
            AIIntentValueKind.Vector2 => $"({Format(value.Vector2.X.Value)},{Format(value.Vector2.Y.Value)})",
            AIIntentValueKind.Vector3 => $"({Format(value.Vector3.X.Value)},{Format(value.Vector3.Y.Value)},{Format(value.Vector3.Z.Value)})",
            AIIntentValueKind.ActorId => value.ActorId,
            AIIntentValueKind.ActionTargetSnapshot => value.ActionTarget.HasTarget
                ? $"{value.ActionTarget.TargetId}@({Format(value.ActionTarget.Position.X.Value)},{Format(value.ActionTarget.Position.Y.Value)},{Format(value.ActionTarget.Position.Z.Value)})/{Format(value.ActionTarget.Yaw.Degrees.Value)}"
                : "None",
            _ => throw new InvalidOperationException($"AI diagnostics value kind '{value.Kind}' is unsupported.")
        };

        static string Format(float value) => value.ToString("R", CultureInfo.InvariantCulture);

        public AIIntentValue EvaluateValue(AIIntentSemanticOperation operation, string outputPort)
        {
            string key = $"{operation.Handle.Value}:{outputPort}";
            if (m_Workspace.ValueCache.TryGetValue(key, out AIIntentValue value))
                return value;
            if (!m_Workspace.ValueStack.Add(key))
                throw new InvalidOperationException($"AI value graph contains a cycle at '{operation.NodePath}/{outputPort}'.");
            try
            {
                value = EvaluateValueCore(operation, outputPort);
                m_Workspace.ValueCache.Add(key, value);
                return value;
            }
            finally
            {
                m_Workspace.ValueStack.Remove(key);
            }
        }

        AIIntentValue EvaluateValueCore(AIIntentSemanticOperation operation, string outputPort)
        {
            SetActive(operation);
            switch (operation.Code)
            {
                case SimulationOperationCode.AIReadSelfObservation:
                    if (Matches(outputPort, "ActorId", "m_ActorId"))
                        return AIIntentValue.FromActorId(Perception.Self.ActorId.Value);
                    if (Matches(outputPort, "Position", "m_ObservedPosition"))
                        return AIIntentValue.FromVector3(Perception.Self.Body.Position);
                    if (Matches(outputPort, "Yaw", "m_Yaw"))
                        return AIIntentValue.FromScalar(Perception.Self.Body.Yaw.Degrees);
                    break;
                case SimulationOperationCode.AIEnumerateConfiguredCandidates:
                    return AIIntentValue.FromInteger(Perception.Candidates.Count);
                case SimulationOperationCode.AIReadTargetDistance:
                    return AIIntentValue.FromScalar(SelectedTarget.ActorId.IsValid
                        ? (SelectedTarget.Body.Position - Perception.Self.Body.Position).Magnitude
                        : Float32Scalar.Zero);
                case SimulationOperationCode.AIReadTargetDirection:
                    if (!SelectedTarget.ActorId.IsValid)
                        return AIIntentValue.FromVector2(Float32Vector2.Zero);
                    Float32Vector3 delta = SelectedTarget.Body.Position - Perception.Self.Body.Position;
                    return AIIntentValue.FromVector2(new Float32Vector2(delta.X, delta.Z).Normalized);
                case SimulationOperationCode.AIReadSelectedTargetSnapshot:
                    return AIIntentValue.FromActionTarget(SelectedTarget.ActorId.IsValid
                        ? new SimulationActionTargetSnapshot(
                            SelectedTarget.ActorId.Value,
                            SelectedTarget.Body.Position,
                            SelectedTarget.Body.Yaw)
                        : SimulationActionTargetSnapshot.None);
                case SimulationOperationCode.AIReadMemory:
                    return ReadMemory(Program.GetRequiredMemory(operation.MemoryIdentity));
                case SimulationOperationCode.Compare:
                    return AIIntentValue.FromBoolean(Compare(operation));
                case SimulationOperationCode.And:
                    return AIIntentValue.FromBoolean(ToBoolean(ReadInput(operation, "Input1")) && ToBoolean(ReadInput(operation, "Input2")));
                case SimulationOperationCode.Or:
                    return AIIntentValue.FromBoolean(ToBoolean(ReadInput(operation, "Input1")) || ToBoolean(ReadInput(operation, "Input2")));
                case SimulationOperationCode.Not:
                    return AIIntentValue.FromBoolean(!ToBoolean(ReadInput(operation, "Input")));
                case SimulationOperationCode.Constant:
                    return m_Layout.TryGetInput(operation.Handle, outputPort, out _)
                        ? ReadInput(operation, outputPort)
                        : ConstantValue(operation, outputPort);
            }
            throw new InvalidOperationException($"AI value output '{operation.NodePath}/{outputPort}' is unsupported.");
        }

        bool Compare(AIIntentSemanticOperation operation)
        {
            double left = ToNumber(ReadInput(operation, "Value1"));
            double right = ToNumber(ReadInput(operation, "Value2"));
            return operation.Integer0 switch
            {
                0 => left == right,
                1 => left != right,
                2 => left < right,
                3 => left <= right,
                4 => left >= right,
                5 => left > right,
                _ => throw new InvalidOperationException($"AI Compare mode '{operation.Integer0}' is invalid.")
            };
        }

        static AIIntentValue ConstantValue(AIIntentSemanticOperation operation, string port)
        {
            bool second = Matches(port, "Input2", "Value2");
            return operation.ValueKind switch
            {
                AIIntentValueKind.Boolean => AIIntentValue.FromBoolean(second ? operation.Integer1 != 0 : operation.Integer0 != 0),
                AIIntentValueKind.Integer => AIIntentValue.FromInteger(second ? operation.Integer1 : operation.Integer0),
                AIIntentValueKind.Scalar => AIIntentValue.FromScalar(Float32Scalar.FromDouble(second ? operation.Scalar1 : operation.Scalar0)),
                AIIntentValueKind.Vector2 => AIIntentValue.FromVector2(new Float32Vector2(Float32Scalar.FromDouble(operation.Scalar0), Float32Scalar.FromDouble(operation.Scalar1))),
                AIIntentValueKind.Vector3 => AIIntentValue.FromVector3(new Float32Vector3(Float32Scalar.FromDouble(operation.Scalar0), Float32Scalar.FromDouble(operation.Scalar1), Float32Scalar.FromDouble(operation.Scalar2))),
                AIIntentValueKind.ActorId => AIIntentValue.FromActorId(operation.BindingIdentity),
                AIIntentValueKind.ActionTargetSnapshot => AIIntentValue.FromActionTarget(
                    new SimulationActionTargetSnapshot(
                        operation.BindingIdentity,
                        new Float32Vector3(
                            Float32Scalar.FromDouble(operation.Scalar0),
                            Float32Scalar.FromDouble(operation.Scalar1),
                            Float32Scalar.FromDouble(operation.Scalar2)),
                        new Float32Yaw(Float32Scalar.FromDouble(operation.Scalar3)))),
                _ => throw new InvalidOperationException($"AI constant value kind '{operation.ValueKind}' is unsupported.")
            };
        }

        public static bool ToBoolean(AIIntentValue value) => value.Kind switch
        {
            AIIntentValueKind.Boolean => value.Boolean,
            AIIntentValueKind.Integer => value.Integer != 0,
            AIIntentValueKind.Scalar => value.Scalar != Float32Scalar.Zero,
            AIIntentValueKind.ActorId => !string.IsNullOrEmpty(value.ActorId),
            AIIntentValueKind.ActionTargetSnapshot => value.ActionTarget.HasTarget,
            _ => throw new InvalidOperationException($"AI value kind '{value.Kind}' cannot be used as Boolean.")
        };

        static double ToNumber(AIIntentValue value) => value.Kind switch
        {
            AIIntentValueKind.Integer => value.Integer,
            AIIntentValueKind.Scalar => value.Scalar.Value,
            _ => throw new InvalidOperationException($"AI value kind '{value.Kind}' cannot be compared numerically.")
        };

        public int RequireInt32Slot(AIIntentSemanticOperation operation, ProgramStateSemantic semantic)
        {
            OperationExecutionDescriptor descriptor = Program.Topology.Operation(operation.Handle);
            for (int i = 0; i < descriptor.StateSlots.Count; i++)
            {
                int index = descriptor.StateSlots[i];
                ProgramStateSlot slot = Program.StateSlots[index];
                if (slot.Semantic == semantic && slot.ValueKind == ProgramStateValueKind.Int32)
                    return index;
            }
            throw new InvalidOperationException($"AI operation '{operation.NodeIdentity}' has no Int32 state slot '{semantic}'.");
        }

        static bool Matches(string value, params string[] candidates)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                if (string.Equals(value, candidates[i], StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

    }

    readonly struct AIIntentOperationTarget : IOperationControlTarget<AIIntentOperationTarget>
    {
        readonly AIIntentEvaluationSlot m_Slot;

        public AIIntentOperationTarget(AIIntentEvaluationSlot slot)
        {
            m_Slot = slot ?? throw new ArgumentNullException(nameof(slot));
        }

        AIIntentEvaluationContext Context => m_Slot.RequireContext();

        public bool DiagnosticsEnabled => true;
        public int ReadInt32(int slotIndex) => Context.State.ReadInt32(slotIndex);
        public void WriteInt32(int slotIndex, int value) => Context.State.WriteInt32(slotIndex, value);
        public ulong ReadUInt64(int slotIndex) => Context.State.ReadUInt64(slotIndex);
        public void WriteUInt64(int slotIndex, ulong value) => Context.State.WriteUInt64(slotIndex, value);
        public string ReadIdentity(int slotIndex) => Context.State.ReadIdentity(slotIndex);
        public void WriteIdentity(int slotIndex, string value) => Context.State.WriteIdentity(slotIndex, value);

        public bool EvaluateCondition(OperationControlCursor<AIIntentOperationTarget> cursor, ProgramControlFlowEdge edge) =>
            AIIntentEvaluationContext.ToBoolean(Context.EvaluateValue(Context.Program.Operation(edge.Condition), "Result"));

        public OperationExecutionResult ExecuteLeaf(
            OperationControlCursor<AIIntentOperationTarget> cursor,
            OperationExecutionDescriptor descriptor)
        {
            AIIntentEvaluationContext context = Context;
            AIIntentSemanticOperation operation = context.Program.Operation(descriptor.Handle);
            context.SetActive(operation);
            switch (operation.Code)
            {
                case SimulationOperationCode.AISelectNearestCandidate:
                    return context.SelectNearest() ? OperationExecutionResult.Success : OperationExecutionResult.Failure;
                case SimulationOperationCode.AIWriteMemory:
                    AIIntentMemoryDeclaration memory = context.Program.GetRequiredMemory(operation.MemoryIdentity);
                    context.WriteMemory(memory, context.ReadInput(operation, "Value"));
                    return OperationExecutionResult.Success;
                case SimulationOperationCode.AIWriteContinuousInput:
                    context.Output.Write(operation.BindingIdentity, context.ReadInput(operation, "Value"));
                    return OperationExecutionResult.Success;
                case SimulationOperationCode.AIWriteActionTargetSnapshot:
                    SimulationActionTargetSnapshot target = context.SelectedTarget.ActorId.IsValid
                        ? new SimulationActionTargetSnapshot(
                            context.SelectedTarget.ActorId.Value,
                            context.SelectedTarget.Body.Position,
                            context.SelectedTarget.Body.Yaw)
                        : SimulationActionTargetSnapshot.None;
                    context.Output.Write(operation.BindingIdentity, AIIntentValue.FromActionTarget(target));
                    return OperationExecutionResult.Success;
                case SimulationOperationCode.AISubmitActionRequest:
                    ulong generation = cursor.ReadGeneration(operation.Handle);
                    bool repeat = operation.Integer1 != 0;
                    if (context.State.TryMarkRequestEmission(operation.Handle, generation, repeat))
                    {
                        ulong duration = operation.Scalar0 <= 0d
                            ? 0UL
                            : (ulong)Math.Max(1, Math.Ceiling(operation.Scalar0 * context.InputContext.TickRate));
                        context.Output.SubmitRequest(new SimulationInputRequest(
                            operation.BindingIdentity,
                            context.State.NextRequestSequence(),
                            context.InputContext.Source.SourceTick,
                            checked(context.InputContext.SimulationTick.Value + duration),
                            operation.Integer0));
                    }
                    return OperationExecutionResult.Success;
                case SimulationOperationCode.AIWaitTicks:
                    int requiredTicks = Math.Max(0, context.ReadInput(operation, "Ticks").Integer);
                    if (requiredTicks == 0)
                        return OperationExecutionResult.Success;
                    int elapsedSlot = context.RequireInt32Slot(operation, ProgramStateSemantic.AIWaitElapsedTicks);
                    int elapsedTicks = context.State.ReadInt32(elapsedSlot) + 1;
                    context.State.WriteInt32(elapsedSlot, elapsedTicks);
                    return elapsedTicks >= requiredTicks
                        ? OperationExecutionResult.Success
                        : OperationExecutionResult.Running;
                case SimulationOperationCode.AIReadSelfObservation:
                case SimulationOperationCode.AIEnumerateConfiguredCandidates:
                case SimulationOperationCode.AIReadTargetDistance:
                case SimulationOperationCode.AIReadTargetDirection:
                case SimulationOperationCode.AIReadSelectedTargetSnapshot:
                case SimulationOperationCode.AIReadMemory:
                case SimulationOperationCode.Compare:
                case SimulationOperationCode.And:
                case SimulationOperationCode.Or:
                case SimulationOperationCode.Not:
                case SimulationOperationCode.Constant:
                    return AIIntentEvaluationContext.ToBoolean(context.EvaluateValue(operation, "Result"))
                        ? OperationExecutionResult.Success
                        : OperationExecutionResult.Failure;
                default:
                    throw new InvalidOperationException($"AI operation '{operation.Code}' reached an unsupported leaf path.");
            }
        }

        public void PrepareActivation(OperationExecutionDescriptor operation) { }
        public void ActivateScopes(OperationControlCursor<AIIntentOperationTarget> cursor, OperationExecutionDescriptor operation, ulong generation) { }
        public void CompleteScopes(OperationExecutionDescriptor operation) { }
        public void ClearStateScope(OperationExecutionDescriptor state) { }
        public void ResetOperationState(OperationExecutionDescriptor operation) => Context.State.ResetOperation(operation);
        public OperationStopStatus ContinueLeafStop(OperationControlCursor<AIIntentOperationTarget> cursor, OperationExecutionDescriptor operation, OperationStopContext context) => OperationStopStatus.Completed;
        public void ForceStopLeaf(OperationControlCursor<AIIntentOperationTarget> cursor, OperationExecutionDescriptor operation, OperationStopContext context) { }
        public void EmitTrace(OperationExecutionDescriptor operation, string code, OperationControlTraceSeverity severity, string detail) =>
            Context.SetActive(Context.Program.Operation(operation.Handle));
        public void NotifyStateLifecycle(OperationExecutionDescriptor machine, OperationHandle state, OperationStateLifecyclePhase phase) { }
    }
}
