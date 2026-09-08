using System;
using System.Collections.Generic;
using FlowCanvas;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public readonly struct BtsmtlSkillValuePortBinding
    {
        public BtsmtlSkillValuePortBinding(OperationHandle operation, string portId)
        {
            if (!operation.IsValid || string.IsNullOrWhiteSpace(portId))
                throw new ArgumentException("编译值端口必须有操作及端口身份。");
            Operation = operation;
            PortId = portId;
        }

        public OperationHandle Operation { get; }
        public string PortId { get; }
    }

    public sealed class BtsmtlSkillOperationBindings
    {
        readonly Dictionary<string, OperationHandle> m_Nodes = new(StringComparer.Ordinal);
        readonly Dictionary<string, OperationHandle> m_FlowSources = new(StringComparer.Ordinal);
        readonly Dictionary<(string Node, string Port), BtsmtlSkillValuePortBinding> m_Inputs = new();
        readonly Dictionary<(string Node, string Port), BtsmtlSkillValuePortBinding> m_Outputs = new();

        public IReadOnlyDictionary<string, OperationHandle> Nodes => m_Nodes;
        public OperationHandle Node(string identity) => m_Nodes[identity];
        public OperationHandle FlowSource(string identity) => m_FlowSources[identity];
        public BtsmtlSkillValuePortBinding Input(string nodeId, string portId) => m_Inputs[(nodeId, portId)];
        public BtsmtlSkillValuePortBinding Output(string nodeId, string portId) => m_Outputs[(nodeId, portId)];

        public void AddNode(FlowNode node, OperationHandle operation)
        {
            m_Nodes.Add(node.UID, operation);
            m_FlowSources.Add(node.UID, operation);
        }

        public void SetFlowSource(FlowNode node, OperationHandle operation) => m_FlowSources[node.UID] = operation;

        public void AddValueInput(FlowNode node, string authoringPort, OperationHandle operation, string compiledPort) =>
            m_Inputs.Add((node.UID, authoringPort), new BtsmtlSkillValuePortBinding(operation, compiledPort));

        public void AddValueOutput(FlowNode node, string authoringPort, OperationHandle operation, string compiledPort) =>
            m_Outputs.Add((node.UID, authoringPort), new BtsmtlSkillValuePortBinding(operation, compiledPort));

        public void AddLeaf(FlowNode node, OperationHandle operation)
        {
            AddNode(node, operation);
            BtsmtlSkillNativeNodeCatalog.TryGet(node.GetType(), out BtsmtlSkillNativeNodeContract native);
            foreach (ValueInput input in node.GetInputValuePorts())
                AddValueInput(node, input.ID, operation, native != null ? native.Input(input.ID) : input.ID);
            foreach (ValueOutput output in node.GetOutputValuePorts())
                AddValueOutput(node, output.ID, operation, native != null ? native.Output(output.ID) : output.ID);
        }
    }
}
