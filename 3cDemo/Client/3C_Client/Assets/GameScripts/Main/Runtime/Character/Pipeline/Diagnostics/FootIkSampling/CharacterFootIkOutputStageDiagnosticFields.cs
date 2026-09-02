using ThirdPerson.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static partial class CharacterFootIkDiagnosticFields
    {
        const string OutputStagesGroup = "output-stages";
        const string SafetyFloorAvailable =
            "character-foot-ik/main/foot-motion-safety-floor-available";

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-state-target-correction", 1,
            DiagnosticValueKind.Vector3, "metres", Main, OutputStagesGroup)]
        internal static DiagnosticVector3 FootMotionStateTargetCorrection(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(OutputStages(in view, in metadata).StateTargetCorrection);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-interpolation-policy", 1,
            DiagnosticValueKind.Int32, "category", Main, OutputStagesGroup)]
        internal static int FootMotionInterpolationPolicy(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)OutputStages(in view, in metadata).InterpolationPolicy;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-interpolation-output-correction", 1,
            DiagnosticValueKind.Vector3, "metres", Main, OutputStagesGroup)]
        internal static DiagnosticVector3 FootMotionInterpolationOutputCorrection(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(OutputStages(in view, in metadata).InterpolationOutputCorrection);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-interpolation-completed", 1,
            DiagnosticValueKind.Boolean, "none", Main, OutputStagesGroup)]
        internal static bool FootMotionInterpolationCompleted(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            OutputStages(in view, in metadata).InterpolationCompleted;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-constraint-state-before", 1,
            DiagnosticValueKind.Int32, "category", Main, OutputStagesGroup)]
        internal static int FootMotionConstraintStateBefore(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)OutputStages(in view, in metadata).ConstraintStateBefore;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-lock-response-before", 1,
            DiagnosticValueKind.Int32, "category", Main, OutputStagesGroup)]
        internal static int FootMotionLockResponseBefore(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)OutputStages(in view, in metadata).LockResponseBefore;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-output-stages-available", 1,
            DiagnosticValueKind.Boolean, "none", Main, OutputStagesGroup)]
        internal static bool FootMotionOutputStagesAvailable(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            OutputStages(in view, in metadata).OutputStagesAvailable;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-releasing-completed-to-swing", 1,
            DiagnosticValueKind.Boolean, "none", Main, OutputStagesGroup)]
        internal static bool FootMotionReleasingCompletedToSwing(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            OutputStages(in view, in metadata).ReleasingCompletedToSwing;

        [DiagnosticField(Capability, SafetyFloorAvailable, 1,
            DiagnosticValueKind.Boolean, "none", Main, OutputStagesGroup)]
        internal static bool FootMotionSafetyFloorAvailable(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            OutputStages(in view, in metadata).SafetyFloorAvailable;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-safety-floor-owner", 1,
            DiagnosticValueKind.Int32, "category", Main, OutputStagesGroup)]
        internal static int FootMotionSafetyFloorOwner(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)OutputStages(in view, in metadata).SafetyFloorOwner;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-safety-floor-owner-surface-identity", 1,
            DiagnosticValueKind.Int32, "identity", Main, OutputStagesGroup)]
        internal static int FootMotionSafetyFloorOwnerSurfaceIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            OutputStages(in view, in metadata).SafetyFloorOwnerSurfaceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-safety-floor-owner-path-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, OutputStagesGroup)]
        internal static ulong FootMotionSafetyFloorOwnerPathIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            OutputStages(in view, in metadata).SafetyFloorOwnerPathIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-correction-before-safety-floor", 1,
            DiagnosticValueKind.Vector3, "metres", Main, OutputStagesGroup)]
        internal static DiagnosticVector3 FootMotionCorrectionBeforeSafetyFloor(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(OutputStages(in view, in metadata).CorrectionBeforeSafetyFloor);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-safety-floor-minimum-correction", 1,
            DiagnosticValueKind.Vector3, "metres", Main, OutputStagesGroup,
            AvailabilityFieldId = SafetyFloorAvailable)]
        internal static DiagnosticVector3 FootMotionSafetyFloorMinimumCorrection(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(OutputStages(in view, in metadata).SafetyFloorMinimumCorrection);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-safety-floor-output-correction", 1,
            DiagnosticValueKind.Vector3, "metres", Main, OutputStagesGroup,
            AvailabilityFieldId = SafetyFloorAvailable)]
        internal static DiagnosticVector3 FootMotionSafetyFloorOutputCorrection(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(OutputStages(in view, in metadata).SafetyFloorOutputCorrection);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-final-effective-correction", 1,
            DiagnosticValueKind.Vector3, "metres", Main, OutputStagesGroup)]
        internal static DiagnosticVector3 FootMotionFinalEffectiveCorrection(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(OutputStages(in view, in metadata).FinalEffectiveCorrection);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-safety-floor-clamped", 1,
            DiagnosticValueKind.Boolean, "none", Main, OutputStagesGroup)]
        internal static bool FootMotionSafetyFloorClamped(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            OutputStages(in view, in metadata).SafetyFloorClamped;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-safety-floor-clamp-meters", 1,
            DiagnosticValueKind.Float32, "metres", Main, OutputStagesGroup)]
        internal static float FootMotionSafetyFloorClampMeters(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            OutputStages(in view, in metadata).SafetyFloorClampMeters;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-safety-floor-clearance-before-meters", 1,
            DiagnosticValueKind.Float32, "metres", Main, OutputStagesGroup)]
        internal static float FootMotionSafetyFloorClearanceBeforeMeters(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            OutputStages(in view, in metadata).SafetyFloorClearanceBeforeMeters;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-safety-floor-clearance-after-meters", 1,
            DiagnosticValueKind.Float32, "metres", Main, OutputStagesGroup)]
        internal static float FootMotionSafetyFloorClearanceAfterMeters(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            OutputStages(in view, in metadata).SafetyFloorClearanceAfterMeters;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-interpolation-evaluated", 1,
            DiagnosticValueKind.Boolean, "none", Main, OutputStagesGroup)]
        internal static bool FootMotionPlantInterpolationEvaluated(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            OutputStages(in view, in metadata).PlantInterpolationEvaluated;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-target-event-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, OutputStagesGroup)]
        internal static ulong FootMotionPlantTargetEventIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            OutputStages(in view, in metadata).PlantTargetEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-target-verified", 1,
            DiagnosticValueKind.Boolean, "none", Main, OutputStagesGroup)]
        internal static bool FootMotionPlantTargetVerified(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            OutputStages(in view, in metadata).PlantTargetVerified;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-target-kind", 1,
            DiagnosticValueKind.Int32, "category", Main, OutputStagesGroup)]
        internal static int FootMotionPlantTargetKind(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)OutputStages(in view, in metadata).PlantTargetKind;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-lock-response", 1,
            DiagnosticValueKind.Int32, "category", Main, OutputStagesGroup)]
        internal static int FootMotionPlantLockResponse(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)OutputStages(in view, in metadata).PlantLockResponse;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-lock-weight-completed", 1,
            DiagnosticValueKind.Boolean, "none", Main, OutputStagesGroup)]
        internal static bool FootMotionPlantLockWeightCompleted(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            OutputStages(in view, in metadata).PlantLockWeightCompleted;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-desired-point", 1,
            DiagnosticValueKind.Vector3, "metres", Main, OutputStagesGroup)]
        internal static DiagnosticVector3 FootMotionPlantDesiredPoint(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(OutputStages(in view, in metadata).PlantDesiredPoint);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-plant-filtered-point", 1,
            DiagnosticValueKind.Vector3, "metres", Main, OutputStagesGroup)]
        internal static DiagnosticVector3 FootMotionPlantFilteredPoint(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(OutputStages(in view, in metadata).PlantFilteredPoint);

        static CharacterFootOutputStagesDiagnostics OutputStages(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).FootMotion.OutputStages;
    }
}
