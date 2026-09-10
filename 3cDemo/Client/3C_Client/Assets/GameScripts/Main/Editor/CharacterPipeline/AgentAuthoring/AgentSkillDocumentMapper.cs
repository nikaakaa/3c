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
