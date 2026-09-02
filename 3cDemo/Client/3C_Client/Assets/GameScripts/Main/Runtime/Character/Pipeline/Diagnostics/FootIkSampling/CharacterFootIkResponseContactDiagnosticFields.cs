using ThirdPerson.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static partial class CharacterFootIkDiagnosticFields
    {
        const string ResponseContactGroup = "response-contact";
        const string PreviousResponseOutputAvailableField =
            "character-foot-ik/main/foot-motion-previous-response-output-available";
        const string PlantWorldResidualDeadlineHalfLifeAvailableField =
            "character-foot-ik/main/foot-motion-plant-world-residual-deadline-half-life-available";

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-target-height-adoption-mode", 1, DiagnosticValueKind.Int32, "category", Main, ResponseContactGroup)]
        internal static int FootMotionPlantTargetHeightAdoptionMode(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)Response(in view, in metadata).PlantTargetHeightAdoptionMode;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-target-maximum-vertical-speed", 1, DiagnosticValueKind.Float32, "metres-per-second", Main, ResponseContactGroup)]
        internal static float FootMotionPlantTargetMaximumVerticalSpeed(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Response(in view, in metadata).PlantTargetMaximumVerticalSpeed;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-target-height-before", 1, DiagnosticValueKind.Float32, "metres", Main, ResponseContactGroup)]
        internal static float FootMotionPlantTargetHeightBefore(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).PlantTargetHeightBefore;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-target-height-target", 1, DiagnosticValueKind.Float32, "metres", Main, ResponseContactGroup)]
        internal static float FootMotionPlantTargetHeightTarget(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).PlantTargetHeightTarget;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-target-vertical-delta", 1, DiagnosticValueKind.Float32, "metres", Main, ResponseContactGroup)]
        internal static float FootMotionPlantTargetVerticalDelta(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).PlantTargetVerticalDelta;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-target-applied-vertical-delta", 1, DiagnosticValueKind.Float32, "metres", Main, ResponseContactGroup)]
        internal static float FootMotionPlantTargetAppliedVerticalDelta(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).PlantTargetAppliedVerticalDelta;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-target-height-after", 1, DiagnosticValueKind.Float32, "metres", Main, ResponseContactGroup)]
        internal static float FootMotionPlantTargetHeightAfter(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).PlantTargetHeightAfter;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-target-height-event-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, ResponseContactGroup)]
        internal static ulong FootMotionPlantTargetHeightEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).PlantTargetHeightEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-target-height-update-reason", 1, DiagnosticValueKind.Int32, "category", Main, ResponseContactGroup)]
        internal static int FootMotionPlantTargetHeightUpdateReason(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)Response(in view, in metadata).PlantTargetHeightUpdateReason;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-target-force-refreshed", 1, DiagnosticValueKind.Boolean, "none", Main, ResponseContactGroup)]
        internal static bool FootMotionPlantTargetForceRefreshed(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).PlantTargetForceRefreshed;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-target-force-refresh-distance", 1, DiagnosticValueKind.Float32, "metres", Main, ResponseContactGroup)]
        internal static float FootMotionPlantTargetForceRefreshDistance(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).PlantTargetForceRefreshDistance;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-target-vertical-clamped", 1, DiagnosticValueKind.Boolean, "none", Main, ResponseContactGroup)]
        internal static bool FootMotionPlantTargetVerticalClamped(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).PlantTargetVerticalClamped;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-previous-selected-world-target", 1, DiagnosticValueKind.Vector3, "metres", Main, ResponseContactGroup)]
        internal static DiagnosticVector3 FootMotionPlantPreviousSelectedWorldTarget(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Response(in view, in metadata).PlantPreviousSelectedWorldTarget);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-selected-world-target", 1, DiagnosticValueKind.Vector3, "metres", Main, ResponseContactGroup)]
        internal static DiagnosticVector3 FootMotionPlantSelectedWorldTarget(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Response(in view, in metadata).PlantSelectedWorldTarget);

        [DiagnosticField(Capability, PreviousResponseOutputAvailableField, 1, DiagnosticValueKind.Boolean, "none", Main, ResponseContactGroup)]
        internal static bool FootMotionPreviousResponseOutputAvailable(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).PreviousResponseOutputAvailable;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-previous-response-output-point", 1, DiagnosticValueKind.Vector3, "metres", Main, ResponseContactGroup, AvailabilityFieldId = PreviousResponseOutputAvailableField)]
        internal static DiagnosticVector3 FootMotionPreviousResponseOutputPoint(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Response(in view, in metadata).PreviousResponseOutputPoint);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-desired-output-point", 1, DiagnosticValueKind.Vector3, "metres", Main, ResponseContactGroup)]
        internal static DiagnosticVector3 FootMotionDesiredOutputPoint(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Response(in view, in metadata).DesiredOutputPoint);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-response-output-point", 1, DiagnosticValueKind.Vector3, "metres", Main, ResponseContactGroup)]
        internal static DiagnosticVector3 FootMotionResponseOutputPoint(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Response(in view, in metadata).ResponseOutputPoint);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-residual-capture-reason", 1, DiagnosticValueKind.Int32, "category", Main, ResponseContactGroup)]
        internal static int FootMotionPlantResidualCaptureReason(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Response(in view, in metadata).PlantResidualCaptureReason;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-world-residual-before-capture", 1, DiagnosticValueKind.Vector3, "metres", Main, ResponseContactGroup)]
        internal static DiagnosticVector3 FootMotionPlantWorldResidualBeforeCapture(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Response(in view, in metadata).PlantWorldResidualBeforeCapture);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-world-residual-captured-before-decay", 1, DiagnosticValueKind.Vector3, "metres", Main, ResponseContactGroup)]
        internal static DiagnosticVector3 FootMotionPlantWorldResidualCapturedBeforeDecay(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Response(in view, in metadata).PlantWorldResidualCapturedBeforeDecay);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-world-residual-decay-applied", 1, DiagnosticValueKind.Boolean, "none", Main, ResponseContactGroup)]
        internal static bool FootMotionPlantWorldResidualDecayApplied(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).PlantWorldResidualDecayApplied;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-world-residual-base-half-life-seconds", 1, DiagnosticValueKind.Float32, "seconds", Main, ResponseContactGroup)]
        internal static float FootMotionPlantWorldResidualBaseHalfLifeSeconds(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).PlantWorldResidualBaseHalfLifeSeconds;

        [DiagnosticField(Capability, PlantWorldResidualDeadlineHalfLifeAvailableField, 1, DiagnosticValueKind.Boolean, "none", Main, ResponseContactGroup)]
        internal static bool FootMotionPlantWorldResidualDeadlineHalfLifeAvailable(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).PlantWorldResidualDeadlineHalfLifeAvailable;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-world-residual-deadline-half-life-seconds", 1, DiagnosticValueKind.Float32, "seconds", Main, ResponseContactGroup, AvailabilityFieldId = PlantWorldResidualDeadlineHalfLifeAvailableField)]
        internal static float FootMotionPlantWorldResidualDeadlineHalfLifeSeconds(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).PlantWorldResidualDeadlineHalfLifeSeconds;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-world-residual-applied-half-life-seconds", 1, DiagnosticValueKind.Float32, "seconds", Main, ResponseContactGroup)]
        internal static float FootMotionPlantWorldResidualAppliedHalfLifeSeconds(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).PlantWorldResidualAppliedHalfLifeSeconds;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-world-residual-after-decay", 1, DiagnosticValueKind.Vector3, "metres", Main, ResponseContactGroup)]
        internal static DiagnosticVector3 FootMotionPlantWorldResidualAfterDecay(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Response(in view, in metadata).PlantWorldResidualAfterDecay);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-world-residual-completion-tolerance", 1, DiagnosticValueKind.Float32, "metres", Main, ResponseContactGroup)]
        internal static float FootMotionPlantWorldResidualCompletionTolerance(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).PlantWorldResidualCompletionTolerance;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-world-residual-cleared-at-completion-tolerance", 1, DiagnosticValueKind.Boolean, "none", Main, ResponseContactGroup)]
        internal static bool FootMotionPlantWorldResidualClearedAtCompletionTolerance(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).PlantWorldResidualClearedAtCompletionTolerance;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-correction-response-domain", 1, DiagnosticValueKind.Int32, "category", Main, ResponseContactGroup)]
        internal static int FootMotionCorrectionResponseDomain(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Response(in view, in metadata).CorrectionResponseDomain;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-correction-response-previous-domain", 1, DiagnosticValueKind.Int32, "category", Main, ResponseContactGroup)]
        internal static int FootMotionCorrectionResponsePreviousDomain(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Response(in view, in metadata).CorrectionResponsePreviousDomain;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-correction-response-domain-transferred", 1, DiagnosticValueKind.Boolean, "none", Main, ResponseContactGroup)]
        internal static bool FootMotionCorrectionResponseDomainTransferred(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).CorrectionResponseDomainTransferred;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-correction-response-evaluated", 1, DiagnosticValueKind.Boolean, "none", Main, ResponseContactGroup)]
        internal static bool FootMotionCorrectionResponseEvaluated(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).CorrectionResponseEvaluated;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-correction-response-initialized-before", 1, DiagnosticValueKind.Boolean, "none", Main, ResponseContactGroup)]
        internal static bool FootMotionCorrectionResponseInitializedBefore(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).CorrectionResponseInitializedBefore;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-correction-response-initialized-this-frame", 1, DiagnosticValueKind.Boolean, "none", Main, ResponseContactGroup)]
        internal static bool FootMotionCorrectionResponseInitializedThisFrame(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).CorrectionResponseInitializedThisFrame;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-correction-response-initialization-reason", 1, DiagnosticValueKind.Int32, "category", Main, ResponseContactGroup)]
        internal static int FootMotionCorrectionResponseInitializationReason(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Response(in view, in metadata).CorrectionResponseInitializationReason;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-correction-response-desired", 1, DiagnosticValueKind.Float32, "metres", Main, ResponseContactGroup)]
        internal static float FootMotionCorrectionResponseDesired(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).CorrectionResponseDesired;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-correction-response-requested-direction", 1, DiagnosticValueKind.Vector3, "direction", Main, ResponseContactGroup)]
        internal static DiagnosticVector3 FootMotionCorrectionResponseRequestedDirection(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Response(in view, in metadata).CorrectionResponseRequestedDirection);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-correction-response-previous-direction", 1, DiagnosticValueKind.Vector3, "direction", Main, ResponseContactGroup)]
        internal static DiagnosticVector3 FootMotionCorrectionResponsePreviousDirection(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Response(in view, in metadata).CorrectionResponsePreviousDirection);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-correction-response-direction-limited", 1, DiagnosticValueKind.Boolean, "none", Main, ResponseContactGroup)]
        internal static bool FootMotionCorrectionResponseDirectionLimited(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).CorrectionResponseDirectionLimited;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-correction-response-maximum-direction-change-degrees", 1, DiagnosticValueKind.Float32, "degrees", Main, ResponseContactGroup)]
        internal static float FootMotionCorrectionResponseMaximumDirectionChangeDegrees(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).CorrectionResponseMaximumDirectionChangeDegrees;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-correction-response-applied-direction-change-degrees", 1, DiagnosticValueKind.Float32, "degrees", Main, ResponseContactGroup)]
        internal static float FootMotionCorrectionResponseAppliedDirectionChangeDegrees(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).CorrectionResponseAppliedDirectionChangeDegrees;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-correction-response-visible-output-transferred", 1, DiagnosticValueKind.Boolean, "none", Main, ResponseContactGroup)]
        internal static bool FootMotionCorrectionResponseVisibleOutputTransferred(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).CorrectionResponseVisibleOutputTransferred;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-correction-response-before-rebase", 1, DiagnosticValueKind.Float32, "metres", Main, ResponseContactGroup)]
        internal static float FootMotionCorrectionResponseBeforeRebase(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).CorrectionResponseBeforeRebase;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-correction-response-previous", 1, DiagnosticValueKind.Float32, "metres", Main, ResponseContactGroup)]
        internal static float FootMotionCorrectionResponsePrevious(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).CorrectionResponsePrevious;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-correction-response-current", 1, DiagnosticValueKind.Float32, "metres", Main, ResponseContactGroup)]
        internal static float FootMotionCorrectionResponseCurrent(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).CorrectionResponseCurrent;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-correction-response-direction", 1, DiagnosticValueKind.Vector3, "direction", Main, ResponseContactGroup)]
        internal static DiagnosticVector3 FootMotionCorrectionResponseDirection(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Response(in view, in metadata).CorrectionResponseDirection);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-correction-response-delta-direction", 1, DiagnosticValueKind.Int32, "category", Main, ResponseContactGroup)]
        internal static int FootMotionCorrectionResponseDeltaDirection(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Response(in view, in metadata).CorrectionResponseDeltaDirection;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-correction-response-selected-speed", 1, DiagnosticValueKind.Float32, "metres-per-second", Main, ResponseContactGroup)]
        internal static float FootMotionCorrectionResponseSelectedSpeed(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).CorrectionResponseSelectedSpeed;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-correction-response-applied-delta", 1, DiagnosticValueKind.Float32, "metres", Main, ResponseContactGroup)]
        internal static float FootMotionCorrectionResponseAppliedDelta(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).CorrectionResponseAppliedDelta;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-vertical-continuity-owners", 1, DiagnosticValueKind.Int32, "category", Main, ResponseContactGroup)]
        internal static int FootMotionPlantVerticalContinuityOwners(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Response(in view, in metadata).PlantVerticalContinuityOwners;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-effective-correction-before", 1, DiagnosticValueKind.Vector3, "metres", Main, ResponseContactGroup)]
        internal static DiagnosticVector3 FootMotionPlantEffectiveCorrectionBefore(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Response(in view, in metadata).PlantEffectiveCorrectionBefore);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-effective-correction-after", 1, DiagnosticValueKind.Vector3, "metres", Main, ResponseContactGroup)]
        internal static DiagnosticVector3 FootMotionPlantEffectiveCorrectionAfter(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Response(in view, in metadata).PlantEffectiveCorrectionAfter);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-output-distance", 1, DiagnosticValueKind.Float32, "metres", Main, ResponseContactGroup)]
        internal static float FootMotionPlantOutputDistance(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).PlantOutputDistance;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-penetration-depth", 1, DiagnosticValueKind.Float32, "metres", Main, ResponseContactGroup)]
        internal static float FootMotionPlantPenetrationDepth(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Response(in view, in metadata).PlantPenetrationDepth;

        static CharacterFootCorrectionResponseDiagnostics Response(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).FootMotion.Response;
    }
}
