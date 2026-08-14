using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ThirdPersonSimulation
{
    static class SimulationIdentity
    {
        public static string Require(string value, string parameter)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Identity is required.", parameter);
            return value.Trim();
        }

        public static string Hash(params string[] values)
        {
            using SHA256 sha = SHA256.Create();
            string joined = string.Join("\u001f", values ?? Array.Empty<string>());
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(joined));
            var builder = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
                builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }
    }

    public readonly struct ProgramId : IEquatable<ProgramId>, IComparable<ProgramId>
    {
        public ProgramId(string value) { Value = SimulationIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(ProgramId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(ProgramId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ProgramId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(ProgramId left, ProgramId right) => left.Equals(right);
        public static bool operator !=(ProgramId left, ProgramId right) => !left.Equals(right);
    }

    public readonly struct ActorId : IEquatable<ActorId>, IComparable<ActorId>
    {
        public ActorId(string value) { Value = SimulationIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(ActorId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(ActorId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ActorId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(ActorId left, ActorId right) => left.Equals(right);
        public static bool operator !=(ActorId left, ActorId right) => !left.Equals(right);
    }

    public readonly struct StableHash : IEquatable<StableHash>, IComparable<StableHash>
    {
        public StableHash(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 64)
                throw new ArgumentException("Stable hash must contain 64 lowercase hexadecimal characters.", nameof(value));
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
                    throw new ArgumentException("Stable hash must contain 64 lowercase hexadecimal characters.", nameof(value));
            }
            Value = value;
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(StableHash other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(StableHash other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is StableHash other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static StableHash Compute(params string[] values) => new StableHash(SimulationIdentity.Hash(values));
        public static bool operator ==(StableHash left, StableHash right) => left.Equals(right);
        public static bool operator !=(StableHash left, StableHash right) => !left.Equals(right);
    }

    public readonly struct ProgramRevision : IEquatable<ProgramRevision>
    {
        public ProgramRevision(string value) { Value = SimulationIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool Equals(ProgramRevision other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ProgramRevision other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct SemanticHash : IEquatable<SemanticHash>
    {
        public SemanticHash(StableHash value) { Value = value; }
        public StableHash Value { get; }
        public bool IsValid => Value.IsValid;
        public bool Equals(SemanticHash other) => Value == other.Value;
        public override bool Equals(object obj) => obj is SemanticHash other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
    }

    public readonly struct NumericProfileId : IEquatable<NumericProfileId>, IComparable<NumericProfileId>
    {
        public NumericProfileId(string value) { Value = SimulationIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(NumericProfileId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(NumericProfileId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is NumericProfileId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(NumericProfileId left, NumericProfileId right) => left.Equals(right);
        public static bool operator !=(NumericProfileId left, NumericProfileId right) => !left.Equals(right);
    }

    public readonly struct OperationSetVersion : IEquatable<OperationSetVersion>
    {
        public OperationSetVersion(string value) { Value = SimulationIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public bool Equals(OperationSetVersion other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is OperationSetVersion other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct TargetAbiVersion : IEquatable<TargetAbiVersion>
    {
        public TargetAbiVersion(int value)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }
        public int Value { get; }
        public bool IsValid => Value > 0;
        public bool Equals(TargetAbiVersion other) => Value == other.Value;
        public override bool Equals(object obj) => obj is TargetAbiVersion other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }

    public readonly struct ProgramHash : IEquatable<ProgramHash>
    {
        public ProgramHash(StableHash value) { Value = value; }
        public StableHash Value { get; }
        public bool IsValid => Value.IsValid;
        public bool Equals(ProgramHash other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ProgramHash other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
    }

    public readonly struct LayoutHash : IEquatable<LayoutHash>
    {
        public LayoutHash(StableHash value) { Value = value; }
        public StableHash Value { get; }
        public bool IsValid => Value.IsValid;
        public bool Equals(LayoutHash other) => Value == other.Value;
        public override bool Equals(object obj) => obj is LayoutHash other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
    }

    public readonly struct ProgramCatalogHash : IEquatable<ProgramCatalogHash>
    {
        public ProgramCatalogHash(StableHash value) { Value = value; }
        public StableHash Value { get; }
        public bool IsValid => Value.IsValid;
        public bool Equals(ProgramCatalogHash other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ProgramCatalogHash other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
    }

    public readonly struct WorldRevision : IEquatable<WorldRevision>
    {
        public WorldRevision(string value) { Value = SimulationIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool Equals(WorldRevision other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is WorldRevision other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct SolverImplementationId : IEquatable<SolverImplementationId>
    {
        public SolverImplementationId(string value) { Value = SimulationIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool Equals(SolverImplementationId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is SolverImplementationId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct HostProductId : IEquatable<HostProductId>, IComparable<HostProductId>
    {
        public HostProductId(string value) { Value = SimulationIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(HostProductId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(HostProductId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is HostProductId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(HostProductId left, HostProductId right) => left.Equals(right);
        public static bool operator !=(HostProductId left, HostProductId right) => !left.Equals(right);
    }

    public readonly struct SimulationTick : IEquatable<SimulationTick>, IComparable<SimulationTick>
    {
        public SimulationTick(ulong value)
        {
            if (value == 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }
        public ulong Value { get; }
        public bool IsValid => Value != 0;
        public int CompareTo(SimulationTick other) => Value.CompareTo(other.Value);
        public bool Equals(SimulationTick other) => Value == other.Value;
        public override bool Equals(object obj) => obj is SimulationTick other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
        public static bool operator ==(SimulationTick left, SimulationTick right) => left.Equals(right);
        public static bool operator !=(SimulationTick left, SimulationTick right) => !left.Equals(right);
    }

    public readonly struct CommittedMovementPlaybackClock : IEquatable<CommittedMovementPlaybackClock>
    {
        public CommittedMovementPlaybackClock(
            string ownerIdentity,
            ulong generation,
            SimulationTick authorityTick,
            int continuousTicks,
            int tickRate)
        {
            OwnerIdentity = SimulationIdentity.Require(ownerIdentity, nameof(ownerIdentity));
            if (generation == 0)
                throw new ArgumentOutOfRangeException(nameof(generation));
            if (!authorityTick.IsValid)
                throw new ArgumentException("Movement playback authority tick is invalid.", nameof(authorityTick));
            if (continuousTicks < 0)
                throw new ArgumentOutOfRangeException(nameof(continuousTicks));
            if (tickRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickRate));
            Generation = generation;
            AuthorityTick = authorityTick;
            ContinuousTicks = continuousTicks;
            TickRate = tickRate;
        }

        public string OwnerIdentity { get; }
        public ulong Generation { get; }
        public SimulationTick AuthorityTick { get; }
        public int ContinuousTicks { get; }
        public int TickRate { get; }
        public bool IsValid =>
            !string.IsNullOrEmpty(OwnerIdentity) &&
            Generation != 0 &&
            AuthorityTick.IsValid &&
            ContinuousTicks >= 0 &&
            TickRate > 0;
        public double ElapsedSeconds => IsValid ? (double)ContinuousTicks / TickRate : 0d;
        public bool Equals(CommittedMovementPlaybackClock other) =>
            Generation == other.Generation &&
            AuthorityTick == other.AuthorityTick &&
            ContinuousTicks == other.ContinuousTicks &&
            TickRate == other.TickRate &&
            string.Equals(OwnerIdentity, other.OwnerIdentity, StringComparison.Ordinal);
        public override bool Equals(object obj) =>
            obj is CommittedMovementPlaybackClock other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(
            OwnerIdentity == null ? 0 : StringComparer.Ordinal.GetHashCode(OwnerIdentity),
            Generation,
            AuthorityTick,
            ContinuousTicks,
            TickRate);
        public static bool operator ==(
            CommittedMovementPlaybackClock left,
            CommittedMovementPlaybackClock right) => left.Equals(right);
        public static bool operator !=(
            CommittedMovementPlaybackClock left,
            CommittedMovementPlaybackClock right) => !left.Equals(right);
    }

    public readonly struct CommittedLocomotionPlanarMotionTimeline : IEquatable<CommittedLocomotionPlanarMotionTimeline>
    {
        public CommittedLocomotionPlanarMotionTimeline(
            string ownerIdentity,
            ulong generation,
            SimulationTick authorityTick,
            int tickRate,
            float currentVelocityX,
            float currentVelocityZ,
            float yawVelocityDegreesPerSecond,
            float maximumYawVelocityDegreesPerSecond,
            int currentSegmentDurationTicks,
            string continuationOwnerIdentity,
            float continuationVelocityX,
            float continuationVelocityZ)
        {
            OwnerIdentity = SimulationIdentity.Require(ownerIdentity, nameof(ownerIdentity));
            if (generation == 0)
                throw new ArgumentOutOfRangeException(nameof(generation));
            if (!authorityTick.IsValid)
                throw new ArgumentException("Locomotion motion timeline authority tick is invalid.", nameof(authorityTick));
            if (tickRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickRate));
            if (!float.IsFinite(currentVelocityX) || !float.IsFinite(currentVelocityZ) ||
                !float.IsFinite(yawVelocityDegreesPerSecond) ||
                !float.IsFinite(maximumYawVelocityDegreesPerSecond) ||
                maximumYawVelocityDegreesPerSecond < 0f ||
                currentSegmentDurationTicks < 0 ||
                !float.IsFinite(continuationVelocityX) || !float.IsFinite(continuationVelocityZ))
            {
                throw new ArgumentException("Locomotion motion timeline contains invalid motion data.");
            }
            if (currentSegmentDurationTicks == 0 && !string.IsNullOrEmpty(continuationOwnerIdentity))
                throw new ArgumentException("Continuous Locomotion motion timeline cannot declare a continuation.", nameof(continuationOwnerIdentity));
            if (string.IsNullOrEmpty(continuationOwnerIdentity) &&
                (continuationVelocityX != 0f || continuationVelocityZ != 0f))
            {
                throw new ArgumentException("Locomotion continuation velocity has no owner.", nameof(continuationOwnerIdentity));
            }
            OwnerIdentity = ownerIdentity;
            Generation = generation;
            AuthorityTick = authorityTick;
            TickRate = tickRate;
            CurrentVelocityX = currentVelocityX;
            CurrentVelocityZ = currentVelocityZ;
            YawVelocityDegreesPerSecond = yawVelocityDegreesPerSecond;
            MaximumYawVelocityDegreesPerSecond = maximumYawVelocityDegreesPerSecond;
            CurrentSegmentDurationTicks = currentSegmentDurationTicks;
            ContinuationOwnerIdentity = continuationOwnerIdentity ?? string.Empty;
            ContinuationVelocityX = continuationVelocityX;
            ContinuationVelocityZ = continuationVelocityZ;
        }

        public string OwnerIdentity { get; }
        public ulong Generation { get; }
        public SimulationTick AuthorityTick { get; }
        public int TickRate { get; }
        public float CurrentVelocityX { get; }
        public float CurrentVelocityZ { get; }
        public float YawVelocityDegreesPerSecond { get; }
        public float MaximumYawVelocityDegreesPerSecond { get; }
        public int CurrentSegmentDurationTicks { get; }
        public string ContinuationOwnerIdentity { get; }
        public float ContinuationVelocityX { get; }
        public float ContinuationVelocityZ { get; }
        public bool HasContinuation => !string.IsNullOrEmpty(ContinuationOwnerIdentity);
        public bool IsValid =>
            !string.IsNullOrEmpty(OwnerIdentity) &&
            Generation != 0 &&
            AuthorityTick.IsValid &&
            TickRate > 0 &&
            float.IsFinite(CurrentVelocityX) &&
            float.IsFinite(CurrentVelocityZ) &&
            float.IsFinite(YawVelocityDegreesPerSecond) &&
            float.IsFinite(MaximumYawVelocityDegreesPerSecond) &&
            MaximumYawVelocityDegreesPerSecond >= 0f &&
            CurrentSegmentDurationTicks >= 0 &&
            float.IsFinite(ContinuationVelocityX) &&
            float.IsFinite(ContinuationVelocityZ) &&
            (HasContinuation || ContinuationVelocityX == 0f && ContinuationVelocityZ == 0f);
        public double CurrentSegmentDurationSeconds =>
            CurrentSegmentDurationTicks > 0 ? (double)CurrentSegmentDurationTicks / TickRate : 0d;
        public bool Matches(CommittedMovementPlaybackClock clock) =>
            IsValid && clock.IsValid &&
            Generation == clock.Generation &&
            AuthorityTick == clock.AuthorityTick &&
            TickRate == clock.TickRate &&
            string.Equals(OwnerIdentity, clock.OwnerIdentity, StringComparison.Ordinal);
        public bool Equals(CommittedLocomotionPlanarMotionTimeline other) =>
            Generation == other.Generation &&
            AuthorityTick == other.AuthorityTick &&
            TickRate == other.TickRate &&
            CurrentVelocityX.Equals(other.CurrentVelocityX) &&
            CurrentVelocityZ.Equals(other.CurrentVelocityZ) &&
            YawVelocityDegreesPerSecond.Equals(other.YawVelocityDegreesPerSecond) &&
            MaximumYawVelocityDegreesPerSecond.Equals(other.MaximumYawVelocityDegreesPerSecond) &&
            CurrentSegmentDurationTicks == other.CurrentSegmentDurationTicks &&
            ContinuationVelocityX.Equals(other.ContinuationVelocityX) &&
            ContinuationVelocityZ.Equals(other.ContinuationVelocityZ) &&
            string.Equals(OwnerIdentity, other.OwnerIdentity, StringComparison.Ordinal) &&
            string.Equals(ContinuationOwnerIdentity, other.ContinuationOwnerIdentity, StringComparison.Ordinal);
        public override bool Equals(object obj) =>
            obj is CommittedLocomotionPlanarMotionTimeline other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(
            HashCode.Combine(
                OwnerIdentity == null ? 0 : StringComparer.Ordinal.GetHashCode(OwnerIdentity),
                Generation,
                AuthorityTick,
                TickRate,
                CurrentVelocityX,
                CurrentVelocityZ,
                YawVelocityDegreesPerSecond,
                CurrentSegmentDurationTicks),
            MaximumYawVelocityDegreesPerSecond,
            ContinuationOwnerIdentity == null ? 0 : StringComparer.Ordinal.GetHashCode(ContinuationOwnerIdentity),
            ContinuationVelocityX,
            ContinuationVelocityZ);
        public static bool operator ==(
            CommittedLocomotionPlanarMotionTimeline left,
            CommittedLocomotionPlanarMotionTimeline right) => left.Equals(right);
        public static bool operator !=(
            CommittedLocomotionPlanarMotionTimeline left,
            CommittedLocomotionPlanarMotionTimeline right) => !left.Equals(right);
    }

    public enum SimulationTickSourceKind : byte
    {
        LocalLogic = 1,
        Authoritative = 2,
        Replay = 3
    }

    public readonly struct SimulationTickSourceIdentity : IEquatable<SimulationTickSourceIdentity>
    {
        public SimulationTickSourceIdentity(SimulationTickSourceKind kind, string clockId, ulong sourceTick)
        {
            Kind = kind;
            ClockId = SimulationIdentity.Require(clockId, nameof(clockId));
            SourceTick = sourceTick;
        }
        public SimulationTickSourceKind Kind { get; }
        public string ClockId { get; }
        public ulong SourceTick { get; }
        public bool Equals(SimulationTickSourceIdentity other) => Kind == other.Kind && SourceTick == other.SourceTick && string.Equals(ClockId, other.ClockId, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is SimulationTickSourceIdentity other && Equals(other);
        public override int GetHashCode() => HashCode.Combine((int)Kind, ClockId == null ? 0 : StringComparer.Ordinal.GetHashCode(ClockId), SourceTick);
    }

    public readonly struct OperationHandle : IEquatable<OperationHandle>, IComparable<OperationHandle>
    {
        readonly int m_EncodedValue;

        public static OperationHandle Invalid => default;

        public OperationHandle(int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            m_EncodedValue = checked(value + 1);
        }
        public int Value => m_EncodedValue - 1;
        public bool IsValid => m_EncodedValue > 0;
        public int CompareTo(OperationHandle other) => Value.CompareTo(other.Value);
        public bool Equals(OperationHandle other) => Value == other.Value;
        public override bool Equals(object obj) => obj is OperationHandle other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }

    public readonly struct ActivationId : IEquatable<ActivationId>
    {
        public ActivationId(OperationHandle operation, ulong generation, string executionPath)
        {
            if (generation == 0)
                throw new ArgumentOutOfRangeException(nameof(generation));
            Operation = operation;
            Generation = generation;
            ExecutionPath = executionPath ?? string.Empty;
        }
        public OperationHandle Operation { get; }
        public ulong Generation { get; }
        public string ExecutionPath { get; }
        public bool IsValid => Generation != 0;
        public bool Equals(ActivationId other) => Operation.Equals(other.Operation) && Generation == other.Generation && string.Equals(ExecutionPath, other.ExecutionPath, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ActivationId other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Operation, Generation, ExecutionPath == null ? 0 : StringComparer.Ordinal.GetHashCode(ExecutionPath));
        public override string ToString() => $"{Operation}/{Generation}/{ExecutionPath}";
    }

    public readonly struct WorldRequestId : IEquatable<WorldRequestId>
    {
        public WorldRequestId(ActorId actorId, SimulationTick tick, ulong sequence)
        {
            if (!actorId.IsValid || !tick.IsValid || sequence == 0)
                throw new ArgumentException("World request identity is incomplete.");
            ActorId = actorId;
            Tick = tick;
            Sequence = sequence;
        }
        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public ulong Sequence { get; }
        public bool IsValid => ActorId.IsValid && Tick.IsValid && Sequence != 0;
        public bool Equals(WorldRequestId other) => ActorId == other.ActorId && Tick == other.Tick && Sequence == other.Sequence;
        public override bool Equals(object obj) => obj is WorldRequestId other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(ActorId, Tick, Sequence);
        public override string ToString() => $"{ActorId}/{Tick}/{Sequence}";
    }

    public readonly struct EventId : IEquatable<EventId>, IComparable<EventId>
    {
        public EventId(StableHash value) { Value = value; }
        public StableHash Value { get; }
        public bool IsValid => Value.IsValid;
        public int CompareTo(EventId other) => Value.CompareTo(other.Value);
        public bool Equals(EventId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is EventId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
        public static EventId Create(ProgramHash program, ActorId actor, ActivationId activation, SimulationTick tick, ulong sequence, string channel)
        {
            if (!program.IsValid || !actor.IsValid || !activation.IsValid || !tick.IsValid || sequence == 0)
                throw new ArgumentException("Event identity is incomplete.");
            return new EventId(StableHash.Compute(program.ToString(), actor.ToString(), activation.ToString(), tick.ToString(), sequence.ToString(CultureInfo.InvariantCulture), channel ?? string.Empty));
        }
    }

    public readonly struct CharacterStateHash : IEquatable<CharacterStateHash>
    {
        public CharacterStateHash(StableHash value) { Value = value; }
        public StableHash Value { get; }
        public bool IsValid => Value.IsValid;
        public bool Equals(CharacterStateHash other) => Value == other.Value;
        public override bool Equals(object obj) => obj is CharacterStateHash other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
    }

    public readonly struct SimulationWorldHash : IEquatable<SimulationWorldHash>
    {
        public SimulationWorldHash(StableHash value) { Value = value; }
        public StableHash Value { get; }
        public bool IsValid => Value.IsValid;
        public bool Equals(SimulationWorldHash other) => Value == other.Value;
        public override bool Equals(object obj) => obj is SimulationWorldHash other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
    }
}
