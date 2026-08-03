using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct ResolvedPoseSourceProviderBinding
    {
        public ResolvedPoseSourceProviderBinding(
            int stateMachineIndex,
            PoseStateSourceProviderPlan plan)
        {
            StateMachineIndex = stateMachineIndex;
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            if (!IsValid)
                throw new ArgumentException("Resolved Pose source provider binding is invalid.");
        }

        public int StateMachineIndex { get; }
        public PoseStateSourceProviderPlan Plan { get; }
        public PresentationPoseSourceProviderId ProviderId =>
            Plan?.ProviderId ?? default;
        public PresentationPoseSourceIndex SourceIndex =>
            Plan?.PresentationPoseSourceIndex ?? default;
        public PoseNodeId PlayerNodeId => Plan?.PlayerNodeId ?? default;
        public AnimationPoseSourceKind SourceKind =>
            Plan?.SourceKind ?? default;
        public bool IsValid =>
            StateMachineIndex >= 0 &&
            Plan != null &&
            ProviderId.IsValid &&
            SourceIndex.IsValid &&
            PlayerNodeId.IsValid &&
            (SourceKind == AnimationPoseSourceKind.Sequence ||
             SourceKind == AnimationPoseSourceKind.BlendSpace ||
             SourceKind == AnimationPoseSourceKind.MotionMatching);
    }

    public sealed class PoseSourceProviderBindingIndex
    {
        readonly Dictionary<PresentationPoseSourceProviderId,
            ResolvedPoseSourceProviderBinding> m_ByProvider =
                new Dictionary<PresentationPoseSourceProviderId,
                    ResolvedPoseSourceProviderBinding>();
        readonly Dictionary<PresentationPoseSourceIndex,
            List<ResolvedPoseSourceProviderBinding>> m_BySource =
                new Dictionary<PresentationPoseSourceIndex,
                    List<ResolvedPoseSourceProviderBinding>>();

        PoseSourceProviderBindingIndex(CharacterPresentationProjection projection)
        {
            Projection = projection;
        }

        public CharacterPresentationProjection Projection { get; }
        public IReadOnlyDictionary<PresentationPoseSourceProviderId,
            ResolvedPoseSourceProviderBinding> Bindings => m_ByProvider;

        public bool TryGet(
            PresentationPoseSourceProviderId providerId,
            out ResolvedPoseSourceProviderBinding binding) =>
            m_ByProvider.TryGetValue(providerId, out binding);

        public IReadOnlyList<ResolvedPoseSourceProviderBinding> RequireBySource(
            PresentationPoseSourceIndex sourceIndex) =>
            m_BySource.TryGetValue(
                sourceIndex,
                out List<ResolvedPoseSourceProviderBinding> bindings)
                ? bindings
                : throw new KeyNotFoundException(
                    $"Presentation Pose source index '{sourceIndex}' has no compiled provider.");

        public static PoseSourceProviderBindingIndex Build(
            CharacterPresentationProjection projection)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            projection.RequirePosePayload();
            var result = new PoseSourceProviderBindingIndex(projection);
            var authoredSources = new Dictionary<PresentationPoseSourceIndex,
                AnimationPoseSourceKind>();
            for (int i = 0; i < projection.PoseSources.Count; i++)
            {
                CharacterPresentationPoseSourcePlan source =
                    projection.PoseSources[i];
                source?.RequireValid();
                if (source == null ||
                    !authoredSources.TryAdd(
                        source.SourceIndex,
                        AnimationPoseSourceKind.Sequence))
                {
                    throw new InvalidOperationException(
                        $"Presentation Pose source #{i} is invalid or duplicated.");
                }
            }
            for (int i = 0; i < projection.BlendSpacePlayers.Count; i++)
            {
                CharacterAnimationBlendSpacePlayerPlan player =
                    projection.BlendSpacePlayers[i];
                player?.RequireValid(projection);
                if (player == null ||
                    !authoredSources.TryAdd(
                        player.PresentationPoseSourceIndex,
                        AnimationPoseSourceKind.BlendSpace))
                {
                    throw new InvalidOperationException(
                        $"Blend Space Pose source #{i} is invalid or duplicated.");
                }
            }
            MotionMatchingProjectionPayload motionMatching =
                projection.MotionMatching;
            if (motionMatching != null)
            {
                for (int i = 0; i < motionMatching.ProviderBindingCount; i++)
                {
                    MotionMatchingProviderBindingPayload binding =
                        motionMatching.GetProviderBinding(i);
                    if (!authoredSources.TryAdd(
                            binding.PresentationPoseSourceIndex,
                            AnimationPoseSourceKind.MotionMatching))
                    {
                        throw new InvalidOperationException(
                            $"Motion Matching source index '{binding.PresentationPoseSourceIndex}' is duplicated.");
                    }
                }
            }

            for (int machineIndex = 0;
                 machineIndex < projection.PosePlan.StateMachines.Count;
                 machineIndex++)
            {
                CharacterPoseStateMachineDescriptor machine =
                    projection.PosePlan.StateMachines[machineIndex];
                for (int stateIndex = 0;
                     stateIndex < machine.States.Count;
                     stateIndex++)
                {
                    CharacterPoseStateDescriptor state =
                        machine.States[stateIndex];
                    for (int providerIndex = 0;
                         providerIndex < state.SourceProviders.Count;
                         providerIndex++)
                    {
                        PoseStateSourceProviderPlan plan =
                            state.SourceProviders[providerIndex];
                        if (plan == null ||
                            plan.StateIndex != stateIndex ||
                            !authoredSources.TryGetValue(
                                plan.PresentationPoseSourceIndex,
                                out AnimationPoseSourceKind kind) ||
                            kind != plan.SourceKind)
                        {
                            throw new InvalidOperationException(
                                $"Pose State provider #{machineIndex}/{stateIndex}/{providerIndex} is invalid.");
                        }
                        var resolved =
                            new ResolvedPoseSourceProviderBinding(
                                machineIndex,
                                plan);
                        if (!result.m_ByProvider.TryAdd(
                                resolved.ProviderId,
                                resolved))
                        {
                            throw new InvalidOperationException(
                                $"Pose provider '{resolved.ProviderId}' is duplicated.");
                        }
                        if (!result.m_BySource.TryGetValue(
                                resolved.SourceIndex,
                                out List<ResolvedPoseSourceProviderBinding>
                                    sourceBindings))
                        {
                            sourceBindings =
                                new List<ResolvedPoseSourceProviderBinding>();
                            result.m_BySource.Add(
                                resolved.SourceIndex,
                                sourceBindings);
                        }
                        sourceBindings.Add(resolved);
                    }
                }
            }
            return result;
        }
    }
}
