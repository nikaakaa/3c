namespace ThirdPersonMovement
{
    public readonly struct LocomotionDecisionFacts
    {
        public LocomotionDecisionFacts(
            MovementInputIntent moveIntent,
            BasicMovementGait gaitCandidate,
            BasicMovementPhaseFacts phaseFacts,
            LocomotionSpatialFacts spatialFacts,
            LocomotionTurnBackIntent turnBackIntent)
        {
            MoveIntent = moveIntent;
            GaitCandidate = gaitCandidate;
            PhaseFacts = phaseFacts;
            SpatialFacts = spatialFacts;
            TurnBackIntent = turnBackIntent;
        }

        public MovementInputIntent MoveIntent { get; }
        public BasicMovementGait GaitCandidate { get; }
        public BasicMovementPhaseFacts PhaseFacts { get; }
        public LocomotionSpatialFacts SpatialFacts { get; }
        public LocomotionTurnBackIntent TurnBackIntent { get; }
        public bool HasMoveIntent => MoveIntent.HasMoveIntent;

        public static LocomotionDecisionFacts Empty => new LocomotionDecisionFacts(
            default,
            BasicMovementGait.Walk,
            BasicMovementPhaseFacts.None,
            LocomotionSpatialFacts.Empty,
            LocomotionTurnBackIntent.None);
    }
}
