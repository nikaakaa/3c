using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Attributes;

namespace ThirdPersonGameplay.Effects
{
    internal sealed class GameplayEffectPredictionJournal
    {
        readonly Dictionary<ulong, List<GameplayEffectPredictionRecord>> m_ByPredictionKey = new Dictionary<ulong, List<GameplayEffectPredictionRecord>>();

        public void Add(GameplayEffectPredictionRecord record)
        {
            if (record == null || record.PredictionKey == 0)
                throw new ArgumentException("Prediction record requires a prediction key.", nameof(record));
            if (!m_ByPredictionKey.TryGetValue(record.PredictionKey, out List<GameplayEffectPredictionRecord> records))
            {
                records = new List<GameplayEffectPredictionRecord>();
                m_ByPredictionKey.Add(record.PredictionKey, records);
            }
            records.Add(record);
        }

        public bool TryGet(ulong predictionKey, out IReadOnlyList<GameplayEffectPredictionRecord> records)
        {
            if (m_ByPredictionKey.TryGetValue(predictionKey, out List<GameplayEffectPredictionRecord> values))
            {
                records = values;
                return true;
            }
            records = Array.Empty<GameplayEffectPredictionRecord>();
            return false;
        }

        public bool Remove(ulong predictionKey, out List<GameplayEffectPredictionRecord> records)
        {
            if (!m_ByPredictionKey.TryGetValue(predictionKey, out records))
                return false;
            m_ByPredictionKey.Remove(predictionKey);
            return true;
        }

        public void MarkConfirmed(ulong predictionKey, GameplayEffectId effectId)
        {
            if (!m_ByPredictionKey.TryGetValue(predictionKey, out List<GameplayEffectPredictionRecord> records))
                return;
            for (int i = 0; i < records.Count; i++)
            {
                if (!effectId.IsValid || records[i].EffectId == effectId)
                    records[i].Confirmed = true;
            }
        }

        public void ClearAction(ulong actionInstanceId)
        {
            if (actionInstanceId == 0)
                return;
            var keys = new List<ulong>();
            foreach (KeyValuePair<ulong, List<GameplayEffectPredictionRecord>> pair in m_ByPredictionKey)
            {
                bool remove = true;
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    if (pair.Value[i].ActionInstanceId != actionInstanceId || !pair.Value[i].Confirmed)
                    {
                        remove = false;
                        break;
                    }
                }
                if (remove)
                    keys.Add(pair.Key);
            }
            for (int i = 0; i < keys.Count; i++)
                m_ByPredictionKey.Remove(keys[i]);
        }

        public void Clear()
        {
            m_ByPredictionKey.Clear();
        }

        public GameplayEffectPredictionJournalSnapshot CaptureTransactionSnapshot()
        {
            var records = new Dictionary<ulong, List<GameplayEffectPredictionRecord>>();
            foreach (KeyValuePair<ulong, List<GameplayEffectPredictionRecord>> pair in m_ByPredictionKey)
                records.Add(pair.Key, new List<GameplayEffectPredictionRecord>(pair.Value));
            return new GameplayEffectPredictionJournalSnapshot(records);
        }

        public void RestoreTransactionSnapshot(GameplayEffectPredictionJournalSnapshot snapshot)
        {
            m_ByPredictionKey.Clear();
            foreach (KeyValuePair<ulong, List<GameplayEffectPredictionRecord>> pair in snapshot.Records)
                m_ByPredictionKey.Add(pair.Key, new List<GameplayEffectPredictionRecord>(pair.Value));
        }
    }

    internal sealed class GameplayEffectPredictionJournalSnapshot
    {
        public GameplayEffectPredictionJournalSnapshot(
            Dictionary<ulong, List<GameplayEffectPredictionRecord>> records)
        {
            Records = records;
        }

        public Dictionary<ulong, List<GameplayEffectPredictionRecord>> Records { get; }
    }

    internal sealed class GameplayEffectPredictionRecord
    {
        readonly Dictionary<GameplayAttributeId, GameplayAttributePredictionSnapshot> m_Attributes = new Dictionary<GameplayAttributeId, GameplayAttributePredictionSnapshot>();

        public GameplayEffectPredictionRecord(
            GameplayEffectSpec spec,
            GameplayEffectHandle handle,
            GameplayEffectInstanceId instanceId,
            bool createdActive,
            bool hasActiveBefore,
            GameplayActiveEffectSnapshot activeBefore)
        {
            Spec = spec;
            EffectId = spec.EffectId;
            Context = spec.Context;
            PredictionKey = spec.Context.PredictionKey;
            ActionInstanceId = spec.Context.SourceActionInstanceId;
            Handle = handle;
            InstanceId = instanceId;
            CreatedActive = createdActive;
            HasActiveBefore = hasActiveBefore;
            ActiveBefore = activeBefore;
        }

        public GameplayEffectId EffectId { get; }
        public GameplayEffectSpec Spec { get; }
        public GameplayEffectContext Context { get; }
        public ulong PredictionKey { get; }
        public ulong ActionInstanceId { get; }
        public GameplayEffectHandle Handle { get; }
        public GameplayEffectInstanceId InstanceId { get; }
        public bool CreatedActive { get; }
        public bool HasActiveBefore { get; }
        public GameplayActiveEffectSnapshot ActiveBefore { get; }
        public bool Confirmed { get; set; }
        public List<string> CueIds { get; } = new List<string>();
        public IEnumerable<KeyValuePair<GameplayAttributeId, GameplayAttributePredictionSnapshot>> Attributes => m_Attributes;

        public void CaptureBefore(GameplayAttributeStateSnapshot snapshot)
        {
            GameplayAttributeId id = snapshot.Value.AttributeId;
            if (!m_Attributes.ContainsKey(id))
                m_Attributes.Add(id, new GameplayAttributePredictionSnapshot(snapshot, 0));
        }

        public void CaptureAfter(GameplayAttributeId attributeId, ulong revision)
        {
            if (m_Attributes.TryGetValue(attributeId, out GameplayAttributePredictionSnapshot value))
                m_Attributes[attributeId] = new GameplayAttributePredictionSnapshot(value.Before, revision);
        }
    }

    internal readonly struct GameplayAttributePredictionSnapshot
    {
        public GameplayAttributePredictionSnapshot(GameplayAttributeStateSnapshot before, ulong afterRevision)
        {
            Before = before;
            AfterRevision = afterRevision;
        }
        public GameplayAttributeStateSnapshot Before { get; }
        public ulong AfterRevision { get; }
    }
}
