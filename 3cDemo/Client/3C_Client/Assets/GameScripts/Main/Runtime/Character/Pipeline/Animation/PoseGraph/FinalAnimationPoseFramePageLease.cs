using System;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class FinalAnimationPoseFramePageLease : IAnimationReadOnlyBufferLease
    {
        ulong m_CurrentLeaseIdentity;

        internal void BeginWrite(ulong leaseIdentity)
        {
            if (leaseIdentity == 0)
                throw new ArgumentOutOfRangeException(nameof(leaseIdentity));
            if (leaseIdentity == m_CurrentLeaseIdentity)
            {
                throw new InvalidOperationException(
                    "Final Animation Pose Frame page lease identity must differ from the current identity.");
            }

            m_CurrentLeaseIdentity = leaseIdentity;
        }

        internal void Invalidate() => m_CurrentLeaseIdentity = 0;

        public void RequireValid(ulong leaseIdentity)
        {
            if (leaseIdentity == 0 || leaseIdentity != m_CurrentLeaseIdentity)
                throw new InvalidOperationException("Final Animation Pose Frame buffer lease is no longer valid.");
        }
    }
}
