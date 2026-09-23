using System;
using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.PresentationReplication
{
    public static class CharacterPresentationReplicationDiagnosticIdentity
    {
        public const string CapabilityId =
            "character-presentation-replication";
        public const int CapabilityRevision = 2;
        public const string MainDimensionId =
            "character-presentation-replication/main";
        public const string CoreSamplerId =
            "character-presentation-replication/core";
        public const string FullSamplerId =
            "character-presentation-replication/full";
        public const string CoreProgramId =
            "character-presentation-replication/core-program";
        public const string FullProgramId =
            "character-presentation-replication/full-program";
        public const string ReferenceProfileId = "corin";
    }

    public readonly struct CharacterPresentationReplicationCaptureMetadata
    {
        public CharacterPresentationReplicationCaptureMetadata(
            Guid sampleIdentity,
            DateTime startedUtc,
            in CharacterPoseDiagnosticTarget presentation,
            Guid targetRuntimeInstanceId,
            int targetHostInstanceId,
            string referenceProfileId)
        {
            if (sampleIdentity == Guid.Empty ||
                startedUtc.Kind != DateTimeKind.Utc ||
                !presentation.IsValid ||
                targetRuntimeInstanceId == Guid.Empty ||
                targetHostInstanceId == 0 ||
                string.IsNullOrWhiteSpace(referenceProfileId))
            {
                throw new ArgumentException(
                    "Presentation replication capture metadata is invalid.");
            }
            SampleIdentity = sampleIdentity.ToString("N");
            StartedUtcTicks = startedUtc.Ticks;
            PresentationIdentity =
                $"{presentation.GraphRevision}|{presentation.ResourceRevision}";
            ReferenceProfileId = referenceProfileId.Trim();
            TargetRuntimeInstanceId = targetRuntimeInstanceId.ToString("N");
            TargetHostInstanceId = targetHostInstanceId;
        }

        [DiagnosticField]
        [DiagnosticGroup("capture-metadata")]
        public string SampleIdentity { get; }

        [DiagnosticField]
        [DiagnosticGroup("capture-metadata")]
        public long StartedUtcTicks { get; }

        [DiagnosticField]
        [DiagnosticGroup("capture-metadata")]
        public string PresentationIdentity { get; }

        [DiagnosticField]
        [DiagnosticGroup("capture-metadata")]
        public string ReferenceProfileId { get; }

        [DiagnosticField]
        [DiagnosticGroup("capture-metadata")]
        public string TargetRuntimeInstanceId { get; }

        [DiagnosticField]
        [DiagnosticGroup("capture-metadata")]
        public int TargetHostInstanceId { get; }
    }

    [DiagnosticCapability(
        CharacterPresentationReplicationDiagnosticIdentity.CapabilityId,
        CharacterPresentationReplicationDiagnosticIdentity.CapabilityRevision,
        typeof(CharacterPresentationReplicationCaptureMetadata))]
    [DiagnosticFactRoot(
        "animation",
        typeof(CharacterNativePoseCaptureFrame))]
    [DiagnosticFactRoot("camera", typeof(CharacterNativeCameraCaptureFrame))]
    [DiagnosticFactRoot(
        "facts",
        typeof(CharacterNativeBodyCaptureFrame))]
    [DiagnosticFactRoot(
        "commands",
        typeof(CharacterNativeCommandCaptureFrame))]
    internal static class CharacterPresentationReplicationDiagnosticCapability
    {
    }

    [DiagnosticSampler(
        CharacterPresentationReplicationDiagnosticIdentity.CapabilityId,
        CharacterPresentationReplicationDiagnosticIdentity.CoreSamplerId,
        1,
        DiagnosticOutputFormat.Csv,
        "animation-frame",
        "animation-identity",
        "animation-output",
        "animation-parameters",
        "animation-state",
        "camera-frame",
        "camera-output",
        "presentation-commands",
        "capture-metadata")]
    internal static class CharacterPresentationReplicationCoreSamplerDefinition
    {
    }

    [DiagnosticSampler(
        CharacterPresentationReplicationDiagnosticIdentity.CapabilityId,
        CharacterPresentationReplicationDiagnosticIdentity.FullSamplerId,
        1,
        DiagnosticOutputFormat.Csv,
        IncludeAll = true)]
    internal static class CharacterPresentationReplicationFullSamplerDefinition
    {
    }

    [DiagnosticCaptureProgram(
        CharacterPresentationReplicationDiagnosticIdentity.CoreProgramId,
        CharacterPresentationReplicationDiagnosticIdentity.CapabilityId,
        CharacterNativePresentationDiagnosticEvent.EventId,
        new[]
        {
            CharacterPresentationReplicationDiagnosticIdentity.MainDimensionId
        },
        typeof(CharacterPresentationReplicationCoreSamplerDefinition))]
    public static partial class CharacterPresentationReplicationCoreCaptureProgram
    {
    }

    [DiagnosticCaptureProgram(
        CharacterPresentationReplicationDiagnosticIdentity.FullProgramId,
        CharacterPresentationReplicationDiagnosticIdentity.CapabilityId,
        CharacterNativePresentationDiagnosticEvent.EventId,
        new[]
        {
            CharacterPresentationReplicationDiagnosticIdentity.MainDimensionId
        },
        typeof(CharacterPresentationReplicationFullSamplerDefinition))]
    public static partial class CharacterPresentationReplicationFullCaptureProgram
    {
    }
}
