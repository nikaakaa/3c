using KK.GeneratedDiagnosticSampling;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    [DiagnosticSampler(
        CharacterFootIkDiagnosticIdentity.CapabilityId,
        "character-foot-ik/full",
        1,
        DiagnosticOutputFormat.Csv,
        "capture-metadata",
        "identity",
        "current-step",
        "root-landing",
        "formal-motion",
        "formal-event",
        "timing",
        "action",
        "primary-support",
        "body-correction",
        "current-support",
        "current-support-probe",
        "support-target",
        "prediction-motion",
        "goal",
        "output-stages",
        "ground-path",
        "landing-observation",
        "lifecycle",
        "motion-core",
        "path-continuity",
        "response-contact",
        "resolved-core",
        "resolved-contact",
        "pelvis-input",
        "pelvis-spring-input",
        Tables = new[]
        {
            "ground-contacts",
            "ground-envelope",
            "ground-surfaces"
        })]
    internal static class CharacterFootIkFullSamplerDefinition
    {
    }

    [DiagnosticCaptureProgram(
        "character-foot-ik/full-program",
        CharacterFootIkDiagnosticIdentity.CapabilityId,
        new[]
        {
            CharacterFootIkDiagnosticIdentity.LeftDimensionId,
            CharacterFootIkDiagnosticIdentity.RightDimensionId
        },
        typeof(CharacterFootIkFullSamplerDefinition))]
    public static partial class CharacterFootIkFullCaptureProgram
    {
    }
}
