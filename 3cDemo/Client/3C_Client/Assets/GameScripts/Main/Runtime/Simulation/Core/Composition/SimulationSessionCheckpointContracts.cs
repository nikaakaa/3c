using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public sealed class SimulationSessionCheckpoint
    {
        readonly byte[] m_Payload;

        public SimulationSessionCheckpoint(
            SimulationSessionId sessionId,
            SimulationTick tick,
            SimulationPipelineHash pipelineHash,
            string backendId,
            string backendSemanticVersion,
            string snapshotId,
            StableHash snapshotHash,
            byte[] payload)
        {
            if (!sessionId.IsValid || !tick.IsValid ||
                !pipelineHash.IsValid || !snapshotHash.IsValid || payload == null || payload.Length == 0)
            {
                throw new ArgumentException("Simulation Session checkpoint identity is incomplete.");
            }
            SessionId = sessionId;
            Tick = tick;
            PipelineHash = pipelineHash;
            BackendId = SimulationIdentity.Require(backendId, nameof(backendId));
            BackendSemanticVersion = SimulationIdentity.Require(backendSemanticVersion, nameof(backendSemanticVersion));
            SnapshotId = SimulationIdentity.Require(snapshotId, nameof(snapshotId));
            SnapshotHash = snapshotHash;
            m_Payload = (byte[])payload.Clone();
        }

        public SimulationSessionId SessionId { get; }
        public SimulationTick Tick { get; }
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
