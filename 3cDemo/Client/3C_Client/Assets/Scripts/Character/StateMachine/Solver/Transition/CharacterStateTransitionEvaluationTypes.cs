namespace ThirdPersonCharacterStateMachine
{
    public readonly struct CharacterStateTransitionConditionEvaluationInput
    {
        public CharacterStateTransitionConditionEvaluationInput(
            CharacterStateTransitionCondition condition,
            in CharacterStateMachineContext context,
            CharacterStateNodeDefinition currentNode,
            CharacterStateVariant currentVariant,
            CharacterStateId currentState,
            CharacterStateTransitionDefinition transition,
            float stateTime,
            float projectedStateTime)
        {
            Condition = condition;
            Context = context;
            CurrentNode = currentNode;
            CurrentVariant = currentVariant;
            CurrentState = currentState;
            Transition = transition;
            StateTime = stateTime < 0f ? 0f : stateTime;
            ProjectedStateTime = projectedStateTime < 0f ? 0f : projectedStateTime;
        }

        public CharacterStateTransitionCondition Condition { get; }
        public CharacterStateMachineContext Context { get; }
        public CharacterStateNodeDefinition CurrentNode { get; }
        public CharacterStateVariant CurrentVariant { get; }
        public CharacterStateId CurrentState { get; }
        public CharacterStateTransitionDefinition Transition { get; }
        public float StateTime { get; }
        public float ProjectedStateTime { get; }
        public string SourceStatePath => CurrentState.Value;
        public string TargetStatePath => Transition != null ? Transition.ToStateId.Value : string.Empty;
        public string TransitionPath => Transition != null ? Transition.TransitionPath : string.Empty;
        public int TransitionPriority => Transition != null ? Transition.Priority : 0;
    }

    public readonly struct CharacterStateTransitionConditionTrace
    {
        public CharacterStateTransitionConditionTrace(
            CharacterStateTransitionConditionKind conditionKind,
            string sourceStatePath,
            string targetStatePath,
            int sourceStep,
            bool passed,
            string reason,
            string context,
            bool emitDiagnostic,
            string message)
        {
            ConditionKind = conditionKind;
            SourceStatePath = sourceStatePath ?? string.Empty;
            TargetStatePath = targetStatePath ?? string.Empty;
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
            Passed = passed;
            Reason = reason ?? string.Empty;
            Context = context ?? string.Empty;
            EmitDiagnostic = emitDiagnostic;
            Message = string.IsNullOrWhiteSpace(message) ? "transition-condition" : message.Trim();
        }

        public CharacterStateTransitionConditionKind ConditionKind { get; }
        public string ConditionKey => ConditionKind.ToString();
        public string SourceStatePath { get; }
        public string TargetStatePath { get; }
        public int SourceStep { get; }
        public bool Passed { get; }
        public string Reason { get; }
        public string Context { get; }
        public bool EmitDiagnostic { get; }
        public string Message { get; }
        public bool HasContext => !string.IsNullOrEmpty(Context);
    }

    public readonly struct CharacterStateTransitionConditionEvaluationResult
    {
        public CharacterStateTransitionConditionEvaluationResult(
            bool passed,
            CharacterStateTransitionConditionTrace trace)
        {
            Passed = passed;
            Trace = trace;
        }

        public bool Passed { get; }
        public CharacterStateTransitionConditionTrace Trace { get; }

        public static CharacterStateTransitionConditionEvaluationResult From(
            in CharacterStateTransitionConditionEvaluationInput input,
            bool passed,
            string reason,
            string context = "",
            bool emitDiagnostic = false,
            string message = "")
        {
            return new CharacterStateTransitionConditionEvaluationResult(
                passed,
                new CharacterStateTransitionConditionTrace(
                    input.Condition.Kind,
                    input.SourceStatePath,
                    input.TargetStatePath,
                    input.Context.CurrentStep,
                    passed,
                    reason,
                    context,
                    emitDiagnostic,
                    message));
        }
    }
}
