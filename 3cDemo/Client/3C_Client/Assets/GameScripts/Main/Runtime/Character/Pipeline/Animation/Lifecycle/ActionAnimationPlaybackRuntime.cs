using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    internal interface IActionAnimationPlaybackFrameSource
    {
        FixedCapacityFrameBuffer<ActionAnimationPlaybackLifecycleFrame> Frames { get; }
        void ReportSlotUsage(
            in ActionAnimationPlaybackLifecycleFrame frame,
            AnimationSlotId slotId,
            ActionSlotSourceUsageKind? usage,
            ulong completionIdentity);
    }

    internal sealed class ActionAnimationPlaybackRuntime : IActionAnimationPlaybackFrameSource
    {
        readonly ActionAnimationPlaybackLifecycleRegistry m_Registry;
        readonly FixedCapacityFrameBuffer<ActionPlaybackInboxEntry> m_Commands;
        readonly FixedCapacityFrameBuffer<ActionSlotSourceUsage> m_Usages;
        readonly FixedCapacityFrameBuffer<ActionRetirementPermission> m_Retirements;
        ActionLifecycleMutationLease m_Lease;
        FixedCapacityFrameBuffer<ActionAnimationPlaybackLifecycleFrame> m_Frames;
        ulong m_CommandSequence;

        internal ActionAnimationPlaybackRuntime(int capacity, int commandCapacity)
        {
            m_Registry = new ActionAnimationPlaybackLifecycleRegistry(capacity, 8, commandCapacity);
            m_Commands = new FixedCapacityFrameBuffer<ActionPlaybackInboxEntry>(commandCapacity);
            m_Usages = new FixedCapacityFrameBuffer<ActionSlotSourceUsage>(capacity);
            m_Retirements = new FixedCapacityFrameBuffer<ActionRetirementPermission>(capacity);
        }

        public FixedCapacityFrameBuffer<ActionAnimationPlaybackLifecycleFrame> Frames =>
            m_Lease.IsValid ? m_Frames : throw new InvalidOperationException("Action playback frame is not open.");
        internal IReadOnlyList<ActionPlaybackInboxEntry> Commands => m_Commands;
        internal IReadOnlyList<ActionRetirementPermission> Retirements => m_Retirements;
        internal bool IsFrameOpen => m_Lease.IsValid;

        internal void BeginFrame(IReadOnlyList<ActionAnimationPlaybackCommand> commands)
        {
            if (IsFrameOpen)
                throw new InvalidOperationException("Action playback frame is already open.");
            m_Lease = m_Registry.BeginMutation();
            try
            {
                for (int i = 0; i < commands.Count; i++)
                {
                    var entry = new ActionPlaybackInboxEntry(checked(++m_CommandSequence), commands[i]);
                    m_Commands.Add(in entry);
                }
                m_Registry.ApplyCommands(m_Lease, m_Commands);
                m_Frames = m_Registry.BuildFrameView(m_Lease);
            }
            catch
            {
                DiscardFrame();
                throw;
            }
        }

        public void ReportSlotUsage(
            in ActionAnimationPlaybackLifecycleFrame frame,
            AnimationSlotId slotId,
            ActionSlotSourceUsageKind? usage,
            ulong completionIdentity)
        {
            m_Registry.BindPlaybackSlot(m_Lease, frame.PlaybackId, slotId);
            if (usage.HasValue)
            {
                var value = new ActionSlotSourceUsage(slotId, frame.PlaybackId, usage.Value, completionIdentity);
                m_Usages.Add(in value);
            }
            else if (frame.EndReason != ActionPlaybackEndReason.None)
            {
                var permission = new ActionRetirementPermission(frame.PlaybackId, slotId, completionIdentity);
                m_Retirements.Add(in permission);
            }
        }

        internal bool TryGetLatestPlayback(
            AnimationChannelId channelId,
            out AnimationPlaybackId playbackId,
            out ActionAnimationPlaybackLifecyclePhase phase) =>
            m_Registry.TryGetLatestPlayback(channelId, out playbackId, out phase);

        internal bool TryGetProjectedSample(
            AnimationPlaybackId playbackId,
            out ActionProjectedSample sample,
            out EventId eventId) =>
            m_Registry.TryGetProjectedSample(playbackId, out sample, out eventId);

        internal void ValidateFrame()
        {
            m_Registry.ReplaceSlotUsageBatch(m_Lease, m_Usages);
            m_Registry.ApplyRetirementPermissions(m_Lease, m_Retirements);
            int retirementCount = m_Retirements.Count;
            for (int i = 0; i < retirementCount; i++)
            {
                ref readonly ActionRetirementPermission permission =
                    ref m_Retirements.ElementAt(i);
                m_Registry.RetireWithoutBackendResources(
                    m_Lease,
                    permission.PlaybackId);
            }
            m_Registry.ValidateFrame(m_Lease);
        }

        internal void CommitFrame()
        {
            m_Registry.Commit(m_Lease);
            ClearFrame();
        }

        internal void DiscardFrame()
        {
            if (!IsFrameOpen)
                return;
            m_Registry.Discard(m_Lease);
            ClearFrame();
        }

        internal void Reset()
        {
            DiscardFrame();
            m_Registry.Reset();
        }

        void ClearFrame()
        {
            m_Lease = default;
            m_Frames = null;
            m_Commands.Clear();
            m_Usages.Clear();
            m_Retirements.Clear();
        }
    }
}
