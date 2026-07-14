using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Contracts;
using ThirdPersonGameplay.Tags;
using UnityEngine;

namespace ThirdPersonGameplay.Effects
{
    internal readonly struct GameplayMagnitudeData
    {
        public GameplayMagnitudeData(
            GameplayMagnitudeSource source,
            float constant,
            string setByCallerParameterId,
            GameplayAttributeId attributeId,
            float coefficient,
            float postAdd)
        {
            Source = source;
            Constant = constant;
            SetByCallerParameterId = setByCallerParameterId ?? string.Empty;
            AttributeId = attributeId;
            Coefficient = coefficient;
            PostAdd = postAdd;
        }

        public GameplayMagnitudeSource Source { get; }
        public float Constant { get; }
        public string SetByCallerParameterId { get; }
        public GameplayAttributeId AttributeId { get; }
        public float Coefficient { get; }
        public float PostAdd { get; }

        public static GameplayMagnitudeData From(GameplayMagnitudeDefinition definition)
        {
            return definition == null
                ? default
                : new GameplayMagnitudeData(
                    definition.Source,
                    definition.Constant,
                    definition.SetByCallerParameterId,
                    definition.AttributeId,
                    definition.Coefficient,
                    definition.PostAdd);
        }
    }

    internal sealed class GameplayEffectDefinitionData
    {
        public GameplayEffectDefinitionData(
            GameplayEffectId effectId,
            uint definitionRevision,
            string displayName,
            string debugCategory,
            GameplayTagId[] effectTags,
            GameplayEffectDurationPolicy durationPolicy,
            GameplayMagnitudeData durationMagnitude,
            bool hasPeriod,
            GameplayMagnitudeData periodMagnitude,
            bool executeOnApplication,
            GameplayEffectStackingPolicy stackingPolicy,
            int maxStacks,
            GameplayEffectDurationUpdatePolicy durationUpdate,
            GameplayEffectPeriodUpdatePolicy periodUpdate,
            GameplayEffectOverflowPolicy overflowPolicy,
            IReadOnlyDictionary<string, bool> setByCallerParameters,
            GameplayAttributeId[] sourceSnapshotAttributes,
            GameplayAttributeId[] targetSnapshotAttributes,
            GameplayEffectComponentData[] components)
        {
            EffectId = effectId;
            DefinitionRevision = definitionRevision;
            DisplayName = displayName ?? string.Empty;
            DebugCategory = debugCategory ?? string.Empty;
            EffectTags = effectTags ?? Array.Empty<GameplayTagId>();
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
            SetByCallerParameters = setByCallerParameters;
            SourceSnapshotAttributes = sourceSnapshotAttributes ?? Array.Empty<GameplayAttributeId>();
            TargetSnapshotAttributes = targetSnapshotAttributes ?? Array.Empty<GameplayAttributeId>();
            Components = components ?? Array.Empty<GameplayEffectComponentData>();
        }

        public GameplayEffectId EffectId { get; }
        public uint DefinitionRevision { get; }
        public string DisplayName { get; }
        public string DebugCategory { get; }
        public GameplayTagId[] EffectTags { get; }
        public GameplayEffectDurationPolicy DurationPolicy { get; }
        public GameplayMagnitudeData DurationMagnitude { get; }
        public bool HasPeriod { get; }
        public GameplayMagnitudeData PeriodMagnitude { get; }
        public bool ExecuteOnApplication { get; }
        public GameplayEffectStackingPolicy StackingPolicy { get; }
        public int MaxStacks { get; }
        public GameplayEffectDurationUpdatePolicy DurationUpdate { get; }
        public GameplayEffectPeriodUpdatePolicy PeriodUpdate { get; }
        public GameplayEffectOverflowPolicy OverflowPolicy { get; }
        public IReadOnlyDictionary<string, bool> SetByCallerParameters { get; }
        public GameplayAttributeId[] SourceSnapshotAttributes { get; }
        public GameplayAttributeId[] TargetSnapshotAttributes { get; }
        public GameplayEffectComponentData[] Components { get; }
    }

