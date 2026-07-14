using System;
using BTSMTL.Diagnostics;

namespace TreeDesigner
{
    public static class TreeRuntimeDiagnostics
    {
        public static void PublishGraph(BaseGraph graph, RuntimeTraceEventKind kind)
        {
            if (!TryGet(graph, RuntimeTraceChannel.Graph, kind, out RuntimeDiagnosticsContext diagnostics))
                return;
            diagnostics.Publish(
                RuntimeTraceChannel.Graph,
                RuntimeTraceDomain.Lifecycle,
                kind,
                RuntimeSourceElementKey.Graph(graph.GraphAuthoringId),
                RuntimeInstanceKey.Graph(diagnostics.CharacterRuntimeId, graph.RuntimeId),
                new RuntimeTracePayload { Name = graph.name, Status = kind.ToString() });
        }

        public static void PublishNode(
            RunnableNode node,
            RuntimeTraceEventKind kind,
            string status,
            NodeStopContext stopContext = default)
        {
            BaseGraph graph = node?.Owner;
            if (!TryGet(graph, RuntimeTraceChannel.Graph, kind, out RuntimeDiagnosticsContext diagnostics))
                return;
            diagnostics.Publish(
                RuntimeTraceChannel.Graph,
                RuntimeTraceDomain.Logic,
                kind,
                RuntimeSourceElementKey.Node(graph.GraphAuthoringId, node.GUID),
                ResolveInstance(graph, diagnostics),
                new RuntimeTracePayload
                {
                    Name = node.ResolvedDisplayName,
                    Status = status,
                    Cause = kind == RuntimeTraceEventKind.NodeStopRequested ||
                            kind == RuntimeTraceEventKind.NodeStopping ||
                            kind == RuntimeTraceEventKind.NodeStopped ||
                            kind == RuntimeTraceEventKind.NodeForceStopped
                        ? stopContext.OriginCause.ToString()
                        : string.Empty,
                    RelatedElementId = stopContext.ReplacementNodeGuid,
                    Detail = stopContext.SourceNodeGuid
                });
        }

        public static void PublishEdge(
            BaseGraph graph,
            BaseEdge edge,
            RuntimeTraceEventKind kind,
            bool result,
            string status = null,
            string detail = null)
        {
            if (edge == null || !TryGet(graph, RuntimeTraceChannel.Graph, kind, out RuntimeDiagnosticsContext diagnostics))
                return;
            diagnostics.Publish(
                RuntimeTraceChannel.Graph,
                RuntimeTraceDomain.Logic,
                kind,
                RuntimeSourceElementKey.Edge(graph.GraphAuthoringId, edge.GUID),
                ResolveInstance(graph, diagnostics),
                new RuntimeTracePayload
                {
                    Status = string.IsNullOrEmpty(status) ? result ? "Passed" : "Failed" : status,
                    Flag = result,
                    Detail = string.IsNullOrEmpty(detail) ? $"{edge.StartNodeGUID}->{edge.EndNodeGUID}" : detail,
                    RelatedElementId = edge.EndNodeGUID,
                    Priority = edge.TransitionPriority
                });
        }

        public static void PublishInvalidConditionEdge(BaseGraph graph, BaseEdge edge, string error)
        {
            PublishEdge(
                graph,
                edge,
                RuntimeTraceEventKind.EdgeEvaluated,
                false,
                "InvalidConditionRuleGraph",
                $"owner={graph?.name}/{graph?.GraphAuthoringId} edge={edge?.GUID} ownership={edge?.ConditionRuleGraphOwnership} reason={error}");
        }

        public static void PublishConditionGraph(BaseGraph graph, bool result)
        {
            if (!TryGet(graph, RuntimeTraceChannel.Graph, RuntimeTraceEventKind.ConditionGraphEvaluated, out RuntimeDiagnosticsContext diagnostics))
                return;
            diagnostics.Publish(
                RuntimeTraceChannel.Graph,
                RuntimeTraceDomain.Logic,
                RuntimeTraceEventKind.ConditionGraphEvaluated,
                RuntimeSourceElementKey.Graph(graph.GraphAuthoringId),
                ResolveInstance(graph, diagnostics),
                new RuntimeTracePayload { Status = result ? "Passed" : "Failed", Flag = result, Name = graph.name });
        }

        public static void PublishState(
            BaseGraph graph,
            Guid graphRuntimeId,
            string stateId,
            ulong generation,
            RuntimeTraceEventKind kind,
            string relatedStateId,
            string cause,
            string status)
        {
            if (!TryGet(graph, RuntimeTraceChannel.StateMachine, kind, out RuntimeDiagnosticsContext diagnostics) || string.IsNullOrEmpty(stateId))
                return;
            diagnostics.Publish(
                RuntimeTraceChannel.StateMachine,
                RuntimeTraceDomain.Logic,
                kind,
                RuntimeSourceElementKey.Node(graph.GraphAuthoringId, stateId),
                RuntimeInstanceKey.State(diagnostics.CharacterRuntimeId, graphRuntimeId, stateId, generation),
                new RuntimeTracePayload
                {
                    Status = status,
                    Cause = cause,
                    RelatedElementId = relatedStateId
                });
        }

        public static void PublishStateTransition(
            BaseGraph graph,
            Guid graphRuntimeId,
            StateMachineExecutionScope scope,
            BaseEdge edge,
            RuntimeTraceEventKind kind,
            bool result,
            string status = null,
            string detail = null)
        {
            if (edge == null || !TryGet(graph, RuntimeTraceChannel.StateMachine, kind, out RuntimeDiagnosticsContext diagnostics))
                return;

            RuntimeInstanceKey instance = scope.IsValid
                ? RuntimeInstanceKey.State(
                    diagnostics.CharacterRuntimeId,
                    graphRuntimeId,
                    scope.StateId,
                    scope.ActivationGeneration)
                : RuntimeInstanceKey.Graph(diagnostics.CharacterRuntimeId, graphRuntimeId);
            diagnostics.Publish(
                RuntimeTraceChannel.StateMachine,
                RuntimeTraceDomain.Logic,
                kind,
                RuntimeSourceElementKey.Edge(graph.GraphAuthoringId, edge.GUID),
                instance,
                new RuntimeTracePayload
                {
                    Status = string.IsNullOrEmpty(status) ? result ? "Passed" : "Failed" : status,
                    Flag = result,
                    Detail = string.IsNullOrEmpty(detail) ? $"{edge.StartNodeGUID}->{edge.EndNodeGUID}" : detail,
                    OwnerId = scope.IsValid ? $"{scope.StateId}/{scope.ActivationGeneration}" : string.Empty,
                    RelatedElementId = edge.EndNodeGUID,
                    Priority = edge.TransitionPriority
                });
        }

        static RuntimeInstanceKey ResolveInstance(BaseGraph graph, RuntimeDiagnosticsContext diagnostics)
        {
            RuntimeInstanceKey current = diagnostics.CurrentRuntimeInstance;
            return current.Kind == RuntimeInstanceKind.StateActivation || current.Kind == RuntimeInstanceKind.TreeClip
                ? current
                : RuntimeInstanceKey.Graph(diagnostics.CharacterRuntimeId, graph.RuntimeId);
        }

        static bool TryGet(BaseGraph graph, RuntimeTraceChannel channel, RuntimeTraceEventKind kind, out RuntimeDiagnosticsContext diagnostics)
        {
            diagnostics = (graph?.User as IRuntimeDiagnosticsContextSource)?.RuntimeDiagnostics;
            return diagnostics != null && diagnostics.ShouldPublish(channel, kind);
        }
    }
}
