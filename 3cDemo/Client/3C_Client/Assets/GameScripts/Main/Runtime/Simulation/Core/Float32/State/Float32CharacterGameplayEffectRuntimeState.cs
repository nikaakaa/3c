using System;

namespace ThirdPersonSimulation
{
    internal sealed class Float32CharacterGameplayEffectRuntimeState : IFloat32GameplayEffectStatePort
    {
        Float32GameplayEffectRuntimeCatalog m_Catalog;
        int m_TickRate;
        GameplayEffectStateAggregate m_Aggregate;
        SimulationGameplayEffectState m_Working;
        Float32GameplayEffectExecutionScratch m_Scratch;
        bool m_Disposed;

        public Float32CharacterGameplayEffectRuntimeState()
        {
        }

        public Float32CharacterGameplayEffectRuntimeState Restart(
            int tickRate,
            Float32GameplayEffectRuntimeCatalog catalog,
            GameplayEffectStateAggregate aggregate)
        {
            m_TickRate = tickRate;
            m_Catalog = catalog;
            m_Aggregate = aggregate;
            if (m_Working != null)
            {
                if (m_Aggregate == null)
                {
                    m_Working = null;
                    m_Scratch = null;
                }
                else
                {
                    m_Working.Restore(m_Aggregate);
                }
            }
            m_Disposed = false;
            return this;
        }

        public int TickRate => m_TickRate;

        public SimulationGameplayEffectState GetGameplayEffectState(Float32GameplayEffectExecutionScratch scratch)
        {
            RequireActive();
            if (scratch == null)
                throw new ArgumentNullException(nameof(scratch));
            if (m_Working != null)
            {
                if (!ReferenceEquals(m_Scratch, scratch))
                    throw new InvalidOperationException("Gameplay Effect state is bound to another Actor workspace.");
                return m_Working;
            }
            if (m_Catalog == null || m_Aggregate == null)
                throw new InvalidOperationException("Character does not install Gameplay Effect state.");
            m_Scratch = scratch;
            m_Working = new SimulationGameplayEffectState(
                m_Catalog,
                m_Aggregate,
                scratch);
            return m_Working;
        }

        public GameplayEffectStateAggregate GetGameplayEffectAggregate()
        {
            RequireActive();
            return m_Working?.Freeze() ?? m_Aggregate ??
                throw new InvalidOperationException("Character does not install Gameplay Effect state.");
        }

        internal GameplayEffectStateAggregate Capture()
        {
            RequireActive();
            return m_Working?.Freeze() ?? m_Aggregate;
        }

        internal void Restore(GameplayEffectStateAggregate aggregate)
        {
            RequireActive();
            m_Aggregate = aggregate;
            if (m_Aggregate == null)
            {
                m_Working = null;
                return;
            }
            if (m_Working == null)
            {
                return;
            }
            m_Working.Restore(m_Aggregate);
        }

        internal void Dispose()
        {
            m_Disposed = true;
        }

        void RequireActive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(Float32CharacterGameplayEffectRuntimeState));
        }
    }
}