    internal readonly struct GameplayEffectStackKey : IEquatable<GameplayEffectStackKey>
    {
        public GameplayEffectStackKey(GameplayEffectId effectId, string sourceActorId)
        {
            EffectId = effectId;
            SourceActorId = sourceActorId ?? string.Empty;
        }

        public GameplayEffectId EffectId { get; }
        public string SourceActorId { get; }
        public bool Equals(GameplayEffectStackKey other) => EffectId == other.EffectId && string.Equals(SourceActorId, other.SourceActorId, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is GameplayEffectStackKey other && Equals(other);
        public override int GetHashCode() => EffectId.GetHashCode() * 397 ^ StringComparer.Ordinal.GetHashCode(SourceActorId);
    }

    public sealed class GameplayEffectSpec
    {
        readonly Dictionary<string, float> m_SetByCallerValues;
        readonly Dictionary<GameplayAttributeId, float> m_SourceAttributeSnapshots;
        readonly Dictionary<GameplayAttributeId, float> m_TargetAttributeSnapshots;

        internal GameplayEffectSpec(
            GameplayEffectDefinitionData definition,
            GameplayEffectContext context,
            Dictionary<string, float> setByCallerValues,
            Dictionary<GameplayAttributeId, float> sourceAttributeSnapshots,
            Dictionary<GameplayAttributeId, float> targetAttributeSnapshots,
            GameplayTagId[] sourceTagSnapshot,
            GameplayTagId[] targetTagSnapshot,
            ulong durationTicks,
            ulong periodTicks,
            ulong firstPeriodTick,
            GameplayEffectStackKey stackKey,
            GameplayEffectInstanceId authoritativeInstanceId,
            ulong authoritativeLifecycleRevision)
        {
            Definition = definition;
            Context = context;
            m_SetByCallerValues = setByCallerValues;
            m_SourceAttributeSnapshots = sourceAttributeSnapshots;
            m_TargetAttributeSnapshots = targetAttributeSnapshots;
            SourceTagSnapshot = sourceTagSnapshot ?? Array.Empty<GameplayTagId>();
            TargetTagSnapshot = targetTagSnapshot ?? Array.Empty<GameplayTagId>();
            DurationTicks = durationTicks;
            PeriodTicks = periodTicks;
            FirstPeriodTick = firstPeriodTick;
            StackKey = stackKey;
            AuthoritativeInstanceId = authoritativeInstanceId;
            AuthoritativeLifecycleRevision = authoritativeLifecycleRevision;
        }

        internal GameplayEffectDefinitionData Definition { get; }
        internal GameplayEffectStackKey StackKey { get; }
        public GameplayEffectId EffectId => Definition.EffectId;
        public uint DefinitionRevision => Definition.DefinitionRevision;
        public GameplayEffectContext Context { get; }
        public IReadOnlyList<GameplayTagId> SourceTagSnapshot { get; }
        public IReadOnlyList<GameplayTagId> TargetTagSnapshot { get; }
        public ulong DurationTicks { get; }
        public ulong PeriodTicks { get; }
        public ulong FirstPeriodTick { get; }
        public GameplayEffectInstanceId AuthoritativeInstanceId { get; }
        public ulong AuthoritativeLifecycleRevision { get; }

        internal bool TryGetSetByCaller(string parameterId, out float value)
        {
            return m_SetByCallerValues.TryGetValue(parameterId, out value);
        }

        internal GameplaySetByCallerValue[] CopySetByCallerValues()
        {
            var values = new GameplaySetByCallerValue[m_SetByCallerValues.Count];
            int index = 0;
            foreach (KeyValuePair<string, float> pair in m_SetByCallerValues)
                values[index++] = new GameplaySetByCallerValue(pair.Key, pair.Value);
            Array.Sort(values, (left, right) => string.Compare(left.ParameterId, right.ParameterId, StringComparison.Ordinal));
            return values;
        }

        internal bool TryGetSourceAttribute(GameplayAttributeId attributeId, out float value)
        {
            return m_SourceAttributeSnapshots.TryGetValue(attributeId, out value);
        }

        internal bool TryGetTargetAttribute(GameplayAttributeId attributeId, out float value)
        {
            return m_TargetAttributeSnapshots.TryGetValue(attributeId, out value);
        }
    }

    public sealed class ActiveGameplayEffect
    {
        internal ActiveGameplayEffect(
            GameplayEffectHandle handle,
            GameplayEffectInstanceId instanceId,
            GameplayEffectSpec spec,
            ulong startTick,
            ulong endTick,
            ulong nextPeriodTick,
            ulong insertionSequence,
            ulong lifecycleRevision)
        {
            Handle = handle;
            InstanceId = instanceId;
            Spec = spec;
            StartTick = startTick;
            EndTick = endTick;
            NextPeriodTick = nextPeriodTick;
            InsertionSequence = insertionSequence;
            StackCount = 1;
            LifecycleRevision = lifecycleRevision;
        }

        public GameplayEffectHandle Handle { get; }
        public GameplayEffectInstanceId InstanceId { get; internal set; }
        public GameplayEffectSpec Spec { get; }
        public ulong StartTick { get; internal set; }
        public ulong EndTick { get; internal set; }
        public ulong NextPeriodTick { get; internal set; }
        public ulong InsertionSequence { get; }
        public int StackCount { get; internal set; }
        public bool Inhibited { get; internal set; }
        public ulong LifecycleRevision { get; internal set; }
        internal bool PendingRemoval { get; set; }
        internal List<GameplayModifierHandle> ModifierHandles { get; } = new List<GameplayModifierHandle>();
        internal List<GameplayTagId> GrantedTags { get; } = new List<GameplayTagId>();

        internal GameplayActiveEffectSnapshot Snapshot()
        {
            return new GameplayActiveEffectSnapshot(
                Handle,
                InstanceId,
                StartTick,
                EndTick,
                NextPeriodTick,
                StackCount,
                Inhibited,
                LifecycleRevision);
        }
    }

    internal readonly struct GameplayActiveEffectSnapshot
    {
        public GameplayActiveEffectSnapshot(
            GameplayEffectHandle handle,
            GameplayEffectInstanceId instanceId,
            ulong startTick,
            ulong endTick,
            ulong nextPeriodTick,
            int stackCount,
            bool inhibited,
            ulong lifecycleRevision)
        {
            Handle = handle;
            InstanceId = instanceId;
            StartTick = startTick;
            EndTick = endTick;
            NextPeriodTick = nextPeriodTick;
            StackCount = stackCount;
            Inhibited = inhibited;
            LifecycleRevision = lifecycleRevision;
        }

        public GameplayEffectHandle Handle { get; }
        public GameplayEffectInstanceId InstanceId { get; }
        public ulong StartTick { get; }
        public ulong EndTick { get; }
        public ulong NextPeriodTick { get; }
        public int StackCount { get; }
        public bool Inhibited { get; }
        public ulong LifecycleRevision { get; }
    }

    internal enum GameplayEffectExecutionTrigger : byte
    {
        Instant,
        Period
    }

    internal interface IGameplayEffectComponentRuntime
    {
        IGameplayTagReader TagReader { get; }
        IGameplayAttributeReader AttributeReader { get; }
        bool TryResolveMagnitude(GameplayEffectSpec spec, GameplayMagnitudeData magnitude, out float value, out GameplayAttributeId liveAttribute);
        bool MutateBase(GameplayAttributeMutation mutation, GameplayEffectHandle causeEffect);
        bool AddModifier(
            ActiveGameplayEffect activeEffect,
            GameplayAttributeId attributeId,
            GameplayModifierOperation operation,
            GameplayMagnitudeData magnitude,
            int priority,
            GameplayClampBound clampBound,
            bool scaleWithStack);
        void GrantTags(ActiveGameplayEffect activeEffect, IReadOnlyList<GameplayTagId> tags);
        void ApplyAdditionalEffect(GameplayEffectComponentContext context, GameplayEffectId effectId);
        void EmitCue(GameplayEffectComponentContext context, string cueId, GameplayCueTrigger trigger);
    }

    internal readonly struct GameplayEffectComponentContext
    {
        public GameplayEffectComponentContext(
            IGameplayEffectComponentRuntime runtime,
            GameplayEffectSpec spec,
            ActiveGameplayEffect activeEffect,
            GameplayEffectHandle handle,
            GameplayEffectInstanceId instanceId,
            int stackCount,
            GameplayEffectExecutionTrigger executionTrigger,
            GameplayEffectLifecycleOperation lifecycleOperation)
        {
            Runtime = runtime;
            Spec = spec;
            ActiveEffect = activeEffect;
            Handle = handle;
            InstanceId = instanceId;
            StackCount = stackCount;
            ExecutionTrigger = executionTrigger;
            LifecycleOperation = lifecycleOperation;
        }

        public IGameplayEffectComponentRuntime Runtime { get; }
        public GameplayEffectSpec Spec { get; }
        public ActiveGameplayEffect ActiveEffect { get; }
        public GameplayEffectHandle Handle { get; }
        public GameplayEffectInstanceId InstanceId { get; }
        public int StackCount { get; }
        public GameplayEffectExecutionTrigger ExecutionTrigger { get; }
        public GameplayEffectLifecycleOperation LifecycleOperation { get; }
    }

    internal abstract class GameplayEffectComponentData
    {
        public virtual bool CanApply(GameplayEffectComponentContext context, out string reason)
        {
            reason = string.Empty;
            return true;
        }

        public virtual bool OngoingRequirementsMet(GameplayEffectComponentContext context)
        {
            return true;
        }

        public virtual bool RemovalRequirementMet(GameplayEffectComponentContext context)
        {
            return false;
        }

        public virtual void OnPersistentActivated(GameplayEffectComponentContext context) { }
        public virtual void OnApplied(GameplayEffectComponentContext context) { }
        public virtual void OnExecute(GameplayEffectComponentContext context) { }
        public virtual void OnWhileActive(GameplayEffectComponentContext context) { }
        public virtual void OnRemoved(GameplayEffectComponentContext context) { }
        public virtual void OnOverflow(GameplayEffectComponentContext context) { }
    }

    internal sealed class GameplayModifierComponentData : GameplayEffectComponentData
    {
        readonly GameplayAttributeId m_AttributeId;
        readonly GameplayModifierApplication m_Application;
        readonly GameplayModifierOperation m_Operation;
        readonly GameplayMagnitudeData m_Magnitude;
        readonly int m_Priority;
        readonly GameplayClampBound m_ClampBound;
        readonly bool m_ScaleWithStack;

        public GameplayModifierComponentData(
            GameplayAttributeId attributeId,
            GameplayModifierApplication application,
            GameplayModifierOperation operation,
            GameplayMagnitudeData magnitude,
            int priority,
            GameplayClampBound clampBound,
            bool scaleWithStack)
        {
            m_AttributeId = attributeId;
            m_Application = application;
            m_Operation = operation;
            m_Magnitude = magnitude;
            m_Priority = priority;
            m_ClampBound = clampBound;
            m_ScaleWithStack = scaleWithStack;
        }

        public override void OnPersistentActivated(GameplayEffectComponentContext context)
        {
            if (m_Application == GameplayModifierApplication.CurrentValue && context.ActiveEffect != null)
                context.Runtime.AddModifier(context.ActiveEffect, m_AttributeId, m_Operation, m_Magnitude, m_Priority, m_ClampBound, m_ScaleWithStack);
        }

        public override void OnExecute(GameplayEffectComponentContext context)
        {
            if (m_Application != GameplayModifierApplication.BaseValue ||
                !context.Runtime.TryResolveMagnitude(context.Spec, m_Magnitude, out float magnitude, out _))
                return;
            if (m_ScaleWithStack)
                magnitude *= context.StackCount;
            context.Runtime.MutateBase(new GameplayAttributeMutation(m_AttributeId, m_Operation, magnitude, m_ClampBound), context.Handle);
        }
    }

    internal sealed class GrantedTagsComponentData : GameplayEffectComponentData
    {
        readonly GameplayTagId[] m_Tags;

        public GrantedTagsComponentData(GameplayTagId[] tags)
        {
            m_Tags = tags;
        }

        public override void OnPersistentActivated(GameplayEffectComponentContext context)
        {
            if (context.ActiveEffect != null)
                context.Runtime.GrantTags(context.ActiveEffect, m_Tags);
        }
    }

    internal sealed class GameplayTagRequirementsComponentData : GameplayEffectComponentData
    {
        readonly GameplayEffectRequirementPhase m_Phase;
        readonly GameplayTagQuery m_Source;
        readonly GameplayTagQuery m_Target;

        public GameplayTagRequirementsComponentData(GameplayEffectRequirementPhase phase, GameplayTagQuery source, GameplayTagQuery target)
        {
            m_Phase = phase;
            m_Source = source;
            m_Target = target;
        }

        public override bool CanApply(GameplayEffectComponentContext context, out string reason)
        {
            if (m_Phase != GameplayEffectRequirementPhase.Application || Evaluate(context))
            {
                reason = string.Empty;
                return true;
            }
            reason = "TagRequirementFailed";
            return false;
        }

        public override bool OngoingRequirementsMet(GameplayEffectComponentContext context)
        {
            return m_Phase != GameplayEffectRequirementPhase.Ongoing || Evaluate(context);
        }

        public override bool RemovalRequirementMet(GameplayEffectComponentContext context)
        {
            return m_Phase == GameplayEffectRequirementPhase.Removal && Evaluate(context);
        }

        bool Evaluate(GameplayEffectComponentContext context)
        {
            bool source = m_Source == null || m_Source.IsEmpty || context.Runtime.TagReader.Matches(m_Source, context.Spec.SourceTagSnapshot);
            bool target = m_Target == null || m_Target.IsEmpty || context.Runtime.TagReader.Matches(m_Target);
            return source && target;
        }
    }

    internal sealed class GameplayAttributeRequirementsComponentData : GameplayEffectComponentData
    {
        readonly GameplayEffectRequirementPhase m_Phase;
        readonly GameplayEffectAttributeSource m_Source;
        readonly GameplayAttributeId m_AttributeId;
        readonly GameplayAttributeComparison m_Comparison;
        readonly GameplayMagnitudeData m_Threshold;

        public GameplayAttributeRequirementsComponentData(
            GameplayEffectRequirementPhase phase,
            GameplayEffectAttributeSource source,
            GameplayAttributeId attributeId,
            GameplayAttributeComparison comparison,
            GameplayMagnitudeData threshold)
        {
            m_Phase = phase;
            m_Source = source;
            m_AttributeId = attributeId;
            m_Comparison = comparison;
            m_Threshold = threshold;
        }

        public override bool CanApply(GameplayEffectComponentContext context, out string reason)
        {
            if (m_Phase != GameplayEffectRequirementPhase.Application || Evaluate(context))
            {
                reason = string.Empty;
                return true;
            }
            reason = "AttributeRequirementFailed";
            return false;
        }

        public override bool OngoingRequirementsMet(GameplayEffectComponentContext context)
        {
            return m_Phase != GameplayEffectRequirementPhase.Ongoing || Evaluate(context);
        }

        public override bool RemovalRequirementMet(GameplayEffectComponentContext context)
        {
            return m_Phase == GameplayEffectRequirementPhase.Removal && Evaluate(context);
        }

        bool Evaluate(GameplayEffectComponentContext context)
        {
            float value;
            if (m_Source == GameplayEffectAttributeSource.SourceSnapshot)
            {
                if (!context.Spec.TryGetSourceAttribute(m_AttributeId, out value))
                    return false;
            }
            else if (!context.Runtime.AttributeReader.TryGetValue(m_AttributeId, out GameplayAttributeValue attribute))
            {
                return false;
            }
            else
            {
                value = attribute.CurrentValue;
            }

            if (!context.Runtime.TryResolveMagnitude(context.Spec, m_Threshold, out float threshold, out _))
                return false;
            return Compare(value, threshold);
        }

        bool Compare(float value, float threshold)
        {
            switch (m_Comparison)
            {
                case GameplayAttributeComparison.Less: return value < threshold;
                case GameplayAttributeComparison.LessOrEqual: return value <= threshold;
                case GameplayAttributeComparison.Equal: return Mathf.Approximately(value, threshold);
                case GameplayAttributeComparison.GreaterOrEqual: return value >= threshold;
                case GameplayAttributeComparison.Greater: return value > threshold;
                case GameplayAttributeComparison.NotEqual: return !Mathf.Approximately(value, threshold);
                default: return false;
            }
        }
    }

    internal readonly struct GameplayExecutionMutationData
    {
        public GameplayExecutionMutationData(
            GameplayAttributeId attributeId,
            GameplayModifierOperation operation,
            GameplayMagnitudeData magnitude,
            GameplayClampBound clampBound)
        {
            AttributeId = attributeId;
            Operation = operation;
            Magnitude = magnitude;
            ClampBound = clampBound;
        }
        public GameplayAttributeId AttributeId { get; }
        public GameplayModifierOperation Operation { get; }
        public GameplayMagnitudeData Magnitude { get; }
        public GameplayClampBound ClampBound { get; }
    }

    internal sealed class GameplayEffectExecutionComponentData : GameplayEffectComponentData
    {
        readonly GameplayExecutionMutationData[] m_Mutations;

        public GameplayEffectExecutionComponentData(GameplayExecutionMutationData[] mutations)
        {
            m_Mutations = mutations;
        }

        public override void OnExecute(GameplayEffectComponentContext context)
        {
            for (int i = 0; i < m_Mutations.Length; i++)
            {
                GameplayExecutionMutationData mutation = m_Mutations[i];
                if (!context.Runtime.TryResolveMagnitude(context.Spec, mutation.Magnitude, out float magnitude, out _))
                    continue;
                magnitude *= context.StackCount;
                context.Runtime.MutateBase(
                    new GameplayAttributeMutation(mutation.AttributeId, mutation.Operation, magnitude, mutation.ClampBound),
                    context.Handle);
            }
        }
    }

    internal readonly struct GameplayAdditionalEffectData
    {
        public GameplayAdditionalEffectData(GameplayAdditionalEffectTrigger trigger, GameplayEffectId effectId)
        {
            Trigger = trigger;
            EffectId = effectId;
        }
        public GameplayAdditionalEffectTrigger Trigger { get; }
        public GameplayEffectId EffectId { get; }
    }

    internal sealed class AdditionalEffectsComponentData : GameplayEffectComponentData
    {
        readonly GameplayAdditionalEffectData[] m_Effects;

        public AdditionalEffectsComponentData(GameplayAdditionalEffectData[] effects)
        {
            m_Effects = effects;
        }

        public override void OnApplied(GameplayEffectComponentContext context)
        {
            Apply(context, GameplayAdditionalEffectTrigger.Applied);
        }

        public override void OnExecute(GameplayEffectComponentContext context)
        {
            if (context.ExecutionTrigger == GameplayEffectExecutionTrigger.Period)
                Apply(context, GameplayAdditionalEffectTrigger.Period);
        }

        public override void OnRemoved(GameplayEffectComponentContext context)
        {
            Apply(context, GameplayAdditionalEffectTrigger.Removed);
        }

        public override void OnOverflow(GameplayEffectComponentContext context)
        {
            Apply(context, GameplayAdditionalEffectTrigger.Overflow);
        }

        void Apply(GameplayEffectComponentContext context, GameplayAdditionalEffectTrigger trigger)
        {
            for (int i = 0; i < m_Effects.Length; i++)
            {
                if (m_Effects[i].Trigger == trigger)
                    context.Runtime.ApplyAdditionalEffect(context, m_Effects[i].EffectId);
            }
        }
    }

    internal sealed class GameplayCueBindingComponentData : GameplayEffectComponentData
    {
        readonly string m_CueId;
        readonly GameplayCueTrigger m_Trigger;

        public GameplayCueBindingComponentData(string cueId, GameplayCueTrigger trigger)
        {
            m_CueId = cueId;
            m_Trigger = trigger;
        }

        public override void OnApplied(GameplayEffectComponentContext context)
        {
            if (m_Trigger == GameplayCueTrigger.OnActive)
                context.Runtime.EmitCue(context, m_CueId, m_Trigger);
        }

        public override void OnExecute(GameplayEffectComponentContext context)
        {
            if (m_Trigger == GameplayCueTrigger.Executed)
                context.Runtime.EmitCue(context, m_CueId, m_Trigger);
        }

        public override void OnWhileActive(GameplayEffectComponentContext context)
        {
            if (m_Trigger == GameplayCueTrigger.WhileActive)
                context.Runtime.EmitCue(context, m_CueId, m_Trigger);
        }

        public override void OnRemoved(GameplayEffectComponentContext context)
        {
            GameplayCueTrigger trigger = context.LifecycleOperation == GameplayEffectLifecycleOperation.Expired
                ? GameplayCueTrigger.Expired
                : GameplayCueTrigger.Removed;
            if (m_Trigger == trigger)
                context.Runtime.EmitCue(context, m_CueId, trigger);
        }
    }
}
