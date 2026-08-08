using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.AI;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentDocumentMutationCompiler
    {
        readonly AgentMutationPlanner m_Planner = new AgentMutationPlanner();
        readonly AgentMutationHandlerCatalog m_Handlers = new AgentMutationHandlerCatalog();

        public AgentDocumentPreparation Prepare(
            CharacterPipelineDefinition definition,
            AgentGraphSnapshot snapshot,
            AgentMutationDraftSet drafts)
        {
            var report = new AgentCompileReport
            {
                success = true,
                applied = false
            };
            if (!m_Planner.TryCreatePlan(drafts, report, out AgentMutationPlan plan))
                return new AgentDocumentPreparation(null, snapshot, null, report);
            if (!AgentMutationPortShapePreflight.Validate(snapshot, plan, report))
                return new AgentDocumentPreparation(plan, snapshot, null, report);

            var session = new AgentMutationSession(definition, snapshot, plan, report, false);
            if (!session.Initialize())
                return new AgentDocumentPreparation(plan, snapshot, null, report);

            for (int i = 0; i < plan.Commands.Count; i++)
            {
                AgentMutation command = plan.Commands[i];
                m_Handlers.Get(command.Kind).Preflight(session, command);
            }

            report.metrics.diffSize = report.plannedDiff.Count;
            report.success = !report.HasErrors();
            AgentDocumentBoundaryIdentity boundary = report.HasErrors()
                ? null
                : AgentDocumentBoundaryIdentity.Capture(session);
            return new AgentDocumentPreparation(plan, snapshot, boundary, report);
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
                return new AgentDocumentApplyResult(report, Array.Empty<UnityEngine.Object>());
            }

            CopyPreparationReport(preparation.Report, report);
            var session = new AgentMutationSession(
                definition,
                preparation.Snapshot,
                preparation.Plan,
                report,
                true);
            if (!session.Initialize() || !preparation.Boundary.Validate(definition, session, report))
                return new AgentDocumentApplyResult(report, session.TouchedOwners.ToArray());

            for (int i = 0; i < preparation.Plan.Commands.Count; i++)
            {
                AgentMutation command = preparation.Plan.Commands[i];
                m_Handlers.Get(command.Kind).Apply(session, command);
                if (report.HasErrors())
                    break;
            }

            report.metrics.diffSize = report.appliedDiff.Count;
            report.applied = !report.HasErrors();
            report.success = !report.HasErrors();
            return new AgentDocumentApplyResult(report, session.TouchedOwners.ToArray());
        }

        public AgentDocumentPreparation Prepare(
            AIControllerDefinition definition,
            AgentGraphSnapshot snapshot,
            AgentMutationDraftSet drafts)
        {
            var report = new AgentCompileReport
            {
                success = true,
                applied = false,
                domain = AgentAuthoringSchema.AIControllerDomain,
                rootIdentity = definition ? definition.ControllerId : string.Empty
            };
            if (!m_Planner.TryCreatePlan(drafts, report, out AgentMutationPlan plan))
                return new AgentDocumentPreparation(null, snapshot, null, report);
            if (!AgentMutationPortShapePreflight.Validate(snapshot, plan, report))
                return new AgentDocumentPreparation(plan, snapshot, null, report);

            var session = new AgentMutationSession(definition, snapshot, plan, report, false);
            if (!session.Initialize())
                return new AgentDocumentPreparation(plan, snapshot, null, report);

            for (int i = 0; i < plan.Commands.Count; i++)
                m_Handlers.Get(plan.Commands[i].Kind).Preflight(session, plan.Commands[i]);

            report.metrics.diffSize = report.plannedDiff.Count;
            report.success = !report.HasErrors();
            AgentDocumentBoundaryIdentity boundary = report.HasErrors() ? null : AgentDocumentBoundaryIdentity.Capture(session);
            return new AgentDocumentPreparation(plan, snapshot, boundary, report);
        }

        public AgentDocumentApplyResult Apply(
            AIControllerDefinition definition,
            AgentDocumentPreparation preparation)
        {
            var report = new AgentCompileReport
            {
                success = true,
                applied = false,
                domain = AgentAuthoringSchema.AIControllerDomain,
                rootIdentity = definition ? definition.ControllerId : string.Empty
            };
            if (preparation == null || !preparation.IsValid)
            {
                report.Error("document.editable", "document_not_prepared", "Document必须先完成无错误的Mutation Plan preflight。");
                return new AgentDocumentApplyResult(report, Array.Empty<UnityEngine.Object>());
            }

            CopyPreparationReport(preparation.Report, report);
            var session = new AgentMutationSession(definition, preparation.Snapshot, preparation.Plan, report, true);
            if (!session.Initialize() || !preparation.Boundary.Validate(definition, session, report))
                return new AgentDocumentApplyResult(report, session.TouchedOwners.ToArray());

            for (int i = 0; i < preparation.Plan.Commands.Count; i++)
            {
                AgentMutation command = preparation.Plan.Commands[i];
                m_Handlers.Get(command.Kind).Apply(session, command);
                if (report.HasErrors())
                    break;
            }

            report.metrics.diffSize = report.appliedDiff.Count;
            report.applied = !report.HasErrors();
            report.success = !report.HasErrors();
            return new AgentDocumentApplyResult(report, session.TouchedOwners.ToArray());
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
            target.metrics.repairIterations = source.metrics.repairIterations;
            target.metrics.businessCoverageCount = source.metrics.businessCoverageCount;
            target.metrics.businessCoverageMissingCount = source.metrics.businessCoverageMissingCount;
        }
    }

    public sealed class AgentDocumentApplyResult
    {
        readonly UnityEngine.Object[] m_TouchedOwners;

        public AgentDocumentApplyResult(AgentCompileReport report, UnityEngine.Object[] touchedOwners)
        {
            Report = report;
            m_TouchedOwners = touchedOwners ?? Array.Empty<UnityEngine.Object>();
        }

        public AgentCompileReport Report { get; }
        public IReadOnlyList<UnityEngine.Object> TouchedOwners => m_TouchedOwners;
    }
}
