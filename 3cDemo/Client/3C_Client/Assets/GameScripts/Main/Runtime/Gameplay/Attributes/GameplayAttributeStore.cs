using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Contracts;
using ThirdPersonGameplay.Effects;
using UnityEngine;

namespace ThirdPersonGameplay.Attributes
{
    public sealed class GameplayAttributeStore : IGameplayAttributeReader, IDisposable
    {
        readonly Dictionary<GameplayAttributeId, Entry> m_Entries = new Dictionary<GameplayAttributeId, Entry>();
        readonly Dictionary<ulong, GameplayAttributeId> m_ModifierAttributes = new Dictionary<ulong, GameplayAttributeId>();
        readonly Dictionary<GameplayAttributeId, HashSet<GameplayAttributeId>> m_Dependents = new Dictionary<GameplayAttributeId, HashSet<GameplayAttributeId>>();
        readonly Dictionary<AttributeDependencyKey, int> m_DependencyRefCounts = new Dictionary<AttributeDependencyKey, int>();
        readonly List<GameplayAttributeChange> m_Changes = new List<GameplayAttributeChange>();
        readonly HashSet<GameplayAttributeId> m_DirtyTraversal = new HashSet<GameplayAttributeId>();
        readonly HashSet<GameplayAttributeId> m_RecalculateTraversal = new HashSet<GameplayAttributeId>();
        ulong m_NextModifierHandle = 1;
        ulong m_NextInsertionSequence = 1;
        bool m_Disposed;

        public GameplayAttributeStore(
            IReadOnlyList<GameplayAttributeDefinitionData> definitions,
            IReadOnlyList<GameplayAttributeInitialValueData> initialValues)
        {
            if (definitions == null)
                throw new ArgumentNullException(nameof(definitions));
            if (initialValues == null)
                throw new ArgumentNullException(nameof(initialValues));

            var initialById = new Dictionary<GameplayAttributeId, float>();
            for (int i = 0; i < initialValues.Count; i++)
            {
                if (!GameplayNumber.IsFinite(initialValues[i].BaseValue))
                    throw new InvalidOperationException($"Attribute '{initialValues[i].AttributeId}' initial value must be finite.");
                initialById.Add(initialValues[i].AttributeId, initialValues[i].BaseValue);
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                GameplayAttributeDefinitionData definition = definitions[i];
                if (!initialById.TryGetValue(definition.AttributeId, out float initialValue))
                    throw new InvalidOperationException($"Attribute '{definition.AttributeId}' is not initialized.");
                m_Entries.Add(definition.AttributeId, new Entry(definition, initialValue));
            }

            foreach (Entry entry in m_Entries.Values)
            {
                RegisterBoundDependency(entry.Definition.AttributeId, entry.Definition.Minimum);
                RegisterBoundDependency(entry.Definition.AttributeId, entry.Definition.Maximum);
                MarkDirtyTree(entry.Definition.AttributeId, default);
            }
            RecalculateDirty();
            m_Changes.Clear();
            foreach (Entry entry in m_Entries.Values)
                entry.Revision = 1;
        }

        public bool TryGetValue(GameplayAttributeId attributeId, out GameplayAttributeValue value)
        {
            value = default;
            if (m_Disposed || !m_Entries.TryGetValue(attributeId, out Entry entry))
                return false;
            if (entry.Dirty)
                RecalculateDirty();
            value = entry.Snapshot();
            return true;
        }

        public bool Capture(GameplayAttributeId attributeId, out GameplayAttributeStateSnapshot snapshot)
        {
            snapshot = default;
            if (!TryGetValue(attributeId, out GameplayAttributeValue value))
                return false;
            snapshot = new GameplayAttributeStateSnapshot(value);
            return true;
        }

