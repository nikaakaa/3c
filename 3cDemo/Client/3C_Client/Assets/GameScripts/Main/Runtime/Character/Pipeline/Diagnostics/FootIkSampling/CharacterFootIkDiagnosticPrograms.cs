using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
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
