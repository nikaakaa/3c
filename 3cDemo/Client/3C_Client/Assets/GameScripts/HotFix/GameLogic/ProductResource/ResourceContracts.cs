using System;

namespace GameLogic.ProductResource
{
    public enum ResourceScopeKind
    {
        Global = 0,
        Home = 1,
        Gameplay = 2,
        Transient = 3
    }

    public enum ResourceScopeState
    {
        Active = 0,
        Closing = 1,
        Disposed = 2
    }

    public enum ResourceMaintenanceReason
    {
        SceneTransitionCompleted = 0,
        ReturnHomeLoading = 1,
        ExplicitMaintenance = 2,
        LowMemory = 3
    }

    public readonly struct ResourceScopeId : IEquatable<ResourceScopeId>
    {
        public ResourceScopeId(long value)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            Value = value;
        }

        public long Value { get; }

        public bool Equals(ResourceScopeId other) => Value == other.Value;

        public override bool Equals(object obj) => obj is ResourceScopeId other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value.ToString();

        public static bool operator ==(ResourceScopeId left, ResourceScopeId right) => left.Equals(right);

        public static bool operator !=(ResourceScopeId left, ResourceScopeId right) => !left.Equals(right);
    }

    public readonly struct ResourceIdentity : IEquatable<ResourceIdentity>
    {
        public ResourceIdentity(string packageName, string location, Type assetType)
        {
            PackageName = NormalizeRequired(packageName, nameof(packageName));
            Location = NormalizeLocation(location);
            AssetType = assetType ?? throw new ArgumentNullException(nameof(assetType));
        }

        public string PackageName { get; }

        public string Location { get; }

        public Type AssetType { get; }

        public bool Equals(ResourceIdentity other)
        {
            return string.Equals(PackageName, other.PackageName, StringComparison.Ordinal) &&
                   string.Equals(Location, other.Location, StringComparison.Ordinal) &&
                   AssetType == other.AssetType;
        }

        public override bool Equals(object obj) => obj is ResourceIdentity other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = StringComparer.Ordinal.GetHashCode(PackageName);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Location);
                hash = (hash * 397) ^ AssetType.GetHashCode();
                return hash;
            }
        }

        public override string ToString() => $"{PackageName}:{Location}:{AssetType.FullName}";

        public static bool operator ==(ResourceIdentity left, ResourceIdentity right) => left.Equals(right);

        public static bool operator !=(ResourceIdentity left, ResourceIdentity right) => !left.Equals(right);

        private static string NormalizeLocation(string location)
        {
            string value = NormalizeRequired(location, nameof(location)).Replace('\\', '/');
            while (value.Contains("//"))
            {
                value = value.Replace("//", "/");
            }

            return value;
        }

        private static string NormalizeRequired(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value is required.", parameterName);
            }

            return value.Trim();
        }
    }
}
