using System;
using ThirdPersonCharacter.Pipeline.Animation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal enum CharacterFootNextLandingTrackingState : byte
    {
        Empty = 0,
        Tracking = 1
    }

    internal enum CharacterFootPlantTargetState : byte
    {
        Empty = 0,
        Tracking = 1,
        Verified = 2
    }

    [Flags]
    internal enum CharacterFootPathRevisionReason : byte
    {
        None = 0,
        PathAvailabilityChanged = 1,
        LandingEventChanged = 2,
        LandingPointChanged = 4
    }

    public enum CharacterFootSafetyFloorOwner : byte
    {
        None = 0,
        GroundPathEnvelope = 1,
        ContactAnchor = 2,
        PlantTarget = 3
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
        ContactEventUnavailable = 3,
        ContactUnavailable = 4,
        ContactOutOfLockRange = 5,
        ContactAcquired = 6,
        ContactReleased = 7,
        ContactOutOfSlideRange = 8,
        LockResponseChanged = 9,
        LandingCompleted = 10,
        ReleaseCompleted = 11,
        SameEventContactReentryRefresh = 12,
        NewEventContactAcquired = 13
    }

    internal enum CharacterFootContactEdge : byte
    {
        None = 0,
        Rising = 1,
        Falling = 2,
        EventChanged = 3
    }

    internal enum CharacterFootLockRequestAvailability : byte
    {
        Ready = 1,
        ContactEventUnavailable = 2
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
        PlantBlend = 2,
        ReleaseResidual = 3
    }

    internal readonly struct CharacterFootPathContinuityFact
    {
        internal CharacterFootPathContinuityFact(
            bool evaluated,
            CharacterFootPathRevisionReason revisionReason,
            bool residualRebuilt,
            bool targetTrackingApplied,
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
            float landingAcceptanceDistance,
            float pathRevisionDistance,
            float swingResidualTolerance,
            float timeToLandingSeconds,
            float baseHalfLifeSeconds,
            bool deadlineHalfLifeAvailable,
            float deadlineHalfLifeSeconds,
            float appliedHalfLifeSeconds,
            float swingRawTargetHeightAlongUp,
            float swingFilteredTargetHeightBefore,
            float swingTargetHeightDelta,
            float swingTargetHeightAppliedDelta,
            bool swingTargetHeightUpdateHeld,
            bool swingTargetHeightRateLimited,
            bool swingTargetHeightClamped,
            float swingTargetMaximumVerticalSpeed,
            float swingFilteredTargetHeightAlongUp)
        {
            Evaluated = evaluated;
            RevisionReason = revisionReason;
            ResidualRebuilt = residualRebuilt;
            TargetTrackingApplied = targetTrackingApplied;
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
            LandingAcceptanceDistance = landingAcceptanceDistance;
            PathRevisionDistance = pathRevisionDistance;
            SwingResidualTolerance = swingResidualTolerance;
            TimeToLandingSeconds = timeToLandingSeconds;
            BaseHalfLifeSeconds = baseHalfLifeSeconds;
            DeadlineHalfLifeAvailable = deadlineHalfLifeAvailable;
            DeadlineHalfLifeSeconds = deadlineHalfLifeSeconds;
            AppliedHalfLifeSeconds = appliedHalfLifeSeconds;
            SwingRawTargetHeightAlongUp = swingRawTargetHeightAlongUp;
            SwingFilteredTargetHeightBefore =
                swingFilteredTargetHeightBefore;
            SwingTargetHeightDelta = swingTargetHeightDelta;
            SwingTargetHeightAppliedDelta = swingTargetHeightAppliedDelta;
            SwingTargetHeightUpdateHeld = swingTargetHeightUpdateHeld;
            SwingTargetHeightRateLimited = swingTargetHeightRateLimited;
            SwingTargetHeightClamped = swingTargetHeightClamped;
            SwingTargetMaximumVerticalSpeed =
                swingTargetMaximumVerticalSpeed;
            SwingFilteredTargetHeightAlongUp =
                swingFilteredTargetHeightAlongUp;
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
            PlantInterpolationEvaluated = false;
            PlantTargetEventIdentity = 0;
            PlantTargetVerified = false;
            PlantDesiredPoint = default;
            PlantFilteredPoint = default;
            PlantBlendWeight = 0f;
            PlantTargetMaximumVerticalSpeed = 0f;
            PlantTargetVerticalDelta = 0f;
            PlantTargetAppliedVerticalDelta = 0f;
            PlantTargetVerticalClamped = false;
            PlantBlendedCorrection = default;
            PlantCorrectionMaximumVerticalSpeed = 0f;
            PlantCorrectionVerticalDelta = 0f;
            PlantCorrectionAppliedVerticalDelta = 0f;
            PlantCorrectionVerticalClamped = false;
            PlantOutputDistance = 0f;
            PlantPenetrationDepth = 0f;
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
            TargetTrackingApplied = source.TargetTrackingApplied;
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
            LandingAcceptanceDistance = source.LandingAcceptanceDistance;
            PathRevisionDistance = source.PathRevisionDistance;
            SwingResidualTolerance = source.SwingResidualTolerance;
            TimeToLandingSeconds = source.TimeToLandingSeconds;
            BaseHalfLifeSeconds = source.BaseHalfLifeSeconds;
            DeadlineHalfLifeAvailable = source.DeadlineHalfLifeAvailable;
            DeadlineHalfLifeSeconds = source.DeadlineHalfLifeSeconds;
            AppliedHalfLifeSeconds = source.AppliedHalfLifeSeconds;
            SwingRawTargetHeightAlongUp =
                source.SwingRawTargetHeightAlongUp;
            SwingFilteredTargetHeightBefore =
                source.SwingFilteredTargetHeightBefore;
            SwingTargetHeightDelta = source.SwingTargetHeightDelta;
            SwingTargetHeightAppliedDelta =
                source.SwingTargetHeightAppliedDelta;
            SwingTargetHeightUpdateHeld =
                source.SwingTargetHeightUpdateHeld;
            SwingTargetHeightRateLimited =
                source.SwingTargetHeightRateLimited;
            SwingTargetHeightClamped = source.SwingTargetHeightClamped;
            SwingTargetMaximumVerticalSpeed =
                source.SwingTargetMaximumVerticalSpeed;
            SwingFilteredTargetHeightAlongUp =
                source.SwingFilteredTargetHeightAlongUp;
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
            CharacterFootPlantInterpolationFact plant = interpolation.PlantFact;
            PlantInterpolationEvaluated = plant.Evaluated;
            PlantTargetEventIdentity = plant.EventIdentity;
            PlantTargetVerified = plant.Verified;
            PlantDesiredPoint = plant.DesiredPoint;
            PlantFilteredPoint = plant.FilteredPoint;
            PlantBlendWeight = plant.BlendWeight;
            PlantTargetMaximumVerticalSpeed =
                plant.TargetMaximumVerticalSpeed;
            PlantTargetVerticalDelta = plant.TargetVerticalDelta;
            PlantTargetAppliedVerticalDelta =
                plant.TargetAppliedVerticalDelta;
            PlantTargetVerticalClamped = plant.TargetVerticalClamped;
            PlantBlendedCorrection = plant.BlendedCorrection;
            PlantCorrectionMaximumVerticalSpeed =
                plant.CorrectionMaximumVerticalSpeed;
            PlantCorrectionVerticalDelta = plant.CorrectionVerticalDelta;
            PlantCorrectionAppliedVerticalDelta =
                plant.CorrectionAppliedVerticalDelta;
            PlantCorrectionVerticalClamped =
                plant.CorrectionVerticalClamped;
            PlantOutputDistance = plant.OutputDistance;
            PlantPenetrationDepth = plant.PenetrationDepth;
        }

        internal bool Evaluated { get; }
        internal CharacterFootPathRevisionReason RevisionReason { get; }
        internal bool ResidualRebuilt { get; }
        internal bool TargetTrackingApplied { get; }
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
        internal float LandingAcceptanceDistance { get; }
        internal float PathRevisionDistance { get; }
        internal float SwingResidualTolerance { get; }
        internal float TimeToLandingSeconds { get; }
        internal float BaseHalfLifeSeconds { get; }
        internal bool DeadlineHalfLifeAvailable { get; }
        internal float DeadlineHalfLifeSeconds { get; }
        internal float AppliedHalfLifeSeconds { get; }
        internal float SwingRawTargetHeightAlongUp { get; }
        internal float SwingFilteredTargetHeightBefore { get; }
        internal float SwingTargetHeightDelta { get; }
        internal float SwingTargetHeightAppliedDelta { get; }
        internal bool SwingTargetHeightUpdateHeld { get; }
        internal bool SwingTargetHeightRateLimited { get; }
        internal bool SwingTargetHeightClamped { get; }
        internal float SwingTargetMaximumVerticalSpeed { get; }
        internal float SwingFilteredTargetHeightAlongUp { get; }
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
        internal bool PlantInterpolationEvaluated { get; }
        internal ulong PlantTargetEventIdentity { get; }
        internal bool PlantTargetVerified { get; }
        internal Vector3 PlantDesiredPoint { get; }
        internal Vector3 PlantFilteredPoint { get; }
        internal float PlantBlendWeight { get; }
        internal float PlantTargetMaximumVerticalSpeed { get; }
        internal float PlantTargetVerticalDelta { get; }
        internal float PlantTargetAppliedVerticalDelta { get; }
        internal bool PlantTargetVerticalClamped { get; }
        internal Vector3 PlantBlendedCorrection { get; }
        internal float PlantCorrectionMaximumVerticalSpeed { get; }
        internal float PlantCorrectionVerticalDelta { get; }
        internal float PlantCorrectionAppliedVerticalDelta { get; }
        internal bool PlantCorrectionVerticalClamped { get; }
        internal float PlantOutputDistance { get; }
        internal float PlantPenetrationDepth { get; }

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
                settings.LandingAcceptanceDistance,
                settings.PathRevisionDistance,
                settings.SwingResidualTolerance,
                timeToLandingSeconds,
                settings.EffectiveCorrectionHalfLifeSeconds,
                false,
                0f,
                settings.EffectiveCorrectionHalfLifeSeconds,
                0f,
                0f,
                0f,
                0f,
                false,
                false,
                false,
                settings.MaximumVerticalTargetSpeed,
                0f);
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
            ulong landingEventIdentity,
            in CharacterFootLandingPredictionResult diagnostics) =>
            new CharacterFootLandingFact(
                landingEventIdentity,
                diagnostics.TrajectoryGeneration,
                diagnostics.FutureBodyTranslationSourceIdentity,
                diagnostics.SurfaceIdentity,
                diagnostics.LandingPoint,
                diagnostics.LandingNormal);
    }

    internal readonly struct CharacterFootLandingSnapshot
    {
        internal CharacterFootLandingSnapshot(
            CharacterFootNextLandingTrackingState nextTrackingState,
            ulong nextTrackingEventIdentity,
            bool hasLastLanding,
            CharacterFootGroundPathLanding lastLanding,
            bool hasNextSwingLanding,
            CharacterFootGroundPathLanding nextSwingLanding,
            float nextSwingPredictionError,
            bool hasPromotedLanding,
            CharacterFootGroundPathLanding promotedLanding,
            CharacterFootPlantTargetState plantTargetState,
            bool hasPlantTarget,
            CharacterFootGroundPathLanding plantTarget,
            bool plantTargetUpdated,
            bool plantVerificationAttempted,
            bool plantVerificationUnavailable)
        {
            NextTrackingState = nextTrackingState;
            NextTrackingEventIdentity = nextTrackingEventIdentity;
            HasLastLanding = hasLastLanding;
            LastLanding = lastLanding;
            HasNextSwingLanding = hasNextSwingLanding;
            NextSwingLanding = nextSwingLanding;
            NextSwingPredictionError = nextSwingPredictionError;
            HasPromotedLanding = hasPromotedLanding;
            PromotedLanding = promotedLanding;
            PlantTargetState = plantTargetState;
            HasPlantTarget = hasPlantTarget;
            PlantTarget = plantTarget;
            PlantTargetUpdated = plantTargetUpdated;
            PlantVerificationAttempted = plantVerificationAttempted;
            PlantVerificationUnavailable = plantVerificationUnavailable;
        }

        internal CharacterFootNextLandingTrackingState NextTrackingState { get; }
        internal ulong NextTrackingEventIdentity { get; }
        internal bool HasLastLanding { get; }
        internal CharacterFootGroundPathLanding LastLanding { get; }
        internal ulong LastLandingEventIdentity =>
            HasLastLanding ? LastLanding.LandingEventIdentity : 0;
        internal bool HasNextSwingLanding { get; }
        internal CharacterFootGroundPathLanding NextSwingLanding { get; }
        internal float NextSwingPredictionError { get; }
        internal bool HasPromotedLanding { get; }
        internal CharacterFootGroundPathLanding PromotedLanding { get; }
        internal CharacterFootPlantTargetState PlantTargetState { get; }
        internal bool HasPlantTarget { get; }
        internal CharacterFootGroundPathLanding PlantTarget { get; }
        internal bool PlantTargetUpdated { get; }
        internal bool PlantVerificationAttempted { get; }
        internal bool PlantVerificationUnavailable { get; }
        internal bool HasVerifiedLastLanding => HasLastLanding;
        internal ulong VerifiedLastLandingEventIdentity =>
            HasLastLanding ? LastLanding.LandingEventIdentity : 0;

        internal bool TryResolveVerifiedLanding(
            ulong landingEventIdentity,
            out CharacterFootGroundPathLanding landing)
        {
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
        internal CharacterFootLandingFact PlantTarget;
        internal Vector3 NextSwingReferencePoint;
        internal float NextSwingPredictionError;
        internal ulong TrackedEventIdentity;
        internal CharacterFootNextLandingTrackingState NextTrackingState;
        internal CharacterFootPlantTargetState PlantTargetState;
        internal bool PlantTargetUpdated;
        internal bool PlantVerificationAttempted;
        internal bool PlantVerificationUnavailable;

        internal CharacterFootLandingSnapshot Snapshot =>
            new CharacterFootLandingSnapshot(
                NextTrackingState,
                TrackedEventIdentity,
                LastLanding.HasValue,
                LastLanding.HasValue ? LastLanding.Resolve() : default,
                NextSwingLanding.HasValue,
                NextSwingLanding.HasValue ? NextSwingLanding.Resolve() : default,
                NextSwingLanding.HasValue ? NextSwingPredictionError : 0f,
                PromotedLanding.HasValue,
                PromotedLanding.HasValue ? PromotedLanding.Resolve() : default,
                PlantTargetState,
                PlantTarget.HasValue,
                PlantTarget.HasValue ? PlantTarget.Resolve() : default,
                PlantTargetUpdated,
                PlantVerificationAttempted,
                PlantVerificationUnavailable);

        internal void BeginFrame()
        {
            PromotedLanding = default;
            PlantTargetUpdated = false;
            PlantVerificationAttempted = false;
            PlantVerificationUnavailable = false;
        }

        internal void RetainTracking()
        {
            bool retainsLanding = NextSwingLanding.HasValue &&
                                  NextSwingLanding.LandingEventIdentity ==
                                  TrackedEventIdentity;
            if (!retainsLanding)
            {
                NextSwingReferencePoint = default;
                NextSwingPredictionError = 0f;
            }
            NextTrackingState = TrackedEventIdentity != 0
                ? CharacterFootNextLandingTrackingState.Tracking
                : CharacterFootNextLandingTrackingState.Empty;
        }

        internal void ClearNextSwing()
        {
            NextSwingLanding = default;
            NextSwingReferencePoint = default;
            NextSwingPredictionError = 0f;
            NextTrackingState = TrackedEventIdentity != 0
                ? CharacterFootNextLandingTrackingState.Tracking
                : CharacterFootNextLandingTrackingState.Empty;
        }

        internal void TrackPlantTarget(in CharacterFootLandingFact target)
        {
            bool changed = !PlantTarget.HasValue ||
                           PlantTarget.LandingEventIdentity !=
                           target.LandingEventIdentity ||
                           PlantTarget.SurfaceIdentity != target.SurfaceIdentity ||
                           Vector3.Distance(
                               PlantTarget.WorldPoint,
                               target.WorldPoint) >
                           CharacterFootConstraintMath.GeometryEpsilon;
            PlantTarget = target;
            PlantTargetState = CharacterFootPlantTargetState.Tracking;
            PlantTargetUpdated |= changed;
        }

        internal void VerifyPlantTarget(in CharacterFootLandingFact target)
        {
            PlantTarget = target;
            PlantTargetState = CharacterFootPlantTargetState.Verified;
            PlantTargetUpdated = true;
        }

        internal void ClearTrackingPlantTarget()
        {
            if (PlantTargetState == CharacterFootPlantTargetState.Verified)
                return;
            PlantTarget = default;
            PlantTargetState = CharacterFootPlantTargetState.Empty;
        }
    }

    internal struct CharacterFootDiscreteStateContext
    {
        internal CharacterFootConstraintState State;
        internal CharacterFootLockResponse LockResponse;
        internal CharacterFootTransitionPhase LastTransitionPhase;
        internal CharacterFootTransitionReason LastTransitionReason;
    }

    internal readonly struct CharacterFootLockRequest
    {
        internal CharacterFootLockRequest(
            in AnimationFootMotionRuntimeSample sample)
        {
            if (!sample.IsValid || !sample.Events.IsValid)
                throw new ArgumentException("Formal Foot Lock request is invalid.");
            Contact = sample.Contact;
            Mode = sample.LockMode;
            Weight = sample.LockWeight;
            bool requestsLock = Contact > 0f &&
                                Mode != AnimationFootStepObservationLockMode.Unlocked;
            AnimationFootMotionEventOccurrence current =
                sample.Events.CurrentContact;
            EventIdentity = current.IsBound ? current.Identity : 0;
            Availability = requestsLock && EventIdentity == 0
                ? CharacterFootLockRequestAvailability.ContactEventUnavailable
                : CharacterFootLockRequestAvailability.Ready;
        }

        internal float Contact { get; }
        internal AnimationFootStepObservationLockMode Mode { get; }
        internal float Weight { get; }
        internal ulong EventIdentity { get; }
        internal CharacterFootLockRequestAvailability Availability { get; }
        internal bool RequestsLock =>
            Availability == CharacterFootLockRequestAvailability.Ready &&
            Contact > 0f &&
            Mode != AnimationFootStepObservationLockMode.Unlocked;
        internal CharacterFootLockResponse Response => Mode switch
        {
            AnimationFootStepObservationLockMode.Sliding =>
                CharacterFootLockResponse.Sliding,
            AnimationFootStepObservationLockMode.Locked =>
                CharacterFootLockResponse.FullAnchor,
            _ => CharacterFootLockResponse.None
        };
    }

    internal struct CharacterFootContactTransitionContext
    {
        internal bool HasPreviousRequest;
        internal bool PreviousRequestedLock;
        internal ulong PreviousEventIdentity;
        internal AnimationFootStepObservationLockMode PreviousMode;
        internal float PreviousWeight;
        internal float SecondsSinceEdge;
        internal ulong LatestContactEventIdentity;
        internal ulong LatestReleasedContactEventIdentity;
        internal CharacterFootContactEdge LastEdge;
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

    internal readonly struct CharacterFootSupportIntent
    {
        internal CharacterFootSupportIntent(
            bool available,
            ulong eventIdentity,
            float weight)
        {
            Available = available;
            EventIdentity = eventIdentity;
            Weight = weight;
        }

        internal bool Available { get; }
        internal ulong EventIdentity { get; }
        internal float Weight { get; }
    }

    internal readonly struct CharacterFootPlantInterpolationFact
    {
        internal CharacterFootPlantInterpolationFact(
            bool evaluated,
            ulong eventIdentity,
            bool verified,
            Vector3 desiredPoint,
            Vector3 filteredPoint,
            float blendWeight,
            float targetMaximumVerticalSpeed,
            float targetVerticalDelta,
            float targetAppliedVerticalDelta,
            bool targetVerticalClamped,
            Vector3 blendedCorrection,
            float correctionMaximumVerticalSpeed,
            float correctionVerticalDelta,
            float correctionAppliedVerticalDelta,
            bool correctionVerticalClamped,
            float outputDistance,
            float penetrationDepth)
        {
            Evaluated = evaluated;
            EventIdentity = eventIdentity;
            Verified = verified;
            DesiredPoint = desiredPoint;
            FilteredPoint = filteredPoint;
            BlendWeight = blendWeight;
            TargetMaximumVerticalSpeed = targetMaximumVerticalSpeed;
            TargetVerticalDelta = targetVerticalDelta;
            TargetAppliedVerticalDelta = targetAppliedVerticalDelta;
            TargetVerticalClamped = targetVerticalClamped;
            BlendedCorrection = blendedCorrection;
            CorrectionMaximumVerticalSpeed = correctionMaximumVerticalSpeed;
            CorrectionVerticalDelta = correctionVerticalDelta;
            CorrectionAppliedVerticalDelta = correctionAppliedVerticalDelta;
            CorrectionVerticalClamped = correctionVerticalClamped;
            OutputDistance = outputDistance;
            PenetrationDepth = penetrationDepth;
        }

        internal bool Evaluated { get; }
        internal ulong EventIdentity { get; }
        internal bool Verified { get; }
        internal Vector3 DesiredPoint { get; }
        internal Vector3 FilteredPoint { get; }
        internal float BlendWeight { get; }
        internal float TargetMaximumVerticalSpeed { get; }
        internal float TargetVerticalDelta { get; }
        internal float TargetAppliedVerticalDelta { get; }
        internal bool TargetVerticalClamped { get; }
        internal Vector3 BlendedCorrection { get; }
        internal float CorrectionMaximumVerticalSpeed { get; }
        internal float CorrectionVerticalDelta { get; }
        internal float CorrectionAppliedVerticalDelta { get; }
        internal bool CorrectionVerticalClamped { get; }
        internal float OutputDistance { get; }
        internal float PenetrationDepth { get; }
    }

    internal struct CharacterFootInterpolationState
    {
        internal bool HasOutput;
        internal bool HasSwingPath;
        internal ulong SwingLandingEventIdentity;
        internal ulong SwingGroundPathInputIdentity;
        internal Vector3 SwingLandingPoint;
        internal Vector3 PreviousTargetCorrection;
        internal Vector3 PreviousSwingTargetCorrection;
        internal Vector3 EffectiveCorrection;
        internal Vector3 SwingResidual;
        internal bool HasTargetHeight;
        internal ulong TargetHeightEventIdentity;
        internal float FilteredTargetHeightAlongUp;
        internal bool TargetHeightRetargetActive;
        internal Vector3 Residual;
        internal float Progress;
        internal float StartResidual;
        internal bool Completed;
        internal CharacterFootInterpolationPolicy Policy;
        internal bool HasPlantTarget;
        internal ulong PlantTargetEventIdentity;
        internal Vector3 PlantDesiredPoint;
        internal Vector3 PlantFilteredPoint;
        internal float PlantBlendWeight;
        internal bool PlantResponseTransitionActive;
        internal CharacterFootPlantInterpolationFact PlantFact;
    }

    internal struct CharacterFootLifecycleContext
    {
        internal CharacterFootLandingContext Landing;
        internal CharacterFootDiscreteStateContext Discrete;
        internal CharacterFootContactContext Contact;
        internal CharacterFootContactTransitionContext ContactTransition;
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
            Vector3 animatedHip,
            float legLength,
            in CharacterFootSwingMotionResult swingMotion,
            bool hasContactLanding,
            in CharacterFootGroundPathLanding contactLanding,
            bool preparedPlantActive,
            in CharacterFootGroundPathLanding preparedPlantTarget,
            float preparedPlantWeight,
            in CharacterFootLockRequest lockRequest,
            float formalSupport,
            ulong formalSupportEventIdentity,
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
            AnimatedHip = animatedHip;
            LegLength = legLength;
            SwingMotion = swingMotion;
            HasContactLanding = hasContactLanding;
            ContactLanding = contactLanding;
            PreparedPlantActive = preparedPlantActive;
            PreparedPlantTarget = preparedPlantTarget;
            PreparedPlantWeight = preparedPlantWeight;
            LockRequest = lockRequest;
            FormalSupport = formalSupport;
            FormalSupportEventIdentity = formalSupportEventIdentity;
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
        internal Vector3 AnimatedHip { get; }
        internal float LegLength { get; }
        internal CharacterFootSwingMotionResult SwingMotion { get; }
        internal bool HasContactLanding { get; }
        internal CharacterFootGroundPathLanding ContactLanding { get; }
        internal bool PreparedPlantActive { get; }
        internal CharacterFootGroundPathLanding PreparedPlantTarget { get; }
        internal float PreparedPlantWeight { get; }
        internal CharacterFootLockRequest LockRequest { get; }
        internal float FormalSupport { get; }
        internal ulong FormalSupportEventIdentity { get; }
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
            in AnimationFootMotionRuntimeSample formalFootMotion,
            in CharacterFootLandingPredictionResult landingPrediction,
            in CharacterFootStateFrame frame)
        {
            Side = side;
            FormalFootMotion = formalFootMotion;
            LandingPrediction = landingPrediction;
            Frame = frame;
        }

        internal CharacterFootSide Side { get; }
        internal AnimationFootMotionRuntimeSample FormalFootMotion { get; }
        internal CharacterFootLandingPredictionResult LandingPrediction { get; }
        internal CharacterFootStateFrame Frame { get; }
    }

    internal readonly struct CharacterFootLifecycleEvaluationReceipt
    {
        internal CharacterFootLifecycleEvaluationReceipt(
            in CharacterFootStateEvaluation evaluation,
            in CharacterFootTransitionDecision preTransition,
            in CharacterFootStateTarget target,
            in CharacterFootInterpolationResult interpolation,
            CharacterFootConstraintState stateBefore,
            CharacterFootLockResponse lockResponseBefore,
            in CharacterFootSwingMotionResult outputSwing,
            in CharacterResolvedFootResult preliminaryResolved,
            in CharacterFootSwingMotionResult preliminaryMotion,
            bool landingCompletionPending)
        {
            Evaluation = evaluation;
            PreTransition = preTransition;
            Target = target;
            Interpolation = interpolation;
            StateBefore = stateBefore;
            LockResponseBefore = lockResponseBefore;
            OutputSwing = outputSwing;
            PreliminaryResolved = preliminaryResolved;
            PreliminaryMotion = preliminaryMotion;
            LandingCompletionPending = landingCompletionPending;
        }

        internal CharacterFootStateEvaluation Evaluation { get; }
        internal CharacterFootTransitionDecision PreTransition { get; }
        internal CharacterFootStateTarget Target { get; }
        internal CharacterFootInterpolationResult Interpolation { get; }
        internal CharacterFootConstraintState StateBefore { get; }
        internal CharacterFootLockResponse LockResponseBefore { get; }
        internal CharacterFootSwingMotionResult OutputSwing { get; }
        internal CharacterResolvedFootResult PreliminaryResolved { get; }
        internal CharacterFootSwingMotionResult PreliminaryMotion { get; }
        internal bool LandingCompletionPending { get; }
    }

    internal readonly struct CharacterFootTransitionDecision
    {
        internal CharacterFootTransitionDecision(
            CharacterFootTransitionPhase phase,
            CharacterFootTransitionReason reason,
            CharacterFootConstraintState sourceState,
            CharacterFootConstraintState targetState,
            CharacterFootLockResponse targetLockResponse,
            CharacterFootContactEdge contactEdge,
            CharacterFootAnchorCommand anchorCommand,
            bool suppressOutput,
            bool resetInterpolation)
        {
            Phase = phase;
            Reason = reason;
            SourceState = sourceState;
            TargetState = targetState;
            TargetLockResponse = targetLockResponse;
            ContactEdge = contactEdge;
            AnchorCommand = anchorCommand;
            SuppressOutput = suppressOutput;
            ResetInterpolation = resetInterpolation;
        }

        internal CharacterFootTransitionPhase Phase { get; }
        internal CharacterFootTransitionReason Reason { get; }
        internal CharacterFootConstraintState SourceState { get; }
        internal CharacterFootConstraintState TargetState { get; }
        internal CharacterFootLockResponse TargetLockResponse { get; }
        internal CharacterFootContactEdge ContactEdge { get; }
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
            CharacterFootInterpolationPolicy interpolationPolicy,
            bool plantTargetAvailable,
            ulong plantTargetEventIdentity,
            bool plantTargetVerified,
            Vector3 plantTargetPoint,
            bool stateEntered,
            bool responseEntered,
            bool directPlantFollow,
            bool suppressOutput,
            float progress,
            float timeToLandingSeconds,
            in CharacterFootSupportIntent supportIntent)
        {
            Correction = correction;
            SwingCorrection = swingCorrection;
            InterpolationPolicy = interpolationPolicy;
            PlantTargetAvailable = plantTargetAvailable;
            PlantTargetEventIdentity = plantTargetEventIdentity;
            PlantTargetVerified = plantTargetVerified;
            PlantTargetPoint = plantTargetPoint;
            StateEntered = stateEntered;
            ResponseEntered = responseEntered;
            DirectPlantFollow = directPlantFollow;
            SuppressOutput = suppressOutput;
            Progress = progress;
            TimeToLandingSeconds = timeToLandingSeconds;
            SupportIntent = supportIntent;
        }

        internal Vector3 Correction { get; }
        internal Vector3 SwingCorrection { get; }
        internal CharacterFootInterpolationPolicy InterpolationPolicy { get; }
        internal bool PlantTargetAvailable { get; }
        internal ulong PlantTargetEventIdentity { get; }
        internal bool PlantTargetVerified { get; }
        internal Vector3 PlantTargetPoint { get; }
        internal bool StateEntered { get; }
        internal bool ResponseEntered { get; }
        internal bool DirectPlantFollow { get; }
        internal bool SuppressOutput { get; }
        internal float Progress { get; }
        internal float TimeToLandingSeconds { get; }
        internal CharacterFootSupportIntent SupportIntent { get; }
    }

    internal readonly struct CharacterFootInterpolationResult
    {
        internal CharacterFootInterpolationResult(
            Vector3 correction,
            bool completed,
            in CharacterFootPathContinuityFact continuityFact,
            in CharacterFootPlantInterpolationFact plantFact)
        {
            Correction = correction;
            Completed = completed;
            ContinuityFact = continuityFact;
            PlantFact = plantFact;
        }

        internal Vector3 Correction { get; }
        internal bool Completed { get; }
        internal CharacterFootPathContinuityFact ContinuityFact { get; }
        internal CharacterFootPlantInterpolationFact PlantFact { get; }
    }
}
