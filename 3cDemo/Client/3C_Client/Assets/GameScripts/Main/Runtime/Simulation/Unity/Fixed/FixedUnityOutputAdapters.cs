using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using FixedPresentationCommand = ThirdPersonSimulation.Fixed.PresentationCommand;
using FixedWorldBodyState = ThirdPersonSimulation.Fixed.WorldBodyState;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    public sealed class FixedUnityPresentationOutputAdapter : IFixedPresentationCommitOutputPort
    {
        readonly ActorId m_ActorId;
        readonly CharacterPresentationProjection m_Projection;
        readonly ICharacterPresentationRuntime m_Runtime;
        readonly int m_MaximumTrackedRecords;
        readonly Dictionary<EventId, ActivePresentationRecord> m_ByEvent =
            new Dictionary<EventId, ActivePresentationRecord>();
        readonly Dictionary<PresentationStateKey, List<EventId>> m_ByState =
            new Dictionary<PresentationStateKey, List<EventId>>();
        readonly Dictionary<PresentationStateKey, ActivePresentationRecord> m_Applied =
            new Dictionary<PresentationStateKey, ActivePresentationRecord>();
        readonly Dictionary<EventId, ActivePresentationRecord> m_DeferredAnimationRetirements =
            new Dictionary<EventId, ActivePresentationRecord>();
        readonly HashSet<PresentationStateKey> m_Dirty = new HashSet<PresentationStateKey>();
        readonly HashSet<PresentationStateKey> m_ConfirmedAnimationTerminals =
            new HashSet<PresentationStateKey>();
        readonly List<CharacterPresentationCommand> m_ConfirmedPublishes =
            new List<CharacterPresentationCommand>();
        bool m_CommitActive;

        public FixedUnityPresentationOutputAdapter(
            ActorId actorId,
            CharacterPresentationProjection projection,
            ICharacterPresentationRuntime runtime,
            int maximumActiveRecords)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Fixed Presentation output requires an ActorId.", nameof(actorId));
            if (maximumActiveRecords <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumActiveRecords));
            m_ActorId = actorId;
            m_Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            m_Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            m_MaximumTrackedRecords = maximumActiveRecords;
        }

        public int ActiveRecordCount => m_ByEvent.Count;

        public void BeginCommit()
        {
            if (m_CommitActive)
                throw new InvalidOperationException("Fixed Presentation output commit is already active.");
            m_Dirty.Clear();
            m_ConfirmedPublishes.Clear();
            m_CommitActive = true;
        }

        public void Publish(FixedPresentationCommand command)
        {
            RequireCommit();
            CharacterPresentationCommand converted = Convert(command);
            if (TryBuildStateKey(converted, out PresentationStateKey key))
                AddRecord(key, converted);
            else
                m_ConfirmedPublishes.Add(converted);
        }

        public void Replace(EventId targetEventId, FixedPresentationCommand command)
        {
            RequireCommit();
            CharacterPresentationCommand converted = Convert(command);
            if (!TryBuildStateKey(converted, out PresentationStateKey key))
                throw new InvalidOperationException($"Confirmed-only Presentation command '{command.Kind}' cannot enter replace lifecycle.");
            if (!m_ByEvent.TryGetValue(targetEventId, out ActivePresentationRecord target))
                throw new InvalidOperationException($"Fixed Presentation replacement target '{targetEventId}' is absent from rollback history.");
            RemoveRecord(target);
            AddRecord(key, converted);
        }

        public void Retire(ActorId actorId, EventId sourceEventId, EventId targetEventId)
        {
            RequireCommit();
            if (actorId != m_ActorId || !sourceEventId.IsValid || !targetEventId.IsValid)
                throw new InvalidOperationException("Fixed Presentation retirement identity is invalid.");
            if (!m_ByEvent.TryGetValue(targetEventId, out ActivePresentationRecord record))
                throw new InvalidOperationException($"Fixed Presentation retirement target '{targetEventId}' is absent from rollback history.");
            RemoveRecord(record);
        }

        public void CompleteCommit(ulong confirmedTick)
        {
            RequireCommit();
            try
            {
                ReconcileDirtyStates(confirmedTick);
                FlushDeferredAnimationRetirements(confirmedTick);
                for (int i = 0; i < m_ConfirmedPublishes.Count; i++)
                    m_Runtime.Publish(m_ConfirmedPublishes[i]);
                PruneConfirmed(confirmedTick);
            }
            finally
            {
                m_Dirty.Clear();
                m_ConfirmedPublishes.Clear();
                m_CommitActive = false;
            }
        }

        public void AbortCommit()
        {
            m_Dirty.Clear();
            m_ConfirmedPublishes.Clear();
            m_CommitActive = false;
        }

        public void Reset()
        {
            m_ByEvent.Clear();
            m_ByState.Clear();
            m_Applied.Clear();
            m_DeferredAnimationRetirements.Clear();
            m_Dirty.Clear();
            m_ConfirmedAnimationTerminals.Clear();
            m_ConfirmedPublishes.Clear();
            m_CommitActive = false;
            m_Runtime.Reset();
        }

        void AddRecord(PresentationStateKey key, CharacterPresentationCommand command)
        {
            if (m_ByEvent.ContainsKey(command.Header.EventId))
                throw new InvalidOperationException($"Fixed Presentation EventId '{command.Header.EventId}' is already tracked.");
            if (m_ByEvent.Count >= m_MaximumTrackedRecords)
                throw new InvalidOperationException($"Fixed Presentation tracked record capacity '{m_MaximumTrackedRecords}' is exhausted.");
            var record = new ActivePresentationRecord(key, command);
            m_ByEvent[command.Header.EventId] = record;
            if (!m_ByState.TryGetValue(key, out List<EventId> events))
            {
                events = new List<EventId>();
                m_ByState.Add(key, events);
            }
            events.Add(command.Header.EventId);
            m_Dirty.Add(key);
        }

        void RemoveRecord(ActivePresentationRecord record)
        {
            m_ByEvent.Remove(record.Command.Header.EventId);
            if (m_ByState.TryGetValue(record.Key, out List<EventId> events))
            {
                events.Remove(record.Command.Header.EventId);
                if (events.Count == 0)
                    m_ByState.Remove(record.Key);
            }
            m_Dirty.Add(record.Key);
        }

        void ReconcileDirtyStates(ulong confirmedTick)
        {
            var keys = new List<PresentationStateKey>(m_Dirty);
            keys.Sort();
            for (int i = 0; i < keys.Count; i++)
            {
                if (!TryResolveLatest(keys[i], out ActivePresentationRecord current) ||
                    !IsTerminal(current.Command) ||
                    current.Command.Header.Tick.Value > confirmedTick)
                {
                    continue;
                }
                m_ConfirmedAnimationTerminals.Add(keys[i]);
            }
            for (int i = 0; i < keys.Count; i++)
            {
                PresentationStateKey key = keys[i];
                bool hasCurrent = TryResolveLatest(key, out ActivePresentationRecord current);
                bool hasApplied = m_Applied.TryGetValue(key, out ActivePresentationRecord applied);
                if (hasCurrent &&
                    IsTerminal(current.Command) &&
                    current.Command.Header.Tick.Value > confirmedTick)
                {
                    continue;
                }
                if (hasCurrent && hasApplied)
                {
                    if (!current.Command.Header.EventId.Equals(applied.Command.Header.EventId))
                    {
                        if (IsAnimationPlaybackCommand(current.Command) &&
                            IsAnimationPlaybackCommand(applied.Command) &&
                            !SamePlayback(current.Command, applied.Command))
                        {
                            PublishOrRestore(current);
                            if (HasConfirmedTerminal(applied.Command))
                                RemoveDeferredAnimationRetirements(applied.Command);
                            else
                                QueueAnimationRetirement(applied);
                        }
                        else
                        {
                            m_Runtime.Replace(applied.Command, current.Command);
                        }
                        m_Applied[key] = current;
                    }
                }
                else if (hasCurrent)
                {
                    if (IsTerminal(current.Command))
                    {
                        RemoveDeferredAnimationRetirements(current.Command);
                        m_Runtime.Publish(current.Command);
                    }
                    else
                        PublishOrRestore(current);
                    m_Applied[key] = current;
                }
                else if (hasApplied)
                {
                    if (IsAnimationPlaybackCommand(applied.Command))
                    {
                        if (HasConfirmedTerminal(applied.Command))
                            RemoveDeferredAnimationRetirements(applied.Command);
                        else
                            QueueAnimationRetirement(applied);
                    }
                    else
                    {
                        m_Runtime.Retire(applied.Command);
                    }
                    m_Applied.Remove(key);
                }
            }
        }

        void PublishOrRestore(ActivePresentationRecord record)
        {
            if (!TryTakeDeferredAnimationRetirement(record, out ActivePresentationRecord deferred))
            {
                m_Runtime.Publish(record.Command);
                return;
            }
            if (!deferred.Key.Equals(record.Key) ||
                !deferred.Command.Header.EventId.Equals(record.Command.Header.EventId))
            {
                if (!deferred.Key.Equals(record.Key))
                    m_Runtime.Publish(record.Command);
                else
                    m_Runtime.Replace(deferred.Command, record.Command);
            }
        }

        void QueueAnimationRetirement(ActivePresentationRecord record)
        {
            if (IsTerminal(record.Command) ||
                m_DeferredAnimationRetirements.ContainsKey(record.Command.Header.EventId))
            {
                return;
            }
            foreach (ActivePresentationRecord deferred in m_DeferredAnimationRetirements.Values)
            {
                if (SamePlayback(deferred.Command, record.Command))
                    return;
            }
            m_DeferredAnimationRetirements.Add(record.Command.Header.EventId, record);
        }

        void FlushDeferredAnimationRetirements(ulong confirmedTick)
        {
            var remove = new List<EventId>();
            foreach (KeyValuePair<EventId, ActivePresentationRecord> pair in m_DeferredAnimationRetirements)
            {
                if (pair.Value.Command.Header.Tick.Value > confirmedTick)
                    continue;
                m_Runtime.Retire(pair.Value.Command);
                remove.Add(pair.Key);
            }
            for (int i = 0; i < remove.Count; i++)
                m_DeferredAnimationRetirements.Remove(remove[i]);
        }

        bool TryTakeDeferredAnimationRetirement(
            ActivePresentationRecord current,
            out ActivePresentationRecord record)
        {
            record = default;
            EventId found = default;
            bool hasFound = false;
            foreach (KeyValuePair<EventId, ActivePresentationRecord> pair in m_DeferredAnimationRetirements)
            {
                if (!SamePlayback(pair.Value.Command, current.Command))
                    continue;
                found = pair.Key;
                record = pair.Value;
                hasFound = true;
                break;
            }
            if (hasFound)
            {
                m_DeferredAnimationRetirements.Remove(found);
                return true;
            }
            return false;
        }

        void RemoveDeferredAnimationRetirements(CharacterPresentationCommand command)
        {
            var remove = new List<EventId>();
            foreach (KeyValuePair<EventId, ActivePresentationRecord> pair in m_DeferredAnimationRetirements)
            {
                if (SamePlayback(pair.Value.Command, command))
                    remove.Add(pair.Key);
            }
            for (int i = 0; i < remove.Count; i++)
                m_DeferredAnimationRetirements.Remove(remove[i]);
        }

        bool HasConfirmedTerminal(CharacterPresentationCommand command)
        {
            return m_ConfirmedAnimationTerminals.Contains(
                new PresentationStateKey(
                    "animation-terminal",
                    command.ProducerId,
                    command.ProducerGeneration));
        }

        static bool IsAnimationPlaybackCommand(CharacterPresentationCommand command)
        {
            return command.Kind == CharacterPresentationCommandKind.SelectProducer ||
                   command.Kind == CharacterPresentationCommandKind.SampleProducer ||
                   IsTerminal(command);
        }

        static bool IsTerminal(CharacterPresentationCommand command)
        {
            return command.Kind == CharacterPresentationCommandKind.CompleteProducer ||
                   command.Kind == CharacterPresentationCommandKind.ReleaseProducer;
        }

        static bool SamePlayback(
            CharacterPresentationCommand left,
            CharacterPresentationCommand right)
        {
            return string.Equals(left.ProducerId, right.ProducerId, StringComparison.Ordinal) &&
                   left.ProducerGeneration == right.ProducerGeneration;
        }

        bool TryResolveLatest(PresentationStateKey key, out ActivePresentationRecord latest)
        {
            latest = default;
            if (!m_ByState.TryGetValue(key, out List<EventId> events) || events.Count == 0)
                return false;
            bool found = false;
            for (int i = 0; i < events.Count; i++)
            {
                if (!m_ByEvent.TryGetValue(events[i], out ActivePresentationRecord candidate))
                    throw new InvalidOperationException($"Fixed Presentation state '{key}' references a missing EventId '{events[i]}'.");
                if (!found || IsNewer(candidate.Command.Header, latest.Command.Header))
                {
                    latest = candidate;
                    found = true;
                }
            }
            return found;
        }

        void PruneConfirmed(ulong confirmedTick)
        {
            if (confirmedTick == 0)
                return;
            var keys = new List<PresentationStateKey>(m_ByState.Keys);
            keys.Sort();
            for (int keyIndex = 0; keyIndex < keys.Count; keyIndex++)
            {
                PresentationStateKey key = keys[keyIndex];
                if (!m_ByState.TryGetValue(key, out List<EventId> events))
                    continue;
                ActivePresentationRecord baseline = default;
                bool hasBaseline = false;
                bool hasUnconfirmed = false;
                var remove = new List<EventId>();
                for (int i = 0; i < events.Count; i++)
                {
                    ActivePresentationRecord record = m_ByEvent[events[i]];
                    if (record.Command.Header.Tick.Value > confirmedTick)
                    {
                        hasUnconfirmed = true;
                        continue;
                    }
                    if (!hasBaseline || IsNewer(record.Command.Header, baseline.Command.Header))
                    {
                        if (hasBaseline)
                            remove.Add(baseline.Command.Header.EventId);
                        baseline = record;
                        hasBaseline = true;
                    }
                    else
                    {
                        remove.Add(record.Command.Header.EventId);
                    }
                }
                for (int i = 0; i < remove.Count; i++)
                    RemoveHistoryRecord(remove[i]);
                if (!hasBaseline || hasUnconfirmed)
                    continue;
                if (string.Equals(key.Channel, "animation-terminal", StringComparison.Ordinal))
                {
                    RemoveHistory(key);
                    m_Applied.Remove(key);
                    var sampleKey = new PresentationStateKey("animation-sample", key.Producer, key.Generation);
                    RemoveHistory(sampleKey);
                    m_Applied.Remove(sampleKey);
                }
                else if (string.Equals(key.Channel, "camera", StringComparison.Ordinal) &&
                         baseline.Command.Weight <= 0f)
                {
                    RemoveHistory(key);
                    m_Applied.Remove(key);
                }
            }
        }

        void RemoveHistoryRecord(EventId eventId)
        {
            if (!m_ByEvent.TryGetValue(eventId, out ActivePresentationRecord record))
                return;
            m_ByEvent.Remove(eventId);
            if (!m_ByState.TryGetValue(record.Key, out List<EventId> events))
                return;
            events.Remove(eventId);
            if (events.Count == 0)
                m_ByState.Remove(record.Key);
        }

        void RemoveHistory(PresentationStateKey key)
        {
            if (!m_ByState.TryGetValue(key, out List<EventId> events))
                return;
            for (int i = 0; i < events.Count; i++)
                m_ByEvent.Remove(events[i]);
            m_ByState.Remove(key);
        }

        void RequireCommit()
        {
            if (!m_CommitActive)
                throw new InvalidOperationException("Fixed Presentation output mutation requires an active rollback commit.");
        }

        static bool IsNewer(
            CharacterPresentationEventHeader candidate,
            CharacterPresentationEventHeader current)
        {
            return candidate.Tick.Value > current.Tick.Value ||
                   candidate.Tick.Value == current.Tick.Value && candidate.Sequence > current.Sequence;
        }

        bool TryBuildStateKey(CharacterPresentationCommand command, out PresentationStateKey key)
        {
            if (!m_Projection.TryGetProducer(command.ProducerId, out CharacterPresentationProducerEntry producer))
                throw new InvalidOperationException($"Fixed Presentation producer '{command.ProducerId}' is absent from the Projection.");
            switch (command.Kind)
            {
                case CharacterPresentationCommandKind.SelectProducer:
                    key = new PresentationStateKey("animation-selection", producer.AnimationChannelId.Value, 0);
                    return true;
                case CharacterPresentationCommandKind.SampleProducer:
                    key = new PresentationStateKey("animation-sample", command.ProducerId, command.ProducerGeneration);
                    return true;
                case CharacterPresentationCommandKind.CompleteProducer:
                case CharacterPresentationCommandKind.ReleaseProducer:
                    key = new PresentationStateKey("animation-terminal", command.ProducerId, command.ProducerGeneration);
                    return true;
                case CharacterPresentationCommandKind.Camera:
                    key = new PresentationStateKey("camera", command.ProducerId, command.ProducerGeneration);
                    return true;
                case CharacterPresentationCommandKind.Cue:
                case CharacterPresentationCommandKind.Vfx:
                case CharacterPresentationCommandKind.Ui:
                    key = default;
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(command.Kind), command.Kind, null);
            }
        }

        CharacterPresentationCommand Convert(FixedPresentationCommand command)
        {
            if (command.Header.ActorId != m_ActorId)
                throw new InvalidOperationException("Fixed Presentation command targets another Actor.");
            var header = new CharacterPresentationEventHeader(
                command.Header.EventId,
                command.Header.ActorId,
                command.Header.Tick,
                command.Header.Activation,
                command.Header.Sequence,
                command.Header.Channel);
            return new CharacterPresentationCommand(
                header,
                (CharacterPresentationCommandKind)(byte)command.Kind,
                command.ProducerId,
                command.SampleTime.ToSingle(),
                command.Weight.ToSingle(),
                command.ProducerGeneration,
                command.Cycle,
                command.SourceActionInstanceId,
                command.VisualTimeScale.ToSingle());
        }

        readonly struct ActivePresentationRecord
        {
            public ActivePresentationRecord(PresentationStateKey key, CharacterPresentationCommand command)
            {
                Key = key;
                Command = command;
            }

            public PresentationStateKey Key { get; }
            public CharacterPresentationCommand Command { get; }
        }

        readonly struct PresentationStateKey : IEquatable<PresentationStateKey>, IComparable<PresentationStateKey>
        {
            public PresentationStateKey(string channel, string producer, ulong generation)
            {
                Channel = channel ?? string.Empty;
                Producer = producer ?? string.Empty;
                Generation = generation;
            }

            public string Channel { get; }
            public string Producer { get; }
            public ulong Generation { get; }
            public bool Equals(PresentationStateKey other) =>
                Generation == other.Generation &&
                string.Equals(Channel, other.Channel, StringComparison.Ordinal) &&
                string.Equals(Producer, other.Producer, StringComparison.Ordinal);
            public override bool Equals(object obj) => obj is PresentationStateKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Channel, Producer, Generation);
            public int CompareTo(PresentationStateKey other)
            {
                int channel = ChannelOrder(Channel).CompareTo(ChannelOrder(other.Channel));
                if (channel != 0)
                    return channel;
                channel = string.CompareOrdinal(Channel, other.Channel);
                if (channel != 0)
                    return channel;
                int producer = string.CompareOrdinal(Producer, other.Producer);
                return producer != 0 ? producer : Generation.CompareTo(other.Generation);
            }
            public override string ToString() => $"{Channel}/{Producer}/{Generation}";

            static int ChannelOrder(string channel)
            {
                return channel switch
                {
                    "animation-selection" => 0,
                    "animation-sample" => 1,
                    "animation-terminal" => 2,
                    "camera" => 3,
                    _ => throw new InvalidOperationException(
                        $"Fixed Presentation state channel '{channel}' has no reconciliation order.")
                };
            }
        }
    }

    public static class FixedUnityPresentationBoundary
    {
        public static CharacterPresentationBodyState Convert(FixedWorldBodyState body)
        {
            return new CharacterPresentationBodyState(
                body.ActorId,
                new Vector3(body.Position.X.ToSingle(), body.Position.Y.ToSingle(), body.Position.Z.ToSingle()),
                Quaternion.Euler(0f, body.Yaw.Degrees.ToSingle(), 0f),
                new Vector3(body.Velocity.X.ToSingle(), body.Velocity.Y.ToSingle(), body.Velocity.Z.ToSingle()),
                body.Grounded);
        }
    }
}
