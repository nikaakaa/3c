using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace ThirdPersonSimulation
{
    public enum AIIntentValueKind : byte
    {
        Boolean = 1,
        Integer = 2,
        Scalar = 3,
        Vector2 = 4,
        Vector3 = 5,
        ActorId = 6,
        ActionTargetSnapshot = 7
    }

    public enum AIMemoryScope : byte
    {
        Controller = 1,
        Tick = 2
    }

    public sealed class AIIntentSemanticOperation
    {
        public AIIntentSemanticOperation(
            OperationHandle handle,
            SimulationOperationCode code,
            string nodeIdentity,
            string nodePath,
            string bindingIdentity,
            string memoryIdentity,
            AIIntentValueKind valueKind,
            int integer0,
            int integer1,
            ulong unsigned0,
            double scalar0,
            double scalar1,
            double scalar2,
            double scalar3)
        {
            if (!handle.IsValid)
                throw new ArgumentException("AI Semantic operation handle is invalid.", nameof(handle));
            AIIntentOperationSet.RequireOperation(code);
            Handle = handle;
            Code = code;
            NodeIdentity = SimulationIdentity.Require(nodeIdentity, nameof(nodeIdentity));
            NodePath = SimulationIdentity.Require(nodePath, nameof(nodePath));
            BindingIdentity = bindingIdentity?.Trim() ?? string.Empty;
            MemoryIdentity = memoryIdentity?.Trim() ?? string.Empty;
            ValueKind = valueKind;
            Integer0 = integer0;
            Integer1 = integer1;
            Unsigned0 = unsigned0;
            Scalar0 = RequireFinite(scalar0, nameof(scalar0));
            Scalar1 = RequireFinite(scalar1, nameof(scalar1));
            Scalar2 = RequireFinite(scalar2, nameof(scalar2));
            Scalar3 = RequireFinite(scalar3, nameof(scalar3));
        }

        public OperationHandle Handle { get; }
        public SimulationOperationCode Code { get; }
        public string NodeIdentity { get; }
        public string NodePath { get; }
        public string BindingIdentity { get; }
        public string MemoryIdentity { get; }
        public AIIntentValueKind ValueKind { get; }
        public int Integer0 { get; }
        public int Integer1 { get; }
        public ulong Unsigned0 { get; }
        public double Scalar0 { get; }
        public double Scalar1 { get; }
        public double Scalar2 { get; }
        public double Scalar3 { get; }

        static double RequireFinite(double value, string parameter)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameter);
            return value == 0d ? 0d : value;
        }
    }

    public sealed class AIIntentMemoryDeclaration
    {
        public AIIntentMemoryDeclaration(
            int address,
            string identity,
            AIMemoryScope scope,
            AIIntentValueKind valueKind,
            int integer0 = 0,
            double scalar0 = 0d,
            double scalar1 = 0d,
            double scalar2 = 0d,
            double scalar3 = 0d,
            string text0 = "")
        {
            if (address < 0 || !Enum.IsDefined(typeof(AIMemoryScope), scope) ||
                !Enum.IsDefined(typeof(AIIntentValueKind), valueKind))
            {
                throw new ArgumentException("AI memory declaration is invalid.");
            }
            Address = address;
            Identity = SimulationIdentity.Require(identity, nameof(identity));
            Scope = scope;
            ValueKind = valueKind;
            Integer0 = integer0;
            Scalar0 = RequireFinite(scalar0, nameof(scalar0));
            Scalar1 = RequireFinite(scalar1, nameof(scalar1));
            Scalar2 = RequireFinite(scalar2, nameof(scalar2));
            Scalar3 = RequireFinite(scalar3, nameof(scalar3));
            Text0 = text0?.Trim() ?? string.Empty;
        }

        public int Address { get; }
        public string Identity { get; }
        public AIMemoryScope Scope { get; }
        public AIIntentValueKind ValueKind { get; }
        public int Integer0 { get; }
        public double Scalar0 { get; }
        public double Scalar1 { get; }
        public double Scalar2 { get; }
        public double Scalar3 { get; }
        public string Text0 { get; }

        static double RequireFinite(double value, string parameter)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameter);
            return value == 0d ? 0d : value;
        }
    }

    public sealed class AIIntentSemanticIr
    {
        readonly ReadOnlyCollection<AIIntentSemanticOperation> m_Operations;
        readonly ReadOnlyCollection<ProgramControlFlowEdge> m_Edges;
        readonly ReadOnlyCollection<AIIntentMemoryDeclaration> m_Memory;

        public AIIntentSemanticIr(
            string controllerId,
            string sourceRevision,
            ProgramId characterProgramId,
            ProgramHash characterProgramHash,
            StableHash perceptionSchemaHash,
            OperationHandle rootOperation,
            IEnumerable<AIIntentSemanticOperation> operations,
            IEnumerable<ProgramControlFlowEdge> edges,
            IEnumerable<AIIntentMemoryDeclaration> memory)
        {
            ControllerId = SimulationIdentity.Require(controllerId, nameof(controllerId));
            SourceRevision = SimulationIdentity.Require(sourceRevision, nameof(sourceRevision));
            if (!characterProgramId.IsValid || !characterProgramHash.IsValid || !perceptionSchemaHash.IsValid || !rootOperation.IsValid)
                throw new ArgumentException("AI Semantic IR binding identity is incomplete.");
            CharacterProgramId = characterProgramId;
            CharacterProgramHash = characterProgramHash;
            PerceptionSchemaHash = perceptionSchemaHash;
            RootOperation = rootOperation;
            var operationValues = operations == null
                ? new List<AIIntentSemanticOperation>()
                : new List<AIIntentSemanticOperation>(operations);
            operationValues.Sort((left, right) => left.Handle.Value.CompareTo(right.Handle.Value));
            if (operationValues.Count == 0 || rootOperation.Value >= operationValues.Count)
                throw new ArgumentException("AI Semantic IR operation roster is invalid.", nameof(operations));
            for (int i = 0; i < operationValues.Count; i++)
            {
                if (operationValues[i] == null || operationValues[i].Handle.Value != i)
                    throw new ArgumentException("AI Semantic IR operation handles are not canonical.", nameof(operations));
            }
            if (operationValues[rootOperation.Value].Code != SimulationOperationCode.Root)
                throw new ArgumentException("AI Semantic IR root operation is not Root.", nameof(rootOperation));
            var edgeValues = edges == null ? new List<ProgramControlFlowEdge>() : new List<ProgramControlFlowEdge>(edges);
            edgeValues.Sort((left, right) => string.CompareOrdinal(left.Identity, right.Identity));
            for (int i = 0; i < edgeValues.Count; i++)
            {
                if (edgeValues[i] == null || edgeValues[i].Source.Value >= operationValues.Count ||
                    edgeValues[i].Target.Value >= operationValues.Count ||
                    edgeValues[i].HasCondition && edgeValues[i].Condition.Value >= operationValues.Count ||
                    i > 0 && string.Equals(edgeValues[i - 1].Identity, edgeValues[i].Identity, StringComparison.Ordinal))
                {
                    throw new ArgumentException("AI Semantic IR contains an invalid edge.", nameof(edges));
                }
            }
            var memoryValues = memory == null
                ? new List<AIIntentMemoryDeclaration>()
                : new List<AIIntentMemoryDeclaration>(memory);
            memoryValues.Sort((left, right) => left.Address.CompareTo(right.Address));
            for (int i = 0; i < memoryValues.Count; i++)
            {
                if (memoryValues[i] == null || memoryValues[i].Address != i ||
                    i > 0 && string.Equals(memoryValues[i - 1].Identity, memoryValues[i].Identity, StringComparison.Ordinal))
                {
                    throw new ArgumentException("AI Semantic IR memory layout is not canonical.", nameof(memory));
                }
            }
            m_Operations = operationValues.AsReadOnly();
            m_Edges = edgeValues.AsReadOnly();
            m_Memory = memoryValues.AsReadOnly();
            SemanticHash = SimulationCanonicalPayloadHash.Compute(AIIntentSemanticIrCodec.Write(this));
        }

        public string ControllerId { get; }
        public string SourceRevision { get; }
        public ProgramId CharacterProgramId { get; }
        public ProgramHash CharacterProgramHash { get; }
        public StableHash PerceptionSchemaHash { get; }
        public OperationHandle RootOperation { get; }
        public IReadOnlyList<AIIntentSemanticOperation> Operations => m_Operations;
        public IReadOnlyList<ProgramControlFlowEdge> Edges => m_Edges;
        public IReadOnlyList<AIIntentMemoryDeclaration> Memory => m_Memory;
        public StableHash SemanticHash { get; }
    }

    public static class AIIntentSemanticIrCodec
    {
        const uint Magic = 0x52495341;
        const int Version = 2;

        public static byte[] Write(AIIntentSemanticIr value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(Magic);
            writer.WriteInt32(Version);
            writer.WriteString(value.ControllerId);
            writer.WriteString(value.SourceRevision);
            writer.WriteString(value.CharacterProgramId.Value);
            writer.WriteString(value.CharacterProgramHash.ToString());
            writer.WriteString(value.PerceptionSchemaHash.ToString());
            writer.WriteInt32(value.RootOperation.Value);
            writer.WriteInt32(value.Operations.Count);
            for (int i = 0; i < value.Operations.Count; i++)
            {
                AIIntentSemanticOperation operation = value.Operations[i];
                writer.WriteInt32(operation.Handle.Value);
                writer.WriteUInt16((ushort)operation.Code);
                writer.WriteString(operation.NodeIdentity);
                writer.WriteString(operation.NodePath);
                writer.WriteString(operation.BindingIdentity);
                writer.WriteString(operation.MemoryIdentity);
                writer.WriteByte((byte)operation.ValueKind);
                writer.WriteInt32(operation.Integer0);
                writer.WriteInt32(operation.Integer1);
                writer.WriteUInt64(operation.Unsigned0);
                writer.WriteDouble(operation.Scalar0);
                writer.WriteDouble(operation.Scalar1);
                writer.WriteDouble(operation.Scalar2);
                writer.WriteDouble(operation.Scalar3);
            }
            writer.WriteInt32(value.Edges.Count);
            for (int i = 0; i < value.Edges.Count; i++)
                WriteEdge(writer, value.Edges[i]);
            writer.WriteInt32(value.Memory.Count);
            for (int i = 0; i < value.Memory.Count; i++)
            {
                AIIntentMemoryDeclaration memory = value.Memory[i];
                writer.WriteInt32(memory.Address);
                writer.WriteString(memory.Identity);
                writer.WriteByte((byte)memory.Scope);
                writer.WriteByte((byte)memory.ValueKind);
                writer.WriteInt32(memory.Integer0);
                writer.WriteDouble(memory.Scalar0);
                writer.WriteDouble(memory.Scalar1);
                writer.WriteDouble(memory.Scalar2);
                writer.WriteDouble(memory.Scalar3);
                writer.WriteString(memory.Text0);
            }
            return writer.ToArray();
        }

        public static AIIntentSemanticIr Read(byte[] bytes)
        {
            var reader = new CanonicalReader(bytes ?? throw new ArgumentNullException(nameof(bytes)));
            if (reader.ReadUInt32() != Magic || reader.ReadInt32() != Version)
                throw new InvalidDataException("AI Semantic IR header is invalid.");
            string controllerId = reader.ReadString();
            string sourceRevision = reader.ReadString();
            var characterProgramId = new ProgramId(reader.ReadString());
            var characterProgramHash = new ProgramHash(new StableHash(reader.ReadString()));
            var perceptionSchemaHash = new StableHash(reader.ReadString());
            var root = new OperationHandle(reader.ReadInt32());
            int operationCount = ReadCount(reader, "operation");
            var operations = new AIIntentSemanticOperation[operationCount];
            for (int i = 0; i < operations.Length; i++)
            {
                operations[i] = new AIIntentSemanticOperation(
                    new OperationHandle(reader.ReadInt32()),
                    (SimulationOperationCode)reader.ReadUInt16(),
                    reader.ReadString(),
                    reader.ReadString(),
                    reader.ReadString(),
                    reader.ReadString(),
                    (AIIntentValueKind)reader.ReadByte(),
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    reader.ReadUInt64(),
                    reader.ReadDouble(),
                    reader.ReadDouble(),
                    reader.ReadDouble(),
                    reader.ReadDouble());
            }
            int edgeCount = ReadCount(reader, "edge");
            var edges = new ProgramControlFlowEdge[edgeCount];
            for (int i = 0; i < edges.Length; i++)
                edges[i] = ReadEdge(reader);
            int memoryCount = ReadCount(reader, "memory");
            var memory = new AIIntentMemoryDeclaration[memoryCount];
            for (int i = 0; i < memory.Length; i++)
            {
                memory[i] = new AIIntentMemoryDeclaration(
                    reader.ReadInt32(),
                    reader.ReadString(),
                    (AIMemoryScope)reader.ReadByte(),
                    (AIIntentValueKind)reader.ReadByte(),
                    reader.ReadInt32(),
                    reader.ReadDouble(),
                    reader.ReadDouble(),
                    reader.ReadDouble(),
                    reader.ReadDouble(),
                    reader.ReadString());
            }
            reader.RequireComplete();
            return new AIIntentSemanticIr(
                controllerId,
                sourceRevision,
                characterProgramId,
                characterProgramHash,
                perceptionSchemaHash,
                root,
                operations,
                edges,
                memory);
        }

        static void WriteEdge(CanonicalWriter writer, ProgramControlFlowEdge edge)
        {
            writer.WriteString(edge.Identity);
            writer.WriteInt32(edge.Source.Value);
            writer.WriteInt32(edge.Target.Value);
            writer.WriteString(edge.SourcePort);
            writer.WriteString(edge.TargetPort);
            writer.WriteByte((byte)edge.Kind);
            writer.WriteInt32(edge.Order);
            writer.WriteInt32(edge.Priority);
            writer.WriteByte((byte)edge.AbortPolicy);
            writer.WriteBoolean(edge.HasCondition);
            writer.WriteInt32(edge.HasCondition ? edge.Condition.Value : -1);
        }

        static ProgramControlFlowEdge ReadEdge(CanonicalReader reader)
        {
            string identity = reader.ReadString();
            var source = new OperationHandle(reader.ReadInt32());
            var target = new OperationHandle(reader.ReadInt32());
            string sourcePort = reader.ReadString();
            string targetPort = reader.ReadString();
            var kind = (ProgramControlFlowKind)reader.ReadByte();
            int order = reader.ReadInt32();
            int priority = reader.ReadInt32();
            var abort = (ProgramAbortPolicy)reader.ReadByte();
            bool hasCondition = reader.ReadBoolean();
            int condition = reader.ReadInt32();
            return new ProgramControlFlowEdge(
                identity,
                source,
                target,
                sourcePort,
                targetPort,
                kind,
                order,
                priority,
                abort,
                hasCondition,
                hasCondition ? new OperationHandle(condition) : OperationHandle.Invalid);
        }

        static int ReadCount(CanonicalReader reader, string label)
        {
            int value = reader.ReadInt32();
            if (value < 0 || value > 1_000_000)
                throw new InvalidDataException($"AI Semantic IR {label} count is invalid.");
            return value;
        }
    }
}
