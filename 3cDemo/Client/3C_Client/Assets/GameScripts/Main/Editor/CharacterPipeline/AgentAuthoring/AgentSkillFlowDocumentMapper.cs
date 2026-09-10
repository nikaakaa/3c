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
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
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
            AgentCompileReport report,
            string inputProviderOwnerId,
            string gameplayProviderOwnerId)
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
                report,
                editable.control?.moduleId,
                inputProviderOwnerId,
                gameplayProviderOwnerId);
            return valid;
        }


        public static bool Validate(
            AgentPackageSkillFlowDocument document,
            AgentCompileReport report,
            string controlModuleId,
            string inputProviderOwnerId = null,
            string gameplayProviderOwnerId = null) =>
            AgentSkillFlowDocumentValidator.Validate(
                document, report, controlModuleId, inputProviderOwnerId, gameplayProviderOwnerId);

        public static bool ValidateDefinition(
            CharacterPipelineDefinition definition,
            AgentCompileReport report) =>
            AgentSkillFlowDocumentValidator.ValidateDefinition(definition, report);
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
                    !AgentSkillFlowDocumentValidator.LocalIdentity(graph.id) ||
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
                    !AgentSkillFlowDocumentValidator.LocalIdentity(macro.id) ||
                    !string.Equals(path, MacroPath(macro.id), StringComparison.Ordinal) ||
                    !AgentSkillFlowDocumentValidator.LocalIdentity(macro.graphId) ||
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
                    !AgentSkillFlowDocumentValidator.LocalIdentity(timeline.id) ||
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

        static void ApplyCurves(AgentPackageSkillTimelineFile timeline, AgentPackageCurvesFile curves)
        {
            var byClip = (timeline.tracks ?? new List<AgentPackageSkillTimelineTrack>())
                .SelectMany(track => track?.clips ?? new List<AgentPackageSkillTimelineClip>())
                .ToDictionary(clip => clip.id, clip => clip, StringComparer.Ordinal);
            foreach (AgentPackageCurve curve in curves.curves ?? new List<AgentPackageCurve>())
                if (byClip.TryGetValue(curve?.clipId ?? string.Empty, out AgentPackageSkillTimelineClip clip))
                    clip.curves.Add(curve);
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


    }
}
