using System;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    static class MotionMatchingIdentity
    {
        public static string Require(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Motion Matching identity is invalid.", parameterName);
            return value;
        }

        public static StableHash Hash(string kind, string value) =>
            string.IsNullOrEmpty(value) ? default : StableHash.Compute(kind, value);
    }

    public readonly struct CharacterMotionMatchingProfileId : IEquatable<CharacterMotionMatchingProfileId>, IComparable<CharacterMotionMatchingProfileId>
    {
        public CharacterMotionMatchingProfileId(string value) { Value = MotionMatchingIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public StableHash StableHash => MotionMatchingIdentity.Hash("motion-matching-profile", Value);
        public int CompareTo(CharacterMotionMatchingProfileId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(CharacterMotionMatchingProfileId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CharacterMotionMatchingProfileId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(CharacterMotionMatchingProfileId left, CharacterMotionMatchingProfileId right) => left.Equals(right);
        public static bool operator !=(CharacterMotionMatchingProfileId left, CharacterMotionMatchingProfileId right) => !left.Equals(right);
    }

    public readonly struct CharacterMotionMatchingDatabaseId : IEquatable<CharacterMotionMatchingDatabaseId>, IComparable<CharacterMotionMatchingDatabaseId>
    {
        public CharacterMotionMatchingDatabaseId(string value) { Value = MotionMatchingIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public StableHash StableHash => MotionMatchingIdentity.Hash("motion-matching-database", Value);
        public int CompareTo(CharacterMotionMatchingDatabaseId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(CharacterMotionMatchingDatabaseId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CharacterMotionMatchingDatabaseId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(CharacterMotionMatchingDatabaseId left, CharacterMotionMatchingDatabaseId right) => left.Equals(right);
        public static bool operator !=(CharacterMotionMatchingDatabaseId left, CharacterMotionMatchingDatabaseId right) => !left.Equals(right);
    }

    public readonly struct CharacterMotionMatchingFeatureSchemaId : IEquatable<CharacterMotionMatchingFeatureSchemaId>, IComparable<CharacterMotionMatchingFeatureSchemaId>
    {
        public CharacterMotionMatchingFeatureSchemaId(string value) { Value = MotionMatchingIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public StableHash StableHash => MotionMatchingIdentity.Hash("motion-matching-feature-schema", Value);
        public int CompareTo(CharacterMotionMatchingFeatureSchemaId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(CharacterMotionMatchingFeatureSchemaId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CharacterMotionMatchingFeatureSchemaId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(CharacterMotionMatchingFeatureSchemaId left, CharacterMotionMatchingFeatureSchemaId right) => left.Equals(right);
        public static bool operator !=(CharacterMotionMatchingFeatureSchemaId left, CharacterMotionMatchingFeatureSchemaId right) => !left.Equals(right);
    }

    public readonly struct CharacterMotionMatchingSearchDomainId : IEquatable<CharacterMotionMatchingSearchDomainId>, IComparable<CharacterMotionMatchingSearchDomainId>
    {
        public CharacterMotionMatchingSearchDomainId(string value) { Value = MotionMatchingIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public StableHash StableHash => MotionMatchingIdentity.Hash("motion-matching-search-domain", Value);
        public int CompareTo(CharacterMotionMatchingSearchDomainId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(CharacterMotionMatchingSearchDomainId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CharacterMotionMatchingSearchDomainId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(CharacterMotionMatchingSearchDomainId left, CharacterMotionMatchingSearchDomainId right) => left.Equals(right);
        public static bool operator !=(CharacterMotionMatchingSearchDomainId left, CharacterMotionMatchingSearchDomainId right) => !left.Equals(right);
    }

    public readonly struct CharacterMotionMatchingSegmentId : IEquatable<CharacterMotionMatchingSegmentId>, IComparable<CharacterMotionMatchingSegmentId>
    {
        public CharacterMotionMatchingSegmentId(string value) { Value = MotionMatchingIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public StableHash StableHash => MotionMatchingIdentity.Hash("motion-matching-segment", Value);
        public int CompareTo(CharacterMotionMatchingSegmentId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(CharacterMotionMatchingSegmentId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CharacterMotionMatchingSegmentId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(CharacterMotionMatchingSegmentId left, CharacterMotionMatchingSegmentId right) => left.Equals(right);
        public static bool operator !=(CharacterMotionMatchingSegmentId left, CharacterMotionMatchingSegmentId right) => !left.Equals(right);
    }

    public readonly struct CharacterMotionMatchingSampleId : IEquatable<CharacterMotionMatchingSampleId>, IComparable<CharacterMotionMatchingSampleId>
    {
        public CharacterMotionMatchingSampleId(uint value)
        {
            if (value == 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }

        public uint Value { get; }
        public bool IsValid => Value != 0;
        public StableHash StableHash => IsValid ? StableHash.Compute("motion-matching-sample", Value.ToString(System.Globalization.CultureInfo.InvariantCulture)) : default;
        public int CompareTo(CharacterMotionMatchingSampleId other) => Value.CompareTo(other.Value);
        public bool Equals(CharacterMotionMatchingSampleId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is CharacterMotionMatchingSampleId other && Equals(other);
        public override int GetHashCode() => (int)Value;
        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public static bool operator ==(CharacterMotionMatchingSampleId left, CharacterMotionMatchingSampleId right) => left.Equals(right);
        public static bool operator !=(CharacterMotionMatchingSampleId left, CharacterMotionMatchingSampleId right) => !left.Equals(right);
    }

    public readonly struct CharacterMotionMatchingQueryId : IEquatable<CharacterMotionMatchingQueryId>, IComparable<CharacterMotionMatchingQueryId>
    {
        public CharacterMotionMatchingQueryId(ulong value)
        {
            if (value == 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }

        public ulong Value { get; }
        public bool IsValid => Value != 0;
        public StableHash StableHash => IsValid ? StableHash.Compute("motion-matching-query", Value.ToString(System.Globalization.CultureInfo.InvariantCulture)) : default;
        public int CompareTo(CharacterMotionMatchingQueryId other) => Value.CompareTo(other.Value);
        public bool Equals(CharacterMotionMatchingQueryId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is CharacterMotionMatchingQueryId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public static bool operator ==(CharacterMotionMatchingQueryId left, CharacterMotionMatchingQueryId right) => left.Equals(right);
        public static bool operator !=(CharacterMotionMatchingQueryId left, CharacterMotionMatchingQueryId right) => !left.Equals(right);
    }

    public readonly struct CharacterMotionMatchingPlanId : IEquatable<CharacterMotionMatchingPlanId>, IComparable<CharacterMotionMatchingPlanId>
    {
        public CharacterMotionMatchingPlanId(ulong value)
        {
            if (value == 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }

        public ulong Value { get; }
        public bool IsValid => Value != 0;
        public StableHash StableHash => IsValid ? StableHash.Compute("motion-matching-plan", Value.ToString(System.Globalization.CultureInfo.InvariantCulture)) : default;
        public int CompareTo(CharacterMotionMatchingPlanId other) => Value.CompareTo(other.Value);
        public bool Equals(CharacterMotionMatchingPlanId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is CharacterMotionMatchingPlanId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public static bool operator ==(CharacterMotionMatchingPlanId left, CharacterMotionMatchingPlanId right) => left.Equals(right);
        public static bool operator !=(CharacterMotionMatchingPlanId left, CharacterMotionMatchingPlanId right) => !left.Equals(right);
    }

    public readonly struct MotionMatchingSelectionGeneration : IEquatable<MotionMatchingSelectionGeneration>, IComparable<MotionMatchingSelectionGeneration>
    {
        public MotionMatchingSelectionGeneration(ulong value)
        {
            if (value == 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }

        public ulong Value { get; }
        public bool IsValid => Value != 0;
        public StableHash StableHash => IsValid ? StableHash.Compute("motion-matching-selection-generation", Value.ToString(System.Globalization.CultureInfo.InvariantCulture)) : default;
        public MotionMatchingSelectionGeneration Next()
        {
            if (Value == ulong.MaxValue)
                throw new InvalidOperationException("Motion Matching selection generation cannot wrap.");
            return new MotionMatchingSelectionGeneration(Value + 1);
        }

        public int CompareTo(MotionMatchingSelectionGeneration other) => Value.CompareTo(other.Value);
        public bool Equals(MotionMatchingSelectionGeneration other) => Value == other.Value;
        public override bool Equals(object obj) => obj is MotionMatchingSelectionGeneration other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public static bool operator ==(MotionMatchingSelectionGeneration left, MotionMatchingSelectionGeneration right) => left.Equals(right);
        public static bool operator !=(MotionMatchingSelectionGeneration left, MotionMatchingSelectionGeneration right) => !left.Equals(right);
    }

    public readonly struct MotionMatchingSelectionIdentity : IEquatable<MotionMatchingSelectionIdentity>
    {
        public MotionMatchingSelectionIdentity(
            CharacterMotionMatchingDatabaseArtifactIdentity databaseIdentity,
            MotionMatchingSelectionGeneration generation,
            CharacterMotionMatchingPlanId planId,
            CharacterMotionMatchingSampleId sampleId,
            int sampleIndex)
        {
            DatabaseIdentity = databaseIdentity ?? throw new ArgumentNullException(nameof(databaseIdentity));
            if (!generation.IsValid || !planId.IsValid || !sampleId.IsValid || sampleIndex < 0)
                throw new ArgumentException("Motion Matching Selection identity is incomplete.");
            Generation = generation;
            PlanId = planId;
            SampleId = sampleId;
            SampleIndex = sampleIndex;
        }

        public CharacterMotionMatchingDatabaseArtifactIdentity DatabaseIdentity { get; }
        public MotionMatchingSelectionGeneration Generation { get; }
        public CharacterMotionMatchingPlanId PlanId { get; }
        public CharacterMotionMatchingSampleId SampleId { get; }
        public int SampleIndex { get; }
        public bool IsValid => DatabaseIdentity != null && Generation.IsValid && PlanId.IsValid && SampleId.IsValid && SampleIndex >= 0;

        public bool Equals(MotionMatchingSelectionIdentity other) =>
            DatabaseIdentity != null && other.DatabaseIdentity != null &&
            DatabaseIdentity.EqualsExact(other.DatabaseIdentity) && Generation.Equals(other.Generation) &&
            PlanId.Equals(other.PlanId) && SampleId.Equals(other.SampleId) && SampleIndex == other.SampleIndex;

        public override bool Equals(object obj) => obj is MotionMatchingSelectionIdentity other && Equals(other);
        public override int GetHashCode() => unchecked(
            (((DatabaseIdentity?.ContentHash.GetHashCode() ?? 0) * 397 ^ Generation.GetHashCode()) * 397 ^ PlanId.GetHashCode()) * 397 ^ SampleIndex);
        public static bool operator ==(MotionMatchingSelectionIdentity left, MotionMatchingSelectionIdentity right) => left.Equals(right);
        public static bool operator !=(MotionMatchingSelectionIdentity left, MotionMatchingSelectionIdentity right) => !left.Equals(right);
    }

    public readonly struct MotionMatchingTrajectorySourceIdentity : IEquatable<MotionMatchingTrajectorySourceIdentity>, IComparable<MotionMatchingTrajectorySourceIdentity>
    {
        public MotionMatchingTrajectorySourceIdentity(string value) { Value = MotionMatchingIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public StableHash StableHash => MotionMatchingIdentity.Hash("motion-matching-trajectory-source", Value);
        public int CompareTo(MotionMatchingTrajectorySourceIdentity other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(MotionMatchingTrajectorySourceIdentity other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is MotionMatchingTrajectorySourceIdentity other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(MotionMatchingTrajectorySourceIdentity left, MotionMatchingTrajectorySourceIdentity right) => left.Equals(right);
        public static bool operator !=(MotionMatchingTrajectorySourceIdentity left, MotionMatchingTrajectorySourceIdentity right) => !left.Equals(right);
    }

    public readonly struct CharacterMotionMatchingSourceSetId : IEquatable<CharacterMotionMatchingSourceSetId>, IComparable<CharacterMotionMatchingSourceSetId>
    {
        public CharacterMotionMatchingSourceSetId(string value) { Value = MotionMatchingIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public StableHash StableHash => MotionMatchingIdentity.Hash("motion-matching-source-set", Value);
        public int CompareTo(CharacterMotionMatchingSourceSetId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(CharacterMotionMatchingSourceSetId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CharacterMotionMatchingSourceSetId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct CharacterMotionMatchingSourceClipId : IEquatable<CharacterMotionMatchingSourceClipId>, IComparable<CharacterMotionMatchingSourceClipId>
    {
        public CharacterMotionMatchingSourceClipId(string value) { Value = MotionMatchingIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public StableHash StableHash => MotionMatchingIdentity.Hash("motion-matching-source-clip", Value);
        public int CompareTo(CharacterMotionMatchingSourceClipId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(CharacterMotionMatchingSourceClipId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CharacterMotionMatchingSourceClipId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct CharacterMotionMatchingIndexNodeId : IEquatable<CharacterMotionMatchingIndexNodeId>, IComparable<CharacterMotionMatchingIndexNodeId>
    {
        public CharacterMotionMatchingIndexNodeId(int value)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }

        public int Value { get; }
        public bool IsValid => Value > 0;
        public int CompareTo(CharacterMotionMatchingIndexNodeId other) => Value.CompareTo(other.Value);
        public bool Equals(CharacterMotionMatchingIndexNodeId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is CharacterMotionMatchingIndexNodeId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public enum MotionMatchingPoseSourceKind : byte
    {
        MotionMatching = 1
    }

    public enum MotionMatchingInvalidReason : byte
    {
        None = 0,
        InvalidIdentity = 1,
        InvalidQuery = 2,
        NoAdmittedCandidate = 3,
        NoValidPlan = 4,
        MissingClipBinding = 5,
        BrokenContinuation = 6,
        InvalidGeneration = 7,
        RuntimeDisposed = 8
    }

    public enum MotionMatchingCandidateRejectReason : byte
    {
        None = 0,
        IdentityMismatch = 1,
        SearchDomainMismatch = 2,
        InitializationNotAllowed = 3,
        JumpNotAllowed = 4,
        EntryExcluded = 5,
        ExitExcluded = 6,
        MinimumJumpInterval = 7,
        InsufficientPlanHorizon = 8,
        BrokenContinuation = 9,
        LeftContactMismatch = 10,
        RightContactMismatch = 11,
        LeftContactPositionJump = 12,
        RightContactPositionJump = 13,
        LeftContactVelocityJump = 14,
        RightContactVelocityJump = 15,
        NonFiniteFeature = 16,
        MissingClipBinding = 17
    }

    public enum MotionMatchingSearchTriggerReason : byte
    {
        Cadence = 1,
        Initialization = 2,
        MandatoryBoundary = 3,
        PlanInvalidated = 4,
        DomainActivated = 5,
        PresentationReset = 6
    }

    public enum MotionMatchingSelectionDecisionKind : byte
    {
        Continue = 1,
        Jump = 2,
        Initialize = 3,
        Invalid = 4
    }
}
