using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.AI;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentPatchCompiler
    {
        readonly AgentPatchCommandLowerer m_Lowerer = new AgentPatchCommandLowerer();
        readonly AgentPatchCommandHandlerCatalog m_Handlers = new AgentPatchCommandHandlerCatalog();

        public AgentPatchPreparation Prepare(
            CharacterPipelineDefinition definition,
            AgentGraphSnapshot snapshot,
            AgentPatchIR patch)
        {
            var report = new AgentCompileReport
            {
                success = true,
                applied = false
            };
            if (!m_Lowerer.TryLower(patch, report, out AgentPatchCommandPlan plan))
                return new AgentPatchPreparation(null, snapshot, null, report);

            var session = new AgentPatchCompileSession(definition, snapshot, plan, report, false);
            if (!session.Initialize())
                return new AgentPatchPreparation(plan, snapshot, null, report);

            for (int i = 0; i < plan.Commands.Count; i++)
            {
                AgentPatchCommand command = plan.Commands[i];
                m_Handlers.Get(command.Kind).Preflight(session, command);
            }

            report.metrics.diffSize = report.plannedDiff.Count;
            report.success = !report.HasErrors();
            AgentPatchBoundaryIdentity boundary = report.HasErrors()
                ? null
                : AgentPatchBoundaryIdentity.Capture(session);
            return new AgentPatchPreparation(plan, snapshot, boundary, report);
        }

        public AgentPatchApplyResult Apply(
            CharacterPipelineDefinition definition,
            AgentPatchPreparation preparation)
        {
            var report = new AgentCompileReport
            {
                success = true,
                applied = false
            };
            if (preparation == null || !preparation.IsValid)
            {
                report.Error("patch", "patch_not_prepared", "Patch 必须先完成无错误的 typed plan preflight。");
                return new AgentPatchApplyResult(report, Array.Empty<UnityEngine.Object>());
            }

            CopyPreparationReport(preparation.Report, report);
            var session = new AgentPatchCompileSession(
                definition,
                preparation.Snapshot,
                preparation.Plan,
                report,
                true);
            if (!session.Initialize() || !preparation.Boundary.Validate(definition, session, report))
                return new AgentPatchApplyResult(report, session.TouchedOwners.ToArray());

            for (int i = 0; i < preparation.Plan.Commands.Count; i++)
            {
                AgentPatchCommand command = preparation.Plan.Commands[i];
                m_Handlers.Get(command.Kind).Apply(session, command);
                if (report.HasErrors())
                    break;
            }

            report.metrics.diffSize = report.appliedDiff.Count;
            report.applied = !report.HasErrors();
            report.success = !report.HasErrors();
            return new AgentPatchApplyResult(report, session.TouchedOwners.ToArray());
        }

        public AgentPatchPreparation Prepare(
            AIControllerDefinition definition,
            AgentGraphSnapshot snapshot,
            AgentPatchIR patch)
        {
            var report = new AgentCompileReport
            {
                success = true,
                applied = false,
                domain = AgentAuthoringSchema.AIControllerDomain,
                rootIdentity = definition ? definition.ControllerId : string.Empty
            };
            if (!m_Lowerer.TryLower(patch, report, out AgentPatchCommandPlan plan))
                return new AgentPatchPreparation(null, snapshot, null, report);

            var session = new AgentPatchCompileSession(definition, snapshot, plan, report, false);
            if (!session.Initialize())
                return new AgentPatchPreparation(plan, snapshot, null, report);

            for (int i = 0; i < plan.Commands.Count; i++)
                m_Handlers.Get(plan.Commands[i].Kind).Preflight(session, plan.Commands[i]);

            report.metrics.diffSize = report.plannedDiff.Count;
            report.success = !report.HasErrors();
            AgentPatchBoundaryIdentity boundary = report.HasErrors() ? null : AgentPatchBoundaryIdentity.Capture(session);
            return new AgentPatchPreparation(plan, snapshot, boundary, report);
        }

        public AgentPatchApplyResult Apply(
            AIControllerDefinition definition,
            AgentPatchPreparation preparation)
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
                report.Error("patch", "patch_not_prepared", "Patch 必须先完成无错误的 typed plan preflight。");
                return new AgentPatchApplyResult(report, Array.Empty<UnityEngine.Object>());
            }

            CopyPreparationReport(preparation.Report, report);
            var session = new AgentPatchCompileSession(definition, preparation.Snapshot, preparation.Plan, report, true);
            if (!session.Initialize() || !preparation.Boundary.Validate(definition, session, report))
                return new AgentPatchApplyResult(report, session.TouchedOwners.ToArray());

            for (int i = 0; i < preparation.Plan.Commands.Count; i++)
            {
                AgentPatchCommand command = preparation.Plan.Commands[i];
                m_Handlers.Get(command.Kind).Apply(session, command);
                if (report.HasErrors())
                    break;
            }

            report.metrics.diffSize = report.appliedDiff.Count;
            report.applied = !report.HasErrors();
            report.success = !report.HasErrors();
            return new AgentPatchApplyResult(report, session.TouchedOwners.ToArray());
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

    public sealed class AgentPatchApplyResult
    {
        readonly UnityEngine.Object[] m_TouchedOwners;

        public AgentPatchApplyResult(AgentCompileReport report, UnityEngine.Object[] touchedOwners)
        {
            Report = report;
            m_TouchedOwners = touchedOwners ?? Array.Empty<UnityEngine.Object>();
        }

        public AgentCompileReport Report { get; }
        public IReadOnlyList<UnityEngine.Object> TouchedOwners => m_TouchedOwners;
    }
}
