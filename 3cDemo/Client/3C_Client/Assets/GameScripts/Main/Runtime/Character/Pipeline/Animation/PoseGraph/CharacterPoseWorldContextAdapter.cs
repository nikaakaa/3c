using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal readonly struct CharacterPoseFootMotionSource
    {
        internal CharacterPoseFootMotionSource(
            string sourceIdentity,
            ulong sourceSampleIdentity,
            AnimationFootStepObservationCurvePair observation)
        {
            if (string.IsNullOrWhiteSpace(sourceIdentity) ||
                sourceSampleIdentity == 0 || observation == null)
            {
                throw new ArgumentException(
                    "Pose Foot Motion source metadata is invalid.");
            }
            SourceIdentity = sourceIdentity.Trim();
            SourceSampleIdentity = sourceSampleIdentity;
            Observation = observation;
        }

        internal string SourceIdentity { get; }
        internal ulong SourceSampleIdentity { get; }
        internal AnimationFootStepObservationCurvePair Observation { get; }
    }

    internal sealed class CharacterPoseWorldContextAdapter
    {
        readonly string m_PosePlanHash;
        readonly CharacterPoseSourceModule m_SourceModule;
        readonly PoseNodeId[] m_PlayerNodeIds;
        readonly Func<AnimationPoseSourceContribution, ClipSamplePlan,
            CharacterPoseFootMotionSource> m_FootMotionResolver;
        readonly AnimationPoseSourceContribution[] m_Contributions;
        AnimationFootMotionRuntimeFrame m_LastSampledFootMotion;
        bool m_HasLastSampledFootMotion;

        internal CharacterPoseWorldContextAdapter(
            string posePlanHash,
            CharacterPoseSourceModule sourceModule,
            IReadOnlyList<PoseNodeId> playerNodeIds,
            Func<AnimationPoseSourceContribution, ClipSamplePlan,
                CharacterPoseFootMotionSource> footMotionResolver,
            int contributionCapacity)
        {
            if (string.IsNullOrWhiteSpace(posePlanHash))
                throw new ArgumentException(
                    "Pose Foot Placement graph identity is missing.",
                    nameof(posePlanHash));
            m_SourceModule = sourceModule ??
                throw new ArgumentNullException(nameof(sourceModule));
            if (playerNodeIds == null || playerNodeIds.Count == 0)
                throw new ArgumentException(
                    "Pose Foot Placement player identities are missing.",
                    nameof(playerNodeIds));
            m_PlayerNodeIds = new PoseNodeId[playerNodeIds.Count];
            for (int i = 0; i < m_PlayerNodeIds.Length; i++)
            {
                if (!playerNodeIds[i].IsValid)
                    throw new ArgumentException(
                        "Pose Foot Placement player identity is invalid.",
                        nameof(playerNodeIds));
                m_PlayerNodeIds[i] = playerNodeIds[i];
            }
            m_FootMotionResolver = footMotionResolver ??
                throw new ArgumentNullException(nameof(footMotionResolver));
            if (contributionCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(contributionCapacity));
            m_PosePlanHash = posePlanHash.Trim();
            m_Contributions = new AnimationPoseSourceContribution[
                contributionCapacity];
        }

        internal CharacterFootPlacementFrameInput BuildFootPlacement(
            ActorId actorId,
            ulong renderFrame,
            float presentationDeltaSeconds,
            in CharacterBodyPresentationFrame bodyFrame,
            in CharacterPresentationFactFrame factFrame,
            in CharacterAnimationPoseInputFrame parameterFrame,
            ulong completionIdentity,
            in AnimationPoseValueNativeReadBinding inputBinding,
            int parameterIndex)
        {
            if (inputBinding.ContributionCount.Length != 1 ||
                inputBinding.ContributionCount[0] <= 0 ||
                inputBinding.ContributionCount[0] > m_Contributions.Length)
            {
                throw new InvalidOperationException(
                    "Foot Placement Component Pose has no resolvable contributions.");
            }
            int contributionCount = ResolveContributions(in inputBinding);
            m_HasLastSampledFootMotion = false;
            AnimationPoseSourceContribution contribution =
                RequireFootMotionContribution(contributionCount);
            AnimationFootMotionRuntimeFrame footMotion = SampleFootMotion(in contribution, completionIdentity);
            var pose = new CharacterFootPlacementPoseInput(
                m_PosePlanHash,
                in inputBinding,
                in footMotion,
                m_Contributions,
                contributionCount);
            if ((uint)parameterIndex >=
                    (uint)inputBinding.PoseParameters.Length ||
                inputBinding.PoseParameterAvailability[parameterIndex] == 0 ||
                !float.IsFinite(inputBinding.PoseParameters[parameterIndex]))
            {
                throw new InvalidOperationException(
                    "Foot Placement input Pose curve is unavailable.");
            }
            return new CharacterFootPlacementFrameInput(
                actorId,
                renderFrame,
                presentationDeltaSeconds,
                inputBinding.PoseParameters[parameterIndex],
                bodyFrame,
                in factFrame,
                in parameterFrame,
                in pose);
        }

        internal AnimationFootMotionRuntimeFrame LastSampledFootMotion =>
            m_HasLastSampledFootMotion
                ? m_LastSampledFootMotion
                : throw new InvalidOperationException("Pose Foot Motion was not sampled at the evaluation barrier.");

        internal AnimationFootMotionRuntimeFrame SampleFootMotion(
            in AnimationPoseSourceContribution contribution,
            ulong completionIdentity)
        {
            ClipSamplePlan clipSample = m_SourceModule.RequireDominantClipSample(
                contribution.SourceId,
                contribution.NodeId,
                completionIdentity);
            CharacterPoseFootMotionSource source = m_FootMotionResolver(
                contribution,
                clipSample);
            int cycle = checked((int)Math.Floor(
                clipSample.ContinuousClipTime / clipSample.DurationSeconds));
            AnimationFootMotionRuntimeFrame result = new AnimationFootMotionRuntimeFrame(
                    completionIdentity,
                    contribution.NodeId,
                    contribution.SourceId,
                    contribution.ContributionContinuityIdentity,
                    source.SourceIdentity,
                    source.SourceSampleIdentity,
                    clipSample.ClipBindingIndex,
                    cycle,
                    contribution.Weight,
                    clipSample.NormalizedTime,
                    source.Observation.Left.Sample(
                        clipSample.NormalizedTime,
                        cycle,
                        clipSample.DurationSeconds,
                        clipSample.IsLooping),
                    source.Observation.Right.Sample(
                        clipSample.NormalizedTime,
                        cycle,
                        clipSample.DurationSeconds,
                        clipSample.IsLooping));
            m_LastSampledFootMotion = result;
            m_HasLastSampledFootMotion = true;
            return result;
        }

        int ResolveContributions(
            in AnimationPoseValueNativeReadBinding inputBinding)
        {
            int count = inputBinding.ContributionCount[0];
            for (int i = 0; i < count; i++)
            {
                m_Contributions[i] = CharacterFinalPoseContributionResolver.Resolve(
                    inputBinding.Contributions[i],
                    m_SourceModule,
                    m_PlayerNodeIds);
            }
            return count;
        }

        AnimationPoseSourceContribution RequireFootMotionContribution(
            int contributionCount)
        {
            AnimationPoseSourceContribution selected = default;
            float selectedWeight = -1f;
            for (int i = 0; i < contributionCount; i++)
            {
                AnimationPoseSourceContribution candidate = m_Contributions[i];
                if (candidate.Kind != AnimationPoseContributionKind.Live ||
                    candidate.Weight <= selectedWeight)
                {
                    continue;
                }
                selected = candidate;
                selectedWeight = candidate.Weight;
            }
            if (!selected.SourceId.IsValid)
                throw new InvalidOperationException(
                    "Foot Placement has no Live Foot Motion source.");
            return selected;
        }
    }
}
