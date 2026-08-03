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
        readonly NativeSlice<AnimationLocalBonePose> m_CurrentPose;
        [NativeDisableParallelForRestriction]
        readonly NativeSlice<AnimationLocalBonePose> m_PreviousPose;
        [NativeDisableParallelForRestriction]
        readonly NativeSlice<AnimationBlendBoneVelocity> m_Velocity;
        [ReadOnly]
        readonly NativeArray<byte> m_PreviousAvailable;
        [NativeDisableParallelForRestriction]
        readonly NativeArray<byte> m_HasPrevious;
        [NativeDisableParallelForRestriction]
        readonly NativeArray<ulong> m_CompletedAt;
        [NativeDisableParallelForRestriction]
        readonly NativeArray<AnimationSourcePoseCaptureFailure> m_Failure;
        readonly int m_SourceIndex;
        readonly ulong m_CompletionIdentity;
        readonly float m_PresentationDeltaSeconds;
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
            AnimationPoseSourceCaptureBinding binding,
            CharacterPoseBoneCounts boneCounts,
            NativeArray<TransformStreamHandle> handles,
            NativeArray<AnimationLocalBonePose> referencePose,
            NativeArray<int> physicalParentIndices,
            NativeArray<CharacterVirtualBoneDescriptor> virtualBones,
            NativeArray<CharacterComponentBonePose> componentScratch,
            int rootBoneIndex,
            CharacterAnimationRootBonePolicy rootBonePolicy,
            CharacterAnimationScalePolicy scalePolicy,
            bool validateBinding = true)
        {
            if (validateBinding)
                RequireValidBinding(binding);
            if (!boneCounts.IsValid ||
                !handles.IsCreated || handles.Length != boneCounts.PhysicalBoneCount ||
                !referencePose.IsCreated || referencePose.Length != boneCounts.PoseBoneCount ||
                !physicalParentIndices.IsCreated || physicalParentIndices.Length != boneCounts.PhysicalBoneCount ||
                !virtualBones.IsCreated || virtualBones.Length != boneCounts.VirtualBoneCount ||
                !componentScratch.IsCreated || componentScratch.Length < boneCounts.PhysicalBoneCount ||
                validateBinding && binding.CurrentPose.Length != boneCounts.PoseBoneCount ||
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

                AnimationLocalBonePose pose = new AnimationLocalBonePose(position, rotation, scale);
                if (m_RootBonePolicy == CharacterAnimationRootBonePolicy.ExcludeSourceRoot &&
                    boneIndex == m_RootBoneIndex)
                {
                    pose = m_ReferencePose[boneIndex];
                }
                else if (m_ScalePolicy == CharacterAnimationScalePolicy.PreserveReferenceScale)
                {
                    pose = new AnimationLocalBonePose(
                        pose.Position,
                        pose.Rotation,
                        m_ReferencePose[boneIndex].Scale);
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

        static void RequireValidBinding(AnimationPoseSourceCaptureBinding binding)
        {
            if (!binding.SourceId.IsValid || binding.SourceIndex < 0 || binding.CompletionIdentity == 0 ||
                binding.CurrentPose.Length == 0 ||
                binding.PreviousPose.Length != binding.CurrentPose.Length ||
                binding.Velocity.Length != binding.CurrentPose.Length ||
                !binding.PreviousAvailable.IsCreated || !binding.HasPrevious.IsCreated || !binding.CompletedAt.IsCreated || !binding.Failure.IsCreated ||
                binding.HasPrevious.Length == 0 ||
                binding.PreviousAvailable.Length != binding.HasPrevious.Length ||
                binding.HasPrevious.Length != binding.CompletedAt.Length ||
                binding.HasPrevious.Length != binding.Failure.Length ||
                binding.SourceIndex >= binding.HasPrevious.Length ||
                binding.PreviousAvailable[binding.SourceIndex] > 1 || binding.HasPrevious[binding.SourceIndex] > 1 ||
                !float.IsFinite(binding.PresentationDeltaSeconds) || binding.PresentationDeltaSeconds < 0f)
            {
                throw new ArgumentException("Animation pose source capture binding is invalid.", nameof(binding));
            }
        }

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z) && float.IsFinite(value.w);
    }
}
