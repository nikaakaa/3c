using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using static ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.AgentMutationLoweringSupport;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentActionMutationLowering
    {
        internal static AgentMutation LowerEnsureGameplayTag(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string tag = context.RequiredText(operation.gameplayTag, string.Empty, "gameplayTag", "ensure_gameplay_tag 缺少 tag id。");
            return context.IsValid ? new AgentEnsureGameplayTagMutation(operation.id, context.Path, tag, operation.parentGameplayTag, First(operation.displayName, tag), operation.debugCategory) : null;
        }

        internal static AgentMutation LowerSetActionProfileGrantedTags(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentAssetReference profile = ReadActionProfile(context, operation);
            return context.IsValid ? new AgentSetActionProfileGrantedTagsMutation(operation.id, context.Path, profile, ReadTags(context, operation.grantedTags, "grantedTags")) : null;
        }

        internal static AgentMutation LowerSetActionProfileCancelQuery(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentAssetReference profile = ReadActionProfile(context, operation);
            List<GameplayTagId> all = ReadTags(context, operation.queryAll, "queryAll");
            List<GameplayTagId> any = ReadTags(context, operation.queryAny, "queryAny");
            List<GameplayTagId> none = ReadTags(context, operation.queryNone, "queryNone");
            return context.IsValid ? new AgentSetActionProfileCancelQueryMutation(operation.id, context.Path, profile, all, any, none) : null;
        }

        internal static AgentMutation LowerSetActionProfileTargetRequirement(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentAssetReference profile = ReadActionProfile(context, operation);
            TryParseEnum(context, operation.targetRequirement, "targetRequirement", out ActionTargetRequirement requirement);
            return context.IsValid
                ? new AgentSetActionProfileTargetRequirementMutation(operation.id, context.Path, profile, requirement)
                : null;
        }

        internal static AgentMutation LowerSetActionRequestTimingClass(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation)
        {
            string requestId = context.RequiredText(
                operation.request,
                string.Empty,
                "request",
                "set_action_request_timing_class 缺少 request id。");
            TryParseEnum(
                context,
                operation.requestTimingClass,
                "requestTimingClass",
                out CharacterActionRequestTimingClass timingClass);
            return context.IsValid
                ? new AgentSetActionRequestTimingClassMutation(operation.id, context.Path, requestId, timingClass)
                : null;
        }
    }
}

