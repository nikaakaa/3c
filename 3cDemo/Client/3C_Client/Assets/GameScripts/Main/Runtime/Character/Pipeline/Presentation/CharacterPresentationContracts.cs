using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public interface ICharacterPresentationLookInput
    {
        bool TryGetLatchedVector2(string inputId, out Vector2 value);
    }

    public enum CharacterPresentationBodyStreamUpdateKind : byte
    {
        Append = 1,
        Reset = 2
    }

    public enum CharacterLocomotionClockMode : byte
    {
        FreeRun = 1,
        CommittedMovement = 2
    }

    public enum CharacterLocomotionBodySource : byte
    {
        CommittedStream = 1,
        SelectedStream = 2
    }

    [Flags]
    public enum CharacterLocomotionPresentationSourceCapability : byte
    {
        None = 0,
        CommittedBodyStream = 1 << 0,
        SelectedBodyStream = 1 << 1,
        CommittedMovementFact = 1 << 2
    }

    public enum LocomotionPresentationFailureCode : byte
    {
        None = 0,
        MissingPlan = 1,
        InvalidPlan = 2,
        MissingBodyProfile = 3,
        InvalidBodyProfile = 4,
        BodySourceUnavailable = 5,
        MissingMovementFact = 6,
        PlanIdentityMismatch = 7,
        BodySourceMismatch = 8,
        LineageGenerationMismatch = 9,
        MovementSegmentMismatch = 10
    }

    public readonly struct CharacterLocomotionPresentationPlan
    {
        public CharacterLocomotionPresentationPlan(
            string identity,
            CharacterLocomotionClockMode clockMode,
            CharacterLocomotionBodySource bodySource)
        {
            Identity = identity?.Trim() ?? string.Empty;
            ClockMode = clockMode;
            BodySource = bodySource;
        }

        public string Identity { get; }
        public CharacterLocomotionClockMode ClockMode { get; }
        public CharacterLocomotionBodySource BodySource { get; }
        public bool IsSpecified =>
            !string.IsNullOrWhiteSpace(Identity) ||
            ClockMode != 0 ||
            BodySource != 0;
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(Identity) &&
            Enum.IsDefined(typeof(CharacterLocomotionClockMode), ClockMode) &&
            Enum.IsDefined(typeof(CharacterLocomotionBodySource), BodySource);
        public bool RequiresMovementFact => ClockMode == CharacterLocomotionClockMode.CommittedMovement;

        internal CharacterBodyPresentationSourceMode RuntimeBodySource =>
            BodySource == CharacterLocomotionBodySource.CommittedStream
                ? CharacterBodyPresentationSourceMode.CommittedStream
                : CharacterBodyPresentationSourceMode.SelectedStream;
    }

    public readonly struct CharacterLocomotionPresentationFactLineage
    {
        public CharacterLocomotionPresentationFactLineage(
            string planIdentity,
            CharacterLocomotionBodySource bodySource,
            string movementClockIdentity,
            ulong lineageGeneration,
            string movementSegmentIdentity)
        {
            PlanIdentity = planIdentity?.Trim() ?? string.Empty;
            BodySource = bodySource;
            MovementClockIdentity = movementClockIdentity?.Trim() ?? string.Empty;
            LineageGeneration = lineageGeneration;
            MovementSegmentIdentity = movementSegmentIdentity?.Trim() ?? string.Empty;
        }

        public string PlanIdentity { get; }
        public CharacterLocomotionBodySource BodySource { get; }
        public string MovementClockIdentity { get; }
        public ulong LineageGeneration { get; }
        public string MovementSegmentIdentity { get; }
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(PlanIdentity) &&
            Enum.IsDefined(typeof(CharacterLocomotionBodySource), BodySource) &&
            !string.IsNullOrWhiteSpace(MovementClockIdentity) &&
            LineageGeneration != 0 &&
            !string.IsNullOrWhiteSpace(MovementSegmentIdentity);

        internal static CharacterLocomotionPresentationFactLineage Create(
            in CharacterLocomotionPresentationPlan plan,
            in CommittedMovementPlaybackClock movementClock)
        {
            if (!plan.IsValid || !movementClock.IsValid)
                return default;
            string movementSegmentIdentity =
                string.Concat(movementClock.OwnerIdentity, "/", movementClock.Generation.ToString());
            return new CharacterLocomotionPresentationFactLineage(
                plan.Identity,
                plan.BodySource,
                movementClock.OwnerIdentity,
                movementClock.Generation,
                movementSegmentIdentity);
        }
    }

    public readonly struct CharacterLocomotionPresentationPreparationRequest
    {
        public CharacterLocomotionPresentationPreparationRequest(
            CharacterLocomotionPresentationPlan plan,
            CharacterBodyPresentationProfile bodyProfile,
            CharacterLocomotionPresentationSourceCapability sourceCapabilities,
            CharacterLocomotionPresentationFactLineage strictFactLineage = default)
        {
            Plan = plan;
            BodyProfile = bodyProfile;
            SourceCapabilities = sourceCapabilities;
            StrictFactLineage = strictFactLineage;
        }

        public CharacterLocomotionPresentationPlan Plan { get; }
        public CharacterBodyPresentationProfile BodyProfile { get; }
        public CharacterLocomotionPresentationSourceCapability SourceCapabilities { get; }
        public CharacterLocomotionPresentationFactLineage StrictFactLineage { get; }
    }

    public readonly struct PreparedCharacterLocomotionPresentationBinding
    {
        readonly CharacterBodyPresentationProfile m_BodyProfile;
        readonly CharacterLocomotionPresentationFactLineage m_StrictFactLineage;

        internal PreparedCharacterLocomotionPresentationBinding(
            CharacterLocomotionPresentationPlan plan,
            CharacterBodyPresentationProfile bodyProfile,
            string bodyProfileIdentity,
            CharacterLocomotionPresentationFactLineage strictFactLineage)
        {
            Plan = plan;
            m_BodyProfile = bodyProfile;
            BodyProfileIdentity = bodyProfileIdentity;
            m_StrictFactLineage = strictFactLineage;
        }

        public CharacterLocomotionPresentationPlan Plan { get; }
        public string PlanIdentity => Plan.Identity;
        public CharacterLocomotionClockMode ClockMode => Plan.ClockMode;
        public CharacterLocomotionBodySource BodySource => Plan.BodySource;
        public CharacterBodyCorrectionMode CorrectionMode => m_BodyProfile.CorrectionMode;
        public string BodyProfileIdentity { get; }
        public CharacterLocomotionPresentationFactLineage StrictFactLineage => m_StrictFactLineage;
        public bool RequiresMovementFact => Plan.RequiresMovementFact;
        public bool IsValid =>
            Plan.IsValid &&
            m_BodyProfile &&
            !string.IsNullOrWhiteSpace(BodyProfileIdentity) &&
            Enum.IsDefined(typeof(CharacterBodyCorrectionMode), CorrectionMode) &&
            (!RequiresMovementFact || m_StrictFactLineage.IsValid);

        internal CharacterBodyPresentationProfile BodyProfile => m_BodyProfile;
        internal CharacterBodyPresentationSourceMode RuntimeBodySource => Plan.RuntimeBodySource;

        public CharacterLocomotionPresentationFactLineage CreateFactLineage(
            in CommittedMovementPlaybackClock movementClock)
        {
            CharacterLocomotionPresentationPlan plan = Plan;
            return CharacterLocomotionPresentationFactLineage.Create(in plan, in movementClock);
        }

        public LocomotionPresentationFailureCode ValidateFact(
            in CharacterLocomotionPresentationFactLineage factLineage,
            in CommittedMovementPlaybackClock movementClock,
            in CommittedLocomotionPlanarMotionTimeline locomotionTimeline)
        {
            if (!RequiresMovementFact)
                return LocomotionPresentationFailureCode.None;
            if (!movementClock.IsValid || !factLineage.IsValid)
                return LocomotionPresentationFailureCode.MissingMovementFact;
            if (!string.Equals(factLineage.PlanIdentity, PlanIdentity, StringComparison.Ordinal))
                return LocomotionPresentationFailureCode.PlanIdentityMismatch;
            if (factLineage.BodySource != BodySource)
                return LocomotionPresentationFailureCode.BodySourceMismatch;
            if (factLineage.LineageGeneration != movementClock.Generation ||
                factLineage.LineageGeneration != m_StrictFactLineage.LineageGeneration)
            {
                return LocomotionPresentationFailureCode.LineageGenerationMismatch;
            }
            string movementSegmentIdentity = string.Concat(
                movementClock.OwnerIdentity,
                "/",
                movementClock.Generation.ToString());
            if (!string.Equals(factLineage.MovementClockIdentity, movementClock.OwnerIdentity, StringComparison.Ordinal) ||
                !string.Equals(factLineage.MovementSegmentIdentity, movementSegmentIdentity, StringComparison.Ordinal) ||
                !string.Equals(factLineage.MovementClockIdentity, m_StrictFactLineage.MovementClockIdentity, StringComparison.Ordinal) ||
                !string.Equals(factLineage.MovementSegmentIdentity, m_StrictFactLineage.MovementSegmentIdentity, StringComparison.Ordinal) ||
                !locomotionTimeline.IsValid ||
                !locomotionTimeline.Matches(movementClock))
            {
                return LocomotionPresentationFailureCode.MovementSegmentMismatch;
            }
            return LocomotionPresentationFailureCode.None;
        }

        internal void RequireValid()
        {
            if (!IsValid)
                throw new InvalidOperationException("Prepared locomotion presentation binding is invalid.");
        }
    }

    public readonly struct CharacterLocomotionPresentationPreparationResult
    {
        internal CharacterLocomotionPresentationPreparationResult(
            PreparedCharacterLocomotionPresentationBinding binding,
            LocomotionPresentationFailureCode failureCode,
            string message)
        {
            Binding = binding;
            FailureCode = failureCode;
            Message = message ?? string.Empty;
        }

        public PreparedCharacterLocomotionPresentationBinding Binding { get; }
        public LocomotionPresentationFailureCode FailureCode { get; }
        public string Message { get; }
        public bool Succeeded => FailureCode == LocomotionPresentationFailureCode.None && Binding.IsValid;
    }

    public static class CharacterLocomotionPresentationPreparation
    {
        public static CharacterLocomotionPresentationPreparationResult Prepare(
            in CharacterLocomotionPresentationPreparationRequest request)
        {
            CharacterLocomotionPresentationPlan plan = request.Plan;
            if (!plan.IsSpecified)
                return Failure(LocomotionPresentationFailureCode.MissingPlan, "Locomotion presentation plan is missing.");
            if (!plan.IsValid)
                return Failure(LocomotionPresentationFailureCode.InvalidPlan, "Locomotion presentation plan is invalid.");
            if (!request.BodyProfile)
                return Failure(LocomotionPresentationFailureCode.MissingBodyProfile, "Body presentation profile is missing.");
            string profileIdentity = request.BodyProfile.name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(profileIdentity))
                return Failure(LocomotionPresentationFailureCode.InvalidBodyProfile, "Body presentation profile identity is missing.");
            try
            {
                request.BodyProfile.BuildSettings();
            }
            catch (Exception exception)
            {
                return Failure(LocomotionPresentationFailureCode.InvalidBodyProfile, exception.Message);
            }
            if (!SupportsBodySource(request.SourceCapabilities, plan.BodySource))
            {
                return Failure(
                    LocomotionPresentationFailureCode.BodySourceUnavailable,
                    $"Locomotion presentation Body source '{plan.BodySource}' is unavailable.");
            }
            if (plan.RequiresMovementFact)
            {
                if ((request.SourceCapabilities & CharacterLocomotionPresentationSourceCapability.CommittedMovementFact) == 0 ||
                    !request.StrictFactLineage.IsValid)
                {
                    return Failure(
                        LocomotionPresentationFailureCode.MissingMovementFact,
                        "Committed locomotion presentation requires an explicit movement fact lineage.");
                }
                CharacterLocomotionPresentationFactLineage lineage = request.StrictFactLineage;
                LocomotionPresentationFailureCode lineageFailure = ValidatePreparationLineage(
                    in plan,
                    in lineage);
                if (lineageFailure != LocomotionPresentationFailureCode.None)
                    return Failure(lineageFailure, "Committed locomotion presentation fact lineage does not match its plan.");
            }
            return new CharacterLocomotionPresentationPreparationResult(
                new PreparedCharacterLocomotionPresentationBinding(
                    plan,
                    request.BodyProfile,
                    profileIdentity,
                    request.StrictFactLineage),
                LocomotionPresentationFailureCode.None,
                string.Empty);
        }

        public static PreparedCharacterLocomotionPresentationBinding RequireBinding(
            in CharacterLocomotionPresentationPreparationRequest request)
        {
            CharacterLocomotionPresentationPreparationResult result = Prepare(in request);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Locomotion presentation preparation failed: {result.FailureCode} {result.Message}");
            }
            return result.Binding;
        }

        static bool SupportsBodySource(
            CharacterLocomotionPresentationSourceCapability capabilities,
            CharacterLocomotionBodySource bodySource) =>
            bodySource == CharacterLocomotionBodySource.CommittedStream
                ? (capabilities & CharacterLocomotionPresentationSourceCapability.CommittedBodyStream) != 0
                : (capabilities & CharacterLocomotionPresentationSourceCapability.SelectedBodyStream) != 0;

        static LocomotionPresentationFailureCode ValidatePreparationLineage(
            in CharacterLocomotionPresentationPlan plan,
            in CharacterLocomotionPresentationFactLineage lineage)
        {
            if (!string.Equals(lineage.PlanIdentity, plan.Identity, StringComparison.Ordinal))
                return LocomotionPresentationFailureCode.PlanIdentityMismatch;
            if (lineage.BodySource != plan.BodySource)
                return LocomotionPresentationFailureCode.BodySourceMismatch;
            if (lineage.LineageGeneration == 0)
                return LocomotionPresentationFailureCode.LineageGenerationMismatch;
            return LocomotionPresentationFailureCode.None;
        }

        static CharacterLocomotionPresentationPreparationResult Failure(
            LocomotionPresentationFailureCode failureCode,
            string message) =>
            new CharacterLocomotionPresentationPreparationResult(
                default,
                failureCode,
                message);
    }

    public readonly struct CharacterPresentationBodyInterval
    {
        public CharacterPresentationBodyInterval(
            ulong previousTick,
            CharacterPresentationBodyState previousBody,
            ulong currentTick,
            CharacterPresentationBodyState currentBody,
            float yawVelocityDegreesPerSecond,
            CharacterPresentationBodyStreamUpdateKind updateKind = CharacterPresentationBodyStreamUpdateKind.Append)
        {
            if (!previousBody.ActorId.IsValid || previousBody.ActorId != currentBody.ActorId)
                throw new ArgumentException("Presentation Body interval Actor identity is invalid.");
            if (currentTick == 0 || previousTick > currentTick)
                throw new ArgumentException("Presentation Body interval Tick order is invalid.");
            if (updateKind != CharacterPresentationBodyStreamUpdateKind.Append &&
                updateKind != CharacterPresentationBodyStreamUpdateKind.Reset)
            {
                throw new ArgumentOutOfRangeException(nameof(updateKind));
            }
            if (float.IsNaN(yawVelocityDegreesPerSecond) || float.IsInfinity(yawVelocityDegreesPerSecond))
                throw new ArgumentOutOfRangeException(nameof(yawVelocityDegreesPerSecond));
            PreviousTick = previousTick;
            PreviousBody = previousBody;
            CurrentTick = currentTick;
            CurrentBody = currentBody;
            YawVelocityDegreesPerSecond = yawVelocityDegreesPerSecond;
            UpdateKind = updateKind;
        }

        public ActorId ActorId => CurrentBody.ActorId;
        public ulong PreviousTick { get; }
        public CharacterPresentationBodyState PreviousBody { get; }
        public ulong CurrentTick { get; }
        public CharacterPresentationBodyState CurrentBody { get; }
        public float YawVelocityDegreesPerSecond { get; }
        public CharacterPresentationBodyStreamUpdateKind UpdateKind { get; }

        public static CharacterPresentationBodyInterval FromFloat32(
            CharacterBodySample sample,
            int simulationTickRate,
            CharacterPresentationBodyStreamUpdateKind updateKind = CharacterPresentationBodyStreamUpdateKind.Append)
        {
            if (simulationTickRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(simulationTickRate));
            return new CharacterPresentationBodyInterval(
                sample.Tick.Value - 1,
                CharacterPresentationBodyState.FromFloat32(sample.BeforeBody),
                sample.Tick.Value,
                CharacterPresentationBodyState.FromFloat32(sample.FinalBody),
                sample.AppliedYawDegrees.ToSingle() * simulationTickRate,
                updateKind);
        }
    }

    public readonly struct CharacterPresentationEventHeader
    {
        public CharacterPresentationEventHeader(
            EventId eventId,
            ActorId actorId,
            SimulationTick tick,
            ActivationId activation,
            ulong sequence,
            string channel)
        {
            if (!eventId.IsValid || !actorId.IsValid || !tick.IsValid || !activation.IsValid || sequence == 0)
                throw new ArgumentException("Presentation event header is incomplete.");
            EventId = eventId;
            ActorId = actorId;
            Tick = tick;
            Activation = activation;
            Sequence = sequence;
            Channel = RequireIdentity(channel, nameof(channel));
        }

        public EventId EventId { get; }
        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public ActivationId Activation { get; }
        public ulong Sequence { get; }
        public string Channel { get; }

        static string RequireIdentity(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Presentation identity is missing.", parameterName);
            return value.Trim();
        }
    }

    public enum CharacterPresentationCommandKind : byte
    {
        SelectProducer = 1,
        SampleProducer = 2,
        CompleteProducer = 3,
        ReleaseProducer = 4,
        Camera = 5,
        Cue = 6,
        Vfx = 7,
        Ui = 8,
        ForceProducer = 9,
        DomainEvent = 10,
        ForceReleaseProducer = 11,
        TimelineProgress = 12
    }

    public readonly struct CharacterPresentationCommand
    {
        public CharacterPresentationCommand(
            CharacterPresentationEventHeader header,
            CharacterPresentationCommandKind kind,
            string producerId,
            float sampleTime,
            float weight,
            ulong producerGeneration = 0,
            int cycle = 0,
            ulong sourceActionInstanceId = 0,
            float visualTimeScale = 0f,
            string domainPayload = null,
            PresentationCameraRequest cameraRequest = default,
            AbilityTimelineProgress timelineProgress = default)
        {
            if (float.IsNaN(sampleTime) || float.IsInfinity(sampleTime) ||
                float.IsNaN(weight) || float.IsInfinity(weight))
            {
                throw new ArgumentOutOfRangeException(nameof(sampleTime));
            }
            if (RequiresProducerGeneration(kind) && producerGeneration == 0)
                throw new ArgumentOutOfRangeException(nameof(producerGeneration));
            if (RequiresProducerGeneration(kind) && producerGeneration != header.Activation.Generation)
                throw new ArgumentException("Presentation producer generation does not match the event activation.", nameof(producerGeneration));
            if (IsPlaybackCommand(kind) && sourceActionInstanceId == 0)
                throw new ArgumentOutOfRangeException(nameof(sourceActionInstanceId));
            if (cycle < 0)
                throw new ArgumentOutOfRangeException(nameof(cycle));
            if (!float.IsFinite(visualTimeScale) || visualTimeScale < 0f)
                throw new ArgumentOutOfRangeException(nameof(visualTimeScale));
            Header = header;
            Kind = kind;
            ProducerId = RequireIdentity(producerId, nameof(producerId));
            SampleTime = sampleTime;
            Weight = weight;
            ProducerGeneration = producerGeneration;
            Cycle = cycle;
            SourceActionInstanceId = sourceActionInstanceId;
            VisualTimeScale = visualTimeScale;
            DomainPayload = domainPayload ?? string.Empty;
            CameraRequest = cameraRequest;
            TimelineProgress = timelineProgress;
            if ((kind == CharacterPresentationCommandKind.TimelineProgress) != timelineProgress.IsValid ||
                timelineProgress.IsValid && (sourceActionInstanceId == 0 || timelineProgress.LogicTick != header.Tick.Value))
                throw new ArgumentException("Timeline progress requires its exact logic tick and Action identity.", nameof(timelineProgress));
            if (kind == CharacterPresentationCommandKind.DomainEvent &&
                (sourceActionInstanceId == 0 || string.IsNullOrWhiteSpace(DomainPayload)))
                throw new ArgumentException("Domain event command requires an Action instance and payload.", nameof(domainPayload));
            if (kind != CharacterPresentationCommandKind.DomainEvent && DomainPayload.Length != 0)
                throw new ArgumentException("Domain payload is only valid on Domain event commands.", nameof(domainPayload));
            if (kind == CharacterPresentationCommandKind.Camera && !cameraRequest.IsValid)
                throw new ArgumentException("Camera commands require a typed camera request.", nameof(cameraRequest));
            if (kind != CharacterPresentationCommandKind.Camera && cameraRequest.IsValid)
                throw new ArgumentException("Typed camera requests are only valid on Camera commands.", nameof(cameraRequest));
        }

        public CharacterPresentationEventHeader Header { get; }
        public CharacterPresentationCommandKind Kind { get; }
        public string ProducerId { get; }
        public float SampleTime { get; }
        public float Weight { get; }
        public ulong ProducerGeneration { get; }
        public int Cycle { get; }
        public ulong SourceActionInstanceId { get; }
        public float VisualTimeScale { get; }
        public string DomainPayload { get; }
        public PresentationCameraRequest CameraRequest { get; }
        public AbilityTimelineProgress TimelineProgress { get; }

        public static CharacterPresentationCommand FromFloat32(PresentationCommand command)
        {
            return new CharacterPresentationCommand(
                new CharacterPresentationEventHeader(
                    command.Header.EventId,
                    command.Header.ActorId,
                    command.Header.Tick,
                    command.Header.Activation,
                    command.Header.Sequence,
                    command.Header.Channel),
                (CharacterPresentationCommandKind)(byte)command.Kind,
                command.ProducerId,
                command.SampleTime.ToSingle(),
                command.Weight.ToSingle(),
                command.ProducerGeneration,
                command.Cycle,
                command.SourceActionInstanceId,
                command.VisualTimeScale.ToSingle(),
                command.DomainPayload,
                command.CameraRequest,
                command.TimelineProgress);
        }

        static bool IsPlaybackCommand(CharacterPresentationCommandKind kind)
        {
            return kind == CharacterPresentationCommandKind.SelectProducer ||
                   kind == CharacterPresentationCommandKind.SampleProducer ||
                   kind == CharacterPresentationCommandKind.CompleteProducer ||
                   kind == CharacterPresentationCommandKind.ReleaseProducer ||
                   kind == CharacterPresentationCommandKind.ForceReleaseProducer;
        }

        static bool RequiresProducerGeneration(CharacterPresentationCommandKind kind) =>
            IsPlaybackCommand(kind) ||
            kind == CharacterPresentationCommandKind.Camera ||
            kind == CharacterPresentationCommandKind.Cue ||
            kind == CharacterPresentationCommandKind.ForceProducer;

        static string RequireIdentity(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Presentation identity is missing.", parameterName);
            return value.Trim();
        }
    }

    public readonly struct CharacterPresentationBodyState
    {
        public CharacterPresentationBodyState(
            ActorId actorId,
            Vector3 position,
            Quaternion rotation,
            Vector3 linearVelocity,
            bool grounded)
        {
            if (!actorId.IsValid || !IsFinite(position) || !IsFinite(rotation) || !IsFinite(linearVelocity))
                throw new ArgumentException("Presentation body state is incomplete.");
            ActorId = actorId;
            Position = position;
            Rotation = rotation.normalized;
            LinearVelocity = linearVelocity;
            Grounded = grounded;
        }

        public ActorId ActorId { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Vector3 LinearVelocity { get; }
        public bool Grounded { get; }

        public bool KinematicallyMatches(in CharacterPresentationBodyState other) =>
            ActorId == other.ActorId &&
            Position == other.Position &&
            Rotation == other.Rotation &&
            LinearVelocity == other.LinearVelocity &&
            Grounded == other.Grounded;

        public static CharacterPresentationBodyState FromFloat32(WorldBodyState body)
        {
            return new CharacterPresentationBodyState(
                body.ActorId,
                new Vector3(body.Position.X.ToSingle(), body.Position.Y.ToSingle(), body.Position.Z.ToSingle()),
                Quaternion.Euler(0f, body.Yaw.Degrees.ToSingle(), 0f),
                new Vector3(body.Velocity.X.ToSingle(), body.Velocity.Y.ToSingle(), body.Velocity.Z.ToSingle()),
                body.Grounded);
        }

        static bool IsFinite(Vector3 value) =>
            IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

        static bool IsFinite(Quaternion value) =>
            IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);

        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public readonly struct CharacterPresentationDomainDiagnosticsSnapshot
    {
        public CharacterPresentationDomainDiagnosticsSnapshot(
            int bodyBranchReplacementCount,
            int animationBranchReplacementCount,
            float followerPositionCorrectionMeters,
            float followerYawCorrectionDegrees,
            CharacterLocomotionPresentationDiagnosticSnapshot locomotion)
        {
            BodyBranchReplacementCount = bodyBranchReplacementCount;
            AnimationBranchReplacementCount = animationBranchReplacementCount;
            FollowerPositionCorrectionMeters = followerPositionCorrectionMeters;
            FollowerYawCorrectionDegrees = followerYawCorrectionDegrees;
            Locomotion = locomotion;
        }

        public int BodyBranchReplacementCount { get; }
        public int AnimationBranchReplacementCount { get; }
        public float FollowerPositionCorrectionMeters { get; }
        public float FollowerYawCorrectionDegrees { get; }
        public CharacterLocomotionPresentationDiagnosticSnapshot Locomotion { get; }
    }

    public readonly struct CharacterLocomotionPresentationDiagnosticSnapshot
    {
        public CharacterLocomotionPresentationDiagnosticSnapshot(
            string planIdentity,
            CharacterLocomotionClockMode clockMode,
            CharacterLocomotionBodySource bodySource,
            CharacterBodyCorrectionMode correctionMode,
            string bodyProfileIdentity,
            CharacterLocomotionPresentationFactLineage factLineage,
            ulong resetSequence,
            CharacterBodyPresentationResetReason resetReason,
            LocomotionPresentationFailureCode failureCode)
        {
            PlanIdentity = planIdentity ?? string.Empty;
            ClockMode = clockMode;
            BodySource = bodySource;
            CorrectionMode = correctionMode;
            BodyProfileIdentity = bodyProfileIdentity ?? string.Empty;
            FactLineage = factLineage;
            ResetSequence = resetSequence;
            ResetReason = resetReason;
            FailureCode = failureCode;
        }

        public string PlanIdentity { get; }
        public CharacterLocomotionClockMode ClockMode { get; }
        public CharacterLocomotionBodySource BodySource { get; }
        public CharacterBodyCorrectionMode CorrectionMode { get; }
        public string BodyProfileIdentity { get; }
        public CharacterLocomotionPresentationFactLineage FactLineage { get; }
        public ulong ResetSequence { get; }
        public CharacterBodyPresentationResetReason ResetReason { get; }
        public LocomotionPresentationFailureCode FailureCode { get; }
    }

    public enum CharacterDomainRuntimeFactKind : byte
    {
        Ability = 1,
        Timeline = 2,
        Pose = 3,
        Camera = 4,
        Motion = 5
    }

    public enum CharacterDomainRuntimeFactState : byte
    {
        Unavailable = 1,
        Prepared = 2,
        Adopted = 3,
        Failed = 4
    }

    public readonly struct CharacterDomainRuntimeFact
    {
        public CharacterDomainRuntimeFact(
            CharacterDomainRuntimeFactKind kind,
            CharacterDomainRuntimeFactState state,
            string requestedIdentity,
            string adoptedIdentity,
            string failureReason)
        {
            if (!Enum.IsDefined(typeof(CharacterDomainRuntimeFactKind), kind) ||
                !Enum.IsDefined(typeof(CharacterDomainRuntimeFactState), state))
                throw new ArgumentOutOfRangeException(nameof(kind));
            Kind = kind;
            State = state;
            RequestedIdentity = requestedIdentity ?? string.Empty;
            AdoptedIdentity = adoptedIdentity ?? string.Empty;
            FailureReason = failureReason ?? string.Empty;
        }

        public CharacterDomainRuntimeFactKind Kind { get; }
        public CharacterDomainRuntimeFactState State { get; }
        public string RequestedIdentity { get; }
        public string AdoptedIdentity { get; }
        public string FailureReason { get; }
    }

    public sealed class CharacterDomainRuntimeAssemblyFacts
    {
        readonly IReadOnlyList<CharacterDomainRuntimeFact> m_Facts;

        public CharacterDomainRuntimeAssemblyFacts(IEnumerable<CharacterDomainRuntimeFact> facts)
        {
            var values = new List<CharacterDomainRuntimeFact>(facts ?? Array.Empty<CharacterDomainRuntimeFact>());
            var kinds = new HashSet<CharacterDomainRuntimeFactKind>();
            for (int i = 0; i < values.Count; i++)
            {
                if (!kinds.Add(values[i].Kind))
                    throw new ArgumentException($"Domain runtime fact kind '{values[i].Kind}' is duplicated.", nameof(facts));
            }
            values.Sort((left, right) => left.Kind.CompareTo(right.Kind));
            m_Facts = values.AsReadOnly();
        }

        public IReadOnlyList<CharacterDomainRuntimeFact> Facts => m_Facts;

        public bool TryGet(CharacterDomainRuntimeFactKind kind, out CharacterDomainRuntimeFact fact)
        {
            for (int i = 0; i < m_Facts.Count; i++)
            {
                if (m_Facts[i].Kind == kind)
                {
                    fact = m_Facts[i];
                    return true;
                }
            }
            fact = default;
            return false;
        }

        public CharacterDomainRuntimeAssemblyFacts Merge(
            IEnumerable<CharacterDomainRuntimeFact> additionalFacts)
        {
            var values = new List<CharacterDomainRuntimeFact>(m_Facts);
            if (additionalFacts != null)
                values.AddRange(additionalFacts);
            return new CharacterDomainRuntimeAssemblyFacts(values);
        }
    }

    public readonly struct CharacterPresentationDomainObservation
    {
        public CharacterPresentationDomainObservation(
            bool poseComposed,
            string poseGraphRevision,
            ulong poseInstanceId,
            ulong poseResetGeneration,
            string poseAvailability,
            int poseBoneCount,
            int poseContributionCount,
            ulong poseCompletionIdentity)
        {
            PoseComposed = poseComposed;
            PoseGraphRevision = poseGraphRevision ?? string.Empty;
            PoseInstanceId = poseInstanceId;
            PoseResetGeneration = poseResetGeneration;
            PoseAvailability = poseAvailability ?? string.Empty;
            PoseBoneCount = poseBoneCount;
            PoseContributionCount = poseContributionCount;
            PoseCompletionIdentity = poseCompletionIdentity;
        }

        public bool PoseComposed { get; }
        public string PoseGraphRevision { get; }
        public ulong PoseInstanceId { get; }
        public ulong PoseResetGeneration { get; }
        public string PoseAvailability { get; }
        public int PoseBoneCount { get; }
        public int PoseContributionCount { get; }
        public ulong PoseCompletionIdentity { get; }
    }

    public interface ICharacterPresentationDomainRuntime :
        IDisposable,
        IGameplayPresentationFrameTarget
    {
        bool AcceptsTrajectoryIntent { get; }
        ulong BodyResetSequence { get; }
        CharacterLocomotionBodySource LocomotionBodySource { get; }
        bool TryGetLatestBody(out CharacterPresentationBodyState body);
        void CaptureBodyStream(IReadOnlyList<CharacterPresentationBodyInterval> intervals);
        CharacterLocomotionPresentationFactLineage CreateLocomotionFactLineage(
            in CommittedMovementPlaybackClock movementClock);
        LocomotionPresentationFailureCode CaptureTrajectoryIntent(
            CharacterPresentationTrajectoryIntent intent);
        void CaptureEquipmentSelections(IReadOnlyList<EquipmentVisualSelection> selections);
        void Publish(CharacterPresentationCommand command);
        void Replace(CharacterPresentationCommand current, CharacterPresentationCommand replacement);
        void Retire(CharacterPresentationCommand command);
        void Reset();
        CharacterPresentationDomainDiagnosticsSnapshot CaptureDiagnostics();
        CharacterPresentationDomainObservation CaptureObservation();
        CharacterDomainRuntimeAssemblyFacts CaptureDomainFacts();
        bool SupportsCheckpointCapture { get; }
        bool SupportsCheckpointRestore { get; }
        bool TryCaptureCheckpoint(SimulationSessionCheckpoint checkpoint, out string error);
        bool TryRestoreCheckpoint(SimulationSessionCheckpoint checkpoint, out string error);
    }
}
