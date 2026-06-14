using System.Collections.Generic;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;

namespace ThirdPersonAction
{
    public readonly struct FullBodyActionRequestGateInput
    {
        public FullBodyActionRequestGateInput(
            InputRequestBuffer inputBuffer,
            int currentStep,
            CharacterStateMachineSnapshot snapshot,
            BasicLocomotionInputSnapshot locomotionInput,
            bool runLatchActive,
            LocomotionDecisionFacts locomotionFacts,
            StateTimelineWindowFacts turnBackTimelineFacts,
            StateTimelineWindowFacts dodgeTimelineFacts,
            DodgeActionConfig dodgeConfig,
            int currentActionResistance,
            IReadOnlyList<ActionInterruptPolicy> interruptPolicies)
        {
            InputBuffer = inputBuffer;
            CurrentStep = currentStep < 0 ? 0 : currentStep;
            Snapshot = snapshot;
            LocomotionInput = locomotionInput;
            RunLatchActive = runLatchActive;
            LocomotionFacts = locomotionFacts;
            TurnBackTimelineFacts = turnBackTimelineFacts;
            DodgeTimelineFacts = dodgeTimelineFacts;
            DodgeConfig = dodgeConfig;
            CurrentActionResistance = currentActionResistance < 0 ? 0 : currentActionResistance;
            InterruptPolicies = interruptPolicies;
        }

        public InputRequestBuffer InputBuffer { get; }
        public int CurrentStep { get; }
        public CharacterStateMachineSnapshot Snapshot { get; }
        public BasicLocomotionInputSnapshot LocomotionInput { get; }
        public bool RunLatchActive { get; }
        public LocomotionDecisionFacts LocomotionFacts { get; }
        public StateTimelineWindowFacts TurnBackTimelineFacts { get; }
        public StateTimelineWindowFacts DodgeTimelineFacts { get; }
        public DodgeActionConfig DodgeConfig { get; }
        public int CurrentActionResistance { get; }
        public IReadOnlyList<ActionInterruptPolicy> InterruptPolicies { get; }
    }

    public readonly struct FullBodyActionRequestGateResult
    {
        public FullBodyActionRequestGateResult(
            CharacterInputRequestFact request,
            ActionInterruptDecision decision)
        {
            Request = request;
            Decision = decision;
        }

        public CharacterInputRequestFact Request { get; }
        public ActionInterruptDecision Decision { get; }
        public bool Accepted => Request.HasRequest && Decision.Accepted;
    }

    public static class FullBodyActionRequestGate
    {
        public static FullBodyActionRequestGateResult Evaluate(in FullBodyActionRequestGateInput input)
        {
            CharacterStateMachineSnapshot snapshot = input.Snapshot;
            LocomotionDecisionFacts locomotionFacts = input.LocomotionFacts;
            BasicLocomotionInputSnapshot locomotionInput = input.LocomotionInput;
            DodgeActionConfig dodgeConfig = input.DodgeConfig;
            CharacterInputRequestFact turnBackRequest = FullBodyActionInterruptGate.BuildTurnBackRequestFact(
                input.CurrentStep,
                in snapshot,
                in locomotionFacts,
                input.TurnBackTimelineFacts,
                input.InterruptPolicies,
                out ActionInterruptDecision turnBackDecision);
            CharacterInputRequestFact dodgeRequest = FullBodyActionInterruptGate.BuildDodgeRequestFact(
                input.InputBuffer,
                input.CurrentStep,
                in snapshot,
                in locomotionInput,
                input.RunLatchActive,
                in locomotionFacts,
                in dodgeConfig,
                input.CurrentActionResistance,
                input.InterruptPolicies,
                input.DodgeTimelineFacts,
                out ActionInterruptDecision dodgeDecision);
            CharacterInputRequestFact request = FullBodyActionInterruptGate.SelectHighestPriorityAcceptedRequest(
                in turnBackRequest,
                in dodgeRequest);
            ActionInterruptDecision decision = request.RequestKind == InputRequestKind.TurnBack
                ? turnBackDecision
                : dodgeDecision;

            return new FullBodyActionRequestGateResult(request, decision);
        }
    }
}
