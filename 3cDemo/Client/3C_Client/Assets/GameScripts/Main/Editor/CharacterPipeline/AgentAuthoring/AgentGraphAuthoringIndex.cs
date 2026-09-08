using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline;
using FlowCanvas;
using FlowCanvas.Macros;
using ThirdPersonCharacter.Control.Authoring;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentGraphAuthoringIndex
    {
        readonly Dictionary<string, BaseTree> m_Graphs = new Dictionary<string, BaseTree>(StringComparer.Ordinal);
        readonly Dictionary<BaseGraph, string> m_GraphPaths = new Dictionary<BaseGraph, string>();
        readonly Dictionary<string, FlowGraph> m_SkillGraphs = new Dictionary<string, FlowGraph>(StringComparer.Ordinal);
        readonly Dictionary<FlowGraph, string> m_SkillGraphPaths = new Dictionary<FlowGraph, string>();

        public void Rebuild(BaseTree root)
        {
            m_Graphs.Clear();
            m_GraphPaths.Clear();
            m_SkillGraphs.Clear();
            m_SkillGraphPaths.Clear();
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

        public void RebuildSkills(CharacterPipelineDefinition definition)
        {
            m_SkillGraphs.Clear();
            m_SkillGraphPaths.Clear();
            if (!definition)
                return;
            var runtime = new AgentSkillFlowDocumentRuntimeIndex();
            runtime.Build(definition);
            foreach (KeyValuePair<string, FlowGraph> pair in runtime.Graphs)
            {
                if (m_SkillGraphs.TryGetValue(pair.Key, out FlowGraph existing) && existing != pair.Value)
                    throw new InvalidOperationException($"Duplicate Skill Graph identity: {pair.Key}.");
                m_SkillGraphs[pair.Key] = pair.Value;
                m_SkillGraphPaths[pair.Value] = "skill/graph:" + pair.Key;
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

        public bool TryGetSkillGraph(string key, out FlowGraph graph)
        {
            graph = null;
            return !string.IsNullOrEmpty(key) && m_SkillGraphs.TryGetValue(key, out graph);
        }

        public string GetSkillGraphPath(FlowGraph graph)
        {
            return graph != null && m_SkillGraphPaths.TryGetValue(graph, out string path) ? path : string.Empty;
        }

        public bool TryFindSkillNode(FlowGraph graph, string key, out FlowNode node)
        {
            node = null;
            if (graph == null || string.IsNullOrEmpty(key))
                return false;
            node = graph.allNodes.OfType<FlowNode>().FirstOrDefault(value => value.UID == key);
            return node != null;
        }

        public bool TryFindSkillConnection(FlowGraph graph, string key, out BinderConnection connection)
        {
            connection = null;
            if (graph == null || string.IsNullOrEmpty(key))
                return false;
            connection = graph.allNodes.OfType<FlowNode>()
                .SelectMany(value => value.outConnections.OfType<BinderConnection>())
                .FirstOrDefault(value => value.UID == key);
            return connection != null;
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
