using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    [DiagnosticGroup("physical")]
    public readonly struct CharacterPhysicalFootPose
    {
        internal CharacterPhysicalFootPose(
            Vector3 ankleWorldPosition,
            Quaternion ankleWorldRotation)
        {
            IsAvailable = true;
            AnkleWorldPosition = ankleWorldPosition;
            AnkleWorldRotation = ankleWorldRotation;
        }

        [DiagnosticField]
        [DiagnosticKey("physical-write-available")]
        public bool IsAvailable { get; }

        [DiagnosticField]
        [DiagnosticKey("physical-ankle-position")]
        [DiagnosticAvailability(
            DiagnosticAvailabilityReference.Member,
            nameof(IsAvailable))]
        public Vector3 AnkleWorldPosition { get; }

        [DiagnosticField]
        [DiagnosticKey("physical-ankle-rotation")]
        [DiagnosticAvailability(
            DiagnosticAvailabilityReference.Member,
            nameof(IsAvailable))]
        public Quaternion AnkleWorldRotation { get; }
    }

    [DiagnosticGroup("physical")]
    public readonly struct CharacterPhysicalBodyPose
    {
        internal CharacterPhysicalBodyPose(
            Vector3 poseRootWorldPosition,
            Quaternion poseRootWorldRotation,
            Vector3 pelvisWorldPosition)
        {
            IsAvailable = true;
            PoseRootWorldPosition = poseRootWorldPosition;
            PoseRootWorldRotation = poseRootWorldRotation;
            PelvisWorldPosition = pelvisWorldPosition;
        }

        [DiagnosticField]
        [DiagnosticKey("physical-body-available")]
        public bool IsAvailable { get; }

        [DiagnosticField]
        [DiagnosticKey("physical-pose-root-position")]
        [DiagnosticAvailability(
            DiagnosticAvailabilityReference.Member,
            nameof(IsAvailable))]
        public Vector3 PoseRootWorldPosition { get; }

        [DiagnosticField]
        [DiagnosticKey("physical-pose-root-rotation")]
        [DiagnosticAvailability(
            DiagnosticAvailabilityReference.Member,
            nameof(IsAvailable))]
        public Quaternion PoseRootWorldRotation { get; }

        [DiagnosticField]
        [DiagnosticKey("physical-pelvis-position")]
        [DiagnosticAvailability(
            DiagnosticAvailabilityReference.Member,
            nameof(IsAvailable))]
        public Vector3 PelvisWorldPosition { get; }
    }

    internal readonly struct CharacterFootIkPhysicalCapture
    {
        internal CharacterFootIkPhysicalCapture(
            Vector3 logicRootWorldPosition,
            Quaternion logicRootWorldRotation,
            Vector3 visualRootLocalPosition,
            Quaternion visualRootLocalRotation,
            Vector3 visualRootWorldPosition,
            Quaternion visualRootWorldRotation,
            Vector3 poseRootLocalPosition,
            Quaternion poseRootLocalRotation,
            Vector3 poseRootWorldPosition,
            Quaternion poseRootWorldRotation,
            Vector3 pelvisWorldPosition,
            Vector3 leftAnkleWorldPosition,
            Quaternion leftAnkleWorldRotation,
            Vector3 rightAnkleWorldPosition,
            Quaternion rightAnkleWorldRotation)
        {
            LogicRootWorldPosition = logicRootWorldPosition;
            LogicRootWorldRotation = logicRootWorldRotation;
            VisualRootLocalPosition = visualRootLocalPosition;
            VisualRootLocalRotation = visualRootLocalRotation;
            VisualRootWorldPosition = visualRootWorldPosition;
            VisualRootWorldRotation = visualRootWorldRotation;
            PoseRootLocalPosition = poseRootLocalPosition;
            PoseRootLocalRotation = poseRootLocalRotation;
            PoseRootWorldPosition = poseRootWorldPosition;
            PoseRootWorldRotation = poseRootWorldRotation;
            Body = new CharacterPhysicalBodyPose(
                poseRootWorldPosition,
                poseRootWorldRotation,
                pelvisWorldPosition);
            Left = new CharacterPhysicalFootPose(
                leftAnkleWorldPosition,
                leftAnkleWorldRotation);
            Right = new CharacterPhysicalFootPose(
                rightAnkleWorldPosition,
                rightAnkleWorldRotation);
        }

        internal Vector3 LogicRootWorldPosition { get; }
        internal Quaternion LogicRootWorldRotation { get; }
        internal Vector3 VisualRootLocalPosition { get; }
        internal Quaternion VisualRootLocalRotation { get; }
        internal Vector3 VisualRootWorldPosition { get; }
        internal Quaternion VisualRootWorldRotation { get; }
        internal Vector3 PoseRootLocalPosition { get; }
        internal Quaternion PoseRootLocalRotation { get; }
        internal Vector3 PoseRootWorldPosition { get; }
        internal Quaternion PoseRootWorldRotation { get; }
        internal CharacterPhysicalBodyPose Body { get; }
        internal CharacterPhysicalFootPose Left { get; }
        internal CharacterPhysicalFootPose Right { get; }
        internal Vector3 LeftAnkleWorldPosition => Left.AnkleWorldPosition;
        internal Quaternion LeftAnkleWorldRotation => Left.AnkleWorldRotation;
        internal Vector3 RightAnkleWorldPosition => Right.AnkleWorldPosition;
        internal Quaternion RightAnkleWorldRotation => Right.AnkleWorldRotation;
        internal bool IsAvailable =>
            IsFinite(LogicRootWorldPosition) &&
            IsFinite(LogicRootWorldRotation) &&
            IsFinite(VisualRootLocalPosition) &&
            IsFinite(VisualRootLocalRotation) &&
            IsFinite(VisualRootWorldPosition) &&
            IsFinite(VisualRootWorldRotation) &&
            IsFinite(PoseRootLocalPosition) &&
            IsFinite(PoseRootLocalRotation) &&
            IsFinite(PoseRootWorldPosition) &&
            IsFinite(PoseRootWorldRotation) &&
            Body.IsAvailable &&
            Left.IsAvailable &&
            Right.IsAvailable &&
            IsFinite(LeftAnkleWorldPosition) &&
            IsFinite(LeftAnkleWorldRotation) &&
            IsFinite(RightAnkleWorldPosition) &&
            IsFinite(RightAnkleWorldRotation);

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) &&
            float.IsFinite(value.y) &&
            float.IsFinite(value.z);

        static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.x) &&
            float.IsFinite(value.y) &&
            float.IsFinite(value.z) &&
            float.IsFinite(value.w) &&
            Quaternion.Dot(value, value) > 0.000001f;
    }

    internal readonly struct AnimationPhysicalBoneWriteDiagnostics
    {
        internal AnimationPhysicalBoneWriteDiagnostics(
            ulong completionIdentity,
            Vector3 leftAnkleComponentPosition,
            Quaternion leftAnkleComponentRotation,
            Vector3 rightAnkleComponentPosition,
            Quaternion rightAnkleComponentRotation,
            Vector3 pelvisComponentPosition,
            Vector3 pelvisWorldPosition,
            in CharacterFootIkPhysicalCapture footIkCapture)
        {
            CompletionIdentity = completionIdentity;
            LeftAnkleComponentPosition = leftAnkleComponentPosition;
            LeftAnkleComponentRotation = leftAnkleComponentRotation;
            RightAnkleComponentPosition = rightAnkleComponentPosition;
            RightAnkleComponentRotation = rightAnkleComponentRotation;
            PelvisComponentPosition = pelvisComponentPosition;
            PelvisWorldPosition = pelvisWorldPosition;
            FootIkCapture = footIkCapture;
        }

        internal ulong CompletionIdentity { get; }
        internal Vector3 LeftAnkleComponentPosition { get; }
        internal Quaternion LeftAnkleComponentRotation { get; }
        internal Vector3 RightAnkleComponentPosition { get; }
        internal Quaternion RightAnkleComponentRotation { get; }
        internal Vector3 PelvisComponentPosition { get; }
        internal Vector3 PelvisWorldPosition { get; }
        internal CharacterFootIkPhysicalCapture FootIkCapture { get; }
        internal bool IsAvailable =>
            CompletionIdentity != 0 &&
            IsFinite(LeftAnkleComponentPosition) &&
            IsFinite(LeftAnkleComponentRotation) &&
            IsFinite(RightAnkleComponentPosition) &&
            IsFinite(RightAnkleComponentRotation) &&
            IsFinite(PelvisComponentPosition) &&
            IsFinite(PelvisWorldPosition);

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) &&
            float.IsFinite(value.y) &&
            float.IsFinite(value.z);

        static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.x) &&
            float.IsFinite(value.y) &&
            float.IsFinite(value.z) &&
            float.IsFinite(value.w) &&
            Quaternion.Dot(value, value) > 0.000001f;
    }

    public readonly struct AnimationReleasedPoseSourceSnapshot
    {
        internal AnimationReleasedPoseSourceSnapshot(
            PoseNodeId poseNodeId,
            AnimationPoseSourceId sourceId,
            ulong completionIdentity)
        {
            PoseNodeId = poseNodeId;
            SourceId = sourceId;
            CompletionIdentity = completionIdentity;
        }

        public PoseNodeId PoseNodeId { get; }
        public AnimationPoseSourceId SourceId { get; }
        public ulong CompletionIdentity { get; }
    }
}
