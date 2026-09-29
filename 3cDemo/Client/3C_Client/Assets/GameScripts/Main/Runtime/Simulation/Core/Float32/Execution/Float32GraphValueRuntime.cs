using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    internal sealed class Float32ValueInputBuffer
    {
        public List<AbilityStateValue> Values { get; } = new List<AbilityStateValue>();

        public void Clear()
        {
            Values.Clear();
        }
    }

    internal readonly struct Float32ValueInputLease : IDisposable
    {
        readonly Float32GraphValueRuntime m_Owner;
        readonly Float32ValueInputBuffer m_Buffer;
        readonly int m_Depth;

        public Float32ValueInputLease(
            Float32GraphValueRuntime owner,
            Float32ValueInputBuffer buffer,
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

    internal sealed class Float32GraphValueWorkspace
    {
        internal readonly HashSet<long> ValueStack = new();
        internal readonly List<Float32ValueInputBuffer> InputBuffers = new();

        public Float32GraphValueWorkspace(Float32GameplayAbilityExecutionData data, GameplayAbilityExecutionLayout layout)
        {
            int depthCapacity = checked(data.Operations.Count + 1);
            int inputCapacity = 0;
            for (int index = 0; index < data.Operations.Count; index++)
                inputCapacity = Math.Max(inputCapacity, layout.ValueInputs(data.Operations[index].Handle).Length);
            ValueStack.EnsureCapacity(depthCapacity);
            while (InputBuffers.Count < depthCapacity)
                InputBuffers.Add(new Float32ValueInputBuffer());
            for (int index = 0; index < InputBuffers.Count; index++)
                if (InputBuffers[index].Values.Capacity < inputCapacity)
                    InputBuffers[index].Values.Capacity = inputCapacity;
        }

        internal void Reset()
        {
            ValueStack.Clear();
        }
    }

    internal abstract class Float32GraphValueRuntime : IFloat32ValueInputReader
    {
        protected readonly Float32GameplayAbilityExecutionData m_Ability;
        protected readonly GameplayAbilityExecutionLayout m_Layout;
        readonly HashSet<long> m_ValueStack;
        readonly List<Float32ValueInputBuffer> m_InputBuffers;
        int m_InputBufferDepth;

        protected Float32GraphValueRuntime(Float32GameplayAbilityExecutionData data,
            GameplayAbilityExecutionLayout layout, Float32GraphValueWorkspace workspace)
        {
            m_Ability = data ?? throw new ArgumentNullException(nameof(data));
            m_Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            m_ValueStack = workspace.ValueStack;
            m_InputBuffers = workspace.InputBuffers;
        }

        protected abstract AbilityStateValue EvaluateDomainValue<TTarget>(OperationControlCursor<TTarget> cursor,
            SimulationOperation operation, string outputPort, Float32ValueInputLease inputs)
            where TTarget : struct, IOperationControlTarget<TTarget>;
        protected abstract void ResetGraphCallParameter(int slot);
        protected abstract void WriteGraphCallParameter(int slot, AbilityStateValue value);
        protected abstract AbilityStateValue ReadGraphCallParameter(int slot);
        protected abstract void TraceResult(SimulationOperation operation, string outputPort, AbilityStateValue value, bool predictive);
        protected abstract void TraceInput(SimulationOperation operation, CompiledValueInputBinding input, AbilityStateValue value, bool predictive);

        public void BeginEvaluation()
        {
            if (m_InputBufferDepth != 0 || m_ValueStack.Count != 0)
                throw new InvalidOperationException("Float32 value runtime retained recursion state across evaluations.");
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
            using Float32ValueInputLease inputs = ReadInputs(cursor, operation);
            if (inputs.Count != frame.Inputs.Count)
                throw new InvalidOperationException($"Graph call frame '{frame.Identity}' received '{inputs.Count}' inputs, expected '{frame.Inputs.Count}'.");
            for (int i = 0; i < frame.Outputs.Count; i++)
                ResetGraphCallParameter(frame.Outputs[i].StateSlot);
            for (int i = 0; i < frame.Inputs.Count; i++)
                WriteGraphCallParameter(frame.Inputs[i].StateSlot, inputs[i]);
        }

		public AbilityStateValue Evaluate<TTarget>(
			OperationControlCursor<TTarget> cursor,
			OperationHandle handle,
			string outputPort = "",
			int outputPortIndex = -1)
			where TTarget : struct, IOperationControlTarget<TTarget>
		{
			cursor.RequireExecution(handle);
			long valueKey = (long)handle.Value << 32 | (uint)outputPortIndex;
			if (!m_ValueStack.Add(valueKey))
				throw new InvalidOperationException($"Value operation cycle reached '{handle}/{outputPort}'.");
			try
			{
				SimulationOperation operation = m_Ability.Operations[handle.Value];
				using Float32ValueInputLease inputs = ReadInputs(cursor, operation);
				AbilityStateValue result;
				switch (operation.Code)
                {
                    case SimulationOperationCode.ConditionResult:
						result = AbilityStateValue.FromBoolean(inputs.Count > 0 && ToBoolean(inputs[0]));
						break;
                    case SimulationOperationCode.SubGraph:
						result = ReadSubGraphOutput(cursor, operation, outputPort);
						break;
                    case SimulationOperationCode.Compare:
						result = AbilityStateValue.FromBoolean(Compare(operation.Integer0, inputs));
						break;
                    case SimulationOperationCode.And:
						result = AbilityStateValue.FromBoolean(inputs.Count >= 2 && ToBoolean(inputs[0]) && ToBoolean(inputs[1]));
						break;
                    case SimulationOperationCode.Or:
						result = AbilityStateValue.FromBoolean(inputs.Count > 0 && (ToBoolean(inputs[0]) || inputs.Count > 1 && ToBoolean(inputs[1])));
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
                        result = EvaluateDomainValue(cursor, operation, outputPort, inputs);
                        break;
                }
                TraceResult(operation, outputPort, result, cursor.IsPredictiveEvaluation);
				return result;
			}
			finally
			{
				m_ValueStack.Remove(valueKey);
			}
		}

        public bool EvaluateCondition<TTarget>(
            OperationControlCursor<TTarget> cursor,
            ProgramControlFlowEdge edge)
            where TTarget : struct, IOperationControlTarget<TTarget>
        {
            return edge != null && (!edge.HasCondition || ToBoolean(Evaluate(cursor, edge.Condition)));
        }

        public Float32ValueInputLease ReadInputs<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation)
            where TTarget : struct, IOperationControlTarget<TTarget>
        {
            ReadOnlySpan<CompiledValueInputBinding> inputs = m_Layout.ValueInputs(operation.Handle);
            int depth = m_InputBufferDepth;
            Float32ValueInputBuffer buffer = RequireInputBuffer(depth);
            m_InputBufferDepth++;
            try
            {
                for (int i = 0; i < inputs.Length; i++)
                {
                    ref readonly CompiledValueInputBinding input = ref inputs[i];
                    AbilityStateValue value = input.SourceKind == CompiledValueInputSourceKind.Operation
                        ? Evaluate(
                            cursor,
                            input.SourceOperation,
                            input.SourceOutputPortIdentity,
                            input.SourceOutputPortIndex)
                        : ValueFromConstant(m_Ability.Constants[input.ConstantIndex]);
                    buffer.Values.Add(value);
                    TraceInput(operation, input, value, cursor.IsPredictiveEvaluation);
                    if (i == 0 &&
                        (operation.Code == SimulationOperationCode.And && !ToBoolean(value) ||
                         operation.Code == SimulationOperationCode.Or && ToBoolean(value)))
                        break;
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
            if (depth >= m_InputBuffers.Count)
                throw new InvalidOperationException("Graph value recursion exceeds its prepared operation bound.");
            return m_InputBuffers[depth];
        }

        internal void ReleaseInputBuffer(Float32ValueInputBuffer buffer, int depth)
        {
            if (depth != m_InputBufferDepth - 1 || !ReferenceEquals(buffer, m_InputBuffers[depth]))
                throw new InvalidOperationException("Float32 value input buffers must be released in recursion order.");
            buffer.Clear();
            m_InputBufferDepth--;
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
            return ReadGraphCallParameter(binding.StateSlot);
        }

        ProgramGraphCallFrame RequireGraphCallFrame(SimulationOperation operation)
        {
            IReadOnlyList<ProgramGraphCallFrame> frames = m_Layout.Topology.GraphCallFrames(operation.Handle);
            if (frames.Count != 1)
                throw new InvalidOperationException($"SubGraph operation '{operation.Handle}' requires exactly one graph call frame.");
            return frames[0];
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

        protected static Float32Scalar ToScalar(AbilityStateValue value)
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

        public static bool ToBoolean(AbilityStateValue value)
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

        protected static AbilityStateValue ConvertValue(AbilityStateValue value, ProgramStateValueKind expected)
        {
            if (value.Kind == expected)
                return value;
            if (expected == ProgramStateValueKind.Scalar)
                return AbilityStateValue.FromScalar(ToScalar(value));
            if (expected == ProgramStateValueKind.Int32 && value.Kind == ProgramStateValueKind.Scalar)
                return AbilityStateValue.FromInt32(checked((int)value.Scalar.ToSingle()));
            throw new InvalidOperationException($"Cannot assign '{value.Kind}' to '{expected}'.");
        }

        protected static AbilityStateValue ValueFromConstant(ProgramConstant constant)
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
