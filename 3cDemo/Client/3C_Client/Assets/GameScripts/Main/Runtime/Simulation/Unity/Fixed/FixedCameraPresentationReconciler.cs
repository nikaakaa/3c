using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;

using ActivePresentationRecord = ThirdPersonCharacter.Pipeline.Simulation.Fixed.FixedCameraPresentationRecord;
using PresentationStateKey = ThirdPersonCharacter.Pipeline.Simulation.Fixed.FixedCameraPresentationStateKey;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    internal sealed class FixedCameraPresentationReconciler
    {
        readonly ICharacterPresentationRuntime m_Runtime;
        readonly Dictionary<PresentationStateKey, List<ActivePresentationRecord>> m_Applied =
            new Dictionary<PresentationStateKey, List<ActivePresentationRecord>>();

        public FixedCameraPresentationReconciler(ICharacterPresentationRuntime runtime)
        {
            m_Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        public void Reset() => m_Applied.Clear();

        public void Reconcile(
            PresentationStateKey key,
            ulong confirmedTick,
            IFixedCameraPresentationHistory history)
        {
            List<ActivePresentationRecord> current = history.CollectCameraRecords(key, confirmedTick);
            m_Applied.TryGetValue(key, out List<ActivePresentationRecord> applied);
            applied = applied ?? new List<ActivePresentationRecord>();
            bool forced = IsCameraScopeForced(key);
            bool requiresReplay = false;
            var currentByEvent = new Dictionary<EventId, ActivePresentationRecord>();
            for (int i = 0; i < current.Count; i++)
                currentByEvent[current[i].Command.Header.EventId] = current[i];
            var appliedByEvent = new Dictionary<EventId, ActivePresentationRecord>();
            for (int i = 0; i < applied.Count; i++)
                appliedByEvent[applied[i].Command.Header.EventId] = applied[i];

            for (int i = 0; i < applied.Count; i++)
            {
                ActivePresentationRecord previous = applied[i];
                if (!currentByEvent.TryGetValue(
                        previous.Command.Header.EventId,
                        out ActivePresentationRecord replacement))
                {
                    requiresReplay = true;
                    if (!IsTerminal(previous.Command))
                        m_Runtime.Retire(previous.Command);
                    continue;
                }
                if (!SameCommand(previous.Command, replacement.Command))
                {
                    if (!forced)
                        m_Runtime.Replace(previous.Command, replacement.Command);
                }
            }

            if (key.IsCameraForce && applied.Count > 0 && current.Count == 0)
            {
                m_Applied.Remove(key);
                if (HasAppliedForceScope(applied[0].Command) ||
                    history.HasCameraForceScope(applied[0].Command, confirmedTick))
                    return;
                m_Runtime.Retire(applied[0].Command);
                RestoreCameraScope(applied[0].Command, confirmedTick, history);
                return;
            }

            if (!forced)
            {
                for (int i = 0; i < current.Count; i++)
                {
                    ActivePresentationRecord next = current[i];
                    if (appliedByEvent.ContainsKey(next.Command.Header.EventId))
                        continue;
                    for (int j = 0; j < applied.Count; j++)
                    {
                        if (!IsNewer(next.Command.Header, applied[j].Command.Header))
                        {
                            requiresReplay = true;
                            break;
                        }
                    }
                    if (requiresReplay)
                        break;
                }
                if (requiresReplay)
                {
                    for (int i = 0; i < current.Count; i++)
                        m_Runtime.Publish(current[i].Command);
                }
                else
                {
                    for (int i = 0; i < current.Count; i++)
                    {
                        ActivePresentationRecord next = current[i];
                        if (!appliedByEvent.ContainsKey(next.Command.Header.EventId))
                            m_Runtime.Publish(next.Command);
                    }
                }
            }

            if (current.Count == 0)
                m_Applied.Remove(key);
            else
                m_Applied[key] = new List<ActivePresentationRecord>(current);
        }

        public void RemoveAppliedEvent(PresentationStateKey key, EventId eventId)
        {
            if (!m_Applied.TryGetValue(key, out List<ActivePresentationRecord> applied))
                return;
            applied.RemoveAll(value => value.Command.Header.EventId.Equals(eventId));
            if (applied.Count == 0)
                m_Applied.Remove(key);
        }

        public void ClearState(PresentationStateKey key) => m_Applied.Remove(key);

        public void ClearScope(CharacterPresentationCommand command)
        {
            var remove = new List<PresentationStateKey>();
            foreach (PresentationStateKey key in m_Applied.Keys)
            {
                if ((key.IsCameraForce || string.Equals(key.Channel, "camera", StringComparison.Ordinal)) &&
                    string.Equals(key.Producer, command.ProducerId, StringComparison.Ordinal) &&
                    key.Generation == command.ProducerGeneration &&
                    key.SourceActionInstanceId == command.SourceActionInstanceId)
                    remove.Add(key);
            }
            for (int i = 0; i < remove.Count; i++)
                m_Applied.Remove(remove[i]);
        }

        void RestoreCameraScope(
            CharacterPresentationCommand force,
            ulong confirmedTick,
            IFixedCameraPresentationHistory history)
        {
            List<PresentationStateKey> keys = history.CollectCameraStateKeys(force);
            keys.Sort();
            for (int i = 0; i < keys.Count; i++)
            {
                m_Applied.Remove(keys[i]);
                Reconcile(keys[i], confirmedTick, history);
            }
        }

        bool IsCameraScopeForced(PresentationStateKey key)
        {
            if (key.IsCameraForce)
                return false;
            FixedCameraPresentationScopeKey scope = key.Scope;
            return HasAppliedForceScope(scope);
        }

        bool HasAppliedForceScope(CharacterPresentationCommand command) =>
            HasAppliedForceScope(new FixedCameraPresentationScopeKey(
                command.ProducerId,
                command.ProducerGeneration,
                command.SourceActionInstanceId));

        bool HasAppliedForceScope(FixedCameraPresentationScopeKey scope)
        {
            foreach (KeyValuePair<PresentationStateKey, List<ActivePresentationRecord>> pair in m_Applied)
            {
                if (pair.Key.IsCameraForce && pair.Key.Scope.Equals(scope) && pair.Value.Count > 0)
                    return true;
            }
            return false;
        }

        static bool SameCommand(
            CharacterPresentationCommand left,
            CharacterPresentationCommand right)
        {
            return left.Kind == right.Kind &&
                   left.Header.Tick.Value == right.Header.Tick.Value &&
                   left.Header.Sequence == right.Header.Sequence &&
                   left.Header.Activation.Equals(right.Header.Activation) &&
                   string.Equals(left.Header.Channel, right.Header.Channel, StringComparison.Ordinal) &&
                   string.Equals(left.ProducerId, right.ProducerId, StringComparison.Ordinal) &&
                   left.SampleTime.Equals(right.SampleTime) &&
                   left.Weight.Equals(right.Weight) &&
                   left.ProducerGeneration == right.ProducerGeneration &&
                   left.Cycle == right.Cycle &&
                   left.SourceActionInstanceId == right.SourceActionInstanceId &&
                   left.VisualTimeScale.Equals(right.VisualTimeScale);
        }

        static bool IsTerminal(CharacterPresentationCommand command) =>
            command.Kind == CharacterPresentationCommandKind.CompleteProducer ||
            command.Kind == CharacterPresentationCommandKind.ReleaseProducer ||
            command.Kind == CharacterPresentationCommandKind.ForceReleaseProducer;

        static bool IsNewer(
            CharacterPresentationEventHeader candidate,
            CharacterPresentationEventHeader current)
        {
            return candidate.Tick.Value > current.Tick.Value ||
                   candidate.Tick.Value == current.Tick.Value && candidate.Sequence > current.Sequence;
        }
    }
}
