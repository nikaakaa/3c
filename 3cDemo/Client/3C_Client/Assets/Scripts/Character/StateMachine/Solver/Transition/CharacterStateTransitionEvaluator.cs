namespace ThirdPersonCharacterStateMachine
{
    public static class CharacterStateTransitionEvaluator
    {
        public static bool Evaluate(
            CharacterStateTransitionCondition condition,
            in CharacterStateMachineContext context,
            CharacterStateNodeDefinition currentNode,
            CharacterStateVariant currentVariant,
            float projectedStateTime)
        {
            CharacterStateTransitionConditionEvaluationInput input = new CharacterStateTransitionConditionEvaluationInput(
                condition,
                in context,
                currentNode,
                currentVariant,
                currentNode != null ? currentNode.StateId : default,
                null,
                projectedStateTime,
                projectedStateTime);
            return CharacterStateTransitionEvaluatorCollection.Default.Evaluate(in input).Passed;
        }
    }
}
