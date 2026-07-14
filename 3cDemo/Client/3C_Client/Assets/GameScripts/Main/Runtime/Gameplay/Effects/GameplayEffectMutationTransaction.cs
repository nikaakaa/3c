using System;
using System.Collections.Generic;

namespace ThirdPersonGameplay.Effects
{
    internal sealed class GameplayEffectMutationTransaction
    {
        readonly GameplayEffectRuntimeState m_State;
        readonly GameplayEffectChangeRecorder m_Changes;
        readonly Queue<GameplayEffectPendingApplication> m_AdditionalEffects = new Queue<GameplayEffectPendingApplication>();
        GameplayEffectRuntimeTransactionSnapshot m_StateSnapshot;
        GameplayEffectChangeRecorder.TransactionSnapshot m_ChangeSnapshot;
        int m_Depth;
        bool m_Failed;

        public GameplayEffectMutationTransaction(
            GameplayEffectRuntimeState state,
            GameplayEffectChangeRecorder changes)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            m_Changes = changes ?? throw new ArgumentNullException(nameof(changes));
        }

        public bool Begin()
        {
            if (m_Depth == 0)
            {
                m_StateSnapshot = m_State.CaptureTransactionSnapshot();
                m_ChangeSnapshot = m_Changes.CaptureTransactionSnapshot();
                m_Failed = false;
            }
            m_Depth++;
            return m_Depth == 1;
        }

        public void End(bool commit)
        {
            if (m_Depth <= 0)
                throw new InvalidOperationException("Gameplay Effect mutation transaction is not active.");
            if (!commit)
                m_Failed = true;
            m_Depth--;
            if (m_Depth != 0)
                return;
            if (m_Failed)
            {
                m_State.RestoreTransactionSnapshot(m_StateSnapshot);
                m_Changes.RestoreTransactionSnapshot(m_ChangeSnapshot);
            }
            m_StateSnapshot = null;
            m_ChangeSnapshot = null;
            m_AdditionalEffects.Clear();
            m_Failed = false;
        }

        public void EnqueueAdditionalEffect(GameplayEffectPendingApplication application)
        {
            if (application.Request != null)
                m_AdditionalEffects.Enqueue(application);
        }

        public bool TryDequeueAdditionalEffect(out GameplayEffectPendingApplication application)
        {
            if (m_AdditionalEffects.Count > 0)
            {
                application = m_AdditionalEffects.Dequeue();
                return true;
            }
            application = default;
            return false;
        }

        public void Clear()
        {
            m_AdditionalEffects.Clear();
            m_StateSnapshot = null;
            m_ChangeSnapshot = null;
            m_Depth = 0;
            m_Failed = false;
        }
    }

    internal readonly struct GameplayEffectPendingApplication
    {
        public GameplayEffectPendingApplication(
            GameplayEffectApplyRequest request,
            GameplayEffectId ownerEffectId,
            GameplayEffectInstanceId ownerInstanceId,
            GameplayEffectLifecycleOperation trigger)
        {
            Request = request;
            OwnerEffectId = ownerEffectId;
            OwnerInstanceId = ownerInstanceId;
            Trigger = trigger;
        }

        public GameplayEffectApplyRequest Request { get; }
        public GameplayEffectId OwnerEffectId { get; }
        public GameplayEffectInstanceId OwnerInstanceId { get; }
        public GameplayEffectLifecycleOperation Trigger { get; }
    }
}
