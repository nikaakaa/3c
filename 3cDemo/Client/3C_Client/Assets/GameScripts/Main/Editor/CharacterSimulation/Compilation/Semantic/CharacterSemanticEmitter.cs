using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class CharacterSemanticEmitter
    {
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly CharacterSimulationCompileReport m_Report;
        readonly CharacterSimulationNodeEmitterRegistry m_NodeEmitters;
        readonly CharacterSemanticTimelineEmitter m_Timelines;
        readonly CharacterSemanticBlackboardEmitter m_Blackboard;
        readonly CharacterSemanticDomainBindingEmitter m_DomainBindings;
        readonly CharacterSemanticSkillProgramEmitter m_SkillPrograms;
        readonly Dictionary<string, Dictionary<string, OperationHandle>> m_CompiledGraphOperations = new Dictionary<string, Dictionary<string, OperationHandle>>(StringComparer.Ordinal);
        int m_SkillCompilationDepth;

        public CharacterSemanticEmitter(
            CharacterAuthoringCompilationModel model,
            CharacterSimulationProgramBuilder builder,
            CharacterSimulationCompileReport report,
            CharacterSimulationCatalogIndex catalogIndex)
        {
            model = model ?? throw new ArgumentNullException(nameof(model));
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
            m_Blackboard = new CharacterSemanticBlackboardEmitter(model.Declarations, builder, report);
            m_DomainBindings = new CharacterSemanticDomainBindingEmitter(catalogIndex, builder, report, m_Blackboard);
            m_SkillPrograms = new CharacterSemanticSkillProgramEmitter(model.SkillRecords, builder, report);
            m_NodeEmitters = model.NodeEmitters;
            m_Timelines = new CharacterSemanticTimelineEmitter(
                model.TimelineEmitters,
                builder,
                report,
                CompileGraph,
                TryGetCompiledOperation);
        }

        public OperationHandle EmitControlSkillPrograms(
            CharacterControlModuleContract contract,
            IReadOnlyList<CharacterControlMotionCompilationRecord> motions)
        {
            if (contract == null)
                throw new ArgumentNullException(nameof(contract));
            if (motions == null)
                throw new ArgumentNullException(nameof(motions));
            m_SkillPrograms.DeclareExecutionState();
            m_Blackboard.CompileDeclarations();
            for (int motionIndex = 0; motionIndex < motions.Count; motionIndex++)
            {
                m_SkillCompilationDepth++;
                try
                {
                    CompileGraph(motions[motionIndex].Graph, OperationHandle.Invalid);
                }
                finally
                {
                    m_SkillCompilationDepth--;
                }
            }
            if (!m_SkillPrograms.Emit(contract, CompileSkillEntry))
                return OperationHandle.Invalid;
            m_Blackboard.DeclareScopes();
            CharacterSimulationSourceLocation rootSource = new CharacterSimulationSourceLocation(
                typeof(ICharacterControlModule).FullName,
                $"control:{contract.ModuleId.Value}",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                $"control:{contract.ModuleId.Value}/root",
                contentHash: contract.SemanticVersion.ToString());
            return m_Builder.DeclareOperation(rootSource, SimulationOperationCode.Root, Array.Empty<int>());
        }

        OperationHandle CompileSkillEntry(CharacterSkillCompilationRecord record)
        {
            m_SkillCompilationDepth++;
            try
            {
                return CompileGraph(record.EntryGraph, OperationHandle.Invalid);
            }
            finally
            {
                m_SkillCompilationDepth--;
            }
        }

        OperationHandle CompileGraph(CharacterAuthoringGraphOccurrence occurrence, OperationHandle stateScopeOwner)
        {
            if (occurrence == null)
                throw new ArgumentNullException(nameof(occurrence));
            BaseTree graph = occurrence.Graph;
            string route = occurrence.Route;
            try
            {
                m_Blackboard.BeginGraph(graph, route, occurrence.Declarations, stateScopeOwner);
                var operations = new Dictionary<string, OperationHandle>(StringComparer.Ordinal);
                foreach (BaseNode node in occurrence.Nodes)
                {
                    if (m_SkillCompilationDepth != 0 && node is ActivateActionInstanceNode)
                        continue;
                    if (!m_NodeEmitters.TryGet(node.GetType(), out ICharacterSimulationNodeEmitter emitter))
                        throw new InvalidOperationException($"Discovered Node '{node.GUID}' has no emitter.");
                    var context = new CharacterSimulationNodeEmitterContext(graph, route, m_Builder);
                    OperationHandle operation;
                    try
                    {
                        operation = emitter.Emit(node, context);
                    }
                    catch (Exception exception)
                    {
                        m_Report.EmissionError("node_emit_failed", $"{route}/node:{node.GUID}", exception.Message);
                        continue;
                    }
                    operations.Add(node.GUID, operation);
                    m_DomainBindings.Bind(node, operation, route, context.Source(node));
                }
                m_CompiledGraphOperations[route] = operations;

                foreach (CharacterAuthoringEdgeRecord edge in occurrence.Edges)
                    CompileEdge(occurrence, edge, operations, stateScopeOwner);
                foreach (CharacterAuthoringEdgeRecord edge in occurrence.PropertyEdges)
                    CompileEdge(occurrence, edge, operations, stateScopeOwner);

                foreach (CharacterAuthoringTimelineRecord timeline in occurrence.Timelines)
                {
                    if (!operations.TryGetValue(timeline.Node.GUID, out OperationHandle owner))
                        throw new InvalidOperationException($"Discovered Timeline Node '{timeline.Node.GUID}' has no emitted operation.");
                    m_Timelines.Emit(timeline, owner, stateScopeOwner);
                }
                foreach (CharacterAuthoringGraphReferenceRecord reference in occurrence.GraphReferences)
                {
                    if (!operations.TryGetValue(reference.Owner.GUID, out OperationHandle owner))
                        throw new InvalidOperationException($"Discovered graph owner Node '{reference.Owner.GUID}' has no emitted operation.");
                    OperationHandle childStateOwner = reference.Owner is StateNode && reference.Child.Graph is StateBehaviorSubTree
                        ? owner
                        : stateScopeOwner;
                    OperationHandle entry = CompileGraph(reference.Child, childStateOwner);
                    if (!entry.IsValid)
                        continue;
                    if (reference.Owner is StateNode && reference.Child.Graph is StateBehaviorSubTree stateBehavior)
                    {
                        DeclareStateBehaviorControlFlow(graph, reference.Owner, owner, stateBehavior, reference.Route, entry);
                        continue;
                    }
                    m_Builder.DeclareControlFlow(
                        $"{reference.Route}/entry",
                        owner,
                        entry,
                        reference.Reference.Key,
                        "Entry",
                        ProgramControlFlowKind.Enter,
                        0,
                        0,
                        ProgramAbortPolicy.None,
                        false,
                        OperationHandle.Invalid,
                        NodeSource(graph, reference.Owner, route));
                    if (reference.Owner is StateMachineNode && reference.Child.Graph is StateMachineGraph stateMachine &&
                        stateMachine.AnyStateNode != null &&
                        TryGetCompiledOperation(reference.Route, stateMachine.AnyStateNode.GUID, out OperationHandle anyState))
                    {
                        m_Builder.DeclareControlFlow(
                            $"{reference.Route}/any-state",
                            owner,
                            anyState,
                            "AnyState",
                            "Entry",
                            ProgramControlFlowKind.Enter,
                            1,
                            0,
                            ProgramAbortPolicy.None,
                            false,
                            OperationHandle.Invalid,
                            NodeSource(graph, reference.Owner, route));
                    }
                }
                OperationHandle graphEntry = FindEntry(occurrence, operations);
                m_Blackboard.CompleteGraph(route, graphEntry);
                return graphEntry;
            }
            finally
            {
                m_Blackboard.EndGraph();
            }
        }

        void DeclareStateBehaviorControlFlow(
            BaseTree graph,
            BaseNode node,
            OperationHandle owner,
            StateBehaviorSubTree stateBehavior,
            string childRoute,
            OperationHandle root)
        {
            CharacterSimulationSourceLocation source = NodeSource(graph, node, childRoute);
            if (!TryGetCompiledOperation(childRoute, stateBehavior.OnEnterGUID, out OperationHandle onEnter) ||
                !TryGetCompiledOperation(childRoute, stateBehavior.OnExitGUID, out OperationHandle onExit))
            {
                m_Report.Error("state_lifecycle_operation_missing", childRoute, "State behavior requires compiled OnEnter and OnExit operations.");
                return;
            }
            m_Builder.DeclareControlFlow(
                $"{childRoute}/state-on-enter",
                owner,
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
                owner,
                root,
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
                owner,
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

        bool TryGetCompiledOperation(string route, string nodeId, out OperationHandle operation)
        {
            operation = OperationHandle.Invalid;
            return !string.IsNullOrEmpty(nodeId) &&
                   m_CompiledGraphOperations.TryGetValue(route, out Dictionary<string, OperationHandle> operations) &&
                   operations.TryGetValue(nodeId, out operation);
        }

        void CompileEdge(
            CharacterAuthoringGraphOccurrence occurrence,
            CharacterAuthoringEdgeRecord record,
            Dictionary<string, OperationHandle> operations,
            OperationHandle stateScopeOwner)
        {
            BaseTree graph = occurrence.Graph;
            BaseEdge edge = record.Edge;
            string edgeRoute = record.Route;
            if (!operations.TryGetValue(edge.StartNodeGUID, out OperationHandle source) || !operations.TryGetValue(edge.EndNodeGUID, out OperationHandle target))
            {
                if (m_SkillCompilationDepth != 0)
                    return;
                throw new InvalidOperationException($"Discovered Edge '{edge.GUID}' has an operation endpoint mismatch.");
            }
            bool hasCondition = false;
            OperationHandle condition = OperationHandle.Invalid;
            if (record.ConditionGraph != null)
            {
                OperationHandle conditionStateOwner = occurrence.Nodes.Any(value => value is StateNode && value.GUID == edge.StartNodeGUID)
                    ? source
                    : stateScopeOwner;
                condition = CompileGraph(record.ConditionGraph, conditionStateOwner);
                hasCondition = condition.IsValid;
            }
            ProgramControlFlowKind kind = edge is PropertyEdge
                ? ProgramControlFlowKind.Value
                : graph is StateMachineGraph stateMachine && stateMachine.IsTransitionEdge(edge)
                    ? ProgramControlFlowKind.Transition
                    : ProgramControlFlowKind.Child;
            m_Builder.DeclareControlFlow(
                edgeRoute,
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
                new CharacterSimulationSourceLocation(
                    edge.GetType().FullName,
                    graph.GraphAuthoringId,
                    string.Empty,
                    edge.GUID,
                    string.Empty,
                    string.Empty,
                    edgeRoute,
                    contentHash: GraphAuthoringFingerprint.Compute(graph)));
        }

        OperationHandle FindEntry(CharacterAuthoringGraphOccurrence occurrence, Dictionary<string, OperationHandle> operations)
        {
            if (!string.IsNullOrEmpty(occurrence.EntryNodeId) && operations.TryGetValue(occurrence.EntryNodeId, out OperationHandle operation))
                return operation;
            m_Report.EmissionError("graph_entry_missing", occurrence.Route, $"Discovered entry Node '{occurrence.EntryNodeId}' was not emitted.");
            return OperationHandle.Invalid;
        }

        static CharacterSimulationSourceLocation NodeSource(BaseTree graph, BaseNode node, string route)
        {
            return new CharacterSimulationSourceLocation(
                node.GetType().FullName,
                graph?.GraphAuthoringId ?? node.Owner?.GraphAuthoringId ?? string.Empty,
                node.GUID,
                string.Empty,
                string.Empty,
                string.Empty,
                $"{route}/node:{node.GUID}",
                contentHash: GraphAuthoringFingerprint.Compute(graph ?? node.Owner));
        }

    }
}
