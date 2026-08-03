using System;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentGraphLinkMutationHandler : IAgentMutationHandler
    {
        public bool Preflight(AgentMutationSession session, AgentMutation command)
        {
            if (command is AgentDeleteFlowEdgeMutation delete)
                return PreflightDeleteFlow(session, delete);
            if (command is AgentDeletePropertyEdgeMutation deleteProperty)
                return PreflightDeleteProperty(session, deleteProperty);
            if (command is not AgentGraphLinkMutation link)
                throw new InvalidOperationException($"Unsupported GraphLink command: {command.Kind}");
            if (!session.TryResolveGraph(link.Graph, link.Path, out BaseTree graph))
                return false;
            BaseNode source = null;
            BaseNode target = null;
            bool valid = true;
            if (graph != null)
            {
                valid &= session.TryResolveNode(graph, link.Source, link.Path, out source);
                valid &= session.TryResolveNode(graph, link.Target, link.Path, out target);
            }
            if (valid && command is AgentLinkPropertyMutation property && source != null && target != null &&
                (!source.PropertyPortMap.ContainsKey(property.StartPropertyPort) || !target.PropertyPortMap.ContainsKey(property.EndPropertyPort)))
            {
                session.Report.Error(property.Path, "property_port_not_found", $"property port 无法解析：{property.StartPropertyPort} -> {property.EndPropertyPort}");
                valid = false;
            }
            if (valid && command is AgentLinkFlowMutation flow && source is CompositeNode && target is not RunnableNode &&
                string.Equals(flow.StartPort, "Output", StringComparison.Ordinal) &&
                string.Equals(flow.EndPort, "Input", StringComparison.Ordinal))
            {
                session.Report.Error(flow.Path, "flow_target_not_runnable", $"Composite flow target 必须是 RunnableNode，当前为 {target.GetType().Name}。ValueNode 应放入边的 ConditionRuleGraph。 ");
                valid = false;
            }
            session.AddPlanned(command, graph, $"{link.Source.Identity}->{link.Target.Identity}", command.OperationName);
            return valid;
        }

        public void Apply(AgentMutationSession session, AgentMutation command)
        {
            switch (command)
            {
                case AgentDeleteFlowEdgeMutation delete:
                    ApplyDeleteFlow(session, delete);
                    break;
                case AgentDeletePropertyEdgeMutation deleteProperty:
                    ApplyDeleteProperty(session, deleteProperty);
                    break;
                case AgentLinkFlowMutation flow:
                    ApplyFlow(session, flow);
                    break;
                case AgentLinkPropertyMutation property:
                    ApplyProperty(session, property);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported GraphLink command: {command.Kind}");
            }
        }

        static bool PreflightDeleteFlow(AgentMutationSession session, AgentDeleteFlowEdgeMutation command)
        {
            if (!session.TryResolveGraph(command.Graph, command.Path, out BaseTree graph))
                return false;
            if (graph != null && !TryFindFlowEdge(graph, command.EdgeAuthoringId, out _))
            {
                session.Report.Error(command.Path, "flow_edge_not_found", $"flow edge 无法解析：{command.EdgeAuthoringId}");
                return false;
            }
            session.AddPlanned(command, graph, command.EdgeAuthoringId, "delete flow edge");
            return true;
        }

        static void ApplyDeleteFlow(AgentMutationSession session, AgentDeleteFlowEdgeMutation command)
        {
            if (!session.TryResolveGraph(command.Graph, command.Path, out BaseTree graph) ||
                !TryFindFlowEdge(graph, command.EdgeAuthoringId, out BaseEdge edge))
            {
                session.Report.Error(command.Path, "flow_edge_not_found", $"flow edge 无法解析：{command.EdgeAuthoringId}");
                return;
            }
            graph.UnLink(edge);
            session.AddAppliedWithoutIdentity(command, graph, command.EdgeAuthoringId, "deleted");
        }

        static bool TryFindFlowEdge(BaseTree graph, string edgeAuthoringId, out BaseEdge edge)
        {
            edge = null;
            if (graph == null || string.IsNullOrEmpty(edgeAuthoringId))
                return false;
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                BaseEdge candidate = graph.Edges[i];
                if (candidate != null && string.Equals(candidate.GUID, edgeAuthoringId, StringComparison.Ordinal))
                {
                    edge = candidate;
                    return true;
                }
            }
            return false;
        }

        static bool PreflightDeleteProperty(AgentMutationSession session, AgentDeletePropertyEdgeMutation command)
        {
            if (!session.TryResolveGraph(command.Graph, command.Path, out BaseTree graph))
                return false;
            if (graph != null && !TryFindPropertyEdge(graph, command.EdgeAuthoringId, out _))
            {
                session.Report.Error(command.Path, "property_edge_not_found", $"property edge 无法解析：{command.EdgeAuthoringId}");
                return false;
            }
            session.AddPlanned(command, graph, command.EdgeAuthoringId, "delete property edge");
            return true;
        }

        static void ApplyDeleteProperty(AgentMutationSession session, AgentDeletePropertyEdgeMutation command)
        {
            if (!session.TryResolveGraph(command.Graph, command.Path, out BaseTree graph) ||
                !TryFindPropertyEdge(graph, command.EdgeAuthoringId, out PropertyEdge edge))
            {
                session.Report.Error(command.Path, "property_edge_not_found", $"property edge 无法解析：{command.EdgeAuthoringId}");
                return;
            }
            graph.UnLinkProperty(edge);
            session.AddAppliedWithoutIdentity(command, graph, command.EdgeAuthoringId, "deleted");
        }

        static bool TryFindPropertyEdge(BaseTree graph, string edgeAuthoringId, out PropertyEdge edge)
        {
            edge = null;
            if (graph == null || string.IsNullOrEmpty(edgeAuthoringId))
                return false;
            for (int i = 0; i < graph.PropertyEdges.Count; i++)
            {
                PropertyEdge candidate = graph.PropertyEdges[i];
                if (candidate != null && string.Equals(candidate.GUID, edgeAuthoringId, StringComparison.Ordinal))
                {
                    edge = candidate;
                    return true;
                }
            }
            return false;
        }

        static void ApplyFlow(AgentMutationSession session, AgentLinkFlowMutation command)
        {
            if (!session.TryResolveGraph(command.Graph, command.Path, out BaseTree graph) ||
                !session.TryResolveNode(graph, command.Source, command.Path, out BaseNode source) ||
                !session.TryResolveNode(graph, command.Target, command.Path, out BaseNode target))
                return;
            BaseEdge edge = AgentMutationGraphAuthoringUtility.FindFlowEdge(graph, source, target, command.StartPort, command.EndPort) ??
                            graph.Link(source, target, command.StartPort, command.EndPort);
            if (edge == null)
            {
                session.Report.Error(command.Path, "flow_not_created", $"flow link 未创建：{source.ResolvedDisplayName} -> {target.ResolvedDisplayName}");
                return;
            }
            if (!string.IsNullOrEmpty(command.EdgeAuthoringId) &&
                !command.EdgeAuthoringId.StartsWith("local:", StringComparison.Ordinal))
                edge.SetAuthoringId(command.EdgeAuthoringId);
            session.AddApplied(command, graph, edge, "flow");
        }

        static void ApplyProperty(AgentMutationSession session, AgentLinkPropertyMutation command)
        {
            if (!session.TryResolveGraph(command.Graph, command.Path, out BaseTree graph) ||
                !session.TryResolveNode(graph, command.Source, command.Path, out BaseNode source) ||
                !session.TryResolveNode(graph, command.Target, command.Path, out BaseNode target))
                return;
            if (!source.PropertyPortMap.TryGetValue(command.StartPropertyPort, out PropertyPort startPort) ||
                !target.PropertyPortMap.TryGetValue(command.EndPropertyPort, out PropertyPort endPort))
            {
                session.Report.Error(command.Path, "property_port_not_found", $"property port 无法解析：{command.StartPropertyPort} -> {command.EndPropertyPort}");
                return;
            }
            PropertyEdge edge = graph.LinkProperty(source, target, startPort, endPort);
            if (edge == null)
            {
                session.Report.Info(command.Path, "property_already_linked", "property link 已存在。");
                session.AddAppliedWithoutIdentity(command, graph, $"{source.ResolvedDisplayName}->{target.ResolvedDisplayName}", "property exists");
                return;
            }
            if (!string.IsNullOrEmpty(command.EdgeAuthoringId) &&
                !command.EdgeAuthoringId.StartsWith("local:", StringComparison.Ordinal))
                edge.SetAuthoringId(command.EdgeAuthoringId);
            session.AddApplied(command, graph, edge, "property");
        }
    }

    public sealed class AgentBTConditionRuleMutationHandler : IAgentMutationHandler
    {
        readonly AgentConditionRuleBuilder m_Builder;

        public AgentBTConditionRuleMutationHandler(AgentConditionRuleBuilder builder)
        {
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public bool Preflight(AgentMutationSession session, AgentMutation command)
        {
            if (command is not AgentEnsureBTConditionRuleMutation condition)
                throw new InvalidOperationException($"Unsupported BT condition command: {command.Kind}");
            if (!session.TryResolveGraph(condition.Graph, condition.Path, out BaseTree graph))
                return false;
            if (graph != null && graph is StateMachineGraph)
            {
                session.Report.Error(condition.Path, "bt_condition_wrong_graph", "BT edge condition 不能配置到 StateMachineGraph。");
                return false;
            }
            BaseEdge edge = null;
            bool valid = graph == null || session.TryResolveFlowEdge(graph, condition.Edge, condition.Path, out edge);
            if (valid && edge != null && (edge.StartNode is not CompositeNode || edge.EndNode is not RunnableNode))
            {
                session.Report.Error(condition.Path, "bt_condition_wrong_edge", "ConditionRuleGraph 只能配置在 Composite 到 RunnableNode 的 flow edge。");
                valid = false;
            }
            valid &= m_Builder.Preflight(session, condition.Groups, condition.Path);
            session.AddPlanned(condition, graph, condition.Edge.Identity, condition.AbortPolicy.ToString());
            return valid;
        }

        public void Apply(AgentMutationSession session, AgentMutation command)
        {
            AgentEnsureBTConditionRuleMutation condition = command as AgentEnsureBTConditionRuleMutation ??
                throw new InvalidOperationException($"Unsupported BT condition command: {command.Kind}");
            if (!session.TryResolveGraph(condition.Graph, condition.Path, out BaseTree graph) ||
                !session.TryResolveFlowEdge(graph, condition.Edge, condition.Path, out BaseEdge edge))
                return;
            if (edge.StartNode is not CompositeNode || edge.EndNode is not RunnableNode)
            {
                session.Report.Error(condition.Path, "bt_condition_wrong_edge", "ConditionRuleGraph 只能配置在 Composite 到 RunnableNode 的 flow edge。");
                return;
            }
            if (!edge.HasConditionRuleGraphConfiguration)
                edge.SetConditionRuleGraph(ConditionRuleGraph.CreateDefaultGraph($"{edge.StartNode.ResolvedDisplayName} To {edge.EndNode.ResolvedDisplayName} Condition", graph.AuthoringRole));
            edge.AbortPolicy = condition.AbortPolicy;
            if (!m_Builder.BuildFlowRule(session, edge, condition.Groups, condition.Path))
                return;
            session.AddApplied(condition, graph, edge, condition.AbortPolicy.ToString());
        }
    }
}
