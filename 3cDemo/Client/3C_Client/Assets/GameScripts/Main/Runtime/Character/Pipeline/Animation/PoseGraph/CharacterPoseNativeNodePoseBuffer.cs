using System;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseNativeNodePoseBuffer : IDisposable
    {
        readonly int m_PhysicalPlayerIndex;
        readonly int m_BoneCount;
        readonly int m_ParameterCount;
        readonly int m_ContributionCapacity;
        NativeArray<AnimationLocalBonePose> m_DenseLocalPoses;
        NativeArray<AnimationBlendBoneVelocity> m_DenseVelocities;
        NativeArray<float> m_PoseParameters;
        NativeArray<byte> m_PoseParameterAvailability;
        NativeArray<AnimationPrimitivePoseContribution> m_Contributions;
        NativeArray<float> m_DenseContributionWeights;
        NativeArray<int> m_ContributionCount;
        NativeArray<float> m_OutputWeight;
        NativeArray<AnimationFootFeatureSample> m_LeftFootFeatures;
        NativeArray<AnimationFootFeatureSample> m_RightFootFeatures;
        NativeArray<byte> m_HasFootFeatures;
        NativeArray<AnimationPoseAvailability> m_Availability;
        NativeArray<ulong> m_ContinuityIdentity;
        NativeArray<PoseDiscontinuityNative> m_Discontinuity;
        NativeArray<AnimationPoseNativeInvalidReason> m_InvalidReason;
        NativeArray<ulong> m_CompletedAt;
        bool m_Disposed;

        internal CharacterPoseNativeNodePoseBuffer(
            int physicalPlayerIndex,
            int boneCount,
            int parameterCount,
            int contributionCapacity)
        {
            if (physicalPlayerIndex < 0 || boneCount <= 0 || parameterCount <= 0 ||
                contributionCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(physicalPlayerIndex));
            }
            m_PhysicalPlayerIndex = physicalPlayerIndex;
            m_BoneCount = boneCount;
            m_ParameterCount = parameterCount;
            m_ContributionCapacity = contributionCapacity;
            try
            {
                m_DenseLocalPoses = Allocate<AnimationLocalBonePose>(boneCount);
                m_DenseVelocities = Allocate<AnimationBlendBoneVelocity>(boneCount);
                m_PoseParameters = Allocate<float>(parameterCount);
                m_PoseParameterAvailability = Allocate<byte>(parameterCount);
                m_Contributions = Allocate<AnimationPrimitivePoseContribution>(contributionCapacity);
                m_DenseContributionWeights = Allocate<float>(checked(contributionCapacity * boneCount));
                m_ContributionCount = Allocate<int>(1);
                m_OutputWeight = Allocate<float>(1);
                m_LeftFootFeatures = Allocate<AnimationFootFeatureSample>(1);
                m_RightFootFeatures = Allocate<AnimationFootFeatureSample>(1);
                m_HasFootFeatures = Allocate<byte>(1);
                m_Availability = Allocate<AnimationPoseAvailability>(1);
                m_ContinuityIdentity = Allocate<ulong>(1);
                m_Discontinuity = Allocate<PoseDiscontinuityNative>(1);
                m_InvalidReason = Allocate<AnimationPoseNativeInvalidReason>(1);
                m_CompletedAt = Allocate<ulong>(1);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal AnimationPlayerPoseNativeWriteBinding RequireWriteBinding(
            ulong completionIdentity)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(
                    nameof(CharacterPoseNativeNodePoseBuffer));
            return new AnimationPlayerPoseNativeWriteBinding(
                m_PhysicalPlayerIndex,
                m_BoneCount,
                m_ParameterCount,
                m_ContributionCapacity,
                completionIdentity,
                new NativeSlice<AnimationLocalBonePose>(m_DenseLocalPoses),
                new NativeSlice<AnimationBlendBoneVelocity>(m_DenseVelocities),
                new NativeSlice<float>(m_PoseParameters),
                new NativeSlice<byte>(m_PoseParameterAvailability),
                new NativeSlice<AnimationPrimitivePoseContribution>(m_Contributions),
                new NativeSlice<float>(m_DenseContributionWeights),
                new NativeSlice<int>(m_ContributionCount),
                new NativeSlice<float>(m_OutputWeight),
                new NativeSlice<AnimationFootFeatureSample>(m_LeftFootFeatures),
                new NativeSlice<AnimationFootFeatureSample>(m_RightFootFeatures),
                new NativeSlice<byte>(m_HasFootFeatures),
                new NativeSlice<AnimationPoseAvailability>(m_Availability),
                new NativeSlice<ulong>(m_ContinuityIdentity),
                new NativeSlice<PoseDiscontinuityNative>(m_Discontinuity),
                new NativeSlice<AnimationPoseNativeInvalidReason>(m_InvalidReason),
                new NativeSlice<ulong>(m_CompletedAt));
        }

        internal int PhysicalPlayerIndex => m_PhysicalPlayerIndex;
        internal int BoneCount => m_BoneCount;
        internal int ParameterCount => m_ParameterCount;
        internal int ContributionCapacity => m_ContributionCapacity;

        internal CharacterPoseNativeNodePoseBuffer CreateSibling() =>
            new CharacterPoseNativeNodePoseBuffer(
                m_PhysicalPlayerIndex,
                m_BoneCount,
                m_ParameterCount,
                m_ContributionCapacity);

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            Dispose(ref m_CompletedAt);
            Dispose(ref m_InvalidReason);
            Dispose(ref m_Discontinuity);
            Dispose(ref m_ContinuityIdentity);
            Dispose(ref m_Availability);
            Dispose(ref m_HasFootFeatures);
            Dispose(ref m_RightFootFeatures);
            Dispose(ref m_LeftFootFeatures);
            Dispose(ref m_OutputWeight);
            Dispose(ref m_ContributionCount);
            Dispose(ref m_DenseContributionWeights);
            Dispose(ref m_Contributions);
            Dispose(ref m_PoseParameterAvailability);
            Dispose(ref m_PoseParameters);
            Dispose(ref m_DenseVelocities);
            Dispose(ref m_DenseLocalPoses);
        }

        static NativeArray<T> Allocate<T>(int length) where T : struct =>
            new NativeArray<T>(
                length,
                Allocator.Persistent,
                NativeArrayOptions.ClearMemory);

        static void Dispose<T>(ref NativeArray<T> values) where T : struct
        {
            if (!values.IsCreated)
                return;
            values.Dispose();
            values = default;
        }
    }
}
