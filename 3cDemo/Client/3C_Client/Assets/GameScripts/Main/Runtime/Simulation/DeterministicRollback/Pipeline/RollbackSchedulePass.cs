using System;
using System.Collections.Generic;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public sealed class RollbackSchedulePassRuntimeFactory : IFixedPipelinePassRuntimeFactory
    {
        readonly SimulationPipelinePassFactoryDescriptor m_Descriptor;
        readonly DeterministicRollbackModelPolicy m_Policy;
        readonly RollbackRuntimeState m_State;

        public RollbackSchedulePassRuntimeFactory(
            SimulationPipelinePassFactoryDescriptor descriptor,
            DeterministicRollbackModelPolicy policy,
            RollbackRuntimeState state)
        {
            m_Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            m_Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            m_State = state ?? throw new ArgumentNullException(nameof(state));
        }

        public SimulationPipelinePassFactoryDescriptor Descriptor => m_Descriptor;

        public IFixedCompiledPipelinePassRuntime Create(FixedPipelinePassRuntimeFactoryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var reads = new RollbackScheduleReadPorts(
                context.Products.BindExclusiveReader<RollbackIngressBatch>(RollbackPipelineProducts.Ingress),
                context.BindTargetPort<IFixedProgramRuntimePort>(FixedPipelineRuntimePortIds.ProgramRuntime));
            var writes = new RollbackScheduleWritePorts(
                context.Products.BindExclusiveWriter<SimulationSessionExecutionPlan<FixedSimulationStep>>(
                    SimulationPipelineProducts.ExecutionPlan));
            return new FixedSchedulePassRuntimeAdapter<RollbackScheduleReadPorts, RollbackScheduleWritePorts>(
                new RollbackSchedulePassRuntime(context.Pass.Descriptor, m_Policy, m_State),
                reads,
                writes);
        }
    }

    public sealed class RollbackSchedulePassRuntime :
        FixedPipelinePassRuntimeBase,
        ISimulationExecutionPlanSchedulePassRuntime<RollbackScheduleReadPorts, RollbackScheduleWritePorts>
    {
        readonly DeterministicRollbackModelPolicy m_Policy;
        readonly RollbackRuntimeState m_State;

        public RollbackSchedulePassRuntime(
            SimulationPipelinePassDescriptor descriptor,
            DeterministicRollbackModelPolicy policy,
            RollbackRuntimeState state) : base(descriptor)
        {
            m_Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            m_State = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void Execute(
            SimulationPipelineScheduleContext context,
            RollbackScheduleReadPorts readPorts,
            RollbackScheduleWritePorts writePorts)
        {
            RequireExecution();
            RollbackIngressBatch ingress = readPorts.Ingress.Read();
            writePorts.ExecutionPlan.Write(BuildPlan(context, ingress, readPorts.ProgramRuntime));
        }

        SimulationSessionExecutionPlan<FixedSimulationStep> BuildPlan(
            SimulationPipelineScheduleContext context,
            RollbackIngressBatch ingress,
            IFixedProgramRuntimePort programRuntime)
        {
            ulong nextTick = checked(context.CurrentCompletedTick + 1);
            if (ingress.Predicted.Tick.Value != nextTick)
                throw new InvalidOperationException("Rollback ingress predicted Tick is not the next Simulation Tick.");
            var roster = new SimulationActorRosterDescriptor(CollectActors(programRuntime.Roster));
            if (context.CurrentCompletedTick == 0 &&
                m_State.Inputs.GetRequired(ingress.Predicted.Tick).Canonical == null)
            {
                return new SimulationSessionExecutionPlan<FixedSimulationStep>(
                    SimulationSessionExecutionPlanStatus.NoStep,
                    context.Source,
                    programRuntime.Catalog.CatalogHash,
                    context.Pipeline.Hash,
                    roster,
                    Array.Empty<SimulationPipelineStepSourceMapping>(),
                    null,
                    Array.Empty<FixedSimulationStep>(),
                    SimulationSessionPlanRequirement.WorkingState |
                    SimulationSessionPlanRequirement.OutputDisposition);
            }
            var steps = new List<FixedSimulationStep>();
            var mappings = new List<SimulationPipelineStepSourceMapping>();
            SimulationRestoreDirective restore = null;
            ulong planSequence = 1;
            SimulationTick replayStart = default;
            bool deepRecoveryReplay = false;
            if (m_State.TryGetPendingRecovery(out SimulationTick recoveryTick))
            {
                if (recoveryTick.Value > context.CurrentCompletedTick)
                    throw new InvalidOperationException("Rollback recovery snapshot is newer than the completed simulation horizon.");
                FixedSimulationSessionSnapshot snapshot = m_State.Snapshots.GetRequired(recoveryTick);
                restore = BuildRestoreDirective(recoveryTick, snapshot, programRuntime, context);
                m_State.MarkRecoveryScheduled(recoveryTick);
                if (recoveryTick.Value < context.CurrentCompletedTick)
                    replayStart = new SimulationTick(checked(recoveryTick.Value + 1));
            }
            else if (m_State.TryGetRequiredRecovery(out _, out _))
            {
                return BuildNoStep(context, programRuntime.Catalog.CatalogHash, roster);
            }
            else if (m_State.TryFindEarliestMismatch(out SimulationTick mismatch) &&
                mismatch.Value <= context.CurrentCompletedTick)
            {
                if (mismatch.Value == 1)
                {
                    m_State.RequireSnapshotRecovery(mismatch, "canonical-mismatch-without-tick-zero-snapshot");
                    return BuildNoStep(context, programRuntime.Catalog.CatalogHash, roster);
                }
                var restoreTick = new SimulationTick(mismatch.Value - 1);
                if (m_State.Snapshots.FloorTick == 0 || restoreTick.Value < m_State.Snapshots.FloorTick)
                {
                    m_State.RequireSnapshotRecovery(mismatch, "canonical-mismatch-before-snapshot-history-floor");
                    return BuildNoStep(context, programRuntime.Catalog.CatalogHash, roster);
                }
                FixedSimulationSessionSnapshot snapshot = m_State.Snapshots.GetRequired(restoreTick);
                int depth = checked((int)(context.CurrentCompletedTick - mismatch.Value + 1));
                if (depth > m_Policy.MaximumRollbackDepthTicks)
                    deepRecoveryReplay = true;
                restore = BuildRestoreDirective(restoreTick, snapshot, programRuntime, context);
                replayStart = mismatch;
            }
            if (replayStart.IsValid)
            {
                string replayClock = $"{context.Source.ClockId}/rollback-replay";
                mappings.Add(new SimulationPipelineStepSourceMapping(
                    replayClock,
                    context.Source.ClockId,
                    SimulationTickSourceKind.Replay));
                if (deepRecoveryReplay)
                    m_State.BeginDeepRecoveryReplay(replayStart, context.CurrentCompletedTick);
                else
                    m_State.BeginRollback(replayStart, context.CurrentCompletedTick);
                for (ulong tick = replayStart.Value; tick <= context.CurrentCompletedTick; tick++)
                {
                    RollbackCanonicalInputBundle bundle = SelectBundle(new SimulationTick(tick));
                    var source = new SimulationTickSourceIdentity(SimulationTickSourceKind.Replay, replayClock, tick);
                    steps.Add(BuildStep(bundle, source, SimulationPipelineStepExecutionKind.Replay, planSequence++, Array.Empty<SimulationPipelineTypedIngress<SimulationIngress>>()));
                    if (tick == ulong.MaxValue)
                        break;
                }
            }
            bool canAdvancePrediction = nextTick <= checked(
                m_State.LastCanonicalContiguousTick + (ulong)m_Policy.MaximumPredictionLeadTicks);
            if (!canAdvancePrediction && steps.Count == 0 && restore == null)
            {
                m_State.RecordPacedNoStep();
                return BuildNoStep(context, programRuntime.Catalog.CatalogHash, roster);
            }
            if (canAdvancePrediction || steps.Count == 0)
            {
                RollbackCanonicalInputBundle current = SelectBundle(new SimulationTick(nextTick));
                mappings.Add(new SimulationPipelineStepSourceMapping(
                    context.Source.ClockId,
                    context.Source.ClockId,
                    context.Source.Kind));
                steps.Add(BuildStep(
                    current,
                    new SimulationTickSourceIdentity(context.Source.Kind, context.Source.ClockId, nextTick),
                    restore == null ? SimulationPipelineStepExecutionKind.Forward : SimulationPipelineStepExecutionKind.Current,
                    planSequence,
                    ingress.TypedIngress.Ingress));
            }
            return new SimulationSessionExecutionPlan<FixedSimulationStep>(
                SimulationSessionExecutionPlanStatus.Executable,
                context.Source,
                programRuntime.Catalog.CatalogHash,
                context.Pipeline.Hash,
                roster,
                mappings,
                restore,
                steps,
                SimulationSessionPlanRequirement.WorkingState |
                SimulationSessionPlanRequirement.Snapshot |
                SimulationSessionPlanRequirement.StateHash |
                SimulationSessionPlanRequirement.OutputDisposition);
        }

        static SimulationSessionExecutionPlan<FixedSimulationStep> BuildNoStep(
            SimulationPipelineScheduleContext context,
            ProgramCatalogHash catalogHash,
            SimulationActorRosterDescriptor roster)
        {
            return new SimulationSessionExecutionPlan<FixedSimulationStep>(
                SimulationSessionExecutionPlanStatus.NoStep,
                context.Source,
                catalogHash,
                context.Pipeline.Hash,
                roster,
                Array.Empty<SimulationPipelineStepSourceMapping>(),
                null,
                Array.Empty<FixedSimulationStep>(),
                SimulationSessionPlanRequirement.WorkingState |
                SimulationSessionPlanRequirement.OutputDisposition);
        }

        static SimulationRestoreDirective BuildRestoreDirective(
            SimulationTick restoreTick,
            FixedSimulationSessionSnapshot snapshot,
            IFixedProgramRuntimePort programRuntime,
            SimulationPipelineScheduleContext context)
        {
            return new SimulationRestoreDirective(
                $"deterministic-rollback:{restoreTick.Value}",
                restoreTick,
                programRuntime.Catalog.CatalogHash,
                context.Pipeline.Hash,
                FixedPassExecutionBackend.BackendId,
                FixedPassExecutionBackend.SemanticVersion,
                snapshot.SnapshotHash);
        }

        RollbackCanonicalInputBundle SelectBundle(SimulationTick tick)
        {
            RollbackInputHistoryEntry entry = m_State.Inputs.GetRequired(tick);
            return entry.Canonical ?? entry.Predicted ??
                throw new InvalidOperationException($"Rollback Tick '{tick}' has neither canonical nor predicted input.");
        }

        static FixedSimulationStep BuildStep(
            RollbackCanonicalInputBundle bundle,
            SimulationTickSourceIdentity source,
            SimulationPipelineStepExecutionKind kind,
            ulong planSequence,
            IReadOnlyList<SimulationPipelineTypedIngress<SimulationIngress>> typedIngress)
        {
            var inputs = new SimulationPipelineActorInput<FixedStepInput>[bundle.Actors.Count];
            for (int i = 0; i < bundle.Actors.Count; i++)
            {
                RollbackActorInputFrame actor = bundle.Actors[i];
                var input = new CharacterSimulationInput(
                    FixedSimulationNumericProfile.Value,
                    source,
                    actor.Input.InputSourceIdentity,
                    actor.Input.Sequence,
                    actor.Input.Values,
                    actor.Input.Requests);
                inputs[i] = new SimulationPipelineActorInput<FixedStepInput>(
                    actor.ActorId,
                    input.Sequence,
                    new FixedStepInput(input));
            }
            return new FixedSimulationStep(
                bundle.Tick,
                new SimulationPipelineStepProvenance(kind, source, planSequence, bundle.GameplayHash.Value),
                inputs,
                typedIngress);
        }

        static ActorId[] CollectActors(IReadOnlyList<SimulationActorBinding> roster)
        {
            var actors = new ActorId[roster.Count];
            for (int i = 0; i < roster.Count; i++)
                actors[i] = roster[i].ActorId;
            return actors;
        }
    }

    public sealed class RollbackScheduleReadPorts : ISimulationPipelineReadPortSet
    {
        public RollbackScheduleReadPorts(
            IReadOnlySimulationPipelineProductPort<RollbackIngressBatch> ingress,
            IFixedProgramRuntimePort programRuntime)
        {
            Ingress = ingress ?? throw new ArgumentNullException(nameof(ingress));
            ProgramRuntime = programRuntime ?? throw new ArgumentNullException(nameof(programRuntime));
        }

        public IReadOnlySimulationPipelineProductPort<RollbackIngressBatch> Ingress { get; }
        public IFixedProgramRuntimePort ProgramRuntime { get; }
    }

    public sealed class RollbackScheduleWritePorts : ISimulationPipelineWritePortSet
    {
        public RollbackScheduleWritePorts(
            IExclusiveSimulationPipelineProductWriter<SimulationSessionExecutionPlan<FixedSimulationStep>> executionPlan)
        {
            ExecutionPlan = executionPlan ?? throw new ArgumentNullException(nameof(executionPlan));
        }

        public IExclusiveSimulationPipelineProductWriter<SimulationSessionExecutionPlan<FixedSimulationStep>> ExecutionPlan { get; }
    }
}
