namespace ThirdPersonCharacterStateMachine
{
    public static class CharacterStateTransitionEvaluator
    {
        public static bool Evaluate(
            CharacterStateTransitionCondition condition,
            in CharacterStateMachineContext context,
            CharacterStateNodeDefinition currentNode,
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
                    return projectedStateTime >= condition.MinSeconds;
                case CharacterStateTransitionConditionKind.RequestPriorityAtLeast:
                    return context.InputRequest.HasRequest && context.InputRequest.Priority >= condition.MinPriority;
                case CharacterStateTransitionConditionKind.CurrentStateHasTag:
                    return currentNode != null && currentNode.HasTag(condition.Tag);
                default:
                    return false;
            }
        }
    }
}
