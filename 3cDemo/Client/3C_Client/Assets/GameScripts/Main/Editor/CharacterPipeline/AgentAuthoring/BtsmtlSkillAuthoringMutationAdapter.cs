using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal sealed class BtsmtlSkillAuthoringMutationAdapter : IAgentMutationHandler
    {
        public bool Preflight(AgentMutationSession session, AgentMutation command)
        {
            if (command is not BtsmtlSetSkillDocumentMutation set)
                throw new InvalidOperationException($"Unsupported Skill Flow command: {command.Kind}");
            if (!session.Definition)
            {
                session.Report.Error(command.Path, "skill_flow_definition_missing", "Skill Flow Document mutation只能作用于CharacterController。");
                return false;
            }
            bool valid = AgentSkillFlowDocumentMapper.Validate(
                set.Document,
                session.Report,
                session.Definition.ControlModuleId,
                AssetProviderOwner(session.Definition.InputProfile),
                AssetProviderOwner(session.Definition.GameplayEffectProfile));
            valid &= BtsmtlSkillDocumentAssetValidator.Validate(
                session,
                set.Document,
                command.Path);
            if (valid)
            {
                try
                {
                    AgentSkillFlowAssetPaths.PlannedRoots(session.Definition, set.Document);
                }
                catch (InvalidOperationException exception)
                {
                    session.Report.Error(command.Path, "skill_root_asset_path_conflict", exception.Message);
                    valid = false;
                }
            }
            if (valid)
                session.AddPlanned(command, "Skill Flow Document", $"graphs={set.Document.graphs.Count}; macros={set.Document.macros.Count}; timelines={set.Document.timelines.Count}");
            return valid;
        }

        public void Apply(AgentMutationSession session, AgentMutation command)
        {
            BtsmtlSetSkillDocumentMutation set = command as BtsmtlSetSkillDocumentMutation ??
                throw new InvalidOperationException($"Unsupported Skill Flow command: {command.Kind}");
            new BtsmtlSkillDocumentApplier().Apply(session, set.Document);
            session.AddAppliedAuthoring(command, session.Definition, session.Definition, "Skill Flow Document", "native Skill Graph closure");
        }

        static string AssetProviderOwner(UnityEngine.Object asset)
        {
            string path = asset ? AssetDatabase.GetAssetPath(asset) : string.Empty;
            return CharacterSkillProviderOwners.Asset(
                string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path));
        }
    }

    internal sealed class BtsmtlSkillDocumentApplier
    {
        AgentMutationSession m_Session;
        AgentPackageSkillFlowDocument m_Document;
        BtsmtlSkillGraphAuthoringApplier m_GraphApplier;
        BtsmtlSkillTimelineAuthoringApplier m_TimelineApplier;

        public void Apply(AgentMutationSession session, AgentPackageSkillFlowDocument document)
        {
            m_Session = session ?? throw new ArgumentNullException(nameof(session));
            m_Document = document ?? throw new ArgumentNullException(nameof(document));
            m_GraphApplier = new BtsmtlSkillGraphAuthoringApplier(
                m_Session,
                m_Document,
                identity => m_TimelineApplier?.ResolveTimeline(identity));
            m_GraphApplier.Resolve();
            m_TimelineApplier = new BtsmtlSkillTimelineAuthoringApplier(
                m_Session,
                m_Document,
                m_GraphApplier.Current,
                m_GraphApplier.ResolveGraph);
            m_TimelineApplier.Resolve();
            m_GraphApplier.Sync();
            m_TimelineApplier.Sync();
            SyncSkillDefinitions();
            m_TimelineApplier.DeleteRemoved();
            m_GraphApplier.DeleteRemoved();
            foreach (BtsmtlSkillFlowGraph root in m_Session.Definition.SkillGraphs)
                BtsmtlSkillGraphClosure.Validate(root, true);
            m_GraphApplier.ValidateAppliedIdentityContracts();
        }

        void SyncSkillDefinitions()
        {
            var values = new List<CharacterSkillAuthoringDefinition>();
            var skillIds = new HashSet<string>(StringComparer.Ordinal);
            var resolvedSkillIds = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (AgentPackageSkillDefinitionFile source in m_Document.skills ?? new List<AgentPackageSkillDefinitionFile>())
            {
                if (source == null)
                    continue;
                resolvedSkillIds[source.skillId] = m_GraphApplier.StableSkillId(source.skillId, skillIds);
                skillIds.Add(resolvedSkillIds[source.skillId]);
            }
            foreach (AgentPackageSkillDefinitionFile source in m_Document.skills ?? new List<AgentPackageSkillDefinitionFile>())
            {
                if (source == null)
                    continue;
                string skillId = resolvedSkillIds[source.skillId];
                if (!m_Session.Resolver.TryResolveActionProfile(source.actionProfileId, out ActionProfile profile) ||
                    !m_Session.Resolver.TryResolveActionContext(
                        new AgentAssetReference(
                            source.actionContext,
                            source.actionContextAssetPath,
                            source.actionContextAssetGuid),
                        out ActionContextSlot context))
                    throw new InvalidOperationException($"SkillDefinition '{source.skillId}'资源在apply阶段无法解析。");
                BtsmtlSkillFlowGraph entry = m_GraphApplier.ResolveGraph(source.entryGraphAuthoringId) as BtsmtlSkillFlowGraph;
                if (!entry)
                    throw new InvalidOperationException($"SkillDefinition '{source.skillId}'入口Graph无法解析。");
                var definition = new CharacterSkillAuthoringDefinition();
                definition.ConfigureAuthoring(
                    skillId,
                    entry.AuthoringId,
                    profile,
                    context,
                    source.sourceInputRequestId,
                    source.consumeSourceInputRequest,
                    source.targetInputValueId,
                    source.targetKey);
                definition.ConfigureSkillRelations(
                    (source.subgraphDependencies ?? new List<AgentPackageSkillSubgraphDependency>())
                        .Where(value => value != null)
                        .Select(value => new CharacterSkillSubgraphDependencyConfiguration(
                            m_GraphApplier.ResolveGraph(value.subgraphIdentity) is IBtsmtlSkillFlowGraph graph
                                ? graph.AuthoringId
                                : value.subgraphIdentity,
                            m_GraphApplier.ResolveCallSite(value.callSiteIdentity))),
                    (source.allowedFollowUpSkillIds ?? new List<string>())
                        .Select(value => resolvedSkillIds.TryGetValue(value, out string resolved) ? resolved : value));
                values.Add(definition);
            }
            m_Session.Definition.SetSkillDefinitions(values.ToArray());
            BtsmtlSkillFlowGraph[] roots = m_Document.skills
                .Select(value => m_GraphApplier.ResolveGraph(value.entryGraphAuthoringId) as BtsmtlSkillFlowGraph)
                .Where(value => value)
                .Distinct()
                .ToArray();
            m_Session.Definition.SetSkillGraphs(roots);
        }
    }
}
