using System;

namespace ThirdPersonAction
{
    [Serializable]
    public readonly struct ActionStateId : IEquatable<ActionStateId>
    {
        public static readonly ActionStateId Empty = new ActionStateId(string.Empty);
        public static readonly ActionStateId Any = new ActionStateId("*");

        readonly string value;

        public ActionStateId(string value)
        {
            this.value = value ?? string.Empty;
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);
        public bool IsWildcard => string.Equals(Value, Any.Value, StringComparison.Ordinal);

        public bool Matches(ActionStateId other)
        {
            return IsWildcard || other.IsWildcard || Equals(other);
        }

        public bool Equals(ActionStateId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActionStateId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(ActionStateId left, ActionStateId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActionStateId left, ActionStateId right)
        {
            return !left.Equals(right);
        }
    }
}
