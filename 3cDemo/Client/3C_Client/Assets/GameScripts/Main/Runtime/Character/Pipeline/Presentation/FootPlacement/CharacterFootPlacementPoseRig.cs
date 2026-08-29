using System;
using ThirdPersonCharacter.Pipeline.Animation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public sealed class CharacterFootPlacementPoseRig
    {
        enum ValidationContract : byte
        {
            PublishedRuntime = 1,
            CalibrationAuthoring = 2
        }

        public CharacterFootPlacementPoseRig(
            CharacterFootPlacementRigCalibration calibration,
            CharacterAnimationRigPayload rig,
            CharacterAnimationRigBinding binding,
            CharacterWorldAwarePresentationBinding world)
            : this(calibration, rig, binding, world, ValidationContract.PublishedRuntime)
        {
        }

        CharacterFootPlacementPoseRig(
            CharacterFootPlacementRigCalibration calibration,
            CharacterAnimationRigPayload rig,
            CharacterAnimationRigBinding binding,
            CharacterWorldAwarePresentationBinding world,
            ValidationContract validationContract)
        {
            Calibration = calibration ? calibration : throw new ArgumentNullException(nameof(calibration));
            Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            Binding = binding ? binding : throw new ArgumentNullException(nameof(binding));
            World = world ? world : throw new ArgumentNullException(nameof(world));
            if (validationContract == ValidationContract.PublishedRuntime)
                Calibration.RequireValid();
            else
                Calibration.RequireConfiguredForAuthoring();
            Rig.RequireValid();
            Binding.RequireValid(Rig);
            World.RequireValid();
            if (!string.Equals(Calibration.RigId, Rig.RigId, StringComparison.Ordinal) ||
                !string.Equals(Calibration.RigRevision, Rig.RigRevision, StringComparison.Ordinal))
                throw new InvalidOperationException("Foot Placement Calibration does not match the compiled Animation Rig.");
            if (Binding.Animator.transform == World.PresentationRoot ||
                !Binding.Animator.transform.IsChildOf(World.PresentationRoot))
                throw new InvalidOperationException("Animation Rig must belong to the World-Aware Presentation Root.");

            Pelvis = Bone(Rig.PelvisPhysicalBoneIndex);
            LeftHip = Bone(Rig.LeftLeg.HipPhysicalBoneIndex);
            LeftKnee = Bone(Rig.LeftLeg.KneePhysicalBoneIndex);
            LeftAnkle = Bone(Rig.LeftLeg.AnklePhysicalBoneIndex);
            LeftToe = Bone(Rig.LeftLeg.ToePhysicalBoneIndex);
            RightHip = Bone(Rig.RightLeg.HipPhysicalBoneIndex);
            RightKnee = Bone(Rig.RightLeg.KneePhysicalBoneIndex);
            RightAnkle = Bone(Rig.RightLeg.AnklePhysicalBoneIndex);
            RightToe = Bone(Rig.RightLeg.ToePhysicalBoneIndex);
            LeftLegLength = Rig.LeftLeg.LegLength;
            RightLegLength = Rig.RightLeg.LegLength;

            CharacterFootPlacementFootCalibration left = Calibration.Left;
            CharacterFootPlacementFootCalibration right = Calibration.Right;
            LeftHeelContactOffset = left.HeelContactLocalOffset;
            LeftToeContactOffset = left.ToeContactLocalOffset;
            RightHeelContactOffset = right.HeelContactLocalOffset;
            RightToeContactOffset = right.ToeContactLocalOffset;
            LeftSoleFrameLocalRotation = left.SoleFrameLocalRotation;
            RightSoleFrameLocalRotation = right.SoleFrameLocalRotation;
            LeftRearProbeExtension = left.RearProbeExtension;
            LeftLateralProbeExtent = left.LateralProbeExtent;
            LeftToeProbeExtension = left.ToeProbeExtension;
            RightRearProbeExtension = right.RearProbeExtension;
            RightLateralProbeExtent = right.LateralProbeExtent;
            RightToeProbeExtension = right.ToeProbeExtension;
        }

        public static CharacterFootPlacementPoseRig CreateCalibrationAuthoringRig(
            CharacterFootPlacementRigCalibration calibration,
            CharacterAnimationRigDefinition rig,
            CharacterAnimationRigBinding binding,
            CharacterWorldAwarePresentationBinding world)
        {
            if (!calibration)
                throw new ArgumentNullException(nameof(calibration));
            if (!rig)
                throw new ArgumentNullException(nameof(rig));
            calibration.RequireRigForAuthoring(rig);
            return new CharacterFootPlacementPoseRig(
                calibration,
                new CharacterAnimationRigPayload(rig),
                binding,
                world,
                ValidationContract.CalibrationAuthoring);
        }

        public CharacterFootPlacementRigCalibration Calibration { get; }
        public CharacterAnimationRigPayload Rig { get; }
        public CharacterAnimationRigBinding Binding { get; }
        public CharacterWorldAwarePresentationBinding World { get; }
        public CharacterFootPlacementRigCalibrationId CalibrationId => Calibration.CalibrationId;
        public int CalibrationSchemaVersion => Calibration.SchemaVersion;
        public string CalibrationRevision => Calibration.ContentRevision;
        public Transform VisualRoot => World.PresentationRoot;
        public Transform PoseRoot => Binding.Animator.transform;
        public Transform SelfColliderRoot => World.SelfColliderRoot;
        public int CharacterLayer => World.CharacterLayer;
        public Transform Pelvis { get; }
        public Transform LeftHip { get; }
        public Transform LeftKnee { get; }
        public Transform LeftAnkle { get; }
        public Transform LeftToe { get; }
        public Transform RightHip { get; }
        public Transform RightKnee { get; }
        public Transform RightAnkle { get; }
        public Transform RightToe { get; }
        public Vector3 LeftHeelContactOffset { get; }
        public Vector3 LeftToeContactOffset { get; }
        public Vector3 RightHeelContactOffset { get; }
        public Vector3 RightToeContactOffset { get; }
        public Quaternion LeftSoleFrameLocalRotation { get; }
        public Quaternion RightSoleFrameLocalRotation { get; }
        public float LeftRearProbeExtension { get; }
        public float LeftLateralProbeExtent { get; }
        public float LeftToeProbeExtension { get; }
        public float RightRearProbeExtension { get; }
        public float RightLateralProbeExtent { get; }
        public float RightToeProbeExtension { get; }
        public float LeftLegLength { get; }
        public float RightLegLength { get; }

        internal CharacterFootPlacementAnimatedPose CaptureAnimatedPose(
            ulong renderFrame,
            NativeSlice<AnimationLocalBonePose> componentPoses)
        {
            if (renderFrame == 0 ||
                componentPoses.Length != Rig.PoseBoneCount)
                throw new ArgumentOutOfRangeException(nameof(renderFrame));
            var pose = new CharacterFootPlacementAnimatedPose(
                renderFrame,
                componentPoses[Rig.PelvisPhysicalBoneIndex].Position,
                CaptureFoot(
                    PoseRoot,
                    componentPoses[Rig.LeftLeg.HipPhysicalBoneIndex],
                    componentPoses[Rig.LeftLeg.KneePhysicalBoneIndex],
                    componentPoses[Rig.LeftLeg.AnklePhysicalBoneIndex],
                    componentPoses[Rig.LeftLeg.ToePhysicalBoneIndex],
                    LeftHeelContactOffset,
                    LeftToeContactOffset,
                    LeftSoleFrameLocalRotation,
                    LeftRearProbeExtension,
                    LeftLateralProbeExtent,
                    LeftToeProbeExtension),
                CaptureFoot(
                    PoseRoot,
                    componentPoses[Rig.RightLeg.HipPhysicalBoneIndex],
                    componentPoses[Rig.RightLeg.KneePhysicalBoneIndex],
                    componentPoses[Rig.RightLeg.AnklePhysicalBoneIndex],
                    componentPoses[Rig.RightLeg.ToePhysicalBoneIndex],
                    RightHeelContactOffset,
                    RightToeContactOffset,
                    RightSoleFrameLocalRotation,
                    RightRearProbeExtension,
                    RightLateralProbeExtent,
                    RightToeProbeExtension));
            RequireFinite(pose.Left.HipPosition);
            RequireFinite(pose.Left.KneePosition);
            RequireFinite(pose.Left.AnklePosition);
            RequireFinite(pose.Left.ToePosition);
            RequireFinite(pose.Left.HeelPosition);
            RequireFinite(pose.Right.HipPosition);
            RequireFinite(pose.Right.KneePosition);
            RequireFinite(pose.Right.AnklePosition);
            RequireFinite(pose.Right.ToePosition);
            RequireFinite(pose.Right.HeelPosition);
            return pose;
        }

        public bool IsSelfCollider(Collider collider) => World.IsSelfCollider(collider);

        public void RequireValid()
        {
            Calibration.RequireValid();
            Rig.RequireValid();
            Binding.RequireValid(Rig);
            World.RequireValid();
            if (!string.Equals(Calibration.RigId, Rig.RigId, StringComparison.Ordinal) ||
                !string.Equals(Calibration.RigRevision, Rig.RigRevision, StringComparison.Ordinal))
                throw new InvalidOperationException("Foot Placement Calibration Rig identity is stale.");
        }

        Transform Bone(int index)
        {
            if ((uint)index >= (uint)Binding.PhysicalBones.Count || !Binding.PhysicalBones[index])
                throw new InvalidOperationException($"Foot Placement Rig Bone #{index} is unavailable.");
            return Binding.PhysicalBones[index];
        }

        static CharacterFootPlacementAnimatedFootPose CaptureFoot(
            Transform poseRoot,
            AnimationLocalBonePose hip,
            AnimationLocalBonePose knee,
            AnimationLocalBonePose ankle,
            AnimationLocalBonePose toe,
            Vector3 heelContactOffset,
            Vector3 toeContactOffset,
            Quaternion soleFrameLocalRotation,
            float rearProbeExtension,
            float lateralProbeExtent,
            float toeProbeExtension)
        {
            if (!hip.IsValid || !knee.IsValid ||
                !ankle.IsValid || !toe.IsValid)
            {
                throw new InvalidOperationException(
                    "Foot Placement upstream Component Pose is invalid.");
            }
            Quaternion rootRotation = poseRoot.rotation;
            Quaternion ankleRotation = rootRotation * ankle.Rotation;
            Quaternion toeRotation = rootRotation * toe.Rotation;
            Quaternion semanticRotation =
                ankleRotation * soleFrameLocalRotation;
            return new CharacterFootPlacementAnimatedFootPose(
                poseRoot.TransformPoint(hip.Position),
                poseRoot.TransformPoint(knee.Position),
                poseRoot.TransformPoint(ankle.Position),
                ankleRotation,
                TransformPoint(
                    poseRoot,
                    in toe,
                    toeContactOffset),
                toeRotation,
                TransformPoint(
                    poseRoot,
                    in ankle,
                    heelContactOffset),
                semanticRotation * Vector3.forward,
                semanticRotation * Vector3.up,
                semanticRotation * Vector3.right,
                semanticRotation,
                soleFrameLocalRotation,
                rearProbeExtension,
                lateralProbeExtent,
                toeProbeExtension);
        }

        static Vector3 TransformPoint(
            Transform poseRoot,
            in AnimationLocalBonePose bone,
            Vector3 localOffset) =>
            poseRoot.TransformPoint(
                bone.Position +
                bone.Rotation * Vector3.Scale(
                    bone.Scale,
                    localOffset));

        static void RequireFinite(Vector3 value)
        {
            if (!IsFinite(value))
                throw new InvalidOperationException("Foot Placement Rig pose is not finite.");
        }

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
