using System.Collections.Generic;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;

namespace ThirdPersonAction
{
    public static class CommittedActionInterruptRequestFactory
    {
        const int TurnBackPriority = 20;

        public static CharacterInputRequestFact BuildDodgeRequestFact(
            InputRequestBuffer inputBuffer,
            int currentStep,
            in CharacterStateMachineSnapshot snapshot,
            in BasicLocomotionInputSnapshot input,
            bool runLatchActive,
            in LocomotionDecisionFacts locomotionFacts,
            in DodgeActionTuning tuning,
            int currentStateResistance,
            IReadOnlyList<ActionInterruptPolicy> policies,
            StateTimelineWindowFacts timelineFacts,
            out ActionInterruptDecision decision)
        {
            if (!CommittedActionInputRequestBuilder.TryBuildDodgeRequest(
                    inputBuffer,
                    currentStep,
                    in input,
                    runLatchActive,
                    in locomotionFacts,
                    in tuning,
                    out DodgeActionRequest request))
            {
                decision = ActionInterruptDecision.Reject(ActionInterruptRejectReason.NoRequest);
                return CharacterInputRequestFact.None(InputRequestKind.Dodge);
            }

            ActionInterruptContext context = CreateContext(in snapshot, currentStep, currentStateResistance, timelineFacts);
            ActionInterruptRequest[] requests = { request.ToInterruptRequest() };
            decision = ActionInterruptArbiter.Arbitrate(context, requests, policies);

            return decision.Accepted
                ? CommittedActionInputRequestBuilder.ToInputRequestFact(in request)
                : CharacterInputRequestFact.None(InputRequestKind.Dodge);
        }

        public static CharacterInputRequestFact BuildDodgeRequestFact(
            InputRequestBuffer inputBuffer,
            int currentStep,
            in CharacterStateMachineSnapshot snapshot,
            in BasicLocomotionInputSnapshot input,
            bool runLatchActive,
            in LocomotionDecisionFacts locomotionFacts,
            in DodgeActionTuning tuning,
            int currentStateResistance,
            IReadOnlyList<ActionInterruptPolicy> policies,
            out ActionInterruptDecision decision)
        {
            return BuildDodgeRequestFact(
                inputBuffer,
                currentStep,
                in snapshot,
                in input,
                runLatchActive,
                in locomotionFacts,
                in tuning,
                currentStateResistance,
                policies,
                default,
                out decision);
        }

        public static ActionInterruptContext CreateContext(
            in CharacterStateMachineSnapshot snapshot,
            int currentStep,
            int currentStateResistance = 0,
            StateTimelineWindowFacts timelineFacts = default)
        {
            CharacterStateDomainView stateView = CharacterStateDomainView.FromSnapshot(in snapshot);
            ActionStateId state = stateView.IsAction ? stateView.ActionState : ActionStateIds.None;
            return new ActionInterruptContext(state, snapshot.StateTime, currentStateResistance, currentStep, timelineFacts);
        }

        public static bool HasDodgePolicy(IReadOnlyList<ActionInterruptPolicy> policies, in DodgeActionTuning tuning)
        {
            if (policies == null)
                return false;

            ActionInterruptRequest request = new ActionInterruptRequest(
                0,
                ActionRequestType.Dodge,
                ActionStateIds.Dodge,
                tuning.Priority,
                0,
                0,
                0);

            if (!HasPolicyForState(policies, request, ActionStateIds.None, tuning))
                return false;

            if (!HasPolicyForState(policies, request, ActionStateIds.Dodge, tuning))
                return false;

            return true;
        }

        public static CharacterInputRequestFact BuildTurnBackRequestFact(
            int currentStep,
            in CharacterStateMachineSnapshot snapshot,
            in LocomotionDecisionFacts locomotionFacts,
            in StateTimelineWindowFacts timelineFacts,
            IReadOnlyList<ActionInterruptPolicy> policies,
            out ActionInterruptDecision decision)
        {
            LocomotionTurnBackIntent intent = locomotionFacts.TurnBackIntent;
            if (!intent.IsValidAt(currentStep) ||
                !intent.HasWorldMoveDirection ||
                locomotionFacts.GaitCandidate != BasicMovementGait.Run)
            {
                decision = ActionInterruptDecision.Reject(ActionInterruptRejectReason.NoRequest);
                return CharacterInputRequestFact.None(InputRequestKind.TurnBack);
            }

            ActionInterruptContext context = new ActionInterruptContext(
                new ActionStateId(snapshot.ActivePath),
                snapshot.StateTime,
                timelineFacts.Resistance,
                currentStep,
                timelineFacts);
            ActionInterruptRequest request = new ActionInterruptRequest(
                currentStep,
                ActionRequestType.Locomotion,
                new ActionStateId(CharacterStateIds.TurnBack.Value),
                TurnBackPriority,
                0,
                intent.OriginStep,
                intent.ExpireStep);
            decision = ActionInterruptArbiter.Arbitrate(context, new[] { request }, policies);

            return decision.Accepted
                ? new CharacterInputRequestFact(
                    true,
                    InputRequestKind.TurnBack,
                    intent.OriginStep,
                    intent.ExpireStep,
                    TurnBackPriority,
                    CharacterStateVariant.None,
                    intent.WorldMoveDirection)
                : CharacterInputRequestFact.None(InputRequestKind.TurnBack);
        }

        public static CharacterInputRequestFact SelectHighestPriorityAcceptedRequest(
            in CharacterInputRequestFact first,
            in CharacterInputRequestFact second)
        {
            if (!first.HasRequest)
                return second;
            if (!second.HasRequest)
                return first;
            if (second.Priority > first.Priority)
                return second;

            return first;
        }

        static bool HasPolicyForState(
            IReadOnlyList<ActionInterruptPolicy> policies,
            in ActionInterruptRequest request,
            ActionStateId fromState,
            in DodgeActionTuning tuning)
        {
            ActionInterruptContext context = new ActionInterruptContext(fromState, 0f, 0, 0);

            for (int i = 0; i < policies.Count; i++)
            {
                ActionInterruptPolicy policy = policies[i];
                if (policy.Matches(context, request) &&
                    ActionInterruptPolicyValidator.IsPolicyValid(policy) &&
                    tuning.Priority >= policy.MinPriority)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
