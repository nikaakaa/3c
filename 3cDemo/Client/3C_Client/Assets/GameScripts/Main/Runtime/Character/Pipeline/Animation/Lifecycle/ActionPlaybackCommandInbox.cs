using System;
using System.Collections;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    public readonly struct ActionPlaybackInboxEntry
    {
        readonly ActionAnimationPlaybackCommand m_Command;

        internal ActionPlaybackInboxEntry(
            ulong sequence,
            ActionAnimationPlaybackCommand command)
        {
            Sequence = sequence;
            m_Command = command;
            if (!IsValid)
                throw new ArgumentException(
                    "Action playback inbox entry is invalid.");
        }

        public ulong Sequence { get; }
        public ActionAnimationPlaybackCommand Command => m_Command;
        internal ref readonly ActionAnimationPlaybackCommand CommandRef => ref m_Command;
        public bool IsValid => Sequence != 0 && Command.IsValid;
    }

    public readonly struct ActionPlaybackInboxReadLease
    {
        internal ActionPlaybackInboxReadLease(
            ulong identity,
            ulong sequenceHighWatermark)
        {
            Identity = identity;
            SequenceHighWatermark = sequenceHighWatermark;
        }

        public ulong Identity { get; }
        public ulong SequenceHighWatermark { get; }
        public bool IsValid => Identity != 0;
    }

    public sealed class ActionPlaybackCommandInbox :
        IReadOnlyList<ActionPlaybackInboxEntry>
    {
        readonly ActionPlaybackInboxEntry[] m_Entries;
        int m_Count;
        ulong m_NextInboxSequence;
        ulong m_NextLeaseIdentity;
        ActionPlaybackInboxReadLease m_ActiveLease;

        public ActionPlaybackCommandInbox(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Entries = new ActionPlaybackInboxEntry[capacity];
        }

        public int PendingCount => m_Count;
        public int Count => m_Count;
        public ActionPlaybackInboxEntry this[int index] =>
            (uint)index < (uint)m_Count
                ? m_Entries[index]
                : throw new ArgumentOutOfRangeException(nameof(index));

        internal ref readonly ActionPlaybackInboxEntry ElementAt(int index)
        {
            if ((uint)index >= (uint)m_Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return ref m_Entries[index];
        }

        public bool HasActiveReadLease => m_ActiveLease.IsValid;

        internal int Capacity => m_Entries.Length;
        internal ulong PublicationSequence => m_NextInboxSequence;

        internal void DiscardPublicationsAfter(ulong sequence)
        {
            RequireWritable();
            int write = 0;
            for (int i = 0; i < m_Count; i++)
                if (m_Entries[i].Sequence <= sequence)
                    m_Entries[write++] = m_Entries[i];
            Array.Clear(m_Entries, write, m_Count - write);
            m_Count = write;
        }

        public void Publish(ActionAnimationPlaybackCommand command)
        {
            if (!command.IsValid)
            {
                throw new ArgumentException(
                    "Action playback command is invalid.",
                    nameof(command));
            }
            ValidateAppendOrder(command);
            if (FindCommand(command) >= 0)
            {
                throw new InvalidOperationException(
                    "Action playback command identity already exists.");
            }
            if (m_Count == m_Entries.Length)
            {
                throw new InvalidOperationException(
                    $"Action playback inbox capacity '{m_Entries.Length}' was exceeded.");
            }
            var entry = new ActionPlaybackInboxEntry(
                NextSequence(),
                command);
            int insertion = m_Count;
            while (insertion > 0 &&
                   CompareCommands(in m_Entries[insertion - 1], in entry) > 0)
            {
                m_Entries[insertion] = m_Entries[insertion - 1];
                insertion--;
            }
            m_Entries[insertion] = entry;
            m_Count++;
        }

        public void Replace(
            EventId targetEventId,
            ActionAnimationPlaybackCommand replacement)
        {
            RequireWritable();
            if (!targetEventId.IsValid || !replacement.IsValid ||
                replacement.Kind == ActionAnimationPlaybackCommandKind.ProjectedSample)
                throw new ArgumentException(
                    "Action playback replacement is invalid.");
            int index = FindEvent(targetEventId);
            if (index < 0)
            {
                Publish(replacement);
                return;
            }
            ref readonly ActionPlaybackInboxEntry current =
                ref ElementAt(index);
            ref readonly ActionAnimationPlaybackCommand currentCommand =
                ref current.CommandRef;
            bool currentTerminal =
                currentCommand.Kind ==
                    ActionAnimationPlaybackCommandKind.Complete ||
                currentCommand.Kind ==
                    ActionAnimationPlaybackCommandKind.Release;
            bool replacementTerminal =
                replacement.Kind ==
                    ActionAnimationPlaybackCommandKind.Complete ||
                replacement.Kind ==
                    ActionAnimationPlaybackCommandKind.Release;
            if (currentCommand.Kind != replacement.Kind &&
                !(currentTerminal && replacementTerminal))
            {
                throw new InvalidOperationException(
                    "Action playback replacement changed command family.");
            }
            if (!targetEventId.Equals(replacement.EventId) &&
                FindEvent(replacement.EventId) >= 0)
            {
                throw new InvalidOperationException(
                    $"Action playback replacement EventId '{replacement.EventId}' already exists.");
            }
            ulong currentSequence = current.Sequence;
            ValidateReplacementOrder(currentSequence, replacement);
            RemoveAt(index);
            var entry = new ActionPlaybackInboxEntry(
                currentSequence,
                replacement);
            int insertion = m_Count;
            while (insertion > 0 &&
                   CompareCommands(in m_Entries[insertion - 1], in entry) > 0)
            {
                m_Entries[insertion] = m_Entries[insertion - 1];
                insertion--;
            }
            m_Entries[insertion] = entry;
            m_Count++;
        }

        public void Retire(ActionAnimationPlaybackCommand command)
        {
            RequireWritable();
            if (!command.IsValid || command.Kind == ActionAnimationPlaybackCommandKind.ProjectedSample)
            {
                throw new ArgumentException(
                    "Action playback retirement command is invalid.",
                    nameof(command));
            }
            int pendingIndex = FindEvent(command.EventId);
            if (pendingIndex >= 0)
            {
                RemoveAt(pendingIndex);
                return;
            }
            Publish(ActionAnimationPlaybackCommand.Withdraw(
                command.EventId,
                command.LocalLogicTick,
                command.PlaybackId,
                command.ActionInstanceId,
                command.AnimationChannelId,
                command.ProgramProducerId));
        }

        public ActionPlaybackInboxReadLease BeginRead()
        {
            if (HasActiveReadLease)
            {
                throw new InvalidOperationException(
                    "Action playback inbox already has an active read lease.");
            }
            m_NextLeaseIdentity++;
            if (m_NextLeaseIdentity == 0)
                m_NextLeaseIdentity++;
            ulong highWatermark = 0;
            for (int i = 0; i < m_Count; i++)
            {
                highWatermark = Math.Max(
                    highWatermark,
                    m_Entries[i].Sequence);
            }
            m_ActiveLease = new ActionPlaybackInboxReadLease(
                m_NextLeaseIdentity,
                highWatermark);
            return m_ActiveLease;
        }

        public void Commit(ActionPlaybackInboxReadLease lease)
        {
            RequireLease(lease);
            int write = 0;
            for (int i = 0; i < m_Count; i++)
            {
                if (m_Entries[i].Sequence <= lease.SequenceHighWatermark)
                    continue;
                m_Entries[write++] = m_Entries[i];
            }
            if (write < m_Count)
                Array.Clear(m_Entries, write, m_Count - write);
            m_Count = write;
            m_ActiveLease = default;
        }

        public void Discard(ActionPlaybackInboxReadLease lease)
        {
            RequireLease(lease);
            m_ActiveLease = default;
        }

        public void Reset()
        {
            if (m_Count > 0)
                Array.Clear(m_Entries, 0, m_Count);
            m_Count = 0;
            m_ActiveLease = default;
        }

        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<ActionPlaybackInboxEntry>
            IEnumerable<ActionPlaybackInboxEntry>.GetEnumerator() =>
                GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        int FindCommand(ActionAnimationPlaybackCommand command)
        {
            if (command.Kind != ActionAnimationPlaybackCommandKind.ProjectedSample)
                return FindEvent(command.EventId);
            for (int i = 0; i < m_Count; i++)
            {
                ref readonly ActionAnimationPlaybackCommand candidate =
                    ref ElementAt(i).CommandRef;
                if (candidate.Kind == ActionAnimationPlaybackCommandKind.ProjectedSample &&
                    candidate.PlaybackId.Equals(command.PlaybackId) &&
                    candidate.ProjectedSample.PresentationFrame == command.ProjectedSample.PresentationFrame)
                    return i;
            }
            return -1;
        }

        int FindEvent(EventId eventId)
        {
            for (int i = 0; i < m_Count; i++)
            {
                ref readonly ActionAnimationPlaybackCommand command =
                    ref ElementAt(i).CommandRef;
                if (command.Kind != ActionAnimationPlaybackCommandKind.ProjectedSample &&
                    command.EventId.Equals(eventId))
                    return i;
            }
            return -1;
        }

        void RemoveAt(int index)
        {
            m_Count--;
            for (int i = index; i < m_Count; i++)
                m_Entries[i] = m_Entries[i + 1];
            m_Entries[m_Count] = default;
        }

        void RequireWritable()
        {
            if (HasActiveReadLease)
            {
                throw new InvalidOperationException(
                    "Action playback inbox cannot mutate during an active read lease.");
            }
        }

        void RequireLease(ActionPlaybackInboxReadLease lease)
        {
            if (!lease.IsValid ||
                !m_ActiveLease.IsValid ||
                lease.Identity != m_ActiveLease.Identity ||
                lease.SequenceHighWatermark !=
                    m_ActiveLease.SequenceHighWatermark)
            {
                throw new InvalidOperationException(
                    "Action playback inbox read lease is invalid.");
            }
        }

        void ValidateAppendOrder(ActionAnimationPlaybackCommand command)
        {
            ulong latestSequence = 0;
            int latestIndex = -1;
            for (int i = 0; i < m_Count; i++)
            {
                ref readonly ActionPlaybackInboxEntry entry = ref ElementAt(i);
                if (!entry.CommandRef.PlaybackId.Equals(command.PlaybackId) ||
                    entry.Sequence <= latestSequence)
                {
                    continue;
                }
                latestSequence = entry.Sequence;
                latestIndex = i;
            }
            if (latestIndex < 0)
                return;
            ref readonly ActionAnimationPlaybackCommand latest =
                ref ElementAt(latestIndex).CommandRef;
            if (latest.ActionInstanceId != command.ActionInstanceId ||
                !latest.AnimationChannelId.Equals(command.AnimationChannelId) ||
                !string.Equals(
                    latest.ProgramProducerId,
                    command.ProgramProducerId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Action playback command ownership changed within one playback.");
            }
            if (command.LocalLogicTick < latest.LocalLogicTick)
            {
                throw new InvalidOperationException(
                    "Action playback command order moved backwards.");
            }
            if (!CanFollow(latest.Kind, command.Kind))
            {
                throw new InvalidOperationException(
                    "Action playback command cannot follow a terminal command.");
            }
        }

        void ValidateReplacementOrder(
            ulong sequence,
            ActionAnimationPlaybackCommand replacement)
        {
            for (int i = 0; i < m_Count; i++)
            {
                ref readonly ActionPlaybackInboxEntry candidate = ref ElementAt(i);
                if (candidate.Sequence == sequence ||
                    !candidate.CommandRef.PlaybackId.Equals(
                        replacement.PlaybackId))
                {
                    continue;
                }
                ref readonly ActionAnimationPlaybackCommand other =
                    ref candidate.CommandRef;
                if (other.ActionInstanceId != replacement.ActionInstanceId ||
                    !other.AnimationChannelId.Equals(
                        replacement.AnimationChannelId) ||
                    !string.Equals(
                        other.ProgramProducerId,
                        replacement.ProgramProducerId,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Action playback replacement conflicts with existing playback ownership.");
                }
                if (candidate.Sequence < sequence)
                {
                    if (replacement.LocalLogicTick < other.LocalLogicTick ||
                        !CanFollow(other.Kind, replacement.Kind))
                    {
                        throw new InvalidOperationException(
                            "Action playback replacement moved before its valid command order.");
                    }
                }
                else if (other.LocalLogicTick < replacement.LocalLogicTick ||
                         !CanFollow(replacement.Kind, other.Kind))
                {
                    throw new InvalidOperationException(
                        "Action playback replacement moved after its valid command order.");
                }
            }
        }

        ulong NextSequence()
        {
            m_NextInboxSequence++;
            if (m_NextInboxSequence == 0)
                m_NextInboxSequence++;
            return m_NextInboxSequence;
        }

        static bool CanFollow(
            ActionAnimationPlaybackCommandKind previous,
            ActionAnimationPlaybackCommandKind next)
        {
            if (previous == ActionAnimationPlaybackCommandKind.Withdraw)
                return next == ActionAnimationPlaybackCommandKind.Withdraw || next == ActionAnimationPlaybackCommandKind.Select;
            if (next == ActionAnimationPlaybackCommandKind.Withdraw)
                return previous != ActionAnimationPlaybackCommandKind.Release;
            if (previous == ActionAnimationPlaybackCommandKind.Release)
                return next == ActionAnimationPlaybackCommandKind.Release;
            if (previous == ActionAnimationPlaybackCommandKind.Complete)
            {
                return next == ActionAnimationPlaybackCommandKind.Complete ||
                       next == ActionAnimationPlaybackCommandKind.Release;
            }
            return true;
        }

        static int CompareCommands(
            in ActionPlaybackInboxEntry left,
            in ActionPlaybackInboxEntry right)
        {
            int tick = left.CommandRef.LocalLogicTick.CompareTo(
                right.CommandRef.LocalLogicTick);
            return tick != 0
                ? tick
                : left.Sequence.CompareTo(right.Sequence);
        }

        public struct Enumerator : IEnumerator<ActionPlaybackInboxEntry>
        {
            readonly ActionPlaybackCommandInbox m_Owner;
            int m_Index;

            internal Enumerator(ActionPlaybackCommandInbox owner)
            {
                m_Owner = owner;
                m_Index = -1;
            }

            public ActionPlaybackInboxEntry Current => m_Owner[m_Index];
            object IEnumerator.Current => Current;
            public bool MoveNext() => ++m_Index < m_Owner.Count;
            public void Reset() => m_Index = -1;
            public void Dispose()
            {
            }
        }
    }
}
