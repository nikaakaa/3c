using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static partial class CharacterFootIkDiagnosticFields
    {
        const string PelvisInputGroup = "pelvis-input";

        [DiagnosticField(Capability, "character-foot-ik/main/stride-state", 1, DiagnosticValueKind.Int32, "category", Main, PelvisInputGroup)]
        internal static int StrideState(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Stride(in view).Core.State;

        [DiagnosticField(Capability, "character-foot-ik/main/stride-reject-reason", 1, DiagnosticValueKind.Int32, "category", Main, PelvisInputGroup)]
        internal static int StrideRejectReason(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Stride(in view).Core.RejectReason;

        [DiagnosticField(Capability, "character-foot-ik/main/stride-support-side", 1, DiagnosticValueKind.Int32, "category", Main, PelvisInputGroup)]
        internal static int StrideSupportSide(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Stride(in view).Core.SupportSide;

        [DiagnosticField(Capability, "character-foot-ik/main/stride-swing-side", 1, DiagnosticValueKind.Int32, "category", Main, PelvisInputGroup)]
        internal static int StrideSwingSide(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Stride(in view).Core.SwingSide;

        [DiagnosticField(Capability, "character-foot-ik/main/stride-progress", 1, DiagnosticValueKind.Float32, "unitless", Main, PelvisInputGroup)]
        internal static float StrideProgress(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Stride(in view).Core.Progress;

        [DiagnosticField(Capability, "character-foot-ik/main/stride-slope", 1, DiagnosticValueKind.Int32, "category", Main, PelvisInputGroup)]
        internal static int StrideSlope(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Stride(in view).Core.Slope;

        [DiagnosticField(Capability, "character-foot-ik/main/stride-start", 1, DiagnosticValueKind.Vector3, "metres", Main, PelvisInputGroup)]
        internal static DiagnosticVector3 StrideStart(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Stride(in view).Core.StrideStart);

        [DiagnosticField(Capability, "character-foot-ik/main/stride-end", 1, DiagnosticValueKind.Vector3, "metres", Main, PelvisInputGroup)]
        internal static DiagnosticVector3 StrideEnd(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Stride(in view).Core.StrideEnd);

        [DiagnosticField(Capability, "character-foot-ik/main/stride-sampled-ground", 1, DiagnosticValueKind.Vector3, "metres", Main, PelvisInputGroup)]
        internal static DiagnosticVector3 StrideSampledGround(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Stride(in view).Core.SampledGround);

        [DiagnosticField(Capability, "character-foot-ik/main/pelvis-pose-input-available", 1, DiagnosticValueKind.Boolean, "none", Main, PelvisInputGroup)]
        internal static bool PelvisPoseInputAvailable(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Stride(in view).Observation.PoseInputAvailable;

        [DiagnosticField(Capability, "character-foot-ik/main/stride-pose-root-position", 1, DiagnosticValueKind.Vector3, "metres", Main, PelvisInputGroup)]
        internal static DiagnosticVector3 StridePoseRootPosition(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Stride(in view).Observation.PoseRootPosition);

        [DiagnosticField(Capability, "character-foot-ik/main/stride-animated-pelvis", 1, DiagnosticValueKind.Vector3, "metres", Main, PelvisInputGroup)]
        internal static DiagnosticVector3 StrideAnimatedPelvis(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Stride(in view).Observation.AnimatedPelvis);

        [DiagnosticField(Capability, "character-foot-ik/main/stride-animated-pelvis-component-position", 1, DiagnosticValueKind.Vector3, "metres", Main, PelvisInputGroup)]
        internal static DiagnosticVector3 StrideAnimatedPelvisComponentPosition(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Stride(in view).Observation.AnimatedPelvisComponentPosition);

        [DiagnosticField(Capability, "character-foot-ik/main/pelvis-height-target-available", 1, DiagnosticValueKind.Boolean, "none", Main, PelvisInputGroup)]
        internal static bool PelvisHeightTargetAvailable(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Stride(in view).HeightTarget.Available;

        [DiagnosticField(Capability, "character-foot-ik/main/pelvis-height-target-component-up", 1, DiagnosticValueKind.Vector3, "direction", Main, PelvisInputGroup)]
        internal static DiagnosticVector3 PelvisHeightTargetComponentUp(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Stride(in view).HeightTarget.ComponentUp);

        [DiagnosticField(Capability, "character-foot-ik/main/pelvis-height-target-left-animated-sole", 1, DiagnosticValueKind.Vector3, "metres", Main, PelvisInputGroup)]
        internal static DiagnosticVector3 PelvisHeightTargetLeftAnimatedSole(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Stride(in view).HeightTarget.LeftAnimatedSole);

        [DiagnosticField(Capability, "character-foot-ik/main/pelvis-height-target-right-animated-sole", 1, DiagnosticValueKind.Vector3, "metres", Main, PelvisInputGroup)]
        internal static DiagnosticVector3 PelvisHeightTargetRightAnimatedSole(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Stride(in view).HeightTarget.RightAnimatedSole);

        [DiagnosticField(Capability, "character-foot-ik/main/pelvis-height-target-left-target-sole", 1, DiagnosticValueKind.Vector3, "metres", Main, PelvisInputGroup)]
        internal static DiagnosticVector3 PelvisHeightTargetLeftTargetSole(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Stride(in view).HeightTarget.LeftTargetSole);

        [DiagnosticField(Capability, "character-foot-ik/main/pelvis-height-target-right-target-sole", 1, DiagnosticValueKind.Vector3, "metres", Main, PelvisInputGroup)]
        internal static DiagnosticVector3 PelvisHeightTargetRightTargetSole(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Stride(in view).HeightTarget.RightTargetSole);

        [DiagnosticField(Capability, "character-foot-ik/main/pelvis-height-target-animated-minimum-along-up", 1, DiagnosticValueKind.Float32, "metres", Main, PelvisInputGroup)]
        internal static float PelvisHeightTargetAnimatedMinimumAlongUp(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Stride(in view).HeightTarget.AnimatedMinimumAlongUp;

        [DiagnosticField(Capability, "character-foot-ik/main/pelvis-height-target-minimum-along-up", 1, DiagnosticValueKind.Float32, "metres", Main, PelvisInputGroup)]
        internal static float PelvisHeightTargetMinimumAlongUp(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Stride(in view).HeightTarget.MinimumAlongUp;

        [DiagnosticField(Capability, "character-foot-ik/main/pelvis-requested-offset-along-up", 1, DiagnosticValueKind.Float32, "metres", Main, PelvisInputGroup)]
        internal static float PelvisRequestedOffsetAlongUp(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Stride(in view).HeightTarget.RequestedOffsetAlongUp;

        static CharacterFootStrideHipsDiagnostics Stride(
            in CharacterFootIkCommittedCaptureViewLease view) =>
            view.LandingPrediction.StrideHips;
    }
}
