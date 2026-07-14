using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Contracts;
using ThirdPersonGameplay.Tags;

namespace ThirdPersonGameplay.Effects
{
    public sealed class GameplayEffectRuntime :
        IGameplayTagReader,
        IGameplayTagSourceSink,
        IGameplayAttributeReader,
        IGameplayEffectCommandSink,
        IGameplayEffectAuthorityInputSink,
        IDisposable
    {
        readonly GameplayEffectRuntimeState m_State;
        readonly GameplayEffectChangeRecorder m_Changes;
        readonly GameplayEffectPredictionJournalService m_PredictionJournal;
        readonly GameplayEffectMutationTransaction m_Transaction;
        readonly GameplayEffectApplicationService m_Application;
        readonly GameplayEffectLifecycleScheduler m_Scheduler;
        readonly GameplayEffectPredictionReconciler m_Reconciler;
        bool m_TickOpen;

        public GameplayEffectRuntime(GameplayEffectRuntimeDefinition definition)
        {
            m_State = new GameplayEffectRuntimeState(definition);
            m_Changes = new GameplayEffectChangeRecorder(m_State);
            var specFactory = new GameplayEffectSpecFactory(m_State);
            m_PredictionJournal = new GameplayEffectPredictionJournalService(m_State);
            m_Transaction = new GameplayEffectMutationTransaction(m_State, m_Changes);
            var components = new GameplayEffectComponentExecutor(
                m_State,
                specFactory,
                m_Changes,
                m_PredictionJournal,
                m_Transaction);
            m_Application = new GameplayEffectApplicationService(
                m_State,
                specFactory,
                components,
                m_Changes,
                m_PredictionJournal,
                m_Transaction);
            m_Scheduler = new GameplayEffectLifecycleScheduler(
                m_State,
                m_Application,
                components,
                m_Changes);
            m_Reconciler = new GameplayEffectPredictionReconciler(
                m_State,
                m_Application,
                components,
                m_Changes);
        }

        public IGameplayTagReader TagReader => this;
        public IGameplayAttributeReader AttributeReader => this;

        public void BeginLogicTick(
            GameplayEffectTickContext context,
            IReadOnlyList<GameplayEffectAuthorityInput> authorityInputs)
        {
            ThrowIfDisposed();
            if (m_TickOpen)
                throw new InvalidOperationException("Previous Gameplay Effect logic tick has not been drained.");
            m_State.AdvanceTick(context);
            m_Changes.BeginTick();
            m_TickOpen = true;
            if (authorityInputs != null)
            {
                for (int i = 0; i < authorityInputs.Count; i++)
                    m_Reconciler.Reconcile(authorityInputs[i]);
            }
            if (!m_Scheduler.Advance(out GameplayEffectExecutionFailure failure))
                m_Changes.AddExecutionFailure(failure);
            m_Changes.DrainStateChanges();
        }

        public GameplayEffectChangeSet DrainChangeSet()
        {
            ThrowIfDisposed();
            if (!m_TickOpen)
                throw new InvalidOperationException("Gameplay Effect logic tick is not open.");
            GameplayEffectChangeSet result = m_Changes.Drain();
            m_TickOpen = false;
            return result;
        }

        public void ClearConfirmedPredictionJournal(ulong actionInstanceId)
        {
            ThrowIfDisposed();
            m_PredictionJournal.ClearAction(actionInstanceId);
        }

        public bool HasTag(GameplayTagId tagId)
        {
            return !m_State.Disposed && m_State.Tags.HasTag(tagId);
        }

        public bool Matches(GameplayTagQuery query)
        {
            return !m_State.Disposed && m_State.Tags.Matches(query);
        }

        public bool Matches(GameplayTagQuery query, IReadOnlyList<GameplayTagId> explicitTags)
        {
            return !m_State.Disposed && m_State.Tags.Matches(query, explicitTags);
        }

        public bool SetSourceTags(GameplayTagSourceHandle source, IReadOnlyList<GameplayTagId> tags)
        {
            return !m_State.Disposed && m_State.Tags.SetSourceTags(source, tags);
        }

        public bool RemoveSource(GameplayTagSourceHandle source)
        {
            return !m_State.Disposed && m_State.Tags.RemoveSource(source);
        }

        public bool TryGetValue(GameplayAttributeId attributeId, out GameplayAttributeValue value)
        {
            value = default;
            return !m_State.Disposed && m_State.Attributes.TryGetValue(attributeId, out value);
        }

        public GameplayEffectCanApplyResult CanApply(GameplayEffectApplyRequest request)
        {
            return m_State.Disposed
                ? new GameplayEffectCanApplyResult(false, GameplayEffectApplyResultCode.Disposed, "RuntimeDisposed")
                : m_Application.CanApply(request);
        }

        public GameplayEffectApplyResult Apply(GameplayEffectApplyRequest request)
        {
            if (m_State.Disposed)
                return new GameplayEffectApplyResult(GameplayEffectApplyResultCode.Disposed, default, default, "RuntimeDisposed");
            RequireOpenTick();
            return m_Application.Apply(request);
        }

        public GameplayEffectRemoveResult Remove(GameplayEffectRemoveRequest request)
        {
            if (m_State.Disposed)
                return new GameplayEffectRemoveResult(Array.Empty<GameplayEffectHandle>());
            RequireOpenTick();
            return m_Application.Remove(request);
        }

        public GameplayEffectReconcileResult Reconcile(GameplayEffectAuthorityInput input)
        {
            return m_State.Disposed
                ? GameplayEffectReconcileResult.Disposed
                : m_Reconciler.Reconcile(input);
        }

        public void Dispose()
        {
            if (m_State.Disposed)
                return;
            List<ActiveGameplayEffect> values = m_State.ActiveEffects.Snapshot();
            for (int i = 0; i < values.Count; i++)
                m_Application.RemoveActive(values[i], GameplayEffectLifecycleOperation.Removed, false);
            m_Transaction.Clear();
            m_Changes.Reset();
            m_TickOpen = false;
            m_State.Dispose();
        }

        void ThrowIfDisposed()
        {
            if (m_State.Disposed)
                throw new ObjectDisposedException(nameof(GameplayEffectRuntime));
        }

        void RequireOpenTick()
        {
            if (!m_TickOpen)
                throw new InvalidOperationException("Gameplay Effect mutations require an open logic tick.");
        }
    }
}
