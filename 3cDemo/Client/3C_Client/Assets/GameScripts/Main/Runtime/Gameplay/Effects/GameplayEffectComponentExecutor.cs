using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Contracts;
using ThirdPersonGameplay.Tags;

namespace ThirdPersonGameplay.Effects
{
    internal sealed class GameplayEffectComponentExecutor : IGameplayEffectComponentRuntime
    {
        readonly GameplayEffectRuntimeState m_State;
        readonly GameplayEffectSpecFactory m_SpecFactory;
        readonly GameplayEffectChangeRecorder m_Changes;
        readonly GameplayEffectPredictionJournalService m_Prediction;
        readonly GameplayEffectMutationTransaction m_Transaction;

        public GameplayEffectComponentExecutor(
            GameplayEffectRuntimeState state,
            GameplayEffectSpecFactory specFactory,
            GameplayEffectChangeRecorder changes,
            GameplayEffectPredictionJournalService prediction,
            GameplayEffectMutationTransaction transaction)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            m_SpecFactory = specFactory ?? throw new ArgumentNullException(nameof(specFactory));
            m_Changes = changes ?? throw new ArgumentNullException(nameof(changes));
            m_Prediction = prediction ?? throw new ArgumentNullException(nameof(prediction));
            m_Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        }

        public IGameplayTagReader TagReader => m_State.Tags;
        public IGameplayAttributeReader AttributeReader => m_State.Attributes;

        public bool CanApply(GameplayEffectSpec spec, out string reason)
        {
            GameplayEffectComponentContext context = CreateContext(
                spec,
                null,
                default,
                default,
                1,
                GameplayEffectExecutionTrigger.Instant,
                GameplayEffectLifecycleOperation.Applied);
            for (int i = 0; i < spec.Definition.Components.Length; i++)
            {
                GameplayEffectComponentData component = spec.Definition.Components[i];
                if (component != null && !component.CanApply(context, out reason))
                    return false;
            }
            reason = string.Empty;
            return true;
        }

        public bool OngoingRequirementsMet(ActiveGameplayEffect active)
        {
            GameplayEffectComponentContext context = CreateContext(active, GameplayEffectExecutionTrigger.Period, GameplayEffectLifecycleOperation.Applied);
            for (int i = 0; i < active.Spec.Definition.Components.Length; i++)
            {
                GameplayEffectComponentData component = active.Spec.Definition.Components[i];
                if (component != null && !component.OngoingRequirementsMet(context))
                    return false;
            }
            return true;
        }

        public bool RemovalRequirementMet(ActiveGameplayEffect active)
        {
            GameplayEffectComponentContext context = CreateContext(active, GameplayEffectExecutionTrigger.Period, GameplayEffectLifecycleOperation.Applied);
            for (int i = 0; i < active.Spec.Definition.Components.Length; i++)
            {
                GameplayEffectComponentData component = active.Spec.Definition.Components[i];
                if (component != null && component.RemovalRequirementMet(context))
                    return true;
            }
            return false;
        }

        public void ActivatePersistent(ActiveGameplayEffect active)
        {
            GameplayEffectComponentContext context = CreateContext(active, GameplayEffectExecutionTrigger.Period, GameplayEffectLifecycleOperation.Applied);
            for (int i = 0; i < active.Spec.Definition.Components.Length; i++)
                active.Spec.Definition.Components[i]?.OnPersistentActivated(context);
        }

        public void DeactivatePersistent(ActiveGameplayEffect active)
        {
            m_State.Attributes.RemoveModifiersByEffect(active.Handle);
            active.ModifierHandles.Clear();
            m_State.Tags.RemoveSource(GameplayTagSourceHandle.ActiveEffect(active.Handle.Value));
            active.GrantedTags.Clear();
        }

        public void InvokeApplied(GameplayEffectSpec spec, GameplayEffectComponentContext context)
        {
            for (int i = 0; i < spec.Definition.Components.Length; i++)
                spec.Definition.Components[i]?.OnApplied(context);
        }

        public void InvokeExecute(GameplayEffectSpec spec, GameplayEffectComponentContext context)
        {
            for (int i = 0; i < spec.Definition.Components.Length; i++)
                spec.Definition.Components[i]?.OnExecute(context);
        }

        public void InvokeWhileActive(ActiveGameplayEffect active)
        {
            GameplayEffectComponentContext context = CreateContext(active, GameplayEffectExecutionTrigger.Period, GameplayEffectLifecycleOperation.Applied);
            for (int i = 0; i < active.Spec.Definition.Components.Length; i++)
                active.Spec.Definition.Components[i]?.OnWhileActive(context);
        }

