using System;
using System.Collections.Generic;
using System.Linq;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentSkillDocumentMutationPlanner
    {
        public static void Build(
            IReadOnlyList<AgentSnapshotSkillDefinition> current,
            IReadOnlyList<AgentSnapshotSkillDefinition> target,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            var oldSkills = Index(current, report, "document.editable.skills");
            var newSkills = Index(target, report, "document.editable.skills");
            foreach (AgentSnapshotSkillDefinition skill in target ?? Array.Empty<AgentSnapshotSkillDefinition>())
            {
                if (skill == null)
                    continue;
                if (oldSkills.TryGetValue(skill.skillId, out AgentSnapshotSkillDefinition oldSkill) &&
                    AgentSkillDocumentMapper.SemanticEquals(oldSkill, skill))
                    continue;
                Add(mutations, $"document.editable.skills[{Escape(skill.skillId)}]", AgentMutationKind.SetSkillDefinition, operation =>
                {
                    operation.skillId = skill.skillId;
                    if (IsLocal(skill.entryGraphAuthoringId))
                        operation.entryGraphPlannedIdentity = skill.entryGraphAuthoringId;
                    else
                        operation.entryGraphAuthoringId = skill.entryGraphAuthoringId;
                    operation.actionProfile = skill.actionProfileId;
                    operation.actionProfileAssetPath = skill.actionProfileAssetPath;
                    operation.actionProfileAssetGuid = skill.actionProfileAssetGuid;
                    operation.actionContext = skill.actionContext;
                    operation.actionContextAssetPath = skill.actionContextAssetPath;
                    operation.actionContextAssetGuid = skill.actionContextAssetGuid;
                    operation.sourceInputRequestId = skill.sourceInputRequestId;
                    operation.consumeSourceInputRequest = skill.consumeSourceInputRequest;
                    operation.targetInputValueId = skill.targetInputValueId;
                    operation.targetKey = skill.targetKey;
                    operation.subgraphDependencies = skill.subgraphDependencies
                        ?.Select(value => AgentAuthoringDocumentCodec.Clone(value))
                        .ToList() ?? new List<AgentSnapshotSkillSubgraphDependency>();
                    operation.allowedFollowUpSkillIds = skill.allowedFollowUpSkillIds?.ToList() ?? new List<string>();
                });
            }
            foreach (string removed in oldSkills.Keys.Except(newSkills.Keys, StringComparer.Ordinal))
                Add(mutations, $"document.editable.skills[{Escape(removed)}]", AgentMutationKind.DeleteSkillDefinition, operation => operation.skillId = removed);
        }

        static Dictionary<string, AgentSnapshotSkillDefinition> Index(
            IEnumerable<AgentSnapshotSkillDefinition> values,
            AgentCompileReport report,
            string path)
        {
            var result = new Dictionary<string, AgentSnapshotSkillDefinition>(StringComparer.Ordinal);
            int index = 0;
            foreach (AgentSnapshotSkillDefinition value in values ?? Array.Empty<AgentSnapshotSkillDefinition>())
            {
                if (value == null || string.IsNullOrWhiteSpace(value.skillId))
                {
                    report.Error($"{path}[{index}]", "skill_identity_missing", "SkillDefinition缺少identity。");
                    index++;
                    continue;
                }
                if (!result.TryAdd(value.skillId, value))
                    report.Error($"{path}[{index}]", "skill_identity_duplicate", $"SkillDefinition identity重复：{value.skillId}");
                index++;
            }
            return result;
        }

        static void Add(
            AgentMutationDraftSet mutations,
            string path,
            AgentMutationKind kind,
            Action<AgentMutationDraft> configure)
        {
            var operation = new AgentMutationDraft
            {
                id = "mutation-" + mutations.mutations.Count.ToString("D4"),
                sourcePath = path,
                kind = kind
            };
            configure(operation);
            mutations.mutations.Add(operation);
        }

        static string Escape(string value)
        {
            return value?.Replace("\\", "\\\\").Replace("]", "\\]") ?? string.Empty;
        }

        static bool IsLocal(string value) => value != null && value.StartsWith("local:", StringComparison.Ordinal);
    }
}
