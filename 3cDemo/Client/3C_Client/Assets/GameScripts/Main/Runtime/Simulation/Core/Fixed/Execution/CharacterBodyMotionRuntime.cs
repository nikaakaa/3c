using ThirdPersonSimulation;
using System;

namespace ThirdPersonSimulation.Fixed
{
    public readonly struct BodyMotionIntegrationPlan
    {
        internal BodyMotionIntegrationPlan(
            ActorId actorId,
            SimulationTick tick,
            StableHash identity,
            string descriptorSourceIdentity,
            StableHash descriptorContentRevision,
            int semanticVersion,
            FixedScalar tickDelta,
            FixedScalar previousVerticalVelocity,
            FixedScalar candidateVerticalVelocity,
            FixedScalar gameplayVerticalDisplacement,
            FixedScalar gravityDisplacement,
            FixedVector3 requestedDisplacement)
        {
            if (!actorId.IsValid || !tick.IsValid || !identity.IsValid || !descriptorContentRevision.IsValid ||
                semanticVersion != 1 || tickDelta <= FixedScalar.Zero)
            {
                throw new ArgumentException("Body Motion integration plan is incomplete.");
            }
            ActorId = actorId;
            Tick = tick;
            Identity = identity;
            DescriptorSourceIdentity = SimulationIdentity.Require(descriptorSourceIdentity, nameof(descriptorSourceIdentity));
            DescriptorContentRevision = descriptorContentRevision;
            SemanticVersion = semanticVersion;
            TickDelta = tickDelta;
            PreviousVerticalVelocity = previousVerticalVelocity;
            CandidateVerticalVelocity = candidateVerticalVelocity;
            GameplayVerticalDisplacement = gameplayVerticalDisplacement;
            GravityDisplacement = gravityDisplacement;
            RequestedDisplacement = requestedDisplacement;
        }

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public StableHash Identity { get; }
        public string DescriptorSourceIdentity { get; }
        public StableHash DescriptorContentRevision { get; }
        public int SemanticVersion { get; }
        public FixedScalar TickDelta { get; }
        public FixedScalar PreviousVerticalVelocity { get; }
        public FixedScalar CandidateVerticalVelocity { get; }
        public FixedScalar GameplayVerticalDisplacement { get; }
        public FixedScalar GravityDisplacement { get; }
        public FixedVector3 RequestedDisplacement { get; }
    }

    public readonly struct BodyMotionPrepareResult
    {
        internal BodyMotionPrepareResult(BodyMotionIntegrationPlan plan, CharacterMotionRequest motion)
        {
            Plan = plan;
            Motion = motion;
        }

        public BodyMotionIntegrationPlan Plan { get; }
        public CharacterMotionRequest Motion { get; }
    }

    public static class CharacterBodyMotionRuntime
    {
        public static BodyMotionPrepareResult Prepare(
            ActorId actorId,
            SimulationTick tick,
            WorldBodyState beforeBody,
            ResolvedGameplayMotion gameplayMotion,
            ProgramBodyMotionDescriptor descriptor,
            FixedScalar tickDelta)
        {
            if (!actorId.IsValid || !tick.IsValid || beforeBody.ActorId != actorId)
                throw new ArgumentException("Body Motion Prepare identity is inconsistent.");
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));
            if (tickDelta <= FixedScalar.Zero)
                throw new ArgumentOutOfRangeException(nameof(tickDelta));

