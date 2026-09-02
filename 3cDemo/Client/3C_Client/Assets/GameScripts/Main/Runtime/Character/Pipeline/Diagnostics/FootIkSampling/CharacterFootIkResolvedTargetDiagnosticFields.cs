using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static partial class CharacterFootIkDiagnosticFields
    {
        const string ResolvedTargetGroup = "resolved-target";
        const string ResolvedSupportTargetAvailableField =
            "character-foot-ik/main/resolved-support-target-available";

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-support-target-available", 1, DiagnosticValueKind.Boolean, "none", Main, ResolvedTargetGroup)]
        internal static bool ResolvedSupportTargetAvailable(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => SupportTarget(in view, in metadata).Available;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-support-target-frame-sequence", 1, DiagnosticValueKind.UInt64, "frame", Main, ResolvedTargetGroup)]
        internal static ulong ResolvedSupportTargetFrameSequence(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => SupportTarget(in view, in metadata).FrameSequence;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-support-target-completion-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, ResolvedTargetGroup)]
        internal static ulong ResolvedSupportTargetCompletionIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => SupportTarget(in view, in metadata).CompletionIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-support-target-side", 1, DiagnosticValueKind.Int32, "category", Main, ResolvedTargetGroup)]
        internal static int ResolvedSupportTargetSide(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)SupportTarget(in view, in metadata).Side;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-support-target-position", 1, DiagnosticValueKind.Vector3, "metres", Main, ResolvedTargetGroup, AvailabilityFieldId = ResolvedSupportTargetAvailableField)]
        internal static DiagnosticVector3 ResolvedSupportTargetPosition(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(SupportTarget(in view, in metadata).Position);

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-support-target-normal", 1, DiagnosticValueKind.Vector3, "direction", Main, ResolvedTargetGroup, AvailabilityFieldId = ResolvedSupportTargetAvailableField)]
        internal static DiagnosticVector3 ResolvedSupportTargetNormal(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(SupportTarget(in view, in metadata).SupportNormal);

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-support-target-surface-identity", 1, DiagnosticValueKind.Int32, "identity", Main, ResolvedTargetGroup, AvailabilityFieldId = ResolvedSupportTargetAvailableField)]
        internal static int ResolvedSupportTargetSurfaceIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => SupportTarget(in view, in metadata).SurfaceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-support-target-world-revision", 1, DiagnosticValueKind.UInt64, "identity", Main, ResolvedTargetGroup, AvailabilityFieldId = ResolvedSupportTargetAvailableField)]
        internal static ulong ResolvedSupportTargetWorldRevision(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => SupportTarget(in view, in metadata).WorldRevision;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-support-target-kind", 1, DiagnosticValueKind.Int32, "category", Main, ResolvedTargetGroup, AvailabilityFieldId = ResolvedSupportTargetAvailableField)]
        internal static int ResolvedSupportTargetKind(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)SupportTarget(in view, in metadata).Kind;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-support-target-position-source", 1, DiagnosticValueKind.Int32, "category", Main, ResolvedTargetGroup, AvailabilityFieldId = ResolvedSupportTargetAvailableField)]
        internal static int ResolvedSupportTargetPositionSource(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)SupportTarget(in view, in metadata).PositionSource;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-support-target-position-frame-sequence", 1, DiagnosticValueKind.UInt64, "frame", Main, ResolvedTargetGroup, AvailabilityFieldId = ResolvedSupportTargetAvailableField)]
        internal static ulong ResolvedSupportTargetPositionFrameSequence(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => SupportTarget(in view, in metadata).PositionFrameSequence;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-support-target-position-completion-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, ResolvedTargetGroup, AvailabilityFieldId = ResolvedSupportTargetAvailableField)]
        internal static ulong ResolvedSupportTargetPositionCompletionIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => SupportTarget(in view, in metadata).PositionCompletionIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-support-target-position-event-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, ResolvedTargetGroup, AvailabilityFieldId = ResolvedSupportTargetAvailableField)]
        internal static ulong ResolvedSupportTargetPositionEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => SupportTarget(in view, in metadata).PositionEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-support-target-position-path-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, ResolvedTargetGroup, AvailabilityFieldId = ResolvedSupportTargetAvailableField)]
        internal static ulong ResolvedSupportTargetPositionPathIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => SupportTarget(in view, in metadata).PositionPathIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-support-target-normal-source", 1, DiagnosticValueKind.Int32, "category", Main, ResolvedTargetGroup, AvailabilityFieldId = ResolvedSupportTargetAvailableField)]
        internal static int ResolvedSupportTargetNormalSource(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)SupportTarget(in view, in metadata).NormalSource;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-support-target-normal-frame-sequence", 1, DiagnosticValueKind.UInt64, "frame", Main, ResolvedTargetGroup, AvailabilityFieldId = ResolvedSupportTargetAvailableField)]
        internal static ulong ResolvedSupportTargetNormalFrameSequence(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => SupportTarget(in view, in metadata).NormalFrameSequence;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-support-target-normal-completion-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, ResolvedTargetGroup, AvailabilityFieldId = ResolvedSupportTargetAvailableField)]
        internal static ulong ResolvedSupportTargetNormalCompletionIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => SupportTarget(in view, in metadata).NormalCompletionIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-support-target-normal-event-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, ResolvedTargetGroup, AvailabilityFieldId = ResolvedSupportTargetAvailableField)]
        internal static ulong ResolvedSupportTargetNormalEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => SupportTarget(in view, in metadata).NormalEventIdentity;

        static CharacterFootSupportTargetDiagnostics SupportTarget(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Resolved(in view, in metadata).SupportTarget;
    }
}
