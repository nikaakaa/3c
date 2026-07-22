using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum CharacterPoseGraphValidationCode : byte
    {
        GraphMissing = 1,
        GraphIdentityInvalid = 2,
        SlotInvalid = 3,
        SlotDuplicate = 4,
        ChannelDuplicate = 5,
        ChannelCoverageMissing = 6,
        ParameterInvalid = 7,
        ParameterDuplicate = 8,
        NodeInvalid = 9,
        NodeDuplicate = 10,
        PortInvalid = 11,
        PortDuplicate = 12,
        PortShapeInvalid = 13,
        EdgeInvalid = 14,
        EdgeDuplicate = 15,
        PortTypeMismatch = 16,
        IllegalFanIn = 17,
        RequiredInputMissing = 18,
        Cycle = 19,
        OutputCountInvalid = 20,
        SlotInputCountInvalid = 21,
        SlotUnreachable = 22,
        RequireOutputPathMissing = 23,
        MaskInvalid = 24,
        AdditiveInvalid = 25,
        ParameterPolicyMissing = 26,
        SubgraphOwnershipInvalid = 27,
        SharedSubgraphCycle = 28,
        UnsupportedDomainData = 29,
        InterfaceBoundaryInvalid = 30,
        InterfaceIdentityInvalid = 31,
        InterfaceBindingInvalid = 32,
        InterfaceDangling = 33,
        SubgraphCycle = 34
    }

    public readonly struct CharacterPoseGraphValidationIssue
    {
        public CharacterPoseGraphValidationIssue(
            CharacterPoseGraphValidationCode code,
            string message,
            string graphId = "",
            PoseNodeId nodeId = default,
            PosePortId portId = default)
        {
            Code = code;
            Message = message ?? string.Empty;
            GraphId = graphId ?? string.Empty;
            NodeId = nodeId;
            PortId = portId;
        }

        public CharacterPoseGraphValidationCode Code { get; }
        public string Message { get; }
        public string GraphId { get; }
        public PoseNodeId NodeId { get; }
        public PosePortId PortId { get; }
    }

    public sealed class CharacterPoseGraphValidationReport
    {
        readonly List<CharacterPoseGraphValidationIssue> m_Issues = new List<CharacterPoseGraphValidationIssue>();
        public IReadOnlyList<CharacterPoseGraphValidationIssue> Issues => m_Issues;
        public bool IsValid => m_Issues.Count == 0;
        internal void Add(CharacterPoseGraphValidationIssue issue) => m_Issues.Add(issue);

        public void CopyMessagesTo(List<string> destination)
        {
            if (destination == null)
                return;
            for (int i = 0; i < m_Issues.Count; i++)
                destination.Add(m_Issues[i].Message);
        }
    }

    public static class CharacterPresentationPoseGraphValidator
    {
        readonly struct InterfaceBindingExpectation
        {
            public InterfaceBindingExpectation(
                CharacterPosePortDefinition boundaryPort,
                CharacterPosePortDirection callSiteDirection)
            {
                BoundaryPort = boundaryPort;
                CallSiteDirection = callSiteDirection;
            }

            public CharacterPosePortDefinition BoundaryPort { get; }
            public CharacterPosePortDirection CallSiteDirection { get; }
        }

        public static CharacterPoseGraphValidationReport Validate(
            CharacterPresentationPoseGraphAsset asset,
            CharacterAnimationRigDefinition rig,
            IReadOnlyCollection<AnimationChannelId> reachableChannels = null)
        {
            var report = new CharacterPoseGraphValidationReport();
            if (!asset || asset.Graph == null)
            {
                Report(report, CharacterPoseGraphValidationCode.GraphMissing, "Character Presentation Pose Graph is missing.");
                return report;
            }
            if (!rig)
            {
                Report(report, CharacterPoseGraphValidationCode.MaskInvalid, "Character Presentation Pose Graph validation requires one Animation Rig Definition.", asset.Graph.GraphId);
                return report;
            }
            try
            {
                rig.RequireValid();
            }
            catch (Exception exception)
            {
                Report(report, CharacterPoseGraphValidationCode.MaskInvalid, exception.Message, asset.Graph.GraphId);
                return report;
            }
            var sharedPath = new HashSet<CharacterPresentationPoseGraphAsset> { asset };
            var graphPath = new HashSet<CharacterPoseGraphData> { asset.Graph };
            ValidateGraph(asset.Graph, rig, reachableChannels, true, sharedPath, graphPath, null, report);
            return report;
        }

        static void ValidateGraph(
            CharacterPoseGraphData graph,
            CharacterAnimationRigDefinition rig,
            IReadOnlyCollection<AnimationChannelId> reachableChannels,
            bool root,
            HashSet<CharacterPresentationPoseGraphAsset> sharedPath,
            HashSet<CharacterPoseGraphData> graphPath,
            IReadOnlyDictionary<PoseParameterId, float> rootParameters,
            CharacterPoseGraphValidationReport report)
        {
            if (graph == null || string.IsNullOrWhiteSpace(graph.GraphId) || string.IsNullOrWhiteSpace(graph.ContentRevision))
            {
                Report(report, CharacterPoseGraphValidationCode.GraphIdentityInvalid, "Pose Graph identity or content revision is missing.", graph?.GraphId);
                return;
            }

            var slotById = new Dictionary<PoseSlotId, CharacterPoseSlotDeclaration>();
            var channelById = new Dictionary<AnimationChannelId, CharacterPoseSlotDeclaration>();
            for (int i = 0; i < graph.PoseSlots.Count; i++)
            {
                CharacterPoseSlotDeclaration slot = graph.PoseSlots[i];
                if (slot == null || !slot.PoseSlotId.IsValid || !slot.AnimationChannelId.IsValid ||
                    !Enum.IsDefined(typeof(PoseSlotOutputPolicy), slot.OutputPolicy))
                {
                    Report(report, CharacterPoseGraphValidationCode.SlotInvalid, $"Pose Graph '{graph.GraphId}' Slot #{i} is invalid.", graph.GraphId);
                    continue;
                }
                if (!slotById.TryAdd(slot.PoseSlotId, slot))
                    Report(report, CharacterPoseGraphValidationCode.SlotDuplicate, $"Pose Graph '{graph.GraphId}' duplicates Pose Slot '{slot.PoseSlotId}'.", graph.GraphId);
                if (!channelById.TryAdd(slot.AnimationChannelId, slot))
                    Report(report, CharacterPoseGraphValidationCode.ChannelDuplicate, $"Pose Graph '{graph.GraphId}' binds Animation Channel '{slot.AnimationChannelId}' more than once.", graph.GraphId);
            }
            if (root && reachableChannels != null)
            {
                foreach (AnimationChannelId channel in reachableChannels)
                {
                    if (!channel.IsValid || !channelById.ContainsKey(channel))
                        Report(report, CharacterPoseGraphValidationCode.ChannelCoverageMissing, $"Pose Graph '{graph.GraphId}' does not bind reachable Animation Channel '{channel}'.", graph.GraphId);
                }
                foreach (AnimationChannelId channel in channelById.Keys)
                {
                    if (!reachableChannels.Contains(channel))
                        Report(report, CharacterPoseGraphValidationCode.ChannelCoverageMissing, $"Pose Graph '{graph.GraphId}' binds unknown Animation Channel '{channel}'.", graph.GraphId);
                }
            }

            var parameters = new HashSet<PoseParameterId>();
            var parameterDefaults = new Dictionary<PoseParameterId, float>();
            for (int i = 0; i < graph.Parameters.Count; i++)
            {
                CharacterPoseParameterDeclaration parameter = graph.Parameters[i];
                if (parameter == null || !parameter.ParameterId.IsValid || !float.IsFinite(parameter.DefaultValue))
                {
                    Report(report, CharacterPoseGraphValidationCode.ParameterInvalid, $"Pose Graph '{graph.GraphId}' Parameter #{i} is invalid.", graph.GraphId);
                    continue;
                }
                if (!parameters.Add(parameter.ParameterId))
                    Report(report, CharacterPoseGraphValidationCode.ParameterDuplicate, $"Pose Graph '{graph.GraphId}' duplicates Pose Parameter '{parameter.ParameterId}'.", graph.GraphId);
                else
                    parameterDefaults.Add(parameter.ParameterId, parameter.DefaultValue);
            }
            if (root)
                rootParameters = parameterDefaults;
            else
                ValidateSubgraphParameters(graph, parameterDefaults, rootParameters, report);

            var nodes = new Dictionary<PoseNodeId, CharacterPoseNodeDefinition>();
            var ports = new Dictionary<string, CharacterPosePortDefinition>(StringComparer.Ordinal);
            var slotInputs = new Dictionary<PoseSlotId, int>();
            int outputCount = 0;
            int graphInputCount = 0;
            int graphOutputCount = 0;
            PoseNodeId outputNodeId = default;
            var interfaceIds = new HashSet<PoseInterfacePortId>();
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                CharacterPoseNodeDefinition node = graph.Nodes[i];
                if (node == null || !node.NodeId.IsValid || !Enum.IsDefined(typeof(CharacterPoseNodeKind), node.Kind))
                {
                    Report(report, CharacterPoseGraphValidationCode.NodeInvalid, $"Pose Graph '{graph.GraphId}' Node #{i} is invalid.", graph.GraphId);
                    continue;
                }
                if (!nodes.TryAdd(node.NodeId, node))
                {
                    Report(report, CharacterPoseGraphValidationCode.NodeDuplicate, $"Pose Graph '{graph.GraphId}' duplicates Node '{node.NodeId}'.", graph.GraphId, node.NodeId);
                    continue;
                }
                ValidateNodeShape(
                    graph,
                    node,
                    parameters,
                    rig,
                    slotById,
                    slotInputs,
                    sharedPath,
                    graphPath,
                    rootParameters,
                    report);
                var localPorts = new HashSet<PosePortId>();
                for (int portIndex = 0; portIndex < node.Ports.Count; portIndex++)
                {
                    CharacterPosePortDefinition port = node.Ports[portIndex];
                    if (port == null || !port.PortId.IsValid || !Enum.IsDefined(typeof(CharacterPosePortKind), port.Kind) ||
                        !Enum.IsDefined(typeof(CharacterPosePortDirection), port.Direction))
                    {
                        Report(report, CharacterPoseGraphValidationCode.PortInvalid, $"Pose Node '{node.NodeId}' Port #{portIndex} is invalid.", graph.GraphId, node.NodeId);
                        continue;
                    }
                    if (!localPorts.Add(port.PortId))
                        Report(report, CharacterPoseGraphValidationCode.PortDuplicate, $"Pose Node '{node.NodeId}' duplicates Port '{port.PortId}'.", graph.GraphId, node.NodeId, port.PortId);
                    bool interfacePort = node.Kind == CharacterPoseNodeKind.GraphInput ||
                                         node.Kind == CharacterPoseNodeKind.GraphOutput ||
                                         node.Kind == CharacterPoseNodeKind.PoseSubgraph;
                    if (interfacePort)
                    {
                        if (!port.InterfacePortId.IsValid ||
                            string.Equals(port.PortId.Value, port.InterfacePortId.Value, StringComparison.Ordinal))
                        {
                            Report(report, CharacterPoseGraphValidationCode.InterfaceIdentityInvalid,
                                $"Pose Node '{node.NodeId}' Port '{port.PortId}' requires an independent Interface Port identity.",
                                graph.GraphId, node.NodeId, port.PortId);
                        }
                        if ((node.Kind == CharacterPoseNodeKind.GraphInput || node.Kind == CharacterPoseNodeKind.GraphOutput) &&
                            port.InterfacePortId.IsValid && !interfaceIds.Add(port.InterfacePortId))
                        {
                            Report(report, CharacterPoseGraphValidationCode.InterfaceIdentityInvalid,
                                $"Pose Subgraph '{graph.GraphId}' duplicates Interface Port '{port.InterfacePortId}'.",
                                graph.GraphId, node.NodeId, port.PortId);
                        }
                    }
                    else if (port.InterfacePortId.IsValid)
                    {
                        Report(report, CharacterPoseGraphValidationCode.InterfaceIdentityInvalid,
                            $"Runtime Pose Node '{node.NodeId}' Port '{port.PortId}' cannot declare an Interface Port identity.",
                            graph.GraphId, node.NodeId, port.PortId);
                    }
                    ports[node.NodeId.Value + "\0" + port.PortId.Value] = port;
                }
                if (node.Kind == CharacterPoseNodeKind.OutputPose)
                {
                    outputCount++;
                    outputNodeId = node.NodeId;
                }
                else if (node.Kind == CharacterPoseNodeKind.GraphInput)
                {
                    graphInputCount++;
                }
                else if (node.Kind == CharacterPoseNodeKind.GraphOutput)
                {
                    graphOutputCount++;
                }
            }
            if (root)
            {
                if (outputCount != 1)
                    Report(report, CharacterPoseGraphValidationCode.OutputCountInvalid, $"Root Pose Graph '{graph.GraphId}' requires exactly one OutputPose node, found {outputCount}.", graph.GraphId);
                if (graphInputCount != 0 || graphOutputCount != 0)
                    Report(report, CharacterPoseGraphValidationCode.InterfaceBoundaryInvalid, $"Root Pose Graph '{graph.GraphId}' cannot contain GraphInput or GraphOutput nodes.", graph.GraphId);
            }
            else
            {
                if (outputCount != 0)
                    Report(report, CharacterPoseGraphValidationCode.OutputCountInvalid, $"Pose Subgraph '{graph.GraphId}' cannot contain OutputPose nodes.", graph.GraphId);
                if (graphInputCount != 1 || graphOutputCount != 1)
                    Report(report, CharacterPoseGraphValidationCode.InterfaceBoundaryInvalid, $"Pose Subgraph '{graph.GraphId}' requires exactly one GraphInput and one GraphOutput node.", graph.GraphId);
                if (graph.PoseSlots.Count != 0)
                    Report(report, CharacterPoseGraphValidationCode.InterfaceBoundaryInvalid, $"Pose Subgraph '{graph.GraphId}' must receive poses through GraphInput and cannot declare Pose Slots.", graph.GraphId);
            }
            foreach (KeyValuePair<PoseSlotId, CharacterPoseSlotDeclaration> pair in slotById)
            {
                int count = slotInputs.TryGetValue(pair.Key, out int value) ? value : 0;
                if (count != 1)
                    Report(report, CharacterPoseGraphValidationCode.SlotInputCountInvalid, $"Pose Slot '{pair.Key}' requires exactly one PoseSlotInput node, found {count}.", graph.GraphId);
            }

            var adjacency = new Dictionary<PoseNodeId, List<PoseNodeId>>();
            var reverse = new Dictionary<PoseNodeId, List<PoseNodeId>>();
            var incoming = new Dictionary<string, CharacterPoseEdge>(StringComparer.Ordinal);
            var outgoingPorts = new HashSet<string>(StringComparer.Ordinal);
            var edgeIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                CharacterPoseEdge edge = graph.Edges[i];
                if (edge == null || string.IsNullOrWhiteSpace(edge.EdgeId) || !edge.SourceNodeId.IsValid || !edge.SourcePortId.IsValid ||
                    !edge.TargetNodeId.IsValid || !edge.TargetPortId.IsValid || !edgeIds.Add(edge.EdgeId))
                {
                    Report(report, CharacterPoseGraphValidationCode.EdgeInvalid, $"Pose Graph '{graph.GraphId}' Edge #{i} is invalid or duplicated.", graph.GraphId);
                    continue;
                }
                string sourceKey = edge.SourceNodeId.Value + "\0" + edge.SourcePortId.Value;
                string targetKey = edge.TargetNodeId.Value + "\0" + edge.TargetPortId.Value;
                if (!nodes.ContainsKey(edge.SourceNodeId) || !nodes.ContainsKey(edge.TargetNodeId) ||
                    !ports.TryGetValue(sourceKey, out CharacterPosePortDefinition sourcePort) ||
                    !ports.TryGetValue(targetKey, out CharacterPosePortDefinition targetPort) ||
                    sourcePort.Direction != CharacterPosePortDirection.Output || targetPort.Direction != CharacterPosePortDirection.Input)
                {
                    Report(report, CharacterPoseGraphValidationCode.EdgeInvalid, $"Pose Edge '{edge.EdgeId}' references a missing or wrong-direction endpoint.", graph.GraphId);
                    continue;
                }
                if (sourcePort.Kind != targetPort.Kind)
                    Report(report, CharacterPoseGraphValidationCode.PortTypeMismatch, $"Pose Edge '{edge.EdgeId}' connects incompatible Port kinds '{sourcePort.Kind}' and '{targetPort.Kind}'.", graph.GraphId);
                if (!incoming.TryAdd(targetKey, edge))
                    Report(report, CharacterPoseGraphValidationCode.IllegalFanIn, $"Pose Port '{edge.TargetNodeId}/{edge.TargetPortId}' has more than one incoming edge.", graph.GraphId, edge.TargetNodeId, edge.TargetPortId);
                outgoingPorts.Add(sourceKey);
                Add(adjacency, edge.SourceNodeId, edge.TargetNodeId);
                Add(reverse, edge.TargetNodeId, edge.SourceNodeId);
            }
            foreach (KeyValuePair<PoseNodeId, CharacterPoseNodeDefinition> pair in nodes)
            {
                CharacterPoseNodeDefinition node = pair.Value;
                for (int i = 0; i < node.Ports.Count; i++)
                {
                    CharacterPosePortDefinition port = node.Ports[i];
                    if (port != null && port.Direction == CharacterPosePortDirection.Input && port.Required &&
                        !incoming.ContainsKey(node.NodeId.Value + "\0" + port.PortId.Value))
                    {
                        Report(report, CharacterPoseGraphValidationCode.RequiredInputMissing, $"Pose Node '{node.NodeId}' required Port '{port.PortId}' is disconnected.", graph.GraphId, node.NodeId, port.PortId);
                    }
                    if (port != null && port.Direction == CharacterPosePortDirection.Output && port.Required &&
                        (node.Kind == CharacterPoseNodeKind.GraphInput || node.Kind == CharacterPoseNodeKind.PoseSubgraph) &&
                        !outgoingPorts.Contains(node.NodeId.Value + "\0" + port.PortId.Value))
                    {
                        Report(report, CharacterPoseGraphValidationCode.InterfaceDangling,
                            $"Pose interface output '{node.NodeId}/{port.PortId}' is required but disconnected.",
                            graph.GraphId, node.NodeId, port.PortId);
                    }
                }
            }
            DetectCycles(graph.GraphId, nodes.Keys, adjacency, report);
            if (root && outputCount == 1)
                ValidateReachability(graph, nodes, reverse, outputNodeId, slotById, report);
        }

        static void ValidateNodeShape(
            CharacterPoseGraphData graph,
            CharacterPoseNodeDefinition node,
            HashSet<PoseParameterId> parameters,
            CharacterAnimationRigDefinition rig,
            Dictionary<PoseSlotId, CharacterPoseSlotDeclaration> slots,
            Dictionary<PoseSlotId, int> slotInputs,
            HashSet<CharacterPresentationPoseGraphAsset> sharedPath,
            HashSet<CharacterPoseGraphData> graphPath,
            IReadOnlyDictionary<PoseParameterId, float> rootParameters,
            CharacterPoseGraphValidationReport report)
        {
            int poseInputs = 0;
            int poseOutputs = 0;
            for (int i = 0; i < node.Ports.Count; i++)
            {
                CharacterPosePortDefinition port = node.Ports[i];
                if (port == null || port.Kind != CharacterPosePortKind.Pose)
                    continue;
                if (port.Direction == CharacterPosePortDirection.Input)
                    poseInputs++;
                else
                    poseOutputs++;
            }
            int expectedInputs;
            int expectedOutputs;
            switch (node.Kind)
            {
                case CharacterPoseNodeKind.PoseSlotInput:
                    expectedInputs = 0;
                    expectedOutputs = 1;
                    if (!node.PoseSlotId.IsValid || !slots.ContainsKey(node.PoseSlotId))
                        Report(report, CharacterPoseGraphValidationCode.SlotInvalid, $"PoseSlotInput '{node.NodeId}' references unknown Pose Slot '{node.PoseSlotId}'.", graph.GraphId, node.NodeId);
                    else
                        slotInputs[node.PoseSlotId] = slotInputs.TryGetValue(node.PoseSlotId, out int count) ? count + 1 : 1;
                    break;
                case CharacterPoseNodeKind.LayeredBoneBlend:
                case CharacterPoseNodeKind.AdditivePose:
                    expectedInputs = 2;
                    expectedOutputs = 1;
                    ValidateMask(graph, node, rig, report);
                    ValidateParameterPolicies(graph, node, parameters, report);
                    if (node.Kind == CharacterPoseNodeKind.AdditivePose &&
                        (!string.Equals(
                             node.AdditiveReferencePoseId,
                             AnimationAdditiveReferencePoseIds.RigReference,
                             StringComparison.Ordinal) ||
                         !Enum.IsDefined(typeof(AdditiveReferenceSpace), node.AdditiveReferenceSpace) ||
                         !Enum.IsDefined(typeof(AdditiveScalePolicy), node.AdditiveScalePolicy)))
                    {
                        Report(report, CharacterPoseGraphValidationCode.AdditiveInvalid, $"AdditivePose '{node.NodeId}' must use reference pose '{AnimationAdditiveReferencePoseIds.RigReference}' with a valid space and scale policy.", graph.GraphId, node.NodeId);
                    }
                    break;
                case CharacterPoseNodeKind.PoseCurveResolve:
                    expectedInputs = 2;
                    expectedOutputs = 1;
                    ValidateParameterPolicies(graph, node, parameters, report);
                    break;
                case CharacterPoseNodeKind.PoseSubgraph:
                    ValidateSubgraph(
                        graph,
                        node,
                        rig,
                        sharedPath,
                        graphPath,
                        rootParameters,
                        report);
                    return;
                case CharacterPoseNodeKind.OutputPose:
                    expectedInputs = 1;
                    expectedOutputs = 0;
                    break;
                case CharacterPoseNodeKind.GraphInput:
                    expectedInputs = 0;
                    expectedOutputs = poseOutputs;
                    for (int i = 0; i < node.Ports.Count; i++)
                    {
                        CharacterPosePortDefinition port = node.Ports[i];
                        if (port != null && port.Direction != CharacterPosePortDirection.Output)
                            Report(report, CharacterPoseGraphValidationCode.InterfaceBoundaryInvalid, $"GraphInput '{node.NodeId}' may only contain output ports.", graph.GraphId, node.NodeId, port.PortId);
                    }
                    break;
                case CharacterPoseNodeKind.GraphOutput:
                    expectedInputs = poseInputs;
                    expectedOutputs = 0;
                    for (int i = 0; i < node.Ports.Count; i++)
                    {
                        CharacterPosePortDefinition port = node.Ports[i];
                        if (port != null && port.Direction != CharacterPosePortDirection.Input)
                            Report(report, CharacterPoseGraphValidationCode.InterfaceBoundaryInvalid, $"GraphOutput '{node.NodeId}' may only contain input ports.", graph.GraphId, node.NodeId, port.PortId);
                    }
                    if (poseInputs == 0)
                        Report(report, CharacterPoseGraphValidationCode.InterfaceBoundaryInvalid, $"GraphOutput '{node.NodeId}' requires at least one Pose output interface.", graph.GraphId, node.NodeId);
                    break;
                default:
                    Report(report, CharacterPoseGraphValidationCode.UnsupportedDomainData, $"Pose Node '{node.NodeId}' uses unsupported code '{node.Kind}'.", graph.GraphId, node.NodeId);
                    return;
            }
            if (poseInputs != expectedInputs || poseOutputs != expectedOutputs)
                Report(report, CharacterPoseGraphValidationCode.PortShapeInvalid, $"Pose Node '{node.NodeId}' requires {expectedInputs} Pose inputs and {expectedOutputs} Pose outputs.", graph.GraphId, node.NodeId);
        }

        static void ValidateSubgraphParameters(
            CharacterPoseGraphData graph,
            IReadOnlyDictionary<PoseParameterId, float> parameters,
            IReadOnlyDictionary<PoseParameterId, float> rootParameters,
            CharacterPoseGraphValidationReport report)
        {
            if (rootParameters == null || parameters.Count != rootParameters.Count)
            {
                Report(report, CharacterPoseGraphValidationCode.InterfaceBoundaryInvalid,
                    $"Pose Subgraph '{graph.GraphId}' Parameter catalog must exactly match the root Pose Graph catalog.",
                    graph.GraphId);
                return;
            }
            foreach (KeyValuePair<PoseParameterId, float> pair in rootParameters)
            {
                if (!parameters.TryGetValue(pair.Key, out float value) || value != pair.Value)
                {
                    Report(report, CharacterPoseGraphValidationCode.InterfaceBoundaryInvalid,
                        $"Pose Subgraph '{graph.GraphId}' Parameter '{pair.Key}' does not match the root Pose Graph catalog.",
                        graph.GraphId);
                }
            }
        }

        static void ValidateSubgraph(
            CharacterPoseGraphData owner,
            CharacterPoseNodeDefinition callSite,
            CharacterAnimationRigDefinition rig,
            HashSet<CharacterPresentationPoseGraphAsset> sharedPath,
            HashSet<CharacterPoseGraphData> graphPath,
            IReadOnlyDictionary<PoseParameterId, float> rootParameters,
            CharacterPoseGraphValidationReport report)
        {
            CharacterPoseSubgraphReference reference = callSite.Subgraph;
            if (reference == null || !reference.IsExclusive)
            {
                Report(report, CharacterPoseGraphValidationCode.SubgraphOwnershipInvalid,
                    $"PoseSubgraph '{callSite.NodeId}' must own exactly one inline or shared graph.",
                    owner.GraphId, callSite.NodeId);
                return;
            }

            CharacterPresentationPoseGraphAsset shared = reference.HasShared ? reference.Shared : null;
            CharacterPoseGraphData child = reference.HasInline ? reference.Inline : shared?.Graph;
            if (child == null)
            {
                Report(report, CharacterPoseGraphValidationCode.SubgraphOwnershipInvalid,
                    $"PoseSubgraph '{callSite.NodeId}' has no graph payload.",
                    owner.GraphId, callSite.NodeId);
                return;
            }
            if (shared && !sharedPath.Add(shared))
            {
                Report(report, CharacterPoseGraphValidationCode.SharedSubgraphCycle,
                    $"Shared PoseSubgraph cycle reaches '{shared.name}'.",
                    owner.GraphId, callSite.NodeId);
                return;
            }
            if (!graphPath.Add(child))
            {
                Report(report, CharacterPoseGraphValidationCode.SubgraphCycle,
                    $"PoseSubgraph call '{owner.GraphId}/{callSite.NodeId}' creates an inline/shared graph cycle through '{child.GraphId}'.",
                    owner.GraphId, callSite.NodeId);
                if (shared)
                    sharedPath.Remove(shared);
                return;
            }

            ValidateGraph(child, rig, null, false, sharedPath, graphPath, rootParameters, report);
            ValidateSubgraphBindings(owner, callSite, child, report);
            graphPath.Remove(child);
            if (shared)
                sharedPath.Remove(shared);
        }

        static void ValidateSubgraphBindings(
            CharacterPoseGraphData owner,
            CharacterPoseNodeDefinition callSite,
            CharacterPoseGraphData child,
            CharacterPoseGraphValidationReport report)
        {
            CharacterPoseNodeDefinition[] graphInputs = child.Nodes
                .Where(value => value != null && value.Kind == CharacterPoseNodeKind.GraphInput)
                .ToArray();
            CharacterPoseNodeDefinition[] graphOutputs = child.Nodes
                .Where(value => value != null && value.Kind == CharacterPoseNodeKind.GraphOutput)
                .ToArray();
            if (graphInputs.Length != 1 || graphOutputs.Length != 1)
            {
                Report(report, CharacterPoseGraphValidationCode.InterfaceBindingInvalid,
                    $"PoseSubgraph '{callSite.NodeId}' cannot bind graph '{child.GraphId}' until it has exactly one GraphInput and GraphOutput.",
                    owner.GraphId, callSite.NodeId);
                return;
            }

            var expected = new Dictionary<PoseInterfacePortId, InterfaceBindingExpectation>();
            CollectInterfaceExpectations(
                child,
                graphInputs[0],
                CharacterPosePortDirection.Input,
                expected,
                report);
            CollectInterfaceExpectations(
                child,
                graphOutputs[0],
                CharacterPosePortDirection.Output,
                expected,
                report);

            var actual = new Dictionary<PoseInterfacePortId, CharacterPosePortDefinition>();
            for (int i = 0; i < callSite.Ports.Count; i++)
            {
                CharacterPosePortDefinition port = callSite.Ports[i];
                if (port == null || !port.InterfacePortId.IsValid)
                    continue;
                if (!actual.TryAdd(port.InterfacePortId, port))
                {
                    Report(report, CharacterPoseGraphValidationCode.InterfaceBindingInvalid,
                        $"PoseSubgraph '{callSite.NodeId}' binds Interface Port '{port.InterfacePortId}' more than once.",
                        owner.GraphId, callSite.NodeId, port.PortId);
                }
            }

            foreach (KeyValuePair<PoseInterfacePortId, InterfaceBindingExpectation> pair in expected)
            {
                if (!actual.TryGetValue(pair.Key, out CharacterPosePortDefinition port))
                {
                    Report(report, CharacterPoseGraphValidationCode.InterfaceBindingInvalid,
                        $"PoseSubgraph '{callSite.NodeId}' does not bind Interface Port '{pair.Key}'.",
                        owner.GraphId, callSite.NodeId);
                    continue;
                }
                CharacterPosePortDefinition boundary = pair.Value.BoundaryPort;
                if (port.Direction != pair.Value.CallSiteDirection || port.Kind != boundary.Kind ||
                    port.Required != boundary.Required)
                {
                    Report(report, CharacterPoseGraphValidationCode.InterfaceBindingInvalid,
                        $"PoseSubgraph '{callSite.NodeId}' binding '{pair.Key}' does not match boundary direction, kind, or required state.",
                        owner.GraphId, callSite.NodeId, port.PortId);
                }
            }
            foreach (KeyValuePair<PoseInterfacePortId, CharacterPosePortDefinition> pair in actual)
            {
                if (!expected.ContainsKey(pair.Key))
                {
                    Report(report, CharacterPoseGraphValidationCode.InterfaceBindingInvalid,
                        $"PoseSubgraph '{callSite.NodeId}' binds unknown Interface Port '{pair.Key}'.",
                        owner.GraphId, callSite.NodeId, pair.Value.PortId);
                }
            }
        }

        static void CollectInterfaceExpectations(
            CharacterPoseGraphData graph,
            CharacterPoseNodeDefinition boundary,
            CharacterPosePortDirection callSiteDirection,
            Dictionary<PoseInterfacePortId, InterfaceBindingExpectation> expected,
            CharacterPoseGraphValidationReport report)
        {
            for (int i = 0; i < boundary.Ports.Count; i++)
            {
                CharacterPosePortDefinition port = boundary.Ports[i];
                if (port == null || !port.InterfacePortId.IsValid)
                    continue;
                if (!expected.TryAdd(port.InterfacePortId, new InterfaceBindingExpectation(port, callSiteDirection)))
                {
                    Report(report, CharacterPoseGraphValidationCode.InterfaceIdentityInvalid,
                        $"Pose Subgraph '{graph.GraphId}' duplicates Interface Port '{port.InterfacePortId}'.",
                        graph.GraphId, boundary.NodeId, port.PortId);
                }
            }
        }

        static void ValidateMask(CharacterPoseGraphData graph, CharacterPoseNodeDefinition node, CharacterAnimationRigDefinition rig, CharacterPoseGraphValidationReport report)
        {
            if (!node.BoneMask)
            {
                Report(report, CharacterPoseGraphValidationCode.MaskInvalid, $"Pose Node '{node.NodeId}' requires an explicit Bone Mask.", graph.GraphId, node.NodeId);
                return;
            }
            try
            {
                node.BoneMask.BuildDense(rig);
            }
            catch (Exception exception)
            {
                Report(report, CharacterPoseGraphValidationCode.MaskInvalid, $"Pose Node '{node.NodeId}' Bone Mask is invalid: {exception.Message}", graph.GraphId, node.NodeId);
            }
        }

        static void ValidateParameterPolicies(CharacterPoseGraphData graph, CharacterPoseNodeDefinition node, HashSet<PoseParameterId> parameters, CharacterPoseGraphValidationReport report)
        {
            var covered = new HashSet<PoseParameterId>();
            for (int i = 0; i < node.ParameterPolicies.Count; i++)
            {
                CharacterPoseParameterPolicy policy = node.ParameterPolicies[i];
                if (policy == null || !policy.ParameterId.IsValid || !parameters.Contains(policy.ParameterId) ||
                    !Enum.IsDefined(typeof(PoseParameterResolvePolicy), policy.Policy) || !covered.Add(policy.ParameterId))
                {
                    Report(report, CharacterPoseGraphValidationCode.ParameterPolicyMissing, $"Pose Node '{node.NodeId}' Parameter policy #{i} is invalid or duplicated.", graph.GraphId, node.NodeId);
                }
            }
            foreach (PoseParameterId parameter in parameters)
            {
                if (!covered.Contains(parameter))
                    Report(report, CharacterPoseGraphValidationCode.ParameterPolicyMissing, $"Pose Node '{node.NodeId}' has no resolve policy for Parameter '{parameter}'.", graph.GraphId, node.NodeId);
            }
        }

        static void ValidateReachability(
            CharacterPoseGraphData graph,
            Dictionary<PoseNodeId, CharacterPoseNodeDefinition> nodes,
            Dictionary<PoseNodeId, List<PoseNodeId>> reverse,
            PoseNodeId output,
            Dictionary<PoseSlotId, CharacterPoseSlotDeclaration> slots,
            CharacterPoseGraphValidationReport report)
        {
            var reachable = new HashSet<PoseNodeId>();
            var stack = new Stack<PoseNodeId>();
            stack.Push(output);
            while (stack.Count > 0)
            {
                PoseNodeId current = stack.Pop();
                if (!reachable.Add(current) || !reverse.TryGetValue(current, out List<PoseNodeId> upstream))
                    continue;
                for (int i = 0; i < upstream.Count; i++)
                    stack.Push(upstream[i]);
            }
            bool hasRequired = false;
            foreach (KeyValuePair<PoseNodeId, CharacterPoseNodeDefinition> pair in nodes)
            {
                CharacterPoseNodeDefinition node = pair.Value;
                if (node.Kind != CharacterPoseNodeKind.PoseSlotInput)
                    continue;
                if (!reachable.Contains(node.NodeId))
                    Report(report, CharacterPoseGraphValidationCode.SlotUnreachable, $"Pose Slot '{node.PoseSlotId}' is not consumed by OutputPose.", graph.GraphId, node.NodeId);
                if (reachable.Contains(node.NodeId) && slots.TryGetValue(node.PoseSlotId, out CharacterPoseSlotDeclaration slot) && slot.OutputPolicy == PoseSlotOutputPolicy.RequireOutput)
                    hasRequired = true;
            }
            if (!hasRequired)
                Report(report, CharacterPoseGraphValidationCode.RequireOutputPathMissing, $"Pose Graph '{graph.GraphId}' OutputPose has no reachable RequireOutput Pose Slot.", graph.GraphId, output);
        }

        static void DetectCycles(
            string graphId,
            IEnumerable<PoseNodeId> nodes,
            Dictionary<PoseNodeId, List<PoseNodeId>> adjacency,
            CharacterPoseGraphValidationReport report)
        {
            var colors = new Dictionary<PoseNodeId, byte>();
            var path = new List<PoseNodeId>();
            foreach (PoseNodeId node in nodes)
            {
                if (!colors.ContainsKey(node))
                    Visit(node);
            }

            void Visit(PoseNodeId node)
            {
                colors[node] = 1;
                path.Add(node);
                if (adjacency.TryGetValue(node, out List<PoseNodeId> next))
                {
                    for (int i = 0; i < next.Count; i++)
                    {
                        PoseNodeId target = next[i];
                        if (!colors.TryGetValue(target, out byte color))
                            Visit(target);
                        else if (color == 1)
                        {
                            int start = path.IndexOf(target);
                            var values = new List<string>();
                            for (int p = Math.Max(0, start); p < path.Count; p++)
                                values.Add(path[p].Value);
                            values.Add(target.Value);
                            Report(report, CharacterPoseGraphValidationCode.Cycle, $"Pose Graph '{graphId}' contains cycle: {string.Join(" -> ", values)}.", graphId, target);
                        }
                    }
                }
                path.RemoveAt(path.Count - 1);
                colors[node] = 2;
            }
        }

        static void Add(Dictionary<PoseNodeId, List<PoseNodeId>> map, PoseNodeId key, PoseNodeId value)
        {
            if (!map.TryGetValue(key, out List<PoseNodeId> values))
            {
                values = new List<PoseNodeId>();
                map.Add(key, values);
            }
            values.Add(value);
        }

        static void Report(
            CharacterPoseGraphValidationReport report,
            CharacterPoseGraphValidationCode code,
            string message,
            string graphId = "",
            PoseNodeId nodeId = default,
            PosePortId portId = default)
        {
            report.Add(new CharacterPoseGraphValidationIssue(code, message, graphId, nodeId, portId));
        }
    }
}
