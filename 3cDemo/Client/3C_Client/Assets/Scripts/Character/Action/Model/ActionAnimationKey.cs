using System;

namespace ThirdPersonAction
{
    [Serializable]
    public readonly struct ActionAnimationKey : IEquatable<ActionAnimationKey>
    {
        readonly string value;

        public ActionAnimationKey(string value)
        {
            this.value = (value ?? string.Empty).Trim();
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(ActionAnimationKey other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActionAnimationKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(ActionAnimationKey left, ActionAnimationKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActionAnimationKey left, ActionAnimationKey right)
        {
            return !left.Equals(right);
        }
    }
}
