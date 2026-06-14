namespace ThirdPersonCharacterStateMachine
{
    public static class CharacterStateTransitionEvaluator
    {
        const float TimeEpsilon = 0.0001f;

        public static bool Evaluate(
            CharacterStateTransitionCondition condition,
            in CharacterStateMachineContext context,
            CharacterStateNodeDefinition currentNode,
            CharacterStateVariant currentVariant,
            float projectedStateTime)
        {
            switch (condition.Kind)
            {
                case CharacterStateTransitionConditionKind.HasMoveIntent:
                    return context.HasMoveIntent;
                case CharacterStateTransitionConditionKind.NoMoveIntent:
                    return !context.HasMoveIntent;
                case CharacterStateTransitionConditionKind.StateCanExit:
                    return context.StateCanExit;
                case CharacterStateTransitionConditionKind.HasInputRequest:
                    return context.InputRequest.HasRequest &&
                           context.InputRequest.HasWorldDirection &&
                           context.InputRequest.RequestKind == condition.RequestKind;
                case CharacterStateTransitionConditionKind.StateElapsedAtLeast:
                    return ElapsedAtLeast(projectedStateTime, condition.MinSeconds);
                case CharacterStateTransitionConditionKind.CurrentStateHasTag:
                    return currentNode != null && currentNode.HasTag(condition.Tag);
                case CharacterStateTransitionConditionKind.MoveTurnBackRequested:
                    return IsMoveTurnBackRequested(in context, condition.MinSeconds);
                case CharacterStateTransitionConditionKind.LocomotionAnimationCanExit:
                    return CanExitLocomotionAnimation(in context, currentNode, currentVariant);
                case CharacterStateTransitionConditionKind.ActionCanExit:
                    return CanExitAction(in context, currentNode, currentVariant);
                default:
                    return false;
            }
        }

        static bool IsMoveTurnBackRequested(in CharacterStateMachineContext context, float minAngle)
        {
            ThirdPersonMovement.LocomotionTurnBackIntent intent = context.LocomotionFacts.TurnBackIntent;
            return context.LocomotionFacts.GaitCandidate == ThirdPersonMovement.BasicMovementGait.Run &&
                   intent.IsValidAt(context.CurrentStep) &&
                   intent.HasWorldMoveDirection &&
                   intent.HasFacingForward &&
                   intent.Angle >= minAngle;
        }

        static bool CanExitLocomotionAnimation(
            in CharacterStateMachineContext context,
            CharacterStateNodeDefinition currentNode,
            CharacterStateVariant currentVariant)
        {
            if (currentNode == null)
                return false;

            CharacterStateAnimationBinding binding = currentNode.Animation;
            if (currentVariant != CharacterStateVariant.None &&
                currentNode.TryResolveVariant(currentVariant, out CharacterStateVariantDefinition variant))
            {
                binding = variant.Animation;
            }

            CharacterRuntimeAnimationFacts animation = context.RuntimeBlackboard.Animation;
            if (!binding.HasKey ||
                !animation.LocomotionProgress.HasValidPlayback ||
                animation.LocomotionProgress.AliasKey != binding.KeyValue)
            {
                return false;
            }

            if (currentNode.StateId == CharacterStateIds.TurnBack && currentNode.Output.HasTurnBackMotionPolicy)
            {
                return context.TimelineFacts.ExitWindowActive ||
                       animation.LocomotionProgress.IsEnded;
            }

            return animation.LocomotionProgress.IsEnded;
        }

        static bool CanExitAction(
            in CharacterStateMachineContext context,
            CharacterStateNodeDefinition currentNode,
            CharacterStateVariant currentVariant)
        {
            if (currentNode == null)
                return false;

            CharacterStateAnimationBinding binding = currentNode.Animation;
            if (currentVariant != CharacterStateVariant.None &&
                currentNode.TryResolveVariant(currentVariant, out CharacterStateVariantDefinition variant))
            {
                binding = variant.Animation;
            }

            CharacterRuntimeAnimationFacts animation = context.RuntimeBlackboard.Animation;
            return binding.HasKey &&
                   animation.ActionProgress.HasValidPlayback &&
                   animation.ActionProgress.Key == binding.Key &&
                   animation.ActionProgress.IsEnded;
        }

        static bool ElapsedAtLeast(float projectedStateTime, float duration)
        {
            return projectedStateTime + TimeEpsilon >= duration;
        }
    }
}
