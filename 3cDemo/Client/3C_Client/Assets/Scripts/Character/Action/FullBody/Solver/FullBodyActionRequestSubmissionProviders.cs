using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAction
{
    public readonly struct CharacterActionRequestSubmissionCandidate
    {
        public CharacterActionRequestSubmissionCandidate(
            CharacterFrameRequestProviderId providerId,
            CharacterInputRequestFact requestFact,
            ActionInterruptRequest interruptRequest,
            ActionInterruptContext interruptContext,
            int sourceOrder)
        {
            ProviderId = providerId;
            RequestFact = requestFact;
            InterruptRequest = interruptRequest;
            InterruptContext = interruptContext;
            SourceOrder = Mathf.Max(0, sourceOrder);
        }

        public CharacterFrameRequestProviderId ProviderId { get; }
        public CharacterInputRequestFact RequestFact { get; }
        public ActionInterruptRequest InterruptRequest { get; }
        public ActionInterruptContext InterruptContext { get; }
        public int SourceOrder { get; }
        public bool HasCandidate => RequestFact.HasRequest;
    }

    public interface ICharacterFrameRequestSubmissionProvider
    {
        bool TryBuild(in CharacterActionRequestSubmissionInput input, int sourceOrder, out CharacterActionRequestSubmissionCandidate candidate);
    }

    public static class FullBodyActionRequestSubmissionProviderCollection
    {
        static readonly ICharacterFrameRequestSubmissionProvider[] defaultProviders =
        {
            new ExternalActionRequestSubmissionProvider(),
            new TurnBackActionRequestSubmissionProvider(),
            new DodgeActionRequestSubmissionProvider()
        };

        public static ICharacterFrameRequestSubmissionProvider[] Default => defaultProviders;
    }

    sealed class ExternalActionRequestSubmissionProvider : ICharacterFrameRequestSubmissionProvider
    {
        public bool TryBuild(
            in CharacterActionRequestSubmissionInput input,
            int sourceOrder,
            out CharacterActionRequestSubmissionCandidate candidate)
        {
            candidate = default;
            CharacterFrameExternalRequestSubmission external = input.ExternalRequestSubmission;
            if (!external.HasSubmission)
                return false;

            CharacterFrameRequestSubmission submission = external.Submission;
            candidate = new CharacterActionRequestSubmissionCandidate(
                CharacterFrameRequestProviderId.External,
                submission.RequestFact,
                submission.InterruptRequest,
                submission.InterruptContext,
                sourceOrder);
            return true;
        }
    }

    sealed class DodgeActionRequestSubmissionProvider : ICharacterFrameRequestSubmissionProvider
    {
        public bool TryBuild(
            in CharacterActionRequestSubmissionInput input,
            int sourceOrder,
            out CharacterActionRequestSubmissionCandidate candidate)
        {
            candidate = default;
            if (!input.HasDodgeConfig)
                return false;

            DodgeActionConfig config = input.DodgeConfig;
            BasicLocomotionInputSnapshot locomotionInput = input.LocomotionInput;
            LocomotionDecisionFacts locomotionFacts = input.LocomotionFacts;
            if (!FullBodyActionInputRequestBuilder.TryBuildDodgeRequest(
                    input.InputBuffer,
                    input.CurrentStep,
                    in locomotionInput,
                    input.RunLatchActive,
                    in locomotionFacts,
                    in config,
                    out DodgeActionRequest request))
            {
                return false;
            }

            CharacterStateMachineSnapshot snapshot = input.Snapshot;
            ActionInterruptContext context = FullBodyActionInterruptRequestFactory.CreateContext(
                in snapshot,
                input.CurrentStep,
                input.CurrentActionResistance,
                input.CurrentTimelineFacts);
            candidate = new CharacterActionRequestSubmissionCandidate(
                CharacterFrameRequestProviderId.Dodge,
                FullBodyActionInputRequestBuilder.ToInputRequestFact(in request),
                request.ToInterruptRequest(),
                context,
                sourceOrder);
            return true;
        }
    }

    sealed class TurnBackActionRequestSubmissionProvider : ICharacterFrameRequestSubmissionProvider
    {
        const int Priority = 20;

        public bool TryBuild(
            in CharacterActionRequestSubmissionInput input,
            int sourceOrder,
            out CharacterActionRequestSubmissionCandidate candidate)
        {
            candidate = default;
            LocomotionDecisionFacts locomotionFacts = input.LocomotionFacts;
            LocomotionTurnBackIntent intent = locomotionFacts.TurnBackIntent;
            if (!intent.IsValidAt(input.CurrentStep) ||
                !intent.HasWorldMoveDirection ||
                locomotionFacts.GaitCandidate != BasicMovementGait.Run)
            {
                return false;
            }

            CharacterStateMachineSnapshot snapshot = input.Snapshot;
            StateTimelineWindowFacts timelineFacts = input.CurrentTimelineFacts;
            ActionInterruptContext context = new ActionInterruptContext(
                new ActionStateId(snapshot.ActivePath),
                snapshot.StateTime,
                timelineFacts.Resistance,
                input.CurrentStep,
                timelineFacts);
            ActionInterruptRequest interruptRequest = new ActionInterruptRequest(
                input.CurrentStep,
                ActionRequestType.Locomotion,
                new ActionStateId(CharacterStateIds.TurnBack.Value),
                Priority,
                sourceOrder,
                intent.OriginStep,
                intent.ExpireStep);
            CharacterInputRequestFact requestFact = new CharacterInputRequestFact(
                true,
                InputRequestKind.TurnBack,
                intent.OriginStep,
                intent.ExpireStep,
                Priority,
                CharacterStateVariant.None,
                intent.WorldMoveDirection);
            candidate = new CharacterActionRequestSubmissionCandidate(
                CharacterFrameRequestProviderId.TurnBack,
                requestFact,
                interruptRequest,
                context,
                sourceOrder);
            return true;
        }
    }
}