        public bool MutateBase(GameplayAttributeMutation mutation, GameplayEffectHandle causeEffect)
        {
            if (m_Disposed || !m_Entries.TryGetValue(mutation.AttributeId, out Entry entry))
                return false;

            if (!GameplayNumber.IsFinite(mutation.Magnitude))
                return false;
            float baseValue;
            switch (mutation.Operation)
            {
                case GameplayModifierOperation.Additive:
                    baseValue = entry.BaseValue + mutation.Magnitude;
                    break;
                case GameplayModifierOperation.Multiplicative:
                    baseValue = entry.BaseValue * mutation.Magnitude;
                    break;
                case GameplayModifierOperation.Override:
                    baseValue = mutation.Magnitude;
                    break;
                case GameplayModifierOperation.Clamp:
                    baseValue = mutation.ClampBound == GameplayClampBound.Minimum
                        ? Mathf.Max(entry.BaseValue, mutation.Magnitude)
                        : Mathf.Min(entry.BaseValue, mutation.Magnitude);
                    break;
                default:
                    return false;
            }
            if (!GameplayNumber.IsFinite(baseValue))
                return false;

            MarkDirtyTree(mutation.AttributeId, causeEffect);
            entry.BaseValue = baseValue;
            RecalculateDirty();
            return true;
        }

        public bool AddModifier(
            GameplayEffectHandle sourceEffect,
            GameplayAttributeId attributeId,
            GameplayModifierOperation operation,
            float magnitude,
            int priority,
            GameplayClampBound clampBound,
            GameplayAttributeId liveMagnitudeAttribute,
            float liveCoefficient,
            float livePostAdd,
            out GameplayModifierHandle handle)
        {
            handle = default;
            if (m_Disposed || !sourceEffect.IsValid || !m_Entries.TryGetValue(attributeId, out Entry entry))
                return false;
            if (!GameplayNumber.IsFinite(magnitude) ||
                !GameplayNumber.IsFinite(liveCoefficient) ||
                !GameplayNumber.IsFinite(livePostAdd))
                return false;
            if (liveMagnitudeAttribute.IsValid && !m_Entries.ContainsKey(liveMagnitudeAttribute))
                return false;

            ulong value = m_NextModifierHandle++;
            ulong sequence = m_NextInsertionSequence++;
            handle = new GameplayModifierHandle(value, sourceEffect, priority, sequence);
            var modifier = new GameplayAttributeModifier(
                handle,
                attributeId,
                operation,
                magnitude,
                clampBound,
                liveMagnitudeAttribute,
                liveCoefficient,
                livePostAdd);

            MarkDirtyTree(attributeId, sourceEffect);
            entry.Modifiers.Add(modifier);
            m_ModifierAttributes.Add(handle.Value, attributeId);
            if (liveMagnitudeAttribute.IsValid)
                AddDependency(liveMagnitudeAttribute, attributeId);
            RecalculateDirty();
            return true;
        }

        public bool RemoveModifier(GameplayModifierHandle handle, GameplayEffectHandle causeEffect)
        {
            if (m_Disposed || !handle.IsValid || !m_ModifierAttributes.TryGetValue(handle.Value, out GameplayAttributeId attributeId))
                return false;
            Entry entry = m_Entries[attributeId];
            for (int i = 0; i < entry.Modifiers.Count; i++)
            {
                GameplayAttributeModifier modifier = entry.Modifiers[i];
                if (!modifier.Handle.Equals(handle))
                    continue;

                MarkDirtyTree(attributeId, causeEffect);
                entry.Modifiers.RemoveAt(i);
                m_ModifierAttributes.Remove(handle.Value);
                if (modifier.HasLiveMagnitude)
                    RemoveDependency(modifier.LiveMagnitudeAttribute, attributeId);
                RecalculateDirty();
                return true;
            }
            return false;
        }

        public int RemoveModifiersByEffect(GameplayEffectHandle sourceEffect)
        {
            if (m_Disposed || !sourceEffect.IsValid)
                return 0;

            int removed = 0;
            var handles = new List<GameplayModifierHandle>();
            foreach (Entry entry in m_Entries.Values)
            {
                for (int i = 0; i < entry.Modifiers.Count; i++)
                {
                    if (entry.Modifiers[i].Handle.SourceEffect == sourceEffect)
                        handles.Add(entry.Modifiers[i].Handle);
                }
            }
            for (int i = 0; i < handles.Count; i++)
            {
                if (RemoveModifier(handles[i], sourceEffect))
                    removed++;
            }
            return removed;
        }

