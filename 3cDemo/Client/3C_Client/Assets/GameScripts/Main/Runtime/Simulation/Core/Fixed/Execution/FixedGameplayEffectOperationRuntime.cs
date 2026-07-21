using ThirdPersonSimulation;
using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedGameplayEffectOperationRuntime : FixedOperationModule,
        IFixedGameplayTagQuery,
        IFixedGameplayEffectActionPort
    {
        readonly FixedEvaluationFrame m_Frame;
        readonly IFixedActionContextReader m_Actions;
        readonly FixedHandleAllocator m_Handles;
        readonly FixedFactSink m_Facts;
        readonly FixedPresentationSink m_Presentation;
        readonly FixedTraceSink m_Trace;
        readonly FixedGameplayEffectExecutionScratch m_Scratch;
        FixedGameplayEffectTarget m_GameplayEffects;

        public FixedGameplayEffectOperationRuntime(
            FixedProgramAccess access,
            FixedEvaluationFrame frame,
            IFixedActionContextReader actions,
            FixedHandleAllocator handles,
            FixedFactSink facts,
            FixedPresentationSink presentation,
            FixedTraceSink trace,
            FixedGameplayEffectExecutionScratch scratch)
            : base(access)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            m_Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            m_Handles = handles ?? throw new ArgumentNullException(nameof(handles));
            m_Facts = facts ?? throw new ArgumentNullException(nameof(facts));
            m_Presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            m_Trace = trace ?? throw new ArgumentNullException(nameof(trace));
            m_Scratch = scratch ?? throw new ArgumentNullException(nameof(scratch));
        }

        public void BeginEvaluation()
        {
            if (m_GameplayEffects != null)
                throw new InvalidOperationException("Gameplay Effect evaluation is already active.");
            m_GameplayEffects = new FixedGameplayEffectTarget(
                m_Frame.Transaction,
                m_Frame.ActorId,
                m_Frame.Tick,
                m_Handles.Next,
                m_Handles.Capture,
                m_Handles.Restore,
                m_Scratch);
        }

        public void EndEvaluation()
        {
            m_GameplayEffects = null;
        }

        public IEnumerable<string> OwnedTags => m_GameplayEffects.OwnedTags;
        public bool HasTag(string tag) => m_GameplayEffects.HasTag(tag);
        public bool Matches(PortableTagQuery query) => m_GameplayEffects.Matches(query);
        public void SetActionTags(ulong actionInstanceId, IEnumerable<string> tags) => m_GameplayEffects.SetActionTags(actionInstanceId, tags);
        public void RemoveActionTags(ulong actionInstanceId) => m_GameplayEffects.RemoveActionTags(actionInstanceId);
        public void ClearConfirmedAction(ulong actionInstanceId) => m_GameplayEffects.ClearConfirmedAction(actionInstanceId);

        public void SetEquipmentTags(string sourceId, IEnumerable<string> tags) =>
            m_GameplayEffects.SetEquipmentTags(sourceId, tags);

        public void RemoveEquipmentTags(string sourceId) =>
            m_GameplayEffects.RemoveEquipmentTags(sourceId);

        public ulong ApplyEquipmentPassive(string effectId)
        {
            ProgramCatalogEntry definition = FindCatalog(ProgramCatalogEntryKind.GameplayEffect, effectId) ??
                FindCatalog(ProgramCatalogEntryKind.GameplayEffect, $"effect:{effectId}") ??
                throw new InvalidOperationException($"Equipment passive Effect '{effectId}' is absent from Program.");
            var context = new SimulationGameplayEffectContext(
                m_Frame.ActorId,
                m_Frame.ActorId,
                0,
                0,
                0,
                m_Frame.Tick.Value,
                SimulationGameplayEffectApplicationMode.Confirmed);
            GameplayEffectApplyResult result = m_GameplayEffects.Apply(
                SimulationGameplayEffectApplication.FromCompiled(
                    definition.Identity,
                    checked((uint)definition.Revision),
                    context,
                    Array.Empty<SimulationSetByCallerValue>()));
            if (!result.Succeeded)
                throw new InvalidOperationException($"Equipment passive Effect '{effectId}' failed: {result.Kind}/{result.Reason}.");
            return result.Handle;
        }

        public void RemoveEquipmentPassive(ulong handle)
        {
            if (handle == 0)
                throw new ArgumentOutOfRangeException(nameof(handle));
            int removed = m_GameplayEffects.Remove(new GameplayEffectRemoveRequest<PortableTagQuery>(
                GameplayEffectRemoveSelector.Handle,
                handle,
                string.Empty,
                m_Frame.ActorId,
                null));
            if (removed != 1)
                throw new InvalidOperationException($"Equipment passive Effect handle '{handle}' was not active.");
        }

        public void CommitEquipmentMutation(SimulationOperation source) =>
            ProjectChanges(source, "equipment:commit");

        public void CancelEquipmentMutation() => m_GameplayEffects.ClearChanges();

        public void ApplyIngress(SimulationIngress ingress)
        {
            if (ingress.Header.ActorId != m_Frame.ActorId)
                throw new InvalidOperationException($"Simulation ingress '{ingress.Header.FactIdentity}' targets '{ingress.Header.ActorId}', expected '{m_Frame.ActorId}'.");
            switch (ingress.Header.Kind)
            {
                case SimulationIngressKind.GameplayResult:
                    m_GameplayEffects.ApplyGameplayResult(ingress.GameplayResult);
                    break;
                case SimulationIngressKind.GameplayEffectLifecycle:
                    m_GameplayEffects.ApplyLifecycle(ingress.GameplayEffectLifecycle);
                    break;
                case SimulationIngressKind.AttributeValue:
                    m_GameplayEffects.ApplyAttribute(ingress.AttributeValue);
                    break;
                default:
                    throw new InvalidOperationException($"Gameplay Effect runtime cannot apply ingress kind '{ingress.Header.Kind}'.");
            }
            ProjectChanges(RootOperation(), $"ingress:{ingress.Header.FactIdentity}");
        }

        public void Advance()
        {
            m_GameplayEffects.Advance();
            ProjectChanges(RootOperation(), "gameplay-effect:advance");
        }

        public bool Apply(SimulationOperation operation)
        {
            ProgramCatalogEntry definition = RequireGameplayEffectCatalog(operation);
            ulong configuredRevision = GetUInt64Constant(operation, OperationNamedConstant.DefinitionRevision, 0);
            if (configuredRevision != (ulong)definition.Revision)
                throw new InvalidOperationException($"Gameplay Effect operation '{SourcePath(operation)}' revision '{configuredRevision}' does not match catalog revision '{definition.Revision}'.");

            string contextId = GetStringConstant(operation, OperationNamedConstant.ActionContext, string.Empty);
            ulong actionInstanceId = 0;
            ulong predictionKey = 0;
            if (!string.IsNullOrEmpty(contextId))
            {
                if (m_Actions.FindActive(contextId, out FixedActionInstanceState action) < 0)
                    return false;
                actionInstanceId = action.InstanceId;
                predictionKey = action.PredictionKey;
            }
            bool predicted = GetBooleanConstant(operation, OperationNamedConstant.Predicted, false);
            if (predicted && (actionInstanceId == 0 || predictionKey == 0))
                return false;

            SimulationSetByCallerValue[] values = m_Frame.Services.SetByCallerValues(operation.Handle);
            var context = new SimulationGameplayEffectContext(
                m_Frame.ActorId,
                m_Frame.ActorId,
                actionInstanceId,
                predictionKey,
                0,
                m_Frame.Tick.Value,
                predicted ? SimulationGameplayEffectApplicationMode.Predicted : SimulationGameplayEffectApplicationMode.Confirmed);
            SimulationGameplayEffectApplication application = SimulationGameplayEffectApplication.FromCompiled(
                definition.Identity,
                checked((uint)definition.Revision),
                context,
                values);
            GameplayEffectApplyResult result = m_GameplayEffects.Apply(application);
            ProjectChanges(operation, "operation:apply-effect");
            return result.Succeeded;
        }

        public bool Remove(SimulationOperation operation)
        {
            if (operation.Integer0 < byte.MinValue || operation.Integer0 > byte.MaxValue)
                throw new InvalidOperationException($"Gameplay Effect remove selector '{operation.Integer0}' is invalid.");
            var selector = (GameplayEffectRemoveSelector)(byte)operation.Integer0;
            if (!Enum.IsDefined(typeof(GameplayEffectRemoveSelector), selector))
                throw new InvalidOperationException($"Gameplay Effect remove selector '{operation.Integer0}' is invalid.");
            ulong handle = GetUInt64Constant(operation, OperationNamedConstant.Handle, 0);
            string effectId = GetStringConstant(operation, OperationNamedConstant.Effect, string.Empty);
            PortableTagQuery query = selector == GameplayEffectRemoveSelector.EffectTagQuery
                ? m_Frame.Services.RequireTagQuery(operation.Handle)
                : null;
            var request = new GameplayEffectRemoveRequest<PortableTagQuery>(selector, handle, effectId, m_Frame.ActorId, query);
            int removed = m_GameplayEffects.Remove(request);
            ProjectChanges(operation, "operation:remove-effect");
            return removed > 0;
        }

        public CharacterStateValue ReadAttribute(SimulationOperation operation, string outputPort)
        {
            if (!m_GameplayEffects.TryGetAttribute(operation.Text0, out FixedScalar baseValue, out FixedScalar currentValue, out _))
            {
                if (string.Equals(outputPort, "m_Valid", StringComparison.Ordinal))
                    return CharacterStateValue.FromBoolean(false);
                return CharacterStateValue.FromScalar(FixedScalar.Zero);
            }
            if (string.Equals(outputPort, "m_Valid", StringComparison.Ordinal))
                return CharacterStateValue.FromBoolean(true);
            if (string.Equals(outputPort, "m_BaseValue", StringComparison.Ordinal))
                return CharacterStateValue.FromScalar(baseValue);
            if (string.Equals(outputPort, "m_CurrentValue", StringComparison.Ordinal))
                return CharacterStateValue.FromScalar(currentValue);
            throw new InvalidOperationException($"Gameplay Attribute output port '{outputPort}' is unknown.");
        }

        void ProjectChanges(SimulationOperation source, string cause)
        {
            IReadOnlyList<PortableEffectRuntimeChange> changes = m_GameplayEffects.PendingChanges;
            try
            {
                for (int i = 0; i < changes.Count; i++)
                {
                    switch (changes[i])
                    {
                        case PortableEffectLifecycleRuntimeChange lifecycle:
                        {
                            SimulationEventHeader header = m_Facts.Next(source);
                            m_Facts.Add(new GameplayFact(header, new GameplayEffectFact(
                                lifecycle.Definition.Id,
                                lifecycle.InstanceId,
                                lifecycle.Operation,
                                lifecycle.Context,
                                lifecycle.StartTick,
                                lifecycle.EndTick,
                                lifecycle.StackCount,
                                lifecycle.LifecycleRevision,
                                lifecycle.Definition.Revision,
                                lifecycle.Instant)));
                            break;
                        }
                        case PortableAttributeRuntimeChange attribute:
                        {
                            SimulationEventHeader header = m_Facts.Next(source);
                            m_Facts.Add(new GameplayFact(header, new GameplayAttributeFact(
                                attribute.Value.AttributeId,
                                attribute.Value.BeforeBase,
                                attribute.Value.BaseValue,
                                attribute.Value.BeforeCurrent,
                                attribute.Value.CurrentValue,
                                attribute.Value.Revision,
                                attribute.CauseEffectId,
                                attribute.CauseInstanceId,
                                attribute.CauseContext)));
                            break;
                        }
                        case PortableCueRuntimeChange cue:
                        {
                            SimulationEventHeader factHeader = m_Facts.Next(source);
                            m_Facts.Add(new GameplayFact(factHeader, new GameplayCueFact(
                                cue.CueId,
                                cue.Trigger.ToString(),
                                cue.Definition.Id,
                                cue.InstanceId,
                                cue.Context)));
                            ProgramProducer producer = RequireGameplayCueProducer(cue.Definition.Id, cue.CueId);
                            SimulationEventHeader presentationHeader = m_Presentation.Next(source);
                            m_Presentation.Add(new PresentationCommand(
                                presentationHeader,
                                PresentationCommandKind.Cue,
                                producer.Identity,
                                FixedScalar.Zero,
                                FixedScalar.One));
                            break;
                        }
                        case PortableEffectFailureRuntimeChange failure:
                            if (m_Trace.Enabled)
                            {
                                m_Trace.Add(
                                    source,
                                    "gameplay_effect_transaction_failed",
                                    SimulationTraceSeverity.Error,
                                    $"{cause}:{failure.OwnerEffectId}/{failure.OwnerInstanceId}->{failure.RequestedEffectId}:{failure.Code}:{failure.Reason}");
                            }
                            break;
                        default:
                            throw new InvalidOperationException($"Gameplay Effect change '{changes[i]?.GetType().FullName}' has no projection.");
                    }
                }
            }
            finally
            {
                m_GameplayEffects.ClearChanges();
            }
        }

        ProgramCatalogEntry RequireGameplayEffectCatalog(SimulationOperation operation)
        {
            ProgramCatalogEntry found = null;
            foreach (ProgramReference reference in References(operation.Handle, ProgramReferenceKind.CatalogEntry))
            {
                ProgramCatalogEntry candidate = m_Program.CatalogEntries[reference.TargetIndex];
                if (candidate.Kind != ProgramCatalogEntryKind.GameplayEffect)
                    continue;
                if (found != null)
                    throw new InvalidOperationException($"Gameplay Effect operation '{SourcePath(operation)}' has multiple Effect catalog references.");
                found = candidate;
            }
            return found ?? throw new InvalidOperationException($"Gameplay Effect operation '{SourcePath(operation)}' has no Effect catalog reference.");
        }

        ProgramProducer RequireGameplayCueProducer(string effectId, string cueId)
        {
            return m_Frame.Services.RequireGameplayCueProducer(effectId, cueId);
        }

        ulong GetUInt64Constant(SimulationOperation operation, OperationNamedConstant field, ulong fallback)
        {
            ProgramConstant constant = FindConstant(operation, field);
            return constant != null && constant.Kind == ProgramConstantKind.UInt64 ? constant.UInt64 : fallback;
        }

        SimulationOperation RootOperation()
        {
            OperationHandle handle = m_Layout.RootOperation;
            return m_Program.Operations[handle.Value];
        }

    }
}

