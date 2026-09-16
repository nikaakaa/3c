using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class BtsmtlSkillGraphFlowEmitter
    {
        readonly GameplayAbilitySemanticBuilder m_Builder;
        readonly SimulationOperationEmitter m_NativeOperations;

        public BtsmtlSkillGraphFlowEmitter(GameplayAbilitySemanticBuilder builder)
        {
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_NativeOperations = new SimulationOperationEmitter(m_Builder);
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
                    kind, record.Order, record.Priority, record.AbortPolicy,
                    record.Condition != null, condition,
                    new SimulationSourceLocation(edge.GetType().FullName, graph.GraphId, string.Empty,
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
                    new SimulationSourceLocation(node.GetType().FullName, child.GraphId, node.UID,
                        string.Empty, string.Empty, string.Empty, $"{child.Route}/node:{node.UID}", contentHash: child.ContentHash));
            }
        }

        public void EmitNativeStateMachine(
            BtsmtlSkillGraphReferenceOccurrence reference,
            OperationHandle owner,
            BtsmtlSkillNativeStateMachineOccurrence machine,
            Func<BtsmtlSkillGraphOccurrence, OperationHandle, BtsmtlSkillInvocationContext, BtsmtlSkillGraphCompilation> compileGraph,
            Func<BtsmtlSkillNativeEdgeOccurrence, OperationHandle, OperationHandle> compileCondition)
        {
            var operations = new Dictionary<string, OperationHandle>(StringComparer.Ordinal);
            foreach (BtsmtlSkillNativeState state in new[] { machine.Entry, machine.Any, machine.Exit }
                         .Concat(machine.States.Select(value => value.State))
                         .Distinct())
            {
                SimulationOperationCode code = state switch
                {
                    BtsmtlSkillNativeEntryState => SimulationOperationCode.StateEnter,
                    BtsmtlSkillNativeAnyState => SimulationOperationCode.StateAny,
                    BtsmtlSkillNativeExitState => SimulationOperationCode.StateExit,
                    _ => SimulationOperationCode.State
                };
                operations[state.UID] = m_NativeOperations.Emit(
                    Source(machine, state),
                    new SimulationNodeEmission(code, text0: state.Body?.AuthoringId),
                    Array.Empty<SimulationConstantInput>());
            }
            DeclareNativeEntry(reference, owner, operations[machine.Entry.UID], "StateMachine", 0, machine);
            DeclareNativeEntry(reference, owner, operations[machine.Any.UID], "AnyState", 1, machine);
            foreach (BtsmtlSkillNativeStateOccurrence state in machine.States)
            {
                if (state.Body == null)
                    continue;
                BtsmtlSkillGraphCompilation body = compileGraph(
                    state.Body,
                    operations[state.State.UID],
                    BtsmtlSkillInvocationContext.Call(state.State.UID, operations[state.State.UID]));
                DeclareNativeBodyEntry(machine, state, body, operations[state.State.UID], "OnEnter", 0);
                DeclareNativeBodyEntry(machine, state, body, operations[state.State.UID], "Root", 1);
                DeclareNativeBodyEntry(machine, state, body, operations[state.State.UID], "OnExit", 2);
            }
            foreach (BtsmtlSkillNativeStateOccurrence state in machine.States)
                m_Builder.DeclareReference(
                    $"{state.Route}/state-machine-owner",
                    operations[state.State.UID],
                    ProgramReferenceKind.Operation,
                    owner.Value,
                    state.Route,
                    Source(machine, state.State));
            foreach (BtsmtlSkillNativeEdgeOccurrence edge in machine.Edges)
            {
                OperationHandle conditionOwner = edge.Source is BtsmtlSkillNativeEntryState ||
                                                 edge.Source is BtsmtlSkillNativeAnyState ||
                                                 edge.Source is BtsmtlSkillNativeExitState
                    ? owner
                    : operations[edge.Source.UID];
                OperationHandle condition = edge.Condition == null
                    ? OperationHandle.Invalid
                    : compileCondition(edge, conditionOwner);
                m_Builder.DeclareControlFlow(
                    edge.Route,
                    operations[edge.Source.UID],
                    operations[edge.Target.UID],
                    "Transfer",
                    "StateIn",
                    ProgramControlFlowKind.Transition,
                    edge.Order,
                    edge.Priority,
                    edge.AbortPolicy,
                    edge.Condition != null,
                    condition,
                    Source(machine, edge.Edge));
            }
        }

        void DeclareEntry(BtsmtlSkillGraphReferenceOccurrence reference, OperationHandle owner, OperationHandle entry,
            string port, ProgramControlFlowKind kind, int order)
        {
            m_Builder.DeclareControlFlow($"{reference.CallSiteIdentity}/entry:{port}", owner, entry, port, "Entry",
                kind, order, 0, ProgramAbortPolicy.None, false, OperationHandle.Invalid,
                new SimulationSourceLocation(reference.Owner.GetType().FullName,
                    ((IBtsmtlSkillFlowGraph)reference.Owner.graph).AuthoringId, reference.Owner.UID,
                    string.Empty, string.Empty, string.Empty, reference.CallSiteIdentity, contentHash: reference.OwnerContentHash));
        }

        void DeclareNativeEntry(
            BtsmtlSkillGraphReferenceOccurrence reference,
            OperationHandle owner,
            OperationHandle entry,
            string port,
            int order,
            BtsmtlSkillNativeStateMachineOccurrence machine)
        {
            m_Builder.DeclareControlFlow(
                $"{reference.CallSiteIdentity}/entry:{port}",
                owner,
                entry,
                port,
                "Entry",
                ProgramControlFlowKind.Enter,
                order,
                0,
                ProgramAbortPolicy.None,
                false,
                OperationHandle.Invalid,
                Source(machine, machine.Machine));
        }

        void DeclareNativeBodyEntry(
            BtsmtlSkillNativeStateMachineOccurrence machine,
            BtsmtlSkillNativeStateOccurrence state,
            BtsmtlSkillGraphCompilation body,
            OperationHandle owner,
            string port,
            int order)
        {
            BtsmtlSkillFlowGraph graph = (BtsmtlSkillFlowGraph)state.Body.Graph;
            FlowNode entry = port switch
            {
                "OnEnter" => graph.allNodes.OfType<BtsmtlSkillStateOnEnterFlowNode>().Single(),
                "Root" => graph.allNodes.OfType<BtsmtlSkillRootFlowNode>().Single(),
                "OnExit" => graph.allNodes.OfType<BtsmtlSkillStateOnExitFlowNode>().Single(),
                _ => throw new ArgumentOutOfRangeException(nameof(port))
            };
            if (!body.Operations.Nodes.TryGetValue(entry.UID, out OperationHandle entryOperation))
                throw new InvalidOperationException($"{state.Route}: StateBody入口'{port}'未编译。");
            ProgramControlFlowKind kind = port == "OnExit"
                ? ProgramControlFlowKind.Exit
                : ProgramControlFlowKind.Enter;
            m_Builder.DeclareControlFlow(
                $"{state.Route}/body-entry:{port}",
                owner,
                entryOperation,
                port,
                "Entry",
                kind,
                order,
                0,
                ProgramAbortPolicy.None,
                false,
                OperationHandle.Invalid,
                Source(machine, state.State));
        }

        static SimulationSourceLocation Source(
            BtsmtlSkillNativeStateMachineOccurrence machine,
            BtsmtlSkillNativeState state) =>
            new(typeof(BtsmtlSkillNativeState).FullName, machine.GraphId, state.UID,
                string.Empty, string.Empty, string.Empty,
                $"{machine.Route}/state:{state.UID}", contentHash: machine.ContentHash);

        static SimulationSourceLocation Source(
            BtsmtlSkillNativeStateMachineOccurrence machine,
            BtsmtlSkillNativeConnection edge) =>
            new(typeof(BtsmtlSkillNativeConnection).FullName, machine.GraphId, string.Empty,
                edge.UID, string.Empty, string.Empty,
                $"{machine.Route}/edge:{edge.UID}", contentHash: machine.ContentHash);

        static SimulationSourceLocation Source(
            BtsmtlSkillNativeStateMachineOccurrence machine,
            BtsmtlSkillNativeStateMachine graph) =>
            new(typeof(BtsmtlSkillNativeStateMachine).FullName, machine.GraphId, string.Empty,
                string.Empty, string.Empty, string.Empty,
                machine.Route, contentHash: machine.ContentHash);

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
