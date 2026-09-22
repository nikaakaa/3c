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

        internal GameplayEffectStateAggregate CreateChangedFrom(
            SortedDictionary<string, string[]> tagSources,
            SortedDictionary<string, PortableAttributeState> attributes,
            List<PortableActiveEffectState> activeEffects,
            SortedDictionary<ulong, ulong> periods,
            SortedDictionary<ulong, List<PortablePredictionRecord>> journal,
            SortedDictionary<ulong, ulong> lastLifecycleRevisions,
            bool tagsChanged,
            bool attributesChanged,
            bool activeEffectsChanged,
            bool periodsChanged,
            bool journalChanged,
            bool lastLifecycleRevisionsChanged,
            ulong changeCursor)
        {
            SortedDictionary<string, string[]> changedTags = tagsChanged
                ? AdoptChangedTagSources(tagSources, m_TagSources)
                : m_TagSources;
            SortedDictionary<string, PortableAttributeState> changedAttributes = attributesChanged
                ? CloneChangedAttributes(attributes, m_Attributes)
                : m_Attributes;
            List<PortableActiveEffectState> changedActiveEffects = activeEffectsChanged
                ? CloneChangedActiveEffects(activeEffects, m_ActiveEffects)
                : m_ActiveEffects;
            SortedDictionary<ulong, ulong> changedPeriods = periodsChanged
                ? CloneChangedMap(periods, m_Periods)
                : m_Periods;
            SortedDictionary<ulong, List<PortablePredictionRecord>> changedJournal = journalChanged
                ? CloneChangedJournal(journal, m_Journal)
                : m_Journal;
            SortedDictionary<ulong, ulong> changedRevisions = lastLifecycleRevisionsChanged
                ? CloneChangedMap(lastLifecycleRevisions, m_LastLifecycleRevisions)
                : m_LastLifecycleRevisions;
            string[] ownedTags = tagsChanged ? CollectOwnedTags(changedTags) : m_OwnedTags;
            return new GameplayEffectStateAggregate(
                changedTags,
                changedAttributes,
                changedActiveEffects,
                changedPeriods,
                changedJournal,
                changedRevisions,
                ownedTags,
                changeCursor);
        }

        GameplayEffectStateAggregate(
            SortedDictionary<string, string[]> tagSources,
            SortedDictionary<string, PortableAttributeState> attributes,
            List<PortableActiveEffectState> activeEffects,
            SortedDictionary<ulong, ulong> periods,
            SortedDictionary<ulong, List<PortablePredictionRecord>> journal,
            SortedDictionary<ulong, ulong> lastLifecycleRevisions,
            string[] ownedTags,
            ulong changeCursor)
        {
            m_TagSources = tagSources;
            m_Attributes = attributes;
            m_ActiveEffects = activeEffects;
            m_Periods = periods;
            m_Journal = journal;
            m_LastLifecycleRevisions = lastLifecycleRevisions;
            m_OwnedTags = ownedTags;
            ChangeCursor = changeCursor;
        }

        internal ulong ChangeCursor { get; }
        internal int ActiveEffectCount => m_ActiveEffects.Count;

        internal bool MatchesTagSources(IReadOnlyDictionary<string, string[]> sources)
        {
            if (sources == null || m_TagSources.Count != sources.Count)
                return false;
            foreach (KeyValuePair<string, string[]> pair in m_TagSources)
            {
                if (!sources.TryGetValue(pair.Key, out string[] values) ||
                    !EqualStrings(pair.Value, values))
                    return false;
            }
            return true;
        }

        internal bool MatchesPeriods(IReadOnlyDictionary<ulong, ulong> periods)
        {
            if (periods == null || m_Periods.Count != periods.Count)
                return false;
            foreach (KeyValuePair<ulong, ulong> pair in m_Periods)
            {
                if (!periods.TryGetValue(pair.Key, out ulong value) || value != pair.Value)
                    return false;
            }
            return true;
        }

        internal bool MatchesAttributes(IReadOnlyDictionary<string, PortableAttributeState> attributes)
        {
            if (attributes == null || m_Attributes.Count != attributes.Count)
                return false;
            foreach (KeyValuePair<string, PortableAttributeState> pair in m_Attributes)
            {
                if (!attributes.TryGetValue(pair.Key, out PortableAttributeState value) ||
                    !MatchesAttribute(pair.Value, value))
                    return false;
            }
            return true;
        }

        static bool MatchesAttribute(PortableAttributeState left, PortableAttributeState right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null)
                return false;
            return ReferenceEquals(left.Definition, right.Definition) &&
                left.BaseValue.Equals(right.BaseValue) &&
                left.CurrentValue.Equals(right.CurrentValue) &&
                left.Revision == right.Revision &&
                MatchesModifiers(left.Modifiers, right.Modifiers);
        }

        static bool MatchesModifiers(IReadOnlyList<PortableAttributeModifierState> left, IReadOnlyList<PortableAttributeModifierState> right)
        {
            if (left.Count != right.Count)
                return false;
            for (int i = 0; i < left.Count; i++)
            {
                PortableAttributeModifierState leftValue = left[i];
                PortableAttributeModifierState rightValue = right[i];
                if (leftValue.Handle != rightValue.Handle ||
                    leftValue.SourceEffectHandle != rightValue.SourceEffectHandle ||
                    leftValue.Operation != rightValue.Operation ||
                    !leftValue.Magnitude.Equals(rightValue.Magnitude) ||
                    leftValue.Priority != rightValue.Priority ||
                    leftValue.ClampBound != rightValue.ClampBound ||
                    !string.Equals(leftValue.LiveAttributeId, rightValue.LiveAttributeId, StringComparison.Ordinal) ||
                    !leftValue.LiveCoefficient.Equals(rightValue.LiveCoefficient) ||
                    !leftValue.LivePostAdd.Equals(rightValue.LivePostAdd) ||
                    leftValue.InsertionSequence != rightValue.InsertionSequence)
                    return false;
            }
            return true;
        }

        internal bool MatchesLastLifecycleRevisions(IReadOnlyDictionary<ulong, ulong> revisions)
        {
            if (revisions == null || m_LastLifecycleRevisions.Count != revisions.Count)
                return false;
            foreach (KeyValuePair<ulong, ulong> pair in m_LastLifecycleRevisions)
            {
                if (!revisions.TryGetValue(pair.Key, out ulong value) || value != pair.Value)
                    return false;
            }
            return true;
        }

        internal bool MatchesActiveEffects(IReadOnlyList<PortableActiveEffectState> activeEffects)
        {
            if (activeEffects == null || m_ActiveEffects.Count != activeEffects.Count)
                return false;
            for (int i = 0; i < m_ActiveEffects.Count; i++)
            {
                if (!MatchesActiveEffect(m_ActiveEffects[i], activeEffects[i]))
                    return false;
            }
            return true;
        }

        static bool MatchesActiveEffect(PortableActiveEffectState left, PortableActiveEffectState right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null)
                return false;
            return left.Handle == right.Handle &&
                left.InstanceId == right.InstanceId &&
                left.StartTick == right.StartTick &&
                left.EndTick == right.EndTick &&
                left.InsertionSequence == right.InsertionSequence &&
                left.StackCount == right.StackCount &&
                left.Inhibited == right.Inhibited &&
                left.LifecycleRevision == right.LifecycleRevision &&
                MatchesSpec(left.Spec, right.Spec);
        }

        internal bool MatchesJournal(IReadOnlyDictionary<ulong, List<PortablePredictionRecord>> journal)
        {
            if (journal == null || m_Journal.Count != journal.Count)
                return false;
            foreach (KeyValuePair<ulong, List<PortablePredictionRecord>> pair in m_Journal)
            {
                if (!journal.TryGetValue(pair.Key, out List<PortablePredictionRecord> records) ||
                    !MatchesRecords(pair.Value, records))
                    return false;
            }
            return true;
        }

        static bool MatchesRecords(IReadOnlyList<PortablePredictionRecord> left, IReadOnlyList<PortablePredictionRecord> right)
        {
            if (left.Count != right.Count)
                return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (!MatchesRecord(left[i], right[i]))
                    return false;
            }
            return true;
        }

        static bool MatchesRecord(PortablePredictionRecord left, PortablePredictionRecord right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null)
                return false;
            return left.Handle == right.Handle &&
                left.InstanceId == right.InstanceId &&
                left.CreatedActive == right.CreatedActive &&
                left.HasActiveBefore == right.HasActiveBefore &&
                left.Confirmed == right.Confirmed &&
                MatchesSnapshot(left.ActiveBefore, right.ActiveBefore) &&
                MatchesSpec(left.Spec, right.Spec) &&
                MatchesStrings(left.CueIds, right.CueIds) &&
                MatchesAttributes(left.Attributes, right.Attributes);
        }

        static bool MatchesSnapshot(GameplayEffectActiveControlSnapshot left, GameplayEffectActiveControlSnapshot right) =>
            left.InstanceId == right.InstanceId &&
            left.StartTick == right.StartTick &&
            left.EndTick == right.EndTick &&
            left.NextPeriodTick == right.NextPeriodTick &&
            left.StackCount == right.StackCount &&
            left.Inhibited == right.Inhibited &&
            left.LifecycleRevision == right.LifecycleRevision;

        static bool MatchesSpec(PortableEffectSpecState left, PortableEffectSpecState right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null)
                return false;
            return (ReferenceEquals(left.Definition, right.Definition) ||
                    string.Equals(left.Definition.Id, right.Definition.Id, StringComparison.Ordinal) &&
                    left.Definition.Revision == right.Definition.Revision) &&
                left.Context.SourceActorId.Equals(right.Context.SourceActorId) &&
                left.Context.TargetActorId.Equals(right.Context.TargetActorId) &&
                left.Context.SourceActionInstanceId == right.Context.SourceActionInstanceId &&
                left.Context.PredictionKey == right.Context.PredictionKey &&
                left.Context.GameplayResultId == right.Context.GameplayResultId &&
                left.Context.SourceTick == right.Context.SourceTick &&
                left.Context.ApplicationMode == right.Context.ApplicationMode &&
                MatchesValues(left.SetByCaller, right.SetByCaller) &&
                MatchesValues(left.SourceAttributes, right.SourceAttributes) &&
                MatchesValues(left.TargetAttributes, right.TargetAttributes) &&
                MatchesStrings(left.SourceTags, right.SourceTags) &&
                MatchesStrings(left.TargetTags, right.TargetTags) &&
                left.DurationTicks == right.DurationTicks &&
                left.PeriodTicks == right.PeriodTicks;
        }

        static bool MatchesValues(IReadOnlyDictionary<string, Float32Scalar> left, IReadOnlyDictionary<string, Float32Scalar> right)
        {
            if (left.Count != right.Count)
                return false;
            foreach (KeyValuePair<string, Float32Scalar> pair in left)
            {
                if (!right.TryGetValue(pair.Key, out Float32Scalar value) || !pair.Value.Equals(value))
                    return false;
            }
            return true;
        }

        static bool MatchesStrings(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (left.Count != right.Count)
                return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

        static bool MatchesAttributes(
            IReadOnlyDictionary<string, PortablePredictionAttributeSnapshot> left,
            IReadOnlyDictionary<string, PortablePredictionAttributeSnapshot> right)
        {
            if (left.Count != right.Count)
                return false;
            foreach (KeyValuePair<string, PortablePredictionAttributeSnapshot> pair in left)
            {
                if (!right.TryGetValue(pair.Key, out PortablePredictionAttributeSnapshot value))
                    return false;
                PortablePredictionAttributeSnapshot leftValue = pair.Value;
                if (!string.Equals(leftValue.AttributeId, value.AttributeId, StringComparison.Ordinal) ||
                    !leftValue.BaseValue.Equals(value.BaseValue) ||
                    !leftValue.CurrentValue.Equals(value.CurrentValue) ||
                    leftValue.BeforeRevision != value.BeforeRevision ||
                    leftValue.AfterRevision != value.AfterRevision)
                    return false;
            }
            return true;
        }

        static bool EqualStrings(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null || left.Count != right.Count)
                return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                    return false;
            }
            return true;
        }

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
                Float32GameplayEffectRuntimeCatalog.NormalizeAttribute(attributeId),
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

        internal static GameplayEffectStateAggregate CreateInitial(
            Float32GameplayEffectRuntimeCatalog catalog,
            Float32GameplayEffectExecutionScratch scratch)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            return new SimulationGameplayEffectState(catalog, null, scratch).Freeze();
        }

        internal void CopyTo(
            IDictionary<string, string[]> tagSources,
            IDictionary<string, PortableAttributeState> attributes,
            IList<PortableActiveEffectState> activeEffects,
            IDictionary<ulong, ulong> periods,
            IDictionary<ulong, List<PortablePredictionRecord>> journal,
            IDictionary<ulong, ulong> lastLifecycleRevisions)
        {
            CopyTagSourcesTo(tagSources);
            CopyAttributesTo(attributes);
            CopyActiveEffectsTo(activeEffects);
            CopyPeriodsTo(periods);
            CopyJournalTo(journal);
            CopyLastLifecycleRevisionsTo(lastLifecycleRevisions);
        }

        internal void CopyTagSourcesTo(IDictionary<string, string[]> tagSources)
        {
            foreach (KeyValuePair<string, string[]> pair in m_TagSources)
                tagSources.Add(pair.Key, pair.Value == null || pair.Value.Length == 0 ? Array.Empty<string>() : (string[])pair.Value.Clone());
        }

        internal void CopyAttributesTo(IDictionary<string, PortableAttributeState> attributes)
        {
            foreach (KeyValuePair<string, PortableAttributeState> pair in m_Attributes)
                attributes.Add(pair.Key, CloneAttribute(pair.Value));
        }

        internal void CopyActiveEffectsTo(IList<PortableActiveEffectState> activeEffects)
        {
            for (int i = 0; i < m_ActiveEffects.Count; i++)
                activeEffects.Add(CloneActive(m_ActiveEffects[i]));
        }

        internal void CopyPeriodsTo(IDictionary<ulong, ulong> periods)
        {
            foreach (KeyValuePair<ulong, ulong> pair in m_Periods)
                periods.Add(pair.Key, pair.Value);
        }

        internal void CopyJournalTo(IDictionary<ulong, List<PortablePredictionRecord>> journal)
        {
            foreach (KeyValuePair<ulong, List<PortablePredictionRecord>> pair in m_Journal)
            {
                var records = new List<PortablePredictionRecord>(pair.Value.Count);
                for (int i = 0; i < pair.Value.Count; i++)
                    records.Add(ClonePrediction(pair.Value[i]));
                journal.Add(pair.Key, records);
            }
        }

        internal void CopyLastLifecycleRevisionsTo(IDictionary<ulong, ulong> lastLifecycleRevisions)
        {
            foreach (KeyValuePair<ulong, ulong> pair in m_LastLifecycleRevisions)
                lastLifecycleRevisions.Add(pair.Key, pair.Value);
        }

        static SortedDictionary<string, string[]> CloneTagSources(IReadOnlyDictionary<string, string[]> source)
        {
            var result = new SortedDictionary<string, string[]>(StringComparer.Ordinal);
            if (source == null)
                return result;
            foreach (KeyValuePair<string, string[]> pair in source)
                result.Add(pair.Key, pair.Value == null || pair.Value.Length == 0 ? Array.Empty<string>() : (string[])pair.Value.Clone());
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

        static SortedDictionary<string, string[]> AdoptChangedTagSources(
            IReadOnlyDictionary<string, string[]> source,
            IReadOnlyDictionary<string, string[]> baseline)
        {
            var result = new SortedDictionary<string, string[]>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string[]> pair in source)
            {
                if (baseline.TryGetValue(pair.Key, out string[] baselineValue) &&
                    EqualStrings(pair.Value, baselineValue))
                    result.Add(pair.Key, baselineValue);
                else
                    result.Add(pair.Key, pair.Value == null || pair.Value.Length == 0 ? Array.Empty<string>() : pair.Value);
            }
            return result;
        }

        static SortedDictionary<string, PortableAttributeState> CloneChangedAttributes(
            IReadOnlyDictionary<string, PortableAttributeState> source,
            SortedDictionary<string, PortableAttributeState> baseline)
        {
            if (source.Count == baseline.Count)
            {
                bool matches = true;
                foreach (KeyValuePair<string, PortableAttributeState> pair in source)
                {
                    if (!baseline.TryGetValue(pair.Key, out PortableAttributeState baselineValue) ||
                        !MatchesAttribute(pair.Value, baselineValue))
                    {
                        matches = false;
                        break;
                    }
                }
                if (matches)
                    return baseline;
            }
            var result = new SortedDictionary<string, PortableAttributeState>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, PortableAttributeState> pair in source)
            {
                if (baseline.TryGetValue(pair.Key, out PortableAttributeState baselineValue) &&
                    MatchesAttribute(pair.Value, baselineValue))
                    result.Add(pair.Key, baselineValue);
                else
                    result.Add(pair.Key, CloneAttribute(pair.Value));
            }
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
            result.Modifiers.Capacity = source.Modifiers.Count;
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

        static List<PortableActiveEffectState> CloneChangedActiveEffects(
            IReadOnlyList<PortableActiveEffectState> source,
            IReadOnlyList<PortableActiveEffectState> baseline)
        {
            var result = new List<PortableActiveEffectState>(source?.Count ?? 0);
            if (source == null)
                return result;
            for (int i = 0; i < source.Count; i++)
                result.Add(FindMatchingActive(baseline, source[i]) ?? CloneActive(source[i]));
            return result;
        }

        static PortableActiveEffectState FindMatchingActive(
            IReadOnlyList<PortableActiveEffectState> baseline,
            PortableActiveEffectState source)
        {
            for (int i = 0; i < baseline.Count; i++)
            {
                if (baseline[i].Handle == source.Handle &&
                    baseline[i].InstanceId == source.InstanceId &&
                    MatchesActiveEffect(baseline[i], source))
                    return baseline[i];
            }
            return null;
        }

        static PortableActiveEffectState CloneActive(PortableActiveEffectState source)
        {
            return new PortableActiveEffectState
            {
                Handle = source.Handle,
                InstanceId = source.InstanceId,
                Spec = source.Spec,
                StartTick = source.StartTick,
                EndTick = source.EndTick,
                InsertionSequence = source.InsertionSequence,
                StackCount = source.StackCount,
                Inhibited = source.Inhibited,
                LifecycleRevision = source.LifecycleRevision
            };
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

        static SortedDictionary<ulong, List<PortablePredictionRecord>> CloneChangedJournal(
            IReadOnlyDictionary<ulong, List<PortablePredictionRecord>> source,
            IReadOnlyDictionary<ulong, List<PortablePredictionRecord>> baseline)
        {
            var result = new SortedDictionary<ulong, List<PortablePredictionRecord>>();
            foreach (KeyValuePair<ulong, List<PortablePredictionRecord>> pair in source)
            {
                if (!baseline.TryGetValue(pair.Key, out List<PortablePredictionRecord> baselineRecords) ||
                    pair.Value.Count != baselineRecords.Count)
                {
                    var changedRecords = new List<PortablePredictionRecord>(pair.Value.Count);
                    for (int i = 0; i < pair.Value.Count; i++)
                        changedRecords.Add(ClonePrediction(pair.Value[i]));
                    result.Add(pair.Key, changedRecords);
                    continue;
                }
                bool matches = MatchesRecords(pair.Value, baselineRecords);
                if (matches)
                {
                    result.Add(pair.Key, baselineRecords);
                    continue;
                }
                var records = new List<PortablePredictionRecord>(pair.Value.Count);
                for (int i = 0; i < pair.Value.Count; i++)
                    records.Add(MatchesRecord(pair.Value[i], baselineRecords[i])
                        ? baselineRecords[i]
                        : ClonePrediction(pair.Value[i]));
                result.Add(pair.Key, records);
            }
            return result;
        }

        static PortablePredictionRecord ClonePrediction(PortablePredictionRecord source)
        {
            var result = new PortablePredictionRecord
            {
                Spec = source.Spec,
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

        static SortedDictionary<ulong, ulong> CloneChangedMap(
            IReadOnlyDictionary<ulong, ulong> source,
            SortedDictionary<ulong, ulong> baseline)
        {
            if (source == null)
                return baseline;
            if (source.Count == baseline.Count)
            {
                bool matches = true;
                foreach (KeyValuePair<ulong, ulong> pair in source)
                {
                    if (!baseline.TryGetValue(pair.Key, out ulong value) || value != pair.Value)
                    {
                        matches = false;
                        break;
                    }
                }
                if (matches)
                    return baseline;
            }
            return CloneMap(source);
        }

        static string[] CollectOwnedTags(SortedDictionary<string, string[]> sources)
        {
            var tags = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string[]> sourcePair in sources)
            {
                string[] source = sourcePair.Value;
                for (int i = 0; i < source.Length; i++)
                    tags.Add(source[i]);
            }
            var result = new string[tags.Count];
            tags.CopyTo(result);
            Array.Sort(result, StringComparer.Ordinal);
            return result;
        }

        static void Copy<T>(IReadOnlyList<T> source, IList<T> destination)
        {
            for (int i = 0; i < source.Count; i++)
                destination.Add(source[i]);
        }
    }

    internal sealed class SimulationGameplayEffectState
    {
        static readonly Comparison<string> s_CompareOwnedTags = string.CompareOrdinal;
        const uint TagsMagic = 0x53474154;
        const uint AttributesMagic = 0x52545441;
        const uint ActiveMagic = 0x56544341;
        const uint PeriodsMagic = 0x44524550;
        const uint JournalMagic = 0x52554F4A;
        const int StateVersion = 1;

        readonly Float32GameplayEffectRuntimeCatalog m_Catalog;
        readonly Float32GameplayEffectExecutionScratch m_Scratch;
        readonly SortedDictionary<string, string[]> m_TagSources = new SortedDictionary<string, string[]>(StringComparer.Ordinal);
        readonly SortedDictionary<string, PortableAttributeState> m_Attributes = new SortedDictionary<string, PortableAttributeState>(StringComparer.Ordinal);
        readonly List<PortableActiveEffectState> m_ActiveEffects = new List<PortableActiveEffectState>();
        readonly SortedDictionary<ulong, ulong> m_Periods = new SortedDictionary<ulong, ulong>();
        readonly SortedDictionary<ulong, List<PortablePredictionRecord>> m_Journal = new SortedDictionary<ulong, List<PortablePredictionRecord>>();
        readonly SortedDictionary<ulong, ulong> m_LastLifecycleRevisions = new SortedDictionary<ulong, ulong>();
        GameplayEffectStateAggregate m_Baseline;
        string[] m_OwnedTagsSnapshot;
        ulong m_ChangeCursor;
        bool m_TagsDirty;
        bool m_AttributesDirty;
        bool m_ActiveEffectsDirty;
        bool m_PeriodsDirty;
        bool m_JournalDirty;
        bool m_ChangeCursorDirty;
        bool m_LastLifecycleRevisionsDirty;

        public SimulationGameplayEffectState(
            Float32GameplayEffectRuntimeCatalog catalog,
            GameplayEffectStateAggregate aggregate,
            Float32GameplayEffectExecutionScratch scratch)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            m_Scratch = scratch ?? throw new ArgumentNullException(nameof(scratch));
            m_Catalog = catalog;
            if (aggregate == null)
                Initialize();
            else
                Restore(aggregate);
            ValidateRuntimeClosure();
        }

        public Float32GameplayEffectRuntimeCatalog Catalog => m_Catalog;
        public IReadOnlyList<PortableActiveEffectState> ActiveEffects => m_ActiveEffects;
        public bool TryGetJournalRecords(ulong predictionKey, out IReadOnlyList<PortablePredictionRecord> records)
        {
            if (m_Journal.TryGetValue(predictionKey, out List<PortablePredictionRecord> values))
            {
                records = values;
                return true;
            }
            records = null;
            return false;
        }

        public void AddJournalRecord(PortablePredictionRecord record)
        {
            ulong key = record.Spec.Context.PredictionKey;
            if (!m_Journal.TryGetValue(key, out List<PortablePredictionRecord> records))
            {
                records = new List<PortablePredictionRecord>();
                m_Journal.Add(key, records);
            }
            records.Add(record);
            RefreshJournalDirty();
        }

        public void RemoveJournalRecords(ulong predictionKey)
        {
            m_Journal.Remove(predictionKey);
            RefreshJournalDirty();
        }

        public void CopyJournalKeys(List<ulong> keys)
        {
            keys.Clear();
            foreach (ulong key in m_Journal.Keys)
                keys.Add(key);
        }
        public bool TryGetLastLifecycleRevision(ulong instanceId, out ulong revision) =>
            m_LastLifecycleRevisions.TryGetValue(instanceId, out revision);

        public void SetLastLifecycleRevision(ulong instanceId, ulong revision)
        {
            if (m_LastLifecycleRevisions.TryGetValue(instanceId, out ulong current) && current == revision)
                return;
            m_LastLifecycleRevisions[instanceId] = revision;
            m_LastLifecycleRevisionsDirty = m_Baseline == null ||
                !m_Baseline.MatchesLastLifecycleRevisions(m_LastLifecycleRevisions);
        }
        public ulong ChangeCursor
        {
            get => m_ChangeCursor;
            set
            {
                if (m_ChangeCursor == value)
                    return;
                m_ChangeCursor = value;
                m_ChangeCursorDirty = m_Baseline == null || m_Baseline.ChangeCursor != value;
            }
        }

        public bool HasChanges =>
            m_TagsDirty ||
            m_AttributesDirty ||
            m_ActiveEffectsDirty ||
            m_PeriodsDirty ||
            m_JournalDirty ||
            m_ChangeCursorDirty ||
            m_LastLifecycleRevisionsDirty;

        public IReadOnlyList<string> OwnedTagsSnapshot => m_OwnedTagsSnapshot ??= BuildOwnedTagsSnapshot();

        internal string[] OwnedTagsSnapshotArray => m_OwnedTagsSnapshot ??= BuildOwnedTagsSnapshot();

        string[] BuildOwnedTagsSnapshot()
        {
            List<string> values = m_Scratch.OwnedTags;
            values.Clear();
            foreach (KeyValuePair<string, string[]> sourcePair in m_TagSources)
            {
                string[] source = sourcePair.Value;
                for (int i = 0; i < source.Length; i++)
                    values.Add(source[i]);
            }
            values.Sort(s_CompareOwnedTags);
            string[] snapshot = values.Count == 0 ? Array.Empty<string>() : values.ToArray();
            values.Clear();
            m_OwnedTagsSnapshot = snapshot;
            return snapshot;
        }

        public bool HasTag(string tagId)
        {
            string query = Float32GameplayEffectRuntimeCatalog.NormalizeTag(tagId);
            IReadOnlyList<string> owned = OwnedTagsSnapshot;
            for (int i = 0; i < owned.Count; i++)
            {
                if (m_Catalog.IsTagOrParent(owned[i], query))
                    return true;
            }
            return false;
        }

        public bool Matches(PortableTagQuery query)
        {
            return m_Catalog.Matches(query, OwnedTagsSnapshot);
        }

        public void SetTagSource(string sourceId, IEnumerable<string> tags)
        {
            string source = SimulationIdentity.Require(sourceId, nameof(sourceId));
            m_TagSources.TryGetValue(source, out string[] current);
            string[] values = CanonicalTags(tags, current);
            if (values.Length == 0)
            {
                if (m_TagSources.Remove(source))
                {
                    m_OwnedTagsSnapshot = null;
                    RefreshTagsDirty();
                }
                return;
            }
            if (ReferenceEquals(current, values))
                return;
            m_TagSources[source] = values;
            m_OwnedTagsSnapshot = null;
            RefreshTagsDirty();
        }

        public void RemoveTagSource(string sourceId)
        {
            if (m_TagSources.Remove(sourceId))
            {
                m_OwnedTagsSnapshot = null;
                RefreshTagsDirty();
            }
        }

        public bool TryGetAttribute(string attributeId, out PortableAttributeState value)
        {
            return m_Attributes.TryGetValue(Float32GameplayEffectRuntimeCatalog.NormalizeAttribute(attributeId), out value);
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
            RefreshAttributesDirty();
            return changes;
        }

        public IReadOnlyList<PortableAttributeChange> AddModifier(string attributeId, PortableAttributeModifierState modifier)
        {
            if (modifier == null || modifier.Handle == 0 || modifier.SourceEffectHandle == 0)
                throw new ArgumentException("Gameplay Attribute modifier identity is incomplete.", nameof(modifier));
            PortableAttributeState attribute = RequireAttribute(attributeId);
            foreach (KeyValuePair<string, PortableAttributeState> attributePair in m_Attributes)
            {
                PortableAttributeState existingAttribute = attributePair.Value;
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
            RefreshAttributesDirty();
            return changes;
        }

        public IReadOnlyList<PortableAttributeChange> RemoveModifiersByEffect(ulong sourceEffectHandle)
        {
            List<PortableAttributeChange> changes = m_Scratch.AttributeChanges;
            changes.Clear();
            bool removed = false;
            foreach (KeyValuePair<string, PortableAttributeState> attributePair in m_Attributes)
            {
                PortableAttributeState attribute = attributePair.Value;
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
                RefreshAttributesDirty();
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
            List<PortableAttributeChange> result = m_Scratch.AttributeChanges;
            result.Clear();
            result.Add(new PortableAttributeChange(attribute.Definition.Id, before[attribute.Definition.Id].Base, baseValue, before[attribute.Definition.Id].Current, currentValue, revision, causeHandle));
            result.AddRange(RecalculateAll(before, causeHandle, attribute.Definition.Id));
            changes = result;
            RefreshAttributesDirty();
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
            List<PortableAttributeChange> result = m_Scratch.AttributeChanges;
            result.Clear();
            result.Add(new PortableAttributeChange(attribute.Definition.Id, before[attribute.Definition.Id].Base, attribute.BaseValue, before[attribute.Definition.Id].Current, attribute.CurrentValue, attribute.Revision, causeHandle));
            result.AddRange(RecalculateAll(before, causeHandle, attribute.Definition.Id));
            changes = result;
            RefreshAttributesDirty();
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
            RefreshActiveEffectsDirty();
        }

        public void RemoveActive(PortableActiveEffectState active)
        {
            if (active == null || !m_ActiveEffects.Remove(active))
                throw new InvalidOperationException("Active Gameplay Effect removal target is missing.");
            RefreshActiveEffectsDirty();
            if (m_Periods.Remove(active.InstanceId))
                RefreshPeriodsDirty();
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
                    RefreshPeriodsDirty();
                return;
            }
            if (m_Periods.TryGetValue(instanceId, out ulong current) && current == tick)
                return;
            m_Periods[instanceId] = tick;
            RefreshPeriodsDirty();
        }

        public void RefreshActiveEffectsDirty()
        {
            m_ActiveEffectsDirty = m_Baseline == null || !m_Baseline.MatchesActiveEffects(m_ActiveEffects);
        }

        public void RefreshJournalDirty()
        {
            m_JournalDirty = m_Baseline == null || !m_Baseline.MatchesJournal(m_Journal);
        }

        public GameplayEffectStateAggregate Freeze()
        {
            if (m_Baseline != null &&
                !m_TagsDirty &&
                !m_AttributesDirty &&
                !m_ActiveEffectsDirty &&
                !m_PeriodsDirty &&
                !m_JournalDirty &&
                !m_ChangeCursorDirty &&
                !m_LastLifecycleRevisionsDirty)
            {
                return m_Baseline;
            }
            return m_Baseline.CreateChangedFrom(
                m_TagSources,
                m_Attributes,
                m_ActiveEffects,
                m_Periods,
                m_Journal,
                m_LastLifecycleRevisions,
                m_TagsDirty,
                m_AttributesDirty,
                m_ActiveEffectsDirty,
                m_PeriodsDirty,
                m_JournalDirty,
                m_LastLifecycleRevisionsDirty,
                m_ChangeCursor);
        }

        internal bool IsCommitted(GameplayEffectStateAggregate aggregate)
        {
            return aggregate != null && ReferenceEquals(m_Baseline, aggregate) && !HasChanges;
        }

        internal GameplayEffectStateAggregate Commit()
        {
            GameplayEffectStateAggregate aggregate = Freeze();
            m_Baseline = aggregate;
            m_ChangeCursor = aggregate.ChangeCursor;
            ClearDirty();
            return aggregate;
        }

        public void Restore(GameplayEffectStateAggregate aggregate)
        {
            if (aggregate == null)
                throw new ArgumentNullException(nameof(aggregate));
            bool tagsMatch = aggregate.MatchesTagSources(m_TagSources);
            bool attributesMatch = aggregate.MatchesAttributes(m_Attributes);
            bool activeEffectsMatch = aggregate.MatchesActiveEffects(m_ActiveEffects);
            bool periodsMatch = aggregate.MatchesPeriods(m_Periods);
            bool journalMatch = aggregate.MatchesJournal(m_Journal);
            bool revisionsMatch = aggregate.MatchesLastLifecycleRevisions(m_LastLifecycleRevisions);
            if (!tagsMatch)
            {
                m_TagSources.Clear();
                aggregate.CopyTagSourcesTo(m_TagSources);
                m_OwnedTagsSnapshot = null;
            }
            if (!attributesMatch)
            {
                m_Attributes.Clear();
                aggregate.CopyAttributesTo(m_Attributes);
            }
            if (!activeEffectsMatch)
            {
                m_ActiveEffects.Clear();
                aggregate.CopyActiveEffectsTo(m_ActiveEffects);
            }
            if (!periodsMatch)
            {
                m_Periods.Clear();
                aggregate.CopyPeriodsTo(m_Periods);
            }
            if (!journalMatch)
            {
                m_Journal.Clear();
                aggregate.CopyJournalTo(m_Journal);
            }
            if (!revisionsMatch)
            {
                m_LastLifecycleRevisions.Clear();
                aggregate.CopyLastLifecycleRevisionsTo(m_LastLifecycleRevisions);
            }
            m_Baseline = aggregate;
            m_ChangeCursor = aggregate.ChangeCursor;
            ClearDirty();
            ValidateRuntimeClosure();
        }

        void Initialize()
        {
            ClearCollections();
            m_Baseline = null;
            string[] initialTags = CanonicalTags(m_Catalog.InitialTags);
            if (initialTags.Length > 0)
                m_TagSources.Add("initial", initialTags);
            foreach (KeyValuePair<string, PortableAttributeDefinition> pair in m_Catalog.Attributes)
            {
                m_Attributes.Add(pair.Key, new PortableAttributeState
                {
                    Definition = pair.Value,
                    BaseValue = pair.Value.InitialBase,
                    CurrentValue = pair.Value.InitialBase,
                    Revision = 1
                });
            }
            Dictionary<string, Float32Scalar> cache = m_Scratch.AttributeValues;
            HashSet<string> stack = m_Scratch.AttributeStack;
            cache.Clear();
            stack.Clear();
            foreach (KeyValuePair<string, PortableAttributeState> pair in m_Attributes)
                CalculateCurrent(pair.Key, null, cache, stack);
            foreach (KeyValuePair<string, PortableAttributeState> pair in m_Attributes)
                pair.Value.Revision = 1;
            m_ChangeCursor = 0;
            ClearDirty();
        }

        void ClearCollections()
        {
            m_TagSources.Clear();
            m_OwnedTagsSnapshot = null;
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
            m_LastLifecycleRevisionsDirty = false;
        }

        void RefreshTagsDirty()
        {
            m_TagsDirty = m_Baseline == null || !m_Baseline.MatchesTagSources(m_TagSources);
        }

        void RefreshPeriodsDirty()
        {
            m_PeriodsDirty = m_Baseline == null || !m_Baseline.MatchesPeriods(m_Periods);
        }

        void RefreshAttributesDirty()
        {
            m_AttributesDirty = m_Baseline == null || !m_Baseline.MatchesAttributes(m_Attributes);
        }

        Dictionary<string, PortableAttributeBefore> CaptureAttributeBefore()
        {
            Dictionary<string, PortableAttributeBefore> result = m_Scratch.AttributeBefore;
            result.Clear();
            foreach (KeyValuePair<string, PortableAttributeState> pair in m_Attributes)
                result.Add(pair.Key, new PortableAttributeBefore(pair.Value.BaseValue, pair.Value.CurrentValue, pair.Value.Revision));
            return result;
        }

        IReadOnlyList<PortableAttributeChange> RecalculateAll(Dictionary<string, PortableAttributeBefore> before, ulong causeHandle, string excludedAttribute)
        {
            Dictionary<string, Float32Scalar> cache = m_Scratch.AttributeValues;
            HashSet<string> stack = m_Scratch.AttributeStack;
            cache.Clear();
            stack.Clear();
            foreach (KeyValuePair<string, PortableAttributeState> pair in m_Attributes)
                CalculateCurrent(pair.Key, excludedAttribute, cache, stack);
            List<PortableAttributeChange> changes = m_Scratch.RecalculatedAttributeChanges;
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
            HashSet<ulong> handles = m_Scratch.ActiveHandles;
            HashSet<ulong> instances = m_Scratch.ActiveInstances;
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
            foreach (KeyValuePair<string, PortableAttributeState> attributePair in m_Attributes)
            {
                PortableAttributeState attribute = attributePair.Value;
                for (int i = 0; i < attribute.Modifiers.Count; i++)
                {
                    if (FindActiveByHandle(attribute.Modifiers[i].SourceEffectHandle) == null)
                        throw new InvalidDataException($"Gameplay Attribute modifier '{attribute.Modifiers[i].Handle}' references unknown Active Effect.");
                }
            }
        }

        string[] CanonicalTags(IEnumerable<string> tags, string[] current = null)
        {
            List<string> values = m_Scratch.CanonicalTags;
            values.Clear();
            try
            {
                if (tags != null)
                {
                    foreach (string tag in tags)
                        values.Add(Float32GameplayEffectRuntimeCatalog.NormalizeTag(tag));
                }
                values.Sort(StringComparer.Ordinal);
                for (int i = values.Count - 1; i > 0; i--)
                {
                    if (string.Equals(values[i - 1], values[i], StringComparison.Ordinal))
                        values.RemoveAt(i);
                }
                return EqualStrings(current, values) ? current : values.ToArray();
            }
            finally
            {
                values.Clear();
            }
        }

        static bool EqualStrings(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null || left.Count != right.Count)
                return false;
            for (int i = 0; i < left.Count; i++)
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
