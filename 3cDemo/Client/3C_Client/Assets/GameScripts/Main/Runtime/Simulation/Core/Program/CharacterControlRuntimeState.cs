using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace ThirdPersonSimulation
{
    public sealed class CharacterControlStateSchema
    {
        readonly ReadOnlyCollection<CharacterControlStateFieldDescriptor> m_Fields;
        readonly Dictionary<CharacterControlStateFieldId, int> m_Indexes;
        readonly Dictionary<CharacterControlStateFieldId, ProgramStateValueKind> m_Kinds;

        public CharacterControlStateSchema(CharacterControlModuleContract contract)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            var fields = new List<CharacterControlStateFieldDescriptor>(contract.StateFields.Count);
            m_Indexes = new Dictionary<CharacterControlStateFieldId, int>();
            m_Kinds = new Dictionary<CharacterControlStateFieldId, ProgramStateValueKind>();
            var hashParts = new List<string>
            {
                "character-control-state-schema/1",
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

        public CharacterControlModuleId ModuleId { get; }
        public int SemanticVersion { get; }
        public IReadOnlyList<CharacterControlStateFieldDescriptor> Fields => m_Fields;
        public int FieldCount => m_Fields.Count;
        public StableHash SchemaHash { get; }

        public int RequireIndex(CharacterControlStateFieldId field) =>
            m_Indexes.TryGetValue(field, out int index)
                ? index
                : throw new InvalidOperationException($"Character control state field '{field}' is not declared.");

        public ProgramStateValueKind RequireKind(CharacterControlStateFieldId field) =>
            m_Kinds.TryGetValue(field, out ProgramStateValueKind kind)
                ? kind
                : throw new InvalidOperationException($"Character control state field '{field}' is not declared.");

        static bool IsSupported(ProgramStateValueKind kind) =>
            kind == ProgramStateValueKind.Identity ||
            kind == ProgramStateValueKind.Boolean ||
            kind == ProgramStateValueKind.Int32 ||
            kind == ProgramStateValueKind.UInt64;
    }

    public readonly struct CharacterControlStateValue
    {
        CharacterControlStateValue(
            ProgramStateValueKind kind,
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

        public ProgramStateValueKind Kind { get; }
        public bool Boolean { get; }
        public int Int32 { get; }
        public ulong UInt64 { get; }
        public string Identity { get; }

        public static CharacterControlStateValue FromBoolean(bool value) =>
            new CharacterControlStateValue(ProgramStateValueKind.Boolean, value, 0, 0, string.Empty);

        public static CharacterControlStateValue FromInt32(int value) =>
            new CharacterControlStateValue(ProgramStateValueKind.Int32, false, value, 0, string.Empty);

        public static CharacterControlStateValue FromUInt64(ulong value) =>
            new CharacterControlStateValue(ProgramStateValueKind.UInt64, false, 0, value, string.Empty);

        public static CharacterControlStateValue FromIdentity(string value) =>
            new CharacterControlStateValue(ProgramStateValueKind.Identity, false, 0, 0, value);

        public static CharacterControlStateValue Default(ProgramStateValueKind kind)
        {
            return kind switch
            {
                ProgramStateValueKind.Boolean => FromBoolean(false),
                ProgramStateValueKind.Int32 => FromInt32(0),
                ProgramStateValueKind.UInt64 => FromUInt64(0),
                ProgramStateValueKind.Identity => FromIdentity(string.Empty),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }
    }

    public sealed class CharacterControlRuntimeState
    {
        readonly ReadOnlyCollection<CharacterControlStateValue> m_Values;

        public CharacterControlRuntimeState(
            CharacterControlStateSchema schema,
            ulong lastCompletedTick,
            IEnumerable<CharacterControlStateValue> values)
        {
            Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            var copied = values == null
                ? new List<CharacterControlStateValue>()
                : new List<CharacterControlStateValue>(values);
            if (copied.Count != schema.FieldCount)
                throw new ArgumentException("Character control runtime state values do not match its schema.", nameof(values));
            for (int i = 0; i < copied.Count; i++)
            {
                if (copied[i].Kind != schema.Fields[i].ValueKind)
                    throw new ArgumentException($"Character control runtime state field '{schema.Fields[i].Id}' kind does not match its schema.", nameof(values));
            }
            ModuleId = schema.ModuleId;
            SemanticVersion = schema.SemanticVersion;
            LastCompletedTick = lastCompletedTick;
            m_Values = copied.AsReadOnly();
            StateHash = ComputeHash(schema, lastCompletedTick, copied);
        }

        public CharacterControlStateSchema Schema { get; }
        public CharacterControlModuleId ModuleId { get; }
        public int SemanticVersion { get; }
        public ulong LastCompletedTick { get; }
        public IReadOnlyList<CharacterControlStateValue> Values => m_Values;
        public StableHash StateHash { get; }

        public CharacterControlStateValue Get(CharacterControlStateFieldId field) =>
            m_Values[Schema.RequireIndex(field)];

        public void RequireContract(CharacterControlModuleContract contract)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            if (contract.ModuleId != ModuleId || contract.SemanticVersion != SemanticVersion)
                throw new InvalidOperationException($"Character control runtime state '{ModuleId}/{SemanticVersion}' does not match module contract '{contract.ModuleId}/{contract.SemanticVersion}'.");
            var expected = new CharacterControlStateSchema(contract);
            if (!expected.SchemaHash.Equals(Schema.SchemaHash))
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
            var schema = new CharacterControlStateSchema(contract);
            var values = new CharacterControlStateValue[schema.FieldCount];
            for (int i = 0; i < values.Length; i++)
                values[i] = CharacterControlStateValue.Default(schema.Fields[i].ValueKind);
            return new CharacterControlRuntimeState(schema, 0, values);
        }

        static StableHash ComputeHash(
            CharacterControlStateSchema schema,
            ulong lastCompletedTick,
            IReadOnlyList<CharacterControlStateValue> values)
        {
            var parts = new List<string>
            {
                "character-control-runtime-state/1",
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
    }

    public enum CharacterControlRuntimeStateTransactionStatus : byte
    {
        Active = 1,
        Committed = 2,
        Aborted = 3
    }

    public sealed class CharacterControlRuntimeStateTransaction : IDisposable
    {
        readonly CharacterControlStateSchema m_Schema;
        readonly SimulationTick m_Tick;
        readonly List<CharacterControlStateValue> m_Values;
        CharacterControlRuntimeStateTransactionStatus m_Status;

        CharacterControlRuntimeStateTransaction(
            CharacterControlRuntimeState state,
            CharacterControlStateSchema schema,
            SimulationTick tick)
        {
            if (state.ModuleId != schema.ModuleId || state.SemanticVersion != schema.SemanticVersion)
                throw new InvalidOperationException("Character control runtime state identity does not match its schema.");
            if (!tick.IsValid || tick.Value != checked(state.LastCompletedTick + 1))
                throw new ArgumentException("Character control runtime state transaction Tick is not the next Tick.", nameof(tick));
            m_Schema = schema;
            m_Tick = tick;
            m_Values = new List<CharacterControlStateValue>(state.Values);
            m_Status = CharacterControlRuntimeStateTransactionStatus.Active;
        }

        public CharacterControlRuntimeStateTransactionStatus Status => m_Status;

        public static CharacterControlRuntimeStateTransaction Begin(
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
            return new CharacterControlRuntimeStateTransaction(state, schema, tick);
        }

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
            m_Values[index] = value;
        }

        public CharacterControlRuntimeState Commit()
        {
            RequireActive();
            m_Status = CharacterControlRuntimeStateTransactionStatus.Committed;
            return new CharacterControlRuntimeState(m_Schema, m_Tick.Value, m_Values);
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
    }
}
