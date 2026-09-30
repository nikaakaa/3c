using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;

namespace ThirdPersonSimulation
{
    public static class SimulationCanonicalPayloadHash
    {
        [ThreadStatic] static SHA256 s_SharedHasher;

        public static StableHash Compute(byte[] payload)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            return Compute(payload.AsSpan());
        }

        public static StableHash Compute(ReadOnlySpan<byte> payload)
        {
            SHA256 sha = s_SharedHasher ??= SHA256.Create();
            Span<byte> hash = stackalloc byte[32];
            if (!sha.TryComputeHash(payload, hash, out int written) || written != hash.Length)
                throw new CryptographicException("SHA-256 did not produce a complete digest.");
            return new StableHash(hash);
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
        readonly IReadOnlyList<SimulationPipelinePassStateSnapshot> m_Participants;

        [ThreadStatic] static CanonicalWriter s_HashWriter;

        private SimulationPipelineStateSnapshot(
            SimulationPipelineIdentity pipeline,
            SimulationComponentIdentity backend,
            ulong lastCompletedTick,
            SimulationPipelinePassStateSnapshot[] participants)
        {
            if (!pipeline.IsValid || !backend.IsValid || backend.Role != SimulationComponentRole.ExecutionBackend)
                throw new ArgumentException("Pipeline state snapshot identity is incomplete.");
            SimulationPipelinePassStateSnapshot[] values = participants ??
                Array.Empty<SimulationPipelinePassStateSnapshot>();
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == null)
                    throw new ArgumentException("Pipeline state snapshot contains a missing participant.", nameof(participants));
            }
            Array.Sort(values, (left, right) => left.PassId.CompareTo(right.PassId));
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0 && values[i - 1].PassId.Equals(values[i].PassId))
                    throw new ArgumentException("Pipeline state snapshot contains a missing or duplicate participant.", nameof(participants));
            }
            Pipeline = pipeline;
            Backend = backend;
            LastCompletedTick = lastCompletedTick;
            m_Participants = values;
            SnapshotHash = ComputeHash();
        }

        public SimulationPipelineIdentity Pipeline { get; }
        public SimulationComponentIdentity Backend { get; }
        public ulong LastCompletedTick { get; }
        public IReadOnlyList<SimulationPipelinePassStateSnapshot> Participants => m_Participants;
        public StableHash SnapshotHash { get; }

        public static SimulationPipelineStateSnapshot FromOwnedParticipants(
            SimulationPipelineIdentity pipeline,
            SimulationComponentIdentity backend,
            ulong lastCompletedTick,
            SimulationPipelinePassStateSnapshot[] ownedParticipants) =>
            new SimulationPipelineStateSnapshot(pipeline, backend, lastCompletedTick, ownedParticipants);

        StableHash ComputeHash()
        {
            CanonicalWriter writer = HashWriter();
            Span<char> number = stackalloc char[20];
            writer.WriteRawUtf8("simulation-pipeline-state-snapshot/1");
            writer.WriteByte(0x1f);
            writer.WriteRawUtf8(Pipeline.Id.Value);
            writer.WriteRawUtf8("@");
            writer.WriteRawUtf8(Pipeline.Revision.Value);
            writer.WriteRawUtf8("/schema");
            Pipeline.SchemaVersion.Value.TryFormat(number, out int characterCount);
            writer.WriteRawUtf8(number.Slice(0, characterCount));
            writer.WriteRawUtf8("/");
            writer.WriteRawHash(Pipeline.Hash.Value);
            writer.WriteByte(0x1f);
            writer.WriteRawUtf8(Backend.ComponentId);
            writer.WriteByte(0x1f);
            writer.WriteRawUtf8(Backend.SemanticVersion);
            writer.WriteByte(0x1f);
            LastCompletedTick.TryFormat(number, out characterCount, provider: CultureInfo.InvariantCulture);
            writer.WriteRawUtf8(number.Slice(0, characterCount));
            for (int i = 0; i < m_Participants.Count; i++)
            {
                SimulationPipelinePassStateSnapshot participant = m_Participants[i];
                writer.WriteByte(0x1f);
                writer.WriteRawUtf8(participant.PassId.Value);
                writer.WriteRawUtf8(":");
                writer.WriteRawUtf8(participant.ImplementationVersion.Value);
                writer.WriteRawUtf8(":");
                writer.WriteRawUtf8(participant.StateOwner);
                writer.WriteRawUtf8(":");
                writer.WriteRawUtf8(participant.StateSchemaId);
                writer.WriteRawUtf8(":");
                participant.StateSchemaVersion.TryFormat(number, out characterCount);
                writer.WriteRawUtf8(number.Slice(0, characterCount));
                writer.WriteRawUtf8(":");
                writer.WriteRawHash(participant.StateHash);
            }
            return writer.ComputeHash();
        }

        static CanonicalWriter HashWriter()
        {
            if (s_HashWriter == null)
                s_HashWriter = new CanonicalWriter();
            s_HashWriter.Reset();
            return s_HashWriter;
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

    internal sealed class SimulationPipelineStateParticipantSet : IReadOnlyList<ISimulationPipelineStateParticipant>
    {
        public SimulationPipelineStateParticipantSet(
            CompiledSimulationPipelinePlan plan,
            ISimulationPipelineStateParticipant[] values)
        {
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            Values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public CompiledSimulationPipelinePlan Plan { get; }
        internal ISimulationPipelineStateParticipant[] Values { get; }
        public int Count => Values.Length;
        public ISimulationPipelineStateParticipant this[int index] => Values[index];
        public IEnumerator<ISimulationPipelineStateParticipant> GetEnumerator() =>
            ((IEnumerable<ISimulationPipelineStateParticipant>)Values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => Values.GetEnumerator();
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
        readonly ISimulationPipelinePassRestoreTransaction[] m_Participants;
        int m_AppliedCount;
        bool m_Validated;
        bool m_Completed;

        private SimulationPipelineStateRestoreTransaction(
            SimulationPipelineStateSnapshot snapshot,
            ISimulationPipelinePassRestoreTransaction[] ownedParticipants)
        {
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            ISimulationPipelinePassRestoreTransaction[] values = ownedParticipants ??
                throw new ArgumentNullException(nameof(ownedParticipants));
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == null)
                    throw new ArgumentException("Pipeline restore transaction contains a missing participant.", nameof(ownedParticipants));
            }
            Array.Sort(values, (left, right) => left.Participant.PassId.CompareTo(right.Participant.PassId));
            if (values.Length != snapshot.Participants.Count)
                throw new ArgumentException("Pipeline restore transaction participant count does not match the snapshot.", nameof(ownedParticipants));
            for (int i = 0; i < values.Length; i++)
            {
                SimulationPipelinePassStateSnapshot state = snapshot.Participants[i];
                SimulationPipelineStateParticipantIdentity participant = values[i].Participant;
                if (!participant.PassId.Equals(state.PassId) ||
                    !participant.ImplementationVersion.Equals(state.ImplementationVersion) ||
                    !string.Equals(participant.StateOwner, state.StateOwner, StringComparison.Ordinal) ||
                    !string.Equals(participant.StateSchemaId, state.StateSchemaId, StringComparison.Ordinal) ||
                    participant.StateSchemaVersion != state.StateSchemaVersion)
                {
                    throw new ArgumentException("Pipeline restore transaction participant order does not match the snapshot.", nameof(ownedParticipants));
                }
            }
            m_Participants = values;
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
                for (int i = 0; i < m_Participants.Length; i++)
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
            if (m_AppliedCount != m_Participants.Length)
                throw new InvalidOperationException("Pipeline restore transaction is not fully applied.");
            for (int i = 0; i < m_Participants.Length; i++)
                m_Participants[i].ValidateApplied();
            m_Validated = true;
        }

        public void CompleteAfterSessionPublish()
        {
            RequireOpen();
            if (m_AppliedCount != m_Participants.Length || !m_Validated)
                throw new InvalidOperationException("Pipeline restore transaction was not applied and validated before Session publish.");
            for (int i = 0; i < m_Participants.Length; i++)
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
            for (int i = m_Participants.Length - 1; i >= 0; i--)
                m_Participants[i].Dispose();
            m_Completed = true;
        }

        void RequireOpen()
        {
            if (m_Completed)
                throw new ObjectDisposedException(nameof(SimulationPipelineStateRestoreTransaction));
        }

        internal static SimulationPipelineStateRestoreTransaction FromOwnedTransactions(
            SimulationPipelineStateSnapshot snapshot,
            ISimulationPipelinePassRestoreTransaction[] ownedParticipants) =>
            new SimulationPipelineStateRestoreTransaction(snapshot, ownedParticipants);
    }

    public sealed class SimulationSessionRestoreTransaction : IDisposable
    {
        readonly ISimulationSessionRestoreParticipantTransaction[] m_Participants;
        int m_AppliedCount;
        bool m_Validated;
        bool m_Completed;

        private SimulationSessionRestoreTransaction(ISimulationSessionRestoreParticipantTransaction[] ownedParticipants)
        {
            ISimulationSessionRestoreParticipantTransaction[] values = ownedParticipants ??
                throw new ArgumentNullException(nameof(ownedParticipants));
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == null)
                    throw new ArgumentException("Session restore transaction contains a missing participant.", nameof(ownedParticipants));
            }
            Array.Sort(values, (left, right) => left.Kind.CompareTo(right.Kind));
            if (values.Length != 3)
                throw new ArgumentException("Session restore requires exactly Character, World and Pipeline transactions.", nameof(ownedParticipants));
            for (int i = 0; i < values.Length; i++)
            {
                SimulationSessionRestoreParticipantKind expected = (SimulationSessionRestoreParticipantKind)(i + 1);
                if (values[i].Kind != expected || string.IsNullOrEmpty(values[i].Identity))
                    throw new ArgumentException("Session restore participant set is incomplete or duplicated.", nameof(ownedParticipants));
            }
            m_Participants = values;
        }

        public void ApplyAndValidate()
        {
            RequireOpen();
            if (m_AppliedCount != 0)
                throw new InvalidOperationException("Session restore transaction is already applied.");
            m_Validated = false;
            try
            {
                for (int i = 0; i < m_Participants.Length; i++)
                {
                    m_Participants[i].Apply();
                    m_AppliedCount++;
                }
                for (int i = 0; i < m_Participants.Length; i++)
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
            if (m_AppliedCount != m_Participants.Length || !m_Validated)
                throw new InvalidOperationException("Session restore transaction is not fully applied and validated.");
            for (int i = 0; i < m_Participants.Length; i++)
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
            for (int i = m_Participants.Length - 1; i >= 0; i--)
                m_Participants[i].Dispose();
            m_Completed = true;
        }

        void RequireOpen()
        {
            if (m_Completed)
                throw new ObjectDisposedException(nameof(SimulationSessionRestoreTransaction));
        }

        public static SimulationSessionRestoreTransaction FromOwnedTransactions(
            ISimulationSessionRestoreParticipantTransaction[] ownedParticipants) =>
            new SimulationSessionRestoreTransaction(ownedParticipants);
    }

    public static class SimulationPipelineStateSnapshotCoordinator
    {
        internal static IReadOnlyList<ISimulationPipelineStateParticipant> CreateParticipantSet(
            CompiledSimulationPipelinePlan plan,
            IReadOnlyList<ISimulationPipelineStateParticipant> participants)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            return new SimulationPipelineStateParticipantSet(
                plan,
                ValidateParticipantSet(plan, participants));
        }

        public static SimulationPipelineStateCheckpointSet CaptureCheckpoints(
            CompiledSimulationPipelinePlan plan,
            IReadOnlyList<ISimulationPipelineStateParticipant> participants)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            ISimulationPipelineStateParticipant[] values = ValidateParticipantSet(plan, participants);
            var checkpoints = values.Length == 0
                ? Array.Empty<ISimulationPipelinePassStateCheckpoint>()
                : new ISimulationPipelinePassStateCheckpoint[values.Length];
            int checkpointCount = 0;
            try
            {
                for (int i = 0; i < values.Length; i++)
                {
                    ISimulationPipelinePassStateCheckpoint checkpoint = values[i].CaptureCheckpoint() ??
                        throw Failure("pipeline_state_checkpoint_missing", values[i].StateIdentity.PassId, "State participant returned no transaction checkpoint.");
                    if (!checkpoint.Participant.Equals(values[i].StateIdentity))
                        throw Failure("pipeline_state_checkpoint_identity_mismatch", values[i].StateIdentity.PassId, "State checkpoint identity does not match its participant.");
                    checkpoints[checkpointCount++] = checkpoint;
                }
                return new SimulationPipelineStateCheckpointSet(checkpoints);
            }
            catch
            {
                for (int i = checkpointCount - 1; i >= 0; i--)
                    checkpoints[i].Dispose();
                throw;
            }
        }

        public static SimulationPipelineStateSnapshot Capture(
            CompiledSimulationPipelinePlan plan,
            ulong lastCompletedTick,
            IReadOnlyList<ISimulationPipelineStateParticipant> participants)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            ISimulationPipelineStateParticipant[] values = ValidateParticipantSet(plan, participants);
            var snapshots = values.Length == 0
                ? Array.Empty<SimulationPipelinePassStateSnapshot>()
                : new SimulationPipelinePassStateSnapshot[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                SimulationPipelinePassStateSnapshot snapshot = values[i].CaptureState() ??
                    throw Failure("pipeline_state_capture_missing", values[i].StateIdentity.PassId, "State participant returned no snapshot.");
                RequireSnapshotIdentity(values[i].StateIdentity, snapshot);
                snapshots[i] = snapshot;
            }
            return SimulationPipelineStateSnapshot.FromOwnedParticipants(plan.Identity, plan.Backend, lastCompletedTick, snapshots);
        }

        public static SimulationPipelineStateSnapshot CaptureStepProjection(
            CompiledSimulationPipelinePlan plan,
            ulong lastCompletedTick,
            IReadOnlyList<ISimulationPipelineStateParticipant> participants)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            ISimulationPipelineStateParticipant[] values = ValidateParticipantSet(plan, participants);
            int snapshotCount = 0;
            for (int i = 0; i < values.Length; i++)
            {
                SimulationPipelineStepProjectionMode projectionMode = values[i].StepProjectionMode;
                if ((byte)projectionMode < (byte)SimulationPipelineStepProjectionMode.Include ||
                    (byte)projectionMode > (byte)SimulationPipelineStepProjectionMode.ReconstructForRestore)
                {
                    throw Failure("pipeline_step_projection_mode_invalid", values[i].StateIdentity.PassId, "State participant Step projection mode is invalid.");
                }
                if (projectionMode == SimulationPipelineStepProjectionMode.Include)
                    snapshotCount++;
            }
            var snapshots = snapshotCount == 0
                ? Array.Empty<SimulationPipelinePassStateSnapshot>()
                : new SimulationPipelinePassStateSnapshot[snapshotCount];
            int snapshotIndex = 0;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].StepProjectionMode == SimulationPipelineStepProjectionMode.ReconstructForRestore)
                    continue;
                SimulationPipelinePassStateSnapshot snapshot = values[i].CaptureState() ??
                    throw Failure("pipeline_state_capture_missing", values[i].StateIdentity.PassId, "State participant returned no snapshot.");
                RequireSnapshotIdentity(values[i].StateIdentity, snapshot);
                snapshots[snapshotIndex++] = snapshot;
            }
            return SimulationPipelineStateSnapshot.FromOwnedParticipants(plan.Identity, plan.Backend, lastCompletedTick, snapshots);
        }

        public static SimulationPipelineStateRestoreTransaction PrepareRestore(
            CompiledSimulationPipelinePlan plan,
            SimulationPipelineStateSnapshot snapshot,
            IReadOnlyList<ISimulationPipelineStateParticipant> participants)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            if (!snapshot.Pipeline.Equals(plan.Identity) || !snapshot.Backend.Equals(plan.Backend))
                throw Failure("pipeline_snapshot_identity_mismatch", default, "Pipeline snapshot identity does not match the compiled plan and Backend.");
            ISimulationPipelineStateParticipant[] values = ValidateParticipantSet(plan, participants);
            if (values.Length != snapshot.Participants.Count)
                throw Failure("pipeline_snapshot_participant_count_mismatch", default, "Pipeline snapshot participant count does not match the compiled plan.");
            var transactions = values.Length == 0
                ? Array.Empty<ISimulationPipelinePassRestoreTransaction>()
                : new ISimulationPipelinePassRestoreTransaction[values.Length];
            int preparedCount = 0;
            try
            {
                for (int i = 0; i < values.Length; i++)
                {
                    SimulationPipelinePassStateSnapshot state = snapshot.Participants[i];
                    RequireSnapshotIdentity(values[i].StateIdentity, state);
                    ISimulationPipelinePassRestoreTransaction transaction = values[i].PrepareRestore(state) ??
                        throw Failure("pipeline_state_restore_prepare_missing", values[i].StateIdentity.PassId, "State participant returned no restore transaction.");
                    if (!transaction.Participant.Equals(values[i].StateIdentity))
                        throw Failure("pipeline_state_restore_participant_mismatch", values[i].StateIdentity.PassId, "Prepared restore transaction identity does not match its participant.");
                    transactions[preparedCount++] = transaction;
                }
                return SimulationPipelineStateRestoreTransaction.FromOwnedTransactions(snapshot, transactions);
            }
            catch
            {
                for (int i = preparedCount - 1; i >= 0; i--)
                    transactions[i].Dispose();
                throw;
            }
        }

        static ISimulationPipelineStateParticipant[] ValidateParticipantSet(
            CompiledSimulationPipelinePlan plan,
            IReadOnlyList<ISimulationPipelineStateParticipant> participants)
        {
            if (participants is SimulationPipelineStateParticipantSet participantSet)
            {
                if (!ReferenceEquals(participantSet.Plan, plan))
                    throw Failure("pipeline_state_participant_plan_mismatch", default, "Runtime Pipeline state participant set belongs to another compiled plan.");
                return participantSet.Values;
            }
            int expectedCount = 0;
            for (int i = 0; i < plan.Passes.Count; i++)
            {
                if (plan.Passes[i].Descriptor.StateClass == SimulationPipelinePassStateClass.SnapshotParticipant)
                    expectedCount++;
            }
            var expected = expectedCount == 0
                ? Array.Empty<CompiledSimulationPipelinePass>()
                : new CompiledSimulationPipelinePass[expectedCount];
            int expectedIndex = 0;
            for (int i = 0; i < plan.Passes.Count; i++)
            {
                if (plan.Passes[i].Descriptor.StateClass == SimulationPipelinePassStateClass.SnapshotParticipant)
                    expected[expectedIndex++] = plan.Passes[i];
            }
            var values = participants == null || participants.Count == 0
                ? Array.Empty<ISimulationPipelineStateParticipant>()
                : new ISimulationPipelineStateParticipant[participants.Count];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = participants[i];
                if (values[i] == null)
                    throw Failure("pipeline_state_participant_missing", default, "Runtime Pipeline state participant is missing.");
            }
            Array.Sort(values, (left, right) => left.StateIdentity.PassId.CompareTo(right.StateIdentity.PassId));
            Array.Sort(expected, (left, right) => left.Descriptor.PassId.CompareTo(right.Descriptor.PassId));
            if (values.Length != expected.Length)
                throw Failure("pipeline_state_participant_count_mismatch", default, "Runtime Pipeline state participant count does not match the compiled plan.");
            for (int i = 0; i < values.Length; i++)
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
            StableHash payloadHash = SimulationCanonicalPayloadHash.Compute(snapshot.Payload.Span);
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
