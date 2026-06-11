namespace ThirdPersonAnimation
{
    public readonly struct AnimationPhaseTimelineFacts
    {
        public AnimationPhaseTimelineFacts(bool canExit)
        {
            CanExit = canExit;
        }

        public bool CanExit { get; }

        public static AnimationPhaseTimelineFacts None => new AnimationPhaseTimelineFacts(false);
    }
}
