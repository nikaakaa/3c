using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonSimulation;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal delegate bool CharacterSemanticCompiledOperationLookup(string route, string nodeId, out OperationHandle operation);

    internal sealed class CharacterSemanticGraphFlowEmitter
    {
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly CharacterSimulationCompileReport m_Report;
        readonly Func<CharacterAuthoringGraphOccurrence, OperationHandle, OperationHandle> m_CompileGraph;
        readonly CharacterSemanticCompiledOperationLookup m_TryGetCompiledOperation;

        public CharacterSemanticGraphFlowEmitter(
            CharacterSimulationProgramBuilder builder,
            CharacterSimulationCompileReport report,
            Func<CharacterAuthoringGraphOccurrence, OperationHandle, OperationHandle> compileGraph,
            CharacterSemanticCompiledOperationLookup tryGetCompiledOperation)
        {
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
            m_CompileGraph = compileGraph ?? throw new ArgumentNullException(nameof(compileGraph));
            m_TryGetCompiledOperation = tryGetCompiledOperation ?? throw new ArgumentNullException(nameof(tryGetCompiledOperation));
        }

        public void EmitStateBehavior(
            BaseTree graph,
            BaseNode node,
            OperationHandle owner,
            StateBehaviorSubTree stateBehavior,
            string childRoute,
            OperationHandle root)
        {
            CharacterSimulationSourceLocation source = CharacterSemanticSourceFactory.Node(graph, node, childRoute);
            if (!m_TryGetCompiledOperation(childRoute, stateBehavior.OnEnterGUID, out OperationHandle onEnter) ||
                !m_TryGetCompiledOperation(childRoute, stateBehavior.OnExitGUID, out OperationHandle onExit))
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

        public void EmitEdge(
            CharacterAuthoringGraphOccurrence occurrence,
            CharacterAuthoringEdgeRecord record,
            Dictionary<string, OperationHandle> operations,
            OperationHandle stateScopeOwner,
            bool ignoreMissingEndpoint)
        {
            BaseTree graph = occurrence.Graph;
            BaseEdge edge = record.Edge;
            string edgeRoute = record.Route;
            if (!operations.TryGetValue(edge.StartNodeGUID, out OperationHandle source) || !operations.TryGetValue(edge.EndNodeGUID, out OperationHandle target))
            {
                if (ignoreMissingEndpoint)
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
                condition = m_CompileGraph(record.ConditionGraph, conditionStateOwner);
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
    }
}
