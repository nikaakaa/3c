namespace ThirdPersonMovement
{
    public sealed class BasicLocomotionPipeline
    {
        public BasicLocomotionFrame Tick(
            in BasicLocomotionInputSnapshot input,
            in BasicMovementSettings settings,
            in LocomotionDecisionFacts decisionFacts,
            BasicMovementPhase phase)
        {
            return Tick(in input, in settings, in decisionFacts, phase, BasicMovementMotionFacts.None(phase));
        }

        public BasicLocomotionFrame Tick(
            in BasicLocomotionInputSnapshot input,
            in BasicMovementSettings settings,
            in LocomotionDecisionFacts decisionFacts,
            BasicMovementPhase phase,
            BasicMovementMotionFacts motionFacts)
        {
            MovementInputIntent intent = decisionFacts.MoveIntent;
            UnityEngine.Vector3 worldDirection = decisionFacts.SpatialFacts.WorldMoveDirection;
            MovementCommand command = MovementCommandBuilder.Build(worldDirection, intent, phase, input.DeltaTime, settings);

            return new BasicLocomotionFrame(input, settings, intent, worldDirection, phase, command);
        }

        public BasicLocomotionFrame Tick(
            in BasicLocomotionInputSnapshot input,
            in BasicMovementSettings settings,
            in LocomotionDecisionFacts decisionFacts,
            BasicMovementPhase phase,
            BasicMovementMotionFacts motionFacts,
            BasicMovementGait frameGait)
        {
            MovementInputIntent intent = decisionFacts.MoveIntent;
            UnityEngine.Vector3 worldDirection = decisionFacts.SpatialFacts.WorldMoveDirection;
            BasicMovementGait commandGait = intent.HasMoveIntent ? intent.Gait : frameGait;
            MovementCommand command = MovementCommandBuilder.Build(worldDirection, intent, phase, input.DeltaTime, settings, motionFacts, commandGait);

            return new BasicLocomotionFrame(input, settings, intent, worldDirection, phase, command);
        }
    }
}
