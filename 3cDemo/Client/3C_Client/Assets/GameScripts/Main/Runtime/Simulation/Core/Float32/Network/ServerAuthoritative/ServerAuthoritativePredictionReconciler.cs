using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.ServerAuthoritative
{
    internal sealed class ServerAuthoritativePredictionReconciler
    {
        readonly CharacterSimulationProgram m_Program;
        readonly ServerAuthoritativePipelineCompatibilityIdentity m_Compatibility;
        readonly ServerAuthoritativeWorldIdentity m_AuthorityWorld;

        public ServerAuthoritativePredictionReconciler(
            CharacterSimulationProgram program,
            ServerAuthoritativePipelineCompatibilityIdentity compatibility,
            ServerAuthoritativeWorldIdentity authorityWorld)
        {
            m_Program = program ?? throw new ArgumentNullException(nameof(program));
            m_Compatibility = compatibility ?? throw new ArgumentNullException(nameof(compatibility));
            m_AuthorityWorld = authorityWorld ?? throw new ArgumentNullException(nameof(authorityWorld));
            if (m_Program.Manifest.ProgramId != m_Compatibility.ProgramId ||
                !m_Program.ProgramHash.Equals(m_Compatibility.ProgramHash) ||
                !m_Program.LayoutHash.Equals(m_Compatibility.LayoutHash) ||
                !m_Program.Manifest.OperationSetVersion.Equals(m_Compatibility.OperationSetVersion))
            {
                throw new InvalidOperationException("Prediction state Program does not match the locked ServerAuthoritative compatibility identity.");
            }
            if ((m_AuthorityWorld.SolverCapabilities & m_Compatibility.AuthoritySolverRequiredCapabilities) !=
                m_Compatibility.AuthoritySolverRequiredCapabilities)
            {
                throw new InvalidOperationException("Authority World does not satisfy the locked ServerAuthoritative Solver capability contract.");
            }
        }

        public void ValidateBaselineIdentity(
            AuthoritativeActorBaseline baseline,
            ServerAuthoritativePredictionHistoryRecord firstHistory)
        {
            if (baseline == null)
                throw new ArgumentNullException(nameof(baseline));
            if (baseline.NumericProfile != m_Program.Manifest.NumericProfile ||
                !baseline.TargetAbiVersion.Equals(m_Program.Manifest.NumericProfile.AbiVersion) ||
                !string.Equals(baseline.StateCodecIdentity, CharacterSimulationStateCodec.CodecIdentity, StringComparison.Ordinal) ||
                !baseline.ProgramHash.Equals(m_Compatibility.ProgramHash) ||
                !baseline.LayoutHash.Equals(m_Compatibility.LayoutHash) ||
                !baseline.OperationSetVersion.Equals(m_Compatibility.OperationSetVersion) ||
                !baseline.SolverId.Equals(m_AuthorityWorld.SolverId) ||
                !string.Equals(baseline.SolverVersion, m_AuthorityWorld.SolverVersion, StringComparison.Ordinal) ||
                baseline.SolverCapabilities != m_AuthorityWorld.SolverCapabilities ||
                !baseline.WorldRevision.Equals(m_AuthorityWorld.WorldRevision))
            {
                throw new InvalidOperationException("Authority baseline does not match the locked Program, operation-set, or Solver identity.");
            }
            var actorSnapshot = new SimulationActorSnapshot(
                baseline.ActorId,
                m_Compatibility.ProgramId,
                baseline.ProgramHash,
                baseline.LayoutHash,
                baseline.StateHash,
                baseline.CopyCharacterStateBytes());
            _ = actorSnapshot.Decode(m_Program);
            if (firstHistory == null)
                return;
            if (firstHistory.Input.ActorId != baseline.ActorId ||
                !firstHistory.Character.ProgramHash.Equals(baseline.ProgramHash) ||
                !firstHistory.Character.LayoutHash.Equals(baseline.LayoutHash) ||
                !firstHistory.World.WorldRevision.Equals(baseline.WorldRevision))
            {
                throw new InvalidOperationException("Authority baseline identity does not match Prediction history.");
            }
        }

        public PredictionCorrectionDecision Decide(
            AuthoritativeActorBaseline baseline,
            ServerAuthoritativePredictionHistoryRecord local,
            int replayCount,
            ServerAuthoritativeModelPolicy policy)
        {
            if (baseline == null)
                throw new ArgumentNullException(nameof(baseline));
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));
            if (local == null)
            {
                if (policy.HardRecoveryPolicy == ServerAuthoritativeHardRecoveryPolicy.FailSession)
                    throw new InvalidOperationException($"Prediction history does not cover authority Tick '{baseline.AuthorityTick}'.");
                return new PredictionCorrectionDecision(
                    PredictionCorrectionDecisionKind.HardRecovery,
                    PredictionCorrectionReason.HistoryUnavailable,
                    baseline.AuthorityTick,
                    baseline.AuthorityTick,
                    default,
                    default,
                    Float32Scalar.Zero,
                    Float32Scalar.Zero);
            }
            Float32Scalar positionError = PositionError(local.Body, baseline.Body);
            Float32Scalar yawError = YawError(local.Body, baseline.Body);
            bool stateMatches = local.Character.StateHash.Equals(baseline.StateHash);
            bool positionMatches = positionError.Value <= policy.BodyPositionTolerance;
            bool yawMatches = yawError.Value <= policy.BodyYawToleranceDegrees;
            if (stateMatches && positionMatches && yawMatches)
            {
                return new PredictionCorrectionDecision(
                    PredictionCorrectionDecisionKind.NoCorrection,
                    PredictionCorrectionReason.StateAndBodyMatch,
                    baseline.AuthorityTick,
                    default,
                    default,
                    default,
                    positionError,
                    yawError);
            }
            if (replayCount > policy.MaximumReplayTicksPerOuterTick)
            {
                return new PredictionCorrectionDecision(
                    PredictionCorrectionDecisionKind.HardRecovery,
                    PredictionCorrectionReason.ReplayLimitExceeded,
                    baseline.AuthorityTick,
                    baseline.AuthorityTick,
                    default,
                    default,
                    positionError,
                    yawError);
            }
            PredictionCorrectionReason reason = !stateMatches
                ? PredictionCorrectionReason.CharacterStateMismatch
                : !positionMatches
                    ? PredictionCorrectionReason.BodyPositionMismatch
                    : PredictionCorrectionReason.BodyYawMismatch;
            return new PredictionCorrectionDecision(
                PredictionCorrectionDecisionKind.RestoreReplay,
                reason,
                baseline.AuthorityTick,
                baseline.AuthorityTick,
                replayCount == 0 ? default : new SimulationTick(checked(baseline.AuthorityTick.Value + 1)),
                replayCount == 0 ? default : new SimulationTick(checked(baseline.AuthorityTick.Value + (ulong)replayCount)),
                positionError,
                yawError);
        }

        public ServerAuthoritativePredictionRestorePlan BuildRestorePlan(
            ServerAuthoritativePredictionHistoryRecord local,
            AuthoritativeActorBaseline baseline,
            PredictionCorrectionDecision decision,
            SimulationPipelineIdentity pipeline,
            byte[] correctionState,
            byte[] historyState,
            byte[] journalState)
        {
            if (local == null || baseline == null || decision == null ||
                decision.Kind == PredictionCorrectionDecisionKind.NoCorrection)
            {
                throw new ArgumentException("Prediction restore requires a local frame, corrective baseline, and decision.");
            }
            SimulationWorldSnapshot world = MergeWorld(local.World, baseline);
            SimulationPipelineStateSnapshot pipelineSnapshot = MergePipeline(
                local.PipelineProjection,
                pipeline,
                baseline.AuthorityTick.Value,
                correctionState,
                historyState,
                journalState);
            var snapshot = new Float32SimulationSessionSnapshot(local.CompositionIdentity, world, pipelineSnapshot);
            string snapshotId = $"server-authoritative/{baseline.ActorId}/{baseline.AuthorityTick}/{snapshot.SnapshotHash}";
            var directive = new SimulationRestoreDirective(
                snapshotId,
                baseline.AuthorityTick,
                world.ProgramCatalogHash,
                pipeline.Hash,
                pipelineSnapshot.Backend.ComponentId,
                pipelineSnapshot.Backend.SemanticVersion,
                snapshot.SnapshotHash);
            return new ServerAuthoritativePredictionRestorePlan(snapshotId, snapshot, directive);
        }

        SimulationWorldSnapshot MergeWorld(SimulationWorldSnapshot local, AuthoritativeActorBaseline baseline)
        {
            SimulationActorSnapshot localActor = local.Actors[0];
            if (localActor.ActorId != baseline.ActorId ||
                !localActor.ProgramHash.Equals(baseline.ProgramHash) ||
                !localActor.LayoutHash.Equals(baseline.LayoutHash))
            {
                throw new InvalidOperationException("Authority baseline cannot replace another Program/Actor history frame.");
            }
            var actor = new SimulationActorSnapshot(
                baseline.ActorId,
                localActor.ProgramId,
                baseline.ProgramHash,
                baseline.LayoutHash,
                baseline.StateHash,
                baseline.CopyCharacterStateBytes());
            WorldSimulationState localWorld = local.DecodeWorldState();
            var world = new WorldSimulationState(
                localWorld.NumericProfile,
                localWorld.SolverId,
                localWorld.SolverVersion,
                localWorld.WorldRevision,
                localWorld.PersistenceMode,
                new[] { baseline.Body },
                localWorld.SolverStatePayload.ToArray());
            return new SimulationWorldSnapshot(
                local.NumericProfile,
                local.ProgramCatalogHash,
                local.SolverId,
                local.SolverVersion,
                local.WorldRevision,
                baseline.AuthorityTick,
                new[] { actor },
                WorldSimulationStateCodec.Write(world),
                false);
        }

        static SimulationPipelineStateSnapshot MergePipeline(
            SimulationPipelineStateSnapshot projection,
            SimulationPipelineIdentity pipeline,
            ulong tick,
            byte[] correctionState,
            byte[] historyState,
            byte[] journalState)
        {
            var participants = new List<SimulationPipelinePassStateSnapshot>();
            for (int i = 0; i < projection.Participants.Count; i++)
            {
                SimulationPipelinePassStateSnapshot participant = projection.Participants[i];
                if (!ServerAuthoritativePredictionPassIds.IsPredictionStatePass(participant.PassId))
                    participants.Add(participant);
            }
            participants.Add(ServerAuthoritativePredictionStateSnapshot.Create(
                ServerAuthoritativePredictionPassIds.CorrectionSchedule,
                ServerAuthoritativePredictionPassIds.CorrectionStateOwner,
                ServerAuthoritativePredictionPassIds.CorrectionStateSchema,
                correctionState,
                3));
            participants.Add(ServerAuthoritativePredictionStateSnapshot.Create(
                ServerAuthoritativePredictionPassIds.HistoryEgress,
                ServerAuthoritativePredictionPassIds.HistoryStateOwner,
                ServerAuthoritativePredictionPassIds.HistoryStateSchema,
                historyState,
                ServerAuthoritativePredictionPassIds.HistoryStateSchemaVersion));
            participants.Add(ServerAuthoritativePredictionStateSnapshot.Create(
                ServerAuthoritativePredictionPassIds.OutputDisposition,
                ServerAuthoritativePredictionPassIds.JournalStateOwner,
                ServerAuthoritativePredictionPassIds.JournalStateSchema,
                journalState,
                ServerAuthoritativePredictionPassIds.JournalStateSchemaVersion));
            return new SimulationPipelineStateSnapshot(pipeline, projection.Backend, tick, participants);
        }

        static Float32Scalar PositionError(WorldBodyState left, WorldBodyState right)
        {
            float x = left.Position.X.Value - right.Position.X.Value;
            float y = left.Position.Y.Value - right.Position.Y.Value;
            float z = left.Position.Z.Value - right.Position.Z.Value;
            return Float32Scalar.FromSingle(MathF.Sqrt(x * x + y * y + z * z));
        }

        static Float32Scalar YawError(WorldBodyState left, WorldBodyState right)
        {
            float delta = MathF.Abs(left.Yaw.Degrees.Value - right.Yaw.Degrees.Value) % 360f;
            return Float32Scalar.FromSingle(delta > 180f ? 360f - delta : delta);
        }
    }

    internal sealed class ServerAuthoritativePredictionRestorePlan
    {
        public ServerAuthoritativePredictionRestorePlan(
            string snapshotId,
            Float32SimulationSessionSnapshot snapshot,
            SimulationRestoreDirective directive)
        {
            SnapshotId = snapshotId;
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            Directive = directive ?? throw new ArgumentNullException(nameof(directive));
        }

        public string SnapshotId { get; }
        public Float32SimulationSessionSnapshot Snapshot { get; }
        public SimulationRestoreDirective Directive { get; }
    }
}
