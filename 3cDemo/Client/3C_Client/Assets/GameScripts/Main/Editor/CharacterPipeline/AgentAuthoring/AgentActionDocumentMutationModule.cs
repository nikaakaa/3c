using System;
using System.Collections.Generic;
using System.Linq;
using static ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.AgentDocumentMutationSupport;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentActionDocumentMutationModule
    {
        internal static void BuildActionMutations(
            AgentGraphSnapshot current,
            AgentDocumentEditable target,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            var oldRequests = Index(current.actionRequests, value => value.requestId, "document.editable.actionRequests", report);
            var newRequests = Index(target.actionRequests, value => value.requestId, "document.editable.actionRequests", report);
            foreach (AgentSnapshotActionRequest request in target.actionRequests ?? new List<AgentSnapshotActionRequest>())
            {
                string path = $"document.editable.actionRequests[{Escape(request.requestId)}]";
                if (!oldRequests.TryGetValue(request.requestId, out AgentSnapshotActionRequest oldRequest))
                {
                    report.Error(path, "action_request_create_unsupported", "Action Request catalog不能由Agent Document创建。");
                    continue;
                }
                if (oldRequest.bufferSeconds != request.bufferSeconds || oldRequest.priority != request.priority)
                    report.Error(path, "action_request_readonly_field_modified", "bufferSeconds与priority来自正式Action catalog，不可由Document修改。");
                if (!string.Equals(oldRequest.timingClass, request.timingClass, StringComparison.Ordinal))
                {
                    Add(mutations, path, AgentMutationKind.SetActionRequestTimingClass, operation =>
                    {
                        operation.request = request.requestId;
                        operation.requestTimingClass = request.timingClass;
                    });
                }
            }
            foreach (string removed in oldRequests.Keys.Except(newRequests.Keys, StringComparer.Ordinal))
                report.Error($"document.editable.actionRequests[{Escape(removed)}]", "action_request_delete_unsupported", "Action Request catalog不可由Document删除。");

            var oldProfiles = Index(current.actionProfiles, value => value.actionId, "document.editable.actionProfiles", report);
            var newProfiles = Index(target.actionProfiles, value => value.actionId, "document.editable.actionProfiles", report);
            foreach (AgentSnapshotActionProfile profile in target.actionProfiles ?? new List<AgentSnapshotActionProfile>())
            {
                string path = $"document.editable.actionProfiles[{Escape(profile.actionId)}]";
                if (!oldProfiles.TryGetValue(profile.actionId, out AgentSnapshotActionProfile oldProfile))
                {
                    report.Error(path, "action_profile_create_unsupported", "ActionProfile资产不能由Agent Document创建。");
                    continue;
                }
                if (!string.Equals(AgentAuthoringDocumentCodec.Hash(oldProfile.blockQuery), AgentAuthoringDocumentCodec.Hash(profile.blockQuery), StringComparison.Ordinal))
                    report.Error(path + ".blockQuery", "action_profile_block_query_readonly", "当前正式authoring API未开放blockQuery写入。");
                if (!SameList(oldProfile.grantedTags, profile.grantedTags))
                {
                    Add(mutations, path + ".grantedTags", AgentMutationKind.SetActionProfileGrantedTags, operation =>
                    {
                        operation.actionProfile = profile.actionId;
                        operation.grantedTags = profile.grantedTags;
                    });
                }
                if (!Same(oldProfile.cancelQuery, profile.cancelQuery))
                {
                    Add(mutations, path + ".cancelQuery", AgentMutationKind.SetActionProfileCancelQuery, operation =>
                    {
                        operation.actionProfile = profile.actionId;
                        operation.queryAll = profile.cancelQuery.all;
                        operation.queryAny = profile.cancelQuery.any;
                        operation.queryNone = profile.cancelQuery.none;
                    });
                }
                if (!string.Equals(oldProfile.targetRequirement, profile.targetRequirement, StringComparison.Ordinal))
                {
                    Add(mutations, path + ".targetRequirement", AgentMutationKind.SetActionProfileTargetRequirement, operation =>
                    {
                        operation.actionProfile = profile.actionId;
                        operation.targetRequirement = profile.targetRequirement;
                    });
                }
            }
            foreach (string removed in oldProfiles.Keys.Except(newProfiles.Keys, StringComparer.Ordinal))
                report.Error($"document.editable.actionProfiles[{Escape(removed)}]", "action_profile_delete_unsupported", "ActionProfile资产不可由Document删除。");
        }
    }
}
