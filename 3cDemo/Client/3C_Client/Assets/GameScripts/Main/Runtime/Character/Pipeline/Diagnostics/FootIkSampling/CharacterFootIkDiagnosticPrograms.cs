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
        "selected-step",
        "current-step",
        "incoming-step",
        "root-landing",
        "formal-output",
        "formal-events",
        "formal-input",
        "input-formal-events",
        "timing",
        "action",
        "primary-support",
        "body-correction",
        "current-support",
        "current-support-target",
        "prediction-motion",
        "goal",
        "selected-support-target",
        "output-stages",
        "ground-path",
        "landing-observation",
        "lifecycle",
        "motion-core",
        "motion-derived",
        "path-continuity",
        "response-contact",
        "resolved-core",
        "resolved-target",
        "resolved-contact",
        "pelvis-input",
        "root-hierarchy",
        Tables = new[] { "ground-geometry" })]
    internal static class CharacterFootIkFullSamplerDefinition
    {
    }

    [DiagnosticCaptureProgram(
        "character-foot-ik/full-program",
        CharacterFootIkDiagnosticIdentity.CapabilityId,
        typeof(CharacterFootIkFullSamplerDefinition))]
    public static partial class CharacterFootIkFullCaptureProgram
    {
    }
}
