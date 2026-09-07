using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentSkillDocumentExporter
    {
        public static List<AgentSnapshotSkillDefinition> Export(
            IReadOnlyList<CharacterSkillAuthoringDefinition> definitions)
        {
            return (definitions ?? Array.Empty<CharacterSkillAuthoringDefinition>())
                .Where(value => value != null)
                .OrderBy(value => value.SkillId, StringComparer.Ordinal)
                .Select(value =>
                {
                    ActionProfile profile = value.ActionProfile;
                    string profilePath = profile ? AssetDatabase.GetAssetPath(profile) : string.Empty;
                    string actionContextPath = value.ActionContext
                        ? AssetDatabase.GetAssetPath(value.ActionContext)
                        : string.Empty;
                    return new AgentSnapshotSkillDefinition
                    {
                        skillId = value.SkillId,
                        entryGraphAuthoringId = value.EntryGraphAuthoringId,
                        actionProfileId = profile ? profile.ActionId : string.Empty,
                        actionProfileAssetPath = profilePath,
                        actionProfileAssetGuid = string.IsNullOrEmpty(profilePath)
                            ? string.Empty
                            : AssetDatabase.AssetPathToGUID(profilePath),
                        actionContext = value.ActionContext ? value.ActionContext.name : string.Empty,
                        actionContextAssetPath = actionContextPath,
                        actionContextAssetGuid = string.IsNullOrEmpty(actionContextPath)
                            ? string.Empty
                            : AssetDatabase.AssetPathToGUID(actionContextPath),
                        sourceInputRequestId = value.SourceInputRequestId,
                        consumeSourceInputRequest = value.ConsumeSourceInputRequest,
                        targetInputValueId = value.TargetInputValueId,
                        targetKey = value.TargetKey,
                        subgraphDependencies = (value.SubgraphDependencies ?? Array.Empty<CharacterSkillSubgraphDependencyConfiguration>())
                            .Where(dependency => dependency != null)
                            .Select(dependency => new AgentSnapshotSkillSubgraphDependency
                            {
                                subgraphIdentity = dependency.SubgraphIdentity,
                                callSiteIdentity = dependency.CallSiteIdentity
                            })
                            .ToList(),
                        allowedFollowUpSkillIds = (value.AllowedFollowUpSkillIds ?? Array.Empty<string>()).ToList()
                    };
                })
                .ToList();
        }
    }
}
