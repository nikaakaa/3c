using ThirdPerson.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static class CharacterFootIkDiagnosticFields
    {
        const string Capability =
            CharacterFootIkDiagnosticIdentity.CapabilityId;
        const string Main = "main";
        const string IdentityGroup = "identity";
        const string CaptureMetadataGroup = "capture-metadata";

        [DiagnosticField(Capability, "character-foot-ik/main/sample-identity", 1,
            DiagnosticValueKind.Identity, "identity", Main, CaptureMetadataGroup)]
        internal static string SampleIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            metadata.SampleIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/sample-started-utc-ticks", 1,
            DiagnosticValueKind.Int64, "utc-ticks", Main, CaptureMetadataGroup)]
        internal static long SampleStartedUtcTicks(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            metadata.StartedUtcTicks;

        [DiagnosticField(Capability, "character-foot-ik/main/program-identity", 1,
            DiagnosticValueKind.Identity, "identity", Main, CaptureMetadataGroup)]
        internal static string ProgramIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            metadata.ProgramIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/target-runtime-instance-id", 1,
            DiagnosticValueKind.Identity, "identity", Main, CaptureMetadataGroup)]
        internal static string TargetRuntimeInstanceId(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            metadata.TargetRuntimeInstanceId;

        [DiagnosticField(Capability, "character-foot-ik/main/target-host-instance-id", 1,
            DiagnosticValueKind.Int32, "identity", Main, CaptureMetadataGroup)]
        internal static int TargetHostInstanceId(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            metadata.TargetHostInstanceId;

        [DiagnosticField(Capability, "character-foot-ik/main/projection-revision", 1,
            DiagnosticValueKind.Identity, "identity", Main, IdentityGroup)]
        internal static string ProjectionRevision(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.Lineage.ProjectionRevision;

        [DiagnosticField(Capability, "character-foot-ik/main/pose-graph-id", 1,
            DiagnosticValueKind.Identity, "identity", Main, IdentityGroup)]
        internal static string PoseGraphId(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.Lineage.PoseGraphId;

        [DiagnosticField(Capability, "character-foot-ik/main/pose-graph-revision", 1,
            DiagnosticValueKind.Identity, "identity", Main, IdentityGroup)]
        internal static string PoseGraphRevision(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.Lineage.PoseGraphRevision;

        [DiagnosticField(Capability, "character-foot-ik/main/pose-plan-hash", 1,
            DiagnosticValueKind.Identity, "identity", Main, IdentityGroup)]
        internal static string PosePlanHash(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.Lineage.PoseProgramIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/frame-sequence", 1,
            DiagnosticValueKind.UInt64, "frame", Main, IdentityGroup)]
        internal static ulong FrameSequence(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.Lineage.PresentationFrame;

        [DiagnosticField(Capability, "character-foot-ik/main/completion-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, IdentityGroup)]
        internal static ulong CompletionIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.Lineage.CompletionIdentity;
    }
}
