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
                !binding.LeftFootFeatures[0].IsValid ||
                !binding.RightFootFeatures[0].IsValid ||
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
            LeftFootFeatures = binding.LeftFootFeatures[0];
            RightFootFeatures = binding.RightFootFeatures[0];
            ContinuityIdentity = binding.ContinuityIdentity[0];
            Contributions = contributions;
            ContributionCount = contributionCount;
        }

        internal string PosePlanHash { get; }
        internal ulong CompletionIdentity { get; }
        internal NativeSlice<AnimationLocalBonePose> DenseComponentPoses { get; }
        internal NativeSlice<float> PoseParameters { get; }
        internal NativeSlice<byte> PoseParameterAvailability { get; }
        internal AnimationFootFeatureSample LeftFootFeatures { get; }
        internal AnimationFootFeatureSample RightFootFeatures { get; }
        internal ulong ContinuityIdentity { get; }
        internal AnimationPoseSourceContribution[] Contributions { get; }
        internal int ContributionCount { get; }
    }

    internal readonly struct CharacterFootPlacementPlanningFrame
    {
        public CharacterFootPlacementPlanningFrame(
            ActorId actorId,
            ulong renderFrame,
            float presentationDeltaSeconds,
            CharacterBodyPresentationFrame body,
            in CharacterPresentationFactFrame facts,
            in CharacterFootPlacementPoseInput upstreamPose)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Foot Placement Actor identity is invalid.", nameof(actorId));
            if (renderFrame == 0)
                throw new ArgumentOutOfRangeException(nameof(renderFrame));
            if (!float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(presentationDeltaSeconds));
            if (upstreamPose.CompletionIdentity == 0)
                throw new ArgumentException("Foot Placement upstream completion identity is invalid.", nameof(upstreamPose));
            if (!facts.IsValid)
                throw new ArgumentException("Foot Placement Presentation Facts are invalid.", nameof(facts));
            ActorId = actorId;
            RenderFrame = renderFrame;
            PresentationDeltaSeconds = presentationDeltaSeconds;
            Body = body;
            LocomotionMotionTimeline = facts.LocomotionMotionTimeline;
            MovementPlaybackTime = facts.MovementPlaybackTime;
            MotionPhase = facts.MotionPhase;
            UpstreamPose = upstreamPose;
        }

        public ActorId ActorId { get; }
        public ulong RenderFrame { get; }
        public ulong CompletionIdentity => UpstreamPose.CompletionIdentity;
        public float PresentationDeltaSeconds { get; }
        public CharacterBodyPresentationFrame Body { get; }
        public CommittedLocomotionPlanarMotionTimeline LocomotionMotionTimeline { get; }
        public double MovementPlaybackTime { get; }
        public CharacterPresentationMotionPhase MotionPhase { get; }
        internal CharacterFootPlacementPoseInput UpstreamPose { get; }
    }
}
