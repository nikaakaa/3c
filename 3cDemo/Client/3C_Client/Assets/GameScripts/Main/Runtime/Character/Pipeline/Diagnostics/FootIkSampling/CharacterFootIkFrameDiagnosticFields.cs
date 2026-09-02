using ThirdPerson.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static partial class CharacterFootIkDiagnosticFields
    {
        const string TimingGroup = "timing";
        const string ActionGroup = "action";
        const string PrimarySupportGroup = "primary-support";
        const string BodyCorrectionGroup = "body-correction";
        const string PrimarySupportAvailable =
            "character-foot-ik/main/primary-support-has-value";

        [DiagnosticField(Capability, "character-foot-ik/main/presentation-delta-seconds", 1,
            DiagnosticValueKind.Float32, "seconds", Main, TimingGroup)]
        internal static float PresentationDeltaSeconds(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PresentationDeltaSeconds;

        [DiagnosticField(Capability, "character-foot-ik/main/previous-body-tick", 1,
            DiagnosticValueKind.UInt64, "frame", Main, TimingGroup)]
        internal static ulong PreviousBodyTick(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PreviousBodyTick;

        [DiagnosticField(Capability, "character-foot-ik/main/current-body-tick", 1,
            DiagnosticValueKind.UInt64, "frame", Main, TimingGroup)]
        internal static ulong CurrentBodyTick(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).CurrentBodyTick;

        [DiagnosticField(Capability, "character-foot-ik/main/body-sample-alpha", 1,
            DiagnosticValueKind.Float32, "unitless", Main, TimingGroup)]
        internal static float BodySampleAlpha(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).BodySampleAlpha;

        [DiagnosticField(Capability, "character-foot-ik/main/body-sample-age-seconds", 1,
            DiagnosticValueKind.Float32, "seconds", Main, TimingGroup)]
        internal static float BodySampleAgeSeconds(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).BodySampleAgeSeconds;

        [DiagnosticField(Capability, "character-foot-ik/main/motion-timeline-available", 1,
            DiagnosticValueKind.Boolean, "none", Main, TimingGroup)]
        internal static bool MotionTimelineAvailable(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).MotionTimelineAvailable;

        [DiagnosticField(Capability, "character-foot-ik/main/timeline-generation", 1,
            DiagnosticValueKind.UInt64, "identity", Main, TimingGroup)]
        internal static ulong TimelineGeneration(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).TimelineGeneration;

        [DiagnosticField(Capability, "character-foot-ik/main/timeline-authority-tick", 1,
            DiagnosticValueKind.UInt64, "frame", Main, TimingGroup)]
        internal static ulong TimelineAuthorityTick(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).TimelineAuthorityTick;

        [DiagnosticField(Capability, "character-foot-ik/main/timeline-tick-rate", 1,
            DiagnosticValueKind.Int32, "hertz", Main, TimingGroup)]
        internal static int TimelineTickRate(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).TimelineTickRate;

        [DiagnosticField(Capability, "character-foot-ik/main/timeline-current-velocity-x", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, TimingGroup)]
        internal static float TimelineCurrentVelocityX(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).TimelineCurrentVelocityX;

        [DiagnosticField(Capability, "character-foot-ik/main/timeline-current-velocity-z", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, TimingGroup)]
        internal static float TimelineCurrentVelocityZ(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).TimelineCurrentVelocityZ;

        [DiagnosticField(Capability, "character-foot-ik/main/timeline-continuation-velocity-x", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, TimingGroup)]
        internal static float TimelineContinuationVelocityX(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).TimelineContinuationVelocityX;

        [DiagnosticField(Capability, "character-foot-ik/main/timeline-continuation-velocity-z", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, TimingGroup)]
        internal static float TimelineContinuationVelocityZ(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).TimelineContinuationVelocityZ;

        [DiagnosticField(Capability, "character-foot-ik/main/timeline-has-continuation", 1,
            DiagnosticValueKind.Boolean, "none", Main, TimingGroup)]
        internal static bool TimelineHasContinuation(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).TimelineHasContinuation;

        [DiagnosticField(Capability, "character-foot-ik/main/timeline-body-yaw-velocity-degrees-per-second", 1,
            DiagnosticValueKind.Float32, "degrees-per-second", Main, TimingGroup)]
        internal static float TimelineBodyYawVelocityDegreesPerSecond(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).TimelineBodyYawVelocityDegreesPerSecond;

        [DiagnosticField(Capability, "character-foot-ik/main/timeline-maximum-body-yaw-velocity-degrees-per-second", 1,
            DiagnosticValueKind.Float32, "degrees-per-second", Main, TimingGroup)]
        internal static float TimelineMaximumBodyYawVelocityDegreesPerSecond(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).TimelineMaximumBodyYawVelocityDegreesPerSecond;

        [DiagnosticField(Capability, "character-foot-ik/main/current-segment-remaining-seconds", 1,
            DiagnosticValueKind.Float32, "seconds", Main, TimingGroup)]
        internal static float CurrentSegmentRemainingSeconds(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).CurrentSegmentRemainingSeconds;

        [DiagnosticField(Capability, "character-foot-ik/main/grounded", 1,
            DiagnosticValueKind.Boolean, "none", Main, ActionGroup)]
        internal static bool Grounded(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).Grounded;

        [DiagnosticField(Capability, "character-foot-ik/main/horizontal-speed", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, ActionGroup)]
        internal static float HorizontalSpeed(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).HorizontalSpeed;

        [DiagnosticField(Capability, "character-foot-ik/main/left-action-instance-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, ActionGroup)]
        internal static ulong LeftActionInstanceIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).LeftActionInstanceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/left-action-foot-weight", 1,
            DiagnosticValueKind.Float32, "unitless", Main, ActionGroup)]
        internal static float LeftActionFootWeight(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).LeftActionFootWeight;

        [DiagnosticField(Capability, "character-foot-ik/main/right-action-instance-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, ActionGroup)]
        internal static ulong RightActionInstanceIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).RightActionInstanceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/right-action-foot-weight", 1,
            DiagnosticValueKind.Float32, "unitless", Main, ActionGroup)]
        internal static float RightActionFootWeight(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).RightActionFootWeight;

        [DiagnosticField(Capability, PrimarySupportAvailable, 1,
            DiagnosticValueKind.Boolean, "none", Main, PrimarySupportGroup)]
        internal static bool PrimarySupportHasValue(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.LandingPrediction.PrimarySupport.HasValue;

        [DiagnosticField(Capability, "character-foot-ik/main/primary-support-side", 1,
            DiagnosticValueKind.Int32, "category", Main, PrimarySupportGroup,
            AvailabilityFieldId = PrimarySupportAvailable)]
        internal static int PrimarySupportSide(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)view.LandingPrediction.PrimarySupport.Side;

        [DiagnosticField(Capability, "character-foot-ik/main/primary-support-landing-event-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, PrimarySupportGroup,
            AvailabilityFieldId = PrimarySupportAvailable)]
        internal static ulong PrimarySupportLandingEventIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.LandingPrediction.PrimarySupport.LandingEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/primary-support-retained", 1,
            DiagnosticValueKind.Boolean, "none", Main, PrimarySupportGroup)]
        internal static bool PrimarySupportRetained(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.LandingPrediction.PrimarySupport.Retained;

        [DiagnosticField(Capability, "character-foot-ik/main/visible-body-position", 1,
            DiagnosticValueKind.Vector3, "metres", Main, BodyCorrectionGroup)]
        internal static DiagnosticVector3 VisibleBodyPosition(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Input(in view).VisibleBodyPosition);

        [DiagnosticField(Capability, "character-foot-ik/main/visible-body-rotation", 1,
            DiagnosticValueKind.Quaternion, "unitless", Main, BodyCorrectionGroup)]
        internal static DiagnosticQuaternion VisibleBodyRotation(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Quaternion(Input(in view).VisibleBodyRotation);

        [DiagnosticField(Capability, "character-foot-ik/main/visible-body-velocity", 1,
            DiagnosticValueKind.Vector3, "metres-per-second", Main, BodyCorrectionGroup)]
        internal static DiagnosticVector3 VisibleBodyVelocity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Input(in view).VisibleBodyVelocity);

        [DiagnosticField(Capability, "character-foot-ik/main/visible-body-yaw-velocity-degrees-per-second", 1,
            DiagnosticValueKind.Float32, "degrees-per-second", Main, BodyCorrectionGroup)]
        internal static float VisibleBodyYawVelocityDegreesPerSecond(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).VisibleBodyYawVelocityDegreesPerSecond;

        [DiagnosticField(Capability, "character-foot-ik/main/target-body-position", 1,
            DiagnosticValueKind.Vector3, "metres", Main, BodyCorrectionGroup)]
        internal static DiagnosticVector3 TargetBodyPosition(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Input(in view).TargetBodyPosition);

        [DiagnosticField(Capability, "character-foot-ik/main/target-body-rotation", 1,
            DiagnosticValueKind.Quaternion, "unitless", Main, BodyCorrectionGroup)]
        internal static DiagnosticQuaternion TargetBodyRotation(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Quaternion(Input(in view).TargetBodyRotation);

        [DiagnosticField(Capability, "character-foot-ik/main/target-body-velocity", 1,
            DiagnosticValueKind.Vector3, "metres-per-second", Main, BodyCorrectionGroup)]
        internal static DiagnosticVector3 TargetBodyVelocity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Input(in view).TargetBodyVelocity);

        [DiagnosticField(Capability, "character-foot-ik/main/target-body-yaw-velocity-degrees-per-second", 1,
            DiagnosticValueKind.Float32, "degrees-per-second", Main, BodyCorrectionGroup)]
        internal static float TargetBodyYawVelocityDegreesPerSecond(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).TargetBodyYawVelocityDegreesPerSecond;

        [DiagnosticField(Capability, "character-foot-ik/main/body-position-error", 1,
            DiagnosticValueKind.Float32, "metres", Main, BodyCorrectionGroup)]
        internal static float BodyPositionError(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).BodyPositionError;

        [DiagnosticField(Capability, "character-foot-ik/main/body-rotation-error", 1,
            DiagnosticValueKind.Float32, "degrees", Main, BodyCorrectionGroup)]
        internal static float BodyRotationError(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).BodyRotationError;

        [DiagnosticField(Capability, "character-foot-ik/main/correction-position-error", 1,
            DiagnosticValueKind.Vector3, "metres", Main, BodyCorrectionGroup)]
        internal static DiagnosticVector3 CorrectionPositionError(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Input(in view).CorrectionPositionError);

        [DiagnosticField(Capability, "character-foot-ik/main/correction-position-velocity", 1,
            DiagnosticValueKind.Vector3, "metres-per-second", Main, BodyCorrectionGroup)]
        internal static DiagnosticVector3 CorrectionPositionVelocity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Input(in view).CorrectionPositionVelocity);

        [DiagnosticField(Capability, "character-foot-ik/main/correction-yaw-velocity-degrees-per-second", 1,
            DiagnosticValueKind.Float32, "degrees-per-second", Main, BodyCorrectionGroup)]
        internal static float CorrectionYawVelocityDegreesPerSecond(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).CorrectionYawVelocityDegreesPerSecond;

        [DiagnosticField(Capability, "character-foot-ik/main/correction-active", 1,
            DiagnosticValueKind.Boolean, "none", Main, BodyCorrectionGroup)]
        internal static bool CorrectionActive(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).CorrectionActive;

        [DiagnosticField(Capability, "character-foot-ik/main/correction-clamped", 1,
            DiagnosticValueKind.Boolean, "none", Main, BodyCorrectionGroup)]
        internal static bool CorrectionClamped(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).CorrectionClamped;

        [DiagnosticField(Capability, "character-foot-ik/main/correction-settled", 1,
            DiagnosticValueKind.Boolean, "none", Main, BodyCorrectionGroup)]
        internal static bool CorrectionSettled(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).CorrectionSettled;

        [DiagnosticField(Capability, "character-foot-ik/main/body-reset-sequence", 1,
            DiagnosticValueKind.UInt64, "identity", Main, BodyCorrectionGroup)]
        internal static ulong BodyResetSequence(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).BodyResetSequence;

        static CharacterFootLandingPredictionInputDiagnostics Input(
            in CharacterFootIkCommittedCaptureViewLease view) =>
            view.LandingPrediction.Input;
    }
}
