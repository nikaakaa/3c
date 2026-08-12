using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BTSMTL.Timeline;
using Newtonsoft.Json.Linq;
using TreeDesigner;
using TreeDesigner.Editor;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal enum AgentAuthoringDocumentReadPhase
    {
        TargetMutation,
        CheckoutRoundTrip
    }

    public sealed class AgentAuthoringPackageMapper
    {
        readonly BtsmtlGraphAuthoringCapabilities m_Catalog =
            new BtsmtlGraphAuthoringCapabilities();

        public Dictionary<string, JToken> ToFiles(
            AgentAuthoringTarget target,
            AgentGraphSnapshot snapshot,
            AgentCompileReport report)
        {
            var files = new Dictionary<string, JToken>(StringComparer.Ordinal);
            if (string.Equals(target.domain, AgentAuthoringSchema.CharacterControllerDomain, StringComparison.Ordinal))
            {
                files["editable/controller.json"] = AgentAuthoringDocumentCodec.ToToken(new AgentPackageControllerFile
                {
                    stateMachines = target.editable.stateMachines,
                    timelineTreeClips = target.editable.timelineTreeClips
                });
                files["editable/blackboard.json"] = AgentAuthoringDocumentCodec.ToToken(new AgentPackageBlackboardFile
                {
                    schemaRevision = target.editable.blackboardSchemaRevision,
                    declarations = target.editable.blackboardDeclarations
                });
                files["editable/actions.json"] = AgentAuthoringDocumentCodec.ToToken(new AgentPackageActionsFile
                {
                    requests = target.editable.actionRequests,
                    profiles = target.editable.actionProfiles
                });
                AgentAuthoringPresentationPackageCodec.Write(
                    files,
                    target.editable.presentation,
                    report);
                AgentAuthoringPresentationPackageCodec.WriteReadonly(
                    files,
                    target.context.presentation,
                    report);
            }

            if (target.editable.aiController != null)
            {
                files["editable/ai/perception.json"] = AgentAuthoringDocumentCodec.ToToken(new AgentPackageAIFile
                {
                    blackboardSchemaRevision = target.editable.blackboardSchemaRevision,
                    controller = ToPackageAI(target.editable.aiController)
                });
            }

            foreach (AgentSnapshotGraph graph in target.editable.graphs ?? new List<AgentSnapshotGraph>())
            {
                string directory = $"editable/graphs/{Segment(graph.graphAuthoringId)}";
                if (!TryToGraphFiles(target.domain, graph, report, out AgentPackageGraphFile graphFile, out AgentPackageLayoutFile layoutFile))
                    continue;
                files[directory + "/graph.json"] = AgentAuthoringDocumentCodec.ToToken(graphFile);
                files[directory + "/layout.json"] = AgentAuthoringDocumentCodec.ToToken(layoutFile);
            }

            foreach (AgentSnapshotTimeline timeline in target.editable.timelines ?? new List<AgentSnapshotTimeline>())
            {
                string directory = $"editable/timelines/{Segment(timeline.timelineAuthoringId)}";
                ToTimelineFiles(timeline, out AgentPackageTimelineFile timelineFile, out AgentPackageCurvesFile curvesFile);
                files[directory + "/timeline.json"] = AgentAuthoringDocumentCodec.ToToken(timelineFile);
                files[directory + "/curves.json"] = AgentAuthoringDocumentCodec.ToToken(curvesFile);
            }

            var nodeCatalog = new AgentPackageNodeCatalogFile
            {
                kinds = m_Catalog.ExportNodeKinds(target.domain).ToList()
            };
            AgentPackageNodeCatalogValidator.Validate(nodeCatalog, report);
            files["context/node-catalog.json"] = AgentAuthoringDocumentCodec.ToToken(nodeCatalog);
            files["context/graph-kinds.json"] = AgentAuthoringDocumentCodec.ToToken(new AgentPackageGraphKindsFile
            {
                kinds = m_Catalog.ExportGraphKinds(target.domain).ToList()
            });
            files["context/asset-catalog.json"] = AgentAuthoringDocumentCodec.ToToken(new AgentPackageAssetCatalogFile
            {
                inputValues = target.context.inputValues,
                actionRequests = target.context.actionRequests,
                blackboardDeclarations = snapshot?.blackboardDeclarations ?? new List<AgentSnapshotBlackboardDeclaration>(),
                aiBlackboardDeclarations = snapshot?.aiController?.blackboardDeclarations ?? new List<AgentSnapshotAIBlackboardDeclaration>(),
                timelineAssets = target.context.timelineAssets,
                actionContextAssets = target.context.actionContextAssets,
                animationBlendCurves = target.context.presentation?.blendCurves ??
                    new List<AgentDocumentBlendAssetContext>(),
                animationBlendProfiles = target.context.presentation?.blendProfiles ??
                    new List<AgentDocumentBlendAssetContext>()
            });
            files["context/dependencies.json"] = AgentAuthoringDocumentCodec.ToToken(new AgentPackageDependenciesFile
            {
                definitionName = target.context.definitionName,
                definitionAssetPath = target.context.definitionAssetPath,
                rootTreeAssetPath = target.context.rootTreeAssetPath,
                rootGraphAuthoringId = target.context.rootGraphAuthoringId,
                bodyMotion = target.context.bodyMotion,
                presentation = target.context.presentation,
                animationMarkerGroups = target.context.animationMarkerGroups,
                generatedProduct = target.context.generatedProduct,
                aiController = target.context.aiController,
                capabilities = target.context.capabilities,
                graphDependencies = (snapshot?.graphs ?? new List<AgentSnapshotGraph>())
                    .Select(graph => new AgentPackageDependency
                    {
                        id = graph.graphAuthoringId,
                        ownerId = graph.ownerElementAuthoringId,
                        slot = graph.referenceKey,
                        ownership = graph.ownership
                    }).ToList(),
                timelineDependencies = (snapshot?.timelines ?? new List<AgentSnapshotTimeline>())
                    .SelectMany(timeline => (timeline.callSites ?? new List<AgentSnapshotTimelineCallSite>())
                        .Select(callSite => new AgentPackageDependency
                        {
                            id = timeline.timelineAuthoringId,
                            ownerId = callSite.nodeAuthoringId,
                            slot = "timeline",
                            mode = callSite.playbackMode
                        })).ToList()
            });
            return files;
        }

        internal bool TryFromFiles(
            AgentAuthoringPackageManifest manifest,
            IReadOnlyDictionary<string, JToken> files,
            AgentGraphSnapshot current,
            AgentCompileReport report,
            AgentAuthoringDocumentReadPhase phase,
            out AgentAuthoringTarget target)
        {
            target = new AgentAuthoringTarget
            {
                domain = manifest.domain,
                rootIdentity = manifest.rootIdentity
            };
            bool valid = true;
            AgentPackageControllerFile controller = new AgentPackageControllerFile();
            AgentPackageBlackboardFile blackboard = new AgentPackageBlackboardFile();
            AgentPackageActionsFile actions = new AgentPackageActionsFile();
            AgentPackageAIFile aiFile = null;
            if (string.Equals(manifest.domain, AgentAuthoringSchema.CharacterControllerDomain, StringComparison.Ordinal))
            {
                valid &= TryFile(files, "editable/controller.json", report, out controller);
                valid &= TryFile(files, "editable/blackboard.json", report, out blackboard);
                valid &= TryFile(files, "editable/actions.json", report, out actions);
                valid &= ValidateBlackboardPackage(blackboard, report);
            }
            valid &= TryFile(files, "context/asset-catalog.json", report, out AgentPackageAssetCatalogFile assets);
            valid &= TryFile(files, "context/dependencies.json", report, out AgentPackageDependenciesFile dependencies);
            valid &= TryFile(files, "context/node-catalog.json", report, out AgentPackageNodeCatalogFile nodeCatalog);
            valid &= TryFile(files, "context/graph-kinds.json", report, out AgentPackageGraphKindsFile _);
            if (nodeCatalog != null)
                valid &= AgentPackageNodeCatalogValidator.Validate(nodeCatalog, report);
            if (!valid)
                return false;

            target.editable.stateMachines = controller.stateMachines ?? new List<AgentSnapshotStateMachineSummary>();
            target.editable.timelineTreeClips = controller.timelineTreeClips ?? new List<AgentSnapshotTimelineTreeClip>();
            target.editable.blackboardSchemaRevision = blackboard.schemaRevision;
            target.editable.blackboardDeclarations = blackboard.declarations ?? new List<AgentSnapshotBlackboardDeclaration>();
            target.editable.actionRequests = actions.requests ?? new List<AgentSnapshotActionRequest>();
            target.editable.actionProfiles = actions.profiles ?? new List<AgentSnapshotActionProfile>();
            if (string.Equals(
                    manifest.domain,
                    AgentAuthoringSchema.CharacterControllerDomain,
                    StringComparison.Ordinal))
            {
                valid &= AgentAuthoringPresentationPackageCodec.TryRead(
                    files,
                    report,
                    out AgentDocumentPresentationEditable presentation);
                target.editable.presentation = presentation;
            }
            else if (files.Keys.Any(path =>
                         path.StartsWith(
                             "editable/presentation/",
                             StringComparison.Ordinal) ||
                         path.StartsWith(
                             "readonly/presentation/",
                             StringComparison.Ordinal)))
            {
                report.Error(
                    "editable/presentation",
                    "document_domain_file_invalid",
                    "AIController文档包不能包含Presentation分片。");
                valid = false;
            }

            AgentPackageAIController packageAI = null;
            if (string.Equals(manifest.domain, AgentAuthoringSchema.AIControllerDomain, StringComparison.Ordinal))
            {
                valid &= TryFile(files, "editable/ai/perception.json", report, out aiFile) &&
                         (packageAI = aiFile.controller) != null;
                if (aiFile != null)
                {
                    target.editable.blackboardSchemaRevision = aiFile.blackboardSchemaRevision;
                    valid &= ValidateBlackboardSchemaRevision(
                        aiFile.blackboardSchemaRevision,
                        "editable/ai/perception.json.blackboardSchemaRevision",
                        report);
                }
            }
            else if (files.ContainsKey("editable/ai/perception.json"))
            {
                report.Error("editable/ai/perception.json", "document_domain_file_invalid", "CharacterController文档包不能包含AI perception分片。");
                valid = false;
            }

            var currentGraphs = (current?.graphs ?? new List<AgentSnapshotGraph>())
                .Where(graph => graph != null && !string.IsNullOrEmpty(graph.graphAuthoringId))
                .ToDictionary(graph => graph.graphAuthoringId, graph => graph, StringComparer.Ordinal);
            foreach (string graphPath in files.Keys
                         .Where(path => path.StartsWith("editable/graphs/", StringComparison.Ordinal) && path.EndsWith("/graph.json", StringComparison.Ordinal))
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                string layoutPath = graphPath.Substring(0, graphPath.Length - "graph.json".Length) + "layout.json";
                if (!TryFile(files, graphPath, report, out AgentPackageGraphFile graphFile) ||
                    !TryFile(files, layoutPath, report, out AgentPackageLayoutFile layoutFile))
                {
                    valid = false;
                    continue;
                }
                string expectedGraphPath = $"editable/graphs/{Segment(graphFile.id)}/graph.json";
                if (!string.Equals(graphPath, expectedGraphPath, StringComparison.Ordinal) ||
                    !string.Equals(layoutFile.graphId, graphFile.id, StringComparison.Ordinal))
                {
                    report.Error(graphPath, "graph_package_path_mismatch", "Graph目录、graph id与layout graphId必须一致。");
                    valid = false;
                    continue;
                }
                currentGraphs.TryGetValue(graphFile.id ?? string.Empty, out AgentSnapshotGraph currentGraph);
                if (!TryFromGraphFiles(
                        manifest.domain,
                        graphPath,
                        graphFile,
                        layoutFile,
                        currentGraph,
                        report,
                        phase,
                        out AgentSnapshotGraph graph))
                {
                    valid = false;
                    continue;
                }
                target.editable.graphs.Add(graph);
            }
            foreach (string layoutPath in files.Keys
                         .Where(path => path.StartsWith("editable/graphs/", StringComparison.Ordinal) && path.EndsWith("/layout.json", StringComparison.Ordinal)))
            {
                string graphPath = layoutPath.Substring(0, layoutPath.Length - "layout.json".Length) + "graph.json";
                if (files.ContainsKey(graphPath))
                    continue;
                report.Error(layoutPath, "graph_file_pair_missing", "Layout分片缺少同目录graph.json。");
                valid = false;
            }
            foreach (string timelinePath in files.Keys
                         .Where(path => path.StartsWith("editable/timelines/", StringComparison.Ordinal) && path.EndsWith("/timeline.json", StringComparison.Ordinal))
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                string curvesPath = timelinePath.Substring(0, timelinePath.Length - "timeline.json".Length) + "curves.json";
                if (!TryFile(files, timelinePath, report, out AgentPackageTimelineFile timelineFile) ||
                    !TryFile(files, curvesPath, report, out AgentPackageCurvesFile curvesFile))
                {
                    valid = false;
                    continue;
                }
                string expectedTimelinePath = $"editable/timelines/{Segment(timelineFile.id)}/timeline.json";
                if (!string.Equals(timelinePath, expectedTimelinePath, StringComparison.Ordinal) ||
                    !string.Equals(curvesFile.timelineId, timelineFile.id, StringComparison.Ordinal))
                {
                    report.Error(timelinePath, "timeline_package_path_mismatch", "Timeline目录、timeline id与curves timelineId必须一致。");
                    valid = false;
                    continue;
                }
                AgentSnapshotTimeline currentTimeline = current?.timelines?.FirstOrDefault(value =>
                    string.Equals(value.timelineAuthoringId, timelineFile.id, StringComparison.Ordinal));
                if (!TryFromTimelineFiles(timelinePath, timelineFile, curvesFile, currentTimeline, report, out AgentSnapshotTimeline timeline))
                {
                    valid = false;
                    continue;
                }
                target.editable.timelines.Add(timeline);
            }
            foreach (string curvesPath in files.Keys
                         .Where(path => path.StartsWith("editable/timelines/", StringComparison.Ordinal) && path.EndsWith("/curves.json", StringComparison.Ordinal)))
            {
                string timelinePath = curvesPath.Substring(0, curvesPath.Length - "curves.json".Length) + "timeline.json";
                if (files.ContainsKey(timelinePath))
                    continue;
                report.Error(curvesPath, "timeline_file_pair_missing", "Curve分片缺少同目录timeline.json。");
                valid = false;
            }

            valid &= ValidateGraphRelationships(target.editable, report);
            valid &= ValidateTimelineRelationships(target.editable, report);
            valid &= ValidatePrimaryIdentities(target.editable, report);
            if (packageAI != null && valid)
            {
                if (!TryFromPackageAI(packageAI, target.editable.graphs, report, out AgentDocumentAIEditable aiController))
                    valid = false;
                target.editable.aiController = aiController;
            }

            target.context = new AgentDocumentContext
            {
                definitionName = dependencies.definitionName,
                definitionAssetPath = dependencies.definitionAssetPath,
                rootTreeAssetPath = dependencies.rootTreeAssetPath,
                rootGraphAuthoringId = dependencies.rootGraphAuthoringId,
                inputValues = assets.inputValues ?? new List<AgentSnapshotInputValue>(),
                actionRequests = assets.actionRequests ?? new List<AgentSnapshotActionRequest>(),
                timelineAssets = assets.timelineAssets ?? new List<AgentSnapshotAsset>(),
                actionContextAssets = assets.actionContextAssets ?? new List<AgentSnapshotAsset>(),
                bodyMotion = dependencies.bodyMotion ?? new AgentSnapshotBodyMotionProfile(),
                presentation = dependencies.presentation ??
                               new AgentDocumentPresentationContext(),
                animationMarkerGroups = dependencies.animationMarkerGroups ?? new List<AgentSnapshotAnimationMarkerGroup>(),
                generatedProduct = dependencies.generatedProduct ?? new AgentDocumentGeneratedProduct(),
                aiController = dependencies.aiController,
                capabilities = dependencies.capabilities ?? new List<string>()
            };
            if (string.Equals(
                    manifest.domain,
                    AgentAuthoringSchema.CharacterControllerDomain,
                    StringComparison.Ordinal))
            {
                valid &= AgentAuthoringPresentationPackageCodec.TryReadReadonly(
                    files,
                    report,
                    out List<AgentPackageLinkedPoseInterfaceFile> linkedPoseInterfaces);
                target.context.presentation.linkedPoseInterfaces =
                    linkedPoseInterfaces;
                valid &= AgentAuthoringPresentationPackageCodec
                    .ValidateReadonlyClosure(
                        target.editable.presentation,
                        linkedPoseInterfaces,
                        report);
            }
            return valid;
        }

        static bool ValidateBlackboardPackage(
            AgentPackageBlackboardFile blackboard,
            AgentCompileReport report)
        {
            bool valid = ValidateBlackboardSchemaRevision(
                blackboard?.schemaRevision ?? 0,
                "editable/blackboard.json.schemaRevision",
                report);
            int index = 0;
            foreach (AgentSnapshotBlackboardDeclaration declaration in
                     blackboard?.declarations ?? new List<AgentSnapshotBlackboardDeclaration>())
            {
                string path = $"editable/blackboard.json.declarations[{index}]";
                if (declaration == null)
                {
                    report.Error(path, "blackboard_declaration_missing", "Blackboard declaration不能为空。");
                    valid = false;
                    index++;
                    continue;
                }
                if (declaration.inputBinding != null &&
                    string.IsNullOrWhiteSpace(declaration.inputBinding.inputValueId))
                {
                    report.Error(
                        path + ".inputBinding.inputValueId",
                        "blackboard_input_value_id_missing",
                        "Blackboard Input Binding必须提供非空inputValueId；没有绑定时应省略inputBinding。");
                    valid = false;
                }
                if (declaration.factProjection != null)
                {
                    if (!Enum.TryParse(
                            declaration.factProjection.kind,
                            false,
                            out PipelineBlackboardFactProjectionKind kind) ||
                        kind != PipelineBlackboardFactProjectionKind.ActionWindow)
                    {
                        report.Error(
                            path + ".factProjection.kind",
                            "blackboard_fact_projection_kind_invalid",
                            "Blackboard Fact Projection必须提供受支持的kind；没有投影时应省略factProjection。");
                        valid = false;
                    }
                    if (string.IsNullOrWhiteSpace(declaration.factProjection.windowType))
                    {
                        report.Error(
                            path + ".factProjection.windowType",
                            "blackboard_action_window_type_missing",
                            "ActionWindow Fact Projection必须提供windowType。");
                        valid = false;
                    }
                    if (string.IsNullOrWhiteSpace(declaration.factProjection.windowId))
                    {
                        report.Error(
                            path + ".factProjection.windowId",
                            "blackboard_action_window_id_missing",
                            "ActionWindow Fact Projection必须提供windowId。");
                        valid = false;
                    }
                }
                index++;
            }
            return valid;
        }

        static bool ValidateBlackboardSchemaRevision(
            int revision,
            string path,
            AgentCompileReport report)
        {
            if (revision == PipelineBlackboardAuthoringSchema.CurrentRevision)
                return true;
            report.Error(
                path,
                "blackboard_schema_revision_outdated",
                $"Blackboard schema revision必须是{PipelineBlackboardAuthoringSchema.CurrentRevision}；请重新checkout Document后再apply。");
            return false;
        }

        static bool ValidateTimelineRelationships(
            AgentDocumentEditable editable,
            AgentCompileReport report)
        {
            var timelines = (editable.timelines ?? new List<AgentSnapshotTimeline>())
                .Where(value => value != null && !string.IsNullOrEmpty(value.timelineAuthoringId))
                .GroupBy(value => value.timelineAuthoringId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var treeClips = new Dictionary<string, AgentSnapshotTimelineTreeClip>(StringComparer.Ordinal);
            var declarations = new HashSet<string>(
                (editable.blackboardDeclarations ?? new List<AgentSnapshotBlackboardDeclaration>())
                    .Where(value => value != null)
                    .Select(value => value.declarationId),
                StringComparer.Ordinal);
            bool valid = true;
            foreach (AgentSnapshotTimelineTreeClip treeClip in editable.timelineTreeClips ?? new List<AgentSnapshotTimelineTreeClip>())
            {
                string path = $"editable.controller.timelineTreeClips[{treeClip?.clipAuthoringId}]";
                if (treeClip == null ||
                    !IsIdentity(treeClip.clipAuthoringId) ||
                    !treeClips.TryAdd(treeClip.clipAuthoringId, treeClip) ||
                    !timelines.TryGetValue(treeClip.timelineAuthoringId ?? string.Empty, out AgentSnapshotTimeline timeline))
                {
                    report.Error(path, "timeline_tree_clip_relationship_invalid", "TreeClip identity重复、Timeline缺失或引用非法。");
                    valid = false;
                    continue;
                }
                if (!string.Equals(treeClip.ownership, TimelineTreeOwnership.Inline.ToString(), StringComparison.Ordinal) ||
                    !Enum.TryParse(treeClip.phase, false, out TimelineTreeExecutionPhase _) ||
                    treeClip.writes == null ||
                    treeClip.writes.Count > 1 ||
                    treeClip.writes.Any(write =>
                        write == null ||
                        !IsIdentity(write.declarationId) ||
                        !declarations.Contains(write.declarationId)))
                {
                    report.Error(path, "timeline_tree_clip_configuration_invalid", "TreeClip必须是Inline、使用合法phase，并且最多引用一个现有Blackboard declaration。");
                    valid = false;
                }
                AgentSnapshotTimelineTrack track = (timeline.tracks ?? new List<AgentSnapshotTimelineTrack>())
                    .FirstOrDefault(value => string.Equals(value.trackAuthoringId, treeClip.trackAuthoringId, StringComparison.Ordinal));
                AgentSnapshotTimelineClip clip = track?.clips?.FirstOrDefault(value =>
                    string.Equals(value.clipAuthoringId, treeClip.clipAuthoringId, StringComparison.Ordinal));
                if (track == null ||
                    clip == null ||
                    clip.typeName?.EndsWith("TreeClip", StringComparison.Ordinal) != true ||
                    clip.startFrame != treeClip.startFrame ||
                    clip.endFrame != treeClip.endFrame)
                {
                    report.Error(path, "timeline_tree_clip_projection_mismatch", "Controller TreeClip必须与timeline分片中的Timeline、Track、Clip和frame范围一致。");
                    valid = false;
                }
            }

            foreach (AgentSnapshotTimeline timeline in editable.timelines ?? new List<AgentSnapshotTimeline>())
            {
                foreach (AgentSnapshotTimelineTrack track in timeline?.tracks ?? new List<AgentSnapshotTimelineTrack>())
                {
                    foreach (AgentSnapshotTimelineClip clip in track?.clips ?? new List<AgentSnapshotTimelineClip>())
                    {
                        if (clip?.typeName?.EndsWith("TreeClip", StringComparison.Ordinal) == true &&
                            !treeClips.ContainsKey(clip.clipAuthoringId))
                        {
                            report.Error(
                                $"editable.timelines[{timeline.timelineAuthoringId}].tracks[{track.trackAuthoringId}].clips[{clip.clipAuthoringId}]",
                                "timeline_tree_clip_summary_missing",
                                "Timeline TreeClip缺少controller分片中的目标状态。");
                            valid = false;
                        }
                    }
                }
            }
            return valid;
        }

        static bool ValidateGraphRelationships(
            AgentDocumentEditable editable,
            AgentCompileReport report)
        {
            IReadOnlyList<AgentSnapshotGraph> graphs = editable?.graphs ?? new List<AgentSnapshotGraph>();
            var graphById = (graphs ?? Array.Empty<AgentSnapshotGraph>())
                .Where(graph => graph != null && !string.IsNullOrEmpty(graph.graphAuthoringId))
                .GroupBy(graph => graph.graphAuthoringId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var nodes = (graphs ?? Array.Empty<AgentSnapshotGraph>())
                .SelectMany(graph => graph.nodes ?? new List<AgentSnapshotNode>())
                .Where(node => node != null && !string.IsNullOrEmpty(node.elementAuthoringId))
                .GroupBy(node => node.elementAuthoringId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var flowEdges = (graphs ?? Array.Empty<AgentSnapshotGraph>())
                .SelectMany(graph => graph.flowEdges ?? new List<AgentSnapshotFlowEdge>())
                .Where(edge => edge != null && !string.IsNullOrEmpty(edge.elementAuthoringId))
                .GroupBy(edge => edge.elementAuthoringId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var ownerIds = new HashSet<string>(
                nodes.Keys.Concat(flowEdges.Keys),
                StringComparer.Ordinal);
            bool valid = true;
            foreach (AgentSnapshotGraph graph in graphs ?? Array.Empty<AgentSnapshotGraph>())
            {
                string path = $"editable.graphs[{graph.graphAuthoringId}]";
                if (!string.Equals(graph.ownership, AgentGraphOwnership.RootAsset.ToString(), StringComparison.Ordinal) &&
                    !ownerIds.Contains(graph.ownerElementAuthoringId ?? string.Empty))
                {
                    report.Error(path + ".owner", "graph_owner_unknown", $"Graph owner不在文档包entity集合：{graph.ownerElementAuthoringId}");
                    valid = false;
                }
                foreach (AgentSnapshotNode node in graph.nodes ?? new List<AgentSnapshotNode>())
                {
                    foreach (AgentSnapshotGraphReference reference in node.graphReferences ?? new List<AgentSnapshotGraphReference>())
                    {
                        if (string.IsNullOrEmpty(reference.graphAuthoringId))
                            continue;
                        if (!graphById.TryGetValue(reference.graphAuthoringId, out AgentSnapshotGraph child))
                        {
                            report.Error(path + ".nodes.graphReferences", "graph_reference_unknown", $"Graph reference不在文档包Graph集合：{reference.graphAuthoringId}");
                            valid = false;
                            continue;
                        }
                        if (!string.Equals(child.ownerElementAuthoringId, node.elementAuthoringId, StringComparison.Ordinal) ||
                            !string.Equals(child.ownership, reference.ownership, StringComparison.Ordinal))
                        {
                            report.Error(path + ".nodes.graphReferences", "graph_reference_owner_mismatch", $"Graph reference与目标Graph owner或ownership不一致：{reference.graphAuthoringId}");
                            valid = false;
                        }
                    }
                }
                foreach (AgentSnapshotFlowEdge edge in graph.flowEdges ?? new List<AgentSnapshotFlowEdge>())
                {
                    if (string.IsNullOrEmpty(edge.conditionRuleGraphAuthoringId))
                        continue;
                    if (!graphById.TryGetValue(edge.conditionRuleGraphAuthoringId, out AgentSnapshotGraph child))
                    {
                        report.Error(path + ".flowEdges.conditionGraph", "graph_reference_unknown", $"Condition Graph reference不在文档包Graph集合：{edge.conditionRuleGraphAuthoringId}");
                        valid = false;
                        continue;
                    }
                    if (!string.Equals(child.kind, AgentGraphKind.ConditionRuleGraph.ToString(), StringComparison.Ordinal) ||
                        !string.Equals(child.ownerElementAuthoringId, edge.elementAuthoringId, StringComparison.Ordinal) ||
                        !IsChildGraphOwnership(child.ownership))
                    {
                        report.Error(path + ".flowEdges.conditionGraph", "graph_reference_owner_mismatch", $"Condition Graph reference与目标Graph kind、owner或ownership不一致：{edge.conditionRuleGraphAuthoringId}");
                        valid = false;
                    }
                }
            }
            foreach (AgentSnapshotGraph graph in graphs ?? Array.Empty<AgentSnapshotGraph>())
            {
                if (string.Equals(graph.ownership, AgentGraphOwnership.RootAsset.ToString(), StringComparison.Ordinal))
                {
                    if (!string.IsNullOrEmpty(graph.ownerElementAuthoringId))
                    {
                        report.Error($"editable.graphs[{graph.graphAuthoringId}].owner", "graph_root_owner_invalid", "Root Graph不能声明owner。");
                        valid = false;
                    }
                    continue;
                }

                if (string.Equals(graph.kind, AgentGraphKind.ConditionRuleGraph.ToString(), StringComparison.Ordinal))
                {
                    List<AgentSnapshotFlowEdge> references = flowEdges.Values
                        .Where(edge => string.Equals(
                            edge.conditionRuleGraphAuthoringId,
                            graph.graphAuthoringId,
                            StringComparison.Ordinal))
                        .ToList();
                    if (references.Count == 1 &&
                        string.Equals(
                            references[0].elementAuthoringId,
                            graph.ownerElementAuthoringId,
                            StringComparison.Ordinal) &&
                        !nodes.ContainsKey(graph.ownerElementAuthoringId ?? string.Empty) &&
                        IsChildGraphOwnership(graph.ownership))
                    {
                        continue;
                    }
                    report.Error($"editable.graphs[{graph.graphAuthoringId}].owner", "graph_owner_reference_invalid", "ConditionRule Graph必须由owner FlowEdge中的唯一conditionGraph反向指向。");
                    valid = false;
                    continue;
                }

                List<(AgentSnapshotNode Owner, AgentSnapshotGraphReference Reference)> nodeReferences =
                    nodes.Values
                        .SelectMany(owner => (owner.graphReferences ?? new List<AgentSnapshotGraphReference>())
                            .Where(reference => string.Equals(
                                reference.graphAuthoringId,
                                graph.graphAuthoringId,
                                StringComparison.Ordinal))
                            .Select(reference => (owner, reference)))
                        .ToList();
                List<AgentSnapshotAuthoringRouteSegment> timelineTreeClipRoutes =
                    (graph.routes ?? new List<AgentSnapshotAuthoringRoute>())
                        .Where(route => route?.segments != null && route.segments.Count > 0)
                        .Select(route => route.segments[route.segments.Count - 1])
                        .Where(segment =>
                            segment != null &&
                            string.Equals(segment.kind, TreeAuthoringRouteSegmentKind.TimelineTreeClip.ToString(), StringComparison.Ordinal) &&
                            string.Equals(segment.childGraphAuthoringId, graph.graphAuthoringId, StringComparison.Ordinal))
                        .ToList();
                if (timelineTreeClipRoutes.Count > 0)
                {
                    if (ValidateTimelineTreeClipGraphOwner(
                            editable,
                            graph,
                            timelineTreeClipRoutes,
                            nodeReferences,
                            flowEdges,
                            nodes))
                    {
                        continue;
                    }
                    report.Error(
                        $"editable.graphs[{graph.graphAuthoringId}].owner",
                        "graph_owner_reference_invalid",
                        "TimelineTreeClip子Graph必须由唯一route、Timeline、Track、Clip与Graph owner双向一致地持有。");
                    valid = false;
                    continue;
                }
                if (nodeReferences.Count == 1 &&
                    string.Equals(
                        nodeReferences[0].Owner.elementAuthoringId,
                        graph.ownerElementAuthoringId,
                        StringComparison.Ordinal) &&
                    !flowEdges.ContainsKey(graph.ownerElementAuthoringId ?? string.Empty) &&
                    string.Equals(
                        nodeReferences[0].Reference.ownership,
                        graph.ownership,
                        StringComparison.Ordinal))
                {
                    continue;
                }
                report.Error($"editable.graphs[{graph.graphAuthoringId}].owner", "graph_owner_reference_invalid", "非ConditionRule子Graph必须由owner Node中的唯一Graph reference反向指向。");
                valid = false;
            }
            return valid;
        }

        static bool ValidateTimelineTreeClipGraphOwner(
            AgentDocumentEditable editable,
            AgentSnapshotGraph graph,
            IReadOnlyList<AgentSnapshotAuthoringRouteSegment> routes,
            IReadOnlyList<(AgentSnapshotNode Owner, AgentSnapshotGraphReference Reference)> nodeReferences,
            IReadOnlyDictionary<string, AgentSnapshotFlowEdge> flowEdges,
            IReadOnlyDictionary<string, AgentSnapshotNode> nodes)
        {
            if (routes.Count != 1 ||
                nodeReferences.Count != 0 ||
                flowEdges.Values.Any(edge => string.Equals(
                    edge.conditionRuleGraphAuthoringId,
                    graph.graphAuthoringId,
                    StringComparison.Ordinal)))
                return false;

            AgentSnapshotAuthoringRouteSegment route = routes[0];
            if (!string.Equals(route.ownerElementKind, TreeAuthoringElementKind.Node.ToString(), StringComparison.Ordinal) ||
                !string.Equals(route.ownerElementAuthoringId, graph.ownerElementAuthoringId, StringComparison.Ordinal) ||
                !string.Equals(route.referenceKey, graph.referenceKey, StringComparison.Ordinal) ||
                !string.Equals(route.referenceKey, "timeline.treeClip", StringComparison.Ordinal) ||
                !string.Equals(route.scopeId, route.clipAuthoringId, StringComparison.Ordinal) ||
                !string.Equals(RouteOwnership(route.ownership), graph.ownership, StringComparison.Ordinal) ||
                !nodes.TryGetValue(route.ownerElementAuthoringId ?? string.Empty, out AgentSnapshotNode ownerNode) ||
                !string.Equals(ownerNode.typeName, typeof(TimelineNode).FullName, StringComparison.Ordinal))
                return false;

            List<AgentSnapshotGraph> ownerGraphs = (editable.graphs ?? new List<AgentSnapshotGraph>())
                .Where(value =>
                    value != null &&
                    string.Equals(value.graphAuthoringId, route.ownerGraphAuthoringId, StringComparison.Ordinal))
                .ToList();
            if (ownerGraphs.Count != 1 ||
                (ownerGraphs[0].nodes ?? new List<AgentSnapshotNode>()).Count(value =>
                    value != null &&
                    string.Equals(value.elementAuthoringId, route.ownerElementAuthoringId, StringComparison.Ordinal)) != 1)
                return false;

            List<AgentSnapshotTimeline> timelines = (editable.timelines ?? new List<AgentSnapshotTimeline>())
                .Where(value =>
                    value != null &&
                    string.Equals(value.timelineAuthoringId, route.timelineAuthoringId, StringComparison.Ordinal))
                .ToList();
            if (timelines.Count != 1 ||
                (timelines[0].callSites ?? new List<AgentSnapshotTimelineCallSite>()).Count(value =>
                    value != null &&
                    string.Equals(value.nodeAuthoringId, route.ownerElementAuthoringId, StringComparison.Ordinal)) != 1)
                return false;

            List<AgentSnapshotTimelineTrack> tracks = (timelines[0].tracks ?? new List<AgentSnapshotTimelineTrack>())
                .Where(value =>
                    value != null &&
                    string.Equals(value.trackAuthoringId, route.trackAuthoringId, StringComparison.Ordinal))
                .ToList();
            List<AgentSnapshotTimelineClip> clips = (tracks.Count == 1
                    ? tracks[0].clips ?? new List<AgentSnapshotTimelineClip>()
                    : new List<AgentSnapshotTimelineClip>())
                .Where(value =>
                    value != null &&
                    string.Equals(value.clipAuthoringId, route.clipAuthoringId, StringComparison.Ordinal))
                .ToList();
            if (tracks.Count != 1 ||
                clips.Count != 1 ||
                clips[0].typeName?.EndsWith("TreeClip", StringComparison.Ordinal) != true)
                return false;

            List<AgentSnapshotTimelineTreeClip> summaries =
                (editable.timelineTreeClips ?? new List<AgentSnapshotTimelineTreeClip>())
                    .Where(value =>
                        value != null &&
                        string.Equals(value.timelineAuthoringId, route.timelineAuthoringId, StringComparison.Ordinal) &&
                        string.Equals(value.trackAuthoringId, route.trackAuthoringId, StringComparison.Ordinal) &&
                        string.Equals(value.clipAuthoringId, route.clipAuthoringId, StringComparison.Ordinal))
                    .ToList();
            return summaries.Count == 1 &&
                   string.Equals(
                       TimelineTreeOwnershipToGraphOwnership(summaries[0].ownership),
                       graph.ownership,
                       StringComparison.Ordinal) &&
                   summaries[0].startFrame == clips[0].startFrame &&
                   summaries[0].endFrame == clips[0].endFrame;
        }

        static string RouteOwnership(string ownership)
        {
            if (string.Equals(ownership, TreeGraphReferenceOwnership.Inline.ToString(), StringComparison.Ordinal))
                return AgentGraphOwnership.Inline.ToString();
            if (string.Equals(ownership, TreeGraphReferenceOwnership.Shared.ToString(), StringComparison.Ordinal))
                return AgentGraphOwnership.SharedAsset.ToString();
            return AgentGraphOwnership.Unknown.ToString();
        }

        static string TimelineTreeOwnershipToGraphOwnership(string ownership)
        {
            if (string.Equals(ownership, TimelineTreeOwnership.Inline.ToString(), StringComparison.Ordinal))
                return AgentGraphOwnership.Inline.ToString();
            if (string.Equals(ownership, TimelineTreeOwnership.Shared.ToString(), StringComparison.Ordinal))
                return AgentGraphOwnership.SharedAsset.ToString();
            return AgentGraphOwnership.Unknown.ToString();
        }

        static bool IsChildGraphOwnership(string ownership)
        {
            return string.Equals(ownership, AgentGraphOwnership.Inline.ToString(), StringComparison.Ordinal) ||
                   string.Equals(ownership, AgentGraphOwnership.SharedAsset.ToString(), StringComparison.Ordinal);
        }

        static bool ValidatePrimaryIdentities(AgentDocumentEditable editable, AgentCompileReport report)
        {
            var owners = new Dictionary<string, string>(StringComparer.Ordinal);
            bool valid = true;
            void Add(string identity, string path)
            {
                if (!IsIdentity(identity))
                {
                    report.Error(path, "entity_identity_invalid", "Entity identity缺失或local语法非法。");
                    valid = false;
                    return;
                }
                if (owners.TryGetValue(identity, out string existing))
                {
                    report.Error(path, "entity_identity_duplicate", $"Entity identity与{existing}重复：{identity}");
                    valid = false;
                    return;
                }
                owners.Add(identity, path);
            }

            foreach (AgentSnapshotGraph graph in editable.graphs ?? new List<AgentSnapshotGraph>())
            {
                string graphPath = $"editable.graphs[{graph.graphAuthoringId}]";
                Add(graph.graphAuthoringId, graphPath);
                foreach (AgentSnapshotNode node in graph.nodes ?? new List<AgentSnapshotNode>())
                {
                    if (node?.elementAuthoringId?.StartsWith("@", StringComparison.Ordinal) != true)
                        Add(node?.elementAuthoringId, graphPath + ".nodes");
                }
                foreach (AgentSnapshotFlowEdge edge in graph.flowEdges ?? new List<AgentSnapshotFlowEdge>())
                    Add(edge?.elementAuthoringId, graphPath + ".flowEdges");
                foreach (AgentSnapshotPropertyEdge edge in graph.propertyEdges ?? new List<AgentSnapshotPropertyEdge>())
                    Add(edge?.elementAuthoringId, graphPath + ".propertyEdges");
            }
            foreach (AgentSnapshotBlackboardDeclaration declaration in editable.blackboardDeclarations ?? new List<AgentSnapshotBlackboardDeclaration>())
                Add(declaration?.declarationId, "editable.blackboard.declarations");
            foreach (AgentSnapshotAIBlackboardDeclaration declaration in editable.aiController?.blackboardDeclarations ?? new List<AgentSnapshotAIBlackboardDeclaration>())
                Add(declaration?.declarationAuthoringId, "editable.ai.blackboard");
            foreach (AgentSnapshotTimeline timeline in editable.timelines ?? new List<AgentSnapshotTimeline>())
            {
                string timelinePath = $"editable.timelines[{timeline.timelineAuthoringId}]";
                Add(timeline.timelineAuthoringId, timelinePath);
                foreach (AgentSnapshotTimelineTrack track in timeline.tracks ?? new List<AgentSnapshotTimelineTrack>())
                {
                    Add(track?.trackAuthoringId, timelinePath + ".tracks");
                    foreach (AgentSnapshotAnimationMarker marker in track?.markers ?? new List<AgentSnapshotAnimationMarker>())
                        Add(marker?.authoringId, timelinePath + ".markers");
                    foreach (AgentSnapshotTimelineClip clip in track?.clips ?? new List<AgentSnapshotTimelineClip>())
                        Add(clip?.clipAuthoringId, timelinePath + ".clips");
                }
            }
            return valid;
        }

        static AgentPackageAIController ToPackageAI(AgentDocumentAIEditable source)
        {
            return new AgentPackageAIController
            {
                controllerId = source.controllerId,
                definitionAssetPath = source.definitionAssetPath,
                definitionAssetGuid = source.definitionAssetGuid,
                treeAssetPath = source.treeAssetPath,
                treeAssetGuid = source.treeAssetGuid,
                graphAuthoringId = source.graphAuthoringId,
                authoringRole = source.authoringRole,
                perceptionAssetPath = source.perceptionAssetPath,
                perceptionAssetGuid = source.perceptionAssetGuid,
                candidateOrdering = source.candidateOrdering,
                candidateActorIds = source.candidateActorIds,
                controlledCharacterAssetPath = source.controlledCharacterAssetPath,
                controlledCharacterAssetGuid = source.controlledCharacterAssetGuid,
                blackboard = (source.blackboardDeclarations ?? new List<AgentSnapshotAIBlackboardDeclaration>())
                    .Select(value => new AgentPackageAIBlackboardDeclaration
                    {
                        id = value.declarationAuthoringId,
                        key = value.displayName,
                        valueType = StableValueType(value.valueType),
                        scope = value.scope,
                        defaultValue = value.defaultValue
                    }).ToList(),
                nodes = (source.nodes ?? new List<AgentSnapshotAINode>())
                    .Select(value => new AgentPackageAINodeConfiguration
                    {
                        id = value.nodeAuthoringId,
                        memoryValueKind = value.memoryValueKind,
                        memoryDeclarationId = value.memoryDeclarationAuthoringId,
                        inputId = value.inputId,
                        requestId = value.requestId,
                        requestBufferSeconds = value.requestBufferSeconds,
                        requestPriority = value.requestPriority,
                        requestRepeatPolicy = value.requestRepeatPolicy
                    }).ToList()
            };
        }

        static bool TryFromPackageAI(
            AgentPackageAIController source,
            IReadOnlyList<AgentSnapshotGraph> graphs,
            AgentCompileReport report,
            out AgentDocumentAIEditable target)
        {
            target = new AgentDocumentAIEditable
            {
                controllerId = source.controllerId,
                definitionAssetPath = source.definitionAssetPath,
                definitionAssetGuid = source.definitionAssetGuid,
                treeAssetPath = source.treeAssetPath,
                treeAssetGuid = source.treeAssetGuid,
                graphAuthoringId = source.graphAuthoringId,
                authoringRole = source.authoringRole,
                perceptionAssetPath = source.perceptionAssetPath,
                perceptionAssetGuid = source.perceptionAssetGuid,
                candidateOrdering = source.candidateOrdering,
                candidateActorIds = source.candidateActorIds ?? new List<string>(),
                controlledCharacterAssetPath = source.controlledCharacterAssetPath,
                controlledCharacterAssetGuid = source.controlledCharacterAssetGuid,
                blackboardDeclarations = (source.blackboard ?? new List<AgentPackageAIBlackboardDeclaration>())
                    .Select(value => new AgentSnapshotAIBlackboardDeclaration
                    {
                        declarationAuthoringId = value.id,
                        ownerGraphAuthoringId = source.graphAuthoringId,
                        displayName = value.key,
                        valueType = InternalValueType(value.valueType),
                        scope = value.scope,
                        lifetime = ResolveAIDefaultLifetime(value.scope),
                        defaultValue = value.defaultValue
                    }).ToList()
            };
            var graphNodes = (graphs ?? Array.Empty<AgentSnapshotGraph>())
                .SelectMany(graph => graph.nodes ?? new List<AgentSnapshotNode>())
                .Where(node => node != null && !string.IsNullOrEmpty(node.elementAuthoringId))
                .ToDictionary(node => node.elementAuthoringId, node => node, StringComparer.Ordinal);
            var configurationIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageAINodeConfiguration configuration in source.nodes ?? new List<AgentPackageAINodeConfiguration>())
            {
                if (configuration == null ||
                    string.IsNullOrEmpty(configuration.id) ||
                    !configurationIds.Add(configuration.id) ||
                    !graphNodes.TryGetValue(configuration.id, out AgentSnapshotNode graphNode))
                {
                    report.Error("editable/ai/perception.json.nodes", "ai_node_graph_reference_invalid", $"AI node配置没有对应Graph Node：{configuration?.id}");
                    return false;
                }
                target.nodes.Add(new AgentSnapshotAINode
                {
                    graphAuthoringId = source.graphAuthoringId,
                    nodeAuthoringId = configuration.id,
                    nodeType = graphNode.typeName,
                    memoryValueKind = configuration.memoryValueKind,
                    memoryDeclarationAuthoringId = configuration.memoryDeclarationId,
                    inputId = configuration.inputId,
                    requestId = configuration.requestId,
                    requestBufferSeconds = configuration.requestBufferSeconds,
                    requestPriority = configuration.requestPriority,
                    requestRepeatPolicy = configuration.requestRepeatPolicy
                });
            }
            AgentSnapshotGraph rootGraph = (graphs ?? Array.Empty<AgentSnapshotGraph>())
                .FirstOrDefault(graph => string.Equals(graph.graphAuthoringId, source.graphAuthoringId, StringComparison.Ordinal));
            var catalog = new BtsmtlGraphAuthoringCapabilities();
            string missing = (rootGraph?.nodes ?? new List<AgentSnapshotNode>())
                .Where(node => node != null &&
                               !catalog.TryGetAnchor(node.typeName, out _) &&
                               !configurationIds.Contains(node.elementAuthoringId))
                .Select(node => node.elementAuthoringId)
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(missing))
            {
                report.Error("editable/ai/perception.json.nodes", "ai_node_configuration_missing", $"AI root Graph Node缺少配置记录：{missing}");
                return false;
            }
            return true;
        }

        static string ResolveAIDefaultLifetime(string scope)
        {
            return Enum.TryParse(scope, false, out PipelineBlackboardVariableScope parsed)
                ? PipelineBlackboardVariablePolicy.DefaultLifetime(parsed).ToString()
                : string.Empty;
        }

        static string StableValueType(string value)
        {
            if (string.Equals(value, typeof(bool).FullName, StringComparison.Ordinal)) return "bool";
            if (string.Equals(value, typeof(int).FullName, StringComparison.Ordinal)) return "int";
            if (string.Equals(value, typeof(float).FullName, StringComparison.Ordinal)) return "float";
            if (string.Equals(value, typeof(string).FullName, StringComparison.Ordinal)) return "string";
            if (string.Equals(value, typeof(UnityEngine.Vector2).FullName, StringComparison.Ordinal)) return "vector2";
            if (string.Equals(value, typeof(UnityEngine.Vector3).FullName, StringComparison.Ordinal)) return "vector3";
            if (string.Equals(value, typeof(ThirdPersonCharacter.AI.AIActionTargetSnapshotValue).FullName, StringComparison.Ordinal)) return "aiActionTargetSnapshot";
            int separator = value?.LastIndexOf('.') ?? -1;
            return separator >= 0 ? value.Substring(separator + 1) : value;
        }

        static string InternalValueType(string value)
        {
            switch (value?.Trim().ToLowerInvariant())
            {
                case "bool":
                case "boolean":
                    return typeof(bool).FullName;
                case "int":
                case "int32":
                    return typeof(int).FullName;
                case "float":
                case "single":
                    return typeof(float).FullName;
                case "string":
                    return typeof(string).FullName;
                case "vector2":
                    return typeof(UnityEngine.Vector2).FullName;
                case "vector3":
                    return typeof(UnityEngine.Vector3).FullName;
                case "aiactiontargetsnapshot":
                    return typeof(ThirdPersonCharacter.AI.AIActionTargetSnapshotValue).FullName;
                default:
                    return value;
            }
        }

        bool TryToGraphFiles(
            string domain,
            AgentSnapshotGraph graph,
            AgentCompileReport report,
            out AgentPackageGraphFile graphFile,
            out AgentPackageLayoutFile layoutFile)
        {
            graphFile = new AgentPackageGraphFile
            {
                id = graph.graphAuthoringId,
                kind = graph.kind,
                ownership = graph.ownership,
                owner = string.IsNullOrEmpty(graph.ownerElementAuthoringId) && string.IsNullOrEmpty(graph.referenceKey)
                    ? null
                    : new AgentPackageGraphOwner
                    {
                        entityId = graph.ownerElementAuthoringId,
                        slot = OwnerSlot(graph.kind)
                    },
                sharedAssetPath = graph.sharedAssetPath
            };
            layoutFile = new AgentPackageLayoutFile { graphId = graph.graphAuthoringId };
            var nodeNames = new Dictionary<string, string>(StringComparer.Ordinal);
            bool valid = true;
            foreach (AgentSnapshotNode node in graph.nodes ?? new List<AgentSnapshotNode>())
            {
                if (!m_Catalog.TryGetKind(node.typeName, out string kind))
                {
                    report.Error($"editable/graphs/{graph.graphAuthoringId}/nodes/{node.elementAuthoringId}", "authoring_capability_incomplete", $"Node类型没有完整authoring capability：{node.typeName}");
                    valid = false;
                    continue;
                }
                if (m_Catalog.TryGetAnchor(node.typeName, out string anchor))
                {
                    nodeNames[node.elementAuthoringId] = anchor;
                    continue;
                }
                if (!m_Catalog.IsNodeTypeAllowed(node.typeName, domain))
                {
                    report.Error(
                        $"editable/graphs/{graph.graphAuthoringId}/nodes/{node.elementAuthoringId}",
                        "node_domain_forbidden",
                        $"{domain}不允许Node kind：{kind}");
                    valid = false;
                    continue;
                }
                if (!m_Catalog.IsFullyRoundTrippable(kind))
                {
                    report.Error(
                        $"editable/graphs/{graph.graphAuthoringId}/nodes/{node.elementAuthoringId}",
                        "authoring_capability_incomplete",
                        $"Node kind没有完整create/configure/delete闭包：{kind}");
                    valid = false;
                    continue;
                }
                nodeNames[node.elementAuthoringId] = node.elementAuthoringId;
                var properties = new JObject();
                string nodePath = $"editable/graphs/{graph.graphAuthoringId}/nodes/{node.elementAuthoringId}";
                bool editGraphReferences = m_Catalog.CanEditProperty(kind, "graphReferences");
                bool editAssetReferences = m_Catalog.CanEditProperty(kind, "assetReferences");
                if (editGraphReferences &&
                    (node.graphReferences ?? new List<AgentSnapshotGraphReference>())
                    .Any(reference => reference.required && string.IsNullOrEmpty(reference.graphAuthoringId)))
                {
                    report.Error(nodePath, "authoring_capability_incomplete", "必需Graph reference当前没有可往返目标。");
                    valid = false;
                }
                if (editAssetReferences &&
                    (node.assetReferences ?? new List<AgentSnapshotAssetReference>())
                    .Any(reference => reference.required && string.IsNullOrEmpty(reference.assetPath) && string.IsNullOrEmpty(reference.assetGuid)))
                {
                    report.Error(nodePath, "authoring_capability_incomplete", "必需Asset reference当前没有可往返目标。");
                    valid = false;
                }
                if (editGraphReferences)
                {
                    Add(properties, "graphReferences", (node.graphReferences ?? new List<AgentSnapshotGraphReference>())
                        .Where(reference => !string.IsNullOrEmpty(reference.graphAuthoringId))
                        .Select(reference => new AgentPackageGraphReference
                        {
                            key = reference.key,
                            graphId = reference.graphAuthoringId,
                            ownership = reference.ownership,
                            sharedAssetPath = reference.sharedAssetPath
                        }).ToList());
                }
                if (editAssetReferences)
                {
                    Add(properties, "assetReferences", (node.assetReferences ?? new List<AgentSnapshotAssetReference>())
                        .Where(reference => !string.IsNullOrEmpty(reference.assetPath) || !string.IsNullOrEmpty(reference.assetGuid))
                        .Select(reference => new AgentPackageAssetReference
                        {
                            key = reference.key,
                            assetPath = reference.assetPath,
                            assetGuid = reference.assetGuid
                        }).ToList());
                }
                if (node.exposedProperty != null)
                {
                    Add(properties, "exposedProperty", new AgentPackageExposedProperty
                    {
                        mode = node.exposedProperty.mode,
                        declarationId = node.exposedProperty.declarationAuthoringId,
                        valueType = node.exposedProperty.valueType,
                        value = node.exposedProperty.value?.DeepClone()
                    });
                }
                if (!string.Equals(node.loopStopType, LoopNode.StopType.None.ToString(), StringComparison.Ordinal))
                    Add(properties, "loopStopType", node.loopStopType);
                if (!string.Equals(node.compareType, CompareNode.CompareType.Equal.ToString(), StringComparison.Ordinal))
                    Add(properties, "compareType", node.compareType);
                if (m_Catalog.CanEditProperty(kind, "moveSpeed"))
                    Add(properties, "moveSpeed", node.moveSpeed);
                if (m_Catalog.CanEditProperty(kind, "displacementMode"))
                    Add(properties, "displacementMode", node.displacementMode);
                if (m_Catalog.CanEditProperty(kind, "turnSpeedDegrees"))
                    Add(properties, "turnSpeedDegrees", node.turnSpeedDegrees);
                if (m_Catalog.CanEditProperty(kind, "cameraRelative"))
                    Add(properties, "cameraRelative", node.cameraRelative);
                if (m_Catalog.CanEditProperty(kind, "executionMode"))
                    Add(properties, "executionMode", node.executionMode);
                if (m_Catalog.CanEditProperty(kind, "durationSeconds"))
                    Add(properties, "durationSeconds", node.durationSeconds);
                if (m_Catalog.CanEditProperty(kind, "inputId"))
                    Add(properties, "inputId", node.inputId);
                if (m_Catalog.CanEditProperty(kind, "requestId"))
                    Add(properties, "requestId", node.requestId);
                if (m_Catalog.CanEditProperty(kind, "blackboardDeclarationId"))
                    Add(properties, "blackboardDeclarationId", node.blackboardDeclarationId);
                if (m_Catalog.CanEditProperty(kind, "stateExitCause"))
                    Add(properties, "stateExitCause", node.stateExitCause);
                if (m_Catalog.CanEditProperty(kind, "actionContextId"))
                    properties["actionContextId"] = node.actionContextId ?? string.Empty;
                if (m_Catalog.CanEditProperty(kind, "windowType"))
                    Add(properties, "windowType", node.windowType);
                if (m_Catalog.CanEditProperty(kind, "actionProfileId"))
                    Add(properties, "actionProfileId", node.actionProfileId);
                if (m_Catalog.CanEditProperty(kind, "targetSnapshotBlackboardDeclarationId"))
                    Add(properties, "targetSnapshotBlackboardDeclarationId", node.targetSnapshotBlackboardDeclarationId);
                graphFile.nodes.Add(new AgentPackageNode
                {
                    id = node.elementAuthoringId,
                    kind = kind,
                    name = string.Equals(node.displayName, node.nodeTypeDisplayName, StringComparison.Ordinal) ? null : node.displayName,
                    properties = properties.HasValues ? properties : null
                });
                if (node.position != null)
                {
                    layoutFile.nodes.Add(new AgentPackageNodeLayout
                    {
                        id = node.elementAuthoringId,
                        x = node.position.x,
                        y = node.position.y
                    });
                }
            }

            foreach (AgentSnapshotFlowEdge edge in graph.flowEdges ?? new List<AgentSnapshotFlowEdge>())
            {
                graphFile.flowEdges.Add(new AgentPackageFlowEdge
                {
                    id = edge.elementAuthoringId,
                    from = new AgentPackageEdgeEndpoint
                    {
                        node = ResolveEndpoint(nodeNames, edge.startElementAuthoringId),
                        port = edge.startPort
                    },
                    to = new AgentPackageEdgeEndpoint
                    {
                        node = ResolveEndpoint(nodeNames, edge.endElementAuthoringId),
                        port = edge.endPort
                    },
                    flowOrder = edge.flowOrder,
                    transitionPriority = edge.transitionPriority,
                    abortPolicy = edge.abortPolicy,
                    conditionGraph = edge.conditionRuleGraphAuthoringId
                });
            }
            foreach (AgentSnapshotPropertyEdge edge in graph.propertyEdges ?? new List<AgentSnapshotPropertyEdge>())
            {
                graphFile.propertyEdges.Add(new AgentPackagePropertyEdge
                {
                    id = edge.elementAuthoringId,
                    from = new AgentPackageEdgeEndpoint
                    {
                        node = ResolveEndpoint(nodeNames, edge.startElementAuthoringId),
                        port = edge.startPortId
                    },
                    to = new AgentPackageEdgeEndpoint
                    {
                        node = ResolveEndpoint(nodeNames, edge.endElementAuthoringId),
                        port = edge.endPortId
                    }
                });
            }
            return valid;
        }

        bool TryFromGraphFiles(
            string domain,
            string path,
            AgentPackageGraphFile graphFile,
            AgentPackageLayoutFile layoutFile,
            AgentSnapshotGraph current,
            AgentCompileReport report,
            AgentAuthoringDocumentReadPhase phase,
            out AgentSnapshotGraph graph)
        {
            graph = new AgentSnapshotGraph
            {
                graphAuthoringId = graphFile.id,
                path = current?.path,
                name = current?.name,
                kind = graphFile.kind,
                ownership = graphFile.ownership,
                ownerElementAuthoringId = graphFile.owner?.entityId,
                referenceKey = current?.referenceKey ?? graphFile.owner?.slot,
                sharedAssetPath = graphFile.sharedAssetPath,
                routes = current?.routes ?? new List<AgentSnapshotAuthoringRoute>()
            };
            if (string.IsNullOrWhiteSpace(graph.graphAuthoringId) ||
                string.IsNullOrWhiteSpace(graph.kind) ||
                graphFile.nodes == null ||
                graphFile.flowEdges == null ||
                graphFile.propertyEdges == null)
            {
                report.Error(path, "graph_required_field_missing", "Graph缺少id、kind、nodes或edges。");
                return false;
            }
            if (!Enum.TryParse(
                    graphFile.ownership,
                    false,
                    out AgentGraphOwnership ownership) ||
                ownership == AgentGraphOwnership.Unknown ||
                ownership == AgentGraphOwnership.RootAsset && graphFile.owner != null ||
                ownership != AgentGraphOwnership.RootAsset && graphFile.owner == null ||
                ownership == AgentGraphOwnership.SharedAsset && string.IsNullOrWhiteSpace(graphFile.sharedAssetPath) ||
                ownership != AgentGraphOwnership.SharedAsset && !string.IsNullOrEmpty(graphFile.sharedAssetPath))
            {
                report.Error(path + ".ownership", "graph_ownership_invalid", "Graph ownership、owner与sharedAssetPath组合不符合合同。");
                return false;
            }
            if (!ValidateGraphFile(domain, path, graphFile, layoutFile, current, phase, report))
                return false;

            var positions = (layoutFile.nodes ?? new List<AgentPackageNodeLayout>())
                .Where(value => value != null && !string.IsNullOrEmpty(value.id))
                .ToDictionary(value => value.id, value => value, StringComparer.Ordinal);
            var currentNodes = (current?.nodes ?? new List<AgentSnapshotNode>())
                .Where(node => node != null && !string.IsNullOrEmpty(node.elementAuthoringId))
                .ToDictionary(node => node.elementAuthoringId, node => node, StringComparer.Ordinal);
            Dictionary<string, AgentPackageNodeLayout> generatedPositions =
                BuildGeneratedPositions(graphFile, positions, currentNodes);
            var anchors = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (AgentSnapshotNode node in current?.nodes ?? new List<AgentSnapshotNode>())
            {
                if (!m_Catalog.TryGetAnchor(node.typeName, out string anchor))
                    continue;
                anchors[anchor] = node.elementAuthoringId;
                graph.nodes.Add(AgentAuthoringDocumentCodec.Clone(node));
            }

            for (int index = 0; index < graphFile.nodes.Count; index++)
            {
                AgentPackageNode packageNode = graphFile.nodes[index];
                string nodePath = $"{path}.nodes[{packageNode?.id}]";
                currentNodes.TryGetValue(packageNode?.id ?? string.Empty, out AgentSnapshotNode currentNode);
                if (packageNode == null ||
                    string.IsNullOrWhiteSpace(packageNode.id) ||
                    string.IsNullOrWhiteSpace(packageNode.kind) ||
                    !m_Catalog.TryGetTypeName(packageNode.kind, out string typeName))
                    return false;
                if (m_Catalog.IsSystemKind(packageNode.kind))
                {
                    report.Error(nodePath, "system_node_not_editable", "系统Node必须使用anchor，不能出现在nodes集合。");
                    return false;
                }
                AgentPackageNodeLayout position = positions.TryGetValue(packageNode.id, out AgentPackageNodeLayout explicitPosition)
                    ? explicitPosition
                    : currentNode?.position != null
                        ? new AgentPackageNodeLayout
                        {
                            id = packageNode.id,
                            x = currentNode.position.x,
                            y = currentNode.position.y
                        }
                        : generatedPositions[packageNode.id];
                JObject properties = packageNode.properties ?? new JObject();
                graph.nodes.Add(new AgentSnapshotNode
                {
                    elementAuthoringId = packageNode.id,
                    typeName = typeName,
                    displayName = string.IsNullOrEmpty(packageNode.name)
                        ? currentNode?.nodeTypeDisplayName ?? packageNode.kind
                        : packageNode.name,
                    nodeTypeDisplayName = currentNode?.nodeTypeDisplayName ?? packageNode.kind,
                    position = new AgentSnapshotVector2 { x = position.x, y = position.y },
                    graphReferences = m_Catalog.CanEditProperty(packageNode.kind, "graphReferences")
                        ? MergeGraphReferences(
                            ReadList<AgentPackageGraphReference>(properties, "graphReferences"),
                            currentNode)
                        : AgentAuthoringDocumentCodec.Clone(currentNode?.graphReferences ?? new List<AgentSnapshotGraphReference>()),
                    assetReferences = m_Catalog.CanEditProperty(packageNode.kind, "assetReferences")
                        ? MergeAssetReferences(
                            ReadList<AgentPackageAssetReference>(properties, "assetReferences"),
                            currentNode)
                        : AgentAuthoringDocumentCodec.Clone(currentNode?.assetReferences ?? new List<AgentSnapshotAssetReference>()),
                    propertyPorts = currentNode?.propertyPorts ?? new List<AgentSnapshotPropertyPort>(),
                    exposedProperty = ToExposedProperty(
                        properties["exposedProperty"]?.ToObject<AgentPackageExposedProperty>(),
                        currentNode),
                    loopStopType = string.Equals(packageNode.kind, "loop", StringComparison.Ordinal)
                        ? properties.Value<string>("loopStopType") ?? LoopNode.StopType.None.ToString()
                        : null,
                    compareType = string.Equals(packageNode.kind, "compare", StringComparison.Ordinal)
                        ? properties.Value<string>("compareType") ?? CompareNode.CompareType.Equal.ToString()
                        : null,
                    moveSpeed = m_Catalog.CanEditProperty(packageNode.kind, "moveSpeed")
                        ? properties.Value<float>("moveSpeed")
                        : 0f,
                    displacementMode = m_Catalog.CanEditProperty(packageNode.kind, "displacementMode")
                        ? properties.Value<string>("displacementMode")
                        : null,
                    turnSpeedDegrees = m_Catalog.CanEditProperty(packageNode.kind, "turnSpeedDegrees")
                        ? properties.Value<float>("turnSpeedDegrees")
                        : 0f,
                    cameraRelative = m_Catalog.CanEditProperty(packageNode.kind, "cameraRelative") &&
                                     properties.Value<bool>("cameraRelative"),
                    executionMode = m_Catalog.CanEditProperty(packageNode.kind, "executionMode")
                        ? properties.Value<string>("executionMode")
                        : null,
                    durationSeconds = m_Catalog.CanEditProperty(packageNode.kind, "durationSeconds")
                        ? properties.Value<float>("durationSeconds")
                        : 0f,
                    inputId = m_Catalog.CanEditProperty(packageNode.kind, "inputId")
                        ? properties.Value<string>("inputId")
                        : null,
                    requestId = m_Catalog.CanEditProperty(packageNode.kind, "requestId")
                        ? properties.Value<string>("requestId")
                        : null,
                    blackboardDeclarationId = m_Catalog.CanEditProperty(packageNode.kind, "blackboardDeclarationId")
                        ? properties.Value<string>("blackboardDeclarationId")
                        : null,
                    stateExitCause = m_Catalog.CanEditProperty(packageNode.kind, "stateExitCause")
                        ? properties.Value<string>("stateExitCause")
                        : null,
                    actionContextId = m_Catalog.CanEditProperty(packageNode.kind, "actionContextId")
                        ? properties.Value<string>("actionContextId")
                        : null,
                    windowType = m_Catalog.CanEditProperty(packageNode.kind, "windowType")
                        ? properties.Value<string>("windowType")
                        : null,
                    actionProfileId = m_Catalog.CanEditProperty(packageNode.kind, "actionProfileId")
                        ? properties.Value<string>("actionProfileId")
                        : null,
                    targetSnapshotBlackboardDeclarationId = m_Catalog.CanEditProperty(packageNode.kind, "targetSnapshotBlackboardDeclarationId")
                        ? properties.Value<string>("targetSnapshotBlackboardDeclarationId")
                        : null
                });
            }

            foreach (AgentPackageFlowEdge packageEdge in graphFile.flowEdges)
            {
                if (!TryEndpoint(packageEdge?.from, anchors, path, report, out string from) ||
                    !TryEndpoint(packageEdge?.to, anchors, path, report, out string to))
                    return false;
                graph.flowEdges.Add(new AgentSnapshotFlowEdge
                {
                    elementAuthoringId = packageEdge.id,
                    startElementAuthoringId = from,
                    endElementAuthoringId = to,
                    startPort = packageEdge.from.port,
                    endPort = packageEdge.to.port,
                    flowOrder = packageEdge.flowOrder,
                    transitionPriority = packageEdge.transitionPriority,
                    abortPolicy = packageEdge.abortPolicy,
                    conditionRuleGraphAuthoringId = packageEdge.conditionGraph
                });
            }
            foreach (AgentPackagePropertyEdge packageEdge in graphFile.propertyEdges)
            {
                if (!TryEndpoint(packageEdge?.from, anchors, path, report, out string from) ||
                    !TryEndpoint(packageEdge?.to, anchors, path, report, out string to))
                    return false;
                graph.propertyEdges.Add(new AgentSnapshotPropertyEdge
                {
                    elementAuthoringId = packageEdge.id,
                    startElementAuthoringId = from,
                    endElementAuthoringId = to,
                    startPortId = packageEdge.from.port,
                    endPortId = packageEdge.to.port
                });
            }
            return true;
        }

        bool ValidateGraphFile(
            string domain,
            string path,
            AgentPackageGraphFile graphFile,
            AgentPackageLayoutFile layoutFile,
            AgentSnapshotGraph current,
            AgentAuthoringDocumentReadPhase phase,
            AgentCompileReport report)
        {
            if (!IsIdentity(graphFile.id) || !m_Catalog.IsGraphKindAllowed(graphFile.kind, domain))
            {
                report.Error(path, "graph_identity_or_kind_invalid", "Graph id非法，或kind不属于当前domain。");
                return false;
            }
            if (current != null && !string.Equals(current.kind, graphFile.kind, StringComparison.Ordinal))
            {
                report.Error(path + ".kind", "graph_kind_changed", "已有Graph不能原地改变kind，必须删除旧identity并创建新local identity。");
                return false;
            }
            if (graphFile.owner != null &&
                (!IsIdentity(graphFile.owner.entityId) || !m_Catalog.IsOwnerSlotAllowed(graphFile.kind, graphFile.owner.slot)))
            {
                report.Error(path + ".owner", "graph_owner_invalid", "Graph owner identity或slot不符合Graph kind合同。");
                return false;
            }

            var nodes = new Dictionary<string, AgentPackageNode>(StringComparer.Ordinal);
            foreach (AgentPackageNode node in graphFile.nodes)
            {
                if (node == null || !IsIdentity(node.id) || nodes.ContainsKey(node.id))
                {
                    report.Error(path + ".nodes", "node_identity_invalid", "Node identity缺失、重复或local语法非法。");
                    return false;
                }
                if (!m_Catalog.IsNodeAllowed(node.kind, graphFile.kind, domain))
                {
                    report.Error(path + $".nodes[{node.id}].kind", "node_kind_not_allowed", $"{domain}/{graphFile.kind}不允许Node kind：{node.kind}");
                    return false;
                }
                AgentSnapshotNode oldNode = current?.nodes?.FirstOrDefault(value =>
                    string.Equals(value.elementAuthoringId, node.id, StringComparison.Ordinal));
                bool allowExistingEmptyActionContext = AllowsExistingEmptyActionContext(
                    phase,
                    node,
                    oldNode);
                if (!m_Catalog.ValidateProperties(
                        node.kind,
                        node.properties,
                        report,
                        path + $".nodes[{node.id}]",
                        allowExistingEmptyActionContext))
                    return false;
                if (!m_Catalog.TryProjectDocumentPortShape(
                        node.kind,
                        node.properties,
                        out _,
                        out _,
                        out GraphAuthoringPortShapeException shapeError))
                {
                    report.Error(
                        path + $".nodes[{node.id}].properties",
                        shapeError.Code,
                        shapeError.Message);
                    return false;
                }
                if (string.Equals(node.kind, "exposed-property", StringComparison.Ordinal))
                {
                    JToken exposedToken = node.properties?["exposedProperty"];
                    string declarationId = exposedToken?["declarationId"]?.Value<string>();
                    if (exposedToken is not JObject || !IsIdentity(declarationId))
                    {
                        report.Error(
                            path + $".nodes[{node.id}].properties.exposedProperty",
                            "exposed_property_required",
                            "exposed-property必须声明有效mode、declarationId、valueType与模式匹配的value。");
                        return false;
                    }
                }
                if (oldNode != null &&
                    m_Catalog.TryGetKind(oldNode.typeName, out string oldKind) &&
                    !string.Equals(oldKind, node.kind, StringComparison.Ordinal))
                {
                    report.Error(path + $".nodes[{node.id}].kind", "node_kind_changed", "已有Node不能原地改变kind，必须删除旧identity并创建新local identity。");
                    return false;
                }
                nodes.Add(node.id, node);
            }

            var layoutIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageNodeLayout layout in layoutFile.nodes ?? new List<AgentPackageNodeLayout>())
            {
                if (layout == null ||
                    !layoutIds.Add(layout.id ?? string.Empty) ||
                    !nodes.ContainsKey(layout.id ?? string.Empty) ||
                    layout.id.StartsWith("@", StringComparison.Ordinal))
                {
                    report.Error(path.Replace("graph.json", "layout.json"), "layout_node_invalid", "Layout必须唯一引用editable Node，不能引用anchor或未知Node。");
                    return false;
                }
            }

            var edgeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageFlowEdge edge in graphFile.flowEdges)
            {
                if (!ValidateEdgeIdentity(edge?.id, edgeIds, path + ".flowEdges", report) ||
                    !ValidateEndpoint(domain, graphFile.kind, nodes, edge.from, "Output", false, path + $".flowEdges[{edge.id}].from", report) ||
                    !ValidateEndpoint(domain, graphFile.kind, nodes, edge.to, "Input", false, path + $".flowEdges[{edge.id}].to", report))
                    return false;
            }
            foreach (AgentPackagePropertyEdge edge in graphFile.propertyEdges)
            {
                if (!ValidateEdgeIdentity(edge?.id, edgeIds, path + ".propertyEdges", report) ||
                    !ValidateEndpoint(domain, graphFile.kind, nodes, edge.from, "Output", true, path + $".propertyEdges[{edge.id}].from", report) ||
                    !ValidateEndpoint(domain, graphFile.kind, nodes, edge.to, "Input", true, path + $".propertyEdges[{edge.id}].to", report))
                    return false;
            }
            return ValidatePortCapacities(nodes, graphFile, path, report);
        }

        bool AllowsExistingEmptyActionContext(
            AgentAuthoringDocumentReadPhase phase,
            AgentPackageNode node,
            AgentSnapshotNode oldNode)
        {
            if (phase != AgentAuthoringDocumentReadPhase.CheckoutRoundTrip ||
                !string.Equals(node.kind, "action-context-active", StringComparison.Ordinal) ||
                oldNode == null ||
                !string.IsNullOrEmpty(oldNode.actionContextId) ||
                !m_Catalog.TryGetKind(oldNode.typeName, out string oldKind) ||
                !string.Equals(oldKind, node.kind, StringComparison.Ordinal))
                return false;
            JToken value = node.properties?["actionContextId"];
            return value == null ||
                   value.Type == JTokenType.String &&
                   string.IsNullOrEmpty(value.Value<string>());
        }

        bool ValidateEndpoint(
            string domain,
            string graphKind,
            IReadOnlyDictionary<string, AgentPackageNode> nodes,
            AgentPackageEdgeEndpoint endpoint,
            string direction,
            bool property,
            string path,
            AgentCompileReport report)
        {
            if (endpoint == null || string.IsNullOrWhiteSpace(endpoint.node) || string.IsNullOrWhiteSpace(endpoint.port))
            {
                report.Error(path, "edge_endpoint_invalid", "Edge endpoint必须声明node与port。");
                return false;
            }
            if (endpoint.node.StartsWith("@", StringComparison.Ordinal))
            {
                if (m_Catalog.IsAnchorPortAllowed(graphKind, endpoint.node, endpoint.port, direction, property, domain))
                    return true;
                report.Error(path, "anchor_or_port_unknown", $"Graph kind不允许anchor或port：{endpoint.node}.{endpoint.port}");
                return false;
            }
            if (!nodes.TryGetValue(endpoint.node, out AgentPackageNode node))
            {
                report.Error(path, "edge_node_unknown", $"Edge引用未知Node：{endpoint.node}");
                return false;
            }
            if (!m_Catalog.TryResolveDocumentPort(
                    node.kind,
                    node.properties,
                    endpoint.port,
                    property,
                    out GraphAuthoringDynamicPortProjection port,
                    out GraphAuthoringPortShapeException error))
            {
                report.Error(path, error.Code, error.Message);
                return false;
            }
            GraphAuthoringPortDirection expected = string.Equals(
                direction,
                GraphAuthoringPortDirection.Input.ToString(),
                StringComparison.Ordinal)
                ? GraphAuthoringPortDirection.Input
                : GraphAuthoringPortDirection.Output;
            if (port.Direction == expected)
                return true;
            string mode = node.properties?["exposedProperty"]?["mode"]?.Value<string>() ?? string.Empty;
            report.Error(
                path,
                "port_shape_direction_mismatch",
                $"Node '{endpoint.node}' kind='{node.kind}' mode='{mode}' 的 port '{endpoint.port}' 实际方向为 {port.Direction}，edge endpoint 要求 {expected}。");
            return false;
        }

        bool ValidatePortCapacities(
            IReadOnlyDictionary<string, AgentPackageNode> nodes,
            AgentPackageGraphFile graph,
            string path,
            AgentCompileReport report)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            void Count(AgentPackageEdgeEndpoint endpoint, bool property)
            {
                if (endpoint?.node?.StartsWith("@", StringComparison.Ordinal) != false)
                    return;
                string key = endpoint.node + "\0" + (property ? "property:" : "flow:") + endpoint.port;
                counts.TryGetValue(key, out int count);
                counts[key] = count + 1;
            }
            foreach (AgentPackageFlowEdge edge in graph.flowEdges)
            {
                Count(edge.from, false);
                Count(edge.to, false);
            }
            foreach (AgentPackagePropertyEdge edge in graph.propertyEdges)
            {
                Count(edge.from, true);
                Count(edge.to, true);
            }

            bool valid = true;
            foreach (KeyValuePair<string, int> pair in counts.Where(value => value.Value > 1))
            {
                string[] identity = pair.Key.Split('\0');
                if (identity.Length != 2 || !nodes.TryGetValue(identity[0], out AgentPackageNode node))
                    continue;
                bool property = identity[1].StartsWith("property:", StringComparison.Ordinal);
                string portId = identity[1].Substring(identity[1].IndexOf(':') + 1);
                if (!m_Catalog.TryResolveDocumentPort(
                        node.kind,
                        node.properties,
                        portId,
                        property,
                        out GraphAuthoringDynamicPortProjection port,
                        out _))
                    continue;
                if (port.Capacity != GraphAuthoringPortCapacity.Single)
                    continue;
                report.Error(
                    $"{path}.nodes[{identity[0]}]",
                    "port_shape_capacity_exceeded",
                    $"Node '{identity[0]}' 的 port '{portId}' 容量为 Single，但目标 Graph 包含 {pair.Value} 条连接。");
                valid = false;
            }
            return valid;
        }

        static bool ValidateEdgeIdentity(string identity, ISet<string> identities, string path, AgentCompileReport report)
        {
            if (IsIdentity(identity) && identities.Add(identity))
                return true;
            report.Error(path, "edge_identity_invalid", "Edge identity缺失、重复或local语法非法。");
            return false;
        }

        static bool IsIdentity(string identity)
        {
            if (string.IsNullOrWhiteSpace(identity) || identity.Any(char.IsWhiteSpace))
                return false;
            if (!identity.StartsWith("local:", StringComparison.Ordinal))
                return !identity.StartsWith("@", StringComparison.Ordinal);
            string local = identity.Substring("local:".Length);
            return local.Length > 0 && local.All(character =>
                char.IsLetterOrDigit(character) || character == '-' || character == '_' || character == '.');
        }

        static void ToTimelineFiles(
            AgentSnapshotTimeline source,
            out AgentPackageTimelineFile timeline,
            out AgentPackageCurvesFile curves)
        {
            timeline = new AgentPackageTimelineFile
            {
                id = source.timelineAuthoringId,
                name = source.name,
                callSites = AgentAuthoringDocumentCodec.Clone(source.callSites) ?? new List<AgentSnapshotTimelineCallSite>(),
                tracks = AgentAuthoringDocumentCodec.Clone(source.tracks) ?? new List<AgentSnapshotTimelineTrack>()
            };
            curves = new AgentPackageCurvesFile { timelineId = source.timelineAuthoringId };
            foreach (AgentSnapshotTimelineTrack track in timeline.tracks)
            {
                foreach (AgentSnapshotTimelineClip clip in track.clips ?? new List<AgentSnapshotTimelineClip>())
                {
                    foreach (AgentSnapshotTimelineCurveChannel channel in clip.curveChannels ?? new List<AgentSnapshotTimelineCurveChannel>())
                    {
                        curves.curves.Add(new AgentPackageCurve
                        {
                            clipId = clip.clipAuthoringId,
                            channelId = channel.channelId,
                            timeDomain = channel.timeDomain,
                            bounded = channel.bounded,
                            minimum = channel.minimum,
                            maximum = channel.maximum,
                            zero = channel.zero,
                            unit = channel.unit,
                            preWrapMode = channel.preWrapMode,
                            postWrapMode = channel.postWrapMode,
                            keys = channel.keys
                        });
                    }
                    clip.curveChannels = new List<AgentSnapshotTimelineCurveChannel>();
                }
            }
        }

        static bool TryFromTimelineFiles(
            string path,
            AgentPackageTimelineFile timeline,
            AgentPackageCurvesFile curves,
            AgentSnapshotTimeline current,
            AgentCompileReport report,
            out AgentSnapshotTimeline result)
        {
            result = new AgentSnapshotTimeline
            {
                timelineAuthoringId = timeline.id,
                name = timeline.name,
                callSites = timeline.callSites ?? new List<AgentSnapshotTimelineCallSite>(),
                tracks = timeline.tracks ?? new List<AgentSnapshotTimelineTrack>()
            };
            if (!IsIdentity(timeline.id) ||
                timeline.tracks == null ||
                timeline.tracks.Any(track =>
                    track == null ||
                    !IsIdentity(track.trackAuthoringId) ||
                    track.clips == null ||
                    track.markers == null ||
                    track.clips.Any(clip => clip == null || !IsIdentity(clip.clipAuthoringId)) ||
                    track.markers.Any(marker => marker == null || !IsIdentity(marker.authoringId))))
            {
                report.Error(path, "timeline_structure_invalid", "Timeline、Track、Clip或Marker缺少合法identity与必需集合。");
                return false;
            }
            List<AgentSnapshotTimelineClip> allClips = result.tracks
                .SelectMany(track => track.clips ?? new List<AgentSnapshotTimelineClip>())
                .Where(clip => clip != null && !string.IsNullOrEmpty(clip.clipAuthoringId))
                .ToList();
            if (allClips.GroupBy(clip => clip.clipAuthoringId, StringComparer.Ordinal).Any(group => group.Count() > 1))
            {
                report.Error(path + ".tracks.clips", "timeline_clip_identity_duplicate", "Timeline内Clip identity重复。");
                return false;
            }
            var clips = allClips.ToDictionary(clip => clip.clipAuthoringId, clip => clip, StringComparer.Ordinal);
            var currentChannels = (current?.tracks ?? new List<AgentSnapshotTimelineTrack>())
                .SelectMany(track => track.clips ?? new List<AgentSnapshotTimelineClip>())
                .SelectMany(clip => (clip.curveChannels ?? new List<AgentSnapshotTimelineCurveChannel>())
                    .Select(channel => new
                    {
                        clip.clipAuthoringId,
                        channel
                    }))
                .GroupBy(value => value.clipAuthoringId + "\0" + value.channel.channelId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().channel, StringComparer.Ordinal);
            var curveIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageCurve curve in curves.curves ?? new List<AgentPackageCurve>())
            {
                string curvePath = $"{path}.curves[{curve?.clipId}:{curve?.channelId}]";
                string identity = (curve?.clipId ?? string.Empty) + "\0" + (curve?.channelId ?? string.Empty);
                if (curve == null ||
                    !curveIds.Add(identity) ||
                    !clips.TryGetValue(curve.clipId ?? string.Empty, out AgentSnapshotTimelineClip clip) ||
                    !ValidateCurve(curve, curvePath, report))
                    return false;
                currentChannels.TryGetValue(identity, out AgentSnapshotTimelineCurveChannel existing);
                TimelineCurveChannelCatalog.TryGet(curve.channelId, out TimelineCurveChannelDescriptor descriptor);
                AgentSnapshotTimelineCurveChannel channel = existing != null
                    ? AgentAuthoringDocumentCodec.Clone(existing)
                    : new AgentSnapshotTimelineCurveChannel
                    {
                        channelId = curve.channelId,
                        displayName = descriptor.DisplayName,
                        timeDomain = descriptor.TimeDomain.ToString(),
                        bounded = descriptor.ValueDomain.IsBounded,
                        minimum = descriptor.ValueDomain.Minimum,
                        maximum = descriptor.ValueDomain.Maximum,
                        zero = descriptor.ValueDomain.Zero,
                        unit = descriptor.ValueDomain.Unit
                    };
                channel.preWrapMode = curve.preWrapMode;
                channel.postWrapMode = curve.postWrapMode;
                channel.keys = curve.keys ?? new List<AgentAnimationCurveKey>();
                clip.curveChannels.Add(channel);
            }
            return true;
        }

        static bool ValidateCurve(AgentPackageCurve curve, string path, AgentCompileReport report)
        {
            if (string.IsNullOrWhiteSpace(curve.channelId) ||
                !TimelineCurveChannelCatalog.TryGet(curve.channelId, out TimelineCurveChannelDescriptor descriptor) ||
                !string.Equals(curve.timeDomain, descriptor.TimeDomain.ToString(), StringComparison.Ordinal) ||
                curve.bounded != descriptor.ValueDomain.IsBounded ||
                curve.minimum != descriptor.ValueDomain.Minimum ||
                curve.maximum != descriptor.ValueDomain.Maximum ||
                curve.zero != descriptor.ValueDomain.Zero ||
                !string.Equals(curve.unit ?? string.Empty, descriptor.ValueDomain.Unit, StringComparison.Ordinal) ||
                !Enum.TryParse(curve.preWrapMode, true, out UnityEngine.WrapMode preWrap) ||
                !Enum.IsDefined(typeof(UnityEngine.WrapMode), preWrap) ||
                !Enum.TryParse(curve.postWrapMode, true, out UnityEngine.WrapMode postWrap) ||
                !Enum.IsDefined(typeof(UnityEngine.WrapMode), postWrap) ||
                curve.keys == null ||
                curve.keys.Count == 0)
            {
                report.Error(path, "timeline_curve_invalid", "Curve缺少registered channel、wrap mode或keys。");
                return false;
            }
            float previous = -1f;
            for (int i = 0; i < curve.keys.Count; i++)
            {
                AgentAnimationCurveKey key = curve.keys[i];
                if (key == null ||
                    !Enum.TryParse(key.weightedMode, true, out UnityEngine.WeightedMode weightedMode) ||
                    !Enum.IsDefined(typeof(UnityEngine.WeightedMode), weightedMode) ||
                    !Finite(key.time) ||
                    key.time < 0f ||
                    key.time > 1f ||
                    key.time <= previous ||
                    !Finite(key.value) ||
                    !Finite(key.inTangent) ||
                    !Finite(key.outTangent) ||
                    !Finite(key.inWeight) ||
                    !Finite(key.outWeight))
                {
                    report.Error($"{path}.keys[{i}]", "timeline_curve_key_invalid", "Curve key必须按normalized time严格递增，并只包含有限数值与合法weightedMode。");
                    return false;
                }
                previous = key.time;
            }
            return true;
        }

        static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        static bool TryEndpoint(
            AgentPackageEdgeEndpoint endpoint,
            IReadOnlyDictionary<string, string> anchors,
            string path,
            AgentCompileReport report,
            out string node)
        {
            node = null;
            if (endpoint == null || string.IsNullOrWhiteSpace(endpoint.node) || string.IsNullOrWhiteSpace(endpoint.port))
            {
                report.Error(path, "edge_endpoint_invalid", "Edge endpoint必须声明node与port。");
                return false;
            }
            if (!endpoint.node.StartsWith("@", StringComparison.Ordinal))
            {
                node = endpoint.node;
                return true;
            }
            if (anchors.TryGetValue(endpoint.node, out node))
                return true;
            node = endpoint.node;
            return true;
        }

        static string ResolveEndpoint(IReadOnlyDictionary<string, string> names, string identity)
        {
            return names.TryGetValue(identity ?? string.Empty, out string value) ? value : identity;
        }

        static void Add(JObject properties, string name, object value)
        {
            if (value == null)
                return;
            JToken token = AgentAuthoringDocumentCodec.ToToken(value);
            if (token.Type == JTokenType.Null || token is JArray array && array.Count == 0 || token.Type == JTokenType.String && string.IsNullOrEmpty(token.Value<string>()))
                return;
            properties[name] = token;
        }

        static List<T> ReadList<T>(JObject properties, string name)
        {
            return properties[name]?.ToObject<List<T>>() ?? new List<T>();
        }

        static AgentSnapshotGraphReference ToGraphReference(AgentPackageGraphReference source, AgentSnapshotNode current)
        {
            AgentSnapshotGraphReference existing = current?.graphReferences?.FirstOrDefault(value => string.Equals(value.key, source.key, StringComparison.Ordinal));
            return new AgentSnapshotGraphReference
            {
                key = source.key,
                label = existing?.label,
                graphAuthoringId = source.graphId,
                graphPath = existing?.graphPath,
                graphKind = existing?.graphKind,
                ownership = source.ownership,
                scopeId = existing?.scopeId,
                sharedAssetPath = source.sharedAssetPath,
                required = existing?.required ?? false
            };
        }

        static List<AgentSnapshotGraphReference> MergeGraphReferences(
            IReadOnlyList<AgentPackageGraphReference> package,
            AgentSnapshotNode current)
        {
            var pending = (package ?? Array.Empty<AgentPackageGraphReference>())
                .Where(value => value != null && !string.IsNullOrEmpty(value.key))
                .ToDictionary(value => value.key, value => value, StringComparer.Ordinal);
            var result = new List<AgentSnapshotGraphReference>();
            foreach (AgentSnapshotGraphReference existing in current?.graphReferences ?? new List<AgentSnapshotGraphReference>())
            {
                if (pending.TryGetValue(existing.key ?? string.Empty, out AgentPackageGraphReference replacement))
                {
                    result.Add(ToGraphReference(replacement, current));
                    pending.Remove(existing.key);
                }
                else if (string.IsNullOrEmpty(existing.graphAuthoringId))
                {
                    result.Add(existing);
                }
            }
            result.AddRange(pending.Values
                .OrderBy(value => value.key, StringComparer.Ordinal)
                .Select(value => ToGraphReference(value, current)));
            return result;
        }

        static AgentSnapshotAssetReference ToAssetReference(AgentPackageAssetReference source, AgentSnapshotNode current)
        {
            AgentSnapshotAssetReference existing = current?.assetReferences?.FirstOrDefault(value => string.Equals(value.key, source.key, StringComparison.Ordinal));
            return new AgentSnapshotAssetReference
            {
                key = source.key,
                label = existing?.label,
                assetPath = source.assetPath,
                assetGuid = source.assetGuid,
                assetType = existing?.assetType,
                required = existing?.required ?? false
            };
        }

        static List<AgentSnapshotAssetReference> MergeAssetReferences(
            IReadOnlyList<AgentPackageAssetReference> package,
            AgentSnapshotNode current)
        {
            var pending = (package ?? Array.Empty<AgentPackageAssetReference>())
                .Where(value => value != null && !string.IsNullOrEmpty(value.key))
                .ToDictionary(value => value.key, value => value, StringComparer.Ordinal);
            var result = new List<AgentSnapshotAssetReference>();
            foreach (AgentSnapshotAssetReference existing in current?.assetReferences ?? new List<AgentSnapshotAssetReference>())
            {
                if (pending.TryGetValue(existing.key ?? string.Empty, out AgentPackageAssetReference replacement))
                {
                    result.Add(ToAssetReference(replacement, current));
                    pending.Remove(existing.key);
                }
                else if (string.IsNullOrEmpty(existing.assetPath) && string.IsNullOrEmpty(existing.assetGuid))
                {
                    result.Add(existing);
                }
            }
            result.AddRange(pending.Values
                .OrderBy(value => value.key, StringComparer.Ordinal)
                .Select(value => ToAssetReference(value, current)));
            return result;
        }

        static AgentSnapshotExposedProperty ToExposedProperty(
            AgentPackageExposedProperty source,
            AgentSnapshotNode current)
        {
            if (source == null)
                return null;
            AgentSnapshotExposedProperty existing = current?.exposedProperty;
            return new AgentSnapshotExposedProperty
            {
                mode = source.mode,
                declarationAuthoringId = source.declarationId,
                declarationOwnerId = existing?.declarationOwnerId,
                key = existing?.key,
                valueType = source.valueType,
                value = source.value?.DeepClone()
            };
        }

        static Dictionary<string, AgentPackageNodeLayout> BuildGeneratedPositions(
            AgentPackageGraphFile graph,
            IReadOnlyDictionary<string, AgentPackageNodeLayout> explicitPositions,
            IReadOnlyDictionary<string, AgentSnapshotNode> currentNodes)
        {
            var nodes = (graph.nodes ?? new List<AgentPackageNode>())
                .Where(node => node != null && !string.IsNullOrEmpty(node.id))
                .OrderBy(node => node.id, StringComparer.Ordinal)
                .ToList();
            var nodeIds = new HashSet<string>(nodes.Select(node => node.id), StringComparer.Ordinal);
            var indegree = nodes.ToDictionary(node => node.id, _ => 0, StringComparer.Ordinal);
            var outgoing = nodes.ToDictionary(node => node.id, _ => new List<string>(), StringComparer.Ordinal);
            foreach (AgentPackageFlowEdge edge in graph.flowEdges ?? new List<AgentPackageFlowEdge>())
            {
                string from = edge?.from?.node;
                string to = edge?.to?.node;
                if (!nodeIds.Contains(to ?? string.Empty) || !nodeIds.Contains(from ?? string.Empty))
                    continue;
                indegree[to]++;
                outgoing[from].Add(to);
            }

            var layers = nodes.ToDictionary(node => node.id, _ => 0, StringComparer.Ordinal);
            var ready = new SortedSet<string>(
                indegree.Where(pair => pair.Value == 0).Select(pair => pair.Key),
                StringComparer.Ordinal);
            while (ready.Count > 0)
            {
                string current = ready.Min;
                ready.Remove(current);
                foreach (string target in outgoing[current].OrderBy(value => value, StringComparer.Ordinal))
                {
                    layers[target] = Math.Max(layers[target], layers[current] + 1);
                    indegree[target]--;
                    if (indegree[target] == 0)
                        ready.Add(target);
                }
            }

            var result = new Dictionary<string, AgentPackageNodeLayout>(StringComparer.Ordinal);
            bool stateMachine = string.Equals(graph.kind, AgentGraphKind.StateMachineGraph.ToString(), StringComparison.Ordinal);
            float horizontalStep = stateMachine ? 340f : 300f;
            float verticalStep = stateMachine ? 200f : 180f;
            foreach (IGrouping<int, AgentPackageNode> layer in nodes
                         .Where(node =>
                             !explicitPositions.ContainsKey(node.id) &&
                             (!currentNodes.TryGetValue(node.id, out AgentSnapshotNode current) || current.position == null))
                         .GroupBy(node => layers[node.id])
                         .OrderBy(group => group.Key))
            {
                int row = 0;
                foreach (AgentPackageNode node in layer.OrderBy(value => value.id, StringComparer.Ordinal))
                {
                    result[node.id] = new AgentPackageNodeLayout
                    {
                        id = node.id,
                        x = layer.Key * horizontalStep,
                        y = row * verticalStep
                    };
                    row++;
                }
            }
            return result;
        }

        static string OwnerSlot(string graphKind)
        {
            if (string.Equals(graphKind, AgentGraphKind.StateMachineGraph.ToString(), StringComparison.Ordinal))
                return "stateMachine";
            if (string.Equals(graphKind, AgentGraphKind.StateBehaviorSubTree.ToString(), StringComparison.Ordinal))
                return "body";
            if (string.Equals(graphKind, AgentGraphKind.ConditionRuleGraph.ToString(), StringComparison.Ordinal))
                return "condition";
            return "root";
        }

        internal static bool IsDiscoverableTimelineFragment(string path)
        {
            return path.StartsWith("editable/timelines/", StringComparison.Ordinal) &&
                   (path.EndsWith("/timeline.json", StringComparison.Ordinal) ||
                    path.EndsWith("/curves.json", StringComparison.Ordinal));
        }

        internal static bool TryDiscoverRemovedAuthoringFragments(
            IReadOnlyCollection<string> missingPaths,
            AgentCompileReport report,
            out IReadOnlyCollection<string> discovered)
        {
            var missing = new HashSet<string>(missingPaths ?? Array.Empty<string>(), StringComparer.Ordinal);
            var result = new HashSet<string>(StringComparer.Ordinal);
            bool valid = true;
            foreach (string path in missing.OrderBy(value => value, StringComparer.Ordinal))
            {
                if (!TryResolveFragmentCompanion(path, out string companion))
                    continue;
                if (!missing.Contains(companion))
                {
                    report.Error(path, "document_fragment_remove_pair_incomplete", "删除Graph或Timeline分片必须同时删除同目录canonical pair。");
                    valid = false;
                    continue;
                }
                result.Add(path);
                result.Add(companion);
            }
            discovered = result;
            return valid;
        }

        static bool TryResolveFragmentCompanion(string path, out string companion)
        {
            companion = null;
            if (AgentAuthoringPresentationPackageCodec.IsImplementationGraphFile(path))
                companion = path.Substring(0, path.Length - "graph.json".Length) + "layout.json";
            else if (AgentAuthoringPresentationPackageCodec.IsImplementationGraphLayoutFile(path))
                companion = path.Substring(0, path.Length - "layout.json".Length) + "graph.json";
            else if (AgentAuthoringPresentationPackageCodec.IsImplementationStateMachineFile(path))
                companion = path.Substring(0, path.Length - "state-machine.json".Length) + "layout.json";
            else if (AgentAuthoringPresentationPackageCodec.IsImplementationStateMachineLayoutFile(path))
                companion = path.Substring(0, path.Length - "layout.json".Length) + "state-machine.json";
            else if (path.StartsWith("editable/graphs/", StringComparison.Ordinal))
            {
                if (path.EndsWith("/graph.json", StringComparison.Ordinal))
                    companion = path.Substring(0, path.Length - "graph.json".Length) + "layout.json";
                else if (path.EndsWith("/layout.json", StringComparison.Ordinal))
                    companion = path.Substring(0, path.Length - "layout.json".Length) + "graph.json";
            }
            else if (path.StartsWith("editable/timelines/", StringComparison.Ordinal))
            {
                if (path.EndsWith("/timeline.json", StringComparison.Ordinal))
                    companion = path.Substring(0, path.Length - "timeline.json".Length) + "curves.json";
                else if (path.EndsWith("/curves.json", StringComparison.Ordinal))
                    companion = path.Substring(0, path.Length - "curves.json".Length) + "timeline.json";
            }
            return companion != null;
        }

        internal static bool TryDiscoverNewTimelineFragments(
            IReadOnlyDictionary<string, JToken> candidates,
            AgentCompileReport report,
            out IReadOnlyCollection<string> discovered)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            bool valid = true;
            foreach (string directory in candidates.Keys
                         .Select(path => path.Substring(0, path.LastIndexOf('/')))
                         .Distinct(StringComparer.Ordinal)
                         .OrderBy(value => value, StringComparer.Ordinal))
            {
                string timelinePath = directory + "/timeline.json";
                string curvesPath = directory + "/curves.json";
                if (!candidates.TryGetValue(timelinePath, out JToken timelineToken) ||
                    !candidates.TryGetValue(curvesPath, out JToken curvesToken))
                {
                    report.Error(directory, "timeline_new_pair_incomplete", "新增Timeline必须同时提供同目录timeline.json与curves.json。");
                    valid = false;
                    continue;
                }
                if (!AgentAuthoringDocumentCodec.TryConvertToken(timelineToken, timelinePath, report, out AgentPackageTimelineFile timeline) ||
                    !AgentAuthoringDocumentCodec.TryConvertToken(curvesToken, curvesPath, report, out AgentPackageCurvesFile curves))
                {
                    valid = false;
                    continue;
                }
                bool localTimeline = timeline.id?.StartsWith("local:", StringComparison.Ordinal) == true;
                bool localCallSite = timeline.callSites?.Count == 1 &&
                                     timeline.callSites[0] != null &&
                                     timeline.callSites[0].nodeAuthoringId?.StartsWith("local:", StringComparison.Ordinal) == true &&
                                     !string.IsNullOrWhiteSpace(timeline.callSites[0].graphPath);
                bool localContents = timeline.tracks != null && timeline.tracks.All(track =>
                    track != null &&
                    track.trackAuthoringId?.StartsWith("local:", StringComparison.Ordinal) == true &&
                    track.clips != null &&
                    track.clips.All(clip => clip != null && clip.clipAuthoringId?.StartsWith("local:", StringComparison.Ordinal) == true));
                string expectedDirectory = $"editable/timelines/{Segment(timeline.id)}";
                if (!localTimeline ||
                    !localCallSite ||
                    !localContents ||
                    !string.Equals(curves.timelineId, timeline.id, StringComparison.Ordinal) ||
                    !string.Equals(directory, expectedDirectory, StringComparison.Ordinal))
                {
                    report.Error(
                        timelinePath,
                        "timeline_new_pair_invalid",
                        "新增Timeline必须使用canonical local identity目录、唯一local TimelineNode调用点、local Track/Clip，并保持curves timelineId一致。");
                    valid = false;
                    continue;
                }
                result.Add(timelinePath);
                result.Add(curvesPath);
            }
            discovered = result;
            return valid;
        }

        internal static string Segment(string identity)
        {
            string value = string.IsNullOrWhiteSpace(identity) ? "entity" : identity;
            var builder = new StringBuilder(value.Length);
            foreach (char character in value)
                builder.Append(char.IsLetterOrDigit(character) || character == '-' || character == '_' ? character : '-');
            string readable = builder.ToString().Trim('-');
            if (readable.Length > 48)
                readable = readable.Substring(0, 48);
            using SHA256 algorithm = SHA256.Create();
            string hash = string.Concat(algorithm.ComputeHash(Encoding.UTF8.GetBytes(value))
                .Take(6)
                .Select(valueByte => valueByte.ToString("x2", CultureInfo.InvariantCulture)));
            return $"{(string.IsNullOrEmpty(readable) ? "entity" : readable)}-{hash}";
        }

        static bool TryFile<T>(
            IReadOnlyDictionary<string, JToken> files,
            string path,
            AgentCompileReport report,
            out T value)
        {
            value = default;
            if (!files.TryGetValue(path, out JToken token))
            {
                report.Error(path, "document_file_missing", $"Manifest缺少必需文件：{path}");
                return false;
            }
            try
            {
                value = token.ToObject<T>();
                if (value == null)
                    throw new InvalidOperationException("文件内容为空。");
                return true;
            }
            catch (Exception exception)
            {
                report.Error(path, "document_json_invalid", exception.Message);
                return false;
            }
        }
    }
}
