using ThirdPersonAction;
using ThirdPersonAnimation;

namespace ThirdPersonCharacterStateMachine
{
    public static class CharacterStateTimelineFactSampler
    {
        public static StateTimelineWindowFacts SampleCurrent(
            CharacterStateMachineDefinition definition,
            in CharacterStateMachineSnapshot snapshot,
            in CharacterStateMachineContext context,
            float elapsedSeconds,
            ActionRequestType requestType)
        {
            if (definition == null ||
                !snapshot.ActiveState.IsValid ||
                !definition.TryGetNode(snapshot.ActiveState, out CharacterStateNodeDefinition node))
            {
                return StateTimelineSampler.None(snapshot.ActiveState);
            }

            return SampleCurrent(
                definition,
                node,
                snapshot.ActiveState,
                snapshot.Variant,
                in context,
                elapsedSeconds,
                requestType);
        }

        public static StateTimelineWindowFacts SampleCurrent(
            CharacterStateMachineDefinition definition,
            CharacterStateNodeDefinition currentNode,
            CharacterStateId currentState,
            CharacterStateVariant currentVariant,
            in CharacterStateMachineContext context,
            float elapsedSeconds,
            ActionRequestType requestType)
        {
            if (definition == null || !definition.TryGetTimelinePolicy(currentState, out StateTimelinePolicyDefinition policy))
                return StateTimelineSampler.None(currentState);

            ResolveCurrentPlaybackProgress(
                currentNode,
                currentState,
                currentVariant,
                in context,
                out float normalizedTime,
                out bool hasValidNormalizedTime);
            StateTimelineWindowFacts facts = StateTimelineSampler.Sample(
                in policy,
                normalizedTime,
                hasValidNormalizedTime,
                elapsedSeconds,
                requestType);
            return facts;
        }

        static void ResolveCurrentPlaybackProgress(
            CharacterStateNodeDefinition currentNode,
            CharacterStateId currentState,
            CharacterStateVariant currentVariant,
            in CharacterStateMachineContext context,
            out float normalizedTime,
            out bool hasValidNormalizedTime)
        {
            normalizedTime = 0f;
            hasValidNormalizedTime = false;
            if (currentNode == null ||
                !currentNode.TryResolveAnimationBinding(
                    currentVariant,
                    out CharacterStateAnimationBinding binding,
                    out CharacterStatePlaybackFactSource playbackFactSource))
            {
                return;
            }

            CharacterRuntimeAnimationFacts animation = context.RuntimeBlackboard.Animation;
            if (playbackFactSource == CharacterStatePlaybackFactSource.Locomotion)
            {
                AnimationPhasePlaybackProgress progress = animation.LocomotionProgress;
                if (!binding.HasKey || !progress.HasValidPlayback || progress.AliasKey != binding.TimelineBindingKey)
                    return;

                normalizedTime = progress.NormalizedTime;
                hasValidNormalizedTime = true;
                return;
            }

            if (playbackFactSource != CharacterStatePlaybackFactSource.Action)
                return;

            ActionAnimationPlaybackProgress actionProgress = animation.ActionProgress;
            if (!binding.HasKey || !actionProgress.HasValidPlayback || actionProgress.Key != binding.Key)
                return;

            normalizedTime = actionProgress.NormalizedTime;
            hasValidNormalizedTime = true;
        }
    }
}
