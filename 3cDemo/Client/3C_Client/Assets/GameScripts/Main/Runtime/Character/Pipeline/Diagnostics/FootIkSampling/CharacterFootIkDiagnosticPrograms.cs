using ThirdPerson.GeneratedDiagnosticSampling;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    [DiagnosticSampler(
        CharacterFootIkDiagnosticIdentity.CapabilityId,
        "character-foot-ik/full",
        1,
        "character-foot-ik/full-host",
        "character-foot-ik-full/1",
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
        "root-hierarchy",
        Tables = new[] { "ground-geometry" })]
    internal static class CharacterFootIkFullSamplerDefinition
    {
    }

    [DiagnosticCaptureProgram(
        "character-foot-ik/full-program",
        CharacterFootIkDiagnosticIdentity.CapabilityId,
        typeof(CharacterFootIkFullSamplerDefinition))]
    internal static partial class CharacterFootIkFullCaptureProgram
    {
    }
}
