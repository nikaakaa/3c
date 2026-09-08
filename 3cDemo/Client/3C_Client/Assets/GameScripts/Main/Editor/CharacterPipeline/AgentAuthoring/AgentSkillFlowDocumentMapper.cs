using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BTSMTL.Timeline;
using FlowCanvas;
using FlowCanvas.Macros;
using NodeCanvas.Framework;
using Newtonsoft.Json.Linq;
using ParadoxNotion;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentSkillFlowDocumentMapper
    {
        const string GraphPrefix = "editable/skills/graphs/";
        const string MacroPrefix = "editable/skills/macros/";
        const string TimelinePrefix = "editable/skills/timelines/";

        public static string GraphPath(string id) =>
            GraphPrefix + AgentAuthoringPackageMapper.Segment(id) + "/graph.json";

        public static string GraphLayoutPath(string id) =>
            GraphPrefix + AgentAuthoringPackageMapper.Segment(id) + "/layout.json";

        public static string MacroPath(string id) =>
            MacroPrefix + AgentAuthoringPackageMapper.Segment(id) + "/macro.json";

        public static string TimelinePath(string id) =>
            TimelinePrefix + AgentAuthoringPackageMapper.Segment(id) + "/timeline.json";

        public static bool IsFragmentPath(string path)
        {
            return IsGraphPath(path) || IsGraphLayoutPath(path) ||
                   IsMacroPath(path) || IsTimelinePath(path) || IsTimelineCurvesPath(path);
        }

        public static bool IsGraphPath(string path) =>
            path.StartsWith(GraphPrefix, StringComparison.Ordinal) &&
            path.EndsWith("/graph.json", StringComparison.Ordinal);

        public static bool IsGraphLayoutPath(string path) =>
            path.StartsWith(GraphPrefix, StringComparison.Ordinal) &&
            path.EndsWith("/layout.json", StringComparison.Ordinal);

        public static bool IsMacroPath(string path) =>
            path.StartsWith(MacroPrefix, StringComparison.Ordinal) &&
            path.EndsWith("/macro.json", StringComparison.Ordinal);

        public static bool IsTimelinePath(string path) =>
            path.StartsWith(TimelinePrefix, StringComparison.Ordinal) &&
            path.EndsWith("/timeline.json", StringComparison.Ordinal);

        public static bool IsTimelineCurvesPath(string path) =>
            path.StartsWith(TimelinePrefix, StringComparison.Ordinal) &&
            path.EndsWith("/curves.json", StringComparison.Ordinal);

        public static void Write(
            IDictionary<string, JToken> files,
            AgentDocumentEditable editable,
            AgentCompileReport report)
        {
            foreach (AgentPackageSkillFlowGraphFile graph in editable?.skillGraphs ??
                         new List<AgentPackageSkillFlowGraphFile>())
            {
                if (graph == null)
                    continue;
                files[GraphPath(graph.id)] = AgentAuthoringDocumentCodec.ToToken(graph);
                AgentPackageSkillFlowGraphLayoutFile layout =
                    editable.skillGraphLayouts?.SingleOrDefault(value =>
                        string.Equals(value?.graphId, graph.id, StringComparison.Ordinal));
                if (layout == null)
                {
                    report.Error(
                        GraphLayoutPath(graph.id),
                        "skill_graph_layout_missing",
                        $"Skill Graph '{graph.id}'缺少layout分片。");
                }
                else
                {
                    files[GraphLayoutPath(graph.id)] = AgentAuthoringDocumentCodec.ToToken(layout);
                }
            }
            foreach (AgentPackageSkillMacroFile macro in editable?.skillMacros ??
                         new List<AgentPackageSkillMacroFile>())
            {
                if (macro != null)
                    files[MacroPath(macro.id)] = AgentAuthoringDocumentCodec.ToToken(macro);
            }
            foreach (AgentPackageSkillTimelineFile timeline in editable?.skillTimelines ??
                         new List<AgentPackageSkillTimelineFile>())
            {
                if (timeline == null)
                    continue;
                files[TimelinePath(timeline.id)] = AgentAuthoringDocumentCodec.ToToken(timeline);
                files[TimelinePrefix + AgentAuthoringPackageMapper.Segment(timeline.id) + "/curves.json"] =
                    AgentAuthoringDocumentCodec.ToToken(new AgentPackageCurvesFile
                    {
                        timelineId = timeline.id,
                        curves = timeline.tracks
                            .SelectMany(track => track?.clips ?? new List<AgentPackageSkillTimelineClip>())
                            .SelectMany(clip => clip?.curves ?? new List<AgentPackageCurve>())
                            .ToList()
                    });
            }
        }

        public static bool TryRead(
            IReadOnlyDictionary<string, JToken> files,
            AgentDocumentEditable editable,
            AgentCompileReport report)
        {
            bool valid = true;
            foreach (string path in files.Keys.Where(IsGraphPath).OrderBy(value => value, StringComparer.Ordinal))
            {
                string layoutPath = path.Substring(0, path.Length - "graph.json".Length) + "layout.json";
                if (!TryFile(files, path, report, out AgentPackageSkillFlowGraphFile graph) ||
                    !TryFile(files, layoutPath, report, out AgentPackageSkillFlowGraphLayoutFile layout))
                {
                    valid = false;
                    continue;
                }
                if (!string.Equals(path, GraphPath(graph.id), StringComparison.Ordinal) ||
                    !string.Equals(layout.graphId, graph.id, StringComparison.Ordinal))
                {
                    report.Error(path, "skill_graph_path_mismatch", "Skill Graph目录、graph id与layout graphId必须一致。");
                    valid = false;
                    continue;
                }
                editable.skillGraphs.Add(graph);
                editable.skillGraphLayouts.Add(layout);
            }
            foreach (string path in files.Keys.Where(IsGraphLayoutPath))
            {
                string graphPath = path.Substring(0, path.Length - "layout.json".Length) + "graph.json";
                if (!files.ContainsKey(graphPath))
                {
                    report.Error(path, "skill_graph_pair_missing", "Skill Graph layout缺少同目录graph.json。");
                    valid = false;
                }
            }
            foreach (string path in files.Keys.Where(IsMacroPath).OrderBy(value => value, StringComparer.Ordinal))
            {
                if (!TryFile(files, path, report, out AgentPackageSkillMacroFile macro))
                {
                    valid = false;
                    continue;
                }
                if (!string.Equals(path, MacroPath(macro.id), StringComparison.Ordinal))
                {
                    report.Error(path, "skill_macro_path_mismatch", "Skill Macro目录与identity必须一致。");
                    valid = false;
                    continue;
                }
                editable.skillMacros.Add(macro);
            }
            foreach (string path in files.Keys.Where(IsTimelinePath).OrderBy(value => value, StringComparer.Ordinal))
            {
                string curvesPath = path.Substring(0, path.Length - "timeline.json".Length) + "curves.json";
                if (!TryFile(files, path, report, out AgentPackageSkillTimelineFile timeline) ||
                    !TryFile(files, curvesPath, report, out AgentPackageCurvesFile curves))
                {
                    valid = false;
                    continue;
                }
                if (!string.Equals(path, TimelinePath(timeline.id), StringComparison.Ordinal) ||
                    !string.Equals(curves.timelineId, timeline.id, StringComparison.Ordinal))
                {
                    report.Error(path, "skill_timeline_path_mismatch", "Skill Timeline目录、timeline id与curves timelineId必须一致。");
                    valid = false;
                    continue;
                }
                ApplyCurves(timeline, curves);
                editable.skillTimelines.Add(timeline);
            }
            foreach (string path in files.Keys.Where(IsTimelineCurvesPath))
            {
                string timelinePath = path.Substring(0, path.Length - "curves.json".Length) + "timeline.json";
                if (!files.ContainsKey(timelinePath))
                {
                    report.Error(path, "skill_timeline_pair_missing", "Skill Timeline curves缺少同目录timeline.json。");
                    valid = false;
                }
            }
            valid &= Validate(
                new AgentPackageSkillFlowDocument
                {
                    skills = editable.skills,
                    graphs = editable.skillGraphs,
                    layouts = editable.skillGraphLayouts,
                    macros = editable.skillMacros,
                    timelines = editable.skillTimelines
                },
                report);
            return valid;
        }

        public static bool Validate(
            AgentPackageSkillFlowDocument document,
            AgentCompileReport report)
        {
            if (document == null)
            {
                report.Error("editable/skills", "skill_document_missing", "Skill Flow Document正文缺失。");
                return false;
            }
            var graphById = new Dictionary<string, AgentPackageSkillFlowGraphFile>(StringComparer.Ordinal);
            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            var edgeIds = new HashSet<string>(StringComparer.Ordinal);
            var layoutById = (document.layouts ?? new List<AgentPackageSkillFlowGraphLayoutFile>())
                .Where(value => value != null && !string.IsNullOrEmpty(value.graphId))
                .GroupBy(value => value.graphId, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.First(), StringComparer.Ordinal);
            bool valid = true;
            if (document.skills == null || document.graphs == null || document.layouts == null ||
                document.macros == null || document.timelines == null)
            {
                report.Error("editable/skills", "skill_document_collection_missing", "Skill Flow Document的顶层集合不能为null。");
                valid = false;
            }
            if ((document.layouts ?? new List<AgentPackageSkillFlowGraphLayoutFile>())
                .Where(value => value != null)
                .GroupBy(value => value.graphId ?? string.Empty, StringComparer.Ordinal)
                .Any(value => value.Count() != 1))
            {
                report.Error("editable/skills/graphs", "skill_graph_layout_duplicate", "Skill Graph layout identity缺失或重复。");
                valid = false;
            }
            foreach (AgentPackageSkillFlowGraphFile graph in document.graphs ??
                         new List<AgentPackageSkillFlowGraphFile>())
            {
                string path = GraphPath(graph?.id);
                if (graph == null || !IsIdentity(graph.id) || !graphById.TryAdd(graph.id, graph) ||
                    !Enum.TryParse(graph.role, false, out BtsmtlSkillFlowGraphRole role) ||
                    !Enum.IsDefined(typeof(BtsmtlSkillFlowGraphRole), role) ||
                    !Enum.TryParse(graph.ownership, false, out AgentGraphOwnership ownership) ||
                    ownership == AgentGraphOwnership.Unknown ||
                    !layoutById.TryGetValue(graph.id ?? string.Empty, out AgentPackageSkillFlowGraphLayoutFile layout) ||
                    !string.Equals(layout.graphId, graph.id, StringComparison.Ordinal))
                {
                    report.Error(path, "skill_graph_invalid", "Skill Graph identity、role、ownership或layout无效。");
                    valid = false;
                    continue;
                }
                if (graph.anchors == null || graph.nodes == null || graph.edges == null || graph.blackboardDeclarations == null)
                {
                    report.Error(path, "skill_graph_collection_missing", "Skill Graph的anchors、nodes、edges和blackboardDeclarations集合不能为null。");
                    valid = false;
                }
                if (!ValidateOwner(graph, role, ownership, path, document.skills, report))
                    valid = false;
                if (!LocalIdentity(graph.id) && !Asset(graph.asset))
                {
                    report.Error(path + ".asset", "skill_graph_asset_invalid", "已有Skill Graph必须带正式资产引用。");
                    valid = false;
                }
                if (LocalIdentity(graph.id) && graph.asset != null)
                {
                    report.Error(path + ".asset", "skill_graph_local_asset_invalid", "新增Skill Graph不能同时声明资产引用。");
                    valid = false;
                }
                if (LocalIdentity(graph.id) &&
                    (role == BtsmtlSkillFlowGraphRole.Skill && ownership != AgentGraphOwnership.RootAsset ||
                     role != BtsmtlSkillFlowGraphRole.Skill && ownership != AgentGraphOwnership.Inline))
                {
                    report.Error(path + ".ownership", "skill_graph_local_ownership_invalid", "新增Skill Graph必须由RootAsset或Inline owner创建。");
                    valid = false;
                }
                var graphNodes = new HashSet<string>(StringComparer.Ordinal);
                var graphAnchors = new HashSet<string>(StringComparer.Ordinal);
                var anchorNodeIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (AgentPackageSkillGraphAnchor anchor in graph.anchors ??
                             new List<AgentPackageSkillGraphAnchor>())
                {
                    if (anchor == null || !IsAnchor(anchor.kind) || !IsIdentity(anchor.nodeId) ||
                        !graphAnchors.Add(anchor.kind) || !anchorNodeIds.Add(anchor.nodeId) ||
                        !nodeIds.Add(anchor.nodeId))
                    {
                        report.Error(path + ".anchors", "skill_graph_anchor_invalid", "Skill Graph anchor缺失、重复或非法。");
                        valid = false;
                    }
                    if (anchor != null &&
                        (anchor.kind == "@enter" || anchor.kind == "@any") &&
                        !ValidateSteps(anchor.steps, path + ".anchors[" + anchor.kind + "].steps", report))
                        valid = false;
                    if (anchor != null && !AnchorAllowed(anchor.kind, role))
                    {
                        report.Error(path + ".anchors[" + anchor.kind + "]", "skill_graph_anchor_role_invalid", "Skill Graph anchor不属于当前role。");
                        valid = false;
                    }
                }
                foreach (string requiredAnchor in RequiredAnchors(role))
                {
                    if (!graphAnchors.Contains(requiredAnchor))
                    {
                        report.Error(path + ".anchors", "skill_graph_anchor_missing", $"Skill Graph缺少必需anchor：{requiredAnchor}");
                        valid = false;
                    }
                }
                foreach (AgentPackageSkillFlowNode node in graph.nodes ??
                             new List<AgentPackageSkillFlowNode>())
                {
                    string nodePath = path + ".nodes[" + (node?.id ?? string.Empty) + "]";
                    if (node == null || !IsIdentity(node.id) || !graphNodes.Add(node.id) ||
                        !nodeIds.Add(node.id) || !AgentSkillFlowAuthoringCapabilities.TryResolveType(node.capability, out _) ||
                        AgentSkillFlowAuthoringCapabilities.IsAnchor(node.capability) ||
                        !AgentSkillFlowAuthoringCapabilities.IsAllowed(node, role))
                    {
                        report.Error(nodePath, "skill_node_invalid", "Skill Node identity、kind或所属页面无效。");
                        valid = false;
                        continue;
                    }
                    valid &= ValidateNodeProperties(node, graph, document.macros, role, nodePath, report);
                    valid &= RejectInternalFields(node.properties, nodePath + ".properties", report);
                }
                var layoutNodes = new HashSet<string>(StringComparer.Ordinal);
                if (layout.nodes == null)
                {
                    report.Error(GraphLayoutPath(graph.id) + ".nodes", "skill_graph_layout_collection_missing", "Skill Graph layout的nodes集合不能为null。");
                    valid = false;
                }
                foreach (AgentPackageSkillFlowNodeLayout layoutNode in layout.nodes ??
                             new List<AgentPackageSkillFlowNodeLayout>())
                {
                    if (layoutNode == null || !IsIdentity(layoutNode.id) || !graphNodes.Contains(layoutNode.id) ||
                        !layoutNodes.Add(layoutNode.id) || !Finite(layoutNode.x) || !Finite(layoutNode.y))
                    {
                        report.Error(GraphLayoutPath(graph.id) + ".nodes", "skill_graph_layout_invalid", "Skill Graph layout必须唯一引用当前Graph的普通节点并使用有限坐标。");
                        valid = false;
                    }
                }
                if (!layoutNodes.SetEquals(graphNodes))
                {
                    report.Error(GraphLayoutPath(graph.id) + ".nodes", "skill_graph_layout_incomplete", "Skill Graph layout必须完整覆盖普通节点，不能包含anchor或漏掉节点。");
                    valid = false;
                }
                foreach (AgentPackageSkillFlowEdge edge in graph.edges ??
                             new List<AgentPackageSkillFlowEdge>())
                {
                    string edgePath = path + ".edges[" + (edge?.id ?? string.Empty) + "]";
                    if (edge == null || !IsIdentity(edge.id) || !edgeIds.Add(edge.id) ||
                        !ValidateEndpoint(edge.from, graphNodes, graphAnchors, edgePath + ".from", report) ||
                        !ValidateEndpoint(edge.to, graphNodes, graphAnchors, edgePath + ".to", report) ||
                        (edge.kind != "flow" && edge.kind != "value"))
                    {
                        report.Error(edgePath, "skill_edge_invalid", "Skill Edge identity、类型或端点无效。");
                        valid = false;
                    }
                    else if (!ValidateEdgePortShapes(
                                 graph,
                                 edge,
                                 document.macros,
                                 edgePath,
                                 report))
                        valid = false;
                }
                valid &= ValidateBlackboard(graph, path, report);
            }
            var macroIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageSkillMacroFile macro in document.macros ??
                         new List<AgentPackageSkillMacroFile>())
            {
                if (macro == null || !macroIds.Add(macro.id))
                {
                    report.Error("editable/skills/macros", "skill_macro_identity_duplicate", "Skill Macro identity缺失或重复。");
                    valid = false;
                    continue;
                }
                if (macro.inputs == null || macro.outputs == null)
                {
                    report.Error(MacroPath(macro.id), "skill_macro_collection_missing", "Skill Macro的inputs和outputs集合不能为null。");
                    valid = false;
                }
                valid &= ValidateMacro(macro, graphById, report);
            }
            foreach (AgentPackageSkillFlowGraphFile graph in document.graphs ?? new List<AgentPackageSkillFlowGraphFile>())
            {
                bool hasMacro = macroIds.Contains(graph?.id ?? string.Empty);
                if (graph?.role == BtsmtlSkillFlowGraphRole.Subgraph.ToString() && !hasMacro ||
                    graph?.role != BtsmtlSkillFlowGraphRole.Subgraph.ToString() && hasMacro)
                {
                    report.Error(GraphPath(graph?.id), "skill_macro_graph_pair_invalid", "Subgraph Graph与Macro接口分片必须一一对应。");
                    valid = false;
                }
            }
            var timelineIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageSkillTimelineFile timeline in document.timelines ??
                         new List<AgentPackageSkillTimelineFile>())
            {
                if (timeline == null || !timelineIds.Add(timeline.id))
                {
                    report.Error("editable/skills/timelines", "skill_timeline_identity_duplicate", "Skill Timeline identity缺失或重复。");
                    valid = false;
                    continue;
                }
                valid &= ValidateTimeline(timeline, graphById, report);
            }
            valid &= ValidateSkillRoots(document.skills, graphById, report);
            valid &= ValidateReferences(document, graphById, report);
            return valid;
        }

        public static bool ValidateDefinition(
            CharacterPipelineDefinition definition,
            AgentCompileReport report)
        {
            if (!definition)
            {
                report.Error("definition", "missing_definition", "CharacterPipelineDefinition缺失。");
                return false;
            }
            try
            {
                AgentPackageSkillFlowDocument document =
                    AgentSkillFlowDocumentExporter.Export(definition, report);
                return !report.HasErrors() && Validate(document, report);
            }
            catch (Exception exception)
            {
                report.Error("definition.SkillGraphs", "skill_flow_source_invalid", exception.Message);
                return false;
            }
        }

        public static bool TryDiscoverNewFragments(
            IReadOnlyDictionary<string, JToken> candidates,
            AgentCompileReport report,
            out IReadOnlyCollection<string> discovered)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            bool valid = true;
            foreach (string path in candidates.Keys.Where(IsGraphPath).OrderBy(value => value, StringComparer.Ordinal))
            {
                string layoutPath = path.Substring(0, path.Length - "graph.json".Length) + "layout.json";
                if (!candidates.ContainsKey(layoutPath) ||
                    !TryFile(candidates, path, report, out AgentPackageSkillFlowGraphFile graph) ||
                    !TryFile(candidates, layoutPath, report, out AgentPackageSkillFlowGraphLayoutFile layout) ||
                    !LocalIdentity(graph.id) ||
                    !string.Equals(path, GraphPath(graph.id), StringComparison.Ordinal) ||
                    !string.Equals(layout.graphId, graph.id, StringComparison.Ordinal))
                {
                    report.Error(path, "skill_graph_new_pair_invalid", "新增Skill Graph必须是canonical local graph/layout文件对。");
                    valid = false;
                    continue;
                }
                result.Add(path);
                result.Add(layoutPath);
            }
            foreach (string path in candidates.Keys.Where(IsMacroPath).OrderBy(value => value, StringComparer.Ordinal))
            {
                if (!TryFile(candidates, path, report, out AgentPackageSkillMacroFile macro) ||
                    !LocalIdentity(macro.id) ||
                    !string.Equals(path, MacroPath(macro.id), StringComparison.Ordinal) ||
                    !LocalIdentity(macro.graphId) ||
                    !candidates.ContainsKey(GraphPath(macro.graphId)))
                {
                    report.Error(path, "skill_macro_new_invalid", "新增Skill Macro必须使用local identity并与新增Graph形成闭包。");
                    valid = false;
                    continue;
                }
                result.Add(path);
            }
            foreach (string path in candidates.Keys.Where(IsTimelinePath).OrderBy(value => value, StringComparer.Ordinal))
            {
                string curvesPath = path.Substring(0, path.Length - "timeline.json".Length) + "curves.json";
                if (!candidates.ContainsKey(curvesPath) ||
                    !TryFile(candidates, path, report, out AgentPackageSkillTimelineFile timeline) ||
                    !TryFile(candidates, curvesPath, report, out AgentPackageCurvesFile curves) ||
                    !LocalIdentity(timeline.id) ||
                    !string.Equals(path, TimelinePath(timeline.id), StringComparison.Ordinal) ||
                    !string.Equals(curves.timelineId, timeline.id, StringComparison.Ordinal))
                {
                    report.Error(path, "skill_timeline_new_pair_invalid", "新增Skill Timeline必须是canonical local timeline/curves文件对。");
                    valid = false;
                    continue;
                }
                result.Add(path);
                result.Add(curvesPath);
            }
            discovered = result;
            return valid;
        }

        public static bool TryDiscoverRemovedFragments(
            IReadOnlyCollection<string> declaredPaths,
            IReadOnlyCollection<string> actualPaths,
            AgentCompileReport report,
            out IReadOnlyCollection<string> discovered)
        {
            var declared = new HashSet<string>(declaredPaths ?? Array.Empty<string>(), StringComparer.Ordinal);
            var actual = new HashSet<string>(actualPaths ?? Array.Empty<string>(), StringComparer.Ordinal);
            var missing = declared.Except(actual, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
            var result = new HashSet<string>(StringComparer.Ordinal);
            bool valid = true;
            foreach (string graphPath in missing.Where(IsGraphPath).ToArray())
            {
                string layoutPath = graphPath.Substring(0, graphPath.Length - "graph.json".Length) + "layout.json";
                if (!missing.Contains(layoutPath))
                {
                    report.Error(graphPath, "skill_graph_remove_pair_incomplete", "删除Skill Graph必须同时删除graph.json与layout.json。");
                    valid = false;
                    continue;
                }
                result.Add(graphPath);
                result.Add(layoutPath);
                string segment = graphPath.Substring(GraphPrefix.Length, graphPath.Length - GraphPrefix.Length - "/graph.json".Length);
                string macroPath = MacroPrefix + segment + "/macro.json";
                foreach (string declaredMacroPath in declared.Where(value => string.Equals(value, macroPath, StringComparison.Ordinal)))
                {
                    if (!missing.Contains(declaredMacroPath))
                    {
                        report.Error(declaredMacroPath, "skill_macro_remove_closure_incomplete", "删除Skill Macro Graph必须同时删除Macro接口分片。");
                        valid = false;
                    }
                    else
                        result.Add(declaredMacroPath);
                }
            }
            foreach (string macroPath in missing.Where(IsMacroPath).ToArray())
            {
                string graphPath = declared.FirstOrDefault(value =>
                    IsGraphPath(value) &&
                    value.Substring(0, value.LastIndexOf("/", StringComparison.Ordinal)) ==
                    macroPath.Substring(0, macroPath.LastIndexOf("/", StringComparison.Ordinal)).Replace("/macros/", "/graphs/"));
                if (graphPath != null && !missing.Contains(graphPath))
                {
                    report.Error(macroPath, "skill_macro_remove_graph_present", "Skill Macro接口不能脱离其Graph分片删除。");
                    valid = false;
                }
            }
            foreach (string timelinePath in missing.Where(IsTimelinePath).ToArray())
            {
                string curvesPath = timelinePath.Substring(0, timelinePath.Length - "timeline.json".Length) + "curves.json";
                if (!missing.Contains(curvesPath))
                {
                    report.Error(timelinePath, "skill_timeline_remove_pair_incomplete", "删除Skill Timeline必须同时删除timeline.json与curves.json。");
                    valid = false;
                    continue;
                }
                result.Add(timelinePath);
                result.Add(curvesPath);
            }
            foreach (string curvesPath in missing.Where(IsTimelineCurvesPath))
            {
                string timelinePath = curvesPath.Substring(0, curvesPath.Length - "curves.json".Length) + "timeline.json";
                if (!missing.Contains(timelinePath))
                {
                    report.Error(curvesPath, "skill_timeline_remove_pair_incomplete", "删除Skill Timeline必须同时删除timeline.json与curves.json。");
                    valid = false;
                }
            }
            discovered = result;
            return valid;
        }

        static bool ValidateOwner(
            AgentPackageSkillFlowGraphFile graph,
            BtsmtlSkillFlowGraphRole role,
            AgentGraphOwnership ownership,
            string path,
            IReadOnlyList<AgentSnapshotSkillDefinition> skills,
            AgentCompileReport report)
        {
            if (graph.owner == null)
            {
                report.Error(path + ".owner", "skill_graph_owner_missing", "Skill Graph必须声明显式owner。");
                return false;
            }
            if (role == BtsmtlSkillFlowGraphRole.Skill)
            {
                if (graph.owner.kind != "skill-root" || !IsIdentity(graph.owner.skillId) ||
                    ownership != AgentGraphOwnership.RootAsset)
                {
                    report.Error(path + ".owner", "skill_root_owner_invalid", "Skill根图必须由SkillDefinition以RootAsset ownership拥有。");
                    return false;
                }
                return (skills ?? new List<AgentSnapshotSkillDefinition>())
                    .Any(value => value != null && string.Equals(value.skillId, graph.owner.skillId, StringComparison.Ordinal));
            }
            if (graph.owner.kind == "shared")
                return ownership == AgentGraphOwnership.SharedAsset && Asset(graph.asset);
            if (graph.owner.kind != "node" && graph.owner.kind != "step" && graph.owner.kind != "timeline-clip")
            {
                report.Error(path + ".owner", "skill_graph_owner_kind_invalid", "Skill Graph owner kind无效。");
                return false;
            }
            if (!IsIdentity(graph.owner.graphId) || !IsIdentity(graph.owner.nodeId) ||
                !IsIdentity(graph.owner.referenceKey) ||
                graph.owner.kind == "timeline-clip" &&
                (!IsIdentity(graph.owner.timelineId) || !IsIdentity(graph.owner.trackId) ||
                 !IsIdentity(graph.owner.clipId)))
            {
                report.Error(path + ".owner", "skill_graph_owner_reference_invalid", "Skill Graph owner必须声明完整的Graph、Node、reference或Timeline Clip身份。");
                return false;
            }
            return ownership == AgentGraphOwnership.Inline || ownership == AgentGraphOwnership.SharedAsset;
        }

        static bool ValidateNodeProperties(
            AgentPackageSkillFlowNode node,
            AgentPackageSkillFlowGraphFile graph,
            IReadOnlyList<AgentPackageSkillMacroFile> macros,
            BtsmtlSkillFlowGraphRole role,
            string path,
            AgentCompileReport report)
        {
            bool valid = true;
            if (node.properties == null || node.values == null)
            {
                report.Error(path, "skill_node_payload_missing", "Skill Node的properties和values必须是object。");
                valid = false;
            }
            JObject properties = node.properties ?? new JObject();
            var allowed = new HashSet<string>(
                AgentSkillFlowAuthoringCapabilities.ExportCatalog()
                    .FirstOrDefault(value => string.Equals(value.kind, node.capability, StringComparison.Ordinal))
                    ?.properties ?? new List<string>(),
                StringComparer.Ordinal);
            if (node.capability == "state" || node.capability == "sequence" ||
                node.capability == "selector" || node.capability == "parallel")
                allowed.Add("steps");
            foreach (JProperty property in properties.Properties())
            {
                if (!allowed.Contains(property.Name))
                {
                    report.Error(path + ".properties." + property.Name, "skill_node_property_unknown", "Skill Node property未在Capability中声明。");
                    valid = false;
                }
            }
            if (node.capability == "state" || node.capability == "sequence" ||
                node.capability == "selector" || node.capability == "parallel")
                valid &= ValidateSteps(properties["steps"] as JArray, path + ".properties.steps", report);
            if (node.capability == "locomotion-input-motion")
            {
                valid &= EnumProperty(properties, "displacementMode", typeof(LocomotionInputMotionDisplacementMode), path, report);
                valid &= EnumProperty(properties, "executionMode", typeof(LocomotionInputMotionExecutionMode), path, report);
                valid &= Number(properties, "moveSpeed", path, report, 0d);
                valid &= Number(properties, "turnSpeedDegrees", path, report, 0.000001d);
                valid &= Number(properties, "durationSeconds", path, report, 0d);
            }
            if (node.capability == "timeline")
            {
                if (!IsIdentity(properties.Value<string>("timelineId")))
                {
                    report.Error(path + ".properties.timelineId", "skill_timeline_reference_invalid", "Skill Timeline节点必须引用稳定Timeline identity。");
                    valid = false;
                }
                valid &= EnumProperty(properties, "timelineOwnership", typeof(BtsmtlSkillTimelineOwnership), path, report);
                valid &= EnumProperty(properties, "playbackMode", typeof(TimelinePlaybackMode), path, report);
            }
            if (node.capability == "submit-action-lifecycle")
            {
                valid &= EnumProperty(properties, "transitionType", typeof(ActionLifecycleTransitionType), path, report);
                if (properties.Value<string>("transitionType") == ActionLifecycleTransitionType.None.ToString())
                {
                    report.Error(path + ".properties.transitionType", "skill_action_transition_invalid", "Skill Action lifecycle transition不能是None。");
                    valid = false;
                }
            }
            if (node.capability == "can-activate-action" &&
                properties["targetSnapshot"] != null &&
                properties["targetSnapshot"] is not JObject)
            {
                report.Error(path + ".properties.targetSnapshot", "skill_target_snapshot_invalid", "Skill CanActivate节点的targetSnapshot必须是object。");
                valid = false;
            }
            if (node.capability == "exposed-property" &&
                properties.Value<string>("accessMode") != "get" &&
                properties.Value<string>("accessMode") != "set")
            {
                report.Error(path + ".properties.accessMode", "skill_blackboard_access_mode_invalid", "Skill Blackboard accessMode只能是get或set。");
                valid = false;
            }
            if (node.capability == "exposed-property" &&
                (!AgentSkillFlowAuthoringCapabilities.TryResolveValueType(properties.Value<string>("valueType"), out Type blackboardNodeType) ||
                 blackboardNodeType == typeof(Flow) ||
                 blackboardNodeType == typeof(uint) ||
                 blackboardNodeType == typeof(ulong) ||
                 blackboardNodeType == typeof(ActionTargetSnapshot)))
            {
                report.Error(path + ".properties.valueType", "skill_blackboard_node_type_invalid", "Skill Blackboard节点valueType必须是Boolean、Integer、Number、Identity、Vector2或Vector3。");
                valid = false;
            }
            if (node.capability == "pipeline-blackboard-bool" &&
                properties.Value<string>("valueType") != "bool")
            {
                report.Error(path + ".properties.valueType", "skill_blackboard_node_type_invalid", "布尔Skill Blackboard读取节点必须声明bool类型。");
                valid = false;
            }
            if (node.capability == "pipeline-blackboard-float" &&
                properties.Value<string>("valueType") != "float")
            {
                report.Error(path + ".properties.valueType", "skill_blackboard_node_type_invalid", "数值Skill Blackboard读取节点必须声明float类型。");
                valid = false;
            }
            valid &= ValidateRequiredNodeProperties(node, properties, path, report);
            if (node.values != null && node.values.Type != JTokenType.Object)
            {
                report.Error(path + ".values", "skill_node_values_invalid", "Skill Node values必须是object。");
                valid = false;
            }
            if (node.values is JObject values)
            {
                IReadOnlyList<AgentPackagePortDescriptor> ports =
                    AgentSkillFlowAuthoringCapabilities.ProjectPorts(node, graph, macros);
                foreach (JProperty property in values.Properties())
                {
                    AgentPackagePortDescriptor port = ports.FirstOrDefault(value =>
                        value.key == property.Name && value.direction == "Input" &&
                        !string.IsNullOrEmpty(value.valueType));
                    if (port == null || !ValueTokenMatches(property.Value, port.valueType))
                    {
                        report.Error(path + ".values." + property.Name, "skill_node_value_invalid", "Skill Node常量值必须匹配声明的Value Input port。");
                        valid = false;
                    }
                }
            }
            return valid;
        }

        static bool ValueTokenMatches(JToken value, string type)
        {
            return type switch
            {
                "bool" => value.Type == JTokenType.Boolean,
                "int" or "uint" or "ulong" => value.Type == JTokenType.Integer,
                "float" => value.Type == JTokenType.Integer || value.Type == JTokenType.Float,
                "string" => value.Type == JTokenType.String,
                "vector2" => Vector(value, "x", "y"),
                "vector3" => Vector(value, "x", "y", "z"),
                "action-target-snapshot" => ActionTarget(value),
                _ => false
            };
        }

        static bool ActionTarget(JToken value)
        {
            return value is JObject target &&
                   target.Properties().Select(property => property.Name).ToHashSet(StringComparer.Ordinal)
                       .SetEquals(new[] { "targetId", "x", "y", "z", "rx", "ry", "rz", "rw" }) &&
                   target.Value<string>("targetId") != null &&
                   new[] { "x", "y", "z", "rx", "ry", "rz", "rw" }
                       .All(field => target[field]?.Type == JTokenType.Integer || target[field]?.Type == JTokenType.Float);
        }

        static bool Vector(JToken value, params string[] fields)
        {
            if (value is not JObject objectValue ||
                !objectValue.Properties().Select(property => property.Name).ToHashSet(StringComparer.Ordinal).SetEquals(fields))
                return false;
            return fields.All(field => objectValue[field]?.Type == JTokenType.Integer || objectValue[field]?.Type == JTokenType.Float);
        }

        static bool ValidateRequiredNodeProperties(
            AgentPackageSkillFlowNode node,
            JObject properties,
            string path,
            AgentCompileReport report)
        {
            string[] required = node.capability switch
            {
                "state-machine" => new[] { "graphId" },
                "state" => new[] { "bodyGraphId", "steps" },
                "sequence" or "selector" or "parallel" => new[] { "steps" },
                "timeline" => new[] { "timelineId", "timelineOwnership", "playbackMode" },
                "macro-call" => new[] { "graphId" },
                "character-input-bool" or "character-input-float" or
                    "character-input-vector2" or "character-input-vector2-magnitude" or
                    "character-action-request" => new[] { "inputId" },
                "action-window-active" => new[] { "windowType" },
                "action-context-active" => new[] { "actionContext" },
                "can-activate-action" => new[] { "actionProfile" },
                "submit-action-lifecycle" => new[] { "transitionType" },
                "pipeline-blackboard-bool" or "pipeline-blackboard-float" => new[] { "declarationId", "ownerId", "valueType" },
                "exposed-property" => new[] { "declarationId", "ownerId", "valueType", "accessMode" },
                _ => Array.Empty<string>()
            };
            bool valid = true;
            foreach (string key in required)
            {
                JToken value = properties[key];
                bool present = value != null && value.Type != JTokenType.Null &&
                               (value.Type != JTokenType.String || !string.IsNullOrWhiteSpace(value.Value<string>()));
                if (!present)
                {
                    report.Error(path + ".properties." + key, "skill_node_property_required", $"Skill Node必须声明{key}。");
                    valid = false;
                }
            }
            return valid;
        }

        static bool ValidateSteps(JArray value, string path, AgentCompileReport report)
        {
            if (value == null)
            {
                report.Error(path, "skill_steps_missing", "组合节点必须声明稳定steps集合。");
                return false;
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            bool valid = true;
            foreach (JObject step in value.OfType<JObject>())
            {
                string id = step.Value<string>("id");
                if (!IsIdentity(id) || !ids.Add(id) || step.Value<string>("name") == null ||
                    !Enum.TryParse(step.Value<string>("abortPolicy"), false, out ProgramAbortPolicy _))
                {
                    report.Error(path, "skill_step_invalid", "Skill step必须有唯一id、name和合法abortPolicy。");
                    valid = false;
                }
            }
            return valid && value.Count == ids.Count;
        }

        static bool ValidateSteps(
            IReadOnlyList<AgentPackageSkillFlowStep> value,
            string path,
            AgentCompileReport report)
        {
            if (value == null)
            {
                report.Error(path, "skill_steps_missing", "组合anchor必须声明稳定steps集合。");
                return false;
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            bool valid = true;
            foreach (AgentPackageSkillFlowStep step in value)
            {
                if (step == null || !IsIdentity(step.id) || !ids.Add(step.id) ||
                    step.name == null || !Enum.TryParse(step.abortPolicy, false, out ProgramAbortPolicy _))
                {
                    report.Error(path, "skill_step_invalid", "Skill step必须有唯一id、name和合法abortPolicy。");
                    valid = false;
                }
            }
            return valid && ids.Count == value.Count;
        }

        static bool ValidateBlackboard(
            AgentPackageSkillFlowGraphFile graph,
            string path,
            AgentCompileReport report)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            bool valid = true;
            foreach (AgentPackageSkillBlackboardDeclaration declaration in graph.blackboardDeclarations ??
                         new List<AgentPackageSkillBlackboardDeclaration>())
            {
                if (declaration == null || !IsIdentity(declaration.id) || !ids.Add(declaration.id) ||
                    string.IsNullOrWhiteSpace(declaration.key) ||
                    !AgentSkillFlowAuthoringCapabilities.TryResolveValueType(declaration.valueType, out Type type) ||
                    type == typeof(Flow) || type == typeof(uint) || type == typeof(ulong) ||
                    !Enum.TryParse(declaration.scope, false, out PipelineBlackboardVariableScope _) ||
                    !Enum.TryParse(declaration.lifetime, false, out PipelineBlackboardVariableLifetime _))
                {
                    report.Error(path + ".blackboardDeclarations", "skill_blackboard_invalid", "Skill Blackboard declaration字段无效。");
                    valid = false;
                    continue;
                }
                if (!PipelineBlackboardVariablePolicy.IsValid(
                        Enum.Parse<PipelineBlackboardVariableScope>(declaration.scope, false),
                        Enum.Parse<PipelineBlackboardVariableLifetime>(declaration.lifetime, false)))
                {
                    report.Error(path + ".blackboardDeclarations[" + declaration.id + "]", "skill_blackboard_policy_invalid", "Skill Blackboard scope与lifetime不匹配。");
                    valid = false;
                }
                if (declaration.inputBinding != null && string.IsNullOrWhiteSpace(declaration.inputBinding.inputValueId))
                {
                    report.Error(path + ".blackboardDeclarations[" + declaration.id + "]", "skill_blackboard_input_invalid", "Blackboard inputBinding必须提供inputValueId。");
                    valid = false;
                }
                if (declaration.inputBinding != null &&
                    (type != typeof(ActionTargetSnapshot) ||
                     declaration.scope != PipelineBlackboardVariableScope.Character.ToString() ||
                     declaration.lifetime != PipelineBlackboardVariableLifetime.Spawn.ToString()))
                {
                    report.Error(path + ".blackboardDeclarations[" + declaration.id + "]", "skill_blackboard_input_type_invalid", "Blackboard inputBinding必须使用ActionTargetSnapshot与Character/Spawn作用域。");
                    valid = false;
                }
                if (declaration.factProjection != null &&
                    (type != typeof(bool) || declaration.factProjection.kind != PipelineBlackboardFactProjectionKind.ActionWindow.ToString()))
                {
                    report.Error(path + ".blackboardDeclarations[" + declaration.id + "]", "skill_blackboard_fact_invalid", "Blackboard factProjection必须是Bool ActionWindow声明。");
                    valid = false;
                }
                if (declaration.factProjection != null &&
                    (declaration.scope != PipelineBlackboardVariableScope.Frame.ToString() ||
                     declaration.lifetime != PipelineBlackboardVariableLifetime.Frame.ToString() ||
                     string.IsNullOrWhiteSpace(declaration.factProjection.windowType) ||
                     string.IsNullOrWhiteSpace(declaration.factProjection.windowId)))
                {
                    report.Error(path + ".blackboardDeclarations[" + declaration.id + "]", "skill_blackboard_fact_scope_invalid", "ActionWindow factProjection必须使用Frame/Frame并指定窗口身份。");
                    valid = false;
                }
                if (declaration.defaultValue != null &&
                    declaration.defaultValue.Type != JTokenType.Null &&
                    !ValueTokenMatches(declaration.defaultValue, declaration.valueType))
                {
                    report.Error(path + ".blackboardDeclarations[" + declaration.id + "].defaultValue", "skill_blackboard_default_invalid", "Skill Blackboard defaultValue必须匹配声明类型。");
                    valid = false;
                }
            }
            return valid;
        }

        static bool ValidateMacro(
            AgentPackageSkillMacroFile macro,
            IReadOnlyDictionary<string, AgentPackageSkillFlowGraphFile> graphs,
            AgentCompileReport report)
        {
            string path = MacroPath(macro?.id);
            if (macro == null || !IsIdentity(macro.id) || !IsIdentity(macro.graphId) ||
                !graphs.TryGetValue(macro.graphId, out AgentPackageSkillFlowGraphFile graph) ||
                graph.role != BtsmtlSkillFlowGraphRole.Subgraph.ToString() ||
                macro.id != graph.id ||
                macro.owner == null ||
                macro.owner.kind != graph.owner?.kind ||
                macro.owner.graphId != graph.owner?.graphId ||
                macro.owner.nodeId != graph.owner?.nodeId ||
                !Enum.TryParse(macro.ownership, false, out AgentGraphOwnership ownership) ||
                ownership == AgentGraphOwnership.Unknown ||
                !string.Equals(macro.ownership, graph.ownership, StringComparison.Ordinal) ||
                !LocalIdentity(macro.id) && !Asset(macro.asset))
            {
                report.Error(path, "skill_macro_invalid", "Skill Macro必须引用Subgraph Graph。");
                return false;
            }
            bool valid = true;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            int flowInputs = 0;
            foreach (AgentPackageSkillMacroParameter parameter in macro.inputs ??
                         new List<AgentPackageSkillMacroParameter>())
            {
                if (!ValidParameter(parameter, ids, path + ".inputs", report))
                    valid = false;
                if (parameter?.valueType == "flow")
                    flowInputs++;
            }
            foreach (AgentPackageSkillMacroParameter parameter in macro.outputs ??
                         new List<AgentPackageSkillMacroParameter>())
                if (!ValidParameter(parameter, ids, path + ".outputs", report))
                    valid = false;
            if ((macro.outputs ?? new List<AgentPackageSkillMacroParameter>()).Any(value => value?.valueType == "flow"))
            {
                report.Error(path + ".outputs", "skill_macro_flow_output_invalid", "Skill Macro不支持flow output，只能声明值output。");
                valid = false;
            }
            if (flowInputs != 1)
            {
                report.Error(path + ".inputs", "skill_macro_flow_input_invalid", "Skill Macro必须有且仅有一个flow input。");
                valid = false;
            }
            return valid;
        }

        static bool ValidParameter(AgentPackageSkillMacroParameter parameter, ISet<string> ids, string path, AgentCompileReport report)
        {
            if (parameter == null || !IsIdentity(parameter.id) || !ids.Add(parameter.id) ||
                string.IsNullOrWhiteSpace(parameter.name) ||
                !AgentSkillFlowAuthoringCapabilities.TryResolveValueType(parameter.valueType, out _) ||
                parameter.valueType == "action-target-snapshot")
            {
                report.Error(path, "skill_macro_parameter_invalid", "Macro parameter必须有稳定identity、名称和稳定值类型。");
                return false;
            }
            return true;
        }

        static bool ValidateTimeline(
            AgentPackageSkillTimelineFile timeline,
            IReadOnlyDictionary<string, AgentPackageSkillFlowGraphFile> graphs,
            AgentCompileReport report)
        {
            string path = TimelinePath(timeline?.id);
            if (timeline == null || !IsIdentity(timeline.id) ||
                !Enum.TryParse(timeline.ownership, false, out BtsmtlSkillTimelineOwnership _) ||
                timeline.asset == null && !LocalIdentity(timeline.id) ||
                LocalIdentity(timeline.id) && timeline.asset != null ||
                !LocalIdentity(timeline.id) && !Asset(timeline.asset) ||
                LocalIdentity(timeline.id) && timeline.ownership != BtsmtlSkillTimelineOwnership.Private.ToString() ||
                timeline.sections == null || timeline.externalBindings == null || timeline.tracks == null)
            {
                report.Error(path, "skill_timeline_invalid", "Skill Timeline identity、ownership或资产引用无效。");
                return false;
            }
            TimelineContractCatalog catalog = TimelineTreeContractComposition.Create();
            bool valid = IsIdentity(timeline.ownerGraphId) && IsIdentity(timeline.ownerNodeId) &&
                         graphs.ContainsKey(timeline.ownerGraphId);
            if (!valid)
                report.Error(path + ".ownerGraphId", "skill_timeline_owner_invalid", "Skill Timeline必须声明存在的owner Graph与owner Node。");
            if (valid &&
                (!(graphs[timeline.ownerGraphId].nodes ?? new List<AgentPackageSkillFlowNode>()).Any(value => value != null && value.id == timeline.ownerNodeId &&
                    value.capability == "timeline" &&
                    value.properties?.Value<string>("timelineId") == timeline.id)))
            {
                report.Error(path + ".ownerNodeId", "skill_timeline_owner_reference_invalid", "Skill Timeline必须由对应Timeline节点反向拥有。");
                valid = false;
            }
            if (timeline.callSites == null)
            {
                report.Error(path + ".callSites", "skill_timeline_call_sites_missing", "Skill Timeline callSites集合不能为null。");
                valid = false;
            }
            var callSites = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageSkillTimelineCallSite callSite in timeline.callSites ?? new List<AgentPackageSkillTimelineCallSite>())
            {
                bool callSiteValid = callSite != null &&
                    IsIdentity(callSite.graphId) &&
                    IsIdentity(callSite.nodeId) &&
                    graphs.TryGetValue(callSite.graphId, out AgentPackageSkillFlowGraphFile callGraph) &&
                    (callGraph.nodes ?? new List<AgentPackageSkillFlowNode>()).Any(value =>
                        value != null && value.id == callSite.nodeId && value.capability == "timeline" &&
                        value.properties?.Value<string>("timelineId") == timeline.id) &&
                    Enum.TryParse(callSite.playbackMode, false, out TimelinePlaybackMode playbackMode) &&
                    Enum.IsDefined(typeof(TimelinePlaybackMode), playbackMode) &&
                    callSites.Add(callSite.graphId + "\0" + callSite.nodeId);
                if (!callSiteValid)
                {
                    report.Error(path + ".callSites", "skill_timeline_call_site_invalid", "Skill Timeline callSite必须唯一引用对应的Timeline节点。");
                    valid = false;
                }
            }
            if (callSites.Count == 0)
            {
                report.Error(path + ".callSites", "skill_timeline_call_site_missing", "Skill Timeline至少需要一个正式调用位置。");
                valid = false;
            }
            var sectionIds = new HashSet<string>(StringComparer.Ordinal);
            var sectionNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageSkillTimelineSection section in timeline.sections ??
                         new List<AgentPackageSkillTimelineSection>())
            {
                if (section == null || !IsIdentity(section.id) || !sectionIds.Add(section.id) ||
                    string.IsNullOrWhiteSpace(section.name) || section.name != section.name.Trim() ||
                    !sectionNames.Add(section.name) || section.frame < 0)
                {
                    report.Error(path + ".sections", "skill_timeline_section_invalid", "Skill Timeline Section必须有唯一identity、名称和非负frame。");
                    valid = false;
                }
            }
            var bindingIds = new HashSet<string>(StringComparer.Ordinal);
            var bindingNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageSkillTimelineExternalBinding binding in timeline.externalBindings ??
                         new List<AgentPackageSkillTimelineExternalBinding>())
            {
                if (binding == null || !IsIdentity(binding.id) || !bindingIds.Add(binding.id) ||
                    !IsIdentity(binding.bindingId) || !bindingNames.Add(binding.bindingId) ||
                    string.IsNullOrWhiteSpace(binding.displayName) || binding.displayName != binding.displayName.Trim() ||
                    string.IsNullOrWhiteSpace(binding.domain) || binding.domain != binding.domain.Trim() ||
                    !Enum.TryParse(binding.valueKind, false, out TimelineBindingValueKind valueKind) ||
                    !Enum.IsDefined(typeof(TimelineBindingValueKind), valueKind) ||
                    !Enum.TryParse(binding.access, false, out TimelineBindingAccess access) ||
                    !Enum.IsDefined(typeof(TimelineBindingAccess), access) ||
                    !Enum.TryParse(binding.lifetime, false, out TimelineBindingLifetime lifetime) ||
                    !Enum.IsDefined(typeof(TimelineBindingLifetime), lifetime) ||
                    valueKind == TimelineBindingValueKind.Target && !string.IsNullOrEmpty(binding.parameterId) ||
                    valueKind != TimelineBindingValueKind.Target && !IsIdentity(binding.parameterId) ||
                    access == TimelineBindingAccess.Input && lifetime != TimelineBindingLifetime.Call ||
                    access != TimelineBindingAccess.Input && lifetime != TimelineBindingLifetime.Tick)
                {
                    report.Error(path + ".externalBindings", "skill_timeline_binding_invalid", "Skill Timeline ExternalBinding字段无效。");
                    valid = false;
                }
            }
            var trackIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageSkillTimelineTrack track in timeline.tracks ??
                         new List<AgentPackageSkillTimelineTrack>())
            {
                if (track == null || !IsIdentity(track.id) || !trackIds.Add(track.id) ||
                    track.clips == null ||
                    !catalog.TryGetTrack(track.kind, out _))
                {
                    report.Error(path + ".tracks", "skill_timeline_track_invalid", "Skill Timeline track identity或kind无效。");
                    valid = false;
                    continue;
                }
                if (track.kind == TimelineContractKinds.AnimationTrack && !IsIdentity(track.animationChannelId))
                {
                    report.Error(path + ".tracks[" + track.id + "].animationChannelId", "skill_timeline_animation_channel_invalid", "AnimationTrack必须声明稳定AnimationChannel identity。");
                    valid = false;
                }
                var clipIds = new HashSet<string>(StringComparer.Ordinal);
                var curveIds = new HashSet<string>(StringComparer.Ordinal);
                catalog.TryGetTrack(track.kind, out TimelineTrackContract trackContract);
                foreach (AgentPackageSkillTimelineClip clip in track.clips ??
                         new List<AgentPackageSkillTimelineClip>())
                {
                    catalog.TryGetClip(clip?.kind, out TimelineClipContract clipContract);
                    if (clip == null || !IsIdentity(clip.id) || !clipIds.Add(clip.id) ||
                        clipContract == null ||
                        !trackContract.AllowsClip(clip.kind) ||
                        clip.startFrame < 0 || clip.endFrame < clip.startFrame ||
                        clipContract.RequiresPositiveDuration && clip.endFrame <= clip.startFrame ||
                        clip.clipInFrame < 0)
                    {
                        report.Error(path + ".tracks[" + track.id + "].clips", "skill_timeline_clip_invalid", "Skill Timeline clip字段无效。");
                        valid = false;
                    }
                    valid &= ValidateClipProperties(
                        clip,
                        clipContract,
                        timeline.externalBindings,
                        graphs,
                        path + ".tracks[" + track.id + "].clips[" + (clip?.id ?? string.Empty) + "]",
                        report);
                    if (!string.IsNullOrEmpty(clip?.treeGraphId) &&
                        (!graphs.ContainsKey(clip.treeGraphId) || clip.treeOwnership != TimelineTreeOwnership.AssetGraph.ToString()))
                    {
                        report.Error(path + ".tracks[" + track.id + "].clips[" + clip.id + "]", "skill_tree_clip_reference_invalid", "Skill TreeClip必须引用Skill TimelineBody Graph。");
                        valid = false;
                    }
                    foreach (AgentPackageCurve curve in clip?.curves ?? new List<AgentPackageCurve>())
                        if (curve == null || curve.clipId != clip.id ||
                            !curveIds.Add(clip.id + "\0" + curve.channelId) ||
                            !ValidateCurve(curve, path + ".tracks[" + track.id + "].clips[" + clip.id + "].curves", report))
                            valid = false;
                    valid &= RejectInternalFields(
                        clip?.properties,
                        path + ".tracks[" + track.id + "].clips[" + (clip?.id ?? string.Empty) + "].properties",
                        report);
                }
            }
            return valid;
        }

        static bool ValidateClipProperties(
            AgentPackageSkillTimelineClip clip,
            TimelineClipContract contract,
            IReadOnlyList<AgentPackageSkillTimelineExternalBinding> bindings,
            IReadOnlyDictionary<string, AgentPackageSkillFlowGraphFile> graphs,
            string path,
            AgentCompileReport report)
        {
            if (clip == null || contract == null)
                return false;
            JObject properties = clip.properties ?? new JObject();
            string[] allowed = clip.kind switch
            {
                TimelineContractKinds.AnimationClip => new[] { "extraPolationMode" },
                TimelineContractKinds.MotionCurveClip => new[] { "curveId", "curveEndFrame", "space", "channel", "blendMode", "priority", "consumeLowerChannels" },
                TimelineContractKinds.MotionWarpClip => new[]
                {
                    "sourceMotionClipId", "translationMode", "targetOffsetSpace", "rotationMode", "rotationMethod",
                    "targetPlanarOffset", "targetYawOffsetDegrees", "maxTotalPositionCorrection",
                    "maxTotalYawCorrectionDegrees", "maximumYawRateDegreesPerSecond", "limitPolicy"
                },
                TimelineContractKinds.ActionCueClip => new[] { "cueId", "cueType" },
                TimelineContractKinds.CameraStateClip => new[] { "mode", "priority", "blendInSeconds", "blendOutSeconds", "targetKey", "interruptPolicy" },
                TimelineContractKinds.CameraCueClip => new[] { "cueId", "cueKind", "cueType", "intensity", "durationSeconds", "priority" },
                TimelineContractKinds.CameraResponseClip => new[] { "lookResponse", "manualOrbitWeight", "pitchResponseWeight", "yawResponseWeight", "priority" },
                TimelineContractKinds.ScenePresentationParameterCurveClip => new[] { "targetBindingId", "parameterBindingId", "valueCurve" },
                _ => Array.Empty<string>()
            };
            var allowedProperties = new HashSet<string>(allowed, StringComparer.Ordinal);
            bool valid = true;
            foreach (JProperty property in properties.Properties())
                if (!allowedProperties.Contains(property.Name))
                {
                    report.Error(path + ".properties." + property.Name, "skill_timeline_property_unknown", "Skill Timeline Clip property未在正式Clip kind中声明。");
                    valid = false;
                }

            switch (clip.kind)
            {
                case TimelineContractKinds.AnimationClip:
                    valid &= Asset(clip.animationClip);
                    valid &= EnumProperty(properties, "extraPolationMode", typeof(ExtraPolationMode), path, report);
                    break;
                case TimelineContractKinds.MotionCurveClip:
                    valid &= TextProperty(properties, "curveId", path, report);
                    valid &= IntegerProperty(properties, "curveEndFrame", path, report);
                    valid &= EnumProperty(properties, "space", typeof(TimelineMotionContributionSpace), path, report);
                    valid &= EnumProperty(properties, "channel", typeof(TimelineMotionChannel), path, report);
                    valid &= EnumProperty(properties, "blendMode", typeof(TimelineMotionBlendMode), path, report);
                    valid &= IntegerProperty(properties, "priority", path, report);
                    valid &= BooleanProperty(properties, "consumeLowerChannels", path, report);
                    if (properties.Value<int?>("curveEndFrame") is int curveEnd &&
                        (curveEnd <= clip.startFrame || curveEnd > clip.endFrame))
                    {
                        report.Error(path + ".properties.curveEndFrame", "skill_motion_curve_range_invalid", "MotionCurve curveEndFrame必须位于clip范围内。");
                        valid = false;
                    }
                    break;
                case TimelineContractKinds.MotionWarpClip:
                    valid &= TextProperty(properties, "sourceMotionClipId", path, report);
                    valid &= EnumProperty(properties, "translationMode", typeof(MotionWarpTranslationMode), path, report);
                    valid &= EnumProperty(properties, "targetOffsetSpace", typeof(MotionWarpTargetOffsetSpace), path, report);
                    valid &= EnumProperty(properties, "rotationMode", typeof(MotionWarpRotationMode), path, report);
                    valid &= EnumProperty(properties, "rotationMethod", typeof(MotionWarpRotationMethod), path, report);
                    valid &= Vector(properties["targetPlanarOffset"], "x", "y");
                    valid &= Number(properties, "targetYawOffsetDegrees", path, report, double.MinValue);
                    valid &= Number(properties, "maxTotalPositionCorrection", path, report, 0d);
                    valid &= Number(properties, "maxTotalYawCorrectionDegrees", path, report, 0d);
                    valid &= Number(properties, "maximumYawRateDegreesPerSecond", path, report, 0d);
                    valid &= EnumProperty(properties, "limitPolicy", typeof(MotionWarpLimitPolicy), path, report);
                    break;
                case TimelineContractKinds.TreeClip:
                    if (!IsIdentity(clip.treeGraphId) ||
                        !graphs.ContainsKey(clip.treeGraphId) ||
                        graphs[clip.treeGraphId].role != BtsmtlSkillFlowGraphRole.TimelineBody.ToString() ||
                        clip.treeOwnership != TimelineTreeOwnership.AssetGraph.ToString() ||
                        !Enum.TryParse(clip.treePhase, false, out TimelineTreeExecutionPhase treePhase) ||
                        !Enum.IsDefined(typeof(TimelineTreeExecutionPhase), treePhase))
                    {
                        report.Error(path, "skill_tree_clip_reference_invalid", "TreeClip必须引用TimelineBody Graph并声明合法执行阶段。");
                        valid = false;
                    }
                    break;
                case TimelineContractKinds.ActionCueClip:
                    valid &= TextProperty(properties, "cueId", path, report);
                    valid &= TextProperty(properties, "cueType", path, report);
                    break;
                case TimelineContractKinds.CameraStateClip:
                    valid &= EnumProperty(properties, "mode", typeof(TimelineCameraMode), path, report);
                    valid &= IntegerProperty(properties, "priority", path, report);
                    valid &= Number(properties, "blendInSeconds", path, report, 0d);
                    valid &= Number(properties, "blendOutSeconds", path, report, 0d);
                    valid &= TextProperty(properties, "targetKey", path, report, false);
                    valid &= EnumProperty(properties, "interruptPolicy", typeof(TimelineCameraInterruptPolicy), path, report);
                    break;
                case TimelineContractKinds.CameraCueClip:
                    valid &= TextProperty(properties, "cueId", path, report);
                    valid &= EnumProperty(properties, "cueKind", typeof(TimelineCameraCueKind), path, report);
                    valid &= TextProperty(properties, "cueType", path, report);
                    valid &= Number(properties, "intensity", path, report, 0d);
                    valid &= Number(properties, "durationSeconds", path, report, 0d);
                    valid &= IntegerProperty(properties, "priority", path, report);
                    break;
                case TimelineContractKinds.CameraResponseClip:
                    valid &= EnumProperty(properties, "lookResponse", typeof(TimelineCameraLookResponseMode), path, report);
                    valid &= Number(properties, "manualOrbitWeight", path, report, 0d, 1d);
                    valid &= Number(properties, "pitchResponseWeight", path, report, 0d, 1d);
                    valid &= Number(properties, "yawResponseWeight", path, report, 0d, 1d);
                    valid &= IntegerProperty(properties, "priority", path, report);
                    break;
                case TimelineContractKinds.ScenePresentationParameterCurveClip:
                    string targetBindingId = properties.Value<string>("targetBindingId");
                    string parameterBindingId = properties.Value<string>("parameterBindingId");
                    AgentPackageSkillTimelineExternalBinding targetBinding = (bindings ?? new List<AgentPackageSkillTimelineExternalBinding>())
                        .FirstOrDefault(value => value != null && value.bindingId == targetBindingId);
                    AgentPackageSkillTimelineExternalBinding parameterBinding = (bindings ?? new List<AgentPackageSkillTimelineExternalBinding>())
                        .FirstOrDefault(value => value != null && value.bindingId == parameterBindingId);
                    if (!IsIdentity(targetBindingId) ||
                        targetBinding == null ||
                        targetBinding.valueKind != TimelineBindingValueKind.Target.ToString() ||
                        targetBinding.access != TimelineBindingAccess.Input.ToString() ||
                        targetBinding.lifetime != TimelineBindingLifetime.Call.ToString() ||
                        !IsIdentity(parameterBindingId) ||
                        parameterBinding == null ||
                        (parameterBinding.valueKind != TimelineBindingValueKind.Scalar.ToString() &&
                         parameterBinding.valueKind != TimelineBindingValueKind.Boolean.ToString()) ||
                        parameterBinding.access != TimelineBindingAccess.Write.ToString() ||
                        parameterBinding.lifetime != TimelineBindingLifetime.Tick.ToString() ||
                        !IsIdentity(parameterBinding.parameterId) ||
                        !ValidateInlineCurve(properties["valueCurve"], path + ".properties.valueCurve", report))
                    {
                        report.Error(path + ".properties", "skill_scene_presentation_clip_invalid", "Scene Presentation Clip必须引用有效ExternalBinding并声明valueCurve。");
                        valid = false;
                    }
                    break;
            }
            return valid;
        }

        static bool TextProperty(JObject properties, string name, string path, AgentCompileReport report, bool required = true)
        {
            string value = properties.Value<string>(name);
            if (required && (string.IsNullOrWhiteSpace(value) || value != value.Trim()))
            {
                report.Error(path + ".properties." + name, "skill_timeline_property_text_invalid", $"属性{name}必须是非空trim字符串。");
                return false;
            }
            if (!required && value != null && value != value.Trim())
            {
                report.Error(path + ".properties." + name, "skill_timeline_property_text_invalid", $"属性{name}必须是trim字符串。");
                return false;
            }
            return true;
        }

        static bool IntegerProperty(JObject properties, string name, string path, AgentCompileReport report)
        {
            JToken value = properties[name];
            if (value == null || value.Type != JTokenType.Integer)
            {
                report.Error(path + ".properties." + name, "skill_timeline_property_integer_invalid", $"属性{name}必须是整数。");
                return false;
            }
            return true;
        }

        static bool BooleanProperty(JObject properties, string name, string path, AgentCompileReport report)
        {
            JToken value = properties[name];
            if (value == null || value.Type != JTokenType.Boolean)
            {
                report.Error(path + ".properties." + name, "skill_timeline_property_boolean_invalid", $"属性{name}必须是布尔值。");
                return false;
            }
            return true;
        }

        static bool ValidateInlineCurve(JToken token, string path, AgentCompileReport report)
        {
            if (token is not JObject value ||
                !Enum.TryParse(value.Value<string>("preWrapMode"), false, out WrapMode preWrap) ||
                !Enum.IsDefined(typeof(WrapMode), preWrap) ||
                !Enum.TryParse(value.Value<string>("postWrapMode"), false, out WrapMode postWrap) ||
                !Enum.IsDefined(typeof(WrapMode), postWrap) ||
                value["keys"] is not JArray keys || keys.Count == 0)
            {
                report.Error(path, "skill_timeline_curve_invalid", "Timeline内嵌curve必须声明合法wrap mode与keys。");
                return false;
            }
            float previous = -1f;
            foreach (JObject key in keys.OfType<JObject>())
            {
                if (!Finite(key.Value<float>("time")) || key.Value<float>("time") < 0f ||
                    key.Value<float>("time") > 1f || key.Value<float>("time") <= previous ||
                    !Finite(key.Value<float>("value")) || !Finite(key.Value<float>("inTangent")) ||
                    !Finite(key.Value<float>("outTangent")) || !Finite(key.Value<float>("inWeight")) ||
                    !Finite(key.Value<float>("outWeight")) ||
                    !Enum.TryParse(key.Value<string>("weightedMode"), false, out WeightedMode weightedMode) ||
                    !Enum.IsDefined(typeof(WeightedMode), weightedMode))
                {
                    report.Error(path, "skill_timeline_curve_key_invalid", "Timeline内嵌curve key必须按normalized time递增并使用有限数值。");
                    return false;
                }
                previous = key.Value<float>("time");
            }
            return keys.OfType<JObject>().Count() == keys.Count;
        }

        static bool RejectInternalFields(JToken token, string path, AgentCompileReport report)
        {
            if (token is JObject value)
            {
                bool valid = true;
                foreach (JProperty property in value.Properties())
                {
                    if (property.Name.StartsWith("m_", StringComparison.Ordinal) ||
                        property.Name == "typeName" ||
                        property.Name.IndexOf("runtime", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        property.Name.IndexOf("compiled", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        property.Name.IndexOf("generated", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        property.Name.IndexOf("cache", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        report.Error(path + "." + property.Name, "skill_internal_field_forbidden", "Skill Flow Document禁止私有字段、运行状态和编译缓存。");
                        valid = false;
                    }
                    valid &= RejectInternalFields(property.Value, path + "." + property.Name, report);
                }
                return valid;
            }
            if (token is JArray array)
                return array.Select((value, index) => RejectInternalFields(value, path + "[" + index + "]", report)).All(value => value);
            return true;
        }

        static bool ValidateCurve(AgentPackageCurve curve, string path, AgentCompileReport report)
        {
            if (curve == null || string.IsNullOrWhiteSpace(curve.channelId) ||
                !TimelineCurveChannelCatalog.TryGet(curve.channelId, out TimelineCurveChannelDescriptor descriptor) ||
                curve.timeDomain != descriptor.TimeDomain.ToString() ||
                curve.bounded != descriptor.ValueDomain.IsBounded ||
                curve.minimum != descriptor.ValueDomain.Minimum ||
                curve.maximum != descriptor.ValueDomain.Maximum ||
                curve.zero != descriptor.ValueDomain.Zero ||
                curve.unit != descriptor.ValueDomain.Unit ||
                !Enum.TryParse(curve.preWrapMode, false, out WrapMode pre) ||
                !Enum.IsDefined(typeof(WrapMode), pre) ||
                !Enum.TryParse(curve.postWrapMode, false, out WrapMode post) ||
                !Enum.IsDefined(typeof(WrapMode), post) ||
                curve.keys == null || curve.keys.Count == 0)
            {
                report.Error(path, "skill_timeline_curve_invalid", "Skill Timeline curve channel或keys无效。");
                return false;
            }
            float previous = -1f;
            foreach (AgentAnimationCurveKey key in curve.keys)
            {
                if (key == null || key.time < 0f || key.time > 1f || key.time <= previous ||
                    !Finite(key.time) || !Finite(key.value) || !Finite(key.inTangent) ||
                    !Finite(key.outTangent) || !Finite(key.inWeight) || !Finite(key.outWeight) ||
                    !Enum.TryParse(key.weightedMode, false, out WeightedMode weighted) ||
                    !Enum.IsDefined(typeof(WeightedMode), weighted))
                {
                    report.Error(path, "skill_timeline_curve_key_invalid", "Skill Timeline curve key必须按normalized time递增并使用有限数值。");
                    return false;
                }
                previous = key.time;
            }
            return true;
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        static bool ValidateSkillRoots(
            IReadOnlyList<AgentSnapshotSkillDefinition> skills,
            IReadOnlyDictionary<string, AgentPackageSkillFlowGraphFile> graphs,
            AgentCompileReport report)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var skillById = new Dictionary<string, AgentSnapshotSkillDefinition>(StringComparer.Ordinal);
            bool valid = true;
            if (skills == null || skills.Count == 0)
            {
                report.Error("editable/skills", "skill_definition_missing", "Skill Flow Document至少需要一个SkillDefinition。");
                return false;
            }
            foreach (AgentSnapshotSkillDefinition skill in skills ??
                         new List<AgentSnapshotSkillDefinition>())
            {
                if (skill == null || !IsIdentity(skill.skillId) || !ids.Add(skill.skillId) ||
                    !IsIdentity(skill.entryGraphAuthoringId) ||
                    !graphs.TryGetValue(skill.entryGraphAuthoringId, out AgentPackageSkillFlowGraphFile graph) ||
                    graph.role != BtsmtlSkillFlowGraphRole.Skill.ToString())
                {
                    report.Error("editable/skills", "skill_root_invalid", "SkillDefinition必须引用Document中的Skill根Graph。");
                    valid = false;
                    continue;
                }
                skillById.Add(skill.skillId, skill);
                if (!IsIdentity(skill.actionProfileId) || !IsIdentity(skill.actionContext))
                {
                    report.Error("editable/skills[" + skill.skillId + "]", "skill_reference_invalid", "SkillDefinition必须引用稳定ActionProfile与ActionContext identity。");
                    valid = false;
                }
                var dependencies = new HashSet<string>(StringComparer.Ordinal);
                foreach (AgentSnapshotSkillSubgraphDependency dependency in skill.subgraphDependencies ??
                             new List<AgentSnapshotSkillSubgraphDependency>())
                {
                    bool dependencyValid = dependency != null &&
                        IsIdentity(dependency.subgraphIdentity) &&
                        graphs.TryGetValue(dependency.subgraphIdentity, out AgentPackageSkillFlowGraphFile subgraph) &&
                        subgraph.role == BtsmtlSkillFlowGraphRole.Subgraph.ToString() &&
                        IsIdentity(dependency.callSiteIdentity) &&
                        dependencies.Add(dependency.subgraphIdentity + "\0" + dependency.callSiteIdentity) &&
                        TryResolveMacroCallSite(
                            skill.skillId,
                            dependency.callSiteIdentity,
                            dependency.subgraphIdentity,
                            graphs);
                    if (!dependencyValid)
                    {
                        report.Error("editable/skills[" + skill.skillId + "].subgraphDependencies", "skill_dependency_invalid", "Skill子图依赖必须引用唯一的Subgraph与调用位置。");
                        valid = false;
                    }
                }
            }
            foreach (AgentSnapshotSkillDefinition skill in skills ??
                         new List<AgentSnapshotSkillDefinition>())
            {
                if (skill == null)
                    continue;
                var followUps = new HashSet<string>(StringComparer.Ordinal);
                foreach (string followUp in skill.allowedFollowUpSkillIds ?? new List<string>())
                {
                    if (!IsIdentity(followUp) || followUp == skill.skillId ||
                        !followUps.Add(followUp) || !skillById.ContainsKey(followUp))
                    {
                        report.Error("editable/skills[" + skill.skillId + "].allowedFollowUpSkillIds", "skill_follow_up_invalid", "Skill后续技能引用必须指向已声明且非自身的唯一Skill。");
                        valid = false;
                    }
                }
            }
            return valid;
        }

        static bool ValidateReferences(
            AgentPackageSkillFlowDocument document,
            IReadOnlyDictionary<string, AgentPackageSkillFlowGraphFile> graphs,
            AgentCompileReport report)
        {
            var timelines = (document.timelines ?? new List<AgentPackageSkillTimelineFile>())
                .Where(value => value != null && !string.IsNullOrEmpty(value.id))
                .GroupBy(value => value.id, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.First(), StringComparer.Ordinal);
            bool valid = true;
            foreach (AgentPackageSkillFlowGraphFile graph in document.graphs ??
                         new List<AgentPackageSkillFlowGraphFile>())
            {
                if (graph == null)
                {
                    valid = false;
                    continue;
                }
                foreach (AgentPackageSkillFlowNode node in graph.nodes ??
                             new List<AgentPackageSkillFlowNode>())
                {
                    if (node == null)
                    {
                        valid = false;
                        continue;
                    }
                    valid &= ValidateBlackboardNodeReference(
                        node,
                        graphs,
                        GraphPath(graph.id) + ".nodes[" + node.id + "]",
                        report);
                    string child = node.properties?.Value<string>("graphId");
                    if (string.IsNullOrEmpty(child))
                        child = node.properties?.Value<string>("bodyGraphId");
                    if (string.IsNullOrEmpty(child))
                        child = node.properties?.Value<string>("stateMachineGraphId");
                    if (!string.IsNullOrEmpty(child) && !graphs.ContainsKey(child))
                    {
                        report.Error(GraphPath(graph.id) + ".nodes[" + node.id + "]", "skill_graph_reference_missing", $"Skill Graph reference不存在：{child}");
                        valid = false;
                    }
                    if (!string.IsNullOrEmpty(child) && graphs.TryGetValue(child, out AgentPackageSkillFlowGraphFile childGraph))
                    {
                        string expectedRole = node.capability == "macro-call"
                            ? BtsmtlSkillFlowGraphRole.Subgraph.ToString()
                            : node.capability == "state-machine"
                                ? BtsmtlSkillFlowGraphRole.StateMachine.ToString()
                                : node.capability == "state"
                                    ? BtsmtlSkillFlowGraphRole.StateBody.ToString()
                                    : string.Empty;
                        if (!string.IsNullOrEmpty(expectedRole) && childGraph.role != expectedRole)
                        {
                            report.Error(GraphPath(graph.id) + ".nodes[" + node.id + "]", "skill_graph_reference_role_invalid", "Skill Graph引用目标role与节点能力不一致。");
                            valid = false;
                        }
                    }
                    if (node.capability == "timeline")
                    {
                        string timelineId = node.properties?.Value<string>("timelineId");
                        if (string.IsNullOrEmpty(timelineId) || !timelines.ContainsKey(timelineId))
                        {
                            report.Error(GraphPath(graph.id) + ".nodes[" + node.id + "]", "skill_timeline_reference_missing", "Skill Timeline节点必须引用Document中的Timeline。");
                            valid = false;
                        }
                    }
                    JArray steps = node.properties?["steps"] as JArray;
                    foreach (JObject step in steps?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                    {
                        string condition = step.Value<string>("conditionGraphId");
                        if (!string.IsNullOrEmpty(condition) && !graphs.ContainsKey(condition))
                        {
                            report.Error(GraphPath(graph.id) + ".nodes[" + node.id + "].properties.steps", "skill_condition_graph_missing", $"Condition Graph不存在：{condition}");
                            valid = false;
                        }
                        if (!string.IsNullOrEmpty(condition) && graphs.TryGetValue(condition, out AgentPackageSkillFlowGraphFile conditionGraph) &&
                            conditionGraph.role != BtsmtlSkillFlowGraphRole.ConditionRule.ToString())
                        {
                            report.Error(GraphPath(graph.id) + ".nodes[" + node.id + "].properties.steps", "skill_condition_graph_role_invalid", "Step condition必须引用ConditionRule Graph。");
                            valid = false;
                        }
                    }
                }
                foreach (AgentPackageSkillGraphAnchor anchor in graph.anchors ?? new List<AgentPackageSkillGraphAnchor>())
                    foreach (AgentPackageSkillFlowStep step in anchor?.steps ?? new List<AgentPackageSkillFlowStep>())
                        if (!string.IsNullOrEmpty(step?.conditionGraphId) && !graphs.ContainsKey(step.conditionGraphId))
                        {
                            report.Error(GraphPath(graph.id) + ".anchors[" + anchor.kind + "].steps", "skill_condition_graph_missing", $"Condition Graph不存在：{step.conditionGraphId}");
                            valid = false;
                        }
                        else if (!string.IsNullOrEmpty(step?.conditionGraphId) &&
                                 graphs.TryGetValue(step.conditionGraphId, out AgentPackageSkillFlowGraphFile anchorCondition) &&
                                 anchorCondition.role != BtsmtlSkillFlowGraphRole.ConditionRule.ToString())
                        {
                            report.Error(GraphPath(graph.id) + ".anchors[" + anchor.kind + "].steps", "skill_condition_graph_role_invalid", "Step condition必须引用ConditionRule Graph。");
                            valid = false;
                        }
            }
            foreach (AgentPackageSkillFlowGraphFile graph in document.graphs ??
                         new List<AgentPackageSkillFlowGraphFile>())
            {
                if (graph == null)
                {
                    valid = false;
                    continue;
                }
                if (graph.owner == null || graph.owner.kind == "skill-root")
                    continue;
                if (!string.IsNullOrEmpty(graph.owner.graphId) && !graphs.ContainsKey(graph.owner.graphId))
                {
                    report.Error(GraphPath(graph.id) + ".owner", "skill_owner_graph_missing", "Skill Graph owner graph不存在。");
                    valid = false;
                }
                if (!graphs.TryGetValue(graph.owner.graphId ?? string.Empty, out AgentPackageSkillFlowGraphFile ownerGraph))
                    continue;
                AgentPackageSkillFlowNode ownerNode = (ownerGraph.nodes ?? new List<AgentPackageSkillFlowNode>())
                    .FirstOrDefault(value => value != null && value.id == graph.owner.nodeId);
                AgentPackageSkillGraphAnchor ownerAnchor = (ownerGraph.anchors ?? new List<AgentPackageSkillGraphAnchor>())
                    .FirstOrDefault(value => value != null && value.nodeId == graph.owner.nodeId);
                bool referenced = false;
                if (ownerNode != null)
                {
                    referenced = ownerNode.properties?.Value<string>("graphId") == graph.id ||
                                 ownerNode.properties?.Value<string>("bodyGraphId") == graph.id ||
                                 ownerNode.properties?.Value<string>("stateMachineGraphId") == graph.id ||
                                 (ownerNode.properties?["steps"] as JArray)?.OfType<JObject>()
                                     .Any(value => value.Value<string>("id") == graph.owner.referenceKey &&
                                                  value.Value<string>("conditionGraphId") == graph.id) == true;
                }
                if (ownerAnchor != null && graph.owner.kind == "step")
                    referenced = (ownerAnchor.steps ?? new List<AgentPackageSkillFlowStep>())
                        .Any(value => value != null && value.id == graph.owner.referenceKey &&
                                      value.conditionGraphId == graph.id);
                if (graph.owner.kind == "timeline-clip")
                    referenced = timelines.Values.SelectMany(value => value.tracks ?? new List<AgentPackageSkillTimelineTrack>())
                        .SelectMany(value => value?.clips ?? new List<AgentPackageSkillTimelineClip>())
                        .Any(value => value != null && value.id == graph.owner.clipId && value.treeGraphId == graph.id);
                if (!referenced && graph.owner.kind != "shared")
                {
                    report.Error(GraphPath(graph.id) + ".owner", "skill_owner_reference_missing", "Skill Graph owner没有对应的正式调用引用。");
                    valid = false;
                }
            }
            var reachable = new HashSet<string>(StringComparer.Ordinal);
            var pending = new Stack<string>((document.skills ?? new List<AgentSnapshotSkillDefinition>())
                .Where(value => value != null && !string.IsNullOrEmpty(value.entryGraphAuthoringId))
                .Select(value => value.entryGraphAuthoringId));
            while (pending.Count > 0)
            {
                string graphId = pending.Pop();
                if (!reachable.Add(graphId) || !graphs.TryGetValue(graphId, out AgentPackageSkillFlowGraphFile graph))
                    continue;
                foreach (AgentPackageSkillFlowNode node in graph.nodes ?? new List<AgentPackageSkillFlowNode>())
                {
                    string child = node.properties?.Value<string>("graphId") ??
                                   node.properties?.Value<string>("bodyGraphId") ??
                                   node.properties?.Value<string>("stateMachineGraphId");
                    if (!string.IsNullOrEmpty(child))
                        pending.Push(child);
                    foreach (JObject step in (node.properties?["steps"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                        if (!string.IsNullOrEmpty(step.Value<string>("conditionGraphId")))
                            pending.Push(step.Value<string>("conditionGraphId"));
                    if (node.capability != "timeline")
                        continue;
                    string timelineId = node.properties?.Value<string>("timelineId");
                    if (!timelines.TryGetValue(timelineId ?? string.Empty, out AgentPackageSkillTimelineFile timeline))
                        continue;
                    foreach (AgentPackageSkillTimelineClip clip in (timeline.tracks ?? new List<AgentPackageSkillTimelineTrack>()).SelectMany(value => value?.clips ?? new List<AgentPackageSkillTimelineClip>()))
                        if (!string.IsNullOrEmpty(clip.treeGraphId))
                            pending.Push(clip.treeGraphId);
                }
                foreach (AgentPackageSkillGraphAnchor anchor in graph.anchors ?? new List<AgentPackageSkillGraphAnchor>())
                    foreach (AgentPackageSkillFlowStep step in anchor?.steps ?? new List<AgentPackageSkillFlowStep>())
                        if (!string.IsNullOrEmpty(step?.conditionGraphId))
                            pending.Push(step.conditionGraphId);
            }
            foreach (string graphId in graphs.Keys)
                if (!reachable.Contains(graphId) && graphs[graphId].owner?.kind != "shared")
                {
                    report.Error(GraphPath(graphId), "skill_graph_unreachable", "Skill Graph未处于任何Skill入口的正式闭包中。");
                    valid = false;
                }
            valid &= ValidateGraphCycles(document, graphs, timelines, report);
            return valid;
        }

        static bool TryResolveMacroCallSite(
            string skillId,
            string callSite,
            string subgraphId,
            IReadOnlyDictionary<string, AgentPackageSkillFlowGraphFile> graphs)
        {
            string prefix = "skill:" + skillId + "/";
            if (!callSite.StartsWith(prefix, StringComparison.Ordinal) ||
                !callSite.EndsWith("/call:Macro", StringComparison.Ordinal))
                return false;
            int nodeMarker = callSite.LastIndexOf("/node:", StringComparison.Ordinal);
            if (nodeMarker < 0)
                return false;
            int graphMarker = callSite.LastIndexOf("/graph:", nodeMarker);
            if (graphMarker < prefix.Length || nodeMarker <= graphMarker)
                return false;
            string ownerGraphId = callSite.Substring(
                graphMarker + "/graph:".Length,
                nodeMarker - graphMarker - "/graph:".Length);
            string nodeId = callSite.Substring(
                nodeMarker + "/node:".Length,
                callSite.Length - nodeMarker - "/node:".Length - "/call:Macro".Length);
            return graphs.TryGetValue(ownerGraphId, out AgentPackageSkillFlowGraphFile owner) &&
                   (owner.nodes ?? new List<AgentPackageSkillFlowNode>()).Any(value =>
                       value != null &&
                       value.id == nodeId &&
                       value.capability == "macro-call" &&
                       value.properties?.Value<string>("graphId") == subgraphId);
        }

        static bool ValidateBlackboardNodeReference(
            AgentPackageSkillFlowNode node,
            IReadOnlyDictionary<string, AgentPackageSkillFlowGraphFile> graphs,
            string path,
            AgentCompileReport report)
        {
            if (node == null)
                return false;
            bool blackboardNode = node.capability == "pipeline-blackboard-bool" ||
                                  node.capability == "pipeline-blackboard-float" ||
                                  node.capability == "exposed-property";
            bool valid = true;
            if (blackboardNode)
            {
                string declarationId = node.properties?.Value<string>("declarationId");
                string ownerId = node.properties?.Value<string>("ownerId");
                AgentPackageSkillBlackboardDeclaration declaration = null;
                if (graphs.TryGetValue(ownerId ?? string.Empty, out AgentPackageSkillFlowGraphFile owner))
                    declaration = (owner.blackboardDeclarations ?? new List<AgentPackageSkillBlackboardDeclaration>())
                        .FirstOrDefault(value => value != null && value.id == declarationId);
                if (declaration == null)
                {
                    report.Error(path + ".properties", "skill_blackboard_reference_missing", "Skill Blackboard节点必须引用owner Graph中的正式declaration。");
                    valid = false;
                }
                else
                {
                    string expectedType = node.capability == "pipeline-blackboard-bool"
                        ? "bool"
                        : node.capability == "pipeline-blackboard-float"
                            ? "float"
                            : node.properties?.Value<string>("valueType");
                    if (!string.Equals(expectedType, declaration.valueType, StringComparison.Ordinal))
                    {
                        report.Error(path + ".properties.valueType", "skill_blackboard_reference_type_invalid", "Skill Blackboard节点类型必须与owner declaration类型一致。");
                        valid = false;
                    }
                }
            }
            if (node.capability == "can-activate-action" &&
                node.properties?["targetSnapshot"] is JObject targetSnapshot)
            {
                string declarationId = targetSnapshot.Value<string>("id");
                string ownerId = targetSnapshot.Value<string>("ownerId");
                if (string.IsNullOrEmpty(declarationId) && string.IsNullOrEmpty(ownerId))
                    return valid;
                AgentPackageSkillBlackboardDeclaration declaration = null;
                if (graphs.TryGetValue(ownerId ?? string.Empty, out AgentPackageSkillFlowGraphFile owner))
                    declaration = (owner.blackboardDeclarations ?? new List<AgentPackageSkillBlackboardDeclaration>())
                        .FirstOrDefault(value => value != null && value.id == declarationId);
                if (declaration == null || declaration.valueType != "action-target-snapshot")
                {
                    report.Error(path + ".properties.targetSnapshot", "skill_target_snapshot_reference_invalid", "Skill CanActivate节点的TargetSnapshot必须引用ActionTargetSnapshot declaration。");
                    valid = false;
                }
            }
            return valid;
        }

        static bool ValidateGraphCycles(
            AgentPackageSkillFlowDocument document,
            IReadOnlyDictionary<string, AgentPackageSkillFlowGraphFile> graphs,
            IReadOnlyDictionary<string, AgentPackageSkillTimelineFile> timelines,
            AgentCompileReport report)
        {
            var states = new Dictionary<string, int>(StringComparer.Ordinal);
            bool valid = true;
            bool Visit(string graphId, string path)
            {
                if (!graphs.TryGetValue(graphId ?? string.Empty, out AgentPackageSkillFlowGraphFile graph))
                    return true;
                if (states.TryGetValue(graphId, out int state))
                {
                    if (state == 1)
                    {
                        report.Error(path, "skill_graph_recursive", $"Skill Graph闭包包含递归引用：{graphId}");
                        return false;
                    }
                    return true;
                }
                states[graphId] = 1;
                bool result = true;
                foreach (AgentPackageSkillFlowNode node in graph.nodes ?? new List<AgentPackageSkillFlowNode>())
                {
                    if (node == null)
                        continue;
                    string child = node.properties?.Value<string>("graphId") ??
                                   node.properties?.Value<string>("bodyGraphId") ??
                                   node.properties?.Value<string>("stateMachineGraphId");
                    if (!string.IsNullOrEmpty(child))
                        result &= Visit(child, GraphPath(graph.id) + ".nodes[" + node.id + "]");
                    foreach (JObject step in (node.properties?["steps"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                        if (!string.IsNullOrEmpty(step.Value<string>("conditionGraphId")))
                            result &= Visit(
                                step.Value<string>("conditionGraphId"),
                                GraphPath(graph.id) + ".nodes[" + node.id + "].properties.steps");
                    if (node.capability != "timeline" ||
                        !timelines.TryGetValue(node.properties?.Value<string>("timelineId") ?? string.Empty, out AgentPackageSkillTimelineFile timeline))
                        continue;
                    foreach (AgentPackageSkillTimelineClip clip in (timeline.tracks ?? new List<AgentPackageSkillTimelineTrack>())
                                 .SelectMany(value => value?.clips ?? new List<AgentPackageSkillTimelineClip>()))
                        if (!string.IsNullOrEmpty(clip.treeGraphId))
                            result &= Visit(clip.treeGraphId, TimelinePath(timeline.id) + ".tracks.clips[" + clip.id + "]");
                }
                foreach (AgentPackageSkillGraphAnchor anchor in graph.anchors ?? new List<AgentPackageSkillGraphAnchor>())
                    foreach (AgentPackageSkillFlowStep step in anchor?.steps ?? new List<AgentPackageSkillFlowStep>())
                        if (!string.IsNullOrEmpty(step?.conditionGraphId))
                            result &= Visit(step.conditionGraphId, GraphPath(graph.id) + ".anchors[" + anchor.kind + "].steps");
                states[graphId] = 2;
                return result;
            }

            foreach (AgentSnapshotSkillDefinition skill in document.skills ?? new List<AgentSnapshotSkillDefinition>())
                if (skill != null)
                    valid &= Visit(skill.entryGraphAuthoringId, "editable/skills[" + skill.skillId + "]");
            return valid;
        }

        static bool ValidateEndpoint(
            AgentPackageSkillFlowEdgeEndpoint endpoint,
            ISet<string> nodes,
            ISet<string> anchors,
            string path,
            AgentCompileReport report)
        {
            if (endpoint == null || string.IsNullOrWhiteSpace(endpoint.node) || string.IsNullOrWhiteSpace(endpoint.port) ||
                !nodes.Contains(endpoint.node) && !anchors.Contains(endpoint.node))
            {
                report.Error(path, "skill_edge_endpoint_invalid", "Skill Edge endpoint必须引用当前Graph node或anchor及逻辑port。");
                return false;
            }
            return true;
        }

        static bool ValidateEdgePortShapes(
            AgentPackageSkillFlowGraphFile graph,
            AgentPackageSkillFlowEdge edge,
            IReadOnlyList<AgentPackageSkillMacroFile> macros,
            string path,
            AgentCompileReport report)
        {
            AgentPackagePortDescriptor from = ResolvePort(graph, edge.from, macros, false);
            AgentPackagePortDescriptor to = ResolvePort(graph, edge.to, macros, true);
            if (from == null || to == null ||
                !string.Equals(from.direction, "Output", StringComparison.Ordinal) ||
                !string.Equals(to.direction, "Input", StringComparison.Ordinal) ||
                edge.kind == "flow" && (!string.IsNullOrEmpty(from.valueType) || !string.IsNullOrEmpty(to.valueType)) ||
                edge.kind == "value" && (string.IsNullOrEmpty(from.valueType) || string.IsNullOrEmpty(to.valueType) ||
                                           !string.Equals(from.valueType, to.valueType, StringComparison.Ordinal)))
            {
                report.Error(path, "skill_edge_port_shape_invalid", "Skill Edge必须连接同一类型的合法输出与输入port。");
                return false;
            }
            return true;
        }

        static AgentPackagePortDescriptor ResolvePort(
            AgentPackageSkillFlowGraphFile graph,
            AgentPackageSkillFlowEdgeEndpoint endpoint,
            IReadOnlyList<AgentPackageSkillMacroFile> macros,
            bool input)
        {
            AgentPackageSkillFlowNode node = (graph.nodes ?? new List<AgentPackageSkillFlowNode>())
                .FirstOrDefault(value => value != null && value.id == endpoint.node);
            if (node != null)
                return AgentSkillFlowAuthoringCapabilities.ProjectPorts(node, graph, macros)
                    .FirstOrDefault(value => value.key == endpoint.port &&
                                             string.Equals(value.direction, input ? "Input" : "Output", StringComparison.Ordinal));
            AgentPackageSkillGraphAnchor anchor = (graph.anchors ?? new List<AgentPackageSkillGraphAnchor>())
                .FirstOrDefault(value => value != null && value.kind == endpoint.node);
            if (anchor == null)
                return null;
            if (anchor.kind == "@enter" || anchor.kind == "@any")
                return anchor.steps.FirstOrDefault(value => value != null && value.id == endpoint.port) is AgentPackageSkillFlowStep step
                    ? new AgentPackagePortDescriptor
                    {
                        key = step.id,
                        direction = "Output",
                        valueType = string.Empty,
                        capacity = "Multiple"
                    }
                    : null;
            if (anchor.kind == "@root" || anchor.kind == "@onEnter" || anchor.kind == "@onExit" ||
                anchor.kind == "@timelineEnable" || anchor.kind == "@timelineDisable" || anchor.kind == "@timelineDestroy")
                return endpoint.port == "Output"
                    ? new AgentPackagePortDescriptor { key = "Output", direction = "Output", capacity = "Multiple" }
                    : null;
            if (anchor.kind == "@exit")
                return endpoint.port == "StateIn"
                    ? new AgentPackagePortDescriptor { key = "StateIn", direction = "Input", capacity = "Single" }
                    : null;
            if (anchor.kind == "@result")
                return endpoint.port == "m_Result"
                    ? new AgentPackagePortDescriptor { key = "m_Result", direction = "Input", valueType = "bool", capacity = "Single" }
                    : null;
            if (anchor.kind == "@input")
            {
                AgentPackageSkillMacroFile macro = (macros ?? new List<AgentPackageSkillMacroFile>())
                    .FirstOrDefault(value => value != null && value.id == graph.id);
                AgentPackageSkillMacroParameter parameter = macro?.inputs?.FirstOrDefault(value => value?.id == endpoint.port);
                return parameter == null || input ? null :
                    parameter.valueType == "flow"
                        ? new AgentPackagePortDescriptor { key = parameter.id, direction = "Output", capacity = "Multiple" }
                        : new AgentPackagePortDescriptor { key = parameter.id, direction = "Output", valueType = parameter.valueType, capacity = "Multiple" };
            }
            if (anchor.kind == "@output")
            {
                AgentPackageSkillMacroFile macro = (macros ?? new List<AgentPackageSkillMacroFile>())
                    .FirstOrDefault(value => value != null && value.id == graph.id);
                AgentPackageSkillMacroParameter parameter = macro?.outputs?.FirstOrDefault(value => value?.id == endpoint.port);
                return parameter == null || !input ? null :
                    parameter.valueType == "flow"
                        ? new AgentPackagePortDescriptor { key = parameter.id, direction = "Input", capacity = "Single" }
                        : new AgentPackagePortDescriptor { key = parameter.id, direction = "Input", valueType = parameter.valueType, capacity = "Single" };
            }
            return null;
        }

        static void ApplyCurves(AgentPackageSkillTimelineFile timeline, AgentPackageCurvesFile curves)
        {
            var byClip = (timeline.tracks ?? new List<AgentPackageSkillTimelineTrack>())
                .SelectMany(track => track?.clips ?? new List<AgentPackageSkillTimelineClip>())
                .ToDictionary(clip => clip.id, clip => clip, StringComparer.Ordinal);
            foreach (AgentPackageCurve curve in curves.curves ?? new List<AgentPackageCurve>())
                if (byClip.TryGetValue(curve?.clipId ?? string.Empty, out AgentPackageSkillTimelineClip clip))
                    clip.curves.Add(curve);
        }

        static bool EnumProperty(JObject properties, string name, Type type, string path, AgentCompileReport report)
        {
            string value = properties.Value<string>(name);
            if (string.IsNullOrWhiteSpace(value) || !Enum.IsDefined(type, value))
            {
                report.Error(path + ".properties." + name, "skill_property_enum_invalid", $"属性{name}不是合法枚举值。");
                return false;
            }
            return true;
        }

        static bool Number(
            JObject properties,
            string name,
            string path,
            AgentCompileReport report,
            double minimum,
            double maximum = double.MaxValue)
        {
            JToken value = properties[name];
            if (value == null || value.Type != JTokenType.Float && value.Type != JTokenType.Integer ||
                value.Value<double>() < minimum || value.Value<double>() > maximum ||
                double.IsNaN(value.Value<double>()) || double.IsInfinity(value.Value<double>()))
            {
                report.Error(path + ".properties." + name, "skill_property_number_invalid", $"属性{name}数值无效。");
                return false;
            }
            return true;
        }

        static bool TryFile<T>(IReadOnlyDictionary<string, JToken> files, string path, AgentCompileReport report, out T value)
        {
            value = default;
            if (!files.TryGetValue(path, out JToken token))
            {
                report.Error(path, "document_file_missing", $"Manifest缺少必需文件：{path}");
                return false;
            }
            return AgentAuthoringDocumentCodec.TryConvertToken(token, path, report, out value);
        }

        static bool IsIdentity(string value) => AgentPackageMappingSupport.IsIdentity(value);
        static bool LocalIdentity(string value) => IsIdentity(value) && value.StartsWith("local:", StringComparison.Ordinal);
        static bool IsAnchor(string value) => AgentSkillFlowAuthoringCapabilities.IsAnchor(value);

        static bool AnchorAllowed(string kind, BtsmtlSkillFlowGraphRole role)
        {
            return RequiredAnchors(role).Contains(kind, StringComparer.Ordinal);
        }

        static IReadOnlyList<string> RequiredAnchors(BtsmtlSkillFlowGraphRole role)
        {
            return role switch
            {
                BtsmtlSkillFlowGraphRole.Skill => new[] { "@root" },
                BtsmtlSkillFlowGraphRole.Subgraph => new[] { "@input", "@output" },
                BtsmtlSkillFlowGraphRole.StateMachine => new[] { "@enter", "@any", "@exit" },
                BtsmtlSkillFlowGraphRole.ConditionRule => new[] { "@result" },
                BtsmtlSkillFlowGraphRole.StateBody => new[] { "@onEnter", "@root", "@onExit" },
                BtsmtlSkillFlowGraphRole.TimelineBody => new[] { "@timelineEnable", "@root", "@timelineDisable", "@timelineDestroy" },
                _ => Array.Empty<string>()
            };
        }

        static bool Asset(AgentPackageObjectReference value)
        {
            return value != null && string.IsNullOrWhiteSpace(value.localId) &&
                   !string.IsNullOrWhiteSpace(value.assetPath) && value.assetPath.StartsWith("Assets/", StringComparison.Ordinal) &&
                   !value.assetPath.Contains("\\") && value.assetGuid?.Length == 32 &&
                   value.assetGuid.All(character => character >= '0' && character <= '9' || character >= 'a' && character <= 'f') &&
                   value.localFileId != 0;
        }
    }

    internal sealed class AgentSkillFlowDocumentExporter
    {
        readonly AgentCompileReport m_Report;
        readonly AgentPackageSkillFlowDocument m_Document = new AgentPackageSkillFlowDocument();
        readonly Dictionary<string, AgentPackageSkillFlowGraphFile> m_Graphs = new Dictionary<string, AgentPackageSkillFlowGraphFile>(StringComparer.Ordinal);
        readonly Dictionary<string, AgentPackageSkillFlowGraphLayoutFile> m_Layouts = new Dictionary<string, AgentPackageSkillFlowGraphLayoutFile>(StringComparer.Ordinal);
        readonly Dictionary<string, AgentPackageSkillMacroFile> m_Macros = new Dictionary<string, AgentPackageSkillMacroFile>(StringComparer.Ordinal);
        readonly Dictionary<string, AgentPackageSkillTimelineFile> m_Timelines = new Dictionary<string, AgentPackageSkillTimelineFile>(StringComparer.Ordinal);
        readonly HashSet<FlowGraph> m_Active = new HashSet<FlowGraph>();

        AgentSkillFlowDocumentExporter(AgentCompileReport report)
        {
            m_Report = report;
        }

        public static AgentPackageSkillFlowDocument Export(CharacterPipelineDefinition definition, AgentCompileReport report)
        {
            var exporter = new AgentSkillFlowDocumentExporter(report);
            exporter.ExportDefinition(definition);
            return exporter.m_Document;
        }

        void ExportDefinition(CharacterPipelineDefinition definition)
        {
            var skillDefinitions = new List<AgentSnapshotSkillDefinition>();
            foreach (CharacterSkillAuthoringDefinition definitionEntry in definition.SkillDefinitions)
            {
                if (definitionEntry == null)
                    continue;
                skillDefinitions.Add(new AgentSnapshotSkillDefinition
                {
                    skillId = definitionEntry.SkillId,
                    entryGraphAuthoringId = definitionEntry.EntryGraphAuthoringId,
                    actionProfileId = definitionEntry.ActionProfile ? definitionEntry.ActionProfile.ActionId : string.Empty,
                    actionProfileAssetPath = AssetPath(definitionEntry.ActionProfile),
                    actionProfileAssetGuid = AssetGuid(definitionEntry.ActionProfile),
                    actionContext = definitionEntry.ActionContext ? definitionEntry.ActionContext.name : string.Empty,
                    actionContextAssetPath = AssetPath(definitionEntry.ActionContext),
                    actionContextAssetGuid = AssetGuid(definitionEntry.ActionContext),
                    sourceInputRequestId = definitionEntry.SourceInputRequestId,
                    consumeSourceInputRequest = definitionEntry.ConsumeSourceInputRequest,
                    targetInputValueId = definitionEntry.TargetInputValueId,
                    targetKey = definitionEntry.TargetKey,
                    subgraphDependencies = definitionEntry.SubgraphDependencies.Where(value => value != null).Select(value => new AgentSnapshotSkillSubgraphDependency
                    {
                        subgraphIdentity = value.SubgraphIdentity,
                        callSiteIdentity = value.CallSiteIdentity
                    }).ToList(),
                    allowedFollowUpSkillIds = definitionEntry.AllowedFollowUpSkillIds.ToList()
                });
            }
            m_Document.skills = skillDefinitions;
            var roots = definition.SkillGraphs ?? Array.Empty<BtsmtlSkillFlowGraph>();
            foreach (BtsmtlSkillFlowGraph root in roots)
            {
                string skillId = skillDefinitions.FirstOrDefault(value =>
                    string.Equals(value.entryGraphAuthoringId, root?.AuthoringId, StringComparison.Ordinal))?.skillId;
                if (root == null || root.Role != BtsmtlSkillFlowGraphRole.Skill || string.IsNullOrEmpty(skillId))
                {
                    m_Report.Error("definition.SkillGraphs", "skill_root_graph_invalid", "Definition.SkillGraphs只能包含被SkillDefinition引用的Skill根图。");
                    continue;
                }
                try
                {
                    BtsmtlSkillGraphClosure.Validate(root, true);
                }
                catch (Exception exception)
                {
                    m_Report.Error("definition.SkillGraphs[" + root.AuthoringId + "]", "skill_graph_closure_invalid", exception.Message);
                    continue;
                }
                VisitGraph(root, Owner("skill-root", skillId: skillId), skillId);
            }
            foreach (AgentSnapshotSkillDefinition skill in skillDefinitions)
                if (!m_Graphs.ContainsKey(skill.entryGraphAuthoringId))
                    m_Report.Error(
                        "definition.SkillDefinitions[" + skill.skillId + "]",
                        "skill_root_graph_missing",
                        "SkillDefinition入口没有对应的正式Skill Graph。");
            m_Document.graphs = m_Graphs.Values.OrderBy(value => value.id, StringComparer.Ordinal).ToList();
            m_Document.layouts = m_Layouts.Values.OrderBy(value => value.graphId, StringComparer.Ordinal).ToList();
            m_Document.macros = m_Macros.Values.OrderBy(value => value.id, StringComparer.Ordinal).ToList();
            m_Document.timelines = m_Timelines.Values.OrderBy(value => value.id, StringComparer.Ordinal).ToList();
        }

        void VisitGraph(FlowGraph graph, AgentPackageSkillGraphOwner owner, string skillId)
        {
            if (graph is not IBtsmtlSkillFlowGraph authoring)
            {
                m_Report.Error("editable/skills", "skill_graph_type_invalid", "Skill闭包包含非正式技能图。");
                return;
            }
            if (!m_Active.Add(graph))
            {
                m_Report.Error("skill:" + skillId, "skill_graph_recursive", "Skill Graph闭包包含递归引用。");
                return;
            }
            try
            {
                if (m_Graphs.ContainsKey(authoring.AuthoringId))
                    return;
                string graphPath = "skill:" + skillId + "/graph:" + authoring.AuthoringId;
                var file = new AgentPackageSkillFlowGraphFile
                {
                    id = authoring.AuthoringId,
                    role = authoring.Role.ToString(),
                    name = graph.name,
                    contentRevision = new BtsmtlSkillGraphFingerprint().Compute(graph),
                    ownership = owner.kind == "skill-root"
                        ? AgentGraphOwnership.RootAsset.ToString()
                        : owner.kind == "shared" || AssetDatabase.IsMainAsset(graph)
                            ? AgentGraphOwnership.SharedAsset.ToString()
                            : AgentGraphOwnership.Inline.ToString(),
                    owner = owner,
                    asset = ObjectReference(graph)
                };
                m_Graphs.Add(authoring.AuthoringId, file);
                ExportBlackboard(graph, file, graphPath);
                foreach (FlowNode node in graph.allNodes.Cast<FlowNode>().OrderBy(value => value.UID, StringComparer.Ordinal))
                {
                    if (AgentSkillFlowAuthoringCapabilities.TryGetKind(node, out string kind) &&
                        AgentSkillFlowAuthoringCapabilities.IsAnchor(kind))
                    {
                        var anchor = new AgentPackageSkillGraphAnchor { kind = kind, nodeId = node.UID };
                        if (node is BtsmtlSkillCompositeFlowNode composite)
                            anchor.steps = composite.Steps.Select(ExportStep).ToList();
                        file.anchors.Add(anchor);
                        ExportReferences(node, file, skillId);
                        continue;
                    }
                    if (!AgentSkillFlowAuthoringCapabilities.TryGetKind(node, out kind))
                    {
                        m_Report.Error(graphPath + ".nodes", "skill_node_capability_missing", $"Node '{node.GetType().Name}'没有正式Skill Capability。");
                        continue;
                    }
                    file.nodes.Add(new AgentPackageSkillFlowNode
                    {
                        id = node.UID,
                        capability = kind,
                        name = node.name,
                        properties = ExportProperties(node, file, skillId),
                        values = ExportValues(node, graphPath)
                    });
                    ExportReferences(node, file, skillId);
                }
                foreach (FlowNode node in graph.allNodes.Cast<FlowNode>())
                    foreach (BinderConnection connection in node.outConnections.OfType<BinderConnection>().OrderBy(value => value.UID, StringComparer.Ordinal))
                    {
                        if (connection.sourcePort == null || connection.targetPort == null ||
                            connection.sourcePort.type != connection.targetPort.type ||
                            connection.sourcePort.IsFlowPort() != connection.targetPort.IsFlowPort())
                        {
                            m_Report.Error(graphPath + ".edges[" + connection.UID + "]", "skill_edge_port_invalid", "Skill Edge端点缺失或类型不一致。");
                            continue;
                        }
                        file.edges.Add(new AgentPackageSkillFlowEdge
                        {
                            id = connection.UID,
                            kind = connection.sourcePort.IsFlowPort() ? "flow" : "value",
                            from = Endpoint(connection.sourceNode as FlowNode, connection.sourcePortID),
                            to = Endpoint(connection.targetNode as FlowNode, connection.targetPortID)
                        });
                    }
                m_Layouts[authoring.AuthoringId] = new AgentPackageSkillFlowGraphLayoutFile
                {
                    graphId = authoring.AuthoringId,
                    nodes = file.nodes.Select(node =>
                    {
                        FlowNode source = graph.allNodes.OfType<FlowNode>().Single(value => value.UID == node.id);
                        return new AgentPackageSkillFlowNodeLayout
                        {
                            id = node.id,
                            x = source.position.x,
                            y = source.position.y
                        };
                    }).ToList()
                };
                if (graph is BtsmtlSkillMacroGraph macro)
                    ExportMacro(macro, owner, graphPath);
            }
            finally
            {
                m_Active.Remove(graph);
            }
        }

        void ExportReferences(FlowNode node, AgentPackageSkillFlowGraphFile ownerGraph, string skillId)
        {
            string ownerId = ownerGraph.id;
            if (node is MacroNodeWrapper macroNode && macroNode.macro is BtsmtlSkillMacroGraph macro)
            {
                string kind = AssetDatabase.IsMainAsset(macro) ? "shared" : "node";
                VisitGraph(macro, Owner(kind, graphId: ownerId, nodeId: node.UID, referenceKey: "macro", asset: ObjectReference(macro)), skillId);
            }
            if (node is BtsmtlSkillStateMachineFlowNode stateMachine && stateMachine.StateMachine)
                VisitGraph(stateMachine.StateMachine, Owner("node", graphId: ownerId, nodeId: node.UID, referenceKey: "stateMachine"), skillId);
            if (node is BtsmtlSkillStateFlowNode state && state.Body)
                VisitGraph(state.Body, Owner("node", graphId: ownerId, nodeId: node.UID, referenceKey: "body"), skillId);
            if (node is BtsmtlSkillCompositeFlowNode composite)
                foreach (BtsmtlSkillStepPort step in composite.Steps)
                    if (step?.Condition)
                        VisitGraph(step.Condition, Owner("step", graphId: ownerId, nodeId: node.UID, referenceKey: step.Id), skillId);
            if (node is BtsmtlSkillTimelineFlowNode timeline && timeline.TimelineAsset)
            {
                ExportTimeline(timeline, ownerId, node.UID, skillId);
                foreach (Track track in timeline.Timeline?.Tracks ?? new List<Track>())
                    foreach (Clip clip in track.Clips)
                        if (clip is TreeClip tree && tree.AssetTree is BtsmtlSkillFlowGraph child)
                            VisitGraph(child, Owner("timeline-clip", graphId: ownerId, nodeId: node.UID,
                                referenceKey: "timeline", timelineId: timeline.Timeline.AuthoringId,
                                trackId: track.AuthoringId, clipId: clip.AuthoringId), skillId);
            }
        }

        JObject ExportProperties(FlowNode node, AgentPackageSkillFlowGraphFile ownerGraph, string skillId)
        {
            var value = new JObject();
            if (node is BtsmtlSkillCompositeFlowNode composite)
                value["steps"] = new JArray(composite.Steps.Select(step => new JObject
                {
                    ["id"] = step.Id,
                    ["name"] = step.Name,
                    ["conditionGraphId"] = step.Condition ? ((IBtsmtlSkillFlowGraph)step.Condition).AuthoringId : null,
                    ["priority"] = step.Priority,
                    ["abortPolicy"] = step.AbortPolicy.ToString()
                }));
            if (node is BtsmtlSkillLoopFlowNode loop)
                value["stopType"] = loop.StopType.ToString();
            if (node is BtsmtlSkillParallelFlowNode parallel)
                value["mode"] = parallel.Mode.ToString();
            if (node is BtsmtlSkillStateExitCauseFlowNode cause)
                value["cause"] = cause.Cause.ToString();
            if (node is IBtsmtlSkillInputNode input)
                value["inputId"] = input.InputId;
            if (node is BtsmtlSkillActionContextActiveFlowNode contextActive)
                value["actionContext"] = LogicalReference(contextActive.ActionContext);
            if (node is BtsmtlSkillActionWindowActiveFlowNode window)
                value["windowType"] = window.WindowType;
            if (node is BtsmtlSkillCanActivateActionFlowNode admission)
            {
                value["actionProfile"] = LogicalReference(admission.ActionProfile);
                value["targetSnapshot"] = new JObject
                {
                    ["id"] = admission.TargetSnapshotDeclarationId,
                    ["ownerId"] = admission.TargetSnapshotOwnerId
                };
            }
            if (node is BtsmtlSkillSubmitActionLifecycleFlowNode lifecycle)
            {
                value["actionContext"] = LogicalReference(lifecycle.ActionContext);
                value["transitionType"] = lifecycle.TransitionType.ToString();
                value["reason"] = lifecycle.Reason;
            }
            if (node is IBtsmtlSkillBlackboardAccessNode blackboard)
            {
                value["declarationId"] = blackboard.Variable.DeclarationId;
                value["ownerId"] = blackboard.Variable.OwnerId;
                value["valueType"] = AgentSkillFlowAuthoringCapabilities.ValueType(blackboard.ValueType);
                if (blackboard is BtsmtlSkillBlackboardAccessFlowNode access)
                {
                    value["accessMode"] = blackboard.Writes ? "set" : "get";
                    if (access.FactContext)
                        value["factContext"] = AgentAuthoringDocumentCodec.ToToken(ObjectReference(access.FactContext));
                }
            }
            if (node is BtsmtlSkillStateMachineFlowNode stateMachine && stateMachine.StateMachine)
                value["graphId"] = ((IBtsmtlSkillFlowGraph)stateMachine.StateMachine).AuthoringId;
            if (node is BtsmtlSkillStateFlowNode state && state.Body)
                value["bodyGraphId"] = ((IBtsmtlSkillFlowGraph)state.Body).AuthoringId;
            if (node is BtsmtlSkillTimelineFlowNode timeline)
            {
                value["timelineId"] = timeline.Timeline?.AuthoringId ?? string.Empty;
                value["timelineOwnership"] = timeline.Ownership.ToString();
                value["actionContext"] = LogicalReference(timeline.ActionContext);
                value["playbackMode"] = timeline.PlaybackMode.ToString();
            }
            if (node is BtsmtlSkillLocomotionFlowNode locomotion)
            {
                value["moveSpeed"] = locomotion.MoveSpeed;
                value["displacementMode"] = locomotion.DisplacementMode.ToString();
                value["turnSpeedDegrees"] = locomotion.TurnSpeedDegrees;
                value["cameraRelative"] = locomotion.CameraRelative;
                value["executionMode"] = locomotion.ExecutionMode.ToString();
                value["durationSeconds"] = locomotion.DurationSeconds;
                if (locomotion.ActionMotionCurve)
                    value["actionMotionCurve"] = AgentAuthoringDocumentCodec.ToToken(ObjectReference(locomotion.ActionMotionCurve));
            }
            if (node is MacroNodeWrapper macro && macro.macro is BtsmtlSkillMacroGraph macroGraph)
                value["graphId"] = ((IBtsmtlSkillFlowGraph)macroGraph).AuthoringId;
            return value;
        }

        static AgentPackageSkillFlowStep ExportStep(BtsmtlSkillStepPort step)
        {
            return new AgentPackageSkillFlowStep
            {
                id = step.Id,
                name = step.Name,
                conditionGraphId = step.Condition ? ((IBtsmtlSkillFlowGraph)step.Condition).AuthoringId : string.Empty,
                priority = step.Priority,
                abortPolicy = step.AbortPolicy.ToString()
            };
        }

        JObject ExportValues(FlowNode node, string path)
        {
            var result = new JObject();
            foreach (ValueInput input in node.GetInputValuePorts())
            {
                if (input.isConnected || input.isDefaultValue)
                    continue;
                JToken value = DocumentValue(input.serializedValue, input.type, path + ".values." + input.ID);
                if (value != null)
                    result[input.ID] = value;
            }
            return result;
        }

        void ExportBlackboard(FlowGraph graph, AgentPackageSkillFlowGraphFile file, string path)
        {
            IBtsmtlSkillFlowGraph authoring = (IBtsmtlSkillFlowGraph)graph;
            foreach (BtsmtlSkillBlackboardDeclaration declaration in authoring.BlackboardDeclarations)
            {
                Variable variable = graph.GetGraphSource().localBlackboard.variables.Values.SingleOrDefault(value =>
                    value != null && string.Equals(value.ID, declaration.VariableId, StringComparison.Ordinal));
                if (variable is not ISerializedVariableValue serialized)
                {
                    m_Report.Error(path + ".blackboardDeclarations", "skill_blackboard_variable_missing", $"Variable '{declaration.VariableId}'缺少正式序列化值。");
                    continue;
                }
                file.blackboardDeclarations.Add(new AgentPackageSkillBlackboardDeclaration
                {
                    id = declaration.VariableId,
                    key = variable.name,
                    valueType = AgentSkillFlowAuthoringCapabilities.ValueType(variable.varType),
                    scope = declaration.Scope.ToString(),
                    lifetime = declaration.Lifetime.ToString(),
                    category = declaration.Category,
                    defaultValue = DocumentValue(serialized.serializedValue, variable.varType, path + ".blackboardDeclarations[" + declaration.VariableId + "]"),
                    inputBinding = declaration.InputBinding == null ? null : new AgentSnapshotBlackboardInputBinding { inputValueId = declaration.InputBinding.InputValueId },
                    factProjection = declaration.FactProjection == null ? null : new AgentSnapshotBlackboardFactProjection
                    {
                        kind = declaration.FactProjection.Kind.ToString(),
                        windowType = declaration.FactProjection.ActionWindowType,
                        windowId = declaration.FactProjection.ActionWindowId,
                        digest = declaration.FactProjection.ActionWindowDigest
                    }
                });
            }
        }

        void ExportMacro(BtsmtlSkillMacroGraph graph, AgentPackageSkillGraphOwner owner, string path)
        {
            var file = new AgentPackageSkillMacroFile
            {
                id = graph.AuthoringId,
                graphId = graph.AuthoringId,
                ownership = owner.kind == "shared" ? AgentGraphOwnership.SharedAsset.ToString() : AgentGraphOwnership.Inline.ToString(),
                owner = owner,
                asset = ObjectReference(graph)
            };
            foreach (DynamicParameterDefinition parameter in graph.inputDefinitions)
                file.inputs.Add(new AgentPackageSkillMacroParameter
                {
                    id = parameter.ID,
                    name = parameter.name,
                    valueType = AgentSkillFlowAuthoringCapabilities.ValueType(parameter.type)
                });
            foreach (DynamicParameterDefinition parameter in graph.outputDefinitions)
                file.outputs.Add(new AgentPackageSkillMacroParameter
                {
                    id = parameter.ID,
                    name = parameter.name,
                    valueType = AgentSkillFlowAuthoringCapabilities.ValueType(parameter.type)
                });
            if (!m_Macros.TryAdd(file.id, file))
                m_Report.Error(path, "skill_macro_duplicate", $"Skill Macro identity重复：{file.id}");
        }

        void ExportTimeline(BtsmtlSkillTimelineFlowNode node, string graphId, string nodeId, string skillId)
        {
            TimelineData data = node.Timeline;
            if (data == null)
            {
                m_Report.Error("skill:" + skillId + "/node:" + nodeId, "skill_timeline_missing", "Skill Timeline缺少TimelineData。");
                return;
            }
            if (!m_Timelines.TryGetValue(data.AuthoringId, out AgentPackageSkillTimelineFile file))
            {
                file = new AgentPackageSkillTimelineFile
                {
                    id = data.AuthoringId,
                    name = data.Name,
                    ownership = node.Ownership.ToString(),
                    asset = ObjectReference(node.TimelineAsset),
                    ownerGraphId = graphId,
                    ownerNodeId = nodeId
                };
                m_Timelines.Add(file.id, file);
                foreach (TimelineExternalBindingDeclaration binding in data.ExternalBindings ?? new List<TimelineExternalBindingDeclaration>())
                    file.externalBindings.Add(new AgentPackageSkillTimelineExternalBinding
                    {
                        id = binding.AuthoringId,
                        bindingId = binding.BindingId,
                        displayName = binding.DisplayName,
                        domain = binding.Domain,
                        parameterId = binding.ParameterId,
                        valueKind = binding.ValueKind.ToString(),
                        access = binding.Access.ToString(),
                        lifetime = binding.Lifetime.ToString()
                    });
                foreach (TimelineSection section in data.Sections)
                    file.sections.Add(new AgentPackageSkillTimelineSection { id = section.AuthoringId, name = section.Name, frame = section.Frame });
                foreach (Track track in data.Tracks)
                {
                    var trackFile = new AgentPackageSkillTimelineTrack
                    {
                        id = track.AuthoringId,
                        kind = track.ContractKind,
                        name = track.Name,
                        animationChannelId = track is AnimationTrack animation ? animation.AnimationChannelId.Value : string.Empty
                    };
                    foreach (Clip clip in track.Clips)
                        trackFile.clips.Add(ExportClip(clip, skillId, file.id, trackFile.id));
                    file.tracks.Add(trackFile);
                }
            }
            file.callSites.Add(new AgentPackageSkillTimelineCallSite
            {
                graphId = graphId,
                nodeId = nodeId,
                playbackMode = node.PlaybackMode.ToString()
            });
        }

        AgentPackageSkillTimelineClip ExportClip(Clip clip, string skillId, string timelineId, string trackId)
        {
            var result = new AgentPackageSkillTimelineClip
            {
                id = clip.AuthoringId,
                kind = clip.ContractKind,
                startFrame = clip.StartFrame,
                endFrame = clip.EndFrame,
                otherEaseInFrame = clip.OtherEaseInFrame,
                otherEaseOutFrame = clip.OtherEaseOutFrame,
                selfEaseInFrame = clip.SelfEaseInFrame,
                selfEaseOutFrame = clip.SelfEaseOutFrame,
                clipInFrame = clip.ClipInFrame
            };
            if (clip is BTSMTL.Timeline.AnimationClip animation)
            {
                result.animationClip = ObjectReference(animation.Clip);
                result.properties["extraPolationMode"] = animation.ExtraPolationMode.ToString();
            }
            if (clip is MotionCurveClip motion)
            {
                result.properties["curveId"] = motion.CurveId;
                result.properties["curveEndFrame"] = motion.CurveEndFrame;
                result.properties["space"] = motion.Space.ToString();
                result.properties["channel"] = motion.Channel.ToString();
                result.properties["blendMode"] = motion.BlendMode.ToString();
                result.properties["priority"] = motion.Priority;
                result.properties["consumeLowerChannels"] = motion.ConsumeLowerChannels;
            }
            if (clip is MotionWarpClip warp)
            {
                result.properties["sourceMotionClipId"] = warp.SourceMotionClipId;
                result.properties["translationMode"] = warp.TranslationMode.ToString();
                result.properties["targetOffsetSpace"] = warp.TargetOffsetSpace.ToString();
                result.properties["rotationMode"] = warp.RotationMode.ToString();
                result.properties["rotationMethod"] = warp.RotationMethod.ToString();
                result.properties["targetPlanarOffset"] = AgentAuthoringDocumentCodec.ToToken(warp.TargetPlanarOffset);
                result.properties["targetYawOffsetDegrees"] = warp.TargetYawOffsetDegrees;
                result.properties["maxTotalPositionCorrection"] = warp.MaxTotalPositionCorrection;
                result.properties["maxTotalYawCorrectionDegrees"] = warp.MaxTotalYawCorrectionDegrees;
                result.properties["maximumYawRateDegreesPerSecond"] = warp.MaximumYawRateDegreesPerSecond;
                result.properties["limitPolicy"] = warp.LimitPolicy.ToString();
            }
            if (clip is ActionCueClip actionCue)
            {
                result.properties["cueId"] = actionCue.CueId;
                result.properties["cueType"] = actionCue.CueType;
            }
            if (clip is CameraStateClip cameraState)
            {
                result.properties["mode"] = cameraState.Mode.ToString();
                result.properties["priority"] = cameraState.Priority;
                result.properties["blendInSeconds"] = cameraState.BlendInSeconds;
                result.properties["blendOutSeconds"] = cameraState.BlendOutSeconds;
                result.properties["targetKey"] = cameraState.TargetKey;
                result.properties["interruptPolicy"] = cameraState.InterruptPolicy.ToString();
            }
            if (clip is CameraCueClip cameraCue)
            {
                result.properties["cueId"] = cameraCue.CueId;
                result.properties["cueKind"] = cameraCue.CueKind.ToString();
                result.properties["cueType"] = cameraCue.CueType;
                result.properties["intensity"] = cameraCue.Intensity;
                result.properties["durationSeconds"] = cameraCue.DurationSeconds;
                result.properties["priority"] = cameraCue.Priority;
            }
            if (clip is CameraResponseClip cameraResponse)
            {
                result.properties["lookResponse"] = cameraResponse.LookResponse.ToString();
                result.properties["manualOrbitWeight"] = cameraResponse.ManualOrbitWeight;
                result.properties["pitchResponseWeight"] = cameraResponse.PitchResponseWeight;
                result.properties["yawResponseWeight"] = cameraResponse.YawResponseWeight;
                result.properties["priority"] = cameraResponse.Priority;
            }
            if (clip is ScenePresentationParameterCurveClip sceneParameter)
            {
                result.properties["targetBindingId"] = sceneParameter.TargetBindingId;
                result.properties["parameterBindingId"] = sceneParameter.ParameterBindingId;
                result.properties["valueCurve"] = CurveToken(sceneParameter.ValueCurve);
            }
            if (clip is TreeClip tree)
            {
                if (tree.AssetTree is BtsmtlSkillFlowGraph graph)
                {
                    result.treeGraphId = graph.AuthoringId;
                    result.treeOwnership = TimelineTreeOwnership.AssetGraph.ToString();
                }
                else
                {
                    m_Report.Error("skill:" + skillId + "/timeline:" + timelineId + "/track:" + trackId + "/clip:" + clip.AuthoringId,
                        "skill_tree_clip_source_invalid", "Skill TreeClip必须引用BtsmtlSkillFlowGraph AssetTree。");
                }
                result.treePhase = tree.ExecutionPhase.ToString();
            }
            foreach (TimelineCurveChannelDescriptor descriptor in TimelineCurveChannelCatalog.All)
            {
                if (!descriptor.Supports(clip))
                    continue;
                AnimationCurve curve = descriptor.Read(clip);
                result.curves.Add(new AgentPackageCurve
                {
                    clipId = clip.AuthoringId,
                    channelId = descriptor.ChannelId.Value,
                    timeDomain = descriptor.TimeDomain.ToString(),
                    bounded = descriptor.ValueDomain.IsBounded,
                    minimum = descriptor.ValueDomain.Minimum,
                    maximum = descriptor.ValueDomain.Maximum,
                    zero = descriptor.ValueDomain.Zero,
                    unit = descriptor.ValueDomain.Unit,
                    preWrapMode = curve.preWrapMode.ToString(),
                    postWrapMode = curve.postWrapMode.ToString(),
                    keys = curve.keys.Select(value => new AgentAnimationCurveKey
                    {
                        time = value.time,
                        value = value.value,
                        inTangent = value.inTangent,
                        outTangent = value.outTangent,
                        inWeight = value.inWeight,
                        outWeight = value.outWeight,
                        weightedMode = value.weightedMode.ToString()
                    }).ToList()
                });
            }
            return result;
        }

        static JObject CurveToken(AnimationCurve curve)
        {
            return new JObject
            {
                ["preWrapMode"] = curve.preWrapMode.ToString(),
                ["postWrapMode"] = curve.postWrapMode.ToString(),
                ["keys"] = new JArray(curve.keys.Select(value => new JObject
                {
                    ["time"] = value.time,
                    ["value"] = value.value,
                    ["inTangent"] = value.inTangent,
                    ["outTangent"] = value.outTangent,
                    ["inWeight"] = value.inWeight,
                    ["outWeight"] = value.outWeight,
                    ["weightedMode"] = value.weightedMode.ToString()
                }))
            };
        }

        AgentPackageSkillGraphOwner Owner(
            string kind,
            string skillId = null,
            string graphId = null,
            string nodeId = null,
            string referenceKey = null,
            string timelineId = null,
            string trackId = null,
            string clipId = null,
            AgentPackageObjectReference asset = null)
        {
            return new AgentPackageSkillGraphOwner
            {
                kind = kind,
                skillId = skillId,
                graphId = graphId,
                nodeId = nodeId,
                referenceKey = referenceKey,
                timelineId = timelineId,
                trackId = trackId,
                clipId = clipId,
                asset = asset
            };
        }

        AgentPackageSkillFlowEdgeEndpoint Endpoint(FlowNode node, string port) =>
            new AgentPackageSkillFlowEdgeEndpoint
            {
                node = AgentSkillFlowAuthoringCapabilities.TryGetKind(node, out string kind) &&
                       AgentSkillFlowAuthoringCapabilities.IsAnchor(kind)
                    ? kind
                    : node?.UID ?? string.Empty,
                port = port
            };

        JObject LogicalReference(UnityEngine.Object value)
        {
            if (!value)
                return new JObject();
            return new JObject
            {
                ["id"] = value is ActionProfile profile ? profile.ActionId : value.name,
                ["asset"] = AgentAuthoringDocumentCodec.ToToken(ObjectReference(value))
            };
        }

        AgentPackageObjectReference ObjectReference(UnityEngine.Object value)
        {
            if (!value)
                return null;
            string path = AssetDatabase.GetAssetPath(value);
            if (string.IsNullOrEmpty(path) || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long localFileId))
            {
                m_Report.Error("editable/skills", "skill_asset_reference_invalid", $"Skill引用'{value.name}'不是持久化资产。");
                return null;
            }
            return new AgentPackageObjectReference
            {
                assetPath = path,
                assetGuid = guid,
                localFileId = localFileId
            };
        }

        JToken DocumentValue(object value, Type type, string path)
        {
            if (value == null)
                return JValue.CreateNull();
            if (type == typeof(Vector2))
            {
                Vector2 vector = (Vector2)value;
                return new JObject { ["x"] = vector.x, ["y"] = vector.y };
            }
            if (type == typeof(Vector3))
            {
                Vector3 vector = (Vector3)value;
                return new JObject { ["x"] = vector.x, ["y"] = vector.y, ["z"] = vector.z };
            }
            if (type == typeof(ActionTargetSnapshot))
            {
                ActionTargetSnapshot target = (ActionTargetSnapshot)value;
                return new JObject
                {
                    ["targetId"] = target.TargetId,
                    ["x"] = target.Position.x,
                    ["y"] = target.Position.y,
                    ["z"] = target.Position.z,
                    ["rx"] = target.Rotation.x,
                    ["ry"] = target.Rotation.y,
                    ["rz"] = target.Rotation.z,
                    ["rw"] = target.Rotation.w
                };
            }
            if (type == typeof(bool) || type == typeof(int) || type == typeof(float) ||
                type == typeof(string) || type == typeof(uint) || type == typeof(ulong))
                return JToken.FromObject(value);
            if (value is UnityEngine.Object asset)
                return AgentAuthoringDocumentCodec.ToToken(ObjectReference(asset));
            m_Report.Error(path, "skill_value_type_unsupported", $"Skill值类型未登记：{type?.Name}");
            return null;
        }

        static string AssetPath(UnityEngine.Object value) => value ? AssetDatabase.GetAssetPath(value) : string.Empty;
        static string AssetGuid(UnityEngine.Object value)
        {
            string path = AssetPath(value);
            return string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
        }
    }
}
