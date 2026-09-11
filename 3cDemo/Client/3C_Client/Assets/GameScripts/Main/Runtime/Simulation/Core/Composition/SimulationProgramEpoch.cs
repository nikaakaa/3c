using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public readonly struct SimulationProgramEpoch : IEquatable<SimulationProgramEpoch>
    {
        public SimulationProgramEpoch(
            ulong value,
            ProgramRevision sourceRevision,
            ProgramCatalogHash programCatalogHash)
        {
            if (value == 0 || string.IsNullOrWhiteSpace(sourceRevision.Value) || !programCatalogHash.IsValid)
                throw new ArgumentException("Simulation Program Epoch identity is incomplete.");
            Value = value;
            SourceRevision = sourceRevision;
            ProgramCatalogHash = programCatalogHash;
            Identity = StableHash.Compute(
                "simulation-program-epoch/1",
                value.ToString(),
                sourceRevision.Value,
                programCatalogHash.ToString());
        }

        public ulong Value { get; }
        public ProgramRevision SourceRevision { get; }
        public ProgramCatalogHash ProgramCatalogHash { get; }
        public StableHash Identity { get; }
        public bool IsValid => Value != 0 && !string.IsNullOrWhiteSpace(SourceRevision.Value) && ProgramCatalogHash.IsValid;

        public static SimulationProgramEpoch Initial(ProgramCatalogHash programCatalogHash) =>
            new SimulationProgramEpoch(1, new ProgramRevision("initial"), programCatalogHash);

        public bool Equals(SimulationProgramEpoch other) =>
            Value == other.Value &&
            SourceRevision.Equals(other.SourceRevision) &&
            ProgramCatalogHash.Equals(other.ProgramCatalogHash);

        public override bool Equals(object obj) => obj is SimulationProgramEpoch other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Value, SourceRevision, ProgramCatalogHash);
        public override string ToString() => $"{Value}:{SourceRevision.Value}:{ProgramCatalogHash}";
        public static bool operator ==(SimulationProgramEpoch left, SimulationProgramEpoch right) => left.Equals(right);
        public static bool operator !=(SimulationProgramEpoch left, SimulationProgramEpoch right) => !left.Equals(right);
    }

    public enum SimulationProgramAdoptionStatus : byte
    {
        Applied = 1,
        Deferred = 2,
        Rejected = 3
    }

    public sealed class SimulationProgramAdoptionResult
    {
        public SimulationProgramAdoptionResult(
            SimulationProgramAdoptionStatus status,
            SimulationProgramEpoch current,
            SimulationProgramEpoch requested,
            string code,
            string message)
        {
            if (!Enum.IsDefined(typeof(SimulationProgramAdoptionStatus), status) ||
                !current.IsValid || !requested.IsValid)
            {
                throw new ArgumentException("Simulation Program adoption result identity is incomplete.");
            }
            Status = status;
            Current = current;
            Requested = requested;
            Code = SimulationIdentity.Require(code, nameof(code));
            Message = SimulationIdentity.Require(message, nameof(message));
        }

        public SimulationProgramAdoptionStatus Status { get; }
        public SimulationProgramEpoch Current { get; }
        public SimulationProgramEpoch Requested { get; }
        public string Code { get; }
        public string Message { get; }
        public bool IsApplied => Status == SimulationProgramAdoptionStatus.Applied;
    }

    public interface ISimulationProgramBinding
    {
        ActorId ActorId { get; }
        ProgramId ProgramId { get; }
        ProgramHash ProgramHash { get; }
        LayoutHash LayoutHash { get; }
        string WorldBodyBindingId { get; }
        object ProgramObject { get; }
    }

    public interface ISimulationSessionProgramAdoption
    {
        SimulationProgramEpoch ProgramEpoch { get; }
        SimulationProgramEpoch PrepareProgramEpoch(
            ProgramRevision sourceRevision,
            IReadOnlyList<ISimulationProgramBinding> bindings);
        SimulationProgramAdoptionResult AdoptProgramEpoch(
            SimulationProgramEpoch epoch,
            IReadOnlyList<ISimulationProgramBinding> bindings);
    }

    public sealed class SimulationSessionCheckpoint
    {
        readonly byte[] m_Payload;

        public SimulationSessionCheckpoint(
            SimulationSessionId sessionId,
            SimulationTick tick,
            ProgramCatalogHash programCatalogHash,
            SimulationPipelineHash pipelineHash,
            string backendId,
            string backendSemanticVersion,
            string snapshotId,
            StableHash snapshotHash,
            byte[] payload)
        {
            if (!sessionId.IsValid || !tick.IsValid || !programCatalogHash.IsValid ||
                !pipelineHash.IsValid || !snapshotHash.IsValid || payload == null || payload.Length == 0)
            {
                throw new ArgumentException("Simulation Session checkpoint identity is incomplete.");
            }
            SessionId = sessionId;
            Tick = tick;
            ProgramCatalogHash = programCatalogHash;
            PipelineHash = pipelineHash;
            BackendId = SimulationIdentity.Require(backendId, nameof(backendId));
            BackendSemanticVersion = SimulationIdentity.Require(backendSemanticVersion, nameof(backendSemanticVersion));
            SnapshotId = SimulationIdentity.Require(snapshotId, nameof(snapshotId));
            SnapshotHash = snapshotHash;
            m_Payload = (byte[])payload.Clone();
        }

        public SimulationSessionId SessionId { get; }
        public SimulationTick Tick { get; }
        public ProgramCatalogHash ProgramCatalogHash { get; }
        public SimulationPipelineHash PipelineHash { get; }
        public string BackendId { get; }
        public string BackendSemanticVersion { get; }
        public string SnapshotId { get; }
        public StableHash SnapshotHash { get; }
        public ReadOnlyMemory<byte> Payload => m_Payload;
        internal byte[] PayloadBuffer => m_Payload;
    }

    public interface ISimulationSessionCheckpointRuntime
    {
        SimulationSessionCheckpoint CaptureCheckpoint();
        void RestoreCheckpoint(SimulationSessionCheckpoint checkpoint);
    }

    public interface ISimulationSessionInputReplayRuntime
    {
        bool IsInputRecording { get; }
        bool TryStartInputRecording(out string error);
        bool TryStopInputRecording(out string error);
        bool TryCancelInputReplay(out string error);
        bool TryReplayInputRange(
            SimulationSessionCheckpoint checkpoint,
            ulong fromTick,
            ulong toTick,
            out string error);
    }
}
