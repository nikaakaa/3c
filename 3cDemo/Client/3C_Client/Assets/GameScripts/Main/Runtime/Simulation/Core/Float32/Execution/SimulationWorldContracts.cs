using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace ThirdPersonSimulation
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
            Float32Vector3 displacement,
            Float32Vector3 requestedVelocity,
            Float32Scalar yawDegrees,
            WorldMotionSpace space,
            bool hasMotion)
        {
            SourceIdentity = SimulationIdentity.Require(sourceIdentity, nameof(sourceIdentity));
            Displacement = displacement;
            RequestedVelocity = requestedVelocity;
            YawDegrees = yawDegrees;
            Space = space;
            HasMotion = hasMotion;
        }

        public string SourceIdentity { get; }
        public Float32Vector3 Displacement { get; }
        public Float32Vector3 RequestedVelocity { get; }
        public Float32Scalar YawDegrees { get; }
        public WorldMotionSpace Space { get; }
        public bool HasMotion { get; }
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

    public enum ObservedWorldConstraintSamplingKind : byte
    {
        Exact = 1,
        Interpolation = 2,
        ConstantVelocityExtrapolation = 3
    }

    public readonly struct ObservedWorldConstraint
    {
        public ObservedWorldConstraint(
            ActorId actorId,
            SimulationTick targetTick,
            WorldBodyState beforeBody,
            WorldBodyState finalBody,
            SimulationTick sourcePreviousTick,
            SimulationTick sourceCurrentTick,
            ObservedWorldConstraintSamplingKind samplingKind,
            StableHash contactShapeConfigurationHash)
        {
            if (!actorId.IsValid || !targetTick.IsValid || !sourcePreviousTick.IsValid || !sourceCurrentTick.IsValid ||
                beforeBody.ActorId != actorId || finalBody.ActorId != actorId ||
                sourceCurrentTick.CompareTo(sourcePreviousTick) < 0 ||
                !Enum.IsDefined(typeof(ObservedWorldConstraintSamplingKind), samplingKind) ||
                !contactShapeConfigurationHash.IsValid)
            {
                throw new ArgumentException("Observed world constraint identity is incomplete or inconsistent.");
            }
            ActorId = actorId;
            TargetTick = targetTick;
            BeforeBody = beforeBody;
            FinalBody = finalBody;
            SourcePreviousTick = sourcePreviousTick;
            SourceCurrentTick = sourceCurrentTick;
            SamplingKind = samplingKind;
            ContactShapeConfigurationHash = contactShapeConfigurationHash;
        }

        public ActorId ActorId { get; }
        public SimulationTick TargetTick { get; }
        public WorldBodyState BeforeBody { get; }
        public WorldBodyState FinalBody { get; }
        public SimulationTick SourcePreviousTick { get; }
        public SimulationTick SourceCurrentTick { get; }
        public ObservedWorldConstraintSamplingKind SamplingKind { get; }
        public StableHash ContactShapeConfigurationHash { get; }
    }

    public sealed class ObservedWorldConstraintFrame
    {
        readonly ReadOnlyCollection<ObservedWorldConstraint> m_Constraints;

        public ObservedWorldConstraintFrame(
            SimulationTick tick,
            IEnumerable<ObservedWorldConstraint> constraints)
        {
            if (!tick.IsValid)
                throw new ArgumentException("Observed world constraint frame Tick is invalid.", nameof(tick));
            var values = constraints == null
                ? throw new ArgumentNullException(nameof(constraints))
                : new List<ObservedWorldConstraint>(constraints);
            values.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i].TargetTick != tick || i > 0 && values[i - 1].ActorId == values[i].ActorId)
                    throw new ArgumentException("Observed world constraint frame contains a duplicate Actor or mismatched Tick.", nameof(constraints));
            }
            Tick = tick;
            m_Constraints = values.AsReadOnly();
            FrameHash = ObservedWorldConstraintCodec.ComputeHash(this);
        }

        public SimulationTick Tick { get; }
        public IReadOnlyList<ObservedWorldConstraint> Constraints => m_Constraints;
        public StableHash FrameHash { get; }
        public static ObservedWorldConstraintFrame Empty(SimulationTick tick) =>
            new ObservedWorldConstraintFrame(tick, Array.Empty<ObservedWorldConstraint>());
    }

    public interface IObservedWorldConstraintProfileProvider
    {
        StableHash ObservedContactShapeConfigurationHash { get; }
    }

    public static class ObservedWorldConstraintCodec
    {
        public const string CanonicalIdentity = "float32-observed-world-constraint-frame/1";

        public static StableHash ComputeHash(ObservedWorldConstraintFrame frame)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));
            using var writer = new CanonicalWriter();
            Write(writer, frame);
            return writer.ComputeHash();
        }

        public static void Write(CanonicalWriter writer, ObservedWorldConstraintFrame frame)
        {
            if (writer == null || frame == null)
                throw new ArgumentNullException(writer == null ? nameof(writer) : nameof(frame));
            writer.WriteString(CanonicalIdentity);
            writer.WriteUInt64(frame.Tick.Value);
            writer.WriteInt32(frame.Constraints.Count);
            for (int i = 0; i < frame.Constraints.Count; i++)
            {
                ObservedWorldConstraint value = frame.Constraints[i];
                writer.WriteString(value.ActorId.Value);
                writer.WriteUInt64(value.TargetTick.Value);
                WriteBody(writer, value.BeforeBody);
                WriteBody(writer, value.FinalBody);
                writer.WriteUInt64(value.SourcePreviousTick.Value);
                writer.WriteUInt64(value.SourceCurrentTick.Value);
                writer.WriteByte((byte)value.SamplingKind);
                writer.WriteString(value.ContactShapeConfigurationHash.Value);
            }
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
    }

    public sealed class WorldSolveBatchRequest
    {
        readonly ReadOnlyCollection<CharacterWorldSolveRequest> m_Requests;

        public WorldSolveBatchRequest(
            SimulationTick tick,
            WorldSimulationState beforeWorldState,
            IEnumerable<CharacterWorldSolveRequest> requests,
            ObservedWorldConstraintFrame observedWorldConstraints)
        {
            if (!tick.IsValid)
                throw new ArgumentException("World batch Tick is invalid.", nameof(tick));
            Tick = tick;
            BeforeWorldState = beforeWorldState ?? throw new ArgumentNullException(nameof(beforeWorldState));
            ObservedWorldConstraints = observedWorldConstraints ?? throw new ArgumentNullException(nameof(observedWorldConstraints));
            if (ObservedWorldConstraints.Tick != tick)
                throw new ArgumentException("Observed world constraint frame Tick does not match the batch.", nameof(observedWorldConstraints));
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
            for (int i = 0; i < ObservedWorldConstraints.Constraints.Count; i++)
            {
                ObservedWorldConstraint observed = ObservedWorldConstraints.Constraints[i];
                for (int activeIndex = 0; activeIndex < copied.Count; activeIndex++)
                {
                    if (copied[activeIndex].ActorId == observed.ActorId)
                        throw new ArgumentException($"Observed ActorId '{observed.ActorId}' is already active in the World batch.", nameof(observedWorldConstraints));
                }
            }
            RequiredCapabilities = required;
            m_Requests = copied.AsReadOnly();
            RequestHash = WorldSolveBatchCodec.ComputeRequestHash(this);
        }

        public SimulationNumericProfile NumericProfile { get; }
        public SimulationTick Tick { get; }
        public WorldSimulationState BeforeWorldState { get; }
        public ObservedWorldConstraintFrame ObservedWorldConstraints { get; }
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
            Float32Vector3 appliedDisplacement,
            Float32Scalar appliedYawDegrees)
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
        public Float32Vector3 AppliedDisplacement { get; }
        public Float32Scalar AppliedYawDegrees { get; }
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
        public const string RequestCanonicalIdentity = "float32-world-solve-request/3";

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
            ObservedWorldConstraintCodec.Write(writer, request.ObservedWorldConstraints);
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
            writer.WriteScalar(request.Motion.YawDegrees);
            writer.WriteByte((byte)request.Motion.Space);
            writer.WriteBoolean(request.Motion.HasMotion);
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
