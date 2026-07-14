using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Tags;

namespace ThirdPersonGameplay.Effects
{
    internal sealed class GameplayEffectRuntimeState : IDisposable
    {
        ulong m_NextHandle = 1;
        ulong m_NextInstanceId = 1;
        ulong m_NextInsertionSequence = 1;

        public GameplayEffectRuntimeState(GameplayEffectRuntimeDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Tags = new GameplayTagContainer(definition.TagCatalog);
            Attributes = new GameplayAttributeStore(definition.Attributes, definition.InitialAttributes);
            ActiveEffects = new ActiveGameplayEffectContainer();
            PredictionJournal = new GameplayEffectPredictionJournal();
            LastLifecycleRevisions = new Dictionary<GameplayEffectInstanceId, ulong>();
            if (!Tags.SetSourceTags(GameplayTagSourceHandle.CharacterInitial, definition.InitialTags))
                throw new InvalidOperationException("Initial Gameplay Tags are invalid.");
            Tags.DrainChanges(new List<GameplayTagCountChange>());
            Attributes.DrainChanges(new List<GameplayAttributeChange>());
        }

        public GameplayEffectRuntimeDefinition Definition { get; }
        public GameplayTagContainer Tags { get; }
        public GameplayAttributeStore Attributes { get; }
        public ActiveGameplayEffectContainer ActiveEffects { get; }
        public GameplayEffectPredictionJournal PredictionJournal { get; }
        public Dictionary<GameplayEffectInstanceId, ulong> LastLifecycleRevisions { get; }
        public ulong CurrentTick { get; private set; }
        public bool Disposed { get; private set; }

        public void AdvanceTick(GameplayEffectTickContext context)
        {
            if (!context.IsValid)
                throw new ArgumentException("Gameplay Effect tick context is invalid.", nameof(context));
            if (CurrentTick != 0 && context.LocalLogicTick <= CurrentTick)
                throw new InvalidOperationException($"Gameplay Effect tick must advance: current={CurrentTick}, incoming={context.LocalLogicTick}.");
            CurrentTick = context.LocalLogicTick;
        }

        public GameplayEffectHandle NextHandle()
        {
            if (m_NextHandle == 0)
                m_NextHandle++;
            return new GameplayEffectHandle(m_NextHandle++);
        }

        public GameplayEffectInstanceId ResolveInstanceId(GameplayEffectInstanceId authoritativeInstanceId)
        {
            if (authoritativeInstanceId.IsValid)
            {
                if (authoritativeInstanceId.Value >= m_NextInstanceId)
                    m_NextInstanceId = authoritativeInstanceId.Value == ulong.MaxValue ? 1 : authoritativeInstanceId.Value + 1;
                return authoritativeInstanceId;
            }
            if (m_NextInstanceId == 0)
                m_NextInstanceId++;
            return new GameplayEffectInstanceId(m_NextInstanceId++);
        }

        public ulong NextInsertionSequence()
        {
            if (m_NextInsertionSequence == 0)
                m_NextInsertionSequence++;
            return m_NextInsertionSequence++;
        }

        public GameplayEffectRuntimeTransactionSnapshot CaptureTransactionSnapshot()
        {
            return new GameplayEffectRuntimeTransactionSnapshot(
                Tags.CaptureTransactionSnapshot(),
                Attributes.CaptureTransactionSnapshot(),
                ActiveEffects.CaptureTransactionSnapshot(),
                PredictionJournal.CaptureTransactionSnapshot(),
                new Dictionary<GameplayEffectInstanceId, ulong>(LastLifecycleRevisions),
                m_NextHandle,
                m_NextInstanceId,
                m_NextInsertionSequence);
        }

        public void RestoreTransactionSnapshot(GameplayEffectRuntimeTransactionSnapshot snapshot)
        {
            Tags.RestoreTransactionSnapshot(snapshot.Tags);
            Attributes.RestoreTransactionSnapshot(snapshot.Attributes);
            ActiveEffects.RestoreTransactionSnapshot(snapshot.ActiveEffects);
            PredictionJournal.RestoreTransactionSnapshot(snapshot.PredictionJournal);
            LastLifecycleRevisions.Clear();
            foreach (KeyValuePair<GameplayEffectInstanceId, ulong> pair in snapshot.LastLifecycleRevisions)
                LastLifecycleRevisions.Add(pair.Key, pair.Value);
            m_NextHandle = snapshot.NextHandle;
            m_NextInstanceId = snapshot.NextInstanceId;
            m_NextInsertionSequence = snapshot.NextInsertionSequence;
        }

        public void Dispose()
        {
            if (Disposed)
                return;
            ActiveEffects.Clear();
            PredictionJournal.Clear();
            LastLifecycleRevisions.Clear();
            Tags.Clear(false);
            Attributes.Dispose();
            Disposed = true;
        }

        public static ulong CheckedAdd(ulong left, ulong right)
        {
            if (ulong.MaxValue - left < right)
                throw new OverflowException("Gameplay Effect tick range overflowed.");
            return left + right;
        }
    }

    internal sealed class GameplayEffectRuntimeTransactionSnapshot
    {
        public GameplayEffectRuntimeTransactionSnapshot(
            GameplayTagContainerSnapshot tags,
            GameplayAttributeStore.TransactionSnapshot attributes,
            ActiveGameplayEffectContainerSnapshot activeEffects,
            GameplayEffectPredictionJournalSnapshot predictionJournal,
            Dictionary<GameplayEffectInstanceId, ulong> lastLifecycleRevisions,
            ulong nextHandle,
            ulong nextInstanceId,
            ulong nextInsertionSequence)
        {
            Tags = tags;
            Attributes = attributes;
            ActiveEffects = activeEffects;
            PredictionJournal = predictionJournal;
            LastLifecycleRevisions = lastLifecycleRevisions;
            NextHandle = nextHandle;
            NextInstanceId = nextInstanceId;
            NextInsertionSequence = nextInsertionSequence;
        }

        public GameplayTagContainerSnapshot Tags { get; }
        public GameplayAttributeStore.TransactionSnapshot Attributes { get; }
        public ActiveGameplayEffectContainerSnapshot ActiveEffects { get; }
        public GameplayEffectPredictionJournalSnapshot PredictionJournal { get; }
        public Dictionary<GameplayEffectInstanceId, ulong> LastLifecycleRevisions { get; }
        public ulong NextHandle { get; }
        public ulong NextInstanceId { get; }
        public ulong NextInsertionSequence { get; }
    }
}
