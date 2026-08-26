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
        GroundQueryMissed = 7,
        GroundQueryCapacityExceeded = 8
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

    internal enum CharacterFootLandingQueryRejectReason : byte
    {
        None = 0,
        InvalidRequest = 1,
        NoHit = 2,
        CapacityExceeded = 3
    }

    internal readonly struct CharacterFootLandingQueryResult
    {
        internal CharacterFootLandingQueryResult(
            CharacterFootLandingQueryRejectReason rejectReason,
            CharacterFootLandingSupport support)
        {
            RejectReason = rejectReason;
            Support = support;
        }

        internal CharacterFootLandingQueryRejectReason RejectReason { get; }
        internal CharacterFootLandingSupport Support { get; }
        internal bool Accepted => RejectReason == CharacterFootLandingQueryRejectReason.None;
    }

    internal interface ICharacterFootLandingWorldQuery
    {
        CharacterFootLandingQueryResult Query(
            in CharacterFootPlacementQueryRequest request);
    }

    internal readonly struct CharacterFootLandingPredictionResult
    {
        internal CharacterFootLandingPredictionResult(
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
            string futureBodyTranslationSourceIdentity,
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
            FutureBodyTranslationSourceIdentity = futureBodyTranslationSourceIdentity ?? string.Empty;
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
            GroundPath = default;
            FootMotion = default;
        }

        CharacterFootLandingPredictionResult(
            in CharacterFootLandingPredictionResult source,
            in CharacterFootGroundPathResult groundPath)
        {
            Side = source.Side;
            State = source.State;
            RejectReason = source.RejectReason;
            StepSource = source.StepSource;
            LandingEventIdentity = source.LandingEventIdentity;
            TrajectoryGeneration = source.TrajectoryGeneration;
            LandingConfidence = source.LandingConfidence;
            TimeToLandingSeconds = source.TimeToLandingSeconds;
            RootLocalLanding = source.RootLocalLanding;
            FutureBodyTranslationAvailable = source.FutureBodyTranslationAvailable;
            FutureBodyTranslationSourceIdentity = source.FutureBodyTranslationSourceIdentity;
            FutureBodyRelativeTranslation = source.FutureBodyRelativeTranslation;
            FutureBodyTranslationVelocity = source.FutureBodyTranslationVelocity;
            CurrentAnimatedSole = source.CurrentAnimatedSole;
            RawLandingCandidate = source.RawLandingCandidate;
            Query = source.Query;
            SurfaceIdentity = source.SurfaceIdentity;
            LandingPoint = source.LandingPoint;
            LandingNormal = source.LandingNormal;
            QueryDistance = source.QueryDistance;
            Goal = source.Goal;
            GroundPath = groundPath;
            FootMotion = source.FootMotion;
        }

        CharacterFootLandingPredictionResult(
            in CharacterFootLandingPredictionResult source,
            CharacterFootLandingStepSource stepSource,
            AnimationBiomechanicalStepHeader step,
            Vector3 currentAnimatedSole,
            CharacterFullBodyIkGoal goal)
        {
            Side = source.Side;
            State = source.State;
            RejectReason = source.RejectReason;
            StepSource = stepSource;
            LandingEventIdentity = source.LandingEventIdentity;
            TrajectoryGeneration = source.TrajectoryGeneration;
            LandingConfidence = step.Confidence;
            TimeToLandingSeconds = step.TimeToLandingSeconds;
            RootLocalLanding = step.RootLocalLanding;
            FutureBodyTranslationAvailable = source.FutureBodyTranslationAvailable;
            FutureBodyTranslationSourceIdentity = source.FutureBodyTranslationSourceIdentity;
            FutureBodyRelativeTranslation = source.FutureBodyRelativeTranslation;
            FutureBodyTranslationVelocity = source.FutureBodyTranslationVelocity;
            CurrentAnimatedSole = currentAnimatedSole;
            RawLandingCandidate = source.RawLandingCandidate;
            Query = source.Query;
            SurfaceIdentity = source.SurfaceIdentity;
            LandingPoint = source.LandingPoint;
            LandingNormal = source.LandingNormal;
            QueryDistance = source.QueryDistance;
            Goal = goal;
            GroundPath = source.GroundPath;
            FootMotion = source.FootMotion;
        }

        CharacterFootLandingPredictionResult(
            in CharacterFootLandingPredictionResult source,
            in CharacterFootSwingMotionResult footMotion,
            CharacterFullBodyIkGoal goal)
        {
            Side = source.Side;
            State = source.State;
            RejectReason = source.RejectReason;
            StepSource = source.StepSource;
            LandingEventIdentity = source.LandingEventIdentity;
            TrajectoryGeneration = source.TrajectoryGeneration;
            LandingConfidence = source.LandingConfidence;
            TimeToLandingSeconds = source.TimeToLandingSeconds;
            RootLocalLanding = source.RootLocalLanding;
            FutureBodyTranslationAvailable = source.FutureBodyTranslationAvailable;
            FutureBodyTranslationSourceIdentity = source.FutureBodyTranslationSourceIdentity;
            FutureBodyRelativeTranslation = source.FutureBodyRelativeTranslation;
            FutureBodyTranslationVelocity = source.FutureBodyTranslationVelocity;
            CurrentAnimatedSole = source.CurrentAnimatedSole;
            RawLandingCandidate = source.RawLandingCandidate;
            Query = source.Query;
            SurfaceIdentity = source.SurfaceIdentity;
            LandingPoint = source.LandingPoint;
            LandingNormal = source.LandingNormal;
            QueryDistance = source.QueryDistance;
            Goal = goal;
            GroundPath = source.GroundPath;
            FootMotion = footMotion;
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
        public string FutureBodyTranslationSourceIdentity { get; }
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
        internal CharacterFootGroundPathResult GroundPath { get; }
        internal CharacterFootSwingMotionResult FootMotion { get; }
        public bool Accepted => State == CharacterFootLandingPredictionState.Accepted;

        internal CharacterFootLandingPredictionResult WithLiveStep(
            CharacterFootLandingStepSource stepSource,
            AnimationBiomechanicalStepHeader step,
            Vector3 currentAnimatedSole,
            CharacterFullBodyIkGoal goal) =>
            new CharacterFootLandingPredictionResult(
                in this,
                stepSource,
                step,
                currentAnimatedSole,
                goal);
        internal CharacterFootLandingPredictionResult WithGroundPath(
            in CharacterFootGroundPathResult groundPath) =>
            new CharacterFootLandingPredictionResult(in this, in groundPath);

        internal CharacterFootLandingPredictionResult WithFootMotion(
            in CharacterFootSwingMotionResult footMotion,
            CharacterFullBodyIkGoal goal) =>
            new CharacterFootLandingPredictionResult(
                in this,
                in footMotion,
                goal);
    }

    public readonly struct CharacterFootLandingPredictionFootDiagnostics
    {
        internal CharacterFootLandingPredictionFootDiagnostics(
            in CharacterFootLandingPredictionResult result,
            CharacterFootPlacementAnimatedFootPose sourcePose)
        {
            Side = result.Side;
            State = result.State;
            RejectReason = result.RejectReason;
            StepSource = result.StepSource;
            LandingEventIdentity = result.LandingEventIdentity;
            TrajectoryGeneration = result.TrajectoryGeneration;
            LandingConfidence = result.LandingConfidence;
            TimeToLandingSeconds = result.TimeToLandingSeconds;
            RootLocalLanding = result.RootLocalLanding;
            FutureBodyTranslationAvailable = result.FutureBodyTranslationAvailable;
            FutureBodyTranslationSourceIdentity = result.FutureBodyTranslationSourceIdentity;
            FutureBodyRelativeTranslation = result.FutureBodyRelativeTranslation;
            FutureBodyTranslationVelocity = result.FutureBodyTranslationVelocity;
            CurrentAnimatedSole = result.CurrentAnimatedSole;
            RawLandingCandidate = result.RawLandingCandidate;
            Query = result.Query;
            SurfaceIdentity = result.SurfaceIdentity;
            LandingPoint = result.LandingPoint;
            LandingNormal = result.LandingNormal;
            QueryDistance = result.QueryDistance;
            Goal = result.Goal;
            SourceAnklePosition = sourcePose.AnklePosition;
            SourceAnkleRotation = sourcePose.AnkleRotation;
            SourceHeelPosition = sourcePose.HeelPosition;
            SourceToePosition = sourcePose.ToePosition;
            CharacterFootGroundPathResult groundPath = result.GroundPath;
            CharacterFootSwingMotionResult footMotion = result.FootMotion;
            GroundPath = new CharacterFootGroundPathDiagnostics(in groundPath);
            FootMotion = new CharacterFootSwingMotionDiagnostics(in footMotion);
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
        public string FutureBodyTranslationSourceIdentity { get; }
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
        public Vector3 SourceAnklePosition { get; }
        public Quaternion SourceAnkleRotation { get; }
        public Vector3 SourceHeelPosition { get; }
        public Vector3 SourceToePosition { get; }
        public CharacterFootGroundPathDiagnostics GroundPath { get; }
        public CharacterFootSwingMotionDiagnostics FootMotion { get; }
        public bool Accepted => State == CharacterFootLandingPredictionState.Accepted;
    }

    public readonly struct CharacterFootStepObservationInputDiagnostics
    {
        internal CharacterFootStepObservationInputDiagnostics(
            in AnimationFootStepObservationFrame frame)
        {
            if (!frame.IsValid)
                throw new ArgumentException("Foot Step observation input diagnostics is invalid.");
            CompletionIdentity = frame.CompletionIdentity;
            SourceId = frame.SourceId.ToString();
            SourceIdentity = frame.SourceIdentity;
            ContributionContinuityIdentity = frame.ContributionContinuityIdentity;
            ClipBindingIndex = frame.ClipBindingIndex;
            Cycle = frame.Cycle;
            SourceWeight = frame.SourceWeight;
            NormalizedTime = frame.NormalizedTime;
            Left = frame.Left;
            Right = frame.Right;
            m_IsSpecified = 1;
        }

        readonly byte m_IsSpecified;
        public ulong CompletionIdentity { get; }
        public string SourceId { get; }
        public string SourceIdentity { get; }
        public ulong ContributionContinuityIdentity { get; }
        public int ClipBindingIndex { get; }
        public int Cycle { get; }
        public float SourceWeight { get; }
        public float NormalizedTime { get; }
        public AnimationFootStepObservationSample Left { get; }
        public AnimationFootStepObservationSample Right { get; }
        public bool IsValid => m_IsSpecified != 0;
    }

    public readonly struct CharacterFootLandingPredictionInputDiagnostics
    {
        internal CharacterFootLandingPredictionInputDiagnostics(
            float presentationDeltaSeconds,
            CharacterBodyPresentationFrame body,
            bool grounded,
            float horizontalSpeed,
            in CharacterFootActionOccupancy leftAction,
            in CharacterFootActionOccupancy rightAction,
            in ThirdPersonSimulation.CommittedLocomotionPlanarMotionTimeline timeline,
            float currentSegmentRemainingSeconds,
            in AnimationFootStepObservationFrame footStepObservation)
        {
            PresentationDeltaSeconds = presentationDeltaSeconds;
            Grounded = grounded;
            HorizontalSpeed = horizontalSpeed;
            LeftActionInstanceIdentity = leftAction.ActionInstanceIdentity;
            LeftActionFootWeight = leftAction.Weight;
            RightActionInstanceIdentity = rightAction.ActionInstanceIdentity;
            RightActionFootWeight = rightAction.Weight;
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
            FootStepObservation =
                new CharacterFootStepObservationInputDiagnostics(in footStepObservation);
        }

        public float PresentationDeltaSeconds { get; }
        public bool Grounded { get; }
        public float HorizontalSpeed { get; }
        public ulong LeftActionInstanceIdentity { get; }
        public float LeftActionFootWeight { get; }
        public ulong RightActionInstanceIdentity { get; }
        public float RightActionFootWeight { get; }
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
        public CharacterFootStepObservationInputDiagnostics FootStepObservation { get; }
    }

    public readonly struct CharacterFootLandingPredictionDiagnostics
    {
        internal CharacterFootLandingPredictionDiagnostics(
            ulong frameSequence,
            ulong completionIdentity,
            int rootInstanceId,
            CharacterFootLandingPredictionInputDiagnostics input,
            in CharacterFootPrimarySupportDiagnostics primarySupport,
            CharacterFullBodyIkGoal pelvisGoal,
            in CharacterFootStrideHipsDiagnostics strideHips,
            CharacterFootLandingPredictionFootDiagnostics left,
            CharacterFootLandingPredictionFootDiagnostics right)
        {
            FrameSequence = frameSequence;
            CompletionIdentity = completionIdentity;
            RootInstanceId = rootInstanceId;
            Input = input;
            PrimarySupport = primarySupport;
            PelvisGoal = pelvisGoal;
            StrideHips = strideHips;
            Left = left;
            Right = right;
        }

        public ulong FrameSequence { get; }
        public ulong CompletionIdentity { get; }
        public int RootInstanceId { get; }
        public CharacterFootLandingPredictionInputDiagnostics Input { get; }
        public CharacterFootPrimarySupportDiagnostics PrimarySupport { get; }
        public CharacterFullBodyIkGoal PelvisGoal { get; }
        public CharacterFootStrideHipsDiagnostics StrideHips { get; }
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
            int preferredSurfaceIdentity,
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
                settings.MinimumGroundNormalDot,
                preferredSurfaceIdentity);
        }

        internal static bool TryResolve(
            CharacterFootSide side,
            Vector3 rawLandingCandidate,
            Vector3 componentUp,
            int preferredSurfaceIdentity,
            in CharacterFootLandingPredictionSettings settings,
            ICharacterFootLandingWorldQuery world,
            out CharacterFootPlacementQueryRequest query,
            out CharacterFootLandingSupport support,
            out CharacterFootLandingQueryRejectReason queryRejectReason)
        {
            query = BuildQuery(
                side,
                rawLandingCandidate,
                componentUp,
                preferredSurfaceIdentity,
                in settings);
            CharacterFootLandingQueryResult result = world.Query(in query);
            support = result.Support;
            queryRejectReason = result.RejectReason;
            return result.Accepted;
        }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
