using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;

namespace ThirdPersonMovement
{
    public interface ILocomotionFrameRuntimePort
    {
        bool RunLatchActive { get; }
        CharacterRuntimeBlackboardSnapshot RuntimeBlackboardSnapshot { get; }

        bool TryPrepareDecisionFrame(
            in BasicLocomotionInputSnapshot input,
            CharacterStateMachineRunner runner,
            int currentStep,
            out LocomotionDecisionFrame decisionFrame);
        bool TryEvaluatePreparedGameplayDecision(
            in LocomotionDecisionFrame decisionFrame,
            CharacterStateMachineRunner runner,
            in CharacterInputRequestFact inputRequest,
            StateTimelineWindowFacts currentTimelineFacts,
            int currentStep,
            out LocomotionStateDecisionFrame stateDecision);
        bool TryBuildMotionFromStateDecision(
            in LocomotionStateDecisionFrame stateDecision,
            int currentStep,
            out BasicLocomotionFrame frame,
            out CharacterStateMachineFrame stateFrame);
    }
}
