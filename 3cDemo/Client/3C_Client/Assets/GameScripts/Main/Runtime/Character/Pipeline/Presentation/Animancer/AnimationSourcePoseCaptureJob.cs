using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Animations;

namespace ThirdPersonCharacter.Pipeline.Presentation.Animancer
{
    internal struct AnimationSourcePoseCaptureJob : IAnimationJob
    {
        [NativeDisableParallelForRestriction]
        NativeSlice<AnimationLocalBonePose> m_CurrentPose;
        [NativeDisableParallelForRestriction]
        NativeSlice<AnimationLocalBonePose> m_PreviousPose;
        [NativeDisableParallelForRestriction]
        NativeSlice<AnimationBlendBoneVelocity> m_Velocity;
        [ReadOnly]
        NativeArray<byte> m_PreviousAvailable;
        [NativeDisableParallelForRestriction]
        NativeArray<byte> m_HasPrevious;
        [NativeDisableParallelForRestriction]
        NativeArray<ulong> m_CompletedAt;
        [NativeDisableParallelForRestriction]
        NativeArray<AnimationSourcePoseCaptureFailure> m_Failure;
        int m_SourceIndex;
        ulong m_CompletionIdentity;
        float m_PresentationDeltaSeconds;
        [ReadOnly]
        readonly NativeArray<TransformStreamHandle> m_Handles;
        [ReadOnly]
        readonly NativeArray<AnimationLocalBonePose> m_ReferencePose;
        [ReadOnly]
        readonly NativeArray<int> m_PhysicalParentIndices;
        [ReadOnly]
        readonly NativeArray<CharacterVirtualBoneDescriptor> m_VirtualBones;
        [NativeDisableParallelForRestriction]
        readonly NativeArray<CharacterComponentBonePose> m_ComponentScratch;
        readonly CharacterPoseBoneCounts m_BoneCounts;
        readonly int m_RootBoneIndex;
        readonly CharacterAnimationRootBonePolicy m_RootBonePolicy;
        readonly CharacterAnimationScalePolicy m_ScalePolicy;

        internal AnimationSourcePoseCaptureJob(
            CharacterPoseBoneCounts boneCounts,
            NativeArray<TransformStreamHandle> handles,
            NativeArray<AnimationLocalBonePose> referencePose,
            NativeArray<int> physicalParentIndices,
            NativeArray<CharacterVirtualBoneDescriptor> virtualBones,
            NativeArray<CharacterComponentBonePose> componentScratch,
            int rootBoneIndex,
            CharacterAnimationRootBonePolicy rootBonePolicy,
            CharacterAnimationScalePolicy scalePolicy)
        {
            if (!boneCounts.IsValid ||
                !handles.IsCreated || handles.Length != boneCounts.PhysicalBoneCount ||
                !referencePose.IsCreated || referencePose.Length != boneCounts.PoseBoneCount ||
                !physicalParentIndices.IsCreated || physicalParentIndices.Length != boneCounts.PhysicalBoneCount ||
                !virtualBones.IsCreated || virtualBones.Length != boneCounts.VirtualBoneCount ||
                !componentScratch.IsCreated || componentScratch.Length < boneCounts.PhysicalBoneCount ||
                rootBoneIndex < 0 || rootBoneIndex >= handles.Length ||
                (byte)rootBonePolicy < (byte)CharacterAnimationRootBonePolicy.ExcludeSourceRoot ||
                (byte)rootBonePolicy > (byte)CharacterAnimationRootBonePolicy.CaptureSourceRoot ||
                (byte)scalePolicy < (byte)CharacterAnimationScalePolicy.PreserveReferenceScale ||
                (byte)scalePolicy > (byte)CharacterAnimationScalePolicy.BlendLocalScale)
            {
                throw new ArgumentException("Animation source pose capture job configuration is invalid.");
            }
            for (int boneIndex = 0; boneIndex < referencePose.Length; boneIndex++)
            {
                if (!referencePose[boneIndex].IsValid)
                    throw new ArgumentException($"Animation source pose reference Bone #{boneIndex} is invalid.");
            }
            CharacterVirtualBonePoseResult layout = CharacterVirtualBonePoseDerivation.ValidateLayout(
                boneCounts, physicalParentIndices, virtualBones);
            if (!layout.Succeeded)
                throw new ArgumentException($"Animation source pose topology is invalid: {layout.Failure}.");

            m_CurrentPose = default;
            m_PreviousPose = default;
            m_Velocity = default;
            m_PreviousAvailable = default;
            m_HasPrevious = default;
            m_CompletedAt = default;
            m_Failure = default;
            m_SourceIndex = 0;
            m_CompletionIdentity = 0;
            m_PresentationDeltaSeconds = 0f;
            m_Handles = handles;
            m_ReferencePose = referencePose;
            m_PhysicalParentIndices = physicalParentIndices;
            m_VirtualBones = virtualBones;
            m_ComponentScratch = componentScratch;
            m_BoneCounts = boneCounts;
            m_RootBoneIndex = rootBoneIndex;
            m_RootBonePolicy = rootBonePolicy;
            m_ScalePolicy = scalePolicy;
        }

