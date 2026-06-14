using System.Collections.Generic;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;

namespace ThirdPersonAction
{
    public readonly struct FullBodyPipelineActionRequestResolverInput
    {
        public FullBodyPipelineActionRequestResolverInput(
            InputRequestBuffer inputBuffer,
            int step,
            float deltaTime,
            CharacterStateMachineRunner stateMachine,
            CharacterStateMachineSnapshot currentSnapshot,
            BasicLocomotionInputSnapshot locomotionInput,
            bool runLatchActive,
            LocomotionDecisionFacts locomotionFacts,
            CharacterRuntimeBlackboardSnapshot runtimeBlackboard,
            bool hasDodgeConfig,
            DodgeActionConfig dodgeConfig,
            int currentActionResistance,
            IReadOnlyList<ActionInterruptPolicy> interruptPolicies)
        {
            InputBuffer = inputBuffer;
            Step = step < 0 ? 0 : step;
            DeltaTime = deltaTime < 0f ? 0f : deltaTime;
            StateMachine = stateMachine;
            CurrentSnapshot = currentSnapshot;
            LocomotionInput = locomotionInput;
            RunLatchActive = runLatchActive;
            LocomotionFacts = locomotionFacts;
            RuntimeBlackboard = runtimeBlackboard;
            HasDodgeConfig = hasDodgeConfig;
            DodgeConfig = dodgeConfig;
            CurrentActionResistance = currentActionResistance < 0 ? 0 : currentActionResistance;
            InterruptPolicies = interruptPolicies;
        }

        public InputRequestBuffer InputBuffer { get; }
        public int Step { get; }
        public float DeltaTime { get; }
        public CharacterStateMachineRunner StateMachine { get; }
        public CharacterStateMachineSnapshot CurrentSnapshot { get; }
        public BasicLocomotionInputSnapshot LocomotionInput { get; }
        public bool RunLatchActive { get; }
        public LocomotionDecisionFacts LocomotionFacts { get; }
        public CharacterRuntimeBlackboardSnapshot RuntimeBlackboard { get; }
        public bool HasDodgeConfig { get; }
        public DodgeActionConfig DodgeConfig { get; }
        public int CurrentActionResistance { get; }
        public IReadOnlyList<ActionInterruptPolicy> InterruptPolicies { get; }
    }

        public static class FullBodyPipelineActionRequestResolver
    {
        public static FullBodyActionRequestGateResult Resolve(in FullBodyPipelineActionRequestResolverInput input)
        {
            LocomotionDecisionFacts locomotionFacts = input.LocomotionFacts;
            CharacterRuntimeBlackboardSnapshot runtimeBlackboard = input.RuntimeBlackboard;
            CharacterInputRequestFact emptyRequest = CharacterInputRequestFact.None(InputRequestKind.TurnBack);
            CharacterStateMachineContext arbitrationContext = new CharacterStateMachineContext(
                input.DeltaTime,
                input.Step,
                in locomotionFacts,
                emptyRequest,
                runtimeBlackboard);
            StateTimelineWindowFacts turnBackTimelineFacts = input.StateMachine != null
                ? input.StateMachine.SampleCurrentTimelineFacts(
                    in arbitrationContext,
                    input.CurrentSnapshot.StateTime,
                    ActionRequestType.Locomotion)
                : default;
            StateTimelineWindowFacts dodgeTimelineFacts = input.StateMachine != null
                ? input.StateMachine.SampleCurrentTimelineFacts(
                    in arbitrationContext,
                    input.CurrentSnapshot.StateTime,
                    ActionRequestType.Dodge)
                : default;
            FullBodyActionRequestGateInput gateInput = new FullBodyActionRequestGateInput(
                input.InputBuffer,
                input.Step,
                input.CurrentSnapshot,
                input.LocomotionInput,
                input.RunLatchActive,
                locomotionFacts,
                turnBackTimelineFacts,
                dodgeTimelineFacts,
                input.HasDodgeConfig,
                input.DodgeConfig,
                input.CurrentActionResistance,
                input.InterruptPolicies);

            return FullBodyActionRequestGate.Evaluate(in gateInput);
        }
    }
}
