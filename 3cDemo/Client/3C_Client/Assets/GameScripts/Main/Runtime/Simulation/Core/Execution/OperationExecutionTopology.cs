using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation
{
    public enum ProgramCatalogFieldId : byte
    {
        ActionWindowDigest = 0,
        ActionWindowId = 1,
        ActionWindowType = 2,
        BlendMode = 3,
        Channel = 4,
        ConsumeLowerChannels = 5,
        CueId = 6,
        CueType = 7,
        CurveEndFrame = 8,
        EaseInCurve = 9,
        EaseInFrame = 10,
        EaseOutCurve = 11,
        EaseOutFrame = 12,
        EndFrame = 13,
        FrameRate = 14,
        Intensity = 15,
        MaxFrame = 16,
        Muted = 17,
        Parent = 18,
        PositionX = 19,
        PositionY = 20,
        PositionZ = 21,
        Priority = 22,
        Projection = 23,
        Space = 24,
        StartFrame = 25,
        Timeline = 26,
        Track = 27,
        WeightCurve = 28,
        Yaw = 29,
        TargetRequirement = 30,
        ValueType = 31,
        SyncPolicy = 32,
        InputValueId = 33
    }

    public sealed class ProgramCatalogRuntimeIndex
    {
        const int FieldCount = 34;
        readonly IReadOnlyList<ProgramCatalogEntry> m_Entries;
        readonly int m_KindCount;
        readonly int[] m_OperationEntries;
        readonly ProgramCatalogField[] m_Fields;
        readonly Dictionary<string, int>[] m_EntriesByIdentity;

        public ProgramCatalogRuntimeIndex(
            int operationCount,
            IReadOnlyList<ProgramReference> references,
            IReadOnlyList<ProgramCatalogEntry> entries)
        {
            if (operationCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(operationCount));
            m_Entries = entries ?? throw new ArgumentNullException(nameof(entries));
            m_KindCount = EnumValueCount(typeof(ProgramCatalogEntryKind));
            m_OperationEntries = new int[checked(operationCount * m_KindCount)];
            Array.Fill(m_OperationEntries, -1);
            m_Fields = new ProgramCatalogField[checked(entries.Count * FieldCount)];
            m_EntriesByIdentity = new Dictionary<string, int>[m_KindCount];

            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                ProgramCatalogEntry entry = entries[entryIndex]
                    ?? throw new ArgumentException($"Program catalog entry '{entryIndex}' is null.", nameof(entries));
                if (entry.Index != entryIndex)
                    throw new ArgumentException($"Program catalog entry '{entry.Index}' does not match index '{entryIndex}'.", nameof(entries));
                int kindIndex = (int)entry.Kind;
                Dictionary<string, int> identities = m_EntriesByIdentity[kindIndex] ??=
                    new Dictionary<string, int>(StringComparer.Ordinal);
                if (!identities.TryAdd(entry.Identity, entryIndex))
                    throw new ArgumentException($"Program catalog identity '{entry.Kind}/{entry.Identity}' is duplicated.", nameof(entries));

                for (int fieldIndex = 0; fieldIndex < entry.Fields.Count; fieldIndex++)
                {
                    ProgramCatalogField field = entry.Fields[fieldIndex];
                    if (!Enum.TryParse(field.Name, false, out ProgramCatalogFieldId fieldId) ||
                        !Enum.IsDefined(typeof(ProgramCatalogFieldId), fieldId))
                    {
                        continue;
                    }
                    int index = checked(entryIndex * FieldCount + (int)fieldId);
                    if (m_Fields[index] != null)
                        throw new ArgumentException($"Program catalog field '{entry.Identity}/{field.Name}' is duplicated.", nameof(entries));
                    m_Fields[index] = field;
                }
            }

            if (references == null)
                throw new ArgumentNullException(nameof(references));
            for (int referenceIndex = 0; referenceIndex < references.Count; referenceIndex++)
            {
                ProgramReference reference = references[referenceIndex];
                if (reference == null || !reference.HasSourceOperation || reference.Kind != ProgramReferenceKind.CatalogEntry)
                    continue;
                if (reference.SourceOperation.Value >= operationCount || reference.TargetIndex < 0 || reference.TargetIndex >= entries.Count)
                    throw new ArgumentException($"Program catalog reference '{reference?.Identity}' is invalid.", nameof(references));
                ProgramCatalogEntry entry = entries[reference.TargetIndex];
                int index = checked(reference.SourceOperation.Value * m_KindCount + (int)entry.Kind);
                int existing = m_OperationEntries[index];
                if (existing == -1)
                    m_OperationEntries[index] = reference.TargetIndex;
                else if (existing != reference.TargetIndex)
                    m_OperationEntries[index] = -2;
            }
        }

        public ProgramCatalogEntry RequireEntry(OperationHandle operation, ProgramCatalogEntryKind kind)
        {
            ProgramCatalogEntry entry = FindEntry(operation, kind);
            return entry ?? throw new InvalidOperationException($"Operation '{operation}' has no '{kind}' catalog reference.");
        }

        public ProgramCatalogEntry FindEntry(OperationHandle operation, ProgramCatalogEntryKind kind)
        {
            if (!operation.IsValid || operation.Value * m_KindCount >= m_OperationEntries.Length)
                throw new ArgumentOutOfRangeException(nameof(operation));
            int entryIndex = m_OperationEntries[operation.Value * m_KindCount + (int)kind];
            if (entryIndex == -2)
                throw new InvalidOperationException($"Operation '{operation}' has multiple '{kind}' catalog references.");
            return entryIndex < 0 ? null : m_Entries[entryIndex];
        }

        public ProgramCatalogEntry FindEntry(ProgramCatalogEntryKind kind, string identity)
        {
            int kindIndex = (int)kind;
            if (kindIndex < 0 || kindIndex >= m_EntriesByIdentity.Length)
                throw new ArgumentOutOfRangeException(nameof(kind));
            Dictionary<string, int> entries = m_EntriesByIdentity[kindIndex];
            return entries != null && entries.TryGetValue(identity ?? string.Empty, out int index)
                ? m_Entries[index]
                : null;
        }

        public ProgramCatalogField RequireField(ProgramCatalogEntry entry, ProgramCatalogFieldId field)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));
            int fieldIndex = (int)field;
            if (entry.Index < 0 || entry.Index >= m_Entries.Count || fieldIndex < 0 || fieldIndex >= FieldCount)
                throw new ArgumentOutOfRangeException(nameof(field));
            ProgramCatalogField value = m_Fields[entry.Index * FieldCount + fieldIndex];
            return value ?? throw new InvalidOperationException($"Catalog '{entry.Identity}' has no field '{field}'.");
        }

        public bool TryGetIdentity(ProgramCatalogEntry entry, ProgramCatalogFieldId field, out string identity)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));
            int fieldIndex = (int)field;
            if (entry.Index < 0 || entry.Index >= m_Entries.Count || fieldIndex < 0 || fieldIndex >= FieldCount)
                throw new ArgumentOutOfRangeException(nameof(field));
            ProgramCatalogField value = m_Fields[entry.Index * FieldCount + fieldIndex];
            if (value != null && value.Kind == ProgramCatalogFieldKind.Identity)
            {
                identity = value.Identity;
                return true;
            }
            identity = string.Empty;
            return false;
        }

        static int EnumValueCount(Type enumType)
        {
            Array values = Enum.GetValues(enumType);
            int maximum = 0;
            for (int i = 0; i < values.Length; i++)
                maximum = Math.Max(maximum, Convert.ToInt32(values.GetValue(i)));
            return checked(maximum + 1);
        }
    }

    public enum OperationNamedConstant : byte
    {
        ActionContext = 0,
        SourceInputRequest = 1,
        ConsumeSourceInputRequest = 2,
        TargetKey = 3,
        TargetSnapshotDeclaration = 4,
        TargetSnapshotOwner = 5,
        FactContext = 6,
        Predicted = 7,
        Effect = 8,
        DefinitionRevision = 9,
        Handle = 10,
        MoveSpeed = 11,
        TurnSpeedDegrees = 12,
        Weight = 13,
        Intensity = 14
    }

    public static class OperationNamedConstantSchema
    {
        const string Marker = "/constant/";

        public static int Count => 15;

        public static bool TryParseIdentity(string identity, out OperationNamedConstant field)
        {
            field = default;
            int markerIndex = identity?.LastIndexOf(Marker, StringComparison.Ordinal) ?? -1;
            if (markerIndex < 0)
                return false;
            string value = identity.Substring(markerIndex + Marker.Length);
            return Enum.TryParse(value, false, out field) && Enum.IsDefined(typeof(OperationNamedConstant), field);
        }

        public static bool TryGetDynamicField(string identity, out string field)
        {
            int markerIndex = identity?.LastIndexOf(Marker, StringComparison.Ordinal) ?? -1;
            field = markerIndex < 0 ? string.Empty : identity.Substring(markerIndex + Marker.Length);
            return field.Length != 0;
        }
    }

    public sealed class OperationExecutionDescriptor
    {
        readonly ReadOnlyCollection<int> m_StateSlots;

        public OperationExecutionDescriptor(
            OperationHandle handle,
            SimulationOperationCode code,
            int integer0,
            int integer1,
            ulong unsigned0,
            string text0,
            uint flags,
            IEnumerable<int> stateSlots)
        {
            if (!handle.IsValid)
                throw new ArgumentException("Operation handle is invalid.", nameof(handle));
            if (!Enum.IsDefined(typeof(SimulationOperationCode), code))
                throw new ArgumentOutOfRangeException(nameof(code), $"Operation code '{(ushort)code}' is undefined.");
            Handle = handle;
            Code = code;
            Integer0 = integer0;
            Integer1 = integer1;
            Unsigned0 = unsigned0;
            Text0 = text0 ?? string.Empty;
            Flags = flags;
            var slots = stateSlots == null ? new List<int>() : new List<int>(stateSlots);
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] < 0)
                    throw new ArgumentOutOfRangeException(nameof(stateSlots));
            }
            m_StateSlots = slots.AsReadOnly();
        }

        public OperationHandle Handle { get; }
        public SimulationOperationCode Code { get; }
        public int Integer0 { get; }
        public int Integer1 { get; }
        public ulong Unsigned0 { get; }
        public string Text0 { get; }
        public uint Flags { get; }
        public IReadOnlyList<int> StateSlots => m_StateSlots;
    }

    public sealed class OperationExecutionTopology
    {
        readonly ReadOnlyCollection<OperationExecutionDescriptor> m_Operations;
        readonly IReadOnlyList<ProgramControlFlowEdge>[][] m_Outgoing;
        readonly IReadOnlyList<ProgramReference>[][] m_References;
        readonly int[][] m_OperationStateSlots;
        readonly int[] m_TimelineOperations;
        readonly int[] m_TimelineOwners;
        readonly int[] m_StateMachineOwners;
        readonly ProgramControlFlowEdge[] m_StateOnEnter;
        readonly ProgramControlFlowEdge[] m_StateRoot;
        readonly ProgramControlFlowEdge[] m_StateOnExit;

        public OperationExecutionTopology(
            IEnumerable<OperationExecutionDescriptor> operations,
            IEnumerable<ProgramControlFlowEdge> controlFlow,
            IEnumerable<ProgramReference> references,
            IReadOnlyList<ProgramStateSlot> stateSlots,
            IReadOnlyList<ProgramSourceMapEntry> sourceMap,
            OperationHandle rootOperation)
        {
            var operationList = operations == null
                ? new List<OperationExecutionDescriptor>()
                : new List<OperationExecutionDescriptor>(operations);
            if (operationList.Count == 0)
                throw new ArgumentException("Operation topology is empty.", nameof(operations));
            for (int i = 0; i < operationList.Count; i++)
            {
                OperationExecutionDescriptor operation = operationList[i]
                    ?? throw new ArgumentException($"Operation topology contains null at index {i}.", nameof(operations));
                if (operation.Handle.Value != i)
                    throw new ArgumentException($"Operation topology handle '{operation.Handle}' does not match index '{i}'.", nameof(operations));
            }
            if (!rootOperation.IsValid || rootOperation.Value >= operationList.Count)
                throw new ArgumentOutOfRangeException(nameof(rootOperation));
            if (operationList[rootOperation.Value].Code != SimulationOperationCode.Root)
                throw new ArgumentException($"Root operation '{rootOperation}' is '{operationList[rootOperation.Value].Code}'.", nameof(rootOperation));
            if (stateSlots == null)
                throw new ArgumentNullException(nameof(stateSlots));
            if (sourceMap == null)
                throw new ArgumentNullException(nameof(sourceMap));

            var edges = controlFlow == null
                ? new List<ProgramControlFlowEdge>()
                : new List<ProgramControlFlowEdge>(controlFlow);
            ValidateEdges(edges, operationList.Count);
            var referenceList = references == null
                ? new List<ProgramReference>()
                : new List<ProgramReference>(references);
            ValidateReferences(referenceList, operationList.Count);
            ValidateStateSlots(operationList, stateSlots);

            m_Operations = operationList.AsReadOnly();
            m_Outgoing = BuildOutgoing(operationList.Count, edges);
            m_References = BuildReferences(operationList.Count, referenceList);
            m_OperationStateSlots = BuildOperationStateSlots(operationList, stateSlots);
            BuildTimelineIndexes(operationList, edges, out m_TimelineOperations, out m_TimelineOwners);
            m_StateMachineOwners = BuildStateMachineOwners(operationList, sourceMap);
            BuildStateEdges(operationList, edges, out m_StateOnEnter, out m_StateRoot, out m_StateOnExit);
            RootOperation = rootOperation;
        }

        public IReadOnlyList<OperationExecutionDescriptor> Operations => m_Operations;
        public OperationHandle RootOperation { get; }

        public OperationExecutionDescriptor Operation(OperationHandle handle)
        {
            RequireOperation(handle);
            return m_Operations[handle.Value];
        }

        public IReadOnlyList<ProgramControlFlowEdge> Outgoing(OperationHandle source, ProgramControlFlowKind kind)
        {
            RequireOperation(source);
            int kindIndex = (int)kind;
            if (kindIndex < 0 || kindIndex >= m_Outgoing[source.Value].Length)
                throw new ArgumentOutOfRangeException(nameof(kind));
            return m_Outgoing[source.Value][kindIndex];
        }

        public IReadOnlyList<ProgramReference> References(OperationHandle source, ProgramReferenceKind kind)
        {
            RequireOperation(source);
            int kindIndex = (int)kind;
            if (kindIndex < 0 || kindIndex >= m_References[source.Value].Length)
                throw new ArgumentOutOfRangeException(nameof(kind));
            return m_References[source.Value][kindIndex];
        }

        public ProgramReference FirstReference(OperationHandle source, ProgramReferenceKind kind)
        {
            IReadOnlyList<ProgramReference> references = References(source, kind);
            return references.Count == 0 ? null : references[0];
        }

        public int TimelineOperationCount => m_TimelineOperations.Length;

        public OperationExecutionDescriptor TimelineOperationAt(int index)
        {
            if (index < 0 || index >= m_TimelineOperations.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Operations[m_TimelineOperations[index]];
        }

        public OperationHandle TimelineOwner(OperationHandle child)
        {
            RequireOperation(child);
            int owner = m_TimelineOwners[child.Value];
            if (owner < 0)
                throw new InvalidOperationException($"Operation '{child}' has no Timeline owner.");
            return new OperationHandle(owner);
        }

        public OperationHandle StateMachineOwner(OperationHandle state)
        {
            RequireOperation(state);
            int owner = m_StateMachineOwners[state.Value];
            return owner < 0 ? OperationHandle.Invalid : new OperationHandle(owner);
        }

        public ProgramControlFlowEdge StateOnEnter(OperationHandle state) => StateEdge(m_StateOnEnter, state);
        public ProgramControlFlowEdge StateRoot(OperationHandle state) => StateEdge(m_StateRoot, state);
        public ProgramControlFlowEdge StateOnExit(OperationHandle state) => StateEdge(m_StateOnExit, state);

        public int FindOperationStateSlot(OperationHandle operation, ProgramStateSemantic semantic)
        {
            RequireOperation(operation);
            int semanticIndex = (int)semantic;
            return semanticIndex < m_OperationStateSlots[operation.Value].Length
                ? m_OperationStateSlots[operation.Value][semanticIndex]
                : -1;
        }

        public int RequireOperationStateSlot(OperationHandle operation, ProgramStateSemantic semantic)
        {
            int slot = FindOperationStateSlot(operation, semantic);
            if (slot < 0)
                throw new InvalidOperationException($"Operation '{operation}' has no state slot for '{semantic}'.");
            return slot;
        }

        public void RequireOperation(OperationHandle operation)
        {
            if (!operation.IsValid || operation.Value >= m_Operations.Count)
                throw new ArgumentOutOfRangeException(nameof(operation), $"Operation handle '{operation}' is outside this topology.");
        }

        static void ValidateEdges(IReadOnlyList<ProgramControlFlowEdge> edges, int operationCount)
        {
            for (int i = 0; i < edges.Count; i++)
            {
                ProgramControlFlowEdge edge = edges[i]
                    ?? throw new ArgumentException($"Control-flow contains null at index {i}.", nameof(edges));
                if (!edge.Source.IsValid || edge.Source.Value >= operationCount ||
                    !edge.Target.IsValid || edge.Target.Value >= operationCount ||
                    edge.HasCondition && (!edge.Condition.IsValid || edge.Condition.Value >= operationCount))
                {
                    throw new ArgumentException($"Control-flow edge '{edge.Identity}' targets an operation outside this topology.", nameof(edges));
                }
            }
        }

        static void ValidateReferences(IReadOnlyList<ProgramReference> references, int operationCount)
        {
            for (int i = 0; i < references.Count; i++)
            {
                ProgramReference reference = references[i]
                    ?? throw new ArgumentException($"Program reference contains null at index {i}.", nameof(references));
                if (reference.HasSourceOperation && reference.SourceOperation.Value >= operationCount)
                    throw new ArgumentException($"Program reference '{reference.Identity}' has an invalid source operation.", nameof(references));
                if ((reference.Kind == ProgramReferenceKind.Operation ||
                     reference.Kind == ProgramReferenceKind.MotionSourceOperation) &&
                    reference.TargetIndex >= operationCount)
                    throw new ArgumentException($"Program reference '{reference.Identity}' has an invalid target operation.", nameof(references));
            }
        }

        static void ValidateStateSlots(
            IReadOnlyList<OperationExecutionDescriptor> operations,
            IReadOnlyList<ProgramStateSlot> stateSlots)
        {
            for (int i = 0; i < stateSlots.Count; i++)
            {
                if (stateSlots[i] == null || stateSlots[i].Index != i)
                    throw new ArgumentException($"State slot at index '{i}' is invalid.", nameof(stateSlots));
            }
            for (int operationIndex = 0; operationIndex < operations.Count; operationIndex++)
            {
                IReadOnlyList<int> slots = operations[operationIndex].StateSlots;
                for (int i = 0; i < slots.Count; i++)
                {
                    if (slots[i] >= stateSlots.Count)
                        throw new ArgumentException($"Operation '{operationIndex}' state slot '{slots[i]}' is outside the layout.", nameof(operations));
                }
            }
        }

        static IReadOnlyList<ProgramControlFlowEdge>[][] BuildOutgoing(
            int operationCount,
            IReadOnlyList<ProgramControlFlowEdge> edges)
        {
            int kindCount = EnumValueCount(typeof(ProgramControlFlowKind));
            var grouped = new List<ProgramControlFlowEdge>[operationCount][];
            for (int operationIndex = 0; operationIndex < operationCount; operationIndex++)
            {
                grouped[operationIndex] = new List<ProgramControlFlowEdge>[kindCount];
                for (int kindIndex = 0; kindIndex < kindCount; kindIndex++)
                    grouped[operationIndex][kindIndex] = new List<ProgramControlFlowEdge>();
            }
            for (int i = 0; i < edges.Count; i++)
                grouped[edges[i].Source.Value][(int)edges[i].Kind].Add(edges[i]);

            var result = new IReadOnlyList<ProgramControlFlowEdge>[operationCount][];
            for (int operationIndex = 0; operationIndex < operationCount; operationIndex++)
            {
                result[operationIndex] = new IReadOnlyList<ProgramControlFlowEdge>[kindCount];
                for (int kindIndex = 0; kindIndex < kindCount; kindIndex++)
                {
                    List<ProgramControlFlowEdge> values = grouped[operationIndex][kindIndex];
                    values.Sort(CompareEdges);
                    result[operationIndex][kindIndex] = values.Count == 0
                        ? Array.Empty<ProgramControlFlowEdge>()
                        : Array.AsReadOnly(values.ToArray());
                }
            }
            return result;
        }

        static int[][] BuildOperationStateSlots(
            IReadOnlyList<OperationExecutionDescriptor> operations,
            IReadOnlyList<ProgramStateSlot> stateSlots)
        {
            int semanticCount = EnumValueCount(typeof(ProgramStateSemantic));
            var result = new int[operations.Count][];
            for (int operationIndex = 0; operationIndex < operations.Count; operationIndex++)
            {
                var slots = new int[semanticCount];
                for (int i = 0; i < slots.Length; i++)
                    slots[i] = -1;
                IReadOnlyList<int> operationSlots = operations[operationIndex].StateSlots;
                for (int i = 0; i < operationSlots.Count; i++)
                {
                    int slotIndex = operationSlots[i];
                    int semanticIndex = (int)stateSlots[slotIndex].Semantic;
                    if (slots[semanticIndex] < 0)
                        slots[semanticIndex] = slotIndex;
                }
                result[operationIndex] = slots;
            }
            return result;
        }

        static IReadOnlyList<ProgramReference>[][] BuildReferences(
            int operationCount,
            IReadOnlyList<ProgramReference> references)
        {
            int kindCount = EnumValueCount(typeof(ProgramReferenceKind));
            var grouped = new List<ProgramReference>[operationCount][];
            for (int operationIndex = 0; operationIndex < operationCount; operationIndex++)
            {
                grouped[operationIndex] = new List<ProgramReference>[kindCount];
                for (int kindIndex = 0; kindIndex < kindCount; kindIndex++)
                    grouped[operationIndex][kindIndex] = new List<ProgramReference>();
            }
            for (int i = 0; i < references.Count; i++)
            {
                ProgramReference reference = references[i];
                if (reference.HasSourceOperation)
                    grouped[reference.SourceOperation.Value][(int)reference.Kind].Add(reference);
            }

            var result = new IReadOnlyList<ProgramReference>[operationCount][];
            for (int operationIndex = 0; operationIndex < operationCount; operationIndex++)
            {
                result[operationIndex] = new IReadOnlyList<ProgramReference>[kindCount];
                for (int kindIndex = 0; kindIndex < kindCount; kindIndex++)
                {
                    List<ProgramReference> values = grouped[operationIndex][kindIndex];
                    result[operationIndex][kindIndex] = values.Count == 0
                        ? Array.Empty<ProgramReference>()
                        : Array.AsReadOnly(values.ToArray());
                }
            }
            return result;
        }

        static void BuildTimelineIndexes(
            IReadOnlyList<OperationExecutionDescriptor> operations,
            IReadOnlyList<ProgramControlFlowEdge> edges,
            out int[] timelines,
            out int[] owners)
        {
            var timelineValues = new List<int>();
            owners = new int[operations.Count];
            for (int i = 0; i < owners.Length; i++)
            {
                owners[i] = -1;
                if (operations[i].Code == SimulationOperationCode.Timeline)
                    timelineValues.Add(i);
            }
            for (int i = 0; i < edges.Count; i++)
            {
                ProgramControlFlowEdge edge = edges[i];
                if (edge.Kind != ProgramControlFlowKind.Child || operations[edge.Source.Value].Code != SimulationOperationCode.Timeline)
                    continue;
                if (owners[edge.Target.Value] >= 0)
                    throw new ArgumentException($"Operation '{edge.Target}' has multiple Timeline owners.", nameof(edges));
                owners[edge.Target.Value] = edge.Source.Value;
            }
            timelines = timelineValues.ToArray();
        }

        static int[] BuildStateMachineOwners(
            IReadOnlyList<OperationExecutionDescriptor> operations,
            IReadOnlyList<ProgramSourceMapEntry> sourceMap)
        {
            var machineByGraph = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < operations.Count; i++)
            {
                if (operations[i].Code != SimulationOperationCode.StateMachine || string.IsNullOrEmpty(operations[i].Text0))
                    continue;
                if (!machineByGraph.TryAdd(operations[i].Text0, i))
                    throw new ArgumentException($"StateMachine graph identity '{operations[i].Text0}' is duplicated.");
            }
            var result = new int[operations.Count];
            for (int i = 0; i < result.Length; i++)
                result[i] = -1;
            for (int i = 0; i < sourceMap.Count; i++)
            {
                ProgramSourceMapEntry source = sourceMap[i];
                if (source.TargetKind != ProgramSourceTargetKind.Operation ||
                    source.TargetIndex < 0 || source.TargetIndex >= operations.Count ||
                    operations[source.TargetIndex].Code != SimulationOperationCode.State ||
                    string.IsNullOrEmpty(source.GraphId) ||
                    !machineByGraph.TryGetValue(source.GraphId, out int machine))
                    continue;
                if (result[source.TargetIndex] >= 0 && result[source.TargetIndex] != machine)
                    throw new ArgumentException($"State '{source.TargetIndex}' resolves to multiple StateMachine owners.");
                result[source.TargetIndex] = machine;
            }
            return result;
        }

        static void BuildStateEdges(
            IReadOnlyList<OperationExecutionDescriptor> operations,
            IReadOnlyList<ProgramControlFlowEdge> edges,
            out ProgramControlFlowEdge[] onEnter,
            out ProgramControlFlowEdge[] root,
            out ProgramControlFlowEdge[] onExit)
        {
            onEnter = new ProgramControlFlowEdge[operations.Count];
            root = new ProgramControlFlowEdge[operations.Count];
            onExit = new ProgramControlFlowEdge[operations.Count];
            for (int i = 0; i < edges.Count; i++)
            {
                ProgramControlFlowEdge edge = edges[i];
                if (operations[edge.Source.Value].Code != SimulationOperationCode.State)
                    continue;
                if (edge.Kind == ProgramControlFlowKind.Enter && string.Equals(edge.SourcePort, "OnEnter", StringComparison.Ordinal))
                    Assign(onEnter, edge);
                else if (edge.Kind == ProgramControlFlowKind.Enter && string.Equals(edge.SourcePort, "Root", StringComparison.Ordinal))
                    Assign(root, edge);
                else if (edge.Kind == ProgramControlFlowKind.Exit && string.Equals(edge.SourcePort, "OnExit", StringComparison.Ordinal))
                    Assign(onExit, edge);
            }
        }

        static void Assign(ProgramControlFlowEdge[] values, ProgramControlFlowEdge edge)
        {
            if (values[edge.Source.Value] != null)
                throw new ArgumentException($"State '{edge.Source}' has duplicate semantic edge '{edge.SourcePort}'.");
            values[edge.Source.Value] = edge;
        }

        ProgramControlFlowEdge StateEdge(ProgramControlFlowEdge[] values, OperationHandle state)
        {
            RequireOperation(state);
            return values[state.Value];
        }

        static int CompareEdges(ProgramControlFlowEdge left, ProgramControlFlowEdge right)
        {
            int byPriority = left.Kind == ProgramControlFlowKind.Transition
                ? left.Priority.CompareTo(right.Priority)
                : 0;
            if (byPriority != 0)
                return byPriority;
            int byOrder = left.Order.CompareTo(right.Order);
            if (byOrder != 0)
                return byOrder;
            int byPort = string.CompareOrdinal(left.SourcePort, right.SourcePort);
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
    }
}
