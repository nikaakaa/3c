using ThirdPersonSimulation;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedActionRuntime : FixedOperationModule, IFixedActionAdmissionQuery, IActionAdmissionReadPort
    {
        readonly FixedEvaluationFrame m_Frame;
        readonly IFixedInputPort m_InputRuntime;
        readonly FixedActionStateStore m_Actions;
        readonly IFixedBlackboardPort m_Blackboard;
        readonly IFixedGameplayTagQuery m_GameplayTags;
        readonly IFixedGameplayEffectActionPort m_GameplayEffectActions;
        readonly FixedHandleAllocator m_Handles;
        readonly FixedFactSink m_Facts;
        readonly FixedTraceSink m_Trace;
        readonly IEquipmentActionContextProvider m_EquipmentContext;
        readonly ActionAdmissionControl m_Admission;

        public FixedActionRuntime(
            FixedProgramAccess access,
            FixedEvaluationFrame frame,
            IFixedInputPort inputRuntime,
            FixedActionStateStore actions,
            IFixedBlackboardPort blackboard,
            IFixedGameplayTagQuery gameplayTags,
            IFixedGameplayEffectActionPort gameplayEffectActions,
            FixedHandleAllocator handles,
            FixedFactSink facts,
            FixedTraceSink trace,
            IEquipmentActionContextProvider equipmentContext)
            : base(access)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            m_InputRuntime = inputRuntime ?? throw new ArgumentNullException(nameof(inputRuntime));
            m_Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            m_Blackboard = blackboard ?? throw new ArgumentNullException(nameof(blackboard));
            m_GameplayTags = gameplayTags ?? throw new ArgumentNullException(nameof(gameplayTags));
            m_GameplayEffectActions = gameplayEffectActions ?? throw new ArgumentNullException(nameof(gameplayEffectActions));
            m_Handles = handles ?? throw new ArgumentNullException(nameof(handles));
            m_Facts = facts ?? throw new ArgumentNullException(nameof(facts));
            m_Trace = trace ?? throw new ArgumentNullException(nameof(trace));
            m_EquipmentContext = equipmentContext ?? throw new ArgumentNullException(nameof(equipmentContext));
            m_Admission = new ActionAdmissionControl(this);
        }

        public bool Activate<TTarget>(OperationControlCursor<TTarget> cursor, SimulationOperation operation)
            where TTarget : struct, IOperationControlTarget<TTarget>
        {
            ActionAdmissionProfile profile = RequireActionProfile(operation);
            string actionId = profile.ActionId;
            string contextId = GetStringConstant(operation, OperationNamedConstant.ActionContext, string.Empty);
            if (string.IsNullOrEmpty(contextId))
                throw new InvalidOperationException($"Action operation '{SourcePath(operation)}' has no formal Action Context.");

            SimulationActionTargetSnapshot targetSnapshot = SelectTargetSnapshot(
                profile,
                ReadActionTargetSnapshot(cursor, operation));
            ActionAdmissionDecision admission = m_Admission.Evaluate(new ActionAdmissionRequest(
                profile,
                new ActionAdmissionTargetCandidate(targetSnapshot.TargetId),
                ActionAdmissionEvaluationMode.CommitActivation));
            if (!admission.Allowed)
            {
                if (m_Trace.Enabled)
                {
                    m_Trace.Add(
                        operation,
                        "action_activation_rejected",
                        SimulationTraceSeverity.Information,
                        $"{actionId}:{admission.RejectReason}:{admission.ActiveSourceActionId}");
                }
                return false;
            }

            string requestId = GetStringConstant(operation, OperationNamedConstant.SourceInputRequest, string.Empty);
            ulong inputSequence = m_Frame.Input.Sequence;
            if (!string.IsNullOrEmpty(requestId))
            {
                if (!m_InputRuntime.HasRequest(requestId, out FixedInputRequestState inputRequest))
                {
                    if (m_Trace.Enabled)
                        m_Trace.Add(operation, "action_request_unavailable", SimulationTraceSeverity.Detail, $"{requestId}:{m_Frame.Tick.Value}");
                    return false;
                }
                inputSequence = inputRequest.Sequence;
                if (GetBooleanConstant(operation, OperationNamedConstant.ConsumeSourceInputRequest, true))
                    m_InputRuntime.ClearRequest(requestId);
            }

            var request = new FixedActionActivationRequestState(
                actionId,
                default,
                OperationHandle.Invalid,
                contextId,
                requestId,
                inputSequence,
                m_Frame.Tick.Value,
                GetStringConstant(operation, OperationNamedConstant.TargetKey, string.Empty),
                targetSnapshot,
                SimulationExecutionSource.FromSkillOperation(operation.Handle, SourcePath(operation)),
                m_EquipmentContext.Current);
            int requestSlot = m_Actions.RequireSlot(actionId, ProgramStateSemantic.ActionRequestBuffer);
            m_Actions.WriteRequest(requestSlot, request);
            try
            {
                FixedActionActivationRequestState staged = m_Actions.ReadRequest(requestSlot);
                ulong instanceId = m_Handles.Next();
                ulong predictionKey = m_Actions.NextSequence();
                var instance = new FixedActionInstanceState(
                    staged.ActionId,
                    staged.SkillId,
                    OperationHandle.Invalid,
                    0,
                    staged.ContextId,
                    instanceId,
                    predictionKey,
                    staged.SourceInputRequestId,
                    staged.InputSequence,
                    staged.StartTick,
                    staged.TargetKey,
                    staged.TargetSnapshot,
                    staged.Source,
                    SimulationActionPhase.Startup,
                    SimulationActionState.Predicted,
                    SimulationActionLifecycleTransitionType.None,
                    staged.StartTick,
                    0,
                    string.Empty,
                    staged.EquipmentContext);
                m_Actions.WriteState(instance);
                m_GameplayEffectActions.SetActionTags(instanceId, profile.Tags);
                EmitActionFact(instance.Source, instance);
                if (m_Trace.Enabled)
                    m_Trace.Add(operation, "action_activated", SimulationTraceSeverity.Information, $"{actionId}:{instanceId}:request={requestId}:sequence={inputSequence}:requirement={profile.TargetRequirement}:candidate={targetSnapshot.TargetId}:captured={instance.TargetSnapshot.TargetId}:captureTick={instance.StartTick}:targetPosition={instance.TargetSnapshot.Position}:targetYaw={instance.TargetSnapshot.Yaw}:equipment={instance.EquipmentContext}");
                return true;
            }
            finally
            {
                m_Actions.ClearRequest(requestSlot);
            }
        }

        public bool IsContextActive(string contextId) => m_Actions.IsContextActive(contextId);
        public bool IsSkillActive(CharacterSkillId skillId) => m_Actions.IsSkillActive(skillId);
        public bool IsSkillCompleted(CharacterSkillId skillId) => m_Actions.IsSkillCompleted(skillId);
        public ulong CompletedSkillInstanceId(CharacterSkillId skillId) => m_Actions.CompletedSkillInstanceId(skillId);

        public void FinishFromControl(
            CharacterSkillId skillId,
            SimulationExecutionSource source,
            bool completed,
            string reason)
        {
            int slot = m_Actions.FindActive(skillId, out FixedActionInstanceState action);
            if (slot < 0)
                return;
            ApplyActionTransition(
                source,
                action,
                completed
                    ? SimulationActionLifecycleTransitionType.Complete
                    : SimulationActionLifecycleTransitionType.Abort,
                reason,
                0);
        }

        public ActionAdmissionDecision PreviewActivation<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation)
            where TTarget : struct, IOperationControlTarget<TTarget>
        {
            ActionAdmissionProfile profile = RequireActionProfile(operation);
            SimulationActionTargetSnapshot targetSnapshot = SelectTargetSnapshot(profile, ReadActionTargetSnapshot(cursor, operation));
            ActionAdmissionDecision decision = m_Admission.Evaluate(new ActionAdmissionRequest(
                profile,
                new ActionAdmissionTargetCandidate(targetSnapshot.TargetId),
                ActionAdmissionEvaluationMode.PreviewReplacement));
            if (m_Trace.Enabled)
            {
                m_Trace.Add(
                    operation,
                    "action_admission_preview",
                    SimulationTraceSeverity.Detail,
                    $"{profile.ActionId}:{decision.Allowed}:{decision.RejectReason}:{decision.ActiveSourceActionId}");
            }
            return decision;
        }

        public bool ActivateFromControl(CharacterControlSkillRequest controlRequest)
        {
            CharacterSkillProgramBinding skill = m_Program.SkillPrograms.Require(controlRequest.SkillId);
            ActionAdmissionProfile profile = RequireActionProfile(skill.ActionProfileId);
            string requestId = string.IsNullOrEmpty(controlRequest.SourceInputRequestId)
                ? skill.SourceInputRequestId
                : controlRequest.SourceInputRequestId;
            bool consumeRequest = string.IsNullOrEmpty(controlRequest.SourceInputRequestId)
                ? skill.ConsumeSourceInputRequest
                : controlRequest.ConsumeSourceInputRequest;
            SimulationActionTargetSnapshot targetSnapshot = profile.TargetRequirement == ActionTargetRequirement.None ||
                string.IsNullOrEmpty(controlRequest.TargetInputValueId) && string.IsNullOrEmpty(skill.TargetInputValueId)
                    ? SimulationActionTargetSnapshot.None
                    : m_InputRuntime.ReadValue(
                        string.IsNullOrEmpty(controlRequest.TargetInputValueId)
                            ? skill.TargetInputValueId
                            : controlRequest.TargetInputValueId,
                        SimulationInputValueKind.ActionTargetSnapshot).ActionTargetSnapshot;
            return ActivateFromControl(
                skill.SkillId,
                skill.EntryOperation,
                profile,
                skill.ActionContextId,
                requestId,
                consumeRequest,
                string.IsNullOrEmpty(controlRequest.TargetKey) ? skill.TargetKey : controlRequest.TargetKey,
                targetSnapshot,
                controlRequest.Source);
        }

        public void StopFromControl(CharacterControlSkillStopRequest controlRequest)
        {
            CharacterSkillProgramBinding skill = m_Program.SkillPrograms.Require(controlRequest.SkillId);
            int slot = m_Actions.FindActive(skill.SkillId, out FixedActionInstanceState action);
            if (slot < 0)
                return;
            SimulationActionLifecycleTransitionType transition = controlRequest.Mode == CharacterControlSkillStopMode.Force
                ? SimulationActionLifecycleTransitionType.Abort
                : SimulationActionLifecycleTransitionType.Cancel;
            ApplyActionTransition(
                controlRequest.Source,
                action,
                transition,
                controlRequest.Reason,
                0);
        }

        bool ActivateFromControl(
            CharacterSkillId skillId,
            OperationHandle skillEntryOperation,
            ActionAdmissionProfile profile,
            string contextId,
            string requestId,
            bool consumeRequest,
            string targetKey,
            SimulationActionTargetSnapshot targetSnapshot,
            SimulationExecutionSource source)
        {
            string actionId = profile.ActionId;
            ActionAdmissionDecision admission = m_Admission.Evaluate(new ActionAdmissionRequest(
                profile,
                new ActionAdmissionTargetCandidate(targetSnapshot.TargetId),
                ActionAdmissionEvaluationMode.CommitActivation));
            bool replacementPending = false;
            if (!admission.Allowed)
            {
                if (admission.RejectReason != ActionAdmissionRejectReason.SourceActionStillActive)
                {
                    if (m_Trace.Enabled)
                        m_Trace.Add(source, "action_activation_rejected", SimulationTraceSeverity.Information, $"{actionId}:{admission.RejectReason}:{admission.ActiveSourceActionId}");
                    return false;
                }
                ActionAdmissionDecision replacement = m_Admission.Evaluate(new ActionAdmissionRequest(
                    profile,
                    new ActionAdmissionTargetCandidate(targetSnapshot.TargetId),
                    ActionAdmissionEvaluationMode.PreviewReplacement));
                if (!replacement.Allowed)
                {
                    if (m_Trace.Enabled)
                        m_Trace.Add(source, "action_replacement_rejected", SimulationTraceSeverity.Information, $"{actionId}:{replacement.RejectReason}:{replacement.ActiveSourceActionId}");
                    return false;
                }
                FixedActionInstanceState active = m_Actions.FindOnlyActive();
                ApplyActionTransition(
                    source,
                    active,
                    SimulationActionLifecycleTransitionType.Interrupt,
                    "SkillReplacement",
                    0);
                replacementPending = true;
            }

            ulong inputSequence = m_Frame.Input.Sequence;
            if (!string.IsNullOrEmpty(requestId))
            {
                if (!m_InputRuntime.HasRequest(requestId, out FixedInputRequestState inputRequest))
                {
                    if (m_Trace.Enabled)
                        m_Trace.Add(source, "action_request_unavailable", SimulationTraceSeverity.Detail, $"{requestId}:{m_Frame.Tick.Value}");
                    return false;
                }
                inputSequence = inputRequest.Sequence;
                if (consumeRequest)
                    m_InputRuntime.ClearRequest(requestId);
            }

            var request = new FixedActionActivationRequestState(
                actionId,
                skillId,
                skillEntryOperation,
                contextId,
                requestId,
                inputSequence,
                m_Frame.Tick.Value,
                targetKey,
                targetSnapshot,
                source,
                m_EquipmentContext.Current);
            int requestSlot = m_Actions.RequireSlot(actionId, ProgramStateSemantic.ActionRequestBuffer);
            if (m_Actions.ReadRequest(requestSlot).IsValid)
                throw new InvalidOperationException($"Action '{actionId}' already has a pending activation request.");
            m_Actions.WriteRequest(requestSlot, request);
            return !replacementPending;
        }

        public bool TryCommitPendingControl(CharacterSkillId skillId)
        {
            int requestSlot = m_Actions.FindPendingSkill(skillId, out FixedActionActivationRequestState request);
            if (requestSlot < 0)
                return false;
            CharacterSkillProgramBinding skill = m_Program.SkillPrograms.Require(skillId);
            ActionAdmissionProfile profile = RequireActionProfile(skill.ActionProfileId);
            ActionAdmissionDecision admission = m_Admission.Evaluate(new ActionAdmissionRequest(
                profile,
                new ActionAdmissionTargetCandidate(request.TargetSnapshot.TargetId),
                ActionAdmissionEvaluationMode.CommitActivation));
            if (!admission.Allowed)
            {
                if (admission.RejectReason == ActionAdmissionRejectReason.SourceActionStillActive)
                    return false;
                m_Actions.ClearRequest(requestSlot);
                if (m_Trace.Enabled)
                    m_Trace.Add(request.Source, "action_activation_rejected", SimulationTraceSeverity.Information, $"{skillId}:{admission.RejectReason}:{admission.ActiveSourceActionId}");
                return false;
            }
            CommitControlActivation(requestSlot, request, profile);
            return true;
        }

        void CommitControlActivation(
            int requestSlot,
            FixedActionActivationRequestState staged,
            ActionAdmissionProfile profile)
        {
            ulong instanceId = m_Handles.Next();
            ulong predictionKey = m_Actions.NextSequence();
            var instance = new FixedActionInstanceState(
                staged.ActionId,
                staged.SkillId,
                staged.SkillEntryOperation,
                0,
                staged.ContextId,
                instanceId,
                predictionKey,
                staged.SourceInputRequestId,
                staged.InputSequence,
                staged.StartTick,
                staged.TargetKey,
                staged.TargetSnapshot,
                staged.Source,
                SimulationActionPhase.Startup,
                SimulationActionState.Predicted,
                SimulationActionLifecycleTransitionType.None,
                staged.StartTick,
                0,
                string.Empty,
                staged.EquipmentContext);
            m_Actions.WriteState(instance);
            m_GameplayEffectActions.SetActionTags(instanceId, profile.Tags);
            m_Actions.ClearRequest(requestSlot);
            EmitActionFact(instance.Source, instance);
            if (m_Trace.Enabled)
                m_Trace.Add(staged.Source, "action_activated", SimulationTraceSeverity.Information, $"{staged.ActionId}:{instanceId}:request={staged.SourceInputRequestId}:sequence={staged.InputSequence}:requirement={profile.TargetRequirement}:candidate={staged.TargetSnapshot.TargetId}:captured={instance.TargetSnapshot.TargetId}:captureTick={instance.StartTick}:targetPosition={instance.TargetSnapshot.Position}:targetYaw={instance.TargetSnapshot.Yaw}:equipment={instance.EquipmentContext}");
        }

        public bool SubmitLifecycle(SimulationOperation operation)
        {
            string contextId = GetStringConstant(operation, OperationNamedConstant.ActionContext, string.Empty);
            int slot = m_Actions.FindActive(contextId, out FixedActionInstanceState action);
            if (slot < 0)
                return false;
            SimulationActionLifecycleTransitionType transition = RequireActionTransition(operation.Integer0);
            ApplyActionTransition(
                SimulationExecutionSource.FromSkillOperation(operation.Handle, SourcePath(operation)),
                action,
                transition,
                operation.Text0,
                0);
            return true;
        }

        public void ApplyIngress(SimulationIngress ingress)
        {
            if (ingress.Header.ActorId != m_Frame.ActorId)
                throw new InvalidOperationException($"Simulation ingress '{ingress.Header.FactIdentity}' targets '{ingress.Header.ActorId}', expected '{m_Frame.ActorId}'.");
            if (ingress.Header.Kind != SimulationIngressKind.ActionLifecycle)
                throw new InvalidOperationException($"Action runtime cannot apply ingress kind '{ingress.Header.Kind}'.");
            ApplyActionIngress(ingress);
        }

        void ApplyActionIngress(SimulationIngress ingress)
        {
            SimulationActionLifecycleIngress payload = ingress.ActionLifecycle;
            FixedActionInstanceState match = default;
            int matches = 0;
            foreach (TypedActionStateAddresses addresses in m_Layout.ActionStateIndex.Values)
            {
                FixedActionInstanceState candidate = m_Actions.ReadSlot(addresses.Instance.SlotIndex);
                if (!candidate.IsActive || !MatchesActionIngress(candidate, payload))
                    continue;
                match = candidate;
                matches++;
            }
            if (matches != 1)
                throw new InvalidOperationException($"Action lifecycle ingress '{ingress.Header.FactIdentity}' matched {matches} active Action instances.");
            SimulationExecutionSource source = match.Source;
            ApplyActionTransition(
                source,
                match,
                payload.TransitionType,
                payload.Reason,
                ingress.Header.SourceTick);
        }

        void ApplyActionTransition(
            SimulationExecutionSource source,
            FixedActionInstanceState action,
            SimulationActionLifecycleTransitionType transition,
            string reason,
            ulong sourceTick)
        {
            if (!action.IsActive)
                throw new InvalidOperationException($"Action '{action.ActionId}/{action.InstanceId}' is not active.");
            if (transition == SimulationActionLifecycleTransitionType.Confirm &&
                action.State != SimulationActionState.Predicted &&
                action.State != SimulationActionState.Corrected)
                throw new InvalidOperationException($"Action '{action.ActionId}/{action.InstanceId}' cannot confirm from '{action.State}'.");

            SimulationActionPhase phase = action.Phase;
            SimulationActionState state = action.State;
            switch (transition)
            {
                case SimulationActionLifecycleTransitionType.Confirm:
                    state = SimulationActionState.Confirmed;
                    reason = string.Empty;
                    break;
                case SimulationActionLifecycleTransitionType.Complete:
                    phase = SimulationActionPhase.Ended;
                    state = SimulationActionState.Ended;
                    break;
                case SimulationActionLifecycleTransitionType.Cancel:
                    phase = SimulationActionPhase.Cancel;
                    state = SimulationActionState.Cancelled;
                    break;
                case SimulationActionLifecycleTransitionType.Interrupt:
                    phase = SimulationActionPhase.Cancel;
                    state = SimulationActionState.Interrupted;
                    break;
                case SimulationActionLifecycleTransitionType.Reject:
                    phase = SimulationActionPhase.Ended;
                    state = SimulationActionState.Rejected;
                    break;
                case SimulationActionLifecycleTransitionType.Correct:
                    state = SimulationActionState.Corrected;
                    break;
                case SimulationActionLifecycleTransitionType.Abort:
                    phase = SimulationActionPhase.Ended;
                    state = SimulationActionState.Aborted;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(transition));
            }

            FixedActionInstanceState next = action.WithLifecycle(
                phase,
                state,
                transition,
                m_Frame.Tick.Value,
                sourceTick,
                reason);
            m_Actions.WriteState(next);
            EmitActionFact(source, next);
            if (m_Trace.Enabled)
                m_Trace.Add(source, "action_lifecycle", SimulationTraceSeverity.Information, $"{next.ActionId}:{next.InstanceId}:{transition}:{next.Reason}:equipment={next.EquipmentContext}", SourceGeneration(source));
            if (!next.IsActive)
            {
                m_GameplayEffectActions.RemoveActionTags(next.InstanceId);
                m_GameplayEffectActions.ClearConfirmedAction(next.InstanceId);
                m_Blackboard.ClearActionInstanceScopes(next.InstanceId);
            }
        }

        void EmitActionFact(SimulationExecutionSource source, FixedActionInstanceState action)
        {
            SimulationEventHeader header = m_Facts.Next(source, SourceGeneration(source));
            m_Facts.Add(new GameplayFact(header, new ActionFact(
                action.InstanceId,
                action.PredictionKey,
                action.InputSequence,
                action.ActionId,
                action.SkillId,
                action.LastTransition,
                action.Phase,
                action.State,
                action.Reason,
                action.EquipmentContext)));
        }

        ulong SourceGeneration(SimulationExecutionSource source)
        {
            if (!source.IsSkillOperation)
                return 1;
            int slot = m_Frame.Layout.FindOperationStateSlot(
                source.Operation,
                ProgramStateSemantic.RunnableActivationGeneration);
            ulong generation = slot < 0 ? 1UL : m_Frame.Transaction.Get(slot).UInt64;
            return generation == 0 ? 1UL : generation;
        }

        ActionAdmissionProfile RequireActionProfile(SimulationOperation operation) =>
            Access.Services.RequireActionProfile(operation.Handle);

        ActionAdmissionProfile RequireActionProfile(string actionId) =>
            Access.Services.RequireActionProfile(actionId);

        static SimulationActionTargetSnapshot SelectTargetSnapshot(
            ActionAdmissionProfile profile,
            SimulationActionTargetSnapshot candidate)
        {
            return profile.TargetRequirement == ActionTargetRequirement.None
                ? SimulationActionTargetSnapshot.None
                : candidate;
        }

        ProgramCatalogEntry FindGameplayTag(string identity)
        {
            return FindCatalog(ProgramCatalogEntryKind.GameplayTag, identity);
        }

        IEnumerable<string> IActionAdmissionReadPort.OwnedGameplayTags => m_GameplayTags.OwnedTags;

        bool IActionAdmissionReadPort.TryGetActiveAction(out string actionId)
        {
            FixedActionInstanceState active = m_Actions.FindOnlyActive();
            actionId = active.IsActive ? active.ActionId : string.Empty;
            return active.IsActive;
        }

        ActionAdmissionProfile IActionAdmissionReadPort.RequireActionProfile(string actionId)
        {
            return RequireActionProfile(actionId);
        }

        bool IActionAdmissionReadPort.TryGetGameplayTagParent(string tag, out string parentTag)
        {
            parentTag = string.Empty;
            ProgramCatalogEntry entry = FindGameplayTag(tag);
            if (entry != null && TryGetCatalogIdentity(entry, ProgramCatalogFieldId.Parent, out parentTag))
                return true;
            return false;
        }

        SimulationActionTargetSnapshot ReadActionTargetSnapshot<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation)
            where TTarget : struct, IOperationControlTarget<TTarget>
        {
            if (!m_Layout.TryGetActionTargetSnapshot(operation.Handle, out TypedStateAddress address))
                return SimulationActionTargetSnapshot.None;
            CharacterStateValue value = m_Blackboard.Read(cursor, operation, address.SlotIndex);
            if (value.Kind != ProgramStateValueKind.ActionTargetSnapshot)
                throw new InvalidOperationException($"Action target snapshot for '{SourcePath(operation)}' has kind '{value.Kind}'.");
            return value.ActionTargetSnapshot;
        }

        static bool MatchesActionIngress(FixedActionInstanceState action, SimulationActionLifecycleIngress ingress)
        {
            return (ingress.ActionInstanceId == 0 || ingress.ActionInstanceId == action.InstanceId) &&
                   (ingress.PredictionKey == 0 || ingress.PredictionKey == action.PredictionKey) &&
                   (ingress.InputSequence == 0 || ingress.InputSequence == action.InputSequence);
        }

        static SimulationActionLifecycleTransitionType RequireActionTransition(int value)
        {
            if (value < byte.MinValue || value > byte.MaxValue)
                throw new InvalidOperationException($"Action lifecycle transition '{value}' is invalid.");
            var transition = (SimulationActionLifecycleTransitionType)(byte)value;
            if (!Enum.IsDefined(typeof(SimulationActionLifecycleTransitionType), transition) || transition == 0)
                throw new InvalidOperationException($"Action lifecycle transition '{value}' is invalid.");
            return transition;
        }
    }
}

