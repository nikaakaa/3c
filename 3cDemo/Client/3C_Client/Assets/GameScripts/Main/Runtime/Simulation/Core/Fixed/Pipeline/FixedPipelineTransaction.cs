using System;
using System.Collections.Generic;

using ThirdPersonPerformance.Instrumentation;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    public enum FixedPipelineTransactionOutcome : byte
    {
        Pending = 1,
        Committed = 2
    }

    public sealed class FixedPipelineTransactionResult
    {
        public FixedPipelineTransactionResult(
            FixedPipelineTransactionOutcome outcome,
            StableHash transactionIdentity,
            ulong lastCompletedTick,
            FixedSimulationCommitBatch commitBatch)
        {
            if (!Enum.IsDefined(typeof(FixedPipelineTransactionOutcome), outcome) || !transactionIdentity.IsValid)
                throw new ArgumentException("Pipeline transaction result identity is incomplete.");
            if ((outcome == FixedPipelineTransactionOutcome.Committed) != (commitBatch != null))
                throw new ArgumentException("Only a committed Pipeline transaction has a Commit batch.", nameof(commitBatch));
            Outcome = outcome;
            TransactionIdentity = transactionIdentity;
            LastCompletedTick = lastCompletedTick;
            CommitBatch = commitBatch;
        }

        public FixedPipelineTransactionOutcome Outcome { get; }
        public StableHash TransactionIdentity { get; }
        public ulong LastCompletedTick { get; }
        public FixedSimulationCommitBatch CommitBatch { get; }
    }

    public sealed class FixedPipelineTransaction
    {
        readonly FixedPipelineTransactionPort m_Target;
        readonly PipelineTransactionCoordinator<
            FixedSimulationStep,
            FixedPipelineWorkingState,
            FixedCompletedSimulationStep,
            SimulationActorTickResult,
            SimulationActorState,
            FixedSourceEgressRecord,
            FixedSimulationCommitBatch> m_Coordinator;

        public FixedPipelineTransaction(
            SimulationSessionCompositionDescriptor descriptor,
            CompiledSimulationPipelinePlan plan,
            FixedCharacterRuntime characterRuntime,
            SimulationWorldStateStore stateStore,
            ICharacterWorldSolver solver,
            IFixedSimulationRestoreSource restoreSource,
            IFixedSimulationSessionSnapshotCodec snapshotCodec,
            IFixedSimulationCommitter committer,
            ISimulationDiagnosticsSink diagnostics,
            IReadOnlyList<IFixedCompiledPipelinePassRuntime> passes,
            IReadOnlyList<ISimulationPipelineStateParticipant> stateParticipants,
            IReadOnlyList<ISimulationPipelineReconstructiblePass> reconstructiblePasses,
            FixedPipelineProductStore products,
            FixedWorkingStatePort workingStatePort,
            FixedCompletedStepPort completedStepPort)
        {
            var services = new PipelineTransactionRuntimeServices(
                descriptor,
                plan,
                passes,
                stateParticipants,
                reconstructiblePasses);
            m_Target = new FixedPipelineTransactionPort(
                services,
                characterRuntime,
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
                FixedSimulationStep,
                FixedPipelineWorkingState,
                FixedCompletedSimulationStep,
                SimulationActorTickResult,
                SimulationActorState,
                FixedSourceEgressRecord,
                FixedSimulationCommitBatch>(services, m_Target);
        }

        [PerformanceProbe("simulation.pipeline.transaction")]
        public FixedPipelineTransactionResult Execute(SimulationSessionLogicTickContext outer)
        {
            PipelineTransactionControlResult<FixedSimulationCommitBatch> result = m_Coordinator.Execute(outer);
            return new FixedPipelineTransactionResult(
                result.Outcome == PipelineTransactionOutcome.Pending
                    ? FixedPipelineTransactionOutcome.Pending
                    : FixedPipelineTransactionOutcome.Committed,
                result.TransactionIdentity,
                result.LastCompletedTick,
                result.CommitBatch);
        }

        public SimulationSessionCheckpoint CaptureCheckpoint() => m_Target.CaptureCheckpoint();

        public void RestoreCheckpoint(SimulationSessionCheckpoint checkpoint) => m_Target.RestoreCheckpoint(checkpoint);
    }

    internal sealed class FixedPipelineTransactionPort :
        IPipelineTransactionTargetPort<
            FixedSimulationStep,
            FixedPipelineWorkingState,
            FixedCompletedSimulationStep,
            SimulationActorTickResult,
            SimulationActorState,
            FixedSourceEgressRecord,
            FixedSimulationCommitBatch>
    {
        readonly IFixedSimulationRestoreSource m_RestoreSource;
        readonly PipelineTransactionRuntimeServices m_Services;
        readonly FixedCharacterRuntime m_CharacterRuntime;
        IReadOnlyList<FixedCharacterRuntimeActor> m_Roster;
        readonly SimulationWorldStateStore m_StateStore;
        readonly ICharacterWorldSolver m_Solver;
        readonly ISimulationDiagnosticsSink m_Diagnostics;
        readonly IFixedSimulationSessionSnapshotCodec m_SnapshotCodec;
        readonly IFixedSimulationCommitter m_Committer;
        readonly FixedPipelineProductStore m_Products;
        readonly IReadOnlySimulationPipelineProductPort<FixedPendingEvaluationBatch> m_PendingEvaluations;
        readonly FixedWorkingStatePort m_WorkingStatePort;
        readonly FixedCompletedStepPort m_CompletedStepPort;

        public FixedPipelineTransactionPort(
            PipelineTransactionRuntimeServices services,
            FixedCharacterRuntime characterRuntime,
            SimulationWorldStateStore stateStore,
            ICharacterWorldSolver solver,
            ISimulationDiagnosticsSink diagnostics,
            IFixedSimulationRestoreSource restoreSource,
            IFixedSimulationSessionSnapshotCodec snapshotCodec,
            IFixedSimulationCommitter committer,
            FixedPipelineProductStore products,
            FixedWorkingStatePort workingStatePort,
            FixedCompletedStepPort completedStepPort)
        {
            m_Services = services ?? throw new ArgumentNullException(nameof(services));
            m_CharacterRuntime = characterRuntime ?? throw new ArgumentNullException(nameof(characterRuntime));
            m_Roster = m_CharacterRuntime.Roster;
            m_StateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
            m_Solver = solver ?? throw new ArgumentNullException(nameof(solver));
            m_Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            m_RestoreSource = restoreSource;
            m_SnapshotCodec = snapshotCodec ?? throw new ArgumentNullException(nameof(snapshotCodec));
            m_Committer = committer ?? throw new ArgumentNullException(nameof(committer));
            m_Products = products ?? throw new ArgumentNullException(nameof(products));
            m_PendingEvaluations = m_Products.GetRequired<FixedExclusiveProductSlot<FixedPendingEvaluationBatch>>(
                SimulationPipelineProducts.PendingActorEvaluations);
            m_WorkingStatePort = workingStatePort ?? throw new ArgumentNullException(nameof(workingStatePort));
            m_CompletedStepPort = completedStepPort ?? throw new ArgumentNullException(nameof(completedStepPort));
            _ = characterRuntime ?? throw new ArgumentNullException(nameof(characterRuntime));
        }

        public string TransactionIdentityDomain => "fixed-pipeline-transaction/1";
        public bool DiagnosticsEnabled => m_Diagnostics.IsEnabled;
        public ulong BaselineCompletedTick => m_StateStore.Current.LastCompletedTick;
        public WorldRevision BaselineWorldRevision => m_StateStore.Current.WorldState.WorldRevision;

        public SimulationSessionCheckpoint CaptureCheckpoint()
        {
            SimulationWorldStateSet state = m_StateStore.Current;
            if (state.LastCompletedTick == 0)
                throw new InvalidOperationException("Session checkpoint requires a completed Logic Tick.");
            SimulationPipelineStateSnapshot pipeline = SimulationPipelineStateSnapshotCoordinator.Capture(
                m_Services.Plan,
                state.LastCompletedTick,
                m_Services.StateParticipants);
            FixedSimulationSessionSnapshot snapshot = m_SnapshotCodec.Capture(
                m_Services.Descriptor,
                m_CharacterRuntime,
                state,
                pipeline,
                m_Solver.Descriptor.Capabilities);
            byte[] payload = m_SnapshotCodec.Write(snapshot);
            string snapshotId = $"{m_Services.Descriptor.SessionId}/checkpoint/{snapshot.Tick.Value}";
            return new SimulationSessionCheckpoint(
                m_Services.Descriptor.SessionId,
                snapshot.Tick,
                m_Services.Descriptor.Pipeline.Hash,
                m_Services.Descriptor.ExecutionBackend.ComponentId,
                m_Services.Descriptor.ExecutionBackend.SemanticVersion,
                snapshotId,
                snapshot.SnapshotHash,
                payload);
        }

        public void RestoreCheckpoint(SimulationSessionCheckpoint checkpoint)
        {
            if (checkpoint == null)
                throw new ArgumentNullException(nameof(checkpoint));
            if (!checkpoint.SessionId.Equals(m_Services.Descriptor.SessionId))
                throw new InvalidOperationException("Checkpoint belongs to another Simulation Session.");
            FixedSimulationSessionSnapshot snapshot = m_SnapshotCodec.Read(checkpoint.PayloadBuffer);
            var directive = new SimulationRestoreDirective(
                checkpoint.SnapshotId,
                checkpoint.Tick,
                checkpoint.PipelineHash,
                checkpoint.BackendId,
                checkpoint.BackendSemanticVersion,
                checkpoint.SnapshotHash);
            m_SnapshotCodec.RequireRestore(
                m_Services.Descriptor,
                m_CharacterRuntime,
                m_Solver,
                directive,
                snapshot);
            SimulationWorldStateSet restored = m_StateStore.PrepareRestore(snapshot.World);
            SimulationPipelineStateRestoreTransaction pipeline = SimulationPipelineStateSnapshotCoordinator.PrepareRestore(
                m_Services.Plan,
                snapshot.Pipeline,
                m_Services.StateParticipants);
            var working = new FixedPipelineWorkingState(m_StateStore.Current);
            using var restore = new SimulationSessionRestoreTransaction(new ISimulationSessionRestoreParticipantTransaction[]
            {
                new FixedCharacterRestoreTransaction(working, restored, snapshot.World.WorldHash.ToString()),
                new FixedWorldRestoreTransaction(working, m_Solver, restored, snapshot.World.WorldHash.ToString()),
                pipeline
            });
            restore.ApplyAndValidate();
            m_StateStore.Restore(snapshot.World);
            restore.CompleteAfterAtomicSessionPublish();
        }

        public FixedPipelineWorkingState CreateWorkingState()
        {
            return new FixedPipelineWorkingState(m_StateStore.Current);
        }

        public ulong GetLastCompletedTick(FixedPipelineWorkingState workingState)
        {
            return workingState.LastCompletedTick;
        }

        public void BeginOuterTransaction()
        {
            m_Products.BeginOuterTransaction();
            m_CompletedStepPort.Clear();
        }

        public void BeginSimulationStep(FixedPipelineWorkingState workingState, FixedSimulationStep step)
        {
            m_Products.BeginSimulationStep();
            m_WorkingStatePort.Set(workingState.Current, step);
        }

        public void SetCompletedSteps(IReadOnlyList<FixedCompletedSimulationStep> steps)
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

        public SimulationSessionExecutionPlan<FixedSimulationStep> ReadExecutionPlan()
        {
            return m_Products
                .GetRequired<FixedExclusiveProductSlot<SimulationSessionExecutionPlan<FixedSimulationStep>>>(
                    SimulationPipelineProducts.ExecutionPlan)
                .Read();
        }

        public void ValidateTargetExecutionPlan(
            SimulationSessionExecutionPlan<FixedSimulationStep> plan,
            SimulationSessionLogicTickContext outer)
        {
            _ = outer;
            FixedCharacterRuntime characterRuntime = m_CharacterRuntime;
            IReadOnlyList<FixedCharacterRuntimeActor> roster = m_Roster;
            for (int stepIndex = 0; stepIndex < plan.Steps.Count; stepIndex++)
            {
                FixedSimulationStep step = plan.Steps[stepIndex];
                if (step.Inputs.Count != roster.Count)
                    throw Failure("execution_plan_roster_mismatch", "ExecutionPlan Step input count does not match the locked roster.");
                for (int i = 0; i < step.Inputs.Count; i++)
                {
                    SimulationPipelineActorInput<FixedStepInput> input = step.Inputs[i];
                    CharacterSimulationInput value = input.Value.Input;
                    if (!input.ActorId.Equals(roster[i].ActorId) || value == null ||
                        !value.NumericProfile.Equals(characterRuntime.NumericProfile) ||
                        !value.TickSource.Equals(step.Source) || value.Sequence != input.Sequence)
                    {
                        throw Failure("execution_plan_input_mismatch", $"ExecutionPlan input for Actor '{input.ActorId}' is invalid.");
                    }
                }
                ValidateIngress(step.Ingress, characterRuntime.NumericProfile);
            }
        }

        public PipelineRestorePreparation PrepareRestore(
            SimulationRestoreDirective directive,
            FixedPipelineWorkingState workingState,
            PipelineTransactionRuntimeServices services)
        {
            if (m_RestoreSource == null)
                throw Failure("restore_source_missing", "ExecutionPlan requested restore without a bound restore Source.");
            FixedSimulationSessionSnapshot snapshot = m_RestoreSource.GetRequiredSnapshot(directive) ??
                throw Failure("restore_snapshot_missing", "Restore Source returned no Session snapshot.");
            m_SnapshotCodec.RequireRestore(
                services.Descriptor,
                m_CharacterRuntime,
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
                new FixedCharacterRestoreTransaction(workingState, restored, snapshot.World.WorldHash.ToString()),
                new FixedWorldRestoreTransaction(workingState, m_Solver, restored, snapshot.World.WorldHash.ToString()),
                pipeline
            });
            return new PipelineRestorePreparation(transaction, snapshot.Pipeline, snapshot.Tick);
        }

        public bool TryGetCoreStepStage(
            ICompiledSimulationPipelinePassRuntime pass,
            out PipelineTransactionStage stage)
        {
            SimulationPipelinePassId passId = pass.Descriptor.PassId;
            if (passId.Equals(StandardFixedPipelinePassContracts.AbilityEvaluate.PassId))
            {
                stage = PipelineTransactionStage.Evaluate;
                return true;
            }
            if (passId.Equals(StandardFixedPipelinePassContracts.WorldResolveBatch.PassId))
            {
                stage = PipelineTransactionStage.ResolveBatch;
                return true;
            }
            if (passId.Equals(StandardFixedPipelinePassContracts.AbilityFinalize.PassId))
            {
                stage = PipelineTransactionStage.Finalize;
                return true;
            }
            stage = default;
            return false;
        }

        public int FinalizedResultCount => GetFinalizedSlot().UnsealedCount;

        public FixedCompletedSimulationStep CompleteStep(
            SimulationSessionExecutionPlan<FixedSimulationStep> executionPlan,
            FixedSimulationStep step,
            int finalizedStart,
            FixedPipelineWorkingState workingState,
            SessionExecutionWorkspace<
                FixedCompletedSimulationStep,
                SimulationActorTickResult,
                SimulationActorState,
                FixedSourceEgressRecord> workspace)
        {
            _ = workingState;
            FixedAppendProductSlot<FixedFinalizedActorResult> finalized = GetFinalizedSlot();
            int finalizedCount = finalized.UnsealedCount - finalizedStart;
            if (finalizedCount != m_Roster.Count)
                throw Failure("finalized_actor_count_mismatch", "Ability Finalize Pass did not produce exactly one result per Actor.", SimulationSessionFailureStage.Step);
            List<SimulationActorTickResult> actorResults = workspace.ActorResults.Values;
            actorResults.Clear();
            workspace.ActorResults.EnsureCapacity(finalizedCount);
            for (int i = finalizedStart; i < finalized.UnsealedCount; i++)
            {
                SimulationActorTickResult result = finalized.GetUnsealed(i).Value.Result;
                if (result.Tick != step.Tick)
                    throw Failure("finalized_actor_tick_mismatch", "Ability Finalize Pass produced a result for another Tick.", SimulationSessionFailureStage.Step);
                actorResults.Add(result);
            }
            actorResults.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            for (int i = 0; i < actorResults.Count; i++)
            {
                if (!actorResults[i].ActorId.Equals(m_Roster[i].ActorId))
                    throw Failure("finalized_actor_roster_mismatch", "Ability Finalize Pass result roster does not match the locked roster.", SimulationSessionFailureStage.Step);
            }
            WorldSolveBatchResult worldResult = m_Products
                .GetRequired<FixedExclusiveProductSlot<WorldSolveBatchResult>>(
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
                    m_CharacterRuntime,
                    step.Tick,
                    nextActors,
                    worldResult.NextWorldState,
                    m_Solver.Descriptor.Capabilities)
                : null;
            FixedSimulationStepSnapshot stepSnapshot = capture
                ? new FixedSimulationStepSnapshot(m_Services.Descriptor.Identity, worldSnapshot, pipelineSnapshot)
                : null;
            var tickResult = new SimulationTickResult(
                m_CharacterRuntime.NumericProfile,
                m_CharacterRuntime.GameplayContentHash,
                step.Tick,
                actorResults,
                worldResult.Summary,
                (executionPlan.Requirements & SimulationSessionPlanRequirement.Snapshot) != 0 ? worldSnapshot : null);
            return new FixedCompletedSimulationStep(step, tickResult, candidateState, stepSnapshot);
        }

        public SimulationPipelineStateSnapshot GetPipelineProjection(FixedCompletedSimulationStep completedStep)
        {
            return completedStep.PipelineProjection;
        }

        public void ApplyCompletedStep(
            FixedPipelineWorkingState workingState,
            FixedCompletedSimulationStep completedStep)
        {
            workingState.Replace(completedStep.State);
        }

        public FixedSimulationCommitBatch FreezeCommitBatch(
            StableHash transactionIdentity,
            IReadOnlyList<FixedCompletedSimulationStep> completedSteps,
            SessionExecutionWorkspace<
                FixedCompletedSimulationStep,
                SimulationActorTickResult,
                SimulationActorState,
                FixedSourceEgressRecord> workspace)
        {
            SimulationPipelineOutputDispositionSet dispositions = m_Products
                .GetRequired<FixedExclusiveProductSlot<SimulationPipelineOutputDispositionSet>>(
                    SimulationPipelineProducts.OutputDispositionSet)
                .Read();
            if (!dispositions.TransactionIdentity.Equals(transactionIdentity))
                throw Failure("output_disposition_transaction_mismatch", "OutputDispositionSet belongs to another outer transaction.", SimulationSessionFailureStage.Egress);
            IReadOnlyList<FixedSourceEgressRecord> sourceEgress = ReadSourceEgress(workspace.Egress);
            return new FixedSimulationCommitBatch(
                transactionIdentity,
                completedSteps,
                dispositions,
                sourceEgress);
        }

        public void Commit(FixedSimulationCommitBatch commitBatch)
        {
            m_Committer.Commit(commitBatch);
        }

        public void PublishWorkingState(FixedPipelineWorkingState workingState)
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
            SimulationNumericProfile numericProfile)
        {
            for (int i = 0; i < ingressValues.Count; i++)
            {
                SimulationPipelineTypedIngress<SimulationIngress> ingress = ingressValues[i];
                SimulationIngressHeader header = ingress.Value.Header;
                if (!header.ActorId.Equals(ingress.ActorId) ||
                    !header.NumericProfile.Equals(numericProfile) ||
                    header.SourceTick != ingress.Source.SourceTick || header.Sequence != ingress.Sequence ||
                    !string.Equals(header.FactIdentity.ToString(), ingress.FactIdentity, StringComparison.Ordinal))
                {
                    throw Failure("execution_plan_ingress_mismatch", "ExecutionPlan typed ingress identity is invalid.");
                }
            }
        }

        FixedAppendProductSlot<FixedFinalizedActorResult> GetFinalizedSlot()
        {
            return m_Products.GetRequired<FixedAppendProductSlot<FixedFinalizedActorResult>>(
                SimulationPipelineProducts.FinalizedStepResult);
        }

        IReadOnlyList<FixedSourceEgressRecord> ReadSourceEgress(
            ExecutionWorkspaceBuffer<FixedSourceEgressRecord> values)
        {
            values.Clear();
            if (!m_Products.TryGet<FixedAppendProductSlot<FixedSourceEgressRecord>>(
                    SimulationPipelineProducts.SourceEgress,
                    out FixedAppendProductSlot<FixedSourceEgressRecord> slot))
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
