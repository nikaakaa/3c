using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Motion;
using TreeDesigner;
using TreeDesigner.Editor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentDocumentReconciler
    {
        public AgentDocumentPreparation Prepare(
            CharacterPipelineDefinition definition,
            AgentGraphSnapshot current,
            AgentAuthoringTarget target)
        {
            AgentMutationDraftSet mutations = CreateMutationSet(current, target);
            var report = ValidateEnvelope(current, target);
            if (!report.HasErrors())
                BuildCharacterMutations(current, target.editable, mutations, report);
            AgentPresentationMutationPlan presentationPlan = null;
            if (!report.HasErrors())
            {
                new AgentAuthoringPresentationReconciler().TryCreatePlan(
                    definition,
                    target.editable,
                    target.context,
                    report,
                    out presentationPlan);
            }
            if (report.HasErrors())
                return new AgentDocumentPreparation(null, current, null, report);
            AgentDocumentPreparation preparation =
                new AgentDocumentMutationCompiler().Prepare(
                    definition,
                    current,
                    mutations);
            preparation.Report.messages.AddRange(report.messages);
            preparation.Report.plannedDiff.AddRange(report.plannedDiff);
            preparation.Report.metrics.diffSize =
                preparation.Report.plannedDiff.Count;
            preparation.Report.success = !preparation.Report.HasErrors();
            return new AgentDocumentPreparation(
                preparation.Plan,
                preparation.Snapshot,
                preparation.Boundary,
                preparation.Report,
                presentationPlan);
        }

        static AgentMutationDraftSet CreateMutationSet(AgentGraphSnapshot current, AgentAuthoringTarget target)
        {
            return new AgentMutationDraftSet
            {
                schemaVersion = AgentAuthoringSchema.Version,
                domain = target?.domain,
                rootIdentity = target?.rootIdentity,
                sourceRevision = current?.sourceRevision,
                mutations = new List<AgentMutationDraft>()
            };
        }

        static AgentCompileReport ValidateEnvelope(AgentGraphSnapshot current, AgentAuthoringTarget target)
        {
            var report = new AgentCompileReport
            {
                success = true,
                domain = current?.domain ?? string.Empty,
                rootIdentity = current?.rootIdentity ?? string.Empty
            };
            if (target == null)
            {
                report.Error("document", "document_missing", "Agent Authoring Document缺失。");
                return report;
            }
            if (!string.Equals(target.domain, current.domain, StringComparison.Ordinal))
                report.Error("document.domain", "document_domain_mismatch", "Document domain与当前root不一致。");
            if (!string.Equals(target.rootIdentity, current.rootIdentity, StringComparison.Ordinal))
                report.Error("document.rootIdentity", "document_root_mismatch", "Document rootIdentity与当前root不一致。");
            if (target.editable == null)
                report.Error("document.editable", "editable_missing", "Document editable正文缺失。");
            else if (target.editable.blackboardSchemaRevision != PipelineBlackboardAuthoringSchema.CurrentRevision)
                report.Error(
                    "document.editable.blackboardSchemaRevision",
                    "blackboard_schema_revision_outdated",
                    $"Blackboard schema revision必须是{PipelineBlackboardAuthoringSchema.CurrentRevision}；请重新checkout Document后再apply。");
            if (string.Equals(
                    target.domain,
                    AgentAuthoringSchema.CharacterControllerDomain,
                    StringComparison.Ordinal) &&
                target.editable != null)
                AgentControlDocumentMapper.Validate(target.editable.control, report);
            return report;
        }

        static void BuildCharacterMutations(
            AgentGraphSnapshot current,
            AgentDocumentEditable target,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            AgentGraphDocumentMutationModule.BuildStateMachineCreationMutations(
                current.stateMachines,
                target.stateMachines,
                target.graphs,
                mutations,
                report);
            AgentGraphDocumentMutationModule.BuildGraphCreationMutations(
                current.graphs,
                target.graphs,
                mutations,
                report);
            bool normalizeBlackboard =
                current.blackboardSchemaRevision != target.blackboardSchemaRevision;
            AgentBlackboardDocumentMutationModule.BuildCharacterBlackboardMutations(
                current.blackboardDeclarations,
                target.blackboardDeclarations,
                target.graphs,
                mutations,
                report,
                normalizeBlackboard);
            AgentBlackboardDocumentMutationModule.BuildBlackboardSchemaRevisionMutation(current, target, mutations, normalizeBlackboard);
            AgentGraphDocumentMutationModule.BuildStateMachineMutations(
                current.rootGraphAuthoringId,
                current.stateMachines,
                target.stateMachines,
                current.graphs,
                target.graphs,
                mutations,
                report,
                false);
            AgentGraphDocumentMutationModule.BuildCharacterGraphMutations(current.graphs, target.graphs, target.stateMachines, mutations, report);
            AgentTimelineDocumentMutationModule.BuildTimelineMutations(current, target, mutations, report);
            AgentActionDocumentMutationModule.BuildActionMutations(current, target, mutations, report);
            AgentSkillFlowDocumentMutationModule.Build(
                current,
                target,
                mutations,
                report);
            AgentControlDocumentMutationModule.BuildControlConfigurationMutations(current, target, mutations, report);
        }





    }
}
