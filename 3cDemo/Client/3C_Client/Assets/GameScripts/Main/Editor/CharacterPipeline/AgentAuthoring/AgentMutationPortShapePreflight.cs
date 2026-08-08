using System;
using System.Collections.Generic;
using System.Linq;
using TreeDesigner.Editor;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public static class AgentMutationPortShapePreflight
    {
        static readonly BtsmtlGraphAuthoringCapabilities s_Capabilities =
            new BtsmtlGraphAuthoringCapabilities();

        public static bool Validate(
            AgentGraphSnapshot snapshot,
            AgentMutationPlan plan,
            AgentCompileReport report)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            var graphs = (snapshot.graphs ?? new List<AgentSnapshotGraph>())
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.graphAuthoringId))
                .GroupBy(value => value.graphAuthoringId, StringComparer.Ordinal)
                .Where(value => value.Count() == 1)
                .ToDictionary(value => value.Key, value => value.Single(), StringComparer.Ordinal);
            bool valid = true;
            for (int commandIndex = 0; commandIndex < plan.Commands.Count; commandIndex++)
            {
                if (plan.Commands[commandIndex] is not AgentEnsureExposedPropertyNodeMutation command ||
                    string.IsNullOrEmpty(command.ElementAuthoringId) ||
                    !graphs.TryGetValue(command.Graph.Identity, out AgentSnapshotGraph graph))
                    continue;

                AgentSnapshotNode current = (graph.nodes ?? new List<AgentSnapshotNode>())
                    .SingleOrDefault(value => value != null &&
                        string.Equals(value.elementAuthoringId, command.ElementAuthoringId, StringComparison.Ordinal));
                if (current == null)
                    continue;
                AgentSnapshotNode target = AgentAuthoringDocumentCodec.Clone(current);
                target.exposedProperty ??= new AgentSnapshotExposedProperty();
                target.exposedProperty.mode = command.Mode.ToString();
                string path = command.Path + ".portShape";
                if (!TrySignature(current, path, report, out string currentShape) ||
                    !TrySignature(target, path, report, out string targetShape))
                {
                    valid = false;
                    continue;
                }
                if (string.Equals(currentShape, targetShape, StringComparison.Ordinal))
                    continue;

                foreach (AgentSnapshotFlowEdge edge in graph.flowEdges ?? new List<AgentSnapshotFlowEdge>())
                {
                    if (Touches(edge.startElementAuthoringId, edge.endElementAuthoringId, command.ElementAuthoringId))
                        valid &= RequireFlowDelete(plan, commandIndex, command.Graph.Identity, edge.elementAuthoringId, path, report);
                }
                foreach (AgentSnapshotPropertyEdge edge in graph.propertyEdges ?? new List<AgentSnapshotPropertyEdge>())
                {
                    if (Touches(edge.startElementAuthoringId, edge.endElementAuthoringId, command.ElementAuthoringId))
                        valid &= RequirePropertyDelete(plan, commandIndex, command.Graph.Identity, edge.elementAuthoringId, path, report);
                }
                for (int linkIndex = 0; linkIndex < plan.Commands.Count; linkIndex++)
                {
                    AgentMutation candidate = plan.Commands[linkIndex];
                    if (candidate is AgentLinkFlowMutation flow &&
                        string.Equals(flow.Graph.Identity, command.Graph.Identity, StringComparison.Ordinal) &&
                        Touches(flow.Source.Identity, flow.Target.Identity, command.ElementAuthoringId))
                    {
                        valid &= ValidateLinkOrder(linkIndex, commandIndex, flow.Path, report);
                        valid &= ValidateTargetPort(target, flow.Source.Identity, command.ElementAuthoringId, flow.StartPort, false, GraphAuthoringPortDirection.Output, flow.Path, report);
                        valid &= ValidateTargetPort(target, flow.Target.Identity, command.ElementAuthoringId, flow.EndPort, false, GraphAuthoringPortDirection.Input, flow.Path, report);
                    }
                    else if (candidate is AgentLinkPropertyMutation property &&
                             string.Equals(property.Graph.Identity, command.Graph.Identity, StringComparison.Ordinal) &&
                             Touches(property.Source.Identity, property.Target.Identity, command.ElementAuthoringId))
                    {
                        valid &= ValidateLinkOrder(linkIndex, commandIndex, property.Path, report);
                        valid &= ValidateTargetPort(target, property.Source.Identity, command.ElementAuthoringId, property.StartPropertyPort, true, GraphAuthoringPortDirection.Output, property.Path, report);
                        valid &= ValidateTargetPort(target, property.Target.Identity, command.ElementAuthoringId, property.EndPropertyPort, true, GraphAuthoringPortDirection.Input, property.Path, report);
                    }
                }
            }
            return valid;
        }

        static bool TrySignature(
            AgentSnapshotNode node,
            string path,
            AgentCompileReport report,
            out string signature)
        {
            signature = string.Empty;
            if (!s_Capabilities.TryProjectSnapshotPortShape(
                    node,
                    out GraphAuthoringCapabilityDescriptor capability,
                    out IReadOnlyList<GraphAuthoringDynamicPortProjection> projected,
                    out GraphAuthoringPortShapeException error))
            {
                report.Error(path, error.Code, error.Message);
                return false;
            }
            signature = string.Join("|", capability.FixedPorts
                .Select(PortSignature)
                .Concat(projected.Select(PortSignature))
                .OrderBy(value => value, StringComparer.Ordinal));
            return true;
        }

        static string PortSignature(GraphAuthoringPortDescriptor value) =>
            $"{value.PortId}:{value.ValueTypeId}:{value.Direction}:{value.Capacity}:{value.Required}";

        static string PortSignature(GraphAuthoringDynamicPortProjection value) =>
            $"{value.PortId}:{value.ValueTypeId}:{value.Direction}:{value.Capacity}:{value.Required}";

        static bool RequireFlowDelete(
            AgentMutationPlan plan,
            int configureIndex,
            string graphId,
            string edgeId,
            string path,
            AgentCompileReport report)
        {
            int index = FindCommand(plan, value => value is AgentDeleteFlowEdgeMutation delete &&
                string.Equals(delete.Graph.Identity, graphId, StringComparison.Ordinal) &&
                string.Equals(delete.EdgeAuthoringId, edgeId, StringComparison.Ordinal));
            return ValidateDeleteOrder(index, configureIndex, edgeId, path, report);
        }

        static bool RequirePropertyDelete(
            AgentMutationPlan plan,
            int configureIndex,
            string graphId,
            string edgeId,
            string path,
            AgentCompileReport report)
        {
            int index = FindCommand(plan, value => value is AgentDeletePropertyEdgeMutation delete &&
                string.Equals(delete.Graph.Identity, graphId, StringComparison.Ordinal) &&
                string.Equals(delete.EdgeAuthoringId, edgeId, StringComparison.Ordinal));
            return ValidateDeleteOrder(index, configureIndex, edgeId, path, report);
        }

        static int FindCommand(AgentMutationPlan plan, Func<AgentMutation, bool> predicate)
        {
            for (int index = 0; index < plan.Commands.Count; index++)
            {
                if (predicate(plan.Commands[index]))
                    return index;
            }
            return -1;
        }

        static bool ValidateDeleteOrder(
            int deleteIndex,
            int configureIndex,
            string edgeId,
            string path,
            AgentCompileReport report)
        {
            if (deleteIndex >= 0 && deleteIndex < configureIndex)
                return true;
            report.Error(
                path,
                deleteIndex < 0 ? "port_shape_edge_delete_missing" : "port_shape_edge_delete_order_invalid",
                $"端口形状变化前必须先删除关联 edge：{edgeId}");
            return false;
        }

        static bool ValidateLinkOrder(
            int linkIndex,
            int configureIndex,
            string path,
            AgentCompileReport report)
        {
            if (linkIndex > configureIndex)
                return true;
            report.Error(path, "port_shape_edge_link_order_invalid", "目标 edge 必须在节点端口形状配置完成后建立。");
            return false;
        }

        static bool ValidateTargetPort(
            AgentSnapshotNode target,
            string endpointNodeId,
            string targetNodeId,
            string port,
            bool property,
            GraphAuthoringPortDirection direction,
            string path,
            AgentCompileReport report)
        {
            if (!string.Equals(endpointNodeId, targetNodeId, StringComparison.Ordinal))
                return true;
            if (!s_Capabilities.TryProjectSnapshotPortShape(
                    target,
                    out GraphAuthoringCapabilityDescriptor capability,
                    out IReadOnlyList<GraphAuthoringDynamicPortProjection> projected,
                    out GraphAuthoringPortShapeException error))
            {
                report.Error(path, error.Code, error.Message);
                return false;
            }
            GraphAuthoringPortId portId = property
                ? BtsmtlSharedGraphPort.Property(port)
                : BtsmtlSharedGraphPort.Flow(port);
            bool matches = capability.FixedPorts.Any(value =>
                               value.PortId.Equals(portId) &&
                               value.Direction == direction) ||
                           projected.Any(value =>
                               value.PortId.Equals(portId) &&
                               value.Direction == direction);
            if (matches)
                return true;
            report.Error(
                path,
                "port_shape_target_link_invalid",
                $"目标节点 '{targetNodeId}' 的端口 '{portId}' 不接受 {direction} endpoint。");
            return false;
        }

        static bool Touches(string start, string end, string nodeId) =>
            string.Equals(start, nodeId, StringComparison.Ordinal) ||
            string.Equals(end, nodeId, StringComparison.Ordinal);
    }
}
