using ThirdPersonSimulation;
using System;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedCharacterControlReadPort : ICharacterControlReadPort
    {
        readonly FixedInputRuntime m_Input;
        readonly FixedEvaluationFrame m_Frame;
        readonly Func<CharacterControlParameterId, FixedScalar> m_ReadParameter;

        public FixedCharacterControlReadPort(
            FixedInputRuntime input,
            FixedEvaluationFrame frame,
            Func<CharacterControlParameterId, FixedScalar> readParameter)
        {
            m_Input = input ?? throw new ArgumentNullException(nameof(input));
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            m_ReadParameter = readParameter ?? throw new ArgumentNullException(nameof(readParameter));
        }

        public bool CompareInputVector2Magnitude(
            SimulationInputValueId input,
            CharacterControlParameterId threshold,
            CharacterControlNumericComparison comparison)
        {
            FixedVector2 value = m_Input.ReadValue(input.Value, SimulationInputValueKind.Vector2).Vector2;
            return Compare(value.Magnitude, m_ReadParameter(threshold), comparison);
        }

        public bool CompareInputDirectionToBodyYaw(
            SimulationInputValueId input,
            CharacterControlParameterId threshold,
            CharacterControlNumericComparison comparison)
        {
            FixedVector2 value = m_Input.ReadValue(input.Value, SimulationInputValueKind.Vector2).Vector2;
            FixedScalar angle = value == FixedVector2.Zero
                ? FixedScalar.Zero
                : FixedScalar.Abs(FixedAngle.Delta(m_Frame.Body.Yaw, FixedAngle.FromPlanarDirection(value)));
            return Compare(angle, m_ReadParameter(threshold), comparison);
        }

        static bool Compare(
            FixedScalar value,
            FixedScalar threshold,
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

    internal sealed class FixedCharacterControlStatePort : ICharacterControlStatePort
    {
        readonly FixedStatePort m_State;
        readonly CharacterControlStateLayout m_Layout;

        public FixedCharacterControlStatePort(
            FixedStatePort state,
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

        public void WriteTransition(CharacterControlStateFieldId field, CharacterControlTransitionId value)
        {
            RequireKind(field, ProgramStateValueKind.Identity);
            m_State.Set(m_Layout.RequireSlot(field), CharacterStateValue.FromIdentity(value.Value));
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