        public bool ApplyAuthoritativeValue(
            GameplayAttributeId attributeId,
            float baseValue,
            float currentValue,
            ulong revision,
            GameplayEffectHandle causeEffect)
        {
            if (m_Disposed || revision == 0 ||
                !GameplayNumber.IsFinite(baseValue) || !GameplayNumber.IsFinite(currentValue) ||
                !m_Entries.TryGetValue(attributeId, out Entry entry) || revision <= entry.Revision)
                return false;

            float beforeBase = entry.BaseValue;
            float beforeCurrent = entry.CurrentValue;
            entry.BaseValue = baseValue;
            entry.CurrentValue = currentValue;
            entry.Revision = revision;
            entry.Dirty = false;
            m_Changes.Add(new GameplayAttributeChange(
                attributeId,
                beforeBase,
                baseValue,
                beforeCurrent,
                currentValue,
                revision,
                causeEffect));

            if (m_Dependents.TryGetValue(attributeId, out HashSet<GameplayAttributeId> dependents))
            {
                foreach (GameplayAttributeId dependent in dependents)
                    MarkDirtyTree(dependent, causeEffect);
                RecalculateDirty();
            }
            return true;
        }

        public bool Restore(
            GameplayAttributeStateSnapshot snapshot,
            ulong expectedRevision,
            GameplayEffectHandle causeEffect)
        {
            GameplayAttributeValue value = snapshot.Value;
            if (m_Disposed || !GameplayNumber.IsFinite(value.BaseValue) || !GameplayNumber.IsFinite(value.CurrentValue) ||
                !m_Entries.TryGetValue(value.AttributeId, out Entry entry) || entry.Revision != expectedRevision)
                return false;

            float beforeBase = entry.BaseValue;
            float beforeCurrent = entry.CurrentValue;
            entry.BaseValue = value.BaseValue;
            entry.CurrentValue = value.CurrentValue;
            entry.Revision = value.Revision;
            entry.Dirty = false;
            m_Changes.Add(new GameplayAttributeChange(
                value.AttributeId,
                beforeBase,
                value.BaseValue,
                beforeCurrent,
                value.CurrentValue,
                value.Revision,
                causeEffect));

            if (m_Dependents.TryGetValue(value.AttributeId, out HashSet<GameplayAttributeId> dependents))
            {
                foreach (GameplayAttributeId dependent in dependents)
                    MarkDirtyTree(dependent, causeEffect);
                RecalculateDirty();
            }
            return true;
        }

        public void DrainChanges(List<GameplayAttributeChange> destination)
        {
            if (destination == null)
                return;
            destination.AddRange(m_Changes);
            m_Changes.Clear();
        }

        internal TransactionSnapshot CaptureTransactionSnapshot()
        {
            var entries = new Dictionary<GameplayAttributeId, EntryTransactionState>();
            foreach (KeyValuePair<GameplayAttributeId, Entry> pair in m_Entries)
            {
                Entry value = pair.Value;
                entries.Add(pair.Key, new EntryTransactionState(
                    value.BaseValue,
                    value.CurrentValue,
                    value.Revision,
                    value.Dirty,
                    value.BeforeDirtyBase,
                    value.BeforeDirtyCurrent,
                    value.DirtyCause,
                    value.Modifiers.ToArray()));
            }
            return new TransactionSnapshot(entries, m_NextModifierHandle, m_NextInsertionSequence);
        }

