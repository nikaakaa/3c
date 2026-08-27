using System;
using ThirdPersonCharacter.Pipeline.Animation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal enum CharacterFootLandingTrackingState : byte
    {
        Empty = 0,
        Tracking = 1,
        Accepted = 2
    }

    [Flags]
    internal enum CharacterFootPathRevisionReason : byte
    {
        None = 0,
        PathAvailabilityChanged = 1,
        LandingEventChanged = 2,
        LandingPointChanged = 4,
        SwingTargetChanged = 8,
        ContinuousTargetChanged = 16
    }

    public enum CharacterFootSafetyFloorOwner : byte
    {
        None = 0,
        GroundPathEnvelope = 1,
        CurrentGroundFloor = 2,
        ContactAnchor = 3
    }

    internal enum CharacterFootTransitionPhase : byte
    {
        None = 0,
        PreInterpolation = 1,
        PostInterpolation = 2
    }

    internal enum CharacterFootTransitionReason : byte
    {
        None = 0,
        OwnershipLost = 1,
        SwingStarted = 2,
        PlantCycleConsumed = 3,
        ContactUnavailable = 4,
        ContactOutOfLockRange = 5,
        ContactAcquired = 6,
        ContactReleased = 7,
        ContactOutOfSlideRange = 8,
        LockResponseChanged = 9,
        LandingCompleted = 10,
        ReleaseCompleted = 11
    }

    internal enum CharacterFootAnchorCommand : byte
    {
        None = 0,
        Create = 1,
        Retain = 2,
        Release = 3
    }

    internal enum CharacterFootInterpolationPolicy : byte
    {
        Suppressed = 0,
        SwingResidual = 1,
        AcquireByWeight = 2,
        Direct = 3,
        HalfLife = 4,
        ReleaseResidual = 5
    }

    internal readonly struct CharacterFootPathContinuityFact
    {
        internal CharacterFootPathContinuityFact(
            bool evaluated,
            CharacterFootPathRevisionReason revisionReason,
            bool residualRebuilt,
            bool pathAvailableBefore,
            bool pathAvailableAfter,
            ulong previousLandingEventIdentity,
            ulong currentLandingEventIdentity,
            Vector3 previousTargetCorrection,
            Vector3 currentTargetCorrection,
            float landingPointDelta,
            float targetDelta,
            Vector3 residualBeforeRevision,
            Vector3 residualBeforeDecay,
            Vector3 residualAfterDecay,
            float landingUpdateDistance,
            float timeToLandingSeconds,
            float baseHalfLifeSeconds,
            bool deadlineHalfLifeAvailable,
            float deadlineHalfLifeSeconds,
            float appliedHalfLifeSeconds)
        {
            Evaluated = evaluated;
            RevisionReason = revisionReason;
            ResidualRebuilt = residualRebuilt;
            PathAvailableBefore = pathAvailableBefore;
            PathAvailableAfter = pathAvailableAfter;
            PreviousLandingEventIdentity = previousLandingEventIdentity;
            CurrentLandingEventIdentity = currentLandingEventIdentity;
            PreviousTargetCorrection = previousTargetCorrection;
            CurrentTargetCorrection = currentTargetCorrection;
            LandingPointDelta = landingPointDelta;
            TargetDelta = targetDelta;
            ResidualBeforeRevision = residualBeforeRevision;
            ResidualBeforeDecay = residualBeforeDecay;
            ResidualAfterDecay = residualAfterDecay;
            ResidualOutputCorrection = currentTargetCorrection + residualAfterDecay;
            LandingUpdateDistance = landingUpdateDistance;
            TimeToLandingSeconds = timeToLandingSeconds;
            BaseHalfLifeSeconds = baseHalfLifeSeconds;
            DeadlineHalfLifeAvailable = deadlineHalfLifeAvailable;
            DeadlineHalfLifeSeconds = deadlineHalfLifeSeconds;
            AppliedHalfLifeSeconds = appliedHalfLifeSeconds;
            PreTransitionReason = CharacterFootTransitionReason.None;
            PreTransitionSource = default;
            PreTransitionTarget = default;
            PreTransitionAnchorCommand = CharacterFootAnchorCommand.None;
            PostTransitionReason = CharacterFootTransitionReason.None;
            PostTransitionSource = default;
            PostTransitionTarget = default;
            PostTransitionAnchorCommand = CharacterFootAnchorCommand.None;
            StateTargetCorrection = default;
            InterpolationPolicy = CharacterFootInterpolationPolicy.Suppressed;
            InterpolationOutputCorrection = default;
            InterpolationCompleted = false;
            StateBefore = default;
            StateAfter = default;
            LockResponseBefore = default;
            LockResponseAfter = default;
            OutputStagesAvailable = false;
            ReleasingCompletedToSwing = false;
            SafetyFloorAvailable = false;
            SafetyFloorOwner = CharacterFootSafetyFloorOwner.None;
            SafetyFloorOwnerSurfaceIdentity = 0;
            SafetyFloorOwnerPathIdentity = 0;
            CorrectionBeforeSafetyFloor = default;
            SafetyFloorMinimumCorrection = default;
            SafetyFloorOutputCorrection = default;
            FinalEffectiveCorrection = default;
            SafetyFloorClamped = false;
            SafetyFloorClampMeters = 0f;
            SafetyFloorClearanceBeforeMeters = 0f;
            SafetyFloorClearanceAfterMeters = 0f;
        }

        CharacterFootPathContinuityFact(
            in CharacterFootPathContinuityFact source,
            in CharacterFootTransitionDecision preTransition,
            in CharacterFootTransitionDecision postTransition,
            in CharacterFootStateTarget stateTarget,
            in CharacterFootInterpolationResult interpolation,
            CharacterFootConstraintState stateBefore,
            CharacterFootConstraintState stateAfter,
            CharacterFootLockResponse lockResponseBefore,
            CharacterFootLockResponse lockResponseAfter,
            bool safetyFloorAvailable,
            CharacterFootSafetyFloorOwner safetyFloorOwner,
            int safetyFloorOwnerSurfaceIdentity,
            ulong safetyFloorOwnerPathIdentity,
            Vector3 correctionBeforeSafetyFloor,
            Vector3 safetyFloorMinimumCorrection,
            Vector3 safetyFloorOutputCorrection,
            Vector3 finalEffectiveCorrection,
            bool safetyFloorClamped,
            float safetyFloorClampMeters,
            float safetyFloorClearanceBeforeMeters,
            float safetyFloorClearanceAfterMeters)
        {
            Evaluated = source.Evaluated;
            RevisionReason = source.RevisionReason;
            ResidualRebuilt = source.ResidualRebuilt;
            PathAvailableBefore = source.PathAvailableBefore;
            PathAvailableAfter = source.PathAvailableAfter;
            PreviousLandingEventIdentity = source.PreviousLandingEventIdentity;
            CurrentLandingEventIdentity = source.CurrentLandingEventIdentity;
            PreviousTargetCorrection = source.PreviousTargetCorrection;
            CurrentTargetCorrection = source.CurrentTargetCorrection;
            LandingPointDelta = source.LandingPointDelta;
            TargetDelta = source.TargetDelta;
            ResidualBeforeRevision = source.ResidualBeforeRevision;
            ResidualBeforeDecay = source.ResidualBeforeDecay;
            ResidualAfterDecay = source.ResidualAfterDecay;
            ResidualOutputCorrection = source.ResidualOutputCorrection;
            LandingUpdateDistance = source.LandingUpdateDistance;
            TimeToLandingSeconds = source.TimeToLandingSeconds;
            BaseHalfLifeSeconds = source.BaseHalfLifeSeconds;
            DeadlineHalfLifeAvailable = source.DeadlineHalfLifeAvailable;
            DeadlineHalfLifeSeconds = source.DeadlineHalfLifeSeconds;
            AppliedHalfLifeSeconds = source.AppliedHalfLifeSeconds;
            PreTransitionReason = preTransition.Reason;
            PreTransitionSource = preTransition.SourceState;
            PreTransitionTarget = preTransition.TargetState;
            PreTransitionAnchorCommand = preTransition.AnchorCommand;
            PostTransitionReason = postTransition.Reason;
            PostTransitionSource = postTransition.SourceState;
            PostTransitionTarget = postTransition.TargetState;
            PostTransitionAnchorCommand = postTransition.AnchorCommand;
            StateTargetCorrection = stateTarget.Correction;
            InterpolationPolicy = stateTarget.InterpolationPolicy;
            InterpolationOutputCorrection = interpolation.Correction;
            InterpolationCompleted = interpolation.Completed;
            StateBefore = stateBefore;
            StateAfter = stateAfter;
            LockResponseBefore = lockResponseBefore;
            LockResponseAfter = lockResponseAfter;
            OutputStagesAvailable = true;
            ReleasingCompletedToSwing =
                stateBefore == CharacterFootConstraintState.Releasing &&
                stateAfter == CharacterFootConstraintState.Swing;
            SafetyFloorAvailable = safetyFloorAvailable;
            SafetyFloorOwner = safetyFloorOwner;
            SafetyFloorOwnerSurfaceIdentity = safetyFloorOwnerSurfaceIdentity;
            SafetyFloorOwnerPathIdentity = safetyFloorOwnerPathIdentity;
            CorrectionBeforeSafetyFloor = correctionBeforeSafetyFloor;
            SafetyFloorMinimumCorrection = safetyFloorMinimumCorrection;
            SafetyFloorOutputCorrection = safetyFloorOutputCorrection;
            FinalEffectiveCorrection = finalEffectiveCorrection;
            SafetyFloorClamped = safetyFloorClamped;
            SafetyFloorClampMeters = safetyFloorClampMeters;
            SafetyFloorClearanceBeforeMeters = safetyFloorClearanceBeforeMeters;
            SafetyFloorClearanceAfterMeters = safetyFloorClearanceAfterMeters;
        }

        internal bool Evaluated { get; }
        internal CharacterFootPathRevisionReason RevisionReason { get; }
        internal bool ResidualRebuilt { get; }
        internal bool PathAvailableBefore { get; }
        internal bool PathAvailableAfter { get; }
        internal ulong PreviousLandingEventIdentity { get; }
        internal ulong CurrentLandingEventIdentity { get; }
        internal Vector3 PreviousTargetCorrection { get; }
        internal Vector3 CurrentTargetCorrection { get; }
        internal float LandingPointDelta { get; }
        internal float TargetDelta { get; }
        internal Vector3 ResidualBeforeRevision { get; }
        internal Vector3 ResidualBeforeDecay { get; }
        internal Vector3 ResidualAfterDecay { get; }
        internal Vector3 ResidualOutputCorrection { get; }
        internal float LandingUpdateDistance { get; }
        internal float TimeToLandingSeconds { get; }
        internal float BaseHalfLifeSeconds { get; }
        internal bool DeadlineHalfLifeAvailable { get; }
        internal float DeadlineHalfLifeSeconds { get; }
        internal float AppliedHalfLifeSeconds { get; }
        internal CharacterFootTransitionReason PreTransitionReason { get; }
        internal CharacterFootConstraintState PreTransitionSource { get; }
        internal CharacterFootConstraintState PreTransitionTarget { get; }
        internal CharacterFootAnchorCommand PreTransitionAnchorCommand { get; }
        internal CharacterFootTransitionReason PostTransitionReason { get; }
        internal CharacterFootConstraintState PostTransitionSource { get; }
        internal CharacterFootConstraintState PostTransitionTarget { get; }
        internal CharacterFootAnchorCommand PostTransitionAnchorCommand { get; }
        internal Vector3 StateTargetCorrection { get; }
        internal CharacterFootInterpolationPolicy InterpolationPolicy { get; }
        internal Vector3 InterpolationOutputCorrection { get; }
        internal bool InterpolationCompleted { get; }
        internal CharacterFootConstraintState StateBefore { get; }
        internal CharacterFootConstraintState StateAfter { get; }
        internal CharacterFootLockResponse LockResponseBefore { get; }
        internal CharacterFootLockResponse LockResponseAfter { get; }
        internal bool OutputStagesAvailable { get; }
        internal bool ReleasingCompletedToSwing { get; }
        internal bool SafetyFloorAvailable { get; }
        internal CharacterFootSafetyFloorOwner SafetyFloorOwner { get; }
        internal int SafetyFloorOwnerSurfaceIdentity { get; }
        internal ulong SafetyFloorOwnerPathIdentity { get; }
        internal Vector3 CorrectionBeforeSafetyFloor { get; }
        internal Vector3 SafetyFloorMinimumCorrection { get; }
        internal Vector3 SafetyFloorOutputCorrection { get; }
        internal Vector3 FinalEffectiveCorrection { get; }
        internal bool SafetyFloorClamped { get; }
        internal float SafetyFloorClampMeters { get; }
        internal float SafetyFloorClearanceBeforeMeters { get; }
        internal float SafetyFloorClearanceAfterMeters { get; }

        internal CharacterFootPathContinuityFact Complete(
            in CharacterFootTransitionDecision preTransition,
            in CharacterFootTransitionDecision postTransition,
            in CharacterFootStateTarget stateTarget,
            in CharacterFootInterpolationResult interpolation,
            CharacterFootConstraintState stateBefore,
            CharacterFootConstraintState stateAfter,
            CharacterFootLockResponse lockResponseBefore,
            CharacterFootLockResponse lockResponseAfter,
            bool safetyFloorAvailable,
            CharacterFootSafetyFloorOwner safetyFloorOwner,
            int safetyFloorOwnerSurfaceIdentity,
            ulong safetyFloorOwnerPathIdentity,
            Vector3 correctionBeforeSafetyFloor,
            Vector3 safetyFloorMinimumCorrection,
            Vector3 safetyFloorOutputCorrection,
            Vector3 finalEffectiveCorrection,
            bool safetyFloorClamped,
            float safetyFloorClampMeters,
            float safetyFloorClearanceBeforeMeters,
            float safetyFloorClearanceAfterMeters) =>
            new CharacterFootPathContinuityFact(
                in this,
                in preTransition,
                in postTransition,
                in stateTarget,
                in interpolation,
                stateBefore,
                stateAfter,
                lockResponseBefore,
                lockResponseAfter,
                safetyFloorAvailable,
                safetyFloorOwner,
                safetyFloorOwnerSurfaceIdentity,
                safetyFloorOwnerPathIdentity,
                correctionBeforeSafetyFloor,
                safetyFloorMinimumCorrection,
                safetyFloorOutputCorrection,
                finalEffectiveCorrection,
                safetyFloorClamped,
                safetyFloorClampMeters,
                safetyFloorClearanceBeforeMeters,
                safetyFloorClearanceAfterMeters);

        internal static CharacterFootPathContinuityFact CreateUnevaluated(
            float timeToLandingSeconds,
            CharacterFootMotionSettings settings) =>
            new CharacterFootPathContinuityFact(
                false,
                CharacterFootPathRevisionReason.None,
                false,
                false,
                false,
                0,
                0,
                default,
                default,
                0f,
                0f,
                default,
                default,
                default,
                settings.LandingUpdateDistance,
                timeToLandingSeconds,
                settings.EffectiveCorrectionHalfLifeSeconds,
                false,
                0f,
                settings.EffectiveCorrectionHalfLifeSeconds);
    }

    internal readonly struct CharacterFootLandingFact
    {
        CharacterFootLandingFact(
            ulong landingEventIdentity,
            ulong trajectoryGeneration,
            string futureBodyTranslationSourceIdentity,
            int surfaceIdentity,
            Vector3 worldPoint,
            Vector3 worldNormal)
        {
            HasValue = true;
            LandingEventIdentity = landingEventIdentity;
            TrajectoryGeneration = trajectoryGeneration;
            FutureBodyTranslationSourceIdentity = futureBodyTranslationSourceIdentity;
            SurfaceIdentity = surfaceIdentity;
            WorldPoint = worldPoint;
            WorldNormal = worldNormal;
        }

        internal bool HasValue { get; }
        internal ulong LandingEventIdentity { get; }
        internal ulong TrajectoryGeneration { get; }
        internal string FutureBodyTranslationSourceIdentity { get; }
        internal int SurfaceIdentity { get; }
        internal Vector3 WorldPoint { get; }
        internal Vector3 WorldNormal { get; }

        internal CharacterFootGroundPathLanding Resolve() =>
            new CharacterFootGroundPathLanding(
                LandingEventIdentity,
                TrajectoryGeneration,
                FutureBodyTranslationSourceIdentity,
                SurfaceIdentity,
                WorldPoint,
                WorldNormal);

        internal static CharacterFootLandingFact Create(
            in AnimationBiomechanicalStepHeader step,
            in CharacterFootLandingPredictionResult diagnostics) =>
            new CharacterFootLandingFact(
                step.LandingEventIdentity,
                diagnostics.TrajectoryGeneration,
                diagnostics.FutureBodyTranslationSourceIdentity,
                diagnostics.SurfaceIdentity,
                diagnostics.LandingPoint,
                diagnostics.LandingNormal);
    }

    internal readonly struct CharacterFootLandingSnapshot
    {
        internal CharacterFootLandingSnapshot(
            CharacterFootLandingTrackingState state,
            ulong eventIdentity,
            bool hasLastLanding,
            CharacterFootGroundPathLanding lastLanding,
            bool hasNextSwingLanding,
            CharacterFootGroundPathLanding nextSwingLanding,
            float nextSwingPredictionError,
            float nextSwingConstraintWeight,
            bool hasPromotedLanding,
            CharacterFootGroundPathLanding promotedLanding)
        {
            State = state;
            EventIdentity = eventIdentity;
            HasLastLanding = hasLastLanding;
            LastLanding = lastLanding;
            HasNextSwingLanding = hasNextSwingLanding;
            NextSwingLanding = nextSwingLanding;
            NextSwingPredictionError = nextSwingPredictionError;
            NextSwingConstraintWeight = nextSwingConstraintWeight;
            HasPromotedLanding = hasPromotedLanding;
            PromotedLanding = promotedLanding;
        }

        internal CharacterFootLandingTrackingState State { get; }
        internal ulong EventIdentity { get; }
        internal bool HasLastLanding { get; }
        internal CharacterFootGroundPathLanding LastLanding { get; }
        internal ulong LastLandingEventIdentity =>
            HasLastLanding ? LastLanding.LandingEventIdentity : 0;
        internal bool HasNextSwingLanding { get; }
        internal CharacterFootGroundPathLanding NextSwingLanding { get; }
        internal float NextSwingPredictionError { get; }
        internal float NextSwingConstraintWeight { get; }
        internal bool HasPromotedLanding { get; }
        internal CharacterFootGroundPathLanding PromotedLanding { get; }

        internal bool TryResolveLanding(
            ulong landingEventIdentity,
            out CharacterFootGroundPathLanding landing)
        {
            if (landingEventIdentity != 0 &&
                HasNextSwingLanding &&
                NextSwingLanding.LandingEventIdentity == landingEventIdentity)
            {
                landing = NextSwingLanding;
                return true;
            }
            if (landingEventIdentity != 0 &&
                HasLastLanding &&
                LastLanding.LandingEventIdentity == landingEventIdentity)
            {
                landing = LastLanding;
                return true;
            }
            landing = default;
            return false;
        }
    }

    internal struct CharacterFootLandingContext
    {
        internal CharacterFootLandingFact LastLanding;
        internal CharacterFootLandingFact NextSwingLanding;
        internal CharacterFootLandingFact PromotedLanding;
        internal Vector3 NextSwingReferencePoint;
        internal float NextSwingPredictionError;
        internal float NextSwingConstraintWeight;
        internal ulong ObservedCurrentEventIdentity;
        internal ulong TrackedEventIdentity;
        internal CharacterFootLandingTrackingState TrackingState;

        internal CharacterFootLandingSnapshot Snapshot =>
            new CharacterFootLandingSnapshot(
                TrackingState,
                TrackedEventIdentity,
                LastLanding.HasValue,
                LastLanding.HasValue ? LastLanding.Resolve() : default,
                TrackingState == CharacterFootLandingTrackingState.Accepted &&
                NextSwingLanding.HasValue,
                TrackingState == CharacterFootLandingTrackingState.Accepted &&
                NextSwingLanding.HasValue ? NextSwingLanding.Resolve() : default,
                TrackingState == CharacterFootLandingTrackingState.Accepted &&
                NextSwingLanding.HasValue ? NextSwingPredictionError : 0f,
                TrackingState == CharacterFootLandingTrackingState.Accepted &&
                NextSwingLanding.HasValue ? NextSwingConstraintWeight : 0f,
                PromotedLanding.HasValue,
                PromotedLanding.HasValue ? PromotedLanding.Resolve() : default);

        internal void BeginFrame() => PromotedLanding = default;

        internal void InvalidateCurrent()
        {
            NextSwingPredictionError = 0f;
            NextSwingConstraintWeight = 0f;
            TrackingState = TrackedEventIdentity != 0
                ? CharacterFootLandingTrackingState.Tracking
                : CharacterFootLandingTrackingState.Empty;
        }

        internal void ClearNextSwing()
        {
            NextSwingLanding = default;
            NextSwingReferencePoint = default;
            NextSwingPredictionError = 0f;
            NextSwingConstraintWeight = 0f;
            TrackingState = TrackedEventIdentity != 0
                ? CharacterFootLandingTrackingState.Tracking
                : CharacterFootLandingTrackingState.Empty;
        }
    }

    internal struct CharacterFootDiscreteStateContext
    {
        internal CharacterFootConstraintState State;
        internal CharacterFootLockResponse LockResponse;
        internal bool PlantCycleConsumed;
        internal CharacterFootTransitionPhase LastTransitionPhase;
        internal CharacterFootTransitionReason LastTransitionReason;
    }

    internal struct CharacterFootContactContext
    {
        internal bool HasContact;
        internal ulong EventIdentity;
        internal int SurfaceIdentity;
        internal Vector3 Anchor;
        internal Vector3 Normal;

        internal void Clear() => this = default;
    }

    internal struct CharacterFootInterpolationState
    {
        internal bool HasOutput;
        internal bool HasSwingPath;
        internal ulong SwingLandingEventIdentity;
        internal Vector3 SwingLandingPoint;
        internal Vector3 PreviousTargetCorrection;
        internal Vector3 PreviousRawSwingCorrection;
        internal Vector3 EffectiveCorrection;
        internal Vector3 Residual;
        internal float Progress;
        internal float StartResidual;
        internal bool Completed;
        internal CharacterFootInterpolationPolicy Policy;
    }

    internal struct CharacterFootLifecycleContext
    {
        internal CharacterFootLandingContext Landing;
        internal CharacterFootDiscreteStateContext Discrete;
        internal CharacterFootContactContext Contact;
        internal CharacterFootInterpolationState Interpolation;

        internal CharacterFootLandingSnapshot LandingSnapshot => Landing.Snapshot;
    }

    internal readonly struct CharacterFootStateFrame
    {
        internal CharacterFootStateFrame(
            ulong frameSequence,
            ulong completionIdentity,
            FixedString64Bytes rigId,
            FixedString64Bytes rigRevision,
            CharacterFootPlacementAnimatedFootPose animatedFoot,
            in CharacterFootSwingMotionResult swingMotion,
            in CharacterFootCurrentGroundFloorResult currentGroundFloor,
            bool hasContactLanding,
            in CharacterFootGroundPathLanding contactLanding,
            bool hardOwnershipLoss,
            float footPlacementWeight,
            Vector3 componentUp,
            float deltaSeconds,
            in CharacterFootMotionSettings settings)
        {
            FrameSequence = frameSequence;
            CompletionIdentity = completionIdentity;
            RigId = rigId;
            RigRevision = rigRevision;
            AnimatedFoot = animatedFoot;
            SwingMotion = swingMotion;
            CurrentGroundFloor = currentGroundFloor;
            HasContactLanding = hasContactLanding;
            ContactLanding = contactLanding;
            HardOwnershipLoss = hardOwnershipLoss;
            FootPlacementWeight = footPlacementWeight;
            ComponentUp = componentUp;
            DeltaSeconds = deltaSeconds;
            Settings = settings;
        }

        internal ulong FrameSequence { get; }
        internal ulong CompletionIdentity { get; }
        internal FixedString64Bytes RigId { get; }
        internal FixedString64Bytes RigRevision { get; }
        internal CharacterFootPlacementAnimatedFootPose AnimatedFoot { get; }
        internal CharacterFootSwingMotionResult SwingMotion { get; }
        internal CharacterFootCurrentGroundFloorResult CurrentGroundFloor { get; }
        internal bool HasContactLanding { get; }
        internal CharacterFootGroundPathLanding ContactLanding { get; }
        internal bool HardOwnershipLoss { get; }
        internal float FootPlacementWeight { get; }
        internal Vector3 ComponentUp { get; }
        internal float DeltaSeconds { get; }
        internal CharacterFootMotionSettings Settings { get; }
    }

    internal readonly struct CharacterFootStateEvaluation
    {
        internal CharacterFootStateEvaluation(
            CharacterFootSide side,
            in AnimationBiomechanicalStepHeader currentStep,
            in AnimationBiomechanicalStepHeader selectedStep,
            in CharacterFootLandingPredictionResult landingPrediction,
            in CharacterFootStateFrame frame)
        {
            Side = side;
            CurrentStep = currentStep;
            SelectedStep = selectedStep;
            LandingPrediction = landingPrediction;
            Frame = frame;
        }

        internal CharacterFootSide Side { get; }
        internal AnimationBiomechanicalStepHeader CurrentStep { get; }
        internal AnimationBiomechanicalStepHeader SelectedStep { get; }
        internal CharacterFootLandingPredictionResult LandingPrediction { get; }
        internal CharacterFootStateFrame Frame { get; }
    }

    internal readonly struct CharacterFootTransitionDecision
    {
        internal CharacterFootTransitionDecision(
            CharacterFootTransitionPhase phase,
            CharacterFootTransitionReason reason,
            CharacterFootConstraintState sourceState,
            CharacterFootConstraintState targetState,
            CharacterFootLockResponse targetLockResponse,
            bool plantCycleConsumed,
            CharacterFootAnchorCommand anchorCommand,
            bool suppressOutput,
            bool resetInterpolation)
        {
            Phase = phase;
            Reason = reason;
            SourceState = sourceState;
            TargetState = targetState;
            TargetLockResponse = targetLockResponse;
            PlantCycleConsumed = plantCycleConsumed;
            AnchorCommand = anchorCommand;
            SuppressOutput = suppressOutput;
            ResetInterpolation = resetInterpolation;
        }

        internal CharacterFootTransitionPhase Phase { get; }
        internal CharacterFootTransitionReason Reason { get; }
        internal CharacterFootConstraintState SourceState { get; }
        internal CharacterFootConstraintState TargetState { get; }
        internal CharacterFootLockResponse TargetLockResponse { get; }
        internal bool PlantCycleConsumed { get; }
        internal CharacterFootAnchorCommand AnchorCommand { get; }
        internal bool SuppressOutput { get; }
        internal bool ResetInterpolation { get; }
        internal bool StateChanged => SourceState != TargetState;
    }

    internal readonly struct CharacterFootStateTarget
    {
        internal CharacterFootStateTarget(
            Vector3 correction,
            Vector3 swingCorrection,
            Vector3 rawSwingCorrection,
            CharacterFootInterpolationPolicy interpolationPolicy,
            bool stateEntered,
            bool responseEntered,
            bool suppressOutput,
            float progress,
            float timeToLandingSeconds)
        {
            Correction = correction;
            SwingCorrection = swingCorrection;
            RawSwingCorrection = rawSwingCorrection;
            InterpolationPolicy = interpolationPolicy;
            StateEntered = stateEntered;
            ResponseEntered = responseEntered;
            SuppressOutput = suppressOutput;
            Progress = progress;
            TimeToLandingSeconds = timeToLandingSeconds;
        }

        internal Vector3 Correction { get; }
        internal Vector3 SwingCorrection { get; }
        internal Vector3 RawSwingCorrection { get; }
        internal CharacterFootInterpolationPolicy InterpolationPolicy { get; }
        internal bool StateEntered { get; }
        internal bool ResponseEntered { get; }
        internal bool SuppressOutput { get; }
        internal float Progress { get; }
        internal float TimeToLandingSeconds { get; }
    }

    internal readonly struct CharacterFootInterpolationResult
    {
        internal CharacterFootInterpolationResult(
            Vector3 correction,
            bool completed,
            in CharacterFootPathContinuityFact continuityFact)
        {
            Correction = correction;
            Completed = completed;
            ContinuityFact = continuityFact;
        }

        internal Vector3 Correction { get; }
        internal bool Completed { get; }
        internal CharacterFootPathContinuityFact ContinuityFact { get; }
    }
}
