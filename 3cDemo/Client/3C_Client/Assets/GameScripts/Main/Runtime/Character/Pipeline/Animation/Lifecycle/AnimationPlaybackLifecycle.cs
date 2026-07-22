using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
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
            PoseSlotId poseSlotId,
            AnimationPlaybackId playbackId,
            AnimationPoseSourceId sourceId,
            AnimationPlaybackLifecyclePhase phase,
            float sampleTime,
            PoseSlotFrameAvailability slotAvailability,
            float slotOutputWeight,
            bool hasVisualSample)
        {
            if (!animationChannelId.IsValid || !poseSlotId.IsValid || !playbackId.IsValid ||
                !Enum.IsDefined(typeof(AnimationPlaybackLifecyclePhase), phase) ||
                !Enum.IsDefined(typeof(PoseSlotFrameAvailability), slotAvailability) ||
                !float.IsFinite(sampleTime) || sampleTime < 0f ||
                !float.IsFinite(slotOutputWeight) || slotOutputWeight < 0f || slotOutputWeight > 1f ||
                hasVisualSample != sourceId.IsValid)
            {
                throw new ArgumentException("Animation Playback Lifecycle snapshot is invalid.");
            }
            AnimationChannelId = animationChannelId;
            PoseSlotId = poseSlotId;
            PlaybackId = playbackId;
            SourceId = sourceId;
            Phase = phase;
            SampleTime = sampleTime;
            SlotAvailability = slotAvailability;
            SlotOutputWeight = slotOutputWeight;
            HasVisualSample = hasVisualSample;
        }

        public AnimationChannelId AnimationChannelId { get; }
        public PoseSlotId PoseSlotId { get; }
        public AnimationPlaybackId PlaybackId { get; }
        public AnimationPoseSourceId SourceId { get; }
        public AnimationPlaybackLifecyclePhase Phase { get; }
        public float SampleTime { get; }
        public PoseSlotFrameAvailability SlotAvailability { get; }
        public float SlotOutputWeight { get; }
        public bool HasVisualSample { get; }
    }

    public sealed class AnimationPlaybackLifecycle
    {
        readonly CharacterAnimationPresentationBindingIndex m_Bindings;
        readonly ChannelState[] m_Channels;
        readonly Dictionary<AnimationChannelId, ChannelState> m_ChannelsById =
            new Dictionary<AnimationChannelId, ChannelState>();
        readonly Dictionary<AnimationPlaybackId, ResolvedAnimationPoseRequest> m_LatestRequests =
            new Dictionary<AnimationPlaybackId, ResolvedAnimationPoseRequest>();

        public AnimationPlaybackLifecycle(CharacterAnimationPresentationBindingIndex bindings)
        {
            m_Bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            if (!bindings.IsValid || bindings.Projection == null)
                throw new ArgumentException("Animation Presentation bindings are invalid.", nameof(bindings));
            m_Channels = new ChannelState[bindings.Slots.Count];
            foreach (KeyValuePair<PoseSlotId, ResolvedAnimationPoseSlot> pair in bindings.Slots)
            {
                ResolvedAnimationPoseSlot slot = pair.Value;
                if (!slot.IsValid || slot.Index < 0 || slot.Index >= m_Channels.Length || m_Channels[slot.Index] != null)
                    throw new InvalidOperationException("Animation Playback Lifecycle Pose Slot layout is invalid.");
                var state = new ChannelState(slot);
                m_Channels[slot.Index] = state;
                m_ChannelsById.Add(slot.AnimationChannelId, state);
            }
            for (int i = 0; i < m_Channels.Length; i++)
            {
                if (m_Channels[i] == null)
                    throw new InvalidOperationException($"Animation Playback Lifecycle Pose Slot #{i} is missing.");
            }
        }

        public bool HasRequiredOutputSelection
        {
            get
            {
                for (int i = 0; i < m_Channels.Length; i++)
                {
                    ChannelState state = m_Channels[i];
                    if (state.Slot.OutputPolicy == PoseSlotOutputPolicy.RequireOutput &&
                        (!state.Selection.IsValid || !state.Selection.HasPlayback || !state.SourceId.IsValid))
                    {
                        return false;
                    }
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
            IReadOnlyList<AnimationBlendStackRuntime> stacks,
            HashSet<AnimationPlaybackId> destination)
        {
            if (stacks == null)
                throw new ArgumentNullException(nameof(stacks));
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
            for (int stackIndex = 0; stackIndex < stacks.Count; stackIndex++)
            {
                AnimationBlendStackRuntime stack = stacks[stackIndex];
                for (int entryIndex = 0; entryIndex < stack.EntryCount; entryIndex++)
                {
                    AnimationBlendEntryId entry = stack.GetEntryId(entryIndex);
                    if (!entry.EmptyTarget)
                        destination.Add(entry.SourceId.PlaybackId);
                }
            }
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
            m_LatestRequests.Clear();
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
                    if (state.Slot.OutputPolicy == PoseSlotOutputPolicy.RequireOutput)
                        throw new InvalidOperationException($"Required Pose Slot '{state.Slot.PoseSlotId}' selected Empty.");
                    if (!state.EmptyTarget)
                    {
                        poseRuntime.PushEmpty(
                            state.Slot.AnimationChannelId,
                            nextPresentationRequestSequence());
                        state.EmptyTarget = true;
                        state.SourceId = default;
                    }
                    continue;
                }

                if (!m_LatestRequests.TryGetValue(state.Selection.PlaybackId, out ResolvedAnimationPoseRequest request))
                {
                    state.SourceId = default;
                    state.EmptyTarget = false;
                    continue;
                }
                poseRuntime.PushPoseRequest(in request);
                state.SourceId = request.SourceId;
                state.EmptyTarget = false;
            }
        }

        internal void BuildSnapshot(
            IReadOnlyList<AnimationBlendStackRuntime> stacks,
            List<AnimationPlaybackLifecycleSnapshot> destination)
        {
            if (stacks == null)
                throw new ArgumentNullException(nameof(stacks));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            for (int i = 0; i < m_Channels.Length; i++)
            {
                ChannelState state = m_Channels[i];
                if (!state.Selection.IsValid || !state.Selection.HasPlayback)
                    continue;
                AnimationBlendStackRuntime stack = stacks[state.Slot.Index];
                ResolvedAnimationPoseRequest request = default;
                bool sampled = state.SourceId.IsValid &&
                               m_LatestRequests.TryGetValue(state.Selection.PlaybackId, out request);
                destination.Add(new AnimationPlaybackLifecycleSnapshot(
                    state.Slot.AnimationChannelId,
                    state.Slot.PoseSlotId,
                    state.Selection.PlaybackId,
                    sampled ? state.SourceId : default,
                    sampled ? AnimationPlaybackLifecyclePhase.Selected : AnimationPlaybackLifecyclePhase.PendingFirstSample,
                    sampled ? request.VisualSampleTime : 0f,
                    stack.HasCompletedFrame ? stack.LastAvailability : PoseSlotFrameAvailability.Invalid,
                    stack.HasCompletedFrame ? stack.LastOutputWeight : 0f,
                    sampled));

                for (int entryIndex = 0; entryIndex < stack.EntryCount; entryIndex++)
                {
                    AnimationBlendEntryId entry = stack.GetEntryId(entryIndex);
                    if (entry.EmptyTarget || entry.SourceId.Equals(state.SourceId))
                        continue;
                    bool hasRequest = m_LatestRequests.TryGetValue(entry.SourceId.PlaybackId, out ResolvedAnimationPoseRequest retained);
                    if (!hasRequest)
                        continue;
                    destination.Add(new AnimationPlaybackLifecycleSnapshot(
                        state.Slot.AnimationChannelId,
                        state.Slot.PoseSlotId,
                        entry.SourceId.PlaybackId,
                        entry.SourceId,
                        AnimationPlaybackLifecyclePhase.Retained,
                        retained.VisualSampleTime,
                        stack.LastAvailability,
                        stack.LastOutputWeight,
                        true));
                }
            }
        }

        internal bool Retains(AnimationPlaybackId playbackId, IReadOnlyList<AnimationBlendStackRuntime> stacks)
        {
            if (!playbackId.IsValid)
                return false;
            for (int i = 0; i < m_Channels.Length; i++)
            {
                AnimationChannelSelection selection = m_Channels[i].Selection;
                if (selection.IsValid && selection.HasPlayback && selection.PlaybackId.Equals(playbackId))
                    return true;
            }
            for (int stackIndex = 0; stackIndex < stacks.Count; stackIndex++)
            {
                AnimationBlendStackRuntime stack = stacks[stackIndex];
                for (int entryIndex = 0; entryIndex < stack.EntryCount; entryIndex++)
                {
                    AnimationBlendEntryId entry = stack.GetEntryId(entryIndex);
                    if (!entry.EmptyTarget && entry.SourceId.PlaybackId.Equals(playbackId))
                        return true;
                }
            }
            return false;
        }

        public void Reset()
        {
            for (int i = 0; i < m_Channels.Length; i++)
                m_Channels[i].Reset();
            m_LatestRequests.Clear();
        }

        void ApplyCommand(AnimationPlaybackCommand command)
        {
            switch (command.Kind)
            {
                case AnimationPlaybackCommandKind.Selection:
                    if (!command.Selection.IsValid ||
                        !m_ChannelsById.TryGetValue(command.Selection.AnimationChannelId, out ChannelState state))
                    {
                        throw new InvalidOperationException(
                            $"Animation selection targets unknown Channel '{command.Selection.AnimationChannelId}'.");
                    }
                    if (!command.Selection.HasPlayback ||
                        m_Bindings.TryGetBinding(command.Selection.PlaybackId.ProducerId, out ResolvedAnimationProducerBinding binding) &&
                        binding.AnimationChannelId == state.Slot.AnimationChannelId)
                    {
                        state.Selection = command.Selection;
                        break;
                    }
                    throw new InvalidOperationException(
                        $"Animation Channel '{state.Slot.AnimationChannelId}' selected an unknown producer '{command.Selection.PlaybackId.ProducerId}'.");
                case AnimationPlaybackCommandKind.PoseRequest:
                    ResolvedAnimationPoseRequest request = command.PoseRequest;
                    if (!request.IsValid || !m_ChannelsById.TryGetValue(request.AnimationChannelId, out state) ||
                        state.Slot.PoseSlotId != request.PoseSlotId ||
                        !m_Bindings.TryGetBinding(request.SourceId.PlaybackId.ProducerId, out binding) ||
                        binding.ProgramProducerIndex != request.ProgramProducerIndex ||
                        binding.AnimationChannelId != request.AnimationChannelId ||
                        binding.PoseSlotId != request.PoseSlotId)
                    {
                        throw new InvalidOperationException("Animation pose request does not match its compiled Channel and Pose Slot binding.");
                    }
                    m_LatestRequests[request.SourceId.PlaybackId] = request;
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

        sealed class ChannelState
        {
            internal ChannelState(ResolvedAnimationPoseSlot slot)
            {
                Slot = slot;
            }

            internal ResolvedAnimationPoseSlot Slot { get; }
            internal AnimationChannelSelection Selection { get; set; }
            internal AnimationPoseSourceId SourceId { get; set; }
            internal bool EmptyTarget { get; set; }

            internal void Reset()
            {
                Selection = default;
                SourceId = default;
                EmptyTarget = false;
            }
        }
    }
}
