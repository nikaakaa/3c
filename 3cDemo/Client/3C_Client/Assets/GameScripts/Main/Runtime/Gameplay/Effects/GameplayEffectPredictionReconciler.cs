using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Attributes;

namespace ThirdPersonGameplay.Effects
{
    internal sealed class GameplayEffectPredictionReconciler
    {
        readonly GameplayEffectRuntimeState m_State;
        readonly GameplayEffectApplicationService m_Application;
        readonly GameplayEffectComponentExecutor m_Components;
        readonly GameplayEffectChangeRecorder m_Changes;

        public GameplayEffectPredictionReconciler(
            GameplayEffectRuntimeState state,
            GameplayEffectApplicationService application,
            GameplayEffectComponentExecutor components,
            GameplayEffectChangeRecorder changes)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            m_Application = application ?? throw new ArgumentNullException(nameof(application));
            m_Components = components ?? throw new ArgumentNullException(nameof(components));
            m_Changes = changes ?? throw new ArgumentNullException(nameof(changes));
        }

        public GameplayEffectReconcileResult Reconcile(GameplayEffectAuthorityInput input)
        {
            if (input == null)
                return GameplayEffectReconcileResult.InvalidInput;
            bool root = m_Application.BeginMutationTransaction();
            bool commit = false;
            try
            {
                GameplayEffectReconcileResult result = ReconcileCore(input);
                if (root && !m_Application.CommitPendingMutations(out _, out _))
                    return GameplayEffectReconcileResult.InvalidInput;
                commit = true;
                return result;
            }
            finally
            {
                m_Application.EndMutationTransaction(commit);
            }
        }

        GameplayEffectReconcileResult ReconcileCore(GameplayEffectAuthorityInput input)
        {
            switch (input.Kind)
            {
                case GameplayEffectAuthorityInputKind.AttributeValue:
                    return ReconcileAttribute(input);
                case GameplayEffectAuthorityInputKind.ConfirmPrediction:
                    return ConfirmPrediction(input);
                case GameplayEffectAuthorityInputKind.RejectPrediction:
                    return RejectPrediction(ResolvePredictionKey(input));
                case GameplayEffectAuthorityInputKind.CorrectPrediction:
                {
                    GameplayEffectReconcileResult rollback = RejectPrediction(ResolvePredictionKey(input));
                    GameplayEffectReconcileResult correction = ReconcileLifecycle(input, true);
                    return correction == GameplayEffectReconcileResult.Applied ? correction : rollback;
                }
                case GameplayEffectAuthorityInputKind.Lifecycle:
                    return ReconcileLifecycle(input, false);
                default:
                    return GameplayEffectReconcileResult.InvalidInput;
            }
        }

        GameplayEffectReconcileResult ConfirmPrediction(GameplayEffectAuthorityInput input)
        {
            ulong predictionKey = ResolvePredictionKey(input);
            if (predictionKey == 0 ||
                !m_State.PredictionJournal.TryGet(predictionKey, out IReadOnlyList<GameplayEffectPredictionRecord> records))
                return GameplayEffectReconcileResult.PredictionNotFound;
            bool found = false;
            for (int i = 0; i < records.Count; i++)
            {
                GameplayEffectPredictionRecord record = records[i];
                if (input.EffectId.IsValid && record.EffectId != input.EffectId)
                    continue;
                found = true;
                GameplayEffectInstanceId instanceId = record.InstanceId;
                ulong revision = input.LifecycleRevision > 0 ? input.LifecycleRevision : 1;
                if (m_State.ActiveEffects.TryGet(record.Handle, out ActiveGameplayEffect active))
                {
                    if (input.InstanceId.IsValid && input.InstanceId != active.InstanceId &&
                        !m_State.ActiveEffects.UpdateInstanceId(active, input.InstanceId))
                        return GameplayEffectReconcileResult.Conflict;
                    active.LifecycleRevision = Math.Max(active.LifecycleRevision, revision);
                    instanceId = active.InstanceId;
                    m_Changes.AddLifecycle(active, GameplayEffectLifecycleOperation.Confirmed);
                }
                else
                {
                    m_Changes.AddLifecycle(
                        record.Spec,
                        input.InstanceId.IsValid ? input.InstanceId : instanceId,
                        GameplayEffectLifecycleOperation.Confirmed,
                        m_State.CurrentTick,
                        m_State.CurrentTick,
                        1,
                        revision,
                        true);
                }
            }
            if (!found)
                return GameplayEffectReconcileResult.PredictionNotFound;
            m_State.PredictionJournal.MarkConfirmed(predictionKey, input.EffectId);
            m_Changes.DrainStateChanges();
            return GameplayEffectReconcileResult.Applied;
        }