        public void InvokeRemoved(ActiveGameplayEffect active, GameplayEffectLifecycleOperation operation)
        {
            GameplayEffectComponentContext context = CreateContext(active, GameplayEffectExecutionTrigger.Period, operation);
            for (int i = 0; i < active.Spec.Definition.Components.Length; i++)
                active.Spec.Definition.Components[i]?.OnRemoved(context);
        }

        public void InvokeOverflow(GameplayEffectSpec spec, ActiveGameplayEffect active)
        {
            GameplayEffectComponentContext context = CreateContext(active, GameplayEffectExecutionTrigger.Period, GameplayEffectLifecycleOperation.Overflow);
            for (int i = 0; i < spec.Definition.Components.Length; i++)
                spec.Definition.Components[i]?.OnOverflow(context);
        }

        public GameplayEffectComponentContext CreateContext(
            ActiveGameplayEffect active,
            GameplayEffectExecutionTrigger trigger,
            GameplayEffectLifecycleOperation operation)
        {
            return CreateContext(
                active.Spec,
                active,
                active.Handle,
                active.InstanceId,
                active.StackCount,
                trigger,
                operation);
        }

        public GameplayEffectComponentContext CreateContext(
            GameplayEffectSpec spec,
            ActiveGameplayEffect active,
            GameplayEffectHandle handle,
            GameplayEffectInstanceId instanceId,
            int stackCount,
            GameplayEffectExecutionTrigger trigger,
            GameplayEffectLifecycleOperation operation)
        {
            return new GameplayEffectComponentContext(this, spec, active, handle, instanceId, stackCount, trigger, operation);
        }

        bool IGameplayEffectComponentRuntime.TryResolveMagnitude(
            GameplayEffectSpec spec,
            GameplayMagnitudeData magnitude,
            out float value,
            out GameplayAttributeId liveAttribute)
        {
            return m_SpecFactory.TryResolveMagnitude(spec, magnitude, out value, out liveAttribute);
        }

        bool IGameplayEffectComponentRuntime.MutateBase(GameplayAttributeMutation mutation, GameplayEffectHandle causeEffect)
        {
            m_Prediction.CaptureBefore(mutation.AttributeId);
            return m_State.Attributes.MutateBase(mutation, causeEffect);
        }

        bool IGameplayEffectComponentRuntime.AddModifier(
            ActiveGameplayEffect activeEffect,
            GameplayAttributeId attributeId,
            GameplayModifierOperation operation,
            GameplayMagnitudeData magnitude,
            int priority,
            GameplayClampBound clampBound,
            bool scaleWithStack)
        {
            if (activeEffect == null ||
                !m_SpecFactory.TryResolveMagnitude(activeEffect.Spec, magnitude, out float value, out GameplayAttributeId liveAttribute))
                return false;
            float scale = scaleWithStack ? activeEffect.StackCount : 1f;
            if (!liveAttribute.IsValid)
                value *= scale;
            if (!m_State.Attributes.AddModifier(
                    activeEffect.Handle,
                    attributeId,
                    operation,
                    value,
                    priority,
                    clampBound,
                    liveAttribute,
                    magnitude.Coefficient * scale,
                    magnitude.PostAdd * scale,
                    out GameplayModifierHandle handle))
                return false;
            activeEffect.ModifierHandles.Add(handle);
            return true;
        }

        void IGameplayEffectComponentRuntime.GrantTags(ActiveGameplayEffect activeEffect, IReadOnlyList<GameplayTagId> tags)
        {
            if (activeEffect == null || tags == null)
                return;
            for (int i = 0; i < tags.Count; i++)
            {
                if (!activeEffect.GrantedTags.Contains(tags[i]))
                    activeEffect.GrantedTags.Add(tags[i]);
            }
            m_State.Tags.SetSourceTags(GameplayTagSourceHandle.ActiveEffect(activeEffect.Handle.Value), activeEffect.GrantedTags);
        }

        void IGameplayEffectComponentRuntime.ApplyAdditionalEffect(GameplayEffectComponentContext context, GameplayEffectId effectId)
        {
            if (!effectId.IsValid)
                return;
            m_Transaction.EnqueueAdditionalEffect(new GameplayEffectPendingApplication(
                new GameplayEffectApplyRequest(
                    effectId,
                    context.Spec.Context,
                    context.Spec.CopySetByCallerValues(),
                    null,
                    context.Spec.SourceTagSnapshot),
                context.Spec.EffectId,
                context.InstanceId,
                context.LifecycleOperation));
        }

        void IGameplayEffectComponentRuntime.EmitCue(GameplayEffectComponentContext context, string cueId, GameplayCueTrigger trigger)
        {
            m_Changes.AddCue(context, cueId, trigger);
            m_Prediction.TrackCue(cueId);
        }
    }
}
