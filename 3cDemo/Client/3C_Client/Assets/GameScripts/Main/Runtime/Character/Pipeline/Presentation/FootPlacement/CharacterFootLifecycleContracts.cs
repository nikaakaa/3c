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
        VerifiedSupport = 2,
        ReleaseResidual = 3
    }

    internal enum CharacterFootPlantTargetKind : byte
    {
        None = 0,
        PreparedPrediction = 1,
        VerifiedAnchor = 2,
        LockedFullAnchor = 3,
        LockedSliding = 4
    }

    internal enum CharacterFootPlantTargetHeightUpdateReason : byte
    {
        None = 0,
        Initialized = 1,
        EventChanged = 2,
        VerificationRefresh = 3,
        DirectAdoption = 4,
        DirectFollow = 5,
        ForceRefreshDistanceExceeded = 6,
        HeldWithinRevisionDistance = 7,
        RateLimited = 8,
        WithinRate = 9
    }

    [Flags]
    internal enum CharacterFootPlantResidualCaptureReason : ushort
    {
        None = 0,
        TargetEventChanged = 1 << 0,
        TargetKindChanged = 1 << 1,
        LockResponseChanged = 1 << 2,
        VerificationChanged = 1 << 3,
        DirectFollowChanged = 1 << 4,
        StateEntered = 1 << 5,
        ResponseEntered = 1 << 6,
        TargetPointRevised = 1 << 7,
        TargetHeightForceRefreshed = 1 << 8
    }

    [Flags]
    internal enum CharacterFootVerticalContinuityOwner : byte
    {
        None = 0,
        TargetHeightHistory = 1 << 0,
        PlantWorldResidual = 1 << 1,
        CorrectionResponseHistory = 1 << 2,
        PlantTarget = 1 << 3
    }

    internal enum CharacterFootCorrectionResponseDeltaDirection : byte
    {
        None = 0,
        Increase = 1,
        Decrease = 2
    }

    internal enum CharacterFootCorrectionResponseDomain : byte
    {
        None = 0,
        AnimationRelativeScalar = 1,
        ContactWorldResidual = 2
    }

    internal enum CharacterFootCorrectionResponseInitializationReason : byte
    {
        None = 0,
        FirstLegalInput = 1,
        FootPlacementReset = 2,
        Retarget = 3,
        SourceLineageInvalidated = 4,
        ProfileLineageInvalidated = 5,
        WorldLineageInvalidated = 6,
        PolicyExited = 7
    }

    [Flags]
    internal enum CharacterFootGoalOwnershipLossReason : byte
    {
        None = 0,
        Ungrounded = 1 << 0,
        SourceLineageInvalidated = 1 << 1
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
            bool swingTargetHeightForceRefreshed,
            bool swingTargetHeightRateLimited,
            bool swingTargetHeightClamped,
            float swingTargetHeightForceRefreshDistance,
            float swingTargetMaximumVerticalSpeed,
            CharacterFootTargetHeightAdoptionMode swingTargetHeightAdoptionMode,
            float swingFilteredTargetHeightAlongUp,
            Vector3 interpolationComponentUp)
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
            SwingTargetHeightForceRefreshed =
                swingTargetHeightForceRefreshed;
            SwingTargetHeightRateLimited = swingTargetHeightRateLimited;
            SwingTargetHeightClamped = swingTargetHeightClamped;
            SwingTargetHeightForceRefreshDistance =
                swingTargetHeightForceRefreshDistance;
            SwingTargetMaximumVerticalSpeed =
                swingTargetMaximumVerticalSpeed;
            SwingTargetHeightAdoptionMode = swingTargetHeightAdoptionMode;
            SwingFilteredTargetHeightAlongUp =
                swingFilteredTargetHeightAlongUp;
            TargetHeightComponentUp = interpolationComponentUp;
            StateTargetCorrection = default;
            InterpolationPolicy = CharacterFootInterpolationPolicy.Suppressed;
            InterpolationOutputCorrection = default;
            InterpolationCompleted = false;
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
            PlantTargetKind = CharacterFootPlantTargetKind.None;
            PlantLockResponse = CharacterFootLockResponse.None;
            PlantLockWeightCompleted = false;
            PlantDesiredPoint = default;
            PlantFilteredPoint = default;
            SelectedSupportTarget = default;
            PlantTargetHeightAdoptionMode = swingTargetHeightAdoptionMode;
            PlantTargetMaximumVerticalSpeed = 0f;
            PlantTargetHeightBefore = 0f;
            PlantTargetHeightTarget = 0f;
            PlantTargetVerticalDelta = 0f;
            PlantTargetAppliedVerticalDelta = 0f;
            PlantTargetHeightAfter = 0f;
            PlantTargetHeightEventIdentity = 0;
            PlantTargetHeightUpdateReason =
                CharacterFootPlantTargetHeightUpdateReason.None;
            PlantTargetForceRefreshed = false;
            PlantTargetForceRefreshDistance = 0f;
            PlantTargetVerticalClamped = false;
            PlantPreviousSelectedWorldTarget = default;
            PlantSelectedWorldTarget = default;
            PreviousResponseOutputAvailable = false;
            PreviousResponseOutputPoint = default;
            DesiredOutputPoint = default;
            ResponseOutputPoint = default;
            PlantResidualCaptureReason =
                CharacterFootPlantResidualCaptureReason.None;
            PlantWorldResidualBeforeCapture = default;
            PlantWorldResidualCapturedBeforeDecay = default;
            PlantWorldResidualDecayApplied = false;
            PlantWorldResidualBaseHalfLifeSeconds = 0f;
            PlantWorldResidualDeadlineHalfLifeAvailable = false;
            PlantWorldResidualDeadlineHalfLifeSeconds = 0f;
            PlantWorldResidualAppliedHalfLifeSeconds = 0f;
            PlantWorldResidualAfterDecay = default;
            PlantWorldResidualCompletionTolerance = 0f;
            PlantWorldResidualClearedAtCompletionTolerance = false;
            CorrectionResponseEvaluated = false;
            CorrectionResponseInitializedBefore = false;
            CorrectionResponseInitializedThisFrame = false;
            CorrectionResponseInitializationReason =
                CharacterFootCorrectionResponseInitializationReason.None;
            CorrectionResponseDesired = 0f;
            CorrectionResponseRequestedDirection = default;
            CorrectionResponsePreviousDirection = default;
            CorrectionResponseDirectionLimited = false;
            CorrectionResponseMaximumDirectionChangeDegrees = 0f;
            CorrectionResponseAppliedDirectionChangeDegrees = 0f;
            CorrectionResponseVisibleOutputTransferred = false;
            CorrectionResponseBeforeRebase = 0f;
            CorrectionResponsePrevious = 0f;
            CorrectionResponseCurrent = 0f;
            CorrectionResponseDirection = default;
            CorrectionResponseDeltaDirection =
                CharacterFootCorrectionResponseDeltaDirection.None;
            CorrectionResponseSelectedSpeed = 0f;
            CorrectionResponseAppliedDelta = 0f;
            CorrectionResponseDomain = CharacterFootCorrectionResponseDomain.None;
            CorrectionResponsePreviousDomain = CharacterFootCorrectionResponseDomain.None;
            CorrectionResponseDomainTransferred = false;
            PlantVerticalContinuityOwners =
                CharacterFootVerticalContinuityOwner.None;
            PlantEffectiveCorrectionBefore = default;
            PlantEffectiveCorrectionAfter = default;
            PlantOutputDistance = 0f;
            PlantPenetrationDepth = 0f;
        }

        CharacterFootPathContinuityFact(
            in CharacterFootPathContinuityFact source,
            in CharacterFootTransitionDecision preTransition,
            in CharacterFootTransitionDecision postTransition,
            in CharacterFootStateTarget stateTarget,
            in CharacterFootInterpolationResult interpolation,
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
            SwingTargetHeightForceRefreshed =
                source.SwingTargetHeightForceRefreshed;
            SwingTargetHeightRateLimited =
                source.SwingTargetHeightRateLimited;
            SwingTargetHeightClamped = source.SwingTargetHeightClamped;
            SwingTargetHeightForceRefreshDistance =
                source.SwingTargetHeightForceRefreshDistance;
            SwingTargetMaximumVerticalSpeed =
                source.SwingTargetMaximumVerticalSpeed;
            SwingTargetHeightAdoptionMode =
                source.SwingTargetHeightAdoptionMode;
            SwingFilteredTargetHeightAlongUp =
                source.SwingFilteredTargetHeightAlongUp;
            TargetHeightComponentUp = source.TargetHeightComponentUp;
            StateTargetCorrection = stateTarget.Correction;
            InterpolationPolicy = stateTarget.InterpolationPolicy;
            InterpolationOutputCorrection = interpolation.Correction;
            InterpolationCompleted = interpolation.Completed;
            OutputStagesAvailable = true;
            ReleasingCompletedToSwing =
                preTransition.SourceState == CharacterFootConstraintState.Releasing &&
                postTransition.TargetState == CharacterFootConstraintState.Swing;
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
            PlantTargetKind = plant.TargetKind;
            PlantLockResponse = plant.LockResponse;
            PlantLockWeightCompleted = stateTarget.LockWeightCompleted;
            PlantDesiredPoint = plant.DesiredPoint;
            PlantFilteredPoint = plant.FilteredPoint;
            CharacterFootSupportTarget selectedSupport =
                interpolation.SupportTarget;
            SelectedSupportTarget = selectedSupport;
            PlantTargetHeightAdoptionMode = plant.Evaluated
                ? plant.TargetHeightAdoptionMode
                : source.PlantTargetHeightAdoptionMode;
            PlantTargetMaximumVerticalSpeed =
                plant.TargetMaximumVerticalSpeed;
            PlantTargetHeightBefore = plant.TargetHeightBefore;
            PlantTargetHeightTarget = plant.TargetHeightTarget;
            PlantTargetVerticalDelta = plant.TargetVerticalDelta;
            PlantTargetAppliedVerticalDelta =
                plant.TargetAppliedVerticalDelta;
            PlantTargetHeightAfter = plant.TargetHeightAfter;
            PlantTargetHeightEventIdentity =
                plant.TargetHeightEventIdentity;
            PlantTargetHeightUpdateReason =
                plant.TargetHeightUpdateReason;
            PlantTargetForceRefreshed = plant.TargetForceRefreshed;
            PlantTargetForceRefreshDistance =
                plant.TargetForceRefreshDistance;
            PlantTargetVerticalClamped = plant.TargetVerticalClamped;
            PlantPreviousSelectedWorldTarget =
                plant.PreviousSelectedWorldTarget;
            PlantSelectedWorldTarget = plant.SelectedWorldTarget;
            CharacterFootCorrectionResponseFact correctionResponse =
                interpolation.CorrectionResponseFact;
            PreviousResponseOutputAvailable =
                correctionResponse.PreviousOutputAvailable;
            PreviousResponseOutputPoint =
                correctionResponse.PreviousOutputPoint;
            DesiredOutputPoint = correctionResponse.DesiredOutputPoint;
            ResponseOutputPoint = correctionResponse.ResponseOutputPoint;
            PlantResidualCaptureReason = plant.ResidualCaptureReason;
            PlantWorldResidualBeforeCapture =
                plant.WorldResidualBeforeCapture;
            PlantWorldResidualCapturedBeforeDecay =
                plant.WorldResidualCapturedBeforeDecay;
            PlantWorldResidualDecayApplied =
                plant.WorldResidualDecayApplied;
            PlantWorldResidualBaseHalfLifeSeconds =
                plant.WorldResidualBaseHalfLifeSeconds;
            PlantWorldResidualDeadlineHalfLifeAvailable =
                plant.WorldResidualDeadlineHalfLifeAvailable;
            PlantWorldResidualDeadlineHalfLifeSeconds =
                plant.WorldResidualDeadlineHalfLifeSeconds;
            PlantWorldResidualAppliedHalfLifeSeconds =
                plant.WorldResidualAppliedHalfLifeSeconds;
            PlantWorldResidualAfterDecay = plant.WorldResidualAfterDecay;
            PlantWorldResidualCompletionTolerance =
                plant.WorldResidualCompletionTolerance;
            PlantWorldResidualClearedAtCompletionTolerance =
                plant.WorldResidualClearedAtCompletionTolerance;
            CorrectionResponseEvaluated = correctionResponse.Evaluated;
            CorrectionResponseInitializedBefore =
                correctionResponse.InitializedBefore;
            CorrectionResponseInitializedThisFrame =
                correctionResponse.InitializedThisFrame;
            CorrectionResponseInitializationReason =
                correctionResponse.InitializationReason;
            CorrectionResponseDesired =
                correctionResponse.DesiredResponse;
            CorrectionResponseRequestedDirection =
                correctionResponse.RequestedResponseDirection;
            CorrectionResponsePreviousDirection =
                correctionResponse.PreviousResponseDirection;
            CorrectionResponseDirectionLimited =
                correctionResponse.DirectionLimited;
            CorrectionResponseMaximumDirectionChangeDegrees =
                correctionResponse.MaximumDirectionChangeDegrees;
            CorrectionResponseAppliedDirectionChangeDegrees =
                correctionResponse.AppliedDirectionChangeDegrees;
            CorrectionResponseVisibleOutputTransferred =
                correctionResponse.VisibleOutputTransferred;
            CorrectionResponseBeforeRebase =
                correctionResponse.ResponseBeforeRebase;
            CorrectionResponsePrevious =
                correctionResponse.PreviousResponse;
            CorrectionResponseCurrent =
                correctionResponse.CurrentResponse;
            CorrectionResponseDirection =
                correctionResponse.ResponseDirection;
            CorrectionResponseDeltaDirection =
                correctionResponse.DeltaDirection;
            CorrectionResponseSelectedSpeed =
                correctionResponse.SelectedSpeed;
            CorrectionResponseAppliedDelta =
                correctionResponse.AppliedDelta;
            CorrectionResponseDomain = correctionResponse.Domain;
            CorrectionResponsePreviousDomain = correctionResponse.PreviousDomain;
            CorrectionResponseDomainTransferred = correctionResponse.DomainTransferred;
            PlantVerticalContinuityOwners =
                plant.VerticalContinuityOwners;
            PlantEffectiveCorrectionBefore =
                plant.EffectiveCorrectionBefore;
            PlantEffectiveCorrectionAfter = plant.EffectiveCorrectionAfter;
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
        internal bool SwingTargetHeightForceRefreshed { get; }
        internal bool SwingTargetHeightRateLimited { get; }
        internal bool SwingTargetHeightClamped { get; }
        internal float SwingTargetHeightForceRefreshDistance { get; }
        internal float SwingTargetMaximumVerticalSpeed { get; }
        internal CharacterFootTargetHeightAdoptionMode SwingTargetHeightAdoptionMode { get; }
        internal float SwingFilteredTargetHeightAlongUp { get; }
        internal Vector3 TargetHeightComponentUp { get; }
        internal Vector3 StateTargetCorrection { get; }
        internal CharacterFootInterpolationPolicy InterpolationPolicy { get; }
        internal Vector3 InterpolationOutputCorrection { get; }
        internal bool InterpolationCompleted { get; }
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
        internal CharacterFootPlantTargetKind PlantTargetKind { get; }
        internal CharacterFootLockResponse PlantLockResponse { get; }
        internal bool PlantLockWeightCompleted { get; }
        internal Vector3 PlantDesiredPoint { get; }
        internal Vector3 PlantFilteredPoint { get; }
        internal CharacterFootSupportTarget SelectedSupportTarget { get; }
        internal CharacterFootTargetHeightAdoptionMode PlantTargetHeightAdoptionMode { get; }
        internal float PlantTargetMaximumVerticalSpeed { get; }
        internal float PlantTargetHeightBefore { get; }
        internal float PlantTargetHeightTarget { get; }
        internal float PlantTargetVerticalDelta { get; }
        internal float PlantTargetAppliedVerticalDelta { get; }
        internal float PlantTargetHeightAfter { get; }
        internal ulong PlantTargetHeightEventIdentity { get; }
        internal CharacterFootPlantTargetHeightUpdateReason PlantTargetHeightUpdateReason { get; }
        internal bool PlantTargetForceRefreshed { get; }
        internal float PlantTargetForceRefreshDistance { get; }
        internal bool PlantTargetVerticalClamped { get; }
        internal Vector3 PlantPreviousSelectedWorldTarget { get; }
        internal Vector3 PlantSelectedWorldTarget { get; }
        internal bool PreviousResponseOutputAvailable { get; }
        internal Vector3 PreviousResponseOutputPoint { get; }
        internal Vector3 DesiredOutputPoint { get; }
        internal Vector3 ResponseOutputPoint { get; }
        internal CharacterFootPlantResidualCaptureReason PlantResidualCaptureReason { get; }
        internal Vector3 PlantWorldResidualBeforeCapture { get; }
        internal Vector3 PlantWorldResidualCapturedBeforeDecay { get; }
        internal bool PlantWorldResidualDecayApplied { get; }
        internal float PlantWorldResidualBaseHalfLifeSeconds { get; }
        internal bool PlantWorldResidualDeadlineHalfLifeAvailable { get; }
        internal float PlantWorldResidualDeadlineHalfLifeSeconds { get; }
        internal float PlantWorldResidualAppliedHalfLifeSeconds { get; }
        internal Vector3 PlantWorldResidualAfterDecay { get; }
        internal float PlantWorldResidualCompletionTolerance { get; }
        internal bool PlantWorldResidualClearedAtCompletionTolerance { get; }
        internal bool CorrectionResponseEvaluated { get; }
        internal bool CorrectionResponseInitializedBefore { get; }
        internal bool CorrectionResponseInitializedThisFrame { get; }
        internal CharacterFootCorrectionResponseInitializationReason
            CorrectionResponseInitializationReason { get; }
        internal float CorrectionResponseDesired { get; }
        internal Vector3 CorrectionResponseRequestedDirection { get; }
        internal Vector3 CorrectionResponsePreviousDirection { get; }
        internal bool CorrectionResponseDirectionLimited { get; }
        internal float CorrectionResponseMaximumDirectionChangeDegrees { get; }
        internal float CorrectionResponseAppliedDirectionChangeDegrees { get; }
        internal bool CorrectionResponseVisibleOutputTransferred { get; }
        internal float CorrectionResponseBeforeRebase { get; }
        internal float CorrectionResponsePrevious { get; }
        internal float CorrectionResponseCurrent { get; }
        internal Vector3 CorrectionResponseDirection { get; }
        internal CharacterFootCorrectionResponseDeltaDirection
            CorrectionResponseDeltaDirection { get; }
        internal float CorrectionResponseSelectedSpeed { get; }
        internal float CorrectionResponseAppliedDelta { get; }
        internal CharacterFootCorrectionResponseDomain CorrectionResponseDomain { get; }
        internal CharacterFootCorrectionResponseDomain CorrectionResponsePreviousDomain { get; }
        internal bool CorrectionResponseDomainTransferred { get; }
        internal CharacterFootVerticalContinuityOwner PlantVerticalContinuityOwners { get; }
        internal Vector3 PlantEffectiveCorrectionBefore { get; }
        internal Vector3 PlantEffectiveCorrectionAfter { get; }
        internal float PlantOutputDistance { get; }
        internal float PlantPenetrationDepth { get; }

        internal CharacterFootPathContinuityFact Complete(
            in CharacterFootTransitionDecision preTransition,
            in CharacterFootTransitionDecision postTransition,
            in CharacterFootStateTarget stateTarget,
            in CharacterFootInterpolationResult interpolation,
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
            CharacterFootMotionSettings settings,
            Vector3 interpolationComponentUp) =>
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
                false,
                settings.TargetHeightForceRefreshDistance,
                settings.MaximumVerticalTargetSpeed,
                settings.TargetHeightAdoptionMode,
                0f,
                interpolationComponentUp);
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
        internal ulong CompletedLockWeightEventIdentity;
        internal CharacterFootContactEdge LastEdge;

        internal bool HasCompletedLockWeight(ulong eventIdentity) =>
            eventIdentity != 0 &&
            CompletedLockWeightEventIdentity == eventIdentity;
    }

    internal struct CharacterFootContactContext
    {
        internal bool HasContact;
        internal ulong EventIdentity;
        internal ulong AcquiredFrameSequence;
        internal ulong AcquiredCompletionIdentity;
        internal ulong WorldRevision;
        internal int SurfaceIdentity;
        internal Vector3 Anchor;
        internal Vector3 Normal;

        internal void Clear() => this = default;
    }

    internal readonly struct CharacterFootContactHistoryFact
    {
        internal CharacterFootContactHistoryFact(
            in CharacterFootContactTransitionContext context)
        {
            RequestAvailable = context.HasPreviousRequest;
            RequestedLock = context.PreviousRequestedLock;
            RequestEventIdentity = context.PreviousEventIdentity;
            RequestMode = context.PreviousMode;
            RequestWeight = context.PreviousWeight;
            SecondsSinceEdge = context.SecondsSinceEdge;
            LatestContactEventIdentity = context.LatestContactEventIdentity;
            LatestReleasedContactEventIdentity =
                context.LatestReleasedContactEventIdentity;
            CompletedLockWeightEventIdentity =
                context.CompletedLockWeightEventIdentity;
        }

        internal bool RequestAvailable { get; }
        internal bool RequestedLock { get; }
        internal ulong RequestEventIdentity { get; }
        internal AnimationFootStepObservationLockMode RequestMode { get; }
        internal float RequestWeight { get; }
        internal float SecondsSinceEdge { get; }
        internal ulong LatestContactEventIdentity { get; }
        internal ulong LatestReleasedContactEventIdentity { get; }
        internal ulong CompletedLockWeightEventIdentity { get; }
    }

    internal readonly struct CharacterFootContactAnchorFact
    {
        internal CharacterFootContactAnchorFact(
            in CharacterFootContactContext context)
        {
            Available = context.HasContact;
            EventIdentity = context.EventIdentity;
            AcquiredFrameSequence = context.AcquiredFrameSequence;
            AcquiredCompletionIdentity = context.AcquiredCompletionIdentity;
            WorldRevision = context.WorldRevision;
            SurfaceIdentity = context.SurfaceIdentity;
            Point = context.Anchor;
            Normal = context.Normal;
        }

        internal bool Available { get; }
        internal ulong EventIdentity { get; }
        internal ulong AcquiredFrameSequence { get; }
        internal ulong AcquiredCompletionIdentity { get; }
        internal ulong WorldRevision { get; }
        internal int SurfaceIdentity { get; }
        internal Vector3 Point { get; }
        internal Vector3 Normal { get; }
    }

    internal readonly struct CharacterFootLifecycleTransitionFact
    {
        CharacterFootLifecycleTransitionFact(
            bool evaluated,
            in CharacterFootContactHistoryFact previousContext,
            in CharacterFootContactHistoryFact currentContext,
            in CharacterFootContactAnchorFact previousAnchor,
            in CharacterFootContactAnchorFact currentAnchor,
            in CharacterFootLockRequest request,
            CharacterFootLockResponse lockResponseBefore,
            CharacterFootGoalOwnershipLossReason ownershipLossReason,
            float formalFootPlacementWeight,
            in CharacterFootTransitionDecision preTransition,
            in CharacterFootTransitionDecision postTransition)
        {
            Evaluated = evaluated;
            PreviousContext = previousContext;
            CurrentContext = currentContext;
            PreviousAnchor = previousAnchor;
            CurrentAnchor = currentAnchor;
            Request = request;
            LockResponseBefore = lockResponseBefore;
            OwnershipLossReason = ownershipLossReason;
            FormalFootPlacementWeight = formalFootPlacementWeight;
            PreTransition = preTransition;
            PostTransition = postTransition;
        }

        internal bool Evaluated { get; }
        internal CharacterFootContactHistoryFact PreviousContext { get; }
        internal CharacterFootContactHistoryFact CurrentContext { get; }
        internal CharacterFootContactAnchorFact PreviousAnchor { get; }
        internal CharacterFootContactAnchorFact CurrentAnchor { get; }
        internal CharacterFootLockRequest Request { get; }
        internal CharacterFootLockResponse LockResponseBefore { get; }
        internal CharacterFootGoalOwnershipLossReason OwnershipLossReason { get; }
        internal float FormalFootPlacementWeight { get; }
        internal CharacterFootTransitionDecision PreTransition { get; }
        internal CharacterFootTransitionDecision PostTransition { get; }
        internal bool PostTransitionEvaluated =>
            PostTransition.Phase == CharacterFootTransitionPhase.PostInterpolation;
        internal bool SameEventContactReentryRefreshed =>
            PreTransition.Reason ==
            CharacterFootTransitionReason.SameEventContactReentryRefresh;
        internal bool SameEventContactReentryUnavailable =>
            PreTransition.Reason == CharacterFootTransitionReason.ContactUnavailable &&
            Request.RequestsLock && Request.EventIdentity != 0 &&
            Request.EventIdentity == PreviousContext.LatestReleasedContactEventIdentity &&
            !PreviousAnchor.Available;
        internal bool RetainedVerifiedAnchor =>
            PreviousAnchor.Available && CurrentAnchor.Available &&
            PreviousAnchor.EventIdentity == CurrentAnchor.EventIdentity &&
            PreTransition.AnchorCommand != CharacterFootAnchorCommand.Create &&
            PreTransition.AnchorCommand != CharacterFootAnchorCommand.Release &&
            PostTransition.AnchorCommand != CharacterFootAnchorCommand.Create &&
            PostTransition.AnchorCommand != CharacterFootAnchorCommand.Release;
        internal bool ReentryInterpolationHistoryRetained =>
            SameEventContactReentryRefreshed && RetainedVerifiedAnchor &&
            !PreTransition.SuppressOutput && !PreTransition.ResetInterpolation &&
            !PostTransition.SuppressOutput && !PostTransition.ResetInterpolation;
        internal bool HardOwnershipLoss =>
            OwnershipLossReason != CharacterFootGoalOwnershipLossReason.None;

        internal static CharacterFootLifecycleTransitionFact Begin(
            in CharacterFootLifecycleContext context,
            in CharacterFootStateFrame frame)
        {
            var history = new CharacterFootContactHistoryFact(
                in context.ContactTransition);
            var anchor = new CharacterFootContactAnchorFact(in context.Contact);
            CharacterFootLockRequest request = frame.LockRequest;
            CharacterFootTransitionDecision decision = default;
            return new CharacterFootLifecycleTransitionFact(
                false,
                in history,
                in history,
                in anchor,
                in anchor,
                in request,
                context.Discrete.LockResponse,
                frame.OwnershipLossReason,
                frame.FootPlacementWeight,
                in decision,
                in decision);
        }

        internal CharacterFootLifecycleTransitionFact Complete(
            in CharacterFootLifecycleContext context,
            in CharacterFootTransitionDecision preTransition,
            in CharacterFootTransitionDecision postTransition)
        {
            var currentContext = new CharacterFootContactHistoryFact(
                in context.ContactTransition);
            var currentAnchor = new CharacterFootContactAnchorFact(
                in context.Contact);
            CharacterFootContactHistoryFact previousContext = PreviousContext;
            CharacterFootContactAnchorFact previousAnchor = PreviousAnchor;
            CharacterFootLockRequest request = Request;
            return new CharacterFootLifecycleTransitionFact(
                true,
                in previousContext,
                in currentContext,
                in previousAnchor,
                in currentAnchor,
                in request,
                LockResponseBefore,
                OwnershipLossReason,
                FormalFootPlacementWeight,
                in preTransition,
                in postTransition);
        }
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

    internal readonly struct CharacterFootCorrectionResponseFact
    {
        internal CharacterFootCorrectionResponseFact(
            bool evaluated,
            bool initializedBefore,
            bool initializedThisFrame,
            CharacterFootCorrectionResponseInitializationReason initializationReason,
            bool previousOutputAvailable,
            Vector3 previousOutputPoint,
            Vector3 desiredOutputPoint,
            Vector3 responseOutputPoint,
            float desiredResponse,
            Vector3 requestedResponseDirection,
            Vector3 previousResponseDirection,
            bool directionLimited,
            float maximumDirectionChangeDegrees,
            float appliedDirectionChangeDegrees,
            bool visibleOutputTransferred,
            float responseBeforeRebase,
            float previousResponse,
            float currentResponse,
            Vector3 responseDirection,
            CharacterFootCorrectionResponseDeltaDirection deltaDirection,
            float selectedSpeed,
            float appliedDelta,
            CharacterFootCorrectionResponseDomain domain,
            CharacterFootCorrectionResponseDomain previousDomain,
            bool domainTransferred)
        {
            Evaluated = evaluated;
            InitializedBefore = initializedBefore;
            InitializedThisFrame = initializedThisFrame;
            InitializationReason = initializationReason;
            PreviousOutputAvailable = previousOutputAvailable;
            PreviousOutputPoint = previousOutputPoint;
            DesiredOutputPoint = desiredOutputPoint;
            ResponseOutputPoint = responseOutputPoint;
            DesiredResponse = desiredResponse;
            RequestedResponseDirection = requestedResponseDirection;
            PreviousResponseDirection = previousResponseDirection;
            DirectionLimited = directionLimited;
            MaximumDirectionChangeDegrees = maximumDirectionChangeDegrees;
            AppliedDirectionChangeDegrees = appliedDirectionChangeDegrees;
            VisibleOutputTransferred = visibleOutputTransferred;
            ResponseBeforeRebase = responseBeforeRebase;
            PreviousResponse = previousResponse;
            CurrentResponse = currentResponse;
            ResponseDirection = responseDirection;
            DeltaDirection = deltaDirection;
            SelectedSpeed = selectedSpeed;
            AppliedDelta = appliedDelta;
            Domain = domain;
            PreviousDomain = previousDomain;
            DomainTransferred = domainTransferred;
        }

        internal bool Evaluated { get; }
        internal bool InitializedBefore { get; }
        internal bool InitializedThisFrame { get; }
        internal CharacterFootCorrectionResponseInitializationReason InitializationReason { get; }
        internal bool PreviousOutputAvailable { get; }
        internal Vector3 PreviousOutputPoint { get; }
        internal Vector3 DesiredOutputPoint { get; }
        internal Vector3 ResponseOutputPoint { get; }
        internal float DesiredResponse { get; }
        internal Vector3 RequestedResponseDirection { get; }
        internal Vector3 PreviousResponseDirection { get; }
        internal bool DirectionLimited { get; }
        internal float MaximumDirectionChangeDegrees { get; }
        internal float AppliedDirectionChangeDegrees { get; }
        internal bool VisibleOutputTransferred { get; }
        internal float ResponseBeforeRebase { get; }
        internal float PreviousResponse { get; }
        internal float CurrentResponse { get; }
        internal Vector3 ResponseDirection { get; }
        internal CharacterFootCorrectionResponseDeltaDirection DeltaDirection
        {
            get;
        }
        internal float SelectedSpeed { get; }
        internal float AppliedDelta { get; }
        internal CharacterFootCorrectionResponseDomain Domain { get; }
        internal CharacterFootCorrectionResponseDomain PreviousDomain { get; }
        internal bool DomainTransferred { get; }
    }

    internal readonly struct CharacterFootPlantInterpolationFact
    {
        internal CharacterFootPlantInterpolationFact(
            bool evaluated,
            ulong eventIdentity,
            bool verified,
            CharacterFootPlantTargetKind targetKind,
            CharacterFootLockResponse lockResponse,
            Vector3 desiredPoint,
            Vector3 filteredPoint,
            CharacterFootTargetHeightAdoptionMode targetHeightAdoptionMode,
            float targetMaximumVerticalSpeed,
            float targetHeightBefore,
            float targetHeightTarget,
            float targetVerticalDelta,
            float targetAppliedVerticalDelta,
            float targetHeightAfter,
            ulong targetHeightEventIdentity,
            CharacterFootPlantTargetHeightUpdateReason targetHeightUpdateReason,
            bool targetForceRefreshed,
            float targetForceRefreshDistance,
            bool targetVerticalClamped,
            Vector3 previousSelectedWorldTarget,
            Vector3 selectedWorldTarget,
            CharacterFootPlantResidualCaptureReason residualCaptureReason,
            Vector3 worldResidualBeforeCapture,
            Vector3 worldResidualCapturedBeforeDecay,
            bool worldResidualDecayApplied,
            float worldResidualBaseHalfLifeSeconds,
            bool worldResidualDeadlineHalfLifeAvailable,
            float worldResidualDeadlineHalfLifeSeconds,
            float worldResidualAppliedHalfLifeSeconds,
            Vector3 worldResidualAfterDecay,
            float worldResidualCompletionTolerance,
            bool worldResidualClearedAtCompletionTolerance,
            CharacterFootVerticalContinuityOwner verticalContinuityOwners,
            Vector3 effectiveCorrectionBefore,
            Vector3 effectiveCorrectionAfter,
            float outputDistance,
            float penetrationDepth)
        {
            Evaluated = evaluated;
            EventIdentity = eventIdentity;
            Verified = verified;
            TargetKind = targetKind;
            LockResponse = lockResponse;
            DesiredPoint = desiredPoint;
            FilteredPoint = filteredPoint;
            TargetHeightAdoptionMode = targetHeightAdoptionMode;
            TargetMaximumVerticalSpeed = targetMaximumVerticalSpeed;
            TargetHeightBefore = targetHeightBefore;
            TargetHeightTarget = targetHeightTarget;
            TargetVerticalDelta = targetVerticalDelta;
            TargetAppliedVerticalDelta = targetAppliedVerticalDelta;
            TargetHeightAfter = targetHeightAfter;
            TargetHeightEventIdentity = targetHeightEventIdentity;
            TargetHeightUpdateReason = targetHeightUpdateReason;
            TargetForceRefreshed = targetForceRefreshed;
            TargetForceRefreshDistance = targetForceRefreshDistance;
            TargetVerticalClamped = targetVerticalClamped;
            PreviousSelectedWorldTarget = previousSelectedWorldTarget;
            SelectedWorldTarget = selectedWorldTarget;
            ResidualCaptureReason = residualCaptureReason;
            WorldResidualBeforeCapture = worldResidualBeforeCapture;
            WorldResidualCapturedBeforeDecay =
                worldResidualCapturedBeforeDecay;
            WorldResidualDecayApplied = worldResidualDecayApplied;
            WorldResidualBaseHalfLifeSeconds =
                worldResidualBaseHalfLifeSeconds;
            WorldResidualDeadlineHalfLifeAvailable =
                worldResidualDeadlineHalfLifeAvailable;
            WorldResidualDeadlineHalfLifeSeconds =
                worldResidualDeadlineHalfLifeSeconds;
            WorldResidualAppliedHalfLifeSeconds =
                worldResidualAppliedHalfLifeSeconds;
            WorldResidualAfterDecay = worldResidualAfterDecay;
            WorldResidualCompletionTolerance =
                worldResidualCompletionTolerance;
            WorldResidualClearedAtCompletionTolerance =
                worldResidualClearedAtCompletionTolerance;
            VerticalContinuityOwners = verticalContinuityOwners;
            EffectiveCorrectionBefore = effectiveCorrectionBefore;
            EffectiveCorrectionAfter = effectiveCorrectionAfter;
            OutputDistance = outputDistance;
            PenetrationDepth = penetrationDepth;
        }

        internal bool Evaluated { get; }
        internal ulong EventIdentity { get; }
        internal bool Verified { get; }
        internal CharacterFootPlantTargetKind TargetKind { get; }
        internal CharacterFootLockResponse LockResponse { get; }
        internal Vector3 DesiredPoint { get; }
        internal Vector3 FilteredPoint { get; }
        internal CharacterFootTargetHeightAdoptionMode TargetHeightAdoptionMode { get; }
        internal float TargetMaximumVerticalSpeed { get; }
        internal float TargetHeightBefore { get; }
        internal float TargetHeightTarget { get; }
        internal float TargetVerticalDelta { get; }
        internal float TargetAppliedVerticalDelta { get; }
        internal float TargetHeightAfter { get; }
        internal ulong TargetHeightEventIdentity { get; }
        internal CharacterFootPlantTargetHeightUpdateReason TargetHeightUpdateReason { get; }
        internal bool TargetForceRefreshed { get; }
        internal float TargetForceRefreshDistance { get; }
        internal bool TargetVerticalClamped { get; }
        internal Vector3 PreviousSelectedWorldTarget { get; }
        internal Vector3 SelectedWorldTarget { get; }
        internal CharacterFootPlantResidualCaptureReason ResidualCaptureReason { get; }
        internal Vector3 WorldResidualBeforeCapture { get; }
        internal Vector3 WorldResidualCapturedBeforeDecay { get; }
        internal bool WorldResidualDecayApplied { get; }
        internal float WorldResidualBaseHalfLifeSeconds { get; }
        internal bool WorldResidualDeadlineHalfLifeAvailable { get; }
        internal float WorldResidualDeadlineHalfLifeSeconds { get; }
        internal float WorldResidualAppliedHalfLifeSeconds { get; }
        internal Vector3 WorldResidualAfterDecay { get; }
        internal float WorldResidualCompletionTolerance { get; }
        internal bool WorldResidualClearedAtCompletionTolerance { get; }
        internal CharacterFootVerticalContinuityOwner VerticalContinuityOwners { get; }
        internal Vector3 EffectiveCorrectionBefore { get; }
        internal Vector3 EffectiveCorrectionAfter { get; }
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
        internal CharacterFootPlantTargetKind PlantTargetKind;
        internal CharacterFootLockResponse PlantLockResponse;
        internal bool PlantTargetVerified;
        internal bool PlantDirectFollow;
        internal Vector3 PlantDesiredPoint;
        internal Vector3 PlantFilteredPoint;
        internal Vector3 PreviousPlantSelectedWorldTarget;
        internal CharacterFootSupportTarget SelectedSupportTarget;
        internal bool HasPreviousResponseOutputPoint;
        internal Vector3 PreviousResponseOutputPoint;
        internal Vector3 PlantWorldResidual;
        internal bool PlantWorldResidualTransitionActive;
        internal bool HasCorrectionResponse;
        internal float CorrectionResponse;
        internal CharacterFootCorrectionResponseDomain CorrectionResponseDomain;
        internal CharacterFootCorrectionResponseFact CorrectionResponseFact;
        internal bool HasCorrectionResponseLineage;
        internal FixedString128Bytes CorrectionResponseSourceLineage;
        internal FixedString128Bytes CorrectionResponseProfileRevision;
        internal ulong CorrectionResponseWorldRevision;
        internal CharacterFootCorrectionResponseInitializationReason
            PendingCorrectionResponseInitializationReason;
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
            CharacterFootSide side,
            CharacterFootPlacementAnimatedFootPose animatedFoot,
            Vector3 animatedHip,
            float legLength,
            in CharacterFootSwingMotionResult swingMotion,
            bool hasContactLanding,
            in CharacterFootGroundPathLanding contactLanding,
            bool preparedPlantActive,
            in CharacterFootGroundPathLanding preparedPlantTarget,
            in CharacterFootCurrentSupportObservation currentSupport,
            bool previousVisibleOutputAvailable,
            Vector3 previousVisibleOutputPoint,
            in CharacterFootLockRequest lockRequest,
            float formalSupport,
            ulong formalSupportEventIdentity,
            CharacterFootGoalOwnershipLossReason ownershipLossReason,
            float footPlacementWeight,
            Vector3 componentUp,
            float deltaSeconds,
            FixedString128Bytes sourceLineage,
            FixedString128Bytes profileRevision,
            ulong worldRevision,
            in CharacterFootMotionSettings settings)
        {
            FrameSequence = frameSequence;
            CompletionIdentity = completionIdentity;
            RigId = rigId;
            RigRevision = rigRevision;
            Side = side;
            AnimatedFoot = animatedFoot;
            AnimatedHip = animatedHip;
            LegLength = legLength;
            SwingMotion = swingMotion;
            HasContactLanding = hasContactLanding;
            ContactLanding = contactLanding;
            PreparedPlantActive = preparedPlantActive;
            PreparedPlantTarget = preparedPlantTarget;
            CurrentSupport = currentSupport;
            PreviousVisibleOutputAvailable = previousVisibleOutputAvailable;
            PreviousVisibleOutputPoint = previousVisibleOutputPoint;
            LockRequest = lockRequest;
            FormalSupport = formalSupport;
            FormalSupportEventIdentity = formalSupportEventIdentity;
            OwnershipLossReason = ownershipLossReason;
            FootPlacementWeight = footPlacementWeight;
            ComponentUp = componentUp;
            DeltaSeconds = deltaSeconds;
            SourceLineage = sourceLineage;
            ProfileRevision = profileRevision;
            WorldRevision = worldRevision;
            Settings = settings;
        }

        internal ulong FrameSequence { get; }
        internal ulong CompletionIdentity { get; }
        internal FixedString64Bytes RigId { get; }
        internal FixedString64Bytes RigRevision { get; }
        internal CharacterFootSide Side { get; }
        internal CharacterFootPlacementAnimatedFootPose AnimatedFoot { get; }
        internal Vector3 AnimatedHip { get; }
        internal float LegLength { get; }
        internal CharacterFootSwingMotionResult SwingMotion { get; }
        internal bool HasContactLanding { get; }
        internal CharacterFootGroundPathLanding ContactLanding { get; }
        internal bool PreparedPlantActive { get; }
        internal CharacterFootGroundPathLanding PreparedPlantTarget { get; }
        internal CharacterFootCurrentSupportObservation CurrentSupport { get; }
        internal bool PreviousVisibleOutputAvailable { get; }
        internal Vector3 PreviousVisibleOutputPoint { get; }
        internal CharacterFootLockRequest LockRequest { get; }
        internal float FormalSupport { get; }
        internal ulong FormalSupportEventIdentity { get; }
        internal CharacterFootGoalOwnershipLossReason OwnershipLossReason { get; }
        internal bool HardOwnershipLoss =>
            OwnershipLossReason != CharacterFootGoalOwnershipLossReason.None;
        internal float FootPlacementWeight { get; }
        internal Vector3 ComponentUp { get; }
        internal float DeltaSeconds { get; }
        internal FixedString128Bytes SourceLineage { get; }
        internal FixedString128Bytes ProfileRevision { get; }
        internal ulong WorldRevision { get; }
        internal CharacterFootMotionSettings Settings { get; }
    }

    internal readonly struct CharacterFootStateEvaluation
    {
        internal CharacterFootStateEvaluation(
            CharacterFootSide side,
            in AnimationFootMotionRuntimeSample formalFootMotion,
            in CharacterFootLandingPredictionResult landingPrediction,
            in CharacterFootStateFrame frame,
            in AnimationFootMotionRuntimeSample selectedFootMotion,
            bool grounded,
            Transform goalRoot)
        {
            Side = side;
            FormalFootMotion = formalFootMotion;
            LandingPrediction = landingPrediction;
            Frame = frame;
            SelectedFootMotion = selectedFootMotion;
            Grounded = grounded;
            GoalRoot = goalRoot;
        }

        internal CharacterFootSide Side { get; }
        internal AnimationFootMotionRuntimeSample FormalFootMotion { get; }
        internal CharacterFootLandingPredictionResult LandingPrediction { get; }
        internal CharacterFootStateFrame Frame { get; }
        internal AnimationFootMotionRuntimeSample SelectedFootMotion { get; }
        internal bool Grounded { get; }
        internal Transform GoalRoot { get; }
    }

    internal readonly struct CharacterFootLifecycleEvaluationReceipt
    {
        internal CharacterFootLifecycleEvaluationReceipt(
            in CharacterFootStateEvaluation evaluation,
            in CharacterFootTransitionDecision preTransition,
            in CharacterFootStateTarget target,
            in CharacterFootInterpolationResult interpolation,
            in CharacterFootSwingMotionResult outputSwing,
            in CharacterFootPlacementRequest request,
            in CharacterFootSwingMotionResult preliminaryMotion,
            in CharacterFootLifecycleTransitionFact lifecycleTransition,
            bool landingCompletionPending)
        {
            Evaluation = evaluation;
            PreTransition = preTransition;
            Target = target;
            Interpolation = interpolation;
            OutputSwing = outputSwing;
            Request = request;
            PreliminaryMotion = preliminaryMotion;
            LifecycleTransition = lifecycleTransition;
            LandingCompletionPending = landingCompletionPending;
        }

        internal CharacterFootStateEvaluation Evaluation { get; }
        internal CharacterFootTransitionDecision PreTransition { get; }
        internal CharacterFootStateTarget Target { get; }
        internal CharacterFootInterpolationResult Interpolation { get; }
        internal CharacterFootSwingMotionResult OutputSwing { get; }
        internal CharacterFootPlacementRequest Request { get; }
        internal CharacterFootSwingMotionResult PreliminaryMotion { get; }
        internal CharacterFootLifecycleTransitionFact LifecycleTransition { get; }
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
            CharacterFootPlantTargetKind plantTargetKind,
            CharacterFootLockResponse plantLockResponse,
            bool lockWeightCompleted,
            bool supportTargetAvailable,
            in CharacterFootSupportTarget supportTarget,
            bool stateEntered,
            bool responseEntered,
            bool directPlantFollow,
            bool suppressOutput,
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
            PlantTargetKind = plantTargetKind;
            PlantLockResponse = plantLockResponse;
            LockWeightCompleted = lockWeightCompleted;
            SupportTargetAvailable = supportTargetAvailable;
            SupportTarget = supportTarget;
            StateEntered = stateEntered;
            ResponseEntered = responseEntered;
            DirectPlantFollow = directPlantFollow;
            SuppressOutput = suppressOutput;
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
        internal CharacterFootPlantTargetKind PlantTargetKind { get; }
        internal CharacterFootLockResponse PlantLockResponse { get; }
        internal bool LockWeightCompleted { get; }
        internal bool SupportTargetAvailable { get; }
        internal CharacterFootSupportTarget SupportTarget { get; }
        internal bool StateEntered { get; }
        internal bool ResponseEntered { get; }
        internal bool DirectPlantFollow { get; }
        internal bool SuppressOutput { get; }
        internal float TimeToLandingSeconds { get; }
        internal CharacterFootSupportIntent SupportIntent { get; }
    }

    internal readonly struct CharacterFootInterpolationResult
    {
        internal CharacterFootInterpolationResult(
            Vector3 correction,
            bool completed,
            in CharacterFootSupportTarget supportTarget,
            in CharacterFootPathContinuityFact continuityFact,
            in CharacterFootPlantInterpolationFact plantFact,
            in CharacterFootCorrectionResponseFact correctionResponseFact)
        {
            Correction = correction;
            Completed = completed;
            SupportTarget = supportTarget;
            ContinuityFact = continuityFact;
            PlantFact = plantFact;
            CorrectionResponseFact = correctionResponseFact;
        }

        internal Vector3 Correction { get; }
        internal bool Completed { get; }
        internal CharacterFootSupportTarget SupportTarget { get; }
        internal CharacterFootPathContinuityFact ContinuityFact { get; }
        internal CharacterFootPlantInterpolationFact PlantFact { get; }
        internal CharacterFootCorrectionResponseFact CorrectionResponseFact
        {
            get;
        }
    }
}
