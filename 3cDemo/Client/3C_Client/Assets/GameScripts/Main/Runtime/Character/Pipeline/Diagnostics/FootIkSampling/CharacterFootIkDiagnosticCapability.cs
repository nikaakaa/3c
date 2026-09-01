using System;
using ThirdPerson.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static class CharacterFootIkDiagnosticIdentity
    {
        internal const string CapabilityId = "character-foot-ik";
        internal const int CapabilityRevision = 1;
        internal const string LineageTypeIdentity =
            "character-foot-ik-lineage/1";
    }

    [DiagnosticCapability(
        CharacterFootIkDiagnosticIdentity.CapabilityId,
        CharacterFootIkDiagnosticIdentity.CapabilityRevision,
        typeof(CharacterFootIkCommittedCaptureViewLease),
        typeof(CharacterFootIkCaptureMetadata))]
    internal static class CharacterFootIkDiagnosticCapability
    {
        internal static DiagnosticSampleKey CreateSampleKey(
            ulong sequence,
            in CharacterFootIkCommittedCaptureViewLease view)
        {
            CharacterPoseFrameLineage lineage = view.Lineage;
            if (!lineage.IsValid)
            {
                throw new ArgumentException(
                    "Foot IK diagnostic lineage is invalid.",
                    nameof(view));
            }
            var diagnosticLineage = new DiagnosticLineageKey(
                CharacterFootIkDiagnosticIdentity.LineageTypeIdentity,
                lineage.FrameIdentity,
                lineage.CompletionIdentity);
            return new DiagnosticSampleKey(
                sequence,
                in diagnosticLineage);
        }
    }

    internal readonly struct CharacterFootIkCaptureMetadata
    {
        internal CharacterFootIkCaptureMetadata(
            Guid sampleIdentity,
            DateTime startedUtc,
            in AnimationPresentationProgramIdentity program,
            Guid targetRuntimeInstanceId,
            int targetHostInstanceId,
            CharacterFootSide side)
        {
            if (sampleIdentity == Guid.Empty ||
                startedUtc.Kind != DateTimeKind.Utc ||
                !program.IsValid ||
                targetRuntimeInstanceId == Guid.Empty ||
                targetHostInstanceId == 0 ||
                (side != CharacterFootSide.Left &&
                 side != CharacterFootSide.Right))
            {
                throw new ArgumentException(
                    "Foot IK capture metadata is invalid.");
            }
            SampleIdentity = sampleIdentity.ToString("N");
            StartedUtcTicks = startedUtc.Ticks;
            ProgramIdentity =
                $"{program.ProjectionRevision}|{program.PosePlanHash}";
            TargetRuntimeInstanceId =
                targetRuntimeInstanceId.ToString("N");
            TargetHostInstanceId = targetHostInstanceId;
            Side = side;
        }

        internal string SampleIdentity { get; }
        internal long StartedUtcTicks { get; }
        internal string ProgramIdentity { get; }
        internal string TargetRuntimeInstanceId { get; }
        internal int TargetHostInstanceId { get; }
        internal CharacterFootSide Side { get; }
    }
}
