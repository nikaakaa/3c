using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;

namespace ThirdPersonSimulation
{
    public static class SimulationCanonicalPayloadHash
    {
        public static StableHash Compute(byte[] payload)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(payload);
            var characters = new char[hash.Length * 2];
            const string hex = "0123456789abcdef";
            for (int i = 0; i < hash.Length; i++)
            {
                characters[i * 2] = hex[hash[i] >> 4];
                characters[i * 2 + 1] = hex[hash[i] & 15];
            }
            return new StableHash(new string(characters));
        }
    }

    public readonly struct SimulationPipelineStateParticipantIdentity : IEquatable<SimulationPipelineStateParticipantIdentity>
    {
        public SimulationPipelineStateParticipantIdentity(
            SimulationPipelinePassId passId,
            SimulationPipelinePassImplementationVersion implementationVersion,
            string stateOwner,
            string stateSchemaId,
            int stateSchemaVersion)
        {
            if (!passId.IsValid || !implementationVersion.IsValid || stateSchemaVersion <= 0)
                throw new ArgumentException("Pipeline state participant identity is incomplete.");
            PassId = passId;
            ImplementationVersion = implementationVersion;
            StateOwner = SimulationIdentity.Require(stateOwner, nameof(stateOwner));
            StateSchemaId = SimulationIdentity.Require(stateSchemaId, nameof(stateSchemaId));
            StateSchemaVersion = stateSchemaVersion;
        }

        public SimulationPipelinePassId PassId { get; }
        public SimulationPipelinePassImplementationVersion ImplementationVersion { get; }
        public string StateOwner { get; }
        public string StateSchemaId { get; }
        public int StateSchemaVersion { get; }

        public bool Equals(SimulationPipelineStateParticipantIdentity other)
        {
            return PassId.Equals(other.PassId) && ImplementationVersion.Equals(other.ImplementationVersion) &&
                   string.Equals(StateOwner, other.StateOwner, StringComparison.Ordinal) &&
                   string.Equals(StateSchemaId, other.StateSchemaId, StringComparison.Ordinal) &&
                   StateSchemaVersion == other.StateSchemaVersion;
        }

        public override bool Equals(object obj) => obj is SimulationPipelineStateParticipantIdentity other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(PassId, ImplementationVersion, StateOwner, StateSchemaId, StateSchemaVersion);
    }

    public sealed class SimulationPipelineStateSnapshot
    {
        readonly ReadOnlyCollection<SimulationPipelinePassStateSnapshot> m_Participants;

        public SimulationPipelineStateSnapshot(
            SimulationPipelineIdentity pipeline,
            SimulationComponentIdentity backend,
            ulong lastCompletedTick,
            IEnumerable<SimulationPipelinePassStateSnapshot> participants)
        {
            if (!pipeline.IsValid || !backend.IsValid || backend.Role != SimulationComponentRole.ExecutionBackend)
                throw new ArgumentException("Pipeline state snapshot identity is incomplete.");
            var values = participants == null
                ? new List<SimulationPipelinePassStateSnapshot>()
                : new List<SimulationPipelinePassStateSnapshot>(participants);
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null)
                    throw new ArgumentException("Pipeline state snapshot contains a missing participant.", nameof(participants));
            }
            values.Sort((left, right) => left.PassId.CompareTo(right.PassId));
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0 && values[i - 1].PassId.Equals(values[i].PassId))
                    throw new ArgumentException("Pipeline state snapshot contains a missing or duplicate participant.", nameof(participants));
            }
            Pipeline = pipeline;
            Backend = backend;
            LastCompletedTick = lastCompletedTick;
            m_Participants = values.AsReadOnly();
            SnapshotHash = ComputeHash();
        }

        public SimulationPipelineIdentity Pipeline { get; }
        public SimulationComponentIdentity Backend { get; }
        public ulong LastCompletedTick { get; }
        public IReadOnlyList<SimulationPipelinePassStateSnapshot> Participants => m_Participants;
        public StableHash SnapshotHash { get; }

        StableHash ComputeHash()
        {
            var values = new string[m_Participants.Count + 5];
            values[0] = "simulation-pipeline-state-snapshot/1";
            values[1] = Pipeline.ToString();
            values[2] = Backend.ComponentId;
            values[3] = Backend.SemanticVersion;
            values[4] = LastCompletedTick.ToString(CultureInfo.InvariantCulture);
            for (int i = 0; i < m_Participants.Count; i++)
            {
                SimulationPipelinePassStateSnapshot participant = m_Participants[i];
                values[i + 5] = $"{participant.PassId}:{participant.ImplementationVersion}:{participant.StateOwner}:{participant.StateSchemaId}:{participant.StateSchemaVersion}:{participant.StateHash}";
            }
            return StableHash.Compute(values);
        }
    }

    public interface ISimulationPipelinePassRestoreTransaction : IDisposable
    {
        SimulationPipelineStateParticipantIdentity Participant { get; }
        void Apply();
        void ValidateApplied();
        void CompleteAfterSessionPublish();
        void Rollback();
    }

    public interface ISimulationPipelineStateParticipant
    {
        SimulationPipelineStateParticipantIdentity StateIdentity { get; }
        SimulationPipelineStepProjectionMode StepProjectionMode { get; }
        ISimulationPipelinePassStateCheckpoint CaptureCheckpoint();
        SimulationPipelinePassStateSnapshot CaptureState();
        ISimulationPipelinePassRestoreTransaction PrepareRestore(SimulationPipelinePassStateSnapshot snapshot);
    }

    public enum SimulationPipelineStepProjectionMode : byte
    {
        Include = 1,
        ReconstructForRestore = 2
    }

    public interface ISimulationPipelinePassStateCheckpoint : IDisposable
    {
        SimulationPipelineStateParticipantIdentity Participant { get; }
        void Restore();
    }

    public sealed class SimulationPipelinePassStateCheckpoint : ISimulationPipelinePassStateCheckpoint
    {
        Action m_Restore;
        bool m_Restored;

        public SimulationPipelinePassStateCheckpoint(
            SimulationPipelineStateParticipantIdentity participant,
            Action restore)
        {
            Participant = participant;
            m_Restore = restore ?? throw new ArgumentNullException(nameof(restore));
        }

        public SimulationPipelineStateParticipantIdentity Participant { get; }

        public void Restore()
        {
            if (m_Restore == null)
                throw new ObjectDisposedException(nameof(SimulationPipelinePassStateCheckpoint));
            if (m_Restored)
                return;
            m_Restore();
            m_Restored = true;
        }

        public void Dispose()
        {
            m_Restore = null;
        }
    }

    public sealed class SimulationPipelineStateCheckpointSet : IDisposable
    {
        readonly IReadOnlyList<ISimulationPipelinePassStateCheckpoint> m_Checkpoints;
        bool m_Disposed;

        internal SimulationPipelineStateCheckpointSet(
            IReadOnlyList<ISimulationPipelinePassStateCheckpoint> checkpoints)
        {
            m_Checkpoints = checkpoints ?? throw new ArgumentNullException(nameof(checkpoints));
        }

        public void Restore()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(SimulationPipelineStateCheckpointSet));
            Exception failure = null;
            for (int i = m_Checkpoints.Count - 1; i >= 0; i--)
            {
                try
                {
                    m_Checkpoints[i].Restore();
                }
                catch (Exception exception)
                {
                    failure ??= exception;
                }
            }
            if (failure != null)
                throw new InvalidOperationException("Pipeline state checkpoint restore failed.", failure);
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            for (int i = m_Checkpoints.Count - 1; i >= 0; i--)
                m_Checkpoints[i].Dispose();
        }
    }

    public enum SimulationSessionRestoreParticipantKind : byte
    {
        Character = 1,
        World = 2,
        Pipeline = 3
    }

    public interface ISimulationSessionRestoreParticipantTransaction : IDisposable
    {
        SimulationSessionRestoreParticipantKind Kind { get; }
        string Identity { get; }
        void Apply();
        void ValidateApplied();
        void CompleteAfterSessionPublish();
        void Rollback();
    }

    public sealed class SimulationPipelineStateRestoreTransaction : ISimulationSessionRestoreParticipantTransaction
    {
        readonly ReadOnlyCollection<ISimulationPipelinePassRestoreTransaction> m_Participants;
        int m_AppliedCount;
        bool m_Validated;
        bool m_Completed;

        public SimulationPipelineStateRestoreTransaction(
            SimulationPipelineStateSnapshot snapshot,
            IEnumerable<ISimulationPipelinePassRestoreTransaction> participants)
        {
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            var values = participants == null
                ? new List<ISimulationPipelinePassRestoreTransaction>()
                : new List<ISimulationPipelinePassRestoreTransaction>(participants);
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null)
                    throw new ArgumentException("Pipeline restore transaction contains a missing participant.", nameof(participants));
            }
            values.Sort((left, right) => left.Participant.PassId.CompareTo(right.Participant.PassId));
            if (values.Count != snapshot.Participants.Count)
                throw new ArgumentException("Pipeline restore transaction participant count does not match the snapshot.", nameof(participants));
            for (int i = 0; i < values.Count; i++)
            {
                SimulationPipelinePassStateSnapshot state = snapshot.Participants[i];
                SimulationPipelineStateParticipantIdentity participant = values[i].Participant;
                if (!participant.PassId.Equals(state.PassId) ||
                    !participant.ImplementationVersion.Equals(state.ImplementationVersion) ||
                    !string.Equals(participant.StateOwner, state.StateOwner, StringComparison.Ordinal) ||
                    !string.Equals(participant.StateSchemaId, state.StateSchemaId, StringComparison.Ordinal) ||
                    participant.StateSchemaVersion != state.StateSchemaVersion)
                {
                    throw new ArgumentException("Pipeline restore transaction participant order does not match the snapshot.", nameof(participants));
                }
            }
            m_Participants = values.AsReadOnly();
        }

        public SimulationPipelineStateSnapshot Snapshot { get; }
        public SimulationSessionRestoreParticipantKind Kind => SimulationSessionRestoreParticipantKind.Pipeline;
        public string Identity => Snapshot.SnapshotHash.ToString();

        public void Apply()
        {
            RequireOpen();
            if (m_AppliedCount != 0)
                throw new InvalidOperationException("Pipeline restore transaction is already applied.");
            m_Validated = false;
            try
            {
                for (int i = 0; i < m_Participants.Count; i++)
                {
                    m_Participants[i].Apply();
                    m_AppliedCount++;
                }
            }
            catch
            {
                Rollback();
                throw;
            }
        }

        public void ValidateApplied()
        {
            RequireOpen();
            if (m_AppliedCount != m_Participants.Count)
                throw new InvalidOperationException("Pipeline restore transaction is not fully applied.");
            for (int i = 0; i < m_Participants.Count; i++)
                m_Participants[i].ValidateApplied();
            m_Validated = true;
        }

        public void CompleteAfterSessionPublish()
        {
            RequireOpen();
            if (m_AppliedCount != m_Participants.Count || !m_Validated)
                throw new InvalidOperationException("Pipeline restore transaction was not applied and validated before Session publish.");
            for (int i = 0; i < m_Participants.Count; i++)
                m_Participants[i].CompleteAfterSessionPublish();
            m_Completed = true;
        }

        public void Rollback()
        {
            if (m_Completed)
                return;
            for (int i = m_AppliedCount - 1; i >= 0; i--)
                m_Participants[i].Rollback();
            m_AppliedCount = 0;
            m_Validated = false;
        }

        public void Dispose()
        {
            if (!m_Completed)
                Rollback();
            for (int i = m_Participants.Count - 1; i >= 0; i--)
                m_Participants[i].Dispose();
            m_Completed = true;
        }

        void RequireOpen()
        {
            if (m_Completed)
                throw new ObjectDisposedException(nameof(SimulationPipelineStateRestoreTransaction));
        }
    }

    public sealed class SimulationSessionRestoreTransaction : IDisposable
    {
        readonly ReadOnlyCollection<ISimulationSessionRestoreParticipantTransaction> m_Participants;
        int m_AppliedCount;
        bool m_Validated;
        bool m_Completed;

        public SimulationSessionRestoreTransaction(IEnumerable<ISimulationSessionRestoreParticipantTransaction> participants)
        {
            var values = participants == null
                ? new List<ISimulationSessionRestoreParticipantTransaction>()
                : new List<ISimulationSessionRestoreParticipantTransaction>(participants);
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null)
                    throw new ArgumentException("Session restore transaction contains a missing participant.", nameof(participants));
            }
            values.Sort((left, right) => left.Kind.CompareTo(right.Kind));
            if (values.Count != 3)
                throw new ArgumentException("Session restore requires exactly Character, World and Pipeline transactions.", nameof(participants));
            for (int i = 0; i < values.Count; i++)
            {
                SimulationSessionRestoreParticipantKind expected = (SimulationSessionRestoreParticipantKind)(i + 1);
                if (values[i].Kind != expected || string.IsNullOrEmpty(values[i].Identity))
                    throw new ArgumentException("Session restore participant set is incomplete or duplicated.", nameof(participants));
            }
            m_Participants = values.AsReadOnly();
        }

        public void ApplyAndValidate()
        {
            RequireOpen();
            if (m_AppliedCount != 0)
                throw new InvalidOperationException("Session restore transaction is already applied.");
            m_Validated = false;
            try
            {
                for (int i = 0; i < m_Participants.Count; i++)
                {
                    m_Participants[i].Apply();
                    m_AppliedCount++;
                }
                for (int i = 0; i < m_Participants.Count; i++)
                    m_Participants[i].ValidateApplied();
                m_Validated = true;
            }
            catch
            {
                Rollback();
                throw;
            }
        }

        public void CompleteAfterAtomicSessionPublish()
        {
            RequireOpen();
            if (m_AppliedCount != m_Participants.Count || !m_Validated)
                throw new InvalidOperationException("Session restore transaction is not fully applied and validated.");
            for (int i = 0; i < m_Participants.Count; i++)
                m_Participants[i].CompleteAfterSessionPublish();
            m_Completed = true;
        }

        public void Rollback()
        {
            if (m_Completed)
                return;
            for (int i = m_AppliedCount - 1; i >= 0; i--)
                m_Participants[i].Rollback();
            m_AppliedCount = 0;
            m_Validated = false;
        }

        public void Dispose()
        {
            if (!m_Completed)
                Rollback();
            for (int i = m_Participants.Count - 1; i >= 0; i--)
                m_Participants[i].Dispose();
            m_Completed = true;
        }

        void RequireOpen()
        {
            if (m_Completed)
                throw new ObjectDisposedException(nameof(SimulationSessionRestoreTransaction));
        }
    }

    public static class SimulationPipelineStateSnapshotCoordinator
    {
        public static SimulationPipelineStateCheckpointSet CaptureCheckpoints(
            CompiledSimulationPipelinePlan plan,
            IEnumerable<ISimulationPipelineStateParticipant> participants)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            List<ISimulationPipelineStateParticipant> values = ValidateParticipantSet(plan, participants);
            var checkpoints = new List<ISimulationPipelinePassStateCheckpoint>(values.Count);
            try
            {
                for (int i = 0; i < values.Count; i++)
                {
                    ISimulationPipelinePassStateCheckpoint checkpoint = values[i].CaptureCheckpoint() ??
                        throw Failure("pipeline_state_checkpoint_missing", values[i].StateIdentity.PassId, "State participant returned no transaction checkpoint.");
                    if (!checkpoint.Participant.Equals(values[i].StateIdentity))
                        throw Failure("pipeline_state_checkpoint_identity_mismatch", values[i].StateIdentity.PassId, "State checkpoint identity does not match its participant.");
                    checkpoints.Add(checkpoint);
                }
                return new SimulationPipelineStateCheckpointSet(checkpoints.AsReadOnly());
            }
            catch
            {
                for (int i = checkpoints.Count - 1; i >= 0; i--)
                    checkpoints[i].Dispose();
                throw;
            }
        }

        public static SimulationPipelineStateSnapshot Capture(
            CompiledSimulationPipelinePlan plan,
            ulong lastCompletedTick,
            IEnumerable<ISimulationPipelineStateParticipant> participants)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            List<ISimulationPipelineStateParticipant> values = ValidateParticipantSet(plan, participants);
            var snapshots = new List<SimulationPipelinePassStateSnapshot>(values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                SimulationPipelinePassStateSnapshot snapshot = values[i].CaptureState() ??
                    throw Failure("pipeline_state_capture_missing", values[i].StateIdentity.PassId, "State participant returned no snapshot.");
                RequireSnapshotIdentity(values[i].StateIdentity, snapshot);
                snapshots.Add(snapshot);
            }
            return new SimulationPipelineStateSnapshot(plan.Identity, plan.Backend, lastCompletedTick, snapshots);
        }

        public static SimulationPipelineStateSnapshot CaptureStepProjection(
            CompiledSimulationPipelinePlan plan,
            ulong lastCompletedTick,
            IEnumerable<ISimulationPipelineStateParticipant> participants)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            List<ISimulationPipelineStateParticipant> values = ValidateParticipantSet(plan, participants);
            var snapshots = new List<SimulationPipelinePassStateSnapshot>(values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                if (!Enum.IsDefined(typeof(SimulationPipelineStepProjectionMode), values[i].StepProjectionMode))
                    throw Failure("pipeline_step_projection_mode_invalid", values[i].StateIdentity.PassId, "State participant Step projection mode is invalid.");
                if (values[i].StepProjectionMode == SimulationPipelineStepProjectionMode.ReconstructForRestore)
                    continue;
                SimulationPipelinePassStateSnapshot snapshot = values[i].CaptureState() ??
                    throw Failure("pipeline_state_capture_missing", values[i].StateIdentity.PassId, "State participant returned no snapshot.");
                RequireSnapshotIdentity(values[i].StateIdentity, snapshot);
                snapshots.Add(snapshot);
            }
            return new SimulationPipelineStateSnapshot(plan.Identity, plan.Backend, lastCompletedTick, snapshots);
        }

        public static SimulationPipelineStateRestoreTransaction PrepareRestore(
            CompiledSimulationPipelinePlan plan,
            SimulationPipelineStateSnapshot snapshot,
            IEnumerable<ISimulationPipelineStateParticipant> participants)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (!snapshot.Pipeline.Equals(plan.Identity) || !snapshot.Backend.Equals(plan.Backend))
                throw Failure("pipeline_snapshot_identity_mismatch", default, "Pipeline snapshot identity does not match the compiled plan and Backend.");
            List<ISimulationPipelineStateParticipant> values = ValidateParticipantSet(plan, participants);
            if (values.Count != snapshot.Participants.Count)
                throw Failure("pipeline_snapshot_participant_count_mismatch", default, "Pipeline snapshot participant count does not match the compiled plan.");
            var transactions = new List<ISimulationPipelinePassRestoreTransaction>(values.Count);
            try
            {
                for (int i = 0; i < values.Count; i++)
                {
                    SimulationPipelinePassStateSnapshot state = snapshot.Participants[i];
                    RequireSnapshotIdentity(values[i].StateIdentity, state);
                    ISimulationPipelinePassRestoreTransaction transaction = values[i].PrepareRestore(state) ??
                        throw Failure("pipeline_state_restore_prepare_missing", values[i].StateIdentity.PassId, "State participant returned no restore transaction.");
                    if (!transaction.Participant.Equals(values[i].StateIdentity))
                        throw Failure("pipeline_state_restore_participant_mismatch", values[i].StateIdentity.PassId, "Prepared restore transaction identity does not match its participant.");
                    transactions.Add(transaction);
                }
                return new SimulationPipelineStateRestoreTransaction(snapshot, transactions);
            }
            catch
            {
                for (int i = transactions.Count - 1; i >= 0; i--)
                    transactions[i].Dispose();
                throw;
            }
        }

        static List<ISimulationPipelineStateParticipant> ValidateParticipantSet(
            CompiledSimulationPipelinePlan plan,
            IEnumerable<ISimulationPipelineStateParticipant> participants)
        {
            var expected = new List<CompiledSimulationPipelinePass>();
            for (int i = 0; i < plan.Passes.Count; i++)
            {
                if (plan.Passes[i].Descriptor.StateClass == SimulationPipelinePassStateClass.SnapshotParticipant)
                    expected.Add(plan.Passes[i]);
            }
            var values = participants == null
                ? new List<ISimulationPipelineStateParticipant>()
                : new List<ISimulationPipelineStateParticipant>(participants);
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null)
                    throw Failure("pipeline_state_participant_missing", default, "Runtime Pipeline state participant is missing.");
            }
            values.Sort((left, right) => left.StateIdentity.PassId.CompareTo(right.StateIdentity.PassId));
            expected.Sort((left, right) => left.Descriptor.PassId.CompareTo(right.Descriptor.PassId));
            if (values.Count != expected.Count)
                throw Failure("pipeline_state_participant_count_mismatch", default, "Runtime Pipeline state participant count does not match the compiled plan.");
            for (int i = 0; i < values.Count; i++)
            {
                SimulationPipelineStateParticipantIdentity actual = values[i].StateIdentity;
                CompiledSimulationPipelinePass required = expected[i];
                if (!actual.PassId.Equals(required.Descriptor.PassId) ||
                    !actual.ImplementationVersion.Equals(required.Descriptor.ImplementationVersion) ||
                    !string.Equals(actual.StateOwner, required.Descriptor.StateOwner, StringComparison.Ordinal) ||
                    !string.Equals(actual.StateSchemaId, required.Factory.StateSchemaId, StringComparison.Ordinal) ||
                    actual.StateSchemaVersion != required.Factory.StateSchemaVersion)
                {
                    throw Failure("pipeline_state_participant_identity_mismatch", required.Descriptor.PassId, "Runtime Pipeline state participant version or schema does not match the compiled plan.");
                }
                if (i > 0 && values[i - 1].StateIdentity.PassId.Equals(actual.PassId))
                    throw Failure("pipeline_state_participant_duplicate", actual.PassId, "Runtime Pipeline state participant is duplicated.");
            }
            return values;
        }

        static void RequireSnapshotIdentity(
            SimulationPipelineStateParticipantIdentity expected,
            SimulationPipelinePassStateSnapshot snapshot)
        {
            if (!snapshot.PassId.Equals(expected.PassId) ||
                !snapshot.ImplementationVersion.Equals(expected.ImplementationVersion) ||
                !string.Equals(snapshot.StateOwner, expected.StateOwner, StringComparison.Ordinal) ||
                !string.Equals(snapshot.StateSchemaId, expected.StateSchemaId, StringComparison.Ordinal) ||
                snapshot.StateSchemaVersion != expected.StateSchemaVersion)
            {
                throw Failure("pipeline_state_snapshot_version_mismatch", expected.PassId, "Pipeline state snapshot participant version or schema does not match.");
            }
            StableHash payloadHash = SimulationCanonicalPayloadHash.Compute(snapshot.CopyPayload());
            if (!payloadHash.Equals(snapshot.StateHash))
                throw Failure("pipeline_state_snapshot_payload_hash_mismatch", expected.PassId, "Pipeline state snapshot payload hash does not match canonical bytes.");
        }

        static SimulationSessionCompositionException Failure(string code, SimulationPipelinePassId passId, string message)
        {
            return new SimulationSessionCompositionException(new SimulationSessionFailure(
                SimulationSessionFailureStage.Runtime,
                code,
                message,
                passIdentity: passId.ToString()));
        }
    }
}
