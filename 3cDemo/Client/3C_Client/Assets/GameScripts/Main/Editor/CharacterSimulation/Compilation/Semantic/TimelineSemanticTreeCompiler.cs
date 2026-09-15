using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonSimulation;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class TimelineSemanticTreeCompiler
    {
        readonly CharacterSimulationNodeEmitterRegistry m_NodeEmitters;
        readonly GameplayAbilitySemanticBuilder m_Builder;
        readonly SimulationCompileReport m_Report;
        readonly Dictionary<string, Dictionary<string, OperationHandle>> m_OperationsByRoute =
            new Dictionary<string, Dictionary<string, OperationHandle>>(StringComparer.Ordinal);
        readonly HashSet<string> m_ActiveGraphs = new HashSet<string>(StringComparer.Ordinal);

        public TimelineSemanticTreeCompiler(
            GameplayAbilitySemanticBuilder builder,
            SimulationCompileReport report)
        {
            m_NodeEmitters = CharacterSimulationNodeEmitterRegistry.CreateDefault();
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public TimelineSemanticTreeCompilation Compile(
            TimelineSemanticClipRecord record,
            OperationHandle stateScopeOwner)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            if (record.Clip is not TreeClip clip || clip.ResolvedTree == null)
            {
                m_Report.Error(
                    "timeline_tree_compile_missing",
                    record.Route,
                    "TreeClip does not contain a resolved TimelineRunningTree.");
                return default;
            }

            string route = $"{record.Route}/tree:{clip.ResolvedTree.GraphAuthoringId}";
            OperationHandle entry = CompileGraph(clip.ResolvedTree, route, stateScopeOwner);
            if (!entry.IsValid)
                return default;
            return new TimelineSemanticTreeCompilation(
                route,
                entry,
                port => ResolveLifecycle(route, clip.ResolvedTree, port));
        }

        OperationHandle CompileGraph(
            BaseTree graph,
            string route,
            OperationHandle stateScopeOwner)
        {
            if (graph == null)
                return OperationHandle.Invalid;
            if (!m_ActiveGraphs.Add(graph.GraphAuthoringId))
            {
                m_Report.Error(
                    "timeline_tree_recursive",
                    route,
                    $"Timeline Tree graph '{graph.GraphAuthoringId}' is recursive.");
                return OperationHandle.Invalid;
            }

            try
            {
                graph.RebindReadOnlyViewReferences();
                if (graph.ExposedProperties.Any(value => value != null))
                {
                    m_Report.Error(
                        "timeline_tree_blackboard_unsupported",
                        route,
                        "Independent Timeline TreeClip cannot bind Character Blackboard declarations.");
                    return OperationHandle.Invalid;
                }

                BaseNode[] nodes = graph.Nodes
                    .Where(value => value != null)
                    .OrderBy(value => value.GUID, StringComparer.Ordinal)
                    .ToArray();
                var operations = new Dictionary<string, OperationHandle>(StringComparer.Ordinal);
                for (int nodeIndex = 0; nodeIndex < nodes.Length; nodeIndex++)
                {
                    BaseNode node = nodes[nodeIndex];
                    if (node is ExposedPropertyNode || node is PipelineBlackboardValueInfoNode)
                    {
                        m_Report.Error(
                            "timeline_tree_blackboard_node_unsupported",
                            $"{route}/node:{node.GUID}",
                            "Independent Timeline TreeClip cannot execute Character Blackboard nodes.");
                        continue;
                    }
                    if (!m_NodeEmitters.TryGet(node.GetType(), out ICharacterSimulationNodeEmitter emitter))
                    {
                        m_Report.Error(
                            "timeline_tree_node_emitter_missing",
                            $"{route}/node:{node.GUID}",
                            $"Timeline Tree node '{node.GetType().FullName}' has no semantic emitter.");
                        continue;
                    }
                    try
                    {
                        operations.Add(
                            node.GUID,
                            emitter.Emit(
                                node,
                                new CharacterSimulationNodeEmitterContext(graph, route, m_Builder)));
                    }
                    catch (Exception exception)
                    {
                        m_Report.EmissionError(
                            "timeline_tree_node_emit_failed",
                            $"{route}/node:{node.GUID}",
                            exception.Message);
                    }
                }
                m_OperationsByRoute[route] = operations;

                BaseEdge[] edges = graph.Edges
                    .Where(value => value != null)
                    .OrderBy(value => value.GUID, StringComparer.Ordinal)
                    .ToArray();
                for (int edgeIndex = 0; edgeIndex < edges.Length; edgeIndex++)
                    CompileEdge(graph, edges[edgeIndex], route, operations, stateScopeOwner);
                PropertyEdge[] propertyEdges = graph.PropertyEdges
                    .Where(value => value != null)
                    .OrderBy(value => value.GUID, StringComparer.Ordinal)
                    .ToArray();
                for (int edgeIndex = 0; edgeIndex < propertyEdges.Length; edgeIndex++)
                    CompileEdge(graph, propertyEdges[edgeIndex], route, operations, stateScopeOwner);

                BaseNode[] referenceOwners = nodes;
                for (int nodeIndex = 0; nodeIndex < referenceOwners.Length; nodeIndex++)
                {
                    BaseNode owner = referenceOwners[nodeIndex];
                    NodeGraphReference[] references = owner.GetGraphReferences()
                        .Where(value => value.Tree != null)
                        .OrderBy(value => value.Key, StringComparer.Ordinal)
                        .ToArray();
                    for (int referenceIndex = 0; referenceIndex < references.Length; referenceIndex++)
                    {
                        NodeGraphReference reference = references[referenceIndex];
                        if (!operations.TryGetValue(owner.GUID, out OperationHandle ownerOperation))
                            continue;
                        string childRoute = $"{route}/node:{owner.GUID}/reference:{reference.Key}/tree:{reference.Tree.GraphAuthoringId}";
                        OperationHandle entry = CompileGraph(reference.Tree, childRoute, stateScopeOwner);
                        if (!entry.IsValid)
                            continue;
                        if (owner is StateNode && reference.Tree is StateBehaviorSubTree stateBehavior)
                        {
                            DeclareStateBehaviorFlow(graph, owner, ownerOperation, stateBehavior, childRoute, entry);
                            continue;
                        }
                        m_Builder.DeclareControlFlow(
                            $"{childRoute}/entry",
                            ownerOperation,
                            entry,
                            reference.Key,
                            "Entry",
                            ProgramControlFlowKind.Enter,
                            0,
                            0,
                            ProgramAbortPolicy.None,
                            false,
                            OperationHandle.Invalid,
                            Source(graph, owner, childRoute));
                        if (owner is StateMachineNode)
                        {
                            foreach (BaseNode stateNode in reference.Tree.Nodes)
                            {
                                if (stateNode is not StateNode)
                                    continue;
                                if (!TryGetOperation(childRoute, stateNode.GUID, out OperationHandle stateOperation))
                                    throw new InvalidOperationException($"State '{stateNode.GUID}' has no compiled operation.");
                                m_Builder.DeclareReference(
                                    $"{childRoute}/node:{stateNode.GUID}/state-machine-owner",
                                    stateOperation,
                                    ProgramReferenceKind.Operation,
                                    ownerOperation.Value,
                                    childRoute,
                                    Source(reference.Tree, stateNode, childRoute));
                            }
                        }
                        if (owner is StateMachineNode &&
                            reference.Tree is StateMachineGraph stateMachine &&
                            stateMachine.AnyStateNode != null &&
                            TryGetOperation(childRoute, stateMachine.AnyStateNode.GUID, out OperationHandle anyState))
                        {
                            m_Builder.DeclareControlFlow(
                                $"{childRoute}/any-state",
                                ownerOperation,
                                anyState,
                                "AnyState",
                                "Entry",
                                ProgramControlFlowKind.Enter,
                                1,
                                0,
                                ProgramAbortPolicy.None,
                                false,
                                OperationHandle.Invalid,
                                Source(graph, owner, childRoute));
                        }
                    }
                }

                string entryId = graph is OneRootTree oneRoot
                    ? oneRoot.RootGUID
                    : nodes.OfType<RootNode>().SingleOrDefault()?.GUID ?? string.Empty;
                if (string.IsNullOrEmpty(entryId) || !operations.TryGetValue(entryId, out OperationHandle result))
                {
                    m_Report.Error(
                        "timeline_tree_entry_missing",
                        route,
                        $"Timeline Tree graph '{graph.GraphAuthoringId}' has no compiled Root node.");
                    return OperationHandle.Invalid;
                }
                return result;
            }
            finally
            {
                m_ActiveGraphs.Remove(graph.GraphAuthoringId);
            }
        }

        void CompileEdge(
            BaseTree graph,
            BaseEdge edge,
            string route,
            IReadOnlyDictionary<string, OperationHandle> operations,
            OperationHandle stateScopeOwner)
        {
            if (!operations.TryGetValue(edge.StartNodeGUID, out OperationHandle source) ||
                !operations.TryGetValue(edge.EndNodeGUID, out OperationHandle target))
            {
                m_Report.Error(
                    "timeline_tree_edge_endpoint_missing",
                    $"{route}/edge:{edge.GUID}",
                    "Timeline Tree edge endpoint was not compiled.");
                return;
            }

            bool hasCondition = false;
            OperationHandle condition = OperationHandle.Invalid;
            if (edge.HasConditionRuleGraphConfiguration)
            {
                if (!edge.TryResolveConditionRuleGraph(out ConditionRuleGraph conditionGraph, out string error))
                {
                    m_Report.Error("timeline_tree_condition_invalid", $"{route}/edge:{edge.GUID}", error);
                    return;
                }
                string conditionRoute = $"{route}/edge:{edge.GUID}/condition:{conditionGraph.GraphAuthoringId}";
                condition = CompileGraph(conditionGraph, conditionRoute, stateScopeOwner);
                hasCondition = condition.IsValid;
                if (!hasCondition)
                    return;
            }

            ProgramControlFlowKind kind = edge is PropertyEdge
                ? ProgramControlFlowKind.Value
                : graph is StateMachineGraph stateMachine && stateMachine.IsTransitionEdge(edge)
                    ? ProgramControlFlowKind.Transition
                    : ProgramControlFlowKind.Child;
            m_Builder.DeclareControlFlow(
                $"{route}/edge:{edge.GUID}",
                source,
                target,
                edge.StartPortName,
                edge.EndPortName,
                kind,
                edge.FlowOrder,
                edge.TransitionPriority,
                (ProgramAbortPolicy)(int)edge.AbortPolicy,
                hasCondition,
                condition,
                Source(graph, edge, route));
        }

        void DeclareStateBehaviorFlow(
            BaseTree graph,
            BaseNode owner,
            OperationHandle ownerOperation,
            StateBehaviorSubTree stateBehavior,
            string childRoute,
            OperationHandle entry)
        {
            if (!TryGetOperation(childRoute, stateBehavior.OnEnterGUID, out OperationHandle onEnter) ||
                !TryGetOperation(childRoute, stateBehavior.OnExitGUID, out OperationHandle onExit))
            {
                m_Report.Error("timeline_tree_state_lifecycle_missing", childRoute, "State behavior is missing OnEnter or OnExit operation.");
                return;
            }
            SimulationSourceLocation source = Source(graph, owner, childRoute);
            m_Builder.DeclareControlFlow(
                $"{childRoute}/state-on-enter",
                ownerOperation,
                onEnter,
                "OnEnter",
                "Entry",
                ProgramControlFlowKind.Enter,
                0,
                0,
                ProgramAbortPolicy.None,
                false,
                OperationHandle.Invalid,
                source);
            m_Builder.DeclareControlFlow(
                $"{childRoute}/state-root",
                ownerOperation,
                entry,
                "Root",
                "Entry",
                ProgramControlFlowKind.Enter,
                1,
                0,
                ProgramAbortPolicy.None,
                false,
                OperationHandle.Invalid,
                source);
            m_Builder.DeclareControlFlow(
                $"{childRoute}/state-on-exit",
                ownerOperation,
                onExit,
                "OnExit",
                "Entry",
                ProgramControlFlowKind.Exit,
                2,
                0,
                ProgramAbortPolicy.None,
                false,
                OperationHandle.Invalid,
                source);
        }

        OperationHandle ResolveLifecycle(string route, TimelineRunningTree tree, string port)
        {
            string nodeId = port switch
            {
                "OnEnable" => tree.OnEnableGUID,
                "OnDisable" => tree.OnDisableGUID,
                "OnDestroy" => tree.OnDestroyGUID,
                _ => string.Empty
            };
            return TryGetOperation(route, nodeId, out OperationHandle operation)
                ? operation
                : OperationHandle.Invalid;
        }

        bool TryGetOperation(string route, string nodeId, out OperationHandle operation)
        {
            operation = OperationHandle.Invalid;
            return !string.IsNullOrEmpty(nodeId) &&
                   m_OperationsByRoute.TryGetValue(route, out Dictionary<string, OperationHandle> operations) &&
                   operations.TryGetValue(nodeId, out operation);
        }

        static SimulationSourceLocation Source(BaseTree graph, BaseNode node, string route) =>
            new SimulationSourceLocation(
                node.GetType().FullName,
                graph.GraphAuthoringId,
                node.GUID,
                string.Empty,
                string.Empty,
                string.Empty,
                $"{route}/node:{node.GUID}",
                contentHash: GraphAuthoringFingerprint.Compute(graph));

        static SimulationSourceLocation Source(BaseTree graph, BaseEdge edge, string route) =>
            new SimulationSourceLocation(
                edge.GetType().FullName,
                graph.GraphAuthoringId,
                string.Empty,
                edge.GUID,
                string.Empty,
                string.Empty,
                $"{route}/edge:{edge.GUID}",
                contentHash: GraphAuthoringFingerprint.Compute(graph));
    }
}