        public void ProcessAnimation(AnimationStream stream)
        {
            NativeSlice<AnimationLocalBonePose> currentPose = m_CurrentPose;
            NativeSlice<AnimationLocalBonePose> previousPose = m_PreviousPose;
            NativeSlice<AnimationBlendBoneVelocity> velocity = m_Velocity;
            NativeArray<byte> hasPreviousStatus = m_HasPrevious;
            NativeArray<ulong> completedAt = m_CompletedAt;
            NativeArray<AnimationSourcePoseCaptureFailure> failure = m_Failure;
            failure[m_SourceIndex] = AnimationSourcePoseCaptureFailure.None;
            bool hasPrevious = m_PreviousAvailable[m_SourceIndex] != 0;

            for (int boneIndex = 0; boneIndex < m_Handles.Length; boneIndex++)
            {
                TransformStreamHandle handle = m_Handles[boneIndex];
                if (!handle.IsValid(stream))
                {
                    failure[m_SourceIndex] = AnimationSourcePoseCaptureFailure.PhysicalPoseInvalid;
                    return;
                }

                Vector3 position = handle.GetLocalPosition(stream);
                Quaternion rotation = handle.GetLocalRotation(stream);
                Vector3 scale = handle.GetLocalScale(stream);
                if (!IsFinite(position) || !IsFinite(rotation) || !IsFinite(scale) ||
                    Quaternion.Dot(rotation, rotation) <= 0f)
                {
                    failure[m_SourceIndex] = AnimationSourcePoseCaptureFailure.PhysicalPoseInvalid;
                    return;
                }

                AnimationLocalBonePose pose;
                if (m_RootBonePolicy == CharacterAnimationRootBonePolicy.ExcludeSourceRoot &&
                    boneIndex == m_RootBoneIndex)
                {
                    pose = m_ReferencePose[boneIndex];
                }
                else
                {
                    pose = new AnimationLocalBonePose(
                        position,
                        rotation,
                        m_ScalePolicy == CharacterAnimationScalePolicy.PreserveReferenceScale
                            ? m_ReferencePose[boneIndex].Scale
                            : scale);
                }
                if (!pose.IsValid)
                {
                    failure[m_SourceIndex] = AnimationSourcePoseCaptureFailure.PhysicalPoseInvalid;
                    return;
                }
                currentPose[boneIndex] = pose;
            }

            CharacterVirtualBonePoseResult derivation = CharacterVirtualBonePoseDerivation.Derive(
                m_BoneCounts,
                currentPose.Slice(0, m_BoneCounts.PhysicalBoneCount),
                m_PhysicalParentIndices,
                m_VirtualBones,
                m_ComponentScratch,
                currentPose);
            if (!derivation.Succeeded)
            {
                failure[m_SourceIndex] = AnimationSourcePoseCaptureFailure.VirtualBoneDerivationInvalid;
                return;
            }

            if (hasPrevious)
            {
                for (int boneIndex = 0; boneIndex < previousPose.Length; boneIndex++)
                {
                    if (!previousPose[boneIndex].IsValid)
                    {
                        failure[m_SourceIndex] = AnimationSourcePoseCaptureFailure.PreviousPoseInvalid;
                        return;
                    }
                }
            }

            for (int boneIndex = 0; boneIndex < currentPose.Length; boneIndex++)
            {
                AnimationLocalBonePose current = currentPose[boneIndex];
                velocity[boneIndex] = hasPrevious && m_PresentationDeltaSeconds > 0f
                    ? AnimationPoseMath.Differentiate(
                        previousPose[boneIndex],
                        current,
                        m_PresentationDeltaSeconds)
                    : default;
            }

            hasPreviousStatus[m_SourceIndex] = 1;
            completedAt[m_SourceIndex] = m_CompletionIdentity;
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        internal void BindFrame(in AnimationPoseSourceCaptureBinding binding)
        {
            if (binding.CurrentPose.Length != m_BoneCounts.PoseBoneCount)
                throw new ArgumentException("Animation pose source capture binding does not match its rig.", nameof(binding));
            m_CurrentPose = binding.CurrentPose;
            m_PreviousPose = binding.PreviousPose;
            m_Velocity = binding.Velocity;
            m_PreviousAvailable = binding.PreviousAvailable;
            m_HasPrevious = binding.HasPrevious;
            m_CompletedAt = binding.CompletedAt;
            m_Failure = binding.Failure;
            m_SourceIndex = binding.SourceIndex;
            m_CompletionIdentity = binding.CompletionIdentity;
            m_PresentationDeltaSeconds = binding.PresentationDeltaSeconds;
        }

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z) && float.IsFinite(value.w);
    }
}
