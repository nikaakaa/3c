using System;
using System.Collections.Generic;
using System.Globalization;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum CharacterPoseTuningInteractionPolicy : byte
    {
        Structural = 0,
        TunableDefault = 1,
        RuntimeInput = 2,
        DerivedReadOnly = 3
    }

    public enum CharacterPoseTuningValueKind : byte
    {
        Float = 0,
        Integer = 1,
        Boolean = 2,
        Enum = 3
    }

    public enum CharacterPoseTuningApplyTiming : byte
    {
        NextFrame = 0,
        NextActivation = 1
    }

    public enum CharacterPoseTuningStatePolicy : byte
    {
        PreserveState = 0,
        ResetOwnerState = 1
    }

    public enum CharacterPoseTuningRuntimeStatus : byte
    {
        Unpublished = 0,
        Pending = 1,
        Applied = 2,
        Rejected = 3
    }

    [Serializable]
    public sealed class CharacterPoseTuningConsumerRange
    {
        [SerializeField] string m_ConsumerId = string.Empty;
        [SerializeField] int m_FirstEntryIndex;
        [SerializeField] int m_EntryCount;

        public CharacterPoseTuningConsumerRange() { }

        public CharacterPoseTuningConsumerRange(
            string consumerId,
            int firstEntryIndex,
            int entryCount)
        {
            m_ConsumerId = RequireIdentity(consumerId, nameof(consumerId));
            if (firstEntryIndex < 0 || entryCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(firstEntryIndex));
            m_FirstEntryIndex = firstEntryIndex;
            m_EntryCount = entryCount;
        }

        public string ConsumerId => m_ConsumerId ?? string.Empty;
        public int FirstEntryIndex => m_FirstEntryIndex;
        public int EntryCount => m_EntryCount;

        static string RequireIdentity(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Tuning identity is incomplete.", field);
            return value;
        }
    }

    [Serializable]
    public sealed class CharacterPoseTuningLayoutEntry
    {
        [SerializeField] string m_OwnerId = string.Empty;
        [SerializeField] string m_FieldId = string.Empty;
        [SerializeField] string m_DisplayName = string.Empty;
        [SerializeField] CharacterPoseTuningInteractionPolicy m_Interaction;
        [SerializeField] CharacterPoseTuningValueKind m_ValueKind;
        [SerializeField] string m_Unit = string.Empty;
        [SerializeField] float m_Minimum;
        [SerializeField] float m_Maximum;
        [SerializeField] bool m_FiniteOnly;
        [SerializeField] CharacterPoseTuningApplyTiming m_ApplyTiming;
        [SerializeField] CharacterPoseTuningStatePolicy m_StatePolicy;
        [SerializeField] int m_ValueIndex;
        [SerializeField] string m_ConsumerId = string.Empty;

        public CharacterPoseTuningLayoutEntry() { }

        public CharacterPoseTuningLayoutEntry(
            string ownerId,
            string fieldId,
            string displayName,
            CharacterPoseTuningInteractionPolicy interaction,
            CharacterPoseTuningValueKind valueKind,
            string unit,
            float minimum,
            float maximum,
            bool finiteOnly,
            CharacterPoseTuningApplyTiming applyTiming,
            CharacterPoseTuningStatePolicy statePolicy,
            int valueIndex,
            string consumerId)
        {
            m_OwnerId = RequireIdentity(ownerId, nameof(ownerId));
            m_FieldId = RequireIdentity(fieldId, nameof(fieldId));
            m_DisplayName = RequireIdentity(displayName, nameof(displayName));
            if (!Enum.IsDefined(typeof(CharacterPoseTuningInteractionPolicy), interaction) ||
                !Enum.IsDefined(typeof(CharacterPoseTuningValueKind), valueKind) ||
                !Enum.IsDefined(typeof(CharacterPoseTuningApplyTiming), applyTiming) ||
                !Enum.IsDefined(typeof(CharacterPoseTuningStatePolicy), statePolicy))
                throw new ArgumentException("Tuning field enum value is invalid.");
            if (!float.IsFinite(minimum) || !float.IsFinite(maximum) || minimum > maximum)
                throw new ArgumentException("Tuning field range is invalid.");
            if (valueIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(valueIndex));
            m_Interaction = interaction;
            m_ValueKind = valueKind;
            m_Unit = unit ?? string.Empty;
            m_Minimum = minimum;
            m_Maximum = maximum;
            m_FiniteOnly = finiteOnly;
            m_ApplyTiming = applyTiming;
            m_StatePolicy = statePolicy;
            m_ValueIndex = valueIndex;
            m_ConsumerId = RequireIdentity(consumerId, nameof(consumerId));
        }

        public string OwnerId => m_OwnerId ?? string.Empty;
        public string FieldId => m_FieldId ?? string.Empty;
        public string DisplayName => m_DisplayName ?? string.Empty;
        public CharacterPoseTuningInteractionPolicy Interaction => m_Interaction;
        public CharacterPoseTuningValueKind ValueKind => m_ValueKind;
        public string Unit => m_Unit ?? string.Empty;
        public float Minimum => m_Minimum;
        public float Maximum => m_Maximum;
        public bool FiniteOnly => m_FiniteOnly;
        public CharacterPoseTuningApplyTiming ApplyTiming => m_ApplyTiming;
        public CharacterPoseTuningStatePolicy StatePolicy => m_StatePolicy;
        public int ValueIndex => m_ValueIndex;
        public string ConsumerId => m_ConsumerId ?? string.Empty;

        internal string HashKey => string.Join(
            "|",
            OwnerId,
            FieldId,
            DisplayName,
            ((byte)Interaction).ToString(CultureInfo.InvariantCulture),
            ((byte)ValueKind).ToString(CultureInfo.InvariantCulture),
            Unit,
            Minimum.ToString("R", CultureInfo.InvariantCulture),
            Maximum.ToString("R", CultureInfo.InvariantCulture),
            FiniteOnly ? "1" : "0",
            ((byte)ApplyTiming).ToString(CultureInfo.InvariantCulture),
            ((byte)StatePolicy).ToString(CultureInfo.InvariantCulture),
            ValueIndex.ToString(CultureInfo.InvariantCulture),
            ConsumerId);

        static string RequireIdentity(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Tuning identity is incomplete.", field);
            return value;
        }
    }

    [Serializable]
    public sealed class CharacterPoseTuningLayout
    {
        [SerializeField] string m_ProgramId = string.Empty;
        [SerializeField] string m_ProjectionRevision = string.Empty;
        [SerializeField] string m_PosePlanHash = string.Empty;
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_RigRevision = string.Empty;
        [SerializeField] CharacterPoseTuningLayoutEntry[] m_Entries = Array.Empty<CharacterPoseTuningLayoutEntry>();
        [SerializeField] CharacterPoseTuningConsumerRange[] m_Consumers = Array.Empty<CharacterPoseTuningConsumerRange>();
        [SerializeField] string m_LayoutHash = string.Empty;

        public CharacterPoseTuningLayout() { }

        CharacterPoseTuningLayout(
            string programId,
            string projectionRevision,
            string posePlanHash,
            string rigId,
            string rigRevision,
            IReadOnlyList<CharacterPoseTuningLayoutEntry> entries,
            IReadOnlyList<CharacterPoseTuningConsumerRange> consumers,
            string layoutHash)
        {
            m_ProgramId = RequireIdentity(programId, nameof(programId));
            m_ProjectionRevision = RequireIdentity(projectionRevision, nameof(projectionRevision));
            m_PosePlanHash = RequireIdentity(posePlanHash, nameof(posePlanHash));
            m_RigId = RequireIdentity(rigId, nameof(rigId));
            m_RigRevision = RequireIdentity(rigRevision, nameof(rigRevision));
            m_Entries = Copy(entries).ToArray();
            m_Consumers = Copy(consumers).ToArray();
            m_LayoutHash = RequireIdentity(layoutHash, nameof(layoutHash));
        }

        public string ProgramId => m_ProgramId ?? string.Empty;
        public string ProjectionRevision => m_ProjectionRevision ?? string.Empty;
        public string PosePlanHash => m_PosePlanHash ?? string.Empty;
        public string RigId => m_RigId ?? string.Empty;
        public string RigRevision => m_RigRevision ?? string.Empty;
        public IReadOnlyList<CharacterPoseTuningLayoutEntry> Entries => m_Entries ?? Array.Empty<CharacterPoseTuningLayoutEntry>();
        public IReadOnlyList<CharacterPoseTuningConsumerRange> Consumers => m_Consumers ?? Array.Empty<CharacterPoseTuningConsumerRange>();
        public string LayoutHash => m_LayoutHash ?? string.Empty;

        public static CharacterPoseTuningLayout Create(
            string programId,
            string projectionRevision,
            string posePlanHash,
            string rigId,
            string rigRevision,
            IReadOnlyList<CharacterPoseTuningLayoutEntry> entries,
            IReadOnlyList<CharacterPoseTuningConsumerRange> consumers)
        {
            var copiedEntries = Copy(entries);
            var copiedConsumers = Copy(consumers);
            var hashParts = new List<string>(8 + copiedEntries.Count + copiedConsumers.Count)
            {
                programId,
                projectionRevision,
                posePlanHash,
                rigId,
                rigRevision
            };
            for (int i = 0; i < copiedEntries.Count; i++)
                hashParts.Add(copiedEntries[i].HashKey);
            for (int i = 0; i < copiedConsumers.Count; i++)
            {
                CharacterPoseTuningConsumerRange consumer = copiedConsumers[i];
                hashParts.Add(string.Join(
                    "|",
                    consumer.ConsumerId,
                    consumer.FirstEntryIndex.ToString(CultureInfo.InvariantCulture),
                    consumer.EntryCount.ToString(CultureInfo.InvariantCulture)));
            }
            return new CharacterPoseTuningLayout(
                programId,
                projectionRevision,
                posePlanHash,
                rigId,
                rigRevision,
                copiedEntries,
                copiedConsumers,
                StableHash.Compute(hashParts.ToArray()).ToString());
        }

        public void RequireValid()
        {
            if (Entries == null || Consumers == null || Entries.Count == 0)
                throw new InvalidOperationException("Pose tuning layout has no entries.");
            var fieldIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < Entries.Count; i++)
            {
                CharacterPoseTuningLayoutEntry entry = Entries[i];
                if (entry == null || !fieldIds.Add(entry.FieldId) || entry.ValueIndex < 0)
                    throw new InvalidOperationException("Pose tuning layout contains duplicate or invalid fields.");
            }
            for (int i = 0; i < Consumers.Count; i++)
            {
                CharacterPoseTuningConsumerRange consumer = Consumers[i];
                if (consumer == null || consumer.FirstEntryIndex + consumer.EntryCount > Entries.Count)
                    throw new InvalidOperationException("Pose tuning layout contains an invalid consumer range.");
            }
            CharacterPoseTuningLayout expected = Create(
                ProgramId,
                ProjectionRevision,
                PosePlanHash,
                RigId,
                RigRevision,
                Entries,
                Consumers);
            if (!string.Equals(LayoutHash, expected.LayoutHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Pose tuning layout hash is stale.");
        }

        static List<CharacterPoseTuningLayoutEntry> Copy(
            IReadOnlyList<CharacterPoseTuningLayoutEntry> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            var result = new List<CharacterPoseTuningLayoutEntry>(values.Count);
            for (int i = 0; i < values.Count; i++)
                result.Add(values[i] ?? throw new ArgumentException("Tuning layout entry is missing."));
            return result;
        }

        static List<CharacterPoseTuningConsumerRange> Copy(
            IReadOnlyList<CharacterPoseTuningConsumerRange> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            var result = new List<CharacterPoseTuningConsumerRange>(values.Count);
            for (int i = 0; i < values.Count; i++)
                result.Add(values[i] ?? throw new ArgumentException("Tuning consumer range is missing."));
            return result;
        }

        static string RequireIdentity(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Tuning identity is incomplete.", field);
            return value;
        }
    }

    public readonly struct CharacterPoseTuningValue
    {
        public CharacterPoseTuningValue(
            CharacterPoseTuningValueKind kind,
            float floatValue,
            int integerValue,
            bool booleanValue,
            int enumValue)
        {
            Kind = kind;
            FloatValue = floatValue;
            IntegerValue = integerValue;
            BooleanValue = booleanValue;
            EnumValue = enumValue;
        }

        public CharacterPoseTuningValueKind Kind { get; }
        public float FloatValue { get; }
        public int IntegerValue { get; }
        public bool BooleanValue { get; }
        public int EnumValue { get; }

        public static CharacterPoseTuningValue Float(float value) =>
            new CharacterPoseTuningValue(CharacterPoseTuningValueKind.Float, value, 0, false, 0);
        public static CharacterPoseTuningValue Integer(int value) =>
            new CharacterPoseTuningValue(CharacterPoseTuningValueKind.Integer, 0f, value, false, 0);
        public static CharacterPoseTuningValue Boolean(bool value) =>
            new CharacterPoseTuningValue(CharacterPoseTuningValueKind.Boolean, 0f, 0, value, 0);
        public static CharacterPoseTuningValue Enum(int value) =>
            new CharacterPoseTuningValue(CharacterPoseTuningValueKind.Enum, 0f, 0, false, value);
    }

    [Serializable]
    public sealed class CharacterPoseTuningParameterBlock
    {
        [SerializeField] string m_LayoutHash = string.Empty;
        [SerializeField] float[] m_Floats = Array.Empty<float>();
        [SerializeField] int[] m_Integers = Array.Empty<int>();
        [SerializeField] byte[] m_Booleans = Array.Empty<byte>();
        [SerializeField] int[] m_Enums = Array.Empty<int>();

        public CharacterPoseTuningParameterBlock() { }

        public CharacterPoseTuningParameterBlock(
            string layoutHash,
            float[] floats,
            int[] integers,
            byte[] booleans,
            int[] enums)
        {
            m_LayoutHash = RequireIdentity(layoutHash, nameof(layoutHash));
            m_Floats = Copy(floats);
            m_Integers = Copy(integers);
            m_Booleans = Copy(booleans);
            m_Enums = Copy(enums);
            RequireFiniteValues();
        }

        public string LayoutHash => m_LayoutHash ?? string.Empty;
        public float[] Floats => m_Floats ?? Array.Empty<float>();
        public int[] Integers => m_Integers ?? Array.Empty<int>();
        public byte[] Booleans => m_Booleans ?? Array.Empty<byte>();
        public int[] Enums => m_Enums ?? Array.Empty<int>();

        public CharacterPoseTuningParameterBlock Clone() =>
            new CharacterPoseTuningParameterBlock(LayoutHash, Floats, Integers, Booleans, Enums);

        public CharacterPoseTuningValue GetValue(CharacterPoseTuningLayoutEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));
            switch (entry.ValueKind)
            {
                case CharacterPoseTuningValueKind.Float:
                    return CharacterPoseTuningValue.Float(Floats[entry.ValueIndex]);
                case CharacterPoseTuningValueKind.Integer:
                    return CharacterPoseTuningValue.Integer(Integers[entry.ValueIndex]);
                case CharacterPoseTuningValueKind.Boolean:
                    return CharacterPoseTuningValue.Boolean(Booleans[entry.ValueIndex] != 0);
                case CharacterPoseTuningValueKind.Enum:
                    return CharacterPoseTuningValue.Enum(Enums[entry.ValueIndex]);
                default:
                    throw new InvalidOperationException("Pose tuning value kind is invalid.");
            }
        }

        public void RequireValid(CharacterPoseTuningLayout layout)
        {
            if (layout == null)
                throw new ArgumentNullException(nameof(layout));
            layout.RequireValid();
            if (!string.Equals(LayoutHash, layout.LayoutHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Pose tuning block layout identity is stale.");
            for (int i = 0; i < layout.Entries.Count; i++)
            {
                CharacterPoseTuningLayoutEntry entry = layout.Entries[i];
                int length = Length(entry.ValueKind);
                if (entry.ValueIndex >= length)
                    throw new InvalidOperationException($"Pose tuning field '{entry.FieldId}' has no dense value.");
                CharacterPoseTuningValue value = GetValue(entry);
                if (entry.ValueKind == CharacterPoseTuningValueKind.Float &&
                    (entry.FiniteOnly && !float.IsFinite(value.FloatValue) ||
                     value.FloatValue < entry.Minimum || value.FloatValue > entry.Maximum))
                    throw new InvalidOperationException($"Pose tuning field '{entry.FieldId}' is outside its published range.");
                int discreteValue = entry.ValueKind == CharacterPoseTuningValueKind.Enum
                    ? value.EnumValue
                    : entry.ValueKind == CharacterPoseTuningValueKind.Boolean
                        ? (value.BooleanValue ? 1 : 0)
                        : value.IntegerValue;
                if (entry.ValueKind != CharacterPoseTuningValueKind.Float &&
                    (discreteValue < entry.Minimum || discreteValue > entry.Maximum))
                    throw new InvalidOperationException($"Pose tuning field '{entry.FieldId}' is outside its published range.");
            }
        }

        void RequireFiniteValues()
        {
            for (int i = 0; i < Floats.Length; i++)
                if (!float.IsFinite(Floats[i]))
                    throw new ArgumentException("Pose tuning block contains a non-finite value.");
        }

        int Length(CharacterPoseTuningValueKind kind) => kind switch
        {
            CharacterPoseTuningValueKind.Float => Floats.Length,
            CharacterPoseTuningValueKind.Integer => Integers.Length,
            CharacterPoseTuningValueKind.Boolean => Booleans.Length,
            CharacterPoseTuningValueKind.Enum => Enums.Length,
            _ => 0
        };

        static float[] Copy(float[] values) => values == null ? Array.Empty<float>() : (float[])values.Clone();
        static int[] Copy(int[] values) => values == null ? Array.Empty<int>() : (int[])values.Clone();
        static byte[] Copy(byte[] values) => values == null ? Array.Empty<byte>() : (byte[])values.Clone();

        static string RequireIdentity(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Tuning identity is incomplete.", field);
            return value;
        }
    }

    public readonly struct CharacterPoseTuningTargetIdentity : IEquatable<CharacterPoseTuningTargetIdentity>
    {
        public CharacterPoseTuningTargetIdentity(
            string targetId,
            string programId,
            string projectionRevision,
            string posePlanHash,
            string rigId,
            string rigRevision,
            string layoutHash)
        {
            TargetId = Require(targetId, nameof(targetId));
            ProgramId = Require(programId, nameof(programId));
            ProjectionRevision = Require(projectionRevision, nameof(projectionRevision));
            PosePlanHash = Require(posePlanHash, nameof(posePlanHash));
            RigId = Require(rigId, nameof(rigId));
            RigRevision = Require(rigRevision, nameof(rigRevision));
            LayoutHash = Require(layoutHash, nameof(layoutHash));
        }

        public string TargetId { get; }
        public string ProgramId { get; }
        public string ProjectionRevision { get; }
        public string PosePlanHash { get; }
        public string RigId { get; }
        public string RigRevision { get; }
        public string LayoutHash { get; }

        public bool Equals(CharacterPoseTuningTargetIdentity other) =>
            string.Equals(TargetId, other.TargetId, StringComparison.Ordinal) &&
            string.Equals(ProgramId, other.ProgramId, StringComparison.Ordinal) &&
            string.Equals(ProjectionRevision, other.ProjectionRevision, StringComparison.Ordinal) &&
            string.Equals(PosePlanHash, other.PosePlanHash, StringComparison.Ordinal) &&
            string.Equals(RigId, other.RigId, StringComparison.Ordinal) &&
            string.Equals(RigRevision, other.RigRevision, StringComparison.Ordinal) &&
            string.Equals(LayoutHash, other.LayoutHash, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is CharacterPoseTuningTargetIdentity other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(TargetId ?? string.Empty);

        static string Require(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Tuning target identity is incomplete.", field);
            return value.Trim();
        }
    }

    [Serializable]
    public sealed class CharacterPoseTuningCandidate
    {
        public CharacterPoseTuningCandidate(
            CharacterPoseTuningTargetIdentity target,
            string sourceAuthoringRevision,
            string candidateRevision,
            CharacterPoseTuningParameterBlock block)
        {
            Target = target;
            SourceAuthoringRevision = Require(sourceAuthoringRevision, nameof(sourceAuthoringRevision));
            CandidateRevision = Require(candidateRevision, nameof(candidateRevision));
            Block = block?.Clone() ?? throw new ArgumentNullException(nameof(block));
        }

        public CharacterPoseTuningTargetIdentity Target { get; }
        public string SourceAuthoringRevision { get; }
        public string CandidateRevision { get; }
        public CharacterPoseTuningParameterBlock Block { get; }

        static string Require(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Tuning candidate revision is incomplete.", field);
            return value.Trim();
        }
    }

    public readonly struct CharacterPoseTuningRuntimeState
    {
        public CharacterPoseTuningRuntimeState(
            CharacterPoseTuningRuntimeStatus status,
            string publishedParameterRevision,
            string appliedCandidateRevision,
            string sourceAuthoringRevision,
            ulong appliedFrame,
            string rejectionReason)
        {
            Status = status;
            PublishedParameterRevision = publishedParameterRevision ?? string.Empty;
            AppliedCandidateRevision = appliedCandidateRevision ?? string.Empty;
            SourceAuthoringRevision = sourceAuthoringRevision ?? string.Empty;
            AppliedFrame = appliedFrame;
            RejectionReason = rejectionReason ?? string.Empty;
        }

        public CharacterPoseTuningRuntimeStatus Status { get; }
        public string PublishedParameterRevision { get; }
        public string AppliedCandidateRevision { get; }
        public string SourceAuthoringRevision { get; }
        public ulong AppliedFrame { get; }
        public string RejectionReason { get; }
    }

    public sealed class CharacterPoseTuningRuntimeBinding
    {
        readonly CharacterPoseTuningTargetIdentity m_Target;
        readonly CharacterPoseTuningLayout m_Layout;
        CharacterPoseTuningParameterBlock m_Active;
        CharacterPoseTuningCandidate m_Pending;
        CharacterPoseTuningRuntimeState m_State;

        public CharacterPoseTuningRuntimeBinding(
            CharacterPoseTuningTargetIdentity target,
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock defaultBlock,
            string publishedParameterRevision)
        {
            m_Target = target;
            m_Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            m_Layout.RequireValid();
            m_Active = defaultBlock?.Clone() ?? throw new ArgumentNullException(nameof(defaultBlock));
            m_Active.RequireValid(layout);
            m_State = new CharacterPoseTuningRuntimeState(
                CharacterPoseTuningRuntimeStatus.Applied,
                publishedParameterRevision,
                string.Empty,
                string.Empty,
                0,
                string.Empty);
        }

        public CharacterPoseTuningParameterBlock ActiveBlock => m_Active?.Clone();
        public CharacterPoseTuningCandidate PendingCandidate => m_Pending;
        public CharacterPoseTuningRuntimeState State => m_State;

        public void ClearPending()
        {
            if (m_Pending == null)
                return;
            m_Pending = null;
            m_State = new CharacterPoseTuningRuntimeState(
                CharacterPoseTuningRuntimeStatus.Applied,
                m_State.PublishedParameterRevision,
                m_State.AppliedCandidateRevision,
                string.Empty,
                m_State.AppliedFrame,
                string.Empty);
        }

        public bool SubmitPending(CharacterPoseTuningCandidate candidate, out string error)
        {
            error = string.Empty;
            if (candidate == null)
            {
                error = "Pose tuning candidate is missing.";
                return false;
            }
            if (!candidate.Target.Equals(m_Target))
            {
                error = "Pose tuning candidate target identity does not match the active target.";
                SetRejected(candidate, error);
                return false;
            }
            try
            {
                candidate.Block.RequireValid(m_Layout);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                SetRejected(candidate, error);
                return false;
            }
            m_Pending = candidate;
            m_State = new CharacterPoseTuningRuntimeState(
                CharacterPoseTuningRuntimeStatus.Pending,
                m_State.PublishedParameterRevision,
                m_State.AppliedCandidateRevision,
                candidate.SourceAuthoringRevision,
                m_State.AppliedFrame,
                string.Empty);
            return true;
        }

        public bool TryApplyPending(
            CharacterPoseTuningTargetIdentity currentTarget,
            ulong frame,
            bool activation,
            Func<CharacterPoseTuningParameterBlock, bool, string> apply,
            out string error)
        {
            error = string.Empty;
            if (m_Pending == null)
                return false;
            if (!currentTarget.Equals(m_Target) || !m_Pending.Target.Equals(currentTarget))
            {
                error = "Pose tuning candidate target identity changed before application.";
                SetRejected(m_Pending, error);
                m_Pending = null;
                return false;
            }
            CharacterPoseTuningCandidate candidate = m_Pending;
            if (!activation && RequiresActivation(m_Layout, candidate.Block, m_Active))
                return false;
            if (apply == null)
            {
                error = "Pose tuning runtime adapter is missing.";
                SetRejected(candidate, error);
                m_Pending = null;
                return false;
            }
            bool resetOwnerState = RequiresReset(m_Layout, candidate.Block, m_Active);
            string applyError;
            try
            {
                applyError = apply(candidate.Block.Clone(), resetOwnerState);
            }
            catch (Exception exception)
            {
                applyError = exception.Message;
            }
            if (!string.IsNullOrEmpty(applyError))
            {
                error = applyError;
                SetRejected(candidate, error);
                m_Pending = null;
                return false;
            }
            m_Active = candidate.Block.Clone();
            m_Pending = null;
            m_State = new CharacterPoseTuningRuntimeState(
                CharacterPoseTuningRuntimeStatus.Applied,
                m_State.PublishedParameterRevision,
                candidate.CandidateRevision,
                candidate.SourceAuthoringRevision,
                frame,
                string.Empty);
            return true;
        }

        static bool RequiresActivation(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock candidate,
            CharacterPoseTuningParameterBlock active)
        {
            for (int i = 0; i < layout.Entries.Count; i++)
                if (layout.Entries[i].ApplyTiming == CharacterPoseTuningApplyTiming.NextActivation &&
                    !ValuesEqual(
                        candidate.GetValue(layout.Entries[i]),
                        active.GetValue(layout.Entries[i])))
                    return true;
            return false;
        }

        static bool RequiresReset(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock candidate,
            CharacterPoseTuningParameterBlock active)
        {
            for (int i = 0; i < layout.Entries.Count; i++)
                if (layout.Entries[i].StatePolicy == CharacterPoseTuningStatePolicy.ResetOwnerState &&
                    !ValuesEqual(
                        candidate.GetValue(layout.Entries[i]),
                        active.GetValue(layout.Entries[i])))
                    return true;
            return false;
        }

        static bool ValuesEqual(
            CharacterPoseTuningValue left,
            CharacterPoseTuningValue right)
        {
            if (left.Kind != right.Kind)
                return false;
            switch (left.Kind)
            {
                case CharacterPoseTuningValueKind.Float:
                    return left.FloatValue == right.FloatValue;
                case CharacterPoseTuningValueKind.Integer:
                    return left.IntegerValue == right.IntegerValue;
                case CharacterPoseTuningValueKind.Boolean:
                    return left.BooleanValue == right.BooleanValue;
                case CharacterPoseTuningValueKind.Enum:
                    return left.EnumValue == right.EnumValue;
                default:
                    return false;
            }
        }

        void SetRejected(CharacterPoseTuningCandidate candidate, string error)
        {
            m_State = new CharacterPoseTuningRuntimeState(
                CharacterPoseTuningRuntimeStatus.Rejected,
                m_State.PublishedParameterRevision,
                m_State.AppliedCandidateRevision,
                candidate?.SourceAuthoringRevision,
                m_State.AppliedFrame,
                error);
        }
    }
}
