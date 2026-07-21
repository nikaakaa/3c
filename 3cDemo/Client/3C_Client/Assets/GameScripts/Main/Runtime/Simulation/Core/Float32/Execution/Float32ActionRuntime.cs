using System;
using System.Collections.Generic;
using System.Globalization;

namespace ThirdPersonSimulation
{
	internal sealed class Float32ActionRuntime : Float32OperationModule, IFloat32ActionAdmissionQuery, IActionAdmissionReadPort
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
		readonly ActionAdmissionControl m_Admission;

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
			SimulationActionTargetSnapshot targetSnapshot = ReadActionTargetSnapshot(cursor, operation);
			return ActivateCore(operation, targetSnapshot).Activated;
		}

		ActionActivationOutcome ActivateCore(
			SimulationOperation operation,
			SimulationActionTargetSnapshot targetSnapshot)
		{
			ActionAdmissionProfile profile = RequireActionProfile(operation);
			string actionId = profile.ActionId;
			targetSnapshot = SelectTargetSnapshot(profile, targetSnapshot);
			string contextId = GetStringConstant(operation, OperationNamedConstant.ActionContext, string.Empty);
			if (string.IsNullOrEmpty(contextId))
				throw new InvalidOperationException($"Action operation '{SourcePath(operation)}' has no formal Action Context.");

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
				return new ActionActivationOutcome(false, admission.RejectReason);
			}

			string requestId = GetStringConstant(operation, OperationNamedConstant.SourceInputRequest, string.Empty);
			ulong inputSequence = m_Frame.Input.Sequence;
			if (!string.IsNullOrEmpty(requestId))
			{
				if (!m_InputRuntime.HasRequest(requestId, out Float32InputRequestState inputRequest))
				{
					if (m_Trace.Enabled)
						m_Trace.Add(operation, "action_request_unavailable", SimulationTraceSeverity.Detail, $"{requestId}:{m_Frame.Tick.Value}");
					return new ActionActivationOutcome(false, ActionAdmissionRejectReason.None);
				}
				inputSequence = inputRequest.Sequence;
				if (GetBooleanConstant(operation, OperationNamedConstant.ConsumeSourceInputRequest, true))
					m_InputRuntime.ClearRequest(requestId);
			}

			var request = new Float32ActionActivationRequestState(
				actionId,
				contextId,
				requestId,
				inputSequence,
				m_Frame.Tick.Value,
				GetStringConstant(operation, OperationNamedConstant.TargetKey, string.Empty),
				targetSnapshot,
				operation.Handle,
				m_EquipmentContext.Current);
			int requestSlot = m_Actions.RequireSlot(actionId, ProgramStateSemantic.ActionRequestBuffer);
			m_Actions.WriteRequest(requestSlot, request);
			try
			{
				Float32ActionActivationRequestState staged = m_Actions.ReadRequest(requestSlot);
				ulong instanceId = m_Handles.Next();
				ulong predictionKey = m_Actions.NextSequence();
				var instance = new Float32ActionInstanceState(
					staged.ActionId,
					staged.ContextId,
					instanceId,
					predictionKey,
					staged.SourceInputRequestId,
					staged.InputSequence,
					staged.StartTick,
					staged.TargetKey,
					staged.TargetSnapshot,
					staged.SourceOperation,
					SimulationActionPhase.Startup,
					SimulationActionState.Predicted,
					SimulationActionLifecycleTransitionType.None,
					staged.StartTick,
					0,
					string.Empty,
					staged.EquipmentContext);
				m_Actions.WriteState(instance);
				m_GameplayEffectActions.SetActionTags(instanceId, profile.Tags);
				EmitActionFact(operation, instance);
				if (m_Trace.Enabled)
					m_Trace.Add(operation, "action_activated", SimulationTraceSeverity.Information, $"{actionId}:{instanceId}:request={requestId}:sequence={inputSequence}:requirement={profile.TargetRequirement}:candidate={targetSnapshot.TargetId}:captured={instance.TargetSnapshot.TargetId}:captureTick={instance.StartTick}:targetPosition={instance.TargetSnapshot.Position}:targetYaw={instance.TargetSnapshot.Yaw}:equipment={instance.EquipmentContext}");
				return new ActionActivationOutcome(true, ActionAdmissionRejectReason.None);
			}
			finally
			{
				m_Actions.ClearRequest(requestSlot);
			}
		}

		readonly struct ActionActivationOutcome
		{
			public ActionActivationOutcome(bool activated, ActionAdmissionRejectReason rejectReason)
			{
				Activated = activated;
				RejectReason = rejectReason;
			}

			public bool Activated { get; }
			public ActionAdmissionRejectReason RejectReason { get; }
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

		public bool SubmitLifecycle(SimulationOperation operation)
		{
			string contextId = GetStringConstant(operation, OperationNamedConstant.ActionContext, string.Empty);
			int slot = m_Actions.FindActive(contextId, out Float32ActionInstanceState action);
			if (slot < 0)
				return false;
			SimulationActionLifecycleTransitionType transition = RequireActionTransition(operation.Integer0);
			ApplyActionTransition(operation, action, transition, operation.Text0, 0);
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
			Float32ActionInstanceState match = default;
			int matches = 0;
			foreach (TypedActionStateAddresses addresses in m_Layout.ActionStateIndex.Values)
			{
				Float32ActionInstanceState candidate = m_Actions.ReadSlot(addresses.Instance.SlotIndex);
				if (!candidate.IsActive || !MatchesActionIngress(candidate, payload))
					continue;
				match = candidate;
				matches++;
			}
			if (matches != 1)
				throw new InvalidOperationException($"Action lifecycle ingress '{ingress.Header.FactIdentity}' matched {matches} active Action instances.");
			SimulationOperation source = m_Program.Operations[match.SourceOperation.Value];
			ApplyActionTransition(
				source,
				match,
				payload.TransitionType,
				payload.Reason,
				ingress.Header.SourceTick);
		}

		void ApplyActionTransition(
			SimulationOperation source,
			Float32ActionInstanceState action,
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

			Float32ActionInstanceState next = action.WithLifecycle(
				phase,
				state,
				transition,
				m_Frame.Tick.Value,
				sourceTick,
				reason);
			m_Actions.WriteState(next);
			EmitActionFact(source, next);
			if (m_Trace.Enabled)
				m_Trace.Add(source, "action_lifecycle", SimulationTraceSeverity.Information, $"{next.ActionId}:{next.InstanceId}:{transition}:{next.Reason}:equipment={next.EquipmentContext}");
			if (!next.IsActive)
			{
				m_GameplayEffectActions.RemoveActionTags(next.InstanceId);
				m_GameplayEffectActions.ClearConfirmedAction(next.InstanceId);
				m_Blackboard.ClearActionInstanceScopes(next.InstanceId);
			}
		}

		void EmitActionFact(SimulationOperation source, Float32ActionInstanceState action)
		{
			SimulationEventHeader header = m_Facts.Next(source);
			m_Facts.Add(new GameplayFact(header, new ActionFact(
				action.InstanceId,
				action.PredictionKey,
				action.InputSequence,
				action.ActionId,
				action.LastTransition,
				action.Phase,
				action.State,
				action.Reason,
				action.EquipmentContext)));
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
			Float32ActionInstanceState active = m_Actions.FindOnlyActive();
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

		static bool MatchesActionIngress(Float32ActionInstanceState action, SimulationActionLifecycleIngress ingress)
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
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         