            FixedScalar previous = beforeBody.VerticalVelocity;
            FixedScalar candidate = FixedScalar.Max(
                previous + descriptor.GravityAcceleration * tickDelta,
                -descriptor.MaximumFallSpeed);
            FixedScalar gravityDisplacement = candidate * tickDelta;
            FixedVector3 gameplayDisplacement = gameplayMotion.Displacement;
            var requestedDisplacement = new FixedVector3(
                gameplayDisplacement.X,
                gameplayDisplacement.Y + gravityDisplacement,
                gameplayDisplacement.Z);
            var requestedVelocity = new FixedVector3(
                requestedDisplacement.X / tickDelta,
                requestedDisplacement.Y / tickDelta,
                requestedDisplacement.Z / tickDelta);
            StableHash identity = ComputeIdentity(
                actorId,
                tick,
                descriptor.SourceIdentity,
                descriptor.ContentRevision,
                descriptor.SemanticVersion,
                tickDelta,
                previous,
                candidate,
                gameplayDisplacement.Y,
                gravityDisplacement,
                requestedDisplacement);
            var plan = new BodyMotionIntegrationPlan(
                actorId,
                tick,
                identity,
                descriptor.SourceIdentity,
                descriptor.ContentRevision,
                descriptor.SemanticVersion,
                tickDelta,
                previous,
                candidate,
                gameplayDisplacement.Y,
                gravityDisplacement,
                requestedDisplacement);
            var motion = new CharacterMotionRequest(
                "body-motion:integrated",
                requestedDisplacement,
                requestedVelocity,
                gameplayMotion.LocomotionPlanarBasis,
                gameplayMotion.YawDegrees,
                WorldMotionSpace.World,
                requestedDisplacement != FixedVector3.Zero || gameplayMotion.YawDegrees != FixedScalar.Zero,
                gameplayMotion.MovementPlaybackClock,
                gameplayMotion.ActionOwnerIdentity,
                gameplayMotion.GameplayResultOwnerIdentity);
            return new BodyMotionPrepareResult(plan, motion);
        }

        public static WorldBodyState Finalize(
            WorldBodyState beforeBody,
            BodyMotionIntegrationPlan plan,
            FixedVector3 finalPosition,
            FixedYaw finalYaw,
            FixedVector3 appliedDisplacement,
            bool grounded,
            WorldCollisionSummary collision,
            FixedScalar tickDelta)
        {
            RequirePlan(beforeBody, plan, tickDelta);
            FixedScalar verticalVelocity = plan.CandidateVerticalVelocity;
            if (verticalVelocity < FixedScalar.Zero && grounded)
                verticalVelocity = FixedScalar.Zero;
            else if (verticalVelocity > FixedScalar.Zero && (collision & WorldCollisionSummary.Above) != 0)
                verticalVelocity = FixedScalar.Zero;
            return new WorldBodyState(
                beforeBody.ActorId,
                finalPosition,
                finalYaw,
                new FixedVector3(
                    appliedDisplacement.X / tickDelta,
                    appliedDisplacement.Y / tickDelta,
                    appliedDisplacement.Z / tickDelta),
                verticalVelocity,
                grounded,
                collision);
        }

        static void RequirePlan(WorldBodyState beforeBody, BodyMotionIntegrationPlan plan, FixedScalar tickDelta)
        {
            if (beforeBody.ActorId != plan.ActorId || tickDelta <= FixedScalar.Zero || tickDelta != plan.TickDelta ||
                beforeBody.VerticalVelocity != plan.PreviousVerticalVelocity)
            {
                throw new InvalidOperationException("Body Motion integration plan does not match the World solve input.");
            }
            StableHash expected = ComputeIdentity(
                plan.ActorId,
                plan.Tick,
                plan.DescriptorSourceIdentity,
                plan.DescriptorContentRevision,
                plan.SemanticVersion,
                plan.TickDelta,
                plan.PreviousVerticalVelocity,
                plan.CandidateVerticalVelocity,
                plan.GameplayVerticalDisplacement,
                plan.GravityDisplacement,
                plan.RequestedDisplacement);
            if (!expected.Equals(plan.Identity))
                throw new InvalidOperationException("Body Motion integration plan identity is invalid.");
        }

        static StableHash ComputeIdentity(
            ActorId actorId,
            SimulationTick tick,
            string descriptorSourceIdentity,
            StableHash descriptorContentRevision,
            int semanticVersion,
            FixedScalar tickDelta,
            FixedScalar previousVerticalVelocity,
            FixedScalar candidateVerticalVelocity,
            FixedScalar gameplayVerticalDisplacement,
            FixedScalar gravityDisplacement,
            FixedVector3 requestedDisplacement)
        {
            using var writer = new CanonicalWriter();
            writer.WriteString("fixed-body-motion-plan/1");
            writer.WriteString(actorId.Value);
            writer.WriteUInt64(tick.Value);
            writer.WriteString(descriptorSourceIdentity);
            writer.WriteString(descriptorContentRevision.Value);
            writer.WriteInt32(semanticVersion);
            writer.WriteScalar(tickDelta);
            writer.WriteScalar(previousVerticalVelocity);
            writer.WriteScalar(candidateVerticalVelocity);
            writer.WriteScalar(gameplayVerticalDisplacement);
            writer.WriteScalar(gravityDisplacement);
            writer.WriteVector3(requestedDisplacement);
            return writer.ComputeHash();
        }
    }
}
