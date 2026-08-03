using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public sealed class CharacterActionPlaybackFrameTransaction
    {
        readonly ActionPlaybackCommandInbox m_Inbox;

        internal CharacterActionPlaybackFrameTransaction(
            ActionPlaybackCommandInbox inbox)
        {
            m_Inbox = inbox ??
                throw new ArgumentNullException(nameof(inbox));
            Closed = true;
        }

        public ulong Identity { get; private set; }
        internal ActionPlaybackInboxReadLease InboxLease { get; private set; }
        internal ActionLifecycleMutationLease LifecycleLease { get; private set; }
        internal ActionSampleHistoryMutationLease HistoryLease { get; private set; }
        public ulong PresentationFrame { get; private set; }
        internal IReadOnlyList<ActionPlaybackInboxEntry> InboxEntries => m_Inbox;
        internal bool Closed { get; set; }
        public bool IsValid =>
            Identity != 0 &&
            LifecycleLease.IsValid &&
            HistoryLease.IsValid &&
            PresentationFrame != 0 &&
            !Closed;

        internal void Begin(
            ulong identity,
            ActionPlaybackInboxReadLease inboxLease,
            ActionLifecycleMutationLease lifecycleLease,
            ActionSampleHistoryMutationLease historyLease,
            ulong presentationFrame)
        {
            if (!Closed ||
                identity == 0 ||
                !lifecycleLease.IsValid ||
                !historyLease.IsValid ||
                presentationFrame == 0)
            {
                throw new InvalidOperationException(
                    "Character Action playback transaction cannot begin.");
            }
            Identity = identity;
            InboxLease = inboxLease;
            LifecycleLease = lifecycleLease;
            HistoryLease = historyLease;
            PresentationFrame = presentationFrame;
            Closed = false;
        }

        internal void Close()
        {
            Identity = 0;
            InboxLease = default;
            LifecycleLease = default;
            HistoryLease = default;
            PresentationFrame = 0;
            Closed = true;
        }
    }

    public sealed class CharacterActionPlaybackRuntime :
        IActionPlaybackCommandPublisher
    {
        readonly ActionAnimationBindingIndex m_Bindings;
        readonly ActionPlaybackCommandInbox m_Inbox;
        readonly ActionAnimationPlaybackLifecycleRegistry m_Lifecycle;
        readonly ActionCommittedSampleHistory m_CommittedSamples;
        readonly FixedCapacityFrameBuffer<
            ActionAnimationPlaybackLifecycleSnapshot> m_Snapshots;
        readonly CharacterActionPlaybackFrameTransaction m_Transaction;
        CharacterActionPlaybackFrameTransaction m_ActiveTransaction;

        public CharacterActionPlaybackRuntime(
            ActionAnimationBindingIndex bindings)
        {
            m_Bindings = bindings ??
                throw new ArgumentNullException(nameof(bindings));
            int playbackCapacity = CalculatePlaybackCapacity(bindings);
            FrameCapacity = playbackCapacity;
            int commandCapacity = checked(playbackCapacity * 4);
            m_Inbox = new ActionPlaybackCommandInbox(commandCapacity);
            m_Lifecycle =
                new ActionAnimationPlaybackLifecycleRegistry(
                    playbackCapacity,
                    playbackCapacity,
                    commandCapacity);
            m_CommittedSamples =
                new ActionCommittedSampleHistory(
                    playbackCapacity,
                    commandCapacity);
            m_Snapshots =
                new FixedCapacityFrameBuffer<
                    ActionAnimationPlaybackLifecycleSnapshot>(
                    playbackCapacity);
            m_Transaction =
                new CharacterActionPlaybackFrameTransaction(m_Inbox);
        }

        public ActionAnimationBindingIndex Bindings => m_Bindings;
        public int PendingCommandCount => m_Inbox.PendingCount;
        internal int FrameCapacity { get; }
        internal int JournalCapacity => checked(
            m_Lifecycle.CommandMutationCapacity +
            m_CommittedSamples.MutationCapacity);
        internal int BackendReleaseCompletionCapacity =>
            checked(FrameCapacity * FrameCapacity);
        public bool HasActiveFrameTransaction =>
            m_ActiveTransaction != null &&
            m_ActiveTransaction.IsValid;

        public void Publish(ActionAnimationPlaybackCommand command)
        {
            RequireKnownBinding(command);
            m_Inbox.Publish(command);
        }

        public void Replace(
            EventId targetEventId,
            ActionAnimationPlaybackCommand replacement)
        {
            RequireKnownBinding(replacement);
            m_Inbox.Replace(targetEventId, replacement);
        }

        public void Retire(ActionAnimationPlaybackCommand terminalCommand)
        {
            RequireKnownBinding(terminalCommand);
            m_Inbox.Retire(terminalCommand);
        }

        public CharacterActionPlaybackFrameTransaction BeginFrame(
            ulong frameIdentity,
            ulong presentationFrame)
        {
            if (HasActiveFrameTransaction)
            {
                throw new InvalidOperationException(
                    "Character Action playback already has an active frame transaction.");
            }
            if (frameIdentity == 0 || presentationFrame == 0)
                throw new ArgumentOutOfRangeException(nameof(presentationFrame));

            ActionPlaybackInboxReadLease inboxLease = default;
            ActionLifecycleMutationLease lifecycleLease = default;
            ActionSampleHistoryMutationLease historyLease = default;
            try
            {
                inboxLease = m_Inbox.BeginRead();
                lifecycleLease = m_Lifecycle.BeginMutation();
                historyLease = m_CommittedSamples.BeginMutation();
                m_Transaction.Begin(
                    frameIdentity,
                    inboxLease,
                    lifecycleLease,
                    historyLease,
                    presentationFrame);
                m_Lifecycle.ApplyCommands(
                    lifecycleLease,
                    m_Transaction.InboxEntries);
                for (int i = 0;
                     i < m_Transaction.InboxEntries.Count;
                     i++)
                {
                    ActionAnimationPlaybackCommand command =
                        m_Transaction.InboxEntries[i].Command;
                    if (!m_Bindings.TryGet(
                            command.PlaybackId.ProducerId,
                            out ResolvedActionAnimationBinding binding))
                    {
                        throw new InvalidOperationException(
                            $"Action playback '{command.PlaybackId}' has no compiled Slot binding.");
                    }
                    m_Lifecycle.BindPlaybackSlot(
                        lifecycleLease,
                        command.PlaybackId,
                        binding.SlotId);
                }
                m_CommittedSamples.ApplyCommands(
                    historyLease,
                    m_Transaction.InboxEntries);
                m_ActiveTransaction = m_Transaction;
                return m_Transaction;
            }
            catch
            {
                if (historyLease.IsValid)
                    m_CommittedSamples.Discard(historyLease);
                if (lifecycleLease.IsValid)
                    m_Lifecycle.Discard(lifecycleLease);
                if (inboxLease.IsValid)
                    m_Inbox.Discard(inboxLease);
                if (m_Transaction.IsValid)
                    m_Transaction.Close();
                throw;
            }
        }

        public void ReplaceSlotUsageBatch(
            CharacterActionPlaybackFrameTransaction transaction,
            IReadOnlyList<ActionSlotSourceUsage> usages)
        {
            RequireTransaction(transaction);
            m_Lifecycle.ReplaceSlotUsageBatch(
                transaction.LifecycleLease,
                usages);
        }

        public void ApplyRetirementPermissions(
            CharacterActionPlaybackFrameTransaction transaction,
            IReadOnlyList<ActionRetirementPermission> permissions)
        {
            RequireTransaction(transaction);
            m_Lifecycle.ApplyRetirementPermissions(
                transaction.LifecycleLease,
                permissions);
        }

        public void RegisterBackendReleaseRequest(
            CharacterActionPlaybackFrameTransaction transaction,
            ActionBackendReleaseRequest request)
        {
            RequireTransaction(transaction);
            m_Lifecycle.RegisterBackendReleaseRequest(
                transaction.LifecycleLease,
                request);
        }

        public void ApplyBackendReleaseCompletions(
            CharacterActionPlaybackFrameTransaction transaction,
            IReadOnlyList<ActionBackendReleaseCompletion> completions)
        {
            RequireTransaction(transaction);
            m_Lifecycle.ApplyBackendReleaseCompletions(
                transaction.LifecycleLease,
                completions);
            RemoveRetiredSampleHistory(transaction);
        }

        public void RetireWithoutBackendResources(
            CharacterActionPlaybackFrameTransaction transaction,
            AnimationPlaybackId playbackId)
        {
            RequireTransaction(transaction);
            m_Lifecycle.RetireWithoutBackendResources(
                transaction.LifecycleLease,
                playbackId);
            m_CommittedSamples.RemovePlayback(
                transaction.HistoryLease,
                playbackId);
        }

        public bool TryGetCommittedSampleWindow(
            CharacterActionPlaybackFrameTransaction transaction,
            AnimationPlaybackId playbackId,
            double presentationSampleTick,
            out ActionCommittedSampleWindow window)
        {
            RequireTransaction(transaction);
            return m_CommittedSamples.TryGetProjectionWindow(
                transaction.HistoryLease,
                playbackId,
                presentationSampleTick,
                out window);
        }

        internal FixedCapacityFrameBuffer<ActionAnimationPlaybackLifecycleFrame>
            BuildLifecycleFrame(
                CharacterActionPlaybackFrameTransaction transaction)
        {
            RequireTransaction(transaction);
            return m_Lifecycle.BuildFrameView(
                transaction.LifecycleLease);
        }

        public IReadOnlyList<ActionAnimationPlaybackLifecycleSnapshot>
            BuildCommittedLifecycleSnapshot()
        {
            m_Lifecycle.BuildCommittedSnapshot(m_Snapshots);
            return m_Snapshots;
        }

        internal void ValidateFrame(
            CharacterActionPlaybackFrameTransaction transaction)
        {
            RequireTransaction(transaction);
            m_Lifecycle.ValidateFrame(transaction.LifecycleLease);
            m_CommittedSamples.ValidateFrame(transaction.HistoryLease);
        }

        public void Commit(
            CharacterActionPlaybackFrameTransaction transaction)
        {
            RequireTransaction(transaction);
            m_Lifecycle.Commit(transaction.LifecycleLease);
            m_CommittedSamples.Commit(transaction.HistoryLease);
            if (transaction.InboxLease.IsValid)
                m_Inbox.Commit(transaction.InboxLease);
            Close(transaction);
        }

        public void DiscardFrame(
            CharacterActionPlaybackFrameTransaction transaction)
        {
            RequireTransaction(transaction);
            m_Lifecycle.Discard(transaction.LifecycleLease);
            m_CommittedSamples.Discard(transaction.HistoryLease);
            if (transaction.InboxLease.IsValid)
                m_Inbox.Discard(transaction.InboxLease);
            Close(transaction);
        }

        public void Reset()
        {
            if (HasActiveFrameTransaction)
            {
                throw new InvalidOperationException(
                    "Character Action playback cannot reset during a frame transaction.");
            }
            m_Inbox.Reset();
            m_Lifecycle.Reset();
            m_CommittedSamples.Reset();
            m_Snapshots.Clear();
        }

        void RequireKnownBinding(ActionAnimationPlaybackCommand command)
        {
            if (!command.IsValid ||
                !m_Bindings.TryGet(
                    command.PlaybackId.ProducerId,
                    out ResolvedActionAnimationBinding binding) ||
                !string.Equals(
                    binding.ProgramProducerId,
                    command.ProgramProducerId,
                    StringComparison.Ordinal) ||
                binding.AnimationChannelId != command.AnimationChannelId)
            {
                throw new InvalidOperationException(
                    "Action playback command has no exact compiled Action binding.");
            }
        }

        void RequireTransaction(
            CharacterActionPlaybackFrameTransaction transaction)
        {
            if (transaction == null ||
                !transaction.IsValid ||
                !ReferenceEquals(transaction, m_ActiveTransaction))
            {
                throw new InvalidOperationException(
                    "Character Action playback frame transaction is invalid.");
            }
        }

        void Close(CharacterActionPlaybackFrameTransaction transaction)
        {
            transaction.Close();
            m_ActiveTransaction = null;
            m_Snapshots.Clear();
        }

        void RemoveRetiredSampleHistory(
            CharacterActionPlaybackFrameTransaction transaction)
        {
            FixedCapacityFrameBuffer<ActionAnimationPlaybackLifecycleFrame>
                lifecycle = m_Lifecycle.BuildFrameView(
                    transaction.LifecycleLease);
            for (int i = 0; i < lifecycle.Count; i++)
            {
                ActionAnimationPlaybackLifecycleFrame frame = lifecycle[i];
                if (frame.Phase ==
                    ActionAnimationPlaybackLifecyclePhase.Retired)
                {
                    m_CommittedSamples.RemovePlayback(
                        transaction.HistoryLease,
                        frame.PlaybackId);
                }
            }
        }

        static int CalculatePlaybackCapacity(
            ActionAnimationBindingIndex bindings)
        {
            CharacterPresentationPosePlan plan = bindings.Projection.PosePlan;
            int capacity = 0;
            for (int i = 0; i < plan.AnimationSlots.Count; i++)
            {
                capacity = checked(
                    capacity +
                    plan.AnimationSlots[i].BlendStackWorkspace.Capacity);
            }
            return Math.Max(
                1,
                checked(capacity + bindings.Bindings.Count));
        }
    }
}
