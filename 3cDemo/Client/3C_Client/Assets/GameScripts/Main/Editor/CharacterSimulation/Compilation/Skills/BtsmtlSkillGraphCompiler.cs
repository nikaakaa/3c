using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using FlowCanvas.Macros;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public interface IBtsmtlSkillBlackboardCompilation
    {
        void BeginSkillGraph(BtsmtlSkillGraphOccurrence graph, OperationHandle stateOwner);
        void CompleteGraph(string route, OperationHandle entry);
        void EndGraph();
    }
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
        readonly BtsmtlSkillTimelineCompiler m_Timelines;
        readonly IBtsmtlSkillBlackboardCompilation m_Blackboard;
        readonly Action<FlowNode, OperationHandle, string, CharacterSimulationSourceLocation> m_BindDomain;
        readonly Dictionary<string, BtsmtlSkillGraphCompilation> m_Graphs = new(StringComparer.Ordinal);

        public BtsmtlSkillGraphCompiler(CharacterSimulationProgramBuilder builder,
            Action<FlowNode, OperationHandle, string, CharacterSimulationSourceLocation> bindDomain,
            TimelineSemanticEmitterRegistry timelineEmitters, IBtsmtlSkillBlackboardCompilation blackboard)
        {
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_BindDomain = bindDomain ?? throw new ArgumentNullException(nameof(bindDomain));
            m_Blackboard = blackboard ?? throw new ArgumentNullException(nameof(blackboard));
            m_Leaves = new BtsmtlSkillFlowLeafEmitter(builder);
            m_Flow = new BtsmtlSkillGraphFlowEmitter(builder);
            m_Timelines = new BtsmtlSkillTimelineCompiler(timelineEmitters, builder, Compile);
        }

        public BtsmtlSkillGraphCompilation Compile(BtsmtlSkillGraphOccurrence graph, OperationHandle stateOwner) =>
            Compile(graph, stateOwner, default);

        BtsmtlSkillGraphCompilation Compile(BtsmtlSkillGraphOccurrence graph, OperationHandle stateOwner, BtsmtlSkillInvocationContext context)
        {
            if (m_Graphs.TryGetValue(graph.Route, out BtsmtlSkillGraphCompilation existing))
                return existing;
            using var invocation = m_Builder.PushGraphInvocation(graph.Route);
            m_Blackboard.BeginSkillGraph(graph, stateOwner);
            try
            {
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
                OperationHandle entry = macro?.Entry ?? FindEntry(graph, operations);
                if (graph.Role == BtsmtlSkillFlowGraphRole.ConditionRule && !context.Owner.IsValid)
                    throw new InvalidOperationException("条件页必须继承明确的调用生命周期。");
                OperationHandle invocationOwner = context.Owner.IsValid ? context.Owner : context.UseTimelineEnable
                    ? operations.Node(graph.Nodes.OfType<BtsmtlSkillTimelineEnableFlowNode>().Single().UID)
                    : entry;
                m_Builder.DeclareGraphInvocation(invocationOwner, new CharacterSimulationSourceLocation(
                    graph.Graph.GetType().FullName, graph.GraphId, string.Empty, string.Empty, string.Empty, string.Empty,
                    graph.Route, contentHash: graph.ContentHash), context.CallerKind, context.CallerId, context.ClipId);
                foreach (BtsmtlSkillGraphReferenceOccurrence reference in graph.References)
                {
                    OperationHandle childStateOwner = reference.Kind == BtsmtlSkillGraphReferenceKind.StateBody
                        ? operations.Node(reference.Owner.UID)
                        : stateOwner;
                    OperationHandle childInvocationOwner = reference.Kind == BtsmtlSkillGraphReferenceKind.Macro
                        ? default : operations.Node(reference.Owner.UID);
                    BtsmtlSkillGraphCompilation child = Compile(reference.Child, childStateOwner,
                        BtsmtlSkillInvocationContext.Call(reference.Owner.UID, childInvocationOwner));
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
                m_Flow.EmitEdges(graph, operations, stateOwner, (condition, owner) => Compile(condition.Condition, owner,
                    BtsmtlSkillInvocationContext.Condition(condition.Edge.UID,
                        condition.Edge.sourceNode is BtsmtlSkillStateFlowNode ? owner : invocationOwner)).Entry);
                foreach (BtsmtlSkillTimelineOccurrence timeline in graph.Timelines)
                    m_Timelines.Emit(graph, timeline, operations.Node(timeline.Node.UID), stateOwner);
                PublishPortSources(graph, operations);
                m_Blackboard.CompleteGraph(graph.Route, entry);
                var result = new BtsmtlSkillGraphCompilation(entry, operations, macro);
                m_Graphs.Add(graph.Route, result);
                return result;
            }
            finally
            {
                m_Blackboard.EndGraph();
            }
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
                {
                    BtsmtlSkillValuePortBinding binding = operations.Input(node.UID, port.ID);
                    m_Builder.DeclareOperationPortSource(binding.Operation, Source(graph, node, port.ID), binding.PortId, ProgramValuePortDirection.Input);
                }
                foreach (ValueOutput port in node.GetOutputValuePorts())
                {
                    BtsmtlSkillValuePortBinding binding = operations.Output(node.UID, port.ID);
                    m_Builder.DeclareOperationPortSource(binding.Operation, Source(graph, node, port.ID), binding.PortId, ProgramValuePortDirection.Output);
                }
            }
        }

        static OperationHandle FindEntry(BtsmtlSkillGraphOccurrence graph, BtsmtlSkillOperationBindings operations)
        {
            FlowNode entry = graph.Role switch
            {
                BtsmtlSkillFlowGraphRole.Skill or BtsmtlSkillFlowGraphRole.StateBody or BtsmtlSkillFlowGraphRole.TimelineBody => graph.Nodes.OfType<BtsmtlSkillRootFlowNode>().Single(),
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
