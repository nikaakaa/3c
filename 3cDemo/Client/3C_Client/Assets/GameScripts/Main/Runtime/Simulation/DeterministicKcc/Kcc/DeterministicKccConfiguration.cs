using System;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    public enum DeterministicActorContactResponseKind : byte
    {
        SolidBodyBlock = 1
    }

    public enum DeterministicKccQueryStage : byte
    {
        CandidateGather = 1,
        Distance = 2,
        ShapeCast = 3,
        Overlap = 4,
        PenetrationRecovery = 5,
        Movement = 6,
        Step = 7,
        Ground = 8,
        StaticReconstraint = 9
    }

    public sealed class DeterministicKccQueryException : InvalidOperationException
    {
        public DeterministicKccQueryException(
            DeterministicKccQueryStage stage,
            string detail,
            int primitiveId = -1,
            int requiredCapacity = 0,
            int configuredCapacity = 0)
            : base($"Deterministic KCC query failed: stage={stage}; primitive={primitiveId}; required={requiredCapacity}; capacity={configuredCapacity}; detail={detail}")
        {
            Stage = stage;
            PrimitiveId = primitiveId;
            RequiredCapacity = requiredCapacity;
            ConfiguredCapacity = configuredCapacity;
        }

        public DeterministicKccQueryStage Stage { get; }
        public int PrimitiveId { get; }
        public int RequiredCapacity { get; }
        public int ConfiguredCapacity { get; }
    }

    public readonly struct DeterministicActorContactShape : IEquatable<DeterministicActorContactShape>
    {
        public DeterministicActorContactShape(FixedScalar radius, FixedScalar height, FixedScalar skinWidth)
        {
            if (radius <= FixedScalar.Zero)
                throw new ArgumentOutOfRangeException(nameof(radius));
            if (height <= radius + radius)
                throw new ArgumentOutOfRangeException(nameof(height));
            if (skinWidth < FixedScalar.Zero || skinWidth >= radius)
                throw new ArgumentOutOfRangeException(nameof(skinWidth));
            Radius = radius;
            Height = height;
            SkinWidth = skinWidth;
            ConfigurationHash = default;
            ConfigurationHash = DeterministicActorContactShapeCodec.ComputeHash(this);
        }

        public FixedScalar Radius { get; }
        public FixedScalar Height { get; }
        public FixedScalar SkinWidth { get; }
        public FixedScalar SeparationRadius => Radius + SkinWidth;
        public StableHash ConfigurationHash { get; }

        public bool Equals(DeterministicActorContactShape other) =>
            Radius == other.Radius && Height == other.Height && SkinWidth == other.SkinWidth;

        public override bool Equals(object obj) => obj is DeterministicActorContactShape other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Radius, Height, SkinWidth);
        public static bool operator ==(DeterministicActorContactShape left, DeterministicActorContactShape right) => left.Equals(right);
        public static bool operator !=(DeterministicActorContactShape left, DeterministicActorContactShape right) => !left.Equals(right);
    }

    public static class DeterministicActorContactShapeCodec
    {
        const uint Magic = 0x48534341;
        const int Version = 1;

        public static byte[] Write(DeterministicActorContactShape shape)
        {
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(Magic);
            writer.WriteInt32(Version);
            writer.WriteInt64(shape.Radius.Raw);
            writer.WriteInt64(shape.Height.Raw);
            writer.WriteInt64(shape.SkinWidth.Raw);
            return writer.ToArray();
        }

        public static DeterministicActorContactShape Read(byte[] bytes)
        {
            var reader = new CanonicalReader(bytes ?? throw new ArgumentNullException(nameof(bytes)));
            if (reader.ReadUInt32() != Magic || reader.ReadInt32() != Version)
                throw new InvalidOperationException("Deterministic Actor contact shape header is invalid.");
            var shape = new DeterministicActorContactShape(
                FixedScalar.FromRaw(reader.ReadInt64()),
                FixedScalar.FromRaw(reader.ReadInt64()),
                FixedScalar.FromRaw(reader.ReadInt64()));
            reader.RequireComplete();
            return shape;
        }

        public static StableHash ComputeHash(DeterministicActorContactShape shape)
        {
            using var writer = new CanonicalWriter();
            writer.WriteString("deterministic-actor-contact-shape/1");
            writer.WriteBytes(WriteWithoutHash(shape));
            return writer.ComputeHash();
        }

        static byte[] WriteWithoutHash(DeterministicActorContactShape shape)
        {
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(Magic);
            writer.WriteInt32(Version);
            writer.WriteInt64(shape.Radius.Raw);
            writer.WriteInt64(shape.Height.Raw);
            writer.WriteInt64(shape.SkinWidth.Raw);
            return writer.ToArray();
        }
    }

    public sealed class DeterministicKccConfiguration
    {
        public const string ActorContactPolicyVersion = "solid-body-block/1";
        public const string QuerySemanticVersion = "fixed-capsule-conservative-cast/3";
        public const string MotorSemanticVersion = "fixed-kcc-motor/2";

        public DeterministicKccConfiguration(
            FixedScalar radius,
            FixedScalar height,
            FixedScalar skinWidth,
            FixedScalar minimumGroundNormalY,
            FixedScalar maximumStepHeight,
            FixedScalar groundSnapDistance,
            FixedScalar maximumMovementDistance,
            FixedScalar queryTolerance,
            FixedScalar minimumMovementDistance,
            FixedScalar minimumStepForwardDistance,
            FixedScalar normalMergeDot,
            int maximumSweepIterations,
            int maximumContactIterations,
            int maximumCandidates,
            int maximumContacts,
            int maximumActorPairs,
            int maximumActorContactIterations,
            DeterministicActorContactResponseKind actorContactResponse = DeterministicActorContactResponseKind.SolidBodyBlock)
        {
            if (radius <= FixedScalar.Zero || height <= radius + radius || skinWidth < FixedScalar.Zero ||
                minimumGroundNormalY <= FixedScalar.Zero || minimumGroundNormalY > FixedScalar.One ||
                maximumStepHeight < FixedScalar.Zero || groundSnapDistance < FixedScalar.Zero ||
                maximumMovementDistance <= FixedScalar.Zero || queryTolerance <= FixedScalar.Zero ||
                queryTolerance >= skinWidth || minimumMovementDistance <= FixedScalar.Zero ||
                minimumStepForwardDistance <= FixedScalar.Zero || normalMergeDot <= FixedScalar.Zero ||
                normalMergeDot >= FixedScalar.One || maximumSweepIterations <= 0 || maximumContactIterations <= 0 ||
                maximumCandidates <= 0 || maximumContacts <= 0 || maximumActorPairs <= 0 ||
                maximumActorPairs > 4096 || maximumActorContactIterations <= 0 || maximumActorContactIterations > 32 ||
                actorContactResponse != DeterministicActorContactResponseKind.SolidBodyBlock)
            {
                throw new ArgumentException("Deterministic KCC configuration is invalid.");
            }
            ActorContactShape = new DeterministicActorContactShape(radius, height, skinWidth);
            MinimumGroundNormalY = minimumGroundNormalY;
            MaximumStepHeight = maximumStepHeight;
            GroundSnapDistance = groundSnapDistance;
            MaximumMovementDistance = maximumMovementDistance;
            QueryTolerance = queryTolerance;
            MinimumMovementDistance = minimumMovementDistance;
            MinimumStepForwardDistance = minimumStepForwardDistance;
            NormalMergeDot = normalMergeDot;
            MaximumSweepIterations = maximumSweepIterations;
            MaximumContactIterations = maximumContactIterations;
            MaximumCandidates = maximumCandidates;
            MaximumContacts = maximumContacts;
            MaximumActorPairs = maximumActorPairs;
            MaximumActorContactIterations = maximumActorContactIterations;
            ActorContactResponse = actorContactResponse;
            ConfigurationHash = StableHash.Compute(
                "deterministic-kcc-configuration/4",
                ActorContactShape.ConfigurationHash.Value,
                minimumGroundNormalY.Raw.ToString(),
                maximumStepHeight.Raw.ToString(),
                groundSnapDistance.Raw.ToString(),
                maximumMovementDistance.Raw.ToString(),
                queryTolerance.Raw.ToString(),
                minimumMovementDistance.Raw.ToString(),
                minimumStepForwardDistance.Raw.ToString(),
                normalMergeDot.Raw.ToString(),
                maximumSweepIterations.ToString(),
                maximumContactIterations.ToString(),
                maximumCandidates.ToString(),
                maximumContacts.ToString(),
                maximumActorPairs.ToString(),
                maximumActorContactIterations.ToString(),
                ((byte)actorContactResponse).ToString(),
                ActorContactPolicyVersion,
                QuerySemanticVersion,
                MotorSemanticVersion);
        }

        public DeterministicActorContactShape ActorContactShape { get; }
        public FixedScalar Radius => ActorContactShape.Radius;
        public FixedScalar Height => ActorContactShape.Height;
        public FixedScalar SkinWidth => ActorContactShape.SkinWidth;
        public FixedScalar MinimumGroundNormalY { get; }
        public FixedScalar MaximumStepHeight { get; }
        public FixedScalar GroundSnapDistance { get; }
        public FixedScalar MaximumMovementDistance { get; }
        public FixedScalar QueryTolerance { get; }
        public FixedScalar MinimumMovementDistance { get; }
        public FixedScalar MinimumStepForwardDistance { get; }
        public FixedScalar NormalMergeDot { get; }
        public int MaximumSweepIterations { get; }
        public int MaximumContactIterations { get; }
        public int MaximumCandidates { get; }
        public int MaximumContacts { get; }
        public int MaximumActorPairs { get; }
        public int MaximumActorContactIterations { get; }
        public DeterministicActorContactResponseKind ActorContactResponse { get; }
        public StableHash ConfigurationHash { get; }

        public static DeterministicKccConfiguration Default { get; } = new DeterministicKccConfiguration(
            FixedScalar.FromRatio(35, 100),
            FixedScalar.FromRatio(18, 10),
            FixedScalar.FromRatio(5, 1000),
            FixedScalar.FromRatio(707106, 1000000),
            FixedScalar.FromRatio(3, 10),
            FixedScalar.FromRatio(12, 100),
            FixedScalar.FromInt64(3),
            FixedScalar.FromRatio(1, 100000),
            FixedScalar.FromRatio(1, 100000),
            FixedScalar.FromRatio(1, 100),
            FixedScalar.FromRatio(9999, 10000),
            16,
            8,
            256,
            32,
            64,
            8);
    }

    public readonly struct DeterministicKccContact
    {
        public DeterministicKccContact(
            int primitiveId,
            int surfaceId,
            DeterministicCollisionFeatureId featureId,
            FixedVector3 normal,
            FixedVector3 characterPoint,
            FixedVector3 worldPoint,
            FixedScalar separation,
            FixedScalar timeOfImpact)
        {
            if (primitiveId < 0 || surfaceId < 0 || !featureId.IsValid ||
                normal.SqrMagnitude == FixedScalar.Zero || timeOfImpact < FixedScalar.Zero || timeOfImpact > FixedScalar.One)
            {
                throw new ArgumentException("Deterministic KCC contact is invalid.");
            }
            PrimitiveId = primitiveId;
            SurfaceId = surfaceId;
            FeatureId = featureId;
            Normal = normal;
            CharacterPoint = characterPoint;
            WorldPoint = worldPoint;
            Separation = separation;
            TimeOfImpact = timeOfImpact;
        }

        public int PrimitiveId { get; }
        public int SurfaceId { get; }
        public DeterministicCollisionFeatureId FeatureId { get; }
        public FixedVector3 Normal { get; }
        public FixedVector3 CharacterPoint { get; }
        public FixedVector3 WorldPoint { get; }
        public FixedScalar Separation { get; }
        public FixedScalar TimeOfImpact { get; }
        public FixedScalar Penetration => Separation < FixedScalar.Zero ? -Separation : FixedScalar.Zero;

        public DeterministicKccContact WithTimeOfImpact(FixedScalar timeOfImpact) => new DeterministicKccContact(
            PrimitiveId,
            SurfaceId,
            FeatureId,
            Normal,
            CharacterPoint,
            WorldPoint,
            Separation,
            timeOfImpact);
    }

    public readonly struct DeterministicKccQuerySummary
    {
        public DeterministicKccQuerySummary(int queryCount, int candidateCount, int contactCount, int iterationCount)
        {
            QueryCount = queryCount;
            CandidateCount = candidateCount;
            ContactCount = contactCount;
            IterationCount = iterationCount;
        }

        public int QueryCount { get; }
        public int CandidateCount { get; }
        public int ContactCount { get; }
        public int IterationCount { get; }
        public DeterministicKccQuerySummary Add(DeterministicKccQuerySummary other) => new DeterministicKccQuerySummary(
            checked(QueryCount + other.QueryCount),
            checked(CandidateCount + other.CandidateCount),
            checked(ContactCount + other.ContactCount),
            checked(IterationCount + other.IterationCount));
    }
}
