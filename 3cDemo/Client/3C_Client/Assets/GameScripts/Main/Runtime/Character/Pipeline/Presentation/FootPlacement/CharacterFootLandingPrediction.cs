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
        GroundQueryCapacityExceeded = 8,
        FormalStepTimeUnavailable = 9,
        FormalStepTimeAmbiguous = 10
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
            CurrentGroundFloor = default;
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
            CurrentGroundFloor = source.CurrentGroundFloor;
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
            CurrentGroundFloor = source.CurrentGroundFloor;
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
            CurrentGroundFloor = source.CurrentGroundFloor;
            FootMotion = footMotion;
        }

        CharacterFootLandingPredictionResult(
            in CharacterFootLandingPredictionResult source,
            in CharacterFootCurrentGroundFloorResult currentGroundFloor)
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
            GroundPath = source.GroundPath;
            CurrentGroundFloor = currentGroundFloor;
            FootMotion = source.FootMotion;
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
        internal CharacterFootCurrentGroundFloorResult CurrentGroundFloor { get; }
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

        internal CharacterFootLandingPredictionResult WithCurrentGroundFloor(
            in CharacterFootCurrentGroundFloorResult currentGroundFloor) =>
            new CharacterFootLandingPredictionResult(
                in this,
                in currentGroundFloor);

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
            CharacterFootPlacementAnimatedFootPose sourcePose,
            in CharacterFootStepCandidateSelectionDiagnostics stepCandidateSelection)
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
            StepCandidateSelection = stepCandidateSelection;
            CharacterFootGroundPathResult groundPath = result.GroundPath;
            CharacterFootCurrentGroundFloorResult currentGroundFloor =
                result.CurrentGroundFloor;
            CharacterFootSwingMotionResult footMotion = result.FootMotion;
            GroundPath = new CharacterFootGroundPathDiagnostics(in groundPath);
            CurrentGroundFloor =
                new CharacterFootCurrentGroundFloorDiagnostics(
                    in currentGroundFloor);
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
        public CharacterFootStepCandidateSelectionDiagnostics StepCandidateSelection { get; }
        public CharacterFootGroundPathDiagnostics GroundPath { get; }
        public CharacterFootCurrentGroundFloorDiagnostics CurrentGroundFloor { get; }
        public CharacterFootSwingMotionDiagnostics FootMotion { get; }
        public bool RawLandingAvailable =>
            RejectReason == CharacterFootLandingPredictionRejectReason.None ||
            RejectReason ==
            CharacterFootLandingPredictionRejectReason.GroundQueryMissed ||
            RejectReason ==
            CharacterFootLandingPredictionRejectReason.GroundQueryCapacityExceeded;
        public bool Accepted => State == CharacterFootLandingPredictionState.Accepted;
    }

    public readonly struct CharacterFootStepCandidateDiagnostics
    {
        internal CharacterFootStepCandidateDiagnostics(
            in AnimationBiomechanicalStepHeader step)
        {
            IsValid = step.IsValid;
            IsAuthoritative = step.IsAuthoritative;
            HasConsistentLandingEventIdentity =
                step.HasConsistentLandingEventIdentity;
            IsPreSwing = step.IsPreSwing;
            IsSwing = step.IsSwing;
            EventOrdinal = step.EventOrdinal;
            SourceLandingCycleOffset = step.SourceLandingCycleOffset;
            SourceSampleCycle = step.SourceSampleCycle;
            ContributionContinuityIdentity =
                step.ContributionContinuityIdentity;
            LandingEventIdentity = step.LandingEventIdentity;
            TimeToLandingSeconds = step.TimeToLandingSeconds;
            RootLocalLanding = step.RootLocalLanding;
        }

        public bool IsValid { get; }
        public bool IsAuthoritative { get; }
        public bool HasConsistentLandingEventIdentity { get; }
        public bool IsPreSwing { get; }
        public bool IsSwing { get; }
        public int EventOrdinal { get; }
        public int SourceLandingCycleOffset { get; }
        public int SourceSampleCycle { get; }
        public ulong ContributionContinuityIdentity { get; }
        public ulong LandingEventIdentity { get; }
        public float TimeToLandingSeconds { get; }
        public Vector3 RootLocalLanding { get; }
    }

    public readonly struct CharacterFootStepCandidateSelectionDiagnostics
    {
        internal CharacterFootStepCandidateSelectionDiagnostics(
            in AnimationBiomechanicalStepHeader current,
            in AnimationBiomechanicalStepHeader incoming,
            ulong lastLandingEventIdentity,
            CharacterFootLandingStepSource selectedSource,
            ulong selectedLandingEventIdentity,
            float maximumPredictionTimeSeconds)
        {
            Current = new CharacterFootStepCandidateDiagnostics(in current);
            Incoming = new CharacterFootStepCandidateDiagnostics(in incoming);
            LastLandingEventIdentity = lastLandingEventIdentity;
            SelectedSource = selectedSource;
            SelectedLandingEventIdentity = selectedLandingEventIdentity;
            MaximumPredictionTimeSeconds = maximumPredictionTimeSeconds;
        }

        public CharacterFootStepCandidateDiagnostics Current { get; }
        public CharacterFootStepCandidateDiagnostics Incoming { get; }
        public ulong LastLandingEventIdentity { get; }
        public CharacterFootLandingStepSource SelectedSource { get; }
        public ulong SelectedLandingEventIdentity { get; }
        public float MaximumPredictionTimeSeconds { get; }
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
        sealed class Frame
        {
            internal Frame(
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

            internal ulong FrameSequence { get; }
            internal ulong CompletionIdentity { get; }
            internal int RootInstanceId { get; }
            internal CharacterFootLandingPredictionInputDiagnostics Input { get; }
            internal CharacterFootPrimarySupportDiagnostics PrimarySupport { get; }
            internal CharacterFullBodyIkGoal PelvisGoal { get; }
            internal CharacterFootStrideHipsDiagnostics StrideHips { get; }
            internal CharacterFootLandingPredictionFootDiagnostics Left { get; }
            internal CharacterFootLandingPredictionFootDiagnostics Right { get; }
        }

        readonly Frame m_Frame;

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
            m_Frame = new Frame(
                frameSequence,
                completionIdentity,
                rootInstanceId,
                input,
                in primarySupport,
                pelvisGoal,
                in strideHips,
                left,
                right);
        }

        public ulong FrameSequence => m_Frame?.FrameSequence ?? 0;
        public ulong CompletionIdentity => m_Frame?.CompletionIdentity ?? 0;
        public int RootInstanceId => m_Frame?.RootInstanceId ?? 0;
        public CharacterFootLandingPredictionInputDiagnostics Input =>
            m_Frame == null ? default : m_Frame.Input;
        public CharacterFootPrimarySupportDiagnostics PrimarySupport =>
            m_Frame == null ? default : m_Frame.PrimarySupport;
        public CharacterFullBodyIkGoal PelvisGoal =>
            m_Frame == null ? default : m_Frame.PelvisGoal;
        public CharacterFootStrideHipsDiagnostics StrideHips =>
            m_Frame == null ? default : m_Frame.StrideHips;
        public CharacterFootLandingPredictionFootDiagnostics Left =>
            m_Frame == null ? default : m_Frame.Left;
        public CharacterFootLandingPredictionFootDiagnostics Right =>
            m_Frame == null ? default : m_Frame.Right;
        public bool IsCompleted =>
            m_Frame != null &&
            m_Frame.FrameSequence != 0 &&
            m_Frame.CompletionIdentity != 0 &&
            m_Frame.RootInstanceId != 0 &&
            m_Frame.PelvisGoal.IsValid &&
            m_Frame.Left.Goal.IsValid &&
            m_Frame.Right.Goal.IsValid;
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

    public enum CharacterFootCurrentGroundFloorState : byte
    {
        None = 0,
        Rejected = 1,
        Accepted = 2
    }

    public enum CharacterFootCurrentGroundFloorRejectReason : byte
    {
        None = 0,
        SwingUnavailable = 1,
        InvalidRequest = 2,
        NoHit = 3,
        CapacityExceeded = 4
    }

    internal readonly struct CharacterFootCurrentGroundFloorResult
    {
        internal CharacterFootCurrentGroundFloorResult(
            CharacterFootSide side,
            CharacterFootCurrentGroundFloorState state,
            CharacterFootCurrentGroundFloorRejectReason rejectReason,
            in CharacterFootPlacementQueryRequest query,
            in CharacterFootLandingSupport support)
        {
            Side = side;
            State = state;
            RejectReason = rejectReason;
            Query = query;
            SurfaceIdentity = support.SurfaceIdentity;
            Point = support.Point;
            Normal = support.Normal;
            Distance = support.Distance;
        }

        internal CharacterFootSide Side { get; }
        internal CharacterFootCurrentGroundFloorState State { get; }
        internal CharacterFootCurrentGroundFloorRejectReason RejectReason { get; }
        internal CharacterFootPlacementQueryRequest Query { get; }
        internal int SurfaceIdentity { get; }
        internal Vector3 Point { get; }
        internal Vector3 Normal { get; }
        internal float Distance { get; }
        internal bool Accepted => State == CharacterFootCurrentGroundFloorState.Accepted;

        internal static CharacterFootCurrentGroundFloorResult SwingUnavailable(
            CharacterFootSide side) =>
            new CharacterFootCurrentGroundFloorResult(
                side,
                CharacterFootCurrentGroundFloorState.Rejected,
                CharacterFootCurrentGroundFloorRejectReason.SwingUnavailable,
                default,
                default);
    }

    public readonly struct CharacterFootCurrentGroundFloorDiagnostics
    {
        internal CharacterFootCurrentGroundFloorDiagnostics(
            in CharacterFootCurrentGroundFloorResult result)
        {
            Side = result.Side;
            State = result.State;
            RejectReason = result.RejectReason;
            Query = result.Query;
            SurfaceIdentity = result.SurfaceIdentity;
            Point = result.Point;
            Normal = result.Normal;
            Distance = result.Distance;
        }

        public CharacterFootSide Side { get; }
        public CharacterFootCurrentGroundFloorState State { get; }
        public CharacterFootCurrentGroundFloorRejectReason RejectReason { get; }
        public CharacterFootPlacementQueryRequest Query { get; }
        public int SurfaceIdentity { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public float Distance { get; }
        public bool Accepted => State == CharacterFootCurrentGroundFloorState.Accepted;
    }

    internal static class CharacterFootCurrentGroundFloorResolver
    {
        internal static CharacterFootCurrentGroundFloorResult Resolve(
            CharacterFootSide side,
            Vector3 currentAnimatedSole,
            Vector3 componentUp,
            in CharacterFootLandingPredictionSettings settings,
            ICharacterFootLandingWorldQuery world)
        {
            if (world == null)
                throw new ArgumentNullException(nameof(world));
            if (side != CharacterFootSide.Left &&
                side != CharacterFootSide.Right)
                throw new ArgumentOutOfRangeException(nameof(side));
            CharacterFootPlacementQueryRequest query = BuildQuery(
                side,
                currentAnimatedSole,
                componentUp,
                in settings);
            CharacterFootLandingQueryResult result = world.Query(in query);
            CharacterFootCurrentGroundFloorRejectReason rejectReason =
                result.RejectReason switch
                {
                    CharacterFootLandingQueryRejectReason.None =>
                        CharacterFootCurrentGroundFloorRejectReason.None,
                    CharacterFootLandingQueryRejectReason.InvalidRequest =>
                        CharacterFootCurrentGroundFloorRejectReason.InvalidRequest,
                    CharacterFootLandingQueryRejectReason.NoHit =>
                        CharacterFootCurrentGroundFloorRejectReason.NoHit,
                    CharacterFootLandingQueryRejectReason.CapacityExceeded =>
                        CharacterFootCurrentGroundFloorRejectReason.CapacityExceeded,
                    _ => throw new ArgumentOutOfRangeException()
                };
            CharacterFootLandingSupport support = result.Support;
            return new CharacterFootCurrentGroundFloorResult(
                side,
                result.Accepted
                    ? CharacterFootCurrentGroundFloorState.Accepted
                    : CharacterFootCurrentGroundFloorState.Rejected,
                rejectReason,
                in query,
                in support);
        }

        static CharacterFootPlacementQueryRequest BuildQuery(
            CharacterFootSide side,
            Vector3 currentAnimatedSole,
            Vector3 componentUp,
            in CharacterFootLandingPredictionSettings settings)
        {
            Vector3 up = componentUp.normalized;
            return new CharacterFootPlacementQueryRequest(
                CharacterFootPlacementQueryShape.Sphere,
                CharacterFootPlacementQueryPurpose.CurrentSwingFloor,
                side == CharacterFootSide.Left ? 0 : 1,
                currentAnimatedSole + up * settings.CastAbove,
                -up,
                settings.CastAbove + settings.CastBelow,
                settings.SphereRadius,
                settings.GroundLayerMask,
                settings.MinimumGroundNormalDot,
                0);
        }
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
