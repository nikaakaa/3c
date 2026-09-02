using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static partial class CharacterFootIkDiagnosticFields
    {
        const string CurrentSupportTargetGroup = "current-support-target";
        const string TargetAvailableField =
            "character-foot-ik/main/current-support-target-available";

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-target-available", 1, DiagnosticValueKind.Boolean, "none", Main, CurrentSupportTargetGroup)]
        internal static bool CurrentSupportTargetAvailable(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Target(in view, in metadata).Available;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-target-frame-sequence", 1, DiagnosticValueKind.UInt64, "frame", Main, CurrentSupportTargetGroup)]
        internal static ulong CurrentSupportTargetFrameSequence(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Target(in view, in metadata).FrameSequence;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-target-completion-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, CurrentSupportTargetGroup)]
        internal static ulong CurrentSupportTargetCompletionIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Target(in view, in metadata).CompletionIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-target-side", 1, DiagnosticValueKind.Int32, "category", Main, CurrentSupportTargetGroup)]
        internal static int CurrentSupportTargetSide(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Target(in view, in metadata).Side;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-target-position", 1, DiagnosticValueKind.Vector3, "metres", Main, CurrentSupportTargetGroup, AvailabilityFieldId = TargetAvailableField)]
        internal static DiagnosticVector3 CurrentSupportTargetPosition(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Target(in view, in metadata).Position);

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-target-normal", 1, DiagnosticValueKind.Vector3, "direction", Main, CurrentSupportTargetGroup, AvailabilityFieldId = TargetAvailableField)]
        internal static DiagnosticVector3 CurrentSupportTargetNormal(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Target(in view, in metadata).SupportNormal);

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-target-surface-identity", 1, DiagnosticValueKind.Int32, "identity", Main, CurrentSupportTargetGroup, AvailabilityFieldId = TargetAvailableField)]
        internal static int CurrentSupportTargetSurfaceIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Target(in view, in metadata).SurfaceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-target-world-revision", 1, DiagnosticValueKind.UInt64, "identity", Main, CurrentSupportTargetGroup, AvailabilityFieldId = TargetAvailableField)]
        internal static ulong CurrentSupportTargetWorldRevision(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Target(in view, in metadata).WorldRevision;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-target-kind", 1, DiagnosticValueKind.Int32, "category", Main, CurrentSupportTargetGroup, AvailabilityFieldId = TargetAvailableField)]
        internal static int CurrentSupportTargetKind(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Target(in view, in metadata).Kind;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-target-position-source", 1, DiagnosticValueKind.Int32, "category", Main, CurrentSupportTargetGroup, AvailabilityFieldId = TargetAvailableField)]
        internal static int CurrentSupportTargetPositionSource(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Target(in view, in metadata).PositionSource;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-target-position-frame-sequence", 1, DiagnosticValueKind.UInt64, "frame", Main, CurrentSupportTargetGroup, AvailabilityFieldId = TargetAvailableField)]
        internal static ulong CurrentSupportTargetPositionFrameSequence(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Target(in view, in metadata).PositionFrameSequence;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-target-position-completion-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, CurrentSupportTargetGroup, AvailabilityFieldId = TargetAvailableField)]
        internal static ulong CurrentSupportTargetPositionCompletionIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Target(in view, in metadata).PositionCompletionIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-target-position-event-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, CurrentSupportTargetGroup, AvailabilityFieldId = TargetAvailableField)]
        internal static ulong CurrentSupportTargetPositionEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Target(in view, in metadata).PositionEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-target-position-path-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, CurrentSupportTargetGroup, AvailabilityFieldId = TargetAvailableField)]
        internal static ulong CurrentSupportTargetPositionPathIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Target(in view, in metadata).PositionPathIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-target-normal-source", 1, DiagnosticValueKind.Int32, "category", Main, CurrentSupportTargetGroup, AvailabilityFieldId = TargetAvailableField)]
        internal static int CurrentSupportTargetNormalSource(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Target(in view, in metadata).NormalSource;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-target-normal-frame-sequence", 1, DiagnosticValueKind.UInt64, "frame", Main, CurrentSupportTargetGroup, AvailabilityFieldId = TargetAvailableField)]
        internal static ulong CurrentSupportTargetNormalFrameSequence(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Target(in view, in metadata).NormalFrameSequence;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-target-normal-completion-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, CurrentSupportTargetGroup, AvailabilityFieldId = TargetAvailableField)]
        internal static ulong CurrentSupportTargetNormalCompletionIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Target(in view, in metadata).NormalCompletionIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-target-normal-event-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, CurrentSupportTargetGroup, AvailabilityFieldId = TargetAvailableField)]
        internal static ulong CurrentSupportTargetNormalEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Target(in view, in metadata).NormalEventIdentity;

        static CharacterFootSupportTargetDiagnostics Target(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).CurrentSupport.Target;
    }
}
