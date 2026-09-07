using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentSkillDocumentMapper
    {
        public static void Write(
            IDictionary<string, JToken> files,
            IReadOnlyList<AgentSnapshotSkillDefinition> skills)
        {
            foreach (AgentSnapshotSkillDefinition skill in skills ?? Array.Empty<AgentSnapshotSkillDefinition>())
            {
                if (skill == null)
                    continue;
                string directory = $"editable/skills/{AgentAuthoringPackageMapper.Segment(skill.skillId)}";
                files[directory + "/definition.json"] = AgentAuthoringDocumentCodec.ToToken(
                    new AgentPackageSkillDefinitionFile
                    {
                        skillId = skill.skillId,
                        entryGraphAuthoringId = skill.entryGraphAuthoringId,
                        actionProfileId = skill.actionProfileId,
                        actionProfileAssetPath = skill.actionProfileAssetPath,
                        actionProfileAssetGuid = skill.actionProfileAssetGuid,
                        actionContext = skill.actionContext,
                        actionContextAssetPath = skill.actionContextAssetPath,
                        actionContextAssetGuid = skill.actionContextAssetGuid,
                        sourceInputRequestId = skill.sourceInputRequestId,
                        consumeSourceInputRequest = skill.consumeSourceInputRequest,
                        targetInputValueId = skill.targetInputValueId,
                        targetKey = skill.targetKey,
                        subgraphDependencies = (skill.subgraphDependencies ?? new List<AgentSnapshotSkillSubgraphDependency>())
                            .Where(value => value != null)
                            .OrderBy(value => value.subgraphIdentity, StringComparer.Ordinal)
                            .ThenBy(value => value.callSiteIdentity, StringComparer.Ordinal)
                            .Select(value => new AgentSnapshotSkillSubgraphDependency
                            {
                                subgraphIdentity = value.subgraphIdentity,
                                callSiteIdentity = value.callSiteIdentity
                            })
                            .ToList(),
                        allowedFollowUpSkillIds = (skill.allowedFollowUpSkillIds ?? new List<string>())
                            .OrderBy(value => value, StringComparer.Ordinal)
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
            foreach (string path in files.Keys
                         .Where(IsDefinitionPath)
                         .OrderBy(value => value, StringComparer.Ordinal))
            {
                if (!files.TryGetValue(path, out JToken token) ||
                    !AgentAuthoringDocumentCodec.TryConvertToken(
                        token,
                        path,
                        report,
                        out AgentPackageSkillDefinitionFile source))
                {
                    valid = false;
                    continue;
                }
                if (!string.Equals(
                        path,
                        $"editable/skills/{AgentAuthoringPackageMapper.Segment(source.skillId)}/definition.json",
                        StringComparison.Ordinal))
                {
                    report.Error(path, "skill_package_path_mismatch", "Skill目录与skillId必须一致。");
                    valid = false;
                    continue;
                }
                editable.skills.Add(ToSnapshot(source));
            }
            return valid;
        }

        public static bool Validate(
            AgentDocumentEditable editable,
            IReadOnlyCollection<string> graphIds,
            AgentCompileReport report)
        {
            var skillIds = new HashSet<string>(StringComparer.Ordinal);
            var graphById = (editable?.graphs ?? new List<AgentSnapshotGraph>())
                .Where(value => value != null && !string.IsNullOrEmpty(value.graphAuthoringId))
                .GroupBy(value => value.graphAuthoringId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var callSiteTargets = new Dictionary<string, string>(StringComparer.Ordinal);
            bool valid = true;
            foreach (AgentSnapshotGraph graph in graphById.Values)
            {
                foreach (AgentSnapshotNode node in graph.nodes ?? new List<AgentSnapshotNode>())
                {
                    foreach (AgentSnapshotGraphReference reference in node?.graphReferences ?? new List<AgentSnapshotGraphReference>())
                    {
                        if (reference == null ||
                            string.IsNullOrWhiteSpace(reference.key) ||
                            string.IsNullOrWhiteSpace(reference.graphAuthoringId))
                            continue;
                        string callSiteIdentity = CallSiteIdentity(
                            graph.graphAuthoringId,
                            node.elementAuthoringId,
                            reference.key);
                        if (!callSiteTargets.TryAdd(callSiteIdentity, reference.graphAuthoringId))
                        {
                            report.Error(
                                $"editable.graphs[{graph.graphAuthoringId}].nodes[{node.elementAuthoringId}].graphReferences[{reference.key}]",
                                "skill_call_site_duplicate",
                                $"Graph call site identity重复：{callSiteIdentity}");
                            valid = false;
                        }
                    }
                }
            }
            int index = 0;
            foreach (AgentSnapshotSkillDefinition skill in editable?.skills ?? new List<AgentSnapshotSkillDefinition>())
            {
                string path = $"editable/skills[{index}]";
                if (skill == null ||
                    !IsIdentity(skill.skillId) ||
                    !skillIds.Add(skill.skillId))
                {
                    report.Error(path, "skill_identity_invalid", "Skill definition identity缺失或重复。");
                    valid = false;
                    index++;
                    continue;
                }
                if (!IsIdentity(skill.entryGraphAuthoringId) ||
                    graphIds == null ||
                    !graphIds.Contains(skill.entryGraphAuthoringId))
                {
                    report.Error(path + ".entryGraphAuthoringId", "skill_entry_graph_invalid", "Skill必须引用当前Document中的入口Graph。");
                    valid = false;
                }
                if (!IsIdentity(skill.actionProfileId))
                {
                    report.Error(path + ".actionProfileId", "skill_action_profile_invalid", "Skill必须引用稳定ActionProfile identity。");
                    valid = false;
                }
                if (!IsIdentity(skill.actionContext))
                {
                    report.Error(path + ".actionContext", "skill_action_context_invalid", "Skill必须引用稳定ActionContext identity。");
                    valid = false;
                }
                var dependencies = new HashSet<string>(StringComparer.Ordinal);
                var reachableCallSites = new HashSet<string>(StringComparer.Ordinal);
                var graphStates = new Dictionary<string, int>(StringComparer.Ordinal);
                var recursiveGraphs = new HashSet<string>(StringComparer.Ordinal);
                var pendingGraphs = new Stack<(string GraphId, bool Exit)>();
                if (!string.IsNullOrEmpty(skill.entryGraphAuthoringId))
                    pendingGraphs.Push((skill.entryGraphAuthoringId, false));
                while (pendingGraphs.Count > 0)
                {
                    (string graphId, bool exit) = pendingGraphs.Pop();
                    if (exit)
                    {
                        graphStates[graphId] = 2;
                        continue;
                    }
                    if (graphStates.TryGetValue(graphId, out int state))
                    {
                        if (state == 1 && recursiveGraphs.Add(graphId))
                        {
                            report.Error(
                                path + ".entryGraphAuthoringId",
                                "skill_subgraph_recursive",
                                $"Skill入口Graph包含循环子图引用：{graphId}");
                            valid = false;
                        }
                        continue;
                    }
                    graphStates[graphId] = 1;
                    if (!graphById.TryGetValue(graphId, out AgentSnapshotGraph graph))
                    {
                        graphStates[graphId] = 2;
                        continue;
                    }
                    pendingGraphs.Push((graphId, true));
                    foreach (AgentSnapshotNode node in graph.nodes ?? new List<AgentSnapshotNode>())
                    {
                        foreach (AgentSnapshotGraphReference reference in node?.graphReferences ?? new List<AgentSnapshotGraphReference>())
                        {
                            if (reference == null ||
                                string.IsNullOrWhiteSpace(reference.key) ||
                                string.IsNullOrWhiteSpace(reference.graphAuthoringId))
                                continue;
                            reachableCallSites.Add(CallSiteIdentity(
                                graph.graphAuthoringId,
                                node.elementAuthoringId,
                                reference.key));
                            pendingGraphs.Push((reference.graphAuthoringId, false));
                        }
                    }
                }
                foreach (AgentSnapshotSkillSubgraphDependency dependency in skill.subgraphDependencies ?? new List<AgentSnapshotSkillSubgraphDependency>())
                {
                    string dependencyPath = path + ".subgraphDependencies";
                    if (dependency == null ||
                        !IsIdentity(dependency.subgraphIdentity) ||
                        !IsIdentity(dependency.callSiteIdentity) ||
                        graphIds == null ||
                        !graphIds.Contains(dependency.subgraphIdentity) ||
                        !dependencies.Add((dependency.subgraphIdentity ?? string.Empty) + "\0" + (dependency.callSiteIdentity ?? string.Empty)))
                    {
                        report.Error(dependencyPath, "skill_subgraph_dependency_invalid", "Skill subgraph dependency必须具有唯一的Graph与call site identity。");
                        valid = false;
                        continue;
                    }
                    if (!callSiteTargets.TryGetValue(dependency.callSiteIdentity, out string targetGraphId))
                    {
                        report.Error(
                            dependencyPath,
                            "skill_subgraph_call_site_missing",
                            $"Skill引用的Graph call site不存在：{dependency.callSiteIdentity}");
                        valid = false;
                        continue;
                    }
                    if (!reachableCallSites.Contains(dependency.callSiteIdentity))
                    {
                        report.Error(
                            dependencyPath,
                            "skill_subgraph_call_site_unreachable",
                            $"Skill引用的Graph call site不在入口Graph闭包：{dependency.callSiteIdentity}");
                        valid = false;
                    }
                    if (!string.Equals(targetGraphId, dependency.subgraphIdentity, StringComparison.Ordinal))
                    {
                        report.Error(
                            dependencyPath,
                            "skill_subgraph_mismatch",
                            $"Skill call site '{dependency.callSiteIdentity}'实际指向Graph '{targetGraphId}'，不是'{dependency.subgraphIdentity}'。");
                        valid = false;
                    }
                }
                if (!string.IsNullOrEmpty(skill.sourceInputRequestId) &&
                    !IsIdentity(skill.sourceInputRequestId))
                {
                    report.Error(path + ".sourceInputRequestId", "skill_input_request_invalid", "Skill的sourceInputRequestId必须使用稳定identity。");
                    valid = false;
                }
                if (!string.IsNullOrEmpty(skill.targetInputValueId) &&
                    !IsIdentity(skill.targetInputValueId))
                {
                    report.Error(path + ".targetInputValueId", "skill_target_input_invalid", "Skill的targetInputValueId必须使用稳定identity。");
                    valid = false;
                }
                index++;
            }
            index = 0;
            foreach (AgentSnapshotSkillDefinition skill in editable?.skills ?? new List<AgentSnapshotSkillDefinition>())
            {
                if (skill == null)
                {
                    index++;
                    continue;
                }
                var followUps = new HashSet<string>(StringComparer.Ordinal);
                foreach (string followUp in skill.allowedFollowUpSkillIds ?? new List<string>())
                {
                    if (!IsIdentity(followUp) ||
                        string.Equals(followUp, skill.skillId, StringComparison.Ordinal) ||
                        !followUps.Add(followUp) ||
                        !skillIds.Contains(followUp))
                    {
                        report.Error($"editable.skills[{index}].allowedFollowUpSkillIds", "skill_follow_up_invalid", "Skill follow-up必须引用其它已登记Skill且不能重复或递归。");
                        valid = false;
                    }
                }
                index++;
            }
            return valid;
        }

        static string CallSiteIdentity(
            string graphAuthoringId,
            string nodeAuthoringId,
            string referenceKey)
        {
            return graphAuthoringId + "/node:" + nodeAuthoringId + "/" + referenceKey + "/call";
        }

        public static HashSet<string> SelectGraphIds(
            AgentGraphSnapshot snapshot,
            IReadOnlyList<AgentSnapshotSkillDefinition> skills)
        {
            var graphById = (snapshot?.graphs ?? new List<AgentSnapshotGraph>())
                .Where(value => value != null && !string.IsNullOrEmpty(value.graphAuthoringId))
                .GroupBy(value => value.graphAuthoringId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var selected = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentSnapshotSkillDefinition skill in skills ?? Array.Empty<AgentSnapshotSkillDefinition>())
            {
                if (skill == null)
                    continue;
                if (graphById.ContainsKey(skill.entryGraphAuthoringId) &&
                    !string.Equals(
                        skill.entryGraphAuthoringId,
                        snapshot?.rootGraphAuthoringId,
                        StringComparison.Ordinal))
                    selected.Add(skill.entryGraphAuthoringId);
                foreach (AgentSnapshotSkillSubgraphDependency dependency in
                         skill.subgraphDependencies ?? new List<AgentSnapshotSkillSubgraphDependency>())
                {
                    if (dependency != null &&
                        graphById.ContainsKey(dependency.subgraphIdentity) &&
                        !string.Equals(
                            dependency.subgraphIdentity,
                            snapshot?.rootGraphAuthoringId,
                            StringComparison.Ordinal))
                        selected.Add(dependency.subgraphIdentity);
                }
            }

            bool changed;
            do
            {
                changed = false;
                var entities = new HashSet<string>(StringComparer.Ordinal);
                foreach (string graphId in selected)
                {
                    if (!graphById.TryGetValue(graphId, out AgentSnapshotGraph graph))
                        continue;
                    foreach (AgentSnapshotNode node in graph.nodes ?? new List<AgentSnapshotNode>())
                    {
                        if (!string.IsNullOrEmpty(node?.elementAuthoringId))
                            entities.Add(node.elementAuthoringId);
                    }
                    foreach (AgentSnapshotFlowEdge edge in graph.flowEdges ?? new List<AgentSnapshotFlowEdge>())
                    {
                        if (!string.IsNullOrEmpty(edge?.elementAuthoringId))
                            entities.Add(edge.elementAuthoringId);
                    }
                    foreach (AgentSnapshotPropertyEdge edge in graph.propertyEdges ?? new List<AgentSnapshotPropertyEdge>())
                    {
                        if (!string.IsNullOrEmpty(edge?.elementAuthoringId))
                            entities.Add(edge.elementAuthoringId);
                    }
                }

                foreach (AgentSnapshotGraph candidate in graphById.Values)
                {
                    if (selected.Contains(candidate.graphAuthoringId) ||
                        string.Equals(
                            candidate.graphAuthoringId,
                            snapshot?.rootGraphAuthoringId,
                            StringComparison.Ordinal))
                        continue;
                    bool ownedBySelectedEntity = !string.IsNullOrEmpty(candidate.ownerElementAuthoringId) &&
                        entities.Contains(candidate.ownerElementAuthoringId);
                    bool routedFromSelectedGraph = (candidate.routes ?? new List<AgentSnapshotAuthoringRoute>())
                        .Any(route => (route?.segments ?? new List<AgentSnapshotAuthoringRouteSegment>())
                            .Any(segment => segment != null &&
                                selected.Contains(segment.ownerGraphAuthoringId) &&
                                string.Equals(
                                    segment.childGraphAuthoringId,
                                    candidate.graphAuthoringId,
                                    StringComparison.Ordinal)));
                    if (ownedBySelectedEntity || routedFromSelectedGraph)
                    {
                        selected.Add(candidate.graphAuthoringId);
                        changed = true;
                    }
                }
            } while (changed);

            return selected;
        }

        public static List<AgentSnapshotStateMachineSummary> SelectStateMachines(
            AgentGraphSnapshot snapshot,
            ISet<string> graphIds,
            IReadOnlyList<AgentSnapshotSkillDefinition> skills)
        {
            var entryGraphIds = (skills ?? Array.Empty<AgentSnapshotSkillDefinition>())
                .Where(value => value != null && !string.IsNullOrEmpty(value.entryGraphAuthoringId))
                .Select(value => value.entryGraphAuthoringId)
                .ToHashSet(StringComparer.Ordinal);
            var selected = new HashSet<string>(StringComparer.Ordinal);
            var byId = (snapshot?.stateMachines ?? new List<AgentSnapshotStateMachineSummary>())
                .Where(value => value != null && !string.IsNullOrEmpty(value.graphAuthoringId))
                .GroupBy(value => value.graphAuthoringId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            foreach (AgentSnapshotStateMachineSummary stateMachine in byId.Values)
            {
                bool ownsSkillEntry = (stateMachine.states ?? new List<AgentSnapshotStateSummary>())
                    .Any(state => state != null && entryGraphIds.Contains(state.behaviorGraphAuthoringId));
                bool explicitlySelected = graphIds?.Contains(stateMachine.graphAuthoringId) == true;
                if (explicitlySelected || ownsSkillEntry)
                    selected.Add(stateMachine.graphAuthoringId);
            }

            bool changed;
            do
            {
                changed = false;
                foreach (AgentSnapshotStateMachineSummary stateMachine in byId.Values)
                {
                    if (selected.Contains(stateMachine.graphAuthoringId))
                        continue;
                    bool nestedFromSelected = (stateMachine.routes ?? new List<AgentSnapshotAuthoringRoute>())
                        .Any(route => (route?.segments ?? new List<AgentSnapshotAuthoringRouteSegment>())
                            .Any(segment => segment != null &&
                                selected.Contains(segment.ownerGraphAuthoringId) &&
                                string.Equals(
                                    segment.childGraphAuthoringId,
                                    stateMachine.graphAuthoringId,
                                    StringComparison.Ordinal)));
                    if (nestedFromSelected)
                    {
                        selected.Add(stateMachine.graphAuthoringId);
                        changed = true;
                    }
                }
            } while (changed);

            return byId.Values
                .Where(value => selected.Contains(value.graphAuthoringId))
                .ToList();
        }

        public static bool IsRootCompositionStateMachine(
            AgentSnapshotStateMachineSummary stateMachine,
            string rootGraphAuthoringId,
            IReadOnlyList<AgentSnapshotGraph> graphs)
        {
            if ((stateMachine?.routes ?? new List<AgentSnapshotAuthoringRoute>())
                .Any(route => route?.segments?.Count == 1 &&
                    string.Equals(
                        route.segments[0].ownerGraphAuthoringId,
                        rootGraphAuthoringId,
                        StringComparison.Ordinal)))
                return true;

            AgentSnapshotGraph graph = (graphs ?? Array.Empty<AgentSnapshotGraph>())
                .FirstOrDefault(value => value != null && string.Equals(
                    value.graphAuthoringId,
                    stateMachine?.graphAuthoringId,
                    StringComparison.Ordinal));
            return (graph?.routes ?? new List<AgentSnapshotAuthoringRoute>())
                .Any(route => route?.segments?.Count == 1 &&
                    string.Equals(
                        route.segments[0].ownerGraphAuthoringId,
                        rootGraphAuthoringId,
                        StringComparison.Ordinal));
        }

        public static bool IsDefinitionPath(string path)
        {
            return path.StartsWith("editable/skills/", StringComparison.Ordinal) &&
                   path.EndsWith("/definition.json", StringComparison.Ordinal);
        }

        public static bool TryDiscoverNewFragments(
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
                string definitionPath = directory + "/definition.json";
                if (!candidates.TryGetValue(definitionPath, out JToken definitionToken) ||
                    !AgentAuthoringDocumentCodec.TryConvertToken(
                        definitionToken,
                        definitionPath,
                        report,
                        out AgentPackageSkillDefinitionFile definition))
                {
                    report.Error(directory, "skill_new_definition_missing", "新增Skill必须提供同目录definition.json。");
                    valid = false;
                    continue;
                }
                string expectedDirectory = $"editable/skills/{AgentAuthoringPackageMapper.Segment(definition.skillId)}";
                if (!IsLocal(definition.skillId) ||
                    !string.Equals(directory, expectedDirectory, StringComparison.Ordinal))
                {
                    report.Error(
                        definitionPath,
                        "skill_new_definition_invalid",
                        "新增Skill必须使用local:* identity并放在由skillId确定的canonical目录。");
                    valid = false;
                    continue;
                }
                result.Add(definitionPath);
            }
            discovered = result;
            return valid;
        }

        public static bool IsLocal(string identity)
        {
            return !string.IsNullOrWhiteSpace(identity) &&
                   identity.StartsWith("local:", StringComparison.Ordinal) &&
                   identity.Length > "local:".Length &&
                   identity.Substring("local:".Length).All(character =>
                       char.IsLetterOrDigit(character) || character == '-' || character == '_' || character == '.');
        }

        public static bool IsIdentity(string identity)
        {
            if (string.IsNullOrWhiteSpace(identity) || identity.Any(char.IsWhiteSpace))
                return false;
            if (identity.StartsWith("@", StringComparison.Ordinal))
                return false;
            return !identity.StartsWith("local:", StringComparison.Ordinal) || IsLocal(identity);
        }

        public static AgentSnapshotSkillDefinition ToSnapshot(
            AgentPackageSkillDefinitionFile source)
        {
            return new AgentSnapshotSkillDefinition
            {
                skillId = source.skillId,
                entryGraphAuthoringId = source.entryGraphAuthoringId,
                actionProfileId = source.actionProfileId,
                actionProfileAssetPath = source.actionProfileAssetPath ?? string.Empty,
                actionProfileAssetGuid = source.actionProfileAssetGuid ?? string.Empty,
                actionContext = source.actionContext ?? string.Empty,
                actionContextAssetPath = source.actionContextAssetPath ?? string.Empty,
                actionContextAssetGuid = source.actionContextAssetGuid ?? string.Empty,
                sourceInputRequestId = source.sourceInputRequestId ?? string.Empty,
                consumeSourceInputRequest = source.consumeSourceInputRequest,
                targetInputValueId = source.targetInputValueId ?? string.Empty,
                targetKey = source.targetKey ?? string.Empty,
                subgraphDependencies = (source.subgraphDependencies ?? new List<AgentSnapshotSkillSubgraphDependency>())
                    .Select(value => value == null
                        ? null
                        : new AgentSnapshotSkillSubgraphDependency
                        {
                            subgraphIdentity = value.subgraphIdentity,
                            callSiteIdentity = value.callSiteIdentity
                        })
                    .ToList(),
                allowedFollowUpSkillIds = (source.allowedFollowUpSkillIds ?? new List<string>()).ToList()
            };
        }

        public static bool SemanticEquals(
            AgentSnapshotSkillDefinition left,
            AgentSnapshotSkillDefinition right)
        {
            return string.Equals(left?.skillId, right?.skillId, StringComparison.Ordinal) &&
                   string.Equals(left?.entryGraphAuthoringId, right?.entryGraphAuthoringId, StringComparison.Ordinal) &&
                   string.Equals(left?.actionProfileId, right?.actionProfileId, StringComparison.Ordinal) &&
                   string.Equals(left?.actionProfileAssetPath, right?.actionProfileAssetPath, StringComparison.Ordinal) &&
                   string.Equals(left?.actionProfileAssetGuid, right?.actionProfileAssetGuid, StringComparison.Ordinal) &&
                   string.Equals(left?.actionContext, right?.actionContext, StringComparison.Ordinal) &&
                   string.Equals(left?.actionContextAssetPath, right?.actionContextAssetPath, StringComparison.Ordinal) &&
                   string.Equals(left?.actionContextAssetGuid, right?.actionContextAssetGuid, StringComparison.Ordinal) &&
                   string.Equals(left?.sourceInputRequestId, right?.sourceInputRequestId, StringComparison.Ordinal) &&
                   left?.consumeSourceInputRequest == right?.consumeSourceInputRequest &&
                   string.Equals(left?.targetInputValueId, right?.targetInputValueId, StringComparison.Ordinal) &&
                   string.Equals(left?.targetKey, right?.targetKey, StringComparison.Ordinal) &&
                   (left?.subgraphDependencies ?? new List<AgentSnapshotSkillSubgraphDependency>())
                       .OrderBy(value => value?.subgraphIdentity, StringComparer.Ordinal)
                       .ThenBy(value => value?.callSiteIdentity, StringComparer.Ordinal)
                       .Select(value => (value?.subgraphIdentity ?? string.Empty) + "\0" + (value?.callSiteIdentity ?? string.Empty))
                       .SequenceEqual((right?.subgraphDependencies ?? new List<AgentSnapshotSkillSubgraphDependency>())
                           .OrderBy(value => value?.subgraphIdentity, StringComparer.Ordinal)
                           .ThenBy(value => value?.callSiteIdentity, StringComparer.Ordinal)
                           .Select(value => (value?.subgraphIdentity ?? string.Empty) + "\0" + (value?.callSiteIdentity ?? string.Empty)), StringComparer.Ordinal) &&
                   (left?.allowedFollowUpSkillIds ?? new List<string>())
                       .OrderBy(value => value, StringComparer.Ordinal)
                       .SequenceEqual((right?.allowedFollowUpSkillIds ?? new List<string>())
                           .OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal);
        }
    }
}
