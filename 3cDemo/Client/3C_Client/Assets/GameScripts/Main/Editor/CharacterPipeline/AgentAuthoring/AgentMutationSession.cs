using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using ThirdPersonCharacter.Control.Authoring;
using UnityEditor;
using UnityEngine;

using ThirdPersonCharacter.Pipeline.Editor.Authoring.Presentation;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.Skill;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentMutationSession
    {
        readonly HashSet<UnityEngine.Object> m_TouchedOwners = new HashSet<UnityEngine.Object>();
        readonly HashSet<string> m_PlannedGameplayTags = new HashSet<string>(StringComparer.Ordinal);
        readonly List<Action> m_RollbackActions = new List<Action>();

        public AgentMutationSession(
            CharacterPipelineDefinition definition,
            AgentAuthoringTarget current,
            string sourceRevision,
            AgentMutationPlan plan,
            AgentCompileReport report,
            bool apply,
            AgentPresentationMutationPlan presentationPlan = null)
        {
            Definition = definition;
            Domain = AgentAuthoringSchema.CharacterControllerDomain;
            Current = current;
            SourceRevision = sourceRevision ?? string.Empty;
            Plan = plan;
            Report = report;
            IsApply = apply;
            PresentationPlan = presentationPlan;
            Resolver = new AgentAssetResolver(definition, current);
        }

        public CharacterPipelineDefinition Definition { get; }
        public string Domain { get; }
        public AgentAuthoringTarget Current { get; }
        public string SourceRevision { get; }
        public AgentMutationPlan Plan { get; }
        public AgentCompileReport Report { get; }
        public bool IsApply { get; }
        public AgentPresentationMutationPlan PresentationPlan { get; }
        public AgentAssetResolver Resolver { get; }
        public IReadOnlyCollection<UnityEngine.Object> TouchedOwners => m_TouchedOwners;

        public bool Initialize()
        {
            if (!Definition)
            {
                Report.Error("definition", "missing_definition", $"{Domain} root definition 缺失。");
                return false;
            }
            if (Current == null || Plan == null)
            {
                Report.Error("document", "mutation_boundary_missing", "Current Document或Mutation Plan缺失。");
                return false;
            }
            string definitionPath = AssetDatabase.GetAssetPath(Definition);
            string definitionIdentity = AssetDatabase.AssetPathToGUID(definitionPath);
            if (!string.Equals(Current.domain, Domain, StringComparison.Ordinal) ||
                !string.Equals(Plan.Domain, Domain, StringComparison.Ordinal) ||
                !string.Equals(Current.rootIdentity, definitionIdentity, StringComparison.Ordinal) ||
                !string.Equals(Plan.RootIdentity, definitionIdentity, StringComparison.Ordinal) ||
                !string.Equals(Plan.SourceRevision, SourceRevision, StringComparison.Ordinal))
            {
                Report.Error(
                    "document",
                    "document_source_changed",
                    $"Current Document与Mutation Plan的source不一致：definitionRoot={definitionPath}, currentIdentity={Current.rootIdentity}, planIdentity={Plan.RootIdentity}, currentRevision={SourceRevision}, planRevision={Plan.SourceRevision}。");
                return false;
            }
            return true;
        }

        public void AddPlanned(AgentMutation command, string target, string detail)
        {
            Report.plannedDiff.Add(new AgentCompileDiffEntry
            {
                mutationId = command.Id,
                action = command.OperationName,
                graph = command.OwnerScope,
                target = target ?? string.Empty,
                detail = detail ?? string.Empty
            });
        }

        public void AddAppliedAuthoring(
            AgentMutation command,
            UnityEngine.Object owner,
            object value,
            string target,
            string detail)
        {
            if (owner != null)
                m_TouchedOwners.Add(owner);
            Report.appliedDiff.Add(new AgentCompileDiffEntry
            {
                mutationId = command.Id,
                action = command.OperationName,
                graph = command.OwnerScope,
                target = target ?? string.Empty,
                detail = detail ?? string.Empty
            });
        }

        public void Touch(UnityEngine.Object owner)
        {
            if (owner != null)
                m_TouchedOwners.Add(owner);
        }

        internal void RegisterRollback(Action rollback)
        {
            if (rollback != null)
                m_RollbackActions.Add(rollback);
        }

        internal void RollbackAuthoring()
        {
            List<Exception> failures = null;
            for (int i = m_RollbackActions.Count - 1; i >= 0; i--)
            {
                try
                {
                    m_RollbackActions[i]();
                }
                catch (Exception exception)
                {
                    failures ??= new List<Exception>();
                    failures.Add(exception);
                }
            }
            if (failures != null)
                throw new AggregateException("技能authoring回滚动作未全部成功。", failures);
        }

        public void PlanGameplayTag(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag))
                m_PlannedGameplayTags.Add(tag);
        }

        public bool IsGameplayTagPlanned(string tag)
        {
            return !string.IsNullOrWhiteSpace(tag) && m_PlannedGameplayTags.Contains(tag);
        }
    }

    public sealed class AgentDocumentPreparation
    {
        internal AgentDocumentPreparation(
            AgentMutationPlan plan,
            AgentAuthoringTarget current,
            string sourceRevision,
            AgentDocumentBoundaryIdentity boundary,
            AgentCompileReport report,
            AgentPresentationMutationPlan presentationPlan = null)
        {
            Plan = plan;
            Current = current;
            SourceRevision = sourceRevision ?? string.Empty;
            Boundary = boundary;
            Report = report;
            PresentationPlan = presentationPlan;
        }

        public AgentMutationPlan Plan { get; }
        public AgentCompileReport Report { get; }
        internal AgentAuthoringTarget Current { get; }
        internal string SourceRevision { get; }
        internal AgentDocumentBoundaryIdentity Boundary { get; }
        public AgentPresentationMutationPlan PresentationPlan { get; }
        public bool IsValid => Plan != null && Report != null && !Report.HasErrors();
    }

    public sealed class AgentDocumentBoundaryIdentity
    {
        readonly Dictionary<string, string> m_SkillGraphs;

        AgentDocumentBoundaryIdentity(
            UnityEngine.Object definition,
            string definitionPath,
            string definitionGuid,
            IDictionary<string, string> skillGraphs)
        {
            Definition = definition;
            DefinitionPath = definitionPath;
            DefinitionGuid = definitionGuid;
            m_SkillGraphs = new Dictionary<string, string>(skillGraphs, StringComparer.Ordinal);
        }

        UnityEngine.Object Definition { get; }
        string DefinitionPath { get; }
        string DefinitionGuid { get; }

        public static AgentDocumentBoundaryIdentity Capture(AgentMutationSession session)
        {
            var skillGraphs = new Dictionary<string, string>(StringComparer.Ordinal);
            if (session.Definition)
            {
                var native = new BtsmtlSkillGraphClosureIndex();
                native.Build(session.Definition);
                var fingerprint = new BtsmtlSkillGraphFingerprint();
                foreach (KeyValuePair<string, FlowGraph> pair in native.Graphs)
                    skillGraphs[pair.Key] = fingerprint.Compute(pair.Value);
            }
            string definitionPath = AssetDatabase.GetAssetPath(session.Definition);
            return new AgentDocumentBoundaryIdentity(
                session.Definition,
                definitionPath,
                AssetDatabase.AssetPathToGUID(definitionPath),
                skillGraphs);
        }

        public bool Validate(
            UnityEngine.Object definition,
            AgentMutationSession session,
            AgentCompileReport report)
        {
            string definitionPath = AssetDatabase.GetAssetPath(definition);
            if (definition != Definition ||
                !string.Equals(definitionPath, DefinitionPath, StringComparison.Ordinal) ||
                !string.Equals(AssetDatabase.AssetPathToGUID(definitionPath), DefinitionGuid, StringComparison.Ordinal))
            {
                report.Error(
                    "transaction",
                    "authoring_source_changed",
                    "Character Definition identity在dry-run与apply之间发生变化。");
                return false;
            }
            try
            {
                var native = new BtsmtlSkillGraphClosureIndex();
                native.Build(session.Definition);
                if (native.Graphs.Count != m_SkillGraphs.Count)
                {
                    report.Error("transaction", "authoring_skill_graph_changed", "Skill Graph闭包在dry-run与apply之间发生变化。");
                    return false;
                }
                var fingerprint = new BtsmtlSkillGraphFingerprint();
                foreach (KeyValuePair<string, string> pair in m_SkillGraphs)
                {
                    if (!native.Graphs.TryGetValue(pair.Key, out FlowGraph graph) ||
                        !string.Equals(fingerprint.Compute(graph), pair.Value, StringComparison.Ordinal))
                    {
                        report.Error("transaction", "authoring_skill_graph_changed", $"Skill Graph identity在dry-run与apply之间发生变化：{pair.Key}");
                        return false;
                    }
                }
            }
            catch (Exception exception)
            {
                report.Error("transaction", "authoring_skill_graph_changed", exception.Message);
                return false;
            }
            return true;
        }
    }
}
