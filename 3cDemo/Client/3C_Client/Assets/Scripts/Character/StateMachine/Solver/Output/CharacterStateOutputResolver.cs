using ThirdPersonAction;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonCharacterStateMachine
{
    public readonly struct CharacterStateOutputResolverInput
    {
        public CharacterStateOutputResolverInput(
            CharacterStateNodeDefinition currentNode,
            CharacterStateId currentState,
            CharacterStateVariant currentVariant,
            float stateTime,
            CharacterStatePayload statePayload,
            in CharacterStateMachineContext context,
            in CharacterStateMachineSnapshot snapshot,
            CharacterStateMachineFrameBuilder builder,
            CharacterStateTransitionConditionTrace[] conditionTraces = null)
        {
            CurrentNode = currentNode;
            CurrentState = currentState;
            CurrentVariant = currentVariant;
            StateTime = Mathf.Max(0f, stateTime);
            StatePayload = statePayload;
            Context = context;
            Snapshot = snapshot;
            Builder = builder;
            ConditionTraces = conditionTraces ?? System.Array.Empty<CharacterStateTransitionConditionTrace>();
        }

        public CharacterStateNodeDefinition CurrentNode { get; }
        public CharacterStateId CurrentState { get; }
        public CharacterStateVariant CurrentVariant { get; }
        public float StateTime { get; }
        public CharacterStatePayload StatePayload { get; }
        public CharacterStateMachineContext Context { get; }
        public CharacterStateMachineSnapshot Snapshot { get; }
        public CharacterStateMachineFrameBuilder Builder { get; }
        public CharacterStateTransitionConditionTrace[] ConditionTraces { get; }
    }

    public static class CharacterStateOutputResolver
    {
        public static CharacterStateMachineFrame Resolve(in CharacterStateOutputResolverInput input)
        {
            bool executeBasicMovement =
                input.CurrentNode != null &&
                (input.CurrentNode.HasModule(CharacterStateModuleType.InputDrivenMotion) ||
                 input.CurrentNode.HasModule(CharacterStateModuleType.TurnBackMotionPolicy));
            bool presentLocomotionAnimation = executeBasicMovement || input.CurrentState == CharacterStateIds.Idle;
            bool setRunLatch = input.Builder != null && input.Builder.SetRunLatch;
            ActionMotionSpec actionMotionSpec = ActionMotionSpec.None(input.Context.CurrentStep);
            TurnBackMotionPolicy turnBackMotionPolicy = default;
            bool hasTurnBackMotionPolicy = input.CurrentNode != null &&
                                           input.CurrentNode.TryGetTurnBackMotionPolicy(out turnBackMotionPolicy);

            if (input.CurrentNode != null &&
                input.CurrentNode.TryResolveActionMovement(input.CurrentVariant, out CharacterActionMovementDefinition movement))
            {
                CharacterStateMachineSnapshot snapshot = input.Snapshot;
                CharacterStateDomainView stateView = CharacterStateDomainView.FromSnapshotAndNode(in snapshot, input.CurrentNode);
                actionMotionSpec = new ActionMotionSpec(
                    stateView.ActionState,
                    input.CurrentState,
                    input.CurrentVariant,
                    movement.Duration,
                    movement.Distance,
                    movement.RotateToDirection,
                    movement.SetRunLatchOnComplete,
                    input.StatePayload.PrimaryWorldDirection,
                    input.StateTime,
                    input.Context.CurrentStep);
            }

            CharacterStateMachineFrameBuilder builder = input.Builder;
            return new CharacterStateMachineFrame(
                input.Snapshot,
                executeBasicMovement,
                presentLocomotionAnimation,
                builder != null && builder.ConsumeInputRequest,
                builder != null ? builder.ConsumedRequestKind : default,
                setRunLatch,
                builder != null && builder.ResetRunLatch,
                actionMotionSpec,
                builder != null ? builder.AnimationRequest : default,
                builder != null && builder.HasAnimationRequest,
                input.StatePayload,
                turnBackMotionPolicy,
                hasTurnBackMotionPolicy,
                input.StatePayload.SecondaryWorldDirection,
                input.StatePayload.EntryBasisForward,
                input.Context.TimelineFacts,
                input.ConditionTraces,
                input.Context.CurrentTimelineFactsTrace,
                input.Context.ProjectedTimelineFactsTrace,
                input.Context.TargetTimelineFactsTrace);
        }
    }
}
