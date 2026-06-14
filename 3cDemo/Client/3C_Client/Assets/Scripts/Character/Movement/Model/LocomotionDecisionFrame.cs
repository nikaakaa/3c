namespace ThirdPersonMovement
{
    public readonly struct LocomotionDecisionFrame
    {
        public LocomotionDecisionFrame(
            BasicLocomotionInputSnapshot input,
            BasicMovementSettings settings,
            MovementInputIntent intent,
            LocomotionDecisionFacts facts,
            BasicMovementGait frameGait)
        {
            Input = input;
            Settings = settings;
            Intent = intent;
            Facts = facts;
            FrameGait = frameGait;
        }

        public BasicLocomotionInputSnapshot Input { get; }
        public BasicMovementSettings Settings { get; }
        public MovementInputIntent Intent { get; }
        public LocomotionDecisionFacts Facts { get; }
        public BasicMovementGait FrameGait { get; }
        public BasicMovementPhaseFacts PhaseFacts => Facts.PhaseFacts;
        public LocomotionSpatialFacts SpatialFacts => Facts.SpatialFacts;
    }
}
