using UnityEngine;

namespace TreeDesigner
{
    public sealed class TransitionRuleGraphRuntime
    {
        readonly TransitionRuleGraph m_SourceGraph;
        TransitionRuleGraph m_RuntimeGraph;

        public TransitionRuleGraphRuntime(TransitionRuleGraph sourceGraph)
        {
            m_SourceGraph = sourceGraph;
        }

        public bool Evaluate(BaseGraph context)
        {
            if (!EnsureRuntimeGraph(context))
                return false;

            m_RuntimeGraph.SetDeltaTime(context?.DeltaTime ?? 0f);
            return m_RuntimeGraph.Evaluate();
        }

        public void Dispose()
        {
            if (m_RuntimeGraph)
                m_RuntimeGraph.DisposeTree();

            m_RuntimeGraph = null;
        }

        bool EnsureRuntimeGraph(BaseGraph context)
        {
            if (!m_SourceGraph)
                return false;

            if (!m_RuntimeGraph)
            {
                m_RuntimeGraph = Application.isPlaying
                    ? Object.Instantiate(m_SourceGraph)
                    : m_SourceGraph;
            }

            if (!m_RuntimeGraph.IsValid)
                m_RuntimeGraph.InitTree(context?.User);

            return true;
        }
    }
}
