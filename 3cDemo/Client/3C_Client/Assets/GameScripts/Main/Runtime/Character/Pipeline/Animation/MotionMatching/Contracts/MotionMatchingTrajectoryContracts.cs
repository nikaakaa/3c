using System;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public enum MotionMatchingTrajectorySourceKind : byte
    {
        AcceptedIntent = 1,
        SelectedBody = 2
    }

    public readonly struct CharacterPresentationTrajectoryIntent
    {
        public CharacterPresentationTrajectoryIntent(
            ActorId actorId,
            SimulationTick previousTick,
            SimulationTick currentTick,
            ulong sourceSequence,
            Vector2 desiredPlanarVelocity,
            Vector2 desiredFacing,
            float acceptedAcceleration,
            float acceptedTurnRateDegrees,
            bool grounded,
            string movementModeId,
            ulong resetSequence)
        {
            if (!actorId.IsValid || !currentTick.IsValid || sourceSequence == 0 ||
                !IsFinite(desiredPlanarVelocity) || !IsFinite(desiredFacing) || desiredFacing.sqrMagnitude <= 0f ||
                !float.IsFinite(acceptedAcceleration) || acceptedAcceleration < 0f ||
                !float.IsFinite(acceptedTurnRateDegrees) || acceptedTurnRateDegrees < 0f ||
                string.IsNullOrWhiteSpace(movementModeId))
                throw new ArgumentException("Character Presentation Trajectory Intent is incomplete.");
            if (previousTick.IsValid && previousTick.Value >= currentTick.Value)
                throw new ArgumentException("Character Presentation Trajectory Intent tick interval is not increasing.");
            ActorId = actorId;
            PreviousTick = previousTick;
            CurrentTick = currentTick;
            SourceSequence = sourceSequence;
            DesiredPlanarVelocity = desiredPlanarVelocity;
            DesiredFacing = desiredFacing.normalized;
            AcceptedAcceleration = acceptedAcceleration;
            AcceptedTurnRateDegrees = acceptedTurnRateDegrees;
            Grounded = grounded;
            MovementModeId = movementModeId;
            ResetSequence = resetSequence;
        }

        public ActorId ActorId { get; }
        public SimulationTick PreviousTick { get; }
        public SimulationTick CurrentTick { get; }
        public ulong SourceSequence { get; }
        public Vector2 DesiredPlanarVelocity { get; }
        public Vector2 DesiredFacing { get; }
        public float AcceptedAcceleration { get; }
        public float AcceptedTurnRateDegrees { get; }
        public bool Grounded { get; }
        public string MovementModeId { get; }
        public ulong ResetSequence { get; }

        static bool IsFinite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);
    }

    public readonly struct MotionMatchingTrajectorySourceFrame
    {
        public MotionMatchingTrajectorySourceFrame(
            MotionMatchingTrajectorySourceIdentity identity,
            MotionMatchingTrajectorySourceKind kind,
            ActorId actorId,
            SimulationTick sourceTick,
            ulong sourceSequence,
            Vector3 worldPosition,
            Quaternion worldRotation,
            Vector2 planarVelocity,
            float yawVelocityDegrees,
            Vector2 desiredPlanarVelocity,
            Vector2 desiredFacing,
            float acceptedAcceleration,
            float acceptedTurnRateDegrees,
            bool grounded,
            string movementModeId,
            float sampleAge,
            ulong resetSequence)
        {
            if (!identity.IsValid || !Enum.IsDefined(typeof(MotionMatchingTrajectorySourceKind), kind) ||
                !actorId.IsValid || !sourceTick.IsValid || sourceSequence == 0 ||
                !IsFinite(worldPosition) || !IsFinite(worldRotation) || !IsFinite(planarVelocity) ||
                !float.IsFinite(yawVelocityDegrees) || !IsFinite(desiredPlanarVelocity) ||
                !IsFinite(desiredFacing) || desiredFacing.sqrMagnitude <= 0f ||
                !float.IsFinite(acceptedAcceleration) || acceptedAcceleration < 0f ||
                !float.IsFinite(acceptedTurnRateDegrees) || acceptedTurnRateDegrees < 0f ||
                !float.IsFinite(sampleAge) || sampleAge < 0f || string.IsNullOrWhiteSpace(movementModeId))
                throw new ArgumentException("Motion Matching Trajectory Source Frame is incomplete.");
            Identity = identity;
            Kind = kind;
            ActorId = actorId;
            SourceTick = sourceTick;
            SourceSequence = sourceSequence;
            WorldPosition = worldPosition;
            WorldRotation = worldRotation.normalized;
            PlanarVelocity = planarVelocity;
            YawVelocityDegrees = yawVelocityDegrees;
            DesiredPlanarVelocity = desiredPlanarVelocity;
            DesiredFacing = desiredFacing.normalized;
            AcceptedAcceleration = acceptedAcceleration;
            AcceptedTurnRateDegrees = acceptedTurnRateDegrees;
            Grounded = grounded;
            MovementModeId = movementModeId;
            SampleAge = sampleAge;
            ResetSequence = resetSequence;
        }

        public MotionMatchingTrajectorySourceIdentity Identity { get; }
        public MotionMatchingTrajectorySourceKind Kind { get; }
        public ActorId ActorId { get; }
        public SimulationTick SourceTick { get; }
        public ulong SourceSequence { get; }
        public Vector3 WorldPosition { get; }
        public Quaternion WorldRotation { get; }
        public Vector2 PlanarVelocity { get; }
        public float YawVelocityDegrees { get; }
        public Vector2 DesiredPlanarVelocity { get; }
        public Vector2 DesiredFacing { get; }
        public float AcceptedAcceleration { get; }
        public float AcceptedTurnRateDegrees { get; }
        public bool Grounded { get; }
        public string MovementModeId { get; }
        public float SampleAge { get; }
        public ulong ResetSequence { get; }

        static bool IsFinite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);
        static bool IsFinite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        static bool IsFinite(Quaternion value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z) && float.IsFinite(value.w) && Quaternion.Dot(value, value) > 0f;
    }

    public interface ICharacterMotionMatchingTrajectorySource : IDisposable
    {
        MotionMatchingTrajectorySourceIdentity Identity { get; }
        bool TryGetFrame(out MotionMatchingTrajectorySourceFrame frame);
        void Reset(ulong resetSequence);
    }

    public sealed class AcceptedIntentMotionMatchingTrajectorySource : ICharacterMotionMatchingTrajectorySource
    {
        readonly MotionMatchingTrajectorySourceIdentity m_Identity;
        MotionMatchingTrajectorySourceFrame m_Frame;
        bool m_HasFrame;
        bool m_Disposed;

        public AcceptedIntentMotionMatchingTrajectorySource(MotionMatchingTrajectorySourceIdentity identity)
        {
            m_Identity = identity.IsValid ? identity : throw new ArgumentException("Trajectory Source identity is invalid.", nameof(identity));
        }

        public MotionMatchingTrajectorySourceIdentity Identity => m_Identity;

        public void Publish(
            CharacterPresentationTrajectoryIntent intent,
            Vector3 worldPosition,
            Quaternion worldRotation,
            Vector2 currentPlanarVelocity)
        {
            RequireAlive();
            m_Frame = new MotionMatchingTrajectorySourceFrame(
                m_Identity,
                MotionMatchingTrajectorySourceKind.AcceptedIntent,
                intent.ActorId,
                intent.CurrentTick,
                intent.SourceSequence,
                worldPosition,
                worldRotation,
                currentPlanarVelocity,
                0f,
                intent.DesiredPlanarVelocity,
                intent.DesiredFacing,
                intent.AcceptedAcceleration,
                intent.AcceptedTurnRateDegrees,
                intent.Grounded,
                intent.MovementModeId,
                0f,
                intent.ResetSequence);
            m_HasFrame = true;
        }

        public bool TryGetFrame(out MotionMatchingTrajectorySourceFrame frame)
        {
            RequireAlive();
            frame = m_Frame;
            return m_HasFrame;
        }

        public void Reset(ulong resetSequence)
        {
            RequireAlive();
            m_Frame = default;
            m_HasFrame = false;
        }

        public void Dispose()
        {
            m_Frame = default;
            m_HasFrame = false;
            m_Disposed = true;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(AcceptedIntentMotionMatchingTrajectorySource));
        }
    }

    public sealed class SelectedBodyMotionMatchingTrajectorySource : ICharacterMotionMatchingTrajectorySource
    {
        readonly MotionMatchingTrajectorySourceIdentity m_Identity;
        MotionMatchingTrajectorySourceFrame m_Frame;
        bool m_HasFrame;
        bool m_Disposed;

        public SelectedBodyMotionMatchingTrajectorySource(MotionMatchingTrajectorySourceIdentity identity)
        {
            m_Identity = identity.IsValid ? identity : throw new ArgumentException("Trajectory Source identity is invalid.", nameof(identity));
        }

        public MotionMatchingTrajectorySourceIdentity Identity => m_Identity;

        public void PublishSelectedBody(
            ActorId actorId,
            SimulationTick selectedTick,
            ulong sourceSequence,
            Vector3 worldPosition,
            Quaternion worldRotation,
            Vector2 planarVelocity,
            float yawVelocityDegrees,
            bool grounded,
            string movementModeId,
            float sampleAge,
            ulong resetSequence)
        {
            RequireAlive();
            Vector3 forward = worldRotation * Vector3.forward;
            Vector2 facing = new Vector2(forward.x, forward.z);
            m_Frame = new MotionMatchingTrajectorySourceFrame(
                m_Identity,
                MotionMatchingTrajectorySourceKind.SelectedBody,
                actorId,
                selectedTick,
                sourceSequence,
                worldPosition,
                worldRotation,
                planarVelocity,
                yawVelocityDegrees,
                planarVelocity,
                facing,
                0f,
                0f,
                grounded,
                movementModeId,
                sampleAge,
                resetSequence);
            m_HasFrame = true;
        }

        public bool TryGetFrame(out MotionMatchingTrajectorySourceFrame frame)
        {
            RequireAlive();
            frame = m_Frame;
            return m_HasFrame;
        }

        public void Reset(ulong resetSequence)
        {
            RequireAlive();
            m_Frame = default;
            m_HasFrame = false;
        }

        public void Dispose()
        {
            m_Frame = default;
            m_HasFrame = false;
            m_Disposed = true;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(SelectedBodyMotionMatchingTrajectorySource));
        }
    }

    public readonly struct MotionMatchingTrajectoryEnvelopePoint
    {
        public MotionMatchingTrajectoryEnvelopePoint(
            float timeOffset,
            Vector2 localPositionCenter,
            Vector2 localFacingCenter,
            float positionToleranceRadius,
            float facingToleranceDegrees,
            float confidence)
        {
            if (!float.IsFinite(timeOffset) || timeOffset < 0f || !IsFinite(localPositionCenter) ||
                !IsFinite(localFacingCenter) || localFacingCenter.sqrMagnitude <= 0f ||
                !float.IsFinite(positionToleranceRadius) || positionToleranceRadius < 0f ||
                !float.IsFinite(facingToleranceDegrees) || facingToleranceDegrees < 0f ||
                !float.IsFinite(confidence) || confidence < 0f || confidence > 1f)
                throw new ArgumentException("Motion Matching Trajectory Envelope point is invalid.");
            TimeOffset = timeOffset;
            LocalPositionCenter = localPositionCenter;
            LocalFacingCenter = localFacingCenter.normalized;
            PositionToleranceRadius = positionToleranceRadius;
            FacingToleranceDegrees = facingToleranceDegrees;
            Confidence = confidence;
        }

        public float TimeOffset { get; }
        public Vector2 LocalPositionCenter { get; }
        public Vector2 LocalFacingCenter { get; }
        public float PositionToleranceRadius { get; }
        public float FacingToleranceDegrees { get; }
        public float Confidence { get; }

        static bool IsFinite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);
    }

    public sealed class MotionMatchingTrajectoryEnvelope
    {
        readonly MotionMatchingTrajectoryEnvelopePoint[] m_Points;

        public MotionMatchingTrajectoryEnvelope(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Points = new MotionMatchingTrajectoryEnvelopePoint[capacity];
        }

        public int Capacity => m_Points.Length;
        public int Count { get; private set; }
        public MotionMatchingTrajectorySourceIdentity SourceIdentity { get; private set; }
        public SimulationTick SourceTick { get; private set; }
        public ulong SourceSequence { get; private set; }
        public float SourceAge { get; private set; }
        public ulong ResetSequence { get; private set; }
        public MotionMatchingTrajectoryEnvelopePoint this[int index] => (uint)index < (uint)Count ? m_Points[index] : throw new ArgumentOutOfRangeException(nameof(index));

        public void Begin(MotionMatchingTrajectorySourceFrame frame)
        {
            SourceIdentity = frame.Identity;
            SourceTick = frame.SourceTick;
            SourceSequence = frame.SourceSequence;
            SourceAge = frame.SampleAge;
            ResetSequence = frame.ResetSequence;
            Count = 0;
        }

        public void RestoreIdentity(
            MotionMatchingTrajectorySourceIdentity sourceIdentity,
            SimulationTick sourceTick,
            ulong sourceSequence,
            float sourceAge,
            ulong resetSequence)
        {
            if (!sourceIdentity.IsValid || !sourceTick.IsValid || sourceSequence == 0 || !float.IsFinite(sourceAge) || sourceAge < 0f)
                throw new ArgumentException("Motion Matching Trajectory Envelope replay identity is invalid.");
            SourceIdentity = sourceIdentity;
            SourceTick = sourceTick;
            SourceSequence = sourceSequence;
            SourceAge = sourceAge;
            ResetSequence = resetSequence;
            Count = 0;
        }

        public void Add(MotionMatchingTrajectoryEnvelopePoint point)
        {
            if (Count >= Capacity)
                throw new InvalidOperationException("Motion Matching Trajectory Envelope capacity is exceeded.");
            if (Count > 0 && point.TimeOffset <= m_Points[Count - 1].TimeOffset)
                throw new InvalidOperationException("Motion Matching Trajectory Envelope points are not strictly ordered.");
            m_Points[Count++] = point;
        }

        public void Clear()
        {
            Count = 0;
            SourceIdentity = default;
            SourceTick = default;
            SourceSequence = 0;
            SourceAge = 0f;
            ResetSequence = 0;
        }
    }
}
