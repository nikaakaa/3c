using System;

namespace ThirdPersonSimulation
{
    internal sealed class Float32ControlStateAccess : IFloat32ActivationReader
    {
        readonly Float32ProgramAccess m_Access;
        readonly Float32StatePort m_State;

        public Float32ControlStateAccess(Float32ProgramAccess access, Float32StatePort state)
        {
            m_Access = access ?? throw new ArgumentNullException(nameof(access));
            m_State = state ?? throw new ArgumentNullException(nameof(state));
        }

        public int ReadInt32(int slot) => m_State.Get(slot).Int32;
        public void WriteInt32(int slot, int value) => m_State.Set(slot, CharacterStateValue.FromInt32(value));
        public ulong ReadUInt64(int slot) => m_State.Get(slot).UInt64;
        public void WriteUInt64(int slot, ulong value) => m_State.Set(slot, CharacterStateValue.FromUInt64(value));
        public string ReadIdentity(int slot) => m_State.Get(slot).Identity;
        public void WriteIdentity(int slot, string value) => m_State.Set(slot, CharacterStateValue.FromIdentity(value));

        public ulong ReadGeneration(OperationHandle operation)
        {
            int slot = m_Access.FindOperationSlot(operation, ProgramStateSemantic.RunnableActivationGeneration);
            return slot < 0 ? 1UL : m_State.Get(slot).UInt64;
        }
    }
}
