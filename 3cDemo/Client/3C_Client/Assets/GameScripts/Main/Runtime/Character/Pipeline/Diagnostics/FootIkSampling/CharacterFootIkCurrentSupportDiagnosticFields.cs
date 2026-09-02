using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static partial class CharacterFootIkDiagnosticFields
    {
        const string CurrentSupportGroup = "current-support";

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-frame-sequence", 1, DiagnosticValueKind.UInt64, "frame", Main, CurrentSupportGroup)]
        internal static ulong CurrentSupportFrameSequence(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => CurrentSupport(in view, in metadata).FrameSequence;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-completion-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, CurrentSupportGroup)]
        internal static ulong CurrentSupportCompletionIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => CurrentSupport(in view, in metadata).CompletionIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-world-revision", 1, DiagnosticValueKind.UInt64, "identity", Main, CurrentSupportGroup)]
        internal static ulong CurrentSupportWorldRevision(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => CurrentSupport(in view, in metadata).WorldRevision;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-is-specified", 1, DiagnosticValueKind.Boolean, "none", Main, CurrentSupportGroup)]
        internal static bool CurrentSupportIsSpecified(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => CurrentSupport(in view, in metadata).IsSpecified;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-available", 1, DiagnosticValueKind.Boolean, "none", Main, CurrentSupportGroup)]
        internal static bool CurrentSupportAvailable(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => CurrentSupport(in view, in metadata).Available;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-reject-reason", 1, DiagnosticValueKind.Int32, "category", Main, CurrentSupportGroup)]
        internal static int CurrentSupportRejectReason(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)CurrentSupport(in view, in metadata).RejectReason;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-heel-purpose", 1, DiagnosticValueKind.Int32, "category", Main, CurrentSupportGroup)]
        internal static int CurrentSupportHeelPurpose(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Heel(in view, in metadata).Purpose;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-heel-kind", 1, DiagnosticValueKind.Int32, "category", Main, CurrentSupportGroup)]
        internal static int CurrentSupportHeelKind(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Heel(in view, in metadata).Kind;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-heel-state", 1, DiagnosticValueKind.Int32, "category", Main, CurrentSupportGroup)]
        internal static int CurrentSupportHeelState(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Heel(in view, in metadata).State;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-heel-reject-reason", 1, DiagnosticValueKind.Int32, "category", Main, CurrentSupportGroup)]
        internal static int CurrentSupportHeelRejectReason(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Heel(in view, in metadata).RejectReason;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-heel-probe-position", 1, DiagnosticValueKind.Vector3, "metres", Main, CurrentSupportGroup)]
        internal static DiagnosticVector3 CurrentSupportHeelProbePosition(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Heel(in view, in metadata).ProbePosition);

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-heel-component-up", 1, DiagnosticValueKind.Vector3, "direction", Main, CurrentSupportGroup)]
        internal static DiagnosticVector3 CurrentSupportHeelComponentUp(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Heel(in view, in metadata).ComponentUp);

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-heel-origin", 1, DiagnosticValueKind.Vector3, "metres", Main, CurrentSupportGroup)]
        internal static DiagnosticVector3 CurrentSupportHeelOrigin(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Heel(in view, in metadata).Origin);

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-heel-direction", 1, DiagnosticValueKind.Vector3, "direction", Main, CurrentSupportGroup)]
        internal static DiagnosticVector3 CurrentSupportHeelDirection(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Heel(in view, in metadata).Direction);

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-heel-maximum-distance", 1, DiagnosticValueKind.Float32, "metres", Main, CurrentSupportGroup)]
        internal static float CurrentSupportHeelMaximumDistance(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Heel(in view, in metadata).MaximumDistance;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-heel-radius", 1, DiagnosticValueKind.Float32, "metres", Main, CurrentSupportGroup)]
        internal static float CurrentSupportHeelRadius(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Heel(in view, in metadata).Radius;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-heel-layer-mask", 1, DiagnosticValueKind.Int32, "bitmask", Main, CurrentSupportGroup)]
        internal static int CurrentSupportHeelLayerMask(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Heel(in view, in metadata).LayerMask;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-heel-minimum-ground-normal-dot", 1, DiagnosticValueKind.Float32, "unitless", Main, CurrentSupportGroup)]
        internal static float CurrentSupportHeelMinimumGroundNormalDot(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Heel(in view, in metadata).MinimumGroundNormalDot;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-heel-hit-capacity", 1, DiagnosticValueKind.Int32, "count", Main, CurrentSupportGroup)]
        internal static int CurrentSupportHeelHitCapacity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Heel(in view, in metadata).HitCapacity;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-heel-candidate-count", 1, DiagnosticValueKind.Int32, "count", Main, CurrentSupportGroup)]
        internal static int CurrentSupportHeelCandidateCount(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Heel(in view, in metadata).CandidateCount;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-heel-surface-identity", 1, DiagnosticValueKind.Int32, "identity", Main, CurrentSupportGroup, AvailabilityFieldId = "character-foot-ik/main/current-support-heel-accepted")]
        internal static int CurrentSupportHeelSurfaceIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Heel(in view, in metadata).SurfaceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-heel-point", 1, DiagnosticValueKind.Vector3, "metres", Main, CurrentSupportGroup, AvailabilityFieldId = "character-foot-ik/main/current-support-heel-accepted")]
        internal static DiagnosticVector3 CurrentSupportHeelPoint(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Heel(in view, in metadata).Point);

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-heel-normal", 1, DiagnosticValueKind.Vector3, "direction", Main, CurrentSupportGroup, AvailabilityFieldId = "character-foot-ik/main/current-support-heel-accepted")]
        internal static DiagnosticVector3 CurrentSupportHeelNormal(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Heel(in view, in metadata).Normal);

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-heel-distance", 1, DiagnosticValueKind.Float32, "metres", Main, CurrentSupportGroup, AvailabilityFieldId = "character-foot-ik/main/current-support-heel-accepted")]
        internal static float CurrentSupportHeelDistance(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Heel(in view, in metadata).Distance;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-heel-world-revision", 1, DiagnosticValueKind.UInt64, "identity", Main, CurrentSupportGroup)]
        internal static ulong CurrentSupportHeelWorldRevision(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Heel(in view, in metadata).WorldRevision;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-heel-sphere-cast-executed", 1, DiagnosticValueKind.Boolean, "none", Main, CurrentSupportGroup)]
        internal static bool CurrentSupportHeelSphereCastExecuted(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Heel(in view, in metadata).SphereCastExecuted;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-heel-accepted", 1, DiagnosticValueKind.Boolean, "none", Main, CurrentSupportGroup)]
        internal static bool CurrentSupportHeelAccepted(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Heel(in view, in metadata).Accepted;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-toe-purpose", 1, DiagnosticValueKind.Int32, "category", Main, CurrentSupportGroup)]
        internal static int CurrentSupportToePurpose(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Toe(in view, in metadata).Purpose;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-toe-kind", 1, DiagnosticValueKind.Int32, "category", Main, CurrentSupportGroup)]
        internal static int CurrentSupportToeKind(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Toe(in view, in metadata).Kind;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-toe-state", 1, DiagnosticValueKind.Int32, "category", Main, CurrentSupportGroup)]
        internal static int CurrentSupportToeState(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Toe(in view, in metadata).State;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-toe-reject-reason", 1, DiagnosticValueKind.Int32, "category", Main, CurrentSupportGroup)]
        internal static int CurrentSupportToeRejectReason(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Toe(in view, in metadata).RejectReason;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-toe-probe-position", 1, DiagnosticValueKind.Vector3, "metres", Main, CurrentSupportGroup)]
        internal static DiagnosticVector3 CurrentSupportToeProbePosition(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Toe(in view, in metadata).ProbePosition);

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-toe-component-up", 1, DiagnosticValueKind.Vector3, "direction", Main, CurrentSupportGroup)]
        internal static DiagnosticVector3 CurrentSupportToeComponentUp(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Toe(in view, in metadata).ComponentUp);

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-toe-origin", 1, DiagnosticValueKind.Vector3, "metres", Main, CurrentSupportGroup)]
        internal static DiagnosticVector3 CurrentSupportToeOrigin(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Toe(in view, in metadata).Origin);

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-toe-direction", 1, DiagnosticValueKind.Vector3, "direction", Main, CurrentSupportGroup)]
        internal static DiagnosticVector3 CurrentSupportToeDirection(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Toe(in view, in metadata).Direction);

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-toe-maximum-distance", 1, DiagnosticValueKind.Float32, "metres", Main, CurrentSupportGroup)]
        internal static float CurrentSupportToeMaximumDistance(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Toe(in view, in metadata).MaximumDistance;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-toe-radius", 1, DiagnosticValueKind.Float32, "metres", Main, CurrentSupportGroup)]
        internal static float CurrentSupportToeRadius(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Toe(in view, in metadata).Radius;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-toe-layer-mask", 1, DiagnosticValueKind.Int32, "bitmask", Main, CurrentSupportGroup)]
        internal static int CurrentSupportToeLayerMask(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Toe(in view, in metadata).LayerMask;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-toe-minimum-ground-normal-dot", 1, DiagnosticValueKind.Float32, "unitless", Main, CurrentSupportGroup)]
        internal static float CurrentSupportToeMinimumGroundNormalDot(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Toe(in view, in metadata).MinimumGroundNormalDot;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-toe-hit-capacity", 1, DiagnosticValueKind.Int32, "count", Main, CurrentSupportGroup)]
        internal static int CurrentSupportToeHitCapacity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Toe(in view, in metadata).HitCapacity;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-toe-candidate-count", 1, DiagnosticValueKind.Int32, "count", Main, CurrentSupportGroup)]
        internal static int CurrentSupportToeCandidateCount(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Toe(in view, in metadata).CandidateCount;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-toe-surface-identity", 1, DiagnosticValueKind.Int32, "identity", Main, CurrentSupportGroup, AvailabilityFieldId = "character-foot-ik/main/current-support-toe-accepted")]
        internal static int CurrentSupportToeSurfaceIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Toe(in view, in metadata).SurfaceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-toe-point", 1, DiagnosticValueKind.Vector3, "metres", Main, CurrentSupportGroup, AvailabilityFieldId = "character-foot-ik/main/current-support-toe-accepted")]
        internal static DiagnosticVector3 CurrentSupportToePoint(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Toe(in view, in metadata).Point);

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-toe-normal", 1, DiagnosticValueKind.Vector3, "direction", Main, CurrentSupportGroup, AvailabilityFieldId = "character-foot-ik/main/current-support-toe-accepted")]
        internal static DiagnosticVector3 CurrentSupportToeNormal(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Toe(in view, in metadata).Normal);

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-toe-distance", 1, DiagnosticValueKind.Float32, "metres", Main, CurrentSupportGroup, AvailabilityFieldId = "character-foot-ik/main/current-support-toe-accepted")]
        internal static float CurrentSupportToeDistance(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Toe(in view, in metadata).Distance;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-toe-world-revision", 1, DiagnosticValueKind.UInt64, "identity", Main, CurrentSupportGroup)]
        internal static ulong CurrentSupportToeWorldRevision(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Toe(in view, in metadata).WorldRevision;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-toe-sphere-cast-executed", 1, DiagnosticValueKind.Boolean, "none", Main, CurrentSupportGroup)]
        internal static bool CurrentSupportToeSphereCastExecuted(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Toe(in view, in metadata).SphereCastExecuted;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-toe-accepted", 1, DiagnosticValueKind.Boolean, "none", Main, CurrentSupportGroup)]
        internal static bool CurrentSupportToeAccepted(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Toe(in view, in metadata).Accepted;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-heel-required-displacement", 1, DiagnosticValueKind.Float32, "metres", Main, CurrentSupportGroup)]
        internal static float CurrentSupportHeelRequiredDisplacement(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => CurrentSupport(in view, in metadata).HeelRequiredDisplacement;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-toe-required-displacement", 1, DiagnosticValueKind.Float32, "metres", Main, CurrentSupportGroup)]
        internal static float CurrentSupportToeRequiredDisplacement(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => CurrentSupport(in view, in metadata).ToeRequiredDisplacement;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-selected-probe", 1, DiagnosticValueKind.Int32, "category", Main, CurrentSupportGroup)]
        internal static int CurrentSupportSelectedProbe(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)CurrentSupport(in view, in metadata).SelectedProbe;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-selection-reason", 1, DiagnosticValueKind.Int32, "category", Main, CurrentSupportGroup)]
        internal static int CurrentSupportSelectionReason(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)CurrentSupport(in view, in metadata).SelectionReason;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-selection-epsilon", 1, DiagnosticValueKind.Float32, "metres", Main, CurrentSupportGroup)]
        internal static float CurrentSupportSelectionEpsilon(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => CurrentSupport(in view, in metadata).SelectionEpsilon;

        [DiagnosticField(Capability, "character-foot-ik/main/current-support-selected-support-normal-before-normalization", 1, DiagnosticValueKind.Vector3, "direction", Main, CurrentSupportGroup)]
        internal static DiagnosticVector3 CurrentSupportSelectedSupportNormalBeforeNormalization(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(CurrentSupport(in view, in metadata).SelectedSupportNormalBeforeNormalization);

        static CharacterFootCurrentSupportDiagnostics CurrentSupport(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).CurrentSupport;

        static CharacterFootCurrentSupportProbeDiagnostics Heel(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            CurrentSupport(in view, in metadata).Heel;

        static CharacterFootCurrentSupportProbeDiagnostics Toe(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            CurrentSupport(in view, in metadata).Toe;
    }
}
