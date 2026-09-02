using ThirdPerson.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static partial class CharacterFootIkDiagnosticFields
    {
        const string PathContinuityGroup = "path-continuity";
        const string PathAvailableBeforeField =
            "character-foot-ik/main/foot-motion-path-available-before";
        const string PathAvailableAfterField =
            "character-foot-ik/main/foot-motion-path-available-after";
        const string ResidualDeadlineHalfLifeAvailableField =
            "character-foot-ik/main/foot-motion-residual-deadline-half-life-available";

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-path-continuity-evaluated", 1,
            DiagnosticValueKind.Boolean, "none", Main, PathContinuityGroup)]
        internal static bool FootMotionPathContinuityEvaluated(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).PathContinuityEvaluated;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-path-revision-reason", 1,
            DiagnosticValueKind.Int32, "category", Main, PathContinuityGroup)]
        internal static int FootMotionPathRevisionReason(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)PathContinuity(in view, in metadata).PathRevisionReason;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-path-residual-rebuilt", 1,
            DiagnosticValueKind.Boolean, "none", Main, PathContinuityGroup)]
        internal static bool FootMotionPathResidualRebuilt(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).PathResidualRebuilt;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-target-tracking-applied", 1,
            DiagnosticValueKind.Boolean, "none", Main, PathContinuityGroup)]
        internal static bool FootMotionTargetTrackingApplied(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).TargetTrackingApplied;

        [DiagnosticField(Capability, PathAvailableBeforeField, 1,
            DiagnosticValueKind.Boolean, "none", Main, PathContinuityGroup)]
        internal static bool FootMotionPathAvailableBefore(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).PathAvailableBefore;

        [DiagnosticField(Capability, PathAvailableAfterField, 1,
            DiagnosticValueKind.Boolean, "none", Main, PathContinuityGroup)]
        internal static bool FootMotionPathAvailableAfter(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).PathAvailableAfter;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-path-previous-landing-event-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, PathContinuityGroup)]
        internal static ulong FootMotionPathPreviousLandingEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).PathPreviousLandingEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-path-current-landing-event-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, PathContinuityGroup)]
        internal static ulong FootMotionPathCurrentLandingEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).PathCurrentLandingEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-path-previous-target-correction", 1,
            DiagnosticValueKind.Vector3, "metres", Main, PathContinuityGroup, AvailabilityFieldId = PathAvailableBeforeField)]
        internal static DiagnosticVector3 FootMotionPathPreviousTargetCorrection(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(PathContinuity(in view, in metadata).PathPreviousTargetCorrection);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-path-current-target-correction", 1,
            DiagnosticValueKind.Vector3, "metres", Main, PathContinuityGroup, AvailabilityFieldId = PathAvailableAfterField)]
        internal static DiagnosticVector3 FootMotionPathCurrentTargetCorrection(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(PathContinuity(in view, in metadata).PathCurrentTargetCorrection);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-path-landing-point-delta-meters", 1,
            DiagnosticValueKind.Float32, "metres", Main, PathContinuityGroup)]
        internal static float FootMotionPathLandingPointDeltaMeters(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).PathLandingPointDelta;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-path-target-delta-meters", 1,
            DiagnosticValueKind.Float32, "metres", Main, PathContinuityGroup)]
        internal static float FootMotionPathTargetDeltaMeters(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).PathTargetDelta;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-swing-residual-before-revision", 1,
            DiagnosticValueKind.Vector3, "metres", Main, PathContinuityGroup)]
        internal static DiagnosticVector3 FootMotionSwingResidualBeforeRevision(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(PathContinuity(in view, in metadata).SwingResidualBeforeRevision);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-swing-residual-before-decay", 1,
            DiagnosticValueKind.Vector3, "metres", Main, PathContinuityGroup)]
        internal static DiagnosticVector3 FootMotionSwingResidualBeforeDecay(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(PathContinuity(in view, in metadata).SwingResidualBeforeDecay);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-swing-residual-after-decay", 1,
            DiagnosticValueKind.Vector3, "metres", Main, PathContinuityGroup)]
        internal static DiagnosticVector3 FootMotionSwingResidualAfterDecay(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(PathContinuity(in view, in metadata).SwingResidualAfterDecay);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-residual-output-correction", 1,
            DiagnosticValueKind.Vector3, "metres", Main, PathContinuityGroup)]
        internal static DiagnosticVector3 FootMotionResidualOutputCorrection(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(PathContinuity(in view, in metadata).ResidualOutputCorrection);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-landing-acceptance-distance", 1,
            DiagnosticValueKind.Float32, "metres", Main, PathContinuityGroup)]
        internal static float FootMotionLandingAcceptanceDistance(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).LandingAcceptanceDistance;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-path-revision-distance", 1,
            DiagnosticValueKind.Float32, "metres", Main, PathContinuityGroup)]
        internal static float FootMotionPathRevisionDistance(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).PathRevisionDistance;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-swing-residual-tolerance", 1,
            DiagnosticValueKind.Float32, "metres", Main, PathContinuityGroup)]
        internal static float FootMotionSwingResidualTolerance(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).SwingResidualTolerance;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-residual-time-to-landing-seconds", 1,
            DiagnosticValueKind.Float32, "seconds", Main, PathContinuityGroup)]
        internal static float FootMotionResidualTimeToLandingSeconds(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).ResidualTimeToLandingSeconds;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-residual-base-half-life-seconds", 1,
            DiagnosticValueKind.Float32, "seconds", Main, PathContinuityGroup)]
        internal static float FootMotionResidualBaseHalfLifeSeconds(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).ResidualBaseHalfLifeSeconds;

        [DiagnosticField(Capability, ResidualDeadlineHalfLifeAvailableField, 1,
            DiagnosticValueKind.Boolean, "none", Main, PathContinuityGroup)]
        internal static bool FootMotionResidualDeadlineHalfLifeAvailable(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).ResidualDeadlineHalfLifeAvailable;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-residual-deadline-half-life-seconds", 1,
            DiagnosticValueKind.Float32, "seconds", Main, PathContinuityGroup, AvailabilityFieldId = ResidualDeadlineHalfLifeAvailableField)]
        internal static float FootMotionResidualDeadlineHalfLifeSeconds(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).ResidualDeadlineHalfLifeSeconds;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-residual-applied-half-life-seconds", 1,
            DiagnosticValueKind.Float32, "seconds", Main, PathContinuityGroup)]
        internal static float FootMotionResidualAppliedHalfLifeSeconds(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).ResidualAppliedHalfLifeSeconds;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-swing-target-height-adoption-mode", 1,
            DiagnosticValueKind.Int32, "category", Main, PathContinuityGroup)]
        internal static int FootMotionSwingTargetHeightAdoptionMode(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)PathContinuity(in view, in metadata).SwingTargetHeightAdoptionMode;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-swing-raw-target-height-along-up", 1,
            DiagnosticValueKind.Float32, "metres", Main, PathContinuityGroup)]
        internal static float FootMotionSwingRawTargetHeightAlongUp(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).SwingRawTargetHeightAlongUp;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-swing-filtered-target-height-before", 1,
            DiagnosticValueKind.Float32, "metres", Main, PathContinuityGroup)]
        internal static float FootMotionSwingFilteredTargetHeightBefore(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).SwingFilteredTargetHeightBefore;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-swing-target-height-delta", 1,
            DiagnosticValueKind.Float32, "metres", Main, PathContinuityGroup)]
        internal static float FootMotionSwingTargetHeightDelta(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).SwingTargetHeightDelta;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-swing-target-height-applied-delta", 1,
            DiagnosticValueKind.Float32, "metres", Main, PathContinuityGroup)]
        internal static float FootMotionSwingTargetHeightAppliedDelta(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).SwingTargetHeightAppliedDelta;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-swing-target-height-update-held", 1,
            DiagnosticValueKind.Boolean, "none", Main, PathContinuityGroup)]
        internal static bool FootMotionSwingTargetHeightUpdateHeld(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).SwingTargetHeightUpdateHeld;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-swing-target-height-force-refreshed", 1,
            DiagnosticValueKind.Boolean, "none", Main, PathContinuityGroup)]
        internal static bool FootMotionSwingTargetHeightForceRefreshed(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).SwingTargetHeightForceRefreshed;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-swing-target-height-rate-limited", 1,
            DiagnosticValueKind.Boolean, "none", Main, PathContinuityGroup)]
        internal static bool FootMotionSwingTargetHeightRateLimited(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).SwingTargetHeightRateLimited;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-swing-target-height-clamped", 1,
            DiagnosticValueKind.Boolean, "none", Main, PathContinuityGroup)]
        internal static bool FootMotionSwingTargetHeightClamped(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).SwingTargetHeightClamped;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-swing-target-height-force-refresh-distance", 1,
            DiagnosticValueKind.Float32, "metres", Main, PathContinuityGroup)]
        internal static float FootMotionSwingTargetHeightForceRefreshDistance(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).SwingTargetHeightForceRefreshDistance;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-swing-target-maximum-vertical-speed", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, PathContinuityGroup)]
        internal static float FootMotionSwingTargetMaximumVerticalSpeed(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).SwingTargetMaximumVerticalSpeed;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-swing-filtered-target-height-along-up", 1,
            DiagnosticValueKind.Float32, "metres", Main, PathContinuityGroup)]
        internal static float FootMotionSwingFilteredTargetHeightAlongUp(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            PathContinuity(in view, in metadata).SwingFilteredTargetHeightAlongUp;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-target-height-component-up", 1,
            DiagnosticValueKind.Vector3, "direction", Main, PathContinuityGroup)]
        internal static DiagnosticVector3 FootMotionTargetHeightComponentUp(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(PathContinuity(in view, in metadata).TargetHeightComponentUp);

        static CharacterFootPathContinuityDiagnostics PathContinuity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).FootMotion.PathContinuity;
    }
}
