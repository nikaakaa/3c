using System;
using System.Collections.Generic;
using FlowCanvas;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class BtsmtlSkillGraphFlowEmitter
    {
        readonly CharacterSimulationProgramBuilder m_Builder;

        public BtsmtlSkillGraphFlowEmitter(CharacterSimulationProgramBuilder builder)
        {
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public void EmitEdges(BtsmtlSkillGraphOccurrence graph, BtsmtlSkillOperationBindings operations,
            OperationHandle stateOwner, Func<BtsmtlSkillEdgeOccurrence, OperationHandle, OperationHandle> compileCondition)
        {
            foreach (BtsmtlSkillEdgeOccurrence record in graph.Edges)
            {
                BinderConnection edge = record.Edge;
                bool value = edge.sourcePort is ValueOutput;
                BtsmtlSkillValuePortBinding valueSource = value ? operations.Output(edge.sourceNode.UID, edge.sourcePortID) : default;
                BtsmtlSkillValuePortBinding valueTarget = value ? operations.Input(edge.targetNode.UID, edge.targetPortID) : default;
                OperationHandle source = value ? valueSource.Operation : operations.FlowSource(edge.sourceNode.UID);
                OperationHandle target = value ? valueTarget.Operation : operations.Node(edge.targetNode.UID);
                ProgramControlFlowKind kind = value
                    ? ProgramControlFlowKind.Value
                    : graph.Role == BtsmtlSkillFlowGraphRole.StateMachine
                        ? ProgramControlFlowKind.Transition
                        : ProgramControlFlowKind.Child;
                OperationHandle condition = record.Condition == null
                    ? OperationHandle.Invalid
                    : compileCondition(record, edge.sourceNode is BtsmtlSkillStateFlowNode ? source : stateOwner);
                string sourcePort = value ? valueSource.PortId : edge.sourcePortID;
                string targetPort = value ? valueTarget.PortId : edge.targetPortID;
                m_Builder.DeclareControlFlow(record.Route, source, target, sourcePort, targetPort,
                    kind, record.Order, record.Step?.Priority ?? 0, record.Step?.AbortPolicy ?? ProgramAbortPolicy.None,
                    record.Condition != null, condition,
                    new CharacterSimulationSourceLocation(edge.GetType().FullName, graph.GraphId, string.Empty,
                        edge.UID, string.Empty, string.Empty, record.Route, contentHash: graph.ContentHash));
            }
        }

        public void EmitStateBody(BtsmtlSkillGraphReferenceOccurrence reference, OperationHandle owner,
            IReadOnlyDictionary<string, OperationHandle> operations)
        {
            BtsmtlSkillGraphOccurrence child = reference.Child;
            DeclareEntry(reference, owner, Require<BtsmtlSkillStateOnEnterFlowNode>(child, operations), "OnEnter", ProgramControlFlowKind.Enter, 0);
            DeclareEntry(reference, owner, Require<BtsmtlSkillRootFlowNode>(child, operations), "Root", ProgramControlFlowKind.Enter, 1);
            DeclareEntry(reference, owner, Require<BtsmtlSkillStateOnExitFlowNode>(child, operations), "OnExit", ProgramControlFlowKind.Exit, 2);
        }

        public void EmitStateMachine(BtsmtlSkillGraphReferenceOccurrence reference, OperationHandle owner,
            IReadOnlyDictionary<string, OperationHandle> operations)
        {
            BtsmtlSkillGraphOccurrence child = reference.Child;
            DeclareEntry(reference, owner, Require<BtsmtlSkillStateEnterFlowNode>(child, operations), "StateMachine", ProgramControlFlowKind.Enter, 0);
            DeclareEntry(reference, owner, Require<BtsmtlSkillStateAnyFlowNode>(child, operations), "AnyState", ProgramControlFlowKind.Enter, 1);
            foreach (FlowNode node in child.Nodes)
            {
                if (node is not BtsmtlSkillStateFlowNode)
                    continue;
                m_Builder.DeclareReference($"{child.Route}/node:{node.UID}/state-machine-owner", operations[node.UID],
                    ProgramReferenceKind.Operation, owner.Value, child.Route,
                    new CharacterSimulationSourceLocation(node.GetType().FullName, child.GraphId, node.UID,
                        string.Empty, string.Empty, string.Empty, $"{child.Route}/node:{node.UID}", contentHash: child.ContentHash));
            }
        }

        void DeclareEntry(BtsmtlSkillGraphReferenceOccurrence reference, OperationHandle owner, OperationHandle entry,
            string port, ProgramControlFlowKind kind, int order)
        {
            m_Builder.DeclareControlFlow($"{reference.CallSiteIdentity}/entry:{port}", owner, entry, port, "Entry",
                kind, order, 0, ProgramAbortPolicy.None, false, OperationHandle.Invalid,
                new CharacterSimulationSourceLocation(reference.Owner.GetType().FullName,
                    ((IBtsmtlSkillFlowGraph)reference.Owner.graph).AuthoringId, reference.Owner.UID,
                    string.Empty, string.Empty, string.Empty, reference.CallSiteIdentity, contentHash: reference.OwnerContentHash));
        }

        static OperationHandle Require<T>(BtsmtlSkillGraphOccurrence graph, IReadOnlyDictionary<string, OperationHandle> operations)
            where T : FlowNode
        {
            FlowNode match = null;
            foreach (FlowNode node in graph.Nodes)
                if (node is T)
                {
                    if (match != null)
                        throw new InvalidOperationException($"{graph.Route}: 系统入口'{typeof(T).Name}'重复。");
                    match = node;
                }
            if (match == null || !operations.TryGetValue(match.UID, out OperationHandle operation))
                throw new InvalidOperationException($"{graph.Route}: 系统入口'{typeof(T).Name}'未发射。");
            return operation;
        }
    }
}
