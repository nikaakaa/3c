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
    public enum CharacterPoseStageStatus : byte
    {
        Completed = 1,
        Unavailable = 2
    }

    public enum CharacterPoseStageUnavailableReason : byte
    {
        None = 0,
        PoseUnavailable = 1,
        WorldContextUnavailable = 2,
        UpstreamStageUnavailable = 3
    }

    public readonly struct CharacterPoseExecutionStageSnapshot
    {
        public CharacterPoseExecutionStageSnapshot(
            int stageIndex,
            CharacterPoseExecutionDomain executionDomain,
            CharacterPoseSpace inputPoseSpace,
            CharacterPoseSpace outputPoseSpace,
            CharacterPoseStageStatus status,
            CharacterPoseStageUnavailableReason unavailableReason,
            ulong completionIdentity)
        {
            bool completed = status == CharacterPoseStageStatus.Completed;
            if (stageIndex < 0 ||
                (byte)executionDomain < (byte)CharacterPoseExecutionDomain.FactAndDemand ||
                (byte)executionDomain > (byte)CharacterPoseExecutionDomain.FinalPublication ||
                (byte)inputPoseSpace > (byte)CharacterPoseSpace.Component ||
                (byte)outputPoseSpace > (byte)CharacterPoseSpace.Component ||
                (byte)status < (byte)CharacterPoseStageStatus.Completed ||
                (byte)status > (byte)CharacterPoseStageStatus.Unavailable ||
                (byte)unavailableReason > (byte)CharacterPoseStageUnavailableReason.UpstreamStageUnavailable ||
                completed && (completionIdentity == 0 || unavailableReason != CharacterPoseStageUnavailableReason.None) ||
                !completed && (completionIdentity != 0 || unavailableReason == CharacterPoseStageUnavailableReason.None))
            {
                throw new ArgumentException("Pose execution stage snapshot is invalid.");
            }
            StageIndex = stageIndex;
            ExecutionDomain = executionDomain;
            InputPoseSpace = inputPoseSpace;
            OutputPoseSpace = outputPoseSpace;
            Status = status;
            UnavailableReason = unavailableReason;
            CompletionIdentity = completionIdentity;
        }

        public int StageIndex { get; }
        public CharacterPoseExecutionDomain ExecutionDomain { get; }
        public CharacterPoseSpace InputPoseSpace { get; }
        public CharacterPoseSpace OutputPoseSpace { get; }
        public CharacterPoseStageStatus Status { get; }
        public CharacterPoseStageUnavailableReason UnavailableReason { get; }
        public ulong CompletionIdentity { get; }
        public bool IsCompleted => Status == CharacterPoseStageStatus.Completed;
    }

    public readonly struct CharacterPosePlanStageSnapshot
    {
        readonly CharacterPoseExecutionStageSnapshot[] m_Stages;

        public CharacterPosePlanStageSnapshot(
            string poseGraphId,
            string posePlanHash,
            AnimationPoseAvailability composedAvailability,
            CharacterPoseExecutionStageSnapshot[] stages)
        {
            if (string.IsNullOrWhiteSpace(poseGraphId) || string.IsNullOrWhiteSpace(posePlanHash) ||
                (byte)composedAvailability < (byte)AnimationPoseAvailability.Pose ||
                (byte)composedAvailability > (byte)AnimationPoseAvailability.Invalid ||
                stages == null || stages.Length == 0)
            {
                throw new ArgumentException("Pose Plan stage snapshot is invalid.");
            }
            for (int i = 0; i < stages.Length; i++)
            {
                if (stages[i].StageIndex != i ||
                    i > 0 && stages[i - 1].Status == CharacterPoseStageStatus.Unavailable &&
                    stages[i].Status == CharacterPoseStageStatus.Completed)
                {
                    throw new ArgumentException("Pose Plan stage snapshot order is invalid.");
                }
            }
            PoseGraphId = poseGraphId;
            PosePlanHash = posePlanHash;
            ComposedAvailability = composedAvailability;
            m_Stages = stages;
        }

        public string PoseGraphId { get; }
        public string PosePlanHash { get; }
        public AnimationPoseAvailability ComposedAvailability { get; }
        public IReadOnlyList<CharacterPoseExecutionStageSnapshot> Stages =>
            m_Stages ?? Array.Empty<CharacterPoseExecutionStageSnapshot>();
        public bool IsValid => !string.IsNullOrWhiteSpace(PoseGraphId) &&
                               !string.IsNullOrWhiteSpace(PosePlanHash) &&
                               m_Stages != null && m_Stages.Length > 0;
    }

    internal static class CharacterPosePlanStageSnapshotFactory
    {
        internal static CharacterPosePlanStageSnapshot Completed(
            CharacterPresentationPosePlan plan,
            in ComposedAnimationPoseFrame composed)
        {
            RequirePlan(plan);
            var stages = new CharacterPoseExecutionStageSnapshot[plan.Stages.Count];
            for (int i = 0; i < stages.Length; i++)
                stages[i] = Complete(plan.Stages[i], composed.CompletionIdentity);
            return new CharacterPosePlanStageSnapshot(
                composed.PoseGraphId,
                composed.PosePlanHash,
                composed.Availability,
                stages);
        }

        internal static CharacterPosePlanStageSnapshot Unavailable(
            CharacterPresentationPosePlan plan,
            AnimationPoseAvailability composedAvailability,
            CharacterPoseStageUnavailableReason reason,
            int firstUnavailableStageIndex = 0,
            ulong completedIdentity = 0)
        {
            RequirePlan(plan);
            if (firstUnavailableStageIndex < 0 || firstUnavailableStageIndex >= plan.Stages.Count ||
                firstUnavailableStageIndex > 0 && completedIdentity == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(firstUnavailableStageIndex));
            }
            var stages = new CharacterPoseExecutionStageSnapshot[plan.Stages.Count];
            for (int i = 0; i < stages.Length; i++)
            {
                stages[i] = i < firstUnavailableStageIndex
                    ? Complete(plan.Stages[i], completedIdentity)
                    : Missing(
                        plan.Stages[i],
                        i == firstUnavailableStageIndex
                            ? reason
                            : CharacterPoseStageUnavailableReason.UpstreamStageUnavailable);
            }
            return new CharacterPosePlanStageSnapshot(
                plan.PoseGraphId,
                plan.PlanHash,
                composedAvailability,
                stages);
        }

        internal static CharacterPosePlanStageSnapshot Preview(
            CharacterPresentationPosePlan plan,
            in ComposedAnimationPoseFrame composed,
            bool worldContextAvailable)
        {
            RequirePlan(plan);
            int worldStage = -1;
            for (int i = 0; i < plan.Stages.Count; i++)
            {
                if (plan.Stages[i].ExecutionDomain != CharacterPoseExecutionDomain.WorldAwareValue)
                    continue;
                worldStage = i;
                break;
            }
            if (worldStage >= 0 && !worldContextAvailable)
            {
                return Unavailable(
                    plan,
                    AnimationPoseAvailability.Invalid,
                    CharacterPoseStageUnavailableReason.WorldContextUnavailable,
                    worldStage,
                    composed.CompletionIdentity == 0
                        ? 1UL
                        : composed.CompletionIdentity);
            }
            if (composed.Availability != AnimationPoseAvailability.Pose)
            {
                return Unavailable(
                    plan,
                    composed.Availability,
                    CharacterPoseStageUnavailableReason.PoseUnavailable);
            }
            return Completed(plan, in composed);
        }

        static CharacterPoseExecutionStageSnapshot Complete(
            CharacterPresentationPoseStage stage,
            ulong completionIdentity) =>
            new CharacterPoseExecutionStageSnapshot(
                stage.Index,
                stage.ExecutionDomain,
                stage.InputPoseSpace,
                stage.OutputPoseSpace,
                CharacterPoseStageStatus.Completed,
                CharacterPoseStageUnavailableReason.None,
                completionIdentity);

        static CharacterPoseExecutionStageSnapshot Missing(
            CharacterPresentationPoseStage stage,
            CharacterPoseStageUnavailableReason reason) =>
            new CharacterPoseExecutionStageSnapshot(
                stage.Index,
                stage.ExecutionDomain,
                stage.InputPoseSpace,
                stage.OutputPoseSpace,
                CharacterPoseStageStatus.Unavailable,
                reason,
                0);

        static void RequirePlan(CharacterPresentationPosePlan plan)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
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
            CharacterPosePlanStageSnapshot posePlanStages,
            bool hasAnimation,
            AnimationPresentationRuntimeSnapshot animation)
        {
            BodyBranchReplacementCount = bodyBranchReplacementCount;
            AnimationBranchReplacementCount = animationBranchReplacementCount;
            FollowerPositionCorrectionMeters = followerPositionCorrectionMeters;
            FollowerYawCorrectionDegrees = followerYawCorrectionDegrees;
            PosePlanStages = posePlanStages;
            HasAnimation = hasAnimation;
            Animation = animation;
        }

        public ulong BodyBranchReplacementCount { get; }
        public ulong AnimationBranchReplacementCount { get; }
        public float FollowerPositionCorrectionMeters { get; }
        public float FollowerYawCorrectionDegrees { get; }
        public CharacterPosePlanStageSnapshot PosePlanStages { get; }
        public bool HasAnimation { get; }
        public AnimationPresentationRuntimeSnapshot Animation { get; }
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
            int cycle = 0,
            ulong sourceActionInstanceId = 0,
            float visualTimeScale = 0f)
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
                command.VisualTimeScale.ToSingle());
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