        internal void RestoreTransactionSnapshot(TransactionSnapshot snapshot)
        {
            m_ModifierAttributes.Clear();
            m_Dependents.Clear();
            m_DependencyRefCounts.Clear();
            m_DirtyTraversal.Clear();
            m_RecalculateTraversal.Clear();
            foreach (Entry entry in m_Entries.Values)
            {
                RegisterBoundDependency(entry.Definition.AttributeId, entry.Definition.Minimum);
                RegisterBoundDependency(entry.Definition.AttributeId, entry.Definition.Maximum);
            }
            foreach (KeyValuePair<GameplayAttributeId, EntryTransactionState> pair in snapshot.Entries)
            {
                Entry entry = m_Entries[pair.Key];
                EntryTransactionState value = pair.Value;
                entry.BaseValue = value.BaseValue;
                entry.CurrentValue = value.CurrentValue;
                entry.Revision = value.Revision;
                entry.Dirty = value.Dirty;
                entry.BeforeDirtyBase = value.BeforeDirtyBase;
                entry.BeforeDirtyCurrent = value.BeforeDirtyCurrent;
                entry.DirtyCause = value.DirtyCause;
                entry.Modifiers.Clear();
                entry.Modifiers.AddRange(value.Modifiers);
                for (int i = 0; i < value.Modifiers.Length; i++)
                {
                    GameplayAttributeModifier modifier = value.Modifiers[i];
                    m_ModifierAttributes.Add(modifier.Handle.Value, pair.Key);
                    if (modifier.HasLiveMagnitude)
                        AddDependency(modifier.LiveMagnitudeAttribute, pair.Key);
                }
            }
            m_NextModifierHandle = snapshot.NextModifierHandle;
            m_NextInsertionSequence = snapshot.NextInsertionSequence;
            m_Changes.Clear();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Entries.Clear();
            m_ModifierAttributes.Clear();
            m_Dependents.Clear();
            m_DependencyRefCounts.Clear();
            m_Changes.Clear();
            m_DirtyTraversal.Clear();
            m_RecalculateTraversal.Clear();
        }

        void RegisterBoundDependency(GameplayAttributeId dependent, GameplayAttributeBoundData bound)
        {
            if (bound.Enabled && bound.Source == GameplayAttributeBoundSource.Attribute)
                AddDependency(bound.AttributeId, dependent);
        }

        void AddDependency(GameplayAttributeId source, GameplayAttributeId dependent)
        {
            var key = new AttributeDependencyKey(source, dependent);
            if (m_DependencyRefCounts.TryGetValue(key, out int count))
            {
                m_DependencyRefCounts[key] = count + 1;
                return;
            }

            m_DependencyRefCounts.Add(key, 1);
            if (!m_Dependents.TryGetValue(source, out HashSet<GameplayAttributeId> values))
            {
                values = new HashSet<GameplayAttributeId>();
                m_Dependents.Add(source, values);
            }
            values.Add(dependent);
        }

        void RemoveDependency(GameplayAttributeId source, GameplayAttributeId dependent)
        {
            var key = new AttributeDependencyKey(source, dependent);
            if (!m_DependencyRefCounts.TryGetValue(key, out int count))
                return;
            if (count > 1)
            {
                m_DependencyRefCounts[key] = count - 1;
                return;
            }

            m_DependencyRefCounts.Remove(key);
            if (!m_Dependents.TryGetValue(source, out HashSet<GameplayAttributeId> values))
                return;
            values.Remove(dependent);
            if (values.Count == 0)
                m_Dependents.Remove(source);
        }

        void MarkDirtyTree(GameplayAttributeId attributeId, GameplayEffectHandle causeEffect)
        {
            m_DirtyTraversal.Clear();
            MarkDirtyRecursive(attributeId, causeEffect);
            m_DirtyTraversal.Clear();
        }

        void MarkDirtyRecursive(GameplayAttributeId attributeId, GameplayEffectHandle causeEffect)
        {
            if (!m_DirtyTraversal.Add(attributeId) || !m_Entries.TryGetValue(attributeId, out Entry entry))
                return;
            if (!entry.Dirty)
            {
                entry.Dirty = true;
                entry.BeforeDirtyBase = entry.BaseValue;
                entry.BeforeDirtyCurrent = entry.CurrentValue;
                entry.DirtyCause = causeEffect;
            }
            if (!m_Dependents.TryGetValue(attributeId, out HashSet<GameplayAttributeId> dependents))
                return;
            foreach (GameplayAttributeId dependent in dependents)
                MarkDirtyRecursive(dependent, causeEffect);
        }

