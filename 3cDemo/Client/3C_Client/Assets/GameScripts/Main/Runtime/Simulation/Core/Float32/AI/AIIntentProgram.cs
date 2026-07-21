using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace ThirdPersonSimulation
{
    public readonly struct AIIntentProgramId : IEquatable<AIIntentProgramId>
    {
        public AIIntentProgramId(string value)
        {
            Value = SimulationIdentity.Require(value, nameof(value));
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public bool Equals(AIIntentProgramId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is AIIntentProgramId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
    }

    public sealed class AIIntentProgram
    {
        readonly ReadOnlyCollection<AIIntentSemanticOperation> m_Operations;
        readonly ReadOnlyCollection<ProgramControlFlowEdge> m_Edges;
        readonly ReadOnlyCollection<AIIntentMemoryDeclaration> m_Memory;
        readonly ReadOnlyCollection<ProgramStateSlot> m_StateSlots;
        readonly ReadOnlyCollection<ProgramSourceMapEntry> m_SourceMap;
        readonly Dictionary<string, AIIntentMemoryDeclaration> m_MemoryByIdentity;

        internal AIIntentProgram(
            AIIntentSemanticIr semanticIr,
            IEnumerable<ProgramStateSlot> stateSlots,
            IEnumerable<ProgramSourceMapEntry> sourceMap)
        {
            SemanticIr = semanticIr ?? throw new ArgumentNullException(nameof(semanticIr));
            AIIntentOperationSet.RequireVersion(AIIntentOperationSet.Version);
            ProgramId = new AIIntentProgramId($"ai-intent:{semanticIr.ControllerId}");
            NumericProfile = Float32SimulationNumericProfile.Value;
            m_Operations = new List<AIIntentSemanticOperation>(semanticIr.Operations).AsReadOnly();
            m_Edges = new List<ProgramControlFlowEdge>(semanticIr.Edges).AsReadOnly();
            m_Memory = new List<AIIntentMemoryDeclaration>(semanticIr.Memory).AsReadOnly();
            m_StateSlots = new List<ProgramStateSlot>(stateSlots ?? throw new ArgumentNullException(nameof(stateSlots))).AsReadOnly();
            m_SourceMap = new List<ProgramSourceMapEntry>(sourceMap ?? throw new ArgumentNullException(nameof(sourceMap))).AsReadOnly();
            m_MemoryByIdentity = new Dictionary<string, AIIntentMemoryDeclaration>(StringComparer.Ordinal);
            for (int i = 0; i < m_Memory.Count; i++)
                m_MemoryByIdentity.Add(m_Memory[i].Identity, m_Memory[i]);
            var execution = new OperationExecutionDescriptor[m_Operations.Count];
            for (int i = 0; i < m_Operations.Count; i++)
            {
                AIIntentSemanticOperation operation = m_Operations[i];
                AIIntentOperationSet.RequireOperation(operation.Code);
                var slots = new List<int>();
                for (int slot = 0; slot < m_StateSlots.Count; slot++)
                {
                    if (string.Equals(m_StateSlots[slot].OwnerIdentity, operation.NodeIdentity, StringComparison.Ordinal) &&
                        m_StateSlots[slot].OwnerKind == ProgramStateOwnerKind.Runnable)
                    {
                        slots.Add(slot);
                    }
                }
                execution[i] = new OperationExecutionDescriptor(
                    operation.Handle,
                    operation.Code,
                    operation.Integer0,
                    operation.Integer1,
                    operation.Unsigned0,
                    operation.BindingIdentity,
                    0,
                    slots);
            }
            Topology = new OperationExecutionTopology(
                execution,
                m_Edges,
                Array.Empty<ProgramReference>(),
                m_StateSlots,
                m_SourceMap,
                semanticIr.RootOperation);
            LayoutHash = ComputeLayoutHash();
            ProgramHash = new ProgramHash(SimulationCanonicalPayloadHash.Compute(AIIntentProgramCodec.WriteArtifactPayload(semanticIr)));
        }

        public AIIntentSemanticIr SemanticIr { get; }
        public AIIntentProgramId ProgramId { get; }
        public ProgramHash ProgramHash { get; }
        public LayoutHash LayoutHash { get; }
        public SimulationNumericProfile NumericProfile { get; }
        public ProgramId CharacterProgramId => SemanticIr.CharacterProgramId;
        public ProgramHash CharacterProgramHash => SemanticIr.CharacterProgramHash;
        public StableHash PerceptionSchemaHash => SemanticIr.PerceptionSchemaHash;
        public OperationExecutionTopology Topology { get; }
        public IReadOnlyList<AIIntentSemanticOperation> Operations => m_Operations;
        public IReadOnlyList<ProgramControlFlowEdge> Edges => m_Edges;
        public IReadOnlyList<AIIntentMemoryDeclaration> Memory => m_Memory;
        public IReadOnlyList<ProgramStateSlot> StateSlots => m_StateSlots;
        public IReadOnlyList<ProgramSourceMapEntry> SourceMap => m_SourceMap;

        public AIIntentSemanticOperation Operation(OperationHandle handle)
        {
            if (!handle.IsValid || handle.Value >= m_Operations.Count)
                throw new ArgumentOutOfRangeException(nameof(handle));
            return m_Operations[handle.Value];
        }

        public AIIntentMemoryDeclaration GetRequiredMemory(string identity)
        {
            if (!m_MemoryByIdentity.TryGetValue(identity ?? string.Empty, out AIIntentMemoryDeclaration declaration))
                throw new InvalidOperationException($"AI Program has no memory declaration '{identity}'.");
            return declaration;
        }

        LayoutHash ComputeLayoutHash()
        {
            var values = new string[m_StateSlots.Count + m_Memory.Count + 4];
            values[0] = "ai-intent-layout/2";
            values[1] = ProgramId.Value;
            values[2] = SemanticIr.PerceptionSchemaHash.ToString();
            values[3] = SemanticIr.CharacterProgramHash.ToString();
            int index = 4;
            for (int i = 0; i < m_StateSlots.Count; i++)
            {
                ProgramStateSlot slot = m_StateSlots[i];
                values[index++] = $"{slot.Index}:{slot.Identity}:{slot.ValueKind}:{slot.OwnerKind}:{slot.Semantic}:{slot.OwnerIdentity}:{slot.StateCodecIdentity}";
            }
            for (int i = 0; i < m_Memory.Count; i++)
            {
                AIIntentMemoryDeclaration memory = m_Memory[i];
                values[index++] = $"{memory.Address}:{memory.Identity}:{memory.Scope}:{memory.ValueKind}:{memory.Integer0}:{memory.Scalar0:R}:{memory.Scalar1:R}:{memory.Scalar2:R}:{memory.Scalar3:R}:{memory.Text0}";
            }
            return new LayoutHash(StableHash.Compute(values));
        }
    }

    public static class AIIntentProgramLowerer
    {
        public const string Version = "float32-ai-intent-lowerer/2";

        public static AIIntentProgram Lower(AIIntentSemanticIr semanticIr)
        {
            if (semanticIr == null)
                throw new ArgumentNullException(nameof(semanticIr));
            var stateSlots = new List<ProgramStateSlot>(semanticIr.Operations.Count * 4);
            var sourceMap = new List<ProgramSourceMapEntry>(semanticIr.Operations.Count);
            for (int i = 0; i < semanticIr.Operations.Count; i++)
            {
                AIIntentSemanticOperation operation = semanticIr.Operations[i];
                SplitIdentity(operation.NodeIdentity, out string graphId, out string nodeId);
                AddRunnableSlot(stateSlots, operation, ProgramStateSemantic.RunnableLifecycle, ProgramStateValueKind.Int32);
                AddRunnableSlot(stateSlots, operation, ProgramStateSemantic.RunnableChildCursor, ProgramStateValueKind.Int32);
                AddRunnableSlot(stateSlots, operation, ProgramStateSemantic.RunnableStopBarrier, ProgramStateValueKind.Int32);
                AddRunnableSlot(stateSlots, operation, ProgramStateSemantic.RunnableActivationGeneration, ProgramStateValueKind.UInt64);
                if (operation.Code == SimulationOperationCode.AIWaitTicks)
                    AddRunnableSlot(stateSlots, operation, ProgramStateSemantic.AIWaitElapsedTicks, ProgramStateValueKind.Int32);
                sourceMap.Add(new ProgramSourceMapEntry(
                    ProgramSourceTargetKind.Operation,
                    operation.Handle.Value,
                    "AIControllerTree",
                    graphId,
                    nodeId,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    operation.NodePath,
                    semanticIr.SourceRevision));
            }
            for (int i = 0; i < semanticIr.Memory.Count; i++)
            {
                AIIntentMemoryDeclaration memory = semanticIr.Memory[i];
                SplitIdentity(memory.Identity, out string ownerId, out string declarationId);
                sourceMap.Add(new ProgramSourceMapEntry(
                    ProgramSourceTargetKind.Constant,
                    memory.Address,
                    "AIControllerBlackboard",
                    ownerId,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    declarationId,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    $"AI/Blackboard/{memory.Scope}/{declarationId}",
                    semanticIr.SourceRevision));
            }
            return new AIIntentProgram(semanticIr, stateSlots, sourceMap);
        }

        static void SplitIdentity(string identity, out string owner, out string element)
        {
            int separator = identity?.LastIndexOf('/') ?? -1;
            if (separator <= 0 || separator == identity.Length - 1)
                throw new InvalidOperationException($"AI source identity '{identity}' is invalid.");
            owner = identity.Substring(0, separator);
            element = identity.Substring(separator + 1);
        }

        static void AddRunnableSlot(
            List<ProgramStateSlot> slots,
            AIIntentSemanticOperation operation,
            ProgramStateSemantic semantic,
            ProgramStateValueKind kind)
        {
            int index = slots.Count;
            slots.Add(new ProgramStateSlot(
                index,
                $"ai:{operation.NodeIdentity}:{semantic}",
                kind,
                ProgramStateOwnerKind.Runnable,
                semantic,
                operation.NodeIdentity,
                -1));
        }
    }

    public static class AIIntentProgramCodec
    {
        const uint Magic = 0x50494146;
        const int Version = 2;

        public static byte[] WriteArtifact(AIIntentProgram program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            return WriteArtifactPayload(program.SemanticIr);
        }

        internal static byte[] WriteArtifactPayload(AIIntentSemanticIr semanticIr)
        {
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(Magic);
            writer.WriteInt32(Version);
            writer.WriteString(AIIntentProgramLowerer.Version);
            writer.WriteString(Float32SimulationNumericProfile.Value.Id.Value);
            writer.WriteInt32(Float32SimulationNumericProfile.Value.AbiVersion.Value);
            writer.WriteString(AIIntentOperationSet.Version.Value);
            writer.WriteBytes(AIIntentSemanticIrCodec.Write(semanticIr));
            return writer.ToArray();
        }

        public static AIIntentProgram ReadArtifact(byte[] bytes)
        {
            var reader = new CanonicalReader(bytes ?? throw new ArgumentNullException(nameof(bytes)));
            if (reader.ReadUInt32() != Magic || reader.ReadInt32() != Version)
                throw new InvalidDataException("AI Intent Program artifact header is invalid.");
            if (!string.Equals(reader.ReadString(), AIIntentProgramLowerer.Version, StringComparison.Ordinal) ||
                !string.Equals(reader.ReadString(), Float32SimulationNumericProfile.Value.Id.Value, StringComparison.Ordinal) ||
                reader.ReadInt32() != Float32SimulationNumericProfile.Value.AbiVersion.Value)
            {
                throw new InvalidDataException("AI Intent Program artifact target identity is unsupported.");
            }
            AIIntentOperationSet.RequireVersion(new OperationSetVersion(reader.ReadString()));
            AIIntentSemanticIr semanticIr = AIIntentSemanticIrCodec.Read(reader.ReadBytes());
            reader.RequireComplete();
            return AIIntentProgramLowerer.Lower(semanticIr);
        }
    }
}
