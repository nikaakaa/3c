using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Attributes;

namespace ThirdPersonGameplay.Effects
{
    internal sealed class GameplayEffectPredictionJournalService
    {
        readonly GameplayEffectRuntimeState m_State;
        GameplayEffectPredictionRecord m_CurrentRecord;

        public GameplayEffectPredictionJournalService(GameplayEffectRuntimeState state)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void Begin(
            GameplayEffectSpec spec,
            GameplayEffectHandle handle,
            GameplayEffectInstanceId instanceId,
            bool createdActive,
            bool hasActiveBefore,
            GameplayActiveEffectSnapshot activeBefore)
        {
            if (!spec.Context.IsPredicted)
                return;
            if (m_CurrentRecord != null)
                throw new InvalidOperationException("Nested prediction journal records are not supported.");
            m_CurrentRecord = new GameplayEffectPredictionRecord(
                spec,
                handle,
                instanceId,
                createdActive,
                hasActiveBefore,
                activeBefore);
        }

        public void CaptureBefore(GameplayAttributeId attributeId)
        {
            if (m_CurrentRecord != null && m_State.Attributes.Capture(attributeId, out GameplayAttributeStateSnapshot before))
                m_CurrentRecord.CaptureBefore(before);
        }

        public void TrackCue(string cueId)
        {
            if (m_CurrentRecord != null && !string.IsNullOrEmpty(cueId))
                m_CurrentRecord.CueIds.Add(cueId);
        }

        public void Complete()
        {
            GameplayEffectPredictionRecord record = m_CurrentRecord;
            m_CurrentRecord = null;
            if (record == null)
                return;
            var ids = new List<GameplayAttributeId>();
            foreach (KeyValuePair<GameplayAttributeId, GameplayAttributePredictionSnapshot> pair in record.Attributes)
                ids.Add(pair.Key);
            for (int i = 0; i < ids.Count; i++)
            {
                if (m_State.Attributes.TryGetValue(ids[i], out GameplayAttributeValue value))
                    record.CaptureAfter(ids[i], value.Revision);
            }
            m_State.PredictionJournal.Add(record);
        }

        public void CancelCurrent()
        {
            m_CurrentRecord = null;
        }

        public void ClearAction(ulong actionInstanceId)
        {
            m_State.PredictionJournal.ClearAction(actionInstanceId);
        }
    }
}
