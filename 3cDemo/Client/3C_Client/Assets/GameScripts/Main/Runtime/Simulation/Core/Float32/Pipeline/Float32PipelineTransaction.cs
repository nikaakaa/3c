using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public enum Float32PipelineTransactionOutcome : byte
    {
        Pending = 1,
        Committed = 2
    }

    public sealed class Float32PipelineTransactionResult
    {
        public Float32PipelineTransactionResult(
            Float32PipelineTransactionOutcome outcome,
            StableHash transactionIdentity,
            ulong lastCompletedTick,
            Float32SimulationCommitBatch commitBatch)
        {
            if (!Enum.IsDefined(typeof(Float32PipelineTransactionOutcome), outcome) || !transactionIdentity.IsValid)
                throw new ArgumentException("Pipeline transaction result identity is incomplete.");
            if ((outcome == Float32PipelineTransactionOutcome.Committed) != (commitBatch != null))
                throw new ArgumentException("Only a committed Pipeline transaction has a Commit batch.", nameof(commitBatch));
            Outcome = outcome;
            TransactionIdentity = transactionIdentity;
            LastCompletedTick = lastCompletedTick;
            CommitBatch = commitBatch;
        }

        public Float32PipelineTransactionOutcome Outcome { get; }
        public StableHash TransactionIdentity { get; }
        public ulong LastCompletedTick { get; }
        public Float32SimulationCommitBatch CommitBatch { get; }
    }

    public sealed class Float32PipelineTransaction
    {
        readonly PipelineTransactionCoordinator<
            Float32SimulationStep,
            Float32PipelineWorkingState,
            Float32CompletedSimulationStep,
            SimulationActorTickResult,
            SimulationActorState,
            Float32SourceEgressRecord,
            Float32SimulationCommitBatch> m_Coordinator;

        public Float32PipelineTransaction(
            SimulationSessionCompositionDescriptor descriptor,
            CompiledSimulationPipelinePlan plan,
            SimulationProgramCatalog catalog,
            IReadOnlyList<SimulationActorBinding> roster,
            SimulationWorldStateStore stateStore,
            ICharacterWorldSolver solver,
            IFloat32SimulationRestoreSource restoreSource,
            IFloat32SimulationSessionSnapshotCodec snapshotCodec,
            IFloat32SimulationCommitter committer,
            ISimulationDiagnosticsSink diagnostics,
            IReadOnlyList<IFloat32CompiledPipelinePassRuntime> passes,
            IReadOnlyList<ISimulationPipelineStateParticipant> stateParticipants,
            IReadOnlyList<ISimulationPipelineReconstructiblePass> reconstructiblePasses,
            Float32PipelineProductStore products,
            Float32WorkingStatePort workingStatePort,
            Float32CompletedStepPort completedStepPort)
        {
            var services = new PipelineTransactionRuntimeServices(
                descriptor,
                plan,
                passes,
                stateParticipants,
                reconstructiblePasses);
            var target = new Float32PipelineTransactionPort(
                services,
                catalog,
                roster,
                stateStore,
                solver,
                diagnostics,
                restoreSource,
                snapshotCodec,
                committer,
                products,
                workingStatePort,
                completedStepPort);
            m_Coordinator = new PipelineTransactionCoordinator<
                Float32SimulationStep,
                Float32PipelineWorkingState,
                Float32CompletedSimulationStep,
                SimulationActorTickResult,
                SimulationActorState,
                Float32SourceEgressRecord,
                Float32SimulationCommitBatch>(services, target);
        }

        public Float32PipelineTransactionResult Execute(SimulationSessionLogicTickContext outer)
        {
            PipelineTransactionControlResult<Float32SimulationCommitBatch> result = m_Coordinator.Execute(outer);
            return new Float32PipelineTransactionResult(
                result.Outcome == PipelineTransactionOutcome.Pending
                    ? Float32PipelineTransactionOutcome.Pending
                    : Float32PipelineTransactionOutcome.Committed,
                result.TransactionIdentity,
                result.LastCompletedTick,
                result.CommitBatch);
        }
    }

    internal sealed class Float32PipelineTransactionPort :
        IPipelineTransactionTargetPort<
            Float32SimulationStep,
            Float32PipelineWorkingState,
            Float32CompletedSimulationStep,
            SimulationActorTickResult,
            SimulationActorState,
            Float32SourceEgressRecord,
            Float32SimulationCommitBatch>
    {
        readonly IFloat32SimulationRestoreSource m_RestoreSource;
        readonly PipelineTransactionRuntimeServices m_Services;
        readonly SimulationProgramCatalog m_Catalog;
        readonly IReadOnlyList<SimulationActorBinding> m_Roster;
        readonly SimulationWorldStateStore m_StateStore;
        readonly ICharacterWorldSolver m_Solver;
        readonly ISimulationDiagnosticsSink m_Diagnostics;
        readonly IFloat32SimulationSessionSnapshotCodec m_SnapshotCodec;
        readonly IFloat32SimulationCommitter m_Committer;
        readonly Float32PipelineProductStore m_Products;
        readonly IReadOnlySimulationPipelineProductPort<Float32PendingEvaluationBatch> m_PendingEvaluations;
        readonly Float32WorkingStatePort m_WorkingStatePort;
        readonly Float32CompletedStepPort m_CompletedStepPort;

        public Float32PipelineTransactionPort(
            PipelineTransactionRuntimeServices services,
            SimulationProgramCatalog catalog,
            IReadOnlyList<SimulationActorBinding> roster,
            SimulationWorldStateStore stateStore,
            ICharacterWorldSolver solver,
            ISimulationDiagnosticsSink diagnostics,
            IFloat32SimulationRestoreSource restoreSource,
            IFloat32SimulationSessionSnapshotCodec snapshotCodec,
            IFloat32SimulationCommitter committer,
            Float32PipelineProductStore products,
            Float32WorkingStatePort workingStatePort,
            Float32CompletedStepPort completedStepPort)
        {
            m_Services = services ?? throw new ArgumentNullException(nameof(services));
            m_Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            m_Roster = roster ?? throw new ArgumentNullException(nameof(roster));
            m_StateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
            m_Solver = solver ?? throw new ArgumentNullException(nameof(solver));
            m_Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            m_RestoreSource = restoreSource;
            m_SnapshotCodec = snapshotCodec ?? throw new ArgumentNullException(nameof(snapshotCodec));
            m_Committer = committer ?? throw new ArgumentNullException(nameof(committer));
            m_Products = products ?? throw new ArgumentNullException(nameof(products));
            m_PendingEvaluations = m_Products.GetRequired<Float32ExclusiveProductSlot<Float32PendingEvaluationBatch>>(
                SimulationPipelineProducts.PendingActorEvaluations);
            m_WorkingStatePort = workingStatePort ?? throw new ArgumentNullException(nameof(workingStatePort));
            m_CompletedStepPort = completedStepPort ?? throw new ArgumentNullException(nameof(completedStepPort));
        }

        public string TransactionIdentityDomain => "float32-pipeline-transaction/1";
        public bool DiagnosticsEnabled => m_Diagnostics.IsEnabled;
        public ulong BaselineCompletedTick => m_StateStore.Current.LastCompletedTick;
        public WorldRevision BaselineWorldRevision => m_StateStore.Current.WorldState.WorldRevision;

        public Float32PipelineWorkingState CreateWorkingState()
        {
            return new Float32PipelineWorkingState(m_StateStore.Current);
        }

        public ulong GetLastCompletedTick(Float32PipelineWorkingState workingState)
        {
            return workingState.LastCompletedTick;
        }

        public void BeginOuterTransaction()
        {
            m_Products.BeginOuterTransaction();
            m_CompletedStepPort.Clear();
        }

        public void BeginSimulationStep(Float32PipelineWorkingState workingState, Float32SimulationStep step)
        {
            m_Products.BeginSimulationStep();
            m_WorkingStatePort.Set(workingState.Current, step);
        }

        public void SetCompletedSteps(IReadOnlyList<Float32CompletedSimulationStep> steps)
        {
            m_CompletedStepPort.Set(steps);
        }

        public void ClearTransientState()
        {
            m_WorkingStatePort.Clear();
            m_CompletedStepPort.Clear();
        }

        public void AbortUnconsumedEvaluations()
        {
            if (m_PendingEvaluations.HasValue)
                m_PendingEvaluations.Read().AbortUnconsumed();
        }

        public SimulationSessionExecutionPlan<Float32SimulationStep> ReadExecutionPlan()
        {
            return m_Products
                .GetRequired<Float32ExclusiveProductSlot<SimulationSessionExecutionPlan<Float32SimulationStep>>>(
                    SimulationPipelineProducts.ExecutionPlan)
                .Read();
        }

        public void ValidateTargetExecutionPlan(
            SimulationSessionExecutionPlan<Float32SimulationStep> plan,
            SimulationSessionLogicTickContext outer)
        {
            _ = outer;
            SimulationProgramCatalog catalog = m_Catalog;
            IReadOnlyList<SimulationActorBinding> roster = m_Roster;
            for (int stepIndex = 0; stepIndex < plan.Steps.Count; stepIndex++)
            {
                Float32SimulationStep step = plan.Steps[stepIndex];
                if (step.Inputs.Count != roster.Count)
                    throw Failure("execution_plan_roster_mismatch", "ExecutionPlan Step input count does not match the locked roster.");
                for (int i = 0; i < step.Inputs.Count; i++)
                {
                    SimulationPipelineActorInput<Float32StepInput> input = step.Inputs[i];
                    CharacterSimulationInput value = input.Value.Input;
                    if (!input.ActorId.Equals(roster[i].ActorId) || value == null ||
                        !value.NumericProfile.Equals(catalog.NumericProfile) ||
                        !value.TickSource.Equals(step.Source) || value.Sequence != input.Sequence)
                    {
                        throw Failure("execution_plan_input_mismatch", $"ExecutionPlan input for Actor '{input.ActorId}' is invalid.");
                    }
                }
                ValidateIngress(step.Ingress, catalog);
            }
        }

        public PipelineRestorePreparation PrepareRestore(
            SimulationRestoreDirective directive,
            Float32PipelineWorkingState workingState,
            PipelineTransactionRuntimeServices services)
        {
            if (m_RestoreSource == null)
                throw Failure("restore_source_missing", "ExecutionPlan requested restore without a bound restore Source.");
            Float32SimulationSessionSnapshot snapshot = m_RestoreSource.GetRequiredSnapshot(directive) ??
                throw Failure("restore_snapshot_missing", "Restore Source returned no Session snapshot.");
            m_SnapshotCodec.RequireRestore(
                services.Descriptor,
                m_Catalog,
                m_Solver,
                directive,
                snapshot);
            SimulationWorldStateSet restored = m_StateStore.PrepareRestore(snapshot.World);
            SimulationPipelineStateRestoreTransaction pipeline = SimulationPipelineStateSnapshotCoordinator.PrepareRestore(
                services.Plan,
                snapshot.Pipeline,
                services.StateParticipants);
            var transaction = new SimulationSessionRestoreTransaction(new ISimulationSessionRestoreParticipantTransaction[]
            {
                new Float32CharacterRestoreTransaction(workingState, restored, snapshot.World.WorldHash.ToString()),
                new Float32WorldRestoreTransaction(workingState, m_Solver, restored, snapshot.World.WorldHash.ToString()),
                pipeline
            });
            return new PipelineRestorePreparation(transaction, snapshot.Pipeline, snapshot.Tick);
        }

        public bool TryGetCoreStepStage(
            ICompiledSimulationPipelinePassRuntime pass,
            out PipelineTransactionStage stage)
        {
            SimulationPipelinePassId passId = pass.Descriptor.PassId;
            if (passId.Equals(StandardFloat32PipelinePassContracts.ProgramEvaluate.PassId))
            {
                stage = PipelineTransactionStage.Evaluate;
                return true;
            }
            if (passId.Equals(StandardFloat32PipelinePassContracts.WorldResolveBatch.PassId))
            {
                stage = PipelineTransactionStage.ResolveBatch;
                return true;
            }
            if (passId.Equals(StandardFloat32PipelinePassContracts.ProgramFinalize.PassId))
            {
                stage = PipelineTransactionStage.Finalize;
                return true;
            }
            stage = default;
            return false;
        }

        public int FinalizedResultCount => GetFinalizedSlot().UnsealedCount;

        public Float32CompletedSimulationStep CompleteStep(
            SimulationSessionExecutionPlan<Float32SimulationStep> executionPlan,
            Float32SimulationStep step,
            int finalizedStart,
            Float32PipelineWorkingState workingState,
            SessionExecutionWorkspace<
                Float32CompletedSimulationStep,
                SimulationActorTickResult,
                SimulationActorState,
                Float32SourceEgressRecord> workspace)
        {
            _ = workingState;
            Float32AppendProductSlot<Float32FinalizedActorResult> finalized = GetFinalizedSlot();
            int finalizedCount = finalized.UnsealedCount - finalizedStart;
            if (finalizedCount != m_Roster.Count)
                throw Failure("finalized_actor_count_mismatch", "Program Finalize Pass did not produce exactly one result per Actor.", SimulationSessionFailureStage.Step);
            List<SimulationActorTickResult> actorResults = workspace.ActorResults.Values;
            actorResults.Clear();
            workspace.ActorResults.EnsureCapacity(finalizedCount);
            for (int i = finalizedStart; i < finalized.UnsealedCount; i++)
            {
                SimulationActorTickResult result = finalized.GetUnsealed(i).Value.Result;
                if (result.Tick != step.Tick)
                    throw Failure("finalized_actor_tick_mismatch", "Program Finalize Pass produced a result for another Tick.", SimulationSessionFailureStage.Step);
                actorResults.Add(result);
            }
            actorResults.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            for (int i = 0; i < actorResults.Count; i++)
            {
                if (!actorResults[i].ActorId.Equals(m_Roster[i].ActorId))
                    throw Failure("finalized_actor_roster_mismatch", "Program Finalize Pass result roster does not match the locked roster.", SimulationSessionFailureStage.Step);
            }
            WorldSolveBatchResult worldResult = m_Products
                .GetRequired<Float32ExclusiveProductSlot<WorldSolveBatchResult>>(
                    SimulationPipelineProducts.WorldSolveBatchResult)
                .Read();
            ValidateWorldResult(worldResult, step.Tick);
            ExecutionWorkspaceBuffer<SimulationActorState> nextActors = workspace.ActorStates;
            nextActors.Clear();
            nextActors.EnsureCapacity(actorResults.Count);
            for (int i = 0; i < actorResults.Count; i++)
                nextActors.Add(new SimulationActorState(actorResults[i].ActorId, actorResults[i].State));
            var candidateState = new SimulationWorldStateSet(step.Tick.Value, nextActors, worldResult.NextWorldState);
            bool capture = (executionPlan.Requirements &
                (SimulationSessionPlanRequirement.Snapshot | SimulationSessionPlanRequirement.StateHash)) != 0;
            SimulationPipelineStateSnapshot pipelineSnapshot = capture
                ? SimulationPipelineStateSnapshotCoordinator.CaptureStepProjection(
                    m_Services.Plan,
                    step.Tick.Value,
                    m_Services.StateParticipants)
                : null;
            SimulationWorldSnapshot worldSnapshot = capture
                ? SimulationWorldSnapshotFactory.Capture(
                    m_Catalog,
                    step.Tick,
                    nextActors,
                    worldResult.NextWorldState,
                    m_Solver.Descriptor.Capabilities)
                : null;
            Float32SimulationStepSnapshot stepSnapshot = capture
                ? new Float32SimulationStepSnapshot(m_Services.Descriptor.Identity, worldSnapshot, pipelineSnapshot)
                : null;
            var tickResult = new SimulationTickResult(
                m_Catalog.NumericProfile,
                m_Catalog.CatalogHash,
                step.Tick,
                actorResults,
                worldResult.Summary,
                (executionPlan.Requirements & SimulationSessionPlanRequirement.Snapshot) != 0 ? worldSnapshot : null);
            return new Float32CompletedSimulationStep(step, tickResult, candidateState, stepSnapshot);
        }

        public SimulationPipelineStateSnapshot GetPipelineProjection(Float32CompletedSimulationStep completedStep)
        {
            return completedStep.PipelineProjection;
        }

        public void ApplyCompletedStep(
            Float32PipelineWorkingState workingState,
            Float32CompletedSimulationStep completedStep)
        {
            workingState.Replace(completedStep.State);
        }

        public Float32SimulationCommitBatch FreezeCommitBatch(
            StableHash transactionIdentity,
            IReadOnlyList<Float32CompletedSimulationStep> completedSteps,
            SessionExecutionWorkspace<
                Float32CompletedSimulationStep,
                SimulationActorTickResult,
                SimulationActorState,
                Float32SourceEgressRecord> workspace)
        {
            SimulationPipelineOutputDispositionSet dispositions = m_Products
                .GetRequired<Float32ExclusiveProductSlot<SimulationPipelineOutputDispositionSet>>(
                    SimulationPipelineProducts.OutputDispositionSet)
                .Read();
            if (!dispositions.TransactionIdentity.Equals(transactionIdentity))
                throw Failure("output_disposition_transaction_mismatch", "OutputDispositionSet belongs to another outer transaction.", SimulationSessionFailureStage.Egress);
            IReadOnlyList<Float32SourceEgressRecord> sourceEgress = ReadSourceEgress(workspace.Egress);
            return new Float32SimulationCommitBatch(
                transactionIdentity,
                completedSteps,
                dispositions,
                sourceEgress);
        }

        public void Commit(Float32SimulationCommitBatch commitBatch)
        {
            m_Committer.Commit(commitBatch);
        }

        public void PublishWorkingState(Float32PipelineWorkingState workingState)
        {
            m_StateStore.ReplaceValidated(workingState.Current);
        }

        public void RestoreSolverBaseline()
        {
            m_Solver.Restore(m_StateStore.Current.WorldState);
        }

        public void PublishTrace(PipelineTransactionTrace trace)
        {
            if (!m_Diagnostics.IsEnabled)
                return;
            m_Diagnostics.PublishPipeline(new SimulationPipelineTraceRecord(
                m_Services.Descriptor.Identity,
                m_Services.Plan.Identity,
                trace.Source,
                trace.CompletedTick,
                (SimulationPipelineTraceKind)(byte)trace.Kind,
                trace.Success,
                trace.Detail,
                trace.Phase,
                trace.PassId,
                trace.PassVersion,
                trace.ScheduleStatus,
                trace.RestoreRequested,
                trace.StepCount,
                trace.ElapsedStopwatchTicks,
                trace.ProductInputs,
                trace.ProductOutputs,
                trace.SnapshotParticipant,
                trace.SnapshotHash));
        }

        void ValidateWorldResult(WorldSolveBatchResult result, SimulationTick tick)
        {
            if (result == null || result.Tick != tick ||
                !result.SolverId.Equals(m_Solver.Descriptor.ImplementationId) ||
                !string.Equals(result.SolverVersion, m_Solver.Descriptor.Version, StringComparison.Ordinal) ||
                !result.NextWorldState.WorldRevision.Equals(m_StateStore.Current.WorldState.WorldRevision))
            {
                throw Failure(
                    "world_result_identity_mismatch",
                    "WorldSolve Pass returned a result for another Tick, Solver or WorldRevision.",
                    SimulationSessionFailureStage.Step);
            }
        }

        void ValidateIngress(
            IReadOnlyList<SimulationPipelineTypedIngress<SimulationIngress>> ingressValues,
            SimulationProgramCatalog catalog)
        {
            for (int i = 0; i < ingressValues.Count; i++)
            {
                SimulationPipelineTypedIngress<SimulationIngress> ingress = ingressValues[i];
                SimulationIngressHeader header = ingress.Value.Header;
                if (!header.ActorId.Equals(ingress.ActorId) ||
                    !header.NumericProfile.Equals(catalog.NumericProfile) ||
                    header.SourceTick != ingress.Source.SourceTick || header.Sequence != ingress.Sequence ||
                    !string.Equals(header.FactIdentity.ToString(), ingress.FactIdentity, StringComparison.Ordinal))
                {
                    throw Failure("execution_plan_ingress_mismatch", "ExecutionPlan typed ingress identity is invalid.");
                }
            }
        }

        Float32AppendProductSlot<Float32FinalizedActorResult> GetFinalizedSlot()
        {
            return m_Products.GetRequired<Float32AppendProductSlot<Float32FinalizedActorResult>>(
                SimulationPipelineProducts.FinalizedStepResult);
        }

        IReadOnlyList<Float32SourceEgressRecord> ReadSourceEgress(
            ExecutionWorkspaceBuffer<Float32SourceEgressRecord> values)
        {
            values.Clear();
            if (!m_Products.TryGet<Float32AppendProductSlot<Float32SourceEgressRecord>>(
                    SimulationPipelineProducts.SourceEgress,
                    out Float32AppendProductSlot<Float32SourceEgressRecord> slot))
            {
                return values;
            }
            values.EnsureCapacity(slot.Count);
            for (int i = 0; i < slot.Count; i++)
                values.Add(slot.Get(i).Value);
            return values;
        }

        static SimulationSessionCompositionException Failure(
            string code,
            string message,
            SimulationSessionFailureStage stage = SimulationSessionFailureStage.Schedule)
        {
            return PipelineTransactionFailure.Create(stage, code, message);
        }
    }
}
