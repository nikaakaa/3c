using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public static class CharacterVirtualBonePoseDerivation
    {
        public static CharacterVirtualBonePoseResult Derive(
            CharacterPoseBoneCounts counts,
            NativeSlice<AnimationLocalBonePose> physicalLocalPoses,
            NativeArray<int> physicalParentIndices,
            NativeArray<CharacterVirtualBoneDescriptor> virtualBones,
            NativeArray<CharacterComponentBonePose> componentScratch,
            NativeSlice<AnimationLocalBonePose> outputPose)
        {
            if (!counts.IsValid ||
                physicalLocalPoses.Length != counts.PhysicalBoneCount ||
                physicalParentIndices.Length != counts.PhysicalBoneCount ||
                virtualBones.Length != counts.VirtualBoneCount ||
                componentScratch.Length < counts.PhysicalBoneCount ||
                outputPose.Length != counts.PoseBoneCount)
            {
                return CharacterVirtualBonePoseResult.Fail(CharacterVirtualBonePoseFailure.InvalidCounts);
            }

            int rootCount = 0;
            for (int physicalIndex = 0; physicalIndex < counts.PhysicalBoneCount; physicalIndex++)
            {
                int parentIndex = physicalParentIndices[physicalIndex];
                if (parentIndex < -1 || parentIndex >= physicalIndex)
                    return CharacterVirtualBonePoseResult.Fail(CharacterVirtualBonePoseFailure.InvalidPhysicalHierarchy);
                if (parentIndex < 0)
                    rootCount++;
                AnimationLocalBonePose local = physicalLocalPoses[physicalIndex];
                if (!local.IsValid)
                    return CharacterVirtualBonePoseResult.Fail(CharacterVirtualBonePoseFailure.InvalidPhysicalPose);
                if (!CharacterPoseConstraintMath.TryCreateComponent(
                        local,
                        parentIndex,
                        componentScratch,
                        out CharacterComponentBonePose component))
                {
                    return CharacterVirtualBonePoseResult.Fail(CharacterVirtualBonePoseFailure.InvalidPhysicalPose);
                }
                componentScratch[physicalIndex] = component;
                outputPose[physicalIndex] = local;
            }
            if (rootCount != 1)
                return CharacterVirtualBonePoseResult.Fail(CharacterVirtualBonePoseFailure.InvalidPhysicalHierarchy);

            for (int virtualIndex = 0; virtualIndex < counts.VirtualBoneCount; virtualIndex++)
            {
                CharacterVirtualBoneDescriptor descriptor = virtualBones[virtualIndex];
                if (!IsValidDescriptor(descriptor, counts, virtualIndex))
                {
                    return CharacterVirtualBonePoseResult.Fail(
                        CharacterVirtualBonePoseFailure.InvalidVirtualDescriptor,
                        virtualIndex,
                        descriptor.VirtualBoneId);
                }
                for (int previousIndex = 0; previousIndex < virtualIndex; previousIndex++)
                {
                    if (virtualBones[previousIndex].VirtualBoneId.Equals(descriptor.VirtualBoneId))
                    {
                        return CharacterVirtualBonePoseResult.Fail(
                            CharacterVirtualBonePoseFailure.DuplicateVirtualBoneIdentity,
                            virtualIndex,
                            descriptor.VirtualBoneId);
                    }
                }

                CharacterComponentBonePose source = componentScratch[descriptor.SourcePhysicalBoneIndex];
                CharacterComponentBonePose target = componentScratch[descriptor.TargetPhysicalBoneIndex];
                if (!CharacterPoseConstraintMath.IsUsableScale(source.Scale))
                {
                    return CharacterVirtualBonePoseResult.Fail(
                        CharacterVirtualBonePoseFailure.DegenerateSourceScale,
                        virtualIndex,
                        descriptor.VirtualBoneId);
                }

                Vector3 unrotatedPosition =
                    Quaternion.Inverse(source.Rotation) * (target.Position - source.Position);
                Vector3 localPosition = new Vector3(
                    unrotatedPosition.x / source.Scale.x,
                    unrotatedPosition.y / source.Scale.y,
                    unrotatedPosition.z / source.Scale.z);
                Quaternion localRotation =
                    (Quaternion.Inverse(source.Rotation) * target.Rotation).normalized;
                if (!CharacterPoseConstraintMath.IsFinite(localPosition) ||
                    !CharacterPoseConstraintMath.IsFinite(localRotation) ||
                    Quaternion.Dot(localRotation, localRotation) <= 0f)
                {
                    return CharacterVirtualBonePoseResult.Fail(
                        CharacterVirtualBonePoseFailure.NonFiniteResult,
                        virtualIndex,
                        descriptor.VirtualBoneId);
                }
                outputPose[descriptor.PoseBoneIndex] =
                    new AnimationLocalBonePose(localPosition, localRotation, Vector3.one);
            }

            return CharacterVirtualBonePoseResult.Success();
        }

        static bool IsValidDescriptor(
            CharacterVirtualBoneDescriptor descriptor,
            CharacterPoseBoneCounts counts,
            int virtualIndex) =>
            descriptor.IsValid &&
            descriptor.SourcePhysicalBoneIndex < counts.PhysicalBoneCount &&
            descriptor.TargetPhysicalBoneIndex < counts.PhysicalBoneCount &&
            descriptor.PoseBoneIndex == counts.PhysicalBoneCount + virtualIndex;
    }
}
