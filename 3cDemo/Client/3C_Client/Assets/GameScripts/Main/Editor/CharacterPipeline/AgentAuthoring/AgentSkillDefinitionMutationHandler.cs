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
            if (!session.TryResolveGraph(command.EntryGraph, command.Path + ".entryGraphAuthoringId", out BaseTree graph))
                valid = false;
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
            foreach (AgentSnapshotSkillSubgraphDependency dependency in
                     definition.subgraphDependencies ?? new List<AgentSnapshotSkillSubgraphDependency>())
            {
                if (dependency == null || string.IsNullOrWhiteSpace(dependency.subgraphIdentity))
                    continue;
                bool resolved = dependency.subgraphIdentity.StartsWith("local:", StringComparison.Ordinal)
                    ? session.Plan.Commands.OfType<AgentEnsureGraphMutation>().Any(value =>
                        string.Equals(value.Id, dependency.subgraphIdentity, StringComparison.Ordinal))
                    : session.Index.TryGetGraph(dependency.subgraphIdentity, out _);
                if (!resolved)
                {
                    session.Report.Error(
                        command.Path + ".subgraphDependencies",
                        "skill_subgraph_dependency_not_found",
                        $"Skill子图依赖无法解析：{dependency.subgraphIdentity}");
                    valid = false;
                }
                if (!ValidateCallSiteIdentity(session, dependency.callSiteIdentity, command.Path + ".subgraphDependencies"))
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
            string skillId = ResolveStableSkillId(session, definition.skillId);
            CharacterSkillAuthoringDefinition target = values.FirstOrDefault(value =>
                string.Equals(value.SkillId, skillId, StringComparison.Ordinal) ||
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
            if (!session.TryResolveGraph(command.EntryGraph, command.Path + ".entryGraphAuthoringId", out BaseTree entryGraph) ||
                entryGraph == null)
                return;
            target.ConfigureAuthoring(
                skillId,
                entryGraph.GraphAuthoringId,
                actionProfile,
                actionContext,
                definition.sourceInputRequestId,
                definition.consumeSourceInputRequest,
                definition.targetInputValueId,
                definition.targetKey);
            target.ConfigureSkillRelations(
                (definition.subgraphDependencies ?? new List<AgentSnapshotSkillSubgraphDependency>())
                    .Select(value => value == null
                        ? null
                        : new CharacterSkillSubgraphDependencyConfiguration(
                            ResolveStableGraphId(session, value.subgraphIdentity, command.Path + ".subgraphDependencies"),
                            session.ResolveStableCallSiteIdentity(value.callSiteIdentity, command.Path + ".subgraphDependencies"))),
                (definition.allowedFollowUpSkillIds ?? new List<string>())
                    .Select(value => ResolveStableSkillId(session, value)));
            session.Definition.SetSkillDefinitions(values.ToArray());
            session.AddAppliedAuthoring(command, session.Definition, target, skillId, entryGraph.GraphAuthoringId);
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

        static string ResolveStableSkillId(
            AgentMutationSession session,
            string identity)
        {
            if (string.IsNullOrEmpty(identity) || !identity.StartsWith("local:", StringComparison.Ordinal))
                return identity;
            string candidate = identity.Substring("local:".Length);
            if (string.IsNullOrEmpty(candidate))
                throw new InvalidOperationException("SkillDefinition local identity不能为空。");
            var used = new HashSet<string>(StringComparer.Ordinal);
            foreach (string existing in session?.InitialSkillIds ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(existing))
                    continue;
                used.Add(existing.StartsWith("local:", StringComparison.Ordinal)
                    ? existing.Substring("local:".Length)
                    : existing);
            }
            foreach (AgentSetSkillDefinitionMutation planned in
                     session?.Plan?.Commands?.OfType<AgentSetSkillDefinitionMutation>() ??
                     Array.Empty<AgentSetSkillDefinitionMutation>())
            {
                if (string.Equals(planned.Definition.skillId, identity, StringComparison.Ordinal))
                    continue;
                if (planned.Definition.skillId.StartsWith("local:", StringComparison.Ordinal))
                    used.Add(planned.Definition.skillId.Substring("local:".Length));
                else
                    used.Add(planned.Definition.skillId);
            }
            if (!used.Contains(candidate))
                return candidate;
            string suffix = AgentAuthoringDocumentCodec.Hash(identity).Substring(0, 12);
            string stable = candidate + "-" + suffix;
            int collision = 0;
            while (used.Contains(stable))
            {
                collision++;
                stable = candidate + "-" + suffix + "-" + collision.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            return stable;
        }

        static string ResolveStableGraphId(AgentMutationSession session, string identity, string path)
        {
            if (string.IsNullOrEmpty(identity) || !identity.StartsWith("local:", StringComparison.Ordinal))
                return identity;
            AgentGraphTargetReference reference = new AgentGraphTargetReference(
                new AgentAuthoringReference(string.Empty, AgentPlannedIdentityReference.Parse(identity)));
            if (!session.TryResolveGraph(reference, path, out BaseTree graph) || graph == null)
                throw new InvalidOperationException($"Graph local identity无法解析：{identity}");
            return graph.GraphAuthoringId;
        }

        static bool ValidateCallSiteIdentity(AgentMutationSession session, string identity, string path)
        {
            int marker = identity?.IndexOf("/node:", StringComparison.Ordinal) ?? -1;
            int nodeStart = marker < 0 ? -1 : marker + "/node:".Length;
            int nodeEnd = nodeStart < 0 ? -1 : identity.IndexOf('/', nodeStart);
            if (marker < 0 || nodeEnd < 0)
                return true;
            string graphId = identity.Substring(0, marker);
            string nodeId = identity.Substring(nodeStart, nodeEnd - nodeStart);
            string nodeBaseId = nodeId;
            int roleSeparator = nodeId.LastIndexOf('#');
            if (roleSeparator > "local:".Length)
                nodeBaseId = nodeId.Substring(0, roleSeparator);
            bool graphValid = !graphId.StartsWith("local:", StringComparison.Ordinal) ||
                session.Plan.Commands.Any(value => value.Id == graphId &&
                    (value.OutputKind == AgentMutationOutputKind.Graph ||
                     value.OutputKind == AgentMutationOutputKind.State ||
                     value.OutputKind == AgentMutationOutputKind.StateMachine));
            bool nodeValid = !nodeBaseId.StartsWith("local:", StringComparison.Ordinal) ||
                session.Plan.Commands.Any(value => value.Id == nodeBaseId &&
                    (value.OutputKind == AgentMutationOutputKind.Node ||
                     value.OutputKind == AgentMutationOutputKind.State ||
                     value.OutputKind == AgentMutationOutputKind.StateMachine));
            if (graphValid && nodeValid)
                return true;
            session.Report.Error(path, "skill_call_site_local_identity_missing", $"Skill call site使用了未计划的local identity：{identity}");
            return false;
        }
    }
}
