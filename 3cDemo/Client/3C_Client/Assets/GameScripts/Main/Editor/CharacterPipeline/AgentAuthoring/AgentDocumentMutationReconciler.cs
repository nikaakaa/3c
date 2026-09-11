using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Motion;
using UnityEngine;

using ThirdPersonCharacter.Pipeline.Editor.Authoring.Presentation;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.Skill;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentDocumentReconciler
    {
        public AgentDocumentPreparation Prepare(
            CharacterPipelineDefinition definition,
            AgentAuthoringPackageProjection current,
            AgentAuthoringTarget target)
        {
            var report = ValidateEnvelope(current, target);
            AgentMutationPlanBuilder mutations = new AgentMutationPlanBuilder(
                report,
                target?.domain,
                target?.rootIdentity,
                current?.SourceRevision);
            if (!report.HasErrors())
                BuildCharacterMutations(current?.Target, target.editable, mutations, report);
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
                return new AgentDocumentPreparation(null, current?.Target, current?.SourceRevision, null, report);
            AgentMutationPlan plan = mutations.Build();
            if (plan == null)
                return new AgentDocumentPreparation(null, current?.Target, current?.SourceRevision, null, report);
            AgentDocumentPreparation preparation =
                new AgentDocumentMutationCompiler().Prepare(
                    definition,
                    current?.Target,
                    current?.SourceRevision,
                    plan);
            preparation.Report.messages.AddRange(report.messages);
            preparation.Report.plannedDiff.AddRange(report.plannedDiff);
            preparation.Report.metrics.diffSize =
                preparation.Report.plannedDiff.Count;
            preparation.Report.success = !preparation.Report.HasErrors();
            return new AgentDocumentPreparation(
                preparation.Plan,
                preparation.Current,
                preparation.SourceRevision,
                preparation.Boundary,
                preparation.Report,
                presentationPlan);
        }

        static AgentCompileReport ValidateEnvelope(AgentAuthoringPackageProjection current, AgentAuthoringTarget target)
        {
            var report = new AgentCompileReport
            {
                success = true,
                domain = current?.Target?.domain ?? string.Empty,
                rootIdentity = current?.Target?.rootIdentity ?? string.Empty
            };
            if (target == null)
            {
                report.Error("document", "document_missing", "Agent Authoring Document缺失。");
                return report;
            }
            if (!string.Equals(target.domain, current?.Target?.domain, StringComparison.Ordinal))
                report.Error("document.domain", "document_domain_mismatch", "Document domain与当前root不一致。");
            if (!string.Equals(target.rootIdentity, current?.Target?.rootIdentity, StringComparison.Ordinal))
                report.Error("document.rootIdentity", "document_root_mismatch", "Document rootIdentity与当前root不一致。");
            if (target.editable == null)
                report.Error("document.editable", "editable_missing", "Document editable正文缺失。");
            if (string.Equals(
                    target.domain,
                    AgentAuthoringSchema.CharacterControllerDomain,
                    StringComparison.Ordinal) &&
                target.editable != null)
                AgentControlDocumentMapper.Validate(target.editable.control, report);
            return report;
        }

        static void BuildCharacterMutations(
            AgentAuthoringTarget current,
            AgentDocumentEditable target,
            AgentMutationPlanBuilder mutations,
            AgentCompileReport report)
        {
            AgentActionDocumentMutationModule.BuildActionMutations(current, target, mutations, report);
            BtsmtlSkillDocumentDiffModule.Build(
                current,
                target,
                mutations,
                report);
            AgentControlDocumentMutationModule.BuildControlConfigurationMutations(current, target, mutations, report);
        }





    }
}
