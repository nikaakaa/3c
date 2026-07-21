using System;
using System.IO;

namespace ThirdPersonSimulation
{
    public readonly struct AIIntentValue
    {
        AIIntentValue(
            AIIntentValueKind kind,
            bool boolean,
            int integer,
            Float32Scalar scalar,
            Float32Vector2 vector2,
            Float32Vector3 vector3,
            string actorId,
            SimulationActionTargetSnapshot actionTarget)
        {
            Kind = kind;
            Boolean = boolean;
            Integer = integer;
            Scalar = scalar;
            Vector2 = vector2;
            Vector3 = vector3;
            ActorId = actorId ?? string.Empty;
            ActionTarget = actionTarget;
        }

        public AIIntentValueKind Kind { get; }
        public bool Boolean { get; }
        public int Integer { get; }
        public Float32Scalar Scalar { get; }
        public Float32Vector2 Vector2 { get; }
        public Float32Vector3 Vector3 { get; }
        public string ActorId { get; }
        public SimulationActionTargetSnapshot ActionTarget { get; }

        public static AIIntentValue FromBoolean(bool value) =>
            new AIIntentValue(AIIntentValueKind.Boolean, value, 0, default, default, default, string.Empty, default);
        public static AIIntentValue FromInteger(int value) =>
            new AIIntentValue(AIIntentValueKind.Integer, false, value, default, default, default, string.Empty, default);
        public static AIIntentValue FromScalar(Float32Scalar value) =>
            new AIIntentValue(AIIntentValueKind.Scalar, false, 0, value, default, default, string.Empty, default);
        public static AIIntentValue FromVector2(Float32Vector2 value) =>
            new AIIntentValue(AIIntentValueKind.Vector2, false, 0, default, value, default, string.Empty, default);
        public static AIIntentValue FromVector3(Float32Vector3 value) =>
            new AIIntentValue(AIIntentValueKind.Vector3, false, 0, default, default, value, string.Empty, default);
        public static AIIntentValue FromActorId(string value) =>
            new AIIntentValue(AIIntentValueKind.ActorId, false, 0, default, default, default, value, default);
        public static AIIntentValue FromActionTarget(SimulationActionTargetSnapshot value) =>
            new AIIntentValue(AIIntentValueKind.ActionTargetSnapshot, false, 0, default, default, default, string.Empty, value);

        public static AIIntentValue Zero(AIIntentValueKind kind)
        {
            return kind switch
            {
                AIIntentValueKind.Boolean => FromBoolean(false),
                AIIntentValueKind.Integer => FromInteger(0),
                AIIntentValueKind.Scalar => FromScalar(Float32Scalar.Zero),
                AIIntentValueKind.Vector2 => FromVector2(Float32Vector2.Zero),
                AIIntentValueKind.Vector3 => FromVector3(Float32Vector3.Zero),
                AIIntentValueKind.ActorId => FromActorId(string.Empty),
                AIIntentValueKind.ActionTargetSnapshot => FromActionTarget(SimulationActionTargetSnapshot.None),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }
    }

    public sealed class AIControllerState
    {
        readonly int[] m_Int32;
        readonly ulong[] m_UInt64;
        readonly string[] m_Identity;
        readonly AIIntentValue[] m_Memory;
        readonly ulong[] m_LastRequestGeneration;

        public AIControllerState(AIIntentProgram program)
        {
            Program = program ?? throw new ArgumentNullException(nameof(program));
            m_Int32 = new int[program.StateSlots.Count];
            m_UInt64 = new ulong[program.StateSlots.Count];
            m_Identity = new string[program.StateSlots.Count];
            for (int i = 0; i < m_Identity.Length; i++)
                m_Identity[i] = string.Empty;
            m_Memory = new AIIntentValue[program.Memory.Count];
            for (int i = 0; i < m_Memory.Length; i++)
                m_Memory[i] = DefaultValue(program.Memory[i]);
            m_LastRequestGeneration = new ulong[program.Operations.Count];
        }

        AIControllerState(
            AIIntentProgram program,
            int[] int32,
            ulong[] uint64,
            string[] identity,
            AIIntentValue[] memory,
            ulong[] lastRequestGeneration,
            ulong requestSequence)
        {
            Program = program;
            m_Int32 = int32;
            m_UInt64 = uint64;
            m_Identity = identity;
            m_Memory = memory;
            m_LastRequestGeneration = lastRequestGeneration;
            RequestSequence = requestSequence;
        }

        public AIIntentProgram Program { get; }
        public ulong RequestSequence { get; private set; }

        public AIControllerState Clone() => new AIControllerState(
            Program,
            (int[])m_Int32.Clone(),
            (ulong[])m_UInt64.Clone(),
            (string[])m_Identity.Clone(),
            (AIIntentValue[])m_Memory.Clone(),
            (ulong[])m_LastRequestGeneration.Clone(),
            RequestSequence);

        public int ReadInt32(int slot) => m_Int32[RequireSlot(slot)];
        public void WriteInt32(int slot, int value) => m_Int32[RequireSlot(slot)] = value;
        public ulong ReadUInt64(int slot) => m_UInt64[RequireSlot(slot)];
        public void WriteUInt64(int slot, ulong value) => m_UInt64[RequireSlot(slot)] = value;
        public string ReadIdentity(int slot) => m_Identity[RequireSlot(slot)] ?? string.Empty;
        public void WriteIdentity(int slot, string value) => m_Identity[RequireSlot(slot)] = value ?? string.Empty;
        public AIIntentValue ReadMemory(int address)
        {
            int index = RequireMemory(address);
            AIIntentMemoryDeclaration declaration = Program.Memory[index];
            if (declaration.Scope != AIMemoryScope.Controller)
                throw new InvalidOperationException($"AI Tick memory '{declaration.Identity}' is not part of AI Controller State.");
            return m_Memory[index];
        }

        public void WriteMemory(int address, AIIntentValue value)
        {
            int index = RequireMemory(address);
            AIIntentMemoryDeclaration declaration = Program.Memory[index];
            if (declaration.Scope != AIMemoryScope.Controller)
                throw new InvalidOperationException($"AI Tick memory '{declaration.Identity}' is not part of AI Controller State.");
            if (value.Kind != declaration.ValueKind)
                throw new InvalidOperationException($"AI memory '{Program.Memory[index].Identity}' value kind does not match its Program layout.");
            m_Memory[index] = value;
        }

        public bool TryMarkRequestEmission(OperationHandle operation, ulong generation, bool allowRepeat)
        {
            if (!operation.IsValid || operation.Value >= m_LastRequestGeneration.Length || generation == 0)
                throw new ArgumentException("AI request emission identity is invalid.");
            if (!allowRepeat && m_LastRequestGeneration[operation.Value] == generation)
                return false;
            m_LastRequestGeneration[operation.Value] = generation;
            return true;
        }

        public ulong NextRequestSequence()
        {
            RequestSequence = checked(RequestSequence + 1);
            if (RequestSequence == 0)
                throw new OverflowException("AI request sequence overflowed.");
            return RequestSequence;
        }

        public void ResetOperation(OperationExecutionDescriptor operation)
        {
            for (int i = 0; i < operation.StateSlots.Count; i++)
            {
                int slotIndex = operation.StateSlots[i];
                ProgramStateSlot slot = Program.StateSlots[slotIndex];
                if (slot.Semantic == ProgramStateSemantic.RunnableActivationGeneration)
                    continue;
                m_Int32[slotIndex] = 0;
                m_UInt64[slotIndex] = 0;
                m_Identity[slotIndex] = string.Empty;
            }
        }

        internal int[] CopyInt32() => (int[])m_Int32.Clone();
        internal ulong[] CopyUInt64() => (ulong[])m_UInt64.Clone();
        internal string[] CopyIdentity() => (string[])m_Identity.Clone();
        internal AIIntentValue[] CopyMemory() => (AIIntentValue[])m_Memory.Clone();
        internal ulong[] CopyLastRequestGeneration() => (ulong[])m_LastRequestGeneration.Clone();

        internal static AIIntentValue DefaultValue(AIIntentMemoryDeclaration declaration)
        {
            return declaration.ValueKind switch
            {
                AIIntentValueKind.Boolean => AIIntentValue.FromBoolean(declaration.Integer0 != 0),
                AIIntentValueKind.Integer => AIIntentValue.FromInteger(declaration.Integer0),
                AIIntentValueKind.Scalar => AIIntentValue.FromScalar(Float32Scalar.FromDouble(declaration.Scalar0)),
                AIIntentValueKind.Vector2 => AIIntentValue.FromVector2(new Float32Vector2(
                    Float32Scalar.FromDouble(declaration.Scalar0),
                    Float32Scalar.FromDouble(declaration.Scalar1))),
                AIIntentValueKind.Vector3 => AIIntentValue.FromVector3(new Float32Vector3(
                    Float32Scalar.FromDouble(declaration.Scalar0),
                    Float32Scalar.FromDouble(declaration.Scalar1),
                    Float32Scalar.FromDouble(declaration.Scalar2))),
                AIIntentValueKind.ActorId => AIIntentValue.FromActorId(declaration.Text0),
                AIIntentValueKind.ActionTargetSnapshot => AIIntentValue.FromActionTarget(
                    new SimulationActionTargetSnapshot(
                        declaration.Text0,
                        new Float32Vector3(
                            Float32Scalar.FromDouble(declaration.Scalar0),
                            Float32Scalar.FromDouble(declaration.Scalar1),
                            Float32Scalar.FromDouble(declaration.Scalar2)),
                        new Float32Yaw(Float32Scalar.FromDouble(declaration.Scalar3)))),
                _ => throw new InvalidOperationException($"AI memory default kind '{declaration.ValueKind}' is unsupported.")
            };
        }

        internal static AIControllerState Restore(
            AIIntentProgram program,
            int[] int32,
            ulong[] uint64,
            string[] identity,
            AIIntentValue[] memory,
            ulong[] requestGeneration,
            ulong requestSequence)
        {
            if (int32.Length != program.StateSlots.Count || uint64.Length != program.StateSlots.Count ||
                identity.Length != program.StateSlots.Count || memory.Length != program.Memory.Count ||
                requestGeneration.Length != program.Operations.Count)
            {
                throw new InvalidDataException("AI Controller State layout does not match its Program.");
            }
            for (int i = 0; i < memory.Length; i++)
            {
                if (memory[i].Kind != program.Memory[i].ValueKind)
                    throw new InvalidDataException("AI Controller State memory value kind does not match its Program.");
            }
            return new AIControllerState(program, int32, uint64, identity, memory, requestGeneration, requestSequence);
        }

        int RequireSlot(int slot)
        {
            if (slot < 0 || slot >= m_Int32.Length)
                throw new ArgumentOutOfRangeException(nameof(slot));
            return slot;
        }

        int RequireMemory(int address)
        {
            if (address < 0 || address >= m_Memory.Length)
                throw new ArgumentOutOfRangeException(nameof(address));
            return address;
        }
    }

    public static class AIControllerStateCodec
    {
        public const string SchemaId = "float32-ai-controller-state";
        public const int SchemaVersion = 2;
        const uint Magic = 0x53434941;

        public static byte[] Write(AIControllerState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(Magic);
            writer.WriteInt32(SchemaVersion);
            writer.WriteString(state.Program.ProgramId.Value);
            writer.WriteString(state.Program.ProgramHash.ToString());
            writer.WriteString(state.Program.LayoutHash.ToString());
            writer.WriteUInt64(state.RequestSequence);
            int[] int32 = state.CopyInt32();
            ulong[] uint64 = state.CopyUInt64();
            string[] identity = state.CopyIdentity();
            writer.WriteInt32(int32.Length);
            for (int i = 0; i < int32.Length; i++)
            {
                writer.WriteInt32(int32[i]);
                writer.WriteUInt64(uint64[i]);
                writer.WriteString(identity[i]);
            }
            AIIntentValue[] memory = state.CopyMemory();
            int persistentCount = 0;
            for (int i = 0; i < state.Program.Memory.Count; i++)
            {
                if (state.Program.Memory[i].Scope == AIMemoryScope.Controller)
                    persistentCount++;
            }
            writer.WriteInt32(persistentCount);
            for (int i = 0; i < state.Program.Memory.Count; i++)
            {
                if (state.Program.Memory[i].Scope != AIMemoryScope.Controller)
                    continue;
                writer.WriteInt32(i);
                WriteValue(writer, memory[i]);
            }
            ulong[] emissions = state.CopyLastRequestGeneration();
            writer.WriteInt32(emissions.Length);
            for (int i = 0; i < emissions.Length; i++)
                writer.WriteUInt64(emissions[i]);
            return writer.ToArray();
        }

        public static AIControllerState Read(AIIntentProgram program, byte[] bytes)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            var reader = new CanonicalReader(bytes ?? throw new ArgumentNullException(nameof(bytes)));
            if (reader.ReadUInt32() != Magic || reader.ReadInt32() != SchemaVersion ||
                !string.Equals(reader.ReadString(), program.ProgramId.Value, StringComparison.Ordinal) ||
                !string.Equals(reader.ReadString(), program.ProgramHash.ToString(), StringComparison.Ordinal) ||
                !string.Equals(reader.ReadString(), program.LayoutHash.ToString(), StringComparison.Ordinal))
            {
                throw new InvalidDataException("AI Controller State identity does not match its Program.");
            }
            ulong requestSequence = reader.ReadUInt64();
            int slotCount = ReadCount(reader, program.StateSlots.Count, "slot");
            var int32 = new int[slotCount];
            var uint64 = new ulong[slotCount];
            var identity = new string[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                int32[i] = reader.ReadInt32();
                uint64[i] = reader.ReadUInt64();
                identity[i] = reader.ReadString();
            }
            var memory = new AIIntentValue[program.Memory.Count];
            int expectedPersistentCount = 0;
            for (int i = 0; i < program.Memory.Count; i++)
            {
                memory[i] = AIControllerState.DefaultValue(program.Memory[i]);
                if (program.Memory[i].Scope == AIMemoryScope.Controller)
                    expectedPersistentCount++;
            }
            int memoryCount = ReadCount(reader, expectedPersistentCount, "persistent memory");
            int previousAddress = -1;
            for (int i = 0; i < memoryCount; i++)
            {
                int address = reader.ReadInt32();
                if (address <= previousAddress || address < 0 || address >= program.Memory.Count ||
                    program.Memory[address].Scope != AIMemoryScope.Controller)
                {
                    throw new InvalidDataException("AI Controller State persistent memory addresses are not canonical.");
                }
                memory[address] = ReadValue(reader);
                previousAddress = address;
            }
            int emissionCount = ReadCount(reader, program.Operations.Count, "request emission");
            var emissions = new ulong[emissionCount];
            for (int i = 0; i < emissionCount; i++)
                emissions[i] = reader.ReadUInt64();
            reader.RequireComplete();
            return AIControllerState.Restore(program, int32, uint64, identity, memory, emissions, requestSequence);
        }

        static void WriteValue(CanonicalWriter writer, AIIntentValue value)
        {
            writer.WriteByte((byte)value.Kind);
            switch (value.Kind)
            {
                case AIIntentValueKind.Boolean: writer.WriteBoolean(value.Boolean); break;
                case AIIntentValueKind.Integer: writer.WriteInt32(value.Integer); break;
                case AIIntentValueKind.Scalar: writer.WriteScalar(value.Scalar); break;
                case AIIntentValueKind.Vector2: writer.WriteVector2(value.Vector2); break;
                case AIIntentValueKind.Vector3: writer.WriteVector3(value.Vector3); break;
                case AIIntentValueKind.ActorId: writer.WriteString(value.ActorId); break;
                case AIIntentValueKind.ActionTargetSnapshot: writer.WriteBytes(SimulationActionTargetSnapshotCodec.Write(value.ActionTarget)); break;
                default: throw new InvalidDataException($"AI memory value kind '{value.Kind}' is unsupported.");
            }
        }

        static AIIntentValue ReadValue(CanonicalReader reader)
        {
            var kind = (AIIntentValueKind)reader.ReadByte();
            return kind switch
            {
                AIIntentValueKind.Boolean => AIIntentValue.FromBoolean(reader.ReadBoolean()),
                AIIntentValueKind.Integer => AIIntentValue.FromInteger(reader.ReadInt32()),
                AIIntentValueKind.Scalar => AIIntentValue.FromScalar(reader.ReadScalar()),
                AIIntentValueKind.Vector2 => AIIntentValue.FromVector2(reader.ReadVector2()),
                AIIntentValueKind.Vector3 => AIIntentValue.FromVector3(reader.ReadVector3()),
                AIIntentValueKind.ActorId => AIIntentValue.FromActorId(reader.ReadString()),
                AIIntentValueKind.ActionTargetSnapshot => AIIntentValue.FromActionTarget(SimulationActionTargetSnapshotCodec.Read(reader.ReadBytes())),
                _ => throw new InvalidDataException($"AI memory value kind '{kind}' is unsupported.")
            };
        }

        static int ReadCount(CanonicalReader reader, int expected, string label)
        {
            int value = reader.ReadInt32();
            if (value != expected)
                throw new InvalidDataException($"AI Controller State {label} count does not match its Program.");
            return value;
        }
    }
}