        GameplayEffectReconcileResult RejectPrediction(ulong predictionKey)
        {
            if (predictionKey == 0 ||
                !m_State.PredictionJournal.Remove(predictionKey, out List<GameplayEffectPredictionRecord> records))
                return GameplayEffectReconcileResult.PredictionNotFound;
            bool conflict = false;
            for (int i = records.Count - 1; i >= 0; i--)
            {
                GameplayEffectPredictionRecord record = records[i];
                if (record.CreatedActive && m_State.ActiveEffects.TryGet(record.Handle, out ActiveGameplayEffect created))
                {
                    m_Components.DeactivatePersistent(created);
                    m_State.ActiveEffects.Remove(created);
                }
                else if (record.HasActiveBefore && m_State.ActiveEffects.TryGet(record.Handle, out ActiveGameplayEffect stacked))
                {
                    m_Components.DeactivatePersistent(stacked);
                    m_Application.RestoreActive(stacked, record.ActiveBefore);
                }

                foreach (KeyValuePair<GameplayAttributeId, GameplayAttributePredictionSnapshot> pair in record.Attributes)
                {
                    if (pair.Value.AfterRevision == 0 ||
                        !m_State.Attributes.Restore(pair.Value.Before, pair.Value.AfterRevision, record.Handle))
                        conflict = true;
                }

                for (int cueIndex = 0; cueIndex < record.CueIds.Count; cueIndex++)
                {
                    GameplayEffectComponentContext context = m_Components.CreateContext(
                        record.Spec,
                        null,
                        record.Handle,
                        record.InstanceId,
                        0,
                        GameplayEffectExecutionTrigger.Instant,
                        GameplayEffectLifecycleOperation.Rejected);
                    m_Changes.AddCue(context, record.CueIds[cueIndex], GameplayCueTrigger.Removed);
                }
                m_Changes.AddLifecycle(
                    record.Spec,
                    record.InstanceId,
                    GameplayEffectLifecycleOperation.Rejected,
                    m_State.CurrentTick,
                    m_State.CurrentTick,
                    0,
                    1,
                    record.Spec.Definition.DurationPolicy == GameplayEffectDurationPolicy.Instant);
            }
            m_Changes.DrainStateChanges();
            return conflict ? GameplayEffectReconcileResult.Conflict : GameplayEffectReconcileResult.Applied;
        }

        GameplayEffectReconcileResult ReconcileAttribute(GameplayEffectAuthorityInput input)
        {
            if (input.ValueRevision == 0 ||
                !m_State.Attributes.TryGetValue(input.AttributeId, out GameplayAttributeValue current))
                return GameplayEffectReconcileResult.InvalidInput;
            if (input.ValueRevision <= current.Revision)
                return GameplayEffectReconcileResult.IgnoredStaleRevision;

            GameplayEffectHandle cause = default;
            if (input.CauseEffectInstanceId.IsValid &&
                m_State.ActiveEffects.TryGet(input.CauseEffectInstanceId, out ActiveGameplayEffect active))
            {
                cause = active.Handle;
                m_Changes.RegisterCause(active);
            }
            else if (input.EffectId.IsValid)
            {
                cause = m_State.NextHandle();
                m_Changes.RegisterCause(cause, input.EffectId, input.CauseEffectInstanceId, input.Context);
            }
            return m_State.Attributes.ApplyAuthoritativeValue(
                input.AttributeId,
                input.BaseValue,
                input.CurrentValue,
                input.ValueRevision,
                cause)
                ? GameplayEffectReconcileResult.Applied
                : GameplayEffectReconcileResult.IgnoredStaleRevision;
        }

