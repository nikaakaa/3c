using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ThirdPersonSimulation
{
    internal enum PortableEffectDurationPolicy : byte
    {
        Instant = 0,
        Duration = 1,
        Infinite = 2
    }

    internal enum PortableMagnitudeSource : byte
    {
        Constant = 0,
        SetByCaller = 1,
        SourceAttributeSnapshot = 2,
        TargetAttributeSnapshot = 3,
        TargetAttributeLive = 4
    }

    internal enum PortableEffectStackingPolicy : byte
    {
        Independent = 0,
        AggregateBySource = 1,
        AggregateByTarget = 2
    }

    internal enum PortableEffectDurationUpdatePolicy : byte
    {
        Keep = 0,
        Refresh = 1,
        Extend = 2
    }

    internal enum PortableEffectPeriodUpdatePolicy : byte
    {
        Keep = 0,
        Reset = 1
    }

    internal enum PortableEffectOverflowPolicy : byte
    {
        Reject = 0,
        ReplaceOldest = 1,
        ApplyOverflowEffects = 2
    }

    internal enum PortableModifierApplication : byte
    {
        BaseValue = 0,
        CurrentValue = 1
    }

    internal enum PortableModifierOperation : byte
    {
        Additive = 0,
        Multiplicative = 1,
        Override = 2,
        Clamp = 3
    }

    internal enum PortableClampBound : byte
    {
        Minimum = 0,
        Maximum = 1
    }

    internal enum PortableRequirementPhase : byte
    {
        Application = 0,
        Ongoing = 1,
        Removal = 2
    }

    internal enum PortableAttributeSource : byte
    {
        SourceSnapshot = 0,
        Target = 1
    }

    internal enum PortableAttributeComparison : byte
    {
        Less = 0,
        LessOrEqual = 1,
        Equal = 2,
        GreaterOrEqual = 3,
        Greater = 4,
        NotEqual = 5
    }

    internal enum PortableAdditionalEffectTrigger : byte
    {
        Applied = 0,
        Period = 1,
        Removed = 2,
        Overflow = 3
    }

    internal enum PortableAdditionalParameterSource : byte
    {
        ParentSetByCaller = 0,
        Constant = 1
    }

    internal enum PortableCueTrigger : byte
    {
        OnActive = 0,
        Executed = 1,
        WhileActive = 2,
        Removed = 3,
        Expired = 4
    }

    internal readonly struct PortableMagnitude
    {
        public PortableMagnitude(
            PortableMagnitudeSource source,
            Float32Scalar constant,
            string setByCallerParameterId,
            string attributeId,
            Float32Scalar coefficient,
            Float32Scalar postAdd)
        {
            Source = source;
            Constant = constant;
            SetByCallerParameterId = setByCallerParameterId ?? string.Empty;
            AttributeId = attributeId ?? string.Empty;
            Coefficient = coefficient;
            PostAdd = postAdd;
        }

        public PortableMagnitudeSource Source { get; }
        public Float32Scalar Constant { get; }
        public string SetByCallerParameterId { get; }
        public string AttributeId { get; }
        public Float32Scalar Coefficient { get; }
        public Float32Scalar PostAdd { get; }
    }

    internal sealed class PortableTagQuery
    {
        public PortableTagQuery(IEnumerable<string> all, IEnumerable<string> any, IEnumerable<string> none)
        {
            All = Canonical(all);
            Any = Canonical(any);
            None = Canonical(none);
        }

        public string[] All { get; }
        public string[] Any { get; }
        public string[] None { get; }
        public bool IsEmpty => All.Length == 0 && Any.Length == 0 && None.Length == 0;

        static string[] Canonical(IEnumerable<string> values)
        {
            var result = values == null
                ? new List<string>()
                : values.Select(SimulationGameplayEffectProgram.NormalizeTag).ToList();
            result.Sort(StringComparer.Ordinal);
            for (int i = 0; i < result.Count; i++)
            {
                if (i > 0 && string.Equals(result[i - 1], result[i], StringComparison.Ordinal))
                    throw new InvalidDataException($"Gameplay Tag query contains duplicate '{result[i]}'.");
            }
            return result.ToArray();
        }
    }

    internal readonly struct PortableAttributeBound
    {
        public PortableAttributeBound(bool enabled, bool fromAttribute, Float32Scalar constant, string attributeId)
        {
            Enabled = enabled;
            FromAttribute = fromAttribute;
            Constant = constant;
            AttributeId = attributeId ?? string.Empty;
        }

        public bool Enabled { get; }
        public bool FromAttribute { get; }
        public Float32Scalar Constant { get; }
        public string AttributeId { get; }
    }

    internal sealed class PortableAttributeDefinition
    {
        public PortableAttributeDefinition(string id, Float32Scalar initialBase, PortableAttributeBound minimum, PortableAttributeBound maximum)
        {
            Id = SimulationGameplayEffectProgram.NormalizeAttribute(id);
            InitialBase = initialBase;
            Minimum = minimum;
            Maximum = maximum;
        }

        public string Id { get; }
        public Float32Scalar InitialBase { get; }
        public PortableAttributeBound Minimum { get; }
        public PortableAttributeBound Maximum { get; }
    }

    internal abstract class PortableEffectComponent
    {
    }

    internal sealed class PortableModifierComponent : PortableEffectComponent
    {
        public PortableModifierComponent(string attributeId, PortableModifierApplication application, PortableModifierOperation operation, PortableMagnitude magnitude, int priority, PortableClampBound clampBound, bool scaleWithStack)
        {
            AttributeId = SimulationGameplayEffectProgram.NormalizeAttribute(attributeId);
            Application = application;
            Operation = operation;
            Magnitude = magnitude;
            Priority = priority;
            ClampBound = clampBound;
            ScaleWithStack = scaleWithStack;
        }

        public string AttributeId { get; }
        public PortableModifierApplication Application { get; }
        public PortableModifierOperation Operation { get; }
        public PortableMagnitude Magnitude { get; }
        public int Priority { get; }
        public PortableClampBound ClampBound { get; }
        public bool ScaleWithStack { get; }
    }

    internal sealed class PortableGrantedTagsComponent : PortableEffectComponent
    {
        public PortableGrantedTagsComponent(IEnumerable<string> tags)
        {
            Tags = tags.Select(SimulationGameplayEffectProgram.NormalizeTag).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        public string[] Tags { get; }
    }

    internal sealed class PortableTagRequirementsComponent : PortableEffectComponent
    {
        public PortableTagRequirementsComponent(PortableRequirementPhase phase, PortableTagQuery source, PortableTagQuery target)
        {
            Phase = phase;
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public PortableRequirementPhase Phase { get; }
        public PortableTagQuery Source { get; }
        public PortableTagQuery Target { get; }
    }

    internal sealed class PortableAttributeRequirementsComponent : PortableEffectComponent
    {
        public PortableAttributeRequirementsComponent(PortableRequirementPhase phase, PortableAttributeSource source, string attributeId, PortableAttributeComparison comparison, PortableMagnitude threshold)
        {
            Phase = phase;
            Source = source;
            AttributeId = SimulationGameplayEffectProgram.NormalizeAttribute(attributeId);
            Comparison = comparison;
            Threshold = threshold;
        }

        public PortableRequirementPhase Phase { get; }
        public PortableAttributeSource Source { get; }
        public string AttributeId { get; }
        public PortableAttributeComparison Comparison { get; }
        public PortableMagnitude Threshold { get; }
    }

    internal readonly struct PortableExecutionMutation
    {
        public PortableExecutionMutation(string attributeId, PortableModifierOperation operation, PortableMagnitude magnitude, PortableClampBound clampBound)
        {
            AttributeId = SimulationGameplayEffectProgram.NormalizeAttribute(attributeId);
            Operation = operation;
            Magnitude = magnitude;
            ClampBound = clampBound;
        }

        public string AttributeId { get; }
        public PortableModifierOperation Operation { get; }
        public PortableMagnitude Magnitude { get; }
        public PortableClampBound ClampBound { get; }
    }

    internal sealed class PortableExecutionComponent : PortableEffectComponent
    {
        public PortableExecutionComponent(PortableExecutionMutation[] mutations)
        {
            Mutations = mutations ?? Array.Empty<PortableExecutionMutation>();
        }

        public PortableExecutionMutation[] Mutations { get; }
    }

    internal readonly struct PortableAdditionalParameterBinding
    {
        public PortableAdditionalParameterBinding(string childParameterId, PortableAdditionalParameterSource source, string parentParameterId, Float32Scalar constant)
        {
            ChildParameterId = SimulationIdentity.Require(childParameterId, nameof(childParameterId));
            Source = source;
            ParentParameterId = parentParameterId ?? string.Empty;
            Constant = constant;
        }

        public string ChildParameterId { get; }
        public PortableAdditionalParameterSource Source { get; }
        public string ParentParameterId { get; }
        public Float32Scalar Constant { get; }
    }

    internal readonly struct PortableAdditionalEffect
    {
        public PortableAdditionalEffect(PortableAdditionalEffectTrigger trigger, string effectId, PortableAdditionalParameterBinding[] bindings)
        {
            Trigger = trigger;
            EffectId = SimulationGameplayEffectProgram.NormalizeEffect(effectId);
            Bindings = bindings ?? Array.Empty<PortableAdditionalParameterBinding>();
        }

        public PortableAdditionalEffectTrigger Trigger { get; }
        public string EffectId { get; }
        public PortableAdditionalParameterBinding[] Bindings { get; }
    }

    internal sealed class PortableAdditionalEffectsComponent : PortableEffectComponent
    {
        public PortableAdditionalEffectsComponent(PortableAdditionalEffect[] effects)
        {
            Effects = effects ?? Array.Empty<PortableAdditionalEffect>();
        }

        public PortableAdditionalEffect[] Effects { get; }
    }

    internal sealed class PortableCueComponent : PortableEffectComponent
    {
        public PortableCueComponent(string cueId, PortableCueTrigger trigger)
        {
            CueId = SimulationIdentity.Require(cueId, nameof(cueId));
            Trigger = trigger;
        }

        public string CueId { get; }
        public PortableCueTrigger Trigger { get; }
    }

    internal sealed class PortableEffectDefinition
    {
        public PortableEffectDefinition(
            string id,
            uint revision,
            string[] effectTags,
            PortableEffectDurationPolicy durationPolicy,
            PortableMagnitude durationMagnitude,
            bool hasPeriod,
            PortableMagnitude periodMagnitude,
            bool executeOnApplication,
            PortableEffectStackingPolicy stackingPolicy,
            int maxStacks,
            PortableEffectDurationUpdatePolicy durationUpdate,
            PortableEffectPeriodUpdatePolicy periodUpdate,
            PortableEffectOverflowPolicy overflowPolicy,
            string[] setByCallerParameters,
            PortableEffectComponent[] components)
        {
            Id = SimulationGameplayEffectProgram.NormalizeEffect(id);
            Revision = revision;
            EffectTags = effectTags ?? Array.Empty<string>();
            DurationPolicy = durationPolicy;
            DurationMagnitude = durationMagnitude;
            HasPeriod = hasPeriod;
            PeriodMagnitude = periodMagnitude;
            ExecuteOnApplication = executeOnApplication;
            StackingPolicy = stackingPolicy;
            MaxStacks = maxStacks;
            DurationUpdate = durationUpdate;
            PeriodUpdate = periodUpdate;
            OverflowPolicy = overflowPolicy;
            SetByCallerParameters = setByCallerParameters ?? Array.Empty<string>();
            Components = components ?? Array.Empty<PortableEffectComponent>();
        }

        public string Id { get; }
        public uint Revision { get; }
        public string[] EffectTags { get; }
        public PortableEffectDurationPolicy DurationPolicy { get; }
        public PortableMagnitude DurationMagnitude { get; }
        public bool HasPeriod { get; }
        public PortableMagnitude PeriodMagnitude { get; }
        public bool ExecuteOnApplication { get; }
        public PortableEffectStackingPolicy StackingPolicy { get; }
        public int MaxStacks { get; }
        public PortableEffectDurationUpdatePolicy DurationUpdate { get; }
        public PortableEffectPeriodUpdatePolicy PeriodUpdate { get; }
        public PortableEffectOverflowPolicy OverflowPolicy { get; }
        public string[] SetByCallerParameters { get; }
        public PortableEffectComponent[] Components { get; }
    }

    internal sealed class SimulationGameplayEffectProgram
    {
        readonly CharacterSimulationProgram m_Program;
        readonly Dictionary<string, string> m_TagParents = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly HashSet<string> m_InitialTags = new HashSet<string>(StringComparer.Ordinal);
        readonly Dictionary<string, PortableAttributeDefinition> m_Attributes = new Dictionary<string, PortableAttributeDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, PortableEffectDefinition> m_Effects = new Dictionary<string, PortableEffectDefinition>(StringComparer.Ordinal);

        public SimulationGameplayEffectProgram(CharacterSimulationProgram program)
        {
            m_Program = program ?? throw new ArgumentNullException(nameof(program));
            ReadTags();
            ReadAttributes();
            ReadEffects();
            ValidateClosure();
        }

        public IReadOnlyDictionary<string, string> TagParents => m_TagParents;
        public IReadOnlyCollection<string> InitialTags => m_InitialTags;
        public IReadOnlyDictionary<string, PortableAttributeDefinition> Attributes => m_Attributes;
        public IReadOnlyDictionary<string, PortableEffectDefinition> Effects => m_Effects;

        public PortableEffectDefinition RequireEffect(string effectId)
        {
            string identity = NormalizeEffect(effectId);
            if (!m_Effects.TryGetValue(identity, out PortableEffectDefinition definition))
                throw new KeyNotFoundException($"Gameplay Effect catalog does not contain '{identity}'.");
            return definition;
        }

        public bool IsTagOrParent(string ownedTag, string queryTag)
        {
            string current = NormalizeTag(ownedTag);
            string query = NormalizeTag(queryTag);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            while (!string.IsNullOrEmpty(current))
            {
                if (!visited.Add(current))
                    throw new InvalidDataException($"Gameplay Tag parent cycle reached '{current}'.");
                if (string.Equals(current, query, StringComparison.Ordinal))
                    return true;
                current = m_TagParents.TryGetValue(current, out string parent) ? parent : string.Empty;
            }
            return false;
        }

        public bool Matches(PortableTagQuery query, IEnumerable<string> tags)
        {
            var owned = tags == null ? Array.Empty<string>() : tags.ToArray();
            for (int i = 0; i < query.All.Length; i++)
            {
                if (!owned.Any(value => IsTagOrParent(value, query.All[i])))
                    return false;
            }
            if (query.Any.Length > 0 && !query.Any.Any(required => owned.Any(value => IsTagOrParent(value, required))))
                return false;
            for (int i = 0; i < query.None.Length; i++)
            {
                if (owned.Any(value => IsTagOrParent(value, query.None[i])))
                    return false;
            }
            return true;
        }

        void ReadTags()
        {
            foreach (ProgramCatalogEntry entry in m_Program.CatalogEntries.Where(value => value.Kind == ProgramCatalogEntryKind.GameplayTag))
            {
                string id = NormalizeTag(entry.Identity);
                string parent = Identity(entry, "Parent", string.Empty);
                m_TagParents.Add(id, string.IsNullOrEmpty(parent) ? string.Empty : NormalizeTag(parent));
                if (Boolean(entry, "Initial", false))
                    m_InitialTags.Add(id);
            }
        }

        void ReadAttributes()
        {
            foreach (ProgramCatalogEntry entry in m_Program.CatalogEntries.Where(value => value.Kind == ProgramCatalogEntryKind.Attribute))
            {
                string id = NormalizeAttribute(entry.Identity);
                var definition = new PortableAttributeDefinition(
                    id,
                    Scalar(entry, "InitialBase"),
                    ReadBound(entry, "Minimum"),
                    ReadBound(entry, "Maximum"));
                m_Attributes.Add(id, definition);
            }
        }

        void ReadEffects()
        {
            foreach (ProgramCatalogEntry entry in m_Program.CatalogEntries.Where(value => value.Kind == ProgramCatalogEntryKind.GameplayEffect))
            {
                ProgramConstant constant = Constant(entry, "Definition");
                if (constant.Kind != ProgramConstantKind.Bytes)
                    throw new InvalidDataException($"Gameplay Effect '{entry.Identity}' Definition is not Bytes.");
                PortableEffectDefinition definition = DecodeEffect(entry, constant.Bytes.ToArray());
                m_Effects.Add(definition.Id, definition);
            }
        }

        PortableEffectDefinition DecodeEffect(ProgramCatalogEntry entry, byte[] bytes)
        {
            var reader = new CanonicalReader(bytes);
            int version = reader.ReadInt32();
            if (version != 1)
                throw new InvalidDataException($"Gameplay Effect '{entry.Identity}' format '{version}' is unsupported.");
            string id = NormalizeEffect(reader.ReadString());
            uint revision = reader.ReadUInt32();
            if (!string.Equals(id, NormalizeEffect(entry.Identity), StringComparison.Ordinal) || revision != entry.Revision || revision == 0)
                throw new InvalidDataException($"Gameplay Effect '{entry.Identity}' catalog identity or revision does not match its definition bytes.");
            PortableEffectDurationPolicy durationPolicy = EnumValue<PortableEffectDurationPolicy>(reader.ReadInt32(), "duration policy");
            PortableMagnitude duration = ReadMagnitude(reader);
            bool hasPeriod = reader.ReadBoolean();
            PortableMagnitude period = ReadMagnitude(reader);
            bool executeOnApplication = reader.ReadBoolean();
            PortableEffectStackingPolicy stacking = EnumValue<PortableEffectStackingPolicy>(reader.ReadInt32(), "stacking policy");
            int maxStacks = reader.ReadInt32();
            if (maxStacks <= 0)
                throw new InvalidDataException($"Gameplay Effect '{id}' MaxStacks must be positive.");
            PortableEffectDurationUpdatePolicy durationUpdate = EnumValue<PortableEffectDurationUpdatePolicy>(reader.ReadInt32(), "duration update policy");
            PortableEffectPeriodUpdatePolicy periodUpdate = EnumValue<PortableEffectPeriodUpdatePolicy>(reader.ReadInt32(), "period update policy");
            PortableEffectOverflowPolicy overflow = EnumValue<PortableEffectOverflowPolicy>(reader.ReadInt32(), "overflow policy");
            string[] setByCaller = ReadStrings(reader, false);
            PortableEffectComponent[] components = ReadComponents(reader);
            reader.RequireComplete();
            string[] tags = entry.Fields
                .Where(value => value.Kind == ProgramCatalogFieldKind.Identity && value.Name.StartsWith("Tag:", StringComparison.Ordinal))
                .Select(value => NormalizeTag(value.Identity))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            return new PortableEffectDefinition(
                id,
                revision,
                tags,
                durationPolicy,
                duration,
                hasPeriod,
                period,
                executeOnApplication,
                stacking,
                maxStacks,
                durationUpdate,
                periodUpdate,
                overflow,
                setByCaller,
                components);
        }

        PortableEffectComponent[] ReadComponents(CanonicalReader reader)
        {
            int count = ReadCount(reader, "Gameplay Effect component");
            var result = new PortableEffectComponent[count];
            for (int i = 0; i < count; i++)
            {
                string type = reader.ReadString();
                if (type.EndsWith(".GameplayModifierComponentDefinition", StringComparison.Ordinal))
                {
                    result[i] = new PortableModifierComponent(
                        reader.ReadString(),
                        EnumValue<PortableModifierApplication>(reader.ReadInt32(), "modifier application"),
                        EnumValue<PortableModifierOperation>(reader.ReadInt32(), "modifier operation"),
                        ReadMagnitude(reader),
                        reader.ReadInt32(),
                        EnumValue<PortableClampBound>(reader.ReadInt32(), "modifier clamp bound"),
                        reader.ReadBoolean());
                }
                else if (type.EndsWith(".GrantedTagsComponentDefinition", StringComparison.Ordinal))
                {
                    result[i] = new PortableGrantedTagsComponent(ReadStrings(reader, true));
                }
                else if (type.EndsWith(".GameplayTagRequirementsComponentDefinition", StringComparison.Ordinal))
                {
                    result[i] = new PortableTagRequirementsComponent(
                        EnumValue<PortableRequirementPhase>(reader.ReadInt32(), "tag requirement phase"),
                        ReadQuery(reader),
                        ReadQuery(reader));
                }
                else if (type.EndsWith(".GameplayAttributeRequirementsComponentDefinition", StringComparison.Ordinal))
                {
                    result[i] = new PortableAttributeRequirementsComponent(
                        EnumValue<PortableRequirementPhase>(reader.ReadInt32(), "attribute requirement phase"),
                        EnumValue<PortableAttributeSource>(reader.ReadInt32(), "attribute requirement source"),
                        reader.ReadString(),
                        EnumValue<PortableAttributeComparison>(reader.ReadInt32(), "attribute comparison"),
                        ReadMagnitude(reader));
                }
                else if (type.EndsWith(".GameplayEffectExecutionComponentDefinition", StringComparison.Ordinal))
                {
                    int mutationCount = ReadCount(reader, "Gameplay Effect execution mutation");
                    var mutations = new PortableExecutionMutation[mutationCount];
                    for (int mutationIndex = 0; mutationIndex < mutationCount; mutationIndex++)
                    {
                        mutations[mutationIndex] = new PortableExecutionMutation(
                            reader.ReadString(),
                            EnumValue<PortableModifierOperation>(reader.ReadInt32(), "execution modifier operation"),
                            ReadMagnitude(reader),
                            EnumValue<PortableClampBound>(reader.ReadInt32(), "execution clamp bound"));
                    }
                    result[i] = new PortableExecutionComponent(mutations);
                }
                else if (type.EndsWith(".AdditionalEffectsComponentDefinition", StringComparison.Ordinal))
                {
                    int effectCount = ReadCount(reader, "Additional Gameplay Effect");
                    var effects = new PortableAdditionalEffect[effectCount];
                    for (int effectIndex = 0; effectIndex < effectCount; effectIndex++)
                    {
                        PortableAdditionalEffectTrigger trigger = EnumValue<PortableAdditionalEffectTrigger>(reader.ReadInt32(), "additional effect trigger");
                        string effectId = reader.ReadString();
                        int bindingCount = ReadCount(reader, "Additional Gameplay Effect parameter binding");
                        var bindings = new PortableAdditionalParameterBinding[bindingCount];
                        for (int bindingIndex = 0; bindingIndex < bindingCount; bindingIndex++)
                        {
                            bindings[bindingIndex] = new PortableAdditionalParameterBinding(
                                reader.ReadString(),
                                EnumValue<PortableAdditionalParameterSource>(reader.ReadInt32(), "additional effect parameter source"),
                                reader.ReadString(),
                                reader.ReadScalar());
                        }
                        effects[effectIndex] = new PortableAdditionalEffect(trigger, effectId, bindings);
                    }
                    result[i] = new PortableAdditionalEffectsComponent(effects);
                }
                else if (type.EndsWith(".GameplayCueBindingComponentDefinition", StringComparison.Ordinal))
                {
                    result[i] = new PortableCueComponent(
                        reader.ReadString(),
                        EnumValue<PortableCueTrigger>(reader.ReadInt32(), "Gameplay Cue trigger"));
                }
                else
                {
                    throw new InvalidDataException($"Gameplay Effect component '{type}' has no portable decoder.");
                }
            }
            return result;
        }

        PortableMagnitude ReadMagnitude(CanonicalReader reader)
        {
            return new PortableMagnitude(
                EnumValue<PortableMagnitudeSource>(reader.ReadInt32(), "magnitude source"),
                reader.ReadScalar(),
                reader.ReadString(),
                NormalizeOptionalAttribute(reader.ReadString()),
                reader.ReadScalar(),
                reader.ReadScalar());
        }

        PortableTagQuery ReadQuery(CanonicalReader reader)
        {
            return new PortableTagQuery(ReadStrings(reader, true), ReadStrings(reader, true), ReadStrings(reader, true));
        }

        PortableAttributeBound ReadBound(ProgramCatalogEntry entry, string prefix)
        {
            bool enabled = Boolean(entry, $"{prefix}:Enabled", false);
            if (!enabled)
                return default;
            int source = Int32(entry, $"{prefix}:Source");
            if (source == 0)
                return new PortableAttributeBound(true, false, Scalar(entry, $"{prefix}:Constant"), string.Empty);
            if (source == 1)
                return new PortableAttributeBound(true, true, Float32Scalar.Zero, NormalizeAttribute(Identity(entry, $"{prefix}:Attribute", string.Empty)));
            throw new InvalidDataException($"Attribute '{entry.Identity}' {prefix} bound source '{source}' is invalid.");
        }

        void ValidateClosure()
        {
            foreach (KeyValuePair<string, string> pair in m_TagParents)
            {
                if (!string.IsNullOrEmpty(pair.Value) && !m_TagParents.ContainsKey(pair.Value))
                    throw new InvalidDataException($"Gameplay Tag '{pair.Key}' references missing parent '{pair.Value}'.");
                string current = pair.Key;
                var visited = new HashSet<string>(StringComparer.Ordinal);
                while (!string.IsNullOrEmpty(current))
                {
                    if (!visited.Add(current))
                        throw new InvalidDataException($"Gameplay Tag parent cycle reaches '{current}'.");
                    current = m_TagParents.TryGetValue(current, out string parent) ? parent : string.Empty;
                }
            }
            foreach (PortableAttributeDefinition attribute in m_Attributes.Values)
            {
                ValidateBound(attribute.Id, attribute.Minimum);
                ValidateBound(attribute.Id, attribute.Maximum);
            }
            foreach (PortableEffectDefinition effect in m_Effects.Values)
            {
                for (int i = 0; i < effect.EffectTags.Length; i++)
                    RequireTag(effect.Id, effect.EffectTags[i]);
                ValidateMagnitude(effect.Id, effect.DurationMagnitude);
                if (effect.HasPeriod)
                    ValidateMagnitude(effect.Id, effect.PeriodMagnitude);
                for (int i = 0; i < effect.Components.Length; i++)
                    ValidateComponent(effect, effect.Components[i]);
            }
        }

        void ValidateComponent(PortableEffectDefinition owner, PortableEffectComponent component)
        {
            switch (component)
            {
                case PortableModifierComponent modifier:
                    RequireAttribute(owner.Id, modifier.AttributeId);
                    ValidateMagnitude(owner.Id, modifier.Magnitude);
                    break;
                case PortableGrantedTagsComponent granted:
                    for (int i = 0; i < granted.Tags.Length; i++) RequireTag(owner.Id, granted.Tags[i]);
                    break;
                case PortableTagRequirementsComponent tags:
                    ValidateQuery(owner.Id, tags.Source);
                    ValidateQuery(owner.Id, tags.Target);
                    break;
                case PortableAttributeRequirementsComponent attributes:
                    RequireAttribute(owner.Id, attributes.AttributeId);
                    ValidateMagnitude(owner.Id, attributes.Threshold);
                    break;
                case PortableExecutionComponent execution:
                    for (int i = 0; i < execution.Mutations.Length; i++)
                    {
                        RequireAttribute(owner.Id, execution.Mutations[i].AttributeId);
                        ValidateMagnitude(owner.Id, execution.Mutations[i].Magnitude);
                    }
                    break;
                case PortableAdditionalEffectsComponent additional:
                    for (int i = 0; i < additional.Effects.Length; i++)
                    {
                        if (!m_Effects.ContainsKey(additional.Effects[i].EffectId))
                            throw new InvalidDataException($"Gameplay Effect '{owner.Id}' references missing Additional Effect '{additional.Effects[i].EffectId}'.");
                    }
                    break;
                case PortableCueComponent:
                    break;
                default:
                    throw new InvalidDataException($"Gameplay Effect '{owner.Id}' contains unknown portable component '{component?.GetType().FullName}'.");
            }
        }

        void ValidateMagnitude(string owner, PortableMagnitude magnitude)
        {
            if (magnitude.Source == PortableMagnitudeSource.SetByCaller && string.IsNullOrEmpty(magnitude.SetByCallerParameterId))
                throw new InvalidDataException($"Gameplay Effect '{owner}' has a SetByCaller magnitude without a parameter id.");
            if (magnitude.Source == PortableMagnitudeSource.SourceAttributeSnapshot ||
                magnitude.Source == PortableMagnitudeSource.TargetAttributeSnapshot ||
                magnitude.Source == PortableMagnitudeSource.TargetAttributeLive)
                RequireAttribute(owner, magnitude.AttributeId);
        }

        void ValidateQuery(string owner, PortableTagQuery query)
        {
            foreach (string tag in query.All.Concat(query.Any).Concat(query.None))
                RequireTag(owner, tag);
        }

        void ValidateBound(string owner, PortableAttributeBound bound)
        {
            if (bound.Enabled && bound.FromAttribute)
                RequireAttribute(owner, bound.AttributeId);
        }

        void RequireTag(string owner, string id)
        {
            if (!m_TagParents.ContainsKey(id))
                throw new InvalidDataException($"Gameplay definition '{owner}' references missing Tag '{id}'.");
        }

        void RequireAttribute(string owner, string id)
        {
            if (!m_Attributes.ContainsKey(id))
                throw new InvalidDataException($"Gameplay definition '{owner}' references missing Attribute '{id}'.");
        }

        ProgramConstant Constant(ProgramCatalogEntry entry, string name)
        {
            ProgramCatalogField field = entry.Fields.FirstOrDefault(value => value.Kind == ProgramCatalogFieldKind.Constant && string.Equals(value.Name, name, StringComparison.Ordinal));
            if (field == null)
                throw new InvalidDataException($"Catalog entry '{entry.Identity}' is missing constant field '{name}'.");
            return m_Program.Constants[field.ConstantIndex];
        }

        string Identity(ProgramCatalogEntry entry, string name, string fallback)
        {
            ProgramCatalogField field = entry.Fields.FirstOrDefault(value => value.Kind == ProgramCatalogFieldKind.Identity && string.Equals(value.Name, name, StringComparison.Ordinal));
            return field?.Identity ?? fallback;
        }

        bool Boolean(ProgramCatalogEntry entry, string name, bool fallback)
        {
            ProgramCatalogField field = entry.Fields.FirstOrDefault(value => value.Kind == ProgramCatalogFieldKind.Constant && string.Equals(value.Name, name, StringComparison.Ordinal));
            if (field == null)
                return fallback;
            ProgramConstant constant = m_Program.Constants[field.ConstantIndex];
            if (constant.Kind != ProgramConstantKind.Boolean)
                throw new InvalidDataException($"Catalog field '{entry.Identity}/{name}' is not Boolean.");
            return constant.Boolean;
        }

        int Int32(ProgramCatalogEntry entry, string name)
        {
            ProgramConstant constant = Constant(entry, name);
            if (constant.Kind != ProgramConstantKind.Int32)
                throw new InvalidDataException($"Catalog field '{entry.Identity}/{name}' is not Int32.");
            return constant.Int32;
        }

        Float32Scalar Scalar(ProgramCatalogEntry entry, string name)
        {
            ProgramConstant constant = Constant(entry, name);
            if (constant.Kind != ProgramConstantKind.Scalar)
                throw new InvalidDataException($"Catalog field '{entry.Identity}/{name}' is not Scalar.");
            return constant.Scalar;
        }

        static string[] ReadStrings(CanonicalReader reader, bool normalizeTags)
        {
            int count = ReadCount(reader, "string array");
            var values = new string[count];
            for (int i = 0; i < count; i++)
                values[i] = normalizeTags ? NormalizeTag(reader.ReadString()) : SimulationIdentity.Require(reader.ReadString(), "CatalogString");
            Array.Sort(values, StringComparer.Ordinal);
            for (int i = 1; i < values.Length; i++)
            {
                if (string.Equals(values[i - 1], values[i], StringComparison.Ordinal))
                    throw new InvalidDataException($"Canonical string array contains duplicate '{values[i]}'.");
            }
            return values;
        }

        static int ReadCount(CanonicalReader reader, string label)
        {
            int count = reader.ReadInt32();
            if (count < 0 || count > 100000)
                throw new InvalidDataException($"{label} count '{count}' is invalid.");
            return count;
        }

        static T EnumValue<T>(int value, string label) where T : struct, Enum
        {
            object candidate = Enum.ToObject(typeof(T), value);
            if (!Enum.IsDefined(typeof(T), candidate))
                throw new InvalidDataException($"Gameplay Effect {label} '{value}' is invalid.");
            return (T)candidate;
        }

        internal static string NormalizeTag(string value) => Normalize(value, "tag:", "GameplayTag");
        internal static string NormalizeAttribute(string value) => Normalize(value, "attribute:", "GameplayAttribute");
        internal static string NormalizeEffect(string value) => Normalize(value, "effect:", "GameplayEffect");
        static string NormalizeOptionalAttribute(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : NormalizeAttribute(value);

        static string Normalize(string value, string prefix, string parameterName)
        {
            string identity = SimulationIdentity.Require(value, parameterName);
            return identity.StartsWith(prefix, StringComparison.Ordinal) ? identity : prefix + identity;
        }
    }
}
