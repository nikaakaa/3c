using System;

namespace ThirdPersonSimulation
{
    internal sealed class Float32CharacterEquipmentRuntimeState : IFloat32EquipmentStatePort
    {
        EquipmentStateAggregate m_State;
        bool m_Disposed;

        public Float32CharacterEquipmentRuntimeState(EquipmentStateAggregate state)
        {
            m_State = state;
        }

        public EquipmentStateAggregate GetEquipmentState()
        {
            RequireActive();
            return m_State ?? throw new InvalidOperationException("Character does not install Equipment state.");
        }

        public void SetEquipmentState(EquipmentStateAggregate state)
        {
            RequireActive();
            m_State = state ?? throw new ArgumentNullException(nameof(state));
        }

        internal EquipmentStateAggregate Capture()
        {
            RequireActive();
            return m_State;
        }

        internal void Restore(EquipmentStateAggregate state)
        {
            RequireActive();
            m_State = state;
        }

        internal void Dispose()
        {
            m_Disposed = true;
        }

        void RequireActive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(Float32CharacterEquipmentRuntimeState));
        }
    }
}
