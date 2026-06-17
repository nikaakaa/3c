using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAction
{
    public readonly struct CharacterActionRequest
    {
        public CharacterActionRequest(
            CharacterFrameRequestProviderId providerId,
            ActionRequestType requestType,
            InputRequestKind sourceInputKind,
            int originStep,
            int expireStep,
            int priorityHint,
            int sourceOrder,
            CharacterStateVariant variantHint,
            Vector3 worldDirection)
        {
            ProviderId = providerId;
            RequestType = requestType;
            SourceInputKind = sourceInputKind;
            OriginStep = Mathf.Max(0, originStep);
            ExpireStep = Mathf.Max(OriginStep, expireStep);
            PriorityHint = Mathf.Max(0, priorityHint);
            SourceOrder = Mathf.Max(0, sourceOrder);
            VariantHint = variantHint;
            WorldDirection = NormalizePlanarOrZero(worldDirection);
        }

        public CharacterFrameRequestProviderId ProviderId { get; }
        public ActionRequestType RequestType { get; }
        public InputRequestKind SourceInputKind { get; }
        public int OriginStep { get; }
        public int ExpireStep { get; }
        public int PriorityHint { get; }
        public int SourceOrder { get; }
        public CharacterStateVariant VariantHint { get; }
        public Vector3 WorldDirection { get; }
        public bool HasRequest => ProviderId != CharacterFrameRequestProviderId.None && RequestType != ActionRequestType.None;
        public bool HasWorldDirection => WorldDirection.sqrMagnitude > 0.000001f;

        public static CharacterActionRequest FromBufferedInput(
            CharacterFrameRequestProviderId providerId,
            ActionRequestType requestType,
            in BufferedInputRequest inputRequest,
            int sourceOrder)
        {
            return new CharacterActionRequest(
                providerId,
                requestType,
                inputRequest.Kind,
                inputRequest.OriginStep,
                inputRequest.ExpireStep,
                0,
                sourceOrder,
                CharacterStateVariant.None,
                Vector3.zero);
        }

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }

    public readonly struct CharacterActionResolveContext
    {
        public CharacterActionResolveContext(
            int currentStep,
            CharacterStateMachineSnapshot snapshot,
            in BasicLocomotionInputSnapshot locomotionInput,
            bool runLatchActive,
            in LocomotionDecisionFacts locomotionFacts,
            StateTimelineWindowFacts currentTimelineFacts,
            bool hasActionCatalog,
            in CharacterActionCatalog actionCatalog,
            int currentActionResistance)
        {
            CurrentStep = Mathf.Max(0, currentStep);
            Snapshot = snapshot;
            LocomotionInput = locomotionInput;
            RunLatchActive = runLatchActive;
            LocomotionFacts = locomotionFacts;
            CurrentTimelineFacts = currentTimelineFacts;
            HasActionCatalog = hasActionCatalog && actionCatalog.HasCatalog;
            ActionCatalog = HasActionCatalog ? actionCatalog : CharacterActionCatalog.Empty;
            CurrentActionResistance = Mathf.Max(0, currentActionResistance);
        }

        public int CurrentStep { get; }
        public CharacterStateMachineSnapshot Snapshot { get; }
        public BasicLocomotionInputSnapshot LocomotionInput { get; }
        public bool RunLatchActive { get; }
        public LocomotionDecisionFacts LocomotionFacts { get; }
        public StateTimelineWindowFacts CurrentTimelineFacts { get; }
        public bool HasActionCatalog { get; }
        public CharacterActionCatalog ActionCatalog { get; }
        public int CurrentActionResistance { get; }

        public static CharacterActionResolveContext FromSubmissionInput(in CharacterActionRequestSubmissionInput input)
        {
            BasicLocomotionInputSnapshot locomotionInput = input.LocomotionInput;
            LocomotionDecisionFacts locomotionFacts = input.LocomotionFacts;
            CharacterActionCatalog actionCatalog = input.ActionCatalog;
            return new CharacterActionResolveContext(
                input.CurrentStep,
                input.Snapshot,
                in locomotionInput,
                input.RunLatchActive,
                in locomotionFacts,
                input.CurrentTimelineFacts,
                input.HasActionCatalog,
                in actionCatalog,
                input.CurrentActionResistance);
        }
    }

    public readonly struct CharacterResolvedAction
    {
        public CharacterResolvedAction(
            CharacterFrameRequestProviderId providerId,
            CharacterActionRequest request,
            CharacterInputRequestFact requestFact,
            ActionInterruptRequest interruptRequest,
            ActionInterruptContext interruptContext,
            ActionAnimationKey animationKey,
            ActionMotionSpec motionSpec)
        {
            ProviderId = providerId;
            Request = request;
            RequestFact = requestFact;
            InterruptRequest = interruptRequest;
            InterruptContext = interruptContext;
            AnimationKey = animationKey;
            MotionSpec = motionSpec;
        }

        public CharacterFrameRequestProviderId ProviderId { get; }
        public CharacterActionRequest Request { get; }
        public CharacterInputRequestFact RequestFact { get; }
        public ActionInterruptRequest InterruptRequest { get; }
        public ActionInterruptContext InterruptContext { get; }
        public ActionAnimationKey AnimationKey { get; }
        public ActionMotionSpec MotionSpec { get; }
        public bool HasResolvedAction => ProviderId != CharacterFrameRequestProviderId.None && Request.HasRequest && RequestFact.HasRequest;
    }
}
