using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonSimulation;
using TreeDesigner.Editor;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public delegate IReadOnlyList<CharacterPosePortDefinition>
        CharacterPosePortContractResolver(
            CharacterTypedPoseNode node);

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
        PredictiveFootPlacementInvalid = 26,
        SubgraphOwnershipInvalid = 27,
        SharedSubgraphCycle = 28,
        InterfaceBoundaryInvalid = 29,
        InterfaceIdentityInvalid = 30,
        InterfaceBindingInvalid = 31,
        InterfaceDangling = 32,
        PoseBoneIkGoalsInvalid = 33,
        StateMachineInvalid = 34,
        AnimationSlotInvalid = 35,
        StateMachineLayoutInvalid = 36,
        FullBodyIkInvalid = 37,
        MotionMatchingInvalid = 38
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
        readonly List<CharacterPoseGraphValidationIssue> m_Issues =
            new List<CharacterPoseGraphValidationIssue>();

        public IReadOnlyList<CharacterPoseGraphValidationIssue> Issues =>
            m_Issues;
        public bool IsValid => m_Issues.Count == 0;

        internal void Add(CharacterPoseGraphValidationIssue issue) =>
            m_Issues.Add(issue);

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
        enum GraphRole : byte
        {
            Root = 1,
            StatePose = 2,
            Subgraph = 3
        }

        public static CharacterPoseGraphValidationReport Validate(
            CharacterPresentationPoseGraphAsset asset,
            CharacterAnimationRigDefinition rig,
            CharacterPosePortContractResolver portResolver,
            IReadOnlyCollection<AnimationChannelId>
                reachableChannels = null,
            IReadOnlyCollection<CharacterPresentationPoseSourceSlot>
                reachableSources = null)
        {
            if (portResolver == null)
                throw new ArgumentNullException(nameof(portResolver));
            var report = new CharacterPoseGraphValidationReport();
            if (!asset || asset.Graph == null)
            {
                Report(
                    report,
                    CharacterPoseGraphValidationCode.GraphMissing,
                    "Character Presentation Pose Graph is missing.");
                return report;
            }
            if (!rig)
            {
                Report(
                    report,
                    CharacterPoseGraphValidationCode.MaskInvalid,
                    "Pose Graph validation requires one Animation Rig Definition.",
                    asset.Graph.GraphId);
                return report;
            }
            try
            {
                rig.RequireValid();
            }
            catch (Exception exception)
            {
                Report(
                    report,
                    CharacterPoseGraphValidationCode.MaskInvalid,
                    exception.Message,
                    asset.Graph.GraphId);
                return report;
            }
            ValidateStateMachineLayouts(asset, report);

            foreach (string error in
                     CharacterPoseGraphCapabilityValidator.Validate(asset))
            {
                Report(
                    report,
                    CharacterPoseGraphValidationCode.NodeInvalid,
                    error,
                    asset.Graph.GraphId);
            }

            var catalogIds = new HashSet<PoseGraphId>();
            var ownerCounts = new Dictionary<PoseGraphId, int>();
            foreach (CharacterTypedPoseGraph graph in
                     asset.EnumerateGraphs())
            {
                if (graph == null ||
                    !graph.GraphId.IsValid ||
                    !catalogIds.Add(graph.GraphId))
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode
                            .GraphIdentityInvalid,
                        "Pose Graph catalog contains a missing or duplicate graph identity.",
                        graph?.GraphId.Value ?? string.Empty);
                    continue;
                }
                foreach (CharacterTypedPoseNode node in graph.Nodes)
                {
                    if (node?.Payload == null)
                        continue;
                    ICharacterPoseCompilerHandler handler =
                        CharacterPoseCompilerHandlerRegistry.Shared
                            .Require(node.Kind);
                    if (handler.NativeRole ==
                        CharacterPoseNativeNodeRole.Subgraph)
                    {
                        CharacterPoseSubgraphReference reference =
                            ((CharacterPoseSubgraphPayload)
                                node.Payload).Subgraph;
                        if (reference?.PoseGraphId.IsValid == true)
                            AddOwner(reference.PoseGraphId);
                    }
                    if (node.Payload is CharacterMotionMatchingPosePayload motionMatching &&
                        motionMatching.EntryGraph?.PoseGraphId.IsValid == true)
                    {
                        AddOwner(motionMatching.EntryGraph.PoseGraphId);
                    }
                    if (!handler.StateMachine)
                        continue;
                    CharacterPoseStateMachineDefinition machine =
                        ((CharacterPoseStateMachineNodePayload)
                            node.Payload).StateMachine;
                    if (machine == null)
                        continue;
                    foreach (CharacterPoseStateDefinition state in
                             machine.States)
                    {
                        if (state?.PoseGraphId.IsValid == true)
                            AddOwner(state.PoseGraphId);
                    }
                }
            }
            foreach (PoseGraphId graphId in catalogIds)
            {
                int expected = asset.Graph.GraphId == graphId ? 0 : 1;
                ownerCounts.TryGetValue(graphId, out int actual);
                if (actual != expected)
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode
                            .SubgraphOwnershipInvalid,
                        $"Pose Graph catalog record '{graphId}' has {actual} owner references; expected {expected}.",
                        graphId.Value);
                }
            }

            HashSet<CharacterPresentationPoseSourceSlot> sourceSlots = null;
            if (reachableSources != null)
            {
                sourceSlots =
                    new HashSet<CharacterPresentationPoseSourceSlot>();
                foreach (CharacterPresentationPoseSourceSlot sourceSlot in
                         reachableSources)
                {
                    if (!sourceSlot ||
                        !sourceSlots.Add(sourceSlot))
                    {
                        Report(
                            report,
                            CharacterPoseGraphValidationCode
                                .SelectionBindingInvalid,
                            "Presentation Pose source catalog contains a missing or duplicate source identity.",
                            asset.Graph.GraphId);
                    }
                }
            }

            var reachableGraphs = new HashSet<PoseGraphId>();
            ValidateGraph(
                asset,
                asset.Graph,
                rig,
                portResolver,
                reachableChannels,
                sourceSlots,
                GraphRole.Root,
                new List<PoseGraphId>(),
                reachableGraphs,
                report);
            foreach (PoseGraphId graphId in catalogIds)
            {
                if (!reachableGraphs.Contains(graphId))
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode
                            .SubgraphOwnershipInvalid,
                        $"Pose Graph catalog record '{graphId}' is unreachable.",
                        graphId.Value);
                }
            }
            return report;

            void AddOwner(PoseGraphId graphId)
            {
                ownerCounts.TryGetValue(graphId, out int count);
                ownerCounts[graphId] = count + 1;
            }
        }

        static void ValidateGraph(
            CharacterPresentationPoseGraphAsset ownerAsset,
            CharacterTypedPoseGraph graph,
            CharacterAnimationRigDefinition rig,
            CharacterPosePortContractResolver portResolver,
            IReadOnlyCollection<AnimationChannelId>
                reachableChannels,
            IReadOnlyCollection<CharacterPresentationPoseSourceSlot>
                reachableSources,
            GraphRole role,
            List<PoseGraphId> callPath,
            HashSet<PoseGraphId> reachableGraphs,
            CharacterPoseGraphValidationReport report)
        {
            if (graph == null ||
                !graph.GraphId.IsValid ||
                string.IsNullOrWhiteSpace(graph.ContentRevision))
            {
                Report(
                    report,
                    CharacterPoseGraphValidationCode
                        .GraphIdentityInvalid,
                    "Pose Graph identity or content revision is missing.",
                    graph?.GraphId.Value ?? string.Empty);
                return;
            }
            int recursiveIndex = callPath.IndexOf(graph.GraphId);
            if (recursiveIndex >= 0)
            {
                string path = string.Join(
                    " -> ",
                    callPath.Skip(recursiveIndex)
                        .Select(value => value.Value)
                        .Concat(new[] { graph.GraphId.Value }));
                Report(
                    report,
                    CharacterPoseGraphValidationCode
                        .SharedSubgraphCycle,
                    $"Pose Graph catalog contains a recursive call: {path}.",
                    graph.GraphId);
                return;
            }

            callPath.Add(graph.GraphId);
            reachableGraphs.Add(graph.GraphId);
            HashSet<PoseParameterId> parameters =
                ValidateParameters(graph, report);
            var nodes =
                new Dictionary<PoseNodeId, CharacterTypedPoseNode>();
            var ports = new Dictionary<
                string,
                CharacterPosePortDefinition>(StringComparer.Ordinal);
            var channelInputs = new HashSet<AnimationChannelId>();
            var slotIds = new HashSet<AnimationSlotId>();
            int outputCount = 0;
            int graphInputCount = 0;
            int graphOutputCount = 0;
            GraphAuthoringDocumentRoleId documentRole =
                Role(role);

            for (int nodeIndex = 0;
                 nodeIndex < graph.Nodes.Count;
                 nodeIndex++)
            {
                CharacterTypedPoseNode node =
                    graph.Nodes[nodeIndex];
                if (node?.Payload == null ||
                    !node.NodeId.IsValid)
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode.NodeInvalid,
                        $"Pose Graph '{graph.GraphId}' Node #{nodeIndex} is invalid.",
                        graph.GraphId);
                    continue;
                }
                if (!nodes.TryAdd(node.NodeId, node))
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode
                            .NodeDuplicate,
                        $"Pose Graph '{graph.GraphId}' duplicates Node '{node.NodeId}'.",
                        graph.GraphId,
                        node.NodeId);
                    continue;
                }

                ICharacterPoseCompilerHandler handler;
                try
                {
                    CharacterPoseGraphAuthoringCapabilities.Catalog
                        .Require(
                            CharacterPoseGraphAuthoringCapabilities
                                .Get(node.Kind),
                            CharacterPoseGraphAuthoringCapabilities
                                .Domain,
                            documentRole);
                    handler =
                        CharacterPoseCompilerHandlerRegistry.Shared
                            .Require(node.Kind);
                    string sourcePath =
                        $"pose-graphs/{graph.GraphId.Value}/nodes/{node.NodeId.Value}";
                    handler.ValidatePayload(
                        node.Payload,
                        sourcePath);
                    handler.ValidateRig(
                        node.Payload,
                        rig,
                        sourcePath);
                }
                catch (Exception exception)
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode.NodeInvalid,
                        $"Pose Node '{node.NodeId}': {exception.Message}",
                        graph.GraphId,
                        node.NodeId);
                    continue;
                }

                PoseParameterId parameter =
                    handler.Parameter(node.Payload);
                if (parameter.IsValid &&
                    !parameters.Contains(parameter))
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode
                            .ParameterInvalid,
                        $"Pose Node '{node.NodeId}' references unknown Parameter '{parameter}'.",
                        graph.GraphId,
                        node.NodeId);
                }
                if (handler.Code ==
                    CharacterPoseOperationCode
                        .PoseParameterResolve)
                {
                    ValidateParameterPolicies(
                        graph,
                        node,
                        handler.ParameterPolicies(node.Payload),
                        parameters,
                        report);
                }

                CharacterPresentationPoseSourceSlot sourceSlot =
                    handler.Source(node.Payload);
                if (reachableSources != null &&
                    sourceSlot &&
                    !reachableSources.Contains(sourceSlot))
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode
                            .SelectionBindingInvalid,
                        $"Pose Source Slot '{sourceSlot.name}' is not present in the compiled Presentation source catalog.",
                        graph.GraphId,
                        node.NodeId);
                }

                AnimationChannelId channel =
                    handler.Channel(node.Payload);
                if (channel.IsValid)
                    channelInputs.Add(channel);
                if (handler.AnimationSlot)
                {
                    AnimationSlotId slotId =
                        ((CharacterAnimationSlotPosePayload)
                            node.Payload).SlotId;
                    if (!slotIds.Add(slotId))
                    {
                        Report(
                            report,
                            CharacterPoseGraphValidationCode
                                .AnimationSlotInvalid,
                            $"Pose Graph '{graph.GraphId}' duplicates Animation Slot '{slotId}'.",
                            graph.GraphId,
                            node.NodeId);
                    }
                }

                if (handler.NativeRole ==
                    CharacterPoseNativeNodeRole.PoseOutput)
                    outputCount++;
                else if (handler.NativeRole ==
                         CharacterPoseNativeNodeRole.GraphInput)
                    graphInputCount++;
                else if (handler.NativeRole ==
                         CharacterPoseNativeNodeRole.GraphOutput)
                    graphOutputCount++;

                if (handler.StateMachine)
                {
                    ValidateStateMachine(
                        ownerAsset,
                        graph,
                        node,
                        (CharacterPoseStateMachineNodePayload)
                            node.Payload,
                        rig,
                        portResolver,
                        reachableSources,
                        callPath,
                        reachableGraphs,
                        report);
                }
                else if (handler.NativeRole ==
                         CharacterPoseNativeNodeRole.Subgraph)
                {
                    ValidateSubgraph(
                        ownerAsset,
                        graph,
                        node,
                        (CharacterPoseSubgraphPayload)
                            node.Payload,
                        rig,
                        portResolver,
                        reachableSources,
                        callPath,
                        reachableGraphs,
                        report);
                }
                else if (node.Payload is CharacterMotionMatchingPosePayload motionMatching)
                {
                    ValidateMotionMatchingEntryGraph(
                        ownerAsset,
                        graph,
                        node,
                        motionMatching,
                        rig,
                        portResolver,
                        callPath,
                        reachableGraphs,
                        report);
                }

                IReadOnlyList<CharacterPosePortDefinition>
                    nodePorts;
                try
                {
                    nodePorts = portResolver(node) ??
                                throw new InvalidOperationException(
                                    "Pose port resolver returned null.");
                }
                catch (Exception exception)
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode.PortInvalid,
                        $"Pose Node '{node.NodeId}': {exception.Message}",
                        graph.GraphId,
                        node.NodeId);
                    continue;
                }
                var localPorts = new HashSet<PosePortId>();
                bool boundary =
                    handler.NativeRole ==
                    CharacterPoseNativeNodeRole.GraphInput ||
                    handler.NativeRole ==
                    CharacterPoseNativeNodeRole.GraphOutput ||
                    handler.NativeRole ==
                    CharacterPoseNativeNodeRole.Subgraph;
                for (int portIndex = 0;
                     portIndex < nodePorts.Count;
                     portIndex++)
                {
                    CharacterPosePortDefinition port =
                        nodePorts[portIndex];
                    if (port == null ||
                        !port.PortId.IsValid ||
                        !Enum.IsDefined(
                            typeof(CharacterPosePortKind),
                            port.Kind) ||
                        !Enum.IsDefined(
                            typeof(CharacterPosePortDirection),
                            port.Direction))
                    {
                        Report(
                            report,
                            CharacterPoseGraphValidationCode
                                .PortInvalid,
                            $"Pose Node '{node.NodeId}' Port #{portIndex} is invalid.",
                            graph.GraphId,
                            node.NodeId);
                        continue;
                    }
                    if (!localPorts.Add(port.PortId))
                    {
                        Report(
                            report,
                            CharacterPoseGraphValidationCode
                                .PortDuplicate,
                            $"Pose Node '{node.NodeId}' duplicates Port '{port.PortId}'.",
                            graph.GraphId,
                            node.NodeId,
                            port.PortId);
                    }
                    if (boundary != port.InterfacePortId.IsValid)
                    {
                        Report(
                            report,
                            CharacterPoseGraphValidationCode
                                .InterfaceIdentityInvalid,
                            $"Pose Node '{node.NodeId}' Port '{port.PortId}' has an invalid interface identity.",
                            graph.GraphId,
                            node.NodeId,
                            port.PortId);
                    }
                    ports[
                        node.NodeId.Value + "\0" +
                        port.PortId.Value] = port;
                }
            }

            if (role == GraphRole.Root ||
                role == GraphRole.StatePose)
            {
                if (outputCount != 1 ||
                    graphInputCount != 0 ||
                    graphOutputCount != 0)
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode
                            .OutputCountInvalid,
                        $"Pose Graph '{graph.GraphId}' requires one OutputPose and no subgraph boundary nodes.",
                        graph.GraphId);
                }
                if (role == GraphRole.Root &&
                    reachableChannels != null)
                {
                    foreach (AnimationChannelId channel in
                             reachableChannels)
                    {
                        if (!channelInputs.Contains(channel))
                        {
                            Report(
                                report,
                                CharacterPoseGraphValidationCode
                                    .ChannelCoverageMissing,
                                $"Pose Graph '{graph.GraphId}' has no Action Playback input for Animation Channel '{channel}'.",
                                graph.GraphId);
                        }
                    }
                    foreach (AnimationChannelId channel in
                             channelInputs)
                    {
                        if (!reachableChannels.Contains(channel))
                        {
                            Report(
                                report,
                                CharacterPoseGraphValidationCode
                                    .ChannelCoverageMissing,
                                $"Pose Graph '{graph.GraphId}' binds unknown Animation Channel '{channel}'.",
                                graph.GraphId);
                        }
                    }
                }
            }
            else if (outputCount != 0 ||
                     graphInputCount != 1 ||
                     graphOutputCount != 1)
            {
                Report(
                    report,
                    CharacterPoseGraphValidationCode
                        .InterfaceBoundaryInvalid,
                    $"Pose Subgraph '{graph.GraphId}' requires one GraphInput, one GraphOutput and no OutputPose.",
                    graph.GraphId);
            }

            ValidateEdges(
                graph,
                nodes,
                ports,
                portResolver,
                role == GraphRole.Root ||
                role == GraphRole.StatePose,
                role == GraphRole.Root,
                report);
            callPath.RemoveAt(callPath.Count - 1);
        }

        static void ValidateStateMachine(
            CharacterPresentationPoseGraphAsset ownerAsset,
            CharacterTypedPoseGraph ownerGraph,
            CharacterTypedPoseNode node,
            CharacterPoseStateMachineNodePayload payload,
            CharacterAnimationRigDefinition rig,
            CharacterPosePortContractResolver portResolver,
            IReadOnlyCollection<CharacterPresentationPoseSourceSlot>
                reachableSources,
            List<PoseGraphId> callPath,
            HashSet<PoseGraphId> reachableGraphs,
            CharacterPoseGraphValidationReport report)
        {
            try
            {
                CharacterPoseStateMachineAuthoringValidator
                    .RequireValid(
                        payload.StateMachine,
                        ownerAsset.RequireGraph);
            }
            catch (Exception exception)
            {
                Report(
                    report,
                    CharacterPoseGraphValidationCode
                        .StateMachineInvalid,
                    $"Pose Node '{node.NodeId}': {exception.Message}",
                    ownerGraph.GraphId,
                    node.NodeId);
                return;
            }
            foreach (CharacterPoseStateDefinition state in
                     payload.StateMachine.States)
            {
                if (state == null ||
                    !ownerAsset.TryGetGraph(
                        state.PoseGraphId,
                        out CharacterTypedPoseGraph stateGraph))
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode
                            .StateMachineInvalid,
                        $"Pose Node '{node.NodeId}' references a missing state Pose Graph.",
                        ownerGraph.GraphId,
                        node.NodeId);
                    continue;
                }
                ValidateGraph(
                    ownerAsset,
                    stateGraph,
                    rig,
                    portResolver,
                    null,
                    reachableSources,
                    GraphRole.StatePose,
                    callPath,
                    reachableGraphs,
                    report);
            }
        }

        static void ValidateStateMachineLayouts(
            CharacterPresentationPoseGraphAsset asset,
            CharacterPoseGraphValidationReport report)
        {
            Dictionary<PoseStateMachineId, CharacterPoseStateMachineDefinition>
                machines = asset.EnumerateStateMachines()
                    .Where(value => value != null &&
                                    value.StateMachineId.IsValid)
                    .GroupBy(value => value.StateMachineId)
                    .Where(value => value.Count() == 1)
                    .ToDictionary(value => value.Key, value => value.Single());
            var owners = new HashSet<PoseStateMachineId>();
            foreach (CharacterPoseStateMachineLayout layout in
                     asset.StateMachineLayouts)
            {
                if (layout == null ||
                    !layout.StateMachineId.IsValid ||
                    !owners.Add(layout.StateMachineId) ||
                    !machines.TryGetValue(
                        layout.StateMachineId,
                        out CharacterPoseStateMachineDefinition machine))
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode
                            .StateMachineLayoutInvalid,
                        "Pose StateMachine layout owner缺失、重复或引用未知StateMachine。",
                        asset.Graph.GraphId);
                    continue;
                }
                var validElements = new HashSet<string>(
                    machine.States
                        .Where(value => value != null)
                        .Select(value => value.StateId.Value)
                        .Concat(machine.Aliases
                            .Where(value => value != null)
                            .Select(value => value.AliasId.Value))
                        .Append(machine.Entry.EntryId.Value),
                    StringComparer.Ordinal);
                var explicitElements = new HashSet<string>(
                    StringComparer.Ordinal);
                foreach (CharacterPoseStateMachineLayoutElement element in
                         layout.Elements)
                {
                    if (element == null ||
                        !validElements.Contains(element.ElementId) ||
                        !explicitElements.Add(element.ElementId) ||
                        !float.IsFinite(element.Position.x) ||
                        !float.IsFinite(element.Position.y))
                    {
                        Report(
                            report,
                            CharacterPoseGraphValidationCode
                                .StateMachineLayoutInvalid,
                            $"Pose StateMachine '{machine.StateMachineId}' layout元素缺失、重复、悬空或坐标非法。",
                            asset.Graph.GraphId);
                    }
                }
            }
        }

        static void ValidateSubgraph(
            CharacterPresentationPoseGraphAsset ownerAsset,
            CharacterTypedPoseGraph ownerGraph,
            CharacterTypedPoseNode node,
            CharacterPoseSubgraphPayload payload,
            CharacterAnimationRigDefinition rig,
            CharacterPosePortContractResolver portResolver,
            IReadOnlyCollection<CharacterPresentationPoseSourceSlot>
                reachableSources,
            List<PoseGraphId> callPath,
            HashSet<PoseGraphId> reachableGraphs,
            CharacterPoseGraphValidationReport report)
        {
            if (payload.Subgraph == null ||
                !payload.Subgraph.PoseGraphId.IsValid ||
                !ownerAsset.TryGetGraph(
                    payload.Subgraph.PoseGraphId,
                    out CharacterTypedPoseGraph child))
            {
                Report(
                    report,
                    CharacterPoseGraphValidationCode
                        .SubgraphOwnershipInvalid,
                    $"Pose Node '{node.NodeId}' references a missing root-owned Pose Graph.",
                    ownerGraph.GraphId,
                    node.NodeId);
                return;
            }
            ValidateGraph(
                ownerAsset,
                child,
                rig,
                portResolver,
                null,
                reachableSources,
                GraphRole.Subgraph,
                callPath,
                reachableGraphs,
                report);
        }

        static void ValidateMotionMatchingEntryGraph(
            CharacterPresentationPoseGraphAsset ownerAsset,
            CharacterTypedPoseGraph ownerGraph,
            CharacterTypedPoseNode node,
            CharacterMotionMatchingPosePayload payload,
            CharacterAnimationRigDefinition rig,
            CharacterPosePortContractResolver portResolver,
            List<PoseGraphId> callPath,
            HashSet<PoseGraphId> reachableGraphs,
            CharacterPoseGraphValidationReport report)
        {
            if (payload.EntryGraph == null ||
                !payload.EntryGraph.PoseGraphId.IsValid ||
                !ownerAsset.TryGetGraph(payload.EntryGraph.PoseGraphId, out CharacterTypedPoseGraph entryGraph))
            {
                Report(
                    report,
                    CharacterPoseGraphValidationCode.MotionMatchingInvalid,
                    $"Motion Matching Pose '{node.NodeId}' references a missing entry processing graph.",
                    ownerGraph.GraphId,
                    node.NodeId);
                return;
            }
            try
            {
                CharacterMotionMatchingEntryGraphPolicy.RequireValid(entryGraph);
            }
            catch (Exception exception)
            {
                Report(
                    report,
                    CharacterPoseGraphValidationCode.MotionMatchingInvalid,
                    $"Motion Matching Pose '{node.NodeId}': {exception.Message}",
                    ownerGraph.GraphId,
                    node.NodeId);
                return;
            }
            ValidateGraph(
                ownerAsset,
                entryGraph,
                rig,
                portResolver,
                null,
                null,
                GraphRole.Subgraph,
                callPath,
                reachableGraphs,
                report);
        }

        static void ValidateMotionMatchingTopology(
            CharacterTypedPoseGraph graph,
            IReadOnlyDictionary<PoseNodeId, CharacterTypedPoseNode> nodes,
            CharacterPoseGraphValidationReport report)
        {
            var collectorOwners = new Dictionary<PoseNodeId, PoseNodeId>();
            foreach (CharacterTypedPoseNode node in nodes.Values)
            {
                if (node.Kind != CharacterPoseNodeKind.MotionMatchingPose)
                    continue;
                CharacterPoseEdge[] historyEdges = graph.Edges
                    .Where(edge => edge != null &&
                                   edge.TargetNodeId == node.NodeId &&
                                   edge.TargetPortId.Equals(CharacterMotionMatchingPosePorts.History))
                    .ToArray();
                if (historyEdges.Length != 1 ||
                    !nodes.TryGetValue(historyEdges[0].SourceNodeId, out CharacterTypedPoseNode collector) ||
                    collector.Kind != CharacterPoseNodeKind.PoseHistoryCollector ||
                    !historyEdges[0].SourcePortId.Equals(CharacterMotionMatchingPosePorts.History))
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode.MotionMatchingInvalid,
                        $"Motion Matching Pose '{node.NodeId}' requires exactly one Pose History Collector read edge.",
                        graph.GraphId,
                        node.NodeId,
                        CharacterMotionMatchingPosePorts.History);
                    continue;
                }
                if (!collectorOwners.TryAdd(collector.NodeId, node.NodeId))
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode.MotionMatchingInvalid,
                        $"Pose History Collector '{collector.NodeId}' has competing Motion Matching owners.",
                        graph.GraphId,
                        collector.NodeId);
                }
                CharacterPoseEdge[] commitEdges = graph.Edges
                    .Where(edge => edge != null &&
                                   edge.SourceNodeId == node.NodeId &&
                                   edge.SourcePortId.Equals(CharacterMotionMatchingPosePorts.LocalPoseOutput) &&
                                   edge.TargetNodeId == collector.NodeId &&
                                   edge.TargetPortId.Equals(CharacterMotionMatchingPosePorts.LocalPoseInput))
                    .ToArray();
                if (commitEdges.Length != 1)
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode.MotionMatchingInvalid,
                        $"Motion Matching Pose '{node.NodeId}' must commit its base Local Pose through Collector '{collector.NodeId}'.",
                        graph.GraphId,
                        node.NodeId,
                        CharacterMotionMatchingPosePorts.LocalPoseOutput);
                }
                if (node.Payload is CharacterMotionMatchingPosePayload motionMatching &&
                    collector.Payload is CharacterPoseHistoryCollectorPayload history &&
                    (!motionMatching.Binding || !history.HistoryId.IsValid))
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode.MotionMatchingInvalid,
                        $"Motion Matching Pose '{node.NodeId}' Binding or Collector history identity is incomplete.",
                        graph.GraphId,
                        node.NodeId);
                }
            }
            foreach (CharacterTypedPoseNode collector in nodes.Values)
            {
                if (collector.Kind == CharacterPoseNodeKind.PoseHistoryCollector &&
                    !collectorOwners.ContainsKey(collector.NodeId))
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode.MotionMatchingInvalid,
                        $"Pose History Collector '{collector.NodeId}' has no Motion Matching owner.",
                        graph.GraphId,
                        collector.NodeId);
                }
            }
        }

        static HashSet<PoseParameterId> ValidateParameters(
            CharacterTypedPoseGraph graph,
            CharacterPoseGraphValidationReport report)
        {
            var result = new HashSet<PoseParameterId>();
            for (int i = 0; i < graph.Parameters.Count; i++)
            {
                CharacterPoseParameterDeclaration parameter =
                    graph.Parameters[i];
                if (parameter == null ||
                    !parameter.ParameterId.IsValid ||
                    !Enum.IsDefined(
                        typeof(PoseParameterValueType),
                        parameter.ValueType) ||
                    !float.IsFinite(parameter.DefaultValue))
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode
                            .ParameterInvalid,
                        $"Pose Graph '{graph.GraphId}' Parameter #{i} is invalid.",
                        graph.GraphId);
                    continue;
                }
                if (!result.Add(parameter.ParameterId))
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode
                            .ParameterDuplicate,
                        $"Pose Graph '{graph.GraphId}' duplicates Parameter '{parameter.ParameterId}'.",
                        graph.GraphId);
                }
            }
            return result;
        }

        static void ValidateParameterPolicies(
            CharacterTypedPoseGraph graph,
            CharacterTypedPoseNode node,
            IReadOnlyList<CharacterPoseParameterPolicy> policies,
            HashSet<PoseParameterId> parameters,
            CharacterPoseGraphValidationReport report)
        {
            var covered = new HashSet<PoseParameterId>();
            for (int i = 0; i < policies.Count; i++)
            {
                CharacterPoseParameterPolicy policy = policies[i];
                if (policy == null ||
                    !parameters.Contains(policy.ParameterId) ||
                    !covered.Add(policy.ParameterId) ||
                    !Enum.IsDefined(
                        typeof(PoseParameterResolvePolicy),
                        policy.Policy))
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode
                            .ParameterPolicyMissing,
                        $"Pose Node '{node.NodeId}' Parameter policy #{i} is invalid.",
                        graph.GraphId,
                        node.NodeId);
                }
            }
            foreach (PoseParameterId parameter in parameters)
            {
                if (!covered.Contains(parameter))
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode
                            .ParameterPolicyMissing,
                        $"Pose Node '{node.NodeId}' has no policy for Parameter '{parameter}'.",
                        graph.GraphId,
                        node.NodeId);
                }
            }
        }

        static void ValidateEdges(
            CharacterTypedPoseGraph graph,
            Dictionary<PoseNodeId, CharacterTypedPoseNode> nodes,
            Dictionary<string, CharacterPosePortDefinition> ports,
            CharacterPosePortContractResolver portResolver,
            bool outputGraph,
            bool requireFullBodyIk,
            CharacterPoseGraphValidationReport report)
        {
            var adjacency =
                new Dictionary<PoseNodeId, List<PoseNodeId>>();
            var reverse =
                new Dictionary<PoseNodeId, List<PoseNodeId>>();
            var incoming =
                new HashSet<string>(StringComparer.Ordinal);
            var edgeIds =
                new HashSet<string>(StringComparer.Ordinal);
            var componentPoseProducers = new Dictionary<PoseNodeId, PoseNodeId>();
            var goalProducers = new Dictionary<PoseNodeId, List<PoseNodeId>>();
            var goalConsumerCounts = new Dictionary<PoseNodeId, int>();
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                CharacterPoseEdge edge = graph.Edges[i];
                if (edge == null ||
                    string.IsNullOrWhiteSpace(edge.EdgeId) ||
                    !edgeIds.Add(edge.EdgeId))
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode
                            .EdgeDuplicate,
                        $"Pose Graph '{graph.GraphId}' Edge #{i} is invalid or duplicated.",
                        graph.GraphId);
                    continue;
                }
                string sourceKey =
                    edge.SourceNodeId.Value + "\0" +
                    edge.SourcePortId.Value;
                string targetKey =
                    edge.TargetNodeId.Value + "\0" +
                    edge.TargetPortId.Value;
                if (!nodes.TryGetValue(edge.SourceNodeId, out CharacterTypedPoseNode sourceNode) ||
                    !nodes.TryGetValue(edge.TargetNodeId, out CharacterTypedPoseNode targetNode) ||
                    !ports.TryGetValue(
                        sourceKey,
                        out CharacterPosePortDefinition source) ||
                    !ports.TryGetValue(
                        targetKey,
                        out CharacterPosePortDefinition target) ||
                    source.Direction !=
                    CharacterPosePortDirection.Output ||
                    target.Direction !=
                    CharacterPosePortDirection.Input)
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode.EdgeInvalid,
                        $"Pose Edge '{edge.EdgeId}' references an invalid endpoint.",
                        graph.GraphId);
                    continue;
                }
                if (source.Kind != target.Kind)
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode
                            .PortTypeMismatch,
                        $"Pose Edge '{edge.EdgeId}' connects '{source.Kind}' to '{target.Kind}'.",
                        graph.GraphId);
                }
                else if (source.Kind == CharacterPosePortKind.FullBodyIkGoals)
                {
                    bool sourceAllowed = sourceNode.Kind == CharacterPoseNodeKind.PoseBoneIKGoals ||
                                         sourceNode.Kind == CharacterPoseNodeKind.PredictiveFootPlacement ||
                                         sourceNode.Kind == CharacterPoseNodeKind.GraphInput ||
                                         sourceNode.Kind == CharacterPoseNodeKind.PoseSubgraph;
                    bool targetAllowed = targetNode.Kind == CharacterPoseNodeKind.FullBodyIK ||
                                         targetNode.Kind == CharacterPoseNodeKind.GraphOutput ||
                                         targetNode.Kind == CharacterPoseNodeKind.PoseSubgraph;
                    if (!sourceAllowed || !targetAllowed)
                    {
                        Report(
                            report,
                            CharacterPoseGraphValidationCode.FullBodyIkInvalid,
                            $"Pose Edge '{edge.EdgeId}' connects Full Body IK Goals outside a Goal Source, Full Body IK or an explicitly typed Subgraph boundary.",
                            graph.GraphId,
                            edge.TargetNodeId,
                            edge.TargetPortId);
                    }
                    else
                    {
                        if (sourceNode.Kind == CharacterPoseNodeKind.PoseBoneIKGoals ||
                            sourceNode.Kind == CharacterPoseNodeKind.PredictiveFootPlacement)
                        {
                            goalConsumerCounts.TryGetValue(edge.SourceNodeId, out int count);
                            goalConsumerCounts[edge.SourceNodeId] = count + 1;
                        }
                        if (targetNode.Kind == CharacterPoseNodeKind.FullBodyIK)
                            Add(goalProducers, edge.TargetNodeId, edge.SourceNodeId);
                    }
                }
                else if (source.Kind == CharacterPosePortKind.ComponentPose &&
                         (targetNode.Kind == CharacterPoseNodeKind.PoseBoneIKGoals ||
                          targetNode.Kind == CharacterPoseNodeKind.PredictiveFootPlacement ||
                          targetNode.Kind == CharacterPoseNodeKind.FullBodyIK))
                {
                    componentPoseProducers.TryAdd(edge.TargetNodeId, edge.SourceNodeId);
                }
                if (!incoming.Add(targetKey))
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode
                            .IllegalFanIn,
                        $"Pose Port '{edge.TargetNodeId}/{edge.TargetPortId}' has multiple incoming edges.",
                        graph.GraphId,
                        edge.TargetNodeId,
                        edge.TargetPortId);
                }
                if (source.Kind != CharacterPosePortKind.PoseHistory)
                {
                    Add(
                        adjacency,
                        edge.SourceNodeId,
                        edge.TargetNodeId);
                    Add(
                        reverse,
                        edge.TargetNodeId,
                        edge.SourceNodeId);
                }
            }
            foreach (CharacterTypedPoseNode node in nodes.Values)
            {
                foreach (CharacterPosePortDefinition port in
                         portResolver(node))
                {
                    if (port != null &&
                        port.Required &&
                        port.Direction ==
                        CharacterPosePortDirection.Input &&
                        !incoming.Contains(
                            node.NodeId.Value + "\0" +
                            port.PortId.Value))
                    {
                        Report(
                            report,
                            CharacterPoseGraphValidationCode
                                .RequiredInputMissing,
                            $"Pose Node '{node.NodeId}' required Port '{port.PortId}' is disconnected.",
                            graph.GraphId,
                            node.NodeId,
                            port.PortId);
                    }
                }
            }
            ValidateFullBodyIkTopology(
                graph,
                nodes,
                componentPoseProducers,
                goalProducers,
                goalConsumerCounts,
                requireFullBodyIk,
                report);
            ValidateMotionMatchingTopology(graph, nodes, report);
            DetectCycles(
                graph.GraphId.Value,
                nodes.Keys,
                adjacency,
                report);
            ValidateRootOrientationWarps(
                graph,
                nodes,
                report);
            if (outputGraph)
                ValidateReachability(nodes, reverse);
        }

        static void ValidateFullBodyIkTopology(
            CharacterTypedPoseGraph graph,
            IReadOnlyDictionary<PoseNodeId, CharacterTypedPoseNode> nodes,
            IReadOnlyDictionary<PoseNodeId, PoseNodeId> componentPoseProducers,
            IReadOnlyDictionary<PoseNodeId, List<PoseNodeId>> goalProducers,
            IReadOnlyDictionary<PoseNodeId, int> goalConsumerCounts,
            bool requireFullBodyIk,
            CharacterPoseGraphValidationReport report)
        {
            int solverCount = 0;
            foreach (CharacterTypedPoseNode node in nodes.Values)
            {
                if (node.Kind == CharacterPoseNodeKind.PoseBoneIKGoals ||
                    node.Kind == CharacterPoseNodeKind.PredictiveFootPlacement)
                {
                    goalConsumerCounts.TryGetValue(node.NodeId, out int count);
                    if (count != 1)
                    {
                        Report(
                            report,
                            node.Kind == CharacterPoseNodeKind.PredictiveFootPlacement
                                ? CharacterPoseGraphValidationCode.PredictiveFootPlacementInvalid
                                : CharacterPoseGraphValidationCode.PoseBoneIkGoalsInvalid,
                            $"Goal Source '{node.NodeId}' must have exactly one Full Body IK consumer.",
                            graph.GraphId,
                            node.NodeId);
                    }
                    continue;
                }
                if (node.Kind != CharacterPoseNodeKind.FullBodyIK)
                    continue;
                solverCount++;
                if (!componentPoseProducers.TryGetValue(node.NodeId, out PoseNodeId poseProducer) ||
                    !goalProducers.TryGetValue(node.NodeId, out List<PoseNodeId> sources) ||
                    sources.Count == 0)
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode.FullBodyIkInvalid,
                        $"Full Body IK '{node.NodeId}' requires one Component Pose and one or more Goal Sets.",
                        graph.GraphId,
                        node.NodeId);
                    continue;
                }
                for (int i = 0; i < sources.Count; i++)
                {
                    PoseNodeId source = sources[i];
                    if (!nodes.TryGetValue(source, out CharacterTypedPoseNode sourceNode) ||
                        (sourceNode.Kind != CharacterPoseNodeKind.PoseBoneIKGoals &&
                         sourceNode.Kind != CharacterPoseNodeKind.PredictiveFootPlacement))
                        continue;
                    if (!componentPoseProducers.TryGetValue(source, out PoseNodeId sourcePoseProducer) ||
                        sourcePoseProducer != poseProducer)
                    {
                        Report(
                            report,
                            CharacterPoseGraphValidationCode.FullBodyIkInvalid,
                            $"Goal Source '{source}' and Full Body IK '{node.NodeId}' must read the same Component Pose branch.",
                            graph.GraphId,
                            node.NodeId);
                    }
                }
            }
            if (requireFullBodyIk && solverCount != 1)
            {
                Report(
                    report,
                    CharacterPoseGraphValidationCode.FullBodyIkInvalid,
                    $"Root Pose Graph '{graph.GraphId}' requires exactly one Full Body IK node.",
                    graph.GraphId);
            }
        }

        static void ValidateRootOrientationWarps(
            CharacterTypedPoseGraph graph,
            IReadOnlyDictionary<PoseNodeId, CharacterTypedPoseNode> nodes,
            CharacterPoseGraphValidationReport report)
        {
            foreach (CharacterTypedPoseNode node in nodes.Values)
            {
                if (node.Kind != CharacterPoseNodeKind.RootOrientationWarp)
                    continue;
                CharacterPoseEdge input = graph.Edges.FirstOrDefault(edge =>
                    edge != null && edge.TargetNodeId.Equals(node.NodeId));
                if (input == null ||
                    !nodes.TryGetValue(input.SourceNodeId, out CharacterTypedPoseNode source) ||
                    source.Kind != CharacterPoseNodeKind.SequencePlayer ||
                    source.Payload is not CharacterSequencePlayerPosePayload sequence ||
                    sequence.Loop)
                {
                    Report(
                        report,
                        CharacterPoseGraphValidationCode.NodeInvalid,
                        $"Root Orientation Warp '{node.NodeId}' must receive Pose directly from one finite Sequence Player.",
                        graph.GraphId,
                        node.NodeId);
                }
            }
        }

        static void ValidateReachability(
            Dictionary<PoseNodeId, CharacterTypedPoseNode> nodes,
            Dictionary<PoseNodeId, List<PoseNodeId>> reverse)
        {
            CharacterTypedPoseNode output =
                nodes.Values.SingleOrDefault(node =>
                    CharacterPoseCompilerHandlerRegistry.Shared
                        .Require(node.Kind).NativeRole ==
                    CharacterPoseNativeNodeRole.PoseOutput);
            if (output == null)
                return;
            var reachable = new HashSet<PoseNodeId>();
            var stack = new Stack<PoseNodeId>();
            stack.Push(output.NodeId);
            while (stack.Count > 0)
            {
                PoseNodeId current = stack.Pop();
                if (!reachable.Add(current) ||
                    !reverse.TryGetValue(
                        current,
                        out List<PoseNodeId> upstream))
                    continue;
                for (int i = 0; i < upstream.Count; i++)
                    stack.Push(upstream[i]);
            }
        }

        static void DetectCycles(
            string graphId,
            IEnumerable<PoseNodeId> nodes,
            Dictionary<PoseNodeId, List<PoseNodeId>> adjacency,
            CharacterPoseGraphValidationReport report)
        {
            var colors = new Dictionary<PoseNodeId, byte>();
            foreach (PoseNodeId node in nodes)
                Visit(node);

            void Visit(PoseNodeId node)
            {
                if (colors.TryGetValue(node, out byte color))
                {
                    if (color == 1)
                    {
                        Report(
                            report,
                            CharacterPoseGraphValidationCode.Cycle,
                            $"Pose Graph '{graphId}' contains a cycle at '{node}'.",
                            graphId,
                            node);
                    }
                    return;
                }
                colors[node] = 1;
                if (adjacency.TryGetValue(
                        node,
                        out List<PoseNodeId> next))
                {
                    for (int i = 0; i < next.Count; i++)
                        Visit(next[i]);
                }
                colors[node] = 2;
            }
        }

        static GraphAuthoringDocumentRoleId Role(GraphRole role) =>
            role switch
            {
                GraphRole.Root =>
                    CharacterPoseGraphAuthoringCapabilities.RootGraph,
                GraphRole.StatePose =>
                    CharacterPoseGraphAuthoringCapabilities
                        .StatePoseGraph,
                GraphRole.Subgraph =>
                    CharacterPoseGraphAuthoringCapabilities.Subgraph,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(role))
            };

        static void Add(
            Dictionary<PoseNodeId, List<PoseNodeId>> values,
            PoseNodeId key,
            PoseNodeId value)
        {
            if (!values.TryGetValue(
                    key,
                    out List<PoseNodeId> entries))
            {
                entries = new List<PoseNodeId>();
                values.Add(key, entries);
            }
            entries.Add(value);
        }

        static void Report(
            CharacterPoseGraphValidationReport report,
            CharacterPoseGraphValidationCode code,
            string message,
            PoseGraphId graphId,
            PoseNodeId nodeId = default,
            PosePortId portId = default) =>
            report.Add(
                new CharacterPoseGraphValidationIssue(
                    code,
                    message,
                    graphId.Value,
                    nodeId,
                    portId));

        static void Report(
            CharacterPoseGraphValidationReport report,
            CharacterPoseGraphValidationCode code,
            string message,
            string graphId = "",
            PoseNodeId nodeId = default,
            PosePortId portId = default) =>
            report.Add(
                new CharacterPoseGraphValidationIssue(
                    code,
                    message,
                    graphId,
                    nodeId,
                    portId));
    }
}
