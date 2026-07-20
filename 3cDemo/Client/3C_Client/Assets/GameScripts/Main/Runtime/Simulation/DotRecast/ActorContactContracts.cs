using System;

namespace ThirdPersonSimulation.DotRecast
{
    public enum ActorContactResponseKind : byte
    {
        SolidBodyBlock = 1
    }

    public readonly struct ActorContactShape : IEquatable<ActorContactShape>
    {
        public ActorContactShape(
            Float32Scalar radius,
            Float32Scalar height,
            Float32Scalar skinWidth)
        {
            if (radius <= Float32Scalar.Zero)
                throw new ArgumentOutOfRangeException(nameof(radius));
            if (height <= Float32Scalar.Zero)
                throw new ArgumentOutOfRangeException(nameof(height));
            if (skinWidth < Float32Scalar.Zero || skinWidth >= radius)
                throw new ArgumentOutOfRangeException(nameof(skinWidth));
            Radius = radius;
            Height = height;
            SkinWidth = skinWidth;
            ConfigurationHash = StableHash.Compute(
                "thirdperson.dotrecast.actor-contact-shape/1",
                radius.Bits.ToString("X8"),
                height.Bits.ToString("X8"),
                skinWidth.Bits.ToString("X8"));
        }

        public Float32Scalar Radius { get; }
        public Float32Scalar Height { get; }
        public Float32Scalar SkinWidth { get; }
        public Float32Scalar SeparationRadius => Radius + SkinWidth;
        public StableHash ConfigurationHash { get; }

        public bool Equals(ActorContactShape other) =>
            Radius == other.Radius &&
            Height == other.Height &&
            SkinWidth == other.SkinWidth;

        public override bool Equals(object obj) => obj is ActorContactShape other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Radius, Height, SkinWidth);
        public static bool operator ==(ActorContactShape left, ActorContactShape right) => left.Equals(right);
        public static bool operator !=(ActorContactShape left, ActorContactShape right) => !left.Equals(right);
    }

    public readonly struct ActorContactSolverConfiguration : IEquatable<ActorContactSolverConfiguration>
    {
        public ActorContactSolverConfiguration(
            int iterationCount,
            Float32Scalar contactTolerance,
            Float32Scalar maximumDepenetrationDistance,
            ActorContactResponseKind responseKind = ActorContactResponseKind.SolidBodyBlock)
        {
            if (iterationCount <= 0 || iterationCount > 32)
                throw new ArgumentOutOfRangeException(nameof(iterationCount));
            if (contactTolerance <= Float32Scalar.Zero)
                throw new ArgumentOutOfRangeException(nameof(contactTolerance));
            if (maximumDepenetrationDistance < contactTolerance)
                throw new ArgumentOutOfRangeException(nameof(maximumDepenetrationDistance));
            if (responseKind != ActorContactResponseKind.SolidBodyBlock)
                throw new ArgumentOutOfRangeException(nameof(responseKind));
            IterationCount = iterationCount;
            ContactTolerance = contactTolerance;
            MaximumDepenetrationDistance = maximumDepenetrationDistance;
            ResponseKind = responseKind;
            ConfigurationHash = StableHash.Compute(
                "thirdperson.dotrecast.actor-contact-solver/1",
                iterationCount.ToString(),
                contactTolerance.Bits.ToString("X8"),
                maximumDepenetrationDistance.Bits.ToString("X8"),
                ((byte)responseKind).ToString());
        }

        public int IterationCount { get; }
        public Float32Scalar ContactTolerance { get; }
        public Float32Scalar MaximumDepenetrationDistance { get; }
        public ActorContactResponseKind ResponseKind { get; }
        public StableHash ConfigurationHash { get; }

        public bool Equals(ActorContactSolverConfiguration other) =>
            IterationCount == other.IterationCount &&
            ContactTolerance == other.ContactTolerance &&
            MaximumDepenetrationDistance == other.MaximumDepenetrationDistance &&
            ResponseKind == other.ResponseKind;

        public override bool Equals(object obj) => obj is ActorContactSolverConfiguration other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(
            IterationCount,
            ContactTolerance,
            MaximumDepenetrationDistance,
            ResponseKind);
        public static bool operator ==(
            ActorContactSolverConfiguration left,
            ActorContactSolverConfiguration right) => left.Equals(right);
        public static bool operator !=(
            ActorContactSolverConfiguration left,
            ActorContactSolverConfiguration right) => !left.Equals(right);
    }

    public static class DotRecastWorldConfigurationIdentity
    {
        public const string WorldSolverDefinitionComponentId = "thirdperson.simulation.world-solver.dotrecast-navigation";
        public const string WorldSolverDefinitionSemanticVersion = "3";

        public static StableHash Compute(
            StableHash navigationSurfaceConfigurationHash,
            ActorContactShape contactShape,
            ActorContactSolverConfiguration contactConfiguration)
        {
            if (!navigationSurfaceConfigurationHash.IsValid || !contactShape.ConfigurationHash.IsValid ||
                !contactConfiguration.ConfigurationHash.IsValid)
            {
                throw new ArgumentException("DotRecast World configuration identity is incomplete.");
            }
            return StableHash.Compute(
                "thirdperson.dotrecast.world-configuration/3",
                navigationSurfaceConfigurationHash.Value,
                contactShape.ConfigurationHash.Value,
                contactConfiguration.ConfigurationHash.Value);
        }

        public static StableHash ComputeSolverDefinition(
            string componentId,
            string semanticVersion,
            StableHash navigationSurfaceConfigurationHash,
            ActorContactShape contactShape,
            ActorContactSolverConfiguration contactConfiguration,
            WorldCapability capabilities,
            WorldFeature features)
        {
            return StableHash.Compute(
                componentId,
                semanticVersion,
                DotRecastSourceIdentity.Commit,
                DotRecastSourceIdentity.AdapterVersion,
                DotRecastWorldSolver.ImplementationIdentity,
                DotRecastWorldSolver.SolverVersion,
                navigationSurfaceConfigurationHash.Value,
                contactShape.ConfigurationHash.Value,
                contactConfiguration.ConfigurationHash.Value,
                ((ulong)capabilities).ToString(),
                ((ulong)features).ToString());
        }
    }
}
