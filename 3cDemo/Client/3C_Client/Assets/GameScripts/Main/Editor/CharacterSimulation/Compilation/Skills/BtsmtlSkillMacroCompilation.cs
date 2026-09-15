using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using FlowCanvas.Macros;
using ParadoxNotion;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class BtsmtlSkillMacroCompilation
    {
        readonly GameplayAbilitySemanticBuilder m_Builder;
        readonly BtsmtlSkillGraphOccurrence m_Graph;
        readonly List<ProgramGraphParameterBinding> m_Inputs = new();
        readonly List<ProgramGraphParameterBinding> m_Outputs = new();
        readonly List<int> m_StateSlots = new();

        public BtsmtlSkillMacroCompilation(GameplayAbilitySemanticBuilder builder, BtsmtlSkillGraphOccurrence graph,
            BtsmtlSkillOperationBindings operations)
        {
            m_Builder = builder;
            m_Graph = graph;
            var macro = (BtsmtlSkillMacroGraph)graph.Graph;
            BtsmtlSkillMacroInterface.Validate(macro);
            MacroInputNode input = graph.Nodes.OfType<MacroInputNode>().Single();
            MacroOutputNode output = graph.Nodes.OfType<MacroOutputNode>().Single();
            Entry = Declare(input, "entry", SimulationOperationCode.Root);
            OperationHandle body = Declare(input, "body-and-results", SimulationOperationCode.Sequence);
            OperationHandle results = Declare(output, "results", SimulationOperationCode.Sequence);
            operations.AddNode(input, Entry);
            operations.SetFlowSource(input, body);
            operations.AddNode(output, results);
            Child(Entry, body, 0, Source(input, "entry"));
            Child(body, results, 1, Source(output, "results"));
            foreach (DynamicParameterDefinition parameter in macro.inputDefinitions)
            {
                if (parameter.type == typeof(Flow))
                    continue;
                SimulationSourceLocation source = Source(input, "parameter-read", parameter.ID);
                int slot = DeclareParameter(parameter, ProgramGraphParameterDirection.Input, source);
                OperationHandle reader = m_Builder.DeclareOperation(source, SimulationOperationCode.BlackboardGet,
                    Array.Empty<int>(), text0: parameter.ID);
                BindState(reader, slot, source);
                operations.AddValueOutput(input, parameter.ID, reader, "m_Output");
            }
            int order = 0;
            foreach (DynamicParameterDefinition parameter in macro.outputDefinitions)
            {
                SimulationSourceLocation source = Source(output, "parameter-write", parameter.ID);
                int slot = DeclareParameter(parameter, ProgramGraphParameterDirection.Output, source);
                OperationHandle writer = m_Builder.DeclareOperation(source, SimulationOperationCode.BlackboardSet,
                    Array.Empty<int>(), integer0: 1, text0: parameter.ID);
                BindState(writer, slot, source);
                operations.AddValueInput(output, parameter.ID, writer, "m_Value");
                Child(results, writer, order++, source);
                var port = (ValueInput)output.GetInputPort(parameter.ID);
                if (!port.isConnected)
                    BindDefault(writer, "m_Value", port, source);
            }
            if (m_StateSlots.Count != 0)
                m_Builder.DeclareScope($"{graph.Route}/scope:macro-parameters", ProgramScopeKind.Graph,
                    graph.Route, Entry, m_StateSlots, Source(input, "parameters"));
        }

        public OperationHandle Entry { get; }

        public OperationHandle EmitCall(BtsmtlSkillGraphReferenceOccurrence reference, BtsmtlSkillOperationBindings parent)
        {
            var node = (MacroNodeWrapper)reference.Owner;
            var source = new SimulationSourceLocation(node.GetType().FullName,
                ((IBtsmtlSkillFlowGraph)node.graph).AuthoringId, node.UID, string.Empty, string.Empty, string.Empty,
                reference.CallSiteIdentity, contentHash: reference.OwnerContentHash);
            OperationHandle call = m_Builder.DeclareOperation(source, SimulationOperationCode.SubGraph, Array.Empty<int>());
            parent.AddNode(node, call);
            m_Builder.DeclareGraphCallFrame(reference.CallSiteIdentity, call, Entry, m_Graph.GraphId, m_Inputs, m_Outputs, source);
            m_Builder.DeclareControlFlow($"{reference.CallSiteIdentity}/entry", call, Entry, "Macro", "Entry",
                ProgramControlFlowKind.Enter, 0, 0, ProgramAbortPolicy.None, false, OperationHandle.Invalid, source);
            foreach (ValueInput port in node.GetInputValuePorts())
            {
                parent.AddValueInput(node, port.ID, call, port.ID);
                if (!port.isConnected)
                    BindDefault(call, port.ID, port, new SimulationSourceLocation(node.GetType().FullName,
                        source.GraphId, node.UID, string.Empty, string.Empty, string.Empty,
                        $"{reference.CallSiteIdentity}/port:{port.ID}", portId: port.ID, contentHash: reference.OwnerContentHash));
            }
            foreach (ValueOutput port in node.GetOutputValuePorts())
                parent.AddValueOutput(node, port.ID, call, port.ID);
            return call;
        }

        int DeclareParameter(DynamicParameterDefinition parameter, ProgramGraphParameterDirection direction,
            SimulationSourceLocation source)
        {
            string owner = $"{m_Graph.Route}/parameter:{direction}:{parameter.ID}";
            ProgramStateValueKind kind = StateKind(parameter.type);
            int value = m_Builder.DeclareStandaloneStateSlot(source, kind, ProgramStateOwnerKind.Blackboard,
                ProgramStateSemantic.BlackboardValue, owner, Default(parameter.type));
            m_StateSlots.Add(value);
            m_StateSlots.Add(m_Builder.DeclareStandaloneStateSlot(source, ProgramStateValueKind.BlackboardOwnerToken,
                ProgramStateOwnerKind.Blackboard, ProgramStateSemantic.BlackboardOwnerToken, owner));
            m_StateSlots.Add(m_Builder.DeclareStandaloneStateSlot(source, ProgramStateValueKind.Int32,
                ProgramStateOwnerKind.Blackboard, ProgramStateSemantic.BlackboardLifetime, owner, (int)ProgramBlackboardLifetime.GraphInstance));
            m_StateSlots.Add(m_Builder.DeclareStandaloneStateSlot(source, ProgramStateValueKind.BlackboardWriteStamp,
                ProgramStateOwnerKind.Blackboard, ProgramStateSemantic.BlackboardWriteStamp, owner));
            var binding = new ProgramGraphParameterBinding(direction, parameter.name,
                $"macro:{m_Graph.GraphId}/parameter:{direction}:{parameter.ID}", value, parameter.ID, ValueKind(parameter.type));
            (direction == ProgramGraphParameterDirection.Input ? m_Inputs : m_Outputs).Add(binding);
            return value;
        }

        void BindDefault(OperationHandle operation, string compiledPort, ValueInput port, SimulationSourceLocation source)
        {
            object value = port.serializedValue;
            if (value == null && port.type == typeof(string))
                value = string.Empty;
            int constant = m_Builder.DeclareConstant(source, "default-value", value);
            if (constant >= 0)
                m_Builder.DeclareConstantInputBinding(operation, compiledPort, constant, ValueKind(port.type), source);
        }

        void BindState(OperationHandle operation, int slot, SimulationSourceLocation source) =>
            m_Builder.DeclareReference($"{source.Identity}/state", operation, ProgramReferenceKind.StateSlot, slot, source.PortId, source);

        OperationHandle Declare(FlowNode node, string phase, SimulationOperationCode code) =>
            m_Builder.DeclareOperation(Source(node, phase), code, Array.Empty<int>());

        void Child(OperationHandle owner, OperationHandle child, int order, SimulationSourceLocation source) =>
            m_Builder.DeclareControlFlow($"{source.Identity}/child:{order}", owner, child, "Output", "Input",
                ProgramControlFlowKind.Child, order, 0, ProgramAbortPolicy.None, false, OperationHandle.Invalid, source);

        SimulationSourceLocation Source(FlowNode node, string phase, string port = "") =>
            new(node.GetType().FullName, m_Graph.GraphId, node.UID, string.Empty, string.Empty, string.Empty,
                $"{m_Graph.Route}/node:{node.UID}/phase:{phase}/port:{port}", portId: port, contentHash: m_Graph.ContentHash);

        static object Default(Type type) => type == typeof(string) ? string.Empty : Activator.CreateInstance(type);

        static SemanticValueKind ValueKind(Type type) => StateKind(type) switch
        {
            ProgramStateValueKind.Boolean => SemanticValueKind.Boolean,
            ProgramStateValueKind.Int32 => SemanticValueKind.Int32,
            ProgramStateValueKind.UInt64 => SemanticValueKind.UInt64,
            ProgramStateValueKind.Scalar => SemanticValueKind.Number,
            ProgramStateValueKind.Vector2 => SemanticValueKind.Vector2,
            ProgramStateValueKind.Vector3 => SemanticValueKind.Vector3,
            ProgramStateValueKind.Identity => SemanticValueKind.Identity,
            _ => throw new InvalidOperationException("Macro参数没有对应的编译值类型。")
        };

        static ProgramStateValueKind StateKind(Type type)
        {
            BtsmtlSkillMacroInterface.RequireValueType(type);
            if (type == typeof(bool)) return ProgramStateValueKind.Boolean;
            if (type == typeof(int)) return ProgramStateValueKind.Int32;
            if (type == typeof(uint) || type == typeof(ulong)) return ProgramStateValueKind.UInt64;
            if (type == typeof(float)) return ProgramStateValueKind.Scalar;
            if (type == typeof(Vector2)) return ProgramStateValueKind.Vector2;
            if (type == typeof(Vector3)) return ProgramStateValueKind.Vector3;
            if (type == typeof(string)) return ProgramStateValueKind.Identity;
            throw new InvalidOperationException($"Macro参数类型'{type}'没有状态存储合同。");
        }
    }
}
