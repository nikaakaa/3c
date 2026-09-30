using ThirdPersonSimulation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed partial class FixedGameplayEffectTarget
    {
        ActorId IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.ActorId => m_ActorId;
        ulong IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.Tick => m_Tick.Value;
        bool IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.HasActiveEffects =>
            m_State != null ? m_State.ActiveEffects.Count > 0 : m_CommittedState.ActiveEffectCount > 0;
        int IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.ChangeCount => m_Changes.Count;

        GameplayEffectApplicationIdentity IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.DescribeApplication(SimulationGameplayEffectApplication application)
        {
            return application == null
                ? default
                : new GameplayEffectApplicationIdentity(application.EffectId, application.AuthoritativeInstanceId, application.AuthoritativeLifecycleRevision);
        }

        bool IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.TryPrepare(
            SimulationGameplayEffectApplication application,
            out GameplayEffectPreparedSpec<PortableEffectSpecState> prepared,
            out GameplayEffectApplyResult failure)
        {
            EnsureWorkingState();
            return m_Admission.TryPrepare(application, out prepared, out failure);
        }

        GameplayEffectPreparedSpec<PortableEffectSpecState> IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.DescribeSpec(PortableEffectSpecState spec) => DescribeSpec(spec);
        int IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.ComponentCount(PortableEffectSpecState spec) => spec.Definition.Components.Length;
        GameplayEffectComponentDescriptor IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.DescribeComponent(PortableEffectSpecState spec, int componentIndex) => DescribeComponent(spec.Definition.Components[componentIndex]);
        bool IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.EvaluateTagRequirement(PortableEffectSpecState spec, int componentIndex) => EvaluateTagRequirement(spec, (PortableTagRequirementsComponent)spec.Definition.Components[componentIndex]);
        bool IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.EvaluateAttributeRequirement(PortableEffectSpecState spec, int componentIndex) => EvaluateAttributeRequirement(spec, (PortableAttributeRequirementsComponent)spec.Definition.Components[componentIndex]);
        bool IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.MatchesEffectId(PortableEffectSpecState spec, string effectId) =>
            spec != null && string.Equals(spec.Definition.Id, FixedGameplayEffectRuntimeCatalog.NormalizeEffect(effectId), StringComparison.Ordinal);
        bool IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.MatchesEffectTagQuery(PortableEffectSpecState spec, PortableTagQuery tagQuery) =>
            tagQuery != null && m_State.Catalog.Matches(tagQuery, spec.Definition.EffectTags);
        PortableActiveEffectState IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.FindActiveByHandle(ulong handle) => m_State.FindActiveByHandle(handle);
        PortableActiveEffectState IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.FindActiveByInstance(ulong instanceId) => m_State.FindActiveByInstance(instanceId);
        IReadOnlyList<PortableActiveEffectState> IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.AcquireActiveEffects()
        {
            List<PortableActiveEffectState> values = m_Scratch.ActiveEffects.Acquire();
            for (int i = 0; i < m_State.ActiveEffects.Count; i++)
                values.Add(m_State.ActiveEffects[i]);
            return values;
        }

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.ReleaseActiveEffects(IReadOnlyList<PortableActiveEffectState> activeEffects)
        {
            if (!(activeEffects is List<PortableActiveEffectState> values))
                throw new InvalidOperationException("Gameplay Effect active snapshot does not belong to the Actor workspace.");
            m_Scratch.ActiveEffects.Release(values);
        }

        PortableActiveEffectState IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.CreateActive(
            GameplayEffectPreparedSpec<PortableEffectSpecState> spec,
            ulong handle,
            ulong instanceId,
            ulong startTick,
            ulong endTick,
            ulong insertionSequence,
            ulong lifecycleRevision)
        {
            return new PortableActiveEffectState
            {
                Handle = handle,
                InstanceId = instanceId,
                Spec = spec.TargetSpec,
                StartTick = startTick,
                EndTick = endTick,
                InsertionSequence = insertionSequence,
                StackCount = 1,
                LifecycleRevision = lifecycleRevision
            };
        }

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.AddActive(PortableActiveEffectState active) => m_State.AddActive(active);
        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.RemoveActive(PortableActiveEffectState active) => m_State.RemoveActive(active);
        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.RefreshActiveEffectsDirty() => m_State.RefreshActiveEffectsDirty();
        ulong IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.GetNextPeriod(ulong instanceId) => m_State.GetNextPeriod(instanceId);
        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.SetNextPeriod(ulong instanceId, ulong tick) => m_State.SetNextPeriod(instanceId, tick);
        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.DeactivatePersistent(PortableActiveEffectState active) => DeactivatePersistent(active);

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.ActivateCurrentModifier(PortableActiveEffectState active, int componentIndex) =>
            ActivateCurrentModifier(active, (PortableModifierComponent)active.Spec.Definition.Components[componentIndex]);
        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.ActivateGrantedTags(PortableActiveEffectState active) => ActivateGrantedTags(active);

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.ExecuteNumericComponent(
            GameplayEffectPreparedSpec<PortableEffectSpecState> spec,
            PortableActiveEffectState active,
            ulong handle,
            int stackCount,
            int componentIndex) => ExecuteNumericComponent(spec.TargetSpec, handle, stackCount, spec.TargetSpec.Definition.Components[componentIndex]);

        int IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.AdditionalEffectCount(PortableEffectSpecState spec, int componentIndex) =>
            ((PortableAdditionalEffectsComponent)spec.Definition.Components[componentIndex]).Effects.Length;

        GameplayEffectAdditionalTrigger IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.DescribeAdditionalEffectTrigger(
            PortableEffectSpecState spec,
            int componentIndex,
            int effectIndex) => ToCommon(((PortableAdditionalEffectsComponent)spec.Definition.Components[componentIndex]).Effects[effectIndex].Trigger);

        SimulationGameplayEffectApplication IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.BuildAdditionalApplication(
            GameplayEffectPreparedSpec<PortableEffectSpecState> spec,
            ulong instanceId,
            int componentIndex,
            int effectIndex) => BuildAdditionalApplication(
                spec.TargetSpec,
                (PortableAdditionalEffectsComponent)spec.TargetSpec.Definition.Components[componentIndex],
                effectIndex);

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.EmitCue(
            GameplayEffectPreparedSpec<PortableEffectSpecState> spec,
            ulong instanceId,
            int componentIndex,
            bool trackPrediction)
        {
            var cue = (PortableCueComponent)spec.TargetSpec.Definition.Components[componentIndex];
            AddCue(cue.CueId, cue.Trigger, spec.TargetSpec.Definition, instanceId, spec.TargetSpec.Context, trackPrediction);
        }

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.RegisterCause(
            ulong handle,
            GameplayEffectPreparedSpec<PortableEffectSpecState> spec,
            ulong instanceId) => m_Causes[handle] = new PortableEffectCause(spec.TargetSpec.Definition, instanceId, spec.TargetSpec.Context);

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.EmitLifecycle(PortableActiveEffectState active, GameplayEffectLifecycleKind lifecycle) => AddLifecycle(active, ToTarget(lifecycle));

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.EmitLifecycle(
            GameplayEffectPreparedSpec<PortableEffectSpecState> spec,
            ulong instanceId,
            GameplayEffectLifecycleKind lifecycle,
            ulong startTick,
            ulong endTick,
            int stackCount,
            ulong revision,
            bool instant) => AddLifecycle(spec.TargetSpec.Definition, instanceId, ToTarget(lifecycle), spec.TargetSpec.Context, startTick, endTick, stackCount, revision, instant);

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.EmitFailure(
            string ownerEffectId,
            ulong ownerInstanceId,
            string requestedEffectId,
            GameplayEffectApplyResult failure) => AddFailure(ownerEffectId, ownerInstanceId, requestedEffectId, ToTarget(failure.Kind), failure.Reason);

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.TrimChanges(int count) => TrimChanges(count);
        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.RebuildCauses() => RebuildCauses();

        IFixedAbilityExecutionSavepoint IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.CreateSavepoint()
        {
            EnsureWorkingState();
            return m_SavepointPort.CreateSavepoint();
        }

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.Restore(IFixedAbilityExecutionSavepoint savepoint) => m_SavepointPort.Restore(savepoint);
        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.Release(IFixedAbilityExecutionSavepoint savepoint) => m_SavepointPort.Release(savepoint);
        bool IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.SavepointIsActive(IFixedAbilityExecutionSavepoint savepoint) => m_SavepointPort.SavepointDepth >= savepoint.Depth;
        ulong IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.CaptureAllocator() => m_CaptureAllocator();
        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.RestoreAllocator(ulong value) => m_RestoreAllocator(value);
        ulong IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.AllocateHandle() => m_AllocateHandle();

        PortablePredictionRecord IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.CreatePrediction(
            GameplayEffectPreparedSpec<PortableEffectSpecState> spec,
            ulong handle,
            ulong instanceId,
            bool createdActive,
            bool hasActiveBefore,
            GameplayEffectActiveControlSnapshot activeBefore)
        {
            return new PortablePredictionRecord
            {
                Spec = spec.TargetSpec,
                Handle = handle,
                InstanceId = instanceId,
                CreatedActive = createdActive,
                HasActiveBefore = hasActiveBefore,
                ActiveBefore = hasActiveBefore
                    ? new GameplayEffectActiveControlSnapshot(
                        activeBefore.InstanceId,
                        activeBefore.StartTick,
                        activeBefore.EndTick,
                        activeBefore.NextPeriodTick,
                        activeBefore.StackCount,
                        activeBefore.Inhibited,
                        activeBefore.LifecycleRevision)
                    : default
            };
        }

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.SetCurrentPrediction(PortablePredictionRecord prediction) => m_CurrentPrediction = prediction;

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.CompletePrediction(PortablePredictionRecord prediction)
        {
            for (int i = 0; i < prediction.Attributes.Count; i++)
            {
                string attribute = prediction.Attributes.Keys[i];
                PortableAttributeState value = m_State.RequireAttribute(attribute);
                prediction.Attributes[attribute] = prediction.Attributes.Values[i].WithAfterRevision(value.Revision);
            }
        }

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.CancelPrediction(PortablePredictionRecord prediction)
        {
        }

        bool IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.TryGetPredictions(
            ulong predictionKey,
            out IReadOnlyList<PortablePredictionRecord> predictions)
        {
            return m_State.TryGetJournalRecords(predictionKey, out predictions);
        }

        IReadOnlyList<ulong> IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.AcquirePredictionKeys()
        {
            List<ulong> values = m_Scratch.PredictionKeys.Acquire();
            m_State.CopyJournalKeys(values);
            return values;
        }

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.ReleasePredictionKeys(IReadOnlyList<ulong> keys)
        {
            if (!(keys is List<ulong> values))
                throw new InvalidOperationException("Gameplay Effect prediction-key snapshot does not belong to the Actor workspace.");
            m_Scratch.PredictionKeys.Release(values);
        }

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.AddPrediction(PortablePredictionRecord prediction)
        {
            m_State.AddJournalRecord(prediction);
        }

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.RemovePredictions(ulong predictionKey) => m_State.RemoveJournalRecords(predictionKey);

        bool IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.RestorePredictionAttributes(PortablePredictionRecord prediction)
        {
            bool restored = true;
            for (int i = 0; i < prediction.Attributes.Count; i++)
            {
                PortablePredictionAttributeSnapshot attribute = prediction.Attributes.Values[i];
                if (m_State.RestorePredictedAttribute(attribute, prediction.Handle, out IReadOnlyList<PortableAttributeChange> changes))
                    AddAttributeChanges(changes);
                else
                    restored = false;
            }
            return restored;
        }

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.EmitPredictionCueRemoval(PortablePredictionRecord prediction, string cueId) =>
            AddCue(cueId, PortableCueTrigger.Removed, prediction.Spec.Definition, prediction.InstanceId, prediction.Spec.Context, false);

        bool IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.TryGetLastLifecycleRevision(ulong instanceId, out ulong revision) => m_State.TryGetLastLifecycleRevision(instanceId, out revision);
        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.SetLastLifecycleRevision(ulong instanceId, ulong revision) => m_State.SetLastLifecycleRevision(instanceId, revision);
        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.RefreshJournalDirty() => m_State.RefreshJournalDirty();

        bool IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, IFixedAbilityExecutionSavepoint>.TryEmitRejectedApplication(
            SimulationGameplayEffectApplication application,
            GameplayEffectApplyResult failure)
        {
            if (application == null || application.AuthoritativeInstanceId == 0)
                return false;
            try
            {
                PortableEffectDefinition definition = m_State.Catalog.RequireEffect(application.EffectId);
                AddLifecycle(
                    definition,
                    application.AuthoritativeInstanceId,
                    SimulationGameplayEffectLifecycleOperation.Rejected,
                    application.Context,
                    m_Tick.Value,
                    m_Tick.Value,
                    0,
                    Math.Max(1UL, application.AuthoritativeLifecycleRevision),
                    definition.DurationPolicy == PortableEffectDurationPolicy.Instant);
                return true;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        }

    }
}
