using System;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct CharacterPoseBoneIkGoalDescriptor
    {
        public CharacterPoseBoneIkGoalDescriptor(
            CharacterFullBodyIkEffectorSlot effectorSlot,
            int targetPoseBoneIndex,
            Vector3 localPositionOffset,
            Quaternion localRotationOffset,
            float positionWeight,
            float rotationWeight)
        {
            EffectorSlot = effectorSlot;
            TargetPoseBoneIndex = targetPoseBoneIndex;
            LocalPositionOffset = localPositionOffset;
            LocalRotationOffset = localRotationOffset;
            PositionWeight = positionWeight;
            RotationWeight = rotationWeight;
            if (!IsValid)
                throw new ArgumentException("Pose Bone IK Goal descriptor is invalid.");
        }

        public CharacterFullBodyIkEffectorSlot EffectorSlot { get; }
        public int TargetPoseBoneIndex { get; }
        public Vector3 LocalPositionOffset { get; }
        public Quaternion LocalRotationOffset { get; }
        public float PositionWeight { get; }
        public float RotationWeight { get; }

        public bool IsValid =>
            EffectorSlot >= CharacterFullBodyIkEffectorSlot.Body &&
            EffectorSlot <= CharacterFullBodyIkEffectorSlot.RightFoot &&
            TargetPoseBoneIndex >= 0 &&
            CharacterPoseConstraintMath.IsFinite(LocalPositionOffset) &&
            CharacterPoseConstraintMath.IsFinite(LocalRotationOffset) &&
            Quaternion.Dot(LocalRotationOffset, LocalRotationOffset) > CharacterPoseConstraintMath.Epsilon &&
            IsWeight(PositionWeight) &&
            IsWeight(RotationWeight);

        static bool IsWeight(float value) =>
            float.IsFinite(value) && value >= 0f && value <= 1f;
    }

    public static class CharacterPoseBoneIkGoalSource
    {
        public static CharacterFullBodyIkGoalContributionHeader Produce(
            NativeSlice<AnimationLocalBonePose> componentPose,
            NativeSlice<CharacterPoseBoneIkGoalDescriptor> descriptors,
            NativeSlice<CharacterFullBodyIkGoal> goalOutput,
            int goalWorkspaceOffset,
            ulong frameSequence,
            ulong completionIdentity,
            FixedString64Bytes rigId,
            FixedString64Bytes rigRevision,
            int producerOperationIndex,
            int producerCallSiteIndex)
        {
            if (descriptors.Length == 0 ||
                descriptors.Length > CharacterFullBodyIkGoalSetHeader.MaximumGoalCount ||
                goalOutput.Length != descriptors.Length ||
                goalWorkspaceOffset < 0)
            {
                throw new ArgumentException("Pose Bone IK Goal workspace is invalid.");
            }
            ushort occupiedSlots = 0;
            for (int i = 0; i < descriptors.Length; i++)
            {
                CharacterPoseBoneIkGoalDescriptor descriptor = descriptors[i];
                if (!descriptor.IsValid || descriptor.TargetPoseBoneIndex >= componentPose.Length)
                    throw new ArgumentException($"Pose Bone IK Goal descriptor #{i} is invalid.", nameof(descriptors));
                int slotBit = 1 << ((int)descriptor.EffectorSlot - 1);
                if ((occupiedSlots & slotBit) != 0)
                    throw new ArgumentException($"Pose Bone IK Goal descriptor #{i} duplicates an Effector Slot.", nameof(descriptors));
                occupiedSlots = (ushort)(occupiedSlots | slotBit);
                AnimationLocalBonePose target = componentPose[descriptor.TargetPoseBoneIndex];
                if (!target.IsValid)
                    throw new ArgumentException($"Pose Bone IK Goal target Pose Bone #{descriptor.TargetPoseBoneIndex} is invalid.", nameof(componentPose));
                Vector3 position = target.Position +
                                   target.Rotation * Vector3.Scale(target.Scale, descriptor.LocalPositionOffset);
                Quaternion rotation = (target.Rotation * descriptor.LocalRotationOffset).normalized;
                goalOutput[i] = new CharacterFullBodyIkGoal(
                    descriptor.EffectorSlot,
                    position,
                    rotation,
                    descriptor.PositionWeight,
                    descriptor.RotationWeight,
                    CharacterFullBodyIkGoalApplication.AbsoluteEffectorTarget,
                    CharacterFullBodyIkGoalSourceKind.PoseBone,
                    i);
            }
            return new CharacterFullBodyIkGoalContributionHeader(
                frameSequence,
                completionIdentity,
                rigId,
                rigRevision,
                producerOperationIndex,
                producerCallSiteIndex,
                goalWorkspaceOffset,
                descriptors.Length,
                CharacterFullBodyIkGoalContributionAvailability.Ready);
        }
    }
}
