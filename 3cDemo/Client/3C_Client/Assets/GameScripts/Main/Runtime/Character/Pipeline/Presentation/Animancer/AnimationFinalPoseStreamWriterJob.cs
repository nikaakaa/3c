using System;
using ThirdPersonCharacter.Pipeline.Animation;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Animations;

namespace ThirdPersonCharacter.Pipeline.Presentation.Animancer
{
    internal struct AnimationFinalPoseStreamWriterJob : IAnimationJob
    {
        [ReadOnly]
        readonly NativeSlice<AnimationLocalBonePose> m_DenseLocalPoses;
        [ReadOnly]
        readonly NativeSlice<PoseSlotFrameAvailability> m_Availability;
        [ReadOnly]
        readonly NativeSlice<ulong> m_ContinuityIdentity;
        [ReadOnly]
        readonly NativeSlice<AnimationPoseNativeInvalidReason> m_OutputInvalidReason;
        [ReadOnly]
        readonly NativeSlice<AnimationPoseNativeInvalidReason> m_PoseGraphInvalidReason;
        [ReadOnly]
        readonly NativeSlice<ulong> m_PoseGraphCompletedAt;
        [NativeDisableParallelForRestriction]
        readonly NativeSlice<ulong> m_AppliedAt;
        [ReadOnly]
        readonly NativeArray<TransformStreamHandle> m_Handles;
        readonly ulong m_CompletionIdentity;

        internal AnimationFinalPoseStreamWriterJob(
            AnimationFinalPoseNativeReadBinding binding,
            NativeArray<TransformStreamHandle> handles)
        {
            RequireValidBinding(binding, handles);
            m_DenseLocalPoses = binding.DenseLocalPoses;
            m_Availability = binding.Availability;
            m_ContinuityIdentity = binding.ContinuityIdentity;
            m_OutputInvalidReason = binding.OutputInvalidReason;
            m_PoseGraphInvalidReason = binding.PoseGraphInvalidReason;
            m_PoseGraphCompletedAt = binding.PoseGraphCompletedAt;
            m_AppliedAt = binding.AppliedAt;
            m_Handles = handles;
            m_CompletionIdentity = binding.CompletionIdentity;
        }

        public void ProcessAnimation(AnimationStream stream)
        {
            if (m_PoseGraphCompletedAt[0] != m_CompletionIdentity ||
                m_Availability[0] != PoseSlotFrameAvailability.Pose ||
                m_OutputInvalidReason[0] != AnimationPoseNativeInvalidReason.None ||
                m_PoseGraphInvalidReason[0] != AnimationPoseNativeInvalidReason.None ||
                m_ContinuityIdentity[0] == 0)
            {
                return;
            }

            for (int boneIndex = 0; boneIndex < m_Handles.Length; boneIndex++)
            {
                if (!m_Handles[boneIndex].IsValid(stream) || !m_DenseLocalPoses[boneIndex].IsValid)
                    return;
            }

            for (int boneIndex = 0; boneIndex < m_Handles.Length; boneIndex++)
            {
                TransformStreamHandle handle = m_Handles[boneIndex];
                AnimationLocalBonePose pose = m_DenseLocalPoses[boneIndex];
                handle.SetLocalPosition(stream, pose.Position);
                handle.SetLocalRotation(stream, pose.Rotation);
                handle.SetLocalScale(stream, pose.Scale);
            }

            NativeSlice<ulong> appliedAt = m_AppliedAt;
            appliedAt[0] = m_CompletionIdentity;
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        }

        static void RequireValidBinding(
            AnimationFinalPoseNativeReadBinding binding,
            NativeArray<TransformStreamHandle> handles)
        {
            if (binding.CompletionIdentity == 0 || binding.OutputPoseValueIndex < 0 ||
                binding.DenseLocalPoses.Length == 0 ||
                !handles.IsCreated || handles.Length != binding.DenseLocalPoses.Length ||
                binding.PoseParameters.Length == 0 ||
                binding.Contributions.Length == 0 ||
                binding.DenseContributionWeights.Length != checked(binding.Contributions.Length * binding.DenseLocalPoses.Length) ||
                !IsUnit(binding.ContributionCount) || !IsUnit(binding.OutputWeight) ||
                !IsUnit(binding.LeftFootFeatures) || !IsUnit(binding.RightFootFeatures) ||
                !IsUnit(binding.HasFootFeatures) || !IsUnit(binding.Availability) ||
                !IsUnit(binding.ContinuityIdentity) || !IsUnit(binding.OutputInvalidReason) ||
                !IsUnit(binding.PoseGraphInvalidReason) || !IsUnit(binding.PoseGraphInvalidOperationIndex) ||
                !IsUnit(binding.PoseGraphCompletedAt) || !IsUnit(binding.AppliedAt))
            {
                throw new ArgumentException("Final animation pose stream writer binding is invalid.");
            }
        }

        static bool IsUnit<T>(NativeSlice<T> values) where T : struct =>
            values.Length == 1;
    }
}
