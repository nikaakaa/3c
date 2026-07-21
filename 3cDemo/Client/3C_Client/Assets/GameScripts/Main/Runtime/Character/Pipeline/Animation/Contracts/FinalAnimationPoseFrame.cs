using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct FinalAnimationPoseFrame
    {
        readonly AnimationReadOnlyBuffer<float> m_DenseContributionWeights;

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
            ulong continuityIdentity)
        {
            if (string.IsNullOrWhiteSpace(poseGraphId) || string.IsNullOrWhiteSpace(poseProgramHash) ||
                completionIdentity == 0 || continuityIdentity == 0 ||
                !Enum.IsDefined(typeof(PoseSlotFrameAvailability), availability) ||
                denseContributionWeights.Count != contributions.Count * denseLocalPose.Count ||
                availability == PoseSlotFrameAvailability.Pose && denseLocalPose.Count == 0 ||
                hasFootFeatures && (!leftFootFeatures.IsValid || !rightFootFeatures.IsValid))
            {
                throw new ArgumentException("Final Animation Pose Frame is invalid.");
            }
            PoseGraphId = poseGraphId;
            PoseProgramHash = poseProgramHash;
            CompletionIdentity = completionIdentity;
            Availability = availability;
            DenseLocalPose = denseLocalPose;
            PoseParameters = poseParameters;
            Contributions = contributions;
            m_DenseContributionWeights = denseContributionWeights;
            LeftFootFeatures = leftFootFeatures;
            RightFootFeatures = rightFootFeatures;
            HasFootFeatures = hasFootFeatures;
            ContinuityIdentity = continuityIdentity;
        }

        public string PoseGraphId { get; }
        public string PoseProgramHash { get; }
        public ulong CompletionIdentity { get; }
        public PoseSlotFrameAvailability Availability { get; }
        public AnimationReadOnlyBuffer<AnimationLocalBonePose> DenseLocalPose { get; }
        public AnimationReadOnlyBuffer<float> PoseParameters { get; }
        public AnimationReadOnlyBuffer<AnimationPoseSourceContribution> Contributions { get; }
        public AnimationFootFeatureSample LeftFootFeatures { get; }
        public AnimationFootFeatureSample RightFootFeatures { get; }
        public bool HasFootFeatures { get; }
        public ulong ContinuityIdentity { get; }

        public float GetContributionBoneWeight(int contributionIndex, int boneIndex)
        {
            if ((uint)contributionIndex >= (uint)Contributions.Count || (uint)boneIndex >= (uint)DenseLocalPose.Count)
                throw new ArgumentOutOfRangeException();
            return m_DenseContributionWeights[contributionIndex * DenseLocalPose.Count + boneIndex];
        }

        public float GetBoneOutputWeight(int boneIndex)
        {
            if ((uint)boneIndex >= (uint)DenseLocalPose.Count)
                throw new ArgumentOutOfRangeException(nameof(boneIndex));
            float weight = 0f;
            for (int i = 0; i < Contributions.Count; i++)
                weight += m_DenseContributionWeights[i * DenseLocalPose.Count + boneIndex];
            return UnityEngine.Mathf.Clamp01(weight);
        }
    }
}
