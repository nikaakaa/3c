using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentGraphSnapshotExporter
    {
        readonly Dictionary<BaseGraph, string> m_GraphIds = new Dictionary<BaseGraph, string>();
        readonly HashSet<BaseGraph> m_ExportedGraphs = new HashSet<BaseGraph>();
        readonly HashSet<string> m_AssetIds = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> m_BlackboardDeclarationIds = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> m_TimelineTreeClipPaths = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> m_TimelineIds = new HashSet<string>(StringComparer.Ordinal);
        readonly Dictionary<string, BaseExposedProperty> m_BlackboardDeclarations = new Dictionary<string, BaseExposedProperty>(StringComparer.Ordinal);
        readonly Dictionary<BaseTree, string> m_ProjectedGraphPaths = new Dictionary<BaseTree, string>();

        public AgentGraphSnapshot Export(CharacterPipelineDefinition definition)
        {
            return Export(definition, AgentSnapshotExportMode.Compact);
        }

        public AgentGraphSnapshot ExportFull(CharacterPipelineDefinition definition)
        {
            return Export(definition, AgentSnapshotExportMode.Full);
        }

        public AgentGraphSnapshot Export(CharacterPipelineDefinition definition, AgentSnapshotExportMode mode)
        {
            m_GraphIds.Clear();
            m_ExportedGraphs.Clear();
            m_AssetIds.Clear();
            m_BlackboardDeclarationIds.Clear();
            m_TimelineTreeClipPaths.Clear();
            m_TimelineIds.Clear();
            m_BlackboardDeclarations.Clear();
            m_ProjectedGraphPaths.Clear();

            AgentGraphSnapshot snapshot = new AgentGraphSnapshot();
            snapshot.domain = AgentAuthoringSchema.CharacterControllerDomain;
            snapshot.exportMode = mode.ToString();
            if (!definition)
                return snapshot;

            snapshot.definitionName = definition.name;
            snapshot.definitionAssetPath = AssetDatabase.GetAssetPath(definition);
            snapshot.rootAssetPath = snapshot.definitionAssetPath;
            snapshot.rootIdentity = AssetDatabase.AssetPathToGUID(snapshot.definitionAssetPath);
            snapshot.rootTreeAssetPath = definition.RootTreeAsset ? AssetDatabase.GetAssetPath(definition.RootTreeAsset) : string.Empty;

            ExportBodyMotion(definition.BodyMotionProfile, mode, snapshot);
            ExportInputs(definition.InputProfile, snapshot);
            ExportActionProfiles(definition.ActionProfiles, snapshot);

            BaseTree root = definition.RootTreeAsset ? definition.RootTreeAsset.Tree : null;
            if (root != null)
            {
                snapshot.rootGraphAuthoringId = GraphId("root", root);
                snapshot.blackboardSchemaRevision = root.BlackboardAuthoringSchemaRevision;
                var program = definition.SimulationProgram;
                if (program)
                {
                    snapshot.programId = program.ProgramId;
                    snapshot.sourceRevision = program.SourceRevision;
                    snapshot.semanticHash = program.SemanticHash;
                    snapshot.numericProfileId = program.NumericProfileId;
                    snapshot.targetAbiVersion = program.TargetAbiVersion;
                    snapshot.programHash = program.ProgramHash;
                    snapshot.layoutHash = program.LayoutHash;
                }
                var projectionErrors = new List<string>();
                CharacterAuthoringTopologyProjection projection = CharacterAuthoringTopologyProjection.Build(root, projectionErrors);
                if (!projection.IsValid)
                    throw new InvalidOperationException(string.Join("\n", projectionErrors));
                IndexBlackboardDeclarations(projection);
                ExportPresentation(definition, projection, snapshot);
                IndexProjectedGraphPaths(projection);
                if (mode == AgentSnapshotExportMode.Full)
                {
                    ExportCompactGraph(root, "root", AgentGraphOwnership.RootAsset, string.Empty, string.Empty, snapshot);
                    ExportProjectedGraphs(projection, AgentSnapshotExportMode.Compact, snapshot);
                    m_ExportedGraphs.Clear();
                    ExportGraph(root, "root", AgentGraphOwnership.RootAsset, string.Empty, string.Empty, string.Empty, snapshot);
                    ExportProjectedGraphs(projection, AgentSnapshotExportMode.Full, snapshot);
                }
                else
                {
                    ExportCompactGraph(root, "root", AgentGraphOwnership.RootAsset, string.Empty, string.Empty, snapshot);
                    ExportProjectedGraphs(projection, AgentSnapshotExportMode.Compact, snapshot);
                }
                AttachAuthoringRoutes(projection, snapshot);
            }

            return snapshot;
        }

        static void ExportBodyMotion(
            CharacterBodyMotionProfile profile,
            AgentSnapshotExportMode mode,
            AgentGraphSnapshot snapshot)
        {
            if (!profile)
                return;
            string path = AssetDatabase.GetAssetPath(profile);
            string guid = AssetDatabase.AssetPathToGUID(path);
            AgentSnapshotBodyMotionProfile bodyMotion = snapshot.bodyMotion;
            bodyMotion.assetPath = path;
            bodyMotion.assetGuid = guid;
            bodyMotion.sourceIdentity = $"asset:{guid}";
            bodyMotion.contentRevision = CharacterAuthoringCompilationModel
                .ComputeBodyMotionContentRevision(profile, guid)
                .ToString();
            bodyMotion.semanticVersion = CharacterBodyMotionProfile.SemanticVersion;
            bodyMotion.requiredWorldCapability = WorldCapability.AirborneVerticalMotion.ToString();
            if (mode == AgentSnapshotExportMode.Full)
            {
                bodyMotion.gravityAcceleration = profile.GravityAcceleration.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                bodyMotion.maximumFallSpeed = profile.MaximumFallSpeed.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        void IndexBlackboardDeclarations(CharacterAuthoringTopologyProjection projection)
        {
            for (int graphIndex = 0; graphIndex < projection.Graphs.Count; graphIndex++)
            {
                CharacterAuthoringGraphEntry entry = projection.Graphs[graphIndex];
                if (!entry.FirstOccurrence || !(entry.Graph is BaseTree tree))
                    continue;

                for (int declarationIndex = 0; declarationIndex < tree.ExposedProperties.Count; declarationIndex++)
                {
                    BaseExposedProperty declaration = tree.ExposedProperties[declarationIndex];
                    if (declaration != null && !string.IsNullOrEmpty(declaration.DeclarationId))
                        m_BlackboardDeclarations[declaration.DeclarationId] = declaration;
                }
            }
        }

        void ExportInputs(CharacterInputProfile inputProfile, AgentGraphSnapshot snapshot)
        {
            if (!inputProfile)
                return;

            IReadOnlyList<CharacterInputValueDefinition> values = inputProfile.InputValues;
            for (int i = 0; i < values.Count; i++)
            {
                CharacterInputValueDefinition value = values[i];
                if (value == null)
                    continue;

                snapshot.inputValues.Add(new AgentSnapshotInputValue
                {
                    inputValueId = value.InputValueId,
                    valueType = value.ValueType.ToString()
                });
            }

            IReadOnlyList<CharacterActionRequestDefinition> requests = inputProfile.ActionRequests;
            for (int i = 0; i < requests.Count; i++)
            {
                CharacterActionRequestDefinition request = requests[i];
                if (request == null)
                    continue;

                snapshot.actionRequests.Add(new AgentSnapshotActionRequest
                {
                    requestId = request.RequestId,
                    bufferSeconds = request.BufferSeconds,
                    priority = request.Priority,
                    timingClass = request.TimingClass.ToString()
                });
            }
        }

        void ExportActionProfiles(IReadOnlyList<ActionProfile> profiles, AgentGraphSnapshot snapshot)
        {
            if (profiles == null)
                return;

            for (int i = 0; i < profiles.Count; i++)
            {
                ActionProfile profile = profiles[i];
                if (!profile)
                    continue;

                string path = AssetDatabase.GetAssetPath(profile);
                snapshot.actionProfiles.Add(new AgentSnapshotActionProfile
                {
                    actionId = profile.ActionId,
                    displayName = profile.DisplayName,
                    assetPath = path,
                    assetGuid = AssetDatabase.AssetPathToGUID(path),
                    targetRequirement = profile.TargetRequirement.ToString(),
                    grantedTags = profile.Tags.Select(value => value.Value).ToList(),
                    blockQuery = ExportTagQuery(profile.BlockTags),
                    cancelQuery = ExportTagQuery(profile.CancelTags)
                });
            }
        }

        void ExportPresentation(
            CharacterPipelineDefinition definition,
            CharacterAuthoringTopologyProjection topology,
            AgentGraphSnapshot snapshot)
        {
            CharacterAnimationPresentationProfile presentation = definition.AnimationPresentationProfile;
            string profilePath = presentation ? AssetDatabase.GetAssetPath(presentation) : string.Empty;
            snapshot.presentation.profileAssetPath = profilePath;
            snapshot.presentation.profileAssetGuid = string.IsNullOrEmpty(profilePath)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(profilePath);
            if (!presentation)
                return;

            CharacterPresentationPoseGraphAsset poseGraph = presentation.PoseGraph;
            string poseGraphPath = poseGraph ? AssetDatabase.GetAssetPath(poseGraph) : string.Empty;
            snapshot.presentation.poseGraphAssetPath = poseGraphPath;
            snapshot.presentation.poseGraphAssetGuid = string.IsNullOrEmpty(poseGraphPath)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(poseGraphPath);
            snapshot.presentation.poseGraphId = poseGraph && poseGraph.Graph != null ? poseGraph.Graph.GraphId.Value : string.Empty;
            snapshot.presentation.poseGraphRevision = poseGraph?.Graph?.ContentRevision ?? string.Empty;

            CharacterAnimationRigDefinition rig = presentation.RigDefinition;
            string rigPath = rig ? AssetDatabase.GetAssetPath(rig) : string.Empty;
            snapshot.presentation.rigAssetPath = rigPath;
            snapshot.presentation.rigAssetGuid = string.IsNullOrEmpty(rigPath)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(rigPath);
            snapshot.presentation.rigId = rig?.RigId ?? string.Empty;
            snapshot.presentation.rigRevision = rig?.Revision ?? string.Empty;

            snapshot.presentation.footAnalysisMode = presentation.FootPlacementAnalysisMode.ToString();
            snapshot.presentation.footAnalysisSourceAssetGuid = presentation.FootPlacementAnalysisSourceAssetGuid;
            if (CharacterFootPlacementAnalysisSource.IsAssetGuid(presentation.FootPlacementAnalysisSourceAssetGuid))
            {
                string sourcePath = AssetDatabase.GUIDToAssetPath(presentation.FootPlacementAnalysisSourceAssetGuid);
                CharacterFootPlacementAnalysisSource source =
                    AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(sourcePath);
                if (source)
                {
                    snapshot.presentation.footAnalysisSourceId = source.AnalysisSourceId.Value;
                    snapshot.presentation.footAnalysisSourceVersion = source.AnalysisVersion;
                    snapshot.presentation.footAnalysisAlgorithmVersion = CharacterFootPlacementAnalysisSource.AlgorithmVersion;
                }
            }

            ExportPresentationPoseGraphContext(
                presentation,
                poseGraph,
                poseGraph?.Graph,
                string.Empty,
                new HashSet<PoseGraphId>(),
                snapshot.presentation);

            ExportBlendSpaces(definition, presentation, snapshot.presentation);

            var exportedProducers = new HashSet<AnimationProducerId>();
            for (int timelineIndex = 0; timelineIndex < topology.Timelines.Count; timelineIndex++)
            {
                CharacterAuthoringTimelineEntry source = topology.Timelines[timelineIndex];
                if (source.Node == null || !source.Node.ActionContext)
                    continue;
                for (int trackIndex = 0; trackIndex < source.Timeline.Tracks.Count; trackIndex++)
                {
                    if (source.Timeline.Tracks[trackIndex] is not AnimationTrack track)
                        continue;
                    var producerId = new AnimationProducerId(source.Timeline.AuthoringId, track.AuthoringId);
                    if (!producerId.IsValid || !exportedProducers.Add(producerId))
                        continue;

                    AnimationProducerPresentationBinding binding = presentation.FindProducerBinding(producerId);
                    UnityEngine.Object sourceAsset = binding?.Source;
                    string sourceAssetPath = sourceAsset ? AssetDatabase.GetAssetPath(sourceAsset) : string.Empty;
                    snapshot.presentation.producers.Add(new AgentSnapshotAnimationProducer
                    {
                        route = ExportRoute(source.Route),
                        ownerKind = "ActionAnimationChannel",
                        timelineAuthoringId = producerId.TimelineAuthoringId,
                        trackAuthoringId = producerId.TrackAuthoringId,
                        timelineName = source.Timeline.Name,
                        trackName = track.Name,
                        actionContextId = AssetIdentity(source.Node.ActionContext),
                        animationChannelId = track.AnimationChannelId.IsValid ? track.AnimationChannelId.Value : string.Empty,
                        sourceAssetPath = sourceAssetPath,
                        sourceAssetGuid = string.IsNullOrEmpty(sourceAssetPath)
                            ? string.Empty
                            : AssetDatabase.AssetPathToGUID(sourceAssetPath),
                        sourceAssetType = sourceAsset ? sourceAsset.GetType().FullName : string.Empty
                    });
                }
            }
        }

        static void ExportBlendSpaces(
            CharacterPipelineDefinition definition,
            CharacterAnimationPresentationProfile profile,
            AgentSnapshotAnimationPresentation destination)
        {
            var assets = new HashSet<CharacterAnimationBlendSpaceAsset>();
            for (int i = 0; i < profile.PoseSourceBindings.Count; i++)
            {
                CharacterPresentationPoseSourceBinding binding =
                    profile.PoseSourceBindings[i];
                if (binding is CharacterBlendSpacePoseSourceBinding blendSpace &&
                    blendSpace.BlendSpace)
                {
                    assets.Add(blendSpace.BlendSpace);
                }
            }
            string compileStatus = ResolvePresentationCompileStatus(definition, out string projectionRevision);
            foreach (CharacterAnimationBlendSpaceAsset asset in assets.OrderBy(value => value.BlendSpaceId.Value, StringComparer.Ordinal))
            {
                string path = AssetDatabase.GetAssetPath(asset);
                var entry = new AgentSnapshotAnimationBlendSpace
                {
                    assetPath = path,
                    assetGuid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path),
                    blendSpaceId = asset.BlendSpaceId.Value,
                    contentRevision = asset.ContentRevision,
                    mode = asset.Mode.ToString(),
                    xParameterId = asset.XAxis?.ParameterId.Value ?? string.Empty,
                    xUnit = asset.XAxis?.Unit ?? string.Empty,
                    xMinimum = asset.XAxis?.Minimum ?? 0f,
                    xMaximum = asset.XAxis?.Maximum ?? 0f,
                    yParameterId = asset.YAxis?.ParameterId.Value ?? string.Empty,
                    yUnit = asset.YAxis?.Unit ?? string.Empty,
                    yMinimum = asset.YAxis?.Minimum ?? 0f,
                    yMaximum = asset.YAxis?.Maximum ?? 0f,
                    sampleCount = asset.Samples.Count,
                    compileStatus = compileStatus,
                    projectionRevision = projectionRevision
                };
                CharacterAnimationBlendSpaceValidationReport report = CharacterAnimationBlendSpaceValidator.Validate(asset);
                for (int issue = 0; issue < report.Issues.Count; issue++)
                    entry.diagnostics.Add(report.Issues[issue].ToString());
                destination.blendSpaces.Add(entry);
            }
        }

        static string ResolvePresentationCompileStatus(
            CharacterPipelineDefinition definition,
            out string projectionRevision)
        {
            projectionRevision = definition && definition.PresentationProjection
                ? definition.PresentationProjection.ProjectionRevision
                : string.Empty;
            if (!definition || !definition.SimulationProgram || !definition.PresentationProjection)
                return "Missing";
            try
            {
                if (CharacterSimulationProgramBuildService.EvaluateExactArtifactStaleness(definition))
                    return "Stale";
                CharacterSimulationProgram program = definition.SimulationProgram.Load();
                CharacterPresentationSemanticContract contract = Float32CharacterPresentationContractAdapter.Create(program);
                _ = definition.PresentationProjection.Load(contract);
                return "Ready";
            }
            catch (Exception exception)
            {
                return "Corrupt: " + exception.Message;
            }
        }

        static void ExportPresentationPoseGraphContext(
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationPoseGraphAsset owner,
            CharacterTypedPoseGraph graph,
            string scope,
            HashSet<PoseGraphId> path,
            AgentSnapshotAnimationPresentation destination)
        {
            if (!owner || graph == null || !path.Add(graph.GraphId))
                return;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                CharacterTypedPoseNode node = graph.Nodes[i];
                if (node == null)
                    continue;
                PoseNodeId scopedNodeId = string.IsNullOrEmpty(scope)
                    ? node.NodeId
                    : new PoseNodeId(scope + "/" + node.NodeId.Value);
                if (IsStateLocalPoseSourceNode(node.Kind))
                {
                    CharacterPresentationPoseSourceSlot sourceSlot =
                        node.PresentationPoseSourceSlot;
                    ResolveObjectIdentity(
                        sourceSlot,
                        out string sourceSlotAssetPath,
                        out string sourceSlotAssetGuid,
                        out long sourceSlotLocalFileId);
                    CharacterPosePortDefinition[] parameters =
                        CharacterPoseAuthoringPortProjection.Get(node)
                        .Where(port => port != null && port.Direction == CharacterPosePortDirection.Input && port.Kind == CharacterPosePortKind.Parameter)
                        .ToArray();
                    destination.stateLocalPoseSources.Add(new AgentSnapshotStateLocalPoseSource
                    {
                        graphId = graph.GraphId.Value,
                        nodeId = scopedNodeId.Value,
                        nodeKind = node.Kind.ToString(),
                        ownerKind = "StateLocalPoseSource",
                        sourceSlotName = sourceSlot ? sourceSlot.name : string.Empty,
                        sourceSlotAssetPath = sourceSlotAssetPath,
                        sourceSlotAssetGuid = sourceSlotAssetGuid,
                        sourceSlotLocalFileId = sourceSlotLocalFileId,
                        sourceKind = ResolveStateLocalPoseSourceKind(profile, node),
                        xParameterPortId = parameters.Length > 0 ? parameters[0].PortId.Value : string.Empty,
                        yParameterPortId = parameters.Length > 1 ? parameters[1].PortId.Value : string.Empty,
                        inputRangePolicy = node.Kind == CharacterPoseNodeKind.BlendSpacePlayer
                            ? node.BlendSpaceInputRangePolicy.ToString()
                            : string.Empty
                    });
                }
                if (node.Kind == CharacterPoseNodeKind.ActionPlaybackInput)
                {
                    destination.actionPlaybackInputs.Add(new AgentSnapshotActionPlaybackInput
                    {
                        graphId = graph.GraphId.Value,
                        nodeId = scopedNodeId.Value,
                        ownerKind = "ActionAnimationChannel",
                        animationChannelId = node.AnimationChannelId.IsValid
                            ? node.AnimationChannelId.Value
                            : string.Empty
                    });
                }
                if (node.Kind == CharacterPoseNodeKind.AnimationSlot)
                {
                    destination.animationSlots.Add(new AgentSnapshotAnimationSlot
                    {
                        graphId = graph.GraphId.Value,
                        nodeId = scopedNodeId.Value,
                        ownerKind = "ActionAnimationChannel",
                        animationSlotId = node.AnimationSlotId.IsValid
                            ? node.AnimationSlotId.Value
                            : string.Empty,
                        animationChannelId = node.AnimationChannelId.IsValid
                            ? node.AnimationChannelId.Value
                            : string.Empty
                    });
                }
                if (node.Kind == CharacterPoseNodeKind.PoseSubgraph &&
                    node.Subgraph?.PoseGraphId.IsValid == true)
                {
                    CharacterTypedPoseGraph child =
                        owner.RequireGraph(node.Subgraph.PoseGraphId);
                    ExportPresentationPoseGraphContext(
                        profile,
                        owner,
                        child,
                        scopedNodeId.Value + "/" + child.GraphId,
                        path,
                        destination);
                }
                CharacterPoseStateMachineDefinition machine =
                    node.PoseStateMachine;
                if (node.Kind != CharacterPoseNodeKind.PoseStateMachine ||
                    machine == null)
                {
                    continue;
                }
                for (int stateIndex = 0;
                     stateIndex < machine.States.Count;
                     stateIndex++)
                {
                    CharacterPoseStateDefinition state = machine.States[stateIndex];
                    if (state == null)
                        continue;
                    CharacterTypedPoseGraph child = RequirePoseStateGraph(
                        owner,
                        machine,
                        state);
                    ExportPresentationPoseGraphContext(
                        profile,
                        owner,
                        child,
                        scopedNodeId.Value + "/state/" + state.StateId.Value,
                        path,
                        destination);
                }
            }
            path.Remove(graph.GraphId);
        }

        static CharacterTypedPoseGraph RequirePoseStateGraph(
            CharacterPresentationPoseGraphAsset owner,
            CharacterPoseStateMachineDefinition machine,
            CharacterPoseStateDefinition state)
        {
            string path =
                $"context.dependencies.presentation.poseStateMachines[{machine.StateMachineId}].states[{state.StateId}]";
            if (!state.PoseGraphId.IsValid)
            {
                throw new AgentAuthoringOperationException(
                    "presentation_pose_state_graph_reference_missing",
                    path,
                    $"Pose State '{state.StateId}' has no GraphCatalog reference.",
                    "使用正式Presentation Pose Graph authoring入口为该State配置PoseGraphId与OutputPoseNodeId。");
            }
            if (!owner.TryGetGraph(state.PoseGraphId, out CharacterTypedPoseGraph graph))
            {
                throw new AgentAuthoringOperationException(
                    "presentation_pose_state_graph_missing",
                    path,
                    $"Pose State '{state.StateId}' references missing Pose Graph '{state.PoseGraphId}'.",
                    "使用正式Presentation Pose Graph authoring入口修复GraphCatalog与State引用。");
            }
            return graph;
        }

        static bool IsStateLocalPoseSourceNode(CharacterPoseNodeKind kind)
        {
            return kind == CharacterPoseNodeKind.SelectedPosePlayer ||
                   kind == CharacterPoseNodeKind.BlendStack ||
                   kind == CharacterPoseNodeKind.BlendSpacePlayer ||
                   kind == CharacterPoseNodeKind.SequencePlayer;
        }

        static string ResolveStateLocalPoseSourceKind(
            CharacterAnimationPresentationProfile profile,
            CharacterTypedPoseNode node)
        {
            if (node == null || profile == null)
                return string.Empty;
            CharacterPresentationPoseSourceSlot sourceSlot =
                node.PresentationPoseSourceSlot;
            CharacterPresentationPoseSourceBinding source = sourceSlot
                ? profile.FindPoseSourceBinding(sourceSlot)
                : null;
            return source?.SourceKind.ToString() ?? string.Empty;
        }

        static void ResolveObjectIdentity(
            UnityEngine.Object asset,
            out string assetPath,
            out string assetGuid,
            out long localFileId)
        {
            assetPath = asset ? AssetDatabase.GetAssetPath(asset) : string.Empty;
            if (!asset ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    asset,
                    out assetGuid,
                    out localFileId))
            {
                assetGuid = string.Empty;
                localFileId = 0;
            }
        }

        void ExportProjectedGraphs(
            CharacterAuthoringTopologyProjection projection,
            AgentSnapshotExportMode mode,
            AgentGraphSnapshot snapshot)
        {
            if (projection == null)
                return;

            for (int i = 0; i < projection.Graphs.Count; i++)
            {
                CharacterAuthoringGraphEntry entry = projection.Graphs[i];
                BaseTree tree = entry.Graph;
                if (m_ExportedGraphs.Contains(tree))
                    continue;

                TreeAuthoringRouteId route = entry.Route;
                TreeAuthoringRouteSegment segment = route.Count > 0 ? route[route.Count - 1] : default;
                AgentGraphOwnership ownership = route.Count == 0
                    ? AgentGraphOwnership.RootAsset
                    : segment.Ownership == TreeGraphReferenceOwnership.Shared
                        ? AgentGraphOwnership.SharedAsset
                        : AgentGraphOwnership.Inline;
                if (!m_ProjectedGraphPaths.TryGetValue(tree, out string path))
                    throw new InvalidOperationException($"Projected Graph '{tree.name}' has no indexed path.");
                string ownerElementId = route.Count > 0 ? segment.OwnerElement.ElementAuthoringId : string.Empty;
                string referenceKey = route.Count > 0 ? segment.ReferenceKey : string.Empty;
                if (mode == AgentSnapshotExportMode.Full)
                {
                    string sharedPath = ownership == AgentGraphOwnership.SharedAsset
                        ? AssetDatabase.GetAssetPath(tree.SerializedOwner)
                        : string.Empty;
                    ExportGraph(tree, path, ownership, ownerElementId, referenceKey, sharedPath, snapshot);
                }
                else
                {
                    ExportCompactGraph(tree, path, ownership, ownerElementId, referenceKey, snapshot);
                }
            }
        }

        void IndexProjectedGraphPaths(CharacterAuthoringTopologyProjection projection)
        {
            for (int i = 0; i < projection.Graphs.Count; i++)
            {
                CharacterAuthoringGraphEntry entry = projection.Graphs[i];
                if (m_ProjectedGraphPaths.ContainsKey(entry.Graph))
                    continue;
                m_ProjectedGraphPaths.Add(
                    entry.Graph,
                    entry.Route.Count == 0 ? "root" : entry.Route.ToString());
            }
        }

        static void AttachAuthoringRoutes(CharacterAuthoringTopologyProjection projection, AgentGraphSnapshot snapshot)
        {
            if (projection == null)
                return;

            for (int routeIndex = 0; routeIndex < projection.Graphs.Count; routeIndex++)
            {
                CharacterAuthoringGraphEntry entry = projection.Graphs[routeIndex];
                string graphId = entry.Graph?.GraphAuthoringId ?? string.Empty;
                if (string.IsNullOrEmpty(graphId))
                    continue;

                for (int i = 0; i < snapshot.graphSummaries.Count; i++)
                {
                    if (string.Equals(snapshot.graphSummaries[i].graphAuthoringId, graphId, StringComparison.Ordinal))
                        snapshot.graphSummaries[i].routes.Add(ExportRoute(entry.Route));
                }
                for (int i = 0; i < snapshot.stateMachines.Count; i++)
                {
                    if (string.Equals(snapshot.stateMachines[i].graphAuthoringId, graphId, StringComparison.Ordinal))
                        snapshot.stateMachines[i].routes.Add(ExportRoute(entry.Route));
                }
                for (int i = 0; i < snapshot.graphs.Count; i++)
                {
                    if (string.Equals(snapshot.graphs[i].graphAuthoringId, graphId, StringComparison.Ordinal))
                        snapshot.graphs[i].routes.Add(ExportRoute(entry.Route));
                }
            }
        }

        static AgentSnapshotAuthoringRoute ExportRoute(TreeAuthoringRouteId route)
        {
            var result = new AgentSnapshotAuthoringRoute
            {
                rootGraphAuthoringId = route?.RootGraphAuthoringId ?? string.Empty
            };
            if (route == null)
                return result;

            for (int i = 0; i < route.Count; i++)
            {
                TreeAuthoringRouteSegment segment = route[i];
                result.segments.Add(new AgentSnapshotAuthoringRouteSegment
                {
                    kind = segment.Kind.ToString(),
                    ownerElementKind = segment.OwnerElement.Kind.ToString(),
                    ownerGraphAuthoringId = segment.OwnerElement.GraphAuthoringId,
                    ownerElementAuthoringId = segment.OwnerElement.ElementAuthoringId,
                    referenceKey = segment.ReferenceKey,
                    scopeId = segment.ScopeId,
                    childGraphAuthoringId = segment.ChildGraphAuthoringId,
                    ownership = segment.Ownership.ToString(),
                    timelineAuthoringId = segment.TimelineAuthoringId,
                    trackAuthoringId = segment.TrackAuthoringId,
                    clipAuthoringId = segment.ClipAuthoringId
                });
            }
            return result;
        }

        void ExportCompactGraph(
            BaseTree tree,
            string path,
            AgentGraphOwnership ownership,
            string ownerNodeName,
            string referenceKey,
            AgentGraphSnapshot snapshot)
        {
            if (tree == null || m_ExportedGraphs.Contains(tree))
                return;

            tree.CheckInit();
            string graphId = GraphId(path, tree);
            m_ExportedGraphs.Add(tree);
            snapshot.graphSummaries.Add(new AgentSnapshotGraphSummary
            {
                graphAuthoringId = graphId,
                path = path,
                name = tree.name,
                kind = ResolveGraphKind(tree).ToString(),
                ownership = ownership.ToString(),
                ownerNode = ownerNodeName,
                referenceKey = referenceKey
            });
            ExportBlackboardDeclarations(tree, path, snapshot);

            if (tree is StateMachineGraph stateMachineGraph)
                snapshot.stateMachines.Add(ExportStateMachineSummary(stateMachineGraph, graphId, path, ownerNodeName, snapshot));

        }

        AgentSnapshotStateMachineSummary ExportStateMachineSummary(
            StateMachineGraph graph,
            string graphId,
            string graphPath,
            string ownerNodeName,
            AgentGraphSnapshot snapshot)
        {
            AgentSnapshotStateMachineSummary summary = new AgentSnapshotStateMachineSummary
            {
                graphAuthoringId = graphId,
                graphPath = graphPath,
                name = graph.name,
                ownerNode = ownerNodeName
            };

            foreach (StateNode state in graph.StateNodes)
            {
                if (state == null)
                    continue;

                AgentSnapshotStateSummary stateSummary = new AgentSnapshotStateSummary
                {
                    stateAuthoringId = state.GUID,
                    state = state.ResolvedDisplayName,
                    behaviorGraphAuthoringId = state.SubTree?.GraphAuthoringId ?? string.Empty,
                    behaviorGraphPath = FindGraphPath(state.SubTree)
                };

                ExportStateBehaviorFacts(state.SubTree as StateBehaviorSubTree, stateSummary, snapshot);
                summary.states.Add(stateSummary);
            }

            for (int i = 0; i < graph.Edges.Count; i++)
            {
                BaseEdge edge = graph.Edges[i];
                if (edge == null || !graph.IsTransitionEdge(edge))
                    continue;

                summary.transitions.Add(ExportTransitionSummary(graph, edge));
            }

            return summary;
        }

        void ExportStateBehaviorFacts(StateBehaviorSubTree tree, AgentSnapshotStateSummary stateSummary, AgentGraphSnapshot snapshot)
        {
            if (tree == null)
                return;

            tree.CheckInit();
            for (int i = 0; i < tree.Nodes.Count; i++)
            {
                BaseNode node = tree.Nodes[i];
                if (node == null)
                    continue;

                if (node is TimelineNode timelineNode)
                {
                    TimelineData timeline = timelineNode.Timeline;
                    RegisterKnownAsset(timelineNode.SharedTimelineAsset, snapshot);
                    RegisterKnownAsset(timelineNode.ActionContext, snapshot);
                    string timelinePath = timelineNode.SharedTimelineAsset
                        ? AssetDatabase.GetAssetPath(timelineNode.SharedTimelineAsset)
                        : string.Empty;
                    string timelineNodePath = $"{stateSummary.behaviorGraphPath}/node:{node.GUID}/timeline";
                    ExportTimelineTreeClips(timeline, timelineNodePath, timelineNode.TimelineOwnership, timelinePath, snapshot);
                    AgentSnapshotTimeline timelineSnapshot = ExportTimeline(timeline, snapshot);
                    timelineSnapshot?.callSites.Add(new AgentSnapshotTimelineCallSite
                    {
                        nodeAuthoringId = node.GUID,
                        graphPath = timelineNodePath,
                        playbackMode = timelineNode.PlaybackMode.ToString()
                    });
                    stateSummary.timelines.Add(new AgentSnapshotTimelineBindingSummary
                    {
                        nodeAuthoringId = node.GUID,
                        timelineAuthoringId = timeline?.AuthoringId ?? string.Empty,
                        displayName = node.ResolvedDisplayName,
                        timeline = timeline?.Name ?? string.Empty,
                        ownership = timelineNode.TimelineOwnership.ToString(),
                        graphPath = timelineNodePath,
                        timelineAssetPath = timelinePath,
                        timelineAssetGuid = string.IsNullOrEmpty(timelinePath) ? string.Empty : AssetDatabase.AssetPathToGUID(timelinePath),
                        actionContext = AssetName(timelineNode.ActionContext),
                        playbackMode = timelineNode.PlaybackMode.ToString(),
                        trackCount = timeline?.Tracks.Count ?? 0,
                        clipCount = timeline?.Tracks.Sum(track => track.Clips.Count) ?? 0
                    });
                    continue;
                }

                if (node is StateMachineNode stateMachineNode && stateMachineNode.Graph != null)
                {
                    string graphPath = FindGraphPath(stateMachineNode.Graph);
                    stateSummary.nestedStateMachines.Add(new AgentSnapshotNestedStateMachineSummary
                    {
                        nodeAuthoringId = node.GUID,
                        node = node.ResolvedDisplayName,
                        graphAuthoringId = GraphId(graphPath, stateMachineNode.Graph),
                        graphPath = graphPath,
                        ownership = stateMachineNode.GetModule<ScopedGraphReferenceModule>()?.SharedGraphAsset
                            ? AgentGraphOwnership.SharedAsset.ToString()
                            : AgentGraphOwnership.Inline.ToString()
                    });
                    continue;
                }

                if (node is ActivateActionInstanceNode activationNode)
                {
                    RegisterKnownAsset(activationNode.ActionContext, snapshot);
                    stateSummary.actionActivations.Add(new AgentSnapshotActionActivationSummary
                    {
                        nodeAuthoringId = node.GUID,
                        displayName = node.ResolvedDisplayName,
                        actionProfile = activationNode.ActionProfile ? activationNode.ActionProfile.ActionId : string.Empty,
                        sourceRequest = activationNode.SourceInputRequestId,
                        actionContext = AssetName(activationNode.ActionContext),
                        targetKey = activationNode.TargetKey,
                        targetSnapshotBlackboardKey = activationNode.TargetSnapshotVariable.DisplayKey
                    });
                    continue;
                }

                if (node is SubmitActionLifecycleTransitionNode lifecycleNode)
                {
                    RegisterKnownAsset(lifecycleNode.ActionContext, snapshot);
                    stateSummary.lifecycleTransitions.Add(new AgentSnapshotLifecycleSummary
                    {
                        nodeAuthoringId = node.GUID,
                        displayName = node.ResolvedDisplayName,
                        transitionType = lifecycleNode.TransitionType.ToString(),
                        reason = lifecycleNode.Reason,
                        actionContext = AssetName(lifecycleNode.ActionContext)
                    });
                    continue;
                }

                if (node is ExposedPropertyNode setter &&
                    setter.NodeType == ExposedPropertyNodeType.Set &&
                    setter.BlackboardVariable.IsValid &&
                    setter.Value.GetValue() is bool boolValue)
                {
                    m_BlackboardDeclarations.TryGetValue(setter.BlackboardVariable.DeclarationId, out BaseExposedProperty declaration);
                    stateSummary.blackboardWrites.Add(new AgentSnapshotBlackboardWriteSummary
                    {
                        nodeAuthoringId = setter.GUID,
                        declarationAuthoringId = setter.BlackboardVariable.DeclarationId,
                        declarationOwnerId = setter.BlackboardVariable.DeclarationOwnerId,
                        key = declaration?.BlackboardKey ?? setter.BlackboardVariable.DisplayKey,
                        valueType = typeof(bool).FullName,
                        boolValue = boolValue,
                        lifecyclePhase = ResolveLifecyclePhase(tree, setter)
                    });
                }
            }
        }

        static string ResolveLifecyclePhase(StateBehaviorSubTree tree, BaseNode target)
        {
            bool onEnter = IsReachable(tree, tree.OnEnter, target);
            bool onExit = IsReachable(tree, tree.OnExit, target);
            if (onEnter && onExit)
                return "Ambiguous";
            if (onEnter)
                return "OnEnter";
            if (onExit)
                return "OnExit";
            return "Unreachable";
        }

        static bool IsReachable(BaseTree graph, BaseNode source, BaseNode target)
        {
            if (graph == null || source == null || target == null)
                return false;

            var pending = new Queue<string>();
            var visited = new HashSet<string>(StringComparer.Ordinal);
            pending.Enqueue(source.GUID);
            while (pending.Count > 0)
            {
                string current = pending.Dequeue();
                if (!visited.Add(current))
                    continue;
                if (string.Equals(current, target.GUID, StringComparison.Ordinal))
                    return true;

                for (int i = 0; i < graph.Edges.Count; i++)
                {
                    BaseEdge edge = graph.Edges[i];
                    if (edge != null && string.Equals(edge.StartNodeGUID, current, StringComparison.Ordinal))
                        pending.Enqueue(edge.EndNodeGUID);
                }
            }

            return false;
        }

        void ExportBlackboardDeclarations(BaseTree tree, string graphPath, AgentGraphSnapshot snapshot)
        {
            for (int i = 0; i < tree.ExposedProperties.Count; i++)
            {
                BaseExposedProperty declaration = tree.ExposedProperties[i];
                if (declaration == null || !m_BlackboardDeclarationIds.Add(declaration.DeclarationId))
                    continue;

                m_BlackboardDeclarations[declaration.DeclarationId] = declaration;

                snapshot.blackboardDeclarations.Add(new AgentSnapshotBlackboardDeclaration
                {
                    declarationId = declaration.DeclarationId,
                    ownerId = tree.GraphAuthoringId,
                    graphPath = graphPath,
                    key = declaration.BlackboardKey,
                    valueType = declaration.ValueType?.FullName ?? string.Empty,
                    defaultValue = AgentAuthoringDocumentCodec.ToToken(declaration.GetValue()),
                    scope = declaration.BlackboardScope.ToString(),
                    lifetime = declaration.BlackboardLifetime.ToString(),
                    inputBinding = declaration.InputBinding == null
                        ? null
                        : new AgentSnapshotBlackboardInputBinding
                        {
                            inputValueId = declaration.InputBinding.InputValueId
                        },
                    factProjection = declaration.FactProjection == null
                        ? null
                        : new AgentSnapshotBlackboardFactProjection
                        {
                            kind = declaration.FactProjection.Kind.ToString(),
                            windowType = declaration.FactProjection.ActionWindowType,
                            windowId = declaration.FactProjection.ActionWindowId,
                            digest = declaration.FactProjection.ActionWindowDigest
                        },
                    categoryPath = declaration.BlackboardCategoryPath
                });
                if (declaration.InputBinding != null)
                {
                    snapshot.inputValues.Add(new AgentSnapshotInputValue
                    {
                        inputValueId = declaration.InputBinding.InputValueId,
                        valueType = declaration.ValueType == typeof(ActionTargetSnapshot)
                            ? ProgramInputValueKind.ActionTargetSnapshot.ToString()
                            : declaration.ValueType?.Name ?? string.Empty
                    });
                }
            }
        }

        void ExportTimelineTreeClips(
            TimelineData timeline,
            string timelineNodePath,
            TimelineOwnership timelineOwnership,
            string timelineAssetPath,
            AgentGraphSnapshot snapshot)
        {
            if (timeline == null || !m_TimelineTreeClipPaths.Add(timelineNodePath))
                return;

            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                if (!(timeline.Tracks[trackIndex] is TreeTrack treeTrack))
                    continue;

                for (int clipIndex = 0; clipIndex < treeTrack.Clips.Count; clipIndex++)
                {
                    if (!(treeTrack.Clips[clipIndex] is TreeClip treeClip))
                        continue;

                    TimelineRunningTree tree = treeClip.ResolvedTree;
                    var summary = new AgentSnapshotTimelineTreeClip
                    {
                        timelineAuthoringId = timeline.AuthoringId,
                        trackAuthoringId = treeTrack.AuthoringId,
                        clipAuthoringId = treeClip.AuthoringId,
                        timeline = timeline.Name,
                        timelineNodePath = timelineNodePath,
                        timelineOwnership = timelineOwnership.ToString(),
                        timelineAssetPath = timelineOwnership == TimelineOwnership.Shared ? timelineAssetPath : string.Empty,
                        trackIndex = trackIndex,
                        clipIndex = clipIndex,
                        startFrame = treeClip.StartFrame,
                        endFrame = treeClip.EndFrame,
                        phase = treeClip.ExecutionPhase.ToString(),
                        ownership = treeClip.Ownership.ToString(),
                        treeName = tree ? tree.name : string.Empty
                    };
                    if (tree != null)
                    {
                        foreach (ExposedPropertyNode setter in tree.Nodes.OfType<ExposedPropertyNode>()
                                     .Where(value => value.NodeType == ExposedPropertyNodeType.Set && value.BlackboardVariable.IsValid))
                        {
                            m_BlackboardDeclarations.TryGetValue(setter.BlackboardVariable.DeclarationId, out BaseExposedProperty declaration);
                            summary.writes.Add(new AgentSnapshotTreeClipWrite
                            {
                                declarationId = setter.BlackboardVariable.DeclarationId,
                                declarationOwnerId = setter.BlackboardVariable.DeclarationOwnerId,
                                blackboardKey = setter.BlackboardVariable.DisplayKey
                            });
                        }
                    }
                    snapshot.timelineTreeClips.Add(summary);
                }
            }
        }

        AgentSnapshotTimeline ExportTimeline(TimelineData timeline, AgentGraphSnapshot snapshot)
        {
            if (timeline == null)
                return null;
            if (!m_TimelineIds.Add(timeline.AuthoringId))
                return snapshot.timelines.FirstOrDefault(value =>
                    string.Equals(value.timelineAuthoringId, timeline.AuthoringId, StringComparison.Ordinal));

            var timelineSnapshot = new AgentSnapshotTimeline
            {
                timelineAuthoringId = timeline.AuthoringId,
                name = timeline.Name
            };
            snapshot.timelines.Add(timelineSnapshot);

            for (int sectionIndex = 0; sectionIndex < timeline.Sections.Count; sectionIndex++)
            {
                TimelineSection section = timeline.Sections[sectionIndex];
                if (section == null)
                    continue;
                timelineSnapshot.sections.Add(new AgentSnapshotTimelineSection
                {
                    sectionAuthoringId = section.AuthoringId,
                    name = section.Name,
                    frame = section.Frame
                });
            }

            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track == null)
                    continue;

                AnimationTrack animationTrack = track as AnimationTrack;
                var trackSnapshot = new AgentSnapshotTimelineTrack
                {
                    trackAuthoringId = track.AuthoringId,
                    typeName = track.GetType().FullName,
                    name = track.Name,
                    index = trackIndex,
                    animationChannelId = animationTrack != null && animationTrack.AnimationChannelId.IsValid
                        ? animationTrack.AnimationChannelId.Value
                        : string.Empty,
                    motionWarpTrack = track is MotionWarpTrack
                };
                timelineSnapshot.tracks.Add(trackSnapshot);

                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip clip = track.Clips[clipIndex];
                    if (clip == null)
                        continue;

                    BTSMTL.Timeline.AnimationClip animationClip =
                        clip as BTSMTL.Timeline.AnimationClip;
                    var clipSnapshot = new AgentSnapshotTimelineClip
                    {
                        clipAuthoringId = clip.AuthoringId,
                        typeName = clip.GetType().FullName,
                        index = clipIndex,
                        startFrame = clip.StartFrame,
                        endFrame = clip.EndFrame,
                        otherEaseInFrame = clip.OtherEaseInFrame,
                        otherEaseOutFrame = clip.OtherEaseOutFrame,
                        selfEaseInFrame = clip.SelfEaseInFrame,
                        selfEaseOutFrame = clip.SelfEaseOutFrame,
                        easeInFrame = clip.EaseInFrame,
                        easeOutFrame = clip.EaseOutFrame,
                        clipInFrame = clip.ClipInFrame,
                        extraPolationMode = animationClip?.ExtraPolationMode.ToString() ?? string.Empty,
                        animationSequence = animationClip != null && animationClip.Sequence
                            ? AssetReference(animationClip.Sequence)
                            : null
                    };
                    if (clip is MotionCurveClip motionCurve)
                    {
                        clipSnapshot.curveId = motionCurve.CurveId;
                        clipSnapshot.curveEndFrame = motionCurve.CurveEndFrame;
                        clipSnapshot.motionSpace = motionCurve.Space.ToString();
                        clipSnapshot.motionChannel = motionCurve.Channel.ToString();
                        clipSnapshot.motionBlendMode = motionCurve.BlendMode.ToString();
                        clipSnapshot.motionPriority = motionCurve.Priority;
                        clipSnapshot.consumeLowerChannels = motionCurve.ConsumeLowerChannels;
                    }
                    if (clip is MotionWarpClip warp)
                    {
                        clipSnapshot.motionWarpClip = true;
                        clipSnapshot.sourceMotionClipAuthoringId = warp.SourceMotionClipId;
                        clipSnapshot.sourceMotionClipPath = ResolveTimelineClipPath(timeline, warp.SourceMotionClipId);
                        clipSnapshot.translationMode = warp.TranslationMode.ToString();
                        clipSnapshot.targetOffsetSpace = warp.TargetOffsetSpace.ToString();
                        clipSnapshot.rotationMode = warp.RotationMode.ToString();
                        clipSnapshot.rotationMethod = warp.RotationMethod.ToString();
                        clipSnapshot.targetPlanarOffset = new AgentSnapshotVector2
                        {
                            x = warp.TargetPlanarOffset.x,
                            y = warp.TargetPlanarOffset.y
                        };
                        clipSnapshot.targetYawOffsetDegrees = warp.TargetYawOffsetDegrees;
                        clipSnapshot.maxTotalPositionCorrection = warp.MaxTotalPositionCorrection;
                        clipSnapshot.maxTotalYawCorrectionDegrees = warp.MaxTotalYawCorrectionDegrees;
                        clipSnapshot.maximumYawRateDegreesPerSecond = warp.MaximumYawRateDegreesPerSecond;
                        clipSnapshot.limitPolicy = warp.LimitPolicy.ToString();
                    }
                    for (int channelIndex = 0; channelIndex < TimelineCurveChannelCatalog.All.Count; channelIndex++)
                    {
                        TimelineCurveChannelDescriptor descriptor = TimelineCurveChannelCatalog.All[channelIndex];
                        if (!descriptor.Supports(clip))
                            continue;
                        AnimationCurve curve = descriptor.Read(clip);
                        clipSnapshot.curveChannels.Add(new AgentSnapshotTimelineCurveChannel
                        {
                            channelId = descriptor.ChannelId.Value,
                            displayName = descriptor.DisplayName,
                            timeDomain = descriptor.TimeDomain.ToString(),
                            bounded = descriptor.ValueDomain.IsBounded,
                            minimum = descriptor.ValueDomain.Minimum,
                            maximum = descriptor.ValueDomain.Maximum,
                            zero = descriptor.ValueDomain.Zero,
                            unit = descriptor.ValueDomain.Unit,
                            preWrapMode = curve.preWrapMode.ToString(),
                            postWrapMode = curve.postWrapMode.ToString(),
                            keys = ExportAnimationCurve(curve)
                        });
                    }
                    trackSnapshot.clips.Add(clipSnapshot);
                }
            }
            return timelineSnapshot;
        }

        static string ResolveTimelineClipPath(TimelineData timeline, string clipAuthoringId)
        {
            if (timeline == null || string.IsNullOrEmpty(clipAuthoringId))
                return string.Empty;
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip clip = track.Clips[clipIndex];
                    if (clip != null && string.Equals(clip.AuthoringId, clipAuthoringId, StringComparison.Ordinal))
                        return $"timeline:{timeline.AuthoringId}/track:{track.AuthoringId}/clip:{clip.AuthoringId}";
                }
            }
            return string.Empty;
        }

        static List<AgentAnimationCurveKey> ExportAnimationCurve(AnimationCurve curve)
        {
            var result = new List<AgentAnimationCurveKey>();
            if (curve == null)
                return result;
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                result.Add(new AgentAnimationCurveKey
                {
                    time = key.time,
                    value = key.value,
                    inTangent = key.inTangent,
                    outTangent = key.outTangent,
                    inWeight = key.inWeight,
                    outWeight = key.outWeight,
                    weightedMode = key.weightedMode.ToString()
                });
            }
            return result;
        }

        AgentSnapshotTransitionSummary ExportTransitionSummary(StateMachineGraph graph, BaseEdge edge)
        {
            AgentSnapshotTransitionSummary summary = new AgentSnapshotTransitionSummary
            {
                edgeAuthoringId = edge.GUID,
                fromElementAuthoringId = edge.StartNodeGUID,
                toElementAuthoringId = edge.EndNodeGUID,
                from = NodeLabel(ResolveNode(graph, edge.StartNode, edge.StartNodeGUID)),
                to = NodeLabel(ResolveNode(graph, edge.EndNode, edge.EndNodeGUID)),
                priority = edge.TransitionPriority
            };

            ConditionRuleGraph ruleGraph = edge.ConditionRuleGraph;
            if (ruleGraph == null)
                return summary;

            ExportCanonicalConditionTerms(ruleGraph, summary);
            return summary;
        }

        static void ExportCanonicalConditionTerms(
            ConditionRuleGraph ruleGraph,
            AgentSnapshotTransitionSummary summary)
        {
            var nodeIndexes = ruleGraph.Nodes
                .Select((node, index) => new { node, index })
                .Where(value => value.node != null)
                .ToDictionary(value => value.node, value => value.index);
            var candidates =
                new List<KeyValuePair<int, AgentSnapshotConditionTerm>>();
            var consumedComparisons = new HashSet<CompareNode>();
            List<CompareNode> walkLower = ruleGraph.Nodes
                .OfType<CompareNode>()
                .Where(compare =>
                    string.Equals(
                        ClassifyMovementComparison(ruleGraph, compare),
                        "walk_lower",
                        StringComparison.Ordinal))
                .ToList();
            List<CompareNode> walkUpper = ruleGraph.Nodes
                .OfType<CompareNode>()
                .Where(compare =>
                    string.Equals(
                        ClassifyMovementComparison(ruleGraph, compare),
                        "walk_upper",
                        StringComparison.Ordinal))
                .ToList();
            int walkCount = Math.Min(walkLower.Count, walkUpper.Count);
            for (int i = 0; i < walkCount; i++)
            {
                CompareNode lower = walkLower[i];
                CompareNode upper = walkUpper[i];
                consumedComparisons.Add(lower);
                consumedComparisons.Add(upper);
                candidates.Add(
                    new KeyValuePair<int, AgentSnapshotConditionTerm>(
                        Math.Min(nodeIndexes[lower], nodeIndexes[upper]),
                        new AgentSnapshotConditionTerm
                        {
                            kind = "move_walk"
                        }));
            }

            for (int i = 0; i < ruleGraph.Nodes.Count; i++)
            {
                BaseNode node = ruleGraph.Nodes[i];
                if (node == null ||
                    node is ConditionRuleResultNode ||
                    node is AndNode ||
                    node is OrNode ||
                    node is NotNode ||
                    node is CharacterInputValueInfoNode ||
                    node is CharacterMoveFacingAngleInfoNode ||
                    node is PipelineBlackboardFloatInfoNode ||
                    consumedComparisons.Contains(node as CompareNode))
                    continue;
                if (node is CompareNode compare)
                {
                    AgentSnapshotConditionTerm compareTerm =
                        ExportCompareTerm(ruleGraph, compare);
                    if (compareTerm != null)
                    {
                        candidates.Add(
                            new KeyValuePair<int, AgentSnapshotConditionTerm>(
                                i,
                                compareTerm));
                    }
                    continue;
                }
                if (node is CharacterActionRequestInfoNode requestNode)
                {
                    AddUnique(summary.requests, requestNode.RequestId);
                    candidates.Add(
                        new KeyValuePair<int, AgentSnapshotConditionTerm>(
                            i,
                            new AgentSnapshotConditionTerm
                            {
                                kind = "action_request",
                                request = requestNode.RequestId,
                                negate = IsNegated(ruleGraph, node)
                            }));
                    continue;
                }

                if (node is StateRootCompletedNode)
                {
                    candidates.Add(
                        new KeyValuePair<int, AgentSnapshotConditionTerm>(
                            i,
                            new AgentSnapshotConditionTerm
                            {
                                kind = "state_root_completed"
                            }));
                    continue;
                }

                if (node is ActionWindowActiveInfoNode windowNode)
                {
                    candidates.Add(
                        new KeyValuePair<int, AgentSnapshotConditionTerm>(
                            i,
                            new AgentSnapshotConditionTerm
                            {
                                kind = "action_window_active",
                                windowType = windowNode.WindowType,
                                negate = IsNegated(ruleGraph, node)
                            }));
                    continue;
                }

                if (node is CanActivateActionInfoNode admissionNode)
                {
                    ActionProfile profile = admissionNode.ActionProfile;
                    string path = profile ? AssetDatabase.GetAssetPath(profile) : string.Empty;
                    candidates.Add(
                        new KeyValuePair<int, AgentSnapshotConditionTerm>(
                            i,
                            new AgentSnapshotConditionTerm
                            {
                                kind = "action_can_activate",
                                actionProfile = profile
                                    ? profile.ActionId
                                    : string.Empty,
                                actionProfileAssetPath = path,
                                actionProfileAssetGuid =
                                    string.IsNullOrEmpty(path)
                                        ? string.Empty
                                        : AssetDatabase.AssetPathToGUID(path),
                                targetSnapshotBlackboardKey =
                                    admissionNode
                                        .TargetSnapshotVariable
                                        .DisplayKey,
                                negate = IsNegated(ruleGraph, node)
                            }));
                    continue;
                }

                if (node is PipelineBlackboardBoolInfoNode blackboardNode)
                {
                    candidates.Add(
                        new KeyValuePair<int, AgentSnapshotConditionTerm>(
                            i,
                            new AgentSnapshotConditionTerm
                            {
                                kind = "blackboard_bool",
                                blackboardKey =
                                    blackboardNode
                                        .BlackboardVariable
                                        .DisplayKey,
                                negate = IsNegated(ruleGraph, node)
                            }));
                }
            }

            summary.conditionTerms.AddRange(
                candidates
                    .OrderBy(value => value.Key)
                    .ThenBy(
                        value => value.Value.kind,
                        StringComparer.Ordinal)
                    .Select(value => value.Value));
        }

        static AgentSnapshotConditionTerm ExportCompareTerm(
            ConditionRuleGraph graph,
            CompareNode compare)
        {
            string movement = ClassifyMovementComparison(graph, compare);
            if (string.Equals(movement, "move_stop", StringComparison.Ordinal) ||
                string.Equals(movement, "move_has", StringComparison.Ordinal) ||
                string.Equals(movement, "move_run", StringComparison.Ordinal))
            {
                return new AgentSnapshotConditionTerm
                {
                    kind = movement,
                    negate = IsNegated(graph, compare)
                };
            }
            BaseNode left = PropertySource(
                graph,
                compare,
                "m_InputValue1");
            BaseNode right = PropertySource(
                graph,
                compare,
                "m_InputValue2");
            if (left is CharacterMoveFacingAngleInfoNode &&
                right is PipelineBlackboardFloatInfoNode angleThreshold &&
                compare.Comparison == CompareNode.CompareType.GreaterEqual)
            {
                return new AgentSnapshotConditionTerm
                {
                    kind = "turn_facing_angle",
                    blackboardKey = angleThreshold.BlackboardVariable.DisplayKey,
                    negate = IsNegated(graph, compare)
                };
            }
            if (left is ReadTargetDistanceNode &&
                right is ReadAIMemoryNode memory)
            {
                return new AgentSnapshotConditionTerm
                {
                    kind = "ai_target_distance_compare_blackboard",
                    blackboardKey =
                        memory.BlackboardVariable.DisplayKey,
                    compareType = compare.Comparison.ToString(),
                    negate = IsNegated(graph, compare)
                };
            }
            return null;
        }

        static string ClassifyMovementComparison(
            ConditionRuleGraph graph,
            CompareNode compare)
        {
            BaseNode left = PropertySource(
                graph,
                compare,
                "m_InputValue1");
            BaseNode right = PropertySource(
                graph,
                compare,
                "m_InputValue2");
            if (left is not CharacterInputVector2MagnitudeInfoNode input ||
                !string.Equals(
                    input.InputValueId,
                    "MoveAxis",
                    StringComparison.Ordinal) ||
                right is not PipelineBlackboardFloatInfoNode threshold)
                return string.Empty;
            string key = threshold.BlackboardVariable.DisplayKey;
            if (string.Equals(
                    key,
                    "StopThreshold",
                    StringComparison.Ordinal))
            {
                if (compare.Comparison ==
                    CompareNode.CompareType.Less)
                    return "move_stop";
                if (compare.Comparison ==
                    CompareNode.CompareType.Greater)
                    return "move_has";
            }
            if (string.Equals(
                    key,
                    "RunThreshold",
                    StringComparison.Ordinal))
            {
                if (compare.Comparison ==
                    CompareNode.CompareType.GreaterEqual)
                    return "move_run";
                if (compare.Comparison ==
                    CompareNode.CompareType.Less)
                    return "walk_upper";
            }
            if (string.Equals(
                    key,
                    "WalkThreshold",
                    StringComparison.Ordinal) &&
                compare.Comparison ==
                CompareNode.CompareType.GreaterEqual)
                return "walk_lower";
            return string.Empty;
        }

        static BaseNode PropertySource(
            ConditionRuleGraph graph,
            BaseNode target,
            string targetPort)
        {
            PropertyEdge edge = graph.PropertyEdges.FirstOrDefault(
                value =>
                    value != null &&
                    (value.EndNode == target ||
                     string.Equals(
                         value.EndNodeGUID,
                         target.GUID,
                         StringComparison.Ordinal)) &&
                    string.Equals(
                        value.EndPortName,
                        targetPort,
                        StringComparison.Ordinal));
            if (edge == null)
                return null;
            if (edge.StartNode != null)
                return edge.StartNode;
            return graph.Nodes.FirstOrDefault(
                node =>
                    node != null &&
                    string.Equals(
                        node.GUID,
                        edge.StartNodeGUID,
                        StringComparison.Ordinal));
        }

        static bool IsNegated(
            ConditionRuleGraph graph,
            BaseNode source)
        {
            return graph.PropertyEdges.Any(
                edge =>
                    edge != null &&
                    (edge.StartNode == source ||
                     string.Equals(
                         edge.StartNodeGUID,
                         source.GUID,
                         StringComparison.Ordinal)) &&
                    (edge.EndNode is NotNode ||
                     graph.Nodes.Any(
                         node =>
                             node is NotNode &&
                             string.Equals(
                                 node.GUID,
                                 edge.EndNodeGUID,
                                 StringComparison.Ordinal))));
        }

        static AgentSnapshotGameplayTagQuery ExportTagQuery(ThirdPersonGameplay.Tags.GameplayTagQuery query)
        {
            var result = new AgentSnapshotGameplayTagQuery();
            if (query == null)
                return result;
            result.all.AddRange(query.All.Select(value => value.Value));
            result.any.AddRange(query.Any.Select(value => value.Value));
            result.none.AddRange(query.None.Select(value => value.Value));
            return result;
        }

        void ExportGraph(
            BaseTree tree,
            string path,
            AgentGraphOwnership ownership,
            string ownerNodeId,
            string referenceKey,
            string sharedAssetPath,
            AgentGraphSnapshot snapshot)
        {
            if (tree == null || m_ExportedGraphs.Contains(tree))
                return;

            tree.CheckInit();
            string graphId = GraphId(path, tree);
            m_ExportedGraphs.Add(tree);
            AgentSnapshotGraph graph = new AgentSnapshotGraph
            {
                graphAuthoringId = graphId,
                path = path,
                name = tree.name,
                kind = ResolveGraphKind(tree).ToString(),
                ownership = ownership.ToString(),
                ownerElementAuthoringId = ownerNodeId,
                referenceKey = referenceKey,
                sharedAssetPath = sharedAssetPath
            };
            snapshot.graphs.Add(graph);
            ExportBlackboardDeclarations(tree, path, snapshot);

            Dictionary<string, string> nodeIds = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < tree.Nodes.Count; i++)
            {
                BaseNode node = tree.Nodes[i];
                if (node == null)
                    continue;

                string nodeId = NodeId(path, node);
                nodeIds[node.GUID] = nodeId;
                graph.nodes.Add(ExportNode(node, nodeId, snapshot));
            }

            for (int i = 0; i < tree.Edges.Count; i++)
            {
                BaseEdge edge = tree.Edges[i];
                if (edge == null)
                    continue;

                graph.flowEdges.Add(new AgentSnapshotFlowEdge
                {
                    elementAuthoringId = EdgeId(path, edge),
                    startElementAuthoringId = ResolveNodeId(nodeIds, edge.StartNodeGUID),
                    endElementAuthoringId = ResolveNodeId(nodeIds, edge.EndNodeGUID),
                    startPort = edge.StartPortName,
                    endPort = edge.EndPortName,
                    flowOrder = edge.FlowOrder,
                    transitionPriority = edge.TransitionPriority,
                    abortPolicy = edge.AbortPolicy.ToString(),
                    conditionRuleGraphAuthoringId = edge.ConditionRuleGraph ? GraphId(FindGraphPath(edge.ConditionRuleGraph), edge.ConditionRuleGraph) : string.Empty,
                    conditionRuleGraphPath = FindGraphPath(edge.ConditionRuleGraph)
                });

            }

            for (int i = 0; i < tree.PropertyEdges.Count; i++)
            {
                PropertyEdge edge = tree.PropertyEdges[i];
                if (edge == null)
                    continue;

                graph.propertyEdges.Add(new AgentSnapshotPropertyEdge
                {
                    elementAuthoringId = EdgeId(path, edge),
                    startElementAuthoringId = ResolveNodeId(nodeIds, edge.StartNodeGUID),
                    endElementAuthoringId = ResolveNodeId(nodeIds, edge.EndNodeGUID),
                    startPortId = edge.StartPortName,
                    endPortId = edge.EndPortName
                });
            }
        }

        AgentSnapshotNode ExportNode(BaseNode node, string nodeId, AgentGraphSnapshot snapshot)
        {
            AgentSnapshotNode result = new AgentSnapshotNode
            {
                elementAuthoringId = nodeId,
                typeName = node.GetType().FullName,
                displayName = node.ResolvedDisplayName,
                nodeTypeDisplayName = node.NodeTypeDisplayName,
                position = new AgentSnapshotVector2
                {
                    x = node.Position.x,
                    y = node.Position.y
                }
            };

            if (node is LoopNode loopNode)
                result.loopStopType = loopNode.LoopStopType.ToString();
            else if (node is CompareNode compareNode)
                result.compareType = compareNode.Comparison.ToString();
            else if (node is LocomotionInputMotionNode motionNode)
            {
                result.moveSpeed = motionNode.MoveSpeed;
                result.displacementMode = motionNode.DisplacementMode.ToString();
                result.turnSpeedDegrees = motionNode.TurnSpeedDegrees;
                result.cameraRelative = motionNode.CameraRelative;
                result.executionMode = motionNode.ExecutionMode.ToString();
                result.durationSeconds = motionNode.DurationSeconds;
            }
            else if (node is CharacterInputValueInfoNode inputValueNode)
                result.inputId = inputValueNode.InputValueId;
            else if (node is CharacterActionRequestInfoNode actionRequestNode)
                result.requestId = actionRequestNode.RequestId;
            else if (node is PipelineBlackboardValueInfoNode blackboardValueNode)
                result.blackboardDeclarationId = blackboardValueNode.BlackboardVariable.DeclarationId;
            else if (node is StateExitCauseInfoNode stateExitCauseNode)
                result.stateExitCause = stateExitCauseNode.Cause.ToString();
            else if (node is ActionContextActiveInfoNode actionContextNode)
            {
                result.actionContextId = AssetIdentity(actionContextNode.ActionContext);
                RegisterKnownAsset(actionContextNode.ActionContext, snapshot);
            }
            else if (node is ActionWindowActiveInfoNode actionWindowNode)
                result.windowType = actionWindowNode.WindowType;
            else if (node is CanActivateActionInfoNode actionAdmissionNode)
            {
                result.actionProfileId = actionAdmissionNode.ActionProfile ? actionAdmissionNode.ActionProfile.ActionId : string.Empty;
                result.targetSnapshotBlackboardDeclarationId = actionAdmissionNode.TargetSnapshotVariable.DeclarationId;
            }

            if (node is ExposedPropertyNode exposedProperty &&
                exposedProperty.BlackboardVariable.IsValid &&
                exposedProperty.Value?.ValueType != null)
            {
                m_BlackboardDeclarations.TryGetValue(
                    exposedProperty.BlackboardVariable.DeclarationId,
                    out BaseExposedProperty declaration);
                result.exposedProperty = new AgentSnapshotExposedProperty
                {
                    mode = exposedProperty.NodeType.ToString(),
                    declarationAuthoringId = exposedProperty.BlackboardVariable.DeclarationId,
                    declarationOwnerId = exposedProperty.BlackboardVariable.DeclarationOwnerId,
                    key = declaration?.BlackboardKey ?? exposedProperty.BlackboardVariable.DisplayKey,
                    valueType = exposedProperty.Value.ValueType.FullName,
                    value = exposedProperty.NodeType == ExposedPropertyNodeType.Set
                        ? AgentAuthoringDocumentCodec.ToToken(exposedProperty.Value.GetValue())
                        : null
                };
            }

            foreach (KeyValuePair<string, PropertyPort> pair in node.PropertyPortMap)
            {
                PropertyPort port = pair.Value;
                if (port == null)
                    continue;

                result.propertyPorts.Add(new AgentSnapshotPropertyPort
                {
                    portId = port.PortId,
                    displayName = port.DisplayName,
                    direction = port.Direction.ToString(),
                    valueType = port.ValueType != null ? port.ValueType.FullName : string.Empty
                });
            }

            foreach (NodeAssetReference reference in node.GetAssetReferences())
            {
                AgentSnapshotAssetReference assetReference = ExportAssetReference(reference);
                result.assetReferences.Add(assetReference);
                RegisterKnownAsset(assetReference, snapshot);
            }

            foreach (NodeGraphReference reference in node.GetGraphReferences())
            {
                BaseTree referenceTree = reference.Tree;
                string childPath = FindGraphPath(referenceTree);
                string childGraphId = referenceTree ? GraphId(childPath, referenceTree) : string.Empty;
                result.graphReferences.Add(new AgentSnapshotGraphReference
                {
                    key = reference.Key,
                    label = reference.Label,
                    graphAuthoringId = childGraphId,
                    graphPath = referenceTree ? childPath : string.Empty,
                    graphKind = referenceTree ? ResolveGraphKind(referenceTree).ToString() : AgentGraphKind.Unknown.ToString(),
                    ownership = reference.SharedAsset ? AgentGraphOwnership.SharedAsset.ToString() : (reference.Inline ? AgentGraphOwnership.Inline.ToString() : AgentGraphOwnership.Unknown.ToString()),
                    scopeId = reference.ScopeId,
                    sharedAssetPath = AssetPath(reference.SharedAsset),
                    required = reference.Required
                });

            }

            return result;
        }

        AgentSnapshotAssetReference ExportAssetReference(NodeAssetReference reference)
        {
            string path = reference.Asset ? AssetDatabase.GetAssetPath(reference.Asset) : string.Empty;
            return new AgentSnapshotAssetReference
            {
                key = reference.Key,
                label = reference.Label,
                assetPath = path,
                assetGuid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path),
                assetType = reference.Asset ? reference.Asset.GetType().FullName : string.Empty,
                required = reference.Required
            };
        }

        void RegisterKnownAsset(AgentSnapshotAssetReference reference, AgentGraphSnapshot snapshot)
        {
            if (string.IsNullOrEmpty(reference.assetPath))
                return;

            string id = string.IsNullOrEmpty(reference.assetGuid) ? reference.assetPath : reference.assetGuid;
            if (!m_AssetIds.Add($"{reference.assetType}:{id}"))
                return;

            AgentSnapshotAsset asset = new AgentSnapshotAsset
            {
                id = id,
                name = System.IO.Path.GetFileNameWithoutExtension(reference.assetPath),
                assetPath = reference.assetPath,
                assetGuid = reference.assetGuid,
                assetType = reference.assetType
            };

            if (reference.assetType == typeof(TimelineAsset).FullName)
                snapshot.timelineAssets.Add(asset);
            else if (reference.assetType == typeof(ActionContextSlot).FullName)
                snapshot.actionContextAssets.Add(asset);
        }

        void RegisterKnownAsset(UnityEngine.Object asset, AgentGraphSnapshot snapshot)
        {
            if (!asset)
                return;

            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
                return;

            RegisterKnownAsset(new AgentSnapshotAssetReference
            {
                key = asset.name,
                label = asset.name,
                assetPath = path,
                assetGuid = AssetDatabase.AssetPathToGUID(path),
                assetType = asset.GetType().FullName,
                required = false
            }, snapshot);
        }

        string FindGraphPath(BaseTree graph)
        {
            if (graph == null)
                return string.Empty;
            if (m_ProjectedGraphPaths.TryGetValue(graph, out string path))
                return path;
            throw new InvalidOperationException($"Graph '{graph.name}' is absent from the authoring topology projection.");
        }

        static string NodeLabel(BaseNode node)
        {
            if (node is StateMachineEnterNode)
                return "Enter";
            if (node is StateMachineAnyStateNode)
                return "AnyState";
            if (node is StateMachineExitNode)
                return "Exit";
            return node != null ? node.ResolvedDisplayName : string.Empty;
        }

        static BaseNode ResolveNode(BaseTree graph, BaseNode cachedNode, string guid)
        {
            if (cachedNode != null)
                return cachedNode;
            if (graph == null || string.IsNullOrEmpty(guid))
                return null;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                BaseNode node = graph.Nodes[i];
                if (node != null && node.GUID == guid)
                    return node;
            }
            return null;
        }

        static string AssetName(UnityEngine.Object asset)
        {
            return asset ? asset.name : string.Empty;
        }

        static string AssetIdentity(UnityEngine.Object asset)
        {
            if (!asset)
                return string.Empty;
            string path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
        }

        static AgentPackageAssetReferenceV3 AssetReference(UnityEngine.Object asset)
        {
            if (!asset ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    asset,
                    out string guid,
                    out long localFileId))
                return null;
            return new AgentPackageAssetReferenceV3
            {
                assetPath = AssetDatabase.GetAssetPath(asset),
                assetGuid = guid,
                localFileId = localFileId
            };
        }

        static void AddUnique(List<string> values, string value)
        {
            if (string.IsNullOrEmpty(value) || values.Contains(value))
                return;

            values.Add(value);
        }

        string GraphId(string path, BaseGraph graph)
        {
            if (!m_GraphIds.TryGetValue(graph, out string id))
            {
                id = graph?.GraphAuthoringId ?? string.Empty;
                m_GraphIds.Add(graph, id);
            }
            return id;
        }

        static AgentGraphKind ResolveGraphKind(BaseGraph graph)
        {
            if (graph is ConditionRuleGraph)
                return AgentGraphKind.ConditionRuleGraph;
            if (graph is StateMachineGraph)
                return AgentGraphKind.StateMachineGraph;
            if (graph is StateBehaviorSubTree)
                return AgentGraphKind.StateBehaviorSubTree;
            if (graph is SubTree)
                return AgentGraphKind.SubTree;
            if (graph is RunnableTree)
                return AgentGraphKind.RunnableTree;
            if (graph is BaseTree)
                return AgentGraphKind.BaseTree;
            return AgentGraphKind.Unknown;
        }

        static string NodeId(string graphPath, BaseNode node)
        {
            return node?.GUID ?? string.Empty;
        }

        static string EdgeId(string graphPath, BaseEdge edge)
        {
            return edge?.GUID ?? string.Empty;
        }

        static string ResolveNodeId(Dictionary<string, string> nodeIds, string guid)
        {
            return !string.IsNullOrEmpty(guid) && nodeIds.TryGetValue(guid, out string id) ? id : string.Empty;
        }

        static string AssetPath(BaseTreeAsset asset)
        {
            return asset ? AssetDatabase.GetAssetPath(asset) : string.Empty;
        }
    }
}
