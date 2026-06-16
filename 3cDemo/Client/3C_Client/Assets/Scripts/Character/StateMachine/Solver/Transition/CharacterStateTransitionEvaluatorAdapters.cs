using System.Collections.Generic;
using ThirdPersonMovement;

namespace ThirdPersonCharacterStateMachine
{
    public sealed class CharacterStateCoreConditionEvaluator : ICharacterStateTransitionConditionEvaluator
    {
        const float TimeEpsilon = 0.0001f;

        static readonly CharacterStateTransitionConditionKind[] supported =
        {
            CharacterStateTransitionConditionKind.HasMoveIntent,
            CharacterStateTransitionConditionKind.NoMoveIntent,
            CharacterStateTransitionConditionKind.StateCanExit,
            CharacterStateTransitionConditionKind.HasInputRequest,
            CharacterStateTransitionConditionKind.StateElapsedAtLeast,
            CharacterStateTransitionConditionKind.CurrentStateHasTag
        };

        public string Name => "Core";
        public IReadOnlyList<CharacterStateTransitionConditionKind> SupportedConditions => supported;

        public CharacterStateTransitionConditionEvaluationResult Evaluate(in CharacterStateTransitionConditionEvaluationInput input)
        {
            CharacterStateMachineContext context = input.Context;
            switch (input.Condition.Kind)
            {
                case CharacterStateTransitionConditionKind.HasMoveIntent:
                    return Result(in input, context.HasMoveIntent, $"hasMove={context.HasMoveIntent}");
                case CharacterStateTransitionConditionKind.NoMoveIntent:
                    return Result(in input, !context.HasMoveIntent, $"hasMove={context.HasMoveIntent}");
                case CharacterStateTransitionConditionKind.StateCanExit:
                    return Result(in input, context.StateCanExit, $"stateCanExit={context.StateCanExit}");
                case CharacterStateTransitionConditionKind.HasInputRequest:
                    bool hasInputRequest = context.InputRequest.HasRequest &&
                                           context.InputRequest.HasWorldDirection &&
                                           context.InputRequest.RequestKind == input.Condition.RequestKind;
                    return Result(
                        in input,
                        hasInputRequest,
                        $"hasRequest={context.InputRequest.HasRequest} requestKind={context.InputRequest.RequestKind} requiredKind={input.Condition.RequestKind} hasWorldDirection={context.InputRequest.HasWorldDirection} priority={context.InputRequest.Priority}");
                case CharacterStateTransitionConditionKind.StateElapsedAtLeast:
                    bool elapsed = input.ProjectedStateTime + TimeEpsilon >= input.Condition.MinSeconds;
                    return Result(in input, elapsed, $"projectedStateTime={input.ProjectedStateTime:F3} minSeconds={input.Condition.MinSeconds:F3}");
                case CharacterStateTransitionConditionKind.CurrentStateHasTag:
                    bool hasTag = input.CurrentNode != null && input.CurrentNode.HasTag(input.Condition.Tag);
                    return Result(in input, hasTag, $"tag={input.Condition.Tag} currentNode={(input.CurrentNode != null ? input.CurrentNode.StateId.Value : string.Empty)}");
                default:
                    return CharacterStateTransitionConditionEvaluationResult.From(in input, false, "unsupported-core-condition");
            }
        }

        static CharacterStateTransitionConditionEvaluationResult Result(
            in CharacterStateTransitionConditionEvaluationInput input,
            bool passed,
            string context)
        {
            return CharacterStateTransitionConditionEvaluationResult.From(
                in input,
                passed,
                passed ? "passed" : "failed",
                context);
        }
    }

    public sealed class CharacterStateLocomotionConditionEvaluator : ICharacterStateTransitionConditionEvaluator
    {
        static readonly CharacterStateTransitionConditionKind[] supported =
        {
            CharacterStateTransitionConditionKind.MoveTurnBackRequested
        };

        public string Name => "Locomotion";
        public IReadOnlyList<CharacterStateTransitionConditionKind> SupportedConditions => supported;

        public CharacterStateTransitionConditionEvaluationResult Evaluate(in CharacterStateTransitionConditionEvaluationInput input)
        {
            if (input.Condition.Kind != CharacterStateTransitionConditionKind.MoveTurnBackRequested)
                return CharacterStateTransitionConditionEvaluationResult.From(in input, false, "unsupported-locomotion-condition");

            CharacterStateMachineContext context = input.Context;
            LocomotionTurnBackIntent intent = context.LocomotionFacts.TurnBackIntent;
            bool passed = context.LocomotionFacts.GaitCandidate == BasicMovementGait.Run &&
                          intent.IsValidAt(context.CurrentStep) &&
                          intent.HasWorldMoveDirection &&
                          intent.HasFacingForward &&
                          intent.Angle >= input.Condition.MinSeconds;

            return CharacterStateTransitionConditionEvaluationResult.From(
                in input,
                passed,
                passed ? "turnback-intent-accepted" : "turnback-intent-blocked",
                $"from={input.SourceStatePath} to={input.TargetStatePath} priority={input.TransitionPriority} hasMove={context.HasMoveIntent} worldMove={context.WorldMoveDirection.ToString("F3")} facing={context.FacingForward.ToString("F3")} intentValid={intent.IsValidAt(context.CurrentStep)} intentOrigin={intent.OriginStep} intentExpire={intent.ExpireStep} angle={intent.Angle:F3} threshold={input.Condition.MinSeconds:F3} passed={passed} stateTime={input.StateTime:F3} projectedStateTime={input.ProjectedStateTime:F3} phaseCanExit={context.StateCanExit} locomotionPhase={context.RuntimeBlackboard.Locomotion.Phase} blackboardHasMove={context.RuntimeBlackboard.Locomotion.HasMoveIntent} blackboardWorld={context.RuntimeBlackboard.Locomotion.WorldDirection.ToString("F3")}",
                true,
                "locomotion-turnback-condition");
        }
    }

