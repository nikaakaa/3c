using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline
{
    internal sealed class TimelineActionPreviewAdapter
    {
        readonly CharacterSimulationProgram m_Program;
        readonly ActorId m_ActorId;
        readonly Dictionary<AnimationPlaybackId, ulong>
            m_ActionInstances =
                new Dictionary<AnimationPlaybackId, ulong>();
        ulong m_EventSequence;
        ulong m_NextActionInstanceId;

        internal TimelineActionPreviewAdapter(
            CharacterSimulationProgram program,
            ActorId actorId)
        {
            m_Program = program ??
                throw new ArgumentNullException(nameof(program));
            m_ActorId = actorId;
            if (!m_ActorId.IsValid)
            {
                throw new ArgumentException(
                    "Timeline Action Preview actor identity is invalid.",
                    nameof(actorId));
            }
        }

        internal PresentationCommand CreateCommand(
            PresentationCommandKind kind,
            CharacterPresentationProducerEntry producer,
            ulong generation,
            SimulationTick tick,
            ActivationId activation,
            float sampleTime,
            string channel)
        {
            if (producer == null ||
                producer.Kind !=
                    CharacterPresentationProducerKind.Animation ||
                producer.Animation == null ||
                generation == 0 ||
                (byte)kind <
                    (byte)PresentationCommandKind
                        .SelectProducer ||
                (byte)kind >
                    (byte)PresentationCommandKind
                        .ReleaseProducer)
            {
                throw new ArgumentException(
                    "Timeline Action Preview command target is invalid.",
                    nameof(producer));
            }
            var playbackId =
                new AnimationPlaybackId(
                    producer.ProducerId,
                    generation);
            ulong actionInstanceId;
            if (kind ==
                PresentationCommandKind.SelectProducer)
            {
                if (m_ActionInstances.ContainsKey(playbackId))
                {
                    throw new InvalidOperationException(
                        $"Timeline Action Preview playback '{playbackId}' was already selected.");
                }
                actionInstanceId =
                    AllocateActionInstanceIdentity();
            }
            else if (!m_ActionInstances.TryGetValue(
                         playbackId,
                         out actionInstanceId))
            {
                throw new InvalidOperationException(
                    $"Timeline Action Preview playback '{playbackId}' was not selected.");
            }

            m_EventSequence++;
            if (m_EventSequence == 0)
            {
                throw new InvalidOperationException(
                    "Timeline Action Preview event identity was exhausted.");
            }
            EventId eventId = EventId.Create(
                m_Program.ProgramHash,
                m_ActorId,
                activation,
                tick,
                m_EventSequence,
                channel);
            var header = new SimulationEventHeader(
                m_Program.Manifest.NumericProfile,
                eventId,
                m_ActorId,
                tick,
                activation,
                m_EventSequence,
                channel);
            var command = new PresentationCommand(
                header,
                kind,
                producer.ProgramProducerIdentity,
                Float32Scalar.FromSingle(
                    Math.Max(0f, sampleTime)),
                Float32Scalar.One,
                generation,
                0,
                actionInstanceId,
                Float32Scalar.Zero);
            if (kind ==
                PresentationCommandKind.SelectProducer)
            {
                m_ActionInstances.Add(
                    playbackId,
                    actionInstanceId);
            }
            return command;
        }

        internal void Forget(
            AnimationPlaybackId playbackId)
        {
            if (!playbackId.IsValid ||
                !m_ActionInstances.Remove(playbackId))
            {
                throw new InvalidOperationException(
                    $"Timeline Action Preview playback '{playbackId}' cannot be forgotten.");
            }
        }

        internal void Reset()
        {
            m_ActionInstances.Clear();
        }

        ulong AllocateActionInstanceIdentity()
        {
            m_NextActionInstanceId++;
            if (m_NextActionInstanceId == 0)
            {
                throw new InvalidOperationException(
                    "Timeline Action Preview ActionInstance identity was exhausted.");
            }
            return m_NextActionInstanceId;
        }
    }

    internal sealed class PoseGraphFactPreviewAdapter
    {
        readonly ActorId m_ActorId;
        readonly CharacterPresentationBodyState m_BodyFixture;

        internal PoseGraphFactPreviewAdapter(
            ActorId actorId,
            CharacterPresentationBodyState bodyFixture)
        {
            m_ActorId = actorId;
            if (!m_ActorId.IsValid)
            {
                throw new ArgumentException(
                    "Pose Graph Fact Preview actor identity is invalid.",
                    nameof(actorId));
            }
            if (!bodyFixture.ActorId.IsValid)
                throw new ArgumentException("Pose Graph Preview body fixture is invalid.", nameof(bodyFixture));
            m_BodyFixture = bodyFixture;
        }

        internal CharacterBodyPresentationFrame CreateBodyFrame(
            ulong evaluationTick) =>
            CreateBodyFrame(
                evaluationTick,
                m_BodyFixture.Grounded,
                new Vector2(
                    m_BodyFixture.LinearVelocity.x,
                    m_BodyFixture.LinearVelocity.z).magnitude,
                m_BodyFixture.LinearVelocity.y,
                ResolveLocalDirection(
                    m_BodyFixture.Rotation,
                    m_BodyFixture.LinearVelocity));

        internal CharacterBodyPresentationFrame CreateBodyFrame(
            ulong evaluationTick,
            bool grounded,
            float horizontalSpeed,
            float verticalSpeed,
            Vector2 movementDirection)
        {
            if (evaluationTick == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(evaluationTick));
            }
            if (!float.IsFinite(horizontalSpeed) || horizontalSpeed < 0f ||
                !float.IsFinite(verticalSpeed) ||
                !float.IsFinite(movementDirection.x) ||
                !float.IsFinite(movementDirection.y))
            {
                throw new ArgumentException("Pose Graph Preview body fixture values are invalid.");
            }
            Vector2 direction = movementDirection.sqrMagnitude > 1f
                ? movementDirection.normalized
                : movementDirection;
            Vector3 velocity =
                m_BodyFixture.Rotation *
                new Vector3(
                    direction.x * horizontalSpeed,
                    0f,
                    direction.y * horizontalSpeed) +
                Vector3.up * verticalSpeed;
            var target = new CharacterVisualTrajectorySample(
                m_BodyFixture.Position,
                m_BodyFixture.Rotation,
                velocity,
                0f,
                grounded);
            var visible = new CharacterVisualTrajectoryResult(
                m_BodyFixture.Position,
                m_BodyFixture.Rotation,
                velocity,
                0f,
                Vector3.zero,
                0f,
                Vector3.zero,
                0f,
                false,
                false,
                true);
            return new CharacterBodyPresentationFrame(
                evaluationTick > 1
                    ? evaluationTick - 1
                    : evaluationTick,
                evaluationTick,
                1f,
                0f,
                CharacterBodyPresentationSourceMode
                    .CommittedStream,
                CharacterVisualTrajectoryMode.Direct,
                target,
                visible,
                Vector3.zero,
                Vector3.zero,
                grounded,
                grounded,
                1,
                CharacterBodyPresentationResetReason
                    .Initialization);
        }

        static Vector2 ResolveLocalDirection(
            Quaternion rotation,
            Vector3 velocity)
        {
            Vector3 local = Quaternion.Inverse(rotation) * velocity;
            Vector2 planar = new Vector2(local.x, local.z);
            return planar.sqrMagnitude <= 0.000001f
                ? Vector2.zero
                : planar.normalized;
        }

        internal CharacterPresentationFactFrame CreateFactFrame(
            ulong presentationFrame,
            ulong simulationTick,
            double presentationTime,
            in CharacterBodyPresentationFrame bodyFrame,
            bool grounded = true,
            float horizontalSpeed = 0f,
            float horizontalAcceleration = 0f,
            float verticalSpeed = 0f,
            Vector2 movementDirection = default,
            Vector2 desiredDirection = default,
            float facingError = 0f,
            CharacterPresentationMotionPhase motionPhase =
                CharacterPresentationMotionPhase
                    .GroundedStationary)
        {
            return new CharacterPresentationFactFrame(
                new CharacterPresentationFactFrameIdentity(
                    m_ActorId,
                    presentationFrame),
                new SimulationTick(simulationTick),
                presentationTime,
                grounded,
                horizontalSpeed,
                horizontalAcceleration,
                verticalSpeed,
                movementDirection,
                desiredDirection,
                facingError,
                motionPhase,
                CharacterPresentationTrajectoryIntent.StationaryMovementModeId,
                bodyFrame.ResetSequence);
        }
    }
}
