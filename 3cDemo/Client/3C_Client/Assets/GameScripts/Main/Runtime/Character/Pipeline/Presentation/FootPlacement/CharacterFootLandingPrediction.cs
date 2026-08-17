using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterFootLandingPredictionState : byte
    {
        Rejected = 1,
        Accepted = 2
    }

    public enum CharacterFootLandingPredictionRejectReason : byte
    {
        None = 0,
        StepUnavailable = 1,
        StepIdentityMismatch = 2,
        LandingTimeInvalid = 3,
        MotionTimelineUnavailable = 4,
        FutureBodyTranslationUnavailable = 5,
        FutureBodyTranslationRangeInvalid = 6,
        GroundQueryMissed = 7
    }

    public enum CharacterFootLandingStepSource : byte
    {
        None = 0,
        Current = 1,
        Incoming = 2
    }

    internal readonly struct CharacterFootLandingSupport
    {
        internal CharacterFootLandingSupport(
            int surfaceIdentity,
            Vector3 point,
            Vector3 normal,
            float distance)
        {
            if (surfaceIdentity == 0 || !Finite(point) || !Finite(normal) ||
                normal.sqrMagnitude <= 0.000001f ||
                !float.IsFinite(distance) || distance < 0f)
            {
                throw new ArgumentException("Foot Landing support is invalid.");
            }
            SurfaceIdentity = surfaceIdentity;
            Point = point;
            Normal = normal.normalized;
            Distance = distance;
        }

        internal int SurfaceIdentity { get; }
        internal Vector3 Point { get; }
        internal Vector3 Normal { get; }
        internal float Distance { get; }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

    internal interface ICharacterFootLandingWorldQuery
    {
        bool TryQuery(
            in CharacterFootPlacementQueryRequest request,
            out CharacterFootLandingSupport support);
    }

    internal sealed class CharacterFootLandingWorldQuery : ICharacterFootLandingWorldQuery
    {
        readonly CharacterFootPlacementWorldQueryBackend m_Backend;

        internal CharacterFootLandingWorldQuery(
            CharacterFootPlacementWorldQueryBackend backend)
        {
            m_Backend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        public bool TryQuery(
            in CharacterFootPlacementQueryRequest request,
            out CharacterFootLandingSupport support)
        {
            if (!m_Backend.Query(in request, out CharacterFootPlacementQueryHit hit))
            {
                support = default;
                return false;
            }
            support = new CharacterFootLandingSupport(
                hit.SurfaceIdentity,
                hit.Point,
                hit.Normal,
                hit.Distance);
            return true;
        }
    }

    public readonly struct CharacterFootLandingPredictionFootDiagnostics
    {
        internal CharacterFootLandingPredictionFootDiagnostics(
            CharacterFootSide side,
            CharacterFootLandingPredictionState state,
            CharacterFootLandingPredictionRejectReason rejectReason,
            CharacterFootLandingStepSource stepSource,
            ulong landingEventIdentity,
            ulong trajectoryGeneration,
            float landingConfidence,
            float timeToLandingSeconds,
            Vector3 rootLocalLanding,
            bool futureBodyTranslationAvailable,
            in ThirdPersonSimulation.CharacterFutureBodyTranslationSample futureBodyTranslation,
            Vector3 currentAnimatedSole,
            Vector3 rawLandingCandidate,
            CharacterFootPlacementQueryRequest query,
            CharacterFootLandingSupport support,
            CharacterFullBodyIkGoal goal)
        {
            Side = side;
            State = state;
            RejectReason = rejectReason;
            StepSource = stepSource;
            LandingEventIdentity = landingEventIdentity;
            TrajectoryGeneration = trajectoryGeneration;
            LandingConfidence = landingConfidence;
            TimeToLandingSeconds = timeToLandingSeconds;
            RootLocalLanding = rootLocalLanding;
            FutureBodyTranslationAvailable = futureBodyTranslationAvailable;
            FutureBodyRelativeTranslation = futureBodyTranslationAvailable
                ? new Vector3(
                    futureBodyTranslation.RelativePositionX,
                    futureBodyTranslation.RelativePositionY,
                    futureBodyTranslation.RelativePositionZ)
                : default;
            FutureBodyTranslationVelocity = futureBodyTranslationAvailable
                ? new Vector3(
                    futureBodyTranslation.VelocityX,
                    futureBodyTranslation.VelocityY,
                    futureBodyTranslation.VelocityZ)
                : default;
            CurrentAnimatedSole = currentAnimatedSole;
            RawLandingCandidate = rawLandingCandidate;
            Query = query;
            SurfaceIdentity = support.SurfaceIdentity;
            LandingPoint = support.Point;
            LandingNormal = support.Normal;
            QueryDistance = support.Distance;
            Goal = goal;
        }

        public CharacterFootSide Side { get; }
        public CharacterFootLandingPredictionState State { get; }
        public CharacterFootLandingPredictionRejectReason RejectReason { get; }
        public CharacterFootLandingStepSource StepSource { get; }
        public ulong LandingEventIdentity { get; }
        public ulong TrajectoryGeneration { get; }
        public float LandingConfidence { get; }
        public float TimeToLandingSeconds { get; }
        public Vector3 RootLocalLanding { get; }
        public bool FutureBodyTranslationAvailable { get; }
        public Vector3 FutureBodyRelativeTranslation { get; }
        public Vector3 FutureBodyTranslationVelocity { get; }
        public Vector3 CurrentAnimatedSole { get; }
        public Vector3 RawLandingCandidate { get; }
        public CharacterFootPlacementQueryRequest Query { get; }
        public int SurfaceIdentity { get; }
        public Vector3 LandingPoint { get; }
        public Vector3 LandingNormal { get; }
        public float QueryDistance { get; }
        public CharacterFullBodyIkGoal Goal { get; }
        public bool Accepted => State == CharacterFootLandingPredictionState.Accepted;
    }

    public readonly struct CharacterFootLandingPredictionInputDiagnostics
    {
        internal CharacterFootLandingPredictionInputDiagnostics(
            float presentationDeltaSeconds,
            CharacterBodyPresentationFrame body,
            in ThirdPersonSimulation.CommittedLocomotionPlanarMotionTimeline timeline,
            float currentSegmentRemainingSeconds)
        {
            PresentationDeltaSeconds = presentationDeltaSeconds;
            PreviousBodyTick = body.PreviousTick;
            CurrentBodyTick = body.CurrentTick;
            BodySampleAlpha = body.SampleAlpha;
            BodySampleAgeSeconds = body.SampleAgeSeconds;
            VisibleBodyPosition = body.VisiblePosition;
            VisibleBodyRotation = body.VisibleRotation;
            VisibleBodyVelocity = body.VisibleVelocity;
            VisibleBodyYawVelocityDegreesPerSecond =
                body.VisibleYawVelocityDegreesPerSecond;
            TargetBodyPosition = body.TargetPosition;
            TargetBodyRotation = body.TargetRotation;
            TargetBodyVelocity = body.TargetVelocity;
            TargetBodyYawVelocityDegreesPerSecond =
                body.TargetYawVelocityDegreesPerSecond;
            BodyPositionError = body.PositionError;
            BodyRotationError = body.RotationError;
            CorrectionPositionError = body.CorrectionPositionError;
            CorrectionPositionVelocity = body.CorrectionPositionVelocity;
            CorrectionYawVelocityDegreesPerSecond =
                body.CorrectionYawVelocityDegreesPerSecond;
            CorrectionActive = body.CorrectionActive;
            CorrectionClamped = body.CorrectionClamped;
            CorrectionSettled = body.CorrectionSettled;
            BodyResetSequence = body.ResetSequence;
            MotionTimelineAvailable = timeline.IsValid;
            TimelineGeneration = timeline.Generation;
            TimelineAuthorityTick = timeline.AuthorityTick.Value;
            TimelineTickRate = timeline.TickRate;
            TimelineCurrentVelocityX = timeline.CurrentVelocityX;
            TimelineCurrentVelocityZ = timeline.CurrentVelocityZ;
            TimelineContinuationVelocityX = timeline.ContinuationVelocityX;
            TimelineContinuationVelocityZ = timeline.ContinuationVelocityZ;
            TimelineHasContinuation = timeline.HasContinuation;
            TimelineBodyYawVelocityDegreesPerSecond =
                timeline.BodyYawVelocityDegreesPerSecond;
            TimelineMaximumBodyYawVelocityDegreesPerSecond =
                timeline.MaximumBodyYawVelocityDegreesPerSecond;
            CurrentSegmentRemainingSeconds = currentSegmentRemainingSeconds;
        }

        public float PresentationDeltaSeconds { get; }
        public ulong PreviousBodyTick { get; }
        public ulong CurrentBodyTick { get; }
        public float BodySampleAlpha { get; }
        public float BodySampleAgeSeconds { get; }
        public Vector3 VisibleBodyPosition { get; }
        public Quaternion VisibleBodyRotation { get; }
        public Vector3 VisibleBodyVelocity { get; }
        public float VisibleBodyYawVelocityDegreesPerSecond { get; }
        public Vector3 TargetBodyPosition { get; }
        public Quaternion TargetBodyRotation { get; }
        public Vector3 TargetBodyVelocity { get; }
        public float TargetBodyYawVelocityDegreesPerSecond { get; }
        public float BodyPositionError { get; }
        public float BodyRotationError { get; }
        public Vector3 CorrectionPositionError { get; }
        public Vector3 CorrectionPositionVelocity { get; }
        public float CorrectionYawVelocityDegreesPerSecond { get; }
        public bool CorrectionActive { get; }
        public bool CorrectionClamped { get; }
        public bool CorrectionSettled { get; }
        public ulong BodyResetSequence { get; }
        public bool MotionTimelineAvailable { get; }
        public ulong TimelineGeneration { get; }
        public ulong TimelineAuthorityTick { get; }
        public int TimelineTickRate { get; }
        public float TimelineCurrentVelocityX { get; }
        public float TimelineCurrentVelocityZ { get; }
        public float TimelineContinuationVelocityX { get; }
        public float TimelineContinuationVelocityZ { get; }
        public bool TimelineHasContinuation { get; }
        public float TimelineBodyYawVelocityDegreesPerSecond { get; }
        public float TimelineMaximumBodyYawVelocityDegreesPerSecond { get; }
        public float CurrentSegmentRemainingSeconds { get; }
    }

    public readonly struct CharacterFootLandingPredictionDiagnostics
    {
        internal CharacterFootLandingPredictionDiagnostics(
            ulong frameSequence,
            ulong completionIdentity,
            int rootInstanceId,
            CharacterFootLandingPredictionInputDiagnostics input,
            CharacterFullBodyIkGoal pelvisGoal,
            CharacterFootLandingPredictionFootDiagnostics left,
            CharacterFootLandingPredictionFootDiagnostics right)
        {
            FrameSequence = frameSequence;
            CompletionIdentity = completionIdentity;
            RootInstanceId = rootInstanceId;
            Input = input;
            PelvisGoal = pelvisGoal;
            Left = left;
            Right = right;
        }

        public ulong FrameSequence { get; }
        public ulong CompletionIdentity { get; }
        public int RootInstanceId { get; }
        public CharacterFootLandingPredictionInputDiagnostics Input { get; }
        public CharacterFullBodyIkGoal PelvisGoal { get; }
        public CharacterFootLandingPredictionFootDiagnostics Left { get; }
        public CharacterFootLandingPredictionFootDiagnostics Right { get; }
        public bool IsCompleted =>
            FrameSequence != 0 && CompletionIdentity != 0 && RootInstanceId != 0 &&
            PelvisGoal.IsValid && Left.Goal.IsValid && Right.Goal.IsValid;
    }

    internal delegate void CharacterFootLandingPredictionPublishedHandler(
        in CharacterFootLandingPredictionDiagnostics diagnostics);

    internal static class CharacterFootLandingPredictionDebugRegistry
    {
        static readonly Dictionary<int, CharacterFootLandingPredictionDiagnostics> s_ByRoot =
            new Dictionary<int, CharacterFootLandingPredictionDiagnostics>();

        internal static event CharacterFootLandingPredictionPublishedHandler Published;

        internal static void Publish(in CharacterFootLandingPredictionDiagnostics diagnostics)
        {
            if (!diagnostics.IsCompleted)
                return;
            s_ByRoot[diagnostics.RootInstanceId] = diagnostics;
            CharacterFootLandingPredictionPublishedHandler published = Published;
            try
            {
                published?.Invoke(in diagnostics);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        internal static bool TryGet(
            int rootInstanceId,
            out CharacterFootLandingPredictionDiagnostics diagnostics) =>
            s_ByRoot.TryGetValue(rootInstanceId, out diagnostics);

        internal static void Remove(int rootInstanceId) => s_ByRoot.Remove(rootInstanceId);
    }

    internal static class CharacterFootLandingPredictor
    {
        internal static Vector3 ProjectRawLanding(
            Vector3 rootPosition,
            Quaternion rootRotation,
            in ThirdPersonSimulation.CharacterFutureBodyTranslationSample bodyTranslation,
            Vector3 rootLocalLanding)
        {
            if (!Finite(rootPosition) || !Finite(rootLocalLanding))
            {
                throw new ArgumentException("Foot Landing projection input is invalid.");
            }
            Vector3 futureRootPosition = rootPosition + new Vector3(
                bodyTranslation.RelativePositionX,
                bodyTranslation.RelativePositionY,
                bodyTranslation.RelativePositionZ);
            return futureRootPosition + rootRotation * rootLocalLanding;
        }

        internal static CharacterFootPlacementQueryRequest BuildQuery(
            CharacterFootSide side,
            Vector3 rawLandingCandidate,
            Vector3 componentUp,
            in CharacterFootLandingPredictionSettings settings)
        {
            Vector3 up = componentUp.normalized;
            return new CharacterFootPlacementQueryRequest(
                CharacterFootPlacementQueryShape.Sphere,
                CharacterFootPlacementQueryPurpose.FutureLanding,
                side == CharacterFootSide.Left ? 0 : 1,
                rawLandingCandidate + up * settings.CastAbove,
                -up,
                settings.CastAbove + settings.CastBelow,
                settings.SphereRadius,
                settings.GroundLayerMask,
                settings.MinimumGroundNormalDot);
        }

        internal static bool TryResolve(
            CharacterFootSide side,
            Vector3 rawLandingCandidate,
            Vector3 componentUp,
            in CharacterFootLandingPredictionSettings settings,
            ICharacterFootLandingWorldQuery world,
            out CharacterFootPlacementQueryRequest query,
            out CharacterFootLandingSupport support)
        {
            query = BuildQuery(
                side,
                rawLandingCandidate,
                componentUp,
                in settings);
            return world.TryQuery(in query, out support);
        }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
