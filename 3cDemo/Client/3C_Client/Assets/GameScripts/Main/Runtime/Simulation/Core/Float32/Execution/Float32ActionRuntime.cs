using System;
using System.Collections.Generic;
using System.Globalization;

namespace ThirdPersonSimulation
{
	internal sealed class Float32ActionRuntime : Float32OperationModule, IFloat32ActionAdmissionQuery, IActionAdmissionReadPort, IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>, IActionSkillCommitPort<SimulationActionTargetSnapshot, Float32ActionInstanceState>, IActionSkillLifecyclePort<Float32ActionInstanceState>
	{
		readonly Float32EvaluationFrame m_Frame;
		readonly IFloat32InputPort m_InputRuntime;
		readonly Float32ActionStateStore m_Actions;
		readonly IFloat32BlackboardPort m_Blackboard;
		readonly IFloat32GameplayTagQuery m_GameplayTags;
		readonly IFloat32GameplayEffectActionPort m_GameplayEffectActions;
		readonly Float32HandleAllocator m_Handles;
		readonly Float32FactSink m_Facts;
		readonly Float32TraceSink m_Trace;
		readonly IEquipmentActionContextProvider m_EquipmentContext;
		readonly Func<OperationHandle, bool> m_IsOperationStopComplete;
		readonly ActionSkillActivationFlow<SimulationActionTargetSnapshot, SimulationOperation, Float32ActionInstanceState> m_Activation;
		readonly ActionSkillCommitFlow<SimulationActionTargetSnapshot, Float32ActionInstanceState> m_Commit;
		readonly ActionSkillLifecycleFlow<Float32ActionInstanceState> m_Lifecycle;

