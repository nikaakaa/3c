using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static partial class CharacterFootIkDiagnosticFields
    {
        const string MotionCoreGroup = "motion-core";
        const string MotionContactPlaneAvailableField =
            "character-foot-ik/main/foot-motion-contact-plane-available";

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-state", 1, DiagnosticValueKind.Int32, "category", Main, MotionCoreGroup)]
        internal static int FootMotionState(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)MotionCore(in view, in metadata).State;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-reject-reason", 1, DiagnosticValueKind.Int32, "category", Main, MotionCoreGroup)]
        internal static int FootMotionRejectReason(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)MotionCore(in view, in metadata).RejectReason;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-landing-event-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, MotionCoreGroup)]
        internal static ulong FootMotionLandingEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => MotionCore(in view, in metadata).LandingEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-ground-path-input-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, MotionCoreGroup)]
        internal static ulong FootMotionGroundPathInputIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => MotionCore(in view, in metadata).GroundPathInputIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-distance", 1, DiagnosticValueKind.Float32, "metres", Main, MotionCoreGroup)]
        internal static float FootMotionDistance(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => MotionCore(in view, in metadata).Distance;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-progress", 1, DiagnosticValueKind.Float32, "unitless", Main, MotionCoreGroup)]
        internal static float FootMotionProgress(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => MotionCore(in view, in metadata).Progress;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-original-sole", 1, DiagnosticValueKind.Vector3, "metres", Main, MotionCoreGroup)]
        internal static DiagnosticVector3 FootMotionOriginalSole(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(MotionCore(in view, in metadata).OriginalSole);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-original-ankle", 1, DiagnosticValueKind.Vector3, "metres", Main, MotionCoreGroup)]
        internal static DiagnosticVector3 FootMotionOriginalAnkle(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(MotionCore(in view, in metadata).OriginalAnkle);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-source-ankle-rotation", 1, DiagnosticValueKind.Quaternion, "unitless", Main, MotionCoreGroup)]
        internal static DiagnosticQuaternion FootMotionSourceAnkleRotation(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Quaternion(Foot(in view, in metadata).SourceAnkleRotation);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-source-heel", 1, DiagnosticValueKind.Vector3, "metres", Main, MotionCoreGroup)]
        internal static DiagnosticVector3 FootMotionSourceHeel(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Foot(in view, in metadata).SourceHeelPosition);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-source-toe", 1, DiagnosticValueKind.Vector3, "metres", Main, MotionCoreGroup)]
        internal static DiagnosticVector3 FootMotionSourceToe(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Foot(in view, in metadata).SourceToePosition);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-baseline-sample", 1, DiagnosticValueKind.Vector3, "metres", Main, MotionCoreGroup)]
        internal static DiagnosticVector3 FootMotionBaselineSample(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(MotionCore(in view, in metadata).BaselineSample);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-envelope-sample", 1, DiagnosticValueKind.Vector3, "metres", Main, MotionCoreGroup)]
        internal static DiagnosticVector3 FootMotionEnvelopeSample(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(MotionCore(in view, in metadata).EnvelopeSample);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-landing-prediction-error", 1, DiagnosticValueKind.Float32, "metres", Main, MotionCoreGroup)]
        internal static float FootMotionLandingPredictionError(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => MotionCore(in view, in metadata).LandingPredictionError;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-corrected-sole", 1, DiagnosticValueKind.Vector3, "metres", Main, MotionCoreGroup)]
        internal static DiagnosticVector3 FootMotionCorrectedSole(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(MotionCore(in view, in metadata).CorrectedSole);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-corrected-ankle", 1, DiagnosticValueKind.Vector3, "metres", Main, MotionCoreGroup)]
        internal static DiagnosticVector3 FootMotionCorrectedAnkle(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(MotionCore(in view, in metadata).CorrectedAnkle);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-position-weight", 1, DiagnosticValueKind.Float32, "unitless", Main, MotionCoreGroup)]
        internal static float FootMotionPositionWeight(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => MotionCore(in view, in metadata).PositionWeight;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-rotation-weight", 1, DiagnosticValueKind.Float32, "unitless", Main, MotionCoreGroup)]
        internal static float FootMotionRotationWeight(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => MotionCore(in view, in metadata).RotationWeight;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-constraint-state", 1, DiagnosticValueKind.Int32, "category", Main, MotionCoreGroup)]
        internal static int FootMotionConstraintState(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)MotionCore(in view, in metadata).ConstraintState;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-lock-response", 1, DiagnosticValueKind.Int32, "category", Main, MotionCoreGroup)]
        internal static int FootMotionLockResponse(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)MotionCore(in view, in metadata).LockResponse;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-support-horizontal-error", 1, DiagnosticValueKind.Float32, "metres", Main, MotionCoreGroup)]
        internal static float FootMotionSupportHorizontalError(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => MotionCore(in view, in metadata).SupportHorizontalError;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-contact-ownership", 1, DiagnosticValueKind.Float32, "unitless", Main, MotionCoreGroup)]
        internal static float FootMotionContactOwnership(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => MotionCore(in view, in metadata).ContactOwnership;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-support-weight", 1, DiagnosticValueKind.Float32, "unitless", Main, MotionCoreGroup)]
        internal static float FootMotionSupportWeight(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => MotionCore(in view, in metadata).SupportWeight;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-landing-reach-evaluated", 1, DiagnosticValueKind.Boolean, "none", Main, MotionCoreGroup)]
        internal static bool FootMotionLandingReachEvaluated(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => MotionCore(in view, in metadata).LandingReachEvaluated;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-landing-reach-available", 1, DiagnosticValueKind.Boolean, "none", Main, MotionCoreGroup)]
        internal static bool FootMotionLandingReachAvailable(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => MotionCore(in view, in metadata).LandingReachAvailable;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-support-contact-anchor", 1, DiagnosticValueKind.Vector3, "metres", Main, MotionCoreGroup)]
        internal static DiagnosticVector3 FootMotionSupportContactAnchor(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(MotionCore(in view, in metadata).SupportContactAnchor);

        [DiagnosticField(Capability, MotionContactPlaneAvailableField, 1, DiagnosticValueKind.Boolean, "none", Main, MotionCoreGroup)]
        internal static bool FootMotionContactPlaneAvailable(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => MotionCore(in view, in metadata).ContactPlaneAvailable;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-contact-surface-identity", 1, DiagnosticValueKind.Int32, "identity", Main, MotionCoreGroup, AvailabilityFieldId = MotionContactPlaneAvailableField)]
        internal static int FootMotionContactSurfaceIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => MotionCore(in view, in metadata).ContactSurfaceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-contact-plane-normal", 1, DiagnosticValueKind.Vector3, "direction", Main, MotionCoreGroup, AvailabilityFieldId = MotionContactPlaneAvailableField)]
        internal static DiagnosticVector3 FootMotionContactPlaneNormal(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(MotionCore(in view, in metadata).ContactPlaneNormal);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-desired-correction", 1, DiagnosticValueKind.Vector3, "metres", Main, MotionCoreGroup)]
        internal static DiagnosticVector3 FootMotionDesiredCorrection(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(MotionCore(in view, in metadata).DesiredCorrection);

        static CharacterFootSwingCoreDiagnostics MotionCore(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).FootMotion.Core;
    }
}
