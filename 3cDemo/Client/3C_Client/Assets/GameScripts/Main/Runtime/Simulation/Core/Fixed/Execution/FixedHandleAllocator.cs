using ThirdPersonSimulation;
using System;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedHandleAllocator : FixedOperationModule
    {
        readonly FixedStatePort m_State;
        readonly int m_Slot;

        public FixedHandleAllocator(FixedProgramAccess access, FixedStatePort state)
            : base(access)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            m_Slot = FindStateSlot(ProgramStateSemantic.HandleAllocator, null);
            if (m_Slot < 0)
                throw new InvalidOperationException("Program has no HandleAllocator state slot.");
        }

        public ulong Next()
        {
            ulong value = checked(m_State.Get(m_Slot).UInt64 + 1);
            if (value == 0)
                throw new OverflowException("Simulation handle allocator overflowed.");
            m_State.Set(m_Slot, CharacterStateValue.FromUInt64(value));
            return value;
        }

        public ulong Capture() => m_State.Get(m_Slot).UInt64;

        public void Restore(ulong value) => m_State.Set(m_Slot, CharacterStateValue.FromUInt64(value));
    }
}

