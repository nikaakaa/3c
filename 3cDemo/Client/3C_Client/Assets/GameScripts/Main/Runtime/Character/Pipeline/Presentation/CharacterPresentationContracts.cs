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
        ForceReleaseProducer = 11
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
            string domainPayload = null)
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
            if (kind == CharacterPresentationCommandKind.DomainEvent &&
                (sourceActionInstanceId == 0 || string.IsNullOrWhiteSpace(DomainPayload)))
                throw new ArgumentException("Domain event command requires an Action instance and payload.", nameof(domainPayload));
            if (kind != CharacterPresentationCommandKind.DomainEvent && DomainPayload.Length != 0)
                throw new ArgumentException("Domain payload is only valid on Domain event commands.", nameof(domainPayload));
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
                command.DomainPayload);
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
            float followerYawCorrectionDegrees)
        {
            BodyBranchReplacementCount = bodyBranchReplacementCount;
            AnimationBranchReplacementCount = animationBranchReplacementCount;
            FollowerPositionCorrectionMeters = followerPositionCorrectionMeters;
            FollowerYawCorrectionDegrees = followerYawCorrectionDegrees;
        }

        public int BodyBranchReplacementCount { get; }
        public int AnimationBranchReplacementCount { get; }
        public float FollowerPositionCorrectionMeters { get; }
        public float FollowerYawCorrectionDegrees { get; }
    }

    public interface ICharacterPresentationDomainRuntime :
        IDisposable,
        IGameplayPresentationFrameTarget
    {
        bool AcceptsTrajectoryIntent { get; }
        ulong BodyResetSequence { get; }
        bool TryGetLatestBody(out CharacterPresentationBodyState body);
        void CaptureBodyTransaction(IReadOnlyList<CharacterPresentationBodyInterval> intervals);
        void CaptureTrajectoryIntent(CharacterPresentationTrajectoryIntent intent);
        void CaptureEquipmentSelections(IReadOnlyList<EquipmentVisualSelection> selections);
        void Publish(CharacterPresentationCommand command);
        void Replace(CharacterPresentationCommand current, CharacterPresentationCommand replacement);
        void Retire(CharacterPresentationCommand command);
        void Reset();
        CharacterPresentationDomainDiagnosticsSnapshot CaptureDiagnostics();
        bool SupportsCheckpointCapture { get; }
        bool SupportsCheckpointRestore { get; }
        bool TryCaptureCheckpoint(SimulationSessionCheckpoint checkpoint, out string error);
        bool TryRestoreCheckpoint(SimulationSessionCheckpoint checkpoint, out string error);
    }
}
