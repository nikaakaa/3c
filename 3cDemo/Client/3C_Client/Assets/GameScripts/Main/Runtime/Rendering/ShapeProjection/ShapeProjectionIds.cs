using System;
using UnityEngine;

namespace ThirdPersonRendering.ShapeProjection
{
    static class ShapeProjectionId
    {
        public static bool IsValid(string value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        public static bool Equals(string left, string right)
        {
            return string.Equals(left, right, StringComparison.Ordinal);
        }

        public static int GetHashCode(string value)
        {
            return value == null ? 0 : StringComparer.Ordinal.GetHashCode(value);
        }
    }

    [Serializable]
    public struct ShapeProjectionProfileId : IEquatable<ShapeProjectionProfileId>
    {
        [SerializeField] string value;

        public ShapeProjectionProfileId(string value) => this.value = value;
        public string Value => value;
        public bool IsValid => ShapeProjectionId.IsValid(Value);
        public bool Equals(ShapeProjectionProfileId other) => ShapeProjectionId.Equals(Value, other.Value);
        public override bool Equals(object obj) => obj is ShapeProjectionProfileId other && Equals(other);
        public override int GetHashCode() => ShapeProjectionId.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    [Serializable]
    public struct ShapeProjectionArtifactId : IEquatable<ShapeProjectionArtifactId>
    {
        [SerializeField] string value;

        public ShapeProjectionArtifactId(string value) => this.value = value;
        public string Value => value;
        public bool IsValid => ShapeProjectionId.IsValid(Value);
        public bool Equals(ShapeProjectionArtifactId other) => ShapeProjectionId.Equals(Value, other.Value);
        public override bool Equals(object obj) => obj is ShapeProjectionArtifactId other && Equals(other);
        public override int GetHashCode() => ShapeProjectionId.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    [Serializable]
    public struct ShapeProjectionSourceId : IEquatable<ShapeProjectionSourceId>
    {
        [SerializeField] string value;

        public ShapeProjectionSourceId(string value) => this.value = value;
        public string Value => value;
        public bool IsValid => ShapeProjectionId.IsValid(Value);
        public bool Equals(ShapeProjectionSourceId other) => ShapeProjectionId.Equals(Value, other.Value);
        public override bool Equals(object obj) => obj is ShapeProjectionSourceId other && Equals(other);
        public override int GetHashCode() => ShapeProjectionId.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    [Serializable]
    public struct ShapeProjectionRegionId : IEquatable<ShapeProjectionRegionId>
    {
        [SerializeField] string value;

        public ShapeProjectionRegionId(string value) => this.value = value;
        public string Value => value;
        public bool IsValid => ShapeProjectionId.IsValid(Value);
        public bool Equals(ShapeProjectionRegionId other) => ShapeProjectionId.Equals(Value, other.Value);
        public override bool Equals(object obj) => obj is ShapeProjectionRegionId other && Equals(other);
        public override int GetHashCode() => ShapeProjectionId.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    [Serializable]
    public struct ShapeProjectionChainId : IEquatable<ShapeProjectionChainId>
    {
        [SerializeField] string value;

        public ShapeProjectionChainId(string value) => this.value = value;
        public string Value => value;
        public bool IsValid => ShapeProjectionId.IsValid(Value);
        public bool Equals(ShapeProjectionChainId other) => ShapeProjectionId.Equals(Value, other.Value);
        public override bool Equals(object obj) => obj is ShapeProjectionChainId other && Equals(other);
        public override int GetHashCode() => ShapeProjectionId.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }
}
