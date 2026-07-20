using ThirdPersonSimulation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ThirdPersonSimulation.Fixed
{
    public readonly struct TypedStateAddress : IEquatable<TypedStateAddress>
    {
        readonly bool m_IsValid;

        internal TypedStateAddress(
            int slotIndex,
            ProgramStateValueKind valueKind,
            int partitionIndex,
            int pageIndex,
            int offset)
        {
            SlotIndex = slotIndex;
            ValueKind = valueKind;
            PartitionIndex = partitionIndex;
            PageIndex = pageIndex;
            Offset = offset;
            m_IsValid = true;
        }

        public int SlotIndex { get; }
        public ProgramStateValueKind ValueKind { get; }
        public int PartitionIndex { get; }
        public int PageIndex { get; }
        public int Offset { get; }
        public bool IsValid => m_IsValid;
        public bool Equals(TypedStateAddress other) =>
            m_IsValid == other.m_IsValid &&
            SlotIndex == other.SlotIndex &&
            ValueKind == other.ValueKind &&
            PartitionIndex == other.PartitionIndex &&
            PageIndex == other.PageIndex &&
            Offset == other.Offset;
        public override bool Equals(object obj) => obj is TypedStateAddress other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(m_IsValid, SlotIndex, (int)ValueKind, PartitionIndex, PageIndex, Offset);
    }

    public sealed class TypedStatePartitionDescriptor
    {
        readonly IReadOnlyList<int> m_SlotIndexes;

        internal TypedStatePartitionDescriptor(int index, ProgramStateValueKind valueKind, IReadOnlyList<int> slotIndexes)
        {
            Index = index;
            ValueKind = valueKind;
            m_SlotIndexes = slotIndexes ?? throw new ArgumentNullException(nameof(slotIndexes));
        }

        public int Index { get; }
        public ProgramStateValueKind ValueKind { get; }
        public int SlotCount => m_SlotIndexes.Count;
        public int PageCount => (SlotCount + CharacterSimulationState.PageSize - 1) / CharacterSimulationState.PageSize;
        public IReadOnlyList<int> SlotIndexes => m_SlotIndexes;
    }

    public readonly struct TypedActionStateAddresses
    {
        public TypedActionStateAddresses(string actionId, TypedStateAddress request, TypedStateAddress instance, TypedStateAddress eventSequence)
        {
            ActionId = SimulationIdentity.Require(actionId, nameof(actionId));
            if (!request.IsValid || !instance.IsValid || !eventSequence.IsValid)
                throw new ArgumentException("Action typed state addresses are incomplete.");
            Request = request;
            Instance = instance;
            EventSequence = eventSequence;
        }

        public string ActionId { get; }
        public TypedStateAddress Request { get; }
        public TypedStateAddress Instance { get; }
        public TypedStateAddress EventSequence { get; }
    }

    public readonly struct InputDerivedStateBinding
    {
        public InputDerivedStateBinding(string inputId, ProgramInputValueKind inputKind, TypedStateAddress stateAddress)
        {
            InputId = SimulationIdentity.Require(inputId, nameof(inputId));
            InputKind = inputKind;
            StateAddress = stateAddress.IsValid ? stateAddress : throw new ArgumentException("InputDerived state address is invalid.", nameof(stateAddress));
        }

        public string InputId { get; }
        public ProgramInputValueKind InputKind { get; }
        public TypedStateAddress StateAddress { get; }
    }

    public sealed class ProgramExecutionLayout
    {
        static readonly ConditionalWeakTable<CharacterSimulationProgram, ProgramExecutionLayout> s_Layouts =
            new ConditionalWeakTable<CharacterSimulationProgram, ProgramExecutionLayout>();

        readonly CharacterSimulationProgram m_Program;
        readonly OperationValueInputRange[] m_ValueInputRanges;
        readonly CompiledValueInputBinding[] m_ValueInputs;
        readonly int[] m_NamedConstantIndexes;
        readonly int[] m_FirstStateSlots;
        readonly Dictionary<string, int>[] m_StateSlotsByOwner;
        readonly TypedStateAddress[] m_TypedAddresses;
        readonly IReadOnlyList<TypedStatePartitionDescriptor> m_Partitions;
        readonly IReadOnlyDictionary<string, TypedStateAddress> m_InputRequests;
        readonly IReadOnlyDictionary<string, TypedActionStateAddresses> m_Actions;
        readonly IReadOnlyDictionary<string, IReadOnlyList<TypedStateAddress>> m_ActionContexts;
        readonly IReadOnlyDictionary<int, TypedStateAddress> m_TimelineRetention;
        readonly IReadOnlyList<InputDerivedStateBinding> m_InputDerivedBindings;
        readonly TypedStateAddress[] m_ActionTargetSnapshotByOperation;
        readonly TypedStateAddress m_GameplayEffectAggregate;
        readonly ProgramCatalogRuntimeIndex m_CatalogIndex;
        readonly TimelineAnimationProducerIndex m_TimelineAnimationProducers;
        readonly ProgramMotionModifierDescriptor[] m_MotionModifiers;
        readonly OperationValueInputRange[] m_MotionModifierRanges;

        ProgramExecutionLayout(CharacterSimulationProgram program)
        {
            m_Program = program ?? throw new ArgumentNullException(nameof(program));
            ProgramId = program.Manifest.ProgramId;
            ProgramHash = program.ProgramHash;
            LayoutHash = program.LayoutHash;

            int operationCount = program.Operations.Count;
            int stateSemanticCount = EnumValueCount(typeof(ProgramStateSemantic));

            m_CatalogIndex = new ProgramCatalogRuntimeIndex(operationCount, program.References, program.CatalogEntries);
            BuildMotionModifierRanges(program, out m_MotionModifiers, out m_MotionModifierRanges);
            string[] operationSourcePaths = BuildOperationSourcePaths(program);
            BuildValueInputs(program, out m_ValueInputRanges, out m_ValueInputs);
            m_NamedConstantIndexes = BuildNamedConstantIndexes(program);
            BuildGlobalStateSlots(program, stateSemanticCount, out m_FirstStateSlots, out m_StateSlotsByOwner);
            BuildTypedStateLayout(program, out m_TypedAddresses, out m_Partitions);
            m_InputDerivedBindings = BuildInputDerivedBindings(program, m_CatalogIndex, m_TypedAddresses);
            BuildDomainIndexes(
                program,
                m_TypedAddresses,
                out m_InputRequests,
                out m_Actions,
                out m_ActionContexts,
                out m_TimelineRetention,
                out IReadOnlyDictionary<string, TypedStateAddress> actionTargetSnapshots,
                out m_GameplayEffectAggregate);
            m_ActionTargetSnapshotByOperation = BuildActionTargetSnapshotIndex(program, actionTargetSnapshots);
            RootOperation = ResolveRootOperation(program);
            var topology = new OperationExecutionTopology(
                BuildControlDescriptors(program),
                program.ControlFlow,
                program.References,
                program.StateSlots,
                program.SourceMap,
                RootOperation);
            m_TimelineAnimationProducers = new TimelineAnimationProducerIndex(
                topology,
                operation => IsTimelineAnimationTrackMuted(program, m_CatalogIndex, operation),
                operation => RequireTimelineAnimationProducerIdentity(program, topology, operation));
            Services = new FixedProgramExecutionServices(
                program,
                this,
                topology,
                operationSourcePaths);
        }

        public ProgramId ProgramId { get; }
        public ProgramHash ProgramHash { get; }
        public LayoutHash LayoutHash { get; }
        public OperationHandle RootOperation { get; }
        public OperationExecutionTopology Topology => Services.Topology;
        internal CharacterSimulationProgram Program => m_Program;
        internal SimulationGameplayEffectProgram GameplayEffectProgram => Services.GameplayEffectProgram;
        internal FixedProgramExecutionServices Services { get; }
        public IReadOnlyList<TypedStatePartitionDescriptor> StatePartitions => m_Partitions;
        public TypedStateAddress GameplayEffectAggregateAddress => m_GameplayEffectAggregate;
        public IReadOnlyList<InputDerivedStateBinding> InputDerivedBindings => m_InputDerivedBindings;

        public static ProgramExecutionLayout GetOrCreate(CharacterSimulationProgram program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            return s_Layouts.GetValue(program, value => new ProgramExecutionLayout(value));
        }

        public void RequireProgram(CharacterSimulationProgram program)
        {
            if (!ReferenceEquals(m_Program, program))
                throw new InvalidOperationException($"Execution layout '{ProgramHash}' is not bound to the supplied Program instance.");
        }

        public IReadOnlyList<ProgramControlFlowEdge> Outgoing(OperationHandle source, ProgramControlFlowKind kind)
        {
            return Topology.Outgoing(source, kind);
        }

        internal IReadOnlyList<OperationHandle> TimelineAnimationRepresentatives(OperationHandle timeline) =>
            m_TimelineAnimationProducers.Representatives(timeline);

        static bool IsTimelineAnimationTrackMuted(
            CharacterSimulationProgram program,
            ProgramCatalogRuntimeIndex catalog,
            OperationHandle operation)
        {
            ProgramCatalogEntry clip = catalog.RequireEntry(operation, ProgramCatalogEntryKind.TimelineClip);
            if (!catalog.TryGetIdentity(clip, ProgramCatalogFieldId.Track, out string trackIdentity))
                throw new InvalidOperationException($"Timeline clip '{clip.Identity}' has no Track identity.");
            ProgramCatalogEntry track = catalog.FindEntry(ProgramCatalogEntryKind.TimelineTrack, trackIdentity) ??
                throw new InvalidOperationException($"Timeline track '{trackIdentity}' is absent from the Program catalog.");
            ProgramCatalogField muted = catalog.RequireField(track, ProgramCatalogFieldId.Muted);
            if (muted.Kind != ProgramCatalogFieldKind.Constant ||
                muted.ConstantIndex < 0 || muted.ConstantIndex >= program.Constants.Count)
            {
                throw new InvalidOperationException($"Timeline track '{track.Identity}' Muted field is invalid.");
            }
            ProgramConstant constant = program.Constants[muted.ConstantIndex];
            if (constant.Kind != ProgramConstantKind.Boolean)
                throw new InvalidOperationException($"Timeline track '{track.Identity}' Muted field is not Boolean.");
            return constant.Boolean;
        }

        static string RequireTimelineAnimationProducerIdentity(
            CharacterSimulationProgram program,
            OperationExecutionTopology topology,
            OperationHandle operation)
        {
            IReadOnlyList<ProgramReference> references = topology.References(operation, ProgramReferenceKind.Producer);
            if (references.Count != 1)
                throw new InvalidOperationException($"Timeline animation operation '{operation}' requires exactly one Producer reference.");
            int producerIndex = references[0].TargetIndex;
            if (producerIndex < 0 || producerIndex >= program.Producers.Count)
                throw new InvalidOperationException($"Timeline animation operation '{operation}' Producer reference is outside the Program catalog.");
            return program.Producers[producerIndex].Identity;
        }

        public ReadOnlySpan<CompiledValueInputBinding> ValueInputs(OperationHandle target)
        {
            RequireOperation(target);
            OperationValueInputRange range = m_ValueInputRanges[target.Value];
            return new ReadOnlySpan<CompiledValueInputBinding>(m_ValueInputs, range.Offset, range.Count);
        }

        internal ReadOnlySpan<ProgramMotionModifierDescriptor> MotionModifiers(ProgramMotionModifierChannel channel)
        {
            int index = (int)channel;
            if (index < 0 || index >= m_MotionModifierRanges.Length)
                throw new ArgumentOutOfRangeException(nameof(channel));
            OperationValueInputRange range = m_MotionModifierRanges[index];
            return new ReadOnlySpan<ProgramMotionModifierDescriptor>(m_MotionModifiers, range.Offset, range.Count);
        }

        internal ProgramConstant FindNamedConstant(OperationHandle operation, OperationNamedConstant field)
        {
            RequireOperation(operation);
            int fieldIndex = (int)field;
            if (fieldIndex < 0 || fieldIndex >= OperationNamedConstantSchema.Count)
                throw new ArgumentOutOfRangeException(nameof(field));
            int constantIndex = m_NamedConstantIndexes[operation.Value * OperationNamedConstantSchema.Count + fieldIndex];
            return constantIndex < 0 ? null : m_Program.Constants[constantIndex];
        }

        internal ProgramCatalogEntry RequireCatalog(OperationHandle operation, ProgramCatalogEntryKind kind) =>
            m_CatalogIndex.RequireEntry(operation, kind);

        internal ProgramCatalogEntry FindCatalog(OperationHandle operation, ProgramCatalogEntryKind kind) =>
            m_CatalogIndex.FindEntry(operation, kind);

        internal ProgramCatalogEntry FindCatalog(ProgramCatalogEntryKind kind, string identity) =>
            m_CatalogIndex.FindEntry(kind, identity);

        internal ProgramCatalogField RequireCatalogField(ProgramCatalogEntry entry, ProgramCatalogFieldId field) =>
            m_CatalogIndex.RequireField(entry, field);

        internal bool TryGetCatalogIdentity(ProgramCatalogEntry entry, ProgramCatalogFieldId field, out string identity) =>
            m_CatalogIndex.TryGetIdentity(entry, field, out identity);

        public string ValueSourceOutputPort(CompiledValueInputBinding binding)
        {
            if (binding.SourceKind != CompiledValueInputSourceKind.Operation)
                throw new InvalidOperationException("Constant Value input has no source output port.");
            RequireOperation(binding.SourceOperation);
            IReadOnlyList<OperationValuePortDefinition> outputs = CharacterGameplayValuePortContracts.Require(
                m_Program.Operations[binding.SourceOperation.Value].Code).Outputs;
            if (binding.SourceOutputPortIndex < 0 || binding.SourceOutputPortIndex >= outputs.Count)
                throw new InvalidOperationException("Compiled Value source output port index is invalid.");
            return outputs[binding.SourceOutputPortIndex].Identity;
        }

        public IReadOnlyList<ProgramReference> References(OperationHandle source, ProgramReferenceKind kind)
        {
            RequireOperation(source);
            return Topology.References(source, kind);
        }

        public string SourcePath(OperationHandle operation)
        {
            return Services.SourcePath(operation);
        }

        public int FindOperationStateSlot(OperationHandle operation, ProgramStateSemantic semantic)
        {
            return Topology.FindOperationStateSlot(operation, semantic);
        }

        public int FindStateSlot(ProgramStateSemantic semantic, string ownerIdentity)
        {
            int semanticIndex = (int)semantic;
            if (semanticIndex >= m_FirstStateSlots.Length)
                return -1;
            if (ownerIdentity == null)
                return m_FirstStateSlots[semanticIndex];
            Dictionary<string, int> owners = m_StateSlotsByOwner[semanticIndex];
            return owners != null && owners.TryGetValue(ownerIdentity, out int slot) ? slot : -1;
        }

        public int RequireStateSlot(ProgramStateSemantic semantic, string ownerIdentity = null)
        {
            int slot = FindStateSlot(semantic, ownerIdentity);
            if (slot < 0)
                throw new InvalidOperationException($"Program has no '{semantic}' state slot for owner '{ownerIdentity ?? "*"}'.");
            return slot;
        }

        public TypedStateAddress Address(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= m_TypedAddresses.Length)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            return m_TypedAddresses[slotIndex];
        }

        public TypedStateAddress RequireInputRequest(string requestId)
        {
            string identity = SimulationIdentity.Require(requestId, nameof(requestId));
            if (!m_InputRequests.TryGetValue(identity, out TypedStateAddress address))
                throw new InvalidOperationException($"Program has no Input request '{identity}'.");
            return address;
        }

        public bool TryGetInputRequest(string requestId, out TypedStateAddress address)
        {
            return m_InputRequests.TryGetValue(requestId ?? string.Empty, out address);
        }

        public TypedActionStateAddresses RequireAction(string actionId)
        {
            string identity = SimulationIdentity.Require(actionId, nameof(actionId));
            if (!m_Actions.TryGetValue(identity, out TypedActionStateAddresses addresses))
                throw new InvalidOperationException($"Program has no Action '{identity}' typed state.");
            return addresses;
        }

        public IReadOnlyList<TypedStateAddress> ActionInstances(string contextId)
        {
            return m_ActionContexts.TryGetValue(contextId ?? string.Empty, out IReadOnlyList<TypedStateAddress> values)
                ? values
                : Array.Empty<TypedStateAddress>();
        }

        internal IReadOnlyDictionary<string, TypedStateAddress> InputRequestIndex => m_InputRequests;
        internal IReadOnlyDictionary<string, TypedActionStateAddresses> ActionStateIndex => m_Actions;

        public TypedStateAddress RequireTimelineRetention(OperationHandle timeline)
        {
            if (!timeline.IsValid || !m_TimelineRetention.TryGetValue(timeline.Value, out TypedStateAddress address))
                throw new InvalidOperationException($"Timeline '{timeline}' has no retained Action reference.");
            return address;
        }

        public bool TryGetActionTargetSnapshot(OperationHandle operation, out TypedStateAddress address)
        {
            RequireOperation(operation);
            address = m_ActionTargetSnapshotByOperation[operation.Value];
            return address.IsValid;
        }

        static TypedStateAddress[] BuildActionTargetSnapshotIndex(
            CharacterSimulationProgram program,
            IReadOnlyDictionary<string, TypedStateAddress> snapshots)
        {
            var result = new TypedStateAddress[program.Operations.Count];
            for (int i = 0; i < program.Operations.Count; i++)
            {
                SimulationOperation operation = program.Operations[i];
                string declarationId = FindStringConstant(program, operation, "TargetSnapshotDeclaration");
                if (string.IsNullOrEmpty(declarationId))
                    continue;
                string ownerId = FindStringConstant(program, operation, "TargetSnapshotOwner");
                string identity = $"blackboard:{ownerId}:{declarationId}";
                if (!snapshots.TryGetValue(identity, out TypedStateAddress address))
                    throw new InvalidDataException($"Action operation '{operation.Handle}' has no target snapshot state address '{identity}'.");
                result[i] = address;
            }
            return result;
        }

        static void BuildTypedStateLayout(
            CharacterSimulationProgram program,
            out TypedStateAddress[] addresses,
            out IReadOnlyList<TypedStatePartitionDescriptor> partitions)
        {
            var slotsByKind = new SortedDictionary<ProgramStateValueKind, List<int>>();
            for (int i = 0; i < program.StateSlots.Count; i++)
            {
                ProgramStateSlot slot = program.StateSlots[i];
                ProgramStateSchema.RequireSlot(slot.ValueKind, slot.OwnerKind, slot.Semantic);
                ValidateDefault(program, slot);
                if (!slotsByKind.TryGetValue(slot.ValueKind, out List<int> slots))
                {
                    slots = new List<int>();
                    slotsByKind.Add(slot.ValueKind, slots);
                }
                slots.Add(i);
            }

            addresses = new TypedStateAddress[program.StateSlots.Count];
            var descriptors = new List<TypedStatePartitionDescriptor>(slotsByKind.Count);
            foreach (KeyValuePair<ProgramStateValueKind, List<int>> pair in slotsByKind)
            {
                int partitionIndex = descriptors.Count;
                int[] slots = pair.Value.ToArray();
                descriptors.Add(new TypedStatePartitionDescriptor(partitionIndex, pair.Key, Array.AsReadOnly(slots)));
                for (int offset = 0; offset < slots.Length; offset++)
                {
                    addresses[slots[offset]] = new TypedStateAddress(
                        slots[offset],
                        pair.Key,
                        partitionIndex,
                        offset / CharacterSimulationState.PageSize,
                        offset % CharacterSimulationState.PageSize);
                }
            }
            partitions = descriptors.AsReadOnly();
        }

        static void ValidateDefault(CharacterSimulationProgram program, ProgramStateSlot slot)
        {
            if (slot.DefaultConstantIndex < 0)
            {
                if (slot.ValueKind == ProgramStateValueKind.ActionTargetSnapshot)
                    throw new InvalidDataException($"Action target snapshot slot '{slot.Identity}' requires a typed default constant.");
                return;
            }
            ProgramConstant constant = program.Constants[slot.DefaultConstantIndex];
            bool valid = slot.ValueKind switch
            {
                ProgramStateValueKind.Boolean => constant.Kind == ProgramConstantKind.Boolean,
                ProgramStateValueKind.Int32 => constant.Kind == ProgramConstantKind.Int32,
                ProgramStateValueKind.UInt64 => constant.Kind == ProgramConstantKind.UInt64,
                ProgramStateValueKind.Scalar => constant.Kind == ProgramConstantKind.Scalar,
                ProgramStateValueKind.Vector2 => constant.Kind == ProgramConstantKind.Vector2,
                ProgramStateValueKind.Vector3 => constant.Kind == ProgramConstantKind.Vector3,
                ProgramStateValueKind.Yaw => constant.Kind == ProgramConstantKind.Yaw,
                ProgramStateValueKind.Identity => constant.Kind == ProgramConstantKind.String,
                ProgramStateValueKind.ActionTargetSnapshot => constant.Kind == ProgramConstantKind.Bytes,
                _ => false
            };
            if (!valid)
            {
                throw new InvalidDataException(
                    $"State slot '{slot.Identity}' kind '{slot.ValueKind}' has invalid default constant kind '{constant.Kind}'.");
            }
        }

        static IReadOnlyList<InputDerivedStateBinding> BuildInputDerivedBindings(
            CharacterSimulationProgram program,
            ProgramCatalogRuntimeIndex catalog,
            TypedStateAddress[] addresses)
        {
            var stateByOwner = new Dictionary<string, TypedStateAddress>(StringComparer.Ordinal);
            for (int i = 0; i < program.StateSlots.Count; i++)
            {
                ProgramStateSlot slot = program.StateSlots[i];
                if (slot.Semantic == ProgramStateSemantic.BlackboardValue)
                    stateByOwner.Add(slot.OwnerIdentity, addresses[i]);
            }
            var bindings = new List<InputDerivedStateBinding>();
            for (int i = 0; i < program.CatalogEntries.Count; i++)
            {
                ProgramCatalogEntry declaration = program.CatalogEntries[i];
                if (declaration.Kind != ProgramCatalogEntryKind.BlackboardDeclaration ||
                    ReadInt32(program, catalog, declaration, ProgramCatalogFieldId.SyncPolicy) != 2)
                    continue;
                string inputId = ReadString(program, catalog, declaration, ProgramCatalogFieldId.InputValueId);
                ProgramCatalogEntry input = catalog.FindEntry(ProgramCatalogEntryKind.InputValue, $"input:value:{inputId}") ??
                    throw new InvalidDataException($"InputDerived Blackboard '{declaration.Identity}' references absent input '{inputId}'.");
                var kind = (ProgramInputValueKind)ReadInt32(program, catalog, input, ProgramCatalogFieldId.ValueType);
                if (!Enum.IsDefined(typeof(ProgramInputValueKind), kind))
                    throw new InvalidDataException($"Input '{input.Identity}' has unknown value kind '{(int)kind}'.");
                if (!stateByOwner.TryGetValue(declaration.Identity, out TypedStateAddress address))
                    throw new InvalidDataException($"InputDerived Blackboard '{declaration.Identity}' has no state slot.");
                if (address.ValueKind != StateKind(kind))
                    throw new InvalidDataException($"InputDerived Blackboard '{declaration.Identity}' state kind '{address.ValueKind}' does not match input kind '{kind}'.");
                bindings.Add(new InputDerivedStateBinding(inputId, kind, address));
            }
            bindings.Sort((left, right) => string.CompareOrdinal(left.InputId, right.InputId));
            return bindings.AsReadOnly();
        }

        static int ReadInt32(CharacterSimulationProgram program, ProgramCatalogRuntimeIndex catalog, ProgramCatalogEntry entry, ProgramCatalogFieldId field)
        {
            ProgramCatalogField value = catalog.RequireField(entry, field);
            if (value.Kind != ProgramCatalogFieldKind.Constant || program.Constants[value.ConstantIndex].Kind != ProgramConstantKind.Int32)
                throw new InvalidDataException($"Catalog '{entry.Identity}' field '{field}' is not Int32.");
            return program.Constants[value.ConstantIndex].Int32;
        }

        static string ReadString(CharacterSimulationProgram program, ProgramCatalogRuntimeIndex catalog, ProgramCatalogEntry entry, ProgramCatalogFieldId field)
        {
            ProgramCatalogField value = catalog.RequireField(entry, field);
            if (value.Kind != ProgramCatalogFieldKind.Constant || program.Constants[value.ConstantIndex].Kind != ProgramConstantKind.String)
                throw new InvalidDataException($"Catalog '{entry.Identity}' field '{field}' is not String.");
            return SimulationIdentity.Require(program.Constants[value.ConstantIndex].Text, field.ToString());
        }

        static ProgramStateValueKind StateKind(ProgramInputValueKind kind)
        {
            return kind switch
            {
                ProgramInputValueKind.Boolean => ProgramStateValueKind.Boolean,
                ProgramInputValueKind.Scalar => ProgramStateValueKind.Scalar,
                ProgramInputValueKind.Vector2 => ProgramStateValueKind.Vector2,
                ProgramInputValueKind.Vector3 => ProgramStateValueKind.Vector3,
                ProgramInputValueKind.Yaw => ProgramStateValueKind.Yaw,
                ProgramInputValueKind.ActionTargetSnapshot => ProgramStateValueKind.ActionTargetSnapshot,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }

        static void BuildDomainIndexes(
            CharacterSimulationProgram program,
            TypedStateAddress[] addresses,
            out IReadOnlyDictionary<string, TypedStateAddress> inputRequests,
            out IReadOnlyDictionary<string, TypedActionStateAddresses> actions,
            out IReadOnlyDictionary<string, IReadOnlyList<TypedStateAddress>> actionContexts,
            out IReadOnlyDictionary<int, TypedStateAddress> timelineRetention,
            out IReadOnlyDictionary<string, TypedStateAddress> actionTargetSnapshots,
            out TypedStateAddress gameplayEffectAggregate)
        {
            var inputs = new Dictionary<string, TypedStateAddress>(StringComparer.Ordinal);
            var actionBuilders = new Dictionary<string, ActionAddressBuilder>(StringComparer.Ordinal);
            var timeline = new Dictionary<int, TypedStateAddress>();
            var targets = new Dictionary<string, TypedStateAddress>(StringComparer.Ordinal);
            gameplayEffectAggregate = default;
            TypedStateAddress eventSequence = default;

            for (int i = 0; i < program.StateSlots.Count; i++)
            {
                ProgramStateSlot slot = program.StateSlots[i];
                TypedStateAddress address = addresses[i];
                switch (slot.Semantic)
                {
                    case ProgramStateSemantic.InputRequestBuffer:
                        AddUnique(inputs, TrimPrefix(slot.OwnerIdentity, "input:request:"), address, "Input request");
                        break;
                    case ProgramStateSemantic.ActionRequestBuffer:
                        RequireActionBuilder(actionBuilders, slot.OwnerIdentity).Request = address;
                        break;
                    case ProgramStateSemantic.ActionInstance:
                        RequireActionBuilder(actionBuilders, slot.OwnerIdentity).Instance = address;
                        break;
                    case ProgramStateSemantic.ActionEventSequence:
                        if (eventSequence.IsValid)
                            throw new InvalidDataException("Program contains duplicate Action event sequence state.");
                        eventSequence = address;
                        break;
                    case ProgramStateSemantic.TimelineRetentionIdentity:
                        int operation = ParseOperationOwner(slot.OwnerIdentity);
                        AddUnique(timeline, operation, address, "Timeline retention");
                        break;
                    case ProgramStateSemantic.BlackboardValue when slot.ValueKind == ProgramStateValueKind.ActionTargetSnapshot:
                        AddUnique(targets, slot.OwnerIdentity, address, "Action target snapshot");
                        break;
                    case ProgramStateSemantic.GameplayEffectAggregate:
                        if (gameplayEffectAggregate.IsValid)
                            throw new InvalidDataException("Program contains duplicate Gameplay Effect aggregate state.");
                        gameplayEffectAggregate = address;
                        break;
                }
            }

            if (!eventSequence.IsValid)
                throw new InvalidDataException("Program has no Action event sequence state.");
            if (!gameplayEffectAggregate.IsValid)
                throw new InvalidDataException("Program has no Gameplay Effect aggregate state.");

            var actionValues = new Dictionary<string, TypedActionStateAddresses>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, ActionAddressBuilder> pair in actionBuilders.OrderBy(value => value.Key, StringComparer.Ordinal))
            {
                if (!pair.Value.Request.IsValid || !pair.Value.Instance.IsValid)
                    throw new InvalidDataException($"Action '{pair.Key}' typed state is incomplete.");
                actionValues.Add(pair.Key, new TypedActionStateAddresses(pair.Key, pair.Value.Request, pair.Value.Instance, eventSequence));
            }

            var contexts = new Dictionary<string, List<TypedStateAddress>>(StringComparer.Ordinal);
            for (int i = 0; i < program.Operations.Count; i++)
            {
                SimulationOperation operation = program.Operations[i];
                if (operation.Code != SimulationOperationCode.ActivateActionInstance)
                    continue;
                string contextId = FindStringConstant(program, operation, "ActionContext");
                string actionId = FindActionId(program, operation);
                if (string.IsNullOrEmpty(contextId) || !actionValues.TryGetValue(actionId, out TypedActionStateAddresses action))
                    throw new InvalidDataException($"Action activation operation '{operation.Handle}' has incomplete typed state binding.");
                if (!contexts.TryGetValue(contextId, out List<TypedStateAddress> values))
                {
                    values = new List<TypedStateAddress>();
                    contexts.Add(contextId, values);
                }
                if (!values.Contains(action.Instance))
                    values.Add(action.Instance);
            }

            var frozenContexts = new Dictionary<string, IReadOnlyList<TypedStateAddress>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, List<TypedStateAddress>> pair in contexts)
            {
                pair.Value.Sort((left, right) => left.SlotIndex.CompareTo(right.SlotIndex));
                frozenContexts.Add(pair.Key, Array.AsReadOnly(pair.Value.ToArray()));
            }

            inputRequests = inputs;
            actions = actionValues;
            actionContexts = frozenContexts;
            timelineRetention = timeline;
            actionTargetSnapshots = targets;
        }

        static ActionAddressBuilder RequireActionBuilder(
            IDictionary<string, ActionAddressBuilder> builders,
            string ownerIdentity)
        {
            string actionId = TrimPrefix(ownerIdentity, "action:");
            if (!builders.TryGetValue(actionId, out ActionAddressBuilder builder))
            {
                builder = new ActionAddressBuilder();
                builders.Add(actionId, builder);
            }
            return builder;
        }

        static string FindActionId(CharacterSimulationProgram program, SimulationOperation operation)
        {
            string result = string.Empty;
            for (int i = 0; i < program.References.Count; i++)
            {
                ProgramReference reference = program.References[i];
                if (!reference.HasSourceOperation || !reference.SourceOperation.Equals(operation.Handle) || reference.Kind != ProgramReferenceKind.CatalogEntry)
                    continue;
                ProgramCatalogEntry entry = program.CatalogEntries[reference.TargetIndex];
                if (entry.Kind != ProgramCatalogEntryKind.Action)
                    continue;
                if (result.Length != 0)
                    throw new InvalidDataException($"Action activation operation '{operation.Handle}' references multiple Action entries.");
                result = TrimPrefix(entry.Identity, "action:");
            }
            return result;
        }

        static string FindStringConstant(CharacterSimulationProgram program, SimulationOperation operation, string field)
        {
            const string marker = "/constant/";
            for (int i = 0; i < operation.ConstantReferences.Count; i++)
            {
                ProgramConstant constant = program.Constants[operation.ConstantReferences[i]];
                int markerIndex = constant.Identity.LastIndexOf(marker, StringComparison.Ordinal);
                if (markerIndex < 0 || !string.Equals(constant.Identity.Substring(markerIndex + marker.Length), field, StringComparison.Ordinal))
                    continue;
                if (constant.Kind != ProgramConstantKind.String)
                    throw new InvalidDataException($"Operation '{operation.Handle}' constant '{field}' is not String.");
                return constant.Text;
            }
            return string.Empty;
        }

        static int ParseOperationOwner(string ownerIdentity)
        {
            const string prefix = "operation:";
            if (ownerIdentity == null || !ownerIdentity.StartsWith(prefix, StringComparison.Ordinal) ||
                !int.TryParse(ownerIdentity.Substring(prefix.Length), out int value) || value < 0)
            {
                throw new InvalidDataException($"State owner '{ownerIdentity}' is not an operation identity.");
            }
            return value;
        }

        static string TrimPrefix(string value, string prefix)
        {
            if (value == null || !value.StartsWith(prefix, StringComparison.Ordinal) || value.Length == prefix.Length)
                throw new InvalidDataException($"State owner '{value}' does not match '{prefix}*'.");
            return value.Substring(prefix.Length);
        }

        static void AddUnique<TKey>(IDictionary<TKey, TypedStateAddress> values, TKey key, TypedStateAddress address, string label)
        {
            if (values.ContainsKey(key))
                throw new InvalidDataException($"{label} '{key}' is duplicated.");
            values.Add(key, address);
        }

        sealed class ActionAddressBuilder
        {
            public TypedStateAddress Request;
            public TypedStateAddress Instance;
        }

        static void BuildValueInputs(
            CharacterSimulationProgram program,
            out OperationValueInputRange[] ranges,
            out CompiledValueInputBinding[] bindings)
        {
            int operationCount = program.Operations.Count;
            var grouped = new CompiledValueInputBinding?[operationCount][];
            for (int i = 0; i < operationCount; i++)
                grouped[i] = new CompiledValueInputBinding?[CharacterGameplayValuePortContracts.Require(program.Operations[i].Code).Inputs.Count];
            for (int i = 0; i < program.ControlFlow.Count; i++)
            {
                ProgramControlFlowEdge edge = program.ControlFlow[i];
                if (edge.Kind != ProgramControlFlowKind.Value)
                    continue;
                SimulationOperation source = program.Operations[edge.Source.Value];
                SimulationOperation target = program.Operations[edge.Target.Value];
                OperationValuePortDefinition sourcePort = CharacterGameplayValuePortContracts.Require(source.Code).RequireOutput(edge.SourcePort);
                OperationValuePortDefinition targetPort = CharacterGameplayValuePortContracts.Require(target.Code).RequireInput(edge.TargetPort);
                SemanticValueKind kind = CharacterSimulationProgramValueResolver.ResolveOutputKind(program, source, sourcePort);
                CharacterSimulationProgramValueResolver.RequireInputKind(program, target, targetPort, kind);
                Add(grouped[target.Handle.Value], targetPort.Order, new CompiledValueInputBinding(
                    targetPort.Order,
                    kind,
                    CompiledValueInputSourceKind.Operation,
                    source.Handle,
                    sourcePort.Order,
                    -1), target);
            }
            for (int i = 0; i < program.ConstantInputBindings.Count; i++)
            {
                ProgramConstantInputBinding input = program.ConstantInputBindings[i];
                SimulationOperation target = program.Operations[input.TargetOperation.Value];
                OperationValuePortDefinition targetPort = CharacterGameplayValuePortContracts.Require(target.Code).RequireInput(input.TargetPort);
                CharacterSimulationProgramValueResolver.RequireInputKind(program, target, targetPort, input.ResolvedValueKind);
                Add(grouped[target.Handle.Value], targetPort.Order, new CompiledValueInputBinding(
                    targetPort.Order,
                    input.ResolvedValueKind,
                    CompiledValueInputSourceKind.Constant,
                    OperationHandle.Invalid,
                    -1,
                    input.ConstantIndex), target);
            }

            ranges = new OperationValueInputRange[operationCount];
            var flattened = new List<CompiledValueInputBinding>();
            for (int i = 0; i < operationCount; i++)
            {
                int offset = flattened.Count;
                for (int port = 0; port < grouped[i].Length; port++)
                {
                    if (grouped[i][port].HasValue)
                        flattened.Add(grouped[i][port].Value);
                }
                ranges[i] = new OperationValueInputRange(offset, flattened.Count - offset);
            }
            bindings = flattened.ToArray();
        }

        static void BuildMotionModifierRanges(
            CharacterSimulationProgram program,
            out ProgramMotionModifierDescriptor[] descriptors,
            out OperationValueInputRange[] ranges)
        {
            int channelCount = EnumValueCount(typeof(ProgramMotionModifierChannel));
            ranges = new OperationValueInputRange[channelCount];
            var flattened = new List<ProgramMotionModifierDescriptor>(program.MotionModifiers.Count);
            for (int channel = 0; channel < channelCount; channel++)
            {
                int offset = flattened.Count;
                for (int i = 0; i < program.MotionModifiers.Count; i++)
                {
                    ProgramMotionModifierDescriptor descriptor = program.MotionModifiers[i];
                    if ((int)descriptor.Channel == channel)
                        flattened.Add(descriptor);
                }
                ranges[channel] = new OperationValueInputRange(offset, flattened.Count - offset);
            }
            descriptors = flattened.ToArray();
        }

        static int[] BuildNamedConstantIndexes(CharacterSimulationProgram program)
        {
            int fieldCount = OperationNamedConstantSchema.Count;
            var indexes = new int[program.Operations.Count * fieldCount];
            Array.Fill(indexes, -1);
            for (int operationIndex = 0; operationIndex < program.Operations.Count; operationIndex++)
            {
                SimulationOperation operation = program.Operations[operationIndex];
                for (int i = 0; i < operation.ConstantReferences.Count; i++)
                {
                    int constantIndex = operation.ConstantReferences[i];
                    ProgramConstant constant = program.Constants[constantIndex];
                    if (!OperationNamedConstantSchema.TryParseIdentity(constant.Identity, out OperationNamedConstant field))
                        continue;
                    int index = operationIndex * fieldCount + (int)field;
                    if (indexes[index] >= 0)
                        throw new InvalidDataException($"Operation '{operation.Handle}' contains duplicate named constant '{field}'.");
                    indexes[index] = constantIndex;
                }
            }
            return indexes;
        }

        static void Add(
            CompiledValueInputBinding?[] inputs,
            int portIndex,
            CompiledValueInputBinding binding,
            SimulationOperation target)
        {
            if (portIndex < 0 || portIndex >= inputs.Length)
                throw new InvalidOperationException($"Operation '{target.Handle}' Value input port index '{portIndex}' is invalid.");
            if (inputs[portIndex].HasValue)
                throw new InvalidOperationException($"Operation '{target.Handle}' Value input port index '{portIndex}' has multiple sources.");
            inputs[portIndex] = binding;
        }

        static IReadOnlyList<OperationExecutionDescriptor> BuildControlDescriptors(CharacterSimulationProgram program)
        {
            var result = new OperationExecutionDescriptor[program.Operations.Count];
            for (int i = 0; i < program.Operations.Count; i++)
            {
                SimulationOperation operation = program.Operations[i];
                result[i] = new OperationExecutionDescriptor(
                    operation.Handle,
                    operation.Code,
                    operation.Integer0,
                    operation.Integer1,
                    operation.Unsigned0,
                    operation.Text0,
                    operation.Flags,
                    operation.StateSlots);
            }
            return result;
        }
        static void BuildGlobalStateSlots(
            CharacterSimulationProgram program,
            int semanticCount,
            out int[] firstSlots,
            out Dictionary<string, int>[] slotsByOwner)
        {
            firstSlots = new int[semanticCount];
            FillMissing(firstSlots);
            slotsByOwner = new Dictionary<string, int>[semanticCount];
            for (int i = 0; i < program.StateSlots.Count; i++)
            {
                ProgramStateSlot slot = program.StateSlots[i];
                int semanticIndex = (int)slot.Semantic;
                if (firstSlots[semanticIndex] < 0)
                    firstSlots[semanticIndex] = i;
                Dictionary<string, int> owners = slotsByOwner[semanticIndex];
                if (owners == null)
                {
                    owners = new Dictionary<string, int>(StringComparer.Ordinal);
                    slotsByOwner[semanticIndex] = owners;
                }
                if (!owners.ContainsKey(slot.OwnerIdentity))
                    owners.Add(slot.OwnerIdentity, i);
            }
        }

        static OperationHandle ResolveRootOperation(CharacterSimulationProgram program)
        {
            for (int i = 0; i < program.References.Count; i++)
            {
                ProgramReference reference = program.References[i];
                if (!reference.HasSourceOperation &&
                    reference.Kind == ProgramReferenceKind.Operation &&
                    string.Equals(reference.Identity, "program:root-operation", StringComparison.Ordinal))
                {
                    return new OperationHandle(reference.TargetIndex);
                }
            }
            throw new InvalidOperationException("Program root operation reference is missing.");
        }

        void RequireOperation(OperationHandle operation)
        {
            if (!operation.IsValid || operation.Value >= m_Program.Operations.Count)
                throw new ArgumentOutOfRangeException(nameof(operation), $"Operation handle '{operation}' is outside Program '{ProgramId}'.");
        }

        static int CompareEdges(ProgramControlFlowEdge left, ProgramControlFlowEdge right, bool incomingValue)
        {
            int byPriority = left.Kind == ProgramControlFlowKind.Transition
                ? left.Priority.CompareTo(right.Priority)
                : 0;
            if (byPriority != 0)
                return byPriority;
            int byOrder = left.Order.CompareTo(right.Order);
            if (byOrder != 0)
                return byOrder;
            int byPort = incomingValue
                ? string.CompareOrdinal(left.TargetPort, right.TargetPort)
                : string.CompareOrdinal(left.SourcePort, right.SourcePort);
            return byPort != 0 ? byPort : string.CompareOrdinal(left.Identity, right.Identity);
        }

        static int EnumValueCount(Type enumType)
        {
            Array values = Enum.GetValues(enumType);
            int maximum = 0;
            for (int i = 0; i < values.Length; i++)
                maximum = Math.Max(maximum, Convert.ToInt32(values.GetValue(i)));
            return checked(maximum + 1);
        }

        static IReadOnlyList<T> Freeze<T>(List<T> values)
        {
            return values.Count == 0 ? Array.Empty<T>() : Array.AsReadOnly(values.ToArray());
        }

        static void FillMissing(int[] values)
        {
            for (int i = 0; i < values.Length; i++)
                values[i] = -1;
        }

        static string[] BuildOperationSourcePaths(CharacterSimulationProgram program)
        {
            var result = new string[program.Operations.Count];
            var assigned = new bool[result.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = $"operation:{i}";
            for (int i = 0; i < program.SourceMap.Count; i++)
            {
                ProgramSourceMapEntry source = program.SourceMap[i];
                if (source.TargetKind != ProgramSourceTargetKind.Operation ||
                    source.TargetIndex < 0 ||
                    source.TargetIndex >= result.Length ||
                    assigned[source.TargetIndex])
                {
                    continue;
                }
                result[source.TargetIndex] = source.DisplayPath;
                assigned[source.TargetIndex] = true;
            }
            return result;
        }
    }

}