        GameplayEffectReconcileResult ReconcileLifecycle(GameplayEffectAuthorityInput input, bool correction)
        {
            if (!input.EffectId.IsValid || !input.InstanceId.IsValid || input.LifecycleRevision == 0)
                return GameplayEffectReconcileResult.InvalidInput;
            if (!m_State.Definition.TryGetEffect(input.EffectId, out GameplayEffectDefinitionData definition))
                return GameplayEffectReconcileResult.MissingDefinition;
            if (input.DefinitionRevision != definition.DefinitionRevision)
                return GameplayEffectReconcileResult.DefinitionRevisionMismatch;
            if (m_State.LastLifecycleRevisions.TryGetValue(input.InstanceId, out ulong revision) &&
                input.LifecycleRevision <= revision)
                return GameplayEffectReconcileResult.IgnoredStaleRevision;

            GameplayEffectReconcileResult result;
            switch (input.Operation)
            {
                case GameplayEffectLifecycleOperation.Confirmed:
                    result = ConfirmPrediction(input);
                    break;
                case GameplayEffectLifecycleOperation.Rejected:
                    result = RejectPrediction(ResolvePredictionKey(input));
                    break;
                case GameplayEffectLifecycleOperation.Removed:
                case GameplayEffectLifecycleOperation.Expired:
                    if (!m_State.ActiveEffects.TryGet(input.InstanceId, out ActiveGameplayEffect remove))
                        return GameplayEffectReconcileResult.InvalidInput;
                    m_Application.RemoveActive(remove, input.Operation, true);
                    result = GameplayEffectReconcileResult.Applied;
                    break;
                case GameplayEffectLifecycleOperation.StackChanged:
                    if (!m_State.ActiveEffects.TryGet(input.InstanceId, out ActiveGameplayEffect stack))
                        return GameplayEffectReconcileResult.InvalidInput;
                    m_Components.DeactivatePersistent(stack);
                    stack.StackCount = Math.Max(1, input.StackCount);
                    stack.StartTick = input.StartTick;
                    stack.EndTick = input.EndTick;
                    stack.LifecycleRevision = input.LifecycleRevision;
                    m_Components.ActivatePersistent(stack);
                    m_Changes.AddLifecycle(stack, GameplayEffectLifecycleOperation.StackChanged);
                    result = GameplayEffectReconcileResult.Applied;
                    break;
                case GameplayEffectLifecycleOperation.Inhibited:
                case GameplayEffectLifecycleOperation.Resumed:
                    result = ReconcileInhibition(input);
                    break;
                case GameplayEffectLifecycleOperation.PeriodExecuted:
                    if (!m_State.ActiveEffects.TryGet(input.InstanceId, out ActiveGameplayEffect period))
                        return GameplayEffectReconcileResult.InvalidInput;
                    m_Application.ExecutePeriod(period, input.LifecycleRevision);
                    result = GameplayEffectReconcileResult.Applied;
                    break;
                default:
                    result = ApplyAuthoritative(input);
                    break;
            }
            if (result == GameplayEffectReconcileResult.Applied)
            {
                m_State.LastLifecycleRevisions[input.InstanceId] = input.LifecycleRevision;
                if (correction && m_State.ActiveEffects.TryGet(input.InstanceId, out ActiveGameplayEffect corrected))
                    m_Changes.AddLifecycle(corrected, GameplayEffectLifecycleOperation.Corrected);
            }
            m_Changes.DrainStateChanges();
            return result;
        }

        GameplayEffectReconcileResult ReconcileInhibition(GameplayEffectAuthorityInput input)
        {
            if (!m_State.ActiveEffects.TryGet(input.InstanceId, out ActiveGameplayEffect active))
                return GameplayEffectReconcileResult.InvalidInput;
            if (input.Operation == GameplayEffectLifecycleOperation.Inhibited && !active.Inhibited)
            {
                m_Components.DeactivatePersistent(active);
                active.Inhibited = true;
            }
            else if (input.Operation == GameplayEffectLifecycleOperation.Resumed && active.Inhibited)
            {
                active.Inhibited = false;
                m_Components.ActivatePersistent(active);
            }
            active.LifecycleRevision = input.LifecycleRevision;
            m_Changes.AddLifecycle(active, input.Operation);
            return GameplayEffectReconcileResult.Applied;
        }

        GameplayEffectReconcileResult ApplyAuthoritative(GameplayEffectAuthorityInput input)
        {
            GameplayEffectContext context = new GameplayEffectContext(
                input.Context.SourceActorId,
                input.Context.TargetActorId,
                input.Context.SourceActionInstanceId,
                input.Context.PredictionKey,
                input.Context.GameplayResultId,
                input.Context.SourceLogicTick,
                GameplayEffectApplicationMode.Confirmed);
            GameplayEffectApplyResult apply = m_Application.Apply(new GameplayEffectApplyRequest(
                input.EffectId,
                context,
                input.SetByCallerValues,
                null,
                null,
                input.InstanceId,
                input.LifecycleRevision,
                input.DefinitionRevision));
            return apply.Succeeded ? GameplayEffectReconcileResult.Applied : GameplayEffectReconcileResult.InvalidInput;
        }

        static ulong ResolvePredictionKey(GameplayEffectAuthorityInput input)
        {
            return input.PredictionKey != 0 ? input.PredictionKey : input.Context.PredictionKey;
        }
    }
}
