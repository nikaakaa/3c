using System;
using System.Collections.Generic;
using System.Globalization;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class GameplayAbilitySemanticBuilder
    {
        sealed class GraphInvocationScope : IDisposable
        {
            Stack<string> m_Paths;
            internal GraphInvocationScope(Stack<string> paths, string path)
            {
                m_Paths = paths;
                paths.Push(path);
            }
            public void Dispose()
            {
                m_Paths?.Pop();
                m_Paths = null;
            }
        }

        readonly Stack<string> m_GraphInvocationPaths = new();
        readonly List<string> m_OperationInvocationPaths = new();

        public IDisposable PushGraphInvocation(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("技能图编译必须指定调用路径。", nameof(path));
            return new GraphInvocationScope(m_GraphInvocationPaths, path);
        }

        readonly ProgramId m_ProgramId;
        readonly string m_CompilerVersion;
        readonly OperationSetVersion m_OperationSetVersion;
        readonly int m_TickRate;
        readonly ProgramRevision m_SourceRevision;
        readonly GameplayAbilityRootDescriptor m_Root;
        readonly SimulationCompileReport m_Report;
        readonly List<SemanticOperation> m_Operations = new List<SemanticOperation>();
        readonly List<SemanticLiteral> m_Literals = new List<SemanticLiteral>();
        readonly List<SemanticConstantInputBinding> m_ConstantInputBindings = new List<SemanticConstantInputBinding>();
        readonly List<ProgramControlFlowEdge> m_ControlFlow = new List<ProgramControlFlowEdge>();
        readonly List<ProgramReference> m_References = new List<ProgramReference>();
        readonly List<ProgramGraphCallFrame> m_GraphCallFrames = new List<ProgramGraphCallFrame>();
        readonly List<ProgramStateSlot> m_StateSlots = new List<ProgramStateSlot>();
        readonly List<ProgramScopeLayout> m_Scopes = new List<ProgramScopeLayout>();
        readonly List<ProgramOutputChannelLayout> m_OutputChannels = new List<ProgramOutputChannelLayout>();
        readonly List<ProgramCatalogEntry> m_CatalogEntries = new List<ProgramCatalogEntry>();
        readonly List<ProgramSourceMapEntry> m_SourceMap = new List<ProgramSourceMapEntry>();
        readonly List<ProgramProducer> m_Producers = new List<ProgramProducer>();
        readonly Dictionary<string, int> m_ConstantByIdentity = new Dictionary<string, int>(StringComparer.Ordinal);
        readonly Dictionary<string, int> m_CatalogByIdentity = new Dictionary<string, int>(StringComparer.Ordinal);
        readonly Dictionary<string, int> m_ProducerByIdentity = new Dictionary<string, int>(StringComparer.Ordinal);
        readonly HashSet<string> m_ReferenceIdentities = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> m_ScopeIdentities = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> m_GameplayCapabilities = new HashSet<string>(StringComparer.Ordinal);
        readonly Dictionary<ProgramStateValueKind, int> m_DefaultConstants = new Dictionary<ProgramStateValueKind, int>();

        public GameplayAbilitySemanticBuilder(
            ProgramId programId,
            string compilerVersion,
            OperationSetVersion operationSetVersion,
            int tickRate,
            ProgramRevision sourceRevision,
            SimulationCompileReport report,
            GameplayAbilityRootDescriptor root)
        {
            m_ProgramId = programId;
            m_CompilerVersion = compilerVersion;
            m_OperationSetVersion = operationSetVersion;
            m_TickRate = tickRate;
            m_SourceRevision = sourceRevision;
            if (!root.IsValid || !root.IsAbility)
                throw new ArgumentException("Ability semantic Builder requires an Ability root descriptor.", nameof(root));
            m_Root = root;
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
            m_OutputChannels.Add(new ProgramOutputChannelLayout(0, "Gameplay", ProgramOutputChannelKind.GameplayFact));
            m_OutputChannels.Add(new ProgramOutputChannelLayout(1, "Presentation", ProgramOutputChannelKind.Presentation));
            m_OutputChannels.Add(new ProgramOutputChannelLayout(2, "Trace", ProgramOutputChannelKind.Trace));
        }

        public SimulationCompileReport Report => m_Report;
        public GameplayAbilityRootDescriptor Root => m_Root;

        public bool TryGetCatalogEntry(ProgramCatalogEntryKind kind, string identity, out int index)
        {
            return m_CatalogByIdentity.TryGetValue($"{kind}:{identity}", out index);
        }

        public OperationHandle DeclareOperation(
            SimulationSourceLocation source,
            SimulationOperationCode code,
            IReadOnlyList<int> constantReferences,
            int integer0 = 0,
            int integer1 = 0,
            ulong unsigned0 = 0,
            double number0 = 0d,
            string text0 = null,
            uint flags = 0)
        {
            var handle = new OperationHandle(m_Operations.Count);
            List<int> stateSlots = DeclareOperationStateSlots(handle, code, source);
            m_Operations.Add(new SemanticOperation(
                handle,
                source.TemplateIdentity,
                code,
                Array.Empty<int>(),
                constantReferences ?? Array.Empty<int>(),
                stateSlots,
                integer0,
                integer1,
                unsigned0,
                number0,
                $"{source.Identity}/number0",
                text0,
                flags));
            m_OperationInvocationPaths.Add(m_GraphInvocationPaths.Count == 0 ? string.Empty : m_GraphInvocationPaths.Peek());
            AddSourceMap(ProgramSourceTargetKind.Operation, handle.Value, source);
            return handle;
        }

        public void DeclareOperationPortSource(OperationHandle operation, SimulationSourceLocation source,
            string compiledPortId = "", ProgramValuePortDirection direction = ProgramValuePortDirection.None)
        {
            if (!operation.IsValid || operation.Value >= m_Operations.Count)
                throw new ArgumentOutOfRangeException(nameof(operation));
            if (string.IsNullOrEmpty(source.PortId))
                throw new ArgumentException("Operation port Source Map requires a port identity.", nameof(source));
            AddSourceMap(ProgramSourceTargetKind.OperationPort, operation.Value, source, compiledPortId, direction);
        }

        public void DeclareOptimizedSource(SimulationSourceLocation source)
        {
            if (string.IsNullOrEmpty(source.GraphId) || string.IsNullOrEmpty(source.NodeId) ||
                string.IsNullOrEmpty(source.ContentHash))
                throw new ArgumentException("Optimized source mapping requires graph, node and content identities.", nameof(source));
            AddSourceMap(ProgramSourceTargetKind.OptimizedAway, 0, source);
        }

        public void DeclareGraphInvocation(OperationHandle owner, SimulationSourceLocation source,
            ProgramInvocationCallerKind callerKind, string callerId, string callerClipId)
        {
            if (!owner.IsValid || owner.Value >= m_Operations.Count)
                throw new InvalidOperationException("图调用必须指定已有生命周期操作。");
            bool hasGeneration = false;
            foreach (int slot in m_Operations[owner.Value].StateSlots)
                hasGeneration |= m_StateSlots[slot].Semantic == ProgramStateSemantic.RunnableActivationGeneration;
            if (!hasGeneration)
                throw new InvalidOperationException("图调用的生命周期操作没有执行代次槽。");
            string parent = string.Empty;
            int depth = 0;
            foreach (string path in m_GraphInvocationPaths)
                if (depth++ == 1)
                {
                    parent = path;
                    break;
                }
            AddSourceMap(ProgramSourceTargetKind.GraphInvocation, owner.Value, source,
                parentInvocation: parent, callerKind: callerKind, callerId: callerId, callerClipId: callerClipId);
        }

        public int DeclareConstant(SimulationSourceLocation source, string fieldName, object value)
        {
            string identity = $"{source.ImmutableDataIdentity}/constant/{fieldName}";
            if (m_ConstantByIdentity.TryGetValue(identity, out int existing))
                return existing;
            int index = m_Literals.Count;
            SemanticLiteral literal;
            try
            {
                literal = CreateLiteral(index, identity, value);
            }
            catch (Exception exception)
            {
                m_Report.Error("numeric_or_constant_invalid", source.Identity, $"Field '{fieldName}' cannot be compiled: {exception.Message}");
                return -1;
            }
            m_Literals.Add(literal);
            m_ConstantByIdentity.Add(identity, index);
            AddSourceMap(ProgramSourceTargetKind.Constant, index, source);
            return index;
        }

        public void DeclareConstantInputBinding(
            OperationHandle targetOperation,
            string targetPort,
            int constantIndex,
            SemanticValueKind resolvedValueKind,
            SimulationSourceLocation source)
        {
            try
            {
                m_ConstantInputBindings.Add(new SemanticConstantInputBinding(
                    targetOperation,
                    targetPort,
                    constantIndex,
                    resolvedValueKind));
            }
            catch (Exception exception)
            {
                m_Report.Error("constant_input_binding_invalid", source.Identity, exception.Message);
            }
        }

        public int DeclareCatalogEntry(ProgramCatalogEntryKind kind, string identity, int revision, IEnumerable<ProgramCatalogField> fields, SimulationSourceLocation source)
        {
            string key = $"{kind}:{identity}";
            if (m_CatalogByIdentity.TryGetValue(key, out int existing))
                return existing;
            int index = m_CatalogEntries.Count;
            try
            {
                var validFields = fields == null
                    ? Array.Empty<ProgramCatalogField>()
                    : new List<ProgramCatalogField>(fields).FindAll(value => value != null).ToArray();
                m_CatalogEntries.Add(new ProgramCatalogEntry(index, kind, identity, revision, validFields));
                m_CatalogByIdentity.Add(key, index);
                AddSourceMap(ProgramSourceTargetKind.CatalogEntry, index, source);
                return index;
            }
            catch (Exception exception)
            {
                m_Report.Error("catalog_entry_invalid", source.Identity, exception.Message);
                return -1;
            }
        }

        internal int DeclareStateSlot(
            string identity,
            ProgramStateValueKind valueKind,
            ProgramStateOwnerKind ownerKind,
            ProgramStateSemantic semantic,
            string ownerIdentity,
            ProgramSourceTargetKind sourceTargetKind,
            SimulationSourceLocation source)
        {
            int index = m_StateSlots.Count;
            m_StateSlots.Add(new ProgramStateSlot(
                index,
                identity,
                valueKind,
                ownerKind,
                semantic,
                ownerIdentity,
                GetDefaultConstant(valueKind)));
            AddSourceMap(sourceTargetKind, index, source);
            return index;
        }

        internal void DeclareSourceMap(
            ProgramSourceTargetKind targetKind,
            int targetIndex,
            SimulationSourceLocation source)
        {
            AddSourceMap(targetKind, targetIndex, source);
        }

        public ProgramCatalogField ConstantField(SimulationSourceLocation source, string name, object value)
        {
            int constant = DeclareConstant(source, name, value);
            return constant >= 0 ? new ProgramCatalogField(name, ProgramCatalogFieldKind.Constant, constant, null) : null;
        }

        public ProgramCatalogField IdentityField(string name, string identity)
        {
            return string.IsNullOrEmpty(identity) ? null : new ProgramCatalogField(name, ProgramCatalogFieldKind.Identity, -1, identity);
        }

        public void DeclareControlFlow(
            string identity,
            OperationHandle source,
            OperationHandle target,
            string sourcePort,
            string targetPort,
            ProgramControlFlowKind kind,
            int order,
            int priority,
            ProgramAbortPolicy abortPolicy,
            bool hasCondition,
            OperationHandle condition,
            SimulationSourceLocation sourceLocation)
        {
            try
            {
                m_ControlFlow.Add(new ProgramControlFlowEdge(identity, source, target, sourcePort, targetPort, kind, order, priority, abortPolicy, hasCondition, condition));
                AddSourceMap(ProgramSourceTargetKind.Reference, m_ControlFlow.Count - 1, sourceLocation);
            }
            catch (Exception exception)
            {
                m_Report.Error("control_flow_invalid", sourceLocation.Identity, exception.Message);
            }
        }

        public void DeclareReference(string identity, OperationHandle sourceOperation, ProgramReferenceKind kind, int targetIndex, string externalIdentity, SimulationSourceLocation source)
        {
            if (!m_ReferenceIdentities.Add(identity))
            {
                m_Report.Error("reference_duplicate", source.Identity, $"Reference identity '{identity}' is duplicated.");
                return;
            }
            try
            {
                m_References.Add(new ProgramReference(identity, sourceOperation, kind, targetIndex, externalIdentity));
            }
            catch (Exception exception)
            {
                m_Report.Error("reference_invalid", source.Identity, exception.Message);
            }
        }

        public void DeclareGraphCallFrame(
            string identity,
            OperationHandle ownerOperation,
            OperationHandle entryOperation,
            string childGraphIdentity,
            IEnumerable<ProgramGraphParameterBinding> inputs,
            IEnumerable<ProgramGraphParameterBinding> outputs,
            SimulationSourceLocation source)
        {
            try
            {
                m_GraphCallFrames.Add(new ProgramGraphCallFrame(
                    m_GraphCallFrames.Count,
                    identity,
                    ownerOperation,
                    entryOperation,
                    childGraphIdentity,
                    inputs,
                    outputs));
            }
            catch (Exception exception)
            {
                m_Report.Error("graph_call_frame_invalid", source.Identity, exception.Message);
            }
        }

        public int DeclareProducer(
            string identity,
            AnimationChannelId animationChannelId,
            string sourceIdentity,
            ProgramOutputChannelKind channelKind,
            SimulationSourceLocation source)
        {
            if (m_ProducerByIdentity.TryGetValue(identity, out int existing))
            {
                ProgramProducer declared = m_Producers[existing];
                if (declared.AnimationChannelId != animationChannelId ||
                    !string.Equals(declared.SourceIdentity, sourceIdentity, StringComparison.Ordinal) ||
                    declared.ChannelKind != channelKind)
                {
                    m_Report.Error(
                        "producer_identity_conflict",
                        source.Identity,
                        $"Producer identity '{identity}' was already declared with different animation channel, source, or output metadata.");
                    return -1;
                }
                return existing;
            }
            int index = m_Producers.Count;
            try
            {
                m_Producers.Add(new ProgramProducer(index, identity, animationChannelId, sourceIdentity, channelKind));
                m_ProducerByIdentity.Add(identity, index);
                AddSourceMap(ProgramSourceTargetKind.Producer, index, source);
                return index;
            }
            catch (Exception exception)
            {
                m_Report.Error("producer_invalid", source.Identity, exception.Message);
                return -1;
            }
        }

        public void DeclareScope(
            string identity,
            ProgramScopeKind kind,
            string ownerIdentity,
            OperationHandle ownerOperation,
            IReadOnlyList<int> slots,
            SimulationSourceLocation source)
        {
            if (!m_ScopeIdentities.Add(identity))
            {
                m_Report.Error("scope_duplicate", source.Identity, $"Scope identity '{identity}' is duplicated.");
                return;
            }
            m_Scopes.Add(new ProgramScopeLayout(m_Scopes.Count, identity, kind, ownerIdentity, ownerOperation, slots));
        }

        public int DeclareStandaloneStateSlot(SimulationSourceLocation source, ProgramStateValueKind valueKind, ProgramStateOwnerKind ownerKind, ProgramStateSemantic semantic, string ownerIdentity)
        {
            return DeclareStandaloneStateSlot(source, valueKind, ownerKind, semantic, ownerIdentity, null, false);
        }

        public int DeclareStandaloneStateSlot(
            SimulationSourceLocation source,
            ProgramStateValueKind valueKind,
            ProgramStateOwnerKind ownerKind,
            ProgramStateSemantic semantic,
            string ownerIdentity,
            object defaultValue)
        {
            return DeclareStandaloneStateSlot(source, valueKind, ownerKind, semantic, ownerIdentity, defaultValue, true);
        }

        int DeclareStandaloneStateSlot(
            SimulationSourceLocation source,
            ProgramStateValueKind valueKind,
            ProgramStateOwnerKind ownerKind,
            ProgramStateSemantic semantic,
            string ownerIdentity,
            object defaultValue,
            bool hasDefaultValue)
        {
            int index = m_StateSlots.Count;
            string identity = $"{source.Identity}/state/{semantic}/{ownerIdentity}";
            int defaultConstant = hasDefaultValue
                ? DeclareStateDefaultConstant(source, valueKind, semantic, ownerIdentity, defaultValue)
                : GetDefaultConstant(valueKind);
            if (defaultConstant < 0)
                defaultConstant = GetDefaultConstant(valueKind);
            m_StateSlots.Add(new ProgramStateSlot(index, identity, valueKind, ownerKind, semantic, ownerIdentity, defaultConstant));
            AddSourceMap(ProgramSourceTargetKind.StateSlot, index, source);
            return index;
        }

        int DeclareStateDefaultConstant(
            SimulationSourceLocation source,
            ProgramStateValueKind valueKind,
            ProgramStateSemantic semantic,
            string ownerIdentity,
            object defaultValue)
        {
            if (valueKind != ProgramStateValueKind.Yaw)
                return DeclareConstant(source, $"default:{semantic}:{ownerIdentity}", defaultValue);
            float yaw = Convert.ToSingle(defaultValue, CultureInfo.InvariantCulture);
            string identity = $"{source.Identity}/constant/default:{semantic}:{ownerIdentity}";
            if (m_ConstantByIdentity.TryGetValue(identity, out int existing))
                return existing;
            int index = m_Literals.Count;
            m_Literals.Add(SemanticLiteral.FromYaw(index, identity, yaw));
            m_ConstantByIdentity.Add(identity, index);
            AddSourceMap(ProgramSourceTargetKind.Constant, index, source);
            return index;
        }

        public void RequireGameplayCapability(string capability)
        {
            if (!string.IsNullOrEmpty(capability))
                m_GameplayCapabilities.Add(capability);
        }

        public GameplayAbilitySemanticIr Build()
        {
            ValidateSingleChildControlFlow();
            try
            {
                GameplayAbilityRootValidation.RequireEntryReference(
                    m_Root,
                    m_References,
                    m_Operations);
            }
            catch (Exception exception)
            {
                m_Report.Error("ability_root_reference_invalid", m_ProgramId.Value, exception.Message);
            }
            if (!m_Report.IsValid)
                return null;
            try
            {
                var manifest = new GameplayAbilitySemanticIrManifest(
                    m_ProgramId,
                    m_CompilerVersion,
                    m_OperationSetVersion,
                    m_TickRate,
                    m_SourceRevision,
                    new GameplayAbilityCapabilityManifest(m_GameplayCapabilities),
                    m_Root);
                return new GameplayAbilitySemanticIr(
                    manifest,
                    m_Operations,
                    m_Literals,
                    m_ConstantInputBindings,
                    m_ControlFlow,
                    m_References,
                    m_StateSlots,
                    m_Scopes,
                    m_OutputChannels,
                    m_CatalogEntries,
                    m_SourceMap,
                    m_Producers,
                    m_GraphCallFrames);
            }
            catch (Exception exception)
            {
                m_Report.Error("ability_invalid", m_ProgramId.Value, exception.Message);
                return null;
            }
        }

        void ValidateSingleChildControlFlow()
        {
            var childCounts = new Dictionary<int, int>();
            for (int i = 0; i < m_ControlFlow.Count; i++)
            {
                ProgramControlFlowEdge edge = m_ControlFlow[i];
                if (edge.Kind != ProgramControlFlowKind.Child)
                    continue;
                childCounts.TryGetValue(edge.Source.Value, out int count);
                childCounts[edge.Source.Value] = count + 1;
            }

            for (int i = 0; i < m_Operations.Count; i++)
            {
                SemanticOperation operation = m_Operations[i];
                if (!RequiresSingleChild(operation.Code) ||
                    !childCounts.TryGetValue(operation.Handle.Value, out int count) ||
                    count <= 1)
                    continue;
                m_Report.Error(
                    "single_child_control_flow_invalid",
                    operation.TemplateIdentity,
                    $"Operation '{operation.Handle}' ({operation.Code}) has '{count}' child edges; at most one is allowed.");
            }
        }

        static bool RequiresSingleChild(SimulationOperationCode code)
        {
            return code == SimulationOperationCode.Root ||
                   code == SimulationOperationCode.SubGraph ||
                   code == SimulationOperationCode.StateOnEnter ||
                   code == SimulationOperationCode.StateOnExit ||
                   code == SimulationOperationCode.TimelineEnter;
        }

        List<int> DeclareOperationStateSlots(OperationHandle handle, SimulationOperationCode code, SimulationSourceLocation source)
        {
            var slots = new List<int>();
            if (IsRunnable(code))
            {
                slots.Add(DeclareStateSlot(source, handle, ProgramStateValueKind.Int32, ProgramStateOwnerKind.Runnable, ProgramStateSemantic.RunnableLifecycle));
                slots.Add(DeclareStateSlot(source, handle, ProgramStateValueKind.Int32, ProgramStateOwnerKind.Runnable, ProgramStateSemantic.RunnableChildCursor));
                slots.Add(DeclareStateSlot(source, handle, ProgramStateValueKind.Int32, ProgramStateOwnerKind.Runnable, ProgramStateSemantic.RunnableStopBarrier));
                slots.Add(DeclareStateSlot(source, handle, ProgramStateValueKind.UInt64, ProgramStateOwnerKind.Runnable, ProgramStateSemantic.RunnableActivationGeneration));
            }
            if (code == SimulationOperationCode.LocomotionInputMotion)
                slots.Add(DeclareStateSlot(source, handle, ProgramStateValueKind.Int32, ProgramStateOwnerKind.Runnable, ProgramStateSemantic.LocomotionMotionElapsedTicks));
            if (code == SimulationOperationCode.StateMachine)
            {
                slots.Add(DeclareStateSlot(source, handle, ProgramStateValueKind.Identity, ProgramStateOwnerKind.StateMachine, ProgramStateSemantic.StateMachineActive));
                slots.Add(DeclareStateSlot(source, handle, ProgramStateValueKind.Identity, ProgramStateOwnerKind.StateMachine, ProgramStateSemantic.StateMachinePending));
                slots.Add(DeclareStateSlot(source, handle, ProgramStateValueKind.Identity, ProgramStateOwnerKind.StateMachine, ProgramStateSemantic.StateMachineExiting));
                slots.Add(DeclareStateSlot(source, handle, ProgramStateValueKind.Identity, ProgramStateOwnerKind.StateMachine, ProgramStateSemantic.StateMachineTransition));
                slots.Add(DeclareStateSlot(source, handle, ProgramStateValueKind.Identity, ProgramStateOwnerKind.StateMachine, ProgramStateSemantic.StateMachineExecutionPath));
            }
            if (code == SimulationOperationCode.Timeline)
            {
                slots.Add(DeclareStateSlot(source, handle, ProgramStateValueKind.Int32, ProgramStateOwnerKind.Timeline, ProgramStateSemantic.TimelinePlayback));
            }
            if (code == SimulationOperationCode.TimelineTreeClip)
            {
                slots.Add(DeclareStateSlot(source, handle, ProgramStateValueKind.Int32, ProgramStateOwnerKind.Timeline, ProgramStateSemantic.TimelinePlayback));
                slots.Add(DeclareStateSlot(source, handle, ProgramStateValueKind.Int32, ProgramStateOwnerKind.Timeline, ProgramStateSemantic.TimelineTreeClipCycle));
                slots.Add(DeclareStateSlot(source, handle, ProgramStateValueKind.Scalar, ProgramStateOwnerKind.Timeline, ProgramStateSemantic.TimelineLogicTime));
            }
            return slots;
        }

        int DeclareStateSlot(SimulationSourceLocation source, OperationHandle handle, ProgramStateValueKind valueKind, ProgramStateOwnerKind ownerKind, ProgramStateSemantic semantic)
        {
            int index = m_StateSlots.Count;
            string identity = $"{source.Identity}/state/{semantic}";
            int defaultConstant = GetDefaultConstant(valueKind);
            m_StateSlots.Add(new ProgramStateSlot(index, identity, valueKind, ownerKind, semantic, $"operation:{handle.Value}", defaultConstant));
            AddSourceMap(ProgramSourceTargetKind.StateSlot, index, source);
            return index;
        }

        int GetDefaultConstant(ProgramStateValueKind kind)
        {
            if (kind == ProgramStateValueKind.BlackboardOwnerToken ||
                kind == ProgramStateValueKind.BlackboardWriteStamp)
            {
                return -1;
            }
            if (m_DefaultConstants.TryGetValue(kind, out int index))
                return index;
            var source = new SimulationSourceLocation("AbilityDefault", "Ability", string.Empty, string.Empty, string.Empty, string.Empty, $"Ability/default/{kind}");
            if (kind == ProgramStateValueKind.Yaw)
            {
                string identity = $"{source.Identity}/constant/value";
                index = m_Literals.Count;
                m_Literals.Add(SemanticLiteral.FromYaw(index, identity, 0d));
                m_ConstantByIdentity.Add(identity, index);
                AddSourceMap(ProgramSourceTargetKind.Constant, index, source);
                m_DefaultConstants.Add(kind, index);
                return index;
            }
            object value = kind switch
            {
                ProgramStateValueKind.Boolean => false,
                ProgramStateValueKind.Int32 => 0,
                ProgramStateValueKind.UInt64 => 0UL,
                ProgramStateValueKind.Scalar => 0f,
                ProgramStateValueKind.Vector2 => Vector2.zero,
                ProgramStateValueKind.Vector3 => Vector3.zero,
                ProgramStateValueKind.Yaw => throw new InvalidOperationException(),
                ProgramStateValueKind.Identity => string.Empty,
                ProgramStateValueKind.ActionTargetSnapshot => SemanticDataDocument.Empty,
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
            index = DeclareConstant(source, "value", value);
            m_DefaultConstants.Add(kind, index);
            return index;
        }

        static SemanticLiteral CreateLiteral(int index, string identity, object value)
        {
            switch (value)
            {
                case bool boolean: return SemanticLiteral.FromBoolean(index, identity, boolean);
                case int int32: return SemanticLiteral.FromInt32(index, identity, int32);
                case uint uint32: return SemanticLiteral.FromUInt64(index, identity, uint32);
                case ulong uint64: return SemanticLiteral.FromUInt64(index, identity, uint64);
                case float single: return SemanticLiteral.FromNumber(index, identity, single);
                case double number: return SemanticLiteral.FromNumber(index, identity, number);
                case string text: return SemanticLiteral.FromString(index, identity, text);
                case AnimationChannelId animationChannelId:
                    return SemanticLiteral.FromString(index, identity, animationChannelId.Value);
                case Vector2 vector2:
                    return SemanticLiteral.FromVector2(index, identity, vector2.x, vector2.y);
                case Vector3 vector3:
                    return SemanticLiteral.FromVector3(index, identity, vector3.x, vector3.y, vector3.z);
                case SemanticDataDocument document: return SemanticLiteral.FromDocument(index, identity, document);
                case Enum enumValue: return SemanticLiteral.FromInt32(index, identity, Convert.ToInt32(enumValue, CultureInfo.InvariantCulture));
                case null: return SemanticLiteral.FromString(index, identity, string.Empty);
                default: throw new InvalidOperationException($"Unsupported constant type '{value.GetType().FullName}'.");
            }
        }

        void AddSourceMap(ProgramSourceTargetKind targetKind, int targetIndex, SimulationSourceLocation source,
            string compiledPortId = "", ProgramValuePortDirection direction = ProgramValuePortDirection.None,
            string parentInvocation = "", ProgramInvocationCallerKind callerKind = ProgramInvocationCallerKind.None,
            string callerId = "", string callerClipId = "")
        {
            try
            {
                m_SourceMap.Add(new ProgramSourceMapEntry(
                    targetKind,
                    targetIndex,
                    string.IsNullOrEmpty(source.SourceType) ? "Unknown" : source.SourceType,
                    source.GraphId,
                    source.NodeId,
                    source.PortId,
                    source.EdgeId,
                    source.DeclarationId,
                    source.TimelineId,
                    source.TrackId,
                    source.ClipId,
                    source.DisplayPath,
                    source.ContentHash,
                    targetKind is ProgramSourceTargetKind.Operation or ProgramSourceTargetKind.OperationPort
                        ? m_OperationInvocationPaths[targetIndex]
                        : m_GraphInvocationPaths.Count == 0 ? string.Empty : m_GraphInvocationPaths.Peek(),
                    compiledPortId,
                    direction,
                    m_GraphInvocationPaths.Count == 0 ? string.Empty : m_GraphInvocationPaths.Peek(),
                    parentInvocation, callerKind, callerId, callerClipId));
            }
            catch (Exception exception)
            {
                m_Report.Error("source_map_invalid", source.Identity, exception.Message);
            }
        }

        static bool IsRunnable(SimulationOperationCode code)
        {
            return code == SimulationOperationCode.Root ||
                   code == SimulationOperationCode.Loop ||
                   code == SimulationOperationCode.Parallel ||
                   code == SimulationOperationCode.Sequence ||
                   code == SimulationOperationCode.Selector ||
                   code == SimulationOperationCode.Succeed ||
                   code == SimulationOperationCode.StateMachine ||
                   code == SimulationOperationCode.State ||
                   code == SimulationOperationCode.StateOnEnter ||
                   code == SimulationOperationCode.StateOnExit ||
                   code == SimulationOperationCode.TimelineEnter ||
                   code == SimulationOperationCode.Timeline ||
                   code == SimulationOperationCode.BlackboardSet ||
                   code == SimulationOperationCode.ActivateActionInstance ||
                   code == SimulationOperationCode.SubmitActionLifecycle ||
                   code == SimulationOperationCode.LocomotionInputMotion ||
                   code == SimulationOperationCode.CameraStateRequest ||
                   code == SimulationOperationCode.CameraCue ||
                   code == SimulationOperationCode.CameraResponse ||
                   code == SimulationOperationCode.CameraTarget ||
                   code == SimulationOperationCode.RequestEquipmentChange ||
                   code == SimulationOperationCode.BeginEquipmentChange ||
                   code == SimulationOperationCode.CommitEquipmentChange ||
                   code == SimulationOperationCode.CancelEquipmentChange;
        }
    }
}
