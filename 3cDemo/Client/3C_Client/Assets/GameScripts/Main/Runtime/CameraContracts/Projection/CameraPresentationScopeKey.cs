using System;

namespace ThirdPersonCamera
{
    public readonly struct CameraPresentationScopeKey : IEquatable<CameraPresentationScopeKey>
    {
        public CameraPresentationScopeKey(
            string sourceId,
            ulong generation,
            ulong sourceActionInstanceId)
        {
            SourceId = sourceId ?? string.Empty;
            Generation = generation;
            SourceActionInstanceId = sourceActionInstanceId;
        }

        public string SourceId { get; }
        public ulong Generation { get; }
        public ulong SourceActionInstanceId { get; }

        public bool Equals(CameraPresentationScopeKey other) =>
            Generation == other.Generation &&
            SourceActionInstanceId == other.SourceActionInstanceId &&
            string.Equals(SourceId, other.SourceId, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is CameraPresentationScopeKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(
            SourceId,
            Generation,
            SourceActionInstanceId);
    }
}
