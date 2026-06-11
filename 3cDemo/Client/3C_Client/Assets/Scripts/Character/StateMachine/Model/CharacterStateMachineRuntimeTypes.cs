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
        {
            DeltaTime = Mathf.Max(0f, deltaTime);
            CurrentStep = Mathf.Max(0, currentStep);
            MoveIntent = moveIntent;
            WorldMoveDirection = NormalizePlanarOrZero(worldMoveDirection);
            PhaseFacts = phaseFacts;
            InputRequest = inputRequest;
        }

        public float DeltaTime { get; }
        public int CurrentStep { get; }
        public MovementInputIntent MoveIntent { get; }
        public Vector3 WorldMoveDirection { get; }
        public BasicMovementPhaseFacts PhaseFacts { get; }
        public CharacterInputRequestFact InputRequest { get; }
        public bool HasMoveIntent => MoveIntent.HasMoveIntent;
        public bool StateCanExit => PhaseFacts.PhaseCanExit;

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
        public bool IsAction => ActivePath.StartsWith(CharacterStateIds.Action.Value + "/", System.StringComparison.Ordinal);
        public bool IsLocomotion => ActivePath.StartsWith(CharacterStateIds.Locomotion.Value + "/", System.StringComparison.Ordinal);

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
            bool hasAnimationRequest)
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
    }
}
