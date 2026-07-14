using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Tags;

namespace ThirdPersonGameplay.Effects
{
    internal sealed class ActiveGameplayEffectContainer
    {
        readonly List<ActiveGameplayEffect> m_Ordered = new List<ActiveGameplayEffect>();
        readonly Dictionary<GameplayEffectHandle, ActiveGameplayEffect> m_ByHandle = new Dictionary<GameplayEffectHandle, ActiveGameplayEffect>();
        readonly Dictionary<GameplayEffectInstanceId, ActiveGameplayEffect> m_ByInstance = new Dictionary<GameplayEffectInstanceId, ActiveGameplayEffect>();
        readonly Dictionary<GameplayEffectStackKey, ActiveGameplayEffect> m_ByStack = new Dictionary<GameplayEffectStackKey, ActiveGameplayEffect>();
        readonly List<PendingMutation> m_Pending = new List<PendingMutation>();
        int m_IterationDepth;

        public void Add(ActiveGameplayEffect effect)
        {
            if (effect == null)
                throw new ArgumentNullException(nameof(effect));
            if (m_IterationDepth > 0)
            {
                m_Pending.Add(new PendingMutation(PendingMutationKind.Add, effect));
                return;
            }
            AddImmediate(effect);
        }

        public bool Remove(ActiveGameplayEffect effect)
        {
            if (effect == null || effect.PendingRemoval)
                return false;
            if (m_IterationDepth > 0)
            {
                effect.PendingRemoval = true;
                m_Pending.Add(new PendingMutation(PendingMutationKind.Remove, effect));
                return true;
            }
            return RemoveImmediate(effect);
        }

        public bool TryGet(GameplayEffectHandle handle, out ActiveGameplayEffect effect)
        {
            return m_ByHandle.TryGetValue(handle, out effect) && !effect.PendingRemoval;
        }

        public bool TryGet(GameplayEffectInstanceId instanceId, out ActiveGameplayEffect effect)
        {
            return m_ByInstance.TryGetValue(instanceId, out effect) && !effect.PendingRemoval;
        }

        public bool TryGetStack(GameplayEffectStackKey stackKey, out ActiveGameplayEffect effect)
        {
            return m_ByStack.TryGetValue(stackKey, out effect) && !effect.PendingRemoval;
        }

        public bool UpdateInstanceId(ActiveGameplayEffect effect, GameplayEffectInstanceId instanceId)
        {
            if (effect == null || !instanceId.IsValid || effect.PendingRemoval || m_ByInstance.ContainsKey(instanceId))
                return false;
            m_ByInstance.Remove(effect.InstanceId);
            effect.InstanceId = instanceId;
            m_ByInstance.Add(instanceId, effect);
            return true;
        }

        public List<ActiveGameplayEffect> BeginStableIteration()
        {
            m_IterationDepth++;
            var values = new List<ActiveGameplayEffect>(m_Ordered.Count);
            for (int i = 0; i < m_Ordered.Count; i++)
            {
                if (!m_Ordered[i].PendingRemoval)
                    values.Add(m_Ordered[i]);
            }
            values.Sort((left, right) => left.InsertionSequence.CompareTo(right.InsertionSequence));
            return values;
        }

        public void EndStableIteration()
        {
            if (m_IterationDepth <= 0)
                throw new InvalidOperationException("Active Gameplay Effect iteration is not active.");
            m_IterationDepth--;
            if (m_IterationDepth != 0)
                return;
            for (int i = 0; i < m_Pending.Count; i++)
            {
                PendingMutation mutation = m_Pending[i];
                if (mutation.Kind == PendingMutationKind.Add)
                    AddImmediate(mutation.Effect);
                else
                    RemoveImmediate(mutation.Effect);
            }
            m_Pending.Clear();
        }

        public List<ActiveGameplayEffect> Snapshot()
        {
            var values = new List<ActiveGameplayEffect>();
            for (int i = 0; i < m_Ordered.Count; i++)
            {
                if (!m_Ordered[i].PendingRemoval)
                    values.Add(m_Ordered[i]);
            }
            return values;
        }