        void RecalculateDirty()
        {
            m_RecalculateTraversal.Clear();
            foreach (Entry entry in m_Entries.Values)
            {
                if (entry.Dirty)
                    Recalculate(entry);
            }
            m_RecalculateTraversal.Clear();
        }

        void Recalculate(Entry entry)
        {
            if (!entry.Dirty)
                return;
            GameplayAttributeId attributeId = entry.Definition.AttributeId;
            if (!m_RecalculateTraversal.Add(attributeId))
                throw new InvalidOperationException($"Gameplay Attribute dependency cycle reached '{attributeId}'.");

            float value = entry.BaseValue;
            float additive = 0f;
            float multiplicative = 1f;
            bool hasOverride = false;
            float overrideValue = 0f;
            int overridePriority = int.MinValue;
            ulong overrideSequence = 0;
            float minimum = float.NegativeInfinity;
            float maximum = float.PositiveInfinity;

            for (int i = 0; i < entry.Modifiers.Count; i++)
            {
                GameplayAttributeModifier modifier = entry.Modifiers[i];
                float magnitude = modifier.HasLiveMagnitude
                    ? ReadCurrent(modifier.LiveMagnitudeAttribute) * modifier.LiveCoefficient + modifier.LivePostAdd
                    : modifier.Magnitude;
                if (!GameplayNumber.IsFinite(magnitude))
                    throw new InvalidOperationException($"Gameplay Attribute '{attributeId}' modifier resolved a non-finite magnitude.");
                switch (modifier.Operation)
                {
                    case GameplayModifierOperation.Additive:
                        additive += magnitude;
                        EnsureFinite(attributeId, additive);
                        break;
                    case GameplayModifierOperation.Multiplicative:
                        multiplicative *= magnitude;
                        EnsureFinite(attributeId, multiplicative);
                        break;
                    case GameplayModifierOperation.Override:
                        if (!hasOverride || modifier.Handle.Priority > overridePriority ||
                            modifier.Handle.Priority == overridePriority && modifier.Handle.InsertionSequence > overrideSequence)
                        {
                            hasOverride = true;
                            overrideValue = magnitude;
                            overridePriority = modifier.Handle.Priority;
                            overrideSequence = modifier.Handle.InsertionSequence;
                        }
                        break;
                    case GameplayModifierOperation.Clamp:
                        if (modifier.ClampBound == GameplayClampBound.Minimum)
                            minimum = Mathf.Max(minimum, magnitude);
                        else
                            maximum = Mathf.Min(maximum, magnitude);
                        break;
                }
            }

            value += additive;
            EnsureFinite(attributeId, value);
            value *= multiplicative;
            EnsureFinite(attributeId, value);
            if (hasOverride)
                value = overrideValue;
            ApplyDefinitionBound(entry.Definition.Minimum, ref minimum, ref maximum, true);
            ApplyDefinitionBound(entry.Definition.Maximum, ref minimum, ref maximum, false);
            if (minimum > maximum)
                throw new InvalidOperationException($"Gameplay Attribute '{attributeId}' resolved minimum {minimum} above maximum {maximum}.");
            value = Mathf.Clamp(value, minimum, maximum);
            EnsureFinite(attributeId, value);

            entry.CurrentValue = value;
            entry.Dirty = false;
            m_RecalculateTraversal.Remove(attributeId);
            if (!entry.BeforeDirtyBase.Equals(entry.BaseValue) || !entry.BeforeDirtyCurrent.Equals(entry.CurrentValue))
            {
                entry.Revision++;
                m_Changes.Add(new GameplayAttributeChange(
                    attributeId,
                    entry.BeforeDirtyBase,
                    entry.BaseValue,
                    entry.BeforeDirtyCurrent,
                    entry.CurrentValue,
                    entry.Revision,
                    entry.DirtyCause));
            }
        }