		public Float32ActionRuntime(
			Float32ProgramAccess access,
			Float32EvaluationFrame frame,
			IFloat32InputPort inputRuntime,
			Float32ActionStateStore actions,
			IFloat32BlackboardPort blackboard,
			IFloat32GameplayTagQuery gameplayTags,
			IFloat32GameplayEffectActionPort gameplayEffectActions,
			Float32HandleAllocator handles,
			Float32FactSink facts,
			Float32TraceSink trace,
			IEquipmentActionContextProvider equipmentContext,
			Func<OperationHandle, bool> isOperationStopComplete = null)
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
			m_IsOperationStopComplete = isOperationStopComplete ?? (operation => true);
			m_Commit = new ActionSkillCommitFlow<SimulationActionTargetSnapshot, Float32ActionInstanceState>(this);
			m_Lifecycle = new ActionSkillLifecycleFlow<Float32ActionInstanceState>(this);
			m_Activation = new ActionSkillActivationFlow<SimulationActionTargetSnapshot, SimulationOperation, Float32ActionInstanceState>(new ActionAdmissionControl(this), this, m_Commit);
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
                    default,
                    operation),
                profile);
		}

		public bool IsContextActive(string contextId) => m_Actions.IsContextActive(contextId);
		public bool IsSkillActive(CharacterSkillId skillId) => m_Actions.IsSkillActive(skillId);
		public bool IsSkillCompleted(CharacterSkillId skillId) => m_Actions.IsSkillCompleted(skillId);
		public ulong CompletedSkillInstanceId(CharacterSkillId skillId) => m_Actions.CompletedSkillInstanceId(skillId);

		public void BindCurrentSkillExecution(OperationHandle operation, ulong generation)
		{
			if (!m_Actions.TryGetCurrentSkillExecution(out Float32ActionInstanceState action) ||
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
			m_Lifecycle.Finish(skillId, source, completed, reason);
		}

		public void FinishFromControl(
			Float32ActionInstanceState action,
			SimulationExecutionSource source,
			bool completed,
			string reason)
		{
			m_Lifecycle.Finish(action, source, completed, reason);
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
            if (controlRequest.EquipmentContext.IsValid &&
                !m_EquipmentContext.IsSkillBinding(controlRequest.EquipmentContext, controlRequest.SkillId))
            {
                if (m_Trace.Enabled)
                    m_Trace.Add(
                        controlRequest.Source,
                        "equipment_skill_binding_invalid",
                        SimulationTraceSeverity.Warning,
                        $"skill={controlRequest.SkillId}:context={controlRequest.EquipmentContext}");
                return false;
            }
			CharacterSkillProgramBinding skill = m_Program.SkillPrograms.Require(controlRequest.SkillId);
			ActionAdmissionProfile profile = RequireActionProfile(skill.ActionProfileId);
			return m_Activation.ActivateFromControl(controlRequest, skill, profile);
		}

        public void StopFromControl(CharacterControlSkillStopRequest controlRequest)
        {
            CharacterSkillProgramBinding skill = m_Program.SkillPrograms.Require(controlRequest.SkillId);
            if (controlRequest.ActionInstanceId != 0)
            {
                if (!m_Actions.TryGetInstance(controlRequest.ActionInstanceId, out Float32ActionInstanceState action))
                    return;
                if (action.SkillId != skill.SkillId)
                    throw new InvalidOperationException($"Action instance '{controlRequest.ActionInstanceId}' does not belong to Skill '{skill.SkillId}'.");
                m_Lifecycle.Stop(action, controlRequest.Mode, controlRequest.Source, controlRequest.Reason);
                return;
            }
            m_Lifecycle.Stop(skill.SkillId, controlRequest.Mode, controlRequest.Source, controlRequest.Reason);
        }

        public bool StopIfEquipmentContextStale(Float32ActionInstanceState action)
        {
            if (!action.IsActive || !action.EquipmentContext.IsValid || m_EquipmentContext.IsCurrentActionContext(action.EquipmentContext))
                return false;
            m_Lifecycle.Stop(
                action,
                CharacterControlSkillStopMode.Force,
                action.Source,
                "EquipmentGenerationChanged");
            return true;
        }

		public bool TryCommitPendingControl(CharacterSkillId skillId)
		{
			CharacterSkillProgramBinding skill = m_Program.SkillPrograms.Require(skillId);
			ActionAdmissionProfile profile = RequireActionProfile(skill.ActionProfileId);
			return m_Activation.TryCommitPendingControl(skillId, profile);
		}

		public bool SubmitLifecycle(SimulationOperation operation)
		{
			return m_Lifecycle.Submit(
				GetStringConstant(operation, OperationNamedConstant.ActionContext, string.Empty),
				operation.Integer0,
				operation.Text0,
				SimulationExecutionSource.FromSkillOperation(operation.Handle, SourcePath(operation)));
		}

		public void ApplyIngress(SimulationIngress ingress)
		{
			if (ingress.Header.ActorId != m_Frame.ActorId)
				throw new InvalidOperationException($"Simulation ingress '{ingress.Header.FactIdentity}' targets '{ingress.Header.ActorId}', expected '{m_Frame.ActorId}'.");
			if (ingress.Header.Kind != SimulationIngressKind.ActionLifecycle)
				throw new InvalidOperationException($"Action runtime cannot apply ingress kind '{ingress.Header.Kind}'.");
			SimulationActionLifecycleIngress payload = ingress.ActionLifecycle;
			m_Lifecycle.ApplyIngress(new ActionSkillLifecycleIngress(
				ingress.Header.FactIdentity.ToString(),
				payload.ActionInstanceId,
				payload.PredictionKey,
				payload.InputSequence,
				(int)payload.TransitionType,
				ingress.Header.SourceTick,
				payload.Reason));
		}

		void EmitActionFact(SimulationExecutionSource source, Float32ActionInstanceState action)
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

		IEnumerable<ActionAdmissionActiveAction> IActionAdmissionReadPort.ActiveActions => EnumerateActiveActions();

		public bool IsActionInstanceStopComplete(ulong actionInstanceId)
		{
			if (!m_Actions.TryGetInstance(actionInstanceId, out Float32ActionInstanceState action))
				return true;
			if (action.IsActive || !action.SkillEntryOperation.IsValid)
				return false;
			if (m_Actions.IsSkillExecutionActive(actionInstanceId))
				return m_IsOperationStopComplete(action.SkillEntryOperation);
			if (!m_Actions.HasSkillExecutionFrame(actionInstanceId))
				return true;
			try
			{
				using (m_Actions.EnterSkillExecution(action))
					return m_IsOperationStopComplete(action.SkillEntryOperation);
			}
			catch (InvalidOperationException)
			{
				return false;
			}
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
			if (!m_InputRuntime.HasRequest(requestId, out Float32InputRequestState request))
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
			return m_Actions.HasPendingRequest(actionId);
		}

		void IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.StageRequest(
			ActionSkillActivationRequest<SimulationActionTargetSnapshot> request)
		{
			int slot = m_Actions.RequireRequestSlot(request.ActionId);
			m_Actions.WriteRequest(
				slot,
				new Float32ActionActivationRequestState(
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
					request.EquipmentContext,
					request.ReplacementActionInstanceId));
		}

		bool IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.TryReadPendingRequest(
			CharacterSkillId skillId,
			out ActionSkillActivationRequest<SimulationActionTargetSnapshot> request)
		{
			int slot = m_Actions.FindPendingSkill(skillId, out Float32ActionActivationRequestState staged);
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
				staged.EquipmentContext,
				staged.ReplacementActionInstanceId);
			return true;
		}

		void IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.ClearPendingRequest(
			ActionSkillActivationRequest<SimulationActionTargetSnapshot> request)
		{
			int slot = m_Actions.FindPendingRequest(
				request.ActionId,
				request.SkillId,
				request.SkillEntryOperation,
				request.ContextId,
				request.InputSequence,
				request.StartTick,
				request.ReplacementActionInstanceId);
			if (slot < 0)
				throw new InvalidOperationException($"Action '{request.ActionId}' has no matching pending activation request.");
			m_Actions.ClearRequest(slot);
		}

        ulong IActionSkillCommitPort<SimulationActionTargetSnapshot, Float32ActionInstanceState>.NextActionInstanceId() =>
            m_Handles.Next();

        ulong IActionSkillCommitPort<SimulationActionTargetSnapshot, Float32ActionInstanceState>.NextPredictionKey() =>
            m_Actions.NextSequence();

        Float32ActionInstanceState IActionSkillCommitPort<SimulationActionTargetSnapshot, Float32ActionInstanceState>.CreatePredictedAction(
            ActionSkillActivationRequest<SimulationActionTargetSnapshot> request,
            ulong instanceId,
            ulong predictionKey) =>
            new Float32ActionInstanceState(
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

        void IActionSkillCommitPort<SimulationActionTargetSnapshot, Float32ActionInstanceState>.WriteAction(
            Float32ActionInstanceState action) => m_Actions.WriteState(action);

        public bool TryReadSegmentGeneration(out ulong segmentGeneration)
        {
            if (m_Actions.TryGetCurrentSkillExecution(out Float32ActionInstanceState action))
            {
                segmentGeneration = action.SegmentGeneration;
                return true;
            }
            segmentGeneration = 0;
            return false;
        }

        public ulong AdvanceSegmentGeneration()
        {
            if (!m_Actions.TryGetCurrentSkillExecution(out Float32ActionInstanceState action))
                return 0;
            ulong next = checked(action.SegmentGeneration + 1);
            m_Actions.WriteState(action.WithSegmentGeneration(next));
            return next;
        }

        void IActionSkillCommitPort<SimulationActionTargetSnapshot, Float32ActionInstanceState>.SetActionTags(
            ulong actionInstanceId,
            IEnumerable<string> tags) => m_GameplayEffectActions.SetActionTags(actionInstanceId, tags);

		void IActionSkillCommitPort<SimulationActionTargetSnapshot, Float32ActionInstanceState>.ClearRequest(
			ActionSkillActivationRequest<SimulationActionTargetSnapshot> request) =>
			m_Actions.ClearRequest(m_Actions.FindPendingRequest(
				request.ActionId,
				request.SkillId,
				request.SkillEntryOperation,
				request.ContextId,
				request.InputSequence,
				request.StartTick,
				request.ReplacementActionInstanceId));

        void IActionSkillCommitPort<SimulationActionTargetSnapshot, Float32ActionInstanceState>.EmitActionFact(
            SimulationExecutionSource source,
            Float32ActionInstanceState action) => EmitActionFact(source, action);

		void IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.InterruptActive(
			ulong actionInstanceId,
			SimulationExecutionSource source,
			string reason)
		{
			if (!m_Actions.TryGetInstance(actionInstanceId, out Float32ActionInstanceState action) || !action.IsActive)
				throw new InvalidOperationException($"Action instance '{actionInstanceId}' is not active for replacement.");
			m_Lifecycle.Interrupt(action, source, reason);
		}

		bool IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.IsActionInstanceStopComplete(ulong actionInstanceId) =>
			IsActionInstanceStopComplete(actionInstanceId);

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

        void IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.TraceActionResult(
            SimulationOperation operation,
            string actionId,
            CharacterSkillId skillId,
            ulong actionInstanceId,
            ulong inputSequence,
            SimulationActionResultKind result,
            string reason) => m_Trace.AddActionResult(
                operation,
                actionId,
                skillId,
                actionInstanceId,
                inputSequence,
                result,
                reason);

        void IActionSkillActivationPort<SimulationActionTargetSnapshot, SimulationOperation>.TraceActionResult(
            SimulationExecutionSource source,
            string actionId,
            CharacterSkillId skillId,
            ulong actionInstanceId,
            ulong inputSequence,
            SimulationActionResultKind result,
            string reason) => m_Trace.AddActionResult(
                source,
                actionId,
                skillId,
                actionInstanceId,
                inputSequence,
                result,
                reason);

		ulong IActionSkillLifecyclePort<Float32ActionInstanceState>.Tick => m_Frame.Tick.Value;

		IEnumerable<Float32ActionInstanceState> IActionSkillLifecyclePort<Float32ActionInstanceState>.ActionStates =>
			EnumerateActionStates();

		bool IActionSkillLifecyclePort<Float32ActionInstanceState>.TryFindActive(
			string contextId,
			out Float32ActionInstanceState action) =>
			m_Actions.FindActive(contextId, out action) >= 0;

		bool IActionSkillLifecyclePort<Float32ActionInstanceState>.TryFindActive(
			CharacterSkillId skillId,
			out Float32ActionInstanceState action) =>
			m_Actions.FindActive(skillId, out action) >= 0;

		bool IActionSkillLifecyclePort<Float32ActionInstanceState>.IsActive(Float32ActionInstanceState action) => action.IsActive;

	string IActionSkillLifecyclePort<Float32ActionInstanceState>.ActionId(Float32ActionInstanceState action) => action.ActionId;

	CharacterSkillId IActionSkillLifecyclePort<Float32ActionInstanceState>.SkillId(Float32ActionInstanceState action) => action.SkillId;

		ulong IActionSkillLifecyclePort<Float32ActionInstanceState>.InstanceId(Float32ActionInstanceState action) => action.InstanceId;

		ulong IActionSkillLifecyclePort<Float32ActionInstanceState>.PredictionKey(Float32ActionInstanceState action) => action.PredictionKey;

		ulong IActionSkillLifecyclePort<Float32ActionInstanceState>.InputSequence(Float32ActionInstanceState action) => action.InputSequence;

		string IActionSkillLifecyclePort<Float32ActionInstanceState>.Reason(Float32ActionInstanceState action) => action.Reason;

		SimulationExecutionSource IActionSkillLifecyclePort<Float32ActionInstanceState>.Source(Float32ActionInstanceState action) => action.Source;

		EquipmentActionContext IActionSkillLifecyclePort<Float32ActionInstanceState>.EquipmentContext(Float32ActionInstanceState action) => action.EquipmentContext;

		ActionSkillLifecyclePhase IActionSkillLifecyclePort<Float32ActionInstanceState>.Phase(Float32ActionInstanceState action) =>
			(ActionSkillLifecyclePhase)(byte)action.Phase;

		ActionSkillLifecycleState IActionSkillLifecyclePort<Float32ActionInstanceState>.State(Float32ActionInstanceState action) =>
			(ActionSkillLifecycleState)(byte)action.State;

		IDisposable IActionSkillLifecyclePort<Float32ActionInstanceState>.EnterExecution(Float32ActionInstanceState action)
		{
			if (!action.SkillId.IsValid)
				return null;
			if (m_Actions.IsSkillExecutionActive(action.InstanceId))
				return null;
			if (m_Actions.TryGetCurrentSkillExecution(out Float32ActionInstanceState current))
			{
				if (current.InstanceId != action.InstanceId)
					throw new InvalidOperationException("Skill execution lifecycle scope does not match the active Action instance.");
				return null;
			}
			return m_Actions.EnterSkillExecution(action);
		}

		Float32ActionInstanceState IActionSkillLifecyclePort<Float32ActionInstanceState>.WithLifecycle(
			Float32ActionInstanceState action,
			ActionSkillLifecycleUpdate update) =>
			action.WithLifecycle(
				(SimulationActionPhase)(byte)update.Phase,
				(SimulationActionState)(byte)update.State,
				(SimulationActionLifecycleTransitionType)(byte)update.Transition,
				update.TransitionTick,
				update.SourceTick,
				update.Reason);

		void IActionSkillLifecyclePort<Float32ActionInstanceState>.WriteState(Float32ActionInstanceState action) =>
			m_Actions.WriteState(action);

		void IActionSkillLifecyclePort<Float32ActionInstanceState>.EmitActionFact(
			SimulationExecutionSource source,
			Float32ActionInstanceState action) => EmitActionFact(source, action);

		void IActionSkillLifecyclePort<Float32ActionInstanceState>.ClearTerminalResources(ulong actionInstanceId)
		{
			m_GameplayEffectActions.RemoveActionTags(actionInstanceId);
			m_GameplayEffectActions.ClearConfirmedAction(actionInstanceId);
			m_Blackboard.ClearActionInstanceScopes(actionInstanceId);
		}

		ulong IActionSkillLifecyclePort<Float32ActionInstanceState>.SourceGeneration(SimulationExecutionSource source) =>
			SourceGeneration(source);

		bool IActionSkillLifecyclePort<Float32ActionInstanceState>.TraceEnabled => m_Trace.Enabled;

	void IActionSkillLifecyclePort<Float32ActionInstanceState>.Trace(
			SimulationExecutionSource source,
			string code,
			ActionSkillTraceSeverity severity,
			string detail,
			ulong generation) => m_Trace.Add(source, code, ToTraceSeverity(severity), detail, generation);

	void IActionSkillLifecyclePort<Float32ActionInstanceState>.TraceActionResult(
			SimulationExecutionSource source,
			string actionId,
			CharacterSkillId skillId,
			ulong actionInstanceId,
			ulong inputSequence,
			SimulationActionResultKind result,
			string reason) => m_Trace.AddActionResult(
			source,
			actionId,
			skillId,
			actionInstanceId,
			inputSequence,
			result,
			reason,
			SourceGeneration(source));

		IEnumerable<Float32ActionInstanceState> EnumerateActionStates()
		{
			foreach (TypedActionStateAddresses addresses in m_Layout.AllActionStateAddresses)
				yield return m_Actions.ReadSlot(addresses.Instance.SlotIndex);
		}

		IEnumerable<ActionAdmissionActiveAction> EnumerateActiveActions()
		{
			foreach (Float32ActionInstanceState action in EnumerateActionStates())
			{
				if (action.IsActive)
					yield return new ActionAdmissionActiveAction(action.ActionId, action.InstanceId);
			}
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

		static SimulationTraceSeverity ToTraceSeverity(ActionSkillTraceSeverity severity) =>
			severity == ActionSkillTraceSeverity.Detail
				? SimulationTraceSeverity.Detail
				: SimulationTraceSeverity.Information;
	}
}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         
