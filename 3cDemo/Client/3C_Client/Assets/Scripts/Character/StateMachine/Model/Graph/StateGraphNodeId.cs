using System;

namespace ThirdPersonCharacterStateMachine
{
    public readonly struct StateGraphNodeId : IEquatable<StateGraphNodeId>
    {
        readonly string value;

        public StateGraphNodeId(string value)
        {
            this.value = Normalize(value);
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(StateGraphNodeId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is StateGraphNodeId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(StateGraphNodeId left, StateGraphNodeId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StateGraphNodeId left, StateGraphNodeId right)
        {
            return !left.Equals(right);
        }

        public static string Normalize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            string[] parts = raw.Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join("/", parts);
        }
    }
}
