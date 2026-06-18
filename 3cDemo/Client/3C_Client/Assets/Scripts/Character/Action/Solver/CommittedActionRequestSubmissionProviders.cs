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
            Request = default;
            ResolvedAction = default;
        }

        public CharacterActionRequestSubmissionCandidate(in CharacterResolvedAction resolvedAction, int sourceOrder)
        {
            ProviderId = resolvedAction.ProviderId;
            RequestFact = resolvedAction.RequestFact;
            InterruptRequest = resolvedAction.InterruptRequest;
            InterruptContext = resolvedAction.InterruptContext;
            SourceOrder = Mathf.Max(0, sourceOrder);
            Request = resolvedAction.Request;
            ResolvedAction = resolvedAction;
        }

        public CharacterFrameRequestProviderId ProviderId { get; }
        public CharacterInputRequestFact RequestFact { get; }
        public ActionInterruptRequest InterruptRequest { get; }
        public ActionInterruptContext InterruptContext { get; }
        public int SourceOrder { get; }
        public CharacterActionRequest Request { get; }
        public CharacterResolvedAction ResolvedAction { get; }
        public bool HasCandidate => RequestFact.HasRequest;
    }

    public interface ICharacterFrameRequestSubmissionProvider
    {
        bool TryBuild(in CharacterActionRequestSubmissionInput input, int sourceOrder, out CharacterActionRequestSubmissionCandidate candidate);
    }

    public static class CommittedActionRequestSubmissionProviderCollection
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
        readonly DodgeActionRequestProvider requestProvider = new DodgeActionRequestProvider();
        readonly CharacterActionRequestResolverCollection resolvers = CharacterActionRequestResolverCollection.Default;

        public bool TryBuild(
            in CharacterActionRequestSubmissionInput input,
            int sourceOrder,
            out CharacterActionRequestSubmissionCandidate candidate)
        {
            candidate = default;
            if (!requestProvider.TryBuild(in input, sourceOrder, out CharacterActionRequest request))
                return false;

            CharacterActionResolveContext context = CharacterActionResolveContext.FromSubmissionInput(in input);
            if (!resolvers.TryResolve(in request, in context, out CharacterResolvedAction resolvedAction))
                return false;

            candidate = new CharacterActionRequestSubmissionCandidate(in resolvedAction, sourceOrder);
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
