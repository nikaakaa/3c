using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    [DiagnosticSampler(
        CharacterFootIkDiagnosticIdentity.CapabilityId,
        CharacterFootIkDiagnosticIdentity.CoreSamplerId,
        2,
        DiagnosticOutputFormat.Csv,
        "body-correction",
        "lifecycle",
        "lean",
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
        4,
        DiagnosticOutputFormat.Csv,
        IncludeAll = true)]
    internal static class CharacterFootIkFullSamplerDefinition
    {
    }

    [DiagnosticCaptureProgram(
        CharacterFootIkDiagnosticIdentity.CoreProgramId,
        CharacterFootIkDiagnosticIdentity.CapabilityId,
        CharacterPoseFootDiagnosticEvent.EventId,
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
        CharacterPoseFootDiagnosticEvent.EventId,
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
