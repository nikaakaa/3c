using System.Collections.Generic;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;

namespace ThirdPersonAction
{
    public readonly struct CharacterActionRequestSubmissionInput
    {
        public CharacterActionRequestSubmissionInput(
            InputRequestBuffer inputBuffer,
            int currentStep,
            CharacterStateMachineSnapshot snapshot,
            BasicLocomotionInputSnapshot locomotionInput,
            bool runLatchActive,
            LocomotionDecisionFacts locomotionFacts,
            StateTimelineWindowFacts currentTimelineFacts,
            bool hasDodgeConfig,
            DodgeActionConfig dodgeConfig,
            int currentActionResistance,
            IReadOnlyList<ActionInterruptPolicy> interruptPolicies)
            : this(
                inputBuffer,
                currentStep,
                snapshot,
                in locomotionInput,
                runLatchActive,
                in locomotionFacts,
                currentTimelineFacts,
                hasDodgeConfig,
                dodgeConfig,
                currentActionResistance,
                interruptPolicies,
                CharacterFrameExternalRequestSubmission.None)
        {
        }

        public CharacterActionRequestSubmissionInput(
            InputRequestBuffer inputBuffer,
            int currentStep,
            CharacterStateMachineSnapshot snapshot,
            in BasicLocomotionInputSnapshot locomotionInput,
            bool runLatchActive,
            in LocomotionDecisionFacts locomotionFacts,
            StateTimelineWindowFacts currentTimelineFacts,
            bool hasDodgeConfig,
            DodgeActionConfig dodgeConfig,
            int currentActionResistance,
            IReadOnlyList<ActionInterruptPolicy> interruptPolicies,
            CharacterFrameExternalRequestSubmission externalRequestSubmission)
        {
            InputBuffer = inputBuffer;
            CurrentStep = currentStep < 0 ? 0 : currentStep;
            Snapshot = snapshot;
            LocomotionInput = locomotionInput;
            RunLatchActive = runLatchActive;
            LocomotionFacts = locomotionFacts;
            CurrentTimelineFacts = currentTimelineFacts;
            HasDodgeConfig = hasDodgeConfig;
            DodgeConfig = dodgeConfig;
            CurrentActionResistance = currentActionResistance < 0 ? 0 : currentActionResistance;
            InterruptPolicies = interruptPolicies;
            ExternalRequestSubmission = externalRequestSubmission;
        }

        public InputRequestBuffer InputBuffer { get; }
        public int CurrentStep { get; }
        public CharacterStateMachineSnapshot Snapshot { get; }
        public BasicLocomotionInputSnapshot LocomotionInput { get; }
        public bool RunLatchActive { get; }
        public LocomotionDecisionFacts LocomotionFacts { get; }
        public StateTimelineWindowFacts CurrentTimelineFacts { get; }
        public bool HasDodgeConfig { get; }
        public DodgeActionConfig DodgeConfig { get; }
        public int CurrentActionResistance { get; }
        public IReadOnlyList<ActionInterruptPolicy> InterruptPolicies { get; }
        public CharacterFrameExternalRequestSubmission ExternalRequestSubmission { get; }
    }

    public readonly struct CharacterActionRequestSubmissionResult
    {
        public CharacterActionRequestSubmissionResult(
            CharacterInputRequestFact request,
            ActionInterruptDecision decision)
            : this(request, decision, CharacterFrameRequestSubmissionSet.Empty)
        {
        }

        public CharacterActionRequestSubmissionResult(
            CharacterInputRequestFact request,
            ActionInterruptDecision decision,
            CharacterFrameRequestSubmissionSet requestSubmissions)
        {
            Request = request;
            Decision = decision;
            RequestSubmissions = requestSubmissions;
        }

        public CharacterInputRequestFact Request { get; }
        public ActionInterruptDecision Decision { get; }
        public CharacterFrameRequestSubmissionSet RequestSubmissions { get; }
        public bool Accepted => Request.HasRequest && Decision.Accepted;
    }

    public static class CharacterActionRequestSubmissionArbiter
    {
        static readonly ActionInterruptRequest[] singleRequestBuffer = new ActionInterruptRequest[1];

