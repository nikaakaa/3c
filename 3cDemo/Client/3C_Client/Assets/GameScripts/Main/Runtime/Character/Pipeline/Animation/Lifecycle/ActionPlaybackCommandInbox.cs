using System;
using System.Collections;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    public readonly struct ActionPlaybackInboxEntry
    {
        internal ActionPlaybackInboxEntry(
            ulong sequence,
            ActionAnimationPlaybackCommand command)
        {
            Sequence = sequence;
            Command = command;
            if (!IsValid)
                throw new ArgumentException(
                    "Action playback inbox entry is invalid.");
        }

        public ulong Sequence { get; }
        public ActionAnimationPlaybackCommand Command { get; }
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
        public bool IsValid =>
            Identity != 0 && SequenceHighWatermark != 0;
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
        public bool HasActiveReadLease => m_ActiveLease.IsValid;

        public void Publish(ActionAnimationPlaybackCommand command)
        {
            RequireWritable();
            if (!command.IsValid)
            {
                throw new ArgumentException(
                    "Action playback command is invalid.",
                    nameof(command));
            }
            ValidateAppendOrder(command);
            if (FindEvent(command.EventId) >= 0)
            {
                throw new InvalidOperationException(
                    $"Action playback EventId '{command.EventId}' already exists.");
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
                   CompareCommands(m_Entries[insertion - 1], entry) > 0)
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
            if (!targetEventId.IsValid || !replacement.IsValid)
                throw new ArgumentException(
                    "Action playback replacement is invalid.");
            int index = FindEvent(targetEventId);
            if (index < 0)
            {
                Publish(replacement);
                return;
            }
            ActionPlaybackInboxEntry current = m_Entries[index];
            ActionAnimationPlaybackCommand currentCommand = current.Command;
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
            ValidateReplacementOrder(current.Sequence, replacement);
            RemoveAt(index);
            var entry = new ActionPlaybackInboxEntry(
                current.Sequence,
                replacement);
            int insertion = m_Count;
            while (insertion > 0 &&
                   CompareCommands(m_Entries[insertion - 1], entry) > 0)
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
            if (!command.IsValid)
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
            Publish(ActionAnimationPlaybackCommand.Release(
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
            if (m_Count == 0)
                return default;
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
                ActionPlaybackInboxEntry entry = m_Entries[i];
                if (entry.Sequence <= lease.SequenceHighWatermark)
                    continue;
                m_Entries[write++] = entry;
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

        int FindEvent(EventId eventId)
        {
            for (int i = 0; i < m_Count; i++)
            {
                if (m_Entries[i].Command.EventId.Equals(eventId))
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
            ActionAnimationPlaybackCommand latest = default;
            for (int i = 0; i < m_Count; i++)
            {
                ActionPlaybackInboxEntry entry = m_Entries[i];
                if (!entry.Command.PlaybackId.Equals(command.PlaybackId) ||
                    entry.Sequence <= latestSequence)
                {
                    continue;
                }
                latestSequence = entry.Sequence;
                latest = entry.Command;
            }
            if (latestSequence == 0)
                return;
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
                ActionPlaybackInboxEntry candidate = m_Entries[i];
                if (candidate.Sequence == sequence ||
                    !candidate.Command.PlaybackId.Equals(
                        replacement.PlaybackId))
                {
                    continue;
                }
                ActionAnimationPlaybackCommand other = candidate.Command;
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
            ActionPlaybackInboxEntry left,
            ActionPlaybackInboxEntry right)
        {
            int tick = left.Command.LocalLogicTick.CompareTo(
                right.Command.LocalLogicTick);
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
