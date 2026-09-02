using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static partial class CharacterFootIkDiagnosticFields
    {
        const string PredictionMotionGroup = "prediction-motion";

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-motion-available", 1,
            DiagnosticValueKind.Boolean, "none", Main, PredictionMotionGroup)]
        internal static bool PredictionMotionAvailable(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionMotionAvailable;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-motion-reject-reason", 1,
            DiagnosticValueKind.Int32, "category", Main, PredictionMotionGroup)]
        internal static int PredictionMotionRejectReason(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)Input(in view).PredictionMotionRejectReason;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-motion-reset-reason", 1,
            DiagnosticValueKind.Int32, "category", Main, PredictionMotionGroup)]
        internal static int PredictionMotionResetReason(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)Input(in view).PredictionMotionResetReason;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-motion-source-identity", 1,
            DiagnosticValueKind.Identity, "identity", Main, PredictionMotionGroup)]
        internal static string PredictionMotionSourceIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionMotionSourceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-raw-current-velocity-x", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, PredictionMotionGroup)]
        internal static float PredictionRawCurrentVelocityX(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionRawCurrentVelocityX;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-raw-current-velocity-z", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, PredictionMotionGroup)]
        internal static float PredictionRawCurrentVelocityZ(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionRawCurrentVelocityZ;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-raw-continuation-velocity-x", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, PredictionMotionGroup)]
        internal static float PredictionRawContinuationVelocityX(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionRawContinuationVelocityX;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-raw-continuation-velocity-z", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, PredictionMotionGroup)]
        internal static float PredictionRawContinuationVelocityZ(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionRawContinuationVelocityZ;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-previous-stable-current-velocity-x", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, PredictionMotionGroup)]
        internal static float PredictionPreviousStableCurrentVelocityX(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionPreviousStableCurrentVelocityX;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-previous-stable-current-velocity-z", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, PredictionMotionGroup)]
        internal static float PredictionPreviousStableCurrentVelocityZ(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionPreviousStableCurrentVelocityZ;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-previous-stable-continuation-velocity-x", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, PredictionMotionGroup)]
        internal static float PredictionPreviousStableContinuationVelocityX(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionPreviousStableContinuationVelocityX;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-previous-stable-continuation-velocity-z", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, PredictionMotionGroup)]
        internal static float PredictionPreviousStableContinuationVelocityZ(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionPreviousStableContinuationVelocityZ;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-stable-current-velocity-x", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, PredictionMotionGroup)]
        internal static float PredictionStableCurrentVelocityX(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionStableCurrentVelocityX;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-stable-current-velocity-z", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, PredictionMotionGroup)]
        internal static float PredictionStableCurrentVelocityZ(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionStableCurrentVelocityZ;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-stable-continuation-velocity-x", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, PredictionMotionGroup)]
        internal static float PredictionStableContinuationVelocityX(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionStableContinuationVelocityX;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-stable-continuation-velocity-z", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, PredictionMotionGroup)]
        internal static float PredictionStableContinuationVelocityZ(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionStableContinuationVelocityZ;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-current-velocity-delta-x", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, PredictionMotionGroup)]
        internal static float PredictionCurrentVelocityDeltaX(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionCurrentVelocityDeltaX;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-current-velocity-delta-z", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, PredictionMotionGroup)]
        internal static float PredictionCurrentVelocityDeltaZ(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionCurrentVelocityDeltaZ;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-continuation-velocity-delta-x", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, PredictionMotionGroup)]
        internal static float PredictionContinuationVelocityDeltaX(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionContinuationVelocityDeltaX;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-continuation-velocity-delta-z", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, PredictionMotionGroup)]
        internal static float PredictionContinuationVelocityDeltaZ(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionContinuationVelocityDeltaZ;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-velocity-response-alpha", 1,
            DiagnosticValueKind.Float32, "unitless", Main, PredictionMotionGroup)]
        internal static float PredictionVelocityResponseAlpha(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionVelocityResponseAlpha;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-velocity-delta-threshold", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, PredictionMotionGroup)]
        internal static float PredictionVelocityDeltaThreshold(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionVelocityDeltaThreshold;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-velocity-smooth-speed", 1,
            DiagnosticValueKind.Float32, "per-second", Main, PredictionMotionGroup)]
        internal static float PredictionVelocitySmoothSpeed(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionVelocitySmoothSpeed;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-maximum-speed", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, PredictionMotionGroup)]
        internal static float PredictionMaximumSpeed(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionMaximumSpeed;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-current-response-applied", 1,
            DiagnosticValueKind.Boolean, "none", Main, PredictionMotionGroup)]
        internal static bool PredictionCurrentResponseApplied(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionCurrentResponseApplied;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-continuation-response-applied", 1,
            DiagnosticValueKind.Boolean, "none", Main, PredictionMotionGroup)]
        internal static bool PredictionContinuationResponseApplied(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionContinuationResponseApplied;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-current-maximum-speed-clamped", 1,
            DiagnosticValueKind.Boolean, "none", Main, PredictionMotionGroup)]
        internal static bool PredictionCurrentMaximumSpeedClamped(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionCurrentMaximumSpeedClamped;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-continuation-maximum-speed-clamped", 1,
            DiagnosticValueKind.Boolean, "none", Main, PredictionMotionGroup)]
        internal static bool PredictionContinuationMaximumSpeedClamped(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionContinuationMaximumSpeedClamped;

        [DiagnosticField(Capability, "character-foot-ik/main/prediction-motion-revision", 1,
            DiagnosticValueKind.UInt64, "identity", Main, PredictionMotionGroup)]
        internal static ulong PredictionMotionRevision(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Input(in view).PredictionMotionRevision;
    }
}
