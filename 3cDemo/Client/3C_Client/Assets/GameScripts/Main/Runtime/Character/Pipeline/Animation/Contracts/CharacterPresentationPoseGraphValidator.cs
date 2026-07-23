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
        ChannelCoverageMissing = 3,
        ParameterInvalid = 4,
        ParameterDuplicate = 5,
        NodeInvalid = 6,
        NodeDuplicate = 7,
        PortInvalid = 8,
        PortDuplicate = 9,
        PortShapeInvalid = 10,
        EdgeInvalid = 11,
        EdgeDuplicate = 12,
        PortTypeMismatch = 13,
        IllegalFanIn = 14,
        RequiredInputMissing = 15,
        Cycle = 16,
        OutputCountInvalid = 17,
        SelectionBindingInvalid = 18,
        SelectionUnreachable = 19,
        RequireSelectionPathMissing = 20,
        PlayerPolicyInvalid = 21,
        MaskInvalid = 22,
        AdditiveInvalid = 23,
        ParameterPolicyMissing = 24,
        ModifyBoneInvalid = 25,
        FootPlacementInvalid = 26,
        SubgraphOwnershipInvalid = 27,
        SharedSubgraphCycle = 28,
        InterfaceBoundaryInvalid = 29,
        InterfaceIdentityInvalid = 30,
        InterfaceBindingInvalid = 31,
        InterfaceDangling = 32
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
                Report(report, CharacterPoseGraphValidationCode.MaskInvalid, "Pose Graph validation requires one Animation Rig Definition.", asset.Graph.GraphId);
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
            ValidateGraph(
                asset.Graph,
                rig,
                reachableChannels,
                true,
                new HashSet<CharacterPresentationPoseGraphAsset> { asset },
                report);
            return report;
        }

        static void ValidateGraph(
            CharacterPoseGraphData graph,
            CharacterAnimationRigDefinition rig,
            IReadOnlyCollection<AnimationChannelId> reachableChannels,
            bool root,
            HashSet<CharacterPresentationPoseGraphAsset> sharedPath,
            CharacterPoseGraphValidationReport report)
        {
            if (graph == null || string.IsNullOrWhiteSpace(graph.GraphId) || string.IsNullOrWhiteSpace(graph.ContentRevision))
            {
                Report(report, CharacterPoseGraphValidationCode.GraphIdentityInvalid, "Pose Graph identity or content revision is missing.", graph?.GraphId);
                return;
            }

            var parameters = ValidateParameters(graph, report);
            var nodes = new Dictionary<PoseNodeId, CharacterPoseNodeDefinition>();
            var ports = new Dictionary<string, CharacterPosePortDefinition>(StringComparer.Ordinal);
            var channelInputs = new HashSet<AnimationChannelId>();
            int outputCount = 0;
            int graphInputCount = 0;
            int graphOutputCount = 0;

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
                ValidateNode(graph, node, parameters, rig, sharedPath, report);
                if (node.Kind == CharacterPoseNodeKind.AnimationSelectionInput)
                    channelInputs.Add(node.AnimationChannelId);
                if (node.Kind == CharacterPoseNodeKind.OutputPose)
                    outputCount++;
                if (node.Kind == CharacterPoseNodeKind.GraphInput)
                    graphInputCount++;
                if (node.Kind == CharacterPoseNodeKind.GraphOutput)
                    graphOutputCount++;

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
                    bool boundary = node.Kind == CharacterPoseNodeKind.GraphInput || node.Kind == CharacterPoseNodeKind.GraphOutput || node.Kind == CharacterPoseNodeKind.PoseSubgraph;
                    if (boundary != port.InterfacePortId.IsValid)
                        Report(report, CharacterPoseGraphValidationCode.InterfaceIdentityInvalid, $"Pose Node '{node.NodeId}' Port '{port.PortId}' has an invalid interface identity.", graph.GraphId, node.NodeId, port.PortId);
                    ports[node.NodeId.Value + "\0" + port.PortId.Value] = port;
                }
            }

            if (root)
            {
                if (outputCount != 1 || graphInputCount != 0 || graphOutputCount != 0)
                    Report(report, CharacterPoseGraphValidationCode.OutputCountInvalid, $"Root Pose Graph '{graph.GraphId}' requires one OutputPose and no subgraph boundary nodes.", graph.GraphId);
                if (reachableChannels != null)
                {
                    foreach (AnimationChannelId channel in reachableChannels)
                    {
                        if (!channel.IsValid || !channelInputs.Contains(channel))
                            Report(report, CharacterPoseGraphValidationCode.ChannelCoverageMissing, $"Pose Graph '{graph.GraphId}' does not bind reachable Animation Channel '{channel}'.", graph.GraphId);
                    }
                    foreach (AnimationChannelId channel in channelInputs)
                    {
                        if (!reachableChannels.Contains(channel))
                            Report(report, CharacterPoseGraphValidationCode.ChannelCoverageMissing, $"Pose Graph '{graph.GraphId}' binds unknown Animation Channel '{channel}'.", graph.GraphId);
                    }
                }
            }
            else if (outputCount != 0 || graphInputCount != 1 || graphOutputCount != 1)
            {
                Report(report, CharacterPoseGraphValidationCode.InterfaceBoundaryInvalid, $"Pose Subgraph '{graph.GraphId}' requires one GraphInput, one GraphOutput and no OutputPose.", graph.GraphId);
            }

            ValidateEdges(graph, nodes, ports, root, report);
        }

        static HashSet<PoseParameterId> ValidateParameters(
            CharacterPoseGraphData graph,
            CharacterPoseGraphValidationReport report)
        {
            var result = new HashSet<PoseParameterId>();
            for (int i = 0; i < graph.Parameters.Count; i++)
            {
                CharacterPoseParameterDeclaration parameter = graph.Parameters[i];
                if (parameter == null || !parameter.ParameterId.IsValid ||
                    !Enum.IsDefined(typeof(PoseParameterValueType), parameter.ValueType) || !float.IsFinite(parameter.DefaultValue))
                {
                    Report(report, CharacterPoseGraphValidationCode.ParameterInvalid, $"Pose Graph '{graph.GraphId}' Parameter #{i} is invalid.", graph.GraphId);
                    continue;
                }
                if (!result.Add(parameter.ParameterId))
                    Report(report, CharacterPoseGraphValidationCode.ParameterDuplicate, $"Pose Graph '{graph.GraphId}' duplicates Parameter '{parameter.ParameterId}'.", graph.GraphId);
            }
            return result;
        }

        static void ValidateNode(
            CharacterPoseGraphData graph,
            CharacterPoseNodeDefinition node,
            HashSet<PoseParameterId> parameters,
            CharacterAnimationRigDefinition rig,
            HashSet<CharacterPresentationPoseGraphAsset> sharedPath,
            CharacterPoseGraphValidationReport report)
        {
            int selectionInputs = CountPorts(node, CharacterPosePortKind.AnimationSelection, CharacterPosePortDirection.Input);
            int selectionOutputs = CountPorts(node, CharacterPosePortKind.AnimationSelection, CharacterPosePortDirection.Output);
            int poseInputs = CountPorts(node, CharacterPosePortKind.Pose, CharacterPosePortDirection.Input);
            int poseOutputs = CountPorts(node, CharacterPosePortKind.Pose, CharacterPosePortDirection.Output);
            int parameterInputs = CountPorts(node, CharacterPosePortKind.Parameter, CharacterPosePortDirection.Input);
            int parameterOutputs = CountPorts(node, CharacterPosePortKind.Parameter, CharacterPosePortDirection.Output);
            int discontinuityInputs = CountPorts(node, CharacterPosePortKind.PoseDiscontinuity, CharacterPosePortDirection.Input);
            int discontinuityOutputs = CountPorts(node, CharacterPosePortKind.PoseDiscontinuity, CharacterPosePortDirection.Output);

            switch (node.Kind)
            {
                case CharacterPoseNodeKind.AnimationSelectionInput:
                    RequireShape(0, 1, 0, 0, 0, 0);
                    if (!node.AnimationChannelId.IsValid || !Enum.IsDefined(typeof(AnimationSelectionAvailabilityPolicy), node.SelectionAvailability))
                        Invalid(CharacterPoseGraphValidationCode.SelectionBindingInvalid, "AnimationSelectionInput requires a valid Animation Channel and availability policy.");
                    break;
                case CharacterPoseNodeKind.MotionMatchingSelectionInput:
                    RequireShape(0, 1, 0, 0, 0, 0);
                    if (!node.AnimationChannelId.IsValid || string.IsNullOrWhiteSpace(node.ProgramProducerId) ||
                        !Enum.IsDefined(typeof(AnimationSelectionAvailabilityPolicy), node.SelectionAvailability))
                        Invalid(CharacterPoseGraphValidationCode.SelectionBindingInvalid, "MotionMatchingSelectionInput requires a channel, producer output and availability policy.");
                    break;
                case CharacterPoseNodeKind.ProgramParameterInput:
                    RequireShape(0, 0, 0, 0, 0, 1);
                    if (!node.ParameterId.IsValid || !parameters.Contains(node.ParameterId))
                        Invalid(CharacterPoseGraphValidationCode.ParameterInvalid, "ProgramParameterInput references an unknown Parameter.");
                    break;
                case CharacterPoseNodeKind.MarkerSync:
                    RequireShape(1, 1, 0, 0, 0, 0);
                    break;
                case CharacterPoseNodeKind.SelectedPosePlayer:
                    RequireShape(1, 0, 0, 1, 0, 0);
                    break;
                case CharacterPoseNodeKind.BlendSpacePlayer:
                    RequireShape(1, 0, 0, 1, 2, 0);
                    if (discontinuityInputs != 0 || discontinuityOutputs != 1 ||
                        !Enum.IsDefined(typeof(AnimationSelectionAvailabilityPolicy), node.SelectionAvailability) ||
                        !Enum.IsDefined(typeof(CharacterAnimationBlendSpaceInputRangePolicy), node.BlendSpaceInputRangePolicy))
                        Invalid(CharacterPoseGraphValidationCode.PortShapeInvalid, "BlendSpacePlayer requires X/Y typed inputs, Pose output and one Discontinuity output.");
                    break;
                case CharacterPoseNodeKind.BlendStack:
                    RequireShape(1, 0, 0, 1, 0, 0);
                    if (!node.BlendPolicy)
                        Invalid(CharacterPoseGraphValidationCode.PlayerPolicyInvalid, "BlendStack requires one explicit Blend Policy.");
                    else TryValidate(() => node.BlendPolicy.RequireValid(rig), CharacterPoseGraphValidationCode.PlayerPolicyInvalid);
                    break;
                case CharacterPoseNodeKind.Inertialization:
                    RequireShape(0, 0, 1, 1, 0, 0);
                    if (!node.InertializationPolicy)
                        Invalid(CharacterPoseGraphValidationCode.PlayerPolicyInvalid, "Inertialization requires one explicit local policy.");
                    else TryValidate(() => node.InertializationPolicy.RequireValid(rig), CharacterPoseGraphValidationCode.PlayerPolicyInvalid);
                    break;
                case CharacterPoseNodeKind.BlendPose:
                    RequireShape(0, 0, 2, 1, 1, 0);
                    break;
                case CharacterPoseNodeKind.LayeredBoneBlend:
                case CharacterPoseNodeKind.AdditivePose:
                    RequireShape(0, 0, 2, 1, 1, 0);
                    if (!node.BoneMask)
                        Invalid(CharacterPoseGraphValidationCode.MaskInvalid, $"{node.Kind} requires one Bone Mask.");
                    else TryValidate(() => node.BoneMask.BuildDense(rig), CharacterPoseGraphValidationCode.MaskInvalid);
                    if (node.Kind == CharacterPoseNodeKind.AdditivePose &&
                        (!string.Equals(node.AdditiveReferencePoseId, AnimationAdditiveReferencePoseIds.RigReference, StringComparison.Ordinal) ||
                         !Enum.IsDefined(typeof(AdditiveReferenceSpace), node.AdditiveReferenceSpace) ||
                         !Enum.IsDefined(typeof(AdditiveScalePolicy), node.AdditiveScalePolicy)))
                        Invalid(CharacterPoseGraphValidationCode.AdditiveInvalid, "AdditivePose reference configuration is invalid.");
                    break;
                case CharacterPoseNodeKind.PoseParameterResolve:
                    RequireShape(0, 0, 2, 1, 0, 0);
                    ValidateParameterPolicies(graph, node, parameters, report);
                    break;
                case CharacterPoseNodeKind.ModifyBone:
                    RequireShape(0, 0, 1, 1, 1, 0);
                    if (!node.BoneId.IsValid ||
                        !Enum.IsDefined(typeof(ModifyBoneReferenceSpace), node.ModifyBoneReferenceSpace) ||
                        node.ModifyBoneOperations == ModifyBoneOperationMask.None ||
                        !IsFinite(node.ModifyPosition) || !IsFinite(node.ModifyRotation) || !IsFinite(node.ModifyScale))
                        Invalid(CharacterPoseGraphValidationCode.ModifyBoneInvalid, "ModifyBone configuration is invalid for the compiled Rig.");
                    else
                        TryValidate(() => rig.RequireBoneIndex(node.BoneId), CharacterPoseGraphValidationCode.ModifyBoneInvalid);
                    break;
                case CharacterPoseNodeKind.FootPlacement:
                    RequireShape(0, 0, 1, 1, 1, 0);
                    if (!node.FootPlacementProfile || !node.FootPlacementCalibration)
                        Invalid(CharacterPoseGraphValidationCode.FootPlacementInvalid, "FootPlacement requires one Profile and Rig Calibration.");
                    break;
                case CharacterPoseNodeKind.OutputPose:
                    RequireShape(0, 0, 1, 0, 0, 0);
                    break;
                case CharacterPoseNodeKind.PoseSubgraph:
                    if (node.Subgraph == null || !node.Subgraph.IsExclusive)
                        Invalid(CharacterPoseGraphValidationCode.SubgraphOwnershipInvalid, "PoseSubgraph requires exactly one inline or shared graph.");
                    else if (node.Subgraph.HasShared)
                    {
                        if (!sharedPath.Add(node.Subgraph.Shared))
                            Invalid(CharacterPoseGraphValidationCode.SharedSubgraphCycle, "PoseSubgraph shared dependency contains a cycle.");
                        else
                        {
                            ValidateGraph(node.Subgraph.Shared.Graph, rig, null, false, sharedPath, report);
                            sharedPath.Remove(node.Subgraph.Shared);
                        }
                    }
                    else
                        ValidateGraph(node.Subgraph.Inline, rig, null, false, sharedPath, report);
                    break;
                case CharacterPoseNodeKind.GraphInput:
                case CharacterPoseNodeKind.GraphOutput:
                    break;
                default:
                    Invalid(CharacterPoseGraphValidationCode.NodeInvalid, "Unsupported Pose node kind.");
                    break;
            }
            if (node.Kind != CharacterPoseNodeKind.BlendSpacePlayer &&
                (discontinuityInputs != 0 || discontinuityOutputs != 0))
                Invalid(CharacterPoseGraphValidationCode.PortShapeInvalid, "This node kind cannot own Pose Discontinuity ports.");

            void RequireShape(int expectedSelectionInputs, int expectedSelectionOutputs, int expectedPoseInputs, int expectedPoseOutputs, int expectedParameterInputs, int expectedParameterOutputs)
            {
                if (selectionInputs != expectedSelectionInputs || selectionOutputs != expectedSelectionOutputs ||
                    poseInputs != expectedPoseInputs || poseOutputs != expectedPoseOutputs ||
                    parameterInputs != expectedParameterInputs || parameterOutputs != expectedParameterOutputs)
                    Invalid(CharacterPoseGraphValidationCode.PortShapeInvalid, "Typed port shape does not match the node contract.");
            }
            void Invalid(CharacterPoseGraphValidationCode code, string detail) =>
                Report(report, code, $"Pose Node '{node.NodeId}': {detail}", graph.GraphId, node.NodeId);
            void TryValidate(Action action, CharacterPoseGraphValidationCode code)
            {
                try { action(); }
                catch (Exception exception) { Invalid(code, exception.Message); }
            }
        }

        static void ValidateEdges(
            CharacterPoseGraphData graph,
            Dictionary<PoseNodeId, CharacterPoseNodeDefinition> nodes,
            Dictionary<string, CharacterPosePortDefinition> ports,
            bool root,
            CharacterPoseGraphValidationReport report)
        {
            var adjacency = new Dictionary<PoseNodeId, List<PoseNodeId>>();
            var reverse = new Dictionary<PoseNodeId, List<PoseNodeId>>();
            var incoming = new HashSet<string>(StringComparer.Ordinal);
            var edgeIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                CharacterPoseEdge edge = graph.Edges[i];
                if (edge == null || string.IsNullOrWhiteSpace(edge.EdgeId) || !edgeIds.Add(edge.EdgeId))
                {
                    Report(report, CharacterPoseGraphValidationCode.EdgeDuplicate, $"Pose Graph '{graph.GraphId}' Edge #{i} is invalid or duplicated.", graph.GraphId);
                    continue;
                }
                string sourceKey = edge.SourceNodeId.Value + "\0" + edge.SourcePortId.Value;
                string targetKey = edge.TargetNodeId.Value + "\0" + edge.TargetPortId.Value;
                if (!nodes.ContainsKey(edge.SourceNodeId) || !nodes.ContainsKey(edge.TargetNodeId) ||
                    !ports.TryGetValue(sourceKey, out CharacterPosePortDefinition source) ||
                    !ports.TryGetValue(targetKey, out CharacterPosePortDefinition target) ||
                    source.Direction != CharacterPosePortDirection.Output || target.Direction != CharacterPosePortDirection.Input)
                {
                    Report(report, CharacterPoseGraphValidationCode.EdgeInvalid, $"Pose Edge '{edge.EdgeId}' references an invalid endpoint.", graph.GraphId);
                    continue;
                }
                if (source.Kind != target.Kind)
                    Report(report, CharacterPoseGraphValidationCode.PortTypeMismatch, $"Pose Edge '{edge.EdgeId}' connects '{source.Kind}' to '{target.Kind}'.", graph.GraphId);
                if (!incoming.Add(targetKey))
                    Report(report, CharacterPoseGraphValidationCode.IllegalFanIn, $"Pose Port '{edge.TargetNodeId}/{edge.TargetPortId}' has multiple incoming edges.", graph.GraphId, edge.TargetNodeId, edge.TargetPortId);
                Add(adjacency, edge.SourceNodeId, edge.TargetNodeId);
                Add(reverse, edge.TargetNodeId, edge.SourceNodeId);
            }
            foreach (CharacterPoseNodeDefinition node in nodes.Values)
            {
                for (int i = 0; i < node.Ports.Count; i++)
                {
                    CharacterPosePortDefinition port = node.Ports[i];
                    if (port != null && port.Required && port.Direction == CharacterPosePortDirection.Input &&
                        !incoming.Contains(node.NodeId.Value + "\0" + port.PortId.Value))
                        Report(report, CharacterPoseGraphValidationCode.RequiredInputMissing, $"Pose Node '{node.NodeId}' required Port '{port.PortId}' is disconnected.", graph.GraphId, node.NodeId, port.PortId);
                }
            }
            foreach (CharacterPoseNodeDefinition node in nodes.Values)
            {
                if (node.Kind != CharacterPoseNodeKind.MarkerSync)
                    continue;
                CharacterPoseEdge[] markerInputs = graph.Edges
                    .Where(edge => edge != null && edge.TargetNodeId == node.NodeId)
                    .ToArray();
                CharacterPoseEdge[] markerOutputs = graph.Edges
                    .Where(edge => edge != null && edge.SourceNodeId == node.NodeId)
                    .ToArray();
                if (markerInputs.Length != 1 || markerOutputs.Length != 1 ||
                    !nodes.TryGetValue(markerInputs[0].SourceNodeId, out CharacterPoseNodeDefinition markerSource) ||
                    !nodes.TryGetValue(markerOutputs[0].TargetNodeId, out CharacterPoseNodeDefinition markerTarget) ||
                    markerSource.Kind == CharacterPoseNodeKind.MarkerSync ||
                    markerTarget.Kind != CharacterPoseNodeKind.SelectedPosePlayer &&
                    markerTarget.Kind != CharacterPoseNodeKind.BlendStack &&
                    markerTarget.Kind != CharacterPoseNodeKind.BlendSpacePlayer)
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode.SelectionBindingInvalid,
                        $"MarkerSync '{node.NodeId}' must have one non-Marker Selection source and exactly one stateful Player consumer.",
                        graph.GraphId,
                        node.NodeId);
                }
            }
            DetectCycles(graph.GraphId, nodes.Keys, adjacency, report);
            if (root)
                ValidateReachability(graph, nodes, reverse, report);
        }

        static void ValidateReachability(
            CharacterPoseGraphData graph,
            Dictionary<PoseNodeId, CharacterPoseNodeDefinition> nodes,
            Dictionary<PoseNodeId, List<PoseNodeId>> reverse,
            CharacterPoseGraphValidationReport report)
        {
            CharacterPoseNodeDefinition output = nodes.Values.SingleOrDefault(node => node.Kind == CharacterPoseNodeKind.OutputPose);
            if (output == null)
                return;
            var reachable = new HashSet<PoseNodeId>();
            var stack = new Stack<PoseNodeId>();
            stack.Push(output.NodeId);
            while (stack.Count > 0)
            {
                PoseNodeId current = stack.Pop();
                if (!reachable.Add(current) || !reverse.TryGetValue(current, out List<PoseNodeId> upstream))
                    continue;
                for (int i = 0; i < upstream.Count; i++)
                    stack.Push(upstream[i]);
            }
            bool requiredSelection = false;
            foreach (CharacterPoseNodeDefinition node in nodes.Values)
            {
                bool selection = node.Kind == CharacterPoseNodeKind.AnimationSelectionInput || node.Kind == CharacterPoseNodeKind.MotionMatchingSelectionInput;
                if (!selection)
                    continue;
                if (!reachable.Contains(node.NodeId))
                    Report(report, CharacterPoseGraphValidationCode.SelectionUnreachable, $"Selection Input '{node.NodeId}' is not consumed by OutputPose.", graph.GraphId, node.NodeId);
                if (reachable.Contains(node.NodeId) && node.SelectionAvailability == AnimationSelectionAvailabilityPolicy.RequireSelection)
                    requiredSelection = true;
            }
            if (!requiredSelection)
                Report(report, CharacterPoseGraphValidationCode.RequireSelectionPathMissing, $"Pose Graph '{graph.GraphId}' has no reachable RequireSelection input.", graph.GraphId, output.NodeId);
        }

        static void ValidateParameterPolicies(CharacterPoseGraphData graph, CharacterPoseNodeDefinition node, HashSet<PoseParameterId> parameters, CharacterPoseGraphValidationReport report)
        {
            var covered = new HashSet<PoseParameterId>();
            for (int i = 0; i < node.ParameterPolicies.Count; i++)
            {
                CharacterPoseParameterPolicy policy = node.ParameterPolicies[i];
                if (policy == null || !parameters.Contains(policy.ParameterId) || !covered.Add(policy.ParameterId) ||
                    !Enum.IsDefined(typeof(PoseParameterResolvePolicy), policy.Policy))
                    Report(report, CharacterPoseGraphValidationCode.ParameterPolicyMissing, $"Pose Node '{node.NodeId}' Parameter policy #{i} is invalid.", graph.GraphId, node.NodeId);
            }
            foreach (PoseParameterId parameter in parameters)
            {
                if (!covered.Contains(parameter))
                    Report(report, CharacterPoseGraphValidationCode.ParameterPolicyMissing, $"Pose Node '{node.NodeId}' has no policy for Parameter '{parameter}'.", graph.GraphId, node.NodeId);
            }
        }

        static int CountPorts(CharacterPoseNodeDefinition node, CharacterPosePortKind kind, CharacterPosePortDirection direction) =>
            node.Ports.Count(port => port != null && port.Kind == kind && port.Direction == direction);

        static void DetectCycles(string graphId, IEnumerable<PoseNodeId> nodes, Dictionary<PoseNodeId, List<PoseNodeId>> adjacency, CharacterPoseGraphValidationReport report)
        {
            var colors = new Dictionary<PoseNodeId, byte>();
            foreach (PoseNodeId node in nodes)
                Visit(node);
            void Visit(PoseNodeId node)
            {
                if (colors.TryGetValue(node, out byte color))
                {
                    if (color == 1)
                        Report(report, CharacterPoseGraphValidationCode.Cycle, $"Pose Graph '{graphId}' contains a cycle at '{node}'.", graphId, node);
                    return;
                }
                colors[node] = 1;
                if (adjacency.TryGetValue(node, out List<PoseNodeId> next))
                {
                    for (int i = 0; i < next.Count; i++)
                        Visit(next[i]);
                }
                colors[node] = 2;
            }
        }

        static void Add(Dictionary<PoseNodeId, List<PoseNodeId>> values, PoseNodeId key, PoseNodeId value)
        {
            if (!values.TryGetValue(key, out List<PoseNodeId> entries))
            {
                entries = new List<PoseNodeId>();
                values.Add(key, entries);
            }
            entries.Add(value);
        }

        static bool IsFinite(UnityEngine.Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        static bool IsFinite(UnityEngine.Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z) && float.IsFinite(value.w);

        static void Report(
            CharacterPoseGraphValidationReport report,
            CharacterPoseGraphValidationCode code,
            string message,
            string graphId = "",
            PoseNodeId nodeId = default,
            PosePortId portId = default) =>
            report.Add(new CharacterPoseGraphValidationIssue(code, message, graphId, nodeId, portId));
    }
}
