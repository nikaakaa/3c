using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public static class CharacterVirtualBonePoseDerivation
    {
        public static CharacterVirtualBonePoseResult ValidateLayout(
            CharacterPoseBoneCounts counts,
            NativeArray<int> physicalParentIndices,
            NativeArray<CharacterVirtualBoneDescriptor> virtualBones)
        {
            if (!counts.IsValid ||
                physicalParentIndices.Length != counts.PhysicalBoneCount ||
                virtualBones.Length != counts.VirtualBoneCount)
                return CharacterVirtualBonePoseResult.Fail(CharacterVirtualBonePoseFailure.InvalidCounts);
            int rootCount = 0;
            for (int i = 0; i < counts.PhysicalBoneCount; i++)
            {
                int parent = physicalParentIndices[i];
                if (parent < -1 || parent >= i)
                    return CharacterVirtualBonePoseResult.Fail(CharacterVirtualBonePoseFailure.InvalidPhysicalHierarchy);
                if (parent < 0)
                    rootCount++;
            }
            if (rootCount != 1)
                return CharacterVirtualBonePoseResult.Fail(CharacterVirtualBonePoseFailure.InvalidPhysicalHierarchy);
            for (int i = 0; i < counts.VirtualBoneCount; i++)
            {
                CharacterVirtualBoneDescriptor descriptor = virtualBones[i];
                if (!IsValidDescriptor(descriptor, counts, i))
                    return CharacterVirtualBonePoseResult.Fail(
                        CharacterVirtualBonePoseFailure.InvalidVirtualDescriptor, i, descriptor.VirtualBoneId);
                for (int previous = 0; previous < i; previous++)
                    if (virtualBones[previous].VirtualBoneId.Equals(descriptor.VirtualBoneId))
                        return CharacterVirtualBonePoseResult.Fail(
                            CharacterVirtualBonePoseFailure.DuplicateVirtualBoneIdentity, i, descriptor.VirtualBoneId);
            }
            return CharacterVirtualBonePoseResult.Success();
        }

        public static CharacterVirtualBonePoseResult Derive(
            CharacterPoseBoneCounts counts,
            NativeSlice<AnimationLocalBonePose> physicalLocalPoses,
            NativeArray<int> physicalParentIndices,
            NativeArray<CharacterVirtualBoneDescriptor> virtualBones,
            NativeArray<CharacterComponentBonePose> componentScratch,
            NativeSlice<AnimationLocalBonePose> outputPose)
        {
            if (physicalLocalPoses.Length != counts.PhysicalBoneCount ||
                componentScratch.Length < counts.PhysicalBoneCount ||
                outputPose.Length != counts.PoseBoneCount)
            {
                return CharacterVirtualBonePoseResult.Fail(CharacterVirtualBonePoseFailure.InvalidCounts);
            }

            for (int physicalIndex = 0; physicalIndex < counts.PhysicalBoneCount; physicalIndex++)
            {
                int parentIndex = physicalParentIndices[physicalIndex];
                AnimationLocalBonePose local = physicalLocalPoses[physicalIndex];
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
            for (int virtualIndex = 0; virtualIndex < counts.VirtualBoneCount; virtualIndex++)
            {
                CharacterVirtualBoneDescriptor descriptor = virtualBones[virtualIndex];
                CharacterComponentBonePose source = componentScratch[descriptor.SourcePhysicalBoneIndex];
                CharacterComponentBonePose target = componentScratch[descriptor.TargetPhysicalBoneIndex];
                if (!CharacterPoseConstraintMath.IsUsableScale(source.Scale))
                {
                    return CharacterVirtualBonePoseResult.Fail(
                        CharacterVirtualBonePoseFailure.DegenerateSourceScale,
                        virtualIndex,
                        descriptor.VirtualBoneId);
                }

                Quaternion inverseSource = Quaternion.Inverse(source.Rotation);
                Vector3 unrotatedPosition = inverseSource * (target.Position - source.Position);
                Vector3 localPosition = new Vector3(
                    unrotatedPosition.x / source.Scale.x,
                    unrotatedPosition.y / source.Scale.y,
                    unrotatedPosition.z / source.Scale.z);
                Quaternion localRotation =
                    (inverseSource * target.Rotation).normalized;
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
