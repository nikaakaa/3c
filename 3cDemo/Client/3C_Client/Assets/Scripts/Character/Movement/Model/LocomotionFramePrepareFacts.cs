namespace ThirdPersonMovement
{
    public readonly struct LocomotionFramePrepareFacts
    {
        public LocomotionFramePrepareFacts(
            MovementInputIntent intent,
            BasicMovementGait frameGait)
        {
            Intent = intent;
            FrameGait = frameGait;
        }

        public MovementInputIntent Intent { get; }
        public BasicMovementGait FrameGait { get; }
    }
}
