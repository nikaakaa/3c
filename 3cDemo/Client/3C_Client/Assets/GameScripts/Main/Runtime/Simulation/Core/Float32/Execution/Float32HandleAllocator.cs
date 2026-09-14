using System;

namespace ThirdPersonSimulation
{
    internal sealed class Float32HandleAllocator : Float32OperationModule
    {
        readonly Float32EvaluationFrame m_Frame;

        public Float32HandleAllocator(Float32ProgramAccess access, Float32EvaluationFrame frame)
            : base(access)
        {
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
        }

        public ulong Next() => m_Frame.Transaction.NextHandleAllocator();

        public ulong Capture() => m_Frame.Transaction.CaptureHandleAllocator();

        public void Restore(ulong value) => m_Frame.Transaction.RestoreHandleAllocator(value);
    }
}
