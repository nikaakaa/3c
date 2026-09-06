using ThirdPersonSimulation;
using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation.Fixed
{
	internal sealed class FixedEquipmentRuntime : FixedOperationModule,
		IEquipmentRuntimePort,
		IEquipmentActionContextProvider
	{
		readonly FixedEvaluationFrame m_Frame;
		readonly FixedStatePort m_State;
		readonly FixedActionStateStore m_Actions;
		readonly FixedHandleAllocator m_Handles;
		readonly FixedGameplayEffectOperationRuntime m_GameplayEffects;
		readonly FixedFactSink m_Facts;
		readonly FixedTraceSink m_Trace;
		readonly EquipmentRuntimeControl m_Control;
		readonly Dictionary<int, EquipmentChangeOutcome> m_Outcomes = new Dictionary<int, EquipmentChangeOutcome>();
		public FixedEquipmentRuntime(
			FixedProgramAccess access,
			FixedEvaluationFrame frame,
			FixedStatePort state,
			FixedActionStateStore actions,
			FixedHandleAllocator handles,
			FixedGameplayEffectOperationRuntime gameplayEffects,
			FixedFactSink facts,
			FixedTraceSink trace)
			: base(access)
		{
			m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
			m_State = state ?? throw new ArgumentNullException(nameof(state));
			m_Actions = actions ?? throw new ArgumentNullException(nameof(actions));
			m_Handles = handles ?? throw new ArgumentNullException(nameof(handles));
			m_GameplayEffects = gameplayEffects ?? throw new ArgumentNullException(nameof(gameplayEffects));
			m_Facts = facts ?? throw new ArgumentNullException(nameof(facts));
			m_Trace = trace ?? throw new ArgumentNullException(nameof(trace));
			m_Control = new EquipmentRuntimeControl(this);
		}

		public bool TryReadActionContext(EquipmentActionRouteId routeId, out EquipmentActionContext context)
		{
			context = default;
			if (!m_Layout.Equipment.CapabilityEnabled || !routeId.IsValid)
				return false;
			EquipmentProgramRoute route = m_Layout.Equipment.RequireRoute(routeId);
			EquipmentSlotState slot = ReadState().RequireSlot(route.OwnerSlotId);
			if (!slot.IsEquipped)
				return false;
			if (!m_Layout.Equipment.TryGetRouteImplementation(slot.FeatureId, routeId, out _))
			{
				if (route.MissingImplementation == EquipmentRouteMissingImplementation.RejectComposition)
					throw new InvalidOperationException($"Equipment Route '{routeId}' has no implementation for Feature '{slot.FeatureId}'.");
				return false;
			}
			context = slot.ActionContext(routeId);
			return context.IsValid;
		}

		public bool IsSkillBinding(EquipmentActionContext context, CharacterSkillId skillId)
		{
			if (!context.IsValid || !skillId.IsValid || !IsCurrentActionContext(context))
				return false;
			return m_Layout.Equipment.TryGetRouteImplementation(context.FeatureId, context.RouteId, out EquipmentProgramRouteImplementation implementation) &&
				implementation.SkillId == skillId;
		}

		public bool IsCurrentActionContext(EquipmentActionContext context)
		{
			if (!context.IsValid || !m_Layout.Equipment.TryGetRouteImplementation(context.FeatureId, context.RouteId, out _))
				return false;
			EquipmentProgramRoute route = m_Layout.Equipment.RequireRoute(context.RouteId);
			EquipmentSlotState slot = ReadState().RequireSlot(route.OwnerSlotId);
			return slot.IsEquipped &&
				slot.SlotId == context.SlotId &&
				slot.EquipmentId == context.EquipmentId &&
				slot.FeatureId == context.FeatureId &&
				slot.Revision == context.EquipmentRevision;
		}

		public void BeginEvaluation()
		{
			m_Outcomes.Clear();
			if (!m_Layout.Equipment.CapabilityEnabled)
				return;
			SimulationOperation source = m_Program.Operations[m_Layout.RootOperation.Value];
			m_Control.InitializeContributions(source.Handle);
			m_Control.CancelOrphanedPending(source.Handle);
			TraceSnapshot(source);
		}

		public void EndEvaluation()
		{
			if (!m_Layout.Equipment.CapabilityEnabled)
				return;
			SimulationOperation source = m_Program.Operations[m_Layout.RootOperation.Value];
			m_Control.CancelOrphanedPending(source.Handle);
		}

		public CharacterStateValue Evaluate(SimulationOperation operation, string outputPort, FixedValueInputLease inputs)
		{
			switch (operation.Code)
			{
				case SimulationOperationCode.ReadEquipmentIdentity:
				{
					EquipmentSlotState slot = RequireSlotState(operation);
					if (string.Equals(outputPort, "m_Equipment", StringComparison.Ordinal) ||
						string.Equals(outputPort, "m_Output", StringComparison.Ordinal))
						return CharacterStateValue.FromIdentity(slot.EquipmentId.Value ?? string.Empty);
					if (string.Equals(outputPort, "m_Feature", StringComparison.Ordinal))
						return CharacterStateValue.FromIdentity(slot.FeatureId.Value ?? string.Empty);
					if (string.Equals(outputPort, "m_Revision", StringComparison.Ordinal))
						return CharacterStateValue.FromUInt64(slot.Revision);
					if (string.Equals(outputPort, "m_Equipped", StringComparison.Ordinal))
						return CharacterStateValue.FromBoolean(slot.IsEquipped);
					throw new InvalidOperationException($"Equipment identity output '{outputPort}' is unknown.");
				}
				case SimulationOperationCode.ReadEquipmentParameter:
					return ReadParameter(operation, inputs);
				case SimulationOperationCode.RequestEquipmentChange:
				case SimulationOperationCode.BeginEquipmentChange:
				case SimulationOperationCode.CommitEquipmentChange:
				case SimulationOperationCode.CancelEquipmentChange:
					return ReadOutcome(operation, outputPort);
				default:
					throw new InvalidOperationException($"Operation '{operation.Code}' is not an Equipment value operation.");
			}
		}

		public bool Execute<TTarget>(OperationControlCursor<TTarget> cursor, SimulationOperation operation, FixedValueInputLease inputs)
			where TTarget : struct, IOperationControlTarget<TTarget>
		{
			switch (operation.Code)
			{
				case SimulationOperationCode.RequestEquipmentChange:
					return Capture(operation, m_Control.Validate(BuildChangeRequest(operation, inputs))).Succeeded;
				case SimulationOperationCode.BeginEquipmentChange:
					return Capture(operation, m_Control.Begin(BuildChangeRequest(operation, inputs))).Succeeded;
				case SimulationOperationCode.CommitEquipmentChange:
				{
					EquipmentChangeOutcome outcome = m_Control.Commit(
						operation.Handle,
						new EquipmentChangeId(ReadUInt64(inputs)));
					return Capture(operation, outcome).Succeeded;
				}
				case SimulationOperationCode.CancelEquipmentChange:
				{
					EquipmentChangeOutcome outcome = m_Control.Cancel(
						operation.Handle,
						new EquipmentChangeId(ReadUInt64(inputs)));
					return Capture(operation, outcome).Succeeded;
				}
				default:
					throw new InvalidOperationException($"Operation '{operation.Code}' is not an Equipment execution operation.");
			}
		}


		CharacterStateValue ReadParameter(SimulationOperation operation, FixedValueInputLease inputs)
		{
			EquipmentSlotState slot = RequireSlotState(operation);
			ulong expectedRevision = ReadUInt64(inputs);
			if (!slot.IsEquipped || expectedRevision == 0)
				throw new InvalidOperationException($"Equipment parameter operation '{SourcePath(operation)}' requires an explicit Slot revision.");
			if (slot.Revision != expectedRevision)
				throw new InvalidOperationException($"Equipment parameter operation '{SourcePath(operation)}' revision is stale.");
			EquipmentParameterId parameterId = m_Layout.Equipment.RequireOperationParameter(operation.Handle);
			EquipmentProgramParameter parameter = m_Layout.Equipment.RequireParameter(slot.EquipmentId, parameterId);
			if (parameter.FeatureId != slot.FeatureId)
				throw new InvalidOperationException($"Equipment parameter '{parameterId}' does not belong to Feature '{slot.FeatureId}'.");
			ProgramConstant constant = m_Program.Constants[parameter.ConstantIndex];
			return CharacterStateValue.FromConstant(constant, ToStateKind(parameter.ValueKind));
		}

		EquipmentChangeRequest BuildChangeRequest(SimulationOperation operation, FixedValueInputLease inputs)
		{
			EquipmentProgramSlot slot = RequireSlot(operation);
			EquipmentId target = TryRequireItem(operation, out EquipmentProgramItem item) ? item.EquipmentId : default;
			EquipmentSlotState current = ReadState().RequireSlot(slot.SlotId);
			ulong expectedRevision = ReadUInt64(inputs);
			if (expectedRevision == 0)
				throw new InvalidOperationException($"Equipment change operation '{SourcePath(operation)}' requires an explicit revision.");
			ulong actionInstanceId = 0;
			string actionContext = GetStringConstant(operation, OperationNamedConstant.ActionContext, string.Empty);
			if (!string.IsNullOrEmpty(actionContext) && m_Actions.FindActive(actionContext, out FixedActionInstanceState action) >= 0)
				actionInstanceId = action.InstanceId;
			return new EquipmentChangeRequest(slot.SlotId, target, expectedRevision, actionInstanceId);
		}

		static ulong ReadUInt64(FixedValueInputLease inputs)
		{
			CharacterStateValue value = inputs.FindByKind(ProgramStateValueKind.UInt64);
			return value.Kind == ProgramStateValueKind.UInt64 ? value.UInt64 : 0;
		}

		CharacterStateValue ReadOutcome(SimulationOperation operation, string outputPort)
		{
			if (!m_Outcomes.TryGetValue(operation.Handle.Value, out EquipmentChangeOutcome outcome))
				throw new InvalidOperationException($"Equipment change operation '{SourcePath(operation)}' has not executed in the current evaluation.");
			if (string.Equals(outputPort, "m_Accepted", StringComparison.Ordinal) ||
				string.Equals(outputPort, "m_Begun", StringComparison.Ordinal) ||
				string.Equals(outputPort, "m_Committed", StringComparison.Ordinal) ||
				string.Equals(outputPort, "m_Cancelled", StringComparison.Ordinal))
				return CharacterStateValue.FromBoolean(outcome.Succeeded);
			if (string.Equals(outputPort, "m_ChangeId", StringComparison.Ordinal))
				return CharacterStateValue.FromUInt64(outcome.ChangeId.Value);
			if (string.Equals(outputPort, "m_Failure", StringComparison.Ordinal))
				return CharacterStateValue.FromInt32((int)outcome.Failure);
			throw new InvalidOperationException($"Equipment change output '{outputPort}' is unknown.");
		}

		EquipmentChangeOutcome Capture(SimulationOperation operation, EquipmentChangeOutcome outcome)
		{
			m_Outcomes[operation.Handle.Value] = outcome;
			TraceOutcome(operation, outcome);
			return outcome;
		}

		EquipmentProgramSlot RequireSlot(SimulationOperation operation)
		{
			return m_Layout.Equipment.RequireSlot(m_Layout.Equipment.RequireOperationSlot(operation.Handle));
		}

		EquipmentSlotState RequireSlotState(SimulationOperation operation) => ReadState().RequireSlot(RequireSlot(operation).SlotId);

		bool TryRequireItem(SimulationOperation operation, out EquipmentProgramItem item)
		{
			if (!m_Layout.Equipment.TryGetOperationEquipment(operation.Handle, out EquipmentId equipmentId))
			{
				item = null;
				return false;
			}
			item = m_Layout.Equipment.RequireItem(equipmentId);
			return true;
		}

		void TraceOutcome(SimulationOperation operation, EquipmentChangeOutcome outcome)
		{
			if (m_Trace.Enabled)
				m_Trace.Add(operation, "equipment_change", outcome.Succeeded ? SimulationTraceSeverity.Information : SimulationTraceSeverity.Warning, $"{operation.Code}:success={outcome.Succeeded}:change={outcome.ChangeId.Value}:failure={outcome.Failure}");
		}

		void TraceSnapshot(SimulationOperation source)
		{
			if (!m_Trace.Enabled)
				return;
			EquipmentStateAggregate aggregate = ReadState();
			for (int i = 0; i < aggregate.Slots.Count; i++)
			{
				EquipmentSlotState slot = aggregate.Slots[i];
				m_Trace.Add(source, "equipment_snapshot", SimulationTraceSeverity.Detail, $"slot={slot.SlotId}:equipment={slot.EquipmentId}:feature={slot.FeatureId}:revision={slot.Revision}:generation={slot.Generation}:tagSource={slot.TagSource}:effects={string.Join(",", slot.PassiveEffectHandles)}");
			}
			PendingEquipmentChange pending = aggregate.PendingChange;
			if (pending.IsValid)
				m_Trace.Add(source, "equipment_snapshot", SimulationTraceSeverity.Detail, $"pending={pending.ChangeId}:{pending.SlotId}:{pending.FromEquipmentId}->{pending.ToEquipmentId}:action={pending.SourceActionInstanceId}:begin={pending.BeginTick}");
			PendingEquipmentChange resolved = aggregate.LastResolvedChange;
			if (resolved.IsValid)
				m_Trace.Add(source, "equipment_snapshot", SimulationTraceSeverity.Detail, $"resolved={resolved.ChangeId}:{resolved.State}:{resolved.SlotId}:{resolved.FromEquipmentId}->{resolved.ToEquipmentId}:begin={resolved.BeginTick}:tick={resolved.ResolvedTick}");
		}

		static ProgramStateValueKind ToStateKind(EquipmentParameterValueKind kind) => kind switch
		{
			EquipmentParameterValueKind.Boolean => ProgramStateValueKind.Boolean,
			EquipmentParameterValueKind.Int32 => ProgramStateValueKind.Int32,
			EquipmentParameterValueKind.Scalar => ProgramStateValueKind.Scalar,
			EquipmentParameterValueKind.Vector2 => ProgramStateValueKind.Vector2,
			EquipmentParameterValueKind.Vector3 => ProgramStateValueKind.Vector3,
			EquipmentParameterValueKind.Yaw => ProgramStateValueKind.Yaw,
			EquipmentParameterValueKind.GameplayTag => ProgramStateValueKind.Identity,
			EquipmentParameterValueKind.GameplayEffect => ProgramStateValueKind.Identity,
			EquipmentParameterValueKind.AnimationProducer => ProgramStateValueKind.Identity,
			_ => throw new InvalidOperationException($"Equipment parameter kind '{kind}' is unsupported.")
		};

		ActorId IEquipmentRuntimePort.ActorId => m_Frame.ActorId;
		ulong IEquipmentRuntimePort.Tick => m_Frame.Tick.Value;
		EquipmentProgramLayout IEquipmentRuntimePort.Layout => m_Layout.Equipment;
		public EquipmentStateAggregate ReadState() => m_State.Get(m_Layout.EquipmentAggregateAddress.SlotIndex).EquipmentAggregate;
		public void WriteState(EquipmentStateAggregate state) => m_State.Set(m_Layout.EquipmentAggregateAddress.SlotIndex, CharacterStateValue.FromEquipmentAggregate(state));
		EquipmentChangeId IEquipmentRuntimePort.AllocateChangeId() => new EquipmentChangeId(m_Handles.Next());
		bool IEquipmentRuntimePort.HasActiveActionConflict(EquipmentSlotState slot, ulong sourceActionInstanceId)
		{
			FixedActionInstanceState active = m_Actions.FindOnlyActive();
			return active.IsActive && active.InstanceId != sourceActionInstanceId && active.EquipmentContext.IsValid &&
				active.EquipmentContext.SlotId == slot.SlotId && active.EquipmentContext.EquipmentRevision == slot.Revision;
		}
		bool IEquipmentRuntimePort.IsActionActive(ulong actionInstanceId)
		{
			FixedActionInstanceState active = m_Actions.FindOnlyActive();
			return active.IsActive && active.InstanceId == actionInstanceId;
		}
		void IEquipmentRuntimePort.ResetLocalState(int stateSlotIndex) => m_State.Reset(stateSlotIndex);
		void IEquipmentRuntimePort.SetTags(string sourceId, IReadOnlyList<string> tags) => m_GameplayEffects.SetEquipmentTags(sourceId, tags);
		void IEquipmentRuntimePort.RemoveTags(string sourceId) => m_GameplayEffects.RemoveEquipmentTags(sourceId);
		ulong IEquipmentRuntimePort.ApplyPassiveEffect(string effectId) => m_GameplayEffects.ApplyEquipmentPassive(effectId);
		void IEquipmentRuntimePort.RemovePassiveEffect(ulong handle) => m_GameplayEffects.RemoveEquipmentPassive(handle);
		IEquipmentMutationScope IEquipmentRuntimePort.BeginMutation() => new MutationScope(m_Frame);
		void IEquipmentRuntimePort.CommitEffectOutputs(OperationHandle source) => m_GameplayEffects.CommitEquipmentMutation(RequireSource(source));
		void IEquipmentRuntimePort.CancelEffectOutputs() => m_GameplayEffects.CancelEquipmentMutation();
		void IEquipmentRuntimePort.EmitLifecycle(OperationHandle source, EquipmentSlotState before, EquipmentSlotState after, PendingEquipmentChangeState state, EquipmentChangeId changeId)
		{
			SimulationEventHeader header = m_Facts.Next(RequireSource(source));
			m_Facts.Add(new GameplayFact(
				header,
				GameplayFactKind.State,
				$"equipment:{after.SlotId.Value}",
				$"{state}:{changeId.Value}:{before.EquipmentId.Value}->{after.EquipmentId.Value}@{after.Revision}",
				FixedScalar.Zero));
		}

		SimulationOperation RequireSource(OperationHandle source)
		{
			if (!source.IsValid || source.Value >= m_Program.Operations.Count)
				throw new InvalidOperationException($"Equipment source Operation '{source}' is absent from the Program.");
			SimulationOperation operation = m_Program.Operations[source.Value];
			if (!operation.Handle.Equals(source))
				throw new InvalidOperationException($"Equipment source Operation '{source}' does not match Program order.");
			return operation;
		}

		sealed class MutationScope : IEquipmentMutationScope
		{
			readonly FixedEvaluationFrame m_Frame;
			readonly FixedCharacterStateSavepoint m_Savepoint;
			readonly FixedEvaluationOutputSavepoint m_OutputSavepoint;
			readonly CharacterStateValue[] m_Values;
			bool m_Completed;

			public MutationScope(FixedEvaluationFrame frame)
			{
				m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
				m_Savepoint = frame.Transaction.CreateSavepoint();
				m_OutputSavepoint = frame.CreateOutputSavepoint();
				m_Values = new CharacterStateValue[frame.Program.StateSlots.Count];
				for (int i = 0; i < m_Values.Length; i++)
					if (frame.Program.StateSlots[i].ValueKind != ProgramStateValueKind.GameplayEffectAggregate)
						m_Values[i] = frame.Transaction.Get(i);
			}

			public void Complete()
			{
				if (m_Completed)
					throw new InvalidOperationException("Equipment mutation scope is already completed.");
				m_Frame.Transaction.Release(m_Savepoint);
				m_Completed = true;
			}

			public void Dispose()
			{
				if (m_Completed)
					return;
				m_Frame.RestoreOutput(m_OutputSavepoint);
				m_Frame.Transaction.Restore(m_Savepoint);
				for (int i = 0; i < m_Values.Length; i++)
					if (m_Frame.Program.StateSlots[i].ValueKind != ProgramStateValueKind.GameplayEffectAggregate)
						m_Frame.Transaction.Set(i, m_Values[i]);
				m_Completed = true;
			}
		}
	}
}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        
