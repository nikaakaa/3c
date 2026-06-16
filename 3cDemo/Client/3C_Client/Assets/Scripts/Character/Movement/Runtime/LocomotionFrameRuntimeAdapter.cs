using ThirdPersonCharacterStateMachine;

namespace ThirdPersonMovement
{
    internal sealed class LocomotionFrameRuntimeAdapter : ILocomotionFrameRuntimePort
    {
        readonly LocomotionFrameRuntime runtime;
        readonly LocomotionRuntimeStateStore stateStore;
        readonly ILocomotionFrameRuntimeOutputHost host;

        public LocomotionFrameRuntimeAdapter(
            LocomotionFrameRuntime runtime,
            LocomotionRuntimeStateStore stateStore,
            ILocomotionFrameRuntimeOutputHost host)
        {
            this.runtime = runtime;
            this.stateStore = stateStore;
            this.host = host;
        }

        public bool RunLatchActive => stateStore.RunLatchActive;
        public CharacterRuntimeBlackboardSnapshot RuntimeBlackboardSnapshot => host.RuntimeBlackboardSnapshot;

        public bool TryPrepareDecisionFrame(
            in BasicLocomotionInputSnapshot input,
            CharacterStateMachineRunner runner,
            int currentStep,
            out LocomotionDecisionFrame decisionFrame)
        {
            return runtime.TryPrepareDecisionFrame(
                in input,
                runner,
                currentStep,
                out decisionFrame);
        }

        public bool TryEvaluatePreparedGameplayDecision(
            in LocomotionDecisionFrame decisionFrame,
            CharacterStateMachineRunner runner,
            in CharacterInputRequestFact inputRequest,
            StateTimelineWindowFacts currentTimelineFacts,
            int currentStep,
            out LocomotionStateDecisionFrame stateDecision)
        {
            return runtime.TryEvaluatePreparedGameplayDecision(
                in decisionFrame,
                runner,
                in inputRequest,
                currentTimelineFacts,
                currentStep,
                out stateDecision);
        }

        public bool TryEvaluatePreparedGameplayDecision(
            in LocomotionDecisionFrame decisionFrame,
            CharacterStateMachineRunner runner,
            in CharacterInputRequestFact inputRequest,
            int currentStep,
            out LocomotionStateDecisionFrame stateDecision)
        {
            return runtime.TryEvaluatePreparedGameplayDecision(
                in decisionFrame,
                runner,
                in inputRequest,
                currentStep,
                out stateDecision);
        }

        public bool TryBuildMotionFromStateDecision(
            in LocomotionStateDecisionFrame stateDecision,
            int currentStep,
            out BasicLocomotionFrame frame,
            out CharacterStateMachineFrame stateFrame)
        {
            return runtime.TryBuildMotionFromStateDecision(
                in stateDecision,
                currentStep,
                out frame,
                out stateFrame);
        }
    }
}