        static void EnsureFinite(GameplayAttributeId attributeId, float value)
        {
            if (!GameplayNumber.IsFinite(value))
                throw new InvalidOperationException($"Gameplay Attribute '{attributeId}' resolved a non-finite value.");
        }

        float ReadCurrent(GameplayAttributeId attributeId)
        {
            if (!m_Entries.TryGetValue(attributeId, out Entry source))
                throw new InvalidOperationException($"Gameplay Attribute '{attributeId}' is not registered.");
            if (source.Dirty)
                Recalculate(source);
            EnsureFinite(attributeId, source.CurrentValue);
            return source.CurrentValue;
        }

        void ApplyDefinitionBound(GameplayAttributeBoundData bound, ref float minimum, ref float maximum, bool isMinimum)
        {
            if (!bound.Enabled)
                return;
            float value = bound.Source == GameplayAttributeBoundSource.Attribute
                ? ReadCurrent(bound.AttributeId)
                : bound.Constant;
            if (isMinimum)
                minimum = Mathf.Max(minimum, value);
            else
                maximum = Mathf.Min(maximum, value);
        }

        sealed class Entry
        {
            public Entry(GameplayAttributeDefinitionData definition, float baseValue)
            {
                Definition = definition;
                BaseValue = baseValue;
                CurrentValue = baseValue;
            }

            public GameplayAttributeDefinitionData Definition { get; }
            public float BaseValue;
            public float CurrentValue;
            public ulong Revision;
            public bool Dirty;
            public float BeforeDirtyBase;
            public float BeforeDirtyCurrent;
            public GameplayEffectHandle DirtyCause;
            public List<GameplayAttributeModifier> Modifiers { get; } = new List<GameplayAttributeModifier>();
            public GameplayAttributeValue Snapshot() => new GameplayAttributeValue(Definition.AttributeId, BaseValue, CurrentValue, Revision);
        }

        internal sealed class TransactionSnapshot
        {
            internal TransactionSnapshot(
                Dictionary<GameplayAttributeId, EntryTransactionState> entries,
                ulong nextModifierHandle,
                ulong nextInsertionSequence)
            {
                Entries = entries;
                NextModifierHandle = nextModifierHandle;
                NextInsertionSequence = nextInsertionSequence;
            }

            internal Dictionary<GameplayAttributeId, EntryTransactionState> Entries { get; }
            internal ulong NextModifierHandle { get; }
            internal ulong NextInsertionSequence { get; }
        }

        internal readonly struct EntryTransactionState
        {
            public EntryTransactionState(
                float baseValue,
                float currentValue,
                ulong revision,
                bool dirty,
                float beforeDirtyBase,
                float beforeDirtyCurrent,
                GameplayEffectHandle dirtyCause,
                GameplayAttributeModifier[] modifiers)
            {
                BaseValue = baseValue;
                CurrentValue = currentValue;
                Revision = revision;
                Dirty = dirty;
                BeforeDirtyBase = beforeDirtyBase;
                BeforeDirtyCurrent = beforeDirtyCurrent;
                DirtyCause = dirtyCause;
                Modifiers = modifiers;
            }

            public float BaseValue { get; }
            public float CurrentValue { get; }
            public ulong Revision { get; }
            public bool Dirty { get; }
            public float BeforeDirtyBase { get; }
            public float BeforeDirtyCurrent { get; }
            public GameplayEffectHandle DirtyCause { get; }
            public GameplayAttributeModifier[] Modifiers { get; }
        }

        readonly struct AttributeDependencyKey : IEquatable<AttributeDependencyKey>
        {
            public AttributeDependencyKey(GameplayAttributeId source, GameplayAttributeId dependent)
            {
                Source = source;
                Dependent = dependent;
            }

            public GameplayAttributeId Source { get; }
            public GameplayAttributeId Dependent { get; }
            public bool Equals(AttributeDependencyKey other) => Source == other.Source && Dependent == other.Dependent;
            public override bool Equals(object obj) => obj is AttributeDependencyKey other && Equals(other);
            public override int GetHashCode() => Source.GetHashCode() * 397 ^ Dependent.GetHashCode();
        }
    }
}
