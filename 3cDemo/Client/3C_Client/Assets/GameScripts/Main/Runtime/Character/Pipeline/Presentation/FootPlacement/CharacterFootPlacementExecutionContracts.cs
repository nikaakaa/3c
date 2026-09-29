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
        readonly AnimationFootMotionRuntimeFrame m_FootMotion;

        internal CharacterFootPlacementPoseInput(
            string posePlanHash,
            in AnimationPoseValueNativeReadBinding binding,
            in AnimationFootMotionRuntimeFrame footStepObservation,
            AnimationPoseSourceContribution[] contributions,
            int contributionCount)
        {
            int nativeContributionCount = binding.ContributionCount[0];
            if (string.IsNullOrWhiteSpace(posePlanHash) ||
                binding.CompletionIdentity == 0 ||
                binding.DensePoses.Length == 0 ||
                binding.Availability[0] != AnimationPoseAvailability.Pose ||
                binding.InvalidReason[0] !=
                AnimationPoseNativeInvalidReason.None ||
                binding.ContinuityIdentity[0] == 0 ||
                !footStepObservation.IsValid ||
                footStepObservation.CompletionIdentity != binding.CompletionIdentity ||
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
            ContinuityIdentity = binding.ContinuityIdentity[0];
            m_FootMotion = footStepObservation;
            Contributions = contributions;
            ContributionCount = contributionCount;
            bool hasObservationContribution = false;
            for (int i = 0; i < contributionCount; i++)
            {
                ref readonly AnimationPoseSourceContribution contribution = ref contributions[i];
                if (contribution.Kind != AnimationPoseContributionKind.Live ||
                    !contribution.NodeIdRef.Equals(footStepObservation.NodeIdRef) ||
                    !contribution.SourceIdRef.Equals(footStepObservation.SourceIdRef) ||
                    contribution.ContributionContinuityIdentity !=
                        footStepObservation.ContributionContinuityIdentity)
                {
                    continue;
                }
                hasObservationContribution = true;
                break;
            }
            if (!hasObservationContribution)
            {
                throw new ArgumentException(
                    "Foot Placement formal Foot Step input does not belong to its Pose contribution frame.");
            }
        }

        internal string PosePlanHash { get; }
        internal ulong CompletionIdentity { get; }
        internal NativeSlice<AnimationLocalBonePose> DenseComponentPoses { get; }
        internal ulong ContinuityIdentity { get; }
        internal ref readonly AnimationFootMotionRuntimeFrame FootMotion => ref m_FootMotion;
        internal AnimationPoseSourceContribution[] Contributions { get; }
        internal int ContributionCount { get; }
    }

    internal readonly struct CharacterFootPlacementResult
    {
        readonly CharacterResolvedFootPair m_Feet;
        readonly CharacterFootPrimarySupportResult m_PrimarySupport;
        readonly CharacterFootStrideHipsResult m_Pelvis;
        readonly CharacterFullBodyIkGoal m_PelvisGoal;
        readonly CharacterFullBodyIkGoal m_LeftGoal;
        readonly CharacterFullBodyIkGoal m_RightGoal;

        internal CharacterFootPlacementResult(
            ulong frameSequence,
            ulong completionIdentity,
            FixedString64Bytes rigId,
            FixedString64Bytes rigRevision,
            in CharacterResolvedFootPair feet,
            in CharacterFootPrimarySupportResult primarySupport,
            in CharacterFootStrideHipsResult pelvis,
            CharacterFullBodyIkGoal pelvisGoal,
            CharacterFullBodyIkGoal leftGoal,
            CharacterFullBodyIkGoal rightGoal)
        {
            if (frameSequence == 0 || completionIdentity == 0 ||
                rigId.Length == 0 || rigRevision.Length == 0 ||
                feet.FrameSequence != frameSequence ||
                feet.CompletionIdentity != completionIdentity ||
                !feet.RigId.Equals(rigId) ||
                !feet.RigRevision.Equals(rigRevision) ||
                !pelvisGoal.IsValid || !leftGoal.IsValid || !rightGoal.IsValid)
            {
                throw new ArgumentException("Foot Placement Result is invalid.");
            }
            FrameSequence = frameSequence;
            CompletionIdentity = completionIdentity;
            RigId = rigId;
            RigRevision = rigRevision;
            m_Feet = feet;
            m_PrimarySupport = primarySupport;
            m_Pelvis = pelvis;
            m_PelvisGoal = pelvisGoal;
            m_LeftGoal = leftGoal;
            m_RightGoal = rightGoal;
        }

        internal ulong FrameSequence { get; }
        internal ulong CompletionIdentity { get; }
        internal FixedString64Bytes RigId { get; }
        internal FixedString64Bytes RigRevision { get; }
        internal ref readonly CharacterResolvedFootPair Feet => ref m_Feet;
        internal ref readonly CharacterFootPrimarySupportResult PrimarySupport =>
            ref m_PrimarySupport;
        internal ref readonly CharacterFootStrideHipsResult Pelvis => ref m_Pelvis;
        internal ref readonly CharacterFullBodyIkGoal PelvisGoal => ref m_PelvisGoal;
        internal ref readonly CharacterFullBodyIkGoal LeftGoal => ref m_LeftGoal;
        internal ref readonly CharacterFullBodyIkGoal RightGoal => ref m_RightGoal;
    }

}
