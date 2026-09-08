using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using FlowCanvas.Macros;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class BtsmtlSkillGraphCompilation
    {
        internal BtsmtlSkillGraphCompilation(OperationHandle entry, BtsmtlSkillOperationBindings operations,
            BtsmtlSkillMacroCompilation macro)
        {
            Entry = entry;
            Operations = operations;
            Macro = macro;
        }

        public OperationHandle Entry { get; }
        public BtsmtlSkillOperationBindings Operations { get; }
        public BtsmtlSkillMacroCompilation Macro { get; }
    }

    public sealed class BtsmtlSkillGraphCompiler
    {
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly BtsmtlSkillFlowLeafEmitter m_Leaves;
        readonly BtsmtlSkillGraphFlowEmitter m_Flow;
        readonly Action<FlowNode, OperationHandle, string, CharacterSimulationSourceLocation> m_BindDomain;
        readonly Dictionary<string, BtsmtlSkillGraphCompilation> m_Graphs = new(StringComparer.Ordinal);

        public BtsmtlSkillGraphCompiler(CharacterSimulationProgramBuilder builder,
            Action<FlowNode, OperationHandle, string, CharacterSimulationSourceLocation> bindDomain)
        {
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_BindDomain = bindDomain ?? throw new ArgumentNullException(nameof(bindDomain));
            m_Leaves = new BtsmtlSkillFlowLeafEmitter(builder);
            m_Flow = new BtsmtlSkillGraphFlowEmitter(builder);
        }

        public BtsmtlSkillGraphCompilation Compile(BtsmtlSkillGraphOccurrence graph, OperationHandle stateOwner)
        {
            if (m_Graphs.TryGetValue(graph.Route, out BtsmtlSkillGraphCompilation existing))
                return existing;
            var operations = new BtsmtlSkillOperationBindings();
            BtsmtlSkillMacroCompilation macro = graph.Role == BtsmtlSkillFlowGraphRole.Subgraph
                ? new BtsmtlSkillMacroCompilation(m_Builder, graph, operations)
                : null;
            foreach (FlowNode node in graph.Nodes)
            {
                if (node is MacroInputNode || node is MacroOutputNode || node is MacroNodeWrapper)
                    continue;
                OperationHandle operation = m_Leaves.Emit(node, graph.Route, graph.ContentHash);
                operations.AddLeaf(node, operation);
                m_BindDomain(node, operation, graph.Route, Source(graph, node));
            }
            foreach (BtsmtlSkillGraphReferenceOccurrence reference in graph.References)
            {
                OperationHandle childStateOwner = reference.Kind == BtsmtlSkillGraphReferenceKind.StateBody
                    ? operations.Node(reference.Owner.UID)
                    : stateOwner;
                BtsmtlSkillGraphCompilation child = Compile(reference.Child, childStateOwner);
                switch (reference.Kind)
                {
                    case BtsmtlSkillGraphReferenceKind.Macro:
                        child.Macro.EmitCall(reference, operations);
                        break;
                    case BtsmtlSkillGraphReferenceKind.StateMachine:
                        m_Flow.EmitStateMachine(reference, operations.Node(reference.Owner.UID), child.Operations.Nodes);
                        break;
                    case BtsmtlSkillGraphReferenceKind.StateBody:
                        m_Flow.EmitStateBody(reference, operations.Node(reference.Owner.UID), child.Operations.Nodes);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            m_Flow.EmitEdges(graph, operations, stateOwner, (condition, owner) => Compile(condition, owner).Entry);
            PublishPortSources(graph, operations);
            OperationHandle entry = macro?.Entry ?? FindEntry(graph, operations);
            var result = new BtsmtlSkillGraphCompilation(entry, operations, macro);
            m_Graphs.Add(graph.Route, result);
            return result;
        }

        void PublishPortSources(BtsmtlSkillGraphOccurrence graph, BtsmtlSkillOperationBindings operations)
        {
            foreach (FlowNode node in graph.Nodes)
            {
                foreach (FlowInput port in node.GetInputFlowPorts())
                    m_Builder.DeclareOperationPortSource(operations.Node(node.UID), Source(graph, node, port.ID));
                foreach (FlowOutput port in node.GetOutputFlowPorts())
                    m_Builder.DeclareOperationPortSource(operations.FlowSource(node.UID), Source(graph, node, port.ID));
                foreach (ValueInput port in node.GetInputValuePorts())
                    m_Builder.DeclareOperationPortSource(operations.Input(node.UID, port.ID).Operation, Source(graph, node, port.ID));
                foreach (ValueOutput port in node.GetOutputValuePorts())
                    m_Builder.DeclareOperationPortSource(operations.Output(node.UID, port.ID).Operation, Source(graph, node, port.ID));
            }
        }

        static OperationHandle FindEntry(BtsmtlSkillGraphOccurrence graph, BtsmtlSkillOperationBindings operations)
        {
            FlowNode entry = graph.Role switch
            {
                BtsmtlSkillFlowGraphRole.Skill or BtsmtlSkillFlowGraphRole.StateBody => graph.Nodes.OfType<BtsmtlSkillRootFlowNode>().Single(),
                BtsmtlSkillFlowGraphRole.StateMachine => graph.Nodes.OfType<BtsmtlSkillStateEnterFlowNode>().Single(),
                BtsmtlSkillFlowGraphRole.ConditionRule => graph.Nodes.OfType<BtsmtlSkillConditionResultFlowNode>().Single(),
                _ => throw new InvalidOperationException($"{graph.Route}: 未登记图入口规则。")
            };
            return operations.Node(entry.UID);
        }

        static CharacterSimulationSourceLocation Source(BtsmtlSkillGraphOccurrence graph, FlowNode node, string portId = "") =>
            new(node.GetType().FullName, graph.GraphId, node.UID, string.Empty, string.Empty, string.Empty,
                string.IsNullOrEmpty(portId) ? $"{graph.Route}/node:{node.UID}" : $"{graph.Route}/node:{node.UID}/port:{portId}",
                portId: portId, contentHash: graph.ContentHash);
    }
}
