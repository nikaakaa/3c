using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal sealed class AgentSkillDefinitionMutationHandler : IAgentMutationHandler
    {
        public bool Preflight(AgentMutationSession session, AgentMutation command)
        {
            switch (command)
            {
                case AgentSetSkillDefinitionMutation set:
                    return PreflightSet(session, set);
                case AgentDeleteSkillDefinitionMutation delete:
                    return PreflightDelete(session, delete);
                default:
                    throw new InvalidOperationException($"Unsupported skill definition command: {command.Kind}");
            }
        }

        public void Apply(AgentMutationSession session, AgentMutation command)
        {
            switch (command)
            {
                case AgentSetSkillDefinitionMutation set:
                    ApplySet(session, set);
                    break;
                case AgentDeleteSkillDefinitionMutation delete:
                    ApplyDelete(session, delete);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported skill definition command: {command.Kind}");
            }
        }

        static bool PreflightSet(AgentMutationSession session, AgentSetSkillDefinitionMutation command)
        {
            AgentSnapshotSkillDefinition definition = command.Definition;
            if (!session.Definition)
            {
                session.Report.Error(command.Path, "skill_definition_domain_invalid", "SkillDefinition mutation只能作用于CharacterController。");
                return false;
            }
            bool valid = true;
            if (!session.Index.TryGetGraph(definition.entryGraphAuthoringId, out BaseTree graph))
            {
                session.Report.Error(command.Path + ".entryGraphAuthoringId", "skill_entry_graph_not_found", $"Skill入口Graph无法解析：{definition.entryGraphAuthoringId}");
                valid = false;
            }
            if (!session.Resolver.TryResolveActionProfile(definition.actionProfileId, out ActionProfile _))
            {
                session.Report.Error(command.Path + ".actionProfileId", "skill_action_profile_not_found", $"ActionProfile无法解析：{definition.actionProfileId}");
                valid = false;
            }
            AgentAssetReference actionContext = new AgentAssetReference(
                definition.actionContext,
                definition.actionContextAssetPath,
                definition.actionContextAssetGuid);
            if (!session.Resolver.TryResolveActionContext(actionContext, out ActionContextSlot _))
            {
                session.Report.Error(command.Path + ".actionContext", "skill_action_context_not_found", $"ActionContext无法解析：{definition.actionContext}");
                valid = false;
            }
            int matches = session.Definition.SkillDefinitions.Count(value =>
                value != null && string.Equals(value.SkillId, definition.skillId, StringComparison.Ordinal));
            if (matches > 1)
            {
                session.Report.Error(command.Path + ".skillId", "skill_definition_duplicate", $"SkillDefinition identity已重复：{definition.skillId}");
                valid = false;
            }
            if (valid)
                session.AddPlanned(command, graph, definition.skillId, definition.entryGraphAuthoringId);
            return valid;
        }

        static bool PreflightDelete(AgentMutationSession session, AgentDeleteSkillDefinitionMutation command)
        {
            if (!session.Definition)
            {
                session.Report.Error(command.Path, "skill_definition_domain_invalid", "SkillDefinition mutation只能作用于CharacterController。");
                return false;
            }
            CharacterSkillAuthoringDefinition definition = session.Definition.SkillDefinitions.FirstOrDefault(value =>
                value != null && string.Equals(value.SkillId, command.SkillId, StringComparison.Ordinal));
            if (definition == null)
            {
                session.Report.Error(command.Path, "skill_definition_not_found", $"SkillDefinition无法解析：{command.SkillId}");
                return false;
            }
            session.AddPlanned(command, null, command.SkillId, "removed");
            return true;
        }

        static void ApplySet(AgentMutationSession session, AgentSetSkillDefinitionMutation command)
        {
            AgentSnapshotSkillDefinition definition = command.Definition;
            List<CharacterSkillAuthoringDefinition> values = session.Definition.SkillDefinitions
                .Where(value => value != null)
                .ToList();
            CharacterSkillAuthoringDefinition target = values.FirstOrDefault(value =>
                string.Equals(value.SkillId, definition.skillId, StringComparison.Ordinal));
            if (target == null)
            {
                target = new CharacterSkillAuthoringDefinition();
                values.Add(target);
            }
            if (!session.Resolver.TryResolveActionProfile(definition.actionProfileId, out ActionProfile actionProfile) ||
                !session.Resolver.TryResolveActionContext(
                    new AgentAssetReference(definition.actionContext, definition.actionContextAssetPath, definition.actionContextAssetGuid),
                    out ActionContextSlot actionContext))
                throw new InvalidOperationException($"SkillDefinition '{definition.skillId}' assets changed after preflight.");
            target.ConfigureAuthoring(
                definition.skillId,
                definition.entryGraphAuthoringId,
                actionProfile,
                actionContext,
                definition.sourceInputRequestId,
                definition.consumeSourceInputRequest,
                definition.targetInputValueId,
                definition.targetKey);
            session.Definition.SetSkillDefinitions(values.ToArray());
            session.AddAppliedAuthoring(command, session.Definition, target, definition.skillId, definition.entryGraphAuthoringId);
        }

        static void ApplyDelete(AgentMutationSession session, AgentDeleteSkillDefinitionMutation command)
        {
            List<CharacterSkillAuthoringDefinition> values = session.Definition.SkillDefinitions
                .Where(value => value != null && !string.Equals(value.SkillId, command.SkillId, StringComparison.Ordinal))
                .ToList();
            if (values.Count == session.Definition.SkillDefinitions.Count)
                throw new InvalidOperationException($"SkillDefinition '{command.SkillId}' changed after preflight.");
            session.Definition.SetSkillDefinitions(values.ToArray());
            session.AddAppliedAuthoring(command, session.Definition, null, command.SkillId, "removed");
        }
    }
}
