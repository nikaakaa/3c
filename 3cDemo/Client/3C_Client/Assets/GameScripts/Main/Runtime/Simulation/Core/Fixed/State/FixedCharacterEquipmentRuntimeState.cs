using System;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedCharacterEquipmentRuntimeState : IFixedEquipmentStatePort
    {
        EquipmentStateAggregate m_State;
        bool m_Disposed;

        public FixedCharacterEquipmentRuntimeState()
        {
        }

        public FixedCharacterEquipmentRuntimeState Restart(EquipmentStateAggregate state)
        {
            m_State = state;
            m_Disposed = false;
            return this;
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
                throw new ObjectDisposedException(nameof(FixedCharacterEquipmentRuntimeState));
        }
    }
}
