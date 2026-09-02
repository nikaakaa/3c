using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static partial class CharacterFootIkDiagnosticFields
    {
        const string LifecycleGroup = "lifecycle";
        const string PreviousLockRequestAvailableField =
            "character-foot-ik/main/foot-motion-previous-lock-request-available";
        const string PreviousContactAnchorAvailableField =
            "character-foot-ik/main/foot-motion-previous-contact-anchor-available";
        const string CurrentContactAnchorAvailableField =
            "character-foot-ik/main/foot-motion-current-contact-anchor-available";

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-pre-transition-reason", 1, DiagnosticValueKind.Int32, "category", Main, LifecycleGroup)]
        internal static int FootMotionPreTransitionReason(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)Lifecycle(in view, in metadata).PreTransitionReason;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-pre-transition-source", 1, DiagnosticValueKind.Int32, "category", Main, LifecycleGroup)]
        internal static int FootMotionPreTransitionSource(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)Lifecycle(in view, in metadata).PreTransitionSource;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-pre-transition-target", 1, DiagnosticValueKind.Int32, "category", Main, LifecycleGroup)]
        internal static int FootMotionPreTransitionTarget(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)Lifecycle(in view, in metadata).PreTransitionTarget;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-pre-transition-anchor-command", 1, DiagnosticValueKind.Int32, "category", Main, LifecycleGroup)]
        internal static int FootMotionPreTransitionAnchorCommand(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)Lifecycle(in view, in metadata).PreTransitionAnchorCommand;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-post-transition-reason", 1, DiagnosticValueKind.Int32, "category", Main, LifecycleGroup)]
        internal static int FootMotionPostTransitionReason(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)Lifecycle(in view, in metadata).PostTransitionReason;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-post-transition-source", 1, DiagnosticValueKind.Int32, "category", Main, LifecycleGroup)]
        internal static int FootMotionPostTransitionSource(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)Lifecycle(in view, in metadata).PostTransitionSource;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-post-transition-target", 1, DiagnosticValueKind.Int32, "category", Main, LifecycleGroup)]
        internal static int FootMotionPostTransitionTarget(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)Lifecycle(in view, in metadata).PostTransitionTarget;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-post-transition-anchor-command", 1, DiagnosticValueKind.Int32, "category", Main, LifecycleGroup)]
        internal static int FootMotionPostTransitionAnchorCommand(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)Lifecycle(in view, in metadata).PostTransitionAnchorCommand;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-lifecycle-transition-evaluated", 1, DiagnosticValueKind.Boolean, "none", Main, LifecycleGroup)]
        internal static bool FootMotionLifecycleTransitionEvaluated(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).LifecycleTransitionEvaluated;

        [DiagnosticField(Capability, PreviousLockRequestAvailableField, 1, DiagnosticValueKind.Boolean, "none", Main, LifecycleGroup)]
        internal static bool FootMotionPreviousLockRequestAvailable(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).PreviousLockRequestAvailable;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-previous-lock-requested", 1, DiagnosticValueKind.Boolean, "none", Main, LifecycleGroup, AvailabilityFieldId = PreviousLockRequestAvailableField)]
        internal static bool FootMotionPreviousLockRequested(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).PreviousLockRequested;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-previous-lock-request-event-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, LifecycleGroup, AvailabilityFieldId = PreviousLockRequestAvailableField)]
        internal static ulong FootMotionPreviousLockRequestEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).PreviousLockRequestEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-previous-lock-request-mode", 1, DiagnosticValueKind.Int32, "category", Main, LifecycleGroup, AvailabilityFieldId = PreviousLockRequestAvailableField)]
        internal static int FootMotionPreviousLockRequestMode(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)Lifecycle(in view, in metadata).PreviousLockRequestMode;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-previous-lock-request-weight", 1, DiagnosticValueKind.Float32, "unitless", Main, LifecycleGroup, AvailabilityFieldId = PreviousLockRequestAvailableField)]
        internal static float FootMotionPreviousLockRequestWeight(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).PreviousLockRequestWeight;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-previous-contact-edge-seconds", 1, DiagnosticValueKind.Float32, "seconds", Main, LifecycleGroup)]
        internal static float FootMotionPreviousContactEdgeSeconds(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).PreviousContactEdgeSeconds;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-previous-latest-contact-event-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, LifecycleGroup)]
        internal static ulong FootMotionPreviousLatestContactEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).PreviousLatestContactEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-previous-latest-released-contact-event-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, LifecycleGroup)]
        internal static ulong FootMotionPreviousLatestReleasedContactEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).PreviousLatestReleasedContactEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-previous-completed-lock-weight-event-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, LifecycleGroup)]
        internal static ulong FootMotionPreviousCompletedLockWeightEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).PreviousCompletedLockWeightEventIdentity;

        [DiagnosticField(Capability, PreviousContactAnchorAvailableField, 1, DiagnosticValueKind.Boolean, "none", Main, LifecycleGroup)]
        internal static bool FootMotionPreviousContactAnchorAvailable(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).PreviousContactAnchorAvailable;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-previous-contact-anchor-event-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, LifecycleGroup, AvailabilityFieldId = PreviousContactAnchorAvailableField)]
        internal static ulong FootMotionPreviousContactAnchorEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).PreviousContactAnchorEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-previous-contact-anchor-acquired-frame-sequence", 1, DiagnosticValueKind.UInt64, "frame", Main, LifecycleGroup, AvailabilityFieldId = PreviousContactAnchorAvailableField)]
        internal static ulong FootMotionPreviousContactAnchorAcquiredFrameSequence(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).PreviousContactAnchorAcquiredFrameSequence;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-previous-contact-anchor-acquired-completion-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, LifecycleGroup, AvailabilityFieldId = PreviousContactAnchorAvailableField)]
        internal static ulong FootMotionPreviousContactAnchorAcquiredCompletionIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).PreviousContactAnchorAcquiredCompletionIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-previous-contact-anchor-world-revision", 1, DiagnosticValueKind.UInt64, "identity", Main, LifecycleGroup, AvailabilityFieldId = PreviousContactAnchorAvailableField)]
        internal static ulong FootMotionPreviousContactAnchorWorldRevision(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).PreviousContactAnchorWorldRevision;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-previous-contact-anchor-surface-identity", 1, DiagnosticValueKind.Int32, "identity", Main, LifecycleGroup, AvailabilityFieldId = PreviousContactAnchorAvailableField)]
        internal static int FootMotionPreviousContactAnchorSurfaceIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).PreviousContactAnchorSurfaceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-previous-contact-anchor-point", 1, DiagnosticValueKind.Vector3, "metres", Main, LifecycleGroup, AvailabilityFieldId = PreviousContactAnchorAvailableField)]
        internal static DiagnosticVector3 FootMotionPreviousContactAnchorPoint(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Lifecycle(in view, in metadata).PreviousContactAnchorPoint);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-previous-contact-anchor-normal", 1, DiagnosticValueKind.Vector3, "direction", Main, LifecycleGroup, AvailabilityFieldId = PreviousContactAnchorAvailableField)]
        internal static DiagnosticVector3 FootMotionPreviousContactAnchorNormal(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Lifecycle(in view, in metadata).PreviousContactAnchorNormal);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-current-lock-requested", 1, DiagnosticValueKind.Boolean, "none", Main, LifecycleGroup)]
        internal static bool FootMotionCurrentLockRequested(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).CurrentLockRequested;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-current-lock-request-event-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, LifecycleGroup)]
        internal static ulong FootMotionCurrentLockRequestEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).CurrentLockRequestEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-current-lock-request-mode", 1, DiagnosticValueKind.Int32, "category", Main, LifecycleGroup)]
        internal static int FootMotionCurrentLockRequestMode(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)Lifecycle(in view, in metadata).CurrentLockRequestMode;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-current-lock-request-weight", 1, DiagnosticValueKind.Float32, "unitless", Main, LifecycleGroup)]
        internal static float FootMotionCurrentLockRequestWeight(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).CurrentLockRequestWeight;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-current-lock-request-availability", 1, DiagnosticValueKind.Int32, "category", Main, LifecycleGroup)]
        internal static int FootMotionCurrentLockRequestAvailability(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)Lifecycle(in view, in metadata).CurrentLockRequestAvailability;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-contact-edge", 1, DiagnosticValueKind.Int32, "category", Main, LifecycleGroup)]
        internal static int FootMotionContactEdge(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)Lifecycle(in view, in metadata).ContactEdge;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-current-contact-edge-seconds", 1, DiagnosticValueKind.Float32, "seconds", Main, LifecycleGroup)]
        internal static float FootMotionCurrentContactEdgeSeconds(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).CurrentContactEdgeSeconds;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-current-latest-contact-event-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, LifecycleGroup)]
        internal static ulong FootMotionCurrentLatestContactEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).CurrentLatestContactEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-current-latest-released-contact-event-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, LifecycleGroup)]
        internal static ulong FootMotionCurrentLatestReleasedContactEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).CurrentLatestReleasedContactEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-current-completed-lock-weight-event-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, LifecycleGroup)]
        internal static ulong FootMotionCurrentCompletedLockWeightEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).CurrentCompletedLockWeightEventIdentity;

        [DiagnosticField(Capability, CurrentContactAnchorAvailableField, 1, DiagnosticValueKind.Boolean, "none", Main, LifecycleGroup)]
        internal static bool FootMotionCurrentContactAnchorAvailable(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).CurrentContactAnchorAvailable;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-current-contact-anchor-event-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, LifecycleGroup, AvailabilityFieldId = CurrentContactAnchorAvailableField)]
        internal static ulong FootMotionCurrentContactAnchorEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).CurrentContactAnchorEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-current-contact-anchor-acquired-frame-sequence", 1, DiagnosticValueKind.UInt64, "frame", Main, LifecycleGroup, AvailabilityFieldId = CurrentContactAnchorAvailableField)]
        internal static ulong FootMotionCurrentContactAnchorAcquiredFrameSequence(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).CurrentContactAnchorAcquiredFrameSequence;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-current-contact-anchor-acquired-completion-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, LifecycleGroup, AvailabilityFieldId = CurrentContactAnchorAvailableField)]
        internal static ulong FootMotionCurrentContactAnchorAcquiredCompletionIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).CurrentContactAnchorAcquiredCompletionIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-current-contact-anchor-world-revision", 1, DiagnosticValueKind.UInt64, "identity", Main, LifecycleGroup, AvailabilityFieldId = CurrentContactAnchorAvailableField)]
        internal static ulong FootMotionCurrentContactAnchorWorldRevision(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).CurrentContactAnchorWorldRevision;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-current-contact-anchor-surface-identity", 1, DiagnosticValueKind.Int32, "identity", Main, LifecycleGroup, AvailabilityFieldId = CurrentContactAnchorAvailableField)]
        internal static int FootMotionCurrentContactAnchorSurfaceIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).CurrentContactAnchorSurfaceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-current-contact-anchor-point", 1, DiagnosticValueKind.Vector3, "metres", Main, LifecycleGroup, AvailabilityFieldId = CurrentContactAnchorAvailableField)]
        internal static DiagnosticVector3 FootMotionCurrentContactAnchorPoint(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Lifecycle(in view, in metadata).CurrentContactAnchorPoint);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-current-contact-anchor-normal", 1, DiagnosticValueKind.Vector3, "direction", Main, LifecycleGroup, AvailabilityFieldId = CurrentContactAnchorAvailableField)]
        internal static DiagnosticVector3 FootMotionCurrentContactAnchorNormal(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Lifecycle(in view, in metadata).CurrentContactAnchorNormal);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-same-event-contact-reentry-refreshed", 1, DiagnosticValueKind.Boolean, "none", Main, LifecycleGroup)]
        internal static bool FootMotionSameEventContactReentryRefreshed(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).SameEventContactReentryRefreshed;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-same-event-contact-reentry-unavailable", 1, DiagnosticValueKind.Boolean, "none", Main, LifecycleGroup)]
        internal static bool FootMotionSameEventContactReentryUnavailable(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).SameEventContactReentryUnavailable;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-retained-verified-anchor", 1, DiagnosticValueKind.Boolean, "none", Main, LifecycleGroup)]
        internal static bool FootMotionRetainedVerifiedAnchor(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).RetainedVerifiedAnchor;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-reentry-interpolation-history-retained", 1, DiagnosticValueKind.Boolean, "none", Main, LifecycleGroup)]
        internal static bool FootMotionReentryInterpolationHistoryRetained(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).ReentryInterpolationHistoryRetained;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-formal-foot-placement-weight", 1, DiagnosticValueKind.Float32, "unitless", Main, LifecycleGroup)]
        internal static float FootMotionFormalFootPlacementWeight(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).FormalFootPlacementWeight;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-hard-ownership-loss", 1, DiagnosticValueKind.Boolean, "none", Main, LifecycleGroup)]
        internal static bool FootMotionHardOwnershipLoss(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).HardOwnershipLoss;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-hard-ownership-loss-reason", 1, DiagnosticValueKind.Int32, "category", Main, LifecycleGroup)]
        internal static int FootMotionHardOwnershipLossReason(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            (int)Lifecycle(in view, in metadata).HardOwnershipLossReason;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-pre-transition-suppress-output", 1, DiagnosticValueKind.Boolean, "none", Main, LifecycleGroup)]
        internal static bool FootMotionPreTransitionSuppressOutput(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).PreTransitionSuppressOutput;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-pre-transition-reset-interpolation", 1, DiagnosticValueKind.Boolean, "none", Main, LifecycleGroup)]
        internal static bool FootMotionPreTransitionResetInterpolation(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).PreTransitionResetInterpolation;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-post-transition-evaluated", 1, DiagnosticValueKind.Boolean, "none", Main, LifecycleGroup)]
        internal static bool FootMotionPostTransitionEvaluated(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).PostTransitionEvaluated;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-post-transition-suppress-output", 1, DiagnosticValueKind.Boolean, "none", Main, LifecycleGroup)]
        internal static bool FootMotionPostTransitionSuppressOutput(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).PostTransitionSuppressOutput;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-post-transition-reset-interpolation", 1, DiagnosticValueKind.Boolean, "none", Main, LifecycleGroup)]
        internal static bool FootMotionPostTransitionResetInterpolation(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) =>
            Lifecycle(in view, in metadata).PostTransitionResetInterpolation;

        static CharacterFootLifecycleTransitionDiagnostics Lifecycle(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).FootMotion.Lifecycle;
    }
}
