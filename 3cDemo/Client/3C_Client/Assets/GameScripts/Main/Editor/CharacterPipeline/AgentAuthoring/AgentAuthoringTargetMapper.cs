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
using static ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.AgentPackageMappingSupport;

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
        readonly AgentGraphDocumentMapper m_GraphMapper = new AgentGraphDocumentMapper();

        public Dictionary<string, JToken> ToFiles(
            AgentAuthoringTarget target,
            AgentGraphSnapshot snapshot,
            AgentCompileReport report)
        {
            var files = new Dictionary<string, JToken>(StringComparer.Ordinal);
            if (string.Equals(target.domain, AgentAuthoringSchema.CharacterControllerDomain, StringComparison.Ordinal))
            {
                files["editable/controller.json"] = AgentControlDocumentMapper.ToToken(target.editable);
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
                AgentSkillDocumentMapper.Write(files, target.editable.skills);
                AgentSkillFlowDocumentMapper.Write(files, target.editable, report);
                AgentAuthoringPresentationPackageCodec.Write(
                    files,
                    target.editable.presentation,
                    report);
                AgentAuthoringPresentationPackageCodec.WriteReadonly(
                    files,
                    target.context.presentation,
                    report);
            }

            foreach (AgentSnapshotGraph graph in target.editable.graphs ?? new List<AgentSnapshotGraph>())
            {
                string directory = $"editable/graphs/{Segment(graph.graphAuthoringId)}";
                if (!m_GraphMapper.TryToGraphFiles(target.domain, graph, report, out AgentPackageGraphFile graphFile, out AgentPackageLayoutFile layoutFile))
                    continue;
                files[directory + "/graph.json"] = AgentAuthoringDocumentCodec.ToToken(graphFile);
                files[directory + "/layout.json"] = AgentAuthoringDocumentCodec.ToToken(layoutFile);
            }

            foreach (AgentSnapshotTimeline timeline in target.editable.timelines ?? new List<AgentSnapshotTimeline>())
            {
                string directory = $"editable/timelines/{Segment(timeline.timelineAuthoringId)}";
                AgentTimelineDocumentMapper.ToTimelineFiles(timeline, out AgentPackageTimelineFile timelineFile, out AgentPackageCurvesFile curvesFile);
                files[directory + "/timeline.json"] = AgentAuthoringDocumentCodec.ToToken(timelineFile);
                files[directory + "/curves.json"] = AgentAuthoringDocumentCodec.ToToken(curvesFile);
            }

            var nodeCatalog = new AgentPackageNodeCatalogFile
            {
                kinds = m_Catalog.ExportNodeKinds(target.domain).ToList(),
                skillKinds = string.Equals(target.domain, AgentAuthoringSchema.CharacterControllerDomain, StringComparison.Ordinal)
                    ? AgentSkillFlowAuthoringCapabilities.ExportCatalog().ToList()
                    : new List<AgentPackageSkillNodeKindDescriptor>()
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
                timelineAssets = target.context.timelineAssets,
                actionContextAssets = target.context.actionContextAssets,
                animationBlendCurves = target.context.presentation?.blendCurves ??
                    new List<AgentDocumentBlendAssetContext>(),
                animationBlendProfiles = target.context.presentation?.blendProfiles ??
                    new List<AgentDocumentBlendAssetContext>(),
                animationClips = target.context.presentation?.animationClips ??
                    new List<AgentDocumentAnimationClipContext>()
            });
            files["context/dependencies.json"] = AgentAuthoringDocumentCodec.ToToken(new AgentPackageDependenciesFile
            {
                definitionName = target.context.definitionName,
                definitionAssetPath = target.context.definitionAssetPath,
                rootTreeAssetPath = target.context.rootTreeAssetPath,
                rootGraphAuthoringId = target.context.rootGraphAuthoringId,
                bodyMotion = target.context.bodyMotion,
                presentation = target.context.presentation,
                generatedProduct = target.context.generatedProduct,
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
            if (!AgentAuthoringSchema.IsDomain(manifest.domain))
            {
                report.Error("manifest.domain", "unsupported_domain", "Agent Document只支持CharacterController；Behavior Designer行为不进入BTSMTL Document。");
                return false;
            }
            bool valid = true;
            AgentPackageControllerFile controller = new AgentPackageControllerFile();
            AgentPackageBlackboardFile blackboard = new AgentPackageBlackboardFile();
            AgentPackageActionsFile actions = new AgentPackageActionsFile();
            if (string.Equals(manifest.domain, AgentAuthoringSchema.CharacterControllerDomain, StringComparison.Ordinal))
            {
                valid &= TryFile(files, "editable/controller.json", report, out controller);
                valid &= TryFile(files, "editable/blackboard.json", report, out blackboard);
                valid &= TryFile(files, "editable/actions.json", report, out actions);
                valid &= AgentBlackboardDocumentMapper.ValidateBlackboardPackage(blackboard, report);
            }
            valid &= TryFile(files, "context/asset-catalog.json", report, out AgentPackageAssetCatalogFile assets);
            valid &= TryFile(files, "context/dependencies.json", report, out AgentPackageDependenciesFile dependencies);
            valid &= TryFile(files, "context/node-catalog.json", report, out AgentPackageNodeCatalogFile nodeCatalog);
            valid &= TryFile(files, "context/graph-kinds.json", report, out AgentPackageGraphKindsFile _);
            if (nodeCatalog != null)
                valid &= AgentPackageNodeCatalogValidator.Validate(nodeCatalog, report);
            if (!valid)
                return false;

            if (string.Equals(
                    manifest.domain,
                    AgentAuthoringSchema.CharacterControllerDomain,
                    StringComparison.Ordinal))
            {
                target.editable.stateMachines = controller.stateMachines ?? new List<AgentSnapshotStateMachineSummary>();
                target.editable.timelineTreeClips = controller.timelineTreeClips ?? new List<AgentSnapshotTimelineTreeClip>();
                target.editable.control = AgentControlDocumentMapper.ReadController(controller, report);
                target.editable.blackboardSchemaRevision = blackboard.schemaRevision;
                target.editable.blackboardDeclarations = blackboard.declarations ?? new List<AgentSnapshotBlackboardDeclaration>();
                target.editable.actionRequests = actions.requests ?? new List<AgentSnapshotActionRequest>();
                target.editable.actionProfiles = actions.profiles ?? new List<AgentSnapshotActionProfile>();
                valid &= AgentSkillDocumentMapper.TryRead(files, target.editable, report);
                valid &= AgentSkillFlowDocumentMapper.TryRead(files, target.editable, report);
            }
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
            if (files.ContainsKey("editable/ai/perception.json"))
            {
                report.Error("editable/ai/perception.json", "document_domain_file_invalid", "BTSMTL Document不能包含Behavior Designer AI分片。");
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
                if (!m_GraphMapper.TryFromGraphFiles(
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
                if (!AgentTimelineDocumentMapper.TryFromTimelineFiles(timelinePath, timelineFile, curvesFile, currentTimeline, report, out AgentSnapshotTimeline timeline))
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

            valid &= m_GraphMapper.ValidateGraphRelationships(target.editable, report);
            if (string.Equals(
                    manifest.domain,
                    AgentAuthoringSchema.CharacterControllerDomain,
                    StringComparison.Ordinal))
                valid &= AgentSkillFlowDocumentMapper.Validate(
                    new AgentPackageSkillFlowDocument
                    {
                        skills = target.editable.skills,
                        graphs = target.editable.skillGraphs,
                        layouts = target.editable.skillGraphLayouts,
                        macros = target.editable.skillMacros,
                        timelines = target.editable.skillTimelines
                    },
                    report,
                    target.editable.control?.moduleId,
                    current?.inputProviderOwnerId,
                    current?.gameplayProviderOwnerId);
            valid &= AgentTimelineDocumentMapper.ValidateTimelineRelationships(target.editable, report);
            valid &= AgentPackageMappingSupport.ValidatePrimaryIdentities(target.editable, report);

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
                generatedProduct = dependencies.generatedProduct ?? new AgentDocumentGeneratedProduct(),
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
            else if (AgentSkillDocumentMapper.IsDefinitionPath(path))
            {
                companion = path;
            }
            return companion != null;
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
            return AgentAuthoringDocumentCodec.TryConvertToken(token, path, report, out value);
        }
    }
}
