using System.Collections.Generic;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;

namespace ThirdPersonAction
{
    public readonly struct FullBodyActionRequestSubmissionResolverInput
    {
        public FullBodyActionRequestSubmissionResolverInput(
            InputRequestBuffer inputBuffer,
            int step,
            float deltaTime,
            CharacterStateMachineSnapshot currentSnapshot,
            BasicLocomotionInputSnapshot locomotionInput,
            bool runLatchActive,
            LocomotionDecisionFacts locomotionFacts,
            StateTimelineWindowFacts currentTimelineFacts,
            bool hasActionCatalog,
            CharacterActionCatalog actionCatalog,
            int currentActionResistance,
            IReadOnlyList<ActionInterruptPolicy> interruptPolicies)
            : this(
                inputBuffer,
                step,
                deltaTime,
                currentSnapshot,
                in locomotionInput,
                runLatchActive,
                in locomotionFacts,
                currentTimelineFacts,
                hasActionCatalog,
                actionCatalog,
                currentActionResistance,
                interruptPolicies,
                CharacterFrameExternalRequestSubmission.None)
        {
        }

        public FullBodyActionRequestSubmissionResolverInput(
            InputRequestBuffer inputBuffer,
            int step,
            float deltaTime,
            CharacterStateMachineSnapshot currentSnapshot,
            in BasicLocomotionInputSnapshot locomotionInput,
            bool runLatchActive,
            in LocomotionDecisionFacts locomotionFacts,
            StateTimelineWindowFacts currentTimelineFacts,
            bool hasActionCatalog,
            CharacterActionCatalog actionCatalog,
            int currentActionResistance,
            IReadOnlyList<ActionInterruptPolicy> interruptPolicies,
            CharacterFrameExternalRequestSubmission externalRequestSubmission)
        {
            InputBuffer = inputBuffer;
            Step = step < 0 ? 0 : step;
            DeltaTime = deltaTime < 0f ? 0f : deltaTime;
            CurrentSnapshot = currentSnapshot;
            LocomotionInput = locomotionInput;
            RunLatchActive = runLatchActive;
            LocomotionFacts = locomotionFacts;
            CurrentTimelineFacts = currentTimelineFacts;
            HasActionCatalog = hasActionCatalog && actionCatalog.HasCatalog;
            ActionCatalog = HasActionCatalog ? actionCatalog : CharacterActionCatalog.Empty;
            CurrentActionResistance = currentActionResistance < 0 ? 0 : currentActionResistance;
            InterruptPolicies = interruptPolicies;
            ExternalRequestSubmission = externalRequestSubmission;
        }

        public InputRequestBuffer InputBuffer { get; }
        public int Step { get; }
        public float DeltaTime { get; }
        public CharacterStateMachineSnapshot CurrentSnapshot { get; }
        public BasicLocomotionInputSnapshot LocomotionInput { get; }
        public bool RunLatchActive { get; }
        public LocomotionDecisionFacts LocomotionFacts { get; }
        public StateTimelineWindowFacts CurrentTimelineFacts { get; }
        public bool HasActionCatalog { get; }
        public CharacterActionCatalog ActionCatalog { get; }
        public int CurrentActionResistance { get; }
        public IReadOnlyList<ActionInterruptPolicy> InterruptPolicies { get; }
        public CharacterFrameExternalRequestSubmission ExternalRequestSubmission { get; }
    }

    public static class FullBodyActionRequestSubmissionResolver
    {
        public static CharacterActionRequestSubmissionResult Resolve(in FullBodyActionRequestSubmissionResolverInput input)
        {
            LocomotionDecisionFacts locomotionFacts = input.LocomotionFacts;
            CharacterActionRequestSubmissionInput submissionInput = new CharacterActionRequestSubmissionInput(
                input.InputBuffer,
                input.Step,
                input.CurrentSnapshot,
                input.LocomotionInput,
                input.RunLatchActive,
                locomotionFacts,
                input.CurrentTimelineFacts,
                input.HasActionCatalog,
                input.ActionCatalog,
                input.CurrentActionResistance,
                input.InterruptPolicies,
                input.ExternalRequestSubmission);

            return CharacterActionRequestSubmissionArbiter.Evaluate(in submissionInput);
        }
    }
}
