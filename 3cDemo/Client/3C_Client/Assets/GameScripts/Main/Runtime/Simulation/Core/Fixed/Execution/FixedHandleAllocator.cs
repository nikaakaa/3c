using ThirdPersonSimulation;
using System;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedHandleAllocator : FixedOperationModule
    {
        readonly FixedEvaluationFrame m_Frame;

        public FixedHandleAllocator(FixedProgramAccess access, FixedEvaluationFrame frame)
            : base(access)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
        }

        public ulong Next() => m_Frame.Transaction.NextHandleAllocator();

        public ulong Capture() => m_Frame.Transaction.CaptureHandleAllocator();

        public void Restore(ulong value) => m_Frame.Transaction.RestoreHandleAllocator(value);
    }
}

