namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal interface IAnimationReadOnlyBufferLease
    {
        void RequireValid(ulong leaseIdentity);
    }
}
