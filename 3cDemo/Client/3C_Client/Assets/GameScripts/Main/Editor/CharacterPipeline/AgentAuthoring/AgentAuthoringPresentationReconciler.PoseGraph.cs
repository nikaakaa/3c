using TreeDesigner.Authoring;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BTSMTL.Timeline;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;
using AnimationClip = UnityEngine.AnimationClip;


namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed partial class AgentAuthoringPresentationReconciler
    {
        void PreparePoseSourceSlots(
            AgentPackagePresentationProfileFile current,
            AgentPackagePresentationProfileFile target,
            CharacterPresentationPoseGraphAsset poseGraph,
            string poseGraphOwnerId,
            PlanBuilder builder,
            AgentCompileReport report)
        {
            Dictionary<string, AgentPackagePoseSourceBinding> targetBySlot =
                Index(target.poseSources, value => ReferenceIdentity(value.slot));
            foreach (AgentPackagePoseSourceBinding removed in current.poseSources
                         .Where(value => !targetBySlot.ContainsKey(ReferenceIdentity(value.slot))))
            {
                CharacterPresentationPoseSourceSlot slot =
                    Resolve<CharacterPresentationPoseSourceSlot>(
                        removed.slot,
                        $"editable/presentation/profile.json.poseSources[{removed.name}].slot",
                        report);
                if (slot)
                {
                    builder.Graph(
                        $"editable/presentation/profile.json.poseSources[{removed.name}]",
                        new DeletePoseSourceSlotMutation(poseGraphOwnerId, slot));
                }
            }
            foreach (AgentPackagePoseSourceBinding source in target.poseSources)
            {
                string path = $"editable/presentation/profile.json.poseSources[{source.name}]";
                PresentationPoseSourceKind kind =
                    Enum.Parse<PresentationPoseSourceKind>(source.kind, false);
                if (!string.IsNullOrWhiteSpace(source.slot.localId))
                {
                    CharacterPresentationPoseSourceSlot slot =
                        CharacterAnimationPresentationAuthoringService.CreateSourceSlot(kind);
                    slot.name = source.name.Trim();
                    if (!m_LocalAssets.TryAdd(source.slot.localId, slot))
                    {
                        report.Error(
                            path + ".slot.localId",
                            "presentation_local_asset_identity_duplicate",
                            "Source Slot local identity重复。");
                        UnityEngine.Object.DestroyImmediate(slot);
                        continue;
                    }
                    builder.Graph(
                        path + ".slot",
                        new CreatePoseSourceSlotMutation(poseGraphOwnerId, slot));
                    continue;
                }
                CharacterPresentationPoseSourceSlot existing =
                    Resolve<CharacterPresentationPoseSourceSlot>(
                        source.slot,
                        path + ".slot",
                        report);
                if (!existing || !poseGraph.SourceSlots.Contains(existing) || existing.SourceKind != kind)
                {
                    report.Error(
                        path + ".slot",
                        "presentation_pose_source_slot_owner_mismatch",
                        "Source Slot必须属于当前Pose Graph且类型匹配。");
                    continue;
                }
                if (!string.Equals(existing.name, source.name, StringComparison.Ordinal))
                {
                    builder.Graph(
                        path + ".name",
                        new RenamePoseSourceSlotMutation(
                            poseGraphOwnerId,
                            existing,
                            source.name));
                }
            }
        }

        void BuildGraphPlan(
            AgentDocumentPresentationEditable current,
            AgentDocumentPresentationEditable target,
            CharacterPresentationPoseGraphAsset poseAsset,
            string poseAssetId,
            PlanBuilder builder,
            AgentCompileReport report,
            bool requireRoot = true)
        {
            Dictionary<string, AgentPackagePoseGraphFile> oldGraphs = Index(
                current.poseGraphs,
                value => value.id);
            Dictionary<string, AgentPackagePoseGraphLayoutFile> oldLayouts =
                Index(current.poseGraphLayouts, value => value.graphId);
            Dictionary<string, AgentPackagePoseGraphFile> newGraphs = Index(
                target.poseGraphs,
                value => value.id);
            Dictionary<string, AgentPackagePoseGraphLayoutFile> newLayouts =
                Index(target.poseGraphLayouts, value => value.graphId);
            string rootGraphId = poseAsset.Graph?.GraphId.Value ?? string.Empty;
            AgentPackagePoseGraphFile targetRoot = target.poseGraphs.SingleOrDefault(
                value => string.Equals(
                    value.role,
                    CharacterPoseGraphAuthoringCapabilities.RootGraph.Value,
                    StringComparison.Ordinal));
            if (requireRoot && (targetRoot == null ||
                !string.Equals(
                    targetRoot.id,
                    rootGraphId,
                    StringComparison.Ordinal)))
            {
                report.Error(
                    "editable/presentation/pose-graphs",
                    "presentation_root_graph_identity_changed",
                    "Document不得替换现有Pose Graph asset的root graph identity。");
                return;
            }

            foreach (AgentPackagePoseGraphFile graph in target.poseGraphs
                         .OrderBy(value => value.id, StringComparer.Ordinal))
            {
                string path = GraphPath(graph.id);
                if (!newLayouts.TryGetValue(
                        graph.id,
                        out AgentPackagePoseGraphLayoutFile layout))
                {
                    report.Error(
                        path,
                        "presentation_pose_layout_missing",
                        "Pose Graph缺少layout目标状态。");
                    continue;
                }
                if (!oldGraphs.TryGetValue(
                        graph.id,
                        out AgentPackagePoseGraphFile oldGraph))
                {
                    CharacterPoseCanvasGraph created = ConvertGraph(
                        graph,
                        layout,
                        target,
                        report,
                        path);
                    if (created != null)
                        builder.Graph(
                            path,
                            new CreatePoseGraphMutation(
                                poseAssetId,
                                created));
                    continue;
                }
                if (!string.Equals(
                        oldGraph.role,
                        graph.role,
                        StringComparison.Ordinal))
                {
                    report.Error(
                        path + ".role",
                        "presentation_pose_graph_role_changed",
                        "Pose Graph role不可原地改变，必须删除并以新identity创建。");
                    continue;
                }
                oldLayouts.TryGetValue(
                    graph.id,
                    out AgentPackagePoseGraphLayoutFile oldLayout);
                BuildExistingGraphPlan(
                    oldGraph,
                    oldLayout,
                    graph,
                    layout,
                    target,
                    builder,
                    report);
            }

            foreach (AgentPackagePoseGraphFile removed in current.poseGraphs
                         .Where(value => !newGraphs.ContainsKey(value.id))
                         .OrderByDescending(value => value.id, StringComparer.Ordinal))
            {
                if (requireRoot && string.Equals(
                        removed.id,
                        rootGraphId,
                        StringComparison.Ordinal))
                {
                    report.Error(
                        GraphPath(removed.id),
                        "presentation_root_graph_delete_forbidden",
                        "Root Pose Graph不可删除。");
                    continue;
                }
                builder.Graph(
                    GraphPath(removed.id),
                    new DeletePoseGraphMutation(
                        poseAssetId,
                        new PoseGraphId(removed.id)));
            }
        }

        void BuildExistingGraphPlan(
            AgentPackagePoseGraphFile current,
            AgentPackagePoseGraphLayoutFile currentLayout,
            AgentPackagePoseGraphFile target,
            AgentPackagePoseGraphLayoutFile targetLayout,
            AgentDocumentPresentationEditable presentation,
            PlanBuilder builder,
            AgentCompileReport report)
        {
            string path = GraphPath(target.id);
            if (!Same(current.parameters, target.parameters))
            {
                builder.Graph(
                    path + ".parameters",
                    new SetPoseGraphParametersMutation(
                        target.id,
                        target.parameters.Select(ConvertParameter).ToArray()));
            }

            Dictionary<string, AgentPackagePoseNode> oldNodes =
                Index(current.nodes, value => value.id);
            Dictionary<string, AgentPackagePoseNode> newNodes =
                Index(target.nodes, value => value.id);
            Dictionary<string, AgentPackagePoseNodeLayout> oldPositions =
                Index(
                    currentLayout?.nodes,
                    value => value.id);
            Dictionary<string, AgentPackagePoseNodeLayout> newPositions =
                Index(targetLayout.nodes, value => value.id);
            Dictionary<string, AgentPackagePoseEdge> oldEdges =
                Index(current.edges, value => value.id);
            Dictionary<string, AgentPackagePoseEdge> newEdges =
                Index(target.edges, value => value.id);

            foreach (AgentPackagePoseEdge edge in current.edges
                         .Where(value =>
                             !newEdges.TryGetValue(value.id, out AgentPackagePoseEdge next) ||
                             !Same(value, next))
                         .OrderBy(value => value.id, StringComparer.Ordinal))
            {
                builder.Graph(
                    path + $".edges[{edge.id}]",
                    new DisconnectPosePortMutation(target.id, edge.id));
            }

            foreach (AgentPackagePoseNode node in current.nodes
                         .Where(value =>
                             !newNodes.ContainsKey(value.id) ||
                             !string.Equals(
                                 value.capability,
                                 newNodes[value.id].capability,
                                 StringComparison.Ordinal) ||
                             !string.Equals(
                                 value.childDocumentId,
                                 newNodes[value.id].childDocumentId,
                                 StringComparison.Ordinal))
                         .OrderBy(value => value.id, StringComparer.Ordinal))
            {
                builder.Graph(
                    path + $".nodes[{node.id}]",
                    new DeletePoseNodeMutation(
                        target.id,
                        new PoseNodeId(node.id)));
            }

            foreach (AgentPackagePoseNode node in target.nodes
                         .OrderBy(value => value.id, StringComparer.Ordinal))
            {
                bool recreate =
                    !oldNodes.TryGetValue(
                        node.id,
                        out AgentPackagePoseNode oldNode) ||
                    !string.Equals(
                        oldNode.capability,
                        node.capability,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        oldNode.childDocumentId,
                        node.childDocumentId,
                        StringComparison.Ordinal);
                if (recreate)
                {
                    CharacterPoseCanvasNode created = ConvertNode(
                        node,
                        target.role,
                        presentation,
                        report,
                        path + $".nodes[{node.id}]");
                    if (created != null)
                    {
                        AgentPackagePoseNodeLayout position =
                            newPositions[node.id];
                        builder.Graph(
                            path + $".nodes[{node.id}]",
                            new CreatePoseNodeMutation(
                                target.id,
                                created,
                                new Vector2(position.x, position.y)));
                    }
                    continue;
                }

                if (!string.Equals(
                        oldNode.name,
                        node.name,
                        StringComparison.Ordinal))
                {
                    builder.Graph(
                        path + $".nodes[{node.id}].name",
                        new SetPoseNodeNameMutation(
                            target.id,
                            new PoseNodeId(node.id),
                            node.name));
                }
                CharacterPoseCanvasNode decodedTarget = null;
                foreach (JProperty property in node.properties.Properties()
                             .OrderBy(value => value.Name, StringComparer.Ordinal))
                {
                    if (SamePoseField(
                            oldNode.properties[property.Name],
                            property.Value))
                        continue;
                    decodedTarget ??= ConvertNode(
                        node,
                        target.role,
                        presentation,
                        report,
                        path + $".nodes[{node.id}]");
                    if (decodedTarget != null && !report.HasErrors())
                    {
                        object value =
                            CharacterPoseAuthoringPayloadCodec.Read(
                                decodedTarget.Payload,
                                property.Name);
                        builder.Graph(
                            path + $".nodes[{node.id}].properties.{property.Name}",
                            new SetPoseNodeFieldMutation(
                                target.id,
                                new PoseNodeId(node.id),
                                property.Name,
                                value));
                    }
                }

                Dictionary<string, AgentPackagePoseDynamicPort> oldPorts =
                    Index(oldNode.dynamicPorts, value => value.id);
                Dictionary<string, AgentPackagePoseDynamicPort> newPorts =
                    Index(node.dynamicPorts, value => value.id);
                foreach (AgentPackagePoseDynamicPort removed in
                         oldNode.dynamicPorts.Where(value =>
                             !newPorts.TryGetValue(
                                 value.id,
                                 out AgentPackagePoseDynamicPort next) ||
                             !Same(value, next)))
                {
                    builder.Graph(
                        path + $".nodes[{node.id}].dynamicPorts[{removed.id}]",
                        new RemoveDynamicPosePortMutation(
                            target.id,
                            new PoseNodeId(node.id),
                            new PosePortId(removed.id)));
                }
                foreach (AgentPackagePoseDynamicPort added in
                         node.dynamicPorts.Where(value =>
                             !oldPorts.TryGetValue(
                                 value.id,
                                 out AgentPackagePoseDynamicPort previous) ||
                             !Same(value, previous)))
                {
                    builder.Graph(
                        path + $".nodes[{node.id}].dynamicPorts[{added.id}]",
                        new AddDynamicPosePortMutation(
                            target.id,
                            new PoseNodeId(node.id),
                            ConvertPort(added)));
                }

                AgentPackagePoseNodeLayout oldPosition = oldPositions[node.id];
                AgentPackagePoseNodeLayout newPosition = newPositions[node.id];
                if (!Mathf.Approximately(oldPosition.x, newPosition.x) ||
                    !Mathf.Approximately(oldPosition.y, newPosition.y))
                {
                    builder.Graph(
                        path + $".layout[{node.id}]",
                        new MovePoseNodeMutation(
                            target.id,
                            new PoseNodeId(node.id),
                            new Vector2(newPosition.x, newPosition.y)));
                }
            }

            foreach (AgentPackagePoseEdge edge in target.edges
                         .Where(value =>
                             !oldEdges.TryGetValue(value.id, out AgentPackagePoseEdge old) ||
                             !Same(old, value))
                         .OrderBy(value => value.id, StringComparer.Ordinal))
            {
                builder.Graph(
                    path + $".edges[{edge.id}]",
                    new ConnectPosePortMutation(
                        target.id,
                        edge.id,
                        new PoseNodeId(edge.from.node),
                        new PosePortId(edge.from.port),
                        new PoseNodeId(edge.to.node),
                        new PosePortId(edge.to.port)));
            }
        }

        void BuildStateMachinePlan(
            AgentDocumentPresentationEditable current,
            AgentDocumentPresentationEditable target,
            PlanBuilder builder,
            AgentCompileReport report)
        {
            Dictionary<string, AgentPackagePoseStateMachineFile> oldMachines =
                Index(current.poseStateMachines, value => value.id);
            Dictionary<string, AgentPackagePoseStateMachineLayoutFile>
                oldLayouts = Index(
                    current.poseStateMachineLayouts,
                    value => value.stateMachineId);
            Dictionary<string, AgentPackagePoseStateMachineLayoutFile>
                targetLayouts = Index(
                    target.poseStateMachineLayouts,
                    value => value.stateMachineId);
            foreach (AgentPackagePoseStateMachineFile machine in
                     target.poseStateMachines.OrderBy(
                         value => value.id,
                         StringComparer.Ordinal))
            {
                AgentPackagePoseStateMachineLayoutFile targetLayout =
                    targetLayouts[machine.id];
                if (!oldMachines.TryGetValue(
                        machine.id,
                        out AgentPackagePoseStateMachineFile old))
                {
                    foreach (AgentPackagePoseStateMachineLayoutElement element
                             in targetLayout.elements.OrderBy(
                                 value => value.id,
                                 StringComparer.Ordinal))
                    {
                        builder.Graph(
                            StateMachineLayoutPath(machine.id) +
                            $".elements[{element.id}]",
                            new SetPoseStateMachineLayoutElementMutation(
                                machine.id,
                                element.id,
                                new Vector2(element.x, element.y)));
                    }
                    continue;
                }
                string path = StateMachinePath(machine.id);
                AgentPackagePoseStateMachineLayoutFile oldLayout =
                    oldLayouts[machine.id];
                Dictionary<string, AgentPackagePoseStateMachineLayoutElement>
                    oldElements = Index(
                        oldLayout.elements,
                        value => value.id);
                Dictionary<string, AgentPackagePoseStateMachineLayoutElement>
                    newElements = Index(
                        targetLayout.elements,
                        value => value.id);
                foreach (AgentPackagePoseStateMachineLayoutElement element in
                         oldLayout.elements
                             .Where(value => !newElements.ContainsKey(value.id))
                             .OrderBy(value => value.id, StringComparer.Ordinal))
                {
                    builder.Graph(
                        StateMachineLayoutPath(machine.id) +
                        $".elements[{element.id}]",
                        new RemovePoseStateMachineLayoutElementMutation(
                            machine.id,
                            element.id));
                }
                Dictionary<string, AgentPackagePoseTransition> oldTransitions =
                    Index(old.transitions, value => value.id);
                Dictionary<string, AgentPackagePoseTransition> newTransitions =
                    Index(machine.transitions, value => value.id);
                foreach (AgentPackagePoseTransition transition in old.transitions
                             .Where(value =>
                                  !newTransitions.TryGetValue(
                                      value.id,
                                      out AgentPackagePoseTransition next) ||
                                  !SameTransition(value, next))
                             .OrderBy(value => value.id, StringComparer.Ordinal))
                {
                    builder.Graph(
                        path + $".transitions[{transition.id}]",
                        new DeletePoseTransitionMutation(
                            machine.id,
                            new PoseStateTransitionId(transition.id)),
                        DescribeTransition(old, transition, "Delete"));
                }

                Dictionary<string, AgentPackagePoseState> oldStates =
                    Index(old.states, value => value.id);
                Dictionary<string, AgentPackagePoseState> newStates =
                    Index(machine.states, value => value.id);
                foreach (AgentPackagePoseState state in old.states
                             .Where(value =>
                                 !newStates.TryGetValue(
                                     value.id,
                                     out AgentPackagePoseState next) ||
                                  !SameStateStructure(value, next))
                             .OrderBy(value => value.id, StringComparer.Ordinal))
                {
                    builder.Graph(
                        path + $".states[{state.id}]",
                        new DeletePoseStateMutation(
                            machine.id,
                            new PoseStateId(state.id)));
                }
                foreach (AgentPackagePoseState state in machine.states
                             .Where(value =>
                                  !oldStates.TryGetValue(
                                      value.id,
                                      out AgentPackagePoseState previous) ||
                                  !SameStateStructure(value, previous))
                             .OrderBy(value => value.id, StringComparer.Ordinal))
                {
                    builder.Graph(
                        path + $".states[{state.id}]",
                        new CreatePoseStateMutation(
                            machine.id,
                            ConvertState(state)));
                }
                foreach (AgentPackagePoseState state in machine.states
                             .Where(value =>
                                 oldStates.TryGetValue(
                                     value.id,
                                     out AgentPackagePoseState previous) &&
                                 SameStateStructure(value, previous) &&
                                 value.alwaysResetOnEntry.Value != previous.alwaysResetOnEntry.Value)
                             .OrderBy(value => value.id, StringComparer.Ordinal))
                {
                    builder.Graph(
                        path + $".states[{state.id}].alwaysResetOnEntry",
                        new SetPoseStateFieldMutation(
                            machine.id,
                            new PoseStateId(state.id),
                            "always-reset-on-entry",
                            state.alwaysResetOnEntry.Value));
                }
                foreach (AgentPackagePoseTransition transition in
                         machine.transitions
                             .Where(value =>
                                  !oldTransitions.TryGetValue(
                                      value.id,
                                      out AgentPackagePoseTransition previous) ||
                                  !SameTransition(value, previous))
                             .OrderBy(value => value.priority)
                             .ThenBy(value => value.id, StringComparer.Ordinal))
                {
                    builder.Graph(
                        path + $".transitions[{transition.id}]",
                        new CreatePoseTransitionMutation(
                            machine.id,
                            ConvertTransition(transition, target, report)),
                        DescribeTransition(machine, transition, "Create"));
                }
                if (!Same(old.entry, machine.entry) ||
                    !Same(old.aliases, machine.aliases) ||
                    old.maxTransitionsPerFrame !=
                    machine.maxTransitionsPerFrame)
                {
                    builder.Graph(
                        path,
                        new ConfigurePoseStateMachineMutation(
                            machine.id,
                            ConvertEntry(machine.entry),
                            machine.aliases.Select(ConvertAlias).ToArray(),
                            machine.maxTransitionsPerFrame));
                }
                foreach (AgentPackagePoseStateMachineLayoutElement element in
                         targetLayout.elements
                             .Where(value =>
                                 !oldElements.TryGetValue(
                                     value.id,
                                     out AgentPackagePoseStateMachineLayoutElement
                                         previous) ||
                                 !Mathf.Approximately(previous.x, value.x) ||
                                 !Mathf.Approximately(previous.y, value.y))
                             .OrderBy(value => value.id, StringComparer.Ordinal))
                {
                    builder.Graph(
                        StateMachineLayoutPath(machine.id) +
                        $".elements[{element.id}]",
                        new SetPoseStateMachineLayoutElementMutation(
                            machine.id,
                            element.id,
                            new Vector2(element.x, element.y)));
                }
            }
        }

        void ValidateLinkedPoseCalls(
            AgentDocumentPresentationEditable presentation,
            AgentDocumentContext context,
            AgentCompileReport report)
        {
            var groups = Index(
                presentation.profile.linkedPoseGroups,
                value => value.groupId);
            var interfaces = Index(
                context?.presentation?.linkedPoseInterfaces,
                value => ReferenceIdentity(value.asset));
            var interfaceByGroup = new Dictionary<
                string,
                CharacterLinkedPoseInterfaceAsset>(StringComparer.Ordinal);
            var contextByGroup = new Dictionary<
                string,
                AgentPackageLinkedPoseInterfaceFile>(StringComparer.Ordinal);
            foreach (AgentPackageLinkedPoseGroupBinding group in groups.Values)
            {
                string interfaceKey = ReferenceIdentity(group.interfaceAsset);
                CharacterLinkedPoseInterfaceAsset linkedInterface =
                    Resolve<CharacterLinkedPoseInterfaceAsset>(
                        group.interfaceAsset,
                        "editable/presentation/profile.json.linkedPoseGroups[" +
                        group.groupId + "]",
                        report);
                if (!linkedInterface ||
                    !interfaces.TryGetValue(
                        interfaceKey,
                        out AgentPackageLinkedPoseInterfaceFile interfaceContext) ||
                    !InterfaceContextMatches(interfaceContext, linkedInterface))
                {
                    report.Error(
                        "editable/presentation/profile.json.linkedPoseGroups[" +
                        group.groupId + "]",
                        "linked_pose_group_interface_context_mismatch",
                        "Linked Pose Group必须引用checkout readonly context中的同一Interface合同。");
                    continue;
                }
                interfaceByGroup.Add(group.groupId, linkedInterface);
                contextByGroup.Add(group.groupId, interfaceContext);
            }

            string capability = CharacterPoseNodeDefinitionModule.Shared
                .Require(CharacterPoseNodeKind.LinkedPoseCall)
                .CapabilityIdentity;
            var calls = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (AgentPackagePoseGraphFile graph in presentation.poseGraphs)
            {
                foreach (AgentPackagePoseNode node in graph.nodes.Where(value =>
                             string.Equals(
                                 value?.capability,
                                 capability,
                                 StringComparison.Ordinal)))
                {
                    string groupId = node.properties?["group-id"]?.Value<string>();
                    string entryId = node.properties?["entry-id"]?.Value<string>();
                    string key = (groupId ?? string.Empty) + "\0" +
                                 (entryId ?? string.Empty);
                    calls.TryGetValue(key, out int count);
                    calls[key] = count + 1;
                    if (!interfaceByGroup.TryGetValue(
                            groupId ?? string.Empty,
                            out CharacterLinkedPoseInterfaceAsset linkedInterface))
                    {
                        report.Error(
                            GraphPath(graph.id) + $".nodes[{node.id}]",
                            "linked_pose_call_group_missing",
                            "Linked Pose Call引用了未声明或Interface context无效的Group。");
                        continue;
                    }
                    CharacterPoseCanvasNode typed = ConvertNode(
                        node,
                        graph.role,
                        presentation,
                        report,
                        GraphPath(graph.id) + $".nodes[{node.id}]");
                    if (typed == null)
                        continue;
                    try
                    {
                        CharacterLinkedPosePortProjection.RequireCallMatch(
                            typed,
                            linkedInterface);
                    }
                    catch (Exception exception)
                    {
                        report.Error(
                            GraphPath(graph.id) + $".nodes[{node.id}]",
                            "linked_pose_call_signature_mismatch",
                            exception.Message);
                    }
                }
            }
            foreach (KeyValuePair<string, AgentPackageLinkedPoseInterfaceFile> pair in
                     contextByGroup)
            {
                foreach (AgentPackageLinkedPoseInterfaceEntry entry in
                         pair.Value.entries)
                {
                    string key = pair.Key + "\0" + entry.entryId;
                    calls.TryGetValue(key, out int count);
                    if (count != 1)
                    {
                        report.Error(
                            "editable/presentation/profile.json.linkedPoseGroups[" +
                            pair.Key + "].entries[" + entry.entryId + "]",
                            "linked_pose_call_count_invalid",
                            $"每个Group Interface Entry必须在root中恰好有一个Call，当前为{count}个。");
                    }
                }
            }
        }
    }
}
