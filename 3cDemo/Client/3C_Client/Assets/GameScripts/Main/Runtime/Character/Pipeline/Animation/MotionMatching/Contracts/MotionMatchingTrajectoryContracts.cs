using System;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    internal enum MotionMatchingTrajectorySourceKind : byte
    {
        AcceptedIntent = 1,
        SelectedBody = 2
    }

    public readonly struct CharacterPresentationTrajectoryIntent
    {
        public const string StationaryMovementModeId = "presentation.movement-mode.stationary";

        public CharacterPresentationTrajectoryIntent(
            ActorId actorId,
            SimulationTick previousTick,
            SimulationTick currentTick,
            ulong sourceSequence,
            Vector2 desiredPlanarVelocity,
            Vector2 desiredFacing,
            float acceptedAcceleration,
            float acceptedTurnRateDegrees,
            bool hasMotion,
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
            HasMotion = hasMotion;
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
        public bool HasMotion { get; }
        public bool Grounded { get; }
        public string MovementModeId { get; }
        public ulong ResetSequence { get; }

        public static CharacterPresentationTrajectoryIntent FromFloat32(
            SimulationActorTickResult result,
            ulong sourceSequence,
            ulong resetSequence)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            Float32Vector3 velocity = result.Motion.RequestedVelocity;
            var desiredVelocity = new Vector2(velocity.X.ToSingle(), velocity.Z.ToSingle());
            return new CharacterPresentationTrajectoryIntent(
                result.ActorId,
                result.Tick.Value > 1 ? new SimulationTick(result.Tick.Value - 1) : default,
                result.Tick,
                sourceSequence,
                desiredVelocity,
                ResolveDesiredFacing(
                    desiredVelocity,
                    result.BodySample.FinalBody.Yaw.Degrees.ToSingle()),
                float.MaxValue,
                float.MaxValue,
                HasPlanarMotion(desiredVelocity),
                result.BodySample.FinalBody.Grounded,
                ResolveMovementModeId(
                    result.Motion.LocomotionOwnerIdentity,
                    result.Motion.ActionOwnerIdentity,
                    result.Motion.GameplayResultOwnerIdentity),
                resetSequence);
        }

        public static string ResolveMovementModeId(
            string locomotionOwnerIdentity,
            string actionOwnerIdentity,
            string gameplayResultOwnerIdentity)
        {
            string ownerIdentity =
                string.IsNullOrWhiteSpace(actionOwnerIdentity) &&
                !string.IsNullOrWhiteSpace(gameplayResultOwnerIdentity)
                    ? gameplayResultOwnerIdentity
                    : locomotionOwnerIdentity;
            if (string.IsNullOrWhiteSpace(ownerIdentity))
                return StationaryMovementModeId;

            const string stateGraphReference = "/reference:stateBehaviorGraph.";
            int referenceIndex = ownerIdentity.IndexOf(
                stateGraphReference,
                StringComparison.Ordinal);
            if (referenceIndex < 0)
            {
                throw new InvalidOperationException(
                    $"Committed movement owner '{ownerIdentity}' has no enclosing Gameplay State identity.");
            }
            int nodeIndex = ownerIdentity.LastIndexOf(
                "/node:",
                referenceIndex,
                StringComparison.Ordinal);
            int valueIndex = nodeIndex + "/node:".Length;
            if (nodeIndex < 0 || valueIndex >= referenceIndex)
            {
                throw new InvalidOperationException(
                    $"Committed movement owner '{ownerIdentity}' has an invalid Gameplay State identity.");
            }
            string stateId = ownerIdentity.Substring(
                valueIndex,
                referenceIndex - valueIndex);
            if (string.IsNullOrWhiteSpace(stateId))
            {
                throw new InvalidOperationException(
                    $"Committed movement owner '{ownerIdentity}' has an empty Gameplay State identity.");
            }
            return $"presentation.movement-mode.state/{stateId}";
        }

        public static bool HasPlanarMotion(Vector2 desiredPlanarVelocity)
        {
            if (!IsFinite(desiredPlanarVelocity))
                throw new ArgumentException("Character Presentation planar motion input is invalid.", nameof(desiredPlanarVelocity));
            return desiredPlanarVelocity.sqrMagnitude > 0.00000001f;
        }

        public static Vector2 ResolveDesiredFacing(Vector2 desiredPlanarVelocity, float committedYawDegrees)
        {
            if (!IsFinite(desiredPlanarVelocity) || !float.IsFinite(committedYawDegrees))
                throw new ArgumentException("Character Presentation desired facing input is invalid.");
            if (desiredPlanarVelocity.sqrMagnitude > 0.00000001f)
                return desiredPlanarVelocity.normalized;
            Quaternion rotation = Quaternion.Euler(0f, committedYawDegrees, 0f);
            Vector3 forward = rotation * Vector3.forward;
            return new Vector2(forward.x, forward.z).normalized;
        }

        static bool IsFinite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);
    }

    internal readonly struct MotionMatchingTrajectorySourceFrame
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
                !float.IsFinite(sampleAge) || sampleAge < 0f ||
                kind == MotionMatchingTrajectorySourceKind.AcceptedIntent && string.IsNullOrWhiteSpace(movementModeId))
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
        sealed class Page
        {
            internal Page(int capacity)
            {
                Points = new MotionMatchingTrajectoryEnvelopePoint[capacity];
            }

            internal readonly MotionMatchingTrajectoryEnvelopePoint[] Points;
            internal int Count;
            internal MotionMatchingTrajectorySourceIdentity SourceIdentity;
            internal SimulationTick SourceTick;
            internal ulong SourceSequence;
            internal float SourceAge;
            internal ulong ResetSequence;

            internal void Clear()
            {
                Count = 0;
                SourceIdentity = default;
                SourceTick = default;
                SourceSequence = 0;
                SourceAge = 0f;
                ResetSequence = 0;
            }
        }

        Page m_CommittedPage;
        Page m_PendingPage;
        Page m_CurrentPage;
        bool m_FrameOpen;

        public MotionMatchingTrajectoryEnvelope(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_CommittedPage = new Page(capacity);
            m_PendingPage = new Page(capacity);
            m_CurrentPage = m_CommittedPage;
        }

        public int Capacity => m_CurrentPage.Points.Length;
        public int Count => m_CurrentPage.Count;
        public MotionMatchingTrajectorySourceIdentity SourceIdentity => m_CurrentPage.SourceIdentity;
        public SimulationTick SourceTick => m_CurrentPage.SourceTick;
        public ulong SourceSequence => m_CurrentPage.SourceSequence;
        public float SourceAge => m_CurrentPage.SourceAge;
        public ulong ResetSequence => m_CurrentPage.ResetSequence;
        public MotionMatchingTrajectoryEnvelopePoint this[int index] =>
            (uint)index < (uint)m_CurrentPage.Count
                ? m_CurrentPage.Points[index]
                : throw new ArgumentOutOfRangeException(nameof(index));

        internal void BeginFrame()
        {
            if (m_FrameOpen)
                throw new InvalidOperationException("Motion Matching Trajectory Envelope frame is already open.");
            m_PendingPage.Clear();
            m_CurrentPage = m_PendingPage;
            m_FrameOpen = true;
        }

        internal void CommitFrame()
        {
            RequireOpenFrame();
            Page previous = m_CommittedPage;
            m_CommittedPage = m_PendingPage;
            m_PendingPage = previous;
            m_CurrentPage = m_CommittedPage;
            m_FrameOpen = false;
        }

        internal void DiscardFrame()
        {
            RequireOpenFrame();
            m_CurrentPage = m_CommittedPage;
            m_FrameOpen = false;
        }

        internal void Begin(MotionMatchingTrajectorySourceFrame frame)
        {
            m_CurrentPage.SourceIdentity = frame.Identity;
            m_CurrentPage.SourceTick = frame.SourceTick;
            m_CurrentPage.SourceSequence = frame.SourceSequence;
            m_CurrentPage.SourceAge = frame.SampleAge;
            m_CurrentPage.ResetSequence = frame.ResetSequence;
            m_CurrentPage.Count = 0;
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
            m_CurrentPage.SourceIdentity = sourceIdentity;
            m_CurrentPage.SourceTick = sourceTick;
            m_CurrentPage.SourceSequence = sourceSequence;
            m_CurrentPage.SourceAge = sourceAge;
            m_CurrentPage.ResetSequence = resetSequence;
            m_CurrentPage.Count = 0;
        }

        public void Add(MotionMatchingTrajectoryEnvelopePoint point)
        {
            if (m_CurrentPage.Count >= Capacity)
                throw new InvalidOperationException("Motion Matching Trajectory Envelope capacity is exceeded.");
            if (m_CurrentPage.Count > 0 && point.TimeOffset <= m_CurrentPage.Points[m_CurrentPage.Count - 1].TimeOffset)
                throw new InvalidOperationException("Motion Matching Trajectory Envelope points are not strictly ordered.");
            m_CurrentPage.Points[m_CurrentPage.Count++] = point;
        }

        public void Clear()
        {
            m_CurrentPage.Clear();
        }

        void RequireOpenFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException("Motion Matching Trajectory Envelope has no open frame.");
        }
    }
}
