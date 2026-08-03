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
        public List<CharacterStateValue> Values { get; } = new List<CharacterStateValue>();

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
        public CharacterStateValue this[int index] => m_Buffer.Values[index];

        public CharacterStateValue FindByKind(ProgramStateValueKind kind)
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
        readonly FixedEvaluationFrame m_Frame;
        readonly HashSet<FixedValueEvaluationKey> m_ValueStack;
        readonly List<FixedValueInputBuffer> m_InputBuffers;
        int m_InputBufferDepth;

        public FixedValueRuntime(
            FixedProgramAccess access,
            IFixedInputPort input,
            IFixedActionContextReader actions,
            IFixedActionAdmissionQuery actionAdmission,
            IFixedGameplayTagQuery gameplayTags,
            FixedEquipmentRuntime equipment,
            IFixedBlackboardPort blackboard,
            FixedEvaluationFrame frame,
            FixedEvaluationWorkspace workspace)
            : base(access)
        {
            m_Input = input ?? throw new ArgumentNullException(nameof(input));
            m_Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            m_ActionAdmission = actionAdmission ?? throw new ArgumentNullException(nameof(actionAdmission));
            m_GameplayTags = gameplayTags ?? throw new ArgumentNullException(nameof(gameplayTags));
            m_Equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
            m_Blackboard = blackboard ?? throw new ArgumentNullException(nameof(blackboard));
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
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

		public CharacterStateValue Evaluate<TTarget>(
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
				CharacterStateValue result;
				switch (operation.Code)
				{
					case SimulationOperationCode.ConditionResult:
						result = CharacterStateValue.FromBoolean(inputs.Count > 0 && ToBoolean(inputs[0]));
						break;
					case SimulationOperationCode.InputBoolean:
						result = CharacterStateValue.FromBoolean(m_Input.ReadValue(operation.Text0, SimulationInputValueKind.Boolean).Boolean);
						break;
					case SimulationOperationCode.InputScalar:
						result = CharacterStateValue.FromScalar(m_Input.ReadValue(operation.Text0, SimulationInputValueKind.Scalar).Scalar);
						break;
					case SimulationOperationCode.InputVector2:
						result = CharacterStateValue.FromVector2(m_Input.ReadValue(operation.Text0, SimulationInputValueKind.Vector2).Vector2);
						break;
					case SimulationOperationCode.InputVector2Magnitude:
						result = CharacterStateValue.FromScalar(m_Input.ReadValue(operation.Text0, SimulationInputValueKind.Vector2).Vector2.Magnitude);
						break;
					case SimulationOperationCode.InputRequest:
						result = CharacterStateValue.FromBoolean(m_Input.HasRequest(operation.Text0, out _));
						break;
					case SimulationOperationCode.BlackboardGet:
						result = ReadBlackboard(cursor, operation);
						break;
					case SimulationOperationCode.ActionContextActive:
						result = CharacterStateValue.FromBoolean(m_Actions.IsContextActive(operation.Text0));
						break;
					case SimulationOperationCode.ActionWindowActive:
						result = CharacterStateValue.FromBoolean(m_Blackboard.IsActionWindowActive(operation));
						break;
					case SimulationOperationCode.CanActivateAction:
						result = CharacterStateValue.FromBoolean(m_ActionAdmission.PreviewActivation(cursor, operation).Allowed);
						break;
					case SimulationOperationCode.GameplayEffectHasTag:
						result = CharacterStateValue.FromBoolean(m_GameplayTags.HasTag(operation.Text0));
						break;
					case SimulationOperationCode.GameplayEffectMatchTags:
						result = CharacterStateValue.FromBoolean(
							m_GameplayTags.Matches(Access.Services.RequireTagQuery(operation.Handle)));
						break;
					case SimulationOperationCode.GameplayAttributeRead:
						result = m_GameplayTags.ReadAttribute(operation, outputPort);
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
						result = m_Equipment.Evaluate(operation, outputPort, inputs);
						break;
					case SimulationOperationCode.StateRootCompleted:
						result = CharacterStateValue.FromBoolean(cursor.CurrentStateRootCompleted());
						break;
					case SimulationOperationCode.StateExitCause:
						result = CharacterStateValue.FromBoolean(operation.Integer0 == cursor.CurrentStateExitCause());
						break;
					case SimulationOperationCode.MoveFacingAngle:
						result = CharacterStateValue.FromScalar(ReadMoveFacingAngle(inputs));
						break;
					case SimulationOperationCode.Compare:
						result = CharacterStateValue.FromBoolean(Compare(operation.Integer0, inputs));
						break;
					case SimulationOperationCode.And:
						result = CharacterStateValue.FromBoolean(inputs.Count >= 2 && ToBoolean(inputs[0]) && ToBoolean(inputs[1]));
						break;
					case SimulationOperationCode.Or:
						result = CharacterStateValue.FromBoolean(inputs.Count >= 2 && (ToBoolean(inputs[0]) || ToBoolean(inputs[1])));
						break;
					case SimulationOperationCode.Not:
						result = CharacterStateValue.FromBoolean(inputs.Count == 0 || !ToBoolean(inputs[0]));
						break;
					case SimulationOperationCode.Constant:
						result = operation.ConstantReferences.Count > 0
							? ValueFromConstant(m_Program.Constants[operation.ConstantReferences[0]])
							: CharacterStateValue.FromBoolean(false);
						break;
					default:
						throw new InvalidOperationException($"Operation '{handle}' code '{operation.Code}' is not a value operation.");
				}
				TraceValue(operation, result);
				return result;
			}
			finally
			{
				m_ValueStack.Remove(valueKey);
			}
		}

		void TraceValue(SimulationOperation operation, CharacterStateValue value)
		{
			if (!m_Frame.Trace.Enabled ||
			    operation.Code != SimulationOperationCode.InputVector2 &&
			    operation.Code != SimulationOperationCode.InputVector2Magnitude &&
			    operation.Code != SimulationOperationCode.MoveFacingAngle &&
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

		static string FormatValue(CharacterStateValue value)
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
            ProgramStateValueKind expected = m_Program.StateSlots[reference.TargetIndex].ValueKind;
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
                    buffer.Values.Add(input.SourceKind == CompiledValueInputSourceKind.Operation
                        ? Evaluate(cursor, input.SourceOperation, m_Layout.ValueSourceOutputPort(input))
                        : ValueFromConstant(m_Program.Constants[input.ConstantIndex]));
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

        CharacterStateValue ReadBlackboard<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation)
            where TTarget : struct, IOperationControlTarget<TTarget>
        {
            ProgramReference reference = m_Layout.Topology.FirstReference(operation.Handle, ProgramReferenceKind.StateSlot);
            if (reference == null)
                throw new InvalidOperationException($"Blackboard operation '{operation.Handle}' has no state address.");
            return m_Blackboard.Read(cursor, operation, reference.TargetIndex);
        }

        FixedScalar ReadMoveFacingAngle(FixedValueInputLease inputs)
        {
            if (inputs.Count == 0 ||
                inputs[0].Kind != ProgramStateValueKind.Vector2 ||
                inputs[0].Vector2 == FixedVector2.Zero)
                return FixedScalar.Zero;
            FixedYaw desired = FixedAngle.FromPlanarDirection(inputs[0].Vector2);
            return FixedScalar.Abs(FixedAngle.Delta(m_Frame.Body.Yaw, desired));
        }

        CharacterStateValue ReadCameraBasis(string outputPort)
        {
            return outputPort switch
            {
                CameraProgramOperationSchema.BasisValidPortId => CharacterStateValue.FromBoolean(
                    m_Input.ReadValue(CameraProgramOperationSchema.BasisValidInputId, SimulationInputValueKind.Boolean).Boolean),
                CameraProgramOperationSchema.BasisPlanarForwardPortId => CharacterStateValue.FromVector3(
                    m_Input.ReadValue(CameraProgramOperationSchema.BasisPlanarForwardInputId, SimulationInputValueKind.Vector3).Vector3),
                CameraProgramOperationSchema.BasisPlanarRightPortId => CharacterStateValue.FromVector3(
                    m_Input.ReadValue(CameraProgramOperationSchema.BasisPlanarRightInputId, SimulationInputValueKind.Vector3).Vector3),
                CameraProgramOperationSchema.BasisLookDirectionPortId => CharacterStateValue.FromVector3(
                    m_Input.ReadValue(CameraProgramOperationSchema.BasisLookDirectionInputId, SimulationInputValueKind.Vector3).Vector3),
                CameraProgramOperationSchema.BasisAimPointPortId => CharacterStateValue.FromVector3(
                    m_Input.ReadValue(CameraProgramOperationSchema.BasisAimPointInputId, SimulationInputValueKind.Vector3).Vector3),
                CameraProgramOperationSchema.BasisYawPortId => CharacterStateValue.FromYaw(
                    m_Input.ReadValue(CameraProgramOperationSchema.BasisYawInputId, SimulationInputValueKind.Yaw).Yaw),
                CameraProgramOperationSchema.BasisPitchPortId => CharacterStateValue.FromScalar(
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

        static FixedScalar ToScalar(CharacterStateValue value)
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

        public static bool ToBoolean(CharacterStateValue value)
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

        static CharacterStateValue ConvertValue(CharacterStateValue value, ProgramStateValueKind expected)
        {
            if (value.Kind == expected)
                return value;
            if (expected == ProgramStateValueKind.Scalar)
                return CharacterStateValue.FromScalar(ToScalar(value));
            if (expected == ProgramStateValueKind.Int32 && value.Kind == ProgramStateValueKind.Scalar)
                return CharacterStateValue.FromInt32(value.Scalar.TruncateToInt32());
            throw new InvalidOperationException($"Cannot assign '{value.Kind}' to '{expected}'.");
        }

        static CharacterStateValue ValueFromConstant(ProgramConstant constant)
        {
            return constant.Kind switch
            {
                ProgramConstantKind.Boolean => CharacterStateValue.FromBoolean(constant.Boolean),
                ProgramConstantKind.Int32 => CharacterStateValue.FromInt32(constant.Int32),
                ProgramConstantKind.UInt64 => CharacterStateValue.FromUInt64(constant.UInt64),
                ProgramConstantKind.Scalar => CharacterStateValue.FromScalar(constant.Scalar),
                ProgramConstantKind.Vector2 => CharacterStateValue.FromVector2(constant.Vector2),
                ProgramConstantKind.Vector3 => CharacterStateValue.FromVector3(constant.Vector3),
                ProgramConstantKind.Yaw => CharacterStateValue.FromYaw(constant.Yaw),
                ProgramConstantKind.String => CharacterStateValue.FromIdentity(constant.Text),
                ProgramConstantKind.Bytes => throw new InvalidOperationException(
                    $"Bytes constant '{constant.Identity}' cannot enter typed Character state evaluation."),
                _ => throw new ArgumentOutOfRangeException(nameof(constant.Kind))
            };
        }
    }
}
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   
