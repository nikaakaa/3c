using ThirdPersonAction;
using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonCharacterStateMachine
{
    public readonly struct CharacterStateMachineContext
    {
        public CharacterStateMachineContext(
            float deltaTime,
            int currentStep,
            MovementInputIntent moveIntent,
            Vector3 worldMoveDirection,
            BasicMovementPhaseFacts phaseFacts,
            CharacterInputRequestFact inputRequest)
            : this(
                deltaTime,
                currentStep,
                moveIntent,
                worldMoveDirection,
                Vector3.forward,
                phaseFacts,
                inputRequest,
                CharacterRuntimeBlackboardSnapshot.Empty,
                StateTimelineWindowFacts.None(default))
        {
        }

        public CharacterStateMachineContext(
            float deltaTime,
            int currentStep,
            in LocomotionDecisionFacts locomotionFacts,
            CharacterInputRequestFact inputRequest)
            : this(
                deltaTime,
                currentStep,
                in locomotionFacts,
                inputRequest,
                CharacterRuntimeBlackboardSnapshot.Empty,
                StateTimelineWindowFacts.None(default))
        {
        }

        public CharacterStateMachineContext(
            float deltaTime,
            int currentStep,
            in LocomotionDecisionFacts locomotionFacts,
            CharacterInputRequestFact inputRequest,
            CharacterRuntimeBlackboardSnapshot runtimeBlackboard)
            : this(
                deltaTime,
                currentStep,
                in locomotionFacts,
                inputRequest,
                runtimeBlackboard,
                StateTimelineWindowFacts.None(default))
        {
        }

        public CharacterStateMachineContext(
            float deltaTime,
            int currentStep,
            in LocomotionDecisionFacts locomotionFacts,
            CharacterInputRequestFact inputRequest,
            CharacterRuntimeBlackboardSnapshot runtimeBlackboard,
            StateTimelineWindowFacts timelineFacts)
        {
            DeltaTime = Mathf.Max(0f, deltaTime);
            CurrentStep = Mathf.Max(0, currentStep);
            LocomotionFacts = locomotionFacts;
            MoveIntent = locomotionFacts.MoveIntent;
            WorldMoveDirection = NormalizePlanarOrZero(locomotionFacts.SpatialFacts.WorldMoveDirection);
            FacingForward = NormalizePlanarOrZero(locomotionFacts.SpatialFacts.FacingForward);
            PhaseFacts = locomotionFacts.PhaseFacts;
            InputRequest = inputRequest;
            RuntimeBlackboard = runtimeBlackboard;
            TimelineFacts = timelineFacts;
        }

        public CharacterStateMachineContext(
            float deltaTime,
            int currentStep,
            MovementInputIntent moveIntent,
            Vector3 worldMoveDirection,
            BasicMovementPhaseFacts phaseFacts,
            CharacterInputRequestFact inputRequest,
            CharacterRuntimeBlackboardSnapshot runtimeBlackboard)
            : this(
                deltaTime,
                currentStep,
                moveIntent,
                worldMoveDirection,
                Vector3.forward,
                phaseFacts,
                inputRequest,
                runtimeBlackboard,
                StateTimelineWindowFacts.None(default))
        {
        }

        public CharacterStateMachineContext(
            float deltaTime,
            int currentStep,
            MovementInputIntent moveIntent,
            Vector3 worldMoveDirection,
            Vector3 facingForward,
            BasicMovementPhaseFacts phaseFacts,
            CharacterInputRequestFact inputRequest,
            CharacterRuntimeBlackboardSnapshot runtimeBlackboard)
            : this(
                deltaTime,
                currentStep,
                moveIntent,
                worldMoveDirection,
                facingForward,
                phaseFacts,
                inputRequest,
                runtimeBlackboard,
                StateTimelineWindowFacts.None(default))
        {
        }

        public CharacterStateMachineContext(
            float deltaTime,
            int currentStep,
            MovementInputIntent moveIntent,
            Vector3 worldMoveDirection,
            Vector3 facingForward,
            BasicMovementPhaseFacts phaseFacts,
            CharacterInputRequestFact inputRequest,
            CharacterRuntimeBlackboardSnapshot runtimeBlackboard,
            StateTimelineWindowFacts timelineFacts)
        {
            DeltaTime = Mathf.Max(0f, deltaTime);
            CurrentStep = Mathf.Max(0, currentStep);
            MoveIntent = moveIntent;
            WorldMoveDirection = NormalizePlanarOrZero(worldMoveDirection);
            FacingForward = NormalizePlanarOrZero(facingForward);
            PhaseFacts = phaseFacts;
            InputRequest = inputRequest;
            RuntimeBlackboard = runtimeBlackboard;
            TimelineFacts = timelineFacts;
            LocomotionFacts = new LocomotionDecisionFacts(
                MoveIntent,
                MoveIntent.HasMoveIntent ? MoveIntent.Gait : BasicMovementGait.Walk,
                PhaseFacts,
                new LocomotionSpatialFacts(WorldMoveDirection, FacingForward, Vector3.zero, Vector3.zero),
                LocomotionTurnBackIntent.None);
        }

        public float DeltaTime { get; }
        public int CurrentStep { get; }
        public LocomotionDecisionFacts LocomotionFacts { get; }
        public MovementInputIntent MoveIntent { get; }
        public Vector3 WorldMoveDirection { get; }
        public Vector3 FacingForward { get; }
        public BasicMovementPhaseFacts PhaseFacts { get; }
        public CharacterInputRequestFact InputRequest { get; }
        public CharacterRuntimeBlackboardSnapshot RuntimeBlackboard { get; }
        public StateTimelineWindowFacts TimelineFacts { get; }
        public bool HasMoveIntent => MoveIntent.HasMoveIntent;
        public bool StateCanExit => PhaseFacts.PhaseCanExit;

        public CharacterStateMachineContext WithTimelineFacts(StateTimelineWindowFacts timelineFacts)
        {
            LocomotionDecisionFacts locomotionFacts = LocomotionFacts;
            return new CharacterStateMachineContext(
                DeltaTime,
                CurrentStep,
                in locomotionFacts,
                InputRequest,
                RuntimeBlackboard,
                timelineFacts);
        }

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }

    public readonly struct CharacterStateMachineSnapshot
    {
        public CharacterStateMachineSnapshot(
            CharacterStateId activeState,
            float stateTime,
            CharacterStateVariant variant,
            string pendingTransitionPath,
            CharacterStateTag[] tags)
        {
            ActiveState = activeState;
            ActivePath = activeState.Value;
            StateTime = Mathf.Max(0f, stateTime);
            Variant = variant;
            PendingTransitionPath = pendingTransitionPath ?? string.Empty;
            Tags = new IReadOnlyListWrapper<CharacterStateTag>(tags);
        }

        public CharacterStateId ActiveState { get; }
        public string ActivePath { get; }
        public float StateTime { get; }
        public CharacterStateVariant Variant { get; }
        public string PendingTransitionPath { get; }
        public IReadOnlyListWrapper<CharacterStateTag> Tags { get; }
        public bool HasPendingTransition => !string.IsNullOrEmpty(PendingTransitionPath);
        public bool IsAction => (ActivePath ?? string.Empty).StartsWith(CharacterStateIds.Action.Value + "/", System.StringComparison.Ordinal);
        public bool IsLocomotion => (ActivePath ?? string.Empty).StartsWith(CharacterStateIds.Locomotion.Value + "/", System.StringComparison.Ordinal);

        public BasicMovementPhase LocomotionPhase
        {
            get
            {
                if (ActiveState == CharacterStateIds.MoveStart)
                    return BasicMovementPhase.MoveStart;
                if (ActiveState == CharacterStateIds.MoveLoop)
                    return BasicMovementPhase.MoveLoop;
                if (ActiveState == CharacterStateIds.MoveStop)
                    return BasicMovementPhase.MoveStop;
                if (ActiveState == CharacterStateIds.TurnBack)
                    return BasicMovementPhase.TurnBack;
                return BasicMovementPhase.Idle;
            }
        }

        public ActionStateId ActionState => IsAction ? new ActionStateId("Action." + LastPathSegment(ActivePath)) : ActionStateIds.None;
        public FullBodyOwner Owner => !ActiveState.IsValid ? FullBodyOwner.None : IsAction ? FullBodyOwner.Action(ActionState) : FullBodyOwner.Locomotion;

        public static CharacterStateMachineSnapshot Inactive => new CharacterStateMachineSnapshot(
            default,
            0f,
            CharacterStateVariant.None,
            string.Empty,
            System.Array.Empty<CharacterStateTag>());

        static string LastPathSegment(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            int index = path.LastIndexOf('/');
            return index >= 0 && index < path.Length - 1 ? path.Substring(index + 1) : path;
        }
    }

    public readonly struct CharacterStateMachineRestoreState
    {
        public CharacterStateMachineRestoreState(
            CharacterStateMachineSnapshot snapshot,
            Vector3 actionWorldDirection,
            bool animationRequestedForState,
            bool consumeRequestOnStateEnter,
            bool resetRunLatchOnStateEnter,
            bool setRunLatchOnTransition)
            : this(
                snapshot,
                actionWorldDirection,
                Vector3.zero,
                Vector3.zero,
                animationRequestedForState,
                consumeRequestOnStateEnter,
                resetRunLatchOnStateEnter,
                setRunLatchOnTransition)
        {
        }

        public CharacterStateMachineRestoreState(
            CharacterStateMachineSnapshot snapshot,
            Vector3 actionWorldDirection,
            Vector3 turnBackWorldDirection,
            bool animationRequestedForState,
            bool consumeRequestOnStateEnter,
            bool resetRunLatchOnStateEnter,
            bool setRunLatchOnTransition)
            : this(
                snapshot,
                actionWorldDirection,
                turnBackWorldDirection,
                Vector3.zero,
                animationRequestedForState,
                consumeRequestOnStateEnter,
                resetRunLatchOnStateEnter,
                setRunLatchOnTransition)
        {
        }

        public CharacterStateMachineRestoreState(
            CharacterStateMachineSnapshot snapshot,
            Vector3 actionWorldDirection,
            Vector3 turnBackWorldDirection,
            Vector3 turnBackEntryBasisForward,
            bool animationRequestedForState,
            bool consumeRequestOnStateEnter,
            bool resetRunLatchOnStateEnter,
            bool setRunLatchOnTransition)
        {
            Snapshot = snapshot;
            ActionWorldDirection = NormalizePlanarOrZero(actionWorldDirection);
            TurnBackWorldDirection = NormalizePlanarOrZero(turnBackWorldDirection);
            TurnBackEntryBasisForward = NormalizePlanarOrZero(turnBackEntryBasisForward);
            AnimationRequestedForState = animationRequestedForState;
            ConsumeRequestOnStateEnter = consumeRequestOnStateEnter;
            ResetRunLatchOnStateEnter = resetRunLatchOnStateEnter;
            SetRunLatchOnTransition = setRunLatchOnTransition;
        }

        public CharacterStateMachineSnapshot Snapshot { get; }
        public Vector3 ActionWorldDirection { get; }
        public Vector3 TurnBackWorldDirection { get; }
        public Vector3 TurnBackEntryBasisForward { get; }
        public bool AnimationRequestedForState { get; }
        public bool ConsumeRequestOnStateEnter { get; }
        public bool ResetRunLatchOnStateEnter { get; }
        public bool SetRunLatchOnTransition { get; }

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }

    public readonly struct CharacterStateAnimationRequest
    {
        public CharacterStateAnimationRequest(CharacterStateAnimationBinding binding, int sourceStep)
        {
            Binding = binding;
            SourceStep = Mathf.Max(0, sourceStep);
        }

        public CharacterStateAnimationBinding Binding { get; }
        public ActionAnimationKey Key => Binding.Key;
        public int SourceStep { get; }
        public bool HasKey => Binding.HasKey;
        public bool HasAnimationReference => Binding.HasAnimationReference;
    }

    public readonly struct CharacterStateMachineFrame
    {
        public CharacterStateMachineFrame(
            CharacterStateMachineSnapshot snapshot,
            bool executeBasicMovement,
            bool presentLocomotionAnimation,
            bool consumeInputRequest,
            InputRequestKind consumedRequestKind,
            bool setRunLatch,
            bool resetRunLatch,
            ActionMovementCommand actionMovementCommand,
            bool hasActionMovement,
            bool actionCompleted,
            CharacterStateAnimationRequest animationRequest,
            bool hasAnimationRequest,
            TurnBackMotionPolicy turnBackMotionPolicy = default,
            bool hasTurnBackMotionPolicy = false,
            Vector3 turnBackWorldDirection = default,
            Vector3 turnBackEntryBasisForward = default,
            StateTimelineWindowFacts timelineFacts = default)
        {
            Snapshot = snapshot;
            ExecuteBasicMovement = executeBasicMovement;
            PresentLocomotionAnimation = presentLocomotionAnimation;
            ConsumeInputRequest = consumeInputRequest;
            ConsumedRequestKind = consumedRequestKind;
            SetRunLatch = setRunLatch;
            ResetRunLatch = resetRunLatch;
            ActionMovementCommand = actionMovementCommand;
            HasActionMovement = hasActionMovement;
            ActionCompleted = actionCompleted;
            AnimationRequest = animationRequest;
            HasAnimationRequest = hasAnimationRequest;
            TurnBackMotionPolicy = turnBackMotionPolicy;
            HasTurnBackMotionPolicy = hasTurnBackMotionPolicy && turnBackMotionPolicy.IsEnabled;
            TurnBackWorldDirection = NormalizePlanarOrZero(turnBackWorldDirection);
            TurnBackEntryBasisForward = NormalizePlanarOrZero(turnBackEntryBasisForward);
            TimelineFacts = timelineFacts;
        }

        public CharacterStateMachineSnapshot Snapshot { get; }
        public BasicMovementPhase LocomotionPhase => Snapshot.LocomotionPhase;
        public FullBodyOwner Owner => Snapshot.Owner;
        public ActionStateId ActionState => Snapshot.ActionState;
        public bool ExecuteBasicMovement { get; }
        public bool PresentLocomotionAnimation { get; }
        public bool ConsumeInputRequest { get; }
        public InputRequestKind ConsumedRequestKind { get; }
        public bool SetRunLatch { get; }
        public bool ResetRunLatch { get; }
        public ActionMovementCommand ActionMovementCommand { get; }
        public bool HasActionMovement { get; }
        public bool ActionCompleted { get; }
        public CharacterStateAnimationRequest AnimationRequest { get; }
        public bool HasAnimationRequest { get; }
        public TurnBackMotionPolicy TurnBackMotionPolicy { get; }
        public bool HasTurnBackMotionPolicy { get; }
        public Vector3 TurnBackWorldDirection { get; }
        public Vector3 TurnBackEntryBasisForward { get; }
        public StateTimelineWindowFacts TimelineFacts { get; }

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            if (sqrMagnitude <= 0.000001f)
                return Vector3.zero;

            return value / Mathf.Sqrt(sqrMagnitude);
        }
    }
}
