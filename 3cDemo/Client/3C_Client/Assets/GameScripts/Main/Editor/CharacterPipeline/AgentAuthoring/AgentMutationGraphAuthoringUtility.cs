using System;
using System.Linq;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentMutationGraphAuthoringUtility
    {
        internal static BaseEdge FindFlowEdge(
            BaseTree graph,
            BaseNode source,
            BaseNode target,
            string startPort,
            string endPort)
        {
            if (graph == null || source == null || target == null)
                return null;
            return graph.Edges.FirstOrDefault(edge =>
                edge != null &&
                (edge.StartNode == source || edge.StartNodeGUID == source.GUID) &&
                (edge.EndNode == target || edge.EndNodeGUID == target.GUID) &&
                string.Equals(edge.StartPortName, startPort, StringComparison.Ordinal) &&
                string.Equals(edge.EndPortName, endPort, StringComparison.Ordinal));
        }

        internal static BaseNode ResolveLifecycleAnchor(
            BaseTree graph,
            string lifecycleSlot)
        {
            if (graph == null)
                return null;
            string slot = lifecycleSlot?.Trim() ?? string.Empty;
            if (graph is StateBehaviorSubTree stateBehavior)
            {
                if (string.Equals(slot, "OnEnter", StringComparison.Ordinal))
                    return stateBehavior.OnEnter ?? graph.Nodes.OfType<StateOnEnterNode>().FirstOrDefault();
                if (string.Equals(slot, "OnExit", StringComparison.Ordinal))
                    return stateBehavior.OnExit ?? graph.Nodes.OfType<StateOnExitNode>().FirstOrDefault();
            }
            if (string.Equals(slot, "Root", StringComparison.Ordinal))
                return graph.Nodes.OfType<RootNode>().FirstOrDefault();
            return graph.Nodes.FirstOrDefault(node =>
                node != null &&
                string.Equals(node.ResolvedDisplayName, slot, StringComparison.Ordinal));
        }

        internal static void RemoveOrphanLinks(BaseTree graph)
        {
            if (graph == null)
                return;
            foreach (BaseEdge edge in graph.Edges
                         .Where(value => value == null || value.StartNode == null || value.EndNode == null)
                         .ToArray())
            {
                if (edge != null)
                    graph.UnLink(edge);
            }
            foreach (PropertyEdge edge in graph.PropertyEdges
                         .Where(value => value == null || value.StartNode == null || value.EndNode == null)
                         .ToArray())
            {
                if (edge != null)
                    graph.UnLinkProperty(edge);
            }
        }

        internal static void TryLinkLifecycleSlot(
            AgentMutationSession session,
            BaseTree graph,
            string lifecycleSlot,
            BaseNode node,
            string path)
        {
            if (string.IsNullOrWhiteSpace(lifecycleSlot) || graph == null || node == null)
                return;
            BaseNode anchor = ResolveLifecycleAnchor(graph, lifecycleSlot);
            if (anchor == null)
            {
                session.Report.Error(path, "lifecycle_anchor_not_found", $"Lifecycle入口无法解析：{lifecycleSlot}");
                return;
            }
            if (FindFlowEdge(graph, anchor, node, "Output", "Input") != null)
                return;
            foreach (BaseEdge existing in graph.Edges
                         .Where(value => value != null && value.EndNode == node && value.EndPortName == "Input")
                         .ToArray())
                graph.UnLink(existing);
            if (graph.Link(anchor, node, "Output", "Input") == null)
            {
                session.Report.Error(path, "lifecycle_link_failed", $"Lifecycle入口无法连接节点：{lifecycleSlot} -> {node.ResolvedDisplayName}");
                return;
            }
            if (anchor is CompositeNode composite)
                composite.OrderChildren();
        }
    }
}
