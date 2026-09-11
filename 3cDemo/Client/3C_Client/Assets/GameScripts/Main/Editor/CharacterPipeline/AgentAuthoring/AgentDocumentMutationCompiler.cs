using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentDocumentMutationCompiler
    {
        readonly AgentMutationPlanner m_Planner = new AgentMutationPlanner();
        readonly AgentMutationHandlerCatalog m_Handlers = new AgentMutationHandlerCatalog();

        public AgentDocumentPreparation Prepare(
            CharacterPipelineDefinition definition,
            AgentAuthoringTarget current,
            string sourceRevision,
            AgentMutationDraftSet drafts)
        {
            var report = new AgentCompileReport
            {
                success = true,
                applied = false
            };
            if (!m_Planner.TryCreatePlan(drafts, report, out AgentMutationPlan plan))
                return new AgentDocumentPreparation(null, current, sourceRevision, null, report);
            var session = new AgentMutationSession(definition, current, sourceRevision, plan, report, false);
            if (!session.Initialize())
                return new AgentDocumentPreparation(plan, current, sourceRevision, null, report);

            for (int i = 0; i < plan.Commands.Count; i++)
            {
                AgentMutation command = plan.Commands[i];
                bool valid = m_Handlers.Get(command.Kind).Preflight(session, command);
                if (!valid && !report.HasErrors())
                    report.Error(command.Path, "mutation_preflight_failed", "typed Mutation preflight未通过。");
            }

            report.metrics.diffSize = report.plannedDiff.Count;
            report.success = !report.HasErrors();
            AgentDocumentBoundaryIdentity boundary = report.HasErrors()
                ? null
                : AgentDocumentBoundaryIdentity.Capture(session);
            return new AgentDocumentPreparation(plan, current, sourceRevision, boundary, report);
        }

        public AgentDocumentApplyResult Apply(
            CharacterPipelineDefinition definition,
            AgentDocumentPreparation preparation)
        {
            var report = new AgentCompileReport
            {
                success = true,
                applied = false
            };
            if (preparation == null || !preparation.IsValid)
            {
                report.Error("document.editable", "document_not_prepared", "Document必须先完成无错误的Mutation Plan preflight。");
                return new AgentDocumentApplyResult(report, Array.Empty<UnityEngine.Object>(), null);
            }

            CopyPreparationReport(preparation.Report, report);
            var session = new AgentMutationSession(
                definition,
                preparation.Current,
                preparation.SourceRevision,
                preparation.Plan,
                report,
                true,
                preparation.PresentationPlan);
            if (!session.Initialize() || !preparation.Boundary.Validate(definition, session, report))
                return new AgentDocumentApplyResult(report, session.TouchedOwners.ToArray(), session.RollbackAuthoring);

            try
            {
                for (int i = 0; i < preparation.Plan.Commands.Count; i++)
                {
                    AgentMutation command = preparation.Plan.Commands[i];
                    m_Handlers.Get(command.Kind).Apply(session, command);
                    if (report.HasErrors())
                        break;
                }
            }
            catch (Exception exception)
            {
                report.Error("apply", "apply_exception", exception.ToString());
            }

            report.metrics.diffSize = report.appliedDiff.Count;
            report.applied = !report.HasErrors();
            report.success = !report.HasErrors();
            return new AgentDocumentApplyResult(report, session.TouchedOwners.ToArray(), session.RollbackAuthoring);
        }

        static void CopyPreparationReport(AgentCompileReport source, AgentCompileReport target)
        {
            target.plannedDiff.AddRange(source.plannedDiff);
            target.messages.AddRange(source.messages);
            target.metrics.schemaValidCount = source.metrics.schemaValidCount;
            target.metrics.schemaInvalidCount = source.metrics.schemaInvalidCount;
            target.metrics.compileSuccessCount = source.metrics.compileSuccessCount;
            target.metrics.compileFailureCount = source.metrics.compileFailureCount;
            target.metrics.semanticValidCount = source.metrics.semanticValidCount;
            target.metrics.semanticInvalidCount = source.metrics.semanticInvalidCount;
            target.metrics.assetResolvedCount = source.metrics.assetResolvedCount;
            target.metrics.assetResolveFailureCount = source.metrics.assetResolveFailureCount;
            target.metrics.businessCoverageCount = source.metrics.businessCoverageCount;
            target.metrics.businessCoverageMissingCount = source.metrics.businessCoverageMissingCount;
        }
    }

    public sealed class AgentDocumentApplyResult
    {
        readonly UnityEngine.Object[] m_TouchedOwners;
        readonly Action m_RollbackAuthoring;

        public AgentDocumentApplyResult(
            AgentCompileReport report,
            UnityEngine.Object[] touchedOwners,
            Action rollbackAuthoring)
        {
            Report = report;
            m_TouchedOwners = touchedOwners ?? Array.Empty<UnityEngine.Object>();
            m_RollbackAuthoring = rollbackAuthoring;
        }

        public AgentCompileReport Report { get; }
        public IReadOnlyList<UnityEngine.Object> TouchedOwners => m_TouchedOwners;
        public void RollbackAuthoring() => m_RollbackAuthoring?.Invoke();
    }
}
