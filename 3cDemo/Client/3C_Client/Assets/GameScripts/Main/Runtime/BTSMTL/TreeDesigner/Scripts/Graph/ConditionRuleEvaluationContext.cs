namespace TreeDesigner
{
    public sealed class ConditionRuleEvaluationContext
    {
        public BaseGraph OwnerGraph { get; }
        public IStateMachineRuntimeFacts StateMachineFacts { get; }
        public StateMachineExecutionScope StateScope { get; }
        public bool Failed { get; private set; }
        public string FailureReason { get; private set; }

        public ConditionRuleEvaluationContext(
            BaseGraph ownerGraph,
            IStateMachineRuntimeFacts stateMachineFacts,
            StateMachineExecutionScope stateScope)
        {
            OwnerGraph = ownerGraph;
            StateMachineFacts = stateMachineFacts;
            StateScope = stateScope;
            FailureReason = string.Empty;
        }

        public void Fail(string reason)
        {
            Failed = true;
            if (string.IsNullOrEmpty(FailureReason))
                FailureReason = reason ?? string.Empty;
        }
    }
}
