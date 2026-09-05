using ThirdPersonSimulation;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedActionRuntime : FixedOperationModule, IFixedActionAdmissionQuery, IActionAdmissionReadPort, IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>
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
        readonly ActionSkillActivationFlow<SimulationActionTargetSnapshot, SimulationOperation> m_Activation;

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
            m_Activation = new ActionSkillActivationFlow<SimulationActionTargetSnapshot, SimulationOperation>(new ActionAdmissionControl(this), this);
        }

        public bool Activate<TTarget>(OperationControlCursor<TTarget> cursor, SimulationOperation operation)
            where TTarget : struct, IOperationControlTarget<TTarget>
        {
            ActionAdmissionProfile profile = RequireActionProfile(operation);
            return m_Activation.ActivateImmediate(
                new ActionSkillActivationCandidate<SimulationActionTargetSnapshot, SimulationOperation>(
                    default,
                    OperationHandle.Invalid,
                    GetStringConstant(operation, OperationNamedConstant.ActionContext, string.Empty),
                    GetStringConstant(operation, OperationNamedConstant.SourceInputRequest, string.Empty),
                    GetBooleanConstant(operation, OperationNamedConstant.ConsumeSourceInputRequest, true),
                    GetStringConstant(operation, OperationNamedConstant.TargetKey, string.Empty),
                    ReadActionTargetSnapshot(cursor, operation),
                    SimulationExecutionSource.FromSkillOperation(operation.Handle, SourcePath(operation)),
                    m_EquipmentContext.Current,
                    operation),
                profile);
        }

        public bool IsContextActive(string contextId) => m_Actions.IsContextActive(contextId);
        public bool IsSkillActive(CharacterSkillId skillId) => m_Actions.IsSkillActive(skillId);
        public bool IsSkillCompleted(CharacterSkillId skillId) => m_Actions.IsSkillCompleted(skillId);
        public ulong CompletedSkillInstanceId(CharacterSkillId skillId) => m_Actions.CompletedSkillInstanceId(skillId);

        public void BindCurrentSkillExecution(OperationHandle operation, ulong generation)
        {
            if (!m_Actions.TryGetCurrentSkillExecution(out FixedActionInstanceState action) ||
                !action.SkillEntryOperation.Equals(operation))
                return;
            m_Actions.BindSkillExecution(action, operation, generation);
        }

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
            return m_Activation.Preview(operation, profile, ReadActionTargetSnapshot(cursor, operation));
        }

        public bool ActivateFromControl(CharacterControlSkillRequest controlRequest)
        {
            CharacterSkillProgramBinding skill = m_Program.SkillPrograms.Require(controlRequest.SkillId);
            ActionAdmissionProfile profile = RequireActionProfile(skill.ActionProfileId);
            return m_Activation.ActivateFromControl(controlRequest, skill, profile);
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

        public bool TryCommitPendingControl(CharacterSkillId skillId)
        {
            CharacterSkillProgramBinding skill = m_Program.SkillPrograms.Require(skillId);
            ActionAdmissionProfile profile = RequireActionProfile(skill.ActionProfileId);
            return m_Activation.TryCommitPendingControl(skillId, profile);
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
            ulong generation = slot < 0 ? 1UL : m_Frame.ReadState(slot).UInt64;
            return generation == 0 ? 1UL : generation;
        }

        ActionAdmissionProfile RequireActionProfile(SimulationOperation operation) =>
            Access.Services.RequireActionProfile(operation.Handle);

        ActionAdmissionProfile RequireActionProfile(string actionId) =>
            Access.Services.RequireActionProfile(actionId);

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

        ulong IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.InputSequence => m_Frame.Input.Sequence;

        ulong IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.Tick => m_Frame.Tick.Value;

        bool IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.TraceEnabled => m_Trace.Enabled;

        bool IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.TryReadInputSequence(string requestId, out ulong sequence)
        {
            if (!m_InputRuntime.HasRequest(requestId, out FixedInputRequestState request))
            {
                sequence = 0;
                return false;
            }
            sequence = request.Sequence;
            return true;
        }

        void IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.ClearInputRequest(string requestId) => m_InputRuntime.ClearRequest(requestId);

        SimulationActionTargetSnapshot IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.NoneTarget =>
            SimulationActionTargetSnapshot.None;

        SimulationActionTargetSnapshot IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.ReadTargetSnapshot(string inputValueId) =>
            m_InputRuntime.ReadValue(inputValueId, SimulationInputValueKind.ActionTargetSnapshot).ActionTargetSnapshot;

        string IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.TargetId(
            SimulationActionTargetSnapshot targetSnapshot) => targetSnapshot.TargetId;

        string IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.FormatTarget(
            SimulationActionTargetSnapshot targetSnapshot) =>
            $"targetPosition={targetSnapshot.Position}:targetYaw={targetSnapshot.Yaw}";

        bool IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.HasPendingRequest(string actionId)
        {
            int slot = m_Actions.RequireSlot(actionId, ProgramStateSemantic.ActionRequestBuffer);
            return m_Actions.ReadRequest(slot).IsValid;
        }

        void IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.StageRequest(
            ActionSkillActivationRequest<SimulationActionTargetSnapshot> request)
        {
            int slot = m_Actions.RequireSlot(request.ActionId, ProgramStateSemantic.ActionRequestBuffer);
            m_Actions.WriteRequest(
                slot,
                new FixedActionActivationRequestState(
                    request.ActionId,
                    request.SkillId,
                    request.SkillEntryOperation,
                    request.ContextId,
                    request.SourceInputRequestId,
                    request.InputSequence,
                    request.StartTick,
                    request.TargetKey,
                    request.TargetSnapshot,
                    request.Source,
                    request.EquipmentContext));
        }

        bool IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.TryReadPendingRequest(
            CharacterSkillId skillId,
            out ActionSkillActivationRequest<SimulationActionTargetSnapshot> request)
        {
            int slot = m_Actions.FindPendingSkill(skillId, out FixedActionActivationRequestState staged);
            if (slot < 0)
            {
                request = default;
                return false;
            }
            request = new ActionSkillActivationRequest<SimulationActionTargetSnapshot>(
                staged.ActionId,
                staged.SkillId,
                staged.SkillEntryOperation,
                staged.ContextId,
                staged.SourceInputRequestId,
                staged.InputSequence,
                staged.StartTick,
                staged.TargetKey,
                staged.TargetSnapshot,
                staged.Source,
                staged.EquipmentContext);
            return true;
        }

        void IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.ClearPendingRequest(
            ActionSkillActivationRequest<SimulationActionTargetSnapshot> request)
        {
            int slot = m_Actions.RequireSlot(request.ActionId, ProgramStateSemantic.ActionRequestBuffer);
            m_Actions.ClearRequest(slot);
        }

        ulong IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.CommitRequest(
            ActionSkillActivationRequest<SimulationActionTargetSnapshot> request,
            ActionAdmissionProfile profile)
        {
            int requestSlot = m_Actions.RequireSlot(request.ActionId, ProgramStateSemantic.ActionRequestBuffer);
            ulong instanceId = m_Handles.Next();
            ulong predictionKey = m_Actions.NextSequence();
            var instance = new FixedActionInstanceState(
                request.ActionId,
                request.SkillId,
                request.SkillEntryOperation,
                0,
                request.ContextId,
                instanceId,
                predictionKey,
                request.SourceInputRequestId,
                request.InputSequence,
                request.StartTick,
                request.TargetKey,
                request.TargetSnapshot,
                request.Source,
                SimulationActionPhase.Startup,
                SimulationActionState.Predicted,
                SimulationActionLifecycleTransitionType.None,
                request.StartTick,
                0,
                string.Empty,
                request.EquipmentContext);
            try
            {
                m_Actions.WriteState(instance);
                m_GameplayEffectActions.SetActionTags(instanceId, profile.Tags);
                m_Actions.ClearRequest(requestSlot);
                EmitActionFact(instance.Source, instance);
                return instanceId;
            }
            finally
            {
                m_Actions.ClearRequest(requestSlot);
            }
        }

        void IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.InterruptActive(
            SimulationExecutionSource source,
            string reason)
        {
            ApplyActionTransition(
                source,
                m_Actions.FindOnlyActive(),
                SimulationActionLifecycleTransitionType.Interrupt,
                reason,
                0);
        }

        void IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.Trace(
            SimulationOperation operation,
            string code,
            ActionSkillTraceSeverity severity,
            string detail) => m_Trace.Add(operation, code, ToTraceSeverity(severity), detail);

        void IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.Trace(
            SimulationExecutionSource source,
            string code,
            ActionSkillTraceSeverity severity,
            string detail) => m_Trace.Add(source, code, ToTraceSeverity(severity), detail);

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

        static SimulationTraceSeverity ToTraceSeverity(ActionSkillTraceSeverity severity) =>
            severity == ActionSkillTraceSeverity.Detail
                ? SimulationTraceSeverity.Detail
                : SimulationTraceSeverity.Information;

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

