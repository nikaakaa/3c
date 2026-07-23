using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterPosePlanPhaseStatus : byte
    {
        Completed = 1,
        NotRequired = 2,
        Unavailable = 3
    }

    public enum CharacterPosePlanPhaseUnavailableReason : byte
    {
        None = 0,
        ComposedPoseUnavailable = 1,
        WorldContextUnavailable = 2,
        WorldAwarePhaseUnavailable = 3
    }

    public readonly struct CharacterPosePlanPhaseSnapshot
    {
        public CharacterPosePlanPhaseSnapshot(
            CharacterPosePlanPhase phase,
            CharacterPosePlanPhaseStatus status,
            CharacterPosePlanPhaseUnavailableReason unavailableReason,
            ulong completionIdentity)
        {
            bool completed = status == CharacterPosePlanPhaseStatus.Completed;
            bool notRequired = status == CharacterPosePlanPhaseStatus.NotRequired;
            bool unavailable = status == CharacterPosePlanPhaseStatus.Unavailable;
            if (!Enum.IsDefined(typeof(CharacterPosePlanPhase), phase) ||
                !Enum.IsDefined(typeof(CharacterPosePlanPhaseStatus), status) ||
                !Enum.IsDefined(typeof(CharacterPosePlanPhaseUnavailableReason), unavailableReason) ||
                completed && (completionIdentity == 0 || unavailableReason != CharacterPosePlanPhaseUnavailableReason.None) ||
                notRequired && (completionIdentity != 0 || unavailableReason != CharacterPosePlanPhaseUnavailableReason.None) ||
                unavailable && (completionIdentity != 0 || unavailableReason == CharacterPosePlanPhaseUnavailableReason.None))
            {
                throw new ArgumentException("Pose Plan phase snapshot is invalid.");
            }
            Phase = phase;
            Status = status;
            UnavailableReason = unavailableReason;
            CompletionIdentity = completionIdentity;
        }

        public CharacterPosePlanPhase Phase { get; }
        public CharacterPosePlanPhaseStatus Status { get; }
        public CharacterPosePlanPhaseUnavailableReason UnavailableReason { get; }
        public ulong CompletionIdentity { get; }
        public bool IsCompleted => Status == CharacterPosePlanPhaseStatus.Completed;
    }

    public readonly struct CharacterPosePlanStageSnapshot
    {
        public CharacterPosePlanStageSnapshot(
            string poseGraphId,
            string posePlanHash,
            AnimationPoseAvailability composedAvailability,
            CharacterPosePlanPhaseSnapshot composed,
            CharacterPosePlanPhaseSnapshot worldAware,
            CharacterPosePlanPhaseSnapshot final)
        {
            if (string.IsNullOrWhiteSpace(poseGraphId) || string.IsNullOrWhiteSpace(posePlanHash) ||
                !Enum.IsDefined(typeof(AnimationPoseAvailability), composedAvailability) ||
                composed.Phase != CharacterPosePlanPhase.SourceAndNativePose ||
                worldAware.Phase != CharacterPosePlanPhase.WorldAwarePostProcess ||
                final.Phase != CharacterPosePlanPhase.FinalPublication ||
                final.IsCompleted && !composed.IsCompleted ||
                final.IsCompleted && worldAware.Status == CharacterPosePlanPhaseStatus.Unavailable)
            {
                throw new ArgumentException("Pose Plan stage snapshot is invalid.");
            }
            PoseGraphId = poseGraphId;
            PosePlanHash = posePlanHash;
            ComposedAvailability = composedAvailability;
            Composed = composed;
            WorldAware = worldAware;
            Final = final;
        }

        public string PoseGraphId { get; }
        public string PosePlanHash { get; }
        public AnimationPoseAvailability ComposedAvailability { get; }
        public CharacterPosePlanPhaseSnapshot Composed { get; }
        public CharacterPosePlanPhaseSnapshot WorldAware { get; }
        public CharacterPosePlanPhaseSnapshot Final { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(PoseGraphId) && !string.IsNullOrWhiteSpace(PosePlanHash);
    }

    internal static class CharacterPosePlanStageSnapshotFactory
    {
        internal static CharacterPosePlanStageSnapshot Completed(
            CharacterPresentationPosePlan plan,
            in ComposedAnimationPoseFrame composed,
            bool worldAwareExecuted)
        {
            RequirePlan(plan);
            return new CharacterPosePlanStageSnapshot(
                composed.PoseGraphId,
                composed.PosePlanHash,
                composed.Availability,
                Complete(CharacterPosePlanPhase.SourceAndNativePose, composed.CompletionIdentity),
                worldAwareExecuted
                    ? Complete(CharacterPosePlanPhase.WorldAwarePostProcess, composed.CompletionIdentity)
                    : NotRequired(CharacterPosePlanPhase.WorldAwarePostProcess),
                Complete(CharacterPosePlanPhase.FinalPublication, composed.CompletionIdentity));
        }

        internal static CharacterPosePlanStageSnapshot Unavailable(
            CharacterPresentationPosePlan plan,
            AnimationPoseAvailability composedAvailability,
            ulong composedCompletionIdentity,
            CharacterPosePlanPhaseUnavailableReason reason)
        {
            RequirePlan(plan);
            CharacterPosePlanPhaseSnapshot composed = composedCompletionIdentity == 0
                ? Missing(CharacterPosePlanPhase.SourceAndNativePose, reason)
                : Complete(CharacterPosePlanPhase.SourceAndNativePose, composedCompletionIdentity);
            return new CharacterPosePlanStageSnapshot(
                plan.PoseGraphId,
                plan.PlanHash,
                composedAvailability,
                composed,
                Missing(CharacterPosePlanPhase.WorldAwarePostProcess, reason),
                Missing(CharacterPosePlanPhase.FinalPublication, CharacterPosePlanPhaseUnavailableReason.WorldAwarePhaseUnavailable));
        }

        internal static CharacterPosePlanStageSnapshot Preview(
            CharacterPresentationPosePlan plan,
            in ComposedAnimationPoseFrame composed)
        {
            RequirePlan(plan);
            if (composed.Availability != AnimationPoseAvailability.Pose)
            {
                return Unavailable(
                    plan,
                    composed.Availability,
                    composed.CompletionIdentity,
                    CharacterPosePlanPhaseUnavailableReason.ComposedPoseUnavailable);
            }
            return plan.FootPlacementNodes.Count == 0
                ? Completed(plan, in composed, false)
                : new CharacterPosePlanStageSnapshot(
                    composed.PoseGraphId,
                    composed.PosePlanHash,
                    composed.Availability,
                    Complete(CharacterPosePlanPhase.SourceAndNativePose, composed.CompletionIdentity),
                    Missing(
                        CharacterPosePlanPhase.WorldAwarePostProcess,
                        CharacterPosePlanPhaseUnavailableReason.WorldContextUnavailable),
                    Missing(
                        CharacterPosePlanPhase.FinalPublication,
                        CharacterPosePlanPhaseUnavailableReason.WorldAwarePhaseUnavailable));
        }

        static CharacterPosePlanPhaseSnapshot Complete(CharacterPosePlanPhase phase, ulong completionIdentity) =>
            new CharacterPosePlanPhaseSnapshot(
                phase,
                CharacterPosePlanPhaseStatus.Completed,
                CharacterPosePlanPhaseUnavailableReason.None,
                completionIdentity);

        static CharacterPosePlanPhaseSnapshot NotRequired(CharacterPosePlanPhase phase) =>
            new CharacterPosePlanPhaseSnapshot(
                phase,
                CharacterPosePlanPhaseStatus.NotRequired,
                CharacterPosePlanPhaseUnavailableReason.None,
                0);

        static CharacterPosePlanPhaseSnapshot Missing(
            CharacterPosePlanPhase phase,
            CharacterPosePlanPhaseUnavailableReason reason) =>
            new CharacterPosePlanPhaseSnapshot(
                phase,
                CharacterPosePlanPhaseStatus.Unavailable,
                reason,
                0);

        static void RequirePlan(CharacterPresentationPosePlan plan)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            plan.RequireValid();
        }
    }

    public interface ICharacterPresentationLookInput
    {
        bool TryGetLatchedVector2(string inputId, out Vector2 value);
    }

    public enum CharacterPresentationBodyStreamUpdateKind : byte
    {
        Append = 1,
        Reset = 2
    }

    public readonly struct CharacterPresentationBodyInterval
    {
        public CharacterPresentationBodyInterval(
            ulong previousTick,
            CharacterPresentationBodyState previousBody,
            ulong currentTick,
            CharacterPresentationBodyState currentBody,
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
            PreviousTick = previousTick;
            PreviousBody = previousBody;
            CurrentTick = currentTick;
            CurrentBody = currentBody;
            UpdateKind = updateKind;
        }

        public ActorId ActorId => CurrentBody.ActorId;
        public ulong PreviousTick { get; }
        public CharacterPresentationBodyState PreviousBody { get; }
        public ulong CurrentTick { get; }
        public CharacterPresentationBodyState CurrentBody { get; }
        public CharacterPresentationBodyStreamUpdateKind UpdateKind { get; }

        public static CharacterPresentationBodyInterval FromFloat32(
            CharacterBodySample sample,
            CharacterPresentationBodyStreamUpdateKind updateKind = CharacterPresentationBodyStreamUpdateKind.Append)
        {
            return new CharacterPresentationBodyInterval(
                sample.Tick.Value - 1,
                CharacterPresentationBodyState.FromFloat32(sample.BeforeBody),
                sample.Tick.Value,
                CharacterPresentationBodyState.FromFloat32(sample.FinalBody),
                updateKind);
        }
    }

    public readonly struct CharacterPresentationRuntimeDiagnosticsSnapshot
    {
        public CharacterPresentationRuntimeDiagnosticsSnapshot(
            ulong bodyBranchReplacementCount,
            ulong animationBranchReplacementCount,
            float followerPositionCorrectionMeters,
            float followerYawCorrectionDegrees,
            CharacterFootPlacementFrameSnapshot footPlacement,
            CharacterPosePlanStageSnapshot posePlanStages)
        {
            BodyBranchReplacementCount = bodyBranchReplacementCount;
            AnimationBranchReplacementCount = animationBranchReplacementCount;
            FollowerPositionCorrectionMeters = followerPositionCorrectionMeters;
            FollowerYawCorrectionDegrees = followerYawCorrectionDegrees;
            FootPlacement = footPlacement;
            PosePlanStages = posePlanStages;
        }

        public ulong BodyBranchReplacementCount { get; }
        public ulong AnimationBranchReplacementCount { get; }
        public float FollowerPositionCorrectionMeters { get; }
        public float FollowerYawCorrectionDegrees { get; }
        public CharacterFootPlacementFrameSnapshot FootPlacement { get; }
        public CharacterPosePlanStageSnapshot PosePlanStages { get; }
    }

    public interface ICharacterPresentationRuntime : IDisposable
    {
        void CaptureBodyInterval(CharacterPresentationBodyInterval interval);
        void CaptureBodyTransaction(IReadOnlyList<CharacterPresentationBodyInterval> intervals);
        void CaptureEquipmentSelections(IReadOnlyList<EquipmentVisualSelection> selections);
        void CaptureTrajectoryIntent(CharacterPresentationTrajectoryIntent intent);
        bool AcceptsTrajectoryIntent { get; }
        ulong BodyResetSequence { get; }
        void Publish(CharacterPresentationCommand command);
        void Replace(CharacterPresentationCommand current, CharacterPresentationCommand replacement);
        void Retire(CharacterPresentationCommand command);
        void Present(GameplayPresentationFrameContext context);
        CharacterPresentationRuntimeDiagnosticsSnapshot CaptureDiagnostics();
        void Reset();
    }

    public sealed class CharacterPresentationRuntimeBinding
    {
        public CharacterPresentationRuntimeBinding(
            CharacterPresentationProjection projection,
            ICharacterPresentationRuntime runtime)
        {
            Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        public CharacterPresentationProjection Projection { get; }
        public ICharacterPresentationRuntime Runtime { get; }
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
        Ui = 8
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
            int cycle = 0)
        {
            if (float.IsNaN(sampleTime) || float.IsInfinity(sampleTime) ||
                float.IsNaN(weight) || float.IsInfinity(weight))
            {
                throw new ArgumentOutOfRangeException(nameof(sampleTime));
            }
            if (IsPlaybackCommand(kind) && producerGeneration == 0)
                throw new ArgumentOutOfRangeException(nameof(producerGeneration));
            if (cycle < 0)
                throw new ArgumentOutOfRangeException(nameof(cycle));
            Header = header;
            Kind = kind;
            ProducerId = RequireIdentity(producerId, nameof(producerId));
            SampleTime = sampleTime;
            Weight = weight;
            ProducerGeneration = producerGeneration;
            Cycle = cycle;
        }

        public CharacterPresentationEventHeader Header { get; }
        public CharacterPresentationCommandKind Kind { get; }
        public string ProducerId { get; }
        public float SampleTime { get; }
        public float Weight { get; }
        public ulong ProducerGeneration { get; }
        public int Cycle { get; }

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
                command.Cycle);
        }

        static bool IsPlaybackCommand(CharacterPresentationCommandKind kind)
        {
            return kind == CharacterPresentationCommandKind.SelectProducer ||
                   kind == CharacterPresentationCommandKind.SampleProducer ||
                   kind == CharacterPresentationCommandKind.CompleteProducer ||
                   kind == CharacterPresentationCommandKind.ReleaseProducer;
        }

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
}
