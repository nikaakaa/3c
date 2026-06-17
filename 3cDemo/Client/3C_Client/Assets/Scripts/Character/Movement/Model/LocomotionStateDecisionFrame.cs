using ThirdPersonCharacterStateMachine;

namespace ThirdPersonMovement
{
    public readonly struct LocomotionStateDecisionFrame
    {
        public LocomotionStateDecisionFrame(
            LocomotionDecisionFrame decisionFrame,
            CharacterStateMachineFrame stateFrame,
            BasicMovementPhase phaseBeforeTick,
            BasicMovementGait frameGait,
            MovementInputIntent pendingIntent,
            BasicMovementPhaseFacts phaseFacts,
            LocomotionDecisionFacts decisionFacts,
            CharacterRuntimeBlackboardSnapshot blackboardBeforeTick,
            bool runLatchBeforeStateTick)
            : this(
                decisionFrame,
                stateFrame,
                phaseBeforeTick,
                frameGait,
                pendingIntent,
                phaseFacts,
                decisionFacts,
                blackboardBeforeTick,
                runLatchBeforeStateTick,
                false)
        {
        }

        public LocomotionStateDecisionFrame(
            LocomotionDecisionFrame decisionFrame,
            CharacterStateMachineFrame stateFrame,
            BasicMovementPhase phaseBeforeTick,
            BasicMovementGait frameGait,
            MovementInputIntent pendingIntent,
            BasicMovementPhaseFacts phaseFacts,
            LocomotionDecisionFacts decisionFacts,
            CharacterRuntimeBlackboardSnapshot blackboardBeforeTick,
            bool runLatchBeforeStateTick,
            bool consumedLocomotionPreemption)
        {
            DecisionFrame = decisionFrame;
            StateFrame = stateFrame;
            PhaseBeforeTick = phaseBeforeTick;
            FrameGait = frameGait;
            PendingIntent = pendingIntent;
            PhaseFacts = phaseFacts;
            DecisionFacts = decisionFacts;
            BlackboardBeforeTick = blackboardBeforeTick;
            RunLatchBeforeStateTick = runLatchBeforeStateTick;
            ConsumedLocomotionPreemption = consumedLocomotionPreemption;
            HasStateFrame = true;
        }

        public LocomotionDecisionFrame DecisionFrame { get; }
        public CharacterStateMachineFrame StateFrame { get; }
        public BasicMovementPhase PhaseBeforeTick { get; }
        public BasicMovementGait FrameGait { get; }
        public MovementInputIntent PendingIntent { get; }
        public BasicMovementPhaseFacts PhaseFacts { get; }
        public LocomotionDecisionFacts DecisionFacts { get; }
        public CharacterRuntimeBlackboardSnapshot BlackboardBeforeTick { get; }
        public bool RunLatchBeforeStateTick { get; }
        public bool ConsumedLocomotionPreemption { get; }
        public bool HasStateFrame { get; }
    }
}
