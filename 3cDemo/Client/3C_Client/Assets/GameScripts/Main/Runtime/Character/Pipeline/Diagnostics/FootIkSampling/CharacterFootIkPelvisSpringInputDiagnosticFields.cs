using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static partial class CharacterFootIkDiagnosticFields
    {
        const string PelvisSpringInputGroup = "pelvis-spring-input";

        [DiagnosticField(Capability, "character-foot-ik/main/stride-had-previous-state", 1, DiagnosticValueKind.Boolean, "none", Main, PelvisSpringInputGroup)]
        internal static bool StrideHadPreviousState(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Stride(in view).Response.HadPreviousState;

        [DiagnosticField(Capability, "character-foot-ik/main/stride-support-changed", 1, DiagnosticValueKind.Boolean, "none", Main, PelvisSpringInputGroup)]
        internal static bool StrideSupportChanged(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Stride(in view).Response.SupportChanged;

        [DiagnosticField(Capability, "character-foot-ik/main/stride-previous-slope", 1, DiagnosticValueKind.Int32, "category", Main, PelvisSpringInputGroup)]
        internal static int StridePreviousSlope(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Stride(in view).Response.PreviousSlope;

        [DiagnosticField(Capability, "character-foot-ik/main/stride-spring-handoff-reason", 1, DiagnosticValueKind.Int32, "category", Main, PelvisSpringInputGroup)]
        internal static int StrideSpringHandoffReason(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Stride(in view).Response.HandoffReason;

        [DiagnosticField(Capability, "character-foot-ik/main/stride-spring-velocity-reset", 1, DiagnosticValueKind.Boolean, "none", Main, PelvisSpringInputGroup)]
        internal static bool StrideSpringVelocityReset(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Stride(in view).Response.VelocityReset;

        [DiagnosticField(Capability, "character-foot-ik/main/stride-previous-spring-target", 1, DiagnosticValueKind.Float32, "metres", Main, PelvisSpringInputGroup)]
        internal static float StridePreviousSpringTarget(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Stride(in view).Response.PreviousTarget;

        [DiagnosticField(Capability, "character-foot-ik/main/stride-previous-spring-output", 1, DiagnosticValueKind.Float32, "metres", Main, PelvisSpringInputGroup)]
        internal static float StridePreviousSpringOutput(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Stride(in view).Response.PreviousOutput;

        [DiagnosticField(Capability, "character-foot-ik/main/stride-previous-spring-velocity", 1, DiagnosticValueKind.Float32, "metres-per-second", Main, PelvisSpringInputGroup)]
        internal static float StridePreviousSpringVelocity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Stride(in view).Response.PreviousVelocity;

        [DiagnosticField(Capability, "character-foot-ik/main/stride-spring-input", 1, DiagnosticValueKind.Float32, "metres", Main, PelvisSpringInputGroup)]
        internal static float StrideSpringInput(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Stride(in view).Response.Input;

        [DiagnosticField(Capability, "character-foot-ik/main/stride-spring-input-velocity", 1, DiagnosticValueKind.Float32, "metres-per-second", Main, PelvisSpringInputGroup)]
        internal static float StrideSpringInputVelocity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Stride(in view).Response.InputVelocity;

        [DiagnosticField(Capability, "character-foot-ik/main/stride-spring-frequency", 1, DiagnosticValueKind.Float32, "hertz", Main, PelvisSpringInputGroup)]
        internal static float StrideSpringFrequency(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Stride(in view).Response.Frequency;
    }
}
