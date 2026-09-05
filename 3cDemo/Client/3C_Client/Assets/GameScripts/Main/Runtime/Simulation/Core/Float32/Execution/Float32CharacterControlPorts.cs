using System;

namespace ThirdPersonSimulation
{
    internal sealed class Float32CharacterControlReadPort : ICharacterControlReadPort
    {
		readonly Float32InputRuntime m_Input;
		readonly Float32EvaluationFrame m_Frame;
		readonly Func<CharacterControlParameterId, Float32Scalar> m_ReadParameter;
		readonly Func<CharacterSkillId, bool> m_IsSkillActive;
		readonly Func<CharacterSkillId, bool> m_IsSkillCompleted;
		readonly Func<CharacterSkillId, ulong> m_CompletedSkillInstanceId;
		readonly Func<CharacterSkillId, string, bool> m_IsActionWindowActive;

        public Float32CharacterControlReadPort(
			Float32InputRuntime input,
			Float32EvaluationFrame frame,
			Func<CharacterControlParameterId, Float32Scalar> readParameter,
			Func<CharacterSkillId, bool> isSkillActive,
			Func<CharacterSkillId, bool> isSkillCompleted,
			Func<CharacterSkillId, ulong> completedSkillInstanceId,
			Func<CharacterSkillId, string, bool> isActionWindowActive)
		{
			m_Input = input ?? throw new ArgumentNullException(nameof(input));
			m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
			m_ReadParameter = readParameter ?? throw new ArgumentNullException(nameof(readParameter));
			m_IsSkillActive = isSkillActive ?? throw new ArgumentNullException(nameof(isSkillActive));
			m_IsSkillCompleted = isSkillCompleted ?? throw new ArgumentNullException(nameof(isSkillCompleted));
			m_CompletedSkillInstanceId = completedSkillInstanceId ?? throw new ArgumentNullException(nameof(completedSkillInstanceId));
			m_IsActionWindowActive = isActionWindowActive ?? throw new ArgumentNullException(nameof(isActionWindowActive));
		}

		public bool HasInputRequest(string requestId) => m_Input.HasRequest(requestId, out _);
		public bool IsSkillActive(CharacterSkillId skillId) => m_IsSkillActive(skillId);
		public bool IsSkillCompleted(CharacterSkillId skillId) => m_IsSkillCompleted(skillId);
		public ulong CompletedSkillInstanceId(CharacterSkillId skillId) => m_CompletedSkillInstanceId(skillId);
		public bool IsActionWindowActive(CharacterSkillId skillId, string windowType) => m_IsActionWindowActive(skillId, windowType);

		public bool CompareInputVector2Magnitude(
            SimulationInputValueId input,
            CharacterControlParameterId threshold,
            CharacterControlNumericComparison comparison)
        {
            Float32Vector2 value = m_Input.ReadValue(input.Value, SimulationInputValueKind.Vector2).Vector2;
            return Compare(value.Magnitude, m_ReadParameter(threshold), comparison);
        }

		public bool CompareInputDirectionToBodyYaw(
			SimulationInputValueId input,
			CharacterControlParameterId threshold,
			CharacterControlNumericComparison comparison)
        {
            Float32Vector2 value = m_Input.ReadValue(input.Value, SimulationInputValueKind.Vector2).Vector2;
            Float32Scalar angle = value == Float32Vector2.Zero
                ? Float32Scalar.Zero
                : Float32Scalar.Abs(Float32Angle.Delta(m_Frame.Body.Yaw, Float32Angle.FromPlanarDirection(value)));
			return Compare(angle, m_ReadParameter(threshold), comparison);
		}

		public bool IsInputDirectionBehindBodyYaw(SimulationInputValueId input)
		{
			Float32Vector2 value = m_Input.ReadValue(input.Value, SimulationInputValueKind.Vector2).Vector2;
			if (value == Float32Vector2.Zero)
				return false;
			Float32Scalar angle = Float32Scalar.Abs(Float32Angle.Delta(m_Frame.Body.Yaw, Float32Angle.FromPlanarDirection(value)));
			return angle >= Float32Scalar.FromInt64(90);
		}

