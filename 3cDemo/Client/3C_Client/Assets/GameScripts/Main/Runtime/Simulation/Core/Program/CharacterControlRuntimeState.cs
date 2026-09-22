using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace ThirdPersonSimulation
{
    public sealed class CharacterControlStateSchema
    {
        readonly ReadOnlyCollection<CharacterControlStateFieldDescriptor> m_Fields;
        readonly Dictionary<CharacterControlStateFieldId, int> m_Indexes;
        readonly Dictionary<CharacterControlStateFieldId, CharacterControlStateValueKind> m_Kinds;

        public CharacterControlStateSchema(CharacterControlModuleContract contract)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            var fields = new List<CharacterControlStateFieldDescriptor>(contract.StateFields.Count);
            m_Indexes = new Dictionary<CharacterControlStateFieldId, int>();
            m_Kinds = new Dictionary<CharacterControlStateFieldId, CharacterControlStateValueKind>();
            var hashParts = new List<string>
            {
                "character-control-state-schema/2",
                contract.ModuleId.Value,
                contract.SemanticVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)
            };
            for (int i = 0; i < contract.StateFields.Count; i++)
            {
                CharacterControlStateFieldDescriptor field = contract.StateFields[i];
                if (!IsSupported(field.ValueKind))
                    throw new InvalidDataException($"Character control state field '{field.Id}' has unsupported kind '{field.ValueKind}'.");
                fields.Add(field);
                if (!m_Indexes.TryAdd(field.Id, i))
                    throw new InvalidDataException($"Character control state field '{field.Id}' is duplicated.");
                m_Kinds.Add(field.Id, field.ValueKind);
                hashParts.Add(field.Id.Value);
                hashParts.Add(((int)field.ValueKind).ToString(System.Globalization.CultureInfo.InvariantCulture));
                hashParts.Add(((int)field.Semantic).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            m_Fields = fields.AsReadOnly();
            ModuleId = contract.ModuleId;
            SemanticVersion = contract.SemanticVersion;
            SchemaHash = StableHash.Compute(hashParts.ToArray());
        }

        internal CharacterControlStateSchema(
            CharacterControlModuleId moduleId,
            int semanticVersion,
            IReadOnlyList<CharacterControlStateFieldDescriptor> fields)
        {
            if (!moduleId.IsValid || semanticVersion <= 0 || fields == null)
                throw new ArgumentException("Character control state schema identity is incomplete.");
            var copied = new List<CharacterControlStateFieldDescriptor>(fields.Count);
            m_Indexes = new Dictionary<CharacterControlStateFieldId, int>();
            m_Kinds = new Dictionary<CharacterControlStateFieldId, CharacterControlStateValueKind>();
            var hashParts = new List<string>
            {
                "character-control-state-schema/2",
                moduleId.Value,
                semanticVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)
            };
            for (int i = 0; i < fields.Count; i++)
            {
                CharacterControlStateFieldDescriptor field = fields[i]
                    ?? throw new ArgumentException("Character control state schema contains a missing field.", nameof(fields));
                if (!IsSupported(field.ValueKind))
                    throw new InvalidDataException($"Character control state field '{field.Id}' has unsupported kind '{field.ValueKind}'.");
                copied.Add(field);
                if (!m_Indexes.TryAdd(field.Id, i))
                    throw new InvalidDataException($"Character control state field '{field.Id}' is duplicated.");
                m_Kinds.Add(field.Id, field.ValueKind);
                hashParts.Add(field.Id.Value);
                hashParts.Add(((int)field.ValueKind).ToString(System.Globalization.CultureInfo.InvariantCulture));
                hashParts.Add(((int)field.Semantic).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            m_Fields = copied.AsReadOnly();
            ModuleId = moduleId;
            SemanticVersion = semanticVersion;
            SchemaHash = StableHash.Compute(hashParts.ToArray());
        }

        public CharacterControlModuleId ModuleId { get; }
        public int SemanticVersion { get; }
        public IReadOnlyList<CharacterControlStateFieldDescriptor> Fields => m_Fields;
        public int FieldCount => m_Fields.Count;
        public StableHash SchemaHash { get; }

        public int RequireIndex(CharacterControlStateFieldId field) =>
            m_Indexes.TryGetValue(field, out int index)
                ? index
                : throw new InvalidOperationException($"Character control state field '{field}' is not declared.");

        public CharacterControlStateValueKind RequireKind(CharacterControlStateFieldId field) =>
            m_Kinds.TryGetValue(field, out CharacterControlStateValueKind kind)
                ? kind
                : throw new InvalidOperationException($"Character control state field '{field}' is not declared.");

        static bool IsSupported(CharacterControlStateValueKind kind) =>
            kind == CharacterControlStateValueKind.Identity ||
            kind == CharacterControlStateValueKind.Boolean ||
            kind == CharacterControlStateValueKind.Int32 ||
            kind == CharacterControlStateValueKind.UInt64;
    }

    public readonly struct CharacterControlStateValue
    {
        CharacterControlStateValue(
            CharacterControlStateValueKind kind,
            bool boolean,
            int int32,
            ulong uint64,
            string identity)
        {
            Kind = kind;
            Boolean = boolean;
            Int32 = int32;
            UInt64 = uint64;
            Identity = identity ?? string.Empty;
        }

        public CharacterControlStateValueKind Kind { get; }
        public bool Boolean { get; }
        public int Int32 { get; }
        public ulong UInt64 { get; }
        public string Identity { get; }

        public bool Equals(CharacterControlStateValue other) =>
            Kind == other.Kind &&
            Boolean == other.Boolean &&
            Int32 == other.Int32 &&
            UInt64 == other.UInt64 &&
            (Kind != CharacterControlStateValueKind.Identity ||
                string.Equals(Identity, other.Identity, StringComparison.Ordinal));

        public static CharacterControlStateValue FromBoolean(bool value) =>
            new CharacterControlStateValue(CharacterControlStateValueKind.Boolean, value, 0, 0, string.Empty);

        public static CharacterControlStateValue FromInt32(int value) =>
            new CharacterControlStateValue(CharacterControlStateValueKind.Int32, false, value, 0, string.Empty);

        public static CharacterControlStateValue FromUInt64(ulong value) =>
            new CharacterControlStateValue(CharacterControlStateValueKind.UInt64, false, 0, value, string.Empty);

        public static CharacterControlStateValue FromIdentity(string value) =>
            new CharacterControlStateValue(CharacterControlStateValueKind.Identity, false, 0, 0, value);

        public static CharacterControlStateValue Default(CharacterControlStateValueKind kind)
        {
            return kind switch
            {
                CharacterControlStateValueKind.Boolean => FromBoolean(false),
                CharacterControlStateValueKind.Int32 => FromInt32(0),
                CharacterControlStateValueKind.UInt64 => FromUInt64(0),
                CharacterControlStateValueKind.Identity => FromIdentity(string.Empty),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }
    }

    public sealed class CharacterControlRuntimeState
    {
        readonly ReadOnlyCollection<CharacterControlStateValue> m_Values;
        readonly CharacterControlStateValue[] m_ValueArray;

        public CharacterControlRuntimeState(
            CharacterControlStateSchema schema,
            ulong lastCompletedTick,
            IEnumerable<CharacterControlStateValue> values)
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            var copied = values == null
                ? new CharacterControlStateValue[0]
                : values is CharacterControlStateValue[] valueArray
                    ? (CharacterControlStateValue[])valueArray.Clone()
                    : new List<CharacterControlStateValue>(values).ToArray();
            if (copied.Length != schema.FieldCount)
                throw new ArgumentException("Character control runtime state values do not match its schema.", nameof(values));
            for (int i = 0; i < copied.Length; i++)
            {
                if (copied[i].Kind != schema.Fields[i].ValueKind)
                    throw new ArgumentException($"Character control runtime state field '{schema.Fields[i].Id}' kind does not match its schema.", nameof(values));
            }
            ModuleId = schema.ModuleId;
            SemanticVersion = schema.SemanticVersion;
            LastCompletedTick = lastCompletedTick;
            m_ValueArray = copied;
            m_Values = Array.AsReadOnly(copied);
            StateHash = ComputeHash(schema, lastCompletedTick, copied);
        }

        public CharacterControlStateSchema Schema { get; }
        public CharacterControlModuleId ModuleId { get; }
        public int SemanticVersion { get; }
        public ulong LastCompletedTick { get; }
        public IReadOnlyList<CharacterControlStateValue> Values => m_Values;
        public StableHash StateHash { get; }

        static StableHash ComputeHash(
            CharacterControlStateSchema schema,
            ulong lastCompletedTick,
            IReadOnlyList<CharacterControlStateValue> values)
        {
            var parts = new List<string>
            {
                "character-control-runtime-state/2",
                schema.SchemaHash.Value,
                lastCompletedTick.ToString(System.Globalization.CultureInfo.InvariantCulture)
            };
            for (int i = 0; i < values.Count; i++)
            {
                CharacterControlStateValue value = values[i];
                parts.Add(((int)value.Kind).ToString(System.Globalization.CultureInfo.InvariantCulture));
                parts.Add(value.Boolean ? "1" : "0");
                parts.Add(value.Int32.ToString(System.Globalization.CultureInfo.InvariantCulture));
                parts.Add(value.UInt64.ToString(System.Globalization.CultureInfo.InvariantCulture));
                parts.Add(value.Identity);
            }
            return StableHash.Compute(parts.ToArray());
        }

        internal CharacterControlRuntimeState(
            CharacterControlStateSchema schema,
            ulong lastCompletedTick,
            CharacterControlStateValue[] values,
            StableHash stateHash,
            ReadOnlyCollection<CharacterControlStateValue> valueView = null)
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (values.Length != schema.FieldCount)
                throw new ArgumentException("Character control runtime state values do not match its schema.", nameof(values));
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].Kind != schema.Fields[i].ValueKind)
                    throw new ArgumentException($"Character control runtime state field '{schema.Fields[i].Id}' kind does not match its schema.", nameof(values));
            }
            ModuleId = schema.ModuleId;
            SemanticVersion = schema.SemanticVersion;
            LastCompletedTick = lastCompletedTick;
            m_ValueArray = values;
            m_Values = valueView ?? Array.AsReadOnly(values);
            StateHash = stateHash;
        }

        internal CharacterControlStateValue[] ValueArray => m_ValueArray;
        internal ReadOnlyCollection<CharacterControlStateValue> ValuesView => m_Values;

        public CharacterControlStateValue Get(CharacterControlStateFieldId field) =>
            m_Values[Schema.RequireIndex(field)];

        public void RequireContract(CharacterControlModuleContract contract)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            if (contract.ModuleId != ModuleId || contract.SemanticVersion != SemanticVersion)
                throw new InvalidOperationException($"Character control runtime state '{ModuleId}/{SemanticVersion}' does not match module contract '{contract.ModuleId}/{contract.SemanticVersion}'.");
            if (!contract.StateSchema.SchemaHash.Equals(Schema.SchemaHash))
                throw new InvalidOperationException($"Character control runtime state schema does not match module '{ModuleId}'.");
        }

        public void RequireBinding(CharacterControlRuntimeBinding binding)
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));
            if (binding.ModuleId != ModuleId || binding.SemanticVersion != SemanticVersion)
                throw new InvalidOperationException($"Character control runtime state '{ModuleId}/{SemanticVersion}' does not match binding '{binding.ModuleId}/{binding.SemanticVersion}'.");
        }

        public static CharacterControlRuntimeState CreateInitial(
            CharacterControlRuntimeBinding binding,
            CharacterControlModuleContract contract)
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            binding.RequireContract(contract);
            CharacterControlStateSchema schema = contract.StateSchema;
            var values = new CharacterControlStateValue[schema.FieldCount];
            for (int i = 0; i < values.Length; i++)
                values[i] = CharacterControlStateValue.Default(schema.Fields[i].ValueKind);
            return new CharacterControlRuntimeState(schema, 0, values);
        }

    }

    public enum CharacterControlRuntimeStateTransactionStatus : byte
    {
        Active = 1,
        Aborted = 2
    }

    public sealed class CharacterControlRuntimeStateTransaction : IDisposable
    {
        CharacterControlStateSchema m_Schema;
        SimulationTick m_Tick;
        readonly List<CharacterControlStateValue> m_Values;
        byte[] m_HashBuffer = Array.Empty<byte>();
        CharacterControlRuntimeStateTransactionStatus m_Status;
        bool m_ValuesChanged;

        public CharacterControlRuntimeStateTransaction()
        {
            m_Values = new List<CharacterControlStateValue>();
            m_Status = CharacterControlRuntimeStateTransactionStatus.Aborted;
        }

        public CharacterControlRuntimeStateTransaction Restart(
            CharacterControlRuntimeState state,
            CharacterControlStateSchema schema,
            SimulationTick tick)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));
            if (!state.Schema.SchemaHash.Equals(schema.SchemaHash))
                throw new InvalidOperationException("Character control runtime state schema is stale.");
            if (state.ModuleId != schema.ModuleId || state.SemanticVersion != schema.SemanticVersion)
                throw new InvalidOperationException("Character control runtime state identity does not match its schema.");
            if (!tick.IsValid || tick.Value != checked(state.LastCompletedTick + 1))
                throw new ArgumentException("Character control runtime state transaction Tick is not the next Tick.", nameof(tick));
            m_Schema = schema;
            m_Tick = tick;
            BaseState = state;
            m_Values.Clear();
            if (m_Values.Capacity < state.Values.Count)
                m_Values.Capacity = state.Values.Count;
            m_Values.AddRange(state.Values);
            m_ValuesChanged = false;
            m_Status = CharacterControlRuntimeStateTransactionStatus.Active;
            return this;
        }

        public CharacterControlRuntimeStateTransactionStatus Status => m_Status;
        public CharacterControlRuntimeState BaseState { get; private set; }
        public SimulationTick Tick => m_Tick;

        public CharacterControlStateValue Get(CharacterControlStateFieldId field)
        {
            RequireActive();
            return m_Values[m_Schema.RequireIndex(field)];
        }

        public void Set(CharacterControlStateFieldId field, CharacterControlStateValue value)
        {
            RequireActive();
            int index = m_Schema.RequireIndex(field);
            if (value.Kind != m_Schema.Fields[index].ValueKind)
                throw new ArgumentException($"Character control state field '{field}' kind does not match its schema.", nameof(value));
            if (m_Values[index].Equals(value))
                return;
            m_Values[index] = value;
            m_ValuesChanged = true;
        }

        public CharacterControlRuntimeState Capture()
        {
            RequireActive();
            CharacterControlStateValue[] values = m_ValuesChanged
                ? m_Values.ToArray()
                : BaseState.ValueArray;
            StableHash stateHash = ComputeHash();
            return new CharacterControlRuntimeState(
                m_Schema,
                m_Tick.Value,
                values,
                stateHash,
                m_ValuesChanged ? null : BaseState.ValuesView);
        }

        public void Restore(CharacterControlRuntimeState state)
        {
            RequireActive();
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (!state.Schema.SchemaHash.Equals(m_Schema.SchemaHash) ||
                state.LastCompletedTick != m_Tick.Value)
                throw new InvalidOperationException("Character control runtime state restore identity does not match the active transaction.");
            m_Values.Clear();
            m_Values.AddRange(state.Values);
            m_ValuesChanged = !MatchesBaseValues(state.Values);
        }

        public void Abort()
        {
            if (m_Status == CharacterControlRuntimeStateTransactionStatus.Active)
                m_Status = CharacterControlRuntimeStateTransactionStatus.Aborted;
        }

        public void Dispose() => Abort();

        void RequireActive()
        {
            if (m_Status != CharacterControlRuntimeStateTransactionStatus.Active)
                throw new InvalidOperationException("Character control runtime state transaction is not active.");
        }

        bool MatchesBaseValues(IReadOnlyList<CharacterControlStateValue> values)
        {
            if (values.Count != BaseState.Values.Count)
                return false;
            for (int i = 0; i < values.Count; i++)
            {
                if (!values[i].Equals(BaseState.Values[i]))
                    return false;
            }
            return true;
        }

        StableHash ComputeHash()
        {
            int length = 0;
            AppendString(ref length, "character-control-runtime-state/2");
            AppendSeparator(ref length);
            AppendString(ref length, m_Schema.SchemaHash.Value);
            AppendSeparator(ref length);
            AppendUInt64(ref length, m_Tick.Value);
            for (int i = 0; i < m_Values.Count; i++)
            {
                CharacterControlStateValue value = m_Values[i];
                AppendSeparator(ref length);
                AppendInt32(ref length, (int)value.Kind);
                AppendSeparator(ref length);
                AppendAscii(ref length, value.Boolean ? '1' : '0');
                AppendSeparator(ref length);
                AppendInt32(ref length, value.Int32);
                AppendSeparator(ref length);
                AppendUInt64(ref length, value.UInt64);
                AppendSeparator(ref length);
                AppendString(ref length, value.Identity);
            }
            return SimulationCanonicalPayloadHash.Compute(m_HashBuffer.AsSpan(0, length));
        }

        void AppendSeparator(ref int length) => AppendAscii(ref length, '\u001f');

        void AppendAscii(ref int length, char value)
        {
            EnsureHashCapacity(length + 1);
            m_HashBuffer[length++] = (byte)value;
        }

        void AppendInt32(ref int length, int value)
        {
            if (value < 0)
            {
                AppendAscii(ref length, '-');
                AppendUInt64(ref length, (ulong)(-(long)value));
            }
            else
            {
                AppendUInt64(ref length, (ulong)value);
            }
        }

        void AppendUInt64(ref int length, ulong value)
        {
            if (value == 0)
            {
                AppendAscii(ref length, '0');
                return;
            }
            int end = length + 20;
            EnsureHashCapacity(end);
            while (value != 0)
            {
                m_HashBuffer[--end] = (byte)('0' + value % 10);
                value /= 10;
            }
            int count = length + 20 - end;
            if (end != length)
                Array.Copy(m_HashBuffer, end, m_HashBuffer, length, count);
            length += count;
        }

        void AppendString(ref int length, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;
            EnsureHashCapacity(length + Encoding.UTF8.GetMaxByteCount(value.Length));
            length += Encoding.UTF8.GetBytes(value, 0, value.Length, m_HashBuffer, length);
        }

        void EnsureHashCapacity(int required)
        {
            if (required <= m_HashBuffer.Length)
                return;
            int capacity = Math.Max(required, m_HashBuffer.Length * 2);
            Array.Resize(ref m_HashBuffer, capacity);
        }
    }

    public static class CharacterControlRuntimeStateCodec
    {
        const uint Magic = 0x54535243;
        const int Version = 2;
        public const string CodecIdentity = "character-control-runtime-state/v2";

        public static void WriteLengthPrefixed(CanonicalWriter writer, CharacterControlRuntimeState state)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            long prefixPosition = writer.BeginLengthPrefixedBlock();
            WriteCanonical(writer, state);
            writer.EndLengthPrefixedBlock(prefixPosition);
        }

        public static CharacterControlRuntimeState Read(ArraySegment<byte> bytes)
        {
            return Read(bytes, ReadSchema(bytes));
        }

        public static CharacterControlRuntimeState Read(
            ArraySegment<byte> bytes,
            CharacterControlStateSchema schema)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));
            var reader = new CanonicalReader(bytes);
            if (reader.ReadUInt32() != Magic || reader.ReadInt32() != Version ||
                !string.Equals(reader.ReadString(), CodecIdentity, StringComparison.Ordinal))
                throw new InvalidDataException("Character control runtime state header is invalid.");
            var moduleId = new CharacterControlModuleId(reader.ReadString());
            int semanticVersion = reader.ReadInt32();
            var schemaHash = new StableHash(reader.ReadString());
            ulong lastCompletedTick = reader.ReadUInt64();
            int count = reader.ReadInt32();
            if (moduleId != schema.ModuleId || semanticVersion != schema.SemanticVersion ||
                !schemaHash.Equals(schema.SchemaHash) || count < 0 || count != schema.FieldCount)
                throw new InvalidDataException("Character control runtime state schema binding is stale or mismatched.");
            var values = new CharacterControlStateValue[count];
            for (int i = 0; i < values.Length; i++)
            {
                var field = new CharacterControlStateFieldId(reader.ReadString());
                CharacterControlStateValueKind kind = ReadValueKind(reader.ReadByte());
                CharacterControlStateSemantic semantic = ReadSemantic(reader.ReadUInt16());
                if (field != schema.Fields[i].Id || kind != schema.Fields[i].ValueKind || semantic != schema.Fields[i].Semantic)
                    throw new InvalidDataException($"Character control runtime state field '{field}' does not match schema index '{i}'.");
                values[i] = ReadValue(reader, kind);
            }
            var expectedHash = new StableHash(reader.ReadString());
            reader.RequireComplete();
            var result = new CharacterControlRuntimeState(schema, lastCompletedTick, values);
            if (!result.StateHash.Equals(expectedHash))
                throw new InvalidDataException("Character control runtime state hash is invalid.");
            using var writer = new CanonicalWriter();
            WriteCanonical(writer, result);
            if (!writer.ContentEquals(bytes.AsSpan()))
                throw new InvalidDataException("Character control runtime state is not canonical.");
            return result;
        }

        static CharacterControlStateSchema ReadSchema(ArraySegment<byte> bytes)
        {
            var reader = new CanonicalReader(bytes);
            if (reader.ReadUInt32() != Magic || reader.ReadInt32() != Version ||
                !string.Equals(reader.ReadString(), CodecIdentity, StringComparison.Ordinal))
                throw new InvalidDataException("Character control runtime state header is invalid.");
            var moduleId = new CharacterControlModuleId(reader.ReadString());
            int semanticVersion = reader.ReadInt32();
            var schemaHash = new StableHash(reader.ReadString());
            reader.ReadUInt64();
            int count = reader.ReadInt32();
            if (count < 0 || count > 1024)
                throw new InvalidDataException($"Character control runtime state field count '{count}' is invalid.");
            var fields = new CharacterControlStateFieldDescriptor[count];
            for (int i = 0; i < fields.Length; i++)
            {
                var fieldId = new CharacterControlStateFieldId(reader.ReadString());
                CharacterControlStateValueKind kind = ReadValueKind(reader.ReadByte());
                CharacterControlStateSemantic semantic = ReadSemantic(reader.ReadUInt16());
                ReadValue(reader, kind);
                fields[i] = new CharacterControlStateFieldDescriptor(fieldId, kind, semantic);
            }
            reader.ReadString();
            reader.RequireComplete();
            var schema = new CharacterControlStateSchema(moduleId, semanticVersion, fields);
            if (!schema.SchemaHash.Equals(schemaHash))
                throw new InvalidDataException("Character control runtime state schema hash is invalid.");
            return schema;
        }

        static void WriteCanonical(CanonicalWriter writer, CharacterControlRuntimeState state)
        {
            writer.WriteUInt32(Magic);
            writer.WriteInt32(Version);
            writer.WriteString(CodecIdentity);
            writer.WriteString(state.ModuleId.Value);
            writer.WriteInt32(state.SemanticVersion);
            writer.WriteString(state.Schema.SchemaHash.Value);
            writer.WriteUInt64(state.LastCompletedTick);
            writer.WriteInt32(state.Values.Count);
            for (int i = 0; i < state.Values.Count; i++)
            {
                CharacterControlStateValue value = state.Values[i];
                writer.WriteString(state.Schema.Fields[i].Id.Value);
                writer.WriteByte((byte)value.Kind);
                writer.WriteUInt16((ushort)state.Schema.Fields[i].Semantic);
                WriteValue(writer, value);
            }
            writer.WriteString(state.StateHash.Value);
        }

        static void WriteValue(CanonicalWriter writer, CharacterControlStateValue value)
        {
            switch (value.Kind)
            {
                case CharacterControlStateValueKind.Boolean: writer.WriteBoolean(value.Boolean); break;
                case CharacterControlStateValueKind.Int32: writer.WriteInt32(value.Int32); break;
                case CharacterControlStateValueKind.UInt64: writer.WriteUInt64(value.UInt64); break;
                case CharacterControlStateValueKind.Identity: writer.WriteString(value.Identity); break;
                default: throw new InvalidDataException($"Unsupported character control runtime state value kind '{value.Kind}'.");
            }
        }

        static CharacterControlStateValue ReadValue(CanonicalReader reader, CharacterControlStateValueKind kind)
        {
            return kind switch
            {
                CharacterControlStateValueKind.Boolean => CharacterControlStateValue.FromBoolean(reader.ReadBoolean()),
                CharacterControlStateValueKind.Int32 => CharacterControlStateValue.FromInt32(reader.ReadInt32()),
                CharacterControlStateValueKind.UInt64 => CharacterControlStateValue.FromUInt64(reader.ReadUInt64()),
                CharacterControlStateValueKind.Identity => CharacterControlStateValue.FromIdentity(reader.ReadString()),
                _ => throw new InvalidDataException($"Unsupported character control runtime state value kind '{kind}'.")
            };
        }

        static CharacterControlStateValueKind ReadValueKind(byte value)
        {
            var result = (CharacterControlStateValueKind)value;
            if (result is not (CharacterControlStateValueKind.Boolean or CharacterControlStateValueKind.Int32 or
                CharacterControlStateValueKind.UInt64 or CharacterControlStateValueKind.Identity))
                throw new InvalidDataException($"Character control runtime state control state value kind '{value}' is invalid.");
            return result;
        }

        static CharacterControlStateSemantic ReadSemantic(ushort value)
        {
            var result = (CharacterControlStateSemantic)value;
            if (result is not (CharacterControlStateSemantic.ActiveState or CharacterControlStateSemantic.EnteredTick or
                CharacterControlStateSemantic.Transition or CharacterControlStateSemantic.StateValue))
                throw new InvalidDataException($"Character control runtime state control state semantic '{value}' is invalid.");
            return result;
        }
    }
}
