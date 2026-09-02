using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static partial class CharacterFootIkDiagnosticFields
    {
        const string ResolvedCoreGroup = "resolved-core";

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-frame-sequence", 1, DiagnosticValueKind.UInt64, "frame", Main, ResolvedCoreGroup)]
        internal static ulong ResolvedFrameSequence(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Core(in view, in metadata).FrameSequence;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-completion-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, ResolvedCoreGroup)]
        internal static ulong ResolvedCompletionIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Core(in view, in metadata).CompletionIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-rig-id", 1, DiagnosticValueKind.Identity, "identity", Main, ResolvedCoreGroup)]
        internal static string ResolvedRigId(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Core(in view, in metadata).RigId;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-rig-revision", 1, DiagnosticValueKind.Identity, "identity", Main, ResolvedCoreGroup)]
        internal static string ResolvedRigRevision(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Core(in view, in metadata).RigRevision;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-side", 1, DiagnosticValueKind.Int32, "category", Main, ResolvedCoreGroup)]
        internal static int ResolvedSide(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Core(in view, in metadata).Side;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-outcome", 1, DiagnosticValueKind.Int32, "category", Main, ResolvedCoreGroup)]
        internal static int ResolvedOutcome(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Core(in view, in metadata).Outcome;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-final-sole", 1, DiagnosticValueKind.Vector3, "metres", Main, ResolvedCoreGroup)]
        internal static DiagnosticVector3 ResolvedFinalSole(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Core(in view, in metadata).FinalSole);

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-effective-sole", 1, DiagnosticValueKind.Vector3, "metres", Main, ResolvedCoreGroup)]
        internal static DiagnosticVector3 ResolvedEffectiveSole(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Core(in view, in metadata).EffectiveSole);

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-goal-target-ankle", 1, DiagnosticValueKind.Vector3, "metres", Main, ResolvedCoreGroup)]
        internal static DiagnosticVector3 ResolvedGoalTargetAnkle(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Core(in view, in metadata).GoalTargetAnkle);

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-goal-target-rotation", 1, DiagnosticValueKind.Quaternion, "unitless", Main, ResolvedCoreGroup)]
        internal static DiagnosticQuaternion ResolvedGoalTargetRotation(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Quaternion(Core(in view, in metadata).GoalTargetRotation);

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-effective-ankle", 1, DiagnosticValueKind.Vector3, "metres", Main, ResolvedCoreGroup)]
        internal static DiagnosticVector3 ResolvedEffectiveAnkle(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Core(in view, in metadata).EffectiveAnkle);

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-effective-rotation", 1, DiagnosticValueKind.Quaternion, "unitless", Main, ResolvedCoreGroup)]
        internal static DiagnosticQuaternion ResolvedEffectiveRotation(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Quaternion(Core(in view, in metadata).EffectiveRotation);

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-effective-heel", 1, DiagnosticValueKind.Vector3, "metres", Main, ResolvedCoreGroup)]
        internal static DiagnosticVector3 ResolvedEffectiveHeel(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Core(in view, in metadata).EffectiveHeel);

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-effective-toe", 1, DiagnosticValueKind.Vector3, "metres", Main, ResolvedCoreGroup)]
        internal static DiagnosticVector3 ResolvedEffectiveToe(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Core(in view, in metadata).EffectiveToe);

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-effective-sole-from-contacts", 1, DiagnosticValueKind.Vector3, "metres", Main, ResolvedCoreGroup)]
        internal static DiagnosticVector3 ResolvedEffectiveSoleFromContacts(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Core(in view, in metadata).EffectiveSoleFromContacts);

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-source-sole-forward", 1, DiagnosticValueKind.Vector3, "direction", Main, ResolvedCoreGroup)]
        internal static DiagnosticVector3 ResolvedSourceSoleForward(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Core(in view, in metadata).SourceSoleForward);

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-source-sole-frame-local-rotation", 1, DiagnosticValueKind.Quaternion, "unitless", Main, ResolvedCoreGroup)]
        internal static DiagnosticQuaternion ResolvedSourceSoleFrameLocalRotation(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Quaternion(Core(in view, in metadata).SourceSoleFrameLocalRotation);

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-goal-target-correction", 1, DiagnosticValueKind.Vector3, "metres", Main, ResolvedCoreGroup)]
        internal static DiagnosticVector3 ResolvedGoalTargetCorrection(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Core(in view, in metadata).GoalTargetCorrection);

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-effective-sole-correction", 1, DiagnosticValueKind.Vector3, "metres", Main, ResolvedCoreGroup)]
        internal static DiagnosticVector3 ResolvedEffectiveSoleCorrection(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Core(in view, in metadata).EffectiveSoleCorrection);

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-position-weight", 1, DiagnosticValueKind.Float32, "unitless", Main, ResolvedCoreGroup)]
        internal static float ResolvedPositionWeight(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Core(in view, in metadata).PositionWeight;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-rotation-weight", 1, DiagnosticValueKind.Float32, "unitless", Main, ResolvedCoreGroup)]
        internal static float ResolvedRotationWeight(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Core(in view, in metadata).RotationWeight;

        static CharacterResolvedFootDiagnostics Resolved(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).Resolved;

        static CharacterResolvedFootCoreDiagnostics Core(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Resolved(in view, in metadata).Core;
    }
}
