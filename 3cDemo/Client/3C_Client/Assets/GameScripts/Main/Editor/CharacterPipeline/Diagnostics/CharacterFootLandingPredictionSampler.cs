using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal enum CharacterFootSwingPathHorizontalAxisState
    {
        Unavailable = 0,
        Available = 1,
        InvalidComponentUp = 2,
        DegenerateAxis = 3
    }

    internal enum CharacterFootActualEnvelopeIntersectionState
    {
        Unavailable = 0,
        InvalidComponentUp = 1,
        DegenerateAxis = 2,
        NoIntersection = 3,
        Unique = 4,
        AmbiguousEnvelopeAtActualFootDistance = 5
    }

    internal enum CharacterFootActualFootAxisRegion
    {
        Unavailable = 0,
        BeforePathStart = 1,
        WithinPathSegment = 2,
        AfterPathEnd = 3
    }

    internal enum CharacterFootActualEnvelopeCounterfactualState
    {
        Unavailable = 0,
        UniqueInCorridor = 1,
        AmbiguousInCorridor = 2,
        OutsideGroundPathCorridor = 3,
        NoIntersection = 4
    }

    internal sealed class CharacterFootActualEnvelopeIntersectionFact
    {
        internal CharacterFootActualEnvelopeIntersectionState State;
        internal float ActualFootHorizontalDistance;
        internal float BaselineHorizontalDistance;
        internal float EnvelopeHorizontalDistance;
        internal CharacterFootActualFootAxisRegion AxisRegion;
        internal float ClosestPathParameter;
        internal float DistanceAlongAxis;
        internal float CrossTrackDistance;
        internal float CorridorRadius;
        internal bool WithinGroundPathCorridor;
        internal int CandidateCount;
        internal float MinimumHeightAlongUp;
        internal float MaximumHeightAlongUp;
        internal float HeightSpan;
        internal bool HasVerticalEdge;
        internal bool HasMultipleHeights;
        internal bool Ambiguous;
        internal CharacterFootActualEnvelopeCounterfactualState
            CounterfactualState;
    }

    [InitializeOnLoad]
    public static class CharacterFootLandingPredictionSampler
    {
        const int MaximumPendingFrameCount = 256;
        const int MaximumQueuedFrameCount = 256;
        const double SamplingStartTimeoutSeconds = 30d;
        const double SamplingFlushIntervalSeconds = 0.5d;
        const float ActualEnvelopeHorizontalEpsilonMeters = 0.001f;
        const float ActualEnvelopeHeightEpsilonMeters = 0.001f;
        const string GameplayLabPlayerActorId = "gameplay-lab-player";
        const string StartMenu =
            "Tools/3C/Diagnostics/Foot Landing Sampling/Start";
        const string StopMenu =
            "Tools/3C/Diagnostics/Foot Landing Sampling/Stop and Save";
        const string GeometryFileName = "ground-path-geometry.csv";
        static readonly string Header =
            "SampleIdentity,SampleStartedUtc,ProgramIdentity,ProjectionRevision,PoseGraphId,PoseGraphRevision,PosePlanHash," +
            "FrameSequence,CompletionIdentity,TargetRuntimeInstanceId,TargetHostInstanceId,RootInstanceId,FootProfileId,FootProfileRevision,Side,State,RejectReason,StepSource," +
            "LandingEventIdentity,TrajectoryGeneration,LandingConfidence,TimeToLandingSeconds," +
            "NextLandingTrackingState,NextLandingTrackingEventIdentity,VerifiedLastLandingAvailable,VerifiedLastLandingEventIdentity," +
            "PlantTargetState,PlantTargetAvailable,PlantTargetEventIdentity,PlantTargetSurfaceIdentity,PlantTargetPointX,PlantTargetPointY,PlantTargetPointZ,PlantTargetNormalX,PlantTargetNormalY,PlantTargetNormalZ,PlantTargetTrajectoryGeneration,PlantTargetFutureBodyTranslationSourceIdentity,PlantTargetUpdated,PlantVerificationAttempted,PlantVerificationUnavailable,ApproachPlantTargetPrepared," +
            "StepSelectionMaximumPredictionTimeSeconds,StepSelectionLastLandingEventIdentity,SelectedStepSource,SelectedLandingEventIdentity," +
            "SelectedStepEventPhase,SelectedStepApproachContactToLandingProgress,SelectedStepLandingPhase,SelectedStepAtOrAfterApproachContact,SelectedStepInApproachContactToLanding," +
            "CurrentStepIsValid,CurrentStepIsAuthoritative,CurrentStepHasConsistentLandingEventIdentity,CurrentStepIsPreSwing,CurrentStepIsSwing," +
            "CurrentStepEventOrdinal,CurrentStepSourceLandingCycleOffset,CurrentStepSourceSampleCycle,CurrentStepContributionContinuityIdentity,CurrentStepLandingEventIdentity,CurrentStepTimeToLandingSeconds," +
            "CurrentStepEventPhase,CurrentStepApproachContactToLandingProgress,CurrentStepLandingPhase,CurrentStepAtOrAfterApproachContact,CurrentStepInApproachContactToLanding," +
            "CurrentStepRootLocalLandingX,CurrentStepRootLocalLandingY,CurrentStepRootLocalLandingZ," +
            "IncomingStepIsValid,IncomingStepIsAuthoritative,IncomingStepHasConsistentLandingEventIdentity,IncomingStepIsPreSwing,IncomingStepIsSwing," +
            "IncomingStepEventOrdinal,IncomingStepSourceLandingCycleOffset,IncomingStepSourceSampleCycle,IncomingStepContributionContinuityIdentity,IncomingStepLandingEventIdentity,IncomingStepTimeToLandingSeconds," +
            "IncomingStepEventPhase,IncomingStepApproachContactToLandingProgress,IncomingStepLandingPhase,IncomingStepAtOrAfterApproachContact,IncomingStepInApproachContactToLanding," +
            "IncomingStepRootLocalLandingX,IncomingStepRootLocalLandingY,IncomingStepRootLocalLandingZ," +
            "FormalStepObservationAvailable,FormalStepSourceIdentity,FormalStepSourceWeight,FormalStepSourceNormalizedTime,FormalStepTimeSeconds,FormalStepDistance," +
            "FormalFootHeight,FormalToeHeight,FormalToeSpeed,FormalPositionError,FormalRotationError," +
            "FormalContact,FormalLockMode,FormalLockWeight,FormalSupport," +
            "FormalEventPhase,FormalEventApproachContactToLandingProgress,FormalEventTimeToLandingSeconds,FormalInApproachContactToLanding," +
            "FormalCurrentContactEventAvailable,FormalCurrentContactEventIdentity,FormalCurrentContactEventOrdinal,FormalCurrentContactEventCycle,FormalCurrentContactEventDistance,FormalCurrentContactRootLocalLandingX,FormalCurrentContactRootLocalLandingY,FormalCurrentContactRootLocalLandingZ," +
            "FormalNextLandingEventAvailable,FormalNextLandingEventIdentity,FormalNextLandingEventOrdinal,FormalNextLandingEventCycle,FormalNextLandingEventDistance,FormalNextRootLocalLandingX,FormalNextRootLocalLandingY,FormalNextRootLocalLandingZ," +
            "InputFormalStepObservationAvailable,InputFormalStepSourceId,InputFormalStepSourceIdentity,InputFormalStepSourceWeight,InputFormalStepSourceNormalizedTime," +
            "InputFormalStepClipBindingIndex,InputFormalStepSourceCycle,InputFormalStepContributionContinuityIdentity,InputFormalStepCompletionIdentity,InputFormalStepTimeSeconds,InputFormalStepDistance," +
            "InputFormalFootHeight,InputFormalToeHeight,InputFormalToeSpeed,InputFormalPositionError,InputFormalRotationError," +
            "InputFormalContact,InputFormalLockMode,InputFormalLockWeight,InputFormalSupport," +
            "InputFormalEventPhase,InputFormalEventApproachContactToLandingProgress,InputFormalEventTimeToLandingSeconds,InputFormalInApproachContactToLanding," +
            "InputFormalCurrentContactEventAvailable,InputFormalCurrentContactEventIdentity,InputFormalCurrentContactEventOrdinal,InputFormalCurrentContactEventCycle,InputFormalCurrentContactEventDistance,InputFormalCurrentContactRootLocalLandingX,InputFormalCurrentContactRootLocalLandingY,InputFormalCurrentContactRootLocalLandingZ," +
            "InputFormalNextLandingEventAvailable,InputFormalNextLandingEventIdentity,InputFormalNextLandingEventOrdinal,InputFormalNextLandingEventCycle,InputFormalNextLandingEventDistance,InputFormalNextRootLocalLandingX,InputFormalNextRootLocalLandingY,InputFormalNextRootLocalLandingZ," +
            "RootLocalLandingX,RootLocalLandingY,RootLocalLandingZ," +
            "PresentationDeltaSeconds,PreviousBodyTick,CurrentBodyTick,BodySampleAlpha,BodySampleAgeSeconds," +
            "MotionTimelineAvailable,TimelineGeneration,TimelineAuthorityTick,TimelineTickRate," +
            "TimelineCurrentVelocityX,TimelineCurrentVelocityZ,TimelineContinuationVelocityX,TimelineContinuationVelocityZ," +
            "TimelineHasContinuation,TimelineBodyYawVelocityDegreesPerSecond,TimelineMaximumBodyYawVelocityDegreesPerSecond,CurrentSegmentRemainingSeconds," +
            "PredictionMotionAvailable,PredictionMotionRejectReason,PredictionMotionResetReason,PredictionMotionSourceIdentity," +
            "PredictionRawCurrentVelocityX,PredictionRawCurrentVelocityZ,PredictionRawContinuationVelocityX,PredictionRawContinuationVelocityZ," +
            "PredictionPreviousStableCurrentVelocityX,PredictionPreviousStableCurrentVelocityZ,PredictionPreviousStableContinuationVelocityX,PredictionPreviousStableContinuationVelocityZ," +
            "PredictionStableCurrentVelocityX,PredictionStableCurrentVelocityZ,PredictionStableContinuationVelocityX,PredictionStableContinuationVelocityZ," +
            "PredictionCurrentVelocityDeltaX,PredictionCurrentVelocityDeltaZ,PredictionContinuationVelocityDeltaX,PredictionContinuationVelocityDeltaZ," +
            "PredictionVelocityResponseAlpha,PredictionVelocityDeltaThreshold,PredictionVelocitySmoothSpeed,PredictionMaximumSpeed," +
            "PredictionCurrentResponseApplied,PredictionContinuationResponseApplied,PredictionCurrentMaximumSpeedClamped,PredictionContinuationMaximumSpeedClamped,PredictionMotionRevision," +
            "Grounded,HorizontalSpeed,LeftActionInstanceIdentity,LeftActionFootWeight,RightActionInstanceIdentity,RightActionFootWeight," +
            "PrimarySupportHasValue,PrimarySupportSide,PrimarySupportLandingEventIdentity,PrimarySupportRetained," +
            "LogicRootPositionX,LogicRootPositionY,LogicRootPositionZ,LogicRootRotationX,LogicRootRotationY,LogicRootRotationZ,LogicRootRotationW," +
            "VisualRootLocalPositionX,VisualRootLocalPositionY,VisualRootLocalPositionZ,VisualRootLocalRotationX,VisualRootLocalRotationY,VisualRootLocalRotationZ,VisualRootLocalRotationW," +
            "VisualRootWorldPositionX,VisualRootWorldPositionY,VisualRootWorldPositionZ,VisualRootWorldRotationX,VisualRootWorldRotationY,VisualRootWorldRotationZ,VisualRootWorldRotationW," +
            "PoseRootLocalPositionX,PoseRootLocalPositionY,PoseRootLocalPositionZ,PoseRootLocalRotationX,PoseRootLocalRotationY,PoseRootLocalRotationZ,PoseRootLocalRotationW," +
            "PoseRootWorldPositionX,PoseRootWorldPositionY,PoseRootWorldPositionZ,PoseRootWorldRotationX,PoseRootWorldRotationY,PoseRootWorldRotationZ,PoseRootWorldRotationW," +
            "VisibleBodyPositionX,VisibleBodyPositionY,VisibleBodyPositionZ," +
            "VisibleBodyRotationX,VisibleBodyRotationY,VisibleBodyRotationZ,VisibleBodyRotationW," +
            "VisibleBodyVelocityX,VisibleBodyVelocityY,VisibleBodyVelocityZ,VisibleBodyYawVelocityDegreesPerSecond," +
            "TargetBodyPositionX,TargetBodyPositionY,TargetBodyPositionZ," +
            "TargetBodyRotationX,TargetBodyRotationY,TargetBodyRotationZ,TargetBodyRotationW," +
            "TargetBodyVelocityX,TargetBodyVelocityY,TargetBodyVelocityZ,TargetBodyYawVelocityDegreesPerSecond," +
            "BodyPositionError,BodyRotationError," +
            "CorrectionPositionErrorX,CorrectionPositionErrorY,CorrectionPositionErrorZ," +
            "CorrectionPositionVelocityX,CorrectionPositionVelocityY,CorrectionPositionVelocityZ," +
            "CorrectionYawVelocityDegreesPerSecond,CorrectionActive,CorrectionClamped,CorrectionSettled,BodyResetSequence," +
            "FutureBodyTranslationAvailable,FutureBodyRelativeTranslationX,FutureBodyRelativeTranslationY,FutureBodyRelativeTranslationZ," +
            "FutureBodyTranslationVelocityX,FutureBodyTranslationVelocityY,FutureBodyTranslationVelocityZ," +
            "CurrentAnimatedSoleX,CurrentAnimatedSoleY,CurrentAnimatedSoleZ," +
            "RawLandingAvailable,RawLandingCandidateX,RawLandingCandidateY,RawLandingCandidateZ," +
            "LandingObservationIdentity,LandingObservationWorldRevision,LandingObservationSourceSampleIdentity,LandingObservationSourceSampleCycle," +
            "LandingObservationCacheState,LandingObservationQueryExecuted,LandingObservationQueryPurpose,LandingObservationRefreshMode,LandingObservationQueryReason," +
            "LandingObservationCanonicalRawX,LandingObservationCanonicalRawY,LandingObservationCanonicalRawZ," +
            "LandingObservationCanonicalComponentUpX,LandingObservationCanonicalComponentUpY,LandingObservationCanonicalComponentUpZ," +
            "LandingObservationCandidateRawX,LandingObservationCandidateRawY,LandingObservationCandidateRawZ," +
            "LandingObservationCandidateComponentUpX,LandingObservationCandidateComponentUpY,LandingObservationCandidateComponentUpZ," +
            "LandingObservationQueryInputDistance,LandingObservationQueryComponentUpAngleDegrees," +
            "LandingObservationPredictionInputAccumulationDistance,LandingObservationComponentUpChangeAngleDegrees," +
            "QueryShape,QueryPurpose,QueryFootIndex,QueryOriginX,QueryOriginY,QueryOriginZ," +
            "QueryDirectionX,QueryDirectionY,QueryDirectionZ,QueryMaximumDistance,QueryRadius,QueryLayerMask,QueryMinimumGroundNormalDot," +
            "QueryCandidateSelectionState,QueryValidCandidateCount," +
            "QuerySelectedCandidateAvailable,QuerySelectedSurfaceIdentity,QuerySelectedPointX,QuerySelectedPointY,QuerySelectedPointZ,QuerySelectedDistance," +
            "Accepted,SurfaceIdentity,LandingPointX,LandingPointY,LandingPointZ," +
            "LandingNormalX,LandingNormalY,LandingNormalZ,QueryDistance," +
            "GroundPathState,GroundPathRejectReason,GroundPathInputIdentity,GroundPathQueryExecuted,GroundPathTargetAvailable," +
            "GroundPathLastLandingEventIdentity,GroundPathNextSwingLandingEventIdentity,GroundPathTrajectoryGeneration,GroundPathAuthorityTick," +
            "GroundPathLastFutureBodyTranslationSourceIdentity,GroundPathNextSwingFutureBodyTranslationSourceIdentity," +
            "GroundPathLastLandingX,GroundPathLastLandingY,GroundPathLastLandingZ," +
            "GroundPathNextSwingLandingX,GroundPathNextSwingLandingY,GroundPathNextSwingLandingZ," +
            "GroundPathLastLandingNormalX,GroundPathLastLandingNormalY,GroundPathLastLandingNormalZ," +
            "GroundPathNextSwingLandingNormalX,GroundPathNextSwingLandingNormalY,GroundPathNextSwingLandingNormalZ," +
            "GroundPathLastLandingSurfaceIdentity,GroundPathNextSwingLandingSurfaceIdentity," +
            "GroundPathComponentUpX,GroundPathComponentUpY,GroundPathComponentUpZ," +
            "GroundPathAxisStartX,GroundPathAxisStartY,GroundPathAxisStartZ," +
            "GroundPathAxisEndX,GroundPathAxisEndY,GroundPathAxisEndZ," +
            "GroundPathRadius,GroundPathMaximumAxisSegmentLength,GroundPathDirectionX,GroundPathDirectionY,GroundPathDirectionZ," +
            "GroundPathMaximumDistance,GroundPathLayerMask,GroundPathSegmentHitCapacity,GroundPathContactCapacity,GroundPathSegmentCount,GroundPathContactCount," +
            "GroundPathEdgeCount,GroundPathHasInvalidSegment,GroundPathFirstInvalidSegmentIndex,GroundPathFirstInvalidSegmentIdentity," +
            "GroundPathFirstInvalidSegmentBottomX,GroundPathFirstInvalidSegmentBottomY,GroundPathFirstInvalidSegmentBottomZ," +
            "GroundPathFirstInvalidSegmentTopX,GroundPathFirstInvalidSegmentTopY,GroundPathFirstInvalidSegmentTopZ," +
            "GroundPathFirstInvalidSegmentVerticalDistance,GroundPathMaximumReachableVerticalEdge,GroundEnvelopeVertexCount," +
            "FootMotionState,FootMotionRejectReason,FootMotionLandingEventIdentity,FootMotionGroundPathInputIdentity," +
            "FootMotionDistance,FootMotionProgress," +
            "FootMotionOriginalSoleX,FootMotionOriginalSoleY,FootMotionOriginalSoleZ," +
            "FootMotionOriginalAnkleX,FootMotionOriginalAnkleY,FootMotionOriginalAnkleZ," +
            "FootMotionSourceAnkleRotationX,FootMotionSourceAnkleRotationY,FootMotionSourceAnkleRotationZ,FootMotionSourceAnkleRotationW," +
            "FootMotionSourceHeelX,FootMotionSourceHeelY,FootMotionSourceHeelZ," +
            "FootMotionSourceToeX,FootMotionSourceToeY,FootMotionSourceToeZ," +
            "FootMotionBaselineSampleX,FootMotionBaselineSampleY,FootMotionBaselineSampleZ,FootMotionBaselineSampleAlongUp," +
            "FootMotionEnvelopeSampleX,FootMotionEnvelopeSampleY,FootMotionEnvelopeSampleZ,FootMotionEnvelopeSampleAlongUp," +
            "FootMotionFormalFootHeight,FootMotionRawFormalTargetHeight,FootMotionEnvelopeMinimumCorrection,FootMotionBuilderSelectedCorrection," +
            "FootMotionBuilderSwingTargetAvailable,FootMotionBuilderSwingTargetCorrectionX,FootMotionBuilderSwingTargetCorrectionY,FootMotionBuilderSwingTargetCorrectionZ," +
            "FootMotionSwingPathHorizontalAxisState,FootMotionActualFootHorizontalDistanceMeters,FootMotionBaselineHorizontalDistanceMeters," +
            "FootMotionEnvelopeHorizontalDistanceMeters,FootMotionActualMinusEnvelopeHorizontalDistanceMeters," +
            "FootMotionActualFootAxisRegion,FootMotionActualFootClosestPathParameter,FootMotionActualFootDistanceAlongAxisMeters," +
            "FootMotionActualFootCrossTrackDistanceMeters,FootMotionActualFootGroundPathCorridorRadiusMeters,FootMotionActualFootWithinGroundPathCorridor," +
            "FootMotionActualEnvelopeIntersectionState,FootMotionActualEnvelopeCandidateCount," +
            "FootMotionActualEnvelopeMinimumHeightAlongUp,FootMotionActualEnvelopeMaximumHeightAlongUp,FootMotionActualEnvelopeHeightSpan," +
            "FootMotionActualEnvelopeHasVerticalEdge,FootMotionActualEnvelopeHasMultipleHeights,FootMotionActualEnvelopeAmbiguous," +
            "FootMotionActualEnvelopeCounterfactualState," +
            "FootMotionActualProgressEnvelopeCorrectionAvailable,FootMotionActualProgressEnvelopeMinimumCorrection," +
            "FootMotionActualProgressEnvelopeAdvanceAboveBuilderTarget," +
            "FootMotionLandingPredictionError," +
            "FootMotionCorrectedSoleX,FootMotionCorrectedSoleY,FootMotionCorrectedSoleZ," +
            "FootMotionCorrectedAnkleX,FootMotionCorrectedAnkleY,FootMotionCorrectedAnkleZ,FootMotionPositionWeight,FootMotionRotationWeight," +
            "FootMotionConstraintState,FootMotionLockResponse,FootMotionSupportHorizontalError," +
            "FootMotionContactOwnership,FootMotionSupportWeight," +
            "FootMotionLandingReachEvaluated,FootMotionLandingReachAvailable,FootMotionLandingReachGoalClamped,FootMotionLandingReachGoalClampDistance," +
            "FootMotionSupportContactAnchorX,FootMotionSupportContactAnchorY,FootMotionSupportContactAnchorZ," +
            "FootMotionContactPlaneAvailable,FootMotionContactSurfaceIdentity," +
            "FootMotionContactPlaneNormalX,FootMotionContactPlaneNormalY,FootMotionContactPlaneNormalZ," +
            "FootContactPlanePenetrationAvailability," +
            "FootMotionDesiredCorrectionX,FootMotionDesiredCorrectionY,FootMotionDesiredCorrectionZ," +
            "FootMotionPathContinuityEvaluated,FootMotionPathRevisionReason,FootMotionPathResidualRebuilt,FootMotionTargetTrackingApplied," +
            "FootMotionPathAvailableBefore,FootMotionPathAvailableAfter,FootMotionPathPreviousLandingEventIdentity,FootMotionPathCurrentLandingEventIdentity," +
            "FootMotionPathPreviousTargetCorrectionX,FootMotionPathPreviousTargetCorrectionY,FootMotionPathPreviousTargetCorrectionZ," +
            "FootMotionPathCurrentTargetCorrectionX,FootMotionPathCurrentTargetCorrectionY,FootMotionPathCurrentTargetCorrectionZ," +
            "FootMotionPathLandingPointDeltaMeters,FootMotionPathTargetDeltaMeters," +
            "FootMotionSwingResidualBeforeRevisionX,FootMotionSwingResidualBeforeRevisionY,FootMotionSwingResidualBeforeRevisionZ," +
            "FootMotionSwingResidualBeforeDecayX,FootMotionSwingResidualBeforeDecayY,FootMotionSwingResidualBeforeDecayZ," +
            "FootMotionSwingResidualAfterDecayX,FootMotionSwingResidualAfterDecayY,FootMotionSwingResidualAfterDecayZ," +
            "FootMotionResidualOutputCorrectionX,FootMotionResidualOutputCorrectionY,FootMotionResidualOutputCorrectionZ," +
            "FootMotionLandingAcceptanceDistance,FootMotionPathRevisionDistance,FootMotionSwingResidualTolerance," +
            "FootMotionResidualTimeToLandingSeconds,FootMotionResidualBaseHalfLifeSeconds," +
            "FootMotionResidualDeadlineHalfLifeAvailable,FootMotionResidualDeadlineHalfLifeSeconds,FootMotionResidualAppliedHalfLifeSeconds," +
            "FootMotionSwingTargetHeightAdoptionMode,FootMotionSwingRawTargetHeightAlongUp,FootMotionSwingFilteredTargetHeightBefore,FootMotionSwingTargetHeightDelta," +
            "FootMotionSwingTargetHeightAppliedDelta,FootMotionSwingTargetHeightUpdateHeld,FootMotionSwingTargetHeightForceRefreshed,FootMotionSwingTargetHeightRateLimited,FootMotionSwingTargetHeightClamped,FootMotionSwingTargetHeightForceRefreshDistance,FootMotionSwingTargetMaximumVerticalSpeed," +
            "FootMotionSwingFilteredTargetHeightAlongUp,FootMotionTargetHeightComponentUpX,FootMotionTargetHeightComponentUpY,FootMotionTargetHeightComponentUpZ," +
            "FootMotionPreTransitionReason,FootMotionPreTransitionSource,FootMotionPreTransitionTarget,FootMotionPreTransitionAnchorCommand," +
            "FootMotionPostTransitionReason,FootMotionPostTransitionSource,FootMotionPostTransitionTarget,FootMotionPostTransitionAnchorCommand," +
            "FootMotionStateTargetCorrectionX,FootMotionStateTargetCorrectionY,FootMotionStateTargetCorrectionZ,FootMotionInterpolationPolicy," +
            "FootMotionInterpolationOutputCorrectionX,FootMotionInterpolationOutputCorrectionY,FootMotionInterpolationOutputCorrectionZ,FootMotionInterpolationCompleted," +
            "FootMotionConstraintStateBefore,FootMotionLockResponseBefore,FootMotionOutputStagesAvailable,FootMotionReleasingCompletedToSwing,FootMotionSafetyFloorAvailable," +
            "FootMotionSafetyFloorOwner,FootMotionSafetyFloorOwnerSurfaceIdentity,FootMotionSafetyFloorOwnerPathIdentity," +
            "FootMotionCorrectionBeforeSafetyFloorX,FootMotionCorrectionBeforeSafetyFloorY,FootMotionCorrectionBeforeSafetyFloorZ," +
            "FootMotionSafetyFloorMinimumCorrectionX,FootMotionSafetyFloorMinimumCorrectionY,FootMotionSafetyFloorMinimumCorrectionZ," +
            "FootMotionSafetyFloorOutputCorrectionX,FootMotionSafetyFloorOutputCorrectionY,FootMotionSafetyFloorOutputCorrectionZ," +
            "FootMotionFinalEffectiveCorrectionX,FootMotionFinalEffectiveCorrectionY,FootMotionFinalEffectiveCorrectionZ," +
            "FootMotionSafetyFloorClamped,FootMotionSafetyFloorClampMeters,FootMotionSafetyFloorClearanceBeforeMeters,FootMotionSafetyFloorClearanceAfterMeters," +
            "FootMotionPlantInterpolationEvaluated,FootMotionPlantTargetEventIdentity,FootMotionPlantTargetVerified,FootMotionPlantTargetKind,FootMotionPlantLockResponse,FootMotionPlantLockWeightCompleted," +
            "FootMotionPlantDesiredPointX,FootMotionPlantDesiredPointY,FootMotionPlantDesiredPointZ," +
            "FootMotionPlantFilteredPointX,FootMotionPlantFilteredPointY,FootMotionPlantFilteredPointZ," +
            "FootMotionSelectedSupportTargetAvailable,FootMotionSelectedSupportTargetFrameSequence,FootMotionSelectedSupportTargetCompletionIdentity,FootMotionSelectedSupportTargetSide,FootMotionSelectedSupportTargetPositionX,FootMotionSelectedSupportTargetPositionY,FootMotionSelectedSupportTargetPositionZ,FootMotionSelectedSupportTargetNormalX,FootMotionSelectedSupportTargetNormalY,FootMotionSelectedSupportTargetNormalZ,FootMotionSelectedSupportTargetSurfaceIdentity,FootMotionSelectedSupportTargetWorldRevision,FootMotionSelectedSupportTargetKind,FootMotionSelectedSupportTargetPositionSource,FootMotionSelectedSupportTargetPositionFrameSequence,FootMotionSelectedSupportTargetPositionCompletionIdentity,FootMotionSelectedSupportTargetPositionEventIdentity,FootMotionSelectedSupportTargetPositionPathIdentity,FootMotionSelectedSupportTargetNormalSource,FootMotionSelectedSupportTargetNormalFrameSequence,FootMotionSelectedSupportTargetNormalCompletionIdentity,FootMotionSelectedSupportTargetNormalEventIdentity,FootMotionSelectedSupportTargetCurrentSupportProbeKind," +
            "FootMotionPlantTargetHeightAdoptionMode,FootMotionPlantTargetMaximumVerticalSpeed," +
            "FootMotionPlantTargetHeightBefore,FootMotionPlantTargetHeightTarget,FootMotionPlantTargetVerticalDelta,FootMotionPlantTargetAppliedVerticalDelta,FootMotionPlantTargetHeightAfter,FootMotionPlantTargetHeightEventIdentity,FootMotionPlantTargetHeightUpdateReason,FootMotionPlantTargetForceRefreshed,FootMotionPlantTargetForceRefreshDistance,FootMotionPlantTargetVerticalClamped," +
            "FootMotionPlantPreviousSelectedWorldTargetX,FootMotionPlantPreviousSelectedWorldTargetY,FootMotionPlantPreviousSelectedWorldTargetZ," +
            "FootMotionPlantSelectedWorldTargetX,FootMotionPlantSelectedWorldTargetY,FootMotionPlantSelectedWorldTargetZ," +
            "FootMotionPreviousResponseOutputAvailable,FootMotionPreviousResponseOutputPointX,FootMotionPreviousResponseOutputPointY,FootMotionPreviousResponseOutputPointZ," +
            "FootMotionDesiredOutputPointX,FootMotionDesiredOutputPointY,FootMotionDesiredOutputPointZ," +
            "FootMotionResponseOutputPointX,FootMotionResponseOutputPointY,FootMotionResponseOutputPointZ,FootMotionPlantResidualCaptureReason," +
            "FootMotionPlantWorldResidualBeforeCaptureX,FootMotionPlantWorldResidualBeforeCaptureY,FootMotionPlantWorldResidualBeforeCaptureZ," +
            "FootMotionPlantWorldResidualCapturedBeforeDecayX,FootMotionPlantWorldResidualCapturedBeforeDecayY,FootMotionPlantWorldResidualCapturedBeforeDecayZ," +
            "FootMotionPlantWorldResidualDecayApplied,FootMotionPlantWorldResidualBaseHalfLifeSeconds,FootMotionPlantWorldResidualDeadlineHalfLifeAvailable,FootMotionPlantWorldResidualDeadlineHalfLifeSeconds,FootMotionPlantWorldResidualAppliedHalfLifeSeconds," +
            "FootMotionPlantWorldResidualAfterDecayX,FootMotionPlantWorldResidualAfterDecayY,FootMotionPlantWorldResidualAfterDecayZ," +
            "FootMotionPlantWorldResidualCompletionTolerance,FootMotionPlantWorldResidualClearedAtCompletionTolerance," +
            "FootMotionCorrectionResponseEvaluated,FootMotionCorrectionResponseInitializedBefore,FootMotionCorrectionResponseInitializedThisFrame,FootMotionCorrectionResponseInitializationReason," +
            "FootMotionCorrectionResponseDesired,FootMotionCorrectionResponseRequestedDirectionX,FootMotionCorrectionResponseRequestedDirectionY,FootMotionCorrectionResponseRequestedDirectionZ,FootMotionCorrectionResponsePreviousDirectionX,FootMotionCorrectionResponsePreviousDirectionY,FootMotionCorrectionResponsePreviousDirectionZ,FootMotionCorrectionResponseDirectionLimited,FootMotionCorrectionResponseMaximumDirectionChangeDegrees,FootMotionCorrectionResponseAppliedDirectionChangeDegrees,FootMotionCorrectionResponseVisibleOutputTransferred,FootMotionCorrectionResponseBeforeRebase,FootMotionCorrectionResponsePrevious,FootMotionCorrectionResponseCurrent,FootMotionCorrectionResponseDirectionX,FootMotionCorrectionResponseDirectionY,FootMotionCorrectionResponseDirectionZ,FootMotionCorrectionResponseDeltaDirection,FootMotionCorrectionResponseSelectedSpeed,FootMotionCorrectionResponseAppliedDelta," +
            "FootMotionPlantVerticalContinuityOwners," +
            "FootMotionPlantEffectiveCorrectionBeforeX,FootMotionPlantEffectiveCorrectionBeforeY,FootMotionPlantEffectiveCorrectionBeforeZ," +
            "FootMotionPlantEffectiveCorrectionAfterX,FootMotionPlantEffectiveCorrectionAfterY,FootMotionPlantEffectiveCorrectionAfterZ," +
            "FootMotionPlantOutputDistance,FootMotionPlantPenetrationDepth," +
            "CurrentSupportFrameSequence,CurrentSupportCompletionIdentity,CurrentSupportWorldRevision,CurrentSupportIsSpecified,CurrentSupportAvailable,CurrentSupportRejectReason," +
            CurrentSupportProbeHeader("CurrentSupportBase") +
            CurrentSupportProbeHeader("CurrentSupportRear") +
            CurrentSupportProbeHeader("CurrentSupportPositiveLateral") +
            CurrentSupportProbeHeader("CurrentSupportNegativeLateral") +
            CurrentSupportProbeHeader("CurrentSupportToe") +
            CurrentSupportCandidateHeader("CurrentSupportBaseCandidate") +
            CurrentSupportCandidateHeader("CurrentSupportRearCandidate") +
            CurrentSupportCandidateHeader("CurrentSupportPositiveLateralCandidate") +
            CurrentSupportCandidateHeader("CurrentSupportNegativeLateralCandidate") +
            CurrentSupportCandidateHeader("CurrentSupportToeCandidate") +
            "CurrentSupportSelectedProbe,CurrentSupportSelectionReason,CurrentSupportSelectionEpsilon,CurrentSupportSelectedDirectionBeforeNormalizationX,CurrentSupportSelectedDirectionBeforeNormalizationY,CurrentSupportSelectedDirectionBeforeNormalizationZ," +
            "CurrentSupportTargetAvailable,CurrentSupportTargetFrameSequence,CurrentSupportTargetCompletionIdentity,CurrentSupportTargetSide,CurrentSupportTargetPositionX,CurrentSupportTargetPositionY,CurrentSupportTargetPositionZ,CurrentSupportTargetNormalX,CurrentSupportTargetNormalY,CurrentSupportTargetNormalZ,CurrentSupportTargetSurfaceIdentity,CurrentSupportTargetWorldRevision,CurrentSupportTargetKind,CurrentSupportTargetPositionSource,CurrentSupportTargetPositionFrameSequence,CurrentSupportTargetPositionCompletionIdentity,CurrentSupportTargetPositionEventIdentity,CurrentSupportTargetPositionPathIdentity,CurrentSupportTargetNormalSource,CurrentSupportTargetNormalFrameSequence,CurrentSupportTargetNormalCompletionIdentity,CurrentSupportTargetNormalEventIdentity,CurrentSupportTargetCurrentSupportProbeKind," +
            "ResolvedFrameSequence,ResolvedCompletionIdentity,ResolvedRigId,ResolvedRigRevision,ResolvedSide,ResolvedOutcome,ResolvedFinalSoleX,ResolvedFinalSoleY,ResolvedFinalSoleZ,ResolvedEffectiveSoleX,ResolvedEffectiveSoleY,ResolvedEffectiveSoleZ,ResolvedGoalTargetAnkleX,ResolvedGoalTargetAnkleY,ResolvedGoalTargetAnkleZ,ResolvedGoalTargetRotationX,ResolvedGoalTargetRotationY,ResolvedGoalTargetRotationZ,ResolvedGoalTargetRotationW,ResolvedEffectiveAnkleX,ResolvedEffectiveAnkleY,ResolvedEffectiveAnkleZ,ResolvedEffectiveRotationX,ResolvedEffectiveRotationY,ResolvedEffectiveRotationZ,ResolvedEffectiveRotationW,ResolvedEffectiveHeelX,ResolvedEffectiveHeelY,ResolvedEffectiveHeelZ,ResolvedEffectiveToeX,ResolvedEffectiveToeY,ResolvedEffectiveToeZ,ResolvedEffectiveSoleFromContactsX,ResolvedEffectiveSoleFromContactsY,ResolvedEffectiveSoleFromContactsZ,ResolvedSourceSoleForwardX,ResolvedSourceSoleForwardY,ResolvedSourceSoleForwardZ,ResolvedSourceSoleFrameLocalRotationX,ResolvedSourceSoleFrameLocalRotationY,ResolvedSourceSoleFrameLocalRotationZ,ResolvedSourceSoleFrameLocalRotationW,ResolvedGoalTargetCorrectionX,ResolvedGoalTargetCorrectionY,ResolvedGoalTargetCorrectionZ,ResolvedEffectiveSoleCorrectionX,ResolvedEffectiveSoleCorrectionY,ResolvedEffectiveSoleCorrectionZ,ResolvedPositionWeight,ResolvedRotationWeight," +
            "ResolvedSupportTargetAvailable,ResolvedSupportTargetFrameSequence,ResolvedSupportTargetCompletionIdentity,ResolvedSupportTargetSide,ResolvedSupportTargetPositionX,ResolvedSupportTargetPositionY,ResolvedSupportTargetPositionZ,ResolvedSupportTargetNormalX,ResolvedSupportTargetNormalY,ResolvedSupportTargetNormalZ,ResolvedSupportTargetSurfaceIdentity,ResolvedSupportTargetWorldRevision,ResolvedSupportTargetKind,ResolvedSupportTargetPositionSource,ResolvedSupportTargetPositionFrameSequence,ResolvedSupportTargetPositionCompletionIdentity,ResolvedSupportTargetPositionEventIdentity,ResolvedSupportTargetPositionPathIdentity,ResolvedSupportTargetNormalSource,ResolvedSupportTargetNormalFrameSequence,ResolvedSupportTargetNormalCompletionIdentity,ResolvedSupportTargetNormalEventIdentity,ResolvedSupportTargetCurrentSupportProbeKind," +
            "ResolvedContactAvailable,ResolvedContactEventIdentity,ResolvedContactPointX,ResolvedContactPointY,ResolvedContactPointZ,ResolvedContactOwnership,ResolvedSupportEligibility,ResolvedSupportWeight,ResolvedSupportIntentWeight,ResolvedSupportHorizontalError,ResolvedSupportEventIdentity,ResolvedPelvisReachAvailable,ResolvedPelvisReachEventIdentity,ResolvedPelvisReachPointX,ResolvedPelvisReachPointY,ResolvedPelvisReachPointZ,ResolvedLandingReachAvailable,ResolvedLandingReachEventIdentity,ResolvedLandingReachHipX,ResolvedLandingReachHipY,ResolvedLandingReachHipZ,ResolvedLandingReachTargetAnkleX,ResolvedLandingReachTargetAnkleY,ResolvedLandingReachTargetAnkleZ,ResolvedLandingReachLegLength,ResolvedLandingReachMinimumCompressionReserve," +
            "FootMotionEncodedGoalAvailable,FootMotionEncodedGoalCorrectionX,FootMotionEncodedGoalCorrectionY,FootMotionEncodedGoalCorrectionZ," +
            "FinalGoalPositionX,FinalGoalPositionY,FinalGoalPositionZ,FinalGoalRotationX,FinalGoalRotationY,FinalGoalRotationZ,FinalGoalRotationW,FinalGoalPositionWeight,FinalGoalRotationWeight,PelvisPositionWeight,PelvisRotationWeight," +
            "StrideState,StrideRejectReason,StrideSupportSide,StrideSwingSide,StrideProgress,StrideSlope," +
            "StrideStartX,StrideStartY,StrideStartZ,StrideEndX,StrideEndY,StrideEndZ," +
            "StrideSampledGroundX,StrideSampledGroundY,StrideSampledGroundZ," +
            "StridePoseRootPositionX,StridePoseRootPositionY,StridePoseRootPositionZ," +
            "StrideAnimatedPelvisX,StrideAnimatedPelvisY,StrideAnimatedPelvisZ," +
            "StrideAnimatedPelvisComponentPositionX,StrideAnimatedPelvisComponentPositionY,StrideAnimatedPelvisComponentPositionZ," +
            "StrideRawPelvisDeltaX,StrideRawPelvisDeltaY,StrideRawPelvisDeltaZ," +
            "StrideRootRelativeGroundTargetAlongUp,StrideSoleClearanceLiftAlongUp,StrideHadPreviousState,StrideSupportChanged," +
            "StridePreviousSlope,StrideSpringHandoffReason,StrideSpringVelocityReset," +
            "StridePreviousSpringTarget,StridePreviousSpringOutput,StridePreviousSpringVelocity,StrideSpringInput,StrideSpringInputVelocity,StrideSpringFrequency," +
            "StrideUnclampedSpringTarget,StrideSupportReachAvailable,StrideSupportLegCompressionReserve,StrideSupportReachUsableLegLength,StrideSupportReachMinimumAlongUp,StrideSupportReachMaximumAlongUp," +
            "StrideSupportReachTargetClamped,StrideSupportReachOutputClamped," +
            "StrideSpringTarget,StrideSpringOutput,StrideSpringVelocity," +
            "StridePelvisDeltaX,StridePelvisDeltaY,StridePelvisDeltaZ,StridePositionWeight," +
            "FinalPelvisGoalX,FinalPelvisGoalY,FinalPelvisGoalZ," +
            "FinalPhysicalPelvisComponentPositionX,FinalPhysicalPelvisComponentPositionY,FinalPhysicalPelvisComponentPositionZ,FinalPhysicalPelvisGoalResidual," +
            "FinalIkSolverAvailable,FinalIkSucceeded,FinalIkFrameSequence,FinalIkInputCompletionIdentity,FinalIkOutputCompletionIdentity," +
            "FinalIkBackendIdentity,FinalIkRigId,FinalIkRigRevision,FinalIkProfileId,FinalIkProfileRevision,FinalIkFailure,FinalIkAppliedGoalCount," +
            "FinalIkEffectorAvailable,FinalIkEffectorSlot,FinalIkTargetPositionX,FinalIkTargetPositionY,FinalIkTargetPositionZ," +
            "FinalIkSolvedPositionX,FinalIkSolvedPositionY,FinalIkSolvedPositionZ,FinalIkPositionResidual,FinalIkRotationResidualDegrees," +
            "FinalIkLegAvailable,FinalIkLegSlot,FinalIkLegBendWeight,FinalIkLegStabilizationWeight,FinalIkLegRetainedPreviousBendDirection," +
            "FinalIkLegOriginalHipX,FinalIkLegOriginalHipY,FinalIkLegOriginalHipZ," +
            "FinalIkLegOriginalKneeX,FinalIkLegOriginalKneeY,FinalIkLegOriginalKneeZ," +
            "FinalIkLegOriginalAnkleX,FinalIkLegOriginalAnkleY,FinalIkLegOriginalAnkleZ," +
            "FinalIkLegTargetAnkleX,FinalIkLegTargetAnkleY,FinalIkLegTargetAnkleZ," +
            "FinalIkLegSolvedHipX,FinalIkLegSolvedHipY,FinalIkLegSolvedHipZ," +
            "FinalIkLegSolvedKneeX,FinalIkLegSolvedKneeY,FinalIkLegSolvedKneeZ," +
            "FinalIkLegSolvedAnkleX,FinalIkLegSolvedAnkleY,FinalIkLegSolvedAnkleZ," +
            "FinalIkLegOriginalBendDegrees,FinalIkLegSolvedBendDegrees," +
            "FinalIkLegOriginalExtensionRatio,FinalIkLegTargetExtensionRatio,FinalIkLegSolvedExtensionRatio," +
            "FinalIkLegOriginalCompressionReserve,FinalIkLegTargetCompressionReserve,FinalIkLegSolvedCompressionReserve," +
            "FinalIkLegEffectiveBendDirectionX,FinalIkLegEffectiveBendDirectionY,FinalIkLegEffectiveBendDirectionZ," +
            "FinalIkLegAnimatedBendDirectionPreviousDot,FinalIkLegEffectiveBendDirectionPreviousDot," +
            "FinalIkPelvisAvailable,FinalIkPelvisTargetPositionX,FinalIkPelvisTargetPositionY,FinalIkPelvisTargetPositionZ," +
            "FinalIkPelvisSolvedPositionX,FinalIkPelvisSolvedPositionY,FinalIkPelvisSolvedPositionZ,FinalIkPelvisPositionResidual,FinalIkPelvisRotationResidualDegrees," +
            "FinalPhysicalWriteAvailable,FinalPhysicalWriteCompletionIdentity," +
            "FinalPhysicalAnkleComponentPositionX,FinalPhysicalAnkleComponentPositionY,FinalPhysicalAnkleComponentPositionZ," +
            "FinalPhysicalAnkleComponentRotationX,FinalPhysicalAnkleComponentRotationY,FinalPhysicalAnkleComponentRotationZ,FinalPhysicalAnkleComponentRotationW," +
            "FinalPhysicalAnkleWorldPositionX,FinalPhysicalAnkleWorldPositionY,FinalPhysicalAnkleWorldPositionZ," +
            "FinalPhysicalAnkleWorldRotationX,FinalPhysicalAnkleWorldRotationY,FinalPhysicalAnkleWorldRotationZ,FinalPhysicalAnkleWorldRotationW," +
            "FinalPhysicalHeelWorldX,FinalPhysicalHeelWorldY,FinalPhysicalHeelWorldZ," +
            "FinalPhysicalToeWorldX,FinalPhysicalToeWorldY,FinalPhysicalToeWorldZ,FinalPhysicalAnkleGoalResidual";
        const string GeometryHeader =
            "SampleIdentity,FrameSequence,CompletionIdentity,Side,GroundPathInputIdentity," +
            "GroundContactIndex,GroundContactSegmentIndex,GroundContactSurfaceIdentity,GroundContactCandidateIdentity," +
            "GroundContactPositionX,GroundContactPositionY,GroundContactPositionZ," +
            "GroundContactNormalX,GroundContactNormalY,GroundContactNormalZ,GroundContactQueryDistance," +
            "GroundEnvelopeVertexIndex,GroundEnvelopeVertexX,GroundEnvelopeVertexY,GroundEnvelopeVertexZ";

        readonly struct SamplingProgramIdentity
        {
            internal SamplingProgramIdentity(
                AnimationPresentationProgramIdentity identity)
                : this(
                    identity.ProjectionRevision,
                    identity.PoseGraphId,
                    identity.PoseGraphRevision,
                    identity.PosePlanHash)
            {
            }

            internal SamplingProgramIdentity(
                string projectionRevision,
                string poseGraphId,
                string poseGraphRevision,
                string posePlanHash)
            {
                if (string.IsNullOrWhiteSpace(projectionRevision) ||
                    string.IsNullOrWhiteSpace(poseGraphId) ||
                    string.IsNullOrWhiteSpace(poseGraphRevision) ||
                    string.IsNullOrWhiteSpace(posePlanHash))
                {
                    throw new ArgumentException(
                        "Foot Landing sampling Program identity is incomplete.");
                }
                ProjectionRevision = projectionRevision.Trim();
                PoseGraphId = poseGraphId.Trim();
                PoseGraphRevision = poseGraphRevision.Trim();
                PosePlanHash = posePlanHash.Trim();
                ProgramIdentity = $"{ProjectionRevision}|{PosePlanHash}";
            }

            internal string ProgramIdentity { get; }
            internal string ProjectionRevision { get; }
            internal string PoseGraphId { get; }
            internal string PoseGraphRevision { get; }
            internal string PosePlanHash { get; }

            internal bool Matches(in SamplingProgramIdentity other) =>
                string.Equals(
                    ProjectionRevision,
                    other.ProjectionRevision,
                    StringComparison.Ordinal) &&
                string.Equals(PoseGraphId, other.PoseGraphId, StringComparison.Ordinal) &&
                string.Equals(
                    PoseGraphRevision,
                    other.PoseGraphRevision,
                    StringComparison.Ordinal) &&
                string.Equals(PosePlanHash, other.PosePlanHash, StringComparison.Ordinal);
        }

        sealed class SamplingSession : IDisposable
        {
            readonly FileStream m_SamplesStream;
            readonly StreamWriter m_SamplesWriter;
            readonly StringBuilder m_SamplesRow = new StringBuilder(4096);
            readonly FileStream m_GeometryStream;
            readonly StreamWriter m_GeometryWriter;
            readonly StringBuilder m_GeometryRow = new StringBuilder(512);
            readonly BlockingCollection<CapturedFrame> m_Queue =
                new BlockingCollection<CapturedFrame>(
                    new ConcurrentQueue<CapturedFrame>(),
                    MaximumQueuedFrameCount);
            readonly Thread m_WriterThread;
            readonly string m_SamplesPartPath;
            readonly string m_GeometryPartPath;
            Exception m_Failure;
            int m_AcceptedFrameCount;
            int m_WrittenFrameCount;
            int m_Disposed;

            internal SamplingSession(in SamplingProgramIdentity program)
            {
                SampleIdentity = Guid.NewGuid();
                StartedUtc = DateTime.UtcNow;
                Program = program;
                string root = ResolveSaveDirectory();
                DirectoryPath = System.IO.Path.Combine(
                    root,
                    $"{DateTime.Now:yyyyMMdd-HHmmss-fff}-{SampleIdentity:N}");
                Directory.CreateDirectory(DirectoryPath);
                Path = System.IO.Path.Combine(DirectoryPath, "samples.csv");
                GeometryPath = System.IO.Path.Combine(
                    DirectoryPath,
                    GeometryFileName);
                m_SamplesPartPath = Path + ".part";
                m_GeometryPartPath = GeometryPath + ".part";
                FileStream samplesStream = null;
                StreamWriter samplesWriter = null;
                FileStream geometryStream = null;
                StreamWriter geometryWriter = null;
                try
                {
                    samplesStream = new FileStream(
                        m_SamplesPartPath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.Read,
                        65536,
                        FileOptions.SequentialScan);
                    samplesWriter = new StreamWriter(
                        samplesStream,
                        new UTF8Encoding(false));
                    geometryStream = new FileStream(
                        m_GeometryPartPath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.Read,
                        65536,
                        FileOptions.SequentialScan);
                    geometryWriter = new StreamWriter(
                        geometryStream,
                        new UTF8Encoding(false));
                    samplesWriter.WriteLine(Header);
                    geometryWriter.WriteLine(GeometryHeader);
                    samplesWriter.Flush();
                    geometryWriter.Flush();
                }
                catch
                {
                    geometryWriter?.Dispose();
                    geometryStream?.Dispose();
                    samplesWriter?.Dispose();
                    samplesStream?.Dispose();
                    throw;
                }
                m_SamplesStream = samplesStream;
                m_SamplesWriter = samplesWriter;
                m_GeometryStream = geometryStream;
                m_GeometryWriter = geometryWriter;
                m_WriterThread = new Thread(WriteLoop)
                {
                    IsBackground = true,
                    Name = $"Foot Landing CSV {SampleIdentity:N}",
                    Priority = System.Threading.ThreadPriority.Normal
                };
                m_WriterThread.Start();
            }

            internal Guid SampleIdentity { get; }
            internal DateTime StartedUtc { get; }
            internal SamplingProgramIdentity Program { get; }
            internal string DirectoryPath { get; }
            internal string Path { get; }
            internal string GeometryPath { get; }
            internal int FrameCount => Volatile.Read(ref m_AcceptedFrameCount);
            internal int WrittenFrameCount => Volatile.Read(ref m_WrittenFrameCount);

            internal void Enqueue(CapturedFrame captured)
            {
                if (captured == null)
                    throw new ArgumentNullException(nameof(captured));
                RequireHealthy();
                if (!m_Queue.TryAdd(captured))
                {
                    throw new InvalidOperationException(
                        "Foot Landing CSV writer queue capacity was exceeded.");
                }
                Interlocked.Increment(ref m_AcceptedFrameCount);
                RequireHealthy();
            }

            internal void RequireHealthy()
            {
                if (Volatile.Read(ref m_Disposed) != 0)
                    throw new ObjectDisposedException(nameof(SamplingSession));
                Exception failure = Volatile.Read(ref m_Failure);
                if (failure != null)
                {
                    throw new IOException(
                        "Foot Landing CSV background writer failed.",
                        failure);
                }
            }

            void WriteLoop()
            {
                long flushIntervalTicks =
                    TimeSpan.FromSeconds(SamplingFlushIntervalSeconds).Ticks;
                long nextFlushTicks = DateTime.UtcNow.Ticks + flushIntervalTicks;
                try
                {
                    foreach (CapturedFrame captured in m_Queue.GetConsumingEnumerable())
                    {
                        Write(captured);
                        Interlocked.Increment(ref m_WrittenFrameCount);
                        long now = DateTime.UtcNow.Ticks;
                        if (now < nextFlushTicks)
                            continue;
                        FlushBuffered();
                        nextFlushTicks = now + flushIntervalTicks;
                    }
                    FlushToDisk();
                }
                catch (Exception exception)
                {
                    Volatile.Write(ref m_Failure, exception);
                }
                finally
                {
                    try
                    {
                        m_GeometryWriter.Dispose();
                        m_SamplesWriter.Dispose();
                    }
                    catch (Exception exception)
                    {
                        Volatile.Write(ref m_Failure, exception);
                    }
                }
            }

            void Write(CapturedFrame captured)
            {
                CharacterFootLandingPredictionDiagnostics frame = captured.Foot;
                FootIkCapture left = captured.Left;
                FootIkCapture right = captured.Right;
                FootStepObservationCapture footStepObservation = captured.FootStepObservation;
                RootHierarchyCapture roots = captured.Roots;
                CharacterFootLandingPredictionFootDiagnostics leftFoot = frame.Left;
                CharacterFootLandingPredictionFootDiagnostics rightFoot = frame.Right;
                WriteSampleRow(
                    this,
                    m_SamplesWriter,
                    m_SamplesRow,
                    in frame,
                    in leftFoot,
                    in left,
                    in footStepObservation,
                    in roots,
                    captured.TargetRuntimeInstanceId,
                    captured.TargetHostInstanceId);
                WriteSampleRow(
                    this,
                    m_SamplesWriter,
                    m_SamplesRow,
                    in frame,
                    in rightFoot,
                    in right,
                    in footStepObservation,
                    in roots,
                    captured.TargetRuntimeInstanceId,
                    captured.TargetHostInstanceId);
                WriteGeometryRows(
                    this,
                    m_GeometryWriter,
                    m_GeometryRow,
                    in frame,
                    in leftFoot);
                WriteGeometryRows(
                    this,
                    m_GeometryWriter,
                    m_GeometryRow,
                    in frame,
                    in rightFoot);
            }

            void FlushBuffered()
            {
                m_SamplesWriter.Flush();
                m_GeometryWriter.Flush();
            }

            void FlushToDisk()
            {
                FlushBuffered();
                m_SamplesStream.Flush(true);
                m_GeometryStream.Flush(true);
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref m_Disposed, 1) != 0)
                    return;
                m_Queue.CompleteAdding();
                m_WriterThread.Join();
                m_Queue.Dispose();
                Exception failure = Volatile.Read(ref m_Failure);
                if (failure != null)
                {
                    throw new IOException(
                        "Foot Landing CSV background writer failed.",
                        failure);
                }
                if (WrittenFrameCount != FrameCount)
                {
                    throw new InvalidOperationException(
                        "Foot Landing CSV background writer did not persist every captured frame.");
                }
                if (File.Exists(Path) || File.Exists(GeometryPath))
                    throw new IOException("Foot Landing sealed capture package already exists.");
                File.Move(m_GeometryPartPath, GeometryPath);
                File.Move(m_SamplesPartPath, Path);
            }
        }

        sealed class FinalizationJob
        {
            readonly SamplingSession m_Session;
            readonly Thread m_Thread;
            CharacterFootMotionDiagnosticAnalysis m_Analysis;
            Exception m_Failure;
            int m_Completed;

            internal FinalizationJob(
                SamplingSession session,
                Exception captureFailure,
                int droppedPendingFrameCount)
            {
                m_Session = session ?? throw new ArgumentNullException(nameof(session));
                CaptureFailure = captureFailure;
                DroppedPendingFrameCount = droppedPendingFrameCount;
                m_Thread = new Thread(Run)
                {
                    IsBackground = true,
                    Name = $"Foot Landing Finalizer {session.SampleIdentity:N}",
                    Priority = System.Threading.ThreadPriority.BelowNormal
                };
            }

            internal Guid SampleIdentity => m_Session.SampleIdentity;
            internal string SamplesPath => m_Session.Path;
            internal string GeometryPath => m_Session.GeometryPath;
            internal string DirectoryPath => m_Session.DirectoryPath;
            internal int AcceptedFrameCount => m_Session.FrameCount;
            internal int WrittenFrameCount => m_Session.WrittenFrameCount;
            internal int DroppedPendingFrameCount { get; }
            internal Exception CaptureFailure { get; }
            internal bool IsCompleted => Volatile.Read(ref m_Completed) != 0;
            internal Exception Failure => m_Failure;
            internal CharacterFootMotionDiagnosticAnalysis Analysis => m_Analysis;

            internal void Start() => m_Thread.Start();

            internal void Wait() => m_Thread.Join();

            void Run()
            {
                try
                {
                    m_Session.Dispose();
                    if (m_Session.WrittenFrameCount == 0)
                    {
                        if (File.Exists(m_Session.Path))
                            File.Delete(m_Session.Path);
                        if (File.Exists(m_Session.GeometryPath))
                            File.Delete(m_Session.GeometryPath);
                        if (Directory.Exists(m_Session.DirectoryPath) &&
                            !Directory.EnumerateFileSystemEntries(
                                m_Session.DirectoryPath).Any())
                        {
                            Directory.Delete(m_Session.DirectoryPath);
                        }
                    }
                    else
                    {
                        m_Analysis = CharacterFootMotionDiagnosticAnalyzer.Analyze(
                            m_Session.Path);
                    }
                }
                catch (Exception exception)
                {
                    m_Failure = exception;
                }
                finally
                {
                    Volatile.Write(ref m_Completed, 1);
                }
            }
        }

        readonly struct FootIkCapture
        {
            internal FootIkCapture(
                CharacterFullBodyIkSolverDiagnostics solver,
                CharacterFullBodyIkEffectorDiagnostics pelvis,
                CharacterFullBodyIkEffectorDiagnostics effector,
                CharacterFullBodyIkLimbDiagnostics limb,
                bool physicalWriteAvailable,
                ulong physicalWriteCompletionIdentity,
                Vector3 physicalAnkleComponentPosition,
                Quaternion physicalAnkleComponentRotation,
                Vector3 physicalPelvisComponentPosition)
            {
                Solver = solver;
                Pelvis = pelvis;
                Effector = effector;
                Limb = limb;
                PhysicalWriteAvailable = physicalWriteAvailable;
                PhysicalWriteCompletionIdentity = physicalWriteCompletionIdentity;
                PhysicalAnkleComponentPosition = physicalAnkleComponentPosition;
                PhysicalAnkleComponentRotation = physicalAnkleComponentRotation;
                PhysicalPelvisComponentPosition = physicalPelvisComponentPosition;
            }

            internal CharacterFullBodyIkSolverDiagnostics Solver { get; }
            internal CharacterFullBodyIkEffectorDiagnostics Pelvis { get; }
            internal CharacterFullBodyIkEffectorDiagnostics Effector { get; }
            internal CharacterFullBodyIkLimbDiagnostics Limb { get; }
            internal bool SolverAvailable => Solver.IsCompleted;
            internal bool PelvisAvailable => Pelvis.IsAvailable;
            internal bool EffectorAvailable => Effector.IsAvailable;
            internal bool PhysicalWriteAvailable { get; }
            internal ulong PhysicalWriteCompletionIdentity { get; }
            internal Vector3 PhysicalAnkleComponentPosition { get; }
            internal Quaternion PhysicalAnkleComponentRotation { get; }
            internal Vector3 PhysicalPelvisComponentPosition { get; }
        }

        sealed class PendingFrame
        {
            internal PendingFrame(in CharacterFootLandingPredictionDiagnostics diagnostics)
            {
                Diagnostics = diagnostics;
            }

            internal CharacterFootLandingPredictionDiagnostics Diagnostics { get; }
        }

        readonly struct RootHierarchyCapture
        {
            internal RootHierarchyCapture(CharacterRootHierarchyBinding binding)
            {
                if (!binding)
                    throw new ArgumentNullException(nameof(binding));
                LogicRootPosition = binding.LogicRoot.position;
                LogicRootRotation = binding.LogicRoot.rotation;
                VisualRootLocalPosition = binding.VisualRoot.localPosition;
                VisualRootLocalRotation = binding.VisualRoot.localRotation;
                VisualRootWorldPosition = binding.VisualRoot.position;
                VisualRootWorldRotation = binding.VisualRoot.rotation;
                PoseRootLocalPosition = binding.PoseRoot.localPosition;
                PoseRootLocalRotation = binding.PoseRoot.localRotation;
                PoseRootWorldPosition = binding.PoseRoot.position;
                PoseRootWorldRotation = binding.PoseRoot.rotation;
                PoseRootLossyScale = binding.PoseRoot.lossyScale;
            }

            internal Vector3 LogicRootPosition { get; }
            internal Quaternion LogicRootRotation { get; }
            internal Vector3 VisualRootLocalPosition { get; }
            internal Quaternion VisualRootLocalRotation { get; }
            internal Vector3 VisualRootWorldPosition { get; }
            internal Quaternion VisualRootWorldRotation { get; }
            internal Vector3 PoseRootLocalPosition { get; }
            internal Quaternion PoseRootLocalRotation { get; }
            internal Vector3 PoseRootWorldPosition { get; }
            internal Quaternion PoseRootWorldRotation { get; }
            internal Vector3 PoseRootLossyScale { get; }
        }

        readonly struct FootStepObservationCapture
        {
            internal FootStepObservationCapture(
                string sourceIdentity,
                float weight,
                float normalizedTime,
                AnimationFootMotionRuntimeSample left,
                AnimationFootMotionRuntimeSample right)
            {
                if (string.IsNullOrWhiteSpace(sourceIdentity) ||
                    !float.IsFinite(weight) || weight < 0f || weight > 1f ||
                    !float.IsFinite(normalizedTime) || normalizedTime < 0f || normalizedTime > 1f ||
                    !left.IsValid || !right.IsValid)
                {
                    throw new ArgumentException("Foot Step observation capture is invalid.");
                }
                SourceIdentity = sourceIdentity.Trim();
                Weight = weight;
                NormalizedTime = normalizedTime;
                Left = left;
                Right = right;
                m_IsSpecified = 1;
            }

            readonly byte m_IsSpecified;
            internal string SourceIdentity { get; }
            internal float Weight { get; }
            internal float NormalizedTime { get; }
            internal AnimationFootMotionRuntimeSample Left { get; }
            internal AnimationFootMotionRuntimeSample Right { get; }
            internal bool IsValid => m_IsSpecified != 0;
        }

        sealed class CapturedFrame
        {
            internal CapturedFrame(
                in CharacterFootLandingPredictionDiagnostics foot,
                FootIkCapture left,
                FootIkCapture right,
                FootStepObservationCapture footStepObservation,
                Vector3 physicalPelvisComponentPosition,
                RootHierarchyCapture roots,
                Guid targetRuntimeInstanceId,
                int targetHostInstanceId)
            {
                Foot = foot;
                Left = left;
                Right = right;
                FootStepObservation = footStepObservation;
                PhysicalPelvisComponentPosition = physicalPelvisComponentPosition;
                Roots = roots;
                TargetRuntimeInstanceId = targetRuntimeInstanceId;
                TargetHostInstanceId = targetHostInstanceId;
            }

            internal CharacterFootLandingPredictionDiagnostics Foot { get; }
            internal FootIkCapture Left { get; }
            internal FootIkCapture Right { get; }
            internal FootStepObservationCapture FootStepObservation { get; }
            internal Vector3 PhysicalPelvisComponentPosition { get; }
            internal RootHierarchyCapture Roots { get; }
            internal Guid TargetRuntimeInstanceId { get; }
            internal int TargetHostInstanceId { get; }
        }

        static readonly List<PendingFrame> s_PendingFrames =
            new List<PendingFrame>(64);
        static readonly HashSet<Guid> s_ConfiguredTargets = new HashSet<Guid>();
        static readonly Dictionary<Guid, string> s_PoseWatchSignatures =
            new Dictionary<Guid, string>();
        static readonly Guid s_DiagnosticsOwnerId = Guid.NewGuid();

        static bool s_Capturing;
        static bool s_StartPending;
        static bool s_ControlledCaptureWindow;
        static bool s_CaptureWindowOpen;
        static double s_StartDeadline;
        static string s_LastStartFailure = string.Empty;
        static string s_StartWaitReason = string.Empty;
        static string s_LastSavedPath = string.Empty;
        static string s_LastSavedGeometryPath = string.Empty;
        static string s_LastSavedDirectory = string.Empty;
        static string s_LastSavedFactsPath = string.Empty;
        static string s_LastSavedDiagnosisDirectory = string.Empty;
        static string s_LastDiagnosticSummary = string.Empty;
        static string s_LastSavedSampleIdentity = string.Empty;
        static string s_LastFinalizationFailure = string.Empty;
        static SamplingSession s_Session;
        static FinalizationJob s_Finalization;
        static int s_DroppedPendingFrameCount;
        static int s_LastSavedFrameCount;
        static int s_LastFactEventCount;
        static int s_LastDiagnosisTargetCount;
        static int s_LastDiagnosisMatchCount;
        static int s_TargetHostInstanceId;
        static int s_TargetRootInstanceId;
        static CharacterRootHierarchyBinding s_TargetRootHierarchy;

        public static bool IsCapturing => s_Capturing;
        public static bool IsStartPending => s_StartPending;
        public static bool IsFinalizing => s_Finalization != null;
        public static bool IsControlledCaptureWindow =>
            s_Capturing && s_ControlledCaptureWindow;
        public static bool IsCaptureWindowOpen =>
            s_Capturing && s_CaptureWindowOpen;
        public static string LastStartFailure => s_LastStartFailure;
        public static string LastSavedPath => s_LastSavedPath;
        public static string LastSavedGeometryPath => s_LastSavedGeometryPath;
        public static string LastSavedDirectory => s_LastSavedDirectory;
        public static string LastSavedFactsPath => s_LastSavedFactsPath;
        public static string LastSavedDiagnosisDirectory =>
            s_LastSavedDiagnosisDirectory;
        public static string LastDiagnosticSummary => s_LastDiagnosticSummary;
        public static string LastFinalizationFailure => s_LastFinalizationFailure;
        public static string CurrentSampleIdentity =>
            s_Session?.SampleIdentity.ToString("N") ??
            s_Finalization?.SampleIdentity.ToString("N") ??
            string.Empty;
        public static string LastSavedSampleIdentity => s_LastSavedSampleIdentity;
        public static int CapturedFrameCount =>
            s_Session?.FrameCount ??
            s_Finalization?.AcceptedFrameCount ??
            0;
        public static int PendingFrameCount => s_PendingFrames.Count;
        public static int DroppedPendingFrameCount => s_DroppedPendingFrameCount;
        public static int LastSavedFrameCount => s_LastSavedFrameCount;
        public static int LastFactEventCount => s_LastFactEventCount;
        public static int LastDiagnosisTargetCount => s_LastDiagnosisTargetCount;
        public static int LastDiagnosisMatchCount => s_LastDiagnosisMatchCount;

        static CharacterFootLandingPredictionSampler()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.quitting += OnEditorQuitting;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            s_LastSavedPath = FindLatestSavedPath();
            if (!string.IsNullOrEmpty(s_LastSavedPath))
            {
                s_LastSavedDirectory = System.IO.Path.GetDirectoryName(
                    s_LastSavedPath) ?? string.Empty;
                string geometryPath = System.IO.Path.Combine(
                    s_LastSavedDirectory,
                    GeometryFileName);
                s_LastSavedGeometryPath = File.Exists(geometryPath)
                    ? geometryPath
                    : string.Empty;
                string factsPath = System.IO.Path.Combine(
                    s_LastSavedDirectory,
                    "facts.json");
                s_LastSavedFactsPath = File.Exists(factsPath)
                    ? factsPath
                    : string.Empty;
                string diagnosisDirectory = System.IO.Path.Combine(
                    s_LastSavedDirectory,
                    "diagnoses");
                s_LastSavedDiagnosisDirectory = Directory.Exists(
                    diagnosisDirectory)
                    ? diagnosisDirectory
                    : string.Empty;
            }
        }

        public static void StartSampling() => StartSampling(false);

        public static void StartControlledSampling() => StartSampling(true);

        public static void OpenControlledCaptureWindow()
        {
            if (!s_Capturing || !s_ControlledCaptureWindow ||
                s_CaptureWindowOpen)
            {
                throw new InvalidOperationException(
                    "Foot Landing controlled capture window cannot open in the current state.");
            }
            s_CaptureWindowOpen = true;
        }

        public static void CloseControlledCaptureWindow()
        {
            if (!s_Capturing || !s_ControlledCaptureWindow ||
                !s_CaptureWindowOpen)
            {
                throw new InvalidOperationException(
                    "Foot Landing controlled capture window cannot close in the current state.");
            }
            s_CaptureWindowOpen = false;
        }

        static void StartSampling(bool controlledCaptureWindow)
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException(
                    "Foot Landing sampling can only start in Play Mode.");
            if (s_Capturing)
                throw new InvalidOperationException(
                    "Foot Landing sampling is already active.");
            if (s_StartPending)
                throw new InvalidOperationException(
                    "Foot Landing sampling is already waiting for the Gameplay Lab player.");
            if (s_Finalization != null)
                throw new InvalidOperationException(
                    "Foot Landing sampling is still finalizing the previous capture.");
            s_PendingFrames.Clear();
            s_DroppedPendingFrameCount = 0;
            s_LastSavedFrameCount = 0;
            s_LastFactEventCount = 0;
            s_LastDiagnosisTargetCount = 0;
            s_LastDiagnosisMatchCount = 0;
            s_LastDiagnosticSummary = string.Empty;
            s_LastFinalizationFailure = string.Empty;
            s_LastStartFailure = string.Empty;
            s_StartWaitReason = string.Empty;
            s_ControlledCaptureWindow = controlledCaptureWindow;
            s_CaptureWindowOpen = !controlledCaptureWindow;
            s_StartPending = true;
            s_StartDeadline = EditorApplication.timeSinceStartup + SamplingStartTimeoutSeconds;
            EditorApplication.update -= PollSamplingStart;
            EditorApplication.update += PollSamplingStart;
            PollSamplingStart();
        }

        static void PollSamplingStart()
        {
            if (!s_StartPending)
            {
                EditorApplication.update -= PollSamplingStart;
                return;
            }
            if (!EditorApplication.isPlaying)
            {
                FailSamplingStart("Gameplay Lab left Play Mode before the player host became available.");
                return;
            }
            try
            {
                if (TryCompleteSamplingStart())
                    return;
            }
            catch (Exception exception)
            {
                FailSamplingStart(exception.Message);
                Debug.LogException(exception);
                return;
            }
            if (EditorApplication.timeSinceStartup >= s_StartDeadline)
                FailSamplingStart(s_StartWaitReason);
        }

        static bool TryCompleteSamplingStart()
        {
            if (!TryBindGameplayLabPlayerTarget())
            {
                s_StartWaitReason = "Gameplay Lab player host did not become available before sampling timed out.";
                return false;
            }
            if (!TryResolveSamplingProgramIdentity(out SamplingProgramIdentity program))
            {
                s_StartWaitReason =
                    "Gameplay Lab player compiled Animation Presentation Program did not become available before sampling timed out.";
                return false;
            }
            s_Capturing = true;
            try
            {
                ConfigureTargets();
                s_Session = new SamplingSession(in program);
                s_LastSavedPath = s_Session.Path;
                s_LastSavedGeometryPath = s_Session.GeometryPath;
                s_LastSavedDirectory = s_Session.DirectoryPath;
                s_LastSavedFactsPath = string.Empty;
                s_LastSavedDiagnosisDirectory = string.Empty;
                s_LastSavedSampleIdentity = s_Session.SampleIdentity.ToString("N");
                CharacterFootLandingPredictionDebugRegistry.Published += Capture;
                AnimationPresentationRuntimeTargetRegistry.TargetRegistered += ConfigureTarget;
                AnimationPresentationRuntimeTargetRegistry.TargetUnregistered += RemoveTarget;
                EditorApplication.update += ProcessPendingFrames;
            }
            catch
            {
                CancelSamplingStart();
                throw;
            }
            s_StartPending = false;
            s_StartDeadline = 0d;
            s_StartWaitReason = string.Empty;
            EditorApplication.update -= PollSamplingStart;
            Debug.Log(
                $"Foot Landing sampling started. " +
                $"Sample={s_Session.SampleIdentity:N}, " +
                $"Program={s_Session.Program.ProgramIdentity}, " +
                $"Path={s_Session.Path}");
            return true;
        }

        [MenuItem(StartMenu)]
        static void StartFromMenu() => StartSampling();

        [MenuItem(StartMenu, true)]
        static bool CanStart() =>
            EditorApplication.isPlaying && !s_Capturing && !s_StartPending &&
            s_Finalization == null;

        [MenuItem(StopMenu)]
        static void Stop() => StopAndSave();

        [MenuItem(StopMenu, true)]
        static bool CanStop() => s_Capturing || s_StartPending;

        public static void StopAndSaveSampling() => StopAndSave();

        static void Capture(in CharacterFootLandingPredictionDiagnostics diagnostics)
        {
            if (!s_Capturing || !s_CaptureWindowOpen)
                return;
            if (diagnostics.RootInstanceId != s_TargetRootInstanceId)
                return;
            if (s_PendingFrames.Count >= MaximumPendingFrameCount)
            {
                s_PendingFrames.RemoveAt(0);
                s_DroppedPendingFrameCount++;
            }
            s_PendingFrames.Add(new PendingFrame(in diagnostics));
        }

        static void ProcessPendingFrames()
        {
            if (!s_Capturing)
                return;
            try
            {
                ProcessPendingFramesCore();
            }
            catch (Exception exception)
            {
                FailActiveSampling(exception);
            }
        }

        static void ProcessPendingFramesCore()
        {
            ConfigureTargets();
            SamplingSession session = s_Session ?? throw new InvalidOperationException(
                "Foot Landing sampling has no active persistent session.");
            session.RequireHealthy();
            for (int pendingIndex = 0; pendingIndex < s_PendingFrames.Count;)
            {
                PendingFrame pending = s_PendingFrames[pendingIndex];
                CharacterFootLandingPredictionDiagnostics pendingDiagnostics = pending.Diagnostics;
                PendingFrameResolution resolution = TryCaptureCommittedIk(
                    in pendingDiagnostics,
                    out CapturedFrame captured);
                if (resolution == PendingFrameResolution.Waiting)
                {
                    pendingIndex++;
                    continue;
                }
                if (resolution == PendingFrameResolution.Captured)
                    session.Enqueue(captured);
                else
                    s_DroppedPendingFrameCount++;
                s_PendingFrames.RemoveAt(pendingIndex);
            }
            session.RequireHealthy();
        }

        enum PendingFrameResolution : byte
        {
            Waiting,
            Captured,
            Stale
        }

        static PendingFrameResolution TryCaptureCommittedIk(
            in CharacterFootLandingPredictionDiagnostics pending,
            out CapturedFrame captured)
        {
            IReadOnlyList<AnimationPresentationRuntimeTarget> targets =
                AnimationPresentationRuntimeTargetRegistry.Targets;
            for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
            {
                AnimationPresentationRuntimeTarget target = targets[targetIndex];
                if (target.HostInstanceId != s_TargetHostInstanceId)
                    continue;
                if (!target.TryGetDebugView(out AnimationPresentationDebugView debugView))
                    continue;
                AnimationFootPlacementRuntimeSnapshot placement = debugView.PosePlan.FootPlacement;
                if (!placement.IsAvailable ||
                    placement.LandingPrediction.RootInstanceId != pending.RootInstanceId)
                {
                    continue;
                }
                if (placement.LandingPrediction.FrameSequence > pending.FrameSequence)
                {
                    captured = default;
                    return PendingFrameResolution.Stale;
                }
                if (placement.LandingPrediction.FrameSequence != pending.FrameSequence ||
                    placement.LandingPrediction.CompletionIdentity != pending.CompletionIdentity)
                {
                    continue;
                }
                captured = new CapturedFrame(
                    in pending,
                    new FootIkCapture(
                        placement.Solver,
                        placement.Pelvis,
                        placement.LeftFoot,
                        placement.LeftLeg,
                        placement.PhysicalWriteAvailable,
                        placement.PhysicalWriteCompletionIdentity,
                        placement.LeftPhysicalAnkleComponentPosition,
                        placement.LeftPhysicalAnkleComponentRotation,
                        placement.PhysicalPelvisComponentPosition),
                    new FootIkCapture(
                        placement.Solver,
                        placement.Pelvis,
                        placement.RightFoot,
                        placement.RightLeg,
                        placement.PhysicalWriteAvailable,
                        placement.PhysicalWriteCompletionIdentity,
                        placement.RightPhysicalAnkleComponentPosition,
                        placement.RightPhysicalAnkleComponentRotation,
                        placement.PhysicalPelvisComponentPosition),
                    CaptureFootStepObservation(debugView.PosePlan),
                    placement.PhysicalPelvisComponentPosition,
                    new RootHierarchyCapture(s_TargetRootHierarchy),
                    target.RuntimeInstanceId,
                    target.HostInstanceId);
                return PendingFrameResolution.Captured;
            }
            captured = default;
            return PendingFrameResolution.Waiting;
        }

        static FootStepObservationCapture CaptureFootStepObservation(
            AnimationPresentationRuntimeSnapshot snapshot)
        {
            AnimationFootStepObservationRuntimeSnapshot observation =
                snapshot.FootStepObservation;
            return observation.IsValid
                ? new FootStepObservationCapture(
                    observation.SourceIdentity,
                    observation.SourceWeight,
                    observation.NormalizedTime,
                    observation.Left,
                    observation.Right)
                : default;
        }

        static void ConfigureTargets()
        {
            IReadOnlyList<AnimationPresentationRuntimeTarget> targets =
                AnimationPresentationRuntimeTargetRegistry.Targets;
            bool configured = false;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].HostInstanceId != s_TargetHostInstanceId)
                    continue;
                ConfigureTarget(targets[i]);
                configured = true;
            }
            if (!configured)
                throw new InvalidOperationException(
                    "Gameplay Lab player Animation Presentation target is unavailable.");
        }

        static void ConfigureTarget(AnimationPresentationRuntimeTarget target)
        {
            if (!s_Capturing || target == null ||
                target.HostInstanceId != s_TargetHostInstanceId)
                return;
            var targetProgram = new SamplingProgramIdentity(target.ProgramIdentity);
            if (s_Session != null && !s_Session.Program.Matches(in targetProgram))
            {
                throw new InvalidOperationException(
                    "Gameplay Lab player compiled Animation Presentation Program changed during sampling.");
            }
            if (!s_ConfiguredTargets.Contains(target.RuntimeInstanceId))
            {
                target.SetDiagnosticsInterest(
                    s_DiagnosticsOwnerId,
                    AnimationPresentationDiagnosticsInterest.Capture |
                    AnimationPresentationDiagnosticsInterest.OperationDetail);
                s_ConfiguredTargets.Add(target.RuntimeInstanceId);
            }
            if (!target.TryGetDebugView(out AnimationPresentationDebugView debugView))
                return;
            AnimationFootPlacementRuntimeSnapshot footPlacement = debugView.PosePlan.FootPlacement;
            if (footPlacement.IsAvailable &&
                footPlacement.LandingPrediction.RootInstanceId != 0)
            {
                int rootInstanceId = footPlacement.LandingPrediction.RootInstanceId;
                if (s_TargetRootInstanceId != 0 && s_TargetRootInstanceId != rootInstanceId)
                {
                    throw new InvalidOperationException(
                        "Gameplay Lab player Animation Presentation root changed after sampling target binding.");
                }
                s_TargetRootInstanceId = rootInstanceId;
            }
            IReadOnlyList<AnimationPoseWatchIdentity> watches = BuildPoseWatches(debugView.PosePlan);
            string signature = BuildPoseWatchSignature(watches);
            if (string.Equals(
                    s_PoseWatchSignatures.TryGetValue(target.RuntimeInstanceId, out string previous)
                        ? previous
                        : string.Empty,
                    signature,
                    StringComparison.Ordinal))
            {
                return;
            }
            s_PoseWatchSignatures[target.RuntimeInstanceId] = signature;
            target.SetPoseWatchInterests(s_DiagnosticsOwnerId, watches);
        }

        static void RemoveTarget(AnimationPresentationRuntimeTarget target)
        {
            if (target == null)
                return;
            s_ConfiguredTargets.Remove(target.RuntimeInstanceId);
            s_PoseWatchSignatures.Remove(target.RuntimeInstanceId);
        }

        static bool TryBindGameplayLabPlayerTarget()
        {
            int selectedHostInstanceId = 0;
            int selectedRootInstanceId = 0;
            CharacterRootHierarchyBinding selectedRootHierarchy = null;
            CharacterPipelineHost[] hosts = UnityEngine.Object.FindObjectsByType<CharacterPipelineHost>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < hosts.Length; i++)
            {
                CharacterPipelineHost host = hosts[i];
                if (host == null || !host.VisualRoot ||
                    !string.Equals(host.ActorId, GameplayLabPlayerActorId, StringComparison.Ordinal))
                    continue;
                if (selectedHostInstanceId != 0)
                    throw new InvalidOperationException(
                        "Gameplay Lab contains multiple gameplay-lab-player hosts.");
                selectedHostInstanceId = host.GetInstanceID();
                selectedRootInstanceId = host.VisualRoot.GetInstanceID();
                selectedRootHierarchy = host.RootHierarchy;
            }
            FixedCharacterHost[] fixedHosts = UnityEngine.Object.FindObjectsByType<FixedCharacterHost>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < fixedHosts.Length; i++)
            {
                FixedCharacterHost host = fixedHosts[i];
                if (host == null || !host.RootHierarchy ||
                    !string.Equals(host.ActorId.Value, GameplayLabPlayerActorId, StringComparison.Ordinal))
                    continue;
                if (selectedHostInstanceId != 0)
                    throw new InvalidOperationException(
                        "Gameplay Lab contains multiple gameplay-lab-player hosts.");
                selectedHostInstanceId = host.GetInstanceID();
                selectedRootInstanceId = host.RootHierarchy.VisualRoot.GetInstanceID();
                selectedRootHierarchy = host.RootHierarchy;
            }
            if (selectedHostInstanceId == 0)
            {
                ResetTargetBinding();
                return false;
            }
            s_TargetHostInstanceId = selectedHostInstanceId;
            s_TargetRootInstanceId = selectedRootInstanceId;
            s_TargetRootHierarchy = selectedRootHierarchy;
            return true;
        }

        static bool TryResolveSamplingProgramIdentity(
            out SamplingProgramIdentity identity)
        {
            identity = default;
            bool found = false;
            IReadOnlyList<AnimationPresentationRuntimeTarget> targets =
                AnimationPresentationRuntimeTargetRegistry.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                AnimationPresentationRuntimeTarget target = targets[i];
                if (target.HostInstanceId != s_TargetHostInstanceId)
                    continue;
                var candidate = new SamplingProgramIdentity(target.ProgramIdentity);
                if (found && !identity.Matches(in candidate))
                {
                    throw new InvalidOperationException(
                        "Gameplay Lab player exposes multiple compiled Animation Presentation Programs.");
                }
                identity = candidate;
                found = true;
            }
            return found;
        }

        static void ResetTargetBinding()
        {
            s_TargetHostInstanceId = 0;
            s_TargetRootInstanceId = 0;
            s_TargetRootHierarchy = null;
        }

        static IReadOnlyList<AnimationPoseWatchIdentity> BuildPoseWatches(
            AnimationPresentationRuntimeSnapshot snapshot)
        {
            var result = new List<AnimationPoseWatchIdentity>(4);
            AnimationReadOnlyBuffer<AnimationPoseOperationSnapshot> operations = snapshot.Operations;
            for (int i = 0; i < operations.Count; i++)
            {
                AnimationPoseOperationSnapshot operation = operations[i];
                if (operation.Code != CharacterPoseOperationCode.FootPlacement &&
                    operation.Code != CharacterPoseOperationCode.FullBodyIK)
                {
                    continue;
                }
                result.Add(new AnimationPoseWatchIdentity(
                    operation.GraphId,
                    snapshot.PoseGraphRevision,
                    operation.NodeId,
                    operation.CallSite));
            }
            return result;
        }

        static string BuildPoseWatchSignature(IReadOnlyList<AnimationPoseWatchIdentity> watches)
        {
            if (watches == null || watches.Count == 0)
                return string.Empty;
            var builder = new StringBuilder(256);
            for (int i = 0; i < watches.Count; i++)
            {
                if (builder.Length != 0)
                    builder.Append('|');
                builder.Append(watches[i]);
            }
            return builder.ToString();
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                StopAndSave();
        }

        static void OnBeforeAssemblyReload()
        {
            StopAndSave();
            WaitForFinalization();
        }

        static void OnEditorQuitting()
        {
            StopAndSave();
            WaitForFinalization();
        }

        static void CancelSamplingStart()
        {
            EditorApplication.update -= PollSamplingStart;
            DetachCapture();
            try
            {
                BeginFinalization(null);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            s_StartPending = false;
            s_StartDeadline = 0d;
            s_StartWaitReason = string.Empty;
            s_ControlledCaptureWindow = false;
            s_CaptureWindowOpen = false;
            s_PendingFrames.Clear();
            ResetTargetBinding();
        }

        static void DetachCapture()
        {
            CharacterFootLandingPredictionDebugRegistry.Published -= Capture;
            AnimationPresentationRuntimeTargetRegistry.TargetRegistered -= ConfigureTarget;
            AnimationPresentationRuntimeTargetRegistry.TargetUnregistered -= RemoveTarget;
            EditorApplication.update -= ProcessPendingFrames;
            RemoveTargetDiagnostics();
            s_Capturing = false;
            s_ControlledCaptureWindow = false;
            s_CaptureWindowOpen = false;
        }

        static string StopAndSave()
        {
            if (s_Finalization != null)
                return s_LastSavedPath;
            if (s_StartPending)
            {
                CancelSamplingStart();
                return s_LastSavedPath;
            }
            if (!s_Capturing)
                return s_LastSavedPath;
            Exception failure = null;
            try
            {
                ProcessPendingFramesCore();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            try
            {
                DetachCapture();
                BeginFinalization(failure);
                failure = null;
            }
            catch (Exception exception)
            {
                failure ??= exception;
            }
            finally
            {
                s_PendingFrames.Clear();
                ResetTargetBinding();
            }
            if (failure != null)
                Debug.LogException(failure);
            return s_LastSavedPath;
        }

        static void BeginFinalization(Exception captureFailure)
        {
            SamplingSession session = s_Session;
            s_Session = null;
            if (session == null)
                return;
            if (s_Finalization != null)
                throw new InvalidOperationException(
                    "Foot Landing finalization is already active.");
            s_LastSavedPath = session.Path;
            s_LastSavedGeometryPath = session.GeometryPath;
            s_LastSavedDirectory = session.DirectoryPath;
            s_LastSavedSampleIdentity = session.SampleIdentity.ToString("N");
            s_LastSavedFactsPath = string.Empty;
            s_LastSavedDiagnosisDirectory = string.Empty;
            s_LastDiagnosticSummary = "Foot Landing finalizing capture package.";
            s_LastFinalizationFailure = string.Empty;
            var job = new FinalizationJob(
                session,
                captureFailure,
                s_DroppedPendingFrameCount);
            s_Finalization = job;
            EditorApplication.update -= PollFinalization;
            EditorApplication.update += PollFinalization;
            job.Start();
            Debug.Log(
                $"Foot Landing sampling stopped and finalization started. " +
                $"Sample={s_LastSavedSampleIdentity}, " +
                $"AcceptedFrames={job.AcceptedFrameCount}, " +
                $"Samples={job.SamplesPath}, Geometry={job.GeometryPath}");
        }

        static void PollFinalization()
        {
            FinalizationJob job = s_Finalization;
            if (job == null)
            {
                EditorApplication.update -= PollFinalization;
                return;
            }
            if (!job.IsCompleted)
                return;
            CompleteFinalization(job);
        }

        static void WaitForFinalization()
        {
            FinalizationJob job = s_Finalization;
            if (job == null)
                return;
            job.Wait();
            CompleteFinalization(job);
        }

        static void CompleteFinalization(FinalizationJob job)
        {
            if (!ReferenceEquals(s_Finalization, job))
                return;
            EditorApplication.update -= PollFinalization;
            s_Finalization = null;
            s_LastSavedFrameCount = job.WrittenFrameCount;
            if (job.Failure != null)
            {
                s_LastFinalizationFailure = job.Failure.Message;
                s_LastDiagnosticSummary =
                    $"Foot Landing finalization failed: {job.Failure.Message}";
                s_LastSavedFactsPath = string.Empty;
                s_LastSavedDiagnosisDirectory = string.Empty;
                Debug.LogError(s_LastDiagnosticSummary);
                Debug.LogException(job.Failure);
                return;
            }
            if (job.WrittenFrameCount == 0)
            {
                s_LastSavedPath = string.Empty;
                s_LastSavedGeometryPath = string.Empty;
                s_LastSavedDirectory = string.Empty;
                s_LastSavedFactsPath = string.Empty;
                s_LastSavedDiagnosisDirectory = string.Empty;
                s_LastSavedSampleIdentity = string.Empty;
                s_LastDiagnosticSummary =
                    "Foot Landing sampling canceled before any Foot rows were captured.";
                s_LastFactEventCount = 0;
                s_LastDiagnosisTargetCount = 0;
                s_LastDiagnosisMatchCount = 0;
                Debug.Log(s_LastDiagnosticSummary);
                return;
            }
            ApplyAnalysis(job.Analysis);
            if (job.CaptureFailure != null)
            {
                s_LastFinalizationFailure = job.CaptureFailure.Message;
                Debug.LogError(
                    $"Foot Landing capture stopped early; the sealed partial package was analyzed. " +
                    $"Reason={job.CaptureFailure.Message}");
                Debug.LogException(job.CaptureFailure);
            }
            Debug.Log(
                $"Foot Landing sampling finalized {s_LastSavedFrameCount} frames " +
                $"with {job.DroppedPendingFrameCount} dropped pending frames. " +
                $"Sample={s_LastSavedSampleIdentity}, " +
                $"Samples={s_LastSavedPath}, Geometry={s_LastSavedGeometryPath}, " +
                $"Facts={s_LastSavedFactsPath}, " +
                $"Diagnoses={s_LastSavedDiagnosisDirectory}, " +
                $"Summary={s_LastDiagnosticSummary}");
        }

        static void ApplyAnalysis(
            CharacterFootMotionDiagnosticAnalysis analysis)
        {
            s_LastSavedPath = analysis.SamplesPath;
            s_LastSavedGeometryPath = analysis.GeometryPath;
            s_LastSavedDirectory = System.IO.Path.GetDirectoryName(
                analysis.SamplesPath) ?? string.Empty;
            s_LastSavedFactsPath = analysis.FactsPath;
            s_LastSavedDiagnosisDirectory = analysis.DiagnosisDirectory;
            s_LastDiagnosticSummary = analysis.Summary;
            s_LastFactEventCount = analysis.EventCount;
            s_LastDiagnosisTargetCount = analysis.DiagnosisTargetCount;
            s_LastDiagnosisMatchCount = analysis.DiagnosisMatchCount;
        }

        static void FailActiveSampling(Exception exception)
        {
            string path = s_Session?.Path ?? s_LastSavedPath;
            try
            {
                DetachCapture();
                BeginFinalization(exception);
            }
            catch (Exception finalizationException)
            {
                Debug.LogException(finalizationException);
            }
            finally
            {
                s_PendingFrames.Clear();
                ResetTargetBinding();
            }
            Debug.LogError(
                $"Foot Landing sampling stopped early and is finalizing its completed portion. " +
                $"Samples={path}, Reason={exception.Message}");
        }

        static void FailSamplingStart(string message)
        {
            s_LastStartFailure = string.IsNullOrWhiteSpace(message)
                ? "Foot Landing sampling could not bind the Gameplay Lab player."
                : message;
            CancelSamplingStart();
            Debug.LogError(s_LastStartFailure);
        }

        static void RemoveTargetDiagnostics()
        {
            IReadOnlyList<AnimationPresentationRuntimeTarget> targets =
                AnimationPresentationRuntimeTargetRegistry.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                AnimationPresentationRuntimeTarget target = targets[i];
                if (!s_ConfiguredTargets.Contains(target.RuntimeInstanceId))
                    continue;
                target.RemovePoseWatchInterests(s_DiagnosticsOwnerId);
                target.RemoveDiagnosticsInterest(s_DiagnosticsOwnerId);
            }
            s_ConfiguredTargets.Clear();
            s_PoseWatchSignatures.Clear();
        }

        static string FindLatestSavedPath()
        {
            string directory = ResolveSaveDirectory();
            if (!Directory.Exists(directory))
                return string.Empty;
            string latestPath = string.Empty;
            DateTime latestWriteTime = DateTime.MinValue;
            foreach (string path in Directory.EnumerateFiles(
                         directory,
                         "samples.csv",
                         SearchOption.AllDirectories))
            {
                DateTime writeTime = File.GetLastWriteTimeUtc(path);
                if (writeTime <= latestWriteTime)
                    continue;
                latestWriteTime = writeTime;
                latestPath = path;
            }
            return latestPath;
        }

        static string ResolveSaveDirectory() => Path.GetFullPath(Path.Combine(
            Application.dataPath,
            "..",
            "Diagnostics",
            "FootPlacementRuns"));

        static void WriteSampleRow(
            SamplingSession session,
            StreamWriter writer,
            StringBuilder row,
            in CharacterFootLandingPredictionDiagnostics frame,
            in CharacterFootLandingPredictionFootDiagnostics foot,
            in FootIkCapture ik,
            in FootStepObservationCapture footStepObservation,
            in RootHierarchyCapture roots,
            Guid targetRuntimeInstanceId,
            int targetHostInstanceId)
        {
            row.Clear();
            CharacterFootLandingPredictionInputDiagnostics input = frame.Input;
            CharacterFootPlacementQueryRequest query = foot.Query;
            Add(row, session.SampleIdentity.ToString("N"));
            Add(row, session.StartedUtc.ToString("O", CultureInfo.InvariantCulture));
            Add(row, session.Program.ProgramIdentity);
            Add(row, session.Program.ProjectionRevision);
            Add(row, session.Program.PoseGraphId);
            Add(row, session.Program.PoseGraphRevision);
            Add(row, session.Program.PosePlanHash);
            Add(row, frame.FrameSequence);
            Add(row, frame.CompletionIdentity);
            Add(row, targetRuntimeInstanceId.ToString("N"));
            Add(row, targetHostInstanceId);
            Add(row, frame.RootInstanceId);
            Add(row, frame.ProfileId);
            Add(row, frame.ProfileRevision);
            Add(row, foot.Side.ToString());
            Add(row, foot.State.ToString());
            Add(row, foot.RejectReason.ToString());
            Add(row, foot.StepSource.ToString());
            Add(row, foot.LandingEventIdentity);
            Add(row, foot.TrajectoryGeneration);
            Add(row, foot.LandingConfidence);
            Add(row, foot.TimeToLandingSeconds);
            Add(row, foot.NextLandingTrackingState);
            Add(row, foot.NextLandingTrackingEventIdentity);
            Add(row, foot.VerifiedLastLandingAvailable);
            Add(row, foot.VerifiedLastLandingEventIdentity);
            Add(row, foot.PlantTargetState);
            Add(row, foot.PlantTargetAvailable);
            Add(row, foot.PlantTargetEventIdentity);
            Add(row, foot.PlantTargetSurfaceIdentity);
            Add(row, foot.PlantTargetPoint);
            Add(row, foot.PlantTargetNormal);
            Add(row, foot.PlantTargetTrajectoryGeneration);
            Add(row, foot.PlantTargetFutureBodyTranslationSourceIdentity);
            Add(row, foot.PlantTargetUpdated);
            Add(row, foot.PlantVerificationAttempted);
            Add(row, foot.PlantVerificationUnavailable);
            Add(row, foot.ApproachPlantTargetPrepared);
            CharacterFootStepCandidateSelectionDiagnostics stepSelection =
                foot.StepCandidateSelection;
            Add(row, stepSelection.MaximumPredictionTimeSeconds);
            Add(row, stepSelection.LastLandingEventIdentity);
            Add(row, stepSelection.SelectedSource.ToString());
            Add(row, stepSelection.SelectedLandingEventIdentity);
            CharacterFootStepCandidateDiagnostics selectedStep =
                stepSelection.SelectedSource ==
                CharacterFootLandingStepSource.FormalNextLanding
                    ? stepSelection.Current
                    : default;
            AddStepPhase(row, in selectedStep);
            AddStepCandidate(row, stepSelection.Current);
            AddStepCandidate(row, stepSelection.Incoming);
            AnimationFootMotionRuntimeSample observedStep =
                foot.Side == CharacterFootSide.Left
                    ? footStepObservation.Left
                    : footStepObservation.Right;
            bool hasObservedStep = footStepObservation.IsValid && observedStep.IsValid;
            Add(row, hasObservedStep);
            Add(row, hasObservedStep ? footStepObservation.SourceIdentity : string.Empty);
            Add(row, hasObservedStep ? footStepObservation.Weight : 0f);
            Add(row, hasObservedStep ? footStepObservation.NormalizedTime : 0f);
            Add(row, hasObservedStep ? observedStep.TimeToLandingSeconds : 0f);
            Add(row, hasObservedStep ? observedStep.Distance : 0f);
            Add(row, hasObservedStep ? observedStep.FootHeight : 0f);
            Add(row, hasObservedStep ? observedStep.ToeHeight : 0f);
            Add(row, hasObservedStep ? observedStep.ToeSpeed : 0f);
            Add(row, hasObservedStep ? observedStep.PositionError : 0f);
            Add(row, hasObservedStep ? observedStep.RotationError : 0f);
            Add(row, hasObservedStep ? observedStep.Contact : 0f);
            Add(row, hasObservedStep ? observedStep.LockMode.ToString() : string.Empty);
            Add(row, hasObservedStep ? observedStep.LockWeight : 0f);
            Add(row, hasObservedStep ? observedStep.Support : 0f);
            AddFormalEventFrame(row, hasObservedStep, observedStep.Events);
            CharacterFootStepObservationInputDiagnostics inputObservation =
                input.FootStepObservation;
            AnimationFootMotionRuntimeSample inputObservedStep =
                foot.Side == CharacterFootSide.Left
                    ? inputObservation.Left
                    : inputObservation.Right;
            bool hasInputObservedStep = inputObservation.IsValid && inputObservedStep.IsValid;
            Add(row, hasInputObservedStep);
            Add(row, hasInputObservedStep ? inputObservation.SourceId : string.Empty);
            Add(row, hasInputObservedStep ? inputObservation.SourceIdentity : string.Empty);
            Add(row, hasInputObservedStep ? inputObservation.SourceWeight : 0f);
            Add(row, hasInputObservedStep ? inputObservation.NormalizedTime : 0f);
            Add(row, hasInputObservedStep ? inputObservation.ClipBindingIndex : -1);
            Add(row, hasInputObservedStep ? inputObservation.Cycle : 0);
            Add(row, hasInputObservedStep
                ? inputObservation.ContributionContinuityIdentity
                : 0UL);
            Add(row, hasInputObservedStep ? inputObservation.CompletionIdentity : 0UL);
            Add(row, hasInputObservedStep ? inputObservedStep.TimeToLandingSeconds : 0f);
            Add(row, hasInputObservedStep ? inputObservedStep.Distance : 0f);
            Add(row, hasInputObservedStep ? inputObservedStep.FootHeight : 0f);
            Add(row, hasInputObservedStep ? inputObservedStep.ToeHeight : 0f);
            Add(row, hasInputObservedStep ? inputObservedStep.ToeSpeed : 0f);
            Add(row, hasInputObservedStep ? inputObservedStep.PositionError : 0f);
            Add(row, hasInputObservedStep ? inputObservedStep.RotationError : 0f);
            Add(row, hasInputObservedStep ? inputObservedStep.Contact : 0f);
            Add(row, hasInputObservedStep ? inputObservedStep.LockMode.ToString() : string.Empty);
            Add(row, hasInputObservedStep ? inputObservedStep.LockWeight : 0f);
            Add(row, hasInputObservedStep ? inputObservedStep.Support : 0f);
            AddFormalEventFrame(row, hasInputObservedStep, inputObservedStep.Events);
            Add(row, foot.RootLocalLanding);
            Add(row, input.PresentationDeltaSeconds);
            Add(row, input.PreviousBodyTick);
            Add(row, input.CurrentBodyTick);
            Add(row, input.BodySampleAlpha);
            Add(row, input.BodySampleAgeSeconds);
            Add(row, input.MotionTimelineAvailable);
            Add(row, input.TimelineGeneration);
            Add(row, input.TimelineAuthorityTick);
            Add(row, input.TimelineTickRate);
            Add(row, input.TimelineCurrentVelocityX);
            Add(row, input.TimelineCurrentVelocityZ);
            Add(row, input.TimelineContinuationVelocityX);
            Add(row, input.TimelineContinuationVelocityZ);
            Add(row, input.TimelineHasContinuation);
            Add(row, input.TimelineBodyYawVelocityDegreesPerSecond);
            Add(row, input.TimelineMaximumBodyYawVelocityDegreesPerSecond);
            Add(row, input.CurrentSegmentRemainingSeconds);
            Add(row, input.PredictionMotionAvailable);
            Add(row, input.PredictionMotionRejectReason);
            Add(row, input.PredictionMotionResetReason);
            Add(row, input.PredictionMotionSourceIdentity);
            Add(row, input.PredictionRawCurrentVelocityX);
            Add(row, input.PredictionRawCurrentVelocityZ);
            Add(row, input.PredictionRawContinuationVelocityX);
            Add(row, input.PredictionRawContinuationVelocityZ);
            Add(row, input.PredictionPreviousStableCurrentVelocityX);
            Add(row, input.PredictionPreviousStableCurrentVelocityZ);
            Add(row, input.PredictionPreviousStableContinuationVelocityX);
            Add(row, input.PredictionPreviousStableContinuationVelocityZ);
            Add(row, input.PredictionStableCurrentVelocityX);
            Add(row, input.PredictionStableCurrentVelocityZ);
            Add(row, input.PredictionStableContinuationVelocityX);
            Add(row, input.PredictionStableContinuationVelocityZ);
            Add(row, input.PredictionCurrentVelocityDeltaX);
            Add(row, input.PredictionCurrentVelocityDeltaZ);
            Add(row, input.PredictionContinuationVelocityDeltaX);
            Add(row, input.PredictionContinuationVelocityDeltaZ);
            Add(row, input.PredictionVelocityResponseAlpha);
            Add(row, input.PredictionVelocityDeltaThreshold);
            Add(row, input.PredictionVelocitySmoothSpeed);
            Add(row, input.PredictionMaximumSpeed);
            Add(row, input.PredictionCurrentResponseApplied);
            Add(row, input.PredictionContinuationResponseApplied);
            Add(row, input.PredictionCurrentMaximumSpeedClamped);
            Add(row, input.PredictionContinuationMaximumSpeedClamped);
            Add(row, input.PredictionMotionRevision);
            Add(row, input.Grounded);
            Add(row, input.HorizontalSpeed);
            Add(row, input.LeftActionInstanceIdentity);
            Add(row, input.LeftActionFootWeight);
            Add(row, input.RightActionInstanceIdentity);
            Add(row, input.RightActionFootWeight);
            Add(row, frame.PrimarySupport.HasValue);
            Add(row, frame.PrimarySupport.Side.ToString());
            Add(row, frame.PrimarySupport.LandingEventIdentity);
            Add(row, frame.PrimarySupport.Retained);
            Add(row, roots.LogicRootPosition);
            Add(row, roots.LogicRootRotation);
            Add(row, roots.VisualRootLocalPosition);
            Add(row, roots.VisualRootLocalRotation);
            Add(row, roots.VisualRootWorldPosition);
            Add(row, roots.VisualRootWorldRotation);
            Add(row, roots.PoseRootLocalPosition);
            Add(row, roots.PoseRootLocalRotation);
            Add(row, roots.PoseRootWorldPosition);
            Add(row, roots.PoseRootWorldRotation);
            Add(row, input.VisibleBodyPosition);
            Add(row, input.VisibleBodyRotation);
            Add(row, input.VisibleBodyVelocity);
            Add(row, input.VisibleBodyYawVelocityDegreesPerSecond);
            Add(row, input.TargetBodyPosition);
            Add(row, input.TargetBodyRotation);
            Add(row, input.TargetBodyVelocity);
            Add(row, input.TargetBodyYawVelocityDegreesPerSecond);
            Add(row, input.BodyPositionError);
            Add(row, input.BodyRotationError);
            Add(row, input.CorrectionPositionError);
            Add(row, input.CorrectionPositionVelocity);
            Add(row, input.CorrectionYawVelocityDegreesPerSecond);
            Add(row, input.CorrectionActive);
            Add(row, input.CorrectionClamped);
            Add(row, input.CorrectionSettled);
            Add(row, input.BodyResetSequence);
            Add(row, foot.FutureBodyTranslationAvailable);
            Add(row, foot.FutureBodyRelativeTranslation);
            Add(row, foot.FutureBodyTranslationVelocity);
            Add(row, foot.CurrentAnimatedSole);
            Add(row, foot.RawLandingAvailable);
            Add(row, foot.RawLandingCandidate);
            CharacterFootLandingObservationDiagnostics observation =
                foot.Observation;
            Add(row, observation.Identity);
            Add(row, observation.WorldRevision);
            Add(row, observation.SourceSampleIdentity);
            Add(row, observation.SourceSampleCycle);
            Add(row, observation.CacheState.ToString());
            Add(row, observation.QueryExecutedThisFrame);
            Add(row, observation.QueryPurpose.ToString());
            Add(row, observation.RefreshMode.ToString());
            Add(row, observation.QueryReason.ToString());
            Add(row, observation.CanonicalRawLanding);
            Add(row, observation.CanonicalComponentUp);
            Add(row, observation.CandidateRawLanding);
            Add(row, observation.CandidateComponentUp);
            Add(row, observation.QueryInputDistance);
            Add(row, observation.QueryComponentUpAngleDegrees);
            Add(row, observation.PredictionInputAccumulationDistance);
            Add(row, observation.ComponentUpChangeAngleDegrees);
            Add(row, query.Shape.ToString());
            Add(row, query.Purpose.ToString());
            Add(row, query.FootIndex);
            Add(row, query.Origin);
            Add(row, query.Direction);
            Add(row, query.MaximumDistance);
            Add(row, query.Radius);
            Add(row, query.LayerMask);
            Add(row, query.MinimumGroundNormalDot);
            CharacterFootLandingQuerySelectionDiagnostics querySelection =
                foot.QuerySelection;
            CharacterFootLandingQueryCandidateDiagnostics selectedCandidate =
                querySelection.Selected;
            Add(row, querySelection.State.ToString());
            Add(row, querySelection.ValidCandidateCount);
            Add(row, selectedCandidate.IsAvailable);
            Add(row, selectedCandidate.SurfaceIdentity);
            Add(row, selectedCandidate.Point);
            Add(row, selectedCandidate.Distance);
            Add(row, foot.Accepted);
            Add(row, foot.SurfaceIdentity);
            Add(row, foot.LandingPoint);
            Add(row, foot.LandingNormal);
            Add(row, foot.QueryDistance);
            CharacterFootGroundPathDiagnostics ground = foot.GroundPath;
            CharacterFootGroundPathQueryRequest groundQuery = ground.Query;
            Add(row, ground.State.ToString());
            Add(row, ground.RejectReason.ToString());
            Add(row, ground.InputIdentity);
            Add(row, ground.QueryExecuted);
            Add(row, ground.NextSwingLandingEventIdentity != 0);
            Add(row, ground.LastLandingEventIdentity);
            Add(row, ground.NextSwingLandingEventIdentity);
            Add(row, ground.TrajectoryGeneration);
            Add(row, ground.AuthorityTick);
            Add(row, ground.LastFutureBodyTranslationSourceIdentity);
            Add(row, ground.NextSwingFutureBodyTranslationSourceIdentity);
            Add(row, ground.LastLanding);
            Add(row, ground.NextSwingLanding);
            Add(row, ground.LastLandingNormal);
            Add(row, ground.NextSwingLandingNormal);
            Add(row, ground.LastLandingSurfaceIdentity);
            Add(row, ground.NextSwingLandingSurfaceIdentity);
            Add(row, ground.ComponentUp);
            Add(row, groundQuery.AxisStart);
            Add(row, groundQuery.AxisEnd);
            Add(row, groundQuery.Radius);
            Add(row, groundQuery.MaximumAxisSegmentLength);
            Add(row, groundQuery.Direction);
            Add(row, groundQuery.MaximumDistance);
            Add(row, groundQuery.LayerMask);
            Add(row, groundQuery.SegmentHitCapacity);
            Add(row, groundQuery.ContactCapacity);
            Add(row, ground.SegmentCount);
            Add(row, ground.ContactCount);
            Add(row, ground.EdgeCount);
            Add(row, ground.HasInvalidSegment);
            Add(row, ground.FirstInvalidSegmentIndex);
            Add(row, ground.FirstInvalidSegmentIdentity);
            Add(row, ground.FirstInvalidSegmentBottom);
            Add(row, ground.FirstInvalidSegmentTop);
            Add(row, ground.FirstInvalidSegmentVerticalDistance);
            Add(row, ground.MaximumReachableVerticalEdge);
            Add(row, ground.EnvelopeVertexCount);
            CharacterFootSwingMotionDiagnostics motion = foot.FootMotion;
            CharacterFullBodyIkGoal footGoal = foot.Goal;
            Add(row, motion.State.ToString());
            Add(row, motion.RejectReason.ToString());
            Add(row, motion.LandingEventIdentity);
            Add(row, motion.GroundPathInputIdentity);
            Add(row, motion.Distance);
            Add(row, motion.Progress);
            Add(row, motion.OriginalSole);
            Add(row, motion.OriginalAnkle);
            Add(row, foot.SourceAnkleRotation);
            Add(row, foot.SourceHeelPosition);
            Add(row, foot.SourceToePosition);
            Add(row, motion.BaselineSample);
            Vector3 motionUp =
                motion.TargetHeightComponentUp.sqrMagnitude > 0.000001f
                ? motion.TargetHeightComponentUp.normalized
                : default;
            Vector3 groundPathUp = ground.ComponentUp.sqrMagnitude > 0.000001f
                ? ground.ComponentUp.normalized
                : default;
            float originalSoleAlongUp = Vector3.Dot(
                motion.OriginalSole,
                motionUp);
            float baselineSampleAlongUp = Vector3.Dot(
                motion.BaselineSample,
                motionUp);
            Add(row, baselineSampleAlongUp);
            Add(row, motion.EnvelopeSample);
            float envelopeSampleAlongUp = Vector3.Dot(
                motion.EnvelopeSample,
                motionUp);
            float motionFormalFootHeight = hasInputObservedStep
                ? inputObservedStep.FootHeight
                : 0f;
            float rawFormalTargetHeight =
                envelopeSampleAlongUp + motionFormalFootHeight;
            float envelopeMinimumCorrection =
                envelopeSampleAlongUp - originalSoleAlongUp;
            float builderSelectedCorrection = Mathf.Max(
                0f,
                rawFormalTargetHeight - originalSoleAlongUp);
            bool builderSwingTargetAvailable =
                motion.PathContinuityEvaluated &&
                motion.PathAvailableAfter &&
                motion.PathCurrentLandingEventIdentity ==
                motion.LandingEventIdentity;
            Vector3 builderSwingTargetCorrection =
                builderSwingTargetAvailable
                    ? motion.PathCurrentTargetCorrection
                    : default;
            Add(row, envelopeSampleAlongUp);
            Add(row, motionFormalFootHeight);
            Add(row, rawFormalTargetHeight);
            Add(row, envelopeMinimumCorrection);
            Add(row, builderSelectedCorrection);
            Add(row, builderSwingTargetAvailable);
            Add(row, builderSwingTargetCorrection);
            CharacterFootActualEnvelopeIntersectionFact actualEnvelope =
                ResolveActualFootEnvelopeIntersection(
                    in ground,
                    in motion,
                    groundPathUp);
            CharacterFootSwingPathHorizontalAxisState horizontalAxisState =
                actualEnvelope.State switch
                {
                    CharacterFootActualEnvelopeIntersectionState.Unavailable =>
                        CharacterFootSwingPathHorizontalAxisState.Unavailable,
                    CharacterFootActualEnvelopeIntersectionState.InvalidComponentUp =>
                        CharacterFootSwingPathHorizontalAxisState.InvalidComponentUp,
                    CharacterFootActualEnvelopeIntersectionState.DegenerateAxis =>
                        CharacterFootSwingPathHorizontalAxisState.DegenerateAxis,
                    _ => CharacterFootSwingPathHorizontalAxisState.Available
                };
            Add(row, horizontalAxisState.ToString());
            Add(row, actualEnvelope.ActualFootHorizontalDistance);
            Add(row, actualEnvelope.BaselineHorizontalDistance);
            Add(row, actualEnvelope.EnvelopeHorizontalDistance);
            Add(
                row,
                actualEnvelope.ActualFootHorizontalDistance -
                actualEnvelope.EnvelopeHorizontalDistance);
            Add(row, actualEnvelope.AxisRegion.ToString());
            Add(row, actualEnvelope.ClosestPathParameter);
            Add(row, actualEnvelope.DistanceAlongAxis);
            Add(row, actualEnvelope.CrossTrackDistance);
            Add(row, actualEnvelope.CorridorRadius);
            Add(row, actualEnvelope.WithinGroundPathCorridor);
            Add(row, actualEnvelope.State.ToString());
            Add(row, actualEnvelope.CandidateCount);
            Add(row, actualEnvelope.MinimumHeightAlongUp);
            Add(row, actualEnvelope.MaximumHeightAlongUp);
            Add(row, actualEnvelope.HeightSpan);
            Add(row, actualEnvelope.HasVerticalEdge);
            Add(row, actualEnvelope.HasMultipleHeights);
            Add(row, actualEnvelope.Ambiguous);
            Add(row, actualEnvelope.CounterfactualState.ToString());
            bool actualEnvelopeCorrectionAvailable =
                actualEnvelope.CounterfactualState ==
                CharacterFootActualEnvelopeCounterfactualState
                    .UniqueInCorridor &&
                builderSwingTargetAvailable;
            float actualEnvelopeMinimumCorrection =
                actualEnvelopeCorrectionAvailable
                    ? actualEnvelope.MinimumHeightAlongUp -
                      originalSoleAlongUp
                    : 0f;
            float builderSwingTargetAlongUp =
                builderSwingTargetAvailable
                    ? Vector3.Dot(builderSwingTargetCorrection, motionUp)
                    : 0f;
            float actualEnvelopeAdvanceAboveBuilderTarget =
                actualEnvelopeCorrectionAvailable
                    ? Mathf.Max(
                        0f,
                        actualEnvelopeMinimumCorrection -
                        builderSwingTargetAlongUp)
                    : 0f;
            Add(row, actualEnvelopeCorrectionAvailable);
            Add(row, actualEnvelopeMinimumCorrection);
            Add(row, actualEnvelopeAdvanceAboveBuilderTarget);
            Add(row, motion.LandingPredictionError);

            Add(row, motion.CorrectedSole);
            Add(row, motion.CorrectedAnkle);
            Add(row, motion.PositionWeight);
            Add(row, motion.RotationWeight);
            Add(row, motion.ConstraintState.ToString());
            Add(row, motion.LockResponse.ToString());
            Add(row, motion.SupportHorizontalError);
            Add(row, motion.ContactOwnership);
            Add(row, motion.SupportWeight);
            Add(row, motion.LandingReachEvaluated);
            Add(row, motion.LandingReachAvailable);
            Add(row, motion.LandingReachGoalClamped);
            Add(row, motion.LandingReachGoalClampDistance);
            Add(row, motion.SupportContactAnchor);
            Add(row, motion.ContactPlaneAvailable);
            Add(row, motion.ContactSurfaceIdentity);
            Add(row, motion.ContactPlaneNormal);
            Add(
                row,
                ResolvePenetrationAvailability(in frame, in motion, in ik)
                    .ToString());
            Add(row, motion.DesiredCorrection);
            Add(row, motion.PathContinuityEvaluated);
            Add(row, motion.PathRevisionReason);
            Add(row, motion.PathResidualRebuilt);
            Add(row, motion.TargetTrackingApplied);
            Add(row, motion.PathAvailableBefore);
            Add(row, motion.PathAvailableAfter);
            Add(row, motion.PathPreviousLandingEventIdentity);
            Add(row, motion.PathCurrentLandingEventIdentity);
            Add(row, motion.PathPreviousTargetCorrection);
            Add(row, motion.PathCurrentTargetCorrection);
            Add(row, motion.PathLandingPointDelta);
            Add(row, motion.PathTargetDelta);
            Add(row, motion.SwingResidualBeforeRevision);
            Add(row, motion.SwingResidualBeforeDecay);
            Add(row, motion.SwingResidualAfterDecay);
            Add(row, motion.ResidualOutputCorrection);
            Add(row, motion.LandingAcceptanceDistance);
            Add(row, motion.PathRevisionDistance);
            Add(row, motion.SwingResidualTolerance);
            Add(row, motion.ResidualTimeToLandingSeconds);
            Add(row, motion.ResidualBaseHalfLifeSeconds);
            Add(row, motion.ResidualDeadlineHalfLifeAvailable);
            Add(row, motion.ResidualDeadlineHalfLifeSeconds);
            Add(row, motion.ResidualAppliedHalfLifeSeconds);
            Add(row, motion.SwingTargetHeightAdoptionMode);
            Add(row, motion.SwingRawTargetHeightAlongUp);
            Add(row, motion.SwingFilteredTargetHeightBefore);
            Add(row, motion.SwingTargetHeightDelta);
            Add(row, motion.SwingTargetHeightAppliedDelta);
            Add(row, motion.SwingTargetHeightUpdateHeld);
            Add(row, motion.SwingTargetHeightForceRefreshed);
            Add(row, motion.SwingTargetHeightRateLimited);
            Add(row, motion.SwingTargetHeightClamped);
            Add(row, motion.SwingTargetHeightForceRefreshDistance);
            Add(row, motion.SwingTargetMaximumVerticalSpeed);
            Add(row, motion.SwingFilteredTargetHeightAlongUp);
            Add(row, motion.TargetHeightComponentUp);
            Add(row, motion.PreTransitionReason);
            Add(row, motion.PreTransitionSource.ToString());
            Add(row, motion.PreTransitionTarget.ToString());
            Add(row, motion.PreTransitionAnchorCommand);
            Add(row, motion.PostTransitionReason);
            Add(row, motion.PostTransitionSource.ToString());
            Add(row, motion.PostTransitionTarget.ToString());
            Add(row, motion.PostTransitionAnchorCommand);
            Add(row, motion.StateTargetCorrection);
            Add(row, motion.InterpolationPolicy);
            Add(row, motion.InterpolationOutputCorrection);
            Add(row, motion.InterpolationCompleted);
            Add(row, motion.ConstraintStateBefore.ToString());
            Add(row, motion.LockResponseBefore.ToString());
            Add(row, motion.OutputStagesAvailable);
            Add(row, motion.ReleasingCompletedToSwing);
            Add(row, motion.SafetyFloorAvailable);
            Add(row, motion.SafetyFloorOwner.ToString());
            Add(row, motion.SafetyFloorOwnerSurfaceIdentity);
            Add(row, motion.SafetyFloorOwnerPathIdentity);
            Add(row, motion.CorrectionBeforeSafetyFloor);
            Add(row, motion.SafetyFloorMinimumCorrection);
            Add(row, motion.SafetyFloorOutputCorrection);
            Add(row, motion.FinalEffectiveCorrection);
            Add(row, motion.SafetyFloorClamped);
            Add(row, motion.SafetyFloorClampMeters);
            Add(row, motion.SafetyFloorClearanceBeforeMeters);
            Add(row, motion.SafetyFloorClearanceAfterMeters);
            Add(row, motion.PlantInterpolationEvaluated);
            Add(row, motion.PlantTargetEventIdentity);
            Add(row, motion.PlantTargetVerified);
            Add(row, motion.PlantTargetKind);
            Add(row, motion.PlantLockResponse.ToString());
            Add(row, motion.PlantLockWeightCompleted);
            Add(row, motion.PlantDesiredPoint);
            Add(row, motion.PlantFilteredPoint);
            AddSupportTarget(row, motion.SelectedSupportTarget);
            Add(row, motion.PlantTargetHeightAdoptionMode);
            Add(row, motion.PlantTargetMaximumVerticalSpeed);
            Add(row, motion.PlantTargetHeightBefore);
            Add(row, motion.PlantTargetHeightTarget);
            Add(row, motion.PlantTargetVerticalDelta);
            Add(row, motion.PlantTargetAppliedVerticalDelta);
            Add(row, motion.PlantTargetHeightAfter);
            Add(row, motion.PlantTargetHeightEventIdentity);
            Add(row, motion.PlantTargetHeightUpdateReason);
            Add(row, motion.PlantTargetForceRefreshed);
            Add(row, motion.PlantTargetForceRefreshDistance);
            Add(row, motion.PlantTargetVerticalClamped);
            Add(row, motion.PlantPreviousSelectedWorldTarget);
            Add(row, motion.PlantSelectedWorldTarget);
            Add(row, motion.PreviousResponseOutputAvailable);
            Add(row, motion.PreviousResponseOutputPoint);
            Add(row, motion.DesiredOutputPoint);
            Add(row, motion.ResponseOutputPoint);
            Add(row, motion.PlantResidualCaptureReason);
            Add(row, motion.PlantWorldResidualBeforeCapture);
            Add(row, motion.PlantWorldResidualCapturedBeforeDecay);
            Add(row, motion.PlantWorldResidualDecayApplied);
            Add(row, motion.PlantWorldResidualBaseHalfLifeSeconds);
            Add(row, motion.PlantWorldResidualDeadlineHalfLifeAvailable);
            Add(row, motion.PlantWorldResidualDeadlineHalfLifeSeconds);
            Add(row, motion.PlantWorldResidualAppliedHalfLifeSeconds);
            Add(row, motion.PlantWorldResidualAfterDecay);
            Add(row, motion.PlantWorldResidualCompletionTolerance);
            Add(row, motion.PlantWorldResidualClearedAtCompletionTolerance);
            Add(row, motion.CorrectionResponseEvaluated);
            Add(row, motion.CorrectionResponseInitializedBefore);
            Add(row, motion.CorrectionResponseInitializedThisFrame);
            Add(row, motion.CorrectionResponseInitializationReason);
            Add(row, motion.CorrectionResponseDesired);
            Add(row, motion.CorrectionResponseRequestedDirection);
            Add(row, motion.CorrectionResponsePreviousDirection);
            Add(row, motion.CorrectionResponseDirectionLimited);
            Add(row, motion.CorrectionResponseMaximumDirectionChangeDegrees);
            Add(row, motion.CorrectionResponseAppliedDirectionChangeDegrees);
            Add(row, motion.CorrectionResponseVisibleOutputTransferred);
            Add(row, motion.CorrectionResponseBeforeRebase);
            Add(row, motion.CorrectionResponsePrevious);
            Add(row, motion.CorrectionResponseCurrent);
            Add(row, motion.CorrectionResponseDirection);
            Add(row, motion.CorrectionResponseDeltaDirection);
            Add(row, motion.CorrectionResponseSelectedSpeed);
            Add(row, motion.CorrectionResponseAppliedDelta);
            Add(row, motion.PlantVerticalContinuityOwners);
            Add(row, motion.PlantEffectiveCorrectionBefore);
            Add(row, motion.PlantEffectiveCorrectionAfter);
            Add(row, motion.PlantOutputDistance);
            Add(row, motion.PlantPenetrationDepth);
            AddCurrentSupport(row, foot.CurrentSupport);
            AddResolvedFoot(row, foot.Resolved);
            Add(row, footGoal.IsValid);
            Add(
                row,
                footGoal.IsValid
                    ? footGoal.ComponentPosition - motion.OriginalAnkle
                    : default);
            Add(row, foot.Goal.ComponentPosition);
            Add(row, foot.Goal.ComponentRotation);
            Add(row, foot.Goal.PositionWeight);
            Add(row, foot.Goal.RotationWeight);
            Add(row, frame.PelvisGoal.PositionWeight);
            Add(row, frame.PelvisGoal.RotationWeight);
            CharacterFootStrideHipsDiagnostics stride = frame.StrideHips;
            Add(row, stride.State.ToString());
            Add(row, stride.RejectReason.ToString());
            Add(row, stride.SupportSide.ToString());
            Add(row, stride.SwingSide.ToString());
            Add(row, stride.Progress);
            Add(row, stride.Slope.ToString());
            Add(row, stride.StrideStart);
            Add(row, stride.StrideEnd);
            Add(row, stride.SampledGround);
            Add(row, stride.PoseRootPosition);
            Add(row, stride.AnimatedPelvis);
            Add(row, stride.AnimatedPelvisComponentPosition);
            Add(row, stride.RawPelvisDelta);
            Add(row, stride.RootRelativeGroundTargetAlongUp);
            Add(row, stride.SoleClearanceLiftAlongUp);
            Add(row, stride.HadPreviousState);
            Add(row, stride.SupportChanged);
            Add(row, stride.PreviousSlope.ToString());
            Add(row, stride.SpringHandoffReason.ToString().Replace(", ", "|"));
            Add(row, stride.SpringVelocityReset);
            Add(row, stride.PreviousSpringTarget);
            Add(row, stride.PreviousSpringOutput);
            Add(row, stride.PreviousSpringVelocity);
            Add(row, stride.SpringInput);
            Add(row, stride.SpringInputVelocity);
            Add(row, stride.SpringFrequency);
            Add(row, stride.UnclampedSpringTarget);
            Add(row, stride.SupportReachAvailable);
            Add(row, stride.SupportLegCompressionReserve);
            Add(row, stride.SupportReachUsableLegLength);
            Add(row, stride.SupportReachMinimumAlongUp);
            Add(row, stride.SupportReachMaximumAlongUp);
            Add(row, stride.SupportReachTargetClamped);
            Add(row, stride.SupportReachOutputClamped);
            Add(row, stride.SpringTarget);
            Add(row, stride.SpringOutput);
            Add(row, stride.SpringVelocity);
            Add(row, stride.PelvisDelta);
            Add(row, stride.PositionWeight);
            Add(row, frame.PelvisGoal.ComponentPosition);
            Add(row, ik.PhysicalPelvisComponentPosition);
            Vector3 expectedPhysicalPelvis = stride.AnimatedPelvisComponentPosition +
                frame.PelvisGoal.ComponentPosition * frame.PelvisGoal.PositionWeight;
            Add(
                row,
                ik.PhysicalWriteAvailable && frame.PelvisGoal.PositionWeight > 0f
                    ? Vector3.Distance(
                        ik.PhysicalPelvisComponentPosition,
                        expectedPhysicalPelvis)
                    : 0f);
            CharacterFullBodyIkSolverDiagnostics solver = ik.Solver;
            CharacterFullBodyIkEffectorDiagnostics effector = ik.Effector;
            Add(row, ik.SolverAvailable);
            Add(row, solver.Succeeded);
            Add(row, solver.FrameSequence);
            Add(row, solver.InputCompletionIdentity);
            Add(row, solver.OutputCompletionIdentity);
            Add(row, solver.BackendIdentity);
            Add(row, solver.RigId);
            Add(row, solver.RigRevision);
            Add(row, solver.ProfileId);
            Add(row, solver.ProfileRevision);
            Add(row, solver.Failure.ToString());
            Add(row, solver.AppliedGoalCount);
            Add(row, ik.EffectorAvailable);
            Add(row, effector.Slot.ToString());
            Add(row, effector.TargetComponentPosition);
            Add(row, effector.SolvedComponentPosition);
            Add(row, effector.PositionResidual);
            Add(row, effector.RotationResidualDegrees);
            CharacterFullBodyIkLimbDiagnostics limb = ik.Limb;
            CharacterFullBodyIkLegPoseDiagnostics legPose = limb.LegPose;
            Add(row, legPose.IsAvailable);
            Add(row, limb.Limb.ToString());
            Add(row, limb.BendWeight);
            Add(row, legPose.StabilizationWeight);
            Add(row, legPose.RetainedPreviousBendDirection);
            Add(row, legPose.OriginalHip);
            Add(row, legPose.OriginalKnee);
            Add(row, legPose.OriginalAnkle);
            Add(row, legPose.TargetAnkle);
            Add(row, legPose.SolvedHip);
            Add(row, legPose.SolvedKnee);
            Add(row, legPose.SolvedAnkle);
            Add(row, legPose.OriginalBendDegrees);
            Add(row, legPose.SolvedBendDegrees);
            Add(row, legPose.OriginalExtensionRatio);
            Add(row, legPose.TargetExtensionRatio);
            Add(row, legPose.SolvedExtensionRatio);
            Add(row, legPose.OriginalCompressionReserve);
            Add(row, legPose.TargetCompressionReserve);
            Add(row, legPose.SolvedCompressionReserve);
            Add(row, legPose.EffectiveBendDirection);
            Add(row, legPose.AnimatedBendDirectionPreviousDot);
            Add(row, legPose.EffectiveBendDirectionPreviousDot);
            CharacterFullBodyIkEffectorDiagnostics pelvis = ik.Pelvis;
            Add(row, ik.PelvisAvailable);
            Add(row, pelvis.TargetComponentPosition);
            Add(row, pelvis.SolvedComponentPosition);
            Add(row, pelvis.PositionResidual);
            Add(row, pelvis.RotationResidualDegrees);
            Add(row, ik.PhysicalWriteAvailable);
            Add(row, ik.PhysicalWriteCompletionIdentity);
            Add(row, ik.PhysicalAnkleComponentPosition);
            Add(row, ik.PhysicalAnkleComponentRotation);
            Vector3 finalAnkleWorldPosition = default;
            Quaternion finalAnkleWorldRotation = Quaternion.identity;
            CharacterFootPlacementSoleContactPose finalContacts = default;
            if (ik.PhysicalWriteAvailable)
            {
                finalAnkleWorldPosition = TransformComponentPoint(
                    roots,
                    ik.PhysicalAnkleComponentPosition);
                finalAnkleWorldRotation =
                    (roots.PoseRootWorldRotation *
                     ik.PhysicalAnkleComponentRotation).normalized;
                finalContacts = CharacterFootPlacementSoleContactPose.Resolve(
                    foot.SourceAnklePosition,
                    foot.SourceAnkleRotation,
                    foot.SourceHeelPosition,
                    foot.SourceToePosition,
                    finalAnkleWorldPosition,
                    finalAnkleWorldRotation);
            }
            Add(row, finalAnkleWorldPosition);
            Add(row, finalAnkleWorldRotation);
            Add(row, finalContacts.HeelPosition);
            Add(row, finalContacts.ToePosition);
            Add(
                row,
                ik.PhysicalWriteAvailable && legPose.IsAvailable &&
                foot.Goal.PositionWeight > 0f
                    ? Vector3.Distance(
                        ik.PhysicalAnkleComponentPosition,
                        ResolveWeightedAnkleComponentPosition(
                            legPose.OriginalAnkle,
                            in footGoal))
                    : 0f);
            writer.WriteLine(row);
        }

        static CharacterFootActualEnvelopeIntersectionFact
            ResolveActualFootEnvelopeIntersection(
                in CharacterFootGroundPathDiagnostics ground,
                in CharacterFootSwingMotionDiagnostics motion,
                Vector3 up)
        {
            var result = new CharacterFootActualEnvelopeIntersectionFact
            {
                State = CharacterFootActualEnvelopeIntersectionState.Unavailable
            };
            if (!ground.Accepted ||
                motion.State != CharacterFootSwingMotionState.Accepted ||
                motion.ConstraintState != CharacterFootConstraintState.Swing ||
                ground.EnvelopeVertexCount < 2)
            {
                return result;
            }
            if (!float.IsFinite(up.x) ||
                !float.IsFinite(up.y) ||
                !float.IsFinite(up.z) ||
                up.sqrMagnitude <= 0.000001f)
            {
                result.State =
                    CharacterFootActualEnvelopeIntersectionState.InvalidComponentUp;
                return result;
            }
            Vector3 horizontalAxis = Vector3.ProjectOnPlane(
                ground.NextSwingLanding - ground.LastLanding,
                up);
            if (!float.IsFinite(horizontalAxis.x) ||
                !float.IsFinite(horizontalAxis.y) ||
                !float.IsFinite(horizontalAxis.z) ||
                horizontalAxis.sqrMagnitude <= 0.00000001f)
            {
                result.State =
                    CharacterFootActualEnvelopeIntersectionState.DegenerateAxis;
                return result;
            }
            Vector3 direction = horizontalAxis.normalized;
            float pathLength = horizontalAxis.magnitude;
            Vector3 actualHorizontalOffset = Vector3.ProjectOnPlane(
                motion.OriginalSole - ground.LastLanding,
                up);
            result.ActualFootHorizontalDistance = Vector3.Dot(
                actualHorizontalOffset,
                direction);
            result.BaselineHorizontalDistance = Vector3.Dot(
                motion.BaselineSample - ground.LastLanding,
                direction);
            result.EnvelopeHorizontalDistance = Vector3.Dot(
                motion.EnvelopeSample - ground.LastLanding,
                direction);
            float rawPathParameter =
                result.ActualFootHorizontalDistance / pathLength;
            result.AxisRegion = result.ActualFootHorizontalDistance <
                                -ActualEnvelopeHorizontalEpsilonMeters
                ? CharacterFootActualFootAxisRegion.BeforePathStart
                : result.ActualFootHorizontalDistance >
                  pathLength + ActualEnvelopeHorizontalEpsilonMeters
                    ? CharacterFootActualFootAxisRegion.AfterPathEnd
                    : CharacterFootActualFootAxisRegion.WithinPathSegment;
            result.ClosestPathParameter = Mathf.Clamp01(rawPathParameter);
            result.DistanceAlongAxis =
                result.ClosestPathParameter * pathLength;
            Vector3 closestHorizontalOffset =
                horizontalAxis * result.ClosestPathParameter;
            result.CrossTrackDistance = Vector3.Distance(
                actualHorizontalOffset,
                closestHorizontalOffset);
            result.CorridorRadius = ground.Query.Radius;
            result.WithinGroundPathCorridor =
                float.IsFinite(result.CorridorRadius) &&
                result.CorridorRadius > 0f &&
                result.CrossTrackDistance <=
                result.CorridorRadius + ActualEnvelopeHorizontalEpsilonMeters;
            var heights = new List<float>(ground.EnvelopeVertexCount * 2);
            for (int i = 1; i < ground.EnvelopeVertexCount; i++)
            {
                CharacterFootGroundEnvelopeVertex previous =
                    ground.EnvelopeVertexAt(i - 1);
                CharacterFootGroundEnvelopeVertex current =
                    ground.EnvelopeVertexAt(i);
                float previousDistance = Vector3.Dot(
                    previous.Position - ground.LastLanding,
                    direction);
                float currentDistance = Vector3.Dot(
                    current.Position - ground.LastLanding,
                    direction);
                float minimumDistance = Mathf.Min(
                    previousDistance,
                    currentDistance);
                float maximumDistance = Mathf.Max(
                    previousDistance,
                    currentDistance);
                if (result.ActualFootHorizontalDistance <
                        minimumDistance - ActualEnvelopeHorizontalEpsilonMeters ||
                    result.ActualFootHorizontalDistance >
                        maximumDistance + ActualEnvelopeHorizontalEpsilonMeters)
                {
                    continue;
                }
                float previousHeight = Vector3.Dot(previous.Position, up);
                float currentHeight = Vector3.Dot(current.Position, up);
                float distanceDelta = currentDistance - previousDistance;
                if (Mathf.Abs(distanceDelta) <=
                    ActualEnvelopeHorizontalEpsilonMeters)
                {
                    if (Mathf.Abs(
                            result.ActualFootHorizontalDistance -
                            previousDistance) >
                        ActualEnvelopeHorizontalEpsilonMeters)
                    {
                        continue;
                    }
                    AddUniqueEnvelopeHeight(heights, previousHeight);
                    AddUniqueEnvelopeHeight(heights, currentHeight);
                    if (Mathf.Abs(currentHeight - previousHeight) >
                        ActualEnvelopeHeightEpsilonMeters)
                    {
                        result.HasVerticalEdge = true;
                    }
                    continue;
                }
                float interpolation =
                    (result.ActualFootHorizontalDistance -
                     previousDistance) / distanceDelta;
                AddUniqueEnvelopeHeight(
                    heights,
                    Mathf.Lerp(
                        previousHeight,
                        currentHeight,
                        Mathf.Clamp01(interpolation)));
            }
            if (heights.Count == 0)
            {
                result.State =
                    CharacterFootActualEnvelopeIntersectionState.NoIntersection;
                result.CounterfactualState =
                    result.WithinGroundPathCorridor
                        ? CharacterFootActualEnvelopeCounterfactualState
                            .NoIntersection
                        : CharacterFootActualEnvelopeCounterfactualState
                            .OutsideGroundPathCorridor;
                return result;
            }
            result.CandidateCount = heights.Count;
            result.MinimumHeightAlongUp = heights.Min();
            result.MaximumHeightAlongUp = heights.Max();
            result.HeightSpan = result.MaximumHeightAlongUp -
                                result.MinimumHeightAlongUp;
            result.HasMultipleHeights = heights.Count > 1 &&
                result.HeightSpan > ActualEnvelopeHeightEpsilonMeters;
            result.Ambiguous = result.HasVerticalEdge ||
                               result.HasMultipleHeights;
            result.State = result.Ambiguous
                ? CharacterFootActualEnvelopeIntersectionState
                    .AmbiguousEnvelopeAtActualFootDistance
                : CharacterFootActualEnvelopeIntersectionState.Unique;
            result.CounterfactualState = !result.WithinGroundPathCorridor
                ? CharacterFootActualEnvelopeCounterfactualState
                    .OutsideGroundPathCorridor
                : result.Ambiguous
                    ? CharacterFootActualEnvelopeCounterfactualState
                        .AmbiguousInCorridor
                    : CharacterFootActualEnvelopeCounterfactualState
                        .UniqueInCorridor;
            return result;
        }

        static void AddUniqueEnvelopeHeight(
            List<float> heights,
            float value)
        {
            if (!float.IsFinite(value))
                return;
            for (int i = 0; i < heights.Count; i++)
            {
                if (Mathf.Abs(heights[i] - value) <=
                    ActualEnvelopeHeightEpsilonMeters)
                {
                    return;
                }
            }
            heights.Add(value);
        }

        static void WriteGeometryRows(
            SamplingSession session,
            StreamWriter writer,
            StringBuilder row,
            in CharacterFootLandingPredictionDiagnostics frame,
            in CharacterFootLandingPredictionFootDiagnostics foot)
        {
            CharacterFootGroundPathDiagnostics ground = foot.GroundPath;
            int rowCount = Math.Max(
                ground.ContactCount,
                ground.EnvelopeVertexCount);
            for (int index = 0; index < rowCount; index++)
            {
                row.Clear();
                Add(row, session.SampleIdentity.ToString("N"));
                Add(row, frame.FrameSequence);
                Add(row, frame.CompletionIdentity);
                Add(row, foot.Side.ToString());
                Add(row, ground.InputIdentity);
                bool hasContact = index < ground.ContactCount;
                CharacterFootGroundContact contact = hasContact
                    ? ground.ContactAt(index)
                    : default;
                Add(row, hasContact ? index : -1);
                Add(row, hasContact ? contact.SegmentIndex : -1);
                Add(row, contact.SurfaceIdentity);
                Add(row, contact.CandidateIdentity);
                Add(row, contact.Position);
                Add(row, contact.Normal);
                Add(row, contact.QueryDistance);
                bool hasEnvelopeVertex = index < ground.EnvelopeVertexCount;
                CharacterFootGroundEnvelopeVertex envelopeVertex = hasEnvelopeVertex
                    ? ground.EnvelopeVertexAt(index)
                    : default;
                Add(row, hasEnvelopeVertex ? index : -1);
                Add(row, envelopeVertex.Position);
                writer.WriteLine(row);
            }
        }

        static Vector3 ResolveWeightedAnkleComponentPosition(
            Vector3 originalComponentPosition,
            in CharacterFullBodyIkGoal goal)
        {
            return originalComponentPosition +
                   (goal.ComponentPosition - originalComponentPosition) *
                   goal.PositionWeight;
        }

        static Vector3 TransformComponentPoint(
            in RootHierarchyCapture roots,
            Vector3 componentPoint) =>
            roots.PoseRootWorldPosition +
            roots.PoseRootWorldRotation *
            Vector3.Scale(componentPoint, roots.PoseRootLossyScale);

        static CharacterFootContactPlanePenetrationAvailability
            ResolvePenetrationAvailability(
                in CharacterFootLandingPredictionDiagnostics frame,
                in CharacterFootSwingMotionDiagnostics motion,
                in FootIkCapture ik)
        {
            if (!ik.PhysicalWriteAvailable ||
                ik.PhysicalWriteCompletionIdentity != frame.CompletionIdentity)
            {
                return CharacterFootContactPlanePenetrationAvailability
                    .FinalPhysicalPoseUnavailable;
            }
            if (motion.ConstraintState != CharacterFootConstraintState.Landing &&
                motion.ConstraintState != CharacterFootConstraintState.Locked)
            {
                return CharacterFootContactPlanePenetrationAvailability
                    .ContactLifecycleUnavailable;
            }
            if (!motion.ContactPlaneAvailable)
            {
                return CharacterFootContactPlanePenetrationAvailability
                    .ContactPlaneUnavailable;
            }
            if (motion.LandingEventIdentity == 0)
            {
                return CharacterFootContactPlanePenetrationAvailability
                    .EventLineageMismatch;
            }
            if (motion.ContactSurfaceIdentity == 0)
            {
                return CharacterFootContactPlanePenetrationAvailability
                    .SurfaceLineageMismatch;
            }
            Vector3 normal = motion.ContactPlaneNormal;
            if (!float.IsFinite(normal.x) ||
                !float.IsFinite(normal.y) ||
                !float.IsFinite(normal.z) ||
                normal.sqrMagnitude <= 0.000001f)
            {
                return CharacterFootContactPlanePenetrationAvailability
                    .InvalidContactNormal;
            }
            return CharacterFootContactPlanePenetrationAvailability.Available;
        }

        static void Add(StringBuilder row, string value)
        {
            Separate(row);
            value ??= string.Empty;
            if (value.IndexOf('\r') >= 0 || value.IndexOf('\n') >= 0)
                throw new InvalidOperationException(
                    "Foot Landing CSV string contains a line break.");
            bool quote = value.IndexOf(',') >= 0 ||
                         value.IndexOf('"') >= 0;
            if (!quote)
            {
                row.Append(value);
                return;
            }
            row.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (character == '"')
                    row.Append('"');
                row.Append(character);
            }
            row.Append('"');
        }

        static void Add(StringBuilder row, bool value) => Add(row, value ? 1 : 0);

        static void Add(StringBuilder row, int value)
        {
            Separate(row);
            row.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        static void Add(StringBuilder row, ulong value)
        {
            Separate(row);
            row.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        static void Add(StringBuilder row, float value)
        {
            Separate(row);
            row.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        static void Add(StringBuilder row, Vector3 value)
        {
            Add(row, value.x);
            Add(row, value.y);
            Add(row, value.z);
        }

        static void Add(StringBuilder row, Quaternion value)
        {
            Add(row, value.x);
            Add(row, value.y);
            Add(row, value.z);
            Add(row, value.w);
        }

        static void AddSupportTarget(
            StringBuilder row,
            in CharacterFootSupportTargetDiagnostics target)
        {
            Add(row, target.Available);
            Add(row, target.FrameSequence);
            Add(row, target.CompletionIdentity);
            Add(row, target.Side.ToString());
            Add(row, target.Position);
            Add(row, target.SupportNormal);
            Add(row, target.SurfaceIdentity);
            Add(row, target.WorldRevision);
            Add(row, target.Kind.ToString());
            Add(row, target.PositionSource.ToString());
            Add(row, target.PositionFrameSequence);
            Add(row, target.PositionCompletionIdentity);
            Add(row, target.PositionEventIdentity);
            Add(row, target.PositionPathIdentity);
            Add(row, target.NormalSource.ToString());
            Add(row, target.NormalFrameSequence);
            Add(row, target.NormalCompletionIdentity);
            Add(row, target.NormalEventIdentity);
            Add(row, target.CurrentSupportProbeKind.ToString());
        }

        static string CurrentSupportProbeHeader(string prefix) =>
            prefix + "Purpose," + prefix + "Kind," + prefix + "State," +
            prefix + "RejectReason," + prefix + "ProbePositionX," +
            prefix + "ProbePositionY," + prefix + "ProbePositionZ," +
            prefix + "ComponentUpX," + prefix + "ComponentUpY," +
            prefix + "ComponentUpZ," + prefix + "OriginX," +
            prefix + "OriginY," + prefix + "OriginZ," +
            prefix + "DirectionX," + prefix + "DirectionY," +
            prefix + "DirectionZ," + prefix + "MaximumDistance," +
            prefix + "Radius," + prefix + "LayerMask," +
            prefix + "MinimumGroundNormalDot," + prefix + "HitCapacity," +
            prefix + "CandidateCount," + prefix + "SurfaceIdentity," +
            prefix + "PointX," + prefix + "PointY," + prefix + "PointZ," +
            prefix + "NormalX," + prefix + "NormalY," + prefix + "NormalZ," +
            prefix + "Distance," + prefix + "WorldRevision," +
            prefix + "SphereCastExecuted," + prefix + "Accepted,";

        static string CurrentSupportCandidateHeader(string prefix) =>
            prefix + "Available," + prefix + "Kind," +
            prefix + "SolePositionX," + prefix + "SolePositionY," +
            prefix + "SolePositionZ," + prefix + "HeightAlongUp," +
            prefix + "DirectionX," + prefix + "DirectionY," +
            prefix + "DirectionZ," + prefix + "SurfaceIdentity," +
            prefix + "WorldRevision,";

        static void AddCurrentSupportProbe(
            StringBuilder row,
            in CharacterFootCurrentSupportProbeDiagnostics probe)
        {
            Add(row, probe.Purpose.ToString());
            Add(row, probe.Kind.ToString());
            Add(row, probe.State.ToString());
            Add(row, probe.RejectReason.ToString());
            Add(row, probe.ProbePosition);
            Add(row, probe.ComponentUp);
            Add(row, probe.Origin);
            Add(row, probe.Direction);
            Add(row, probe.MaximumDistance);
            Add(row, probe.Radius);
            Add(row, probe.LayerMask);
            Add(row, probe.MinimumGroundNormalDot);
            Add(row, probe.HitCapacity);
            Add(row, probe.CandidateCount);
            Add(row, probe.SurfaceIdentity);
            Add(row, probe.Point);
            Add(row, probe.Normal);
            Add(row, probe.Distance);
            Add(row, probe.WorldRevision);
            Add(row, probe.SphereCastExecuted);
            Add(row, probe.Accepted);
        }

        static void AddCurrentSupportCandidate(
            StringBuilder row,
            in CharacterFootCurrentSupportCandidateDiagnostics candidate)
        {
            Add(row, candidate.Available);
            Add(row, candidate.Kind.ToString());
            Add(row, candidate.SolePosition);
            Add(row, candidate.HeightAlongUp);
            Add(row, candidate.Direction);
            Add(row, candidate.SurfaceIdentity);
            Add(row, candidate.WorldRevision);
        }

        static void AddCurrentSupport(
            StringBuilder row,
            in CharacterFootCurrentSupportDiagnostics support)
        {
            Add(row, support.FrameSequence);
            Add(row, support.CompletionIdentity);
            Add(row, support.WorldRevision);
            Add(row, support.IsSpecified);
            Add(row, support.Available);
            Add(row, support.RejectReason.ToString());
            AddCurrentSupportProbe(row, support.Base);
            AddCurrentSupportProbe(row, support.Rear);
            AddCurrentSupportProbe(row, support.PositiveLateral);
            AddCurrentSupportProbe(row, support.NegativeLateral);
            AddCurrentSupportProbe(row, support.Toe);
            AddCurrentSupportCandidate(row, support.BaseCandidate);
            AddCurrentSupportCandidate(row, support.RearCandidate);
            AddCurrentSupportCandidate(row, support.PositiveLateralCandidate);
            AddCurrentSupportCandidate(row, support.NegativeLateralCandidate);
            AddCurrentSupportCandidate(row, support.ToeCandidate);
            Add(row, support.SelectedProbe.ToString());
            Add(row, support.SelectionReason.ToString());
            Add(row, support.SelectionEpsilon);
            Add(row, support.SelectedDirectionBeforeNormalization);
            AddSupportTarget(row, support.Target);
        }

        static void AddResolvedFoot(
            StringBuilder row,
            in CharacterResolvedFootDiagnostics resolved)
        {
            Add(row, resolved.FrameSequence);
            Add(row, resolved.CompletionIdentity);
            Add(row, resolved.RigId);
            Add(row, resolved.RigRevision);
            Add(row, resolved.Side.ToString());
            Add(row, resolved.Outcome.ToString());
            Add(row, resolved.FinalSole);
            Add(row, resolved.EffectiveSole);
            Add(row, resolved.GoalTargetAnkle);
            Add(row, resolved.GoalTargetRotation);
            Add(row, resolved.EffectiveAnkle);
            Add(row, resolved.EffectiveRotation);
            Add(row, resolved.EffectiveHeel);
            Add(row, resolved.EffectiveToe);
            Add(row, resolved.EffectiveSoleFromContacts);
            Add(row, resolved.SourceSoleForward);
            Add(row, resolved.SourceSoleFrameLocalRotation);
            Add(row, resolved.GoalTargetCorrection);
            Add(row, resolved.EffectiveSoleCorrection);
            Add(row, resolved.PositionWeight);
            Add(row, resolved.RotationWeight);
            AddSupportTarget(row, resolved.SupportTarget);
            Add(row, resolved.ContactAvailable);
            Add(row, resolved.ContactEventIdentity);
            Add(row, resolved.ContactPoint);
            Add(row, resolved.ContactOwnership);
            Add(row, resolved.SupportEligibility.ToString());
            Add(row, resolved.SupportWeight);
            Add(row, resolved.SupportIntentWeight);
            Add(row, resolved.SupportHorizontalError);
            Add(row, resolved.SupportEventIdentity);
            Add(row, resolved.PelvisReachAvailable);
            Add(row, resolved.PelvisReachEventIdentity);
            Add(row, resolved.PelvisReachPoint);
            Add(row, resolved.LandingReachAvailable);
            Add(row, resolved.LandingReachEventIdentity);
            Add(row, resolved.LandingReachHip);
            Add(row, resolved.LandingReachTargetAnkle);
            Add(row, resolved.LandingReachLegLength);
            Add(row, resolved.LandingReachMinimumCompressionReserve);
        }

        static void AddFormalEventFrame(
            StringBuilder row,
            bool available,
            AnimationFootMotionEventFrame events)
        {
            bool valid = available && events.IsValid;
            AnimationFootMotionEventOccurrence current = valid
                ? events.CurrentContact
                : default;
            AnimationFootMotionEventOccurrence next = valid
                ? events.NextLanding
                : default;
            Add(row, valid ? events.Phase.ToString() : string.Empty);
            Add(row, valid ? events.ApproachContactToLandingProgress : 0f);
            Add(row, valid ? events.TimeToLandingSeconds : 0f);
            Add(row, valid && events.InApproachContactToLanding);
            Add(row, current.IsValid);
            Add(row, current.IsBound ? current.Identity : 0UL);
            Add(row, current.IsValid ? current.Ordinal : 0);
            Add(row, current.IsValid ? current.LandingCycle : 0);
            Add(row, current.IsValid ? current.Distance : 0f);
            Add(row, current.IsValid ? current.RootLocalLanding : Vector3.zero);
            Add(row, next.IsValid);
            Add(row, next.IsBound ? next.Identity : 0UL);
            Add(row, next.IsValid ? next.Ordinal : 0);
            Add(row, next.IsValid ? next.LandingCycle : 0);
            Add(row, next.IsValid ? next.Distance : 0f);
            Add(row, next.IsValid ? next.RootLocalLanding : Vector3.zero);
        }

        static void AddStepCandidate(
            StringBuilder row,
            in CharacterFootStepCandidateDiagnostics candidate)
        {
            Add(row, candidate.IsValid);
            Add(row, candidate.IsAuthoritative);
            Add(row, candidate.HasConsistentLandingEventIdentity);
            Add(row, candidate.IsPreSwing);
            Add(row, candidate.IsSwing);
            Add(row, candidate.EventOrdinal);
            Add(row, candidate.SourceLandingCycleOffset);
            Add(row, candidate.SourceSampleCycle);
            Add(row, candidate.ContributionContinuityIdentity);
            Add(row, candidate.LandingEventIdentity);
            Add(row, candidate.TimeToLandingSeconds);
            AddStepPhase(row, in candidate);
            Add(row, candidate.RootLocalLanding);
        }

        static void AddStepPhase(
            StringBuilder row,
            in CharacterFootStepCandidateDiagnostics candidate)
        {
            Add(row, candidate.EventPhase);
            Add(row, candidate.ApproachContactToLandingProgress);
            Add(row, candidate.LandingPhase);
            Add(row, candidate.AtOrAfterApproachContact);
            Add(row, candidate.InApproachContactToLanding);
        }

        static void Separate(StringBuilder row)
        {
            if (row.Length > 0)
                row.Append(',');
        }
    }
}
