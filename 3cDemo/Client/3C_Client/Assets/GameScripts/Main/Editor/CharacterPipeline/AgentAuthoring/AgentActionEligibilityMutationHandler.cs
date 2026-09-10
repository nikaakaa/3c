using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonGameplay.Tags;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentActionEligibilityMutationHandler : IAgentMutationHandler
    {
        public bool Preflight(AgentMutationSession session, AgentMutation command)
        {
            switch (command)
            {
                case AgentEnsureGameplayTagMutation value:
                    if (!ValidateTag(session, value.Tag, value.ParentTag, value.Path))
                        return false;
                    session.PlanGameplayTag(value.Tag.Value);
                    session.AddPlanned(value, value.Tag.Value, "ensure gameplay tag");
                    return true;
                case AgentSetActionProfileGrantedTagsMutation value:
                    if (!TryResolveProfile(session, value.ActionProfile, value.Path, out ActionProfile grantedProfile) ||
                        !ValidateTags(session, value.Tags, value.Path))
                        return false;
                    session.AddPlanned(value, grantedProfile.ActionId, "set granted tags");
                    return true;
                case AgentSetActionProfileCancelQueryMutation value:
                    if (!TryResolveProfile(session, value.ActionProfile, value.Path, out ActionProfile cancelProfile) ||
                        !ValidateTags(session, value.All.Concat(value.Any).Concat(value.None).ToList(), value.Path))
                        return false;
                    session.AddPlanned(value, cancelProfile.ActionId, "set cancel query");
                    return true;
                case AgentSetActionProfileTargetRequirementMutation value:
                    if (!TryResolveProfile(session, value.ActionProfile, value.Path, out ActionProfile targetProfile))
                        return false;
                    session.AddPlanned(value, targetProfile.ActionId, $"set target requirement {value.TargetRequirement}");
                    return true;
                case AgentSetActionRequestTimingClassMutation value:
                    if (!TryResolveActionRequest(session, value.RequestId, value.Path, out _))
                        return false;
                    session.AddPlanned(value, value.RequestId, $"set request timing {value.TimingClass}");
                    return true;
                default:
                    throw new InvalidOperationException($"Unsupported action eligibility command: {command.Kind}");
            }
        }

        public void Apply(AgentMutationSession session, AgentMutation command)
        {
            switch (command)
            {
                case AgentEnsureGameplayTagMutation value:
                    ApplyEnsureGameplayTag(session, value);
                    break;
                case AgentSetActionProfileGrantedTagsMutation value:
                    ApplyGrantedTags(session, value);
                    break;
                case AgentSetActionProfileCancelQueryMutation value:
                    ApplyCancelQuery(session, value);
                    break;
                case AgentSetActionProfileTargetRequirementMutation value:
                    ApplyTargetRequirement(session, value);
                    break;
                case AgentSetActionRequestTimingClassMutation value:
                    ApplyRequestTimingClass(session, value);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported action eligibility command: {command.Kind}");
            }
        }

        static void ApplyEnsureGameplayTag(AgentMutationSession session, AgentEnsureGameplayTagMutation command)
        {
            GameplayTagCatalog catalog = session.Definition.GameplayEffectProfile.TagCatalog;
            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty tags = serialized.FindProperty("m_Tags");
            SerializedProperty entry = FindTag(tags, command.Tag.Value);
            if (entry == null)
            {
                int index = tags.arraySize;
                tags.InsertArrayElementAtIndex(index);
                entry = tags.GetArrayElementAtIndex(index);
            }
            entry.FindPropertyRelative("m_TagId").FindPropertyRelative("m_Value").stringValue = command.Tag.Value;
            entry.FindPropertyRelative("m_DisplayName").stringValue = command.DisplayName;
            entry.FindPropertyRelative("m_ParentTag").FindPropertyRelative("m_Value").stringValue = command.ParentTag.Value;
            entry.FindPropertyRelative("m_DebugCategory").stringValue = command.DebugCategory;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            session.AddAppliedAuthoring(command, catalog, null, command.Tag.Value, "ensure gameplay tag");
        }

        static void ApplyGrantedTags(AgentMutationSession session, AgentSetActionProfileGrantedTagsMutation command)
        {
            if (!TryResolveProfile(session, command.ActionProfile, command.Path, out ActionProfile profile))
                return;
            SerializedObject serialized = new SerializedObject(profile);
            WriteTagArray(serialized.FindProperty("m_Tags"), command.Tags);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            session.AddAppliedAuthoring(command, profile, null, profile.ActionId, "set granted tags");
        }

        static void ApplyCancelQuery(AgentMutationSession session, AgentSetActionProfileCancelQueryMutation command)
        {
            if (!TryResolveProfile(session, command.ActionProfile, command.Path, out ActionProfile profile))
                return;
            SerializedObject serialized = new SerializedObject(profile);
            SerializedProperty query = serialized.FindProperty("m_CancelTags");
            WriteTagArray(query.FindPropertyRelative("m_All"), command.All);
            WriteTagArray(query.FindPropertyRelative("m_Any"), command.Any);
            WriteTagArray(query.FindPropertyRelative("m_None"), command.None);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            session.AddAppliedAuthoring(command, profile, null, profile.ActionId, "set cancel query");
        }

        static void ApplyTargetRequirement(
            AgentMutationSession session,
            AgentSetActionProfileTargetRequirementMutation command)
        {
            if (!TryResolveProfile(session, command.ActionProfile, command.Path, out ActionProfile profile))
                return;
            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("m_TargetRequirement").enumValueIndex = (int)command.TargetRequirement;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            session.AddAppliedAuthoring(command, profile, null, profile.ActionId, $"set target requirement {command.TargetRequirement}");
        }

        static void ApplyRequestTimingClass(
            AgentMutationSession session,
            AgentSetActionRequestTimingClassMutation command)
        {
            if (!TryResolveActionRequest(session, command.RequestId, command.Path, out CharacterActionRequestDefinition request))
                return;
            CharacterInputProfile profile = session.Definition.InputProfile;
            SerializedObject serialized = new SerializedObject(profile);
            SerializedProperty requests = serialized.FindProperty("m_ActionRequests");
            SerializedProperty target = null;
            for (int i = 0; i < requests.arraySize; i++)
            {
                SerializedProperty candidate = requests.GetArrayElementAtIndex(i);
                if (string.Equals(
                        candidate.FindPropertyRelative("m_RequestId").stringValue,
                        request.RequestId,
                        StringComparison.Ordinal))
                {
                    target = candidate;
                    break;
                }
            }
            if (target == null)
                throw new InvalidOperationException($"Action request '{command.RequestId}' disappeared during apply.");
            target.FindPropertyRelative("m_TimingClass").intValue = (int)command.TimingClass;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            session.AddAppliedAuthoring(command, profile, null, command.RequestId, "set request timing class");
        }

        static bool TryResolveActionRequest(
            AgentMutationSession session,
            string requestId,
            string path,
            out CharacterActionRequestDefinition request)
        {
            request = null;
            CharacterInputProfile profile = session.Definition ? session.Definition.InputProfile : null;
            if (profile)
            {
                for (int i = 0; i < profile.ActionRequests.Count; i++)
                {
                    CharacterActionRequestDefinition candidate = profile.ActionRequests[i];
                    if (candidate != null && string.Equals(candidate.RequestId, requestId, StringComparison.Ordinal))
                    {
                        request = candidate;
                        return true;
                    }
                }
            }
            session.Report.Error(path, "action_request_missing", $"Action request 无法解析：{requestId}");
            return false;
        }

        static bool TryResolveProfile(
            AgentMutationSession session,
            AgentAssetReference reference,
            string path,
            out ActionProfile profile)
        {
            if (session.Resolver.TryResolveActionProfile(reference.LogicalId, out profile))
                return true;
            session.Report.Error(path, "action_profile_not_found", $"ActionProfile 未在当前 Definition 中找到：{reference.LogicalId}");
            return false;
        }

        static bool ValidateTag(
            AgentMutationSession session,
            GameplayTagId tag,
            GameplayTagId parent,
            string path)
        {
            if (!tag.IsValid)
            {
                session.Report.Error(path, "gameplay_tag_invalid", "GameplayTag id 缺失。");
                return false;
            }
            GameplayTagCatalog catalog = session.Definition.GameplayEffectProfile?.TagCatalog;
            if (!catalog)
            {
                session.Report.Error(path, "gameplay_tag_catalog_missing", "GameplayTagCatalog 缺失。");
                return false;
            }
            if (parent.IsValid &&
                !catalog.Tags.Any(value => value != null && value.TagId == parent) &&
                !session.IsGameplayTagPlanned(parent.Value))
            {
                session.Report.Error(path, "gameplay_tag_parent_missing", $"GameplayTag parent 未注册：{parent}");
                return false;
            }
            return true;
        }

        static bool ValidateTags(
            AgentMutationSession session,
            IReadOnlyList<GameplayTagId> tags,
            string path)
        {
            GameplayTagCatalog catalog = session.Definition.GameplayEffectProfile?.TagCatalog;
            if (!catalog)
            {
                session.Report.Error(path, "gameplay_tag_catalog_missing", "GameplayTagCatalog 缺失。");
                return false;
            }
            bool valid = true;
            for (int i = 0; i < tags.Count; i++)
            {
                if (catalog.Tags.Any(value => value != null && value.TagId == tags[i]) ||
                    session.IsGameplayTagPlanned(tags[i].Value))
                    continue;
                session.Report.Error(path, "gameplay_tag_missing", $"GameplayTag 未注册：{tags[i]}");
                valid = false;
            }
            return valid;
        }

        static SerializedProperty FindTag(SerializedProperty tags, string tag)
        {
            for (int i = 0; i < tags.arraySize; i++)
            {
                SerializedProperty entry = tags.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("m_TagId").FindPropertyRelative("m_Value").stringValue == tag)
                    return entry;
            }
            return null;
        }

        static void WriteTagArray(SerializedProperty array, IReadOnlyList<GameplayTagId> values)
        {
            array.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                array.GetArrayElementAtIndex(i).FindPropertyRelative("m_Value").stringValue = values[i].Value;
        }
    }
}

