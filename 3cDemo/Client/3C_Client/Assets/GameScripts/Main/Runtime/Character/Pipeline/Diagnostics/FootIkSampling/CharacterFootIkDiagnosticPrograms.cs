using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    [DiagnosticSampler(
        CharacterFootIkDiagnosticIdentity.CapabilityId,
        CharacterFootIkDiagnosticIdentity.CoreSamplerId,
        1,
        DiagnosticOutputFormat.Csv,
        "body-correction",
        "lifecycle",
        "motion-core",
        "physical",
        "resolved-core",
        "timing")]
    internal static class CharacterFootIkCoreSamplerDefinition
    {
    }

    [DiagnosticSampler(
        CharacterFootIkDiagnosticIdentity.CapabilityId,
        CharacterFootIkDiagnosticIdentity.FullSamplerId,
        1,
        DiagnosticOutputFormat.Csv,
        IncludeAll = true)]
    internal static class CharacterFootIkFullSamplerDefinition
    {
    }

    [DiagnosticCaptureProgram(
        CharacterFootIkDiagnosticIdentity.CoreProgramId,
        CharacterFootIkDiagnosticIdentity.CapabilityId,
        CharacterFootIkCommitDiagnosticEvent.EventId,
        new[]
        {
            CharacterFootIkDiagnosticIdentity.LeftDimensionId,
            CharacterFootIkDiagnosticIdentity.RightDimensionId
        },
        typeof(CharacterFootIkCoreSamplerDefinition))]
    public static partial class CharacterFootIkCoreCaptureProgram
    {
    }

    [DiagnosticCaptureProgram(
        CharacterFootIkDiagnosticIdentity.FullProgramId,
        CharacterFootIkDiagnosticIdentity.CapabilityId,
        CharacterFootIkCommitDiagnosticEvent.EventId,
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
