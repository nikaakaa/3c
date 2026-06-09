namespace ThirdPersonAnimation
{
    public readonly struct LocomotionAnimationTiming
    {
        public LocomotionAnimationTiming(float exitDuration)
        {
            ExitDuration = exitDuration < 0f ? 0f : exitDuration;
        }

        public float ExitDuration { get; }
    }
}
