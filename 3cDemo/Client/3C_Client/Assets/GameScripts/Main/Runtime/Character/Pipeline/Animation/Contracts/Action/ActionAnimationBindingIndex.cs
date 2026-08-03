using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Timeline;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct ResolvedActionAnimationBinding
    {
        public ResolvedActionAnimationBinding(
            ActionPlaybackInputPlan input,
            AnimationProducerId producerId,
            TimelinePlaybackMode playbackMode,
            CharacterPresentationAnimationBinding animation)
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
            ProducerId = producerId;
            PlaybackMode = playbackMode;
            Animation = animation ?? throw new ArgumentNullException(nameof(animation));
            if (!IsValid)
                throw new ArgumentException("Resolved Action animation binding is invalid.");
        }

        public ActionPlaybackInputPlan Input { get; }
        public int ProgramProducerIndex => Input?.ProgramProducerIndex ?? -1;
        public string ProgramProducerId => Input?.ProgramProducerId ?? string.Empty;
        public AnimationProducerId ProducerId { get; }
        public TimelinePlaybackMode PlaybackMode { get; }
        public AnimationChannelId AnimationChannelId =>
            Input?.AnimationChannelId ?? default;
        public AnimationSlotId SlotId => Input?.SlotId ?? default;
        public PoseNodeId SlotNodeId =>
            Input?.SlotNodeId ?? default;
        public PoseNodeId ActionPlayerNodeId =>
            Input?.ActionPlayerNodeId ?? default;
        public CharacterPresentationAnimationBinding Animation { get; }
        public TransitionAssetBase Source => Animation?.Source;
        public int AuthoredClipCount => Animation?.Clips.Count ?? 0;
        public bool UsesMixer => AuthoredClipCount > 1;
        public bool IsValid =>
            Input != null &&
            ProducerId.IsValid &&
            Enum.IsDefined(typeof(TimelinePlaybackMode), PlaybackMode) &&
            Animation != null &&
            Source &&
            Source.IsValid &&
            AuthoredClipCount > 0 &&
            float.IsFinite(Animation.DurationSeconds) &&
            Animation.DurationSeconds > 0f &&
            float.IsFinite(Animation.LastSampleTimeSeconds) &&
            Animation.LastSampleTimeSeconds > 0f &&
            Animation.LastSampleTimeSeconds <=
            Animation.DurationSeconds;
    }

    public sealed class ActionAnimationBindingIndex
    {
        readonly Dictionary<AnimationProducerId, ResolvedActionAnimationBinding>
            m_ByProducerId =
                new Dictionary<AnimationProducerId, ResolvedActionAnimationBinding>();
        readonly Dictionary<string, ResolvedActionAnimationBinding>
            m_ByProgramProducerId =
                new Dictionary<string, ResolvedActionAnimationBinding>(
                    StringComparer.Ordinal);

        ActionAnimationBindingIndex(CharacterPresentationProjection projection)
        {
            Projection = projection;
        }

        public CharacterPresentationProjection Projection { get; }
        public IReadOnlyDictionary<AnimationProducerId, ResolvedActionAnimationBinding>
            Bindings => m_ByProducerId;

        public bool TryGet(
            AnimationProducerId producerId,
            out ResolvedActionAnimationBinding binding) =>
            m_ByProducerId.TryGetValue(producerId, out binding);

        public bool TryGet(
            string programProducerId,
            out ResolvedActionAnimationBinding binding) =>
            m_ByProgramProducerId.TryGetValue(
                programProducerId ?? string.Empty,
                out binding);

        public static ActionAnimationBindingIndex Build(
            CharacterPresentationProjection projection,
            CharacterPresentationSemanticContract contract)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            projection.RequireContract(contract);
            projection.RequirePosePayload();

            var result = new ActionAnimationBindingIndex(projection);
            var inputs = new Dictionary<string, ActionPlaybackInputPlan>(
                StringComparer.Ordinal);
            for (int i = 0; i < projection.PosePlan.ActionPlaybackInputs.Count; i++)
            {
                ActionPlaybackInputPlan input =
                    projection.PosePlan.ActionPlaybackInputs[i];
                input?.RequireValid();
                if (input == null ||
                    !inputs.TryAdd(input.ProgramProducerId, input))
                {
                    throw new InvalidOperationException(
                        $"Action Playback input #{i} is invalid or duplicated.");
                }
            }

            for (int i = 0; i < projection.Producers.Count; i++)
            {
                CharacterPresentationProducerEntry producer =
                    projection.Producers[i];
                if (producer == null ||
                    producer.ProgramProducerIndex != i ||
                    i >= contract.Producers.Count ||
                    !string.Equals(
                        producer.ProgramProducerIdentity,
                        contract.Producers[i].Identity,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Presentation producer #{i} does not match the Program manifest.");
                }
                if (producer.Kind !=
                    CharacterPresentationProducerKind.Animation)
                {
                    continue;
                }
                if (!inputs.TryGetValue(
                        producer.ProgramProducerIdentity,
                        out ActionPlaybackInputPlan input) ||
                    input.ProgramProducerIndex != producer.ProgramProducerIndex ||
                    input.AnimationChannelId != producer.AnimationChannelId)
                {
                    throw new InvalidOperationException(
                        $"Action producer '{producer.ProgramProducerIdentity}' has no exact Action Playback input.");
                }
                CharacterPresentationAnimationBinding animation =
                    producer.Animation;
                if (animation == null ||
                    !animation.Source ||
                    !animation.Source.IsValid ||
                    animation.Clips.Count == 0 ||
                    !float.IsFinite(animation.DurationSeconds) ||
                    animation.DurationSeconds <= 0f ||
                    !float.IsFinite(animation.LastSampleTimeSeconds) ||
                    animation.LastSampleTimeSeconds <= 0f ||
                    animation.LastSampleTimeSeconds >
                    animation.DurationSeconds)
                {
                    throw new InvalidOperationException(
                        $"Action producer '{producer.ProgramProducerIdentity}' has an invalid Timeline source.");
                }
                string markerError = string.Empty;
                if (animation.MarkerSync == null ||
                    !animation.MarkerSync.TryValidate(out markerError))
                {
                    throw new InvalidOperationException(
                        $"Action producer '{producer.ProgramProducerIdentity}' marker binding is invalid: {markerError}.");
                }
                var binding = new ResolvedActionAnimationBinding(
                    input,
                    producer.ProducerId,
                    producer.PlaybackMode,
                    animation);
                if (!result.m_ByProducerId.TryAdd(
                        producer.ProducerId,
                        binding) ||
                    !result.m_ByProgramProducerId.TryAdd(
                        producer.ProgramProducerIdentity,
                        binding))
                {
                    throw new InvalidOperationException(
                        $"Action producer '{producer.ProgramProducerIdentity}' is duplicated.");
                }
                inputs.Remove(producer.ProgramProducerIdentity);
            }
            if (inputs.Count != 0)
                throw new InvalidOperationException(
                    "Pose Plan retains Action Playback inputs without finite Action producers.");
            return result;
        }
    }
}
