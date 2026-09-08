using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentPackageMappingSupport
    {
        internal static bool ValidatePrimaryIdentities(AgentDocumentEditable editable, AgentCompileReport report)
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
            foreach (AgentSnapshotSkillDefinition skill in editable.skills ?? new List<AgentSnapshotSkillDefinition>())
                Add(skill?.skillId, "editable.skills");
            foreach (AgentPackageSkillFlowGraphFile graph in editable.skillGraphs ?? new List<AgentPackageSkillFlowGraphFile>())
            {
                string graphPath = "editable.skills.graphs[" + graph?.id + "]";
                Add(graph?.id, graphPath);
                foreach (AgentPackageSkillFlowNode node in graph?.nodes ?? new List<AgentPackageSkillFlowNode>())
                    Add(node?.id, graphPath + ".nodes");
                foreach (AgentPackageSkillFlowEdge edge in graph?.edges ?? new List<AgentPackageSkillFlowEdge>())
                    Add(edge?.id, graphPath + ".edges");
                foreach (AgentPackageSkillBlackboardDeclaration declaration in graph?.blackboardDeclarations ?? new List<AgentPackageSkillBlackboardDeclaration>())
                    Add(declaration?.id, graphPath + ".blackboardDeclarations");
            }
            foreach (AgentPackageSkillMacroFile macro in editable.skillMacros ?? new List<AgentPackageSkillMacroFile>())
            {
                string macroPath = "editable.skills.macros[" + macro?.id + "]";
                foreach (AgentPackageSkillMacroParameter parameter in (macro?.inputs ?? new List<AgentPackageSkillMacroParameter>()).Concat(macro?.outputs ?? new List<AgentPackageSkillMacroParameter>()))
                    Add(parameter?.id, macroPath + ".parameters");
            }
            foreach (AgentPackageSkillTimelineFile timeline in editable.skillTimelines ?? new List<AgentPackageSkillTimelineFile>())
            {
                string timelinePath = "editable.skills.timelines[" + timeline?.id + "]";
                Add(timeline?.id, timelinePath);
                foreach (AgentPackageSkillTimelineSection section in timeline?.sections ?? new List<AgentPackageSkillTimelineSection>())
                    Add(section?.id, timelinePath + ".sections");
                foreach (AgentPackageSkillTimelineExternalBinding binding in timeline?.externalBindings ?? new List<AgentPackageSkillTimelineExternalBinding>())
                    Add(binding?.id, timelinePath + ".externalBindings");
                foreach (AgentPackageSkillTimelineTrack track in timeline?.tracks ?? new List<AgentPackageSkillTimelineTrack>())
                {
                    Add(track?.id, timelinePath + ".tracks");
                    foreach (AgentPackageSkillTimelineClip clip in track?.clips ?? new List<AgentPackageSkillTimelineClip>())
                        Add(clip?.id, timelinePath + ".clips");
                }
            }
            var summaryGraphIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentSnapshotStateMachineSummary stateMachine in editable.stateMachines ?? new List<AgentSnapshotStateMachineSummary>())
            {
                if (stateMachine == null)
                    continue;
                if (!summaryGraphIds.Add(stateMachine.graphAuthoringId))
                {
                    report.Error("editable.controller.stateMachines", "entity_identity_duplicate", $"StateMachine摘要重复：{stateMachine.graphAuthoringId}");
                    valid = false;
                }
                AgentSnapshotGraph ownerGraph = (editable.graphs ?? new List<AgentSnapshotGraph>())
                    .FirstOrDefault(graph => graph != null && string.Equals(
                        graph.graphAuthoringId,
                        stateMachine.graphAuthoringId,
                        StringComparison.Ordinal));
                if (ownerGraph == null)
                {
                    Add(stateMachine.graphAuthoringId, "editable.controller.stateMachines");
                }
                var summaryStateIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (AgentSnapshotStateSummary state in stateMachine.states ?? new List<AgentSnapshotStateSummary>())
                {
                    string stateId = state?.stateAuthoringId;
                    if (!summaryStateIds.Add(stateId))
                    {
                        report.Error("editable.controller.stateMachines.states", "entity_identity_duplicate", $"State摘要重复：{stateId}");
                        valid = false;
                        continue;
                    }
                    if (ownerGraph == null)
                    {
                        Add(stateId, "editable.controller.stateMachines.states");
                        continue;
                    }
                    if (!IsIdentity(stateId) ||
                        !string.Equals(ownerGraph.kind, AgentGraphKind.StateMachineGraph.ToString(), StringComparison.Ordinal) ||
                        !(ownerGraph.nodes ?? new List<AgentSnapshotNode>()).Any(node =>
                            node != null &&
                            string.Equals(node.elementAuthoringId, stateId, StringComparison.Ordinal) &&
                            string.Equals(node.typeName, typeof(TreeDesigner.StateNode).FullName, StringComparison.Ordinal)))
                    {
                        report.Error("editable.controller.stateMachines.states", "state_summary_reference_invalid", $"State摘要必须引用所属StateMachine Graph中的State节点：{stateId}");
                        valid = false;
                    }
                }
            }
            foreach (AgentSnapshotAIBlackboardDeclaration declaration in editable.aiController?.blackboardDeclarations ?? new List<AgentSnapshotAIBlackboardDeclaration>())
                Add(declaration?.declarationAuthoringId, "editable.ai.blackboard");
            foreach (AgentSnapshotTimeline timeline in editable.timelines ?? new List<AgentSnapshotTimeline>())
            {
                string timelinePath = $"editable.timelines[{timeline.timelineAuthoringId}]";
                Add(timeline.timelineAuthoringId, timelinePath);
                foreach (AgentSnapshotTimelineSection section in timeline.sections ?? new List<AgentSnapshotTimelineSection>())
                    Add(section?.sectionAuthoringId, timelinePath + ".sections");
                foreach (AgentSnapshotTimelineTrack track in timeline.tracks ?? new List<AgentSnapshotTimelineTrack>())
                {
                    Add(track?.trackAuthoringId, timelinePath + ".tracks");
                    foreach (AgentSnapshotTimelineClip clip in track?.clips ?? new List<AgentSnapshotTimelineClip>())
                        Add(clip?.clipAuthoringId, timelinePath + ".clips");
                }
            }
            return valid;
        }

        internal static bool IsIdentity(string identity)
        {
            if (string.IsNullOrWhiteSpace(identity) || identity.Any(char.IsWhiteSpace))
                return false;
            if (!identity.StartsWith("local:", StringComparison.Ordinal))
                return !identity.StartsWith("@", StringComparison.Ordinal);
            string local = identity.Substring("local:".Length);
            return local.Length > 0 && local.All(character =>
                char.IsLetterOrDigit(character) || character == '-' || character == '_' || character == '.');
        }

        internal static bool IsAssetReference(AgentPackageObjectReference reference)
        {
            if (reference == null)
                return false;
            if (!string.IsNullOrWhiteSpace(reference.localId))
                return IsIdentity(reference.localId) &&
                       reference.localId.StartsWith("local:", StringComparison.Ordinal) &&
                       string.IsNullOrWhiteSpace(reference.assetPath) &&
                       string.IsNullOrWhiteSpace(reference.assetGuid) &&
                       reference.localFileId == 0;
            return !string.IsNullOrWhiteSpace(reference.assetPath) &&
                   reference.assetPath.StartsWith("Assets/", StringComparison.Ordinal) &&
                   !reference.assetPath.Contains("\\") &&
                   IsIdentity(reference.assetGuid) &&
                   reference.localFileId != 0;
        }

        internal static void Add(JObject properties, string name, object value)
        {
            if (value == null)
                return;
            JToken token = AgentAuthoringDocumentCodec.ToToken(value);
            if (token.Type == JTokenType.Null || token is JArray array && array.Count == 0 || token.Type == JTokenType.String && string.IsNullOrEmpty(token.Value<string>()))
                return;
            properties[name] = token;
        }

        internal static List<T> ReadList<T>(JObject properties, string name)
        {
            return properties[name]?.ToObject<List<T>>() ?? new List<T>();
        }

    }
}
