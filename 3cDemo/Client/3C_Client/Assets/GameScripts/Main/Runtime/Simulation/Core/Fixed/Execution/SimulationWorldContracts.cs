using ThirdPersonSimulation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace ThirdPersonSimulation.Fixed
{
    public enum WorldMotionSpace : byte
    {
        World = 1,
        ActorLocal = 2
    }

    public readonly struct CharacterMotionRequest
    {
        internal CharacterMotionRequest(
            string sourceIdentity,
            FixedVector3 displacement,
            FixedVector3 requestedVelocity,
            FixedVector2 locomotionPlanarBasis,
            FixedScalar yawDegrees,
            WorldMotionSpace space,
            bool hasMotion,
            CommittedMovementPlaybackClock movementPlaybackClock,
            CommittedLocomotionPlanarMotionTimeline locomotionTimeline,
            string actionOwnerIdentity,
            string gameplayResultOwnerIdentity)
        {
            SourceIdentity = SimulationIdentity.Require(sourceIdentity, nameof(sourceIdentity));
            Displacement = displacement;
            RequestedVelocity = requestedVelocity;
            LocomotionPlanarBasis = locomotionPlanarBasis;
            YawDegrees = yawDegrees;
            Space = space;
            HasMotion = hasMotion;
            MovementPlaybackClock = movementPlaybackClock;
            if (movementPlaybackClock.IsValid != locomotionTimeline.IsValid ||
                locomotionTimeline.IsValid && !locomotionTimeline.Matches(movementPlaybackClock))
            {
                throw new ArgumentException("Character motion request Locomotion timeline does not match its Movement clock.", nameof(locomotionTimeline));
            }
            LocomotionTimeline = locomotionTimeline;
            ActionOwnerIdentity = actionOwnerIdentity ?? string.Empty;
            GameplayResultOwnerIdentity = gameplayResultOwnerIdentity ?? string.Empty;
        }

        public string SourceIdentity { get; }
        public FixedVector3 Displacement { get; }
        public FixedVector3 RequestedVelocity { get; }
        public FixedVector2 LocomotionPlanarBasis { get; }
        public FixedScalar YawDegrees { get; }
        public WorldMotionSpace Space { get; }
        public bool HasMotion { get; }
        public CommittedMovementPlaybackClock MovementPlaybackClock { get; }
        public CommittedLocomotionPlanarMotionTimeline LocomotionTimeline { get; }
        public string ActionOwnerIdentity { get; }
        public string GameplayResultOwnerIdentity { get; }
    }

    public sealed class CharacterWorldSolveRequest
    {
        public CharacterWorldSolveRequest(
            SimulationNumericProfile numericProfile,
            ActorId actorId,
            WorldRequestId requestId,
            SimulationTick tick,
            WorldBodyState beforeBody,
            CharacterMotionRequest motion,
            BodyMotionIntegrationPlan bodyMotionPlan,
            WorldCapability requiredCapabilities)
        {
            if (!numericProfile.IsValid || !actorId.IsValid || !requestId.IsValid || !tick.IsValid || actorId != requestId.ActorId || requestId.Tick != tick)
                throw new ArgumentException("World solve request identity is incomplete or inconsistent.");
            if (beforeBody.ActorId != actorId)
                throw new ArgumentException("World solve request body does not match ActorId.", nameof(beforeBody));
            if (motion.MovementPlaybackClock.IsValid && motion.MovementPlaybackClock.AuthorityTick != tick)
                throw new ArgumentException("World solve request Movement playback clock does not match Tick.", nameof(motion));
            if (requiredCapabilities == WorldCapability.None)
                throw new ArgumentOutOfRangeException(nameof(requiredCapabilities));
            if (bodyMotionPlan.ActorId != actorId || bodyMotionPlan.Tick != tick || !bodyMotionPlan.Identity.IsValid ||
                bodyMotionPlan.PreviousVerticalVelocity != beforeBody.VerticalVelocity ||
                bodyMotionPlan.RequestedDisplacement != motion.Displacement)
            {
                throw new ArgumentException("World solve request Body Motion plan is inconsistent.", nameof(bodyMotionPlan));
            }
            NumericProfile = numericProfile;
            ActorId = actorId;
            RequestId = requestId;
            Tick = tick;
            BeforeBody = beforeBody;
            Motion = motion;
            BodyMotionPlan = bodyMotionPlan;
            RequiredCapabilities = requiredCapabilities;
        }

        public SimulationNumericProfile NumericProfile { get; }
        public ActorId ActorId { get; }
        public WorldRequestId RequestId { get; }
        public SimulationTick Tick { get; }
        public WorldBodyState BeforeBody { get; }
        public CharacterMotionRequest Motion { get; }
        public BodyMotionIntegrationPlan BodyMotionPlan { get; }
        public WorldCapability RequiredCapabilities { get; }
    }

    public sealed class WorldSolveBatchRequest
    {
        readonly ReadOnlyCollection<CharacterWorldSolveRequest> m_Requests;

        public WorldSolveBatchRequest(SimulationTick tick, WorldSimulationState beforeWorldState, IEnumerable<CharacterWorldSolveRequest> requests)
        {
            if (!tick.IsValid)
                throw new ArgumentException("World batch Tick is invalid.", nameof(tick));
            Tick = tick;
            BeforeWorldState = beforeWorldState ?? throw new ArgumentNullException(nameof(beforeWorldState));
            NumericProfile = beforeWorldState.NumericProfile;
            var copied = requests == null ? new List<CharacterWorldSolveRequest>() : new List<CharacterWorldSolveRequest>(requests);
            copied.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            if (copied.Count == 0 || copied.Count != beforeWorldState.Bodies.Count)
                throw new ArgumentException("World batch must contain exactly one request per world body.", nameof(requests));
            WorldCapability required = WorldCapability.None;
            for (int i = 0; i < copied.Count; i++)
            {
                CharacterWorldSolveRequest request = copied[i] ?? throw new ArgumentException("World batch contains a null request.", nameof(requests));
                if (request.NumericProfile != NumericProfile || request.Tick != tick || request.ActorId != beforeWorldState.Bodies[i].ActorId || !BodyEquals(request.BeforeBody, beforeWorldState.Bodies[i]))
                    throw new ArgumentException("World batch request order or before-body state does not match WorldSimulationState.", nameof(requests));
                if (i > 0 && copied[i - 1].ActorId == request.ActorId)
                    throw new ArgumentException($"World batch contains duplicate ActorId '{request.ActorId}'.", nameof(requests));
                required |= request.RequiredCapabilities;
            }
            RequiredCapabilities = required;
            m_Requests = copied.AsReadOnly();
            RequestHash = WorldSolveBatchCodec.ComputeRequestHash(this);
        }

        public SimulationNumericProfile NumericProfile { get; }
        public SimulationTick Tick { get; }
        public WorldSimulationState BeforeWorldState { get; }
        public IReadOnlyList<CharacterWorldSolveRequest> Requests => m_Requests;
        public WorldCapability RequiredCapabilities { get; }
        public StableHash RequestHash { get; }

        internal static bool BodyEquals(WorldBodyState left, WorldBodyState right)
        {
            return left.ActorId == right.ActorId &&
                   left.Position == right.Position &&
                   left.Yaw == right.Yaw &&
                   left.Velocity == right.Velocity &&
                   left.VerticalVelocity == right.VerticalVelocity &&
                   left.Grounded == right.Grounded &&
                   left.Collision == right.Collision;
        }
    }

    public sealed class CharacterWorldSolveResult
    {
        public CharacterWorldSolveResult(
            SimulationNumericProfile numericProfile,
            ActorId actorId,
            WorldRequestId requestId,
            SimulationTick tick,
            SolverImplementationId solverId,
            WorldBodyState finalBody,
            FixedVector3 appliedDisplacement,
            FixedScalar appliedYawDegrees)
        {
            if (!numericProfile.IsValid || !actorId.IsValid || !requestId.IsValid || !tick.IsValid || string.IsNullOrEmpty(solverId.Value))
                throw new ArgumentException("World solve result identity is incomplete.");
            if (requestId.ActorId != actorId || requestId.Tick != tick || finalBody.ActorId != actorId)
                throw new ArgumentException("World solve result identity does not match its body or request.");
            NumericProfile = numericProfile;
            ActorId = actorId;
            RequestId = requestId;
            Tick = tick;
            SolverId = solverId;
            FinalBody = finalBody;
            AppliedDisplacement = appliedDisplacement;
            AppliedYawDegrees = appliedYawDegrees;
        }

        public SimulationNumericProfile NumericProfile { get; }
        public ActorId ActorId { get; }
        public WorldRequestId RequestId { get; }
        public SimulationTick Tick { get; }
        public SolverImplementationId SolverId { get; }
        public WorldBodyState FinalBody { get; }
        public FixedVector3 AppliedDisplacement { get; }
        public FixedScalar AppliedYawDegrees { get; }
    }

    public readonly struct WorldSolveBatchSummary
    {
        public WorldSolveBatchSummary(int actorCount, StableHash requestHash, StableHash resultHash)
        {
            if (actorCount <= 0 || !requestHash.IsValid || !resultHash.IsValid)
                throw new ArgumentException("World solve summary is incomplete.");
            ActorCount = actorCount;
            RequestHash = requestHash;
            ResultHash = resultHash;
        }

        public int ActorCount { get; }
        public StableHash RequestHash { get; }
        public StableHash ResultHash { get; }
    }

    public sealed class WorldSolveBatchResult
    {
        readonly ReadOnlyCollection<CharacterWorldSolveResult> m_Results;

        public WorldSolveBatchResult(
            WorldSolveBatchRequest request,
            SolverImplementationId solverId,
            string solverVersion,
            WorldSimulationState nextWorldState,
            IEnumerable<CharacterWorldSolveResult> results)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrEmpty(solverId.Value))
                throw new ArgumentException("Solver identity is missing.", nameof(solverId));
            SolverId = solverId;
            SolverVersion = SimulationIdentity.Require(solverVersion, nameof(solverVersion));
            NextWorldState = nextWorldState ?? throw new ArgumentNullException(nameof(nextWorldState));
            if (nextWorldState.NumericProfile != request.NumericProfile)
                throw new ArgumentException("World result state Numeric Profile does not match request.", nameof(nextWorldState));
            if (!nextWorldState.SolverId.Equals(solverId) || !string.Equals(nextWorldState.SolverVersion, SolverVersion, StringComparison.Ordinal))
                throw new ArgumentException("Next World state does not match Solver identity.", nameof(nextWorldState));
            var copied = results == null ? new List<CharacterWorldSolveResult>() : new List<CharacterWorldSolveResult>(results);
            copied.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            if (copied.Count != request.Requests.Count || copied.Count != nextWorldState.Bodies.Count)
                throw new ArgumentException("World result must contain exactly one result per request.", nameof(results));
            for (int i = 0; i < copied.Count; i++)
            {
                CharacterWorldSolveResult result = copied[i] ?? throw new ArgumentException("World batch contains a null result.", nameof(results));
                CharacterWorldSolveRequest expected = request.Requests[i];
                if (result.NumericProfile != request.NumericProfile || result.ActorId != expected.ActorId || !result.RequestId.Equals(expected.RequestId) || result.Tick != request.Tick ||
                    !result.SolverId.Equals(solverId) || result.FinalBody.ActorId != expected.ActorId ||
                    !WorldSolveBatchRequest.BodyEquals(result.FinalBody, nextWorldState.Bodies[i]))
                    throw new ArgumentException("World result is missing, duplicated, unknown, or mismatched.", nameof(results));
            }
            m_Results = copied.AsReadOnly();
            StableHash resultHash = WorldSolveBatchCodec.ComputeResultHash(this);
            Summary = new WorldSolveBatchSummary(copied.Count, request.RequestHash, resultHash);
        }

        public WorldSolveBatchRequest Request { get; }
        public SimulationTick Tick => Request.Tick;
        public SolverImplementationId SolverId { get; }
        public string SolverVersion { get; }
        public WorldSimulationState NextWorldState { get; }
        public IReadOnlyList<CharacterWorldSolveResult> Results => m_Results;
        public WorldSolveBatchSummary Summary { get; }
    }

    public sealed class CharacterWorldSolverDescriptor
    {
        public CharacterWorldSolverDescriptor(
            SimulationNumericProfile numericProfile,
            SolverImplementationId implementationId,
            string version,
            WorldCapability capabilities,
            WorldFeature features)
        {
            if (!numericProfile.IsValid || string.IsNullOrEmpty(implementationId.Value) || capabilities == WorldCapability.None)
                throw new ArgumentException("World Solver descriptor is incomplete.");
            if ((capabilities & WorldCapability.DeterministicReplay) != 0 && !numericProfile.DeterministicReplay)
                throw new ArgumentException("World Solver cannot declare DeterministicReplay for a non-deterministic Numeric Profile.", nameof(capabilities));
            NumericProfile = numericProfile;
            ImplementationId = implementationId;
            Version = SimulationIdentity.Require(version, nameof(version));
            Capabilities = capabilities;
            Features = features;
        }

        public SimulationNumericProfile NumericProfile { get; }
        public SolverImplementationId ImplementationId { get; }
        public string Version { get; }
        public WorldCapability Capabilities { get; }
        public WorldFeature Features { get; }
        public bool Supports(WorldCapability required) => (Capabilities & required) == required;
    }

    public interface ICharacterWorldSolver : global::ThirdPersonSimulation.ICharacterWorldSolver<
        CharacterWorldSolverDescriptor,
        WorldBodyState,
        WorldSimulationState,
        WorldSolveBatchRequest,
        WorldSolveBatchResult,
        ISimulationDiagnosticsSink>
    {
    }

    public static class WorldSolveBatchCodec
    {
        public const string RequestCanonicalIdentity = "fixed-world-solve-request/2";

        public static StableHash ComputeRequestHash(WorldSolveBatchRequest request)
        {
            using var writer = new CanonicalWriter();
            writer.WriteString(RequestCanonicalIdentity);
            SimulationNumericProfileCodec.Write(writer, request.NumericProfile);
            writer.WriteUInt64(request.Tick.Value);
            writer.WriteBytes(WorldSimulationStateCodec.Write(request.BeforeWorldState));
            writer.WriteUInt64((ulong)request.RequiredCapabilities);
            writer.WriteInt32(request.Requests.Count);
            for (int i = 0; i < request.Requests.Count; i++)
                WriteRequest(writer, request.Requests[i]);
            return writer.ComputeHash();
        }

        public static StableHash ComputeResultHash(WorldSolveBatchResult result)
        {
            using var writer = new CanonicalWriter();
            SimulationNumericProfileCodec.Write(writer, result.Request.NumericProfile);
            writer.WriteUInt64(result.Tick.Value);
            writer.WriteString(result.SolverId.Value);
            writer.WriteString(result.SolverVersion);
            writer.WriteBytes(WorldSimulationStateCodec.Write(result.NextWorldState));
            writer.WriteInt32(result.Results.Count);
            for (int i = 0; i < result.Results.Count; i++)
                WriteResult(writer, result.Results[i]);
            return writer.ComputeHash();
        }

        static void WriteRequest(CanonicalWriter writer, CharacterWorldSolveRequest request)
        {
            SimulationNumericProfileCodec.Write(writer, request.NumericProfile);
            writer.WriteString(request.ActorId.Value);
            writer.WriteUInt64(request.RequestId.Tick.Value);
            writer.WriteUInt64(request.RequestId.Sequence);
            WriteBody(writer, request.BeforeBody);
            writer.WriteString(request.Motion.SourceIdentity);
            writer.WriteVector3(request.Motion.Displacement);
            writer.WriteVector3(request.Motion.RequestedVelocity);
            writer.WriteVector2(request.Motion.LocomotionPlanarBasis);
            writer.WriteScalar(request.Motion.YawDegrees);
            writer.WriteByte((byte)request.Motion.Space);
            writer.WriteBoolean(request.Motion.HasMotion);
            CommittedMovementPlaybackClock movementClock = request.Motion.MovementPlaybackClock;
            writer.WriteBoolean(movementClock.IsValid);
            if (movementClock.IsValid)
            {
                writer.WriteString(movementClock.OwnerIdentity);
                writer.WriteUInt64(movementClock.Generation);
                writer.WriteUInt64(movementClock.AuthorityTick.Value);
                writer.WriteInt32(movementClock.ContinuousTicks);
                writer.WriteInt32(movementClock.TickRate);
            }
            WriteBodyMotionPlan(writer, request.BodyMotionPlan);
            writer.WriteUInt64((ulong)request.RequiredCapabilities);
        }

        static void WriteResult(CanonicalWriter writer, CharacterWorldSolveResult result)
        {
            SimulationNumericProfileCodec.Write(writer, result.NumericProfile);
            writer.WriteString(result.ActorId.Value);
            writer.WriteUInt64(result.RequestId.Tick.Value);
            writer.WriteUInt64(result.RequestId.Sequence);
            writer.WriteString(result.SolverId.Value);
            WriteBody(writer, result.FinalBody);
            writer.WriteVector3(result.AppliedDisplacement);
            writer.WriteScalar(result.AppliedYawDegrees);
        }

        static void WriteBody(CanonicalWriter writer, WorldBodyState body)
        {
            writer.WriteString(body.ActorId.Value);
            writer.WriteVector3(body.Position);
            writer.WriteYaw(body.Yaw);
            writer.WriteVector3(body.Velocity);
            writer.WriteScalar(body.VerticalVelocity);
            writer.WriteBoolean(body.Grounded);
            writer.WriteUInt32((uint)body.Collision);
        }

        static void WriteBodyMotionPlan(CanonicalWriter writer, BodyMotionIntegrationPlan plan)
        {
            writer.WriteString(plan.Identity.Value);
            writer.WriteString(plan.DescriptorSourceIdentity);
            writer.WriteString(plan.DescriptorContentRevision.Value);
            writer.WriteInt32(plan.SemanticVersion);
            writer.WriteScalar(plan.TickDelta);
            writer.WriteScalar(plan.PreviousVerticalVelocity);
            writer.WriteScalar(plan.CandidateVerticalVelocity);
            writer.WriteScalar(plan.GameplayVerticalDisplacement);
            writer.WriteScalar(plan.GravityDisplacement);
            writer.WriteVector3(plan.RequestedDisplacement);
        }
    }
}

