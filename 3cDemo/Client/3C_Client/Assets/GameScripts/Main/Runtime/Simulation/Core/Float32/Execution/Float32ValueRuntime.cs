using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal sealed class Float32ValueRuntime : Float32GraphValueRuntime
    {
        readonly Float32InputRuntime m_Input;
        readonly IFloat32ActionContextReader m_Actions;
        readonly IFloat32ActionAdmissionQuery m_ActionAdmission;
        readonly IFloat32GameplayTagQuery m_GameplayTags;
        readonly Float32EquipmentRuntime m_Equipment;
        readonly IFloat32BlackboardPort m_Blackboard;
        readonly Float32AbilityExecutionFrame m_Frame;
        readonly Float32StatePort m_ControlState;
        readonly Float32GameplayAbilityExecutionAccess Access;

        public Float32ValueRuntime(
            Float32GameplayAbilityExecutionAccess access,
            Float32InputRuntime input,
            IFloat32ActionContextReader actions,
            IFloat32ActionAdmissionQuery actionAdmission,
            IFloat32GameplayTagQuery gameplayTags,
            Float32EquipmentRuntime equipment,
            IFloat32BlackboardPort blackboard,
            Float32AbilityExecutionFrame frame,
            Float32StatePort controlState,
            Float32AbilityExecutionWorkspace workspace)
            : base(access.Data, access.Layout, workspace.Values)
        {
            m_Input = input;
            m_Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            m_ActionAdmission = actionAdmission ?? throw new ArgumentNullException(nameof(actionAdmission));
            m_GameplayTags = gameplayTags;
            m_Equipment = equipment;
            m_Blackboard = blackboard ?? throw new ArgumentNullException(nameof(blackboard));
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            m_ControlState = controlState;
            Access = access;
        }

        protected override AbilityStateValue EvaluateDomainValue<TTarget>(OperationControlCursor<TTarget> cursor,
            SimulationOperation operation, string outputPort, Float32ValueInputLease inputs)
        {
            AbilityStateValue result;
            switch (operation.Code)
            {
                case SimulationOperationCode.InputBoolean:
						result = AbilityStateValue.FromBoolean(m_Input.ReadValue(operation.Text0, SimulationInputValueKind.Boolean).Boolean);
						break;
                case SimulationOperationCode.InputScalar:
						result = AbilityStateValue.FromScalar(m_Input.ReadValue(operation.Text0, SimulationInputValueKind.Scalar).Scalar);
						break;
                case SimulationOperationCode.ActivationEntry:
                        result = AbilityStateValue.FromBoolean(m_Actions.IsActivationEntry(operation.Text0));
                        break;
                    case SimulationOperationCode.TimelineTime:
                        result = AbilityStateValue.FromScalar(Float32Scalar.FromSingle(m_Frame.TreeClipInvocation.Time.ToSingle()));
                        break;
                case SimulationOperationCode.InputVector2:
						result = AbilityStateValue.FromVector2(m_Input.ReadValue(operation.Text0, SimulationInputValueKind.Vector2).Vector2);
						break;
                case SimulationOperationCode.InputVector2Magnitude:
						result = AbilityStateValue.FromScalar(m_Input.ReadValue(operation.Text0, SimulationInputValueKind.Vector2).Vector2.Magnitude);
						break;
                case SimulationOperationCode.InputRequest:
						result = AbilityStateValue.FromBoolean(m_Input.HasRequest(operation.Text0, out _));
						break;
                case SimulationOperationCode.BlackboardGet:
						result = ReadBlackboard(cursor, operation);
						break;
                case SimulationOperationCode.ActionContextActive:
						result = AbilityStateValue.FromBoolean(
							string.IsNullOrEmpty(operation.Text0)
								? m_Actions.IsCurrentExecutionContextActive()
								: m_Actions.IsContextActive(operation.Text0));
						break;
                case SimulationOperationCode.ActionEventReceived:
                        result = AbilityStateValue.FromBoolean(
                            m_Actions.TryGetCurrentSkillExecution(out Float32ActionInstanceState eventAction) &&
                            m_Frame.Input.HasActionEvent(eventAction.InstanceId, operation.Text0));
                        break;
                    case SimulationOperationCode.ActionWindowActive:
						result = AbilityStateValue.FromBoolean(m_Blackboard.IsActionWindowActive(operation));
						break;
                case SimulationOperationCode.CanActivateAction:
						result = AbilityStateValue.FromBoolean(m_ActionAdmission.PreviewActivation(cursor, operation).Allowed);
						break;
                case SimulationOperationCode.GameplayEffectApply:
                        result = outputPort switch
                        {
                            "m_Handle" => m_ControlState.Get(Access.RequireOperationSlot(operation.Handle, ProgramStateSemantic.GameplayEffectAppliedHandle)),
                            "m_Applied" => AbilityStateValue.FromBoolean(cursor.ReadStatus(operation.Handle) == OperationRunnableStatus.Success),
                            _ => throw new InvalidOperationException($"Gameplay Effect apply output '{outputPort}' is unsupported.")
                        };
                        break;
                    case SimulationOperationCode.GameplayEffectRemove:
                        result = AbilityStateValue.FromBoolean(cursor.ReadStatus(operation.Handle) == OperationRunnableStatus.Success);
                        break;
                    case SimulationOperationCode.GameplayEffectHasTag:
                        result = AbilityStateValue.FromBoolean(RequireGameplayTags().HasTag(operation.Text0));
                        break;
                case SimulationOperationCode.GameplayEffectMatchTags:
                        result = AbilityStateValue.FromBoolean(
                            RequireGameplayTags().Matches(Access.Services.RequireTagQuery(operation.Handle)));
                        break;
                case SimulationOperationCode.GameplayAttributeRead:
                        result = RequireGameplayTags().ReadAttribute(operation, outputPort);
                        break;
                case SimulationOperationCode.CameraBasisRead:
						result = ReadCameraBasis(outputPort);
						break;
                case SimulationOperationCode.ReadEquipmentIdentity:
                case SimulationOperationCode.ReadEquipmentParameter:
                case SimulationOperationCode.RequestEquipmentChange:
                case SimulationOperationCode.BeginEquipmentChange:
                case SimulationOperationCode.CommitEquipmentChange:
                case SimulationOperationCode.CancelEquipmentChange:
                        result = RequireEquipment().Evaluate(operation, outputPort, inputs);
                        break;
                case SimulationOperationCode.StateRootCompleted:
						result = AbilityStateValue.FromBoolean(cursor.CurrentStateRootCompleted());
						break;
                case SimulationOperationCode.StateExitCause:
						result = AbilityStateValue.FromBoolean(operation.Integer0 == cursor.CurrentStateExitCause());
						break;
                case SimulationOperationCode.MoveFacingAngle:
						result = AbilityStateValue.FromScalar(ReadMoveFacingAngle(inputs));
						break;
                case SimulationOperationCode.CharacterStateRead:
						result = ReadCharacterState(operation.Text0);
						break;
                default:
                    throw new InvalidOperationException($"Operation '{operation.Handle}' code '{operation.Code}' is not a value operation.");
            }
            return result;
        }

        protected override void ResetGraphCallParameter(int slot) => m_Blackboard.ResetGraphCallParameter(slot);
        protected override void WriteGraphCallParameter(int slot, AbilityStateValue value) => m_Blackboard.WriteGraphCallParameter(slot, value);
        protected override AbilityStateValue ReadGraphCallParameter(int slot) => m_Blackboard.ReadGraphCallParameter(slot);

        protected override void TraceResult(SimulationOperation operation, string outputPort, AbilityStateValue value, bool predictive)
        {
            TraceValue(operation, value);
            if (!predictive)
                m_Frame.Trace.AddValue(operation, outputPort, ProgramValuePortDirection.Output, value);
        }

        protected override void TraceInput(SimulationOperation operation, CompiledValueInputBinding input,
            AbilityStateValue value, bool predictive)
        {
                    if (!predictive && (m_Frame.Trace.CaptureValues ||
                        m_Frame.Trace.CaptureControlFlow && input.SourceKind == CompiledValueInputSourceKind.Operation))
                    {
                        string port = GameplayAbilityValuePortContracts.Require(operation.Code, operation.Handle, m_Ability.GraphCallFrames)
                            .Inputs[input.TargetPortIndex].Identity;
                        m_Frame.Trace.AddValue(operation, port, ProgramValuePortDirection.Input, value);
                        m_Frame.Trace.AddValueEdge(operation, port);
                    }
        }

		void TraceValue(SimulationOperation operation, AbilityStateValue value)
		{
			if (!m_Frame.Trace.Enabled ||
			    operation.Code != SimulationOperationCode.InputVector2 &&
			    operation.Code != SimulationOperationCode.InputVector2Magnitude &&
			    operation.Code != SimulationOperationCode.MoveFacingAngle &&
			    operation.Code != SimulationOperationCode.CharacterStateRead &&
			    operation.Code != SimulationOperationCode.Compare &&
			    operation.Code != SimulationOperationCode.And &&
			    operation.Code != SimulationOperationCode.Or &&
			    operation.Code != SimulationOperationCode.Not &&
			    operation.Code != SimulationOperationCode.ConditionResult)
				return;
			m_Frame.Trace.Add(
				operation,
				"condition_value_evaluated",
				SimulationTraceSeverity.Detail,
				$"code={operation.Code};kind={value.Kind};value={FormatValue(value)}");
		}

        static string FormatValue(AbilityStateValue value)
		{
			return value.Kind switch
			{
				ProgramStateValueKind.Boolean => value.Boolean.ToString(),
				ProgramStateValueKind.Scalar => value.Scalar.ToString(),
				ProgramStateValueKind.Vector2 => $"({value.Vector2.X},{value.Vector2.Y})",
				ProgramStateValueKind.Vector3 => $"({value.Vector3.X},{value.Vector3.Y},{value.Vector3.Z})",
				ProgramStateValueKind.Yaw => value.Yaw.Degrees.ToString(),
				_ => value.Kind.ToString()
			};
		}

        IFloat32GameplayTagQuery RequireGameplayTags() => m_GameplayTags ??
            throw new InvalidOperationException(
                "Ability value operation requires the declared Gameplay Effect service.");

        Float32EquipmentRuntime RequireEquipment() => m_Equipment ??
            throw new InvalidOperationException(
                "Ability value operation requires the declared Equipment service.");

        public bool SetBlackboard<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation)
            where TTarget : struct, IOperationControlTarget<TTarget>
        {
            ProgramReference reference = m_Layout.Topology.FirstReference(operation.Handle, ProgramReferenceKind.StateSlot);
            if (reference == null)
                return false;
            using Float32ValueInputLease values = ReadInputs(cursor, operation);
            if (values.Count == 0)
                return false;
            ProgramStateValueKind expected = m_Ability.StateSlots[reference.TargetIndex].ValueKind;
            m_Blackboard.Write(cursor, operation, reference.TargetIndex, ConvertValue(values[0], expected));
            return true;
        }

        AbilityStateValue ReadBlackboard<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation)
            where TTarget : struct, IOperationControlTarget<TTarget>
        {
            ProgramReference reference = m_Layout.Topology.FirstReference(operation.Handle, ProgramReferenceKind.StateSlot);
            if (reference == null)
                throw new InvalidOperationException($"Blackboard operation '{operation.Handle}' has no state address.");
            return m_Blackboard.Read(cursor, operation, reference.TargetIndex);
        }

        Float32Scalar ReadMoveFacingAngle(Float32ValueInputLease inputs)
        {
            if (inputs.Count == 0 ||
                inputs[0].Kind != ProgramStateValueKind.Vector2 ||
                inputs[0].Vector2 == Float32Vector2.Zero)
                return Float32Scalar.Zero;
            Float32Yaw desired = Float32Angle.FromPlanarDirection(inputs[0].Vector2);
            return Float32Scalar.Abs(Float32Angle.Delta(m_Frame.BodyFacts.Yaw, desired));
        }

        AbilityStateValue ReadCharacterState(string field)
        {
            return field switch
            {
                CharacterStateProviderFields.Position => AbilityStateValue.FromVector3(m_Frame.BodyFacts.Position),
                CharacterStateProviderFields.Velocity => AbilityStateValue.FromVector3(m_Frame.BodyFacts.Velocity),
                CharacterStateProviderFields.VerticalVelocity => AbilityStateValue.FromScalar(m_Frame.BodyFacts.VerticalVelocity),
                CharacterStateProviderFields.BodyYaw => AbilityStateValue.FromYaw(m_Frame.BodyFacts.Yaw),
                CharacterStateProviderFields.Grounded => AbilityStateValue.FromBoolean(m_Frame.BodyFacts.Grounded),
                _ => throw new InvalidOperationException($"Character State field '{field}' is not supported by Float32 runtime.")
            };
        }

        AbilityStateValue ReadCameraBasis(string outputPort)
        {
            return outputPort switch
            {
                CameraProgramOperationSchema.BasisValidPortId => AbilityStateValue.FromBoolean(
                    m_Input.ReadValue(CameraProgramOperationSchema.BasisValidInputId, SimulationInputValueKind.Boolean).Boolean),
                CameraProgramOperationSchema.BasisPlanarForwardPortId => AbilityStateValue.FromVector3(
                    m_Input.ReadValue(CameraProgramOperationSchema.BasisPlanarForwardInputId, SimulationInputValueKind.Vector3).Vector3),
                CameraProgramOperationSchema.BasisPlanarRightPortId => AbilityStateValue.FromVector3(
                    m_Input.ReadValue(CameraProgramOperationSchema.BasisPlanarRightInputId, SimulationInputValueKind.Vector3).Vector3),
                CameraProgramOperationSchema.BasisLookDirectionPortId => AbilityStateValue.FromVector3(
                    m_Input.ReadValue(CameraProgramOperationSchema.BasisLookDirectionInputId, SimulationInputValueKind.Vector3).Vector3),
                CameraProgramOperationSchema.BasisAimPointPortId => AbilityStateValue.FromVector3(
                    m_Input.ReadValue(CameraProgramOperationSchema.BasisAimPointInputId, SimulationInputValueKind.Vector3).Vector3),
                CameraProgramOperationSchema.BasisYawPortId => AbilityStateValue.FromYaw(
                    m_Input.ReadValue(CameraProgramOperationSchema.BasisYawInputId, SimulationInputValueKind.Yaw).Yaw),
                CameraProgramOperationSchema.BasisPitchPortId => AbilityStateValue.FromScalar(
                    m_Input.ReadValue(CameraProgramOperationSchema.BasisPitchInputId, SimulationInputValueKind.Scalar).Scalar),
                _ => throw new InvalidOperationException($"Camera basis contains unknown output port '{outputPort}'.")
            };
        }

    }
}
