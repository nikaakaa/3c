using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using BTSMTL.Timeline;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;
using AnimationClip = UnityEngine.AnimationClip;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentPresentationMutationPlan
    {
        internal AgentPresentationMutationPlan(
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationPoseGraphAsset poseGraph,
            string profileId,
            string poseGraphOwnerId,
            CharacterPresentationMutationTransaction graphTransaction,
            CharacterPresentationMutationTransaction profileTransaction,
            IReadOnlyList<AgentLinkedPoseGraphMutationPlan> linkedPoseGraphs)
        {
            Profile = profile;
            PoseGraph = poseGraph;
            ProfileId = profileId;
            PoseGraphOwnerId = poseGraphOwnerId;
            GraphTransaction = graphTransaction;
            ProfileTransaction = profileTransaction;
            LinkedPoseGraphs = linkedPoseGraphs ??
                               Array.Empty<AgentLinkedPoseGraphMutationPlan>();
        }

        public CharacterAnimationPresentationProfile Profile { get; }
        public CharacterPresentationPoseGraphAsset PoseGraph { get; }
        public string ProfileId { get; }
        public string PoseGraphOwnerId { get; }
        public CharacterPresentationMutationTransaction GraphTransaction { get; }
        public CharacterPresentationMutationTransaction ProfileTransaction { get; }
        public IReadOnlyList<AgentLinkedPoseGraphMutationPlan> LinkedPoseGraphs { get; }
        public bool IsEmpty =>
            GraphTransaction.Mutations.Count == 0 &&
            ProfileTransaction.Mutations.Count == 0 &&
            LinkedPoseGraphs.All(value => value.Transaction.Mutations.Count == 0);
    }

    public sealed class AgentLinkedPoseGraphMutationPlan
    {
        internal AgentLinkedPoseGraphMutationPlan(
            CharacterLinkedPoseImplementationAsset implementation,
            CharacterPresentationPoseGraphAsset graphOwner,
            string graphOwnerId,
            CharacterPresentationMutationTransaction transaction)
        {
            Implementation = implementation;
            GraphOwner = graphOwner;
            GraphOwnerId = graphOwnerId;
            Transaction = transaction;
        }

        public CharacterLinkedPoseImplementationAsset Implementation { get; }
        public CharacterPresentationPoseGraphAsset GraphOwner { get; }
        public string GraphOwnerId { get; }
        public CharacterPresentationMutationTransaction Transaction { get; }
    }

    public sealed class AgentAuthoringPresentationReconciler
    {
        Dictionary<string, AgentDocumentBlendAssetContext> m_BlendCurveCatalog;
        Dictionary<string, AgentDocumentBlendAssetContext> m_BlendProfileCatalog;
        readonly Dictionary<string, UnityEngine.Object> m_LocalAssets =
            new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);
        CharacterAnimationRigDefinition m_Rig;

        public bool TryCreatePlan(
            CharacterPipelineDefinition definition,
            AgentDocumentEditable editable,
            AgentDocumentContext context,
            AgentCompileReport report,
            out AgentPresentationMutationPlan plan)
        {
            plan = null;
            m_LocalAssets.Clear();
            AgentDocumentPresentationEditable target =
                editable?.presentation;
            if (!definition || !definition.AnimationPresentationProfile ||
                target?.profile == null)
            {
                report.Error(
                    "editable/presentation",
                    "presentation_owner_missing",
                    "Presentation Reconciler缺少Definition、Profile或目标状态。");
                return false;
            }
            ValidateCrossOwnerReferences(editable, report);
            if (report.HasErrors())
                return false;

            CharacterAnimationPresentationProfile profile =
                definition.AnimationPresentationProfile;
            string profilePath = AssetDatabase.GetAssetPath(profile);
            string profileGuid = AssetDatabase.AssetPathToGUID(profilePath);
            if (!Matches(target.profile.owner, profile, profileGuid) ||
                !string.Equals(
                    target.profile.id,
                    profileGuid,
                    StringComparison.Ordinal))
            {
                report.Error(
                    "editable/presentation/profile.json.owner",
                    "presentation_profile_owner_mismatch",
                    "Presentation Profile owner必须保持当前Definition绑定的稳定identity。");
                return false;
            }

            CharacterPresentationPoseGraphAsset poseGraph =
                Resolve<CharacterPresentationPoseGraphAsset>(
                    target.profile.poseGraph,
                    "editable/presentation/profile.json.poseGraph",
                    report);
            CharacterAnimationRigDefinition rig =
                Resolve<CharacterAnimationRigDefinition>(
                    target.profile.rig,
                    "editable/presentation/profile.json.rig",
                    report);
            if (!poseGraph || !rig)
                return false;
            InitializeBlendCatalog(context, rig, report);
            if (report.HasErrors())
                return false;

            AgentDocumentPresentationEditable current;
            AgentDocumentPresentationEditable graphCurrent;
            try
            {
                var exporter = new AgentAuthoringPresentationExporter();
                current = exporter.Export(definition);
                graphCurrent = poseGraph == profile.PoseGraph
                    ? current
                    : exporter.ExportPoseGraph(poseGraph);
            }
            catch (Exception exception)
            {
                report.Error(
                    "editable/presentation",
                    "presentation_current_export_failed",
                    exception.Message);
                return false;
            }

            var identities = new IdentityMap(
                AssetDatabase.AssetPathToGUID(
                    AssetDatabase.GetAssetPath(definition)),
                CurrentIdentities(current).Concat(
                    CurrentIdentities(graphCurrent)));
            AgentDocumentPresentationEditable normalized =
                AgentAuthoringDocumentCodec.Clone(target);
            try
            {
                Normalize(normalized, identities, report);
            }
            catch (Exception exception)
            {
                report.Error(
                    "editable/presentation",
                    "presentation_identity_planning_failed",
                    exception.Message);
            }
            if (report.HasErrors())
                return false;
            ValidateLinkedPoseCalls(normalized, context, report);
            if (report.HasErrors())
                return false;

            string poseGraphPath = AssetDatabase.GetAssetPath(poseGraph);
            string poseGraphGuid =
                AssetDatabase.AssetPathToGUID(poseGraphPath);
            var graphTransaction = new CharacterPresentationMutationTransaction(
                "document-presentation-graph",
                "Apply Presentation Graph Document");
            var profileTransaction =
                new CharacterPresentationMutationTransaction(
                    "document-presentation-profile",
                    "Apply Presentation Profile Document");
            var builder = new PlanBuilder(
                graphTransaction,
                profileTransaction,
                report);

            PreparePoseSourceSlots(
                current.profile,
                normalized.profile,
                poseGraph,
                poseGraphGuid,
                builder,
                report);
            if (report.HasErrors())
                return false;

            BuildGraphPlan(
                graphCurrent,
                normalized,
                poseGraph,
                poseGraphGuid,
                builder,
                report);
            BuildStateMachinePlan(
                graphCurrent,
                normalized,
                builder,
                report);
            BuildLinkedPosePlan(
                current,
                normalized,
                context,
                profile,
                profileGuid,
                builder,
                report,
                out IReadOnlyList<AgentLinkedPoseGraphMutationPlan>
                    linkedPoseGraphs);
            BuildProfilePlan(
                current.profile,
                normalized.profile,
                profile,
                poseGraph,
                rig,
                builder,
                report);
            if (report.HasErrors())
                return false;

            plan = new AgentPresentationMutationPlan(
                profile,
                poseGraph,
                profileGuid,
                poseGraphGuid,
                graphTransaction,
                profileTransaction,
                linkedPoseGraphs);
            return true;
        }

        static void ValidateCrossOwnerReferences(
            AgentDocumentEditable editable,
            AgentCompileReport report)
        {
            AgentDocumentPresentationEditable presentation =
                editable.presentation;
            var timelines = (editable.timelines ??
                             new List<AgentSnapshotTimeline>())
                .Where(value =>
                    value != null &&
                    !string.IsNullOrWhiteSpace(value.timelineAuthoringId))
                .ToDictionary(
                    value => value.timelineAuthoringId,
                    StringComparer.Ordinal);
            var channels = new HashSet<string>(
                timelines.Values
                    .SelectMany(value =>
                        value.tracks ??
                        new List<AgentSnapshotTimelineTrack>())
                    .Where(value =>
                        value != null &&
                        !string.IsNullOrWhiteSpace(
                            value.animationChannelId))
                    .Select(value => value.animationChannelId),
                StringComparer.Ordinal);
            var sources = new HashSet<string>(
                presentation.profile.poseSources.Select(value =>
                    ReferenceIdentity(value.slot)),
                StringComparer.Ordinal);
            var graphs = new HashSet<string>(
                presentation.poseGraphs.Select(value => value.id),
                StringComparer.Ordinal);
            var sourceCapabilities = new HashSet<string>(
                CharacterPoseCompilerHandlerRegistry.Shared.All
                    .Where(value =>
                        value.Player &&
                        !value.AnimationSlot)
                    .Select(value =>
                        value.CapabilityIdentity),
                StringComparer.Ordinal);
            var channelCapabilities = new HashSet<string>(
                CharacterPoseCompilerHandlerRegistry.Shared.All
                    .Where(value =>
                        value.ActionPlaybackControl)
                    .Select(value =>
                        value.CapabilityIdentity),
                StringComparer.Ordinal);
            var subgraphCapabilities = new HashSet<string>(
                CharacterPoseCompilerHandlerRegistry.Shared.All
                    .Where(value =>
                        value.NativeRole ==
                        CharacterPoseNativeNodeRole.Subgraph)
                    .Select(value =>
                        value.CapabilityIdentity),
                StringComparer.Ordinal);
            foreach (AgentPackagePoseGraphFile graph in
                     presentation.poseGraphs)
            {
                foreach (AgentPackagePoseNode node in graph.nodes)
                {
                    string path =
                        GraphPath(graph.id) + $".nodes[{node.id}]";
                    if (sourceCapabilities.Contains(node.capability) &&
                        !sources.Contains(
                            ReferenceIdentity(
                                node.properties["pose-source-slot"]
                                    ?.ToObject<AgentPackageAssetReferenceV3>())))
                    {
                        report.Error(
                            path + ".properties.pose-source-slot",
                            "presentation_pose_source_unresolved",
                            "Pose节点引用的Source Slot不在Profile目标状态中。");
                    }
                    if (channelCapabilities.Contains(
                            node.capability) &&
                        !channels.Contains(
                            node.properties["animation-channel-id"]
                                ?.Value<string>() ??
                            string.Empty))
                    {
                        report.Error(
                            path + ".properties.animation-channel-id",
                            "presentation_animation_channel_unresolved",
                            "Pose节点引用的Animation Channel不在Timeline目标状态中。");
                    }
                    if (subgraphCapabilities.Contains(
                            node.capability) &&
                        !graphs.Contains(
                            node.properties["graph-id"]?.Value<string>() ??
                            string.Empty))
                    {
                        report.Error(
                            path + ".properties.graph-id",
                            "presentation_subgraph_unresolved",
                            "Pose Subgraph引用不在root-owned Graph catalog中。");
                    }
                }
            }
            foreach (AgentPackageAnimationProducerBinding producer in
                     presentation.profile.actionProducers)
            {
                string path =
                    "editable/presentation/profile.json.actionProducers[" +
                    ProducerKey(producer) + "]";
                if (producer.timelineId.StartsWith(
                        "local:",
                        StringComparison.Ordinal) ||
                    producer.trackId.StartsWith(
                        "local:",
                        StringComparison.Ordinal))
                {
                    report.Error(
                        path,
                        "presentation_action_producer_local_reference_invalid",
                        "Action producer只允许绑定已存在的Timeline与Animation track；当前正式Timeline Mutation不创建这两类owner。");
                }
                else if (!timelines.TryGetValue(
                        producer.timelineId,
                        out AgentSnapshotTimeline timeline) ||
                    !(timeline.tracks ??
                      new List<AgentSnapshotTimelineTrack>()).Any(value =>
                        value != null &&
                        string.Equals(
                            value.trackAuthoringId,
                            producer.trackId,
                            StringComparison.Ordinal)))
                {
                    report.Error(
                        path,
                        "presentation_action_producer_unresolved",
                        "Action producer必须引用Timeline目标状态中的现有track。");
                }
            }
        }

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
                    CharacterPresentationPoseSourceSlot slot = kind switch
                    {
                        PresentationPoseSourceKind.Sequence =>
                            ScriptableObject.CreateInstance<CharacterSequencePoseSourceSlot>(),
                        PresentationPoseSourceKind.BlendSpace =>
                            ScriptableObject.CreateInstance<CharacterBlendSpacePoseSourceSlot>(),
                        PresentationPoseSourceKind.MotionMatching =>
                            ScriptableObject.CreateInstance<CharacterMotionMatchingPoseSourceSlot>(),
                        _ => throw new InvalidOperationException($"Unsupported Pose source kind '{kind}'.")
                    };
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
                    CharacterTypedPoseGraph created = ConvertGraph(
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
                    CharacterTypedPoseNode created = ConvertNode(
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
                CharacterTypedPoseNode decodedTarget = null;
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
                            ConvertTransition(transition)),
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

            string capability = CharacterPoseGraphAuthoringCapabilities
                .Get(CharacterPoseNodeKind.LinkedPoseCall).Value;
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
                    CharacterTypedPoseNode typed = ConvertNode(
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

        void BuildLinkedPosePlan(
            AgentDocumentPresentationEditable current,
            AgentDocumentPresentationEditable target,
            AgentDocumentContext context,
            CharacterAnimationPresentationProfile profile,
            string profileId,
            PlanBuilder builder,
            AgentCompileReport report,
            out IReadOnlyList<AgentLinkedPoseGraphMutationPlan> graphPlans)
        {
            var plans = new List<AgentLinkedPoseGraphMutationPlan>();
            graphPlans = plans;
            var currentImplementations = Index(
                current.linkedPoseImplementations,
                value => value.id);
            var targetImplementations = Index(
                target.linkedPoseImplementations,
                value => value.id);
            var contextInterfaces = Index(
                context?.presentation?.linkedPoseInterfaces,
                value => ReferenceIdentity(value.asset));

            foreach (AgentPackageLinkedPoseImplementationFile value in
                     target.linkedPoseImplementations
                         .OrderBy(item => item.implementationId, StringComparer.Ordinal))
            {
                string path =
                    "editable/presentation/linked-pose-implementations/" +
                    AgentAuthoringPackageMapper.Segment(value.id) +
                    "/implementation.json";
                CharacterLinkedPoseInterfaceAsset linkedInterface =
                    Resolve<CharacterLinkedPoseInterfaceAsset>(
                        value.interfaceAsset,
                        path + ".interfaceAsset",
                        report);
                string interfaceKey = ReferenceIdentity(value.interfaceAsset);
                if (!linkedInterface ||
                    !contextInterfaces.TryGetValue(
                        interfaceKey,
                        out AgentPackageLinkedPoseInterfaceFile interfaceContext) ||
                    !InterfaceContextMatches(interfaceContext, linkedInterface))
                {
                    report.Error(
                        path + ".interfaceAsset",
                        "linked_pose_interface_context_mismatch",
                        "Implementation必须引用checkout readonly context中的同一Interface revision与signature。");
                    continue;
                }
                if (!ValidateLinkedTargetEntries(
                        value,
                        linkedInterface,
                        interfaceContext,
                        path,
                        report))
                    continue;

                bool created = !string.IsNullOrWhiteSpace(value.asset?.localId);
                CharacterLinkedPoseImplementationAsset implementation;
                CharacterPresentationPoseGraphAsset graphOwner;
                AgentPackageLinkedPoseImplementationFile previous = null;
                if (created)
                {
                    if (!string.Equals(
                            value.id,
                            value.asset.localId,
                            StringComparison.Ordinal) ||
                        string.IsNullOrWhiteSpace(value.graphOwner?.localId))
                    {
                        report.Error(
                            path,
                            "linked_pose_local_identity_invalid",
                            "新增Implementation的object key、asset localId与Graph owner localId不完整。");
                        continue;
                    }
                    implementation = ScriptableObject.CreateInstance<
                        CharacterLinkedPoseImplementationAsset>();
                    implementation.name = value.name;
                    graphOwner = ScriptableObject.CreateInstance<
                        CharacterPresentationPoseGraphAsset>();
                    graphOwner.name = value.name + " Graphs";
                    if (!m_LocalAssets.TryAdd(value.asset.localId, implementation) ||
                        !m_LocalAssets.TryAdd(value.graphOwner.localId, graphOwner))
                    {
                        report.Error(
                            path,
                            "linked_pose_local_identity_duplicate",
                            "Implementation或Graph owner local identity重复。");
                        UnityEngine.Object.DestroyImmediate(implementation);
                        UnityEngine.Object.DestroyImmediate(graphOwner);
                        continue;
                    }
                    builder.Profile(
                        path,
                        new CreateLinkedPoseImplementationMutation(
                            profileId,
                            implementation,
                            graphOwner));
                }
                else
                {
                    implementation = Resolve<CharacterLinkedPoseImplementationAsset>(
                        value.asset,
                        path + ".asset",
                        report);
                    graphOwner = Resolve<CharacterPresentationPoseGraphAsset>(
                        value.graphOwner,
                        path + ".graphOwner",
                        report);
                    if (!implementation || !graphOwner ||
                        !profile.LinkedPoseImplementations.Contains(implementation) ||
                        !currentImplementations.TryGetValue(value.id, out previous) ||
                        !Same(previous.graphOwner, value.graphOwner) ||
                        !string.Equals(
                            previous.ownerIdentity,
                            value.ownerIdentity,
                            StringComparison.Ordinal) ||
                        !string.Equals(
                            previous.graphOwnerIdentity,
                            value.graphOwnerIdentity,
                            StringComparison.Ordinal) ||
                        !string.Equals(
                            previous.implementationId,
                            value.implementationId,
                            StringComparison.Ordinal))
                    {
                        report.Error(
                            path,
                            "linked_pose_implementation_owner_mismatch",
                            "既有Implementation必须保持Profile成员身份与同一Graph owner对象。");
                        continue;
                    }
                }

                if (!created &&
                    (value.revision < previous.revision ||
                     !SameImplementationSemantic(previous, value) &&
                     value.revision <= previous.revision))
                {
                    report.Error(
                        path + ".revision",
                        "linked_pose_implementation_revision_stale",
                        "Implementation语义变化必须显式提高revision，revision不得回退。");
                    continue;
                }

                var graphTransaction =
                    new CharacterPresentationMutationTransaction(
                        "document-linked-pose-graph-" +
                        AgentAuthoringPackageMapper.Segment(value.id),
                        "Apply Linked Pose Entry Graph Document");
                PlanBuilder graphBuilder = builder.ForGraph(graphTransaction);
                AgentDocumentPresentationEditable previousClosure =
                    LinkedClosure(previous);
                AgentDocumentPresentationEditable targetClosure =
                    LinkedClosure(value);
                BuildGraphPlan(
                    previousClosure,
                    targetClosure,
                    graphOwner,
                    value.graphOwnerIdentity,
                    graphBuilder,
                    report,
                    false);
                BuildStateMachinePlan(
                    previousClosure,
                    targetClosure,
                    graphBuilder,
                    report);

                CharacterLinkedPoseImplementationEntryMutationValue[] entries =
                    value.entries.Select(entry =>
                        new CharacterLinkedPoseImplementationEntryMutationValue(
                            new LinkedPoseEntryId(entry.entryId),
                            value.graphOwnerIdentity,
                            graphOwner,
                            new PoseGraphId(entry.graphId)))
                    .OrderBy(entry => entry.EntryId)
                    .ToArray();
                if (created || !SameImplementationHeader(previous, value))
                {
                    builder.Profile(
                        path,
                        new ConfigureLinkedPoseImplementationMutation(
                            profileId,
                            implementation,
                            value.ownerIdentity,
                            value.name,
                            new LinkedPoseImplementationId(
                                value.implementationId),
                            new LinkedPoseRevision(value.revision),
                            linkedInterface,
                            entries));
                }
                plans.Add(new AgentLinkedPoseGraphMutationPlan(
                    implementation,
                    graphOwner,
                    value.graphOwnerIdentity,
                    graphTransaction));
            }

            BuildLinkedPoseGroups(
                current.profile,
                target.profile,
                profileId,
                builder,
                report);
            BuildLinkedPoseSelectors(
                current.profile,
                target.profile,
                profile,
                profileId,
                builder,
                report);

            foreach (AgentPackageLinkedPoseImplementationFile removed in
                     current.linkedPoseImplementations
                         .Where(value => !targetImplementations.ContainsKey(value.id))
                         .OrderBy(value => value.implementationId, StringComparer.Ordinal))
            {
                CharacterLinkedPoseImplementationAsset implementation =
                    Resolve<CharacterLinkedPoseImplementationAsset>(
                        removed.asset,
                        "editable/presentation/linked-pose-implementations/" +
                        AgentAuthoringPackageMapper.Segment(removed.id),
                        report);
                if (implementation)
                {
                    builder.Profile(
                        "editable/presentation/linked-pose-implementations/" +
                        AgentAuthoringPackageMapper.Segment(removed.id),
                        new RemoveLinkedPoseImplementationMutation(
                            profileId,
                            implementation));
                }
            }
        }

        void BuildLinkedPoseGroups(
            AgentPackagePresentationProfileFile current,
            AgentPackagePresentationProfileFile target,
            string profileId,
            PlanBuilder builder,
            AgentCompileReport report)
        {
            var oldGroups = Index(current.linkedPoseGroups, value => value.groupId);
            var newGroups = Index(target.linkedPoseGroups, value => value.groupId);
            foreach (AgentPackageLinkedPoseGroupBinding value in
                     target.linkedPoseGroups.OrderBy(
                         item => item.groupId,
                         StringComparer.Ordinal))
            {
                if (oldGroups.TryGetValue(value.groupId, out AgentPackageLinkedPoseGroupBinding previous) &&
                    Same(previous, value))
                    continue;
                CharacterLinkedPoseInterfaceAsset linkedInterface =
                    Resolve<CharacterLinkedPoseInterfaceAsset>(
                        value.interfaceAsset,
                        "editable/presentation/profile.json.linkedPoseGroups[" +
                        value.groupId + "]",
                        report);
                if (linkedInterface)
                {
                    builder.Profile(
                        "editable/presentation/profile.json.linkedPoseGroups[" +
                        value.groupId + "]",
                        new SetLinkedPoseGroupMutation(
                            profileId,
                            new CharacterLinkedPoseGroupBinding(
                                new LinkedPoseGroupId(value.groupId),
                                linkedInterface)));
                }
            }
            foreach (AgentPackageLinkedPoseGroupBinding removed in
                     current.linkedPoseGroups.Where(value =>
                         !newGroups.ContainsKey(value.groupId)))
            {
                builder.Profile(
                    "editable/presentation/profile.json.linkedPoseGroups[" +
                    removed.groupId + "]",
                    new RemoveLinkedPoseGroupMutation(
                        profileId,
                        new LinkedPoseGroupId(removed.groupId)));
            }
        }

        void BuildLinkedPoseSelectors(
            AgentPackagePresentationProfileFile current,
            AgentPackagePresentationProfileFile target,
            CharacterAnimationPresentationProfile profile,
            string profileId,
            PlanBuilder builder,
            AgentCompileReport report)
        {
            var oldSelectors = Index(current.linkedPoseSelectors, value => value.id);
            var newSelectors = Index(target.linkedPoseSelectors, value => value.id);
            foreach (AgentPackageLinkedPoseSelectorBinding value in
                     target.linkedPoseSelectors.OrderBy(
                         item => item.selectorId,
                         StringComparer.Ordinal))
            {
                string path =
                    "editable/presentation/profile.json.linkedPoseSelectors[" +
                    value.selectorId + "]";
                bool created = !string.IsNullOrWhiteSpace(value.asset?.localId);
                CharacterEquipmentLinkedPoseSelectionBinding selector;
                AgentPackageLinkedPoseSelectorBinding previous = null;
                if (created)
                {
                    if (!string.Equals(value.id, value.asset.localId, StringComparison.Ordinal))
                    {
                        report.Error(
                            path,
                            "linked_pose_selector_local_identity_invalid",
                            "新增selector的object key必须等于asset localId。");
                        continue;
                    }
                    selector = ScriptableObject.CreateInstance<
                        CharacterEquipmentLinkedPoseSelectionBinding>();
                    selector.name = value.selectorId;
                    if (!m_LocalAssets.TryAdd(value.asset.localId, selector))
                    {
                        report.Error(
                            path,
                            "linked_pose_selector_local_identity_duplicate",
                            "selector local identity重复。");
                        UnityEngine.Object.DestroyImmediate(selector);
                        continue;
                    }
                    builder.Profile(
                        path,
                        new CreateEquipmentLinkedPoseSelectorMutation(
                            profileId,
                            selector));
                }
                else
                {
                    selector = Resolve<CharacterEquipmentLinkedPoseSelectionBinding>(
                        value.asset,
                        path + ".asset",
                        report);
                    if (!selector || !profile.LinkedPoseSelectors.Contains(selector) ||
                        !oldSelectors.TryGetValue(value.id, out previous))
                    {
                        report.Error(
                            path,
                            "linked_pose_selector_owner_mismatch",
                            "既有selector必须保持Profile成员身份与稳定对象identity。");
                        continue;
                    }
                }

                CharacterEquipmentLinkedPoseMapping[] mappings =
                    (value.equipment?.mappings ??
                     new List<AgentPackageEquipmentLinkedPoseMapping>())
                    .Select(mapping => new CharacterEquipmentLinkedPoseMapping(
                        new EquipmentId(mapping.equipmentId),
                        new LinkedPoseImplementationId(mapping.implementationId)))
                    .OrderBy(mapping => mapping.EquipmentId)
                    .ToArray();
                bool envelopeChanged = created || previous == null ||
                    !string.Equals(previous.selectorId, value.selectorId, StringComparison.Ordinal) ||
                    !string.Equals(previous.groupId, value.groupId, StringComparison.Ordinal) ||
                    !string.Equals(previous.kind, value.kind, StringComparison.Ordinal) ||
                    !string.Equals(previous.equipment?.slotId, value.equipment?.slotId, StringComparison.Ordinal) ||
                    !string.Equals(
                        previous.equipment?.emptyImplementationId,
                        value.equipment?.emptyImplementationId,
                        StringComparison.Ordinal);
                if (envelopeChanged)
                {
                    builder.Profile(
                        path,
                        new ConfigureEquipmentLinkedPoseSelectorMutation(
                            profileId,
                            selector,
                            new LinkedPoseSelectorId(value.selectorId),
                            new LinkedPoseGroupId(value.groupId),
                            new EquipmentSlotId(value.equipment.slotId),
                            new LinkedPoseImplementationId(
                                value.equipment.emptyImplementationId),
                            mappings));
                    continue;
                }

                var oldMappings = Index(
                    previous.equipment.mappings,
                    mapping => mapping.equipmentId);
                var newMappings = Index(
                    value.equipment.mappings,
                    mapping => mapping.equipmentId);
                foreach (AgentPackageEquipmentLinkedPoseMapping mapping in
                         previous.equipment.mappings.Where(item =>
                             !newMappings.ContainsKey(item.equipmentId)))
                {
                    builder.Profile(
                        path + ".equipment.mappings[" + mapping.equipmentId + "]",
                        new RemoveEquipmentLinkedPoseMappingMutation(
                            profileId,
                            selector,
                            new EquipmentId(mapping.equipmentId)));
                }
                foreach (AgentPackageEquipmentLinkedPoseMapping mapping in
                         value.equipment.mappings.Where(item =>
                             !oldMappings.TryGetValue(
                                 item.equipmentId,
                                 out AgentPackageEquipmentLinkedPoseMapping old) ||
                             !Same(old, item)))
                {
                    builder.Profile(
                        path + ".equipment.mappings[" + mapping.equipmentId + "]",
                        new SetEquipmentLinkedPoseMappingMutation(
                            profileId,
                            selector,
                            new CharacterEquipmentLinkedPoseMapping(
                                new EquipmentId(mapping.equipmentId),
                                new LinkedPoseImplementationId(
                                    mapping.implementationId))));
                }
            }
            foreach (AgentPackageLinkedPoseSelectorBinding removed in
                     current.linkedPoseSelectors.Where(value =>
                         !newSelectors.ContainsKey(value.id)))
            {
                CharacterLinkedPoseSelectorBindingAsset selector =
                    Resolve<CharacterLinkedPoseSelectorBindingAsset>(
                        removed.asset,
                        "editable/presentation/profile.json.linkedPoseSelectors[" +
                        removed.selectorId + "]",
                        report);
                if (selector)
                {
                    builder.Profile(
                        "editable/presentation/profile.json.linkedPoseSelectors[" +
                        removed.selectorId + "]",
                        new RemoveLinkedPoseSelectorMutation(
                            profileId,
                            selector));
                }
            }
        }

        static AgentDocumentPresentationEditable LinkedClosure(
            AgentPackageLinkedPoseImplementationFile value) =>
            new AgentDocumentPresentationEditable
            {
                profile = new AgentPackagePresentationProfileFile(),
                poseGraphs = value?.poseGraphs ??
                             new List<AgentPackagePoseGraphFile>(),
                poseGraphLayouts = value?.poseGraphLayouts ??
                                   new List<AgentPackagePoseGraphLayoutFile>(),
                poseStateMachines = value?.poseStateMachines ??
                                    new List<AgentPackagePoseStateMachineFile>(),
                poseStateMachineLayouts = value?.poseStateMachineLayouts ??
                    new List<AgentPackagePoseStateMachineLayoutFile>()
            };

        static bool SameImplementationHeader(
            AgentPackageLinkedPoseImplementationFile left,
            AgentPackageLinkedPoseImplementationFile right) =>
            left != null && right != null &&
            string.Equals(left.name, right.name, StringComparison.Ordinal) &&
            string.Equals(left.ownerIdentity, right.ownerIdentity, StringComparison.Ordinal) &&
            string.Equals(
                left.implementationId,
                right.implementationId,
                StringComparison.Ordinal) &&
            left.revision == right.revision &&
            Same(left.interfaceAsset, right.interfaceAsset) &&
            Same(left.graphOwner, right.graphOwner) &&
            string.Equals(
                left.graphOwnerIdentity,
                right.graphOwnerIdentity,
                StringComparison.Ordinal) &&
            Same(left.entries, right.entries);

        static bool SameImplementationSemantic(
            AgentPackageLinkedPoseImplementationFile left,
            AgentPackageLinkedPoseImplementationFile right) =>
            left != null && right != null &&
            string.Equals(
                left.ownerIdentity,
                right.ownerIdentity,
                StringComparison.Ordinal) &&
            string.Equals(
                left.implementationId,
                right.implementationId,
                StringComparison.Ordinal) &&
            Same(left.interfaceAsset, right.interfaceAsset) &&
            Same(left.graphOwner, right.graphOwner) &&
            string.Equals(
                left.graphOwnerIdentity,
                right.graphOwnerIdentity,
                StringComparison.Ordinal) &&
            Same(left.entries, right.entries) &&
            Same(left.poseGraphs, right.poseGraphs) &&
            Same(left.poseStateMachines, right.poseStateMachines);

        static bool InterfaceContextMatches(
            AgentPackageLinkedPoseInterfaceFile context,
            CharacterLinkedPoseInterfaceAsset asset) =>
            context != null && asset &&
            string.Equals(
                context.interfaceId,
                asset.InterfaceId.Value,
                StringComparison.Ordinal) &&
            context.revision == asset.Revision.Value &&
            string.Equals(
                context.signatureHash,
                asset.SignatureHash.ToString(),
                StringComparison.Ordinal) &&
            string.Equals(
                context.factContractIdentity,
                asset.FactContractIdentity.ToString(),
                StringComparison.Ordinal) &&
            string.Equals(
                context.executionContract,
                asset.ExecutionContract,
                StringComparison.Ordinal);

        bool ValidateLinkedTargetEntries(
            AgentPackageLinkedPoseImplementationFile implementation,
            CharacterLinkedPoseInterfaceAsset linkedInterface,
            AgentPackageLinkedPoseInterfaceFile interfaceContext,
            string path,
            AgentCompileReport report)
        {
            HashSet<string> expected = interfaceContext.entries
                .Select(value => value.entryId)
                .ToHashSet(StringComparer.Ordinal);
            HashSet<string> actual = implementation.entries
                .Select(value => value.entryId)
                .ToHashSet(StringComparer.Ordinal);
            if (!expected.SetEquals(actual))
            {
                report.Error(
                    path + ".entries",
                    "linked_pose_entry_coverage_mismatch",
                    "Implementation Entry映射必须精确覆盖Interface全部Entry。");
                return false;
            }
            var graphs = Index(implementation.poseGraphs, value => value.id);
            var layouts = Index(
                implementation.poseGraphLayouts,
                value => value.graphId);
            AgentDocumentPresentationEditable closure =
                LinkedClosure(implementation);
            bool valid = true;
            foreach (AgentPackageLinkedPoseImplementationEntry entry in
                     implementation.entries)
            {
                if (!graphs.TryGetValue(
                        entry.graphId,
                        out AgentPackagePoseGraphFile graph) ||
                    !layouts.TryGetValue(
                        entry.graphId,
                        out AgentPackagePoseGraphLayoutFile layout))
                {
                    report.Error(
                        path + $".entries[{entry.entryId}]",
                        "linked_pose_entry_graph_missing",
                        "Implementation Entry缺少同owner下的Graph或layout。");
                    valid = false;
                    continue;
                }
                CharacterTypedPoseGraph typed = ConvertGraph(
                    graph,
                    layout,
                    closure,
                    report,
                    path + $".entries[{entry.entryId}]");
                if (typed == null)
                {
                    valid = false;
                    continue;
                }
                try
                {
                    CharacterLinkedPosePortProjection.RequireEntryGraphMatch(
                        typed,
                        linkedInterface,
                        new LinkedPoseEntryId(entry.entryId));
                }
                catch (Exception exception)
                {
                    report.Error(
                        path + $".entries[{entry.entryId}]",
                        "linked_pose_entry_signature_mismatch",
                        exception.Message);
                    valid = false;
                }
            }
            return valid;
        }

        void BuildProfilePlan(
            AgentPackagePresentationProfileFile current,
            AgentPackagePresentationProfileFile target,
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationPoseGraphAsset poseGraph,
            CharacterAnimationRigDefinition rig,
            PlanBuilder builder,
            AgentCompileReport report)
        {
            string path = "editable/presentation/profile.json";
            if (!Same(current.poseGraph, target.poseGraph) ||
                !Same(current.rig, target.rig))
            {
                builder.Profile(
                    path,
                    new SetPresentationGraphMutation(
                        target.id,
                        poseGraph,
                        rig));
            }

            CharacterMotionMatchingProfile motionMatching =
                ResolveOptional<CharacterMotionMatchingProfile>(
                    target.policy.motionMatchingProfile,
                    path + ".policy.motionMatchingProfile",
                    report);
            if (!Same(
                    current.policy.motionMatchingProfile,
                    target.policy.motionMatchingProfile))
            {
                builder.Profile(
                    path + ".policy.motionMatchingProfile",
                    new SetMotionMatchingProfileMutation(
                        target.id,
                        motionMatching));
            }
            if (!string.Equals(
                    current.policy.footPlacementAnalysisMode,
                    target.policy.footPlacementAnalysisMode,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    current.policy.footPlacementAnalysisSourceAssetGuid,
                    target.policy.footPlacementAnalysisSourceAssetGuid,
                    StringComparison.Ordinal))
            {
                builder.Profile(
                    path + ".policy.footPlacement",
                    new SetFootPlacementAnalysisMutation(
                        target.id,
                        Enum.Parse<CharacterFootPlacementAnalysisMode>(
                            target.policy.footPlacementAnalysisMode,
                            false),
                        target.policy.footPlacementAnalysisSourceAssetGuid));
            }

            Dictionary<string, AgentPackagePoseSourceBinding> oldSources =
                Index(current.poseSources, value =>
                    ReferenceIdentity(value.binding));
            Dictionary<string, AgentPackagePoseSourceBinding> newSources =
                Index(target.poseSources, value =>
                    ReferenceIdentity(value.binding));
            foreach (AgentPackagePoseSourceBinding removed in
                     current.poseSources.Where(value =>
                         !newSources.ContainsKey(
                             ReferenceIdentity(value.binding))))
            {
                CharacterPresentationPoseSourceBinding binding =
                    Resolve<CharacterPresentationPoseSourceBinding>(
                        removed.binding,
                        path + $".poseSources[{removed.name}].binding",
                        report);
                if (!binding)
                {
                    report.Error(
                        path + $".poseSources[{removed.name}]",
                        "presentation_pose_source_binding_missing",
                        "待删除Pose source不能解析到Profile-owned typed binding子资产。");
                    continue;
                }
                builder.Profile(
                    path + $".poseSources[{removed.name}]",
                    new RemoveProfileSourceBindingMutation(
                        target.id,
                        binding));
            }
            foreach (AgentPackagePoseSourceBinding source in target.poseSources
                         .Where(value =>
                             !oldSources.TryGetValue(
                                 ReferenceIdentity(value.binding),
                                 out AgentPackagePoseSourceBinding previous) ||
                             !Same(previous, value)))
            {
                CharacterPresentationPoseSourceBinding binding =
                    ConvertSource(source, poseGraph, rig, report);
                if (binding)
                {
                    bool replace = oldSources.ContainsKey(
                        ReferenceIdentity(source.binding));
                    builder.Profile(
                        path + $".poseSources[{source.name}]",
                        replace
                            ? new SetProfileSourceBindingMutation(
                                target.id,
                                binding)
                            : new CreateProfileSourceBindingMutation(
                                target.id,
                                binding));
                }
            }

            Dictionary<string, AgentPackageAnimationProducerBinding>
                oldProducers = Index(
                    current.actionProducers,
                    ProducerKey);
            Dictionary<string, AgentPackageAnimationProducerBinding>
                newProducers = Index(
                    target.actionProducers,
                    ProducerKey);
            foreach (AgentPackageAnimationProducerBinding removed in
                     current.actionProducers.Where(value =>
                         !newProducers.ContainsKey(ProducerKey(value))))
            {
                builder.Profile(
                    path + $".actionProducers[{ProducerKey(removed)}]",
                    new RemoveProfileProducerBindingMutation(
                        target.id,
                        new AnimationProducerId(
                            removed.timelineId,
                            removed.trackId)));
            }
            foreach (AgentPackageAnimationProducerBinding producer in
                     target.actionProducers.Where(value =>
                         !oldProducers.TryGetValue(
                             ProducerKey(value),
                             out AgentPackageAnimationProducerBinding previous) ||
                         !Same(previous, value)))
            {
                AnimationProducerPresentationBinding binding =
                    ConvertProducer(producer, report);
                if (binding != null)
                {
                    builder.Profile(
                        path + $".actionProducers[{ProducerKey(producer)}]",
                        new SetProfileProducerBindingMutation(
                            target.id,
                            binding));
                }
            }
        }

        CharacterTypedPoseGraph ConvertGraph(
            AgentPackagePoseGraphFile graph,
            AgentPackagePoseGraphLayoutFile layout,
            AgentDocumentPresentationEditable presentation,
            AgentCompileReport report,
            string path)
        {
            var nodes = new List<CharacterTypedPoseNode>();
            foreach (AgentPackagePoseNode node in graph.nodes)
            {
                CharacterTypedPoseNode converted = ConvertNode(
                    node,
                    graph.role,
                    presentation,
                    report,
                    path + $".nodes[{node.id}]");
                if (converted != null)
                    nodes.Add(converted);
            }
            if (report.HasErrors())
                return null;
            Dictionary<string, AgentPackagePoseNodeLayout> positions =
                Index(layout.nodes, value => value.id);
            return new CharacterTypedPoseGraph(
                new PoseGraphId(graph.id),
                graph.contentRevision,
                graph.parameters.Select(ConvertParameter).ToArray(),
                nodes.ToArray(),
                graph.edges.Select(value => new CharacterPoseEdge(
                    value.id,
                    new PoseNodeId(value.from.node),
                    new PosePortId(value.from.port),
                    new PoseNodeId(value.to.node),
                    new PosePortId(value.to.port))).ToArray(),
                graph.nodes.Select(value =>
                {
                    AgentPackagePoseNodeLayout position = positions[value.id];
                    return new CharacterPoseGraphLayoutEntry(
                        new PoseNodeId(value.id),
                        new Vector2(position.x, position.y));
                }).ToArray());
        }

        CharacterTypedPoseNode ConvertNode(
            AgentPackagePoseNode node,
            string role,
            AgentDocumentPresentationEditable presentation,
            AgentCompileReport report,
            string path)
        {
            try
            {
                CharacterPoseNodeKind kind = ResolveKind(
                    node.capability,
                    new GraphAuthoringDocumentRoleId(role));
                CharacterPoseNodePayload payload =
                    CharacterPoseAuthoringPayloadCodec.Create(
                        kind,
                        new CharacterPoseAuthoringPayloadInput(
                            (field, expectedType) => ConvertProperty(
                                node.capability,
                                role,
                                field,
                                node.properties[field],
                                expectedType,
                                report,
                                path + ".properties." + field),
                            CharacterPoseCompilerHandlerRegistry
                                .Shared.Require(kind).StateMachine
                                ? new Func<CharacterPoseStateMachineDefinition>(() =>
                                    ConvertStateMachine(
                                        RequireStateMachine(
                                            node,
                                            presentation)))
                                : null));
                return new CharacterTypedPoseNode(
                    new PoseNodeId(node.id),
                    node.name,
                    payload,
                    node.dynamicPorts.Select(ConvertPort).ToArray());
            }
            catch (Exception exception)
            {
                report.Error(
                    path,
                    "presentation_pose_node_lower_failed",
                    exception.Message);
                return null;
            }
        }

        static CharacterPoseParameterDeclaration ConvertParameter(
            AgentPackagePoseParameter value) =>
            new CharacterPoseParameterDeclaration(
                new PoseParameterId(value.id),
                Enum.Parse<PoseParameterValueType>(value.valueType, false),
                value.defaultValue,
                value.unit);

        static CharacterPoseDynamicPort ConvertPort(
            AgentPackagePoseDynamicPort value) =>
            new CharacterPoseDynamicPort(
                new PosePortId(value.id),
                value.name,
                CharacterPoseAuthoringPortProjection.Kind(
                    value.valueType),
                Enum.Parse<CharacterPosePortDirection>(
                    value.direction,
                    false),
                value.required,
                value.order,
                string.IsNullOrWhiteSpace(value.interfacePortId)
                    ? default
                    : new PoseInterfacePortId(value.interfacePortId));

        static CharacterPoseStateDefinition ConvertState(
            AgentPackagePoseState value) =>
            new CharacterPoseStateDefinition(
                new PoseStateId(value.id),
                value.name,
                new PoseGraphId(value.poseGraphId),
                new PoseNodeId(value.outputPoseNodeId),
                value.alwaysResetOnEntry.Value);

        static CharacterPoseStateEntry ConvertEntry(
            AgentPackagePoseStateEntry value) =>
            new CharacterPoseStateEntry(
                new PoseStateEntryId(value.id),
                new PoseStateId(value.targetStateId));

        static CharacterPoseStateAlias ConvertAlias(
            AgentPackagePoseStateAlias value) =>
            new CharacterPoseStateAlias(
                new PoseStateAliasId(value.id),
                value.name,
                value.sources.Select(ConvertSource).ToArray());

        static CharacterPoseStateTransitionSource ConvertSource(
            AgentPackagePoseTransitionSource value) =>
            Enum.Parse<PoseStateTransitionSourceKind>(
                value.kind,
                false) == PoseStateTransitionSourceKind.State
                ? CharacterPoseStateTransitionSource.FromState(
                    new PoseStateId(value.stateId))
                : CharacterPoseStateTransitionSource.FromAlias(
                    new PoseStateAliasId(value.aliasId));

        CharacterPoseStateTransition ConvertTransition(
            AgentPackagePoseTransition value) =>
            new CharacterPoseStateTransition(
                new PoseStateTransitionId(value.id),
                ConvertSource(value.source),
                new PoseStateId(value.targetStateId),
                value.priority,
                ConvertRule(value.rule),
                Enum.Parse<AnimationTransitionBlendLogic>(
                    value.blendLogic,
                    false),
                value.durationSeconds,
                Enum.Parse<CharacterAnimationBlendMode>(
                    value.blendMode,
                    false),
                ResolveBlendCurveAsset(value.customBlendCurveAssetId),
                ResolveBlendProfileAsset(value.blendProfileAssetId));

        internal CharacterPoseStateMachineDefinition
            ConvertStateMachine(
                AgentPackagePoseStateMachineFile value) =>
            new CharacterPoseStateMachineDefinition(
                new PoseStateMachineId(value.id),
                value.contentRevision,
                ConvertEntry(value.entry),
                value.states.Select(ConvertState).ToArray(),
                value.transitions.Select(ConvertTransition).ToArray(),
                value.aliases.Select(ConvertAlias).ToArray(),
                value.maxTransitionsPerFrame);

        static CharacterPoseTransitionRuleGraph ConvertRule(
            AgentPackagePoseTransitionRule value) =>
            new CharacterPoseTransitionRuleGraph(
                new PoseTransitionRuleGraphId(value.id),
                value.contentRevision,
                value.operations.Select(operation =>
                    new CharacterPoseTransitionRuleOperation(
                        new PoseTransitionRuleOperationId(operation.id),
                        Enum.Parse<PoseTransitionRuleOperationKind>(
                            operation.kind,
                            false),
                        Optional(
                            operation.inputA,
                            text =>
                                new PoseTransitionRuleOperationId(text)),
                        Optional(
                            operation.inputB,
                            text =>
                                new PoseTransitionRuleOperationId(text)),
                        Optional(
                            operation.factId,
                            text => new PresentationFactId(text)),
                        operation.boolLiteral,
                        operation.floatLiteral,
                        operation.enumTypeId,
                        operation.enumLiteral,
                        operation.identityLiteral)).ToArray(),
                new PoseTransitionRuleOperationId(value.outputOperationId));

        static T Optional<T>(string value, Func<string, T> create) =>
            string.IsNullOrWhiteSpace(value) ? default : create(value);

        CharacterPresentationPoseSourceBinding ConvertSource(
            AgentPackagePoseSourceBinding value,
            CharacterPresentationPoseGraphAsset poseGraph,
            CharacterAnimationRigDefinition profileRig,
            AgentCompileReport report)
        {
            string path =
                $"editable/presentation/profile.json.poseSources[{value.name}]";
            CharacterAnimationRigDefinition rig =
                Resolve<CharacterAnimationRigDefinition>(
                    value.rig,
                    path + ".rig",
                    report);
            if (!rig || rig != profileRig)
            {
                report.Error(
                    path + ".rig",
                    "presentation_pose_source_rig_mismatch",
                    "Pose source Rig必须与Presentation Profile Rig一致。");
                return null;
            }
            try
            {
                PresentationPoseSourceKind kind =
                    Enum.Parse<PresentationPoseSourceKind>(
                        value.kind,
                        false);
                CharacterPresentationPoseSourceSlot slot = ResolveSourceSlot(
                    poseGraph,
                    value.slot,
                    kind,
                    path,
                    report);
                if (!slot)
                    return null;
                if (kind == PresentationPoseSourceKind.Sequence)
                {
                    AnimationClip clip = Resolve<AnimationClip>(
                        value.source,
                        path + ".source",
                        report);
                    if (!clip)
                        return null;
                    var binding = ScriptableObject.CreateInstance<
                        CharacterSequencePoseSourceBinding>();
                    binding.name = slot.name + " Binding";
                    binding.Configure(
                        (CharacterSequencePoseSourceSlot)slot,
                        clip,
                        rig,
                        value.loop,
                        value.defaultPlayRate,
                        value.markerGroupId,
                        Enum.Parse<AnimationMarkerSequenceTopology>(
                            value.markerTopology,
                            false),
                        Enum.Parse<AnimationMarkerSyncRole>(
                            value.syncRole,
                            false),
                        value.markers.Select(marker =>
                            new PresentationPoseSourceMarker(
                                marker.id,
                                marker.markerId,
                                marker.frame)).ToArray(),
                        ConvertCurve(value.footPlacementWeight),
                        value.footAnalysisIdentity);
                    return RegisterLocalBinding(value.binding, binding, path, report);
                }
                if (kind == PresentationPoseSourceKind.BlendSpace)
                {
                    CharacterAnimationBlendSpaceAsset blendSpace =
                        Resolve<CharacterAnimationBlendSpaceAsset>(
                            value.source,
                            path + ".source",
                            report);
                    if (!blendSpace)
                        return null;
                    var binding = ScriptableObject.CreateInstance<
                        CharacterBlendSpacePoseSourceBinding>();
                    binding.name = slot.name + " Binding";
                    binding.Configure(
                        (CharacterBlendSpacePoseSourceSlot)slot,
                        blendSpace,
                        rig,
                        value.footAnalysisIdentity);
                    return RegisterLocalBinding(value.binding, binding, path, report);
                }
                CharacterMotionMatchingProfile motionMatching =
                    Resolve<CharacterMotionMatchingProfile>(
                        value.source,
                        path + ".source",
                        report);
                CharacterMotionMatchingDatabaseDefinition[] databases =
                    (value.databases ??
                     new List<AgentPackageAssetReferenceV3>())
                    .Select((reference, index) =>
                        Resolve<CharacterMotionMatchingDatabaseDefinition>(
                            reference,
                            path + $".databases[{index}]",
                            report))
                    .Where(database => database)
                    .ToArray();
                if (!motionMatching ||
                    databases.Length != (value.databases?.Count ?? 0))
                    return null;
                var motionMatchingBinding = ScriptableObject.CreateInstance<
                    CharacterMotionMatchingPoseSourceBinding>();
                motionMatchingBinding.name = slot.name + " Binding";
                motionMatchingBinding.Configure(
                    (CharacterMotionMatchingPoseSourceSlot)slot,
                    motionMatching,
                    rig,
                    new CharacterMotionMatchingSearchDomainId(
                        value.searchDomainId),
                    databases,
                    value.footAnalysisIdentity);
                return RegisterLocalBinding(
                    value.binding,
                    motionMatchingBinding,
                    path,
                    report);
            }
            catch (Exception exception)
            {
                report.Error(
                    path,
                    "presentation_pose_source_lower_failed",
                    exception.Message);
                return null;
            }
        }

        CharacterPresentationPoseSourceSlot ResolveSourceSlot(
            CharacterPresentationPoseGraphAsset poseGraph,
            AgentPackageAssetReferenceV3 reference,
            PresentationPoseSourceKind kind,
            string path,
            AgentCompileReport report)
        {
            CharacterPresentationPoseSourceSlot slot;
            bool local = !string.IsNullOrWhiteSpace(reference?.localId);
            if (local)
            {
                m_LocalAssets.TryGetValue(reference.localId, out UnityEngine.Object value);
                slot = value as CharacterPresentationPoseSourceSlot;
            }
            else
            {
                slot = Resolve<CharacterPresentationPoseSourceSlot>(
                    reference,
                    path + ".slot",
                    report);
            }
            if (!slot || !local && !poseGraph.SourceSlots.Contains(slot) ||
                slot.SourceKind != kind)
            {
                report.Error(
                    path,
                    "presentation_pose_source_slot_unresolved",
                    "Pose source必须解析到当前Pose Graph中唯一且类型匹配的Source Slot对象。");
                return null;
            }
            return slot;
        }

        CharacterPresentationPoseSourceBinding RegisterLocalBinding(
            AgentPackageAssetReferenceV3 reference,
            CharacterPresentationPoseSourceBinding binding,
            string path,
            AgentCompileReport report)
        {
            if (string.IsNullOrWhiteSpace(reference?.localId))
                return binding;
            if (m_LocalAssets.TryAdd(reference.localId, binding))
                return binding;
            report.Error(
                path + ".binding.localId",
                "presentation_local_asset_identity_duplicate",
                "Profile binding local identity重复。");
            UnityEngine.Object.DestroyImmediate(binding);
            return null;
        }

        static string ReferenceIdentity(AgentPackageAssetReferenceV3 value) =>
            value == null
                ? string.Empty
                : !string.IsNullOrWhiteSpace(value.localId)
                    ? value.localId
                    : value.assetGuid + ":" + value.localFileId;

        static AnimationProducerPresentationBinding ConvertProducer(
            AgentPackageAnimationProducerBinding value,
            AgentCompileReport report)
        {
            string path =
                $"editable/presentation/profile.json.actionProducers[{ProducerKey(value)}]";
            TransitionAssetBase source = Resolve<TransitionAssetBase>(
                value.source,
                path + ".source",
                report);
            if (!source)
                return null;
            try
            {
                var binding = new AnimationProducerPresentationBinding();
                binding.ConfigureTimeline(
                    new AnimationProducerId(
                        value.timelineId,
                        value.trackId),
                    source,
                    value.footAnalysisIdentity);
                return binding;
            }
            catch (Exception exception)
            {
                report.Error(
                    path,
                    "presentation_producer_lower_failed",
                    exception.Message);
                return null;
            }
        }

        static AnimationCurve ConvertCurve(AgentPackageCurve value)
        {
            var curve = new AnimationCurve(
                value.keys.Select(key => new Keyframe(
                    key.time,
                    key.value,
                    key.inTangent,
                    key.outTangent,
                    key.inWeight,
                    key.outWeight)
                {
                    weightedMode = Enum.Parse<WeightedMode>(
                        key.weightedMode,
                        false)
                }).ToArray())
            {
                preWrapMode = Enum.Parse<WrapMode>(
                    value.preWrapMode,
                    false),
                postWrapMode = Enum.Parse<WrapMode>(
                    value.postWrapMode,
                    false)
            };
            return curve;
        }

        object ConvertProperty(
            string capabilityId,
            string role,
            string fieldId,
            JToken token,
            Type expectedType,
            AgentCompileReport report,
            string path)
        {
            GraphAuthoringCapabilityDescriptor capability;
            GraphAuthoringFieldDescriptor field;
            try
            {
                capability =
                    CharacterPoseGraphAuthoringCapabilities.Catalog.Require(
                        new GraphAuthoringCapabilityId(capabilityId),
                        CharacterPoseGraphAuthoringCapabilities.Domain,
                        new GraphAuthoringDocumentRoleId(role));
                field = capability.Fields.Single(value =>
                    string.Equals(
                        value.FieldId.Value,
                        fieldId,
                        StringComparison.Ordinal) &&
                    value.AuthoringWritable);
            }
            catch (Exception exception)
            {
                report.Error(
                    path,
                    "presentation_property_contract_missing",
                    exception.Message);
                return null;
            }
            try
            {
                return CharacterPoseAuthoringPayloadCodec.DecodeValue(
                    field,
                    token,
                    expectedType,
                    (descriptor, value, _) => ResolvePropertyAsset(
                        descriptor,
                        value,
                        path,
                        report));
            }
            catch (Exception exception)
            {
                report.Error(
                    path,
                    "presentation_property_lower_failed",
                    exception.Message);
                return null;
            }
        }

        static AgentPackagePoseStateMachineFile RequireStateMachine(
            AgentPackagePoseNode node,
            AgentDocumentPresentationEditable presentation)
        {
            AgentPackagePoseStateMachineFile[] matches =
                presentation.poseStateMachines
                    .Where(value => string.Equals(
                        value.id,
                        node.childDocumentId,
                        StringComparison.Ordinal))
                    .ToArray();
            return matches.Length == 1
                ? matches[0]
                : throw new InvalidOperationException(
                    $"Pose node '{node.id}' must reference one StateMachine document.");
        }

        UnityEngine.Object ResolvePropertyAsset(
            GraphAuthoringFieldDescriptor field,
            JToken token,
            string path,
            AgentCompileReport report)
        {
            if (token == null || token.Type == JTokenType.Null)
                return null;
            AgentPackageAssetReferenceV3 reference =
                token.ToObject<AgentPackageAssetReferenceV3>();
            Type expected = field.ObjectType ?? field.PickerKind switch
            {
                "animation-blend-policy" =>
                    typeof(CharacterAnimationBlendPolicy),
                "pose-inertialization-policy" =>
                    typeof(CharacterPoseInertializationPolicy),
                "animation-bone-mask" =>
                    typeof(CharacterAnimationBoneMaskAsset),
                "foot-placement-profile" =>
                    typeof(CharacterFootPlacementProfile),
                "foot-placement-calibration" =>
                    typeof(CharacterFootPlacementRigCalibration),
                _ => typeof(UnityEngine.Object)
            };
            if (!string.IsNullOrWhiteSpace(reference.localId))
            {
                if (m_LocalAssets.TryGetValue(reference.localId, out UnityEngine.Object local) &&
                    local && expected.IsInstanceOfType(local))
                    return local;
                report.Error(
                    path,
                    "presentation_local_asset_unresolved",
                    $"Local对象引用'{reference.localId}'没有解析到类型匹配的事务对象。");
                return null;
            }
            return Resolve(reference, expected, path, report);
        }

        static CharacterPoseNodeKind ResolveKind(
            string capability,
            GraphAuthoringDocumentRoleId role)
        {
            CharacterPoseGraphAuthoringCapabilities.Catalog.Require(
                new GraphAuthoringCapabilityId(capability),
                CharacterPoseGraphAuthoringCapabilities.Domain,
                role);
            return CharacterPoseCompilerHandlerRegistry.Shared
                .RequireCapability(capability)
                .Kind;
        }

        static void Normalize(
            AgentDocumentPresentationEditable value,
            IdentityMap identities,
            AgentCompileReport report)
        {
            foreach (AgentPackagePoseSourceBinding source in
                     value.profile.poseSources)
            {
                foreach (AgentPackagePoseSourceMarker marker in source.markers)
                    marker.id = identities.Map(marker.id);
            }
            foreach (AgentPackageAnimationProducerBinding producer in
                     value.profile.actionProducers)
            {
                producer.timelineId = identities.Map(producer.timelineId);
                producer.trackId = identities.Map(producer.trackId);
            }
            foreach (AgentPackagePoseGraphFile graph in value.poseGraphs)
            {
                graph.id = identities.Map(graph.id);
                foreach (AgentPackagePoseParameter parameter in graph.parameters)
                    parameter.id = identities.Map(parameter.id);
                GraphAuthoringDocumentRoleId role =
                    new GraphAuthoringDocumentRoleId(graph.role);
                foreach (AgentPackagePoseNode node in graph.nodes)
                {
                    node.id = identities.Map(node.id);
                    node.childDocumentId =
                        identities.MapOptional(node.childDocumentId);
                    try
                    {
                        GraphAuthoringCapabilityDescriptor capability =
                            CharacterPoseGraphAuthoringCapabilities.Catalog.Require(
                                new GraphAuthoringCapabilityId(node.capability),
                                CharacterPoseGraphAuthoringCapabilities.Domain,
                                role);
                        foreach (GraphAuthoringFieldDescriptor field in
                                 capability.Fields.Where(field =>
                                     field.AuthoringWritable &&
                                     field.ValueKind ==
                                     GraphAuthoringFieldValueKind
                                         .IdentityReference))
                        {
                            JToken token = node.properties[field.FieldId.Value];
                            if (token?.Type == JTokenType.String)
                                node.properties[field.FieldId.Value] =
                                    identities.Map(token.Value<string>());
                        }
                    }
                    catch (Exception exception)
                    {
                        report.Error(
                            GraphPath(graph.id) + $".nodes[{node.id}]",
                            "presentation_capability_normalize_failed",
                            exception.Message);
                    }
                    foreach (AgentPackagePoseDynamicPort port in
                             node.dynamicPorts)
                    {
                        port.id = identities.Map(port.id);
                        port.interfacePortId =
                            identities.MapOptional(port.interfacePortId);
                    }
                }
                foreach (AgentPackagePoseEdge edge in graph.edges)
                {
                    edge.id = identities.Map(edge.id);
                    edge.from.node = identities.Map(edge.from.node);
                    edge.from.port = identities.Map(edge.from.port);
                    edge.to.node = identities.Map(edge.to.node);
                    edge.to.port = identities.Map(edge.to.port);
                }
            }
            foreach (AgentPackagePoseGraphLayoutFile layout in
                     value.poseGraphLayouts)
            {
                layout.graphId = identities.Map(layout.graphId);
                foreach (AgentPackagePoseNodeLayout node in layout.nodes)
                    node.id = identities.Map(node.id);
            }
            foreach (AgentPackagePoseStateMachineFile machine in
                     value.poseStateMachines)
            {
                machine.id = identities.Map(machine.id);
                machine.entry.id = identities.Map(machine.entry.id);
                machine.entry.targetStateId =
                    identities.Map(machine.entry.targetStateId);
                foreach (AgentPackagePoseState state in machine.states)
                {
                    state.id = identities.Map(state.id);
                    state.poseGraphId = identities.Map(state.poseGraphId);
                    state.outputPoseNodeId =
                        identities.Map(state.outputPoseNodeId);
                }
                foreach (AgentPackagePoseStateAlias alias in machine.aliases)
                {
                    alias.id = identities.Map(alias.id);
                    foreach (AgentPackagePoseTransitionSource source in
                             alias.sources)
                        Normalize(source, identities);
                }
                foreach (AgentPackagePoseTransition transition in
                         machine.transitions)
                {
                    transition.id = identities.Map(transition.id);
                    Normalize(transition.source, identities);
                    transition.targetStateId =
                        identities.Map(transition.targetStateId);
                    transition.rule.id =
                        identities.Map(transition.rule.id);
                    transition.rule.outputOperationId =
                        identities.Map(
                            transition.rule.outputOperationId);
                    foreach (AgentPackagePoseTransitionRuleOperation operation
                             in transition.rule.operations)
                    {
                        operation.id = identities.Map(operation.id);
                        operation.inputA =
                            identities.MapOptional(operation.inputA);
                        operation.inputB =
                            identities.MapOptional(operation.inputB);
                    }
                }
            }
            foreach (AgentPackagePoseStateMachineLayoutFile layout in
                     value.poseStateMachineLayouts)
            {
                layout.stateMachineId = identities.Map(
                    layout.stateMachineId);
                foreach (AgentPackagePoseStateMachineLayoutElement element in
                         layout.elements)
                    element.id = identities.Map(element.id);
            }
            foreach (AgentPackageLinkedPoseImplementationFile implementation in
                     value.linkedPoseImplementations ??
                     new List<AgentPackageLinkedPoseImplementationFile>())
            {
                implementation.ownerIdentity = identities.MapOptional(
                    implementation.ownerIdentity);
                implementation.graphOwnerIdentity = identities.MapOptional(
                    implementation.graphOwnerIdentity);
                foreach (AgentPackageLinkedPoseImplementationEntry entry in
                         implementation.entries ??
                         new List<AgentPackageLinkedPoseImplementationEntry>())
                {
                    entry.graphId = identities.Map(entry.graphId);
                }
                var closure = new AgentDocumentPresentationEditable
                {
                    profile = new AgentPackagePresentationProfileFile(),
                    poseGraphs = implementation.poseGraphs,
                    poseGraphLayouts = implementation.poseGraphLayouts,
                    poseStateMachines = implementation.poseStateMachines,
                    poseStateMachineLayouts =
                        implementation.poseStateMachineLayouts
                };
                Normalize(closure, identities, report);
                implementation.poseGraphs = closure.poseGraphs;
                implementation.poseGraphLayouts = closure.poseGraphLayouts;
                implementation.poseStateMachines = closure.poseStateMachines;
                implementation.poseStateMachineLayouts =
                    closure.poseStateMachineLayouts;
            }
        }

        static void Normalize(
            AgentPackagePoseTransitionSource value,
            IdentityMap identities)
        {
            value.stateId = identities.MapOptional(value.stateId);
            value.aliasId = identities.MapOptional(value.aliasId);
        }

        static IEnumerable<string> CurrentIdentities(
            AgentDocumentPresentationEditable value)
        {
            foreach (AgentPackagePoseSourceBinding source in
                     value.profile?.poseSources ??
                     new List<AgentPackagePoseSourceBinding>())
            {
                yield return ReferenceIdentity(source.slot);
                yield return ReferenceIdentity(source.binding);
                foreach (AgentPackagePoseSourceMarker marker in source.markers)
                    yield return marker.id;
            }
            foreach (AgentPackageAnimationProducerBinding producer in
                     value.profile?.actionProducers ??
                     new List<AgentPackageAnimationProducerBinding>())
            {
                yield return producer.timelineId;
                yield return producer.trackId;
            }
            foreach (AgentPackagePoseGraphFile graph in value.poseGraphs)
            {
                yield return graph.id;
                foreach (AgentPackagePoseParameter parameter in graph.parameters)
                    yield return parameter.id;
                foreach (AgentPackagePoseNode node in graph.nodes)
                {
                    yield return node.id;
                    foreach (AgentPackagePoseDynamicPort port in
                             node.dynamicPorts)
                        yield return port.id;
                }
                foreach (AgentPackagePoseEdge edge in graph.edges)
                    yield return edge.id;
            }
            foreach (AgentPackagePoseStateMachineFile machine in
                     value.poseStateMachines)
            {
                yield return machine.id;
                yield return machine.entry.id;
                foreach (AgentPackagePoseState state in machine.states)
                    yield return state.id;
                foreach (AgentPackagePoseStateAlias alias in machine.aliases)
                    yield return alias.id;
                foreach (AgentPackagePoseTransition transition in
                         machine.transitions)
                {
                    yield return transition.id;
                    yield return transition.rule.id;
                    foreach (AgentPackagePoseTransitionRuleOperation operation
                             in transition.rule.operations)
                        yield return operation.id;
                }
            }
            foreach (AgentPackageLinkedPoseImplementationFile implementation in
                     value.linkedPoseImplementations ??
                     new List<AgentPackageLinkedPoseImplementationFile>())
            {
                yield return implementation.ownerIdentity;
                yield return implementation.graphOwnerIdentity;
                foreach (AgentPackageLinkedPoseImplementationEntry entry in
                         implementation.entries ??
                         new List<AgentPackageLinkedPoseImplementationEntry>())
                    yield return entry.graphId;
                var closure = new AgentDocumentPresentationEditable
                {
                    profile = new AgentPackagePresentationProfileFile(),
                    poseGraphs = implementation.poseGraphs,
                    poseGraphLayouts = implementation.poseGraphLayouts,
                    poseStateMachines = implementation.poseStateMachines,
                    poseStateMachineLayouts =
                        implementation.poseStateMachineLayouts
                };
                foreach (string identity in CurrentIdentities(closure))
                    yield return identity;
            }
        }

        static T Resolve<T>(
            AgentPackageAssetReferenceV3 reference,
            string path,
            AgentCompileReport report)
            where T : UnityEngine.Object =>
            Resolve(reference, typeof(T), path, report) as T;

        void InitializeBlendCatalog(
            AgentDocumentContext context,
            CharacterAnimationRigDefinition rig,
            AgentCompileReport report)
        {
            m_Rig = rig;
            m_BlendCurveCatalog = IndexBlendAssets(
                context?.presentation?.blendCurves,
                "AnimationBlendCurve",
                "context/asset-catalog.json.animationBlendCurves",
                report);
            m_BlendProfileCatalog = IndexBlendAssets(
                context?.presentation?.blendProfiles,
                "AnimationBlendProfile",
                "context/asset-catalog.json.animationBlendProfiles",
                report);
        }

        static Dictionary<string, AgentDocumentBlendAssetContext> IndexBlendAssets(
            IEnumerable<AgentDocumentBlendAssetContext> source,
            string kind,
            string path,
            AgentCompileReport report)
        {
            var result = new Dictionary<string, AgentDocumentBlendAssetContext>(StringComparer.Ordinal);
            foreach (AgentDocumentBlendAssetContext value in
                     source ?? Array.Empty<AgentDocumentBlendAssetContext>())
            {
                if (value == null || string.IsNullOrWhiteSpace(value.id) ||
                    !string.Equals(value.kind, kind, StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(value.revision) ||
                    string.IsNullOrWhiteSpace(value.assetPath) ||
                    string.IsNullOrWhiteSpace(value.assetGuid) ||
                    !result.TryAdd(value.id, value))
                {
                    report.Error(
                        path,
                        "presentation_blend_asset_catalog_invalid",
                        $"Blend资产目录包含非法或重复的{kind}条目。");
                }
            }
            return result;
        }

        CharacterAnimationBlendCurveAsset ResolveBlendCurveAsset(
            string curveId)
        {
            if (string.IsNullOrWhiteSpace(curveId))
                return null;
            if (m_BlendCurveCatalog == null ||
                !m_BlendCurveCatalog.TryGetValue(curveId, out AgentDocumentBlendAssetContext entry))
                throw new InvalidOperationException(
                    $"Custom Blend Curve asset identity '{curveId}' is not in the checkout Asset Catalog.");
            CharacterAnimationBlendCurveAsset asset = ResolveBlendAsset<CharacterAnimationBlendCurveAsset>(entry);
            asset.RequireValid();
            if (!string.Equals(asset.CurveId, curveId, StringComparison.Ordinal) ||
                !string.Equals(asset.Revision, entry.revision, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Custom Blend Curve asset identity or revision '{curveId}@{entry.revision}' is stale.");
            }
            return asset;
        }

        CharacterAnimationBlendProfile ResolveBlendProfileAsset(
            string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId))
                return null;
            if (m_BlendProfileCatalog == null ||
                !m_BlendProfileCatalog.TryGetValue(profileId, out AgentDocumentBlendAssetContext entry))
                throw new InvalidOperationException(
                    $"Animation Blend Profile asset identity '{profileId}' is not in the checkout Asset Catalog.");
            CharacterAnimationBlendProfile asset = ResolveBlendAsset<CharacterAnimationBlendProfile>(entry);
            AnimationBlendProfilePayload payload = new AnimationBlendProfilePayload(asset, m_Rig);
            string revision = StableHash.Compute(
                AnimationBlendCanonicalPayload.ProfileKey(payload)).ToString();
            if (!string.Equals(asset.ProfileId, profileId, StringComparison.Ordinal) ||
                !string.Equals(revision, entry.revision, StringComparison.Ordinal) ||
                !string.Equals(asset.RigId, entry.rigId, StringComparison.Ordinal) ||
                !string.Equals(asset.RigRevision, entry.rigRevision, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Animation Blend Profile asset identity, revision or Rig '{profileId}@{entry.revision}' is stale.");
            }
            return asset;
        }

        static T ResolveBlendAsset<T>(AgentDocumentBlendAssetContext entry)
            where T : UnityEngine.Object
        {
            string resolvedPath = AssetDatabase.GUIDToAssetPath(entry.assetGuid);
            if (!string.Equals(resolvedPath, entry.assetPath, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Blend asset '{entry.id}' path and GUID do not resolve to the same asset.");
            T asset = AssetDatabase.LoadAssetAtPath<T>(resolvedPath);
            return asset
                ? asset
                : throw new InvalidOperationException(
                    $"Blend asset '{entry.id}' is missing or not a {typeof(T).Name}.");
        }

        static T ResolveOptional<T>(
            AgentPackageAssetReferenceV3 reference,
            string path,
            AgentCompileReport report)
            where T : UnityEngine.Object =>
            reference == null
                ? null
                : Resolve<T>(reference, path, report);

        static UnityEngine.Object Resolve(
            AgentPackageAssetReferenceV3 reference,
            Type expected,
            string path,
            AgentCompileReport report)
        {
            if (reference == null)
                return null;
            if (!string.IsNullOrWhiteSpace(reference.localId))
            {
                report.Error(
                    path,
                    "presentation_local_asset_unresolved",
                    $"Local子资产引用'{reference.localId}'没有解析到当前事务的正式symbol。");
                return null;
            }
            string resolvedPath =
                AssetDatabase.GUIDToAssetPath(reference.assetGuid);
            if (!string.Equals(
                    resolvedPath,
                    reference.assetPath,
                    StringComparison.Ordinal))
            {
                report.Error(
                    path,
                    "presentation_asset_identity_mismatch",
                    "Presentation资源的assetGuid与assetPath没有指向同一正式资产。");
                return null;
            }
            UnityEngine.Object asset = AssetDatabase.LoadAllAssetsAtPath(
                    resolvedPath)
                .FirstOrDefault(candidate =>
                    candidate && expected.IsInstanceOfType(candidate) &&
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                        candidate,
                        out string guid,
                        out long localFileId) &&
                    string.Equals(
                        guid,
                        reference.assetGuid,
                        StringComparison.Ordinal) &&
                    localFileId == reference.localFileId);
            if (!asset)
            {
                report.Error(
                    path,
                    "presentation_asset_missing",
                    $"Presentation资源不存在或类型不是{expected.Name}。");
            }
            return asset;
        }

        static bool Matches(
            AgentPackageAssetReferenceV3 reference,
            UnityEngine.Object asset,
            string guid) =>
            reference != null &&
            asset &&
            string.Equals(
                reference.assetGuid,
                guid,
                StringComparison.Ordinal) &&
            string.Equals(
                reference.assetPath,
                AssetDatabase.GetAssetPath(asset),
                StringComparison.Ordinal) &&
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                asset,
                out _,
                out long localFileId) &&
            localFileId == reference.localFileId;

        static Dictionary<string, T> Index<T>(
            IEnumerable<T> values,
            Func<T, string> identity)
            where T : class =>
            (values ?? Array.Empty<T>())
            .Where(value => value != null)
            .ToDictionary(identity, StringComparer.Ordinal);

        static string ProducerKey(
            AgentPackageAnimationProducerBinding value) =>
            value.timelineId + ":" + value.trackId;

        static bool Same(object left, object right) =>
            string.Equals(
                AgentAuthoringDocumentCodec.Hash(left),
                AgentAuthoringDocumentCodec.Hash(right),
                StringComparison.Ordinal);

        static bool SameTransition(
            AgentPackagePoseTransition left,
            AgentPackagePoseTransition right)
        {
            if (left == null || right == null)
                return left == right;
            return Text(left.id, right.id) &&
                   SameSource(left.source, right.source) &&
                   Text(left.targetStateId, right.targetStateId) &&
                   left.priority == right.priority &&
                   SameRule(left.rule, right.rule) &&
                   Text(left.blendLogic, right.blendLogic) &&
                   left.durationSeconds.Equals(right.durationSeconds) &&
                   Text(left.blendMode, right.blendMode) &&
                   OptionalText(
                       left.customBlendCurveAssetId,
                       right.customBlendCurveAssetId) &&
                   OptionalText(
                       left.blendProfileAssetId,
                       right.blendProfileAssetId);
        }

        static bool SameStateStructure(
            AgentPackagePoseState left,
            AgentPackagePoseState right) =>
            left != null && right != null &&
            Text(left.id, right.id) &&
            Text(left.name, right.name) &&
            Text(left.poseGraphId, right.poseGraphId) &&
            Text(left.outputPoseNodeId, right.outputPoseNodeId);

        static bool SameSource(
            AgentPackagePoseTransitionSource left,
            AgentPackagePoseTransitionSource right) =>
            left != null && right != null &&
            Text(left.kind, right.kind) &&
            OptionalText(left.stateId, right.stateId) &&
            OptionalText(left.aliasId, right.aliasId);

        static bool SameRule(
            AgentPackagePoseTransitionRule left,
            AgentPackagePoseTransitionRule right)
        {
            if (left == null || right == null ||
                !Text(left.id, right.id) ||
                !Text(left.outputOperationId, right.outputOperationId))
                return false;
            AgentPackagePoseTransitionRuleOperation[] leftOperations =
                (left.operations ?? new List<AgentPackagePoseTransitionRuleOperation>())
                .OrderBy(value => value.id, StringComparer.Ordinal)
                .ToArray();
            AgentPackagePoseTransitionRuleOperation[] rightOperations =
                (right.operations ?? new List<AgentPackagePoseTransitionRuleOperation>())
                .OrderBy(value => value.id, StringComparer.Ordinal)
                .ToArray();
            return leftOperations.Length == rightOperations.Length &&
                   leftOperations.Zip(rightOperations, SameOperation).All(value => value);
        }

        static bool SameOperation(
            AgentPackagePoseTransitionRuleOperation left,
            AgentPackagePoseTransitionRuleOperation right) =>
            left != null && right != null &&
            Text(left.id, right.id) &&
            Text(left.kind, right.kind) &&
            OptionalText(left.inputA, right.inputA) &&
            OptionalText(left.inputB, right.inputB) &&
            OptionalText(left.factId, right.factId) &&
            left.boolLiteral == right.boolLiteral &&
            left.floatLiteral.Equals(right.floatLiteral) &&
            OptionalText(left.enumTypeId, right.enumTypeId) &&
            left.enumLiteral == right.enumLiteral &&
            OptionalText(left.identityLiteral, right.identityLiteral);

        static bool Text(string left, string right) =>
            string.Equals(left, right, StringComparison.Ordinal);

        static bool OptionalText(string left, string right) =>
            string.Equals(
                left ?? string.Empty,
                right ?? string.Empty,
                StringComparison.Ordinal);

        static bool SamePoseField(JToken left, JToken right)
        {
            if (left == null || right == null)
                return left == null && right == null;
            if (IsNumber(left.Type) && IsNumber(right.Type))
                return left.Value<float>().Equals(right.Value<float>());
            if (left is JArray leftArray && right is JArray rightArray)
            {
                return leftArray.Count == rightArray.Count &&
                       leftArray.Zip(
                               rightArray,
                               SamePoseField)
                           .All(value => value);
            }
            if (left is JObject leftObject && right is JObject rightObject)
            {
                List<JProperty> leftProperties = leftObject.Properties()
                    .OrderBy(value => value.Name, StringComparer.Ordinal)
                    .ToList();
                List<JProperty> rightProperties = rightObject.Properties()
                    .OrderBy(value => value.Name, StringComparer.Ordinal)
                    .ToList();
                return leftProperties.Count == rightProperties.Count &&
                       leftProperties.Zip(
                               rightProperties,
                               (leftProperty, rightProperty) =>
                                   string.Equals(
                                       leftProperty.Name,
                                       rightProperty.Name,
                                       StringComparison.Ordinal) &&
                                   SamePoseField(
                                       leftProperty.Value,
                                       rightProperty.Value))
                           .All(value => value);
            }
            return JToken.DeepEquals(left, right);
        }

        static bool IsNumber(JTokenType type) =>
            type == JTokenType.Integer ||
            type == JTokenType.Float;

        static string GraphPath(string id) =>
            $"editable/presentation/pose-graphs/{id}/graph.json";

        static string StateMachinePath(string id) =>
            $"editable/presentation/pose-state-machines/{id}/state-machine.json";

        static string StateMachineLayoutPath(string id) =>
            $"editable/presentation/pose-state-machines/{id}/layout.json";

        static string DescribeTransition(
            AgentPackagePoseStateMachineFile machine,
            AgentPackagePoseTransition transition,
            string action)
        {
            string source = transition.source == null
                ? "Missing Source"
                : string.Equals(transition.source.kind, "State", StringComparison.Ordinal)
                    ? StateName(machine, transition.source.stateId)
                    : AliasName(machine, transition.source.aliasId);
            string target = StateName(machine, transition.targetStateId);
            string curve = string.IsNullOrWhiteSpace(
                transition.customBlendCurveAssetId)
                ? string.Empty
                : $"; Custom Curve={transition.customBlendCurveAssetId}";
            return FormattableString.Invariant(
                $"{action} {source} -> {target}; Logic={transition.blendLogic}; Duration={transition.durationSeconds:R}s; Mode={transition.blendMode}{curve}; Profile={transition.blendProfileAssetId}");
        }

        static string StateName(
            AgentPackagePoseStateMachineFile machine,
            string id)
        {
            AgentPackagePoseState state = machine.states.FirstOrDefault(
                value => string.Equals(value.id, id, StringComparison.Ordinal));
            return state == null || string.IsNullOrWhiteSpace(state.name)
                ? id ?? string.Empty
                : state.name;
        }

        static string AliasName(
            AgentPackagePoseStateMachineFile machine,
            string id)
        {
            AgentPackagePoseStateAlias alias = machine.aliases.FirstOrDefault(
                value => string.Equals(value.id, id, StringComparison.Ordinal));
            return alias == null || string.IsNullOrWhiteSpace(alias.name)
                ? id ?? string.Empty
                : alias.name;
        }

        sealed class PlanBuilder
        {
            readonly CharacterPresentationMutationTransaction
                m_GraphTransaction;
            readonly CharacterPresentationMutationTransaction
                m_ProfileTransaction;
            readonly AgentCompileReport m_Report;
            readonly DiffSequence m_Sequence;

            public PlanBuilder(
                CharacterPresentationMutationTransaction graphTransaction,
                CharacterPresentationMutationTransaction profileTransaction,
                AgentCompileReport report,
                DiffSequence sequence = null)
            {
                m_GraphTransaction = graphTransaction;
                m_ProfileTransaction = profileTransaction;
                m_Report = report;
                m_Sequence = sequence ?? new DiffSequence();
            }

            public PlanBuilder ForGraph(
                CharacterPresentationMutationTransaction graphTransaction) =>
                new PlanBuilder(
                    graphTransaction,
                    m_ProfileTransaction,
                    m_Report,
                    m_Sequence);

            public void Graph(
                string path,
                CharacterPresentationMutation mutation,
                string detail = null)
            {
                m_GraphTransaction.Add(mutation);
                Add(path, mutation, detail);
            }

            public void Profile(
                string path,
                CharacterPresentationMutation mutation)
            {
                m_ProfileTransaction.Add(mutation);
                Add(path, mutation, null);
            }

            void Add(
                string path,
                CharacterPresentationMutation mutation,
                string detail)
            {
                m_Report.plannedDiff.Add(new AgentCompileDiffEntry
                {
                    mutationId =
                        "presentation-" +
                        m_Sequence.Index++.ToString("D4"),
                    action = mutation.Kind.ToString(),
                    graph = mutation.OwnerId,
                    target = path,
                    detail = string.IsNullOrWhiteSpace(detail)
                        ? mutation.GetType().Name
                        : detail
                });
            }

            public sealed class DiffSequence
            {
                public int Index;
            }
        }

        sealed class IdentityMap
        {
            readonly string m_RootIdentity;
            readonly HashSet<string> m_Current;
            readonly Dictionary<string, string> m_Values =
                new Dictionary<string, string>(StringComparer.Ordinal);

            public IdentityMap(
                string rootIdentity,
                IEnumerable<string> current)
            {
                m_RootIdentity = rootIdentity ?? string.Empty;
                m_Current = new HashSet<string>(
                    current.Where(value => !string.IsNullOrWhiteSpace(value)),
                    StringComparer.Ordinal);
            }

            public string MapOptional(string value) =>
                string.IsNullOrWhiteSpace(value) ? string.Empty : Map(value);

            public string Map(string value)
            {
                if (string.IsNullOrWhiteSpace(value) ||
                    !value.StartsWith("local:", StringComparison.Ordinal))
                    return value;
                if (m_Values.TryGetValue(value, out string mapped))
                    return mapped;
                mapped = AgentAuthoringDocumentCodec.Hash(
                        m_RootIdentity + "\n" + value)
                    .Substring(0, 32);
                if (!m_Current.Add(mapped) ||
                    m_Values.Values.Contains(
                        mapped,
                        StringComparer.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Presentation local identity冲突：{value}");
                }
                m_Values.Add(value, mapped);
                return mapped;
            }
        }
    }
}
