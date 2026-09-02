using System;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseWorldContextAdapter
    {
        readonly CharacterPresentationProjection m_Projection;
        readonly CharacterPoseSourceModule m_SourceModule;
        readonly CharacterFinalPosePublication m_FinalPublication;
        readonly AnimationPoseSourceContribution[] m_Contributions;

        internal CharacterPoseWorldContextAdapter(
            CharacterPresentationProjection projection,
            CharacterPoseSourceModule sourceModule,
            CharacterFinalPosePublication finalPublication)
        {
            m_Projection = projection ??
                throw new ArgumentNullException(nameof(projection));
            m_SourceModule = sourceModule ??
                throw new ArgumentNullException(nameof(sourceModule));
            m_FinalPublication = finalPublication ??
                throw new ArgumentNullException(nameof(finalPublication));
            m_Contributions = new AnimationPoseSourceContribution[
                projection.PosePlan.ContributionCapacity];
        }

        internal CharacterFootPlacementFrameInput BuildFootPlacement(
            ActorId actorId,
            ulong renderFrame,
            float presentationDeltaSeconds,
            in CharacterBodyPresentationFrame bodyFrame,
            in CharacterPresentationFactFrame factFrame,
            ulong completionIdentity,
            in AnimationPoseValueNativeReadBinding inputBinding,
            int parameterIndex)
        {
            int contributionCount =
                m_FinalPublication.ResolveContributions(
                    in inputBinding,
                    m_SourceModule,
                    m_Contributions);
            AnimationFootMotionRuntimeFrame footStepObservation =
                ResolveFootStepObservationFrame(
                    completionIdentity,
                    m_Contributions,
                    contributionCount);
            var input = new CharacterFootPlacementPoseInput(
                m_Projection.PosePlan.PlanHash,
                in inputBinding,
                in footStepObservation,
                m_Contributions,
                contributionCount);
            if ((uint)parameterIndex >=
                    (uint)inputBinding.PoseParameters.Length ||
                inputBinding.PoseParameterAvailability[parameterIndex] == 0 ||
                !float.IsFinite(inputBinding.PoseParameters[parameterIndex]))
            {
                throw new InvalidOperationException(
                    "Foot Placement parameter input is unavailable.");
            }
            var planningFrame = new CharacterFootPlacementFrameInput(
                actorId,
                renderFrame,
                presentationDeltaSeconds,
                inputBinding.PoseParameters[parameterIndex],
                bodyFrame,
                in factFrame,
                in input);
            return planningFrame;
        }

        AnimationFootMotionRuntimeFrame ResolveFootStepObservationFrame(
            ulong completionIdentity,
            AnimationPoseSourceContribution[] contributions,
            int contributionCount)
        {
            AnimationPoseSourceContribution contribution =
                RequireFootStepObservationContribution(
                    contributions,
                    contributionCount);
            ClipSamplePlan clipSample = m_SourceModule.RequireDominantClipSample(
                contribution.SourceId,
                contribution.NodeId,
                completionIdentity);
            AnimationFootStepObservationCurvePair curves;
            string sourceIdentity;
            ulong sourceSampleIdentity;
            switch (contribution.SourceId.SourceKind)
            {
                case AnimationPoseSourceKind.Timeline:
                    if ((uint)contribution.SourceOwnerIndex >=
                        (uint)m_Projection.Producers.Count)
                    {
                        throw new InvalidOperationException(
                            $"Timeline Foot Step source '{contribution.SourceId}' has no exact producer.");
                    }
                    CharacterPresentationAnimationBinding animation =
                        m_Projection.Producers[contribution.SourceOwnerIndex]
                            ?.Animation ??
                        throw new InvalidOperationException(
                            $"Timeline Foot Step source '{contribution.SourceId}' has no animation binding.");
                    if ((uint)clipSample.ClipBindingIndex >=
                        (uint)animation.Clips.Count)
                    {
                        throw new InvalidOperationException(
                            $"Timeline Foot Step source '{contribution.SourceId}' Clip binding is outside its producer catalog.");
                    }
                    CharacterPresentationAnimationClipBinding binding =
                        animation.Clips[clipSample.ClipBindingIndex] ??
                        throw new InvalidOperationException(
                            $"Timeline Foot Step source '{contribution.SourceId}' Clip binding is missing.");
                    binding.RequireSampleable(clipSample.ClipBindingIndex);
                    if (!ReferenceEquals(binding.Clip, clipSample.Clip))
                    {
                        throw new InvalidOperationException(
                            $"Timeline Foot Step source '{contribution.SourceId}' Clip sample does not match its compiled binding.");
                    }
                    curves = binding.FootStepObservation;
                    sourceIdentity = binding.ClipIdentity;
                    sourceSampleIdentity = AnimationFootMotionIdentity.Source(
                        binding.ClipAuthoringId);
                    break;
                case AnimationPoseSourceKind.Clip:
                    if (!m_Projection.TryGetPoseSource(
                            contribution.SourceId.PresentationPoseSourceIndex,
                            out CharacterPresentationPoseSourcePlan source) ||
                        clipSample.ClipBindingIndex != 0 ||
                        !ReferenceEquals(source.Clip, clipSample.Clip))
                    {
                        throw new InvalidOperationException(
                            $"Clip Foot Step source '{contribution.SourceId}' does not match its compiled source plan.");
                    }
                    source.RequireValid();
                    curves = source.FootStepObservation;
                    sourceIdentity = source.ClipIdentity;
                    sourceSampleIdentity = AnimationFootMotionIdentity.Source(
                        contribution.SourceId);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Foot Step source kind '{contribution.SourceId.SourceKind}' has no formal observation contract.");
            }
            curves.RequireValid();
            int cycle = checked((int)Math.Floor(
                clipSample.ContinuousClipTime / clipSample.Clip.length));
            return new AnimationFootMotionRuntimeFrame(
                completionIdentity,
                contribution.NodeId,
                contribution.SourceId,
                contribution.ContributionContinuityIdentity,
                sourceIdentity,
                sourceSampleIdentity,
                clipSample.ClipBindingIndex,
                cycle,
                contribution.Weight,
                clipSample.NormalizedTime,
                curves.Left.Sample(
                    clipSample.NormalizedTime,
                    cycle,
                    clipSample.Clip.length,
                    clipSample.Clip.isLooping),
                curves.Right.Sample(
                    clipSample.NormalizedTime,
                    cycle,
                    clipSample.Clip.length,
                    clipSample.Clip.isLooping));
        }

        static AnimationPoseSourceContribution
            RequireFootStepObservationContribution(
                AnimationPoseSourceContribution[] contributions,
                int contributionCount)
        {
            if (contributions == null || contributionCount <= 0 ||
                contributionCount > contributions.Length)
            {
                throw new ArgumentException(
                    "Foot Step observation contribution input is invalid.");
            }
            AnimationPoseSourceContribution selected = default;
            float selectedWeight = -1f;
            for (int i = 0; i < contributionCount; i++)
            {
                AnimationPoseSourceContribution candidate = contributions[i];
                if (candidate.Kind != AnimationPoseContributionKind.Live ||
                    candidate.Weight <= selectedWeight)
                {
                    continue;
                }
                selected = candidate;
                selectedWeight = candidate.Weight;
            }
            if (!selected.SourceId.IsValid)
            {
                throw new InvalidOperationException(
                    "Foot Placement has no Live Foot Step observation source.");
            }
            return selected;
        }
    }
}
