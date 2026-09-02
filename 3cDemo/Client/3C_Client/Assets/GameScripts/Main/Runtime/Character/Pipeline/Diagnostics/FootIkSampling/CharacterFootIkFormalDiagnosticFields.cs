using ThirdPerson.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static partial class CharacterFootIkDiagnosticFields
    {
        const string FormalOutputGroup = "formal-output";
        const string FormalInputGroup = "formal-input";
        const string FormalOutputAvailable =
            "character-foot-ik/main/formal-step-observation-available";
        const string FormalInputAvailable =
            "character-foot-ik/main/input-formal-step-observation-available";

        [DiagnosticField(Capability, FormalOutputAvailable, 1,
            DiagnosticValueKind.Boolean, "none", Main, FormalOutputGroup)]
        internal static bool FormalStepObservationAvailable(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputSample(in view, in metadata).IsValid;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-step-source-identity", 1,
            DiagnosticValueKind.Identity, "identity", Main, FormalOutputGroup,
            AvailabilityFieldId = FormalOutputAvailable)]
        internal static string FormalStepSourceIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.FootStepObservation.SourceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-step-source-weight", 1,
            DiagnosticValueKind.Float32, "unitless", Main, FormalOutputGroup,
            AvailabilityFieldId = FormalOutputAvailable)]
        internal static float FormalStepSourceWeight(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.FootStepObservation.SourceWeight;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-step-source-normalized-time", 1,
            DiagnosticValueKind.Float32, "unitless", Main, FormalOutputGroup,
            AvailabilityFieldId = FormalOutputAvailable)]
        internal static float FormalStepSourceNormalizedTime(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.FootStepObservation.NormalizedTime;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-step-time-seconds", 1,
            DiagnosticValueKind.Float32, "seconds", Main, FormalOutputGroup,
            AvailabilityFieldId = FormalOutputAvailable)]
        internal static float FormalStepTimeSeconds(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputSample(in view, in metadata).TimeToLandingSeconds;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-step-distance", 1,
            DiagnosticValueKind.Float32, "metres", Main, FormalOutputGroup,
            AvailabilityFieldId = FormalOutputAvailable)]
        internal static float FormalStepDistance(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputSample(in view, in metadata).Distance;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-foot-height", 1,
            DiagnosticValueKind.Float32, "metres", Main, FormalOutputGroup,
            AvailabilityFieldId = FormalOutputAvailable)]
        internal static float FormalFootHeight(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputSample(in view, in metadata).FootHeight;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-toe-height", 1,
            DiagnosticValueKind.Float32, "metres", Main, FormalOutputGroup,
            AvailabilityFieldId = FormalOutputAvailable)]
        internal static float FormalToeHeight(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputSample(in view, in metadata).ToeHeight;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-toe-speed", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, FormalOutputGroup,
            AvailabilityFieldId = FormalOutputAvailable)]
        internal static float FormalToeSpeed(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputSample(in view, in metadata).ToeSpeed;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-position-error", 1,
            DiagnosticValueKind.Float32, "metres", Main, FormalOutputGroup,
            AvailabilityFieldId = FormalOutputAvailable)]
        internal static float FormalPositionError(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputSample(in view, in metadata).PositionError;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-rotation-error", 1,
            DiagnosticValueKind.Float32, "degrees", Main, FormalOutputGroup,
            AvailabilityFieldId = FormalOutputAvailable)]
        internal static float FormalRotationError(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputSample(in view, in metadata).RotationError;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-contact", 1,
            DiagnosticValueKind.Float32, "unitless", Main, FormalOutputGroup,
            AvailabilityFieldId = FormalOutputAvailable)]
        internal static float FormalContact(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputSample(in view, in metadata).Contact;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-lock-mode", 1,
            DiagnosticValueKind.Int32, "category", Main, FormalOutputGroup,
            AvailabilityFieldId = FormalOutputAvailable)]
        internal static int FormalLockMode(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)FormalOutputSample(in view, in metadata).LockMode;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-lock-weight", 1,
            DiagnosticValueKind.Float32, "unitless", Main, FormalOutputGroup,
            AvailabilityFieldId = FormalOutputAvailable)]
        internal static float FormalLockWeight(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputSample(in view, in metadata).LockWeight;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-support", 1,
            DiagnosticValueKind.Float32, "unitless", Main, FormalOutputGroup,
            AvailabilityFieldId = FormalOutputAvailable)]
        internal static float FormalSupport(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputSample(in view, in metadata).Support;

        [DiagnosticField(Capability, FormalInputAvailable, 1,
            DiagnosticValueKind.Boolean, "none", Main, FormalInputGroup)]
        internal static bool InputFormalStepObservationAvailable(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputOrigin(in view).IsValid &&
            FormalInputSample(in view, in metadata).IsValid;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-step-source-id", 1,
            DiagnosticValueKind.Identity, "identity", Main, FormalInputGroup,
            AvailabilityFieldId = FormalInputAvailable)]
        internal static string InputFormalStepSourceId(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputOrigin(in view).SourceId;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-step-source-identity", 1,
            DiagnosticValueKind.Identity, "identity", Main, FormalInputGroup,
            AvailabilityFieldId = FormalInputAvailable)]
        internal static string InputFormalStepSourceIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputOrigin(in view).SourceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-step-source-weight", 1,
            DiagnosticValueKind.Float32, "unitless", Main, FormalInputGroup,
            AvailabilityFieldId = FormalInputAvailable)]
        internal static float InputFormalStepSourceWeight(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputOrigin(in view).SourceWeight;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-step-source-normalized-time", 1,
            DiagnosticValueKind.Float32, "unitless", Main, FormalInputGroup,
            AvailabilityFieldId = FormalInputAvailable)]
        internal static float InputFormalStepSourceNormalizedTime(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputOrigin(in view).NormalizedTime;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-step-clip-binding-index", 1,
            DiagnosticValueKind.Int32, "count", Main, FormalInputGroup,
            AvailabilityFieldId = FormalInputAvailable)]
        internal static int InputFormalStepClipBindingIndex(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputOrigin(in view).ClipBindingIndex;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-step-source-cycle", 1,
            DiagnosticValueKind.Int32, "count", Main, FormalInputGroup,
            AvailabilityFieldId = FormalInputAvailable)]
        internal static int InputFormalStepSourceCycle(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputOrigin(in view).Cycle;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-step-contribution-continuity-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, FormalInputGroup,
            AvailabilityFieldId = FormalInputAvailable)]
        internal static ulong InputFormalStepContributionContinuityIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputOrigin(in view).ContributionContinuityIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-step-completion-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, FormalInputGroup,
            AvailabilityFieldId = FormalInputAvailable)]
        internal static ulong InputFormalStepCompletionIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputOrigin(in view).CompletionIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-step-time-seconds", 1,
            DiagnosticValueKind.Float32, "seconds", Main, FormalInputGroup,
            AvailabilityFieldId = FormalInputAvailable)]
        internal static float InputFormalStepTimeSeconds(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputSample(in view, in metadata).TimeToLandingSeconds;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-step-distance", 1,
            DiagnosticValueKind.Float32, "metres", Main, FormalInputGroup,
            AvailabilityFieldId = FormalInputAvailable)]
        internal static float InputFormalStepDistance(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputSample(in view, in metadata).Distance;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-foot-height", 1,
            DiagnosticValueKind.Float32, "metres", Main, FormalInputGroup,
            AvailabilityFieldId = FormalInputAvailable)]
        internal static float InputFormalFootHeight(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputSample(in view, in metadata).FootHeight;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-toe-height", 1,
            DiagnosticValueKind.Float32, "metres", Main, FormalInputGroup,
            AvailabilityFieldId = FormalInputAvailable)]
        internal static float InputFormalToeHeight(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputSample(in view, in metadata).ToeHeight;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-toe-speed", 1,
            DiagnosticValueKind.Float32, "metres-per-second", Main, FormalInputGroup,
            AvailabilityFieldId = FormalInputAvailable)]
        internal static float InputFormalToeSpeed(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputSample(in view, in metadata).ToeSpeed;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-position-error", 1,
            DiagnosticValueKind.Float32, "metres", Main, FormalInputGroup,
            AvailabilityFieldId = FormalInputAvailable)]
        internal static float InputFormalPositionError(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputSample(in view, in metadata).PositionError;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-rotation-error", 1,
            DiagnosticValueKind.Float32, "degrees", Main, FormalInputGroup,
            AvailabilityFieldId = FormalInputAvailable)]
        internal static float InputFormalRotationError(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputSample(in view, in metadata).RotationError;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-contact", 1,
            DiagnosticValueKind.Float32, "unitless", Main, FormalInputGroup,
            AvailabilityFieldId = FormalInputAvailable)]
        internal static float InputFormalContact(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputSample(in view, in metadata).Contact;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-lock-mode", 1,
            DiagnosticValueKind.Int32, "category", Main, FormalInputGroup,
            AvailabilityFieldId = FormalInputAvailable)]
        internal static int InputFormalLockMode(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)FormalInputSample(in view, in metadata).LockMode;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-lock-weight", 1,
            DiagnosticValueKind.Float32, "unitless", Main, FormalInputGroup,
            AvailabilityFieldId = FormalInputAvailable)]
        internal static float InputFormalLockWeight(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputSample(in view, in metadata).LockWeight;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-support", 1,
            DiagnosticValueKind.Float32, "unitless", Main, FormalInputGroup,
            AvailabilityFieldId = FormalInputAvailable)]
        internal static float InputFormalSupport(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputSample(in view, in metadata).Support;

        static AnimationFootMotionRuntimeSample FormalOutputSample(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata)
        {
            AnimationFootStepObservationRuntimeSnapshot observation =
                view.FootStepObservation;
            return metadata.Side == CharacterFootSide.Left
                ? observation.Left
                : observation.Right;
        }

        static CharacterFootStepObservationInputDiagnostics FormalInputOrigin(
            in CharacterFootIkCommittedCaptureViewLease view) =>
            view.LandingPrediction.Input.FootStepObservation;

        static AnimationFootMotionRuntimeSample FormalInputSample(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata)
        {
            CharacterFootStepObservationInputDiagnostics origin =
                FormalInputOrigin(in view);
            return metadata.Side == CharacterFootSide.Left
                ? origin.Left
                : origin.Right;
        }
    }
}
