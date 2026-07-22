using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct FinalAnimationPoseFrame
    {
        readonly string m_PoseGraphId;
        readonly string m_PoseProgramHash;
        readonly ulong m_CompletionIdentity;
        readonly PoseSlotFrameAvailability m_Availability;
        readonly AnimationReadOnlyBuffer<AnimationLocalBonePose> m_DenseLocalPose;
        readonly AnimationReadOnlyBuffer<float> m_PoseParameters;
        readonly AnimationReadOnlyBuffer<AnimationPoseSourceContribution> m_Contributions;
        readonly AnimationReadOnlyBuffer<float> m_DenseContributionWeights;
        readonly AnimationFootFeatureSample m_LeftFootFeatures;
        readonly AnimationFootFeatureSample m_RightFootFeatures;
        readonly bool m_HasFootFeatures;
        readonly ulong m_ContinuityIdentity;
        readonly IAnimationReadOnlyBufferLease m_Lease;
        readonly ulong m_LeaseIdentity;

        internal FinalAnimationPoseFrame(
            string poseGraphId,
            string poseProgramHash,
            ulong completionIdentity,
            PoseSlotFrameAvailability availability,
            AnimationReadOnlyBuffer<AnimationLocalBonePose> denseLocalPose,
            AnimationReadOnlyBuffer<float> poseParameters,
            AnimationReadOnlyBuffer<AnimationPoseSourceContribution> contributions,
            AnimationReadOnlyBuffer<float> denseContributionWeights,
            AnimationFootFeatureSample leftFootFeatures,
            AnimationFootFeatureSample rightFootFeatures,
            bool hasFootFeatures,
            ulong continuityIdentity,
            IAnimationReadOnlyBufferLease lease,
            ulong leaseIdentity)
        {
            if (lease == null)
                throw new ArgumentNullException(nameof(lease));
            if (leaseIdentity == 0)
                throw new ArgumentOutOfRangeException(nameof(leaseIdentity));
            lease.RequireValid(leaseIdentity);

            bool isPose = availability == PoseSlotFrameAvailability.Pose;
            bool isInvalid = availability == PoseSlotFrameAvailability.Invalid;
            bool footFeaturesValid = hasFootFeatures
                ? leftFootFeatures.IsValid && rightFootFeatures.IsValid
                : !leftFootFeatures.IsValid && !rightFootFeatures.IsValid;
            int expectedDenseWeightCount = isPose
                ? checked(contributions.Count * denseLocalPose.Count)
                : 0;
            bool payloadValid = isPose
                ? denseLocalPose.Count > 0 && poseParameters.Count > 0 && contributions.Count > 0 &&
                  denseContributionWeights.Count == expectedDenseWeightCount
                : poseParameters.Count > 0 && denseLocalPose.Count == 0 && contributions.Count == 0 &&
                  denseContributionWeights.Count == 0 && !hasFootFeatures;
            if (string.IsNullOrWhiteSpace(poseGraphId) || string.IsNullOrWhiteSpace(poseProgramHash) ||
                completionIdentity == 0 || continuityIdentity == 0 || !isPose && !isInvalid ||
                !footFeaturesValid || !payloadValid)
            {
                throw new ArgumentException("Final Animation Pose Frame is invalid.");
            }
            m_PoseGraphId = poseGraphId;
            m_PoseProgramHash = poseProgramHash;
            m_CompletionIdentity = completionIdentity;
            m_Availability = availability;
            m_DenseLocalPose = denseLocalPose;
            m_PoseParameters = poseParameters;
            m_Contributions = contributions;
            m_DenseContributionWeights = denseContributionWeights;
            m_LeftFootFeatures = leftFootFeatures;
            m_RightFootFeatures = rightFootFeatures;
            m_HasFootFeatures = hasFootFeatures;
            m_ContinuityIdentity = continuityIdentity;
            m_Lease = lease;
            m_LeaseIdentity = leaseIdentity;
        }

        public string PoseGraphId
        {
            get
            {
                RequireLease();
                return m_PoseGraphId;
            }
        }

        public string PoseProgramHash
        {
            get
            {
                RequireLease();
                return m_PoseProgramHash;
            }
        }

        public ulong CompletionIdentity
        {
            get
            {
                RequireLease();
                return m_CompletionIdentity;
            }
        }

        public PoseSlotFrameAvailability Availability
        {
            get
            {
                RequireLease();
                return m_Availability;
            }
        }

        public AnimationReadOnlyBuffer<AnimationLocalBonePose> DenseLocalPose
        {
            get
            {
                RequireLease();
                return m_DenseLocalPose;
            }
        }

        public AnimationReadOnlyBuffer<float> PoseParameters
        {
            get
            {
                RequireLease();
                return m_PoseParameters;
            }
        }

        public AnimationReadOnlyBuffer<AnimationPoseSourceContribution> Contributions
        {
            get
            {
                RequireLease();
                return m_Contributions;
            }
        }

        public AnimationFootFeatureSample LeftFootFeatures
        {
            get
            {
                RequireLease();
                return m_LeftFootFeatures;
            }
        }

        public AnimationFootFeatureSample RightFootFeatures
        {
            get
            {
                RequireLease();
                return m_RightFootFeatures;
            }
        }

        public bool HasFootFeatures
        {
            get
            {
                RequireLease();
                return m_HasFootFeatures;
            }
        }

        public ulong ContinuityIdentity
        {
            get
            {
                RequireLease();
                return m_ContinuityIdentity;
            }
        }

        public float GetContributionBoneWeight(int contributionIndex, int boneIndex)
        {
            RequireLease();
            if ((uint)contributionIndex >= (uint)m_Contributions.Count ||
                (uint)boneIndex >= (uint)m_DenseLocalPose.Count)
                throw new ArgumentOutOfRangeException();
            return m_DenseContributionWeights[contributionIndex * m_DenseLocalPose.Count + boneIndex];
        }

        public float GetBoneOutputWeight(int boneIndex)
        {
            RequireLease();
            if ((uint)boneIndex >= (uint)m_DenseLocalPose.Count)
                throw new ArgumentOutOfRangeException(nameof(boneIndex));
            float weight = 0f;
            for (int i = 0; i < m_Contributions.Count; i++)
                weight += m_DenseContributionWeights[i * m_DenseLocalPose.Count + boneIndex];
            return UnityEngine.Mathf.Clamp01(weight);
        }

        void RequireLease()
        {
            if (m_Lease == null || m_LeaseIdentity == 0)
                throw new InvalidOperationException("Final Animation Pose Frame lease is unavailable.");
            m_Lease.RequireValid(m_LeaseIdentity);
        }
    }
}