        public ActiveGameplayEffectContainerSnapshot CaptureTransactionSnapshot()
        {
            if (m_IterationDepth != 0)
                throw new InvalidOperationException("Cannot capture Active Gameplay Effects during stable iteration.");
            var values = new ActiveGameplayEffectTransactionState[m_Ordered.Count];
            for (int i = 0; i < m_Ordered.Count; i++)
            {
                ActiveGameplayEffect active = m_Ordered[i];
                values[i] = new ActiveGameplayEffectTransactionState(
                    active,
                    active.Snapshot(),
                    active.ModifierHandles.ToArray(),
                    active.GrantedTags.ToArray());
            }
            return new ActiveGameplayEffectContainerSnapshot(values);
        }

        public void RestoreTransactionSnapshot(ActiveGameplayEffectContainerSnapshot snapshot)
        {
            Clear();
            for (int i = 0; i < snapshot.Effects.Length; i++)
            {
                ActiveGameplayEffectTransactionState value = snapshot.Effects[i];
                ActiveGameplayEffect active = value.Active;
                active.InstanceId = value.Snapshot.InstanceId;
                active.StartTick = value.Snapshot.StartTick;
                active.EndTick = value.Snapshot.EndTick;
                active.NextPeriodTick = value.Snapshot.NextPeriodTick;
                active.StackCount = value.Snapshot.StackCount;
                active.Inhibited = value.Snapshot.Inhibited;
                active.LifecycleRevision = value.Snapshot.LifecycleRevision;
                active.ModifierHandles.Clear();
                active.ModifierHandles.AddRange(value.ModifierHandles);
                active.GrantedTags.Clear();
                active.GrantedTags.AddRange(value.GrantedTags);
                AddImmediate(active);
            }
        }

        public void Clear()
        {
            m_Ordered.Clear();
            m_ByHandle.Clear();
            m_ByInstance.Clear();
            m_ByStack.Clear();
            m_Pending.Clear();
            m_IterationDepth = 0;
        }

        void AddImmediate(ActiveGameplayEffect effect)
        {
            if (m_ByHandle.ContainsKey(effect.Handle) || m_ByInstance.ContainsKey(effect.InstanceId))
                throw new InvalidOperationException($"Duplicate Active Gameplay Effect '{effect.Handle}/{effect.InstanceId}'.");
            effect.PendingRemoval = false;
            m_Ordered.Add(effect);
            m_ByHandle.Add(effect.Handle, effect);
            m_ByInstance.Add(effect.InstanceId, effect);
            if (effect.Spec.Definition.StackingPolicy != GameplayEffectStackingPolicy.Independent)
                m_ByStack.Add(effect.Spec.StackKey, effect);
        }

        bool RemoveImmediate(ActiveGameplayEffect effect)
        {
            if (!m_ByHandle.Remove(effect.Handle))
                return false;
            m_Ordered.Remove(effect);
            m_ByInstance.Remove(effect.InstanceId);
            if (effect.Spec.Definition.StackingPolicy != GameplayEffectStackingPolicy.Independent &&
                m_ByStack.TryGetValue(effect.Spec.StackKey, out ActiveGameplayEffect current) && current == effect)
                m_ByStack.Remove(effect.Spec.StackKey);
            effect.PendingRemoval = true;
            return true;
        }

        enum PendingMutationKind : byte
        {
            Add,
            Remove
        }

        readonly struct PendingMutation
        {
            public PendingMutation(PendingMutationKind kind, ActiveGameplayEffect effect)
            {
                Kind = kind;
                Effect = effect;
            }
            public PendingMutationKind Kind { get; }
            public ActiveGameplayEffect Effect { get; }
        }
    }

    internal sealed class ActiveGameplayEffectContainerSnapshot
    {
        public ActiveGameplayEffectContainerSnapshot(ActiveGameplayEffectTransactionState[] effects)
        {
            Effects = effects;
        }

        public ActiveGameplayEffectTransactionState[] Effects { get; }
    }

    internal readonly struct ActiveGameplayEffectTransactionState
    {
        public ActiveGameplayEffectTransactionState(
            ActiveGameplayEffect active,
            GameplayActiveEffectSnapshot snapshot,
            GameplayModifierHandle[] modifierHandles,
            GameplayTagId[] grantedTags)
        {
            Active = active;
            Snapshot = snapshot;
            ModifierHandles = modifierHandles;
            GrantedTags = grantedTags;
        }

        public ActiveGameplayEffect Active { get; }
        public GameplayActiveEffectSnapshot Snapshot { get; }
        public GameplayModifierHandle[] ModifierHandles { get; }
        public GameplayTagId[] GrantedTags { get; }
    }
}
