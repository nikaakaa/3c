using System;
using System.Collections.Generic;
using Unity.Collections;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal delegate CharacterPoseFootMotionSource CharacterPoseFootMotionResolver(
        in AnimationPoseSourceId sourceId,
        in ClipSamplePlan clipSample);

    internal readonly struct CharacterPoseFootMotionSource
    {
        internal CharacterPoseFootMotionSource(
            int sourceNameIndex,
            ulong sourceSampleIdentity,
            AnimationFootStepObservationCurvePair observation)
        {
            if (sourceNameIndex < 0 ||
                sourceSampleIdentity == 0 || observation == null)
            {
                throw new ArgumentException(
                    "Pose Foot Motion source metadata is invalid.");
            }
            SourceNameIndex = sourceNameIndex;
            SourceSampleIdentity = sourceSampleIdentity;
            Observation = observation;
        }

        internal int SourceNameIndex { get; }
        internal ulong SourceSampleIdentity { get; }
        internal AnimationFootStepObservationCurvePair Observation { get; }
    }

    internal sealed class CharacterPoseWorldContextAdapter
    {
        readonly string m_PosePlanHash;
        readonly CharacterPoseSourceModule m_SourceModule;
        readonly PoseNodeId[] m_PlayerNodeIds;
        readonly Func<int, string> m_ResolveFootMotionSourceName;
        readonly AnimationPoseSourceContribution[] m_Contributions;
        AnimationFootMotionRuntimeFrame m_LastSampledFootMotion;
        bool m_HasLastSampledFootMotion;

        internal CharacterPoseWorldContextAdapter(
            string posePlanHash,
            CharacterPoseSourceModule sourceModule,
            IReadOnlyList<PoseNodeId> playerNodeIds,
            Func<int, string> resolveFootMotionSourceName,
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
            if (contributionCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(contributionCapacity));
            m_PosePlanHash = posePlanHash.Trim();
            m_ResolveFootMotionSourceName = resolveFootMotionSourceName;
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
            NativeSlice<int> contributionCounts =
                inputBinding.ContributionCount;
            if (contributionCounts.Length != 1 ||
                contributionCounts[0] <= 0 ||
                contributionCounts[0] > m_Contributions.Length)
            {
                throw new InvalidOperationException(
                    "Foot Placement Component Pose has no resolvable contributions.");
            }
            int contributionCount = contributionCounts[0];
            NativeSlice<float> poseParameters = inputBinding.PoseParameters;
            NativeSlice<byte> parameterAvailability =
                inputBinding.PoseParameterAvailability;
            ResolveContributions(in inputBinding, contributionCount);
            m_HasLastSampledFootMotion = false;
            int selectedContribution =
                RequireFootMotionContribution(m_Contributions, contributionCount);
            ref readonly AnimationPoseSourceContribution contribution = ref m_Contributions[selectedContribution];
            AnimationPrimitivePoseContribution primitive = inputBinding.Contributions[selectedContribution];
            AnimationFootMotionRuntimeFrame footMotion = SampleFootMotion(
                in contribution, in primitive.FootMotion, completionIdentity);
            var pose = new CharacterFootPlacementPoseInput(
                m_PosePlanHash,
                in inputBinding,
                in footMotion,
                m_Contributions,
                contributionCount);
            if ((uint)parameterIndex >=
                    (uint)poseParameters.Length ||
                parameterAvailability[parameterIndex] == 0 ||
                !float.IsFinite(poseParameters[parameterIndex]))
            {
                throw new InvalidOperationException(
                    "Foot Placement input Pose curve is unavailable.");
            }
            return new CharacterFootPlacementFrameInput(
                actorId,
                renderFrame,
                presentationDeltaSeconds,
                poseParameters[parameterIndex],
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
            in AnimationFootMotionSourceSample sample,
            ulong completionIdentity)
        {
            ref readonly AnimationPoseSourceId sourceId = ref contribution.SourceIdRef;
            ref readonly PoseNodeId nodeId = ref contribution.NodeIdRef;
            AnimationFootMotionRuntimeFrame result = new AnimationFootMotionRuntimeFrame(
                    completionIdentity,
                    nodeId,
                    sourceId,
                    contribution.Kind,
                    contribution.ContributionContinuityIdentity,
                    contribution.Weight,
                    m_ResolveFootMotionSourceName(sample.SourceNameIndex),
                    in sample);
            m_LastSampledFootMotion = result;
            m_HasLastSampledFootMotion = true;
            return result;
        }

        void ResolveContributions(
            in AnimationPoseValueNativeReadBinding inputBinding,
            int count)
        {
            NativeSlice<AnimationPrimitivePoseContribution> primitives =
                inputBinding.Contributions;
            for (int i = 0; i < count; i++)
            {
                AnimationPrimitivePoseContribution primitive = primitives[i];
                m_Contributions[i] = CharacterFinalPoseContributionResolver.Resolve(
                    in primitive,
                    m_SourceModule,
                    m_PlayerNodeIds);
            }
        }

        internal static int RequireFootMotionContribution(
            AnimationPoseSourceContribution[] contributions,
            int contributionCount)
        {
            int selected = -1;
            float selectedWeight = 0f;
            for (int i = 0; i < contributionCount; i++)
            {
                ref readonly AnimationPoseSourceContribution candidate = ref contributions[i];
                if (candidate.Weight <= selectedWeight)
                {
                    continue;
                }
                selected = i;
                selectedWeight = candidate.Weight;
            }
            if (selected < 0)
                throw new InvalidOperationException(
                    "Foot Placement has no contributing Foot Motion sample.");
            return selected;
        }
    }
}
