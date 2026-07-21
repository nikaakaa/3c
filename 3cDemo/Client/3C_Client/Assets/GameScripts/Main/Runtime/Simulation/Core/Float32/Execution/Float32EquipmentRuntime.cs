using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
	internal sealed class Float32EquipmentRuntime : Float32OperationModule,
		IEquipmentRuntimePort,
		IEquipmentActionContextProvider
	{
		readonly Float32EvaluationFrame m_Frame;
		readonly Float32StatePort m_State;
		readonly Float32ActionStateStore m_Actions;
		readonly IFloat32InputPort m_Input;
		readonly Float32HandleAllocator m_Handles;
		readonly Float32GameplayEffectOperationRuntime m_GameplayEffects;
		readonly Float32FactSink m_Facts;
		readonly Float32TraceSink m_Trace;
		readonly EquipmentRuntimeControl m_Control;
		readonly Dictionary<int, EquipmentChangeOutcome> m_Outcomes = new Dictionary<int, EquipmentChangeOutcome>();
		EquipmentActionContext m_CurrentContext;

		public Float32EquipmentRuntime(
			Float32ProgramAccess access,
			Float32EvaluationFrame frame,
			Float32StatePort state,
			Float32ActionStateStore actions,
			IFloat32InputPort input,
			Float32HandleAllocator handles,
			Float32GameplayEffectOperationRuntime gameplayEffects,
			Float32FactSink facts,
			Float32TraceSink trace)
			: base(access)
		{
			m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
			m_State = state ?? throw new ArgumentNullException(nameof(state));
			m_Actions = actions ?? throw new ArgumentNullException(nameof(actions));
			m_Input = input ?? throw new ArgumentNullException(nameof(input));
			m_Handles = handles ?? throw new ArgumentNullException(nameof(handles));
			m_GameplayEffects = gameplayEffects ?? throw new ArgumentNullException(nameof(gameplayEffects));
			m_Facts = facts ?? throw new ArgumentNullException(nameof(facts));
			m_Trace = trace ?? throw new ArgumentNullException(nameof(trace));
			m_Control = new EquipmentRuntimeControl(this);
		}

		public EquipmentActionContext Current => m_CurrentContext;

		public void BeginEvaluation()
		{
			if (m_CurrentContext.IsValid)
				throw new InvalidOperationException("Equipment Action Context leaked across evaluations.");
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

		public CharacterStateValue Evaluate(SimulationOperation operation, string outputPort, Float32ValueInputLease inputs)
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

		public bool Execute<TTarget>(OperationControlCursor<TTarget> cursor, SimulationOperation operation, Float32ValueInputLease inputs)
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
						new EquipmentChangeId(ReadUInt64(inputs)),
						outgoing => AbortPersistent(cursor, outgoing, operation.Handle));
					return Capture(operation, outcome).Succeeded;
				}
				case SimulationOperationCode.CancelEquipmentChange:
				{
					EquipmentChangeOutcome outcome = m_Control.Cancel(
						operation.Handle,
						new EquipmentChangeId(ReadUInt64(inputs)));
					return Capture(operation, outcome).Succeeded;
				}
				case SimulationOperationCode.EnterEquipmentFeatureHost:
					return TickPersistent(cursor, operation) == OperationExecutionResult.Running;
				case SimulationOperationCode.ExitEquipmentFeatureHost:
					StopPersistent(cursor, operation, operation.Handle);
					return true;
				case SimulationOperationCode.ResolveEquipmentActionRoute:
					return TickRoute(cursor, operation) != OperationExecutionResult.Failure;
				default:
					throw new InvalidOperationException($"Operation '{operation.Code}' is not an Equipment execution operation.");
			}
		}

		public OperationExecutionResult TickHost<TTarget>(OperationControlCursor<TTarget> cursor, SimulationOperation operation, Float32ValueInputLease inputs)
			where TTarget : struct, IOperationControlTarget<TTarget>
		{
			return operation.Code switch
			{
				SimulationOperationCode.EnterEquipmentFeatureHost => TickPersistent(cursor, operation),
				SimulationOperationCode.ResolveEquipmentActionRoute => TickRoute(cursor, operation),
				_ => Execute(cursor, operation, inputs) ? OperationExecutionResult.Success : OperationExecutionResult.Failure
			};
		}

		public void ForceStopHost<TTarget>(
			OperationControlCursor<TTarget> cursor,
			SimulationOperation operation,
			OperationStopContext context)
			where TTarget : struct, IOperationControlTarget<TTarget>
		{
			if (operation.Code == SimulationOperationCode.EnterEquipmentFeatureHost)
				StopPersistent(cursor, operation, context.Source);
			else if (operation.Code == SimulationOperationCode.ResolveEquipmentActionRoute)
			{
				EquipmentProgramRoute route = RequireRoute(operation);
				EquipmentSlotState slot = ReadState().RequireSlot(route.OwnerSlotId);
				if (slot.IsEquipped && m_Layout.Equipment.TryGetRouteImplementation(slot.FeatureId, route.RouteId, out EquipmentProgramRouteImplementation implementation))
					cursor.ForceStop(implementation.EntryOperation, context);
			}
		}

		CharacterStateValue ReadParameter(SimulationOperation operation, Float32ValueInputLease inputs)
		{
			EquipmentSlotState slot;
			if (m_CurrentContext.IsValid)
			{
				slot = ReadState().RequireSlot(m_CurrentContext.SlotId);
				if (!slot.IsEquipped || slot.EquipmentId != m_CurrentContext.EquipmentId || slot.FeatureId != m_CurrentContext.FeatureId || slot.Revision != m_CurrentContext.EquipmentRevision)
					throw new InvalidOperationException($"Equipment parameter context '{m_CurrentContext}' is stale.");
			}
			else
			{
				slot = RequireSlotState(operation);
				ulong expectedRevision = ReadUInt64(inputs);
				if (!slot.IsEquipped || expectedRevision == 0)
					throw new InvalidOperationException($"Equipment parameter operation '{SourcePath(operation)}' requires Action Context or explicit Slot revision.");
				if (slot.Revision != expectedRevision)
					throw new InvalidOperationException($"Equipment parameter operation '{SourcePath(operation)}' revision is stale.");
			}
			EquipmentParameterId parameterId = m_Layout.Equipment.RequireOperationParameter(operation.Handle);
			EquipmentProgramParameter parameter = m_Layout.Equipment.RequireParameter(slot.EquipmentId, parameterId);
			if (parameter.FeatureId != slot.FeatureId)
				throw new InvalidOperationException($"Equipment parameter '{parameterId}' does not belong to Feature '{slot.FeatureId}'.");
			ProgramConstant constant = m_Program.Constants[parameter.ConstantIndex];
			return CharacterStateValue.FromConstant(constant, ToStateKind(parameter.ValueKind));
		}

		EquipmentChangeRequest BuildChangeRequest(SimulationOperation operation, Float32ValueInputLease inputs)
		{
			EquipmentProgramSlot slot = RequireSlot(operation);
			EquipmentId target = TryRequireItem(operation, out EquipmentProgramItem item) ? item.EquipmentId : default;
			EquipmentSlotState current = ReadState().RequireSlot(slot.SlotId);
			ulong expectedRevision = ReadUInt64(inputs);
			if (m_CurrentContext.IsValid)
			{
				if (m_CurrentContext.SlotId != slot.SlotId || m_CurrentContext.EquipmentRevision != current.Revision)
					throw new InvalidOperationException($"Equipment change context '{m_CurrentContext}' does not match Slot '{slot.SlotId}'.");
				if (expectedRevision == 0)
					expectedRevision = m_CurrentContext.EquipmentRevision;
			}
			if (expectedRevision == 0)
				throw new InvalidOperationException($"Equipment change operation '{SourcePath(operation)}' requires an explicit revision outside a Route Context.");
			ulong actionInstanceId = 0;
			string actionContext = GetStringConstant(operation, OperationNamedConstant.ActionContext, string.Empty);
			if (!string.IsNullOrEmpty(actionContext) && m_Actions.FindActive(actionContext, out Float32ActionInstanceState action) >= 0)
				actionInstanceId = action.InstanceId;
			return new EquipmentChangeRequest(slot.SlotId, target, expectedRevision, actionInstanceId);
		}

		static ulong ReadUInt64(Float32ValueInputLease inputs)
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

		OperationExecutionResult TickPersistent<TTarget>(OperationControlCursor<TTarget> cursor, SimulationOperation operation)
			where TTarget : struct, IOperationControlTarget<TTarget>
		{
			EquipmentSlotState slot = RequireSlotState(operation);
			if (!slot.IsEquipped)
				return OperationExecutionResult.Running;
			EquipmentProgramFeature feature = m_Layout.Equipment.RequireFeature(slot.FeatureId);
			if (m_Trace.Enabled)
				m_Trace.Add(operation, "equipment_host", SimulationTraceSeverity.Detail, $"persistent:{slot.SlotId}:{slot.EquipmentId}:{slot.FeatureId}:revision={slot.Revision}:generation={slot.HostGeneration}:entry={feature.PersistentEntry}");
			if (!feature.PersistentEntry.IsValid)
				return OperationExecutionResult.Running;
			cursor.Tick(feature.PersistentEntry);
			return OperationExecutionResult.Running;
		}

		OperationExecutionResult TickRoute<TTarget>(OperationControlCursor<TTarget> cursor, SimulationOperation operation)
			where TTarget : struct, IOperationControlTarget<TTarget>
		{
			EquipmentProgramRoute route = RequireRoute(operation);
			EquipmentSlotState slot = ReadState().RequireSlot(route.OwnerSlotId);
			EquipmentProgramRouteImplementation implementation = null;
			bool hasImplementation = slot.IsEquipped &&
				m_Layout.Equipment.TryGetRouteImplementation(slot.FeatureId, route.RouteId, out implementation);
			if (!hasImplementation)
			{
				if (route.RequestConsumption == EquipmentRouteRequestConsumption.Always)
					m_Input.ClearRequest(route.InputRequestId);
				if (route.MissingImplementation == EquipmentRouteMissingImplementation.RejectComposition)
					throw new InvalidOperationException($"Equipment Route '{route.RouteId}' has no implementation for Feature '{slot.FeatureId}'.");
				return OperationExecutionResult.Failure;
			}
			bool active = cursor.IsActive(implementation.EntryOperation);
			if (!active && !m_Input.HasRequest(route.InputRequestId, out _))
				return OperationExecutionResult.Failure;
			using (PushContext(slot.ActionContext(route.RouteId)))
			{
				if (m_Trace.Enabled)
					m_Trace.Add(operation, "equipment_host", SimulationTraceSeverity.Detail, $"route:{slot.SlotId}:{slot.EquipmentId}:{slot.FeatureId}:revision={slot.Revision}:generation={slot.HostGeneration}:route={route.RouteId}:entry={implementation.EntryOperation}");
				OperationExecutionResult result = cursor.Tick(implementation.EntryOperation);
				if (!active && result != OperationExecutionResult.Failure &&
					route.RequestConsumption == EquipmentRouteRequestConsumption.OnActivated)
				{
					m_Input.ClearRequest(route.InputRequestId);
				}
				else if (route.RequestConsumption == EquipmentRouteRequestConsumption.Always)
				{
					m_Input.ClearRequest(route.InputRequestId);
				}
				return result;
			}
		}

		void StopPersistent<TTarget>(OperationControlCursor<TTarget> cursor, SimulationOperation operation, OperationHandle source)
			where TTarget : struct, IOperationControlTarget<TTarget>
		{
			EquipmentSlotState slot = RequireSlotState(operation);
			AbortPersistent(cursor, slot, source);
		}

		void AbortPersistent<TTarget>(OperationControlCursor<TTarget> cursor, EquipmentSlotState slot, OperationHandle source)
			where TTarget : struct, IOperationControlTarget<TTarget>
		{
			if (!slot.IsEquipped)
				return;
			EquipmentProgramFeature feature = m_Layout.Equipment.RequireFeature(slot.FeatureId);
			if (feature.PersistentEntry.IsValid)
				cursor.ForceStop(feature.PersistentEntry, OperationStopContext.ParentStop(source));
		}

		IDisposable PushContext(EquipmentActionContext context)
		{
			if (m_CurrentContext.IsValid)
				throw new InvalidOperationException("Equipment Route Context is nested.");
			m_CurrentContext = context;
			return new ContextScope(this);
		}

		EquipmentProgramSlot RequireSlot(SimulationOperation operation)
		{
			return m_Layout.Equipment.RequireSlot(m_Layout.Equipment.RequireOperationSlot(operation.Handle));
		}

		EquipmentSlotState RequireSlotState(SimulationOperation operation) => ReadState().RequireSlot(RequireSlot(operation).SlotId);

		EquipmentProgramRoute RequireRoute(SimulationOperation operation)
		{
			return m_Layout.Equipment.RequireRoute(m_Layout.Equipment.RequireOperationRoute(operation.Handle));
		}

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
				m_Trace.Add(source, "equipment_snapshot", SimulationTraceSeverity.Detail, $"slot={slot.SlotId}:equipment={slot.EquipmentId}:feature={slot.FeatureId}:revision={slot.Revision}:generation={slot.HostGeneration}:tagSource={slot.TagSource}:effects={string.Join(",", slot.PassiveEffectHandles)}");
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
			Float32ActionInstanceState active = m_Actions.FindOnlyActive();
			return active.IsActive && active.InstanceId != sourceActionInstanceId && active.EquipmentContext.IsValid &&
				active.EquipmentContext.SlotId == slot.SlotId && active.EquipmentContext.EquipmentRevision == slot.Revision;
		}
		bool IEquipmentRuntimePort.IsActionActive(ulong actionInstanceId)
		{
			Float32ActionInstanceState active = m_Actions.FindOnlyActive();
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
				Float32Scalar.Zero));
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

		sealed class ContextScope : IDisposable
		{
			Float32EquipmentRuntime m_Owner;
			public ContextScope(Float32EquipmentRuntime owner) { m_Owner = owner; }
			public void Dispose()
			{
				if (m_Owner == null)
					return;
				m_Owner.m_CurrentContext = default;
				m_Owner = null;
			}
		}

		sealed class MutationScope : IEquipmentMutationScope
		{
			readonly Float32EvaluationFrame m_Frame;
			readonly Float32CharacterStateSavepoint m_Savepoint;
			readonly Float32EvaluationOutputSavepoint m_OutputSavepoint;
			readonly CharacterStateValue[] m_Values;
			bool m_Completed;

			public MutationScope(Float32EvaluationFrame frame)
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
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            