        public static CharacterActionRequestSubmissionResult Evaluate(in CharacterActionRequestSubmissionInput input)
        {
            ICharacterFrameRequestSubmissionProvider[] providers = FullBodyActionRequestSubmissionProviderCollection.Default;
            CharacterInputRequestFact selectedRequest = default;
            ActionInterruptDecision selectedDecision = ActionInterruptDecision.Reject(ActionInterruptRejectReason.NoRequest);
            CharacterActionRequestSubmissionCandidate selectedCandidate = default;
            CharacterFrameRequestSubmission firstSubmission = default;
            CharacterFrameRequestSubmission secondSubmission = default;
            CharacterFrameRequestSubmission thirdSubmission = default;
            CharacterFrameRequestSubmission fourthSubmission = default;
            int submissionCount = 0;
            bool hasSelected = false;

            for (int i = 0; i < providers.Length; i++)
            {
                ICharacterFrameRequestSubmissionProvider provider = providers[i];
                if (provider == null ||
                    !provider.TryBuild(in input, i, out CharacterActionRequestSubmissionCandidate candidate) ||
                    !candidate.HasCandidate)
                {
                    continue;
                }

                CharacterFrameRequestSubmission submission = new CharacterFrameRequestSubmission(
                    candidate.ProviderId,
                    candidate.RequestFact,
                    candidate.InterruptRequest,
                    candidate.InterruptContext,
                    candidate.SourceOrder);
                AddSubmission(
                    in submission,
                    ref firstSubmission,
                    ref secondSubmission,
                    ref thirdSubmission,
                    ref fourthSubmission,
                    ref submissionCount);
                singleRequestBuffer[0] = candidate.InterruptRequest;
                ActionInterruptDecision decision = ActionInterruptArbiter.Arbitrate(
                    candidate.InterruptContext,
                    singleRequestBuffer,
                    input.InterruptPolicies);
                if (!decision.Accepted)
                {
                    selectedDecision = SelectMoreUsefulReject(in selectedDecision, in decision);
                    continue;
                }

                if (!hasSelected || IsHigherPriority(in candidate, in selectedCandidate))
                {
                    selectedRequest = candidate.RequestFact;
                    selectedDecision = decision;
                    selectedCandidate = candidate;
                    hasSelected = true;
                }
            }

            CharacterFrameRequestSubmissionSet submissions = new CharacterFrameRequestSubmissionSet(
                firstSubmission,
                secondSubmission,
                thirdSubmission,
                fourthSubmission,
                submissionCount);
            return new CharacterActionRequestSubmissionResult(selectedRequest, selectedDecision, submissions);
        }

        static void AddSubmission(
            in CharacterFrameRequestSubmission submission,
            ref CharacterFrameRequestSubmission first,
            ref CharacterFrameRequestSubmission second,
            ref CharacterFrameRequestSubmission third,
            ref CharacterFrameRequestSubmission fourth,
            ref int count)
        {
            if (!submission.HasRequest || count >= 4)
                return;

            if (count == 0)
                first = submission;
            else if (count == 1)
                second = submission;
            else if (count == 2)
                third = submission;
            else
                fourth = submission;

            count++;
        }

        static bool IsHigherPriority(
            in CharacterActionRequestSubmissionCandidate candidate,
            in CharacterActionRequestSubmissionCandidate current)
        {
            ActionInterruptRequest candidateRequest = candidate.InterruptRequest;
            ActionInterruptRequest currentRequest = current.InterruptRequest;
            if (candidateRequest.Priority != currentRequest.Priority)
                return candidateRequest.Priority > currentRequest.Priority;
            if (candidateRequest.SourceOrder != currentRequest.SourceOrder)
                return candidateRequest.SourceOrder < currentRequest.SourceOrder;

            return candidate.SourceOrder < current.SourceOrder;
        }

        static ActionInterruptDecision SelectMoreUsefulReject(
            in ActionInterruptDecision current,
            in ActionInterruptDecision next)
        {
            return RejectRank(next.RejectReason) > RejectRank(current.RejectReason)
                ? next
                : current;
        }

        static int RejectRank(ActionInterruptRejectReason reason)
        {
            return reason switch
            {
                ActionInterruptRejectReason.InvalidPolicy => 7,
                ActionInterruptRejectReason.BlockedByResistance => 6,
                ActionInterruptRejectReason.PriorityTooLow => 5,
                ActionInterruptRejectReason.TimingNotSatisfied => 4,
                ActionInterruptRejectReason.NoPolicy => 3,
                ActionInterruptRejectReason.Expired => 2,
                ActionInterruptRejectReason.NoRequest => 1,
                _ => 0
            };
        }
    }
}
