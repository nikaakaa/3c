using ThirdPersonSimulation;
using System;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedHandleAllocator : FixedOperationModule
    {
        readonly FixedAbilityExecutionFrame m_Frame;

        public FixedHandleAllocator(FixedGameplayAbilityExecutionAccess access, FixedAbilityExecutionFrame frame)
            : base(access)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
        }

        public ulong Next() => m_Frame.DomainState.NextHandleAllocator();

        public ulong Capture() => m_Frame.DomainState.CaptureHandleAllocator();

        public void Restore(ulong value) => m_Frame.DomainState.RestoreHandleAllocator(value);
    }
}

