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
        StaticReconstraint = 9,
        Raycast = 10
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
        public DeterministicActorContactShape(FixedScalar radius, FixedScalar height, FixedScalar collisionOffset)
        {
            if (radius <= FixedScalar.Zero)
                throw new ArgumentOutOfRangeException(nameof(radius));
            if (height <= radius + radius)
                throw new ArgumentOutOfRangeException(nameof(height));
            if (collisionOffset <= FixedScalar.Zero || collisionOffset >= radius)
                throw new ArgumentOutOfRangeException(nameof(collisionOffset));
            Radius = radius;
            Height = height;
            CollisionOffset = collisionOffset;
            ConfigurationHash = default;
            ConfigurationHash = DeterministicActorContactShapeCodec.ComputeHash(this);
        }

        public FixedScalar Radius { get; }
        public FixedScalar Height { get; }
        public FixedScalar CollisionOffset { get; }
        public FixedScalar SeparationRadius => Radius + CollisionOffset;
        public StableHash ConfigurationHash { get; }

        public bool Equals(DeterministicActorContactShape other) =>
            Radius == other.Radius && Height == other.Height && CollisionOffset == other.CollisionOffset;

        public override bool Equals(object obj) => obj is DeterministicActorContactShape other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Radius, Height, CollisionOffset);
        public static bool operator ==(DeterministicActorContactShape left, DeterministicActorContactShape right) => left.Equals(right);
        public static bool operator !=(DeterministicActorContactShape left, DeterministicActorContactShape right) => !left.Equals(right);
    }

    public static class DeterministicActorContactShapeCodec
    {
        const uint Magic = 0x48534341;
        const int Version = 2;

        public static byte[] Write(DeterministicActorContactShape shape)
        {
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(Magic);
            writer.WriteInt32(Version);
            writer.WriteInt64(shape.Radius.Raw);
            writer.WriteInt64(shape.Height.Raw);
            writer.WriteInt64(shape.CollisionOffset.Raw);
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
            writer.WriteString("deterministic-actor-contact-shape/2");
            writer.WriteBytes(Write(shape));
            return writer.ComputeHash();
        }
    }

    public sealed class DeterministicKccConfiguration
    {
        public const string ActorContactPolicyVersion = "solid-body-block/2";
        public const string QuerySemanticVersion = "fixed-capsule-ray-conservative-cast/5";
        public const string MotorSemanticVersion = "fixed-philippe-kcc-motor/10";

        public DeterministicKccConfiguration(
            FixedScalar radius,
            FixedScalar height,
            FixedScalar collisionOffset,
            FixedScalar minimumGroundNormalY,
            FixedScalar maximumStepHeight,
            FixedScalar groundDetectionExtraDistance,
            FixedScalar groundProbeReboundDistance,
            FixedScalar minimumGroundProbingDistance,
            FixedScalar secondaryProbeVerticalDistance,
            FixedScalar secondaryProbeHorizontalDistance,
            FixedScalar steppingForwardDistance,
            FixedScalar minimumRequiredStepDepth,
            FixedScalar maximumStableDistanceFromLedge,
            FixedScalar maximumStableDenivelationAngle,
            FixedScalar verticalObstructionCorrelation,
            FixedScalar maximumMovementDistance,
            FixedScalar queryTolerance,
            FixedScalar minimumMovementDistance,
            FixedScalar normalMergeDot,
            int maximumSweepIterations,
            int maximumContactIterations,
            int maximumCandidates,
            int maximumContacts,
            int maximumActorPairs,
            int maximumActorContactIterations,
            DeterministicActorContactResponseKind actorContactResponse = DeterministicActorContactResponseKind.SolidBodyBlock)
        {
            if (radius <= FixedScalar.Zero || height <= radius + radius ||
                collisionOffset <= FixedScalar.Zero || collisionOffset >= radius ||
                minimumGroundNormalY <= FixedScalar.Zero || minimumGroundNormalY > FixedScalar.One ||
                maximumStepHeight < FixedScalar.Zero || groundDetectionExtraDistance < FixedScalar.Zero ||
                groundProbeReboundDistance <= FixedScalar.Zero || minimumGroundProbingDistance <= FixedScalar.Zero ||
                secondaryProbeVerticalDistance <= FixedScalar.Zero || secondaryProbeHorizontalDistance <= FixedScalar.Zero ||
                steppingForwardDistance <= FixedScalar.Zero || minimumRequiredStepDepth <= FixedScalar.Zero ||
                minimumRequiredStepDepth > radius || maximumStableDistanceFromLedge < FixedScalar.Zero ||
                maximumStableDistanceFromLedge > radius || maximumStableDenivelationAngle < FixedScalar.Zero ||
                maximumStableDenivelationAngle > FixedScalar.FromInt64(180) ||
                verticalObstructionCorrelation < FixedScalar.Zero || verticalObstructionCorrelation >= FixedScalar.One ||
                maximumMovementDistance <= FixedScalar.Zero || queryTolerance <= FixedScalar.Zero ||
                queryTolerance >= collisionOffset || minimumMovementDistance <= FixedScalar.Zero ||
                normalMergeDot <= FixedScalar.Zero || normalMergeDot >= FixedScalar.One ||
                maximumSweepIterations <= 0 || maximumContactIterations <= 0 ||
                maximumCandidates <= 0 || maximumContacts <= 0 || maximumActorPairs <= 0 ||
                maximumActorPairs > 4096 || maximumActorContactIterations <= 0 || maximumActorContactIterations > 32 ||
                actorContactResponse != DeterministicActorContactResponseKind.SolidBodyBlock)
            {
                throw new ArgumentException("Deterministic KCC configuration is invalid.");
            }

            ActorContactShape = new DeterministicActorContactShape(radius, height, collisionOffset);
            MinimumGroundNormalY = minimumGroundNormalY;
            MaximumStepHeight = maximumStepHeight;
            GroundDetectionExtraDistance = groundDetectionExtraDistance;
            GroundProbeReboundDistance = groundProbeReboundDistance;
            MinimumGroundProbingDistance = minimumGroundProbingDistance;
            SecondaryProbeVerticalDistance = secondaryProbeVerticalDistance;
            SecondaryProbeHorizontalDistance = secondaryProbeHorizontalDistance;
            SteppingForwardDistance = steppingForwardDistance;
            MinimumRequiredStepDepth = minimumRequiredStepDepth;
            MaximumStableDistanceFromLedge = maximumStableDistanceFromLedge;
            MaximumStableDenivelationAngle = maximumStableDenivelationAngle;
            FixedAngle.SinCos(new FixedYaw(maximumStableDenivelationAngle), out _, out FixedScalar denivelationCosine);
            MinimumStableDenivelationNormalDot = denivelationCosine;
            VerticalObstructionCorrelation = verticalObstructionCorrelation;
            MaximumMovementDistance = maximumMovementDistance;
            QueryTolerance = queryTolerance;
            MinimumMovementDistance = minimumMovementDistance;
            NormalMergeDot = normalMergeDot;
            MaximumSweepIterations = maximumSweepIterations;
            MaximumContactIterations = maximumContactIterations;
            MaximumCandidates = maximumCandidates;
            MaximumContacts = maximumContacts;
            MaximumActorPairs = maximumActorPairs;
            MaximumActorContactIterations = maximumActorContactIterations;
            ActorContactResponse = actorContactResponse;
            ConfigurationHash = StableHash.Compute(
                "deterministic-kcc-configuration/8",
                ActorContactShape.ConfigurationHash.Value,
                minimumGroundNormalY.Raw.ToString(),
                maximumStepHeight.Raw.ToString(),
                groundDetectionExtraDistance.Raw.ToString(),
                groundProbeReboundDistance.Raw.ToString(),
                minimumGroundProbingDistance.Raw.ToString(),
                secondaryProbeVerticalDistance.Raw.ToString(),
                secondaryProbeHorizontalDistance.Raw.ToString(),
                steppingForwardDistance.Raw.ToString(),
                minimumRequiredStepDepth.Raw.ToString(),
                maximumStableDistanceFromLedge.Raw.ToString(),
                maximumStableDenivelationAngle.Raw.ToString(),
                verticalObstructionCorrelation.Raw.ToString(),
                maximumMovementDistance.Raw.ToString(),
                queryTolerance.Raw.ToString(),
                minimumMovementDistance.Raw.ToString(),
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
        public FixedScalar CollisionOffset => ActorContactShape.CollisionOffset;
        public FixedScalar MinimumGroundNormalY { get; }
        public FixedScalar MaximumStepHeight { get; }
        public FixedScalar GroundDetectionExtraDistance { get; }
        public FixedScalar GroundProbeReboundDistance { get; }
        public FixedScalar MinimumGroundProbingDistance { get; }
        public FixedScalar SecondaryProbeVerticalDistance { get; }
        public FixedScalar SecondaryProbeHorizontalDistance { get; }
        public FixedScalar SteppingForwardDistance { get; }
        public FixedScalar MinimumRequiredStepDepth { get; }
        public FixedScalar MaximumStableDistanceFromLedge { get; }
        public FixedScalar MaximumStableDenivelationAngle { get; }
        public FixedScalar MinimumStableDenivelationNormalDot { get; }
        public FixedScalar VerticalObstructionCorrelation { get; }
        public FixedScalar MaximumMovementDistance { get; }
        public FixedScalar QueryTolerance { get; }
        public FixedScalar MinimumMovementDistance { get; }
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
            FixedScalar.FromRatio(1, 100),
            FixedScalar.FromRatio(707106, 1000000),
            FixedScalar.FromRatio(3, 10),
            FixedScalar.Zero,
            FixedScalar.FromRatio(2, 100),
            FixedScalar.FromRatio(5, 1000),
            FixedScalar.FromRatio(2, 100),
            FixedScalar.FromRatio(1, 1000),
            FixedScalar.FromRatio(3, 100),
            FixedScalar.FromRatio(1, 10),
            FixedScalar.FromRatio(35, 100),
            FixedScalar.FromInt64(180),
            FixedScalar.FromRatio(1, 100),
            FixedScalar.FromInt64(3),
            FixedScalar.FromRatio(1, 100000),
            FixedScalar.FromRatio(1, 100000),
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

    public readonly struct DeterministicKccRayHit
    {
        public DeterministicKccRayHit(
            int surfaceId,
            int primitiveId,
            DeterministicCollisionFeatureId featureId,
            FixedScalar distance,
            FixedVector3 point,
            FixedVector3 normal)
        {
            if (surfaceId < 0 || primitiveId < 0 || !featureId.IsValid || distance < FixedScalar.Zero || normal.SqrMagnitude == FixedScalar.Zero)
                throw new ArgumentException("Deterministic KCC ray hit is invalid.");
            SurfaceId = surfaceId;
            PrimitiveId = primitiveId;
            FeatureId = featureId;
            Distance = distance;
            Point = point;
            Normal = normal;
        }

        public int SurfaceId { get; }
        public int PrimitiveId { get; }
        public DeterministicCollisionFeatureId FeatureId { get; }
        public FixedScalar Distance { get; }
        public FixedVector3 Point { get; }
        public FixedVector3 Normal { get; }
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
