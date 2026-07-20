using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Graph;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentGraphAuthoringIndex
    {
        readonly Dictionary<string, BaseTree> m_Graphs = new Dictionary<string, BaseTree>(StringComparer.Ordinal);
        readonly Dictionary<BaseGraph, string> m_GraphPaths = new Dictionary<BaseGraph, string>();

        public void Rebuild(BaseTree root)
        {
            m_Graphs.Clear();
            m_GraphPaths.Clear();
            if (root == null)
                return;

            var errors = new List<string>();
            CharacterAuthoringTopologyProjection projection = CharacterAuthoringTopologyProjection.Build(root, errors);
            if (!projection.IsValid)
                throw new InvalidOperationException(string.Join("\n", errors));

            for (int i = 0; i < projection.Graphs.Count; i++)
            {
                CharacterAuthoringGraphEntry entry = projection.Graphs[i];
                BaseTree graph = entry.Graph;
                if (m_GraphPaths.ContainsKey(graph))
                    continue;
                if (string.IsNullOrEmpty(graph.GraphAuthoringId))
                    throw new InvalidOperationException($"Graph at '{entry.Route}' has no GraphAuthoringId.");
                if (m_Graphs.TryGetValue(graph.GraphAuthoringId, out BaseTree existing) && existing != graph)
                    throw new InvalidOperationException($"Duplicate GraphAuthoringId: {graph.GraphAuthoringId}.");
                m_Graphs[graph.GraphAuthoringId] = graph;
                m_GraphPaths[graph] = entry.Route.Count == 0 ? "root" : entry.Route.ToString();
            }
        }

        public bool TryGetGraph(string key, out BaseTree graph)
        {
            graph = null;
            return !string.IsNullOrEmpty(key) && m_Graphs.TryGetValue(key, out graph);
        }

        public string GetGraphPath(BaseGraph graph)
        {
            return graph != null && m_GraphPaths.TryGetValue(graph, out string path) ? path : string.Empty;
        }

        public bool TryFindNode(BaseGraph graph, string key, out BaseNode node)
        {
            node = null;
            if (graph == null || string.IsNullOrEmpty(key))
                return false;

            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                BaseNode candidate = graph.Nodes[i];
                if (candidate == null)
                    continue;

                if (string.Equals(candidate.GUID, key, StringComparison.Ordinal))
                {
                    node = candidate;
                    return true;
                }
            }
            return false;
        }

        public bool TryFindState(StateMachineGraph graph, string stateAuthoringId, out StateNode state)
        {
            state = null;
            if (graph == null || string.IsNullOrEmpty(stateAuthoringId))
                return false;

            foreach (StateNode candidate in graph.StateNodes)
            {
                if (candidate == null)
                    continue;

                if (string.Equals(candidate.GUID, stateAuthoringId, StringComparison.Ordinal))
                {
                    state = candidate;
                    return true;
                }
            }
            return false;
        }

        public bool TryFindStateMachineGraph(string key, out StateMachineGraph graph)
        {
            graph = null;
            if (TryGetGraph(key, out BaseTree direct) && direct is StateMachineGraph directGraph)
            {
                graph = directGraph;
                return true;
            }
            return false;
        }

        public bool TryFindControlNode(StateMachineGraph graph, string key, out BaseNode node)
        {
            node = null;
            if (graph == null || string.IsNullOrEmpty(key))
                return false;

            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                BaseNode candidate = graph.Nodes[i];
                if (candidate != null &&
                    candidate is StateMachineEnterNode or StateMachineAnyStateNode or StateMachineExitNode &&
                    string.Equals(candidate.GUID, key, StringComparison.Ordinal))
                {
                    node = candidate;
                    return true;
                }
            }
            return false;
        }

        public StateBehaviorSubTree GetStateBehaviorTree(StateNode state)
        {
            return state != null ? state.SubTree as StateBehaviorSubTree : null;
        }

    }
}
