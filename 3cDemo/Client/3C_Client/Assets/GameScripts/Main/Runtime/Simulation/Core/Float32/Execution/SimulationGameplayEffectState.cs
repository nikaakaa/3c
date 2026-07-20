using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ThirdPersonSimulation
{
    internal readonly struct PortableAttributeChange
    {
        public PortableAttributeChange(
            string attributeId,
            Float32Scalar beforeBase,
            Float32Scalar baseValue,
            Float32Scalar beforeCurrent,
            Float32Scalar currentValue,
            ulong revision,
            ulong causeHandle)
        {
            AttributeId = attributeId;
            BeforeBase = beforeBase;
            BaseValue = baseValue;
            BeforeCurrent = beforeCurrent;
            CurrentValue = currentValue;
            Revision = revision;
            CauseHandle = causeHandle;
        }

        public string AttributeId { get; }
        public Float32Scalar BeforeBase { get; }
        public Float32Scalar BaseValue { get; }
        public Float32Scalar BeforeCurrent { get; }
        public Float32Scalar CurrentValue { get; }
        public ulong Revision { get; }
        public ulong CauseHandle { get; }
    }

    internal readonly struct PortableAttributeBefore
    {
        public PortableAttributeBefore(Float32Scalar @base, Float32Scalar current, ulong revision)
        {
            Base = @base;
            Current = current;
            Revision = revision;
        }

        public Float32Scalar Base { get; }
        public Float32Scalar Current { get; }
        public ulong Revision { get; }
    }

    internal sealed class PortableAttributeModifierState
    {
        public ulong Handle;
        public ulong SourceEffectHandle;
        public PortableModifierOperation Operation;
        public Float32Scalar Magnitude;
        public int Priority;
        public PortableClampBound ClampBound;
        public string LiveAttributeId = string.Empty;
        public Float32Scalar LiveCoefficient;
        public Float32Scalar LivePostAdd;
        public ulong InsertionSequence;
    }

    internal sealed class PortableAttributeState
    {
        public PortableAttributeDefinition Definition;
        public Float32Scalar BaseValue;
        public Float32Scalar CurrentValue;
        public ulong Revision;
        public List<PortableAttributeModifierState> Modifiers { get; } = new List<PortableAttributeModifierState>();
    }

    internal sealed class PortableEffectSpecState
    {
        public PortableEffectDefinition Definition;
        public SimulationGameplayEffectContext Context;
        public SortedDictionary<string, Float32Scalar> SetByCaller { get; } = new SortedDictionary<string, Float32Scalar>(StringComparer.Ordinal);
        public SortedDictionary<string, Float32Scalar> SourceAttributes { get; } = new SortedDictionary<string, Float32Scalar>(StringComparer.Ordinal);
        public SortedDictionary<string, Float32Scalar> TargetAttributes { get; } = new SortedDictionary<string, Float32Scalar>(StringComparer.Ordinal);
        public string[] SourceTags = Array.Empty<string>();
        public string[] TargetTags = Array.Empty<string>();
        public ulong DurationTicks;
        public ulong PeriodTicks;
    }

    internal sealed class PortableActiveEffectState : GameplayEffectActiveControlState<PortableEffectSpecState>
    {
    }

    internal readonly struct GameplayEffectActiveIdentity
    {
        internal GameplayEffectActiveIdentity(
            ulong handle,
            ulong instanceId,
            PortableEffectDefinition definition,
            SimulationGameplayEffectContext context)
        {
            Handle = handle;
            InstanceId = instanceId;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Context = context;
        }

        internal ulong Handle { get; }
        internal ulong InstanceId { get; }
        internal PortableEffectDefinition Definition { get; }
        internal SimulationGameplayEffectContext Context { get; }
    }

    internal readonly struct PortablePredictionAttributeSnapshot
    {
        public PortablePredictionAttributeSnapshot(string attributeId, Float32Scalar baseValue, Float32Scalar currentValue, ulong beforeRevision, ulong afterRevision)
        {
            AttributeId = attributeId;
            BaseValue = baseValue;
            CurrentValue = currentValue;
            BeforeRevision = beforeRevision;
            AfterRevision = afterRevision;
        }

        public string AttributeId { get; }
        public Float32Scalar BaseValue { get; }
        public Float32Scalar CurrentValue { get; }
        public ulong BeforeRevision { get; }
        public ulong AfterRevision { get; }

        public PortablePredictionAttributeSnapshot WithAfterRevision(ulong revision)
        {
            return new PortablePredictionAttributeSnapshot(AttributeId, BaseValue, CurrentValue, BeforeRevision, revision);
        }
    }

    internal sealed class PortablePredictionRecord : GameplayEffectPredictionControlState<PortableEffectSpecState, PortablePredictionAttributeSnapshot>
    {
        protected override GameplayEffectContextIdentity DescribeContext()
        {
            SimulationGameplayEffectContext context = Spec.Context;
            return new GameplayEffectContextIdentity(
                context.SourceActorId,
                context.TargetActorId,
                context.SourceActionInstanceId,
                context.PredictionKey,
                context.GameplayResultId,
                context.SourceTick,
                context.IsPredicted);
        }
    }

    internal sealed class GameplayEffectStateAggregate
    {
        readonly SortedDictionary<string, string[]> m_TagSources;
        readonly SortedDictionary<string, PortableAttributeState> m_Attributes;
        readonly List<PortableActiveEffectState> m_ActiveEffects;
        readonly SortedDictionary<ulong, ulong> m_Periods;
        readonly SortedDictionary<ulong, List<PortablePredictionRecord>> m_Journal;
        readonly SortedDictionary<ulong, ulong> m_LastLifecycleRevisions;
        readonly string[] m_OwnedTags;

        internal GameplayEffectStateAggregate(
            IReadOnlyDictionary<string, string[]> tagSources,
            IReadOnlyDictionary<string, PortableAttributeState> attributes,
            IReadOnlyList<PortableActiveEffectState> activeEffects,
            IReadOnlyDictionary<ulong, ulong> periods,
            IReadOnlyDictionary<ulong, List<PortablePredictionRecord>> journal,
            IReadOnlyDictionary<ulong, ulong> lastLifecycleRevisions,
            ulong changeCursor)
        {
            m_TagSources = CloneTagSources(tagSources);
            m_Attributes = CloneAttributes(attributes);
            m_ActiveEffects = CloneActiveEffects(activeEffects);
            m_Periods = CloneMap(periods);
            m_Journal = CloneJournal(journal);
            m_LastLifecycleRevisions = CloneMap(lastLifecycleRevisions);
            m_OwnedTags = CollectOwnedTags(m_TagSources);
            ChangeCursor = changeCursor;
        }

        internal ulong ChangeCursor { get; }
        internal int ActiveEffectCount => m_ActiveEffects.Count;

        internal void CollectActiveEffectIdentities(List<GameplayEffectActiveIdentity> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            values.Clear();
            if (values.Capacity < m_ActiveEffects.Count)
                values.Capacity = m_ActiveEffects.Count;
            for (int i = 0; i < m_ActiveEffects.Count; i++)
            {
                PortableActiveEffectState active = m_ActiveEffects[i];
                values.Add(new GameplayEffectActiveIdentity(
                    active.Handle,
                    active.InstanceId,
                    active.Spec.Definition,
                    active.Spec.Context));
            }
        }

        internal IReadOnlyList<string> CopyOwnedTags() => m_OwnedTags;

        internal bool TryGetAttribute(
            string attributeId,
            out Float32Scalar baseValue,
            out Float32Scalar currentValue,
            out ulong revision)
        {
            if (m_Attributes.TryGetValue(
                SimulationGameplayEffectProgram.NormalizeAttribute(attributeId),
                out PortableAttributeState value))
            {
                baseValue = value.BaseValue;
                currentValue = value.CurrentValue;
                revision = value.Revision;
                return true;
            }
            baseValue = Float32Scalar.Zero;
            currentValue = Float32Scalar.Zero;
            revision = 0;
            return false;
        }

        internal static GameplayEffectStateAggregate CreateInitial(SimulationGameplayEffectProgram program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            return new SimulationGameplayEffectState(program, null).Freeze();
        }

        internal void CopyTo(
            IDictionary<string, string[]> tagSources,
            IDictionary<string, PortableAttributeState> attributes,
            IList<PortableActiveEffectState> activeEffects,
            IDictionary<ulong, ulong> periods,
            IDictionary<ulong, List<PortablePredictionRecord>> journal,
            IDictionary<ulong, ulong> lastLifecycleRevisions)
        {
            Copy(CloneTagSources(m_TagSources), tagSources);
            Copy(CloneAttributes(m_Attributes), attributes);
            Copy(CloneActiveEffects(m_ActiveEffects), activeEffects);
            Copy(CloneMap(m_Periods), periods);
            Copy(CloneJournal(m_Journal), journal);
            Copy(CloneMap(m_LastLifecycleRevisions), lastLifecycleRevisions);
        }

        static SortedDictionary<string, string[]> CloneTagSources(IReadOnlyDictionary<string, string[]> source)
        {
            var result = new SortedDictionary<string, string[]>(StringComparer.Ordinal);
            if (source == null)
                return result;
            foreach (KeyValuePair<string, string[]> pair in source)
                result.Add(pair.Key, pair.Value == null ? Array.Empty<string>() : (string[])pair.Value.Clone());
            return result;
        }

        static SortedDictionary<string, PortableAttributeState> CloneAttributes(IReadOnlyDictionary<string, PortableAttributeState> source)
        {
            var result = new SortedDictionary<string, PortableAttributeState>(StringComparer.Ordinal);
            if (source == null)
                return result;
            foreach (KeyValuePair<string, PortableAttributeState> pair in source)
                result.Add(pair.Key, CloneAttribute(pair.Value));
            return result;
        }

        static PortableAttributeState CloneAttribute(PortableAttributeState source)
        {
            var result = new PortableAttributeState
            {
                Definition = source.Definition,
                BaseValue = source.BaseValue,
                CurrentValue = source.CurrentValue,
                Revision = source.Revision
            };
            for (int i = 0; i < source.Modifiers.Count; i++)
                result.Modifiers.Add(CloneModifier(source.Modifiers[i]));
            return result;
        }

        static PortableAttributeModifierState CloneModifier(PortableAttributeModifierState source)
        {
            return new PortableAttributeModifierState
            {
                Handle = source.Handle,
                SourceEffectHandle = source.SourceEffectHandle,
                Operation = source.Operation,
                Magnitude = source.Magnitude,
                Priority = source.Priority,
                ClampBound = source.ClampBound,
                LiveAttributeId = source.LiveAttributeId,
                LiveCoefficient = source.LiveCoefficient,
                LivePostAdd = source.LivePostAdd,
                InsertionSequence = source.InsertionSequence
            };
        }

        static List<PortableActiveEffectState> CloneActiveEffects(IReadOnlyList<PortableActiveEffectState> source)
        {
            var result = new List<PortableActiveEffectState>(source?.Count ?? 0);
            if (source == null)
                return result;
            for (int i = 0; i < source.Count; i++)
                result.Add(CloneActive(source[i]));
            return result;
        }

        static PortableActiveEffectState CloneActive(PortableActiveEffectState source)
        {
            return new PortableActiveEffectState
            {
                Handle = source.Handle,
                InstanceId = source.InstanceId,
                Spec = CloneSpec(source.Spec),
                StartTick = source.StartTick,
                EndTick = source.EndTick,
                InsertionSequence = source.InsertionSequence,
                StackCount = source.StackCount,
                Inhibited = source.Inhibited,
                LifecycleRevision = source.LifecycleRevision
            };
        }

        static PortableEffectSpecState CloneSpec(PortableEffectSpecState source)
        {
            if (source == null)
                return null;
            var result = new PortableEffectSpecState
            {
                Definition = source.Definition,
                Context = source.Context,
                SourceTags = source.SourceTags == null ? Array.Empty<string>() : (string[])source.SourceTags.Clone(),
                TargetTags = source.TargetTags == null ? Array.Empty<string>() : (string[])source.TargetTags.Clone(),
                DurationTicks = source.DurationTicks,
                PeriodTicks = source.PeriodTicks
            };
            Copy(source.SetByCaller, result.SetByCaller);
            Copy(source.SourceAttributes, result.SourceAttributes);
            Copy(source.TargetAttributes, result.TargetAttributes);
            return result;
        }

        static SortedDictionary<ulong, List<PortablePredictionRecord>> CloneJournal(
            IReadOnlyDictionary<ulong, List<PortablePredictionRecord>> source)
        {
            var result = new SortedDictionary<ulong, List<PortablePredictionRecord>>();
            if (source == null)
                return result;
            foreach (KeyValuePair<ulong, List<PortablePredictionRecord>> pair in source)
            {
                var records = new List<PortablePredictionRecord>(pair.Value.Count);
                for (int i = 0; i < pair.Value.Count; i++)
                    records.Add(ClonePrediction(pair.Value[i]));
                result.Add(pair.Key, records);
            }
            return result;
        }

        static PortablePredictionRecord ClonePrediction(PortablePredictionRecord source)
        {
            var result = new PortablePredictionRecord
            {
                Spec = CloneSpec(source.Spec),
                Handle = source.Handle,
                InstanceId = source.InstanceId,
                CreatedActive = source.CreatedActive,
                HasActiveBefore = source.HasActiveBefore,
                ActiveBefore = source.ActiveBefore,
                Confirmed = source.Confirmed
            };
            result.CueIds.AddRange(source.CueIds);
            foreach (KeyValuePair<string, PortablePredictionAttributeSnapshot> pair in source.Attributes)
                result.Attributes.Add(pair.Key, pair.Value);
            return result;
        }

        static SortedDictionary<ulong, ulong> CloneMap(IReadOnlyDictionary<ulong, ulong> source)
        {
            var result = new SortedDictionary<ulong, ulong>();
            if (source != null)
            {
                foreach (KeyValuePair<ulong, ulong> pair in source)
                    result.Add(pair.Key, pair.Value);
            }
            return result;
        }

        static string[] CollectOwnedTags(IReadOnlyDictionary<string, string[]> sources)
        {
            var tags = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string[] source in sources.Values)
            {
                for (int i = 0; i < source.Length; i++)
                    tags.Add(source[i]);
            }
            var result = new string[tags.Count];
            tags.CopyTo(result);
            return result;
        }

        static void Copy<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> source, IDictionary<TKey, TValue> destination)
        {
            foreach (KeyValuePair<TKey, TValue> pair in source)
                destination.Add(pair.Key, pair.Value);
        }

        static void Copy<T>(IEnumerable<T> source, IList<T> destination)
        {
            foreach (T value in source)
                destination.Add(value);
        }
    }

    internal sealed class SimulationGameplayEffectState
    {
        const uint TagsMagic = 0x53474154;
        const uint AttributesMagic = 0x52545441;
        const uint ActiveMagic = 0x56544341;
        const uint PeriodsMagic = 0x44524550;
        const uint JournalMagic = 0x52554F4A;
        const int StateVersion = 1;

        readonly SimulationGameplayEffectProgram m_Program;
        readonly Float32GameplayEffectExecutionScratch m_Scratch;
        readonly SortedDictionary<string, string[]> m_TagSources = new SortedDictionary<string, string[]>(StringComparer.Ordinal);
        readonly SortedDictionary<string, PortableAttributeState> m_Attributes = new SortedDictionary<string, PortableAttributeState>(StringComparer.Ordinal);
        readonly List<PortableActiveEffectState> m_ActiveEffects = new List<PortableActiveEffectState>();
        readonly SortedDictionary<ulong, ulong> m_Periods = new SortedDictionary<ulong, ulong>();
        readonly SortedDictionary<ulong, List<PortablePredictionRecord>> m_Journal = new SortedDictionary<ulong, List<PortablePredictionRecord>>();
        readonly SortedDictionary<ulong, ulong> m_LastLifecycleRevisions = new SortedDictionary<ulong, ulong>();
        GameplayEffectStateAggregate m_Baseline;
        ulong m_ChangeCursor;
        bool m_TagsDirty;
        bool m_AttributesDirty;
        bool m_ActiveEffectsDirty;
        bool m_PeriodsDirty;
        bool m_JournalDirty;
        bool m_ChangeCursorDirty;
        bool m_RestoredDirty;

        public SimulationGameplayEffectState(
            SimulationGameplayEffectProgram program,
            GameplayEffectStateAggregate aggregate,
            Float32GameplayEffectExecutionScratch scratch = null)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            m_Program = program;
            m_Scratch = scratch;
            if (aggregate == null)
                Initialize();
            else
                Restore(aggregate);
            ValidateRuntimeClosure();
        }

        public SimulationGameplayEffectProgram Program => m_Program;
        public IReadOnlyList<PortableActiveEffectState> ActiveEffects => m_ActiveEffects;
        public SortedDictionary<ulong, List<PortablePredictionRecord>> Journal => m_Journal;
        public SortedDictionary<ulong, ulong> LastLifecycleRevisions => m_LastLifecycleRevisions;
        public ulong ChangeCursor
        {
            get => m_ChangeCursor;
            set
            {
                if (m_ChangeCursor == value)
                    return;
                m_ChangeCursor = value;
                m_ChangeCursorDirty = true;
            }
        }

        public bool HasChanges =>
            m_TagsDirty ||
            m_AttributesDirty ||
            m_ActiveEffectsDirty ||
            m_PeriodsDirty ||
            m_JournalDirty ||
            m_ChangeCursorDirty ||
            m_RestoredDirty;

        public IReadOnlyList<string> CopyOwnedTags()
        {
            SortedSet<string> tags = m_Scratch?.OwnedTagSet ??
                new SortedSet<string>(StringComparer.Ordinal);
            List<string> values = m_Scratch?.OwnedTags ?? new List<string>();
            tags.Clear();
            values.Clear();
            foreach (string[] source in m_TagSources.Values)
            {
                for (int i = 0; i < source.Length; i++)
                    tags.Add(source[i]);
            }
            foreach (string tag in tags)
                values.Add(tag);
            return values;
        }

        public bool HasTag(string tagId)
        {
            string query = SimulationGameplayEffectProgram.NormalizeTag(tagId);
            foreach (string owned in CopyOwnedTags())
            {
                if (m_Program.IsTagOrParent(owned, query))
                    return true;
            }
            return false;
        }

        public bool Matches(PortableTagQuery query)
        {
            return m_Program.Matches(query, CopyOwnedTags());
        }

        public void SetTagSource(string sourceId, IEnumerable<string> tags)
        {
            string source = SimulationIdentity.Require(sourceId, nameof(sourceId));
            string[] values = CanonicalTags(tags);
            if (values.Length == 0)
            {
                if (m_TagSources.Remove(source))
                    m_TagsDirty = true;
                return;
            }
            if (m_TagSources.TryGetValue(source, out string[] current) && EqualStrings(current, values))
                return;
            m_TagSources[source] = values;
            m_TagsDirty = true;
        }

        public void RemoveTagSource(string sourceId)
        {
            if (m_TagSources.Remove(sourceId))
                m_TagsDirty = true;
        }

        public bool TryGetAttribute(string attributeId, out PortableAttributeState value)
        {
            return m_Attributes.TryGetValue(SimulationGameplayEffectProgram.NormalizeAttribute(attributeId), out value);
        }

        public PortableAttributeState RequireAttribute(string attributeId)
        {
            if (!TryGetAttribute(attributeId, out PortableAttributeState value))
                throw new KeyNotFoundException($"Gameplay Attribute '{attributeId}' is not registered.");
            return value;
        }

        public IReadOnlyList<PortableAttributeChange> MutateBase(string attributeId, PortableModifierOperation operation, Float32Scalar magnitude, PortableClampBound clampBound, ulong causeHandle)
        {
            PortableAttributeState attribute = RequireAttribute(attributeId);
            Dictionary<string, PortableAttributeBefore> before = CaptureAttributeBefore();
            attribute.BaseValue = ApplyModifier(attribute.BaseValue, operation, magnitude, clampBound);
            IReadOnlyList<PortableAttributeChange> changes = RecalculateAll(before, causeHandle, null);
            m_AttributesDirty = true;
            return changes;
        }

        public IReadOnlyList<PortableAttributeChange> AddModifier(string attributeId, PortableAttributeModifierState modifier)
        {
            if (modifier == null || modifier.Handle == 0 || modifier.SourceEffectHandle == 0)
                throw new ArgumentException("Gameplay Attribute modifier identity is incomplete.", nameof(modifier));
            PortableAttributeState attribute = RequireAttribute(attributeId);
            foreach (PortableAttributeState existingAttribute in m_Attributes.Values)
            {
                for (int i = 0; i < existingAttribute.Modifiers.Count; i++)
                {
                    if (existingAttribute.Modifiers[i].Handle == modifier.Handle)
                        throw new InvalidOperationException($"Duplicate Gameplay Attribute modifier handle '{modifier.Handle}'.");
                }
            }
            if (!string.IsNullOrEmpty(modifier.LiveAttributeId))
                RequireAttribute(modifier.LiveAttributeId);
            Dictionary<string, PortableAttributeBefore> before = CaptureAttributeBefore();
            attribute.Modifiers.Add(modifier);
            attribute.Modifiers.Sort(CompareModifier);
            IReadOnlyList<PortableAttributeChange> changes = RecalculateAll(before, modifier.SourceEffectHandle, null);
            m_AttributesDirty = true;
            return changes;
        }

        public IReadOnlyList<PortableAttributeChange> RemoveModifiersByEffect(ulong sourceEffectHandle)
        {
            List<PortableAttributeChange> changes = m_Scratch?.AttributeChanges ?? new List<PortableAttributeChange>();
            changes.Clear();
            bool removed = false;
            foreach (PortableAttributeState attribute in m_Attributes.Values)
            {
                for (int i = attribute.Modifiers.Count - 1; i >= 0; i--)
                {
                    if (attribute.Modifiers[i].SourceEffectHandle != sourceEffectHandle)
                        continue;
                    Dictionary<string, PortableAttributeBefore> before = CaptureAttributeBefore();
                    attribute.Modifiers.RemoveAt(i);
                    removed = true;
                    changes.AddRange(RecalculateAll(before, sourceEffectHandle, null));
                }
            }
            if (removed)
                m_AttributesDirty = true;
            return changes;
        }

        public bool ApplyAuthoritativeAttribute(string attributeId, Float32Scalar baseValue, Float32Scalar currentValue, ulong revision, ulong causeHandle, out IReadOnlyList<PortableAttributeChange> changes)
        {
            PortableAttributeState attribute = RequireAttribute(attributeId);
            if (revision <= attribute.Revision)
            {
                changes = Array.Empty<PortableAttributeChange>();
                return false;
            }
            Dictionary<string, PortableAttributeBefore> before = CaptureAttributeBefore();
            attribute.BaseValue = baseValue;
            attribute.CurrentValue = currentValue;
            attribute.Revision = revision;
            List<PortableAttributeChange> result = m_Scratch?.AttributeChanges ?? new List<PortableAttributeChange>();
            result.Clear();
            result.Add(new PortableAttributeChange(attribute.Definition.Id, before[attribute.Definition.Id].Base, baseValue, before[attribute.Definition.Id].Current, currentValue, revision, causeHandle));
            result.AddRange(RecalculateAll(before, causeHandle, attribute.Definition.Id));
            changes = result;
            m_AttributesDirty = true;
            return true;
        }

        public bool RestorePredictedAttribute(PortablePredictionAttributeSnapshot snapshot, ulong causeHandle, out IReadOnlyList<PortableAttributeChange> changes)
        {
            PortableAttributeState attribute = RequireAttribute(snapshot.AttributeId);
            if (snapshot.AfterRevision == 0 || attribute.Revision != snapshot.AfterRevision)
            {
                changes = Array.Empty<PortableAttributeChange>();
                return false;
            }
            Dictionary<string, PortableAttributeBefore> before = CaptureAttributeBefore();
            attribute.BaseValue = snapshot.BaseValue;
            attribute.CurrentValue = snapshot.CurrentValue;
            attribute.Revision = snapshot.BeforeRevision;
            List<PortableAttributeChange> result = m_Scratch?.AttributeChanges ?? new List<PortableAttributeChange>();
            result.Clear();
            result.Add(new PortableAttributeChange(attribute.Definition.Id, before[attribute.Definition.Id].Base, attribute.BaseValue, before[attribute.Definition.Id].Current, attribute.CurrentValue, attribute.Revision, causeHandle));
            result.AddRange(RecalculateAll(before, causeHandle, attribute.Definition.Id));
            changes = result;
            m_AttributesDirty = true;
            return true;
        }

        public PortableActiveEffectState FindActiveByHandle(ulong handle)
        {
            for (int i = 0; i < m_ActiveEffects.Count; i++)
            {
                if (m_ActiveEffects[i].Handle == handle)
                    return m_ActiveEffects[i];
            }
            return null;
        }

        public PortableActiveEffectState FindActiveByInstance(ulong instanceId)
        {
            for (int i = 0; i < m_ActiveEffects.Count; i++)
            {
                if (m_ActiveEffects[i].InstanceId == instanceId)
                    return m_ActiveEffects[i];
            }
            return null;
        }

        public void AddActive(PortableActiveEffectState active)
        {
            if (active == null || active.Handle == 0 || active.InstanceId == 0 || active.Spec == null)
                throw new ArgumentException("Active Gameplay Effect identity is incomplete.", nameof(active));
            if (FindActiveByHandle(active.Handle) != null || FindActiveByInstance(active.InstanceId) != null)
                throw new InvalidOperationException($"Duplicate Active Gameplay Effect '{active.Handle}/{active.InstanceId}'.");
            m_ActiveEffects.Add(active);
            m_ActiveEffects.Sort(CompareActive);
            m_ActiveEffectsDirty = true;
        }

        public void RemoveActive(PortableActiveEffectState active)
        {
            if (active == null || !m_ActiveEffects.Remove(active))
                throw new InvalidOperationException("Active Gameplay Effect removal target is missing.");
            m_ActiveEffectsDirty = true;
            if (m_Periods.Remove(active.InstanceId))
                m_PeriodsDirty = true;
        }

        public ulong GetNextPeriod(ulong instanceId)
        {
            return m_Periods.TryGetValue(instanceId, out ulong value) ? value : 0;
        }
        public void SetNextPeriod(ulong instanceId, ulong tick)
        {
            if (tick == 0)
            {
                if (m_Periods.Remove(instanceId))
                    m_PeriodsDirty = true;
                return;
            }
            if (m_Periods.TryGetValue(instanceId, out ulong current) && current == tick)
                return;
            m_Periods[instanceId] = tick;
            m_PeriodsDirty = true;
        }

        public void MarkActiveEffectsDirty()
        {
            m_ActiveEffectsDirty = true;
        }

        public void MarkJournalDirty()
        {
            m_JournalDirty = true;
        }

        public GameplayEffectStateAggregate Freeze()
        {
            if (m_Baseline != null &&
                !m_TagsDirty &&
                !m_AttributesDirty &&
                !m_ActiveEffectsDirty &&
                !m_PeriodsDirty &&
                !m_JournalDirty &&
                !m_ChangeCursorDirty)
            {
                return m_Baseline;
            }
            return new GameplayEffectStateAggregate(
                m_TagSources,
                m_Attributes,
                m_ActiveEffects,
                m_Periods,
                m_Journal,
                m_LastLifecycleRevisions,
                m_ChangeCursor);
        }

        public void Restore(GameplayEffectStateAggregate aggregate, bool hasChanges = false)
        {
            if (aggregate == null)
                throw new ArgumentNullException(nameof(aggregate));
            ClearCollections();
            aggregate.CopyTo(
                m_TagSources,
                m_Attributes,
                m_ActiveEffects,
                m_Periods,
                m_Journal,
                m_LastLifecycleRevisions);
            m_Baseline = aggregate;
            m_ChangeCursor = aggregate.ChangeCursor;
            ClearDirty();
            m_RestoredDirty = hasChanges;
            ValidateRuntimeClosure();
        }

        void Initialize()
        {
            ClearCollections();
            m_Baseline = null;
            string[] initialTags = CanonicalTags(m_Program.InitialTags);
            if (initialTags.Length > 0)
                m_TagSources.Add("initial", initialTags);
            foreach (KeyValuePair<string, PortableAttributeDefinition> pair in m_Program.Attributes)
            {
                m_Attributes.Add(pair.Key, new PortableAttributeState
                {
                    Definition = pair.Value,
                    BaseValue = pair.Value.InitialBase,
                    CurrentValue = pair.Value.InitialBase,
                    Revision = 1
                });
            }
            Dictionary<string, Float32Scalar> cache = m_Scratch?.AttributeValues ??
                new Dictionary<string, Float32Scalar>(StringComparer.Ordinal);
            HashSet<string> stack = m_Scratch?.AttributeStack ?? new HashSet<string>(StringComparer.Ordinal);
            cache.Clear();
            stack.Clear();
            foreach (string id in m_Attributes.Keys)
                CalculateCurrent(id, null, cache, stack);
            foreach (PortableAttributeState attribute in m_Attributes.Values)
                attribute.Revision = 1;
            m_ChangeCursor = 0;
            ClearDirty();
        }

        void ClearCollections()
        {
            m_TagSources.Clear();
            m_Attributes.Clear();
            m_ActiveEffects.Clear();
            m_Periods.Clear();
            m_Journal.Clear();
            m_LastLifecycleRevisions.Clear();
        }

        void ClearDirty()
        {
            m_TagsDirty = false;
            m_AttributesDirty = false;
            m_ActiveEffectsDirty = false;
            m_PeriodsDirty = false;
            m_JournalDirty = false;
            m_ChangeCursorDirty = false;
            m_RestoredDirty = false;
        }

        Dictionary<string, PortableAttributeBefore> CaptureAttributeBefore()
        {
            Dictionary<string, PortableAttributeBefore> result = m_Scratch?.AttributeBefore ??
                new Dictionary<string, PortableAttributeBefore>(StringComparer.Ordinal);
            result.Clear();
            foreach (KeyValuePair<string, PortableAttributeState> pair in m_Attributes)
                result.Add(pair.Key, new PortableAttributeBefore(pair.Value.BaseValue, pair.Value.CurrentValue, pair.Value.Revision));
            return result;
        }

        IReadOnlyList<PortableAttributeChange> RecalculateAll(Dictionary<string, PortableAttributeBefore> before, ulong causeHandle, string excludedAttribute)
        {
            Dictionary<string, Float32Scalar> cache = m_Scratch?.AttributeValues ??
                new Dictionary<string, Float32Scalar>(StringComparer.Ordinal);
            HashSet<string> stack = m_Scratch?.AttributeStack ?? new HashSet<string>(StringComparer.Ordinal);
            cache.Clear();
            stack.Clear();
            foreach (string id in m_Attributes.Keys)
                CalculateCurrent(id, excludedAttribute, cache, stack);
            List<PortableAttributeChange> changes = m_Scratch?.RecalculatedAttributeChanges ??
                new List<PortableAttributeChange>();
            changes.Clear();
            foreach (KeyValuePair<string, PortableAttributeState> pair in m_Attributes)
            {
                if (string.Equals(pair.Key, excludedAttribute, StringComparison.Ordinal))
                    continue;
                PortableAttributeBefore previous = before[pair.Key];
                PortableAttributeState current = pair.Value;
                if (previous.Base == current.BaseValue && previous.Current == current.CurrentValue)
                    continue;
                current.Revision = checked(current.Revision + 1);
                changes.Add(new PortableAttributeChange(pair.Key, previous.Base, current.BaseValue, previous.Current, current.CurrentValue, current.Revision, causeHandle));
            }
            return changes;
        }

        Float32Scalar CalculateCurrent(string id, string excludedAttribute, Dictionary<string, Float32Scalar> cache, HashSet<string> stack)
        {
            if (cache.TryGetValue(id, out Float32Scalar cached))
                return cached;
            PortableAttributeState attribute = RequireAttribute(id);
            if (string.Equals(id, excludedAttribute, StringComparison.Ordinal))
            {
                cache.Add(id, attribute.CurrentValue);
                return attribute.CurrentValue;
            }
            if (!stack.Add(id))
                throw new InvalidOperationException($"Gameplay Attribute dependency cycle reached '{id}'.");

            Float32Scalar additive = Float32Scalar.Zero;
            Float32Scalar multiplicative = Float32Scalar.One;
            bool hasOverride = false;
            Float32Scalar overrideValue = Float32Scalar.Zero;
            int overridePriority = int.MinValue;
            ulong overrideSequence = 0;
            bool hasMinimum = false;
            Float32Scalar minimum = Float32Scalar.Zero;
            bool hasMaximum = false;
            Float32Scalar maximum = Float32Scalar.Zero;
            for (int i = 0; i < attribute.Modifiers.Count; i++)
            {
                PortableAttributeModifierState modifier = attribute.Modifiers[i];
                Float32Scalar magnitude = string.IsNullOrEmpty(modifier.LiveAttributeId)
                    ? modifier.Magnitude
                    : CalculateCurrent(modifier.LiveAttributeId, excludedAttribute, cache, stack) * modifier.LiveCoefficient + modifier.LivePostAdd;
                switch (modifier.Operation)
                {
                    case PortableModifierOperation.Additive:
                        additive += magnitude;
                        break;
                    case PortableModifierOperation.Multiplicative:
                        multiplicative *= magnitude;
                        break;
                    case PortableModifierOperation.Override:
                        if (!hasOverride || modifier.Priority > overridePriority ||
                            modifier.Priority == overridePriority && modifier.InsertionSequence > overrideSequence)
                        {
                            hasOverride = true;
                            overrideValue = magnitude;
                            overridePriority = modifier.Priority;
                            overrideSequence = modifier.InsertionSequence;
                        }
                        break;
                    case PortableModifierOperation.Clamp:
                        if (modifier.ClampBound == PortableClampBound.Minimum)
                        {
                            minimum = hasMinimum ? Float32Scalar.Max(minimum, magnitude) : magnitude;
                            hasMinimum = true;
                        }
                        else
                        {
                            maximum = hasMaximum ? Float32Scalar.Min(maximum, magnitude) : magnitude;
                            hasMaximum = true;
                        }
                        break;
                    default:
                        throw new InvalidOperationException($"Gameplay Attribute modifier operation '{modifier.Operation}' is invalid.");
                }
            }

            Float32Scalar value = (attribute.BaseValue + additive) * multiplicative;
            if (hasOverride)
                value = overrideValue;
            ApplyDefinitionBound(attribute.Definition.Minimum, true, excludedAttribute, cache, stack, ref hasMinimum, ref minimum, ref hasMaximum, ref maximum);
            ApplyDefinitionBound(attribute.Definition.Maximum, false, excludedAttribute, cache, stack, ref hasMinimum, ref minimum, ref hasMaximum, ref maximum);
            if (hasMinimum && hasMaximum && minimum > maximum)
                throw new InvalidOperationException($"Gameplay Attribute '{id}' minimum exceeds maximum.");
            if (hasMinimum && value < minimum)
                value = minimum;
            if (hasMaximum && value > maximum)
                value = maximum;
            attribute.CurrentValue = value;
            cache.Add(id, value);
            stack.Remove(id);
            return value;
        }

        void ApplyDefinitionBound(
            PortableAttributeBound bound,
            bool minimumBound,
            string excludedAttribute,
            Dictionary<string, Float32Scalar> cache,
            HashSet<string> stack,
            ref bool hasMinimum,
            ref Float32Scalar minimum,
            ref bool hasMaximum,
            ref Float32Scalar maximum)
        {
            if (!bound.Enabled)
                return;
            Float32Scalar value = bound.FromAttribute
                ? CalculateCurrent(bound.AttributeId, excludedAttribute, cache, stack)
                : bound.Constant;
            if (minimumBound)
            {
                minimum = hasMinimum ? Float32Scalar.Max(minimum, value) : value;
                hasMinimum = true;
            }
            else
            {
                maximum = hasMaximum ? Float32Scalar.Min(maximum, value) : value;
                hasMaximum = true;
            }
        }

        static Float32Scalar ApplyModifier(Float32Scalar current, PortableModifierOperation operation, Float32Scalar magnitude, PortableClampBound clampBound)
        {
            return operation switch
            {
                PortableModifierOperation.Additive => current + magnitude,
                PortableModifierOperation.Multiplicative => current * magnitude,
                PortableModifierOperation.Override => magnitude,
                PortableModifierOperation.Clamp when clampBound == PortableClampBound.Minimum => Float32Scalar.Max(current, magnitude),
                PortableModifierOperation.Clamp => Float32Scalar.Min(current, magnitude),
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            };
        }
        void ValidateRuntimeClosure()
        {
            HashSet<ulong> handles = m_Scratch?.ActiveHandles ?? new HashSet<ulong>();
            HashSet<ulong> instances = m_Scratch?.ActiveInstances ?? new HashSet<ulong>();
            handles.Clear();
            instances.Clear();
            ulong previousInsertion = 0;
            for (int i = 0; i < m_ActiveEffects.Count; i++)
            {
                PortableActiveEffectState active = m_ActiveEffects[i];
                if (!handles.Add(active.Handle) || !instances.Add(active.InstanceId) || active.InsertionSequence < previousInsertion)
                    throw new InvalidDataException("Active Gameplay Effect state is duplicated or not ordered.");
                previousInsertion = active.InsertionSequence;
            }
            foreach (KeyValuePair<ulong, ulong> period in m_Periods)
            {
                PortableActiveEffectState active = FindActiveByInstance(period.Key);
                if (active == null || active.Spec.PeriodTicks == 0)
                    throw new InvalidDataException($"Gameplay Effect period references unknown or non-periodic instance '{period.Key}'.");
            }
            foreach (PortableAttributeState attribute in m_Attributes.Values)
            {
                for (int i = 0; i < attribute.Modifiers.Count; i++)
                {
                    if (FindActiveByHandle(attribute.Modifiers[i].SourceEffectHandle) == null)
                        throw new InvalidDataException($"Gameplay Attribute modifier '{attribute.Modifiers[i].Handle}' references unknown Active Effect.");
                }
            }
        }

        string[] CanonicalTags(IEnumerable<string> tags)
        {
            List<string> values = m_Scratch?.CanonicalTags ?? new List<string>();
            values.Clear();
            try
            {
                if (tags != null)
                {
                    foreach (string tag in tags)
                        values.Add(SimulationGameplayEffectProgram.NormalizeTag(tag));
                }
                values.Sort(StringComparer.Ordinal);
                for (int i = values.Count - 1; i > 0; i--)
                {
                    if (string.Equals(values[i - 1], values[i], StringComparison.Ordinal))
                        values.RemoveAt(i);
                }
                return values.ToArray();
            }
            finally
            {
                values.Clear();
            }
        }

        static bool EqualStrings(string[] left, string[] right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null || left.Length != right.Length)
                return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        static int CompareModifier(PortableAttributeModifierState left, PortableAttributeModifierState right)
        {
            int byInsertion = left.InsertionSequence.CompareTo(right.InsertionSequence);
            return byInsertion != 0 ? byInsertion : left.Handle.CompareTo(right.Handle);
        }

        static int CompareActive(PortableActiveEffectState left, PortableActiveEffectState right)
        {
            int byInsertion = left.InsertionSequence.CompareTo(right.InsertionSequence);
            return byInsertion != 0 ? byInsertion : left.Handle.CompareTo(right.Handle);
        }
    }
}
