using System;
using System.Globalization;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Animation.TransitionRouting
{
    static class TransitionRoutingIdentity
    {
        public static string Normalize(string value) => value == null ? string.Empty : value.Trim();

        public static int GetHashCode(string value) =>
            string.IsNullOrEmpty(value) ? 0 : StringComparer.Ordinal.GetHashCode(value);
    }

    public readonly struct TransitionEndpointId : IEquatable<TransitionEndpointId>, IComparable<TransitionEndpointId>
    {
        public static TransitionEndpointId Empty { get; } = new TransitionEndpointId("$empty");

        public TransitionEndpointId(string value)
        {
            Value = TransitionRoutingIdentity.Normalize(value);
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public bool IsEmpty => Equals(Empty);
        public int CompareTo(TransitionEndpointId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(TransitionEndpointId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is TransitionEndpointId other && Equals(other);
        public override int GetHashCode() => TransitionRoutingIdentity.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(TransitionEndpointId left, TransitionEndpointId right) => left.Equals(right);
        public static bool operator !=(TransitionEndpointId left, TransitionEndpointId right) => !left.Equals(right);
    }

    public readonly struct TransitionRuleId : IEquatable<TransitionRuleId>, IComparable<TransitionRuleId>
    {
        public TransitionRuleId(string value)
        {
            Value = TransitionRoutingIdentity.Normalize(value);
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(TransitionRuleId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(TransitionRuleId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is TransitionRuleId other && Equals(other);
        public override int GetHashCode() => TransitionRoutingIdentity.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(TransitionRuleId left, TransitionRuleId right) => left.Equals(right);
        public static bool operator !=(TransitionRuleId left, TransitionRuleId right) => !left.Equals(right);
    }

    public readonly struct TransitionRouteOwnerId : IEquatable<TransitionRouteOwnerId>
    {
        public TransitionRouteOwnerId(string value)
        {
            Value = TransitionRoutingIdentity.Normalize(value);
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public bool Equals(TransitionRouteOwnerId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is TransitionRouteOwnerId other && Equals(other);
        public override int GetHashCode() => TransitionRoutingIdentity.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(TransitionRouteOwnerId left, TransitionRouteOwnerId right) => left.Equals(right);
        public static bool operator !=(TransitionRouteOwnerId left, TransitionRouteOwnerId right) => !left.Equals(right);
    }

    public readonly struct TransitionDefinitionRevision : IEquatable<TransitionDefinitionRevision>
    {
        public TransitionDefinitionRevision(string value)
        {
            Value = TransitionRoutingIdentity.Normalize(value);
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public bool Equals(TransitionDefinitionRevision other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is TransitionDefinitionRevision other && Equals(other);
        public override int GetHashCode() => TransitionRoutingIdentity.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct TransitionBlendCurveId : IEquatable<TransitionBlendCurveId>
    {
        public TransitionBlendCurveId(string value)
        {
            Value = TransitionRoutingIdentity.Normalize(value);
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public bool Equals(TransitionBlendCurveId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is TransitionBlendCurveId other && Equals(other);
        public override int GetHashCode() => TransitionRoutingIdentity.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct TransitionBlendProfileId : IEquatable<TransitionBlendProfileId>
    {
        public TransitionBlendProfileId(string value)
        {
            Value = TransitionRoutingIdentity.Normalize(value);
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public bool Equals(TransitionBlendProfileId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is TransitionBlendProfileId other && Equals(other);
        public override int GetHashCode() => TransitionRoutingIdentity.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct TransitionRoutingPlanId : IEquatable<TransitionRoutingPlanId>
    {
        public TransitionRoutingPlanId(StableHash value)
        {
            Value = value;
        }

        public StableHash Value { get; }
        public bool IsValid => Value.IsValid;
        public bool Equals(TransitionRoutingPlanId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is TransitionRoutingPlanId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
        public static bool operator ==(TransitionRoutingPlanId left, TransitionRoutingPlanId right) => left.Equals(right);
        public static bool operator !=(TransitionRoutingPlanId left, TransitionRoutingPlanId right) => !left.Equals(right);
    }

    public readonly struct TransitionFrameId : IEquatable<TransitionFrameId>, IComparable<TransitionFrameId>
    {
        public TransitionFrameId(ulong value)
        {
            Value = value;
        }

        public ulong Value { get; }
        public bool IsValid => Value > 0;
        public int CompareTo(TransitionFrameId other) => Value.CompareTo(other.Value);
        public bool Equals(TransitionFrameId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is TransitionFrameId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
        public static bool operator ==(TransitionFrameId left, TransitionFrameId right) => left.Equals(right);
        public static bool operator !=(TransitionFrameId left, TransitionFrameId right) => !left.Equals(right);
    }

    public readonly struct TransitionSelectionGeneration : IEquatable<TransitionSelectionGeneration>
    {
        public TransitionSelectionGeneration(ulong value)
        {
            Value = value;
        }

        public ulong Value { get; }
        public bool IsValid => Value > 0;
        public bool Equals(TransitionSelectionGeneration other) => Value == other.Value;
        public override bool Equals(object obj) => obj is TransitionSelectionGeneration other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
        public static bool operator ==(TransitionSelectionGeneration left, TransitionSelectionGeneration right) => left.Equals(right);
        public static bool operator !=(TransitionSelectionGeneration left, TransitionSelectionGeneration right) => !left.Equals(right);
    }

    public readonly struct TransitionRequestGeneration : IEquatable<TransitionRequestGeneration>
    {
        public TransitionRequestGeneration(ulong value)
        {
            Value = value;
        }

        public ulong Value { get; }
        public bool IsValid => Value > 0;
        public bool Equals(TransitionRequestGeneration other) => Value == other.Value;
        public override bool Equals(object obj) => obj is TransitionRequestGeneration other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
        public static bool operator ==(TransitionRequestGeneration left, TransitionRequestGeneration right) => left.Equals(right);
        public static bool operator !=(TransitionRequestGeneration left, TransitionRequestGeneration right) => !left.Equals(right);
    }

    public readonly struct TransitionRequestEventId : IEquatable<TransitionRequestEventId>
    {
        public TransitionRequestEventId(StableHash value)
        {
            Value = value;
        }

        public StableHash Value { get; }
        public bool IsValid => Value.IsValid;
        public bool Equals(TransitionRequestEventId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is TransitionRequestEventId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
        public static bool operator ==(TransitionRequestEventId left, TransitionRequestEventId right) => left.Equals(right);
        public static bool operator !=(TransitionRequestEventId left, TransitionRequestEventId right) => !left.Equals(right);
    }
}