    public sealed class CharacterStateAnimationConditionEvaluator : ICharacterStateTransitionConditionEvaluator
    {
        static readonly CharacterStateTransitionConditionKind[] supported =
        {
            CharacterStateTransitionConditionKind.LocomotionAnimationCanExit
        };

        public string Name => "Animation";
        public IReadOnlyList<CharacterStateTransitionConditionKind> SupportedConditions => supported;

        public CharacterStateTransitionConditionEvaluationResult Evaluate(in CharacterStateTransitionConditionEvaluationInput input)
        {
            if (input.Condition.Kind != CharacterStateTransitionConditionKind.LocomotionAnimationCanExit)
                return CharacterStateTransitionConditionEvaluationResult.From(in input, false, "unsupported-animation-condition");

            CharacterStateNodeDefinition currentNode = input.CurrentNode;
            if (currentNode == null)
                return CharacterStateTransitionConditionEvaluationResult.From(in input, false, "current-node-missing");

            if (!currentNode.TryResolveAnimationBinding(
                    input.CurrentVariant,
                    out CharacterStateAnimationBinding binding,
                    out CharacterStatePlaybackFactSource playbackFactSource) ||
                playbackFactSource != CharacterStatePlaybackFactSource.Locomotion)
            {
                return CharacterStateTransitionConditionEvaluationResult.From(in input, false, "locomotion-binding-missing");
            }

            CharacterRuntimeAnimationFacts animation = input.Context.RuntimeBlackboard.Animation;
            if (!binding.HasKey ||
                !animation.LocomotionProgress.HasValidPlayback ||
                animation.LocomotionProgress.AliasKey != binding.TimelineBindingKey)
            {
                return CharacterStateTransitionConditionEvaluationResult.From(
                    in input,
                    false,
                    "locomotion-playback-mismatch",
                    $"binding={binding.TimelineBindingKey} playback={animation.LocomotionProgress.AliasKey} hasPlayback={animation.LocomotionProgress.HasValidPlayback}");
            }

            StateTimelineWindowFacts projectedFacts = input.Context.ProjectedTimelineFacts;
            bool passed = currentNode.TryGetTurnBackMotionPolicy(out _)
                ? projectedFacts.ExitWindowActive || animation.LocomotionProgress.IsEnded
                : animation.LocomotionProgress.IsEnded;

            return CharacterStateTransitionConditionEvaluationResult.From(
                in input,
                passed,
                passed ? "locomotion-animation-can-exit" : "locomotion-animation-not-ended",
                $"binding={binding.TimelineBindingKey} playback={animation.LocomotionProgress.AliasKey} ended={animation.LocomotionProgress.IsEnded} exitWindow={projectedFacts.ExitWindowActive} currentFacts={input.Context.CurrentTimelineFactsTrace.FactsId} projectedFacts={input.Context.ProjectedTimelineFactsTrace.FactsId}");
        }
    }

    public sealed class CharacterStateActionConditionEvaluator : ICharacterStateTransitionConditionEvaluator
    {
        static readonly CharacterStateTransitionConditionKind[] supported =
        {
            CharacterStateTransitionConditionKind.ActionCanExit
        };

        public string Name => "Action";
        public IReadOnlyList<CharacterStateTransitionConditionKind> SupportedConditions => supported;

        public CharacterStateTransitionConditionEvaluationResult Evaluate(in CharacterStateTransitionConditionEvaluationInput input)
        {
            if (input.Condition.Kind != CharacterStateTransitionConditionKind.ActionCanExit)
                return CharacterStateTransitionConditionEvaluationResult.From(in input, false, "unsupported-action-condition");

            CharacterStateNodeDefinition currentNode = input.CurrentNode;
            if (currentNode == null)
                return CharacterStateTransitionConditionEvaluationResult.From(in input, false, "current-node-missing");

            if (!currentNode.TryResolveAnimationBinding(
                    input.CurrentVariant,
                    out CharacterStateAnimationBinding binding,
                    out CharacterStatePlaybackFactSource playbackFactSource) ||
                playbackFactSource != CharacterStatePlaybackFactSource.Action)
            {
                return CharacterStateTransitionConditionEvaluationResult.From(in input, false, "action-binding-missing");
            }

            CharacterRuntimeAnimationFacts animation = input.Context.RuntimeBlackboard.Animation;
            bool passed = binding.HasKey &&
                          animation.ActionProgress.HasValidPlayback &&
                          animation.ActionProgress.Key == binding.Key &&
                          animation.ActionProgress.IsEnded;

            return CharacterStateTransitionConditionEvaluationResult.From(
                in input,
                passed,
                passed ? "action-animation-can-exit" : "action-animation-not-ended",
                $"binding={binding.Key.Value} playback={animation.ActionProgress.Key.Value} hasPlayback={animation.ActionProgress.HasValidPlayback} ended={animation.ActionProgress.IsEnded}");
        }
    }
}
