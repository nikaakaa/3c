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
                new RuntimeTracePayload { Name = graph.name, Status = GraphStatusText(kind) });
        }

        public static void PublishNode(
            RunnableNode node,
            RuntimeTraceEventKind kind,
            State status,
            NodeStopContext stopContext = default)
        {
            PublishNode(node, kind, StatusText(status), stopContext);
        }

        public static void PublishNode(
            RunnableNode node,
            RuntimeTraceEventKind kind,
            NodeStopStatus status,
            NodeStopContext stopContext = default)
        {
            PublishNode(node, kind, StatusText(status), stopContext);
        }

        static void PublishNode(
            RunnableNode node,
            RuntimeTraceEventKind kind,
            string status,
            NodeStopContext stopContext)
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
                    Cause = IsNodeStop(kind) ? CauseText(stopContext.OriginCause) : string.Empty,
                    RelatedElementId = stopContext.ReplacementNodeGuid,
                    Detail = stopContext.SourceNodeGuid
                });
        }

        static string StatusText(State status)
        {
            return status switch
            {
                State.None => "None",
                State.Running => "Running",
                State.Success => "Success",
                State.Failure => "Failure",
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Node state has no diagnostics status.")
            };
        }

        static string StatusText(NodeStopStatus status)
        {
            return status switch
            {
                NodeStopStatus.Running => "Running",
                NodeStopStatus.Completed => "Completed",
                NodeStopStatus.Failed => "Failed",
                _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Node stop status has no diagnostics status.")
            };
        }

        static bool IsNodeStop(RuntimeTraceEventKind kind)
        {
            return kind == RuntimeTraceEventKind.NodeStopRequested ||
                   kind == RuntimeTraceEventKind.NodeStopping ||
                   kind == RuntimeTraceEventKind.NodeStopped ||
                   kind == RuntimeTraceEventKind.NodeForceStopped;
        }

        static string CauseText(NodeStopOriginCause cause)
        {
            return cause switch
            {
                NodeStopOriginCause.SelfAbort => "SelfAbort",
                NodeStopOriginCause.LowerPriorityAbort => "LowerPriorityAbort",
                NodeStopOriginCause.ExplicitParentStop => "ExplicitParentStop",
                NodeStopOriginCause.StateTransition => "StateTransition",
                NodeStopOriginCause.Reset => "Reset",
                NodeStopOriginCause.Shutdown => "Shutdown",
                _ => throw new ArgumentOutOfRangeException(nameof(cause), cause, "Node stop cause has no diagnostics text.")
            };
        }

        static string CauseText(StateExitCause cause)
        {
            return cause switch
            {
                StateExitCause.StateTransition => "StateTransition",
                StateExitCause.TreeSelfAbort => "TreeSelfAbort",
                StateExitCause.TreeLowerPriorityAbort => "TreeLowerPriorityAbort",
                StateExitCause.TreeParentStop => "TreeParentStop",
                _ => throw new ArgumentOutOfRangeException(nameof(cause), cause, "State exit cause has no diagnostics text.")
            };
        }

        static string GraphStatusText(RuntimeTraceEventKind kind)
        {
            return kind switch
            {
                RuntimeTraceEventKind.GraphCreated => "GraphCreated",
                RuntimeTraceEventKind.GraphDestroyed => "GraphDestroyed",
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Graph lifecycle has no diagnostics status.")
            };
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
                    Detail = string.IsNullOrEmpty(detail) ? edge.DiagnosticDetail : detail,
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
            NodeStopOriginCause cause,
            string status)
        {
            PublishState(graph, graphRuntimeId, stateId, generation, kind, relatedStateId, CauseText(cause), status);
        }

        public static void PublishState(
            BaseGraph graph,
            Guid graphRuntimeId,
            string stateId,
            ulong generation,
            RuntimeTraceEventKind kind,
            string relatedStateId,
            StateExitCause cause,
            string status)
        {
            PublishState(graph, graphRuntimeId, stateId, generation, kind, relatedStateId, CauseText(cause), status);
        }

        static void PublishState(
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
                    Detail = string.IsNullOrEmpty(detail) ? edge.DiagnosticDetail : detail,
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
