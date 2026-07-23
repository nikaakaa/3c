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
        [NativeDisableParallelForRestriction]
        readonly NativeArray<byte> m_HasPrevious;
        [NativeDisableParallelForRestriction]
        readonly NativeArray<ulong> m_CompletedAt;
        readonly int m_SourceIndex;
        readonly ulong m_CompletionIdentity;
        readonly float m_PresentationDeltaSeconds;
        [ReadOnly]
        readonly NativeArray<TransformStreamHandle> m_Handles;
        [ReadOnly]
        readonly NativeArray<AnimationLocalBonePose> m_ReferencePose;
        readonly int m_RootBoneIndex;
        readonly CharacterAnimationRootBonePolicy m_RootBonePolicy;
        readonly CharacterAnimationScalePolicy m_ScalePolicy;

        internal AnimationSourcePoseCaptureJob(
            AnimationPoseSourceCaptureBinding binding,
            NativeArray<TransformStreamHandle> handles,
            NativeArray<AnimationLocalBonePose> referencePose,
            int rootBoneIndex,
            CharacterAnimationRootBonePolicy rootBonePolicy,
            CharacterAnimationScalePolicy scalePolicy)
        {
            RequireValidBinding(binding);
            if (!handles.IsCreated || handles.Length == 0 ||
                !referencePose.IsCreated || referencePose.Length != handles.Length ||
                binding.CurrentPose.Length != handles.Length ||
                rootBoneIndex < 0 || rootBoneIndex >= handles.Length ||
                !Enum.IsDefined(typeof(CharacterAnimationRootBonePolicy), rootBonePolicy) ||
                !Enum.IsDefined(typeof(CharacterAnimationScalePolicy), scalePolicy))
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
            m_HasPrevious = binding.HasPrevious;
            m_CompletedAt = binding.CompletedAt;
            m_SourceIndex = binding.SourceIndex;
            m_CompletionIdentity = binding.CompletionIdentity;
            m_PresentationDeltaSeconds = binding.PresentationDeltaSeconds;
            m_Handles = handles;
            m_ReferencePose = referencePose;
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
            bool hasPrevious = hasPreviousStatus[m_SourceIndex] != 0;

            for (int boneIndex = 0; boneIndex < m_Handles.Length; boneIndex++)
            {
                TransformStreamHandle handle = m_Handles[boneIndex];
                if (!handle.IsValid(stream))
                    return;

                Vector3 position = handle.GetLocalPosition(stream);
                Quaternion rotation = handle.GetLocalRotation(stream);
                Vector3 scale = handle.GetLocalScale(stream);
                if (!IsFinite(position) || !IsFinite(rotation) || !IsFinite(scale) ||
                    Quaternion.Dot(rotation, rotation) <= 0f)
                {
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
                    return;
                currentPose[boneIndex] = pose;
            }

            if (hasPrevious)
            {
                for (int boneIndex = 0; boneIndex < previousPose.Length; boneIndex++)
                {
                    if (!previousPose[boneIndex].IsValid)
                        return;
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
                previousPose[boneIndex] = current;
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
                !binding.HasPrevious.IsCreated || !binding.CompletedAt.IsCreated || binding.HasPrevious.Length == 0 ||
                binding.HasPrevious.Length != binding.CompletedAt.Length ||
                binding.SourceIndex >= binding.HasPrevious.Length ||
                binding.HasPrevious[binding.SourceIndex] > 1 ||
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
