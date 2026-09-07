using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentMutationLoweringSupport
    {
        internal static AgentAssetReference ReadActionContext(AgentMutationDraft operation)
        {
            return new AgentAssetReference(operation.actionContext, operation.actionContextAssetPath, operation.actionContextAssetGuid);
        }

        internal static AgentAssetReference ReadActionProfile(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            string actionProfile = context.RequiredText(operation.actionProfile, string.Empty, "actionProfile", "ActionProfile identity 缺失。");
            return new AgentAssetReference(actionProfile, string.Empty, string.Empty);
        }

        internal static List<GameplayTagId> ReadTags(AgentMutationPlanningContext context, List<string> values, string field)
        {
            var result = new List<GameplayTagId>();
            var unique = new HashSet<GameplayTagId>();
            if (values == null)
                return result;
            for (int i = 0; i < values.Count; i++)
            {
                var tag = new GameplayTagId(values[i]);
                if (!tag.IsValid || !unique.Add(tag))
                {
                    context.Error($"{field}[{i}]", "gameplay_tag_invalid", $"GameplayTag 缺失或重复：{values[i]}");
                    continue;
                }
                result.Add(tag);
            }
            return result;
        }

        internal static Type ParseBlackboardValueType(AgentMutationPlanningContext context, string value)
        {
            switch (value?.Trim().ToLowerInvariant())
            {
                case "bool": case "boolean": case "system.boolean": return typeof(bool);
                case "int": case "int32": case "system.int32": return typeof(int);
                case "float": case "single": case "system.single": return typeof(float);
                case "string": case "system.string": return typeof(string);
                case "vector2": case "unityengine.vector2": return typeof(Vector2);
                case "vector3": case "unityengine.vector3": return typeof(Vector3);
                case "actiontargetsnapshot": case "action_target_snapshot": case "thirdpersoncharacter.actionsystem.actiontargetsnapshot": return typeof(ActionTargetSnapshot);
                case "aiactorid": case "ai_actor_id": case "thirdpersoncharacter.ai.aiactoridvalue": return typeof(AIActorIdValue);
                case "aiactiontargetsnapshot": case "ai_action_target_snapshot": case "thirdpersoncharacter.ai.aiactiontargetsnapshotvalue": return typeof(AIActionTargetSnapshotValue);
                default:
                    context.Error("blackboardValueType", "blackboard_value_type_invalid", $"不支持的 Blackboard value type：{value}");
                    return null;
            }
        }

        internal static object AIBlackboardDefault(AgentMutationDraft operation, Type valueType)
        {
            if (valueType == typeof(bool)) return operation.blackboardBoolValue;
            if (valueType == typeof(int)) return operation.blackboardIntValue;
            if (valueType == typeof(float)) return operation.blackboardFloatValue;
            if (valueType == typeof(Vector2)) return operation.blackboardVector2Value;
            if (valueType == typeof(Vector3)) return operation.blackboardVector3Value;
            if (valueType == typeof(AIActorIdValue)) return new AIActorIdValue(operation.blackboardActorIdValue);
            if (valueType == typeof(AIActionTargetSnapshotValue))
            {
                return new AIActionTargetSnapshotValue(
                    new AIActorIdValue(operation.blackboardTargetActorIdValue),
                    operation.blackboardTargetPositionValue,
                    operation.blackboardTargetYawValue);
            }
            throw new InvalidOperationException($"Unsupported AI Blackboard value type: {valueType?.FullName}");
        }

        internal static object ReadBlackboardDefault(
            AgentMutationPlanningContext context,
            Newtonsoft.Json.Linq.JToken token,
            Type valueType)
        {
            if (valueType == null)
                return null;
            if (token == null ||
                token.Type == Newtonsoft.Json.Linq.JTokenType.Null && valueType.IsValueType)
            {
                context.Error("blackboardDefaultValue", "blackboard_default_missing", "Blackboard declaration 必须显式声明与ValueType一致的defaultValue。");
                return null;
            }
            try
            {
                return token.Type == Newtonsoft.Json.Linq.JTokenType.Null
                    ? null
                    : token.ToObject(valueType);
            }
            catch (Exception exception)
            {
                context.Error("blackboardDefaultValue", "blackboard_default_invalid", $"Blackboard defaultValue无效：{exception.Message}");
                return null;
            }
        }

        internal static void ValidateBlackboardPayloads(
            AgentMutationPlanningContext context,
            AgentSnapshotBlackboardInputBinding inputBinding,
            AgentSnapshotBlackboardFactProjection factProjection)
        {
            if (inputBinding != null && string.IsNullOrWhiteSpace(inputBinding.inputValueId))
                context.Error("inputBinding.inputValueId", "input_value_id_missing", "Blackboard Input Binding 必须显式提供 inputValueId。");
            if (factProjection == null)
                return;
            if (!TryParseEnum(context, factProjection.kind, "factProjection.kind", out PipelineBlackboardFactProjectionKind projection))
                return;
            if (projection == PipelineBlackboardFactProjectionKind.ActionWindow)
            {
                if (string.IsNullOrWhiteSpace(factProjection.windowType))
                    context.Error("factProjection.windowType", "window_type_missing", "ActionWindow Fact Projection 必须显式提供 windowType。");
                if (string.IsNullOrWhiteSpace(factProjection.windowId))
                    context.Error("factProjection.windowId", "window_id_missing", "ActionWindow Fact Projection 必须显式提供 windowId。");
            }
        }

        internal static bool TryParseEnum<T>(AgentMutationPlanningContext context, string value, string field, out T result) where T : struct
        {
            if (Enum.TryParse(value, true, out result) && Enum.IsDefined(typeof(T), result))
                return true;
            context.Error(field, $"{field}_invalid", $"{field} 无效：{value}");
            return false;
        }

        internal static string First(string value, string fallback)
        {
            return !string.IsNullOrEmpty(value) ? value : fallback ?? string.Empty;
        }
    }
}

