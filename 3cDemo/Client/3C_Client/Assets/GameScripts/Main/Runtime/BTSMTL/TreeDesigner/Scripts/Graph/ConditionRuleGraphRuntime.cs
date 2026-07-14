namespace TreeDesigner
{
    public sealed class ConditionRuleGraphRuntime
    {
        readonly ConditionRuleGraph m_SourceGraph;
        readonly BaseEdge m_SourceEdge;
        ConditionRuleGraph m_RuntimeGraph;

        public ConditionRuleGraphRuntime(ConditionRuleGraph sourceGraph, BaseEdge sourceEdge)
        {
            m_SourceGraph = sourceGraph;
            m_SourceEdge = sourceEdge;
        }

        public bool Evaluate(
            BaseGraph context,
            IStateMachineRuntimeFacts stateMachineFacts = null,
            StateMachineExecutionScope stateScope = default)
        {
            if (!EnsureRuntimeGraph(context))
                return false;

            m_RuntimeGraph.SetDeltaTime(context?.DeltaTime ?? 0f);
            var evaluationContext = new ConditionRuleEvaluationContext(context, stateMachineFacts, stateScope);
            m_RuntimeGraph.SetEvaluationContext(evaluationContext);
            bool result = m_RuntimeGraph.Evaluate();
            bool passed = !evaluationContext.Failed && result;
            TreeRuntimeDiagnostics.PublishConditionGraph(m_RuntimeGraph, passed);
            return passed;
        }

        public void Dispose()
        {
            if (m_RuntimeGraph)
            {
                m_RuntimeGraph.SetEvaluationContext(null);
                m_RuntimeGraph.DisposeTree();
            }

            m_RuntimeGraph = null;
        }

        public void Reset()
        {
            Dispose();
        }

        bool EnsureRuntimeGraph(BaseGraph context)
        {
            if (!m_SourceGraph)
                return false;

            if (!m_RuntimeGraph)
                m_RuntimeGraph = m_SourceGraph.Clone();

            if (!m_RuntimeGraph.IsValid)
            {
                if (context == null || m_SourceEdge == null)
                    throw new System.InvalidOperationException("ConditionRuleGraph runtime requires its owner Graph and edge.");
                TreeGraphReferenceOwnership ownership = m_SourceEdge.ConditionRuleGraphOwnership == ConditionRuleGraphOwnership.Shared
                    ? TreeGraphReferenceOwnership.Shared
                    : TreeGraphReferenceOwnership.Inline;
                TreeAuthoringRouteId route = TreeAuthoringRouteBuilder.AppendEdgeGraph(
                    context,
                    m_SourceEdge,
                    "conditionRuleGraph",
                    m_SourceEdge.GUID,
                    m_RuntimeGraph,
                    ownership);
                m_RuntimeGraph.InitTree(context.User, context, route);
            }

            return true;
        }
    }
}
