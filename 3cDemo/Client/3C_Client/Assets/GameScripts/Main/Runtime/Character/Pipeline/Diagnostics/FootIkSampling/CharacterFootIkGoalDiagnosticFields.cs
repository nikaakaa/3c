using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static partial class CharacterFootIkDiagnosticFields
    {
        const string GoalGroup = "goal";
        const string SelectedSupportTargetGroup = "selected-support-target";
        const string EncodedGoalAvailable =
            "character-foot-ik/main/foot-motion-encoded-goal-available";
        const string SelectedSupportTargetAvailable =
            "character-foot-ik/main/foot-motion-selected-support-target-available";

        [DiagnosticField(Capability, EncodedGoalAvailable, 1,
            DiagnosticValueKind.Boolean, "none", Main, GoalGroup)]
        internal static bool FootMotionEncodedGoalAvailable(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Goal(in view, in metadata).IsValid;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-encoded-goal-correction", 1,
            DiagnosticValueKind.Vector3, "metres", Main, GoalGroup,
            AvailabilityFieldId = EncodedGoalAvailable)]
        internal static DiagnosticVector3 FootMotionEncodedGoalCorrection(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata)
        {
            CharacterFullBodyIkGoal goal = Goal(in view, in metadata);
            return Vector3(
                goal.ComponentPosition -
                Foot(in view, in metadata).FootMotion.Core.OriginalAnkle);
        }

        [DiagnosticField(Capability, "character-foot-ik/main/final-goal-position", 1,
            DiagnosticValueKind.Vector3, "metres", Main, GoalGroup)]
        internal static DiagnosticVector3 FinalGoalPosition(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Goal(in view, in metadata).ComponentPosition);

        [DiagnosticField(Capability, "character-foot-ik/main/final-goal-rotation", 1,
            DiagnosticValueKind.Quaternion, "unitless", Main, GoalGroup)]
        internal static DiagnosticQuaternion FinalGoalRotation(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Quaternion(Goal(in view, in metadata).ComponentRotation);

        [DiagnosticField(Capability, "character-foot-ik/main/final-goal-position-weight", 1,
            DiagnosticValueKind.Float32, "unitless", Main, GoalGroup)]
        internal static float FinalGoalPositionWeight(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Goal(in view, in metadata).PositionWeight;

        [DiagnosticField(Capability, "character-foot-ik/main/final-goal-rotation-weight", 1,
            DiagnosticValueKind.Float32, "unitless", Main, GoalGroup)]
        internal static float FinalGoalRotationWeight(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Goal(in view, in metadata).RotationWeight;

        [DiagnosticField(Capability, "character-foot-ik/main/pelvis-position-weight", 1,
            DiagnosticValueKind.Float32, "unitless", Main, GoalGroup)]
        internal static float PelvisPositionWeight(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.LandingPrediction.PelvisGoal.PositionWeight;

        [DiagnosticField(Capability, "character-foot-ik/main/pelvis-rotation-weight", 1,
            DiagnosticValueKind.Float32, "unitless", Main, GoalGroup)]
        internal static float PelvisRotationWeight(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            view.LandingPrediction.PelvisGoal.RotationWeight;

        [DiagnosticField(Capability, SelectedSupportTargetAvailable, 1,
            DiagnosticValueKind.Boolean, "none", Main, SelectedSupportTargetGroup)]
        internal static bool FootMotionSelectedSupportTargetAvailable(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            SelectedSupportTarget(in view, in metadata).Available;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-selected-support-target-frame-sequence", 1,
            DiagnosticValueKind.UInt64, "frame", Main, SelectedSupportTargetGroup)]
        internal static ulong FootMotionSelectedSupportTargetFrameSequence(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            SelectedSupportTarget(in view, in metadata).FrameSequence;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-selected-support-target-completion-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, SelectedSupportTargetGroup)]
        internal static ulong FootMotionSelectedSupportTargetCompletionIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            SelectedSupportTarget(in view, in metadata).CompletionIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-selected-support-target-side", 1,
            DiagnosticValueKind.Int32, "category", Main, SelectedSupportTargetGroup)]
        internal static int FootMotionSelectedSupportTargetSide(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)SelectedSupportTarget(in view, in metadata).Side;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-selected-support-target-position", 1,
            DiagnosticValueKind.Vector3, "metres", Main, SelectedSupportTargetGroup,
            AvailabilityFieldId = SelectedSupportTargetAvailable)]
        internal static DiagnosticVector3 FootMotionSelectedSupportTargetPosition(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(SelectedSupportTarget(in view, in metadata).Position);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-selected-support-target-normal", 1,
            DiagnosticValueKind.Vector3, "direction", Main, SelectedSupportTargetGroup,
            AvailabilityFieldId = SelectedSupportTargetAvailable)]
        internal static DiagnosticVector3 FootMotionSelectedSupportTargetNormal(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(SelectedSupportTarget(in view, in metadata).SupportNormal);

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-selected-support-target-surface-identity", 1,
            DiagnosticValueKind.Int32, "identity", Main, SelectedSupportTargetGroup,
            AvailabilityFieldId = SelectedSupportTargetAvailable)]
        internal static int FootMotionSelectedSupportTargetSurfaceIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            SelectedSupportTarget(in view, in metadata).SurfaceIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-selected-support-target-world-revision", 1,
            DiagnosticValueKind.UInt64, "identity", Main, SelectedSupportTargetGroup,
            AvailabilityFieldId = SelectedSupportTargetAvailable)]
        internal static ulong FootMotionSelectedSupportTargetWorldRevision(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            SelectedSupportTarget(in view, in metadata).WorldRevision;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-selected-support-target-kind", 1,
            DiagnosticValueKind.Int32, "category", Main, SelectedSupportTargetGroup,
            AvailabilityFieldId = SelectedSupportTargetAvailable)]
        internal static int FootMotionSelectedSupportTargetKind(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)SelectedSupportTarget(in view, in metadata).Kind;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-selected-support-target-position-source", 1,
            DiagnosticValueKind.Int32, "category", Main, SelectedSupportTargetGroup,
            AvailabilityFieldId = SelectedSupportTargetAvailable)]
        internal static int FootMotionSelectedSupportTargetPositionSource(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)SelectedSupportTarget(in view, in metadata).PositionSource;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-selected-support-target-position-frame-sequence", 1,
            DiagnosticValueKind.UInt64, "frame", Main, SelectedSupportTargetGroup,
            AvailabilityFieldId = SelectedSupportTargetAvailable)]
        internal static ulong FootMotionSelectedSupportTargetPositionFrameSequence(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            SelectedSupportTarget(in view, in metadata).PositionFrameSequence;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-selected-support-target-position-completion-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, SelectedSupportTargetGroup,
            AvailabilityFieldId = SelectedSupportTargetAvailable)]
        internal static ulong FootMotionSelectedSupportTargetPositionCompletionIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            SelectedSupportTarget(in view, in metadata).PositionCompletionIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-selected-support-target-position-event-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, SelectedSupportTargetGroup,
            AvailabilityFieldId = SelectedSupportTargetAvailable)]
        internal static ulong FootMotionSelectedSupportTargetPositionEventIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            SelectedSupportTarget(in view, in metadata).PositionEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-selected-support-target-position-path-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, SelectedSupportTargetGroup,
            AvailabilityFieldId = SelectedSupportTargetAvailable)]
        internal static ulong FootMotionSelectedSupportTargetPositionPathIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            SelectedSupportTarget(in view, in metadata).PositionPathIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-selected-support-target-normal-source", 1,
            DiagnosticValueKind.Int32, "category", Main, SelectedSupportTargetGroup,
            AvailabilityFieldId = SelectedSupportTargetAvailable)]
        internal static int FootMotionSelectedSupportTargetNormalSource(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)SelectedSupportTarget(in view, in metadata).NormalSource;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-selected-support-target-normal-frame-sequence", 1,
            DiagnosticValueKind.UInt64, "frame", Main, SelectedSupportTargetGroup,
            AvailabilityFieldId = SelectedSupportTargetAvailable)]
        internal static ulong FootMotionSelectedSupportTargetNormalFrameSequence(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            SelectedSupportTarget(in view, in metadata).NormalFrameSequence;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-selected-support-target-normal-completion-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, SelectedSupportTargetGroup,
            AvailabilityFieldId = SelectedSupportTargetAvailable)]
        internal static ulong FootMotionSelectedSupportTargetNormalCompletionIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            SelectedSupportTarget(in view, in metadata).NormalCompletionIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/foot-motion-selected-support-target-normal-event-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, SelectedSupportTargetGroup,
            AvailabilityFieldId = SelectedSupportTargetAvailable)]
        internal static ulong FootMotionSelectedSupportTargetNormalEventIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            SelectedSupportTarget(in view, in metadata).NormalEventIdentity;

        static CharacterFullBodyIkGoal Goal(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).Goal;

        static CharacterFootSupportTargetDiagnostics SelectedSupportTarget(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).FootMotion.SelectedSupportTarget;
    }
}
