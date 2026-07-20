using ThirdPersonSimulation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed partial class FixedGameplayEffectTarget
    {
        ActorId IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.ActorId => m_ActorId;
        ulong IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.Tick => m_Tick.Value;
        bool IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.HasActiveEffects =>
            m_State != null ? m_State.ActiveEffects.Count > 0 : m_CommittedState.ActiveEffectCount > 0;
        int IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.ChangeCount => m_Changes.Count;

        GameplayEffectApplicationIdentity IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.DescribeApplication(SimulationGameplayEffectApplication application)
        {
            return application == null
                ? default
                : new GameplayEffectApplicationIdentity(application.EffectId, application.AuthoritativeInstanceId, application.AuthoritativeLifecycleRevision);
        }

        bool IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.TryPrepare(
            SimulationGameplayEffectApplication application,
            out GameplayEffectPreparedSpec<PortableEffectSpecState> prepared,
            out GameplayEffectApplyResult failure)
        {
            EnsureWorkingState();
            return m_Admission.TryPrepare(application, out prepared, out failure);
        }

        GameplayEffectPreparedSpec<PortableEffectSpecState> IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.DescribeSpec(PortableEffectSpecState spec) => DescribeSpec(spec);
        int IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.ComponentCount(PortableEffectSpecState spec) => spec.Definition.Components.Length;
        GameplayEffectComponentDescriptor IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.DescribeComponent(PortableEffectSpecState spec, int componentIndex) => DescribeComponent(spec.Definition.Components[componentIndex]);
        bool IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.EvaluateTagRequirement(PortableEffectSpecState spec, int componentIndex) => EvaluateTagRequirement(spec, (PortableTagRequirementsComponent)spec.Definition.Components[componentIndex]);
        bool IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.EvaluateAttributeRequirement(PortableEffectSpecState spec, int componentIndex) => EvaluateAttributeRequirement(spec, (PortableAttributeRequirementsComponent)spec.Definition.Components[componentIndex]);
        bool IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.MatchesEffectId(PortableEffectSpecState spec, string effectId) =>
            spec != null && string.Equals(spec.Definition.Id, SimulationGameplayEffectProgram.NormalizeEffect(effectId), StringComparison.Ordinal);
        bool IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.MatchesEffectTagQuery(PortableEffectSpecState spec, PortableTagQuery tagQuery) =>
            tagQuery != null && m_State.Program.Matches(tagQuery, spec.Definition.EffectTags);
        PortableActiveEffectState IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.FindActiveByHandle(ulong handle) => m_State.FindActiveByHandle(handle);
        PortableActiveEffectState IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.FindActiveByInstance(ulong instanceId) => m_State.FindActiveByInstance(instanceId);
        IReadOnlyList<PortableActiveEffectState> IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.AcquireActiveEffects()
        {
            List<PortableActiveEffectState> values = m_Scratch.ActiveEffects.Acquire();
            for (int i = 0; i < m_State.ActiveEffects.Count; i++)
                values.Add(m_State.ActiveEffects[i]);
            return values;
        }

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.ReleaseActiveEffects(IReadOnlyList<PortableActiveEffectState> activeEffects)
        {
            if (!(activeEffects is List<PortableActiveEffectState> values))
                throw new InvalidOperationException("Gameplay Effect active snapshot does not belong to the Actor workspace.");
            m_Scratch.ActiveEffects.Release(values);
        }

        PortableActiveEffectState IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.CreateActive(
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

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.AddActive(PortableActiveEffectState active) => m_State.AddActive(active);
        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.RemoveActive(PortableActiveEffectState active) => m_State.RemoveActive(active);
        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.MarkActiveEffectsDirty() => m_State.MarkActiveEffectsDirty();
        ulong IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.GetNextPeriod(ulong instanceId) => m_State.GetNextPeriod(instanceId);
        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.SetNextPeriod(ulong instanceId, ulong tick) => m_State.SetNextPeriod(instanceId, tick);
        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.DeactivatePersistent(PortableActiveEffectState active) => DeactivatePersistent(active);

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.ActivateCurrentModifier(PortableActiveEffectState active, int componentIndex) =>
            ActivateCurrentModifier(active, (PortableModifierComponent)active.Spec.Definition.Components[componentIndex]);
        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.ActivateGrantedTags(PortableActiveEffectState active) => ActivateGrantedTags(active);

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.ExecuteNumericComponent(
            GameplayEffectPreparedSpec<PortableEffectSpecState> spec,
            PortableActiveEffectState active,
            ulong handle,
            int stackCount,
            int componentIndex) => ExecuteNumericComponent(spec.TargetSpec, handle, stackCount, spec.TargetSpec.Definition.Components[componentIndex]);

        int IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.AdditionalEffectCount(PortableEffectSpecState spec, int componentIndex) =>
            ((PortableAdditionalEffectsComponent)spec.Definition.Components[componentIndex]).Effects.Length;

        GameplayEffectAdditionalTrigger IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.DescribeAdditionalEffectTrigger(
            PortableEffectSpecState spec,
            int componentIndex,
            int effectIndex) => ToCommon(((PortableAdditionalEffectsComponent)spec.Definition.Components[componentIndex]).Effects[effectIndex].Trigger);

        SimulationGameplayEffectApplication IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.BuildAdditionalApplication(
            GameplayEffectPreparedSpec<PortableEffectSpecState> spec,
            ulong instanceId,
            int componentIndex,
            int effectIndex) => BuildAdditionalApplication(
                spec.TargetSpec,
                (PortableAdditionalEffectsComponent)spec.TargetSpec.Definition.Components[componentIndex],
                effectIndex);

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.EmitCue(
            GameplayEffectPreparedSpec<PortableEffectSpecState> spec,
            ulong instanceId,
            int componentIndex,
            bool trackPrediction)
        {
            var cue = (PortableCueComponent)spec.TargetSpec.Definition.Components[componentIndex];
            AddCue(cue.CueId, cue.Trigger, spec.TargetSpec.Definition, instanceId, spec.TargetSpec.Context, trackPrediction);
        }

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.RegisterCause(
            ulong handle,
            GameplayEffectPreparedSpec<PortableEffectSpecState> spec,
            ulong instanceId) => m_Causes[handle] = new PortableEffectCause(spec.TargetSpec.Definition, instanceId, spec.TargetSpec.Context);

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.EmitLifecycle(PortableActiveEffectState active, GameplayEffectLifecycleKind lifecycle) => AddLifecycle(active, ToTarget(lifecycle));

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.EmitLifecycle(
            GameplayEffectPreparedSpec<PortableEffectSpecState> spec,
            ulong instanceId,
            GameplayEffectLifecycleKind lifecycle,
            ulong startTick,
            ulong endTick,
            int stackCount,
            ulong revision,
            bool instant) => AddLifecycle(spec.TargetSpec.Definition, instanceId, ToTarget(lifecycle), spec.TargetSpec.Context, startTick, endTick, stackCount, revision, instant);

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.EmitFailure(
            string ownerEffectId,
            ulong ownerInstanceId,
            string requestedEffectId,
            GameplayEffectApplyResult failure) => AddFailure(ownerEffectId, ownerInstanceId, requestedEffectId, ToTarget(failure.Kind), failure.Reason);

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.TrimChanges(int count) => TrimChanges(count);
        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.RebuildCauses() => RebuildCauses();

        FixedCharacterStateSavepoint IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.CreateSavepoint()
        {
            EnsureWorkingState();
            return m_Transaction.CreateSavepoint();
        }

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.Restore(FixedCharacterStateSavepoint savepoint) => m_Transaction.Restore(savepoint);
        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.Release(FixedCharacterStateSavepoint savepoint) => m_Transaction.Release(savepoint);
        bool IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.SavepointIsActive(FixedCharacterStateSavepoint savepoint) => m_Transaction.Diagnostics().SavepointDepth >= savepoint.Depth;
        ulong IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.CaptureAllocator() => m_CaptureAllocator();
        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.RestoreAllocator(ulong value) => m_RestoreAllocator(value);
        ulong IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.AllocateHandle() => m_AllocateHandle();

        PortablePredictionRecord IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.CreatePrediction(
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

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.SetCurrentPrediction(PortablePredictionRecord prediction) => m_CurrentPrediction = prediction;

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.CompletePrediction(PortablePredictionRecord prediction)
        {
            List<string> attributes = m_Scratch.PredictionAttributes.Acquire();
            try
            {
                foreach (string attribute in prediction.Attributes.Keys)
                    attributes.Add(attribute);
                for (int i = 0; i < attributes.Count; i++)
                {
                    PortableAttributeState value = m_State.RequireAttribute(attributes[i]);
                    prediction.Attributes[attributes[i]] = prediction.Attributes[attributes[i]].WithAfterRevision(value.Revision);
                }
            }
            finally
            {
                m_Scratch.PredictionAttributes.Release(attributes);
            }
        }

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.CancelPrediction(PortablePredictionRecord prediction)
        {
        }

        bool IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.TryGetPredictions(
            ulong predictionKey,
            out IReadOnlyList<PortablePredictionRecord> predictions)
        {
            if (m_State.Journal.TryGetValue(predictionKey, out List<PortablePredictionRecord> records))
            {
                predictions = records;
                return true;
            }
            predictions = null;
            return false;
        }

        IReadOnlyList<ulong> IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.AcquirePredictionKeys()
        {
            List<ulong> values = m_Scratch.PredictionKeys.Acquire();
            foreach (ulong key in m_State.Journal.Keys)
                values.Add(key);
            return values;
        }

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.ReleasePredictionKeys(IReadOnlyList<ulong> keys)
        {
            if (!(keys is List<ulong> values))
                throw new InvalidOperationException("Gameplay Effect prediction-key snapshot does not belong to the Actor workspace.");
            m_Scratch.PredictionKeys.Release(values);
        }

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.AddPrediction(PortablePredictionRecord prediction)
        {
            ulong key = prediction.Spec.Context.PredictionKey;
            if (!m_State.Journal.TryGetValue(key, out List<PortablePredictionRecord> records))
            {
                records = new List<PortablePredictionRecord>();
                m_State.Journal.Add(key, records);
            }
            records.Add(prediction);
        }

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.RemovePredictions(ulong predictionKey) => m_State.Journal.Remove(predictionKey);

        bool IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.RestorePredictionAttributes(PortablePredictionRecord prediction)
        {
            bool restored = true;
            foreach (PortablePredictionAttributeSnapshot attribute in prediction.Attributes.Values)
            {
                if (m_State.RestorePredictedAttribute(attribute, prediction.Handle, out IReadOnlyList<PortableAttributeChange> changes))
                    AddAttributeChanges(changes);
                else
                    restored = false;
            }
            return restored;
        }

        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.EmitPredictionCueRemoval(PortablePredictionRecord prediction, string cueId) =>
            AddCue(cueId, PortableCueTrigger.Removed, prediction.Spec.Definition, prediction.InstanceId, prediction.Spec.Context, false);

        bool IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.TryGetLastLifecycleRevision(ulong instanceId, out ulong revision) => m_State.LastLifecycleRevisions.TryGetValue(instanceId, out revision);
        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.SetLastLifecycleRevision(ulong instanceId, ulong revision) => m_State.LastLifecycleRevisions[instanceId] = revision;
        void IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.MarkJournalDirty() => m_State.MarkJournalDirty();

        bool IGameplayEffectControlPort<SimulationGameplayEffectApplication, PortableEffectSpecState, PortableActiveEffectState, PortablePredictionRecord, PortableTagQuery, FixedCharacterStateSavepoint>.TryEmitRejectedApplication(
            SimulationGameplayEffectApplication application,
            GameplayEffectApplyResult failure)
        {
            if (application == null || application.AuthoritativeInstanceId == 0)
                return false;
            try
            {
                PortableEffectDefinition definition = m_State.Program.RequireEffect(application.EffectId);
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
