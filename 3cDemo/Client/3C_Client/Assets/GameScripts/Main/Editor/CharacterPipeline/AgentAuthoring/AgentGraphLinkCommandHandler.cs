using System;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentGraphLinkCommandHandler : IAgentPatchCommandHandler
    {
        public bool Preflight(AgentPatchCompileSession session, AgentPatchCommand command)
        {
            if (command is AgentDeleteFlowEdgeCommand delete)
                return PreflightDeleteFlow(session, delete);
            if (command is not AgentGraphLinkCommand link)
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
            if (valid && command is AgentLinkPropertyCommand property && source != null && target != null &&
                (!source.PropertyPortMap.ContainsKey(property.StartPropertyPort) || !target.PropertyPortMap.ContainsKey(property.EndPropertyPort)))
            {
                session.Report.Error(property.Path, "property_port_not_found", $"property port 无法解析：{property.StartPropertyPort} -> {property.EndPropertyPort}");
                valid = false;
            }
            session.AddPlanned(command, graph, $"{link.Source.Identity}->{link.Target.Identity}", command.OperationName);
            return valid;
        }

        public void Apply(AgentPatchCompileSession session, AgentPatchCommand command)
        {
            switch (command)
            {
                case AgentDeleteFlowEdgeCommand delete:
                    ApplyDeleteFlow(session, delete);
                    break;
                case AgentLinkFlowCommand flow:
                    ApplyFlow(session, flow);
                    break;
                case AgentLinkPropertyCommand property:
                    ApplyProperty(session, property);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported GraphLink command: {command.Kind}");
            }
        }

        static bool PreflightDeleteFlow(AgentPatchCompileSession session, AgentDeleteFlowEdgeCommand command)
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

        static void ApplyDeleteFlow(AgentPatchCompileSession session, AgentDeleteFlowEdgeCommand command)
        {
            if (!session.TryResolveGraph(command.Graph, command.Path, out BaseTree graph) ||
                !TryFindFlowEdge(graph, command.EdgeAuthoringId, out BaseEdge edge))
            {
                session.Report.Error(command.Path, "flow_edge_not_found", $"flow edge 无法解析：{command.EdgeAuthoringId}");
                return;
            }
            graph.UnLink(edge);
            session.AddAppliedWithoutOutput(command, graph, command.EdgeAuthoringId, "deleted");
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

        static void ApplyFlow(AgentPatchCompileSession session, AgentLinkFlowCommand command)
        {
            if (!session.TryResolveGraph(command.Graph, command.Path, out BaseTree graph) ||
                !session.TryResolveNode(graph, command.Source, command.Path, out BaseNode source) ||
                !session.TryResolveNode(graph, command.Target, command.Path, out BaseNode target))
                return;
            BaseEdge edge = AgentPatchGraphAuthoringUtility.FindFlowEdge(graph, source, target, command.StartPort, command.EndPort) ??
                            graph.Link(source, target, command.StartPort, command.EndPort);
            if (edge == null)
            {
                session.Report.Error(command.Path, "flow_not_created", $"flow link 未创建：{source.ResolvedDisplayName} -> {target.ResolvedDisplayName}");
                return;
            }
            session.AddApplied(command, graph, edge, "flow");
        }

        static void ApplyProperty(AgentPatchCompileSession session, AgentLinkPropertyCommand command)
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
                session.AddAppliedWithoutOutput(command, graph, $"{source.ResolvedDisplayName}->{target.ResolvedDisplayName}", "property exists");
                return;
            }
            session.AddApplied(command, graph, edge, "property");
        }
    }
}
