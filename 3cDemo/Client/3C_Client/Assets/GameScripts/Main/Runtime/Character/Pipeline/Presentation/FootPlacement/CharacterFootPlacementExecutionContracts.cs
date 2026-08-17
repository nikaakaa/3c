using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal enum CharacterFootPlacementResetReason : byte
    {
        Initialization = 1,
        BodyStreamReset = 2,
        PresentationReset = 3,
        MissingAnimationOutput = 4,
        InvalidPose = 5,
        Dispose = 6
    }

    internal readonly struct CharacterFootPlacementReset
    {
        public CharacterFootPlacementReset(
            ActorId actorId,
            ulong renderFrame,
            ulong resetSequence,
            CharacterFootPlacementResetReason reason,
            CharacterBodyPresentationResetReason bodyReason)
        {
            ActorId = actorId;
            RenderFrame = renderFrame;
            ResetSequence = resetSequence;
            Reason = reason;
            BodyReason = bodyReason;
        }

        public ActorId ActorId { get; }
        public ulong RenderFrame { get; }
        public ulong ResetSequence { get; }
        public CharacterFootPlacementResetReason Reason { get; }
        public CharacterBodyPresentationResetReason BodyReason { get; }
    }

    internal readonly struct CharacterFootPlacementPoseInput
    {
        internal CharacterFootPlacementPoseInput(
            string posePlanHash,
            in AnimationPoseValueNativeReadBinding binding,
            AnimationPoseSourceContribution[] contributions,
            int contributionCount)
        {
            int nativeContributionCount = binding.ContributionCount[0];
            if (string.IsNullOrWhiteSpace(posePlanHash) ||
                binding.CompletionIdentity == 0 ||
                binding.DensePoses.Length == 0 ||
                binding.PoseParameters.Length == 0 ||
                binding.PoseParameterAvailability.Length !=
                binding.PoseParameters.Length ||
                binding.Availability[0] != AnimationPoseAvailability.Pose ||
                binding.InvalidReason[0] !=
                AnimationPoseNativeInvalidReason.None ||
                binding.ContinuityIdentity[0] == 0 ||
                binding.HasFootFeatures[0] != 1 ||
                contributions == null || contributionCount <= 0 ||
                contributionCount != nativeContributionCount ||
                contributionCount > contributions.Length)
            {
                throw new ArgumentException(
                    "Foot Placement upstream Component Pose input is invalid.");
            }
            PosePlanHash = posePlanHash;
            CompletionIdentity = binding.CompletionIdentity;
            DenseComponentPoses = binding.DensePoses;
            PoseParameters = binding.PoseParameters;
            PoseParameterAvailability =
                binding.PoseParameterAvailability;
            AnimationFootFeatureSample left = binding.LeftFootFeatures[0];
            AnimationFootFeatureSample right = binding.RightFootFeatures[0];
            if (!left.IsValid || !right.IsValid)
                throw new ArgumentException("Foot Placement feature input is invalid.");
            LeftFootSteps = new AnimationBiomechanicalStepReadPage(
                in left,
                CharacterFootSide.Left);
            RightFootSteps = new AnimationBiomechanicalStepReadPage(
                in right,
                CharacterFootSide.Right);
            ContinuityIdentity = binding.ContinuityIdentity[0];
            Contributions = contributions;
            ContributionCount = contributionCount;
        }

        internal string PosePlanHash { get; }
        internal ulong CompletionIdentity { get; }
        internal NativeSlice<AnimationLocalBonePose> DenseComponentPoses { get; }
        internal NativeSlice<float> PoseParameters { get; }
        internal NativeSlice<byte> PoseParameterAvailability { get; }
        internal AnimationBiomechanicalStepReadPage LeftFootSteps { get; }
        internal AnimationBiomechanicalStepReadPage RightFootSteps { get; }
        internal ulong ContinuityIdentity { get; }
        internal AnimationPoseSourceContribution[] Contributions { get; }
        internal int ContributionCount { get; }
    }

}
