using System;

namespace ThirdPersonSimulation
{
    internal sealed class Float32CharacterControlReadPort : ICharacterControlReadPort
    {
		readonly Float32InputRuntime m_Input;
		readonly Float32EvaluationFrame m_Frame;
		readonly Func<CharacterControlParameterId, Float32Scalar> m_ReadParameter;
		readonly Func<CharacterSkillId, bool> m_IsAbilityActive;
		readonly Func<CharacterSkillId, (bool Found, ulong InstanceId)> m_TryGetActiveAbilityInstanceId;
		readonly Func<CharacterSkillId, bool> m_IsAbilityCompleted;
		readonly Func<CharacterSkillId, ulong> m_CompletedAbilityInstanceId;
		readonly Func<CharacterSkillId, string, bool> m_IsActionWindowActive;
        readonly Func<EquipmentActionRouteId, (bool Found, EquipmentActionContext Context)> m_TryReadEquipmentActionContext;

        public Float32CharacterControlReadPort(
			Float32InputRuntime input,
			Float32EvaluationFrame frame,
			Func<CharacterControlParameterId, Float32Scalar> readParameter,
			Func<CharacterSkillId, bool> isSkillActive,
			Func<CharacterSkillId, (bool Found, ulong InstanceId)> tryGetActiveSkillInstanceId,
			Func<CharacterSkillId, bool> isSkillCompleted,
			Func<CharacterSkillId, ulong> completedSkillInstanceId,
			Func<CharacterSkillId, string, bool> isActionWindowActive,
            Func<EquipmentActionRouteId, (bool Found, EquipmentActionContext Context)> tryReadEquipmentActionContext)
		{
			m_Input = input ?? throw new ArgumentNullException(nameof(input));
			m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
			m_ReadParameter = readParameter ?? throw new ArgumentNullException(nameof(readParameter));
			m_IsAbilityActive = isSkillActive ?? throw new ArgumentNullException(nameof(isSkillActive));
			m_TryGetActiveAbilityInstanceId = tryGetActiveSkillInstanceId ?? throw new ArgumentNullException(nameof(tryGetActiveSkillInstanceId));
			m_IsAbilityCompleted = isSkillCompleted ?? throw new ArgumentNullException(nameof(isSkillCompleted));
			m_CompletedAbilityInstanceId = completedSkillInstanceId ?? throw new ArgumentNullException(nameof(completedSkillInstanceId));
			m_IsActionWindowActive = isActionWindowActive ?? throw new ArgumentNullException(nameof(isActionWindowActive));
            m_TryReadEquipmentActionContext = tryReadEquipmentActionContext ?? throw new ArgumentNullException(nameof(tryReadEquipmentActionContext));
		}

		public bool HasInputRequest(string requestId) => m_Input.HasRequest(requestId, out _);
		public bool IsAbilityActive(CharacterSkillId abilityId) => m_IsAbilityActive(abilityId);
		public bool TryGetActiveAbilityInstanceId(CharacterSkillId abilityId, out ulong instanceId)
		{
			(bool found, ulong value) = m_TryGetActiveAbilityInstanceId(abilityId);
			instanceId = value;
			return found;
		}
		public bool IsAbilityCompleted(CharacterSkillId abilityId) => m_IsAbilityCompleted(abilityId);
		public ulong CompletedAbilityInstanceId(CharacterSkillId abilityId) => m_CompletedAbilityInstanceId(abilityId);
		public bool IsAbilityWindowActive(CharacterSkillId abilityId, string windowType) => m_IsActionWindowActive(abilityId, windowType);
        public bool TryReadEquipmentActionContext(EquipmentActionRouteId routeId, out EquipmentActionContext context)
        {
            (bool found, EquipmentActionContext value) = m_TryReadEquipmentActionContext(routeId);
            context = value;
            return found;
        }

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
        readonly CharacterControlRuntimeStateTransaction m_State;
        readonly CharacterControlStateSchema m_Schema;

        public Float32CharacterControlStatePort(
            CharacterControlRuntimeStateTransaction state,
            CharacterControlStateSchema schema)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            m_Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

        public CharacterControlStateId ReadState(CharacterControlStateFieldId field)
        {
            RequireKind(field, CharacterControlStateValueKind.Identity);
            string value = m_State.Get(field).Identity;
            return string.IsNullOrEmpty(value) ? default : new CharacterControlStateId(value);
        }

        public void WriteState(CharacterControlStateFieldId field, CharacterControlStateId value)
        {
            RequireKind(field, CharacterControlStateValueKind.Identity);
            m_State.Set(field, CharacterControlStateValue.FromIdentity(value.Value));
        }

        public CharacterControlTransitionId ReadTransition(CharacterControlStateFieldId field)
        {
            RequireKind(field, CharacterControlStateValueKind.Identity);
            string value = m_State.Get(field).Identity;
            return string.IsNullOrEmpty(value) ? default : new CharacterControlTransitionId(value);
        }

        public bool ReadBoolean(CharacterControlStateFieldId field)
        {
            RequireKind(field, CharacterControlStateValueKind.Boolean);
            return m_State.Get(field).Boolean;
        }

        public void WriteTransition(CharacterControlStateFieldId field, CharacterControlTransitionId value)
        {
            RequireKind(field, CharacterControlStateValueKind.Identity);
            m_State.Set(field, CharacterControlStateValue.FromIdentity(value.Value));
        }

        public void WriteBoolean(CharacterControlStateFieldId field, bool value)
        {
            RequireKind(field, CharacterControlStateValueKind.Boolean);
            m_State.Set(field, CharacterControlStateValue.FromBoolean(value));
        }

        public int ReadInt32(CharacterControlStateFieldId field)
        {
            RequireKind(field, CharacterControlStateValueKind.Int32);
            return m_State.Get(field).Int32;
        }

        public void WriteInt32(CharacterControlStateFieldId field, int value)
        {
            RequireKind(field, CharacterControlStateValueKind.Int32);
            m_State.Set(field, CharacterControlStateValue.FromInt32(value));
        }

        public ulong ReadUInt64(CharacterControlStateFieldId field)
        {
            RequireKind(field, CharacterControlStateValueKind.UInt64);
            return m_State.Get(field).UInt64;
        }

        public void WriteUInt64(CharacterControlStateFieldId field, ulong value)
        {
            RequireKind(field, CharacterControlStateValueKind.UInt64);
            m_State.Set(field, CharacterControlStateValue.FromUInt64(value));
        }

        void RequireKind(CharacterControlStateFieldId field, CharacterControlStateValueKind expected)
        {
            if (m_Schema.RequireKind(field) != expected)
                throw new InvalidOperationException($"Control state field '{field}' is not '{expected}'.");
        }
    }
}
