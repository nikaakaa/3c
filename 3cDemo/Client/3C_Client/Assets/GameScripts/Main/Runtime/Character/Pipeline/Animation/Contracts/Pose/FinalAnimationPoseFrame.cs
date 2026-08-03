using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct ComposedAnimationPoseFrame
    {
        readonly string m_PoseGraphId;
        readonly string m_PosePlanHash;
        readonly ulong m_CompletionIdentity;
        readonly AnimationPoseAvailability m_Availability;
        readonly AnimationReadOnlyBuffer<AnimationLocalBonePose> m_DenseLocalPose;
        readonly AnimationReadOnlyBuffer<float> m_PoseParameters;
        readonly AnimationReadOnlyBuffer<byte> m_PoseParameterAvailability;
        readonly AnimationReadOnlyBuffer<AnimationPoseSourceContribution> m_Contributions;
        readonly AnimationReadOnlyBuffer<float> m_DenseContributionWeights;
        readonly AnimationReadOnlyBuffer<CharacterPoseBoneKind> m_BoneKinds;
        readonly int m_PhysicalBoneCount;
        readonly int m_VirtualBoneCount;
        readonly int m_PoseBoneCount;
        readonly AnimationFootFeatureSample m_LeftFootFeatures;
        readonly AnimationFootFeatureSample m_RightFootFeatures;
        readonly bool m_HasFootFeatures;
        readonly ulong m_ContinuityIdentity;
        readonly IAnimationReadOnlyBufferLease m_Lease;
        readonly ulong m_LeaseIdentity;

        internal ComposedAnimationPoseFrame(
            string poseGraphId,
            string posePlanHash,
            ulong completionIdentity,
            AnimationPoseAvailability availability,
            AnimationReadOnlyBuffer<AnimationLocalBonePose> denseLocalPose,
            AnimationReadOnlyBuffer<float> poseParameters,
            AnimationReadOnlyBuffer<byte> poseParameterAvailability,
            AnimationReadOnlyBuffer<AnimationPoseSourceContribution> contributions,
            AnimationReadOnlyBuffer<float> denseContributionWeights,
            AnimationReadOnlyBuffer<CharacterPoseBoneKind> boneKinds,
            int physicalBoneCount,
            int virtualBoneCount,
            int poseBoneCount,
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

            bool isPose = availability == AnimationPoseAvailability.Pose;
            bool isInvalid = availability == AnimationPoseAvailability.Invalid;
            bool footFeaturesValid = hasFootFeatures
                ? leftFootFeatures.IsValid && rightFootFeatures.IsValid
                : !leftFootFeatures.IsValid && !rightFootFeatures.IsValid;
            int expectedDenseWeightCount = isPose
                ? checked(contributions.Count * denseLocalPose.Count)
                : 0;
            bool boneCatalogValid =
                physicalBoneCount > 0 &&
                virtualBoneCount >= 0 &&
                poseBoneCount == physicalBoneCount + virtualBoneCount &&
                boneKinds.Count == poseBoneCount;
            bool payloadValid = isPose
                ? denseLocalPose.Count == poseBoneCount && poseParameters.Count > 0 && poseParameterAvailability.Count == poseParameters.Count && contributions.Count > 0 &&
                  denseContributionWeights.Count == expectedDenseWeightCount
                : poseParameters.Count > 0 && poseParameterAvailability.Count == poseParameters.Count && denseLocalPose.Count == 0 && contributions.Count == 0 &&
                  denseContributionWeights.Count == 0 && !hasFootFeatures;
            if (string.IsNullOrWhiteSpace(poseGraphId) || string.IsNullOrWhiteSpace(posePlanHash) ||
                completionIdentity == 0 || continuityIdentity == 0 || !isPose && !isInvalid ||
                !footFeaturesValid || !payloadValid || !boneCatalogValid)
            {
                throw new ArgumentException("Composed Animation Pose Frame is invalid.");
            }
            m_PoseGraphId = poseGraphId;
            m_PosePlanHash = posePlanHash;
            m_CompletionIdentity = completionIdentity;
            m_Availability = availability;
            m_DenseLocalPose = denseLocalPose;
            m_PoseParameters = poseParameters;
            m_PoseParameterAvailability = poseParameterAvailability;
            m_Contributions = contributions;
            m_DenseContributionWeights = denseContributionWeights;
            m_BoneKinds = boneKinds;
            m_PhysicalBoneCount = physicalBoneCount;
            m_VirtualBoneCount = virtualBoneCount;
            m_PoseBoneCount = poseBoneCount;
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

        public string PosePlanHash
        {
            get
            {
                RequireLease();
                return m_PosePlanHash;
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

        public AnimationPoseAvailability Availability
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
                throw new InvalidOperationException("Composed Animation Pose Frame lease is unavailable.");
            m_Lease.RequireValid(m_LeaseIdentity);
        }

        public AnimationReadOnlyBuffer<CharacterPoseBoneKind> BoneKinds
        {
            get
            {
                RequireLease();
                return m_BoneKinds;
            }
        }

        public int PhysicalBoneCount
        {
            get
            {
                RequireLease();
                return m_PhysicalBoneCount;
            }
        }

        public int VirtualBoneCount
        {
            get
            {
                RequireLease();
                return m_VirtualBoneCount;
            }
        }

        public int PoseBoneCount
        {
            get
            {
                RequireLease();
                return m_PoseBoneCount;
            }
        }

        public AnimationReadOnlyBuffer<byte> PoseParameterAvailability
        {
            get
            {
                RequireLease();
                return m_PoseParameterAvailability;
            }
        }
    }

    public readonly struct FinalAnimationPoseFrame
    {
        readonly ComposedAnimationPoseFrame m_Composed;
        readonly ulong m_WorldAwareCompletionIdentity;

        internal FinalAnimationPoseFrame(
            in ComposedAnimationPoseFrame composed,
            ulong worldAwareCompletionIdentity)
        {
            if (composed.CompletionIdentity == 0 || worldAwareCompletionIdentity == 0)
                throw new ArgumentException("Final Animation Pose Frame completion is invalid.");
            m_Composed = composed;
            m_WorldAwareCompletionIdentity = worldAwareCompletionIdentity;
        }

        public ComposedAnimationPoseFrame ComposedPose => m_Composed;
        public ulong WorldAwareCompletionIdentity => m_WorldAwareCompletionIdentity;
        public string PoseGraphId => m_Composed.PoseGraphId;
        public string PosePlanHash => m_Composed.PosePlanHash;
        public ulong CompletionIdentity => m_Composed.CompletionIdentity;
        public AnimationPoseAvailability Availability => m_Composed.Availability;
        public AnimationReadOnlyBuffer<AnimationLocalBonePose> DenseLocalPose => m_Composed.DenseLocalPose;
        public AnimationReadOnlyBuffer<float> PoseParameters => m_Composed.PoseParameters;
        public AnimationReadOnlyBuffer<AnimationPoseSourceContribution> Contributions => m_Composed.Contributions;
        public AnimationReadOnlyBuffer<CharacterPoseBoneKind> BoneKinds => m_Composed.BoneKinds;
        public int PhysicalBoneCount => m_Composed.PhysicalBoneCount;
        public int VirtualBoneCount => m_Composed.VirtualBoneCount;
        public int PoseBoneCount => m_Composed.PoseBoneCount;
        public AnimationFootFeatureSample LeftFootFeatures => m_Composed.LeftFootFeatures;
        public AnimationFootFeatureSample RightFootFeatures => m_Composed.RightFootFeatures;
        public bool HasFootFeatures => m_Composed.HasFootFeatures;
        public ulong ContinuityIdentity => m_Composed.ContinuityIdentity;
        public float GetContributionBoneWeight(int contributionIndex, int boneIndex) =>
            m_Composed.GetContributionBoneWeight(contributionIndex, boneIndex);
        public float GetBoneOutputWeight(int boneIndex) => m_Composed.GetBoneOutputWeight(boneIndex);
    }
}
