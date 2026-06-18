using System;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAction
{
    public interface ICharacterActionRequestProvider
    {
        bool TryBuild(in CharacterActionRequestSubmissionInput input, int sourceOrder, out CharacterActionRequest request);
    }

    public interface ICharacterActionRequestResolver
    {
        bool TryResolve(in CharacterActionRequest request, in CharacterActionResolveContext context, out CharacterResolvedAction resolvedAction);
    }

    public readonly struct CharacterActionRequestResolverCollection
    {
        static readonly ICharacterActionRequestResolver[] defaultResolvers =
        {
            new DodgeCharacterActionRequestResolver()
        };

        readonly ICharacterActionRequestResolver[] resolvers;

        public CharacterActionRequestResolverCollection(ICharacterActionRequestResolver[] resolvers)
        {
            this.resolvers = resolvers ?? Array.Empty<ICharacterActionRequestResolver>();
        }

        public bool TryResolve(
            in CharacterActionRequest request,
            in CharacterActionResolveContext context,
            out CharacterResolvedAction resolvedAction)
        {
            ICharacterActionRequestResolver[] activeResolvers = resolvers ?? Array.Empty<ICharacterActionRequestResolver>();
            for (int i = 0; i < activeResolvers.Length; i++)
            {
                ICharacterActionRequestResolver resolver = activeResolvers[i];
                if (resolver != null && resolver.TryResolve(in request, in context, out resolvedAction))
                    return true;
            }

            resolvedAction = default;
            return false;
        }

        public static CharacterActionRequestResolverCollection Default =>
            new CharacterActionRequestResolverCollection(defaultResolvers);
    }

    public sealed class DodgeActionRequestProvider : ICharacterActionRequestProvider
    {
        public bool TryBuild(in CharacterActionRequestSubmissionInput input, int sourceOrder, out CharacterActionRequest request)
        {
            request = default;
            if (input.InputBuffer == null ||
                !input.InputBuffer.TryPeek(InputRequestKind.Dodge, input.CurrentStep, out BufferedInputRequest bufferedRequest))
            {
                return false;
            }

            request = CharacterActionRequest.FromBufferedInput(
                CharacterFrameRequestProviderId.Dodge,
                ActionRequestType.Dodge,
                in bufferedRequest,
                bufferedRequest.OriginStep);
            return true;
        }
    }

    public sealed class DodgeCharacterActionRequestResolver : ICharacterActionRequestResolver
    {
        public bool TryResolve(
            in CharacterActionRequest request,
            in CharacterActionResolveContext context,
            out CharacterResolvedAction resolvedAction)
        {
            resolvedAction = default;
            if (!request.HasRequest ||
                request.RequestType != ActionRequestType.Dodge ||
                request.SourceInputKind != InputRequestKind.Dodge ||
                !context.ActionCatalog.TryGetDodgeDefinition(out CharacterActionDefinition definition))
            {
                return false;
            }

            LocomotionDecisionFacts locomotionFacts = context.LocomotionFacts;
            MovementInputIntent moveIntent = locomotionFacts.MoveIntent;
            LocomotionSpatialFacts spatialFacts = locomotionFacts.SpatialFacts;
            if (!DodgeActionPlanner.TryResolveRequest(
                    in request,
                    in moveIntent,
                    spatialFacts.WorldMoveDirection,
                    spatialFacts.FacingForward,
                    definition.Priority,
                    out DodgeActionRequest dodgeRequest))
            {
                return false;
            }

            CharacterStateVariant variant = dodgeRequest.Variant == DodgeActionVariant.Backstep
                ? CharacterStateVariant.Backstep
                : CharacterStateVariant.Directional;
            CharacterInputRequestFact requestFact = CommittedActionInputRequestBuilder.ToInputRequestFact(in dodgeRequest);
            ActionInterruptContext interruptContext = CommittedActionInterruptRequestFactory.CreateContext(
                context.Snapshot,
                context.CurrentStep,
                context.CurrentActionResistance,
                context.CurrentTimelineFacts);
            ActionInterruptRequest interruptRequest = dodgeRequest.ToInterruptRequest();

            ActionMotionSpec motionSpec = new ActionMotionSpec(
                definition.ActionState,
                definition.MotionSourceState,
                variant,
                0f,
                0f,
                false,
                false,
                dodgeRequest.WorldDirection,
                0f,
                context.CurrentStep);

            resolvedAction = new CharacterResolvedAction(
                CharacterFrameRequestProviderId.Dodge,
                request,
                requestFact,
                interruptRequest,
                interruptContext,
                DodgeActionPlanner.ResolveAnimationKey(dodgeRequest.Variant),
                motionSpec);
            return true;
        }
    }
}
