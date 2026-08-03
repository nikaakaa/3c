using System;
using System.Linq;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentGraphNodeMutationHandler : IAgentMutationHandler
    {
        readonly BtsmtlGraphAuthoringCapabilities m_Catalog;

        public AgentGraphNodeMutationHandler(
            BtsmtlGraphAuthoringCapabilities catalog)
        {
            m_Catalog = catalog;
        }

        public bool Preflight(AgentMutationSession session, AgentMutation command)
        {
            if (command is AgentDeleteGraphNodeMutation delete)
                return PreflightDelete(session, delete);
            AgentEnsureGraphNodeMutation ensure = command as AgentEnsureGraphNodeMutation ??
                                                  throw new InvalidOperationException($"Unsupported GraphNode command: {command.Kind}");
            if (!session.TryResolveGraph(ensure.Graph, ensure.Path, out BaseTree graph))
                return false;
            if (!m_Catalog.TryResolveNodeType(ensure.NodeType, out Type nodeType))
            {
                session.Report.Error(ensure.Path, "unknown_node_type", $"节点类型未登记：{ensure.NodeType}");
                return false;
            }
            if (graph != null)
            {
                if (!session.TryResolveOptionalNode(graph, ensure.Existing, ensure.Path, out BaseNode existing))
                    return false;
                if (existing == null && !graph.CanCreateNodeType(nodeType))
                {
                    session.Report.Error(ensure.Path, "node_type_rejected", $"{graph.GetType().Name}不能创建{nodeType.Name}。");
                    return false;
                }
            }
            session.AddPlanned(ensure, graph, ensure.DisplayName, "create or update graph node");
            return true;
        }

        public void Apply(AgentMutationSession session, AgentMutation command)
        {
            if (command is AgentDeleteGraphNodeMutation delete)
            {
                ApplyDelete(session, delete);
                return;
            }
            AgentEnsureGraphNodeMutation ensure = command as AgentEnsureGraphNodeMutation ??
                                                  throw new InvalidOperationException($"Unsupported GraphNode command: {command.Kind}");
            if (!session.TryResolveGraph(ensure.Graph, ensure.Path, out BaseTree graph) ||
                !session.TryResolveOptionalNode(graph, ensure.Existing, ensure.Path, out BaseNode node))
                return;
            if (node == null &&
                !m_Catalog.TryCreateNode(graph, ensure.NodeType, ensure.DisplayName, ensure.Position, out node, session.Report, ensure.Path))
                return;
            node.DisplayName = ensure.DisplayName;
            node.Position = ensure.Position;
            if (node is LoopNode loop)
                loop.ConfigureAuthoring(ensure.LoopStopType);
            if (node is CompareNode compare)
                compare.ConfigureAuthoring(ensure.CompareType);
            if (!session.RefreshIndex(ensure.Path))
                return;
            session.AddApplied(ensure, graph, node, "graph node");
        }

        static bool PreflightDelete(AgentMutationSession session, AgentDeleteGraphNodeMutation command)
        {
            if (!session.TryResolveGraph(command.Graph, command.Path, out BaseTree graph))
                return false;
            if (graph != null && !session.TryResolveNode(graph, command.Element, command.Path, out _))
                return false;
            session.AddPlanned(command, graph, command.Element.Identity, "delete graph node");
            return true;
        }

        static void ApplyDelete(AgentMutationSession session, AgentDeleteGraphNodeMutation command)
        {
            if (!session.TryResolveGraph(command.Graph, command.Path, out BaseTree graph) ||
                !session.TryResolveNode(graph, command.Element, command.Path, out BaseNode node))
                return;
            foreach (PropertyEdge edge in graph.PropertyEdges.Where(value => value.StartNode == node || value.EndNode == node).ToList())
                graph.UnLinkProperty(edge);
            foreach (BaseEdge edge in graph.Edges.Where(value => value.StartNode == node || value.EndNode == node).ToList())
                graph.UnLink(edge);
            graph.DeleteNode(node);
            if (!session.RefreshIndex(command.Path))
                return;
            session.AddAppliedWithoutIdentity(command, graph, command.Element.Identity, "deleted");
        }
    }
}
