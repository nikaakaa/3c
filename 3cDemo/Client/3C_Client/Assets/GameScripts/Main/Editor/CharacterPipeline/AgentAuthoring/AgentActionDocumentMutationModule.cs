using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonSimulation;

using ThirdPersonCharacter.Pipeline.Editor.Authoring.Presentation;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.Skill;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentActionDocumentMutationModule
    {
        internal static void BuildActionMutations(
            AgentAuthoringTarget current,
            AgentDocumentEditable target,
            AgentMutationPlanBuilder mutations,
            AgentCompileReport report)
        {
            var oldRequests = AgentDocumentMutationSupport.Index(
                current?.editable?.actionRequests,
                value => value.requestId,
                "document.editable.actionRequests",
                report);
            var newRequests = AgentDocumentMutationSupport.Index(
                target.actionRequests,
                value => value.requestId,
                "document.editable.actionRequests",
                report);
            foreach (AgentActionRequest request in target.actionRequests ?? new List<AgentActionRequest>())
            {
                string path = $"document.editable.actionRequests[{AgentDocumentMutationSupport.Escape(request.requestId)}]";
                if (!oldRequests.TryGetValue(request.requestId, out AgentActionRequest oldRequest))
                {
                    report.Error(path, "action_request_create_unsupported", "Action Request catalog不能由Agent Document创建。");
                    continue;
                }
                if (oldRequest.bufferSeconds != request.bufferSeconds || oldRequest.priority != request.priority)
                    report.Error(path, "action_request_readonly_field_modified", "bufferSeconds与priority来自正式Action catalog，不可由Document修改。");
                if (!string.Equals(oldRequest.timingClass, request.timingClass, StringComparison.Ordinal))
                {
                    var reader = new AgentMutationValueReader(report, path);
                    string requestId = reader.RequiredText(request.requestId, "request", "Action Request identity缺失。");
                    if (!reader.TryParseEnum(
                            request.timingClass,
                            "requestTimingClass",
                            out CharacterActionRequestTimingClass timingClass))
                        continue;
                    mutations.Add(path, id => new AgentSetActionRequestTimingClassMutation(
                        id,
                        path,
                        requestId,
                        timingClass));
                }
            }
            foreach (string removed in oldRequests.Keys.Except(newRequests.Keys, StringComparer.Ordinal))
                report.Error(
                    $"document.editable.actionRequests[{AgentDocumentMutationSupport.Escape(removed)}]",
                    "action_request_delete_unsupported",
                    "Action Request catalog不可由Document删除。");

            var oldProfiles = AgentDocumentMutationSupport.Index(
                current?.editable?.actionProfiles,
                value => value.actionId,
                "document.editable.actionProfiles",
                report);
            var newProfiles = AgentDocumentMutationSupport.Index(
                target.actionProfiles,
                value => value.actionId,
                "document.editable.actionProfiles",
                report);
            foreach (AgentActionProfile profile in target.actionProfiles ?? new List<AgentActionProfile>())
            {
                string path = $"document.editable.actionProfiles[{AgentDocumentMutationSupport.Escape(profile.actionId)}]";
                if (!oldProfiles.TryGetValue(profile.actionId, out AgentActionProfile oldProfile))
                {
                    report.Error(path, "action_profile_create_unsupported", "ActionProfile资产不能由Agent Document创建。");
                    continue;
                }
                if (!AgentDocumentMutationSupport.Same(oldProfile.blockQuery, profile.blockQuery))
                    report.Error(path + ".blockQuery", "action_profile_block_query_readonly", "当前正式authoring API未开放blockQuery写入。");

                AgentAssetReference actionProfile = new AgentAssetReference(profile.actionId, profile.assetPath, profile.assetGuid);
                if (!AgentDocumentMutationSupport.SameList(oldProfile.grantedTags, profile.grantedTags))
                {
                    var reader = new AgentMutationValueReader(report, path + ".grantedTags");
                    List<ThirdPersonGameplay.Tags.GameplayTagId> tags = reader.ReadTags(profile.grantedTags, "grantedTags");
                    if (reader.IsValid)
                        mutations.Add(path + ".grantedTags", id => new AgentSetActionProfileGrantedTagsMutation(id, path + ".grantedTags", actionProfile, tags));
                }
                if (!AgentDocumentMutationSupport.Same(oldProfile.cancelQuery, profile.cancelQuery))
                {
                    var reader = new AgentMutationValueReader(report, path + ".cancelQuery");
                    List<ThirdPersonGameplay.Tags.GameplayTagId> all = reader.ReadTags(profile.cancelQuery?.all, "queryAll");
                    List<ThirdPersonGameplay.Tags.GameplayTagId> any = reader.ReadTags(profile.cancelQuery?.any, "queryAny");
                    List<ThirdPersonGameplay.Tags.GameplayTagId> none = reader.ReadTags(profile.cancelQuery?.none, "queryNone");
                    if (reader.IsValid)
                        mutations.Add(path + ".cancelQuery", id => new AgentSetActionProfileCancelQueryMutation(id, path + ".cancelQuery", actionProfile, all, any, none));
                }
                if (!string.Equals(oldProfile.targetRequirement, profile.targetRequirement, StringComparison.Ordinal))
                {
                    var reader = new AgentMutationValueReader(report, path + ".targetRequirement");
                    if (reader.TryParseEnum(
                            profile.targetRequirement,
                            "targetRequirement",
                            out ActionTargetRequirement targetRequirement))
                        mutations.Add(path + ".targetRequirement", id => new AgentSetActionProfileTargetRequirementMutation(id, path + ".targetRequirement", actionProfile, targetRequirement));
                }
            }
            foreach (string removed in oldProfiles.Keys.Except(newProfiles.Keys, StringComparer.Ordinal))
                report.Error(
                    $"document.editable.actionProfiles[{AgentDocumentMutationSupport.Escape(removed)}]",
                    "action_profile_delete_unsupported",
                    "ActionProfile资产不可由Document删除。");
        }
    }
}
