using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    public enum AnimationPlaybackLifecyclePhase
    {
        PendingFirstSample,
        Selected,
        Retained,
        Retired
    }

    public readonly struct AnimationPlaybackLifecycleSnapshot
    {
        public AnimationPlaybackLifecycleSnapshot(
            AnimationChannelId animationChannelId,
            PoseNodeId playerNodeId,
            AnimationPlaybackId playbackId,
            AnimationPoseSourceId sourceId,
            AnimationPlaybackLifecyclePhase phase,
            float sampleTime,
            float visualTimeScale,
            AnimationPoseAvailability availability,
            float outputWeight,
            bool hasVisualSample)
        {
            if (!animationChannelId.IsValid || !playerNodeId.IsValid || !playbackId.IsValid ||
                !Enum.IsDefined(typeof(AnimationPlaybackLifecyclePhase), phase) ||
                !Enum.IsDefined(typeof(AnimationPoseAvailability), availability) ||
                !float.IsFinite(sampleTime) || sampleTime < 0f ||
                !float.IsFinite(visualTimeScale) || visualTimeScale < 0f ||
                !float.IsFinite(outputWeight) || outputWeight < 0f || outputWeight > 1f ||
                hasVisualSample != sourceId.IsValid)
                throw new ArgumentException("Animation Playback Lifecycle snapshot is invalid.");
            AnimationChannelId = animationChannelId;
            PoseNodeId = playerNodeId;
            PlaybackId = playbackId;
            SourceId = sourceId;
            Phase = phase;
            SampleTime = sampleTime;
            VisualTimeScale = visualTimeScale;
            Availability = availability;
            OutputWeight = outputWeight;
            HasVisualSample = hasVisualSample;
        }

        public AnimationChannelId AnimationChannelId { get; }
        public PoseNodeId PoseNodeId { get; }
        public AnimationPlaybackId PlaybackId { get; }
        public AnimationPoseSourceId SourceId { get; }
        public AnimationPlaybackLifecyclePhase Phase { get; }
        public float SampleTime { get; }
        public float VisualTimeScale { get; }
        public AnimationPoseAvailability Availability { get; }
        public float OutputWeight { get; }
        public bool HasVisualSample { get; }
    }

    public sealed class AnimationPlaybackLifecycle
    {
        readonly CharacterAnimationPresentationBindingIndex m_Bindings;
        readonly ChannelState[] m_Channels;
        readonly Dictionary<AnimationChannelId, ChannelState> m_ChannelsById =
            new Dictionary<AnimationChannelId, ChannelState>();
        readonly Dictionary<AnimationPlaybackId, AnimationSelectionFrame> m_LatestSelections =
            new Dictionary<AnimationPlaybackId, AnimationSelectionFrame>();
        readonly HashSet<AnimationPlaybackId> m_LatestUnavailable = new HashSet<AnimationPlaybackId>();

        public AnimationPlaybackLifecycle(CharacterAnimationPresentationBindingIndex bindings)
        {
            m_Bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            if (!bindings.IsValid || bindings.Projection == null)
                throw new ArgumentException("Animation Presentation bindings are invalid.", nameof(bindings));

            var channels = new List<ChannelState>();
            IReadOnlyList<CharacterPresentationSelectionInputEntry> inputs = bindings.Projection.PosePlan.SelectionInputs;
            for (int i = 0; i < inputs.Count; i++)
            {
                CharacterPresentationSelectionInputEntry input = inputs[i];
                if (!m_ChannelsById.TryGetValue(input.AnimationChannelId, out ChannelState state))
                {
                    state = new ChannelState(input.AnimationChannelId, input.NodeId);
                    m_ChannelsById.Add(input.AnimationChannelId, state);
                    channels.Add(state);
                }
                state.AddInput(input);
            }
            if (channels.Count == 0)
                throw new InvalidOperationException("Animation Playback Lifecycle requires at least one Selection Input channel.");
            m_Channels = channels.ToArray();
        }

        public bool HasRequiredOutputSelection
        {
            get
            {
                for (int i = 0; i < m_Channels.Length; i++)
                {
                    ChannelState state = m_Channels[i];
                    if (state.RequiresSelection &&
                        (!state.Selection.IsValid || !state.Selection.HasPlayback || !state.SourceId.IsValid))
                        return false;
                }
                return true;
            }
        }

        public bool TryGetSelectedPlayback(AnimationChannelId channelId, out AnimationPlaybackId playbackId)
        {
            if (m_ChannelsById.TryGetValue(channelId, out ChannelState state) &&
                state.Selection.IsValid && state.Selection.HasPlayback)
            {
                playbackId = state.Selection.PlaybackId;
                return true;
            }
            playbackId = default;
            return false;
        }

        internal void CollectSampleDemand(
            IReadOnlyList<AnimationPlaybackCommand> commands,
            AnimationPosePlayableGraphRuntime poseRuntime,
            HashSet<AnimationPlaybackId> destination)
        {
            if (poseRuntime == null)
                throw new ArgumentNullException(nameof(poseRuntime));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            for (int i = 0; i < m_Channels.Length; i++)
            {
                AnimationChannelSelection selection = m_Channels[i].Selection;
                if (selection.IsValid && selection.HasPlayback)
                    destination.Add(selection.PlaybackId);
            }
            if (commands != null)
            {
                for (int i = 0; i < commands.Count; i++)
                {
                    AnimationPlaybackCommand command = commands[i];
                    if (command.Kind == AnimationPlaybackCommandKind.Selection && command.Selection.HasPlayback)
                        destination.Add(command.Selection.PlaybackId);
                }
            }
            poseRuntime.CollectRetainedPlaybackDemand(destination);
        }

        internal void Apply(
            IReadOnlyList<AnimationPlaybackCommand> commands,
            AnimationPosePlayableGraphRuntime poseRuntime,
            Func<ulong> nextPresentationRequestSequence)
        {
            if (poseRuntime == null)
                throw new ArgumentNullException(nameof(poseRuntime));
            if (nextPresentationRequestSequence == null)
                throw new ArgumentNullException(nameof(nextPresentationRequestSequence));
            m_LatestSelections.Clear();
            m_LatestUnavailable.Clear();
            if (commands != null)
            {
                for (int i = 0; i < commands.Count; i++)
                    ApplyCommand(commands[i]);
            }

            for (int i = 0; i < m_Channels.Length; i++)
            {
                ChannelState state = m_Channels[i];
                if (!state.Selection.IsValid)
                    continue;
                if (!state.Selection.HasPlayback)
                {
                    if (state.RequiresSelection)
                        throw new InvalidOperationException($"Required Selection Input channel '{state.AnimationChannelId}' selected Empty.");
                    if (!state.EmptyTarget)
                    {
                        poseRuntime.PublishEmptySelection(state.AnimationChannelId, nextPresentationRequestSequence());
                        state.EmptyTarget = true;
                        state.SourceId = default;
                    }
                    continue;
                }
                if (!m_LatestSelections.TryGetValue(state.Selection.PlaybackId, out AnimationSelectionFrame selection))
                {
                    state.SourceId = default;
                    if (m_LatestUnavailable.Contains(state.Selection.PlaybackId))
                    {
                        if (state.RequiresSelection)
                        {
                            poseRuntime.PublishUnavailableSelection(state.AnimationChannelId, state.Selection.PlaybackId);
                            state.EmptyTarget = false;
                        }
                        else
                        {
                            poseRuntime.PublishEmptySelection(state.AnimationChannelId, nextPresentationRequestSequence());
                            state.EmptyTarget = true;
                        }
                    }
                    else
                    {
                        state.EmptyTarget = false;
                    }
                    continue;
                }
                poseRuntime.PublishSelection(in selection);
                state.SourceId = selection.SourceId;
                state.EmptyTarget = false;
            }
        }

        internal void BuildSnapshot(
            AnimationPosePlayableGraphRuntime poseRuntime,
            List<AnimationPlaybackLifecycleSnapshot> destination)
        {
            if (poseRuntime == null)
                throw new ArgumentNullException(nameof(poseRuntime));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            for (int i = 0; i < m_Channels.Length; i++)
            {
                ChannelState state = m_Channels[i];
                if (!state.Selection.IsValid || !state.Selection.HasPlayback)
                    continue;
                AnimationSelectionFrame selection = default;
                bool sampled = state.SourceId.IsValid &&
                               m_LatestSelections.TryGetValue(state.Selection.PlaybackId, out selection);
                if (!poseRuntime.TryGetPlaybackStatus(
                        state.Selection.PlaybackId,
                        out PoseNodeId playerNodeId,
                        out AnimationPoseAvailability availability,
                        out float outputWeight))
                {
                    playerNodeId = state.PrimaryInputNodeId;
                }
                destination.Add(new AnimationPlaybackLifecycleSnapshot(
                    state.AnimationChannelId,
                    playerNodeId,
                    state.Selection.PlaybackId,
                    sampled ? state.SourceId : default,
                    sampled || state.EmptyTarget
                        ? AnimationPlaybackLifecyclePhase.Selected
                        : AnimationPlaybackLifecyclePhase.PendingFirstSample,
                    sampled ? selection.VisualSampleTime : 0f,
                    sampled ? selection.VisualTimeScale : 0f,
                    availability,
                    outputWeight,
                    sampled));
            }
        }

        internal bool Retains(AnimationPlaybackId playbackId, AnimationPosePlayableGraphRuntime poseRuntime)
        {
            if (!playbackId.IsValid || poseRuntime == null)
                return false;
            for (int i = 0; i < m_Channels.Length; i++)
            {
                AnimationChannelSelection selection = m_Channels[i].Selection;
                if (selection.IsValid && selection.HasPlayback && selection.PlaybackId.Equals(playbackId))
                    return true;
            }
            return poseRuntime.RetainsPlayback(playbackId);
        }

        public void Reset()
        {
            for (int i = 0; i < m_Channels.Length; i++)
                m_Channels[i].Reset();
            m_LatestSelections.Clear();
            m_LatestUnavailable.Clear();
        }

        void ApplyCommand(AnimationPlaybackCommand command)
        {
            switch (command.Kind)
            {
                case AnimationPlaybackCommandKind.Selection:
                    if (!command.Selection.IsValid ||
                        !m_ChannelsById.TryGetValue(command.Selection.AnimationChannelId, out ChannelState state))
                        throw new InvalidOperationException($"Animation selection targets unknown Channel '{command.Selection.AnimationChannelId}'.");
                    if (command.Selection.HasPlayback)
                        RequireProducer(command.Selection.PlaybackId.ProducerId, state.AnimationChannelId);
                    state.Selection = command.Selection;
                    break;
                case AnimationPlaybackCommandKind.PoseRequest:
                    AnimationSelectionFrame selection = command.PoseRequest;
                    if (!selection.IsValid || !m_ChannelsById.ContainsKey(selection.AnimationChannelId))
                        throw new InvalidOperationException("Animation Selection Frame targets an unknown channel.");
                    CharacterPresentationProducerEntry producer = RequireProducer(
                        selection.SourceId.PlaybackId.ProducerId,
                        selection.AnimationChannelId);
                    if (producer.ProgramProducerIndex != selection.ProgramProducerIndex)
                        throw new InvalidOperationException("Animation Selection Frame producer index does not match the compiled Projection.");
                    m_LatestSelections[selection.SourceId.PlaybackId] = selection;
                    m_LatestUnavailable.Remove(selection.SourceId.PlaybackId);
                    break;
                case AnimationPlaybackCommandKind.PoseUnavailable:
                    AnimationChannelSelection unavailable = command.Selection;
                    if (!unavailable.IsValid || !unavailable.HasPlayback ||
                        !m_ChannelsById.ContainsKey(unavailable.AnimationChannelId) ||
                        !unavailable.PlaybackId.Equals(command.PlaybackId))
                    {
                        throw new InvalidOperationException("Unavailable animation pose targets an unknown channel or playback.");
                    }
                    RequireProducer(unavailable.PlaybackId.ProducerId, unavailable.AnimationChannelId);
                    m_LatestSelections.Remove(unavailable.PlaybackId);
                    m_LatestUnavailable.Add(unavailable.PlaybackId);
                    break;
                case AnimationPlaybackCommandKind.Complete:
                case AnimationPlaybackCommandKind.Release:
                    if (!command.PlaybackId.IsValid)
                        throw new InvalidOperationException("Animation terminal command has no PlaybackId.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(command.Kind), command.Kind, null);
            }
        }

        CharacterPresentationProducerEntry RequireProducer(AnimationProducerId producerId, AnimationChannelId channelId)
        {
            IReadOnlyList<CharacterPresentationProducerEntry> producers = m_Bindings.Projection.Producers;
            for (int i = 0; i < producers.Count; i++)
            {
                CharacterPresentationProducerEntry producer = producers[i];
                if (producer != null && producer.Kind == CharacterPresentationProducerKind.Animation &&
                    producer.ProducerId.Equals(producerId) && producer.AnimationChannelId.Equals(channelId))
                    return producer;
            }
            throw new InvalidOperationException($"Animation Channel '{channelId}' selected unknown producer '{producerId}'.");
        }

        sealed class ChannelState
        {
            internal ChannelState(AnimationChannelId animationChannelId, PoseNodeId primaryInputNodeId)
            {
                AnimationChannelId = animationChannelId;
                PrimaryInputNodeId = primaryInputNodeId;
            }

            internal AnimationChannelId AnimationChannelId { get; }
            internal PoseNodeId PrimaryInputNodeId { get; }
            internal bool RequiresSelection { get; private set; }
            internal AnimationChannelSelection Selection { get; set; }
            internal AnimationPoseSourceId SourceId { get; set; }
            internal bool EmptyTarget { get; set; }

            internal void AddInput(CharacterPresentationSelectionInputEntry input)
            {
                if (input == null || input.AnimationChannelId != AnimationChannelId)
                    throw new ArgumentException("Selection Input does not belong to the channel.", nameof(input));
                RequiresSelection |= input.Availability == AnimationSelectionAvailabilityPolicy.RequireSelection;
            }

            internal void Reset()
            {
                Selection = default;
                SourceId = default;
                EmptyTarget = false;
            }
        }
    }
}
