using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal readonly struct Float32ValueEvaluationKey : IEquatable<Float32ValueEvaluationKey>
    {
        public Float32ValueEvaluationKey(int operation, string outputPort)
        {
            Operation = operation;
            OutputPort = outputPort ?? string.Empty;
        }

        public int Operation { get; }
        public string OutputPort { get; }

        public bool Equals(Float32ValueEvaluationKey other) =>
            Operation == other.Operation && string.Equals(OutputPort, other.OutputPort, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is Float32ValueEvaluationKey other && Equals(other);
        public override int GetHashCode() => unchecked(Operation * 397 ^ StringComparer.Ordinal.GetHashCode(OutputPort));
    }

    internal sealed class Float32ValueInputBuffer
    {
        public List<CharacterStateValue> Values { get; } = new List<CharacterStateValue>();

        public void Clear()
        {
            Values.Clear();
        }
    }

    internal readonly struct Float32ValueInputLease : IDisposable
    {
        readonly Float32ValueRuntime m_Owner;
        readonly Float32ValueInputBuffer m_Buffer;
        readonly int m_Depth;

        public Float32ValueInputLease(
            Float32ValueRuntime owner,
            Float32ValueInputBuffer buffer,
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

    internal sealed class Float32ValueRuntime : Float32OperationModule, IFloat32ValueInputReader
    {
        readonly IFloat32InputPort m_Input;
        readonly IFloat32ActionContextReader m_Actions;
        readonly IFloat32ActionAdmissionQuery m_ActionAdmission;
        readonly IFloat32GameplayTagQuery m_GameplayTags;
        readonly Float32EquipmentRuntime m_Equipment;
        readonly IFloat32BlackboardPort m_Blackboard;
        readonly Float32EvaluationFrame m_Frame;
        readonly HashSet<Float32ValueEvaluationKey> m_ValueStack;
        readonly List<Float32ValueInputBuffer> m_InputBuffers;
        int m_InputBufferDepth;

        public Float32ValueRuntime(
            Float32ProgramAccess access,
            IFloat32InputPort input,
            IFloat32ActionContextReader actions,
            IFloat32ActionAdmissionQuery actionAdmission,
            IFloat32GameplayTagQuery gameplayTags,
            Float32EquipmentRuntime equipment,
            IFloat32BlackboardPort blackboard,
            Float32EvaluationFrame frame,
            Float32EvaluationWorkspace workspace)
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
                throw new InvalidOperationException("Float32 value runtime retained recursion state across evaluations.");
        }

		public CharacterStateValue Evaluate<TTarget>(
			OperationControlCursor<TTarget> cursor,
			OperationHandle handle,
			string outputPort = "")
			where TTarget : struct, IOperationControlTarget<TTarget>
		{
			cursor.RequireExecution(handle);
			var valueKey = new Float32ValueEvaluationKey(handle.Value, outputPort);
			if (!m_ValueStack.Add(valueKey))
				throw new InvalidOperationException($"Value operation cycle reached '{handle}/{outputPort}'.");
			try
			{
				SimulationOperation operation = Access.Operation(handle);
				using Float32ValueInputLease inputs = ReadInputs(cursor, operation);
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
				ProgramStateValueKind.Vector2 => $"({value.Vector2.X},{value.Vector2.Y})",
				ProgramStateValueKind.Vector3 => $"({value.Vector3.X},{value.Vector3.Y},{value.Vector3.Z})",
				ProgramStateValueKind.Yaw => value.Yaw.Degrees.ToString(),
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
            using Float32ValueInputLease values = ReadInputs(cursor, operation);
            if (values.Count == 0)
                return false;
            ProgramStateValueKind expected = m_Program.StateSlots[reference.TargetIndex].ValueKind;
            m_Blackboard.Write(cursor, operation, reference.TargetIndex, ConvertValue(values[0], expected));
            return true;
        }

        public Float32ValueInputLease ReadInputs<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation)
            where TTarget : struct, IOperationControlTarget<TTarget>
        {
            ReadOnlySpan<CompiledValueInputBinding> inputs = m_Layout.ValueInputs(operation.Handle);
            int depth = m_InputBufferDepth++;
            Float32ValueInputBuffer buffer = RequireInputBuffer(depth);
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
                return new Float32ValueInputLease(this, buffer, depth);
            }
            catch
            {
                ReleaseInputBuffer(buffer, depth);
                throw;
            }
        }

        Float32ValueInputBuffer RequireInputBuffer(int depth)
        {
            while (m_InputBuffers.Count <= depth)
                m_InputBuffers.Add(new Float32ValueInputBuffer());
            return m_InputBuffers[depth];
        }

        internal void ReleaseInputBuffer(Float32ValueInputBuffer buffer, int depth)
        {
            if (depth != m_InputBufferDepth - 1 || !ReferenceEquals(buffer, m_InputBuffers[depth]))
                throw new InvalidOperationException("Float32 value input buffers must be released in recursion order.");
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

        Float32Scalar ReadMoveFacingAngle(Float32ValueInputLease inputs)
        {
            if (inputs.Count == 0 ||
                inputs[0].Kind != ProgramStateValueKind.Vector2 ||
                inputs[0].Vector2 == Float32Vector2.Zero)
                return Float32Scalar.Zero;
            Float32Yaw desired = Float32Angle.FromPlanarDirection(inputs[0].Vector2);
            return Float32Scalar.Abs(Float32Angle.Delta(m_Frame.Body.Yaw, desired));
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

        static bool Compare(int comparison, Float32ValueInputLease inputs)
        {
            if (inputs.Count < 2)
                return false;
            Float32Scalar left = ToScalar(inputs[0]);
            Float32Scalar right = ToScalar(inputs[1]);
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

        static Float32Scalar ToScalar(CharacterStateValue value)
        {
            return value.Kind switch
            {
                ProgramStateValueKind.Int32 => Float32Scalar.FromInt64(value.Int32),
                ProgramStateValueKind.UInt64 when value.UInt64 <= long.MaxValue => Float32Scalar.FromInt64((long)value.UInt64),
                ProgramStateValueKind.Scalar => value.Scalar,
                ProgramStateValueKind.Boolean => value.Boolean ? Float32Scalar.One : Float32Scalar.Zero,
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
                ProgramStateValueKind.Scalar => value.Scalar != Float32Scalar.Zero,
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
                return CharacterStateValue.FromInt32(checked((int)value.Scalar.ToSingle()));
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
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     
