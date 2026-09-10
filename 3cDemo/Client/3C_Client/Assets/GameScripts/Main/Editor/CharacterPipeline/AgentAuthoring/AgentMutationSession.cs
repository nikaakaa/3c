using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using ThirdPersonCharacter.Control.Authoring;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentMutationSession
    {
        readonly HashSet<UnityEngine.Object> m_TouchedOwners = new HashSet<UnityEngine.Object>();
        readonly HashSet<string> m_PlannedGameplayTags = new HashSet<string>(StringComparer.Ordinal);

        public AgentMutationSession(
            CharacterPipelineDefinition definition,
            AgentGraphSnapshot snapshot,
            AgentMutationPlan plan,
            AgentCompileReport report,
            bool apply,
            AgentPresentationMutationPlan presentationPlan = null)
        {
            Definition = definition;
            Domain = AgentAuthoringSchema.CharacterControllerDomain;
            Snapshot = snapshot;
            Plan = plan;
            Report = report;
            IsApply = apply;
            PresentationPlan = presentationPlan;
            Resolver = new AgentAssetResolver(definition, snapshot);
        }

        public CharacterPipelineDefinition Definition { get; }
        public string Domain { get; }
        public AgentGraphSnapshot Snapshot { get; }
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
            if (Snapshot == null || Plan == null)
            {
                Report.Error("document", "mutation_boundary_missing", "Snapshot或Mutation Plan缺失。");
                return false;
            }
            if (!string.Equals(Snapshot.schemaVersion, AgentAuthoringSchema.Version, StringComparison.Ordinal))
            {
                Report.Error(
                    "snapshot.schemaVersion",
                    "unsupported_schema_version",
                    $"Snapshot schema 必须是 {AgentAuthoringSchema.Version}，当前为 {Snapshot.schemaVersion}。");
                return false;
            }
            string definitionPath = AssetDatabase.GetAssetPath(Definition);
            string definitionIdentity = AssetDatabase.AssetPathToGUID(definitionPath);
            if (!string.Equals(Snapshot.domain, Domain, StringComparison.Ordinal) ||
                !string.Equals(Plan.Domain, Domain, StringComparison.Ordinal) ||
                !string.Equals(Snapshot.rootAssetPath, definitionPath, StringComparison.Ordinal) ||
                !string.Equals(Snapshot.rootIdentity, definitionIdentity, StringComparison.Ordinal) ||
                !string.Equals(Plan.RootIdentity, definitionIdentity, StringComparison.Ordinal) ||
                !string.Equals(Plan.SourceRevision, Snapshot.sourceRevision, StringComparison.Ordinal))
            {
                Report.Error(
                    "snapshot",
                    "snapshot_source_changed",
                    $"Snapshot与Mutation Plan的source不一致：snapshotRoot={Snapshot.rootAssetPath}, definitionRoot={definitionPath}, snapshotIdentity={Snapshot.rootIdentity}, planIdentity={Plan.RootIdentity}, snapshotRevision={Snapshot.sourceRevision}, planRevision={Plan.SourceRevision}。");
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
            AgentGraphSnapshot snapshot,
            AgentDocumentBoundaryIdentity boundary,
            AgentCompileReport report,
            AgentPresentationMutationPlan presentationPlan = null)
        {
            Plan = plan;
            Snapshot = snapshot;
            Boundary = boundary;
            Report = report;
            PresentationPlan = presentationPlan;
        }

        public AgentMutationPlan Plan { get; }
        public AgentCompileReport Report { get; }
        internal AgentGraphSnapshot Snapshot { get; }
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
                var native = new AgentSkillFlowDocumentRuntimeIndex();
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
                var native = new AgentSkillFlowDocumentRuntimeIndex();
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
