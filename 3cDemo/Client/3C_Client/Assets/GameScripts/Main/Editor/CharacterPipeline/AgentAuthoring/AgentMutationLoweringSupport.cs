using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonGameplay.Tags;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentMutationLoweringSupport
    {
        internal static AgentAssetReference ReadActionProfile(
            AgentMutationPlanningContext context,
            AgentMutationDraft operation)
        {
            string actionProfile = context.RequiredText(
                operation.actionProfile,
                string.Empty,
                "actionProfile",
                "ActionProfile identity 缺失。");
            return new AgentAssetReference(actionProfile, string.Empty, string.Empty);
        }

        internal static List<GameplayTagId> ReadTags(
            AgentMutationPlanningContext context,
            List<string> values,
            string field)
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

        internal static bool TryParseEnum<T>(
            AgentMutationPlanningContext context,
            string value,
            string field,
            out T result)
            where T : struct
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
