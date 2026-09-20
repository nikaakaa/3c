using ThirdPersonSimulation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ThirdPersonSimulation.Fixed
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

    internal enum PortableAttackCollisionKind : byte
    {
        Box = 0,
        BoxContinuous = 1,
        FanWithHeight = 2
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
            FixedScalar constant,
            string setByCallerParameterId,
            string attributeId,
            FixedScalar coefficient,
            FixedScalar postAdd)
        {
            Source = source;
            Constant = constant;
            SetByCallerParameterId = setByCallerParameterId ?? string.Empty;
            AttributeId = attributeId ?? string.Empty;
            Coefficient = coefficient;
            PostAdd = postAdd;
        }

        public PortableMagnitudeSource Source { get; }
        public FixedScalar Constant { get; }
        public string SetByCallerParameterId { get; }
        public string AttributeId { get; }
        public FixedScalar Coefficient { get; }
        public FixedScalar PostAdd { get; }
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
                : values.Select(FixedGameplayEffectRuntimeCatalog.NormalizeTag).ToList();
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
        public PortableAttributeBound(bool enabled, bool fromAttribute, FixedScalar constant, string attributeId)
        {
            Enabled = enabled;
            FromAttribute = fromAttribute;
            Constant = constant;
            AttributeId = attributeId ?? string.Empty;
        }

        public bool Enabled { get; }
        public bool FromAttribute { get; }
        public FixedScalar Constant { get; }
        public string AttributeId { get; }
    }

    internal sealed class PortableAttributeDefinition
    {
        public PortableAttributeDefinition(string id, FixedScalar initialBase, PortableAttributeBound minimum, PortableAttributeBound maximum)
        {
            Id = FixedGameplayEffectRuntimeCatalog.NormalizeAttribute(id);
            InitialBase = initialBase;
            Minimum = minimum;
            Maximum = maximum;
        }

        public string Id { get; }
        public FixedScalar InitialBase { get; }
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
            AttributeId = FixedGameplayEffectRuntimeCatalog.NormalizeAttribute(attributeId);
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
            Tags = tags.Select(FixedGameplayEffectRuntimeCatalog.NormalizeTag).OrderBy(value => value, StringComparer.Ordinal).ToArray();
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
            AttributeId = FixedGameplayEffectRuntimeCatalog.NormalizeAttribute(attributeId);
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
            AttributeId = FixedGameplayEffectRuntimeCatalog.NormalizeAttribute(attributeId);
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
        public PortableAdditionalParameterBinding(string childParameterId, PortableAdditionalParameterSource source, string parentParameterId, FixedScalar constant)
        {
            ChildParameterId = SimulationIdentity.Require(childParameterId, nameof(childParameterId));
            Source = source;
            ParentParameterId = parentParameterId ?? string.Empty;
            Constant = constant;
        }

        public string ChildParameterId { get; }
        public PortableAdditionalParameterSource Source { get; }
        public string ParentParameterId { get; }
        public FixedScalar Constant { get; }
    }

    internal readonly struct PortableAdditionalEffect
    {
        public PortableAdditionalEffect(PortableAdditionalEffectTrigger trigger, string effectId, PortableAdditionalParameterBinding[] bindings)
        {
            Trigger = trigger;
            EffectId = FixedGameplayEffectRuntimeCatalog.NormalizeEffect(effectId);
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

    internal sealed class PortableAttackCollisionComponent : PortableEffectComponent
    {
        public PortableAttackCollisionComponent(
            PortableAttackCollisionKind kind,
            FixedVector3 centerOffset,
            FixedScalar width,
            FixedScalar height,
            FixedScalar depth,
            FixedScalar fanAngle,
            FixedScalar radius,
            FixedScalar invalidRadius,
            FixedScalar invalidAngle,
            int followDirectionType,
            int coreDistance,
            bool isSubtractive,
            FixedScalar hitInterval,
            int aliveMaxHitCount,
            int unitMaxHitCount,
            bool followAttacker)
        {
            Kind = kind;
            CenterOffset = centerOffset;
            Width = width;
            Height = height;
            Depth = depth;
            FanAngle = fanAngle;
            Radius = radius;
            InvalidRadius = invalidRadius;
            InvalidAngle = invalidAngle;
            FollowDirectionType = followDirectionType;
            CoreDistance = coreDistance;
            IsSubtractive = isSubtractive;
            HitInterval = hitInterval;
            AliveMaxHitCount = aliveMaxHitCount;
            UnitMaxHitCount = unitMaxHitCount;
            FollowAttacker = followAttacker;
        }

        public PortableAttackCollisionKind Kind { get; }
        public FixedVector3 CenterOffset { get; }
        public FixedScalar Width { get; }
        public FixedScalar Height { get; }
        public FixedScalar Depth { get; }
        public FixedScalar FanAngle { get; }
        public FixedScalar Radius { get; }
        public FixedScalar InvalidRadius { get; }
        public FixedScalar InvalidAngle { get; }
        public int FollowDirectionType { get; }
        public int CoreDistance { get; }
        public bool IsSubtractive { get; }
        public FixedScalar HitInterval { get; }
        public int AliveMaxHitCount { get; }
        public int UnitMaxHitCount { get; }
        public bool FollowAttacker { get; }
    }

    internal sealed class PortableAttackPropertyComponent : PortableEffectComponent
    {
        public PortableAttackPropertyComponent(
            string sourceKey,
            int hitType,
            int hitStrengthType,
            string[] combatTags,
            PortableMagnitude damagePercentage,
            PortableMagnitude addedDamage,
            PortableMagnitude breakStunRatio,
            PortableMagnitude elementAccumulation,
            PortableMagnitude exhaustedAccumulation,
            PortableMagnitude exhaustedChase,
            int damageElement,
            int damageHitType,
            int damageBreakLevel,
            PortableMagnitude damageBreakLevelProbability,
            int triggerBuffLevel,
            int destructionClass,
            int destructionDurability,
            int overrideDamageStaggerLevel,
            int damageTextId,
            int damageTextWaitMilliseconds,
            int frameHalt,
            int attackerFrameHalt,
            uint groundHitEffectId,
            uint skyHitEffectId,
            uint downHitEffectId,
            string standardConfigKey,
            string abilityTargetKey,
            bool isCauseStun,
            bool isHeavyAttack,
            bool isCauseExhausted,
            bool isHeal,
            bool isIndirect,
            bool hitsEnemy,
            bool hitsAllied,
            bool hitsNeutral,
            bool useAbilityTargetKey,
            bool banDamage)
        {
            SourceKey = SimulationIdentity.Require(sourceKey, nameof(sourceKey));
            HitType = hitType;
            HitStrengthType = hitStrengthType;
            CombatTags = combatTags ?? Array.Empty<string>();
            DamagePercentage = damagePercentage;
            AddedDamage = addedDamage;
            BreakStunRatio = breakStunRatio;
            ElementAccumulation = elementAccumulation;
            ExhaustedAccumulation = exhaustedAccumulation;
            ExhaustedChase = exhaustedChase;
            DamageElement = damageElement;
            DamageHitType = damageHitType;
            DamageBreakLevel = damageBreakLevel;
            DamageBreakLevelProbability = damageBreakLevelProbability;
            TriggerBuffLevel = triggerBuffLevel;
            DestructionClass = destructionClass;
            DestructionDurability = destructionDurability;
            OverrideDamageStaggerLevel = overrideDamageStaggerLevel;
            DamageTextId = damageTextId;
            DamageTextWaitMilliseconds = damageTextWaitMilliseconds;
            FrameHalt = frameHalt;
            AttackerFrameHalt = attackerFrameHalt;
            GroundHitEffectId = groundHitEffectId;
            SkyHitEffectId = skyHitEffectId;
            DownHitEffectId = downHitEffectId;
            StandardConfigKey = standardConfigKey ?? string.Empty;
            AbilityTargetKey = abilityTargetKey ?? string.Empty;
            IsCauseStun = isCauseStun;
            IsHeavyAttack = isHeavyAttack;
            IsCauseExhausted = isCauseExhausted;
            IsHeal = isHeal;
            IsIndirect = isIndirect;
            HitsEnemy = hitsEnemy;
            HitsAllied = hitsAllied;
            HitsNeutral = hitsNeutral;
            UseAbilityTargetKey = useAbilityTargetKey;
            BanDamage = banDamage;
        }

        public string SourceKey { get; }
        public int HitType { get; }
        public int HitStrengthType { get; }
        public string[] CombatTags { get; }
        public PortableMagnitude DamagePercentage { get; }
        public PortableMagnitude AddedDamage { get; }
        public PortableMagnitude BreakStunRatio { get; }
        public PortableMagnitude ElementAccumulation { get; }
        public PortableMagnitude ExhaustedAccumulation { get; }
        public PortableMagnitude ExhaustedChase { get; }
        public int DamageElement { get; }
        public int DamageHitType { get; }
        public int DamageBreakLevel { get; }
        public PortableMagnitude DamageBreakLevelProbability { get; }
        public int TriggerBuffLevel { get; }
        public int DestructionClass { get; }
        public int DestructionDurability { get; }
        public int OverrideDamageStaggerLevel { get; }
        public int DamageTextId { get; }
        public int DamageTextWaitMilliseconds { get; }
        public int FrameHalt { get; }
        public int AttackerFrameHalt { get; }
        public uint GroundHitEffectId { get; }
        public uint SkyHitEffectId { get; }
        public uint DownHitEffectId { get; }
        public string StandardConfigKey { get; }
        public string AbilityTargetKey { get; }
        public bool IsCauseStun { get; }
        public bool IsHeavyAttack { get; }
        public bool IsCauseExhausted { get; }
        public bool IsHeal { get; }
        public bool IsIndirect { get; }
        public bool HitsEnemy { get; }
        public bool HitsAllied { get; }
        public bool HitsNeutral { get; }
        public bool UseAbilityTargetKey { get; }
        public bool BanDamage { get; }
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
            Id = FixedGameplayEffectRuntimeCatalog.NormalizeEffect(id);
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
            SourceSnapshotAttributes = CollectSnapshotAttributes(PortableMagnitudeSource.SourceAttributeSnapshot);
            TargetSnapshotAttributes = CollectSnapshotAttributes(PortableMagnitudeSource.TargetAttributeSnapshot);
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
        public IReadOnlyList<string> SourceSnapshotAttributes { get; }
        public IReadOnlyList<string> TargetSnapshotAttributes { get; }

        string[] CollectSnapshotAttributes(PortableMagnitudeSource source)
        {
            var result = new SortedSet<string>(StringComparer.Ordinal);
            Collect(DurationMagnitude);
            if (HasPeriod)
                Collect(PeriodMagnitude);
            for (int i = 0; i < Components.Length; i++)
            {
                switch (Components[i])
                {
                    case PortableModifierComponent modifier:
                        Collect(modifier.Magnitude);
                        break;
                    case PortableAttributeRequirementsComponent requirement:
                        if (requirement.Source == PortableAttributeSource.SourceSnapshot && source == PortableMagnitudeSource.SourceAttributeSnapshot)
                            result.Add(requirement.AttributeId);
                        Collect(requirement.Threshold);
                        break;
                    case PortableExecutionComponent execution:
                        for (int mutationIndex = 0; mutationIndex < execution.Mutations.Length; mutationIndex++)
                            Collect(execution.Mutations[mutationIndex].Magnitude);
                        break;
                }
            }
            return result.ToArray();

            void Collect(PortableMagnitude magnitude)
            {
                if (magnitude.Source == source)
                    result.Add(magnitude.AttributeId);
            }
        }
    }

    internal sealed class FixedGameplayEffectRuntimeCatalog
    {
        readonly Dictionary<string, string> m_TagParents = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly HashSet<string> m_InitialTags = new HashSet<string>(StringComparer.Ordinal);
        readonly Dictionary<string, PortableAttributeDefinition> m_Attributes = new Dictionary<string, PortableAttributeDefinition>(StringComparer.Ordinal);
        readonly Dictionary<string, PortableEffectDefinition> m_Effects = new Dictionary<string, PortableEffectDefinition>(StringComparer.Ordinal);

        public FixedGameplayEffectRuntimeCatalog(CharacterGameplayEffectRuntimeBinding binding)
        {
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));
            ReadBinding(binding.CatalogBytes);
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

        void ReadBinding(byte[] bytes)
        {
            var reader = new CanonicalReader(bytes);
            if (reader.ReadInt32() != CharacterGameplayEffectRuntimeBinding.CatalogFormatVersion)
                throw new InvalidDataException("Character Gameplay Effect runtime catalog format is unsupported.");
            int tagCount = ReadCount(reader, "Gameplay Tag");
            for (int i = 0; i < tagCount; i++)
            {
                string id = NormalizeTag(reader.ReadString());
                string parent = reader.ReadString();
                m_TagParents.Add(id, string.IsNullOrEmpty(parent) ? string.Empty : NormalizeTag(parent));
                if (reader.ReadBoolean())
                    m_InitialTags.Add(id);
            }
            int attributeCount = ReadCount(reader, "Gameplay Attribute");
            for (int i = 0; i < attributeCount; i++)
            {
                string id = NormalizeAttribute(reader.ReadString());
                m_Attributes.Add(
                    id,
                    new PortableAttributeDefinition(
                        id,
                        FixedScalar.FromDouble(reader.ReadDouble()),
                        ReadBindingBound(reader),
                        ReadBindingBound(reader)));
            }
            int effectCount = ReadCount(reader, "Gameplay Effect");
            for (int i = 0; i < effectCount; i++)
            {
                string id = reader.ReadString();
                uint revision = reader.ReadUInt32();
                string[] tags = ReadStrings(reader, true);
                PortableEffectDefinition definition = DecodeBindingEffect(id, revision, tags, reader.ReadBytes());
                if (!m_Effects.TryAdd(definition.Id, definition))
                    throw new InvalidDataException($"Gameplay Effect '{definition.Id}' is duplicated in the runtime catalog.");
            }
            reader.RequireComplete();
        }

        PortableAttributeBound ReadBindingBound(CanonicalReader reader)
        {
            if (!reader.ReadBoolean())
                return default;
            int source = reader.ReadInt32();
            if (source == 0)
                return new PortableAttributeBound(true, false, FixedScalar.FromDouble(reader.ReadDouble()), string.Empty);
            if (source == 1)
                return new PortableAttributeBound(true, true, FixedScalar.Zero, NormalizeAttribute(reader.ReadString()));
            throw new InvalidDataException($"Gameplay Attribute bound source '{source}' is invalid.");
        }

        PortableEffectDefinition DecodeBindingEffect(
            string entryIdentity,
            uint entryRevision,
            string[] tags,
            byte[] bytes)
        {
            var reader = new CanonicalReader(bytes);
            int version = reader.ReadInt32();
            if (version != 1)
                throw new InvalidDataException($"Gameplay Effect '{entryIdentity}' format '{version}' is unsupported.");
            string id = NormalizeEffect(reader.ReadString());
            uint revision = reader.ReadUInt32();
            if (!string.Equals(id, NormalizeEffect(entryIdentity), StringComparison.Ordinal) || revision != entryRevision || revision == 0)
                throw new InvalidDataException($"Gameplay Effect '{entryIdentity}' runtime identity or revision does not match its definition bytes.");
            PortableEffectDurationPolicy durationPolicy = EnumValue<PortableEffectDurationPolicy>(reader.ReadInt32(), "duration policy");
            PortableMagnitude duration = ReadMagnitude(reader, true);
            bool hasPeriod = reader.ReadBoolean();
            PortableMagnitude period = ReadMagnitude(reader, true);
            bool executeOnApplication = reader.ReadBoolean();
            PortableEffectStackingPolicy stacking = EnumValue<PortableEffectStackingPolicy>(reader.ReadInt32(), "stacking policy");
            int maxStacks = reader.ReadInt32();
            if (maxStacks <= 0)
                throw new InvalidDataException($"Gameplay Effect '{id}' MaxStacks must be positive.");
            PortableEffectDurationUpdatePolicy durationUpdate = EnumValue<PortableEffectDurationUpdatePolicy>(reader.ReadInt32(), "duration update policy");
            PortableEffectPeriodUpdatePolicy periodUpdate = EnumValue<PortableEffectPeriodUpdatePolicy>(reader.ReadInt32(), "period update policy");
            PortableEffectOverflowPolicy overflow = EnumValue<PortableEffectOverflowPolicy>(reader.ReadInt32(), "overflow policy");
            string[] setByCaller = ReadStrings(reader, false);
            PortableEffectComponent[] components = ReadComponents(reader, true);
            reader.RequireComplete();
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

        PortableEffectComponent[] ReadComponents(CanonicalReader reader, bool sourceDouble = false)
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
                        ReadMagnitude(reader, sourceDouble),
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
                        ReadMagnitude(reader, sourceDouble));
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
                            ReadMagnitude(reader, sourceDouble),
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
                                sourceDouble ? FixedScalar.FromDouble(reader.ReadDouble()) : reader.ReadScalar());
                        }
                        effects[effectIndex] = new PortableAdditionalEffect(trigger, effectId, bindings);
                    }
                    result[i] = new PortableAdditionalEffectsComponent(effects);
                }
                else if (type.EndsWith(".GameplayAttackCollisionComponentDefinition", StringComparison.Ordinal))
                {
                    result[i] = new PortableAttackCollisionComponent(
                        EnumValue<PortableAttackCollisionKind>(reader.ReadInt32(), "attack collision kind"),
                        reader.ReadVector3(),
                        sourceDouble ? FixedScalar.FromDouble(reader.ReadDouble()) : reader.ReadScalar(),
                        sourceDouble ? FixedScalar.FromDouble(reader.ReadDouble()) : reader.ReadScalar(),
                        sourceDouble ? FixedScalar.FromDouble(reader.ReadDouble()) : reader.ReadScalar(),
                        sourceDouble ? FixedScalar.FromDouble(reader.ReadDouble()) : reader.ReadScalar(),
                        sourceDouble ? FixedScalar.FromDouble(reader.ReadDouble()) : reader.ReadScalar(),
                        sourceDouble ? FixedScalar.FromDouble(reader.ReadDouble()) : reader.ReadScalar(),
                        sourceDouble ? FixedScalar.FromDouble(reader.ReadDouble()) : reader.ReadScalar(),
                        reader.ReadInt32(),
                        reader.ReadInt32(),
                        reader.ReadBoolean(),
                        sourceDouble ? FixedScalar.FromDouble(reader.ReadDouble()) : reader.ReadScalar(),
                        reader.ReadInt32(),
                        reader.ReadInt32(),
                        reader.ReadBoolean());
                }
                else if (type.EndsWith(".GameplayAttackPropertyComponentDefinition", StringComparison.Ordinal))
                {
                    result[i] = new PortableAttackPropertyComponent(
                        reader.ReadString(),
                        reader.ReadInt32(),
                        reader.ReadInt32(),
                        ReadStrings(reader, false),
                        ReadMagnitude(reader, sourceDouble),
                        ReadMagnitude(reader, sourceDouble),
                        ReadMagnitude(reader, sourceDouble),
                        ReadMagnitude(reader, sourceDouble),
                        ReadMagnitude(reader, sourceDouble),
                        ReadMagnitude(reader, sourceDouble),
                        reader.ReadInt32(),
                        reader.ReadInt32(),
                        reader.ReadInt32(),
                        ReadMagnitude(reader, sourceDouble),
                        reader.ReadInt32(),
                        reader.ReadInt32(),
                        reader.ReadInt32(),
                        reader.ReadInt32(),
                        reader.ReadInt32(),
                        reader.ReadInt32(),
                        reader.ReadInt32(),
                        reader.ReadInt32(),
                        reader.ReadUInt32(),
                        reader.ReadUInt32(),
                        reader.ReadUInt32(),
                        reader.ReadString(),
                        reader.ReadString(),
                        reader.ReadBoolean(),
                        reader.ReadBoolean(),
                        reader.ReadBoolean(),
                        reader.ReadBoolean(),
                        reader.ReadBoolean(),
                        reader.ReadBoolean(),
                        reader.ReadBoolean(),
                        reader.ReadBoolean(),
                        reader.ReadBoolean(),
                        reader.ReadBoolean());
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

        PortableMagnitude ReadMagnitude(CanonicalReader reader, bool sourceDouble = false)
        {
            return new PortableMagnitude(
                EnumValue<PortableMagnitudeSource>(reader.ReadInt32(), "magnitude source"),
                sourceDouble ? FixedScalar.FromDouble(reader.ReadDouble()) : reader.ReadScalar(),
                reader.ReadString(),
                NormalizeOptionalAttribute(reader.ReadString()),
                sourceDouble ? FixedScalar.FromDouble(reader.ReadDouble()) : reader.ReadScalar(),
                sourceDouble ? FixedScalar.FromDouble(reader.ReadDouble()) : reader.ReadScalar());
        }

        PortableTagQuery ReadQuery(CanonicalReader reader)
        {
            return new PortableTagQuery(ReadStrings(reader, true), ReadStrings(reader, true), ReadStrings(reader, true));
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
                case PortableAttackCollisionComponent:
                    break;
                case PortableAttackPropertyComponent attackComponent:
                    for (int i = 0; i < attackComponent.CombatTags.Length; i++)
                        SimulationIdentity.Require(attackComponent.CombatTags[i], "CombatTag");
                    ValidateMagnitude(owner.Id, attackComponent.DamagePercentage);
                    ValidateMagnitude(owner.Id, attackComponent.AddedDamage);
                    ValidateMagnitude(owner.Id, attackComponent.BreakStunRatio);
                    ValidateMagnitude(owner.Id, attackComponent.ElementAccumulation);
                    ValidateMagnitude(owner.Id, attackComponent.ExhaustedAccumulation);
                    ValidateMagnitude(owner.Id, attackComponent.ExhaustedChase);
                    ValidateMagnitude(owner.Id, attackComponent.DamageBreakLevelProbability);
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



