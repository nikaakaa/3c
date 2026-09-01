using System;
using ThirdPerson.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;

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
        typeof(CharacterFootIkCommittedCaptureViewLease))]
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
}
