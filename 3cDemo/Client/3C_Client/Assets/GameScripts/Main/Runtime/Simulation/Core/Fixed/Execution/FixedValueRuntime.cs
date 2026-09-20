using ThirdPersonSimulation;
using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation.Fixed
{
    internal readonly struct FixedValueEvaluationKey : IEquatable<FixedValueEvaluationKey>
    {
        public FixedValueEvaluationKey(int operation, string outputPort)
        {
            Operation = operation;
            OutputPort = outputPort ?? string.Empty;
        }

        public int Operation { get; }
        public string OutputPort { get; }

        public bool Equals(FixedValueEvaluationKey other) =>
            Operation == other.Operation && string.Equals(OutputPort, other.OutputPort, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is FixedValueEvaluationKey other && Equals(other);
        public override int GetHashCode() => unchecked(Operation * 397 ^ StringComparer.Ordinal.GetHashCode(OutputPort));
    }

    internal sealed class FixedValueInputBuffer
    {
        public List<AbilityStateValue> Values { get; } = new List<AbilityStateValue>();

        public void Clear()
        {
            Values.Clear();
        }
    }

    internal readonly struct FixedValueInputLease : IDisposable
    {
        readonly FixedValueRuntime m_Owner;
        readonly FixedValueInputBuffer m_Buffer;
        readonly int m_Depth;

        public FixedValueInputLease(
            FixedValueRuntime owner,
            FixedValueInputBuffer buffer,
            int depth)
        {
            m_Owner = owner;
            m_Buffer = buffer;
            m_Depth = depth;
        }

        public int Count => m_Buffer.Values.Count;
        public AbilityStateValue this[int index] => m_Buffer.Values[index];

        public AbilityStateValue FindByKind(ProgramStateValueKind kind)
        {
            for (int i = 0; i < m_Buffer.Values.Count; i++)
            {
                if (m_Buffer.Values[i].Kind == kind)
                    return m_Buffer.Values[i];
            }
            return default;
        }

        public void Dispose()
        {
            m_Owner?.ReleaseInputBuffer(m_Buffer, m_Depth);
        }
    }

    internal sealed class FixedValueRuntime : FixedOperationModule, IFixedValueInputReader
    {
        readonly IFixedInputPort m_Input;
        readonly IFixedActionContextReader m_Actions;
        readonly IFixedActionAdmissionQuery m_ActionAdmission;
        readonly IFixedGameplayTagQuery m_GameplayTags;
        readonly FixedEquipmentRuntime m_Equipment;
        readonly IFixedBlackboardPort m_Blackboard;
        readonly FixedAbilityExecutionFrame m_Frame;
        readonly FixedStatePort m_ControlState;
        readonly HashSet<FixedValueEvaluationKey> m_ValueStack;
        readonly List<FixedValueInputBuffer> m_InputBuffers;
        int m_InputBufferDepth;

        public FixedValueRuntime(
            FixedGameplayAbilityExecutionAccess access,
            IFixedInputPort input,
            IFixedActionContextReader actions,
            IFixedActionAdmissionQuery actionAdmission,
            IFixedGameplayTagQuery gameplayTags,
            FixedEquipmentRuntime equipment,
            IFixedBlackboardPort blackboard,
            FixedAbilityExecutionFrame frame,
            FixedStatePort controlState,
            FixedAbilityExecutionWorkspace workspace)
            : base(access)
        {
            m_Input = input ?? throw new ArgumentNullException(nameof(input));
            m_Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            m_ActionAdmission = actionAdmission ?? throw new ArgumentNullException(nameof(actionAdmission));
            m_GameplayTags = gameplayTags;
            m_Equipment = equipment;
            m_Blackboard = blackboard ?? throw new ArgumentNullException(nameof(blackboard));
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            m_ControlState = controlState ?? throw new ArgumentNullException(nameof(controlState));
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));
            m_ValueStack = workspace.ValueStack;
            m_InputBuffers = workspace.ValueBuffers;
        }

        public void BeginEvaluation()
        {
            if (m_InputBufferDepth != 0 || m_ValueStack.Count != 0)
                throw new InvalidOperationException("Fixed value runtime retained recursion state across evaluations.");
        }

        public void PrepareSubGraph<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation)
            where TTarget : struct, IOperationControlTarget<TTarget>
        {
            ProgramGraphCallFrame frame = RequireGraphCallFrame(operation);
            OperationRunnableStatus status = cursor.ReadStatus(frame.EntryOperation);
            if (status == OperationRunnableStatus.Running || status == OperationRunnableStatus.Stopping)
                return;
            using FixedValueInputLease inputs = ReadInputs(cursor, operation);
            if (inputs.Count != frame.Inputs.Count)
                throw new InvalidOperationException($"Graph call frame '{frame.Identity}' received '{inputs.Count}' inputs, expected '{frame.Inputs.Count}'.");
            for (int i = 0; i < frame.Outputs.Count; i++)
                m_Blackboard.ResetGraphCallParameter(frame.Outputs[i].StateSlot);
            for (int i = 0; i < frame.Inputs.Count; i++)
                m_Blackboard.WriteGraphCallParameter(frame.Inputs[i].StateSlot, inputs[i]);
        }

		public AbilityStateValue Evaluate<TTarget>(
			OperationControlCursor<TTarget> cursor,
			OperationHandle handle,
			string outputPort = "")
			where TTarget : struct, IOperationControlTarget<TTarget>
		{
			cursor.RequireExecution(handle);
			var valueKey = new FixedValueEvaluationKey(handle.Value, outputPort);
			if (!m_ValueStack.Add(valueKey))
				throw new InvalidOperationException($"Value operation cycle reached '{handle}/{outputPort}'.");
			try
			{
				SimulationOperation operation = Access.Operation(handle);
				using FixedValueInputLease inputs = ReadInputs(cursor, operation);
				AbilityStateValue result;
				switch (operation.Code)
				{
					case SimulationOperationCode.ConditionResult:
						result = AbilityStateValue.FromBoolean(inputs.Count > 0 && ToBoolean(inputs[0]));
						break;
					case SimulationOperationCode.InputBoolean:
						result = AbilityStateValue.FromBoolean(m_Input.ReadValue(operation.Text0, SimulationInputValueKind.Boolean).Boolean);
						break;
					case SimulationOperationCode.InputScalar:
						result = AbilityStateValue.FromScalar(m_Input.ReadValue(operation.Text0, SimulationInputValueKind.Scalar).Scalar);
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
					case SimulationOperationCode.SubGraph:
						result = ReadSubGraphOutput(cursor, operation, outputPort);
						break;
					case SimulationOperationCode.ActionContextActive:
						result = AbilityStateValue.FromBoolean(
							string.IsNullOrEmpty(operation.Text0)
								? m_Actions.IsCurrentExecutionContextActive()
								: m_Actions.IsContextActive(operation.Text0));
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
                            "m_Handle" => m_ControlState.Get(RequireOperationSlot(operation, ProgramStateSemantic.GameplayEffectAppliedHandle)),
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
					case SimulationOperationCode.Compare:
						result = AbilityStateValue.FromBoolean(Compare(operation.Integer0, inputs));
						break;
					case SimulationOperationCode.And:
						result = AbilityStateValue.FromBoolean(inputs.Count >= 2 && ToBoolean(inputs[0]) && ToBoolean(inputs[1]));
						break;
					case SimulationOperationCode.Or:
						result = AbilityStateValue.FromBoolean(inputs.Count >= 2 && (ToBoolean(inputs[0]) || ToBoolean(inputs[1])));
						break;
					case SimulationOperationCode.Not:
						result = AbilityStateValue.FromBoolean(inputs.Count == 0 || !ToBoolean(inputs[0]));
						break;
					case SimulationOperationCode.Constant:
						result = operation.ConstantReferences.Count > 0
							? ValueFromConstant(m_Ability.Constants[operation.ConstantReferences[0]])
							: AbilityStateValue.FromBoolean(false);
						break;
					default:
						throw new InvalidOperationException($"Operation '{handle}' code '{operation.Code}' is not a value operation.");
				}
				TraceValue(operation, result);
                if (!cursor.IsPredictiveEvaluation)
                    m_Frame.Trace.AddValue(operation, outputPort, ProgramValuePortDirection.Output, result);
				return result;
			}
			finally
			{
				m_ValueStack.Remove(valueKey);
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
				ProgramStateValueKind.Vector2 => value.Vector2.ToString(),
				ProgramStateValueKind.Vector3 => value.Vector3.ToString(),
				ProgramStateValueKind.Yaw => value.Yaw.ToString(),
				_ => value.Kind.ToString()
			};
		}

        public bool EvaluateCondition<TTarget>(
            OperationControlCursor<TTarget> cursor,
            ProgramControlFlowEdge edge)
            where TTarget : struct, IOperationControlTarget<TTarget>
        {
            return edge != null && (!edge.HasCondition || ToBoolean(Evaluate(cursor, edge.Condition)));
        }

        IFixedGameplayTagQuery RequireGameplayTags() => m_GameplayTags ??
            throw new InvalidOperationException(
                "Ability value operation requires the declared Gameplay Effect service.");

        FixedEquipmentRuntime RequireEquipment() => m_Equipment ??
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
            using FixedValueInputLease values = ReadInputs(cursor, operation);
            if (values.Count == 0)
                return false;
            ProgramStateValueKind expected = m_Ability.StateSlots[reference.TargetIndex].ValueKind;
            m_Blackboard.Write(cursor, operation, reference.TargetIndex, ConvertValue(values[0], expected));
            return true;
        }

        public FixedValueInputLease ReadInputs<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation)
            where TTarget : struct, IOperationControlTarget<TTarget>
        {
            ReadOnlySpan<CompiledValueInputBinding> inputs = m_Layout.ValueInputs(operation.Handle);
            int depth = m_InputBufferDepth++;
            FixedValueInputBuffer buffer = RequireInputBuffer(depth);
            buffer.Clear();
            try
            {
                for (int i = 0; i < inputs.Length; i++)
                {
                    CompiledValueInputBinding input = inputs[i];
                    AbilityStateValue value = input.SourceKind == CompiledValueInputSourceKind.Operation
                        ? Evaluate(cursor, input.SourceOperation, m_Layout.ValueSourceOutputPort(input))
                        : ValueFromConstant(m_Ability.Constants[input.ConstantIndex]);
                    buffer.Values.Add(value);
                    if (!cursor.IsPredictiveEvaluation && (m_Frame.Trace.CaptureValues ||
                        m_Frame.Trace.CaptureControlFlow && input.SourceKind == CompiledValueInputSourceKind.Operation))
                    {
                        string port = GameplayAbilityValuePortContracts.Require(operation.Code, operation.Handle, m_Ability.GraphCallFrames)
                            .Inputs[input.TargetPortIndex].Identity;
                        m_Frame.Trace.AddValue(operation, port, ProgramValuePortDirection.Input, value);
                        m_Frame.Trace.AddValueEdge(operation, port);
                    }
                }
                return new FixedValueInputLease(this, buffer, depth);
            }
            catch
            {
                ReleaseInputBuffer(buffer, depth);
                throw;
            }
        }

        FixedValueInputBuffer RequireInputBuffer(int depth)
        {
            while (m_InputBuffers.Count <= depth)
                m_InputBuffers.Add(new FixedValueInputBuffer());
            return m_InputBuffers[depth];
        }

        internal void ReleaseInputBuffer(FixedValueInputBuffer buffer, int depth)
        {
            if (depth != m_InputBufferDepth - 1 || !ReferenceEquals(buffer, m_InputBuffers[depth]))
                throw new InvalidOperationException("Fixed value input buffers must be released in recursion order.");
            buffer.Clear();
            m_InputBufferDepth--;
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

        AbilityStateValue ReadSubGraphOutput<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation,
            string outputPort)
            where TTarget : struct, IOperationControlTarget<TTarget>
        {
            ProgramGraphCallFrame frame = RequireGraphCallFrame(operation);
            ProgramGraphParameterBinding binding = null;
            for (int i = 0; i < frame.Outputs.Count; i++)
            {
                if (string.Equals(frame.Outputs[i].PortId, outputPort ?? string.Empty, StringComparison.Ordinal))
                {
                    binding = frame.Outputs[i];
                    break;
                }
            }
            if (binding == null)
                throw new InvalidOperationException($"Graph call frame '{frame.Identity}' has no output port '{outputPort}'.");
            if (cursor.ReadStatus(frame.EntryOperation) != OperationRunnableStatus.Success)
                return AbilityStateValue.Default(m_Ability.StateSlots[binding.StateSlot].ValueKind);
            return m_Blackboard.ReadGraphCallParameter(binding.StateSlot);
        }

        ProgramGraphCallFrame RequireGraphCallFrame(SimulationOperation operation)
        {
            IReadOnlyList<ProgramGraphCallFrame> frames = m_Layout.Topology.GraphCallFrames(operation.Handle);
            if (frames.Count != 1)
                throw new InvalidOperationException($"SubGraph operation '{operation.Handle}' requires exactly one graph call frame.");
            return frames[0];
        }

        FixedScalar ReadMoveFacingAngle(FixedValueInputLease inputs)
        {
            if (inputs.Count == 0 ||
                inputs[0].Kind != ProgramStateValueKind.Vector2 ||
                inputs[0].Vector2 == FixedVector2.Zero)
                return FixedScalar.Zero;
            FixedYaw desired = FixedAngle.FromPlanarDirection(inputs[0].Vector2);
            return FixedScalar.Abs(FixedAngle.Delta(m_Frame.BodyFacts.Yaw, desired));
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
                _ => throw new InvalidOperationException($"Character State field '{field}' is not supported by Fixed runtime.")
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

        static bool Compare(int comparison, FixedValueInputLease inputs)
        {
            if (inputs.Count < 2)
                return false;
            FixedScalar left = ToScalar(inputs[0]);
            FixedScalar right = ToScalar(inputs[1]);
            return comparison switch
            {
                0 => left == right,
                1 => left != right,
                2 => left < right,
                3 => left <= right,
                4 => left >= right,
                5 => left > right,
                _ => false
            };
        }

        static FixedScalar ToScalar(AbilityStateValue value)
        {
            return value.Kind switch
            {
                ProgramStateValueKind.Int32 => FixedScalar.FromInt64(value.Int32),
                ProgramStateValueKind.UInt64 when value.UInt64 <= long.MaxValue => FixedScalar.FromInt64((long)value.UInt64),
                ProgramStateValueKind.Scalar => value.Scalar,
                ProgramStateValueKind.Boolean => value.Boolean ? FixedScalar.One : FixedScalar.Zero,
                _ => throw new InvalidOperationException($"State value '{value.Kind}' is not numeric.")
            };
        }

        public static bool ToBoolean(AbilityStateValue value)
        {
            return value.Kind switch
            {
                ProgramStateValueKind.Boolean => value.Boolean,
                ProgramStateValueKind.Int32 => value.Int32 != 0,
                ProgramStateValueKind.UInt64 => value.UInt64 != 0,
                ProgramStateValueKind.Scalar => value.Scalar != FixedScalar.Zero,
                ProgramStateValueKind.Identity => !string.IsNullOrEmpty(value.Identity),
                _ => false
            };
        }

        static AbilityStateValue ConvertValue(AbilityStateValue value, ProgramStateValueKind expected)
        {
            if (value.Kind == expected)
                return value;
            if (expected == ProgramStateValueKind.Scalar)
                return AbilityStateValue.FromScalar(ToScalar(value));
            if (expected == ProgramStateValueKind.Int32 && value.Kind == ProgramStateValueKind.Scalar)
                return AbilityStateValue.FromInt32(value.Scalar.TruncateToInt32());
            throw new InvalidOperationException($"Cannot assign '{value.Kind}' to '{expected}'.");
        }

        static AbilityStateValue ValueFromConstant(ProgramConstant constant)
        {
            return constant.Kind switch
            {
                ProgramConstantKind.Boolean => AbilityStateValue.FromBoolean(constant.Boolean),
                ProgramConstantKind.Int32 => AbilityStateValue.FromInt32(constant.Int32),
                ProgramConstantKind.UInt64 => AbilityStateValue.FromUInt64(constant.UInt64),
                ProgramConstantKind.Scalar => AbilityStateValue.FromScalar(constant.Scalar),
                ProgramConstantKind.Vector2 => AbilityStateValue.FromVector2(constant.Vector2),
                ProgramConstantKind.Vector3 => AbilityStateValue.FromVector3(constant.Vector3),
                ProgramConstantKind.Yaw => AbilityStateValue.FromYaw(constant.Yaw),
                ProgramConstantKind.String => AbilityStateValue.FromIdentity(constant.Text),
                ProgramConstantKind.Bytes => throw new InvalidOperationException(
                    $"Bytes constant '{constant.Identity}' cannot enter typed Character state evaluation."),
                _ => throw new ArgumentOutOfRangeException(nameof(constant.Kind))
            };
        }
    }
}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   

