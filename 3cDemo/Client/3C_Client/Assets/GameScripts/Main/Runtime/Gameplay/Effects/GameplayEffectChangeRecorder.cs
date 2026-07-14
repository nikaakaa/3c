using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Tags;

namespace ThirdPersonGameplay.Effects
{
    internal sealed class GameplayEffectChangeRecorder
    {
        readonly GameplayEffectRuntimeState m_State;
        readonly List<GameplayAttributeChange> m_AttributeChanges = new List<GameplayAttributeChange>();
        readonly Dictionary<GameplayEffectHandle, CauseIdentity> m_Causes = new Dictionary<GameplayEffectHandle, CauseIdentity>();
        GameplayEffectChangeSet m_ChangeSet = new GameplayEffectChangeSet();

        public GameplayEffectChangeRecorder(GameplayEffectRuntimeState state)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void BeginTick()
        {
            m_ChangeSet.LocalLogicTick = m_State.CurrentTick;
        }

        public void AddLifecycle(ActiveGameplayEffect active, GameplayEffectLifecycleOperation operation)
        {
            AddLifecycle(
                active.Spec,
                active.InstanceId,
                operation,
                active.StartTick,
                active.EndTick,
                active.StackCount,
                active.LifecycleRevision,
                false);
        }

        public void AddLifecycle(
            GameplayEffectSpec spec,
            GameplayEffectInstanceId instanceId,
            GameplayEffectLifecycleOperation operation,
            ulong startTick,
            ulong endTick,
            int stackCount,
            ulong revision,
            bool instant)
        {
            m_ChangeSet.EffectChanges.Add(new GameplayEffectLifecycleChange(
                spec.EffectId,
                instanceId,
                operation,
                spec.Context,
                startTick,
                endTick,
                stackCount,
                revision,
                spec.DefinitionRevision,
                instant,
                spec.CopySetByCallerValues()));
        }

        public void AddCue(GameplayEffectComponentContext context, string cueId, GameplayCueTrigger trigger)
        {
            if (string.IsNullOrEmpty(cueId))
                return;
            m_ChangeSet.CueChanges.Add(new GameplayCueChange(
                cueId,
                trigger,
                context.Spec.EffectId,
                context.InstanceId,
                context.Spec.Context));
        }

        public void AddExecutionFailure(GameplayEffectExecutionFailure failure)
        {
            m_ChangeSet.ExecutionFailures.Add(failure);
        }

        public void RegisterCause(
            GameplayEffectHandle handle,
            GameplayEffectSpec spec,
            GameplayEffectInstanceId instanceId)
        {
            if (handle.IsValid && spec != null)
                RegisterCause(handle, spec.EffectId, instanceId, spec.Context);
        }

        public void RegisterCause(
            GameplayEffectHandle handle,
            GameplayEffectId effectId,
            GameplayEffectInstanceId instanceId,
            GameplayEffectContext context)
        {
            if (handle.IsValid && effectId.IsValid)
                m_Causes[handle] = new CauseIdentity(effectId, instanceId, context);
        }

        public void RegisterCause(ActiveGameplayEffect active)
        {
            if (active != null)
                RegisterCause(active.Handle, active.Spec, active.InstanceId);
        }

        public void DrainStateChanges()
        {
            m_AttributeChanges.Clear();
            m_State.Attributes.DrainChanges(m_AttributeChanges);
            for (int i = 0; i < m_AttributeChanges.Count; i++)
            {
                GameplayAttributeChange change = m_AttributeChanges[i];
                m_Causes.TryGetValue(change.CauseEffect, out CauseIdentity cause);
                m_ChangeSet.AttributeChanges.Add(new GameplayEffectAttributeChange(
                    change,
                    cause.EffectId,
                    cause.InstanceId,
                    cause.Context));
            }
            m_State.Tags.DrainChanges(m_ChangeSet.TagChanges);
        }

        public GameplayEffectChangeSet Drain()
        {
            DrainStateChanges();
            GameplayEffectChangeSet result = m_ChangeSet;
            m_ChangeSet = new GameplayEffectChangeSet { LocalLogicTick = m_State.CurrentTick };
            m_Causes.Clear();
            return result;
        }

        public TransactionSnapshot CaptureTransactionSnapshot()
        {
            return new TransactionSnapshot(
                Clone(m_ChangeSet),
                new List<GameplayAttributeChange>(m_AttributeChanges),
                new Dictionary<GameplayEffectHandle, CauseIdentity>(m_Causes));
        }

        public void RestoreTransactionSnapshot(TransactionSnapshot snapshot)
        {
            m_ChangeSet = Clone(snapshot.ChangeSet);
            m_AttributeChanges.Clear();
            m_AttributeChanges.AddRange(snapshot.AttributeChanges);
            m_Causes.Clear();
            foreach (KeyValuePair<GameplayEffectHandle, CauseIdentity> pair in snapshot.Causes)
                m_Causes.Add(pair.Key, pair.Value);
        }

        public void Reset()
        {
            m_ChangeSet = new GameplayEffectChangeSet();
            m_AttributeChanges.Clear();
            m_Causes.Clear();
        }

        internal readonly struct CauseIdentity
        {
            public CauseIdentity(
                GameplayEffectId effectId,
                GameplayEffectInstanceId instanceId,
                GameplayEffectContext context)
            {
                EffectId = effectId;
                InstanceId = instanceId;
                Context = context;
            }

            public GameplayEffectId EffectId { get; }
            public GameplayEffectInstanceId InstanceId { get; }
            public GameplayEffectContext Context { get; }
        }

        static GameplayEffectChangeSet Clone(GameplayEffectChangeSet source)
        {
            var result = new GameplayEffectChangeSet { LocalLogicTick = source.LocalLogicTick };
            result.EffectChanges.AddRange(source.EffectChanges);
            result.AttributeChanges.AddRange(source.AttributeChanges);
            result.TagChanges.AddRange(source.TagChanges);
            result.CueChanges.AddRange(source.CueChanges);
            result.ExecutionFailures.AddRange(source.ExecutionFailures);
            return result;
        }

        internal sealed class TransactionSnapshot
        {
            internal TransactionSnapshot(
                GameplayEffectChangeSet changeSet,
                List<GameplayAttributeChange> attributeChanges,
                Dictionary<GameplayEffectHandle, CauseIdentity> causes)
            {
                ChangeSet = changeSet;
                AttributeChanges = attributeChanges;
                Causes = causes;
            }

            internal GameplayEffectChangeSet ChangeSet { get; }
            internal List<GameplayAttributeChange> AttributeChanges { get; }
            internal Dictionary<GameplayEffectHandle, CauseIdentity> Causes { get; }
        }
    }
}