        static bool Compare(
            Float32Scalar value,
            Float32Scalar threshold,
            CharacterControlNumericComparison comparison)
        {
            return comparison switch
            {
                CharacterControlNumericComparison.Less => value < threshold,
                CharacterControlNumericComparison.LessOrEqual => value <= threshold,
                CharacterControlNumericComparison.Equal => value == threshold,
                CharacterControlNumericComparison.Greater => value > threshold,
                CharacterControlNumericComparison.GreaterOrEqual => value >= threshold,
                _ => throw new ArgumentOutOfRangeException(nameof(comparison))
            };
        }
    }

    internal sealed class Float32CharacterControlStatePort : ICharacterControlStatePort
    {
        readonly Float32StatePort m_State;
        readonly CharacterControlStateLayout m_Layout;

        public Float32CharacterControlStatePort(
            Float32StatePort state,
            CharacterControlStateLayout layout)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            m_Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        }

        public CharacterControlStateId ReadState(CharacterControlStateFieldId field)
        {
            RequireKind(field, ProgramStateValueKind.Identity);
            string value = m_State.Get(m_Layout.RequireSlot(field)).Identity;
            return string.IsNullOrEmpty(value) ? default : new CharacterControlStateId(value);
        }

        public void WriteState(CharacterControlStateFieldId field, CharacterControlStateId value)
        {
            RequireKind(field, ProgramStateValueKind.Identity);
            m_State.Set(m_Layout.RequireSlot(field), CharacterStateValue.FromIdentity(value.Value));
        }

        public CharacterControlTransitionId ReadTransition(CharacterControlStateFieldId field)
        {
            RequireKind(field, ProgramStateValueKind.Identity);
            string value = m_State.Get(m_Layout.RequireSlot(field)).Identity;
            return string.IsNullOrEmpty(value) ? default : new CharacterControlTransitionId(value);
        }

        public bool ReadBoolean(CharacterControlStateFieldId field)
        {
            RequireKind(field, ProgramStateValueKind.Boolean);
            return m_State.Get(m_Layout.RequireSlot(field)).Boolean;
        }

        public void WriteTransition(CharacterControlStateFieldId field, CharacterControlTransitionId value)
        {
            RequireKind(field, ProgramStateValueKind.Identity);
            m_State.Set(m_Layout.RequireSlot(field), CharacterStateValue.FromIdentity(value.Value));
        }

        public void WriteBoolean(CharacterControlStateFieldId field, bool value)
        {
            RequireKind(field, ProgramStateValueKind.Boolean);
            m_State.Set(m_Layout.RequireSlot(field), CharacterStateValue.FromBoolean(value));
        }

        public int ReadInt32(CharacterControlStateFieldId field)
        {
            RequireKind(field, ProgramStateValueKind.Int32);
            return m_State.Get(m_Layout.RequireSlot(field)).Int32;
        }

        public void WriteInt32(CharacterControlStateFieldId field, int value)
        {
            RequireKind(field, ProgramStateValueKind.Int32);
            m_State.Set(m_Layout.RequireSlot(field), CharacterStateValue.FromInt32(value));
        }

        public ulong ReadUInt64(CharacterControlStateFieldId field)
        {
            RequireKind(field, ProgramStateValueKind.UInt64);
            return m_State.Get(m_Layout.RequireSlot(field)).UInt64;
        }

        public void WriteUInt64(CharacterControlStateFieldId field, ulong value)
        {
            RequireKind(field, ProgramStateValueKind.UInt64);
            m_State.Set(m_Layout.RequireSlot(field), CharacterStateValue.FromUInt64(value));
        }

        void RequireKind(CharacterControlStateFieldId field, ProgramStateValueKind expected)
        {
            if (m_Layout.RequireKind(field) != expected)
                throw new InvalidOperationException($"Control state field '{field}' is not '{expected}'.");
        }
    }
}
