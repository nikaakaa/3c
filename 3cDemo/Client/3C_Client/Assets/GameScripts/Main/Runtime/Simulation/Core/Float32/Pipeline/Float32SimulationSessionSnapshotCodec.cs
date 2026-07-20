using System;
using System.Collections.Generic;
using System.IO;

namespace ThirdPersonSimulation
{
    public interface IFloat32SimulationSessionSnapshotCodec
    {
        SimulationComponentIdentity Identity { get; }
        Float32SimulationSessionSnapshot Capture(
            SimulationSessionCompositionDescriptor descriptor,
            SimulationProgramCatalog catalog,
            SimulationWorldStateSet state,
            SimulationPipelineStateSnapshot pipelineState,
            WorldCapability solverCapabilities);
        byte[] Write(Float32SimulationSessionSnapshot snapshot);
        Float32SimulationSessionSnapshot Read(byte[] bytes);
        void RequireRestore(
            SimulationSessionCompositionDescriptor descriptor,
            SimulationProgramCatalog catalog,
            ICharacterWorldSolver solver,
            SimulationRestoreDirective directive,
            Float32SimulationSessionSnapshot snapshot);
    }

    public sealed class Float32SimulationSessionSnapshotCodec : IFloat32SimulationSessionSnapshotCodec
    {
        const uint Magic = 0x53534643;
        const int Version = 2;

        public Float32SimulationSessionSnapshotCodec(SimulationComponentIdentity identity)
        {
            if (!identity.IsValid || identity.Role != SimulationComponentRole.SnapshotCodec)
                throw new ArgumentException("Snapshot codec identity is invalid.", nameof(identity));
            Identity = identity;
        }

        public SimulationComponentIdentity Identity { get; }

        public Float32SimulationSessionSnapshot Capture(
            SimulationSessionCompositionDescriptor descriptor,
            SimulationProgramCatalog catalog,
            SimulationWorldStateSet state,
            SimulationPipelineStateSnapshot pipelineState,
            WorldCapability solverCapabilities)
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (state == null || state.LastCompletedTick == 0)
                throw new InvalidOperationException("Session snapshot requires a completed Simulation Tick.");
            if (pipelineState == null || pipelineState.LastCompletedTick != state.LastCompletedTick ||
                !pipelineState.Pipeline.Equals(descriptor.Pipeline) ||
                !pipelineState.Backend.Equals(descriptor.ExecutionBackend))
            {
                throw new InvalidOperationException("Pipeline state snapshot does not match the active Session composition.");
            }
            SimulationWorldSnapshot world = SimulationWorldSnapshotFactory.Capture(
                catalog,
                new SimulationTick(state.LastCompletedTick),
                state.Actors,
                state.WorldState,
                solverCapabilities);
            return new Float32SimulationSessionSnapshot(descriptor.Identity, world, pipelineState);
        }

        public byte[] Write(Float32SimulationSessionSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(Magic);
            writer.WriteInt32(Version);
            writer.WriteString(snapshot.SnapshotHash.ToString());
            writer.WriteString(snapshot.CompositionIdentity.ToString());
            writer.WriteBytes(SimulationWorldSnapshotCodec.Write(snapshot.World));
            WritePipeline(writer, snapshot.Pipeline);
            return writer.ToArray();
        }

        public Float32SimulationSessionSnapshot Read(byte[] bytes)
        {
            var reader = new CanonicalReader(bytes);
            if (reader.ReadUInt32() != Magic || reader.ReadInt32() != Version)
                throw new InvalidDataException("Float32 Session snapshot header is invalid.");
            var expectedHash = new StableHash(reader.ReadString());
            var compositionIdentity = new SimulationSessionCompositionIdentity(new StableHash(reader.ReadString()));
            SimulationWorldSnapshot world = SimulationWorldSnapshotCodec.Read(reader.ReadBytes());
            SimulationPipelineStateSnapshot pipeline = ReadPipeline(reader);
            reader.RequireComplete();
            var snapshot = new Float32SimulationSessionSnapshot(compositionIdentity, world, pipeline);
            if (!snapshot.SnapshotHash.Equals(expectedHash))
                throw new InvalidDataException($"Float32 Session snapshot hash mismatch. Expected '{expectedHash}', actual '{snapshot.SnapshotHash}'.");
            return snapshot;
        }

        public void RequireRestore(
            SimulationSessionCompositionDescriptor descriptor,
            SimulationProgramCatalog catalog,
            ICharacterWorldSolver solver,
            SimulationRestoreDirective directive,
            Float32SimulationSessionSnapshot snapshot)
        {
            if (descriptor == null || catalog == null || solver == null || directive == null || snapshot == null)
                throw new ArgumentNullException("Restore validation requires complete Session dependencies.");
            if (!snapshot.CompositionIdentity.Equals(descriptor.Identity) ||
                snapshot.Tick != directive.Tick || !snapshot.SnapshotHash.Equals(directive.SnapshotHash) ||
                !snapshot.World.ProgramCatalogHash.Equals(catalog.CatalogHash) ||
                !directive.ProgramCatalogHash.Equals(catalog.CatalogHash) ||
                !snapshot.Pipeline.Pipeline.Equals(descriptor.Pipeline) ||
                !directive.PipelineHash.Equals(descriptor.Pipeline.Hash) ||
                !snapshot.Pipeline.Backend.Equals(descriptor.ExecutionBackend) ||
                !string.Equals(directive.BackendId, descriptor.ExecutionBackend.ComponentId, StringComparison.Ordinal) ||
                !string.Equals(directive.BackendSemanticVersion, descriptor.ExecutionBackend.SemanticVersion, StringComparison.Ordinal) ||
                !snapshot.World.SolverId.Equals(solver.Descriptor.ImplementationId) ||
                !string.Equals(snapshot.World.SolverVersion, solver.Descriptor.Version, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Restore snapshot identity does not match the active Session composition.");
            }
        }

        static void WritePipeline(CanonicalWriter writer, SimulationPipelineStateSnapshot snapshot)
        {
            writer.WriteString(snapshot.SnapshotHash.ToString());
            writer.WriteString(snapshot.Pipeline.Id.Value);
            writer.WriteString(snapshot.Pipeline.Revision.Value);
            writer.WriteInt32(snapshot.Pipeline.SchemaVersion.Value);
            writer.WriteString(snapshot.Pipeline.Hash.ToString());
            WriteComponent(writer, snapshot.Backend);
            writer.WriteUInt64(snapshot.LastCompletedTick);
            writer.WriteInt32(snapshot.Participants.Count);
            for (int i = 0; i < snapshot.Participants.Count; i++)
            {
                SimulationPipelinePassStateSnapshot participant = snapshot.Participants[i];
                writer.WriteString(participant.PassId.Value);
                writer.WriteString(participant.ImplementationVersion.Value);
                writer.WriteString(participant.StateOwner);
                writer.WriteString(participant.StateSchemaId);
                writer.WriteInt32(participant.StateSchemaVersion);
                writer.WriteString(participant.StateHash.ToString());
                writer.WriteBytes(participant.CopyPayload());
            }
        }

        static SimulationPipelineStateSnapshot ReadPipeline(CanonicalReader reader)
        {
            var expectedHash = new StableHash(reader.ReadString());
            var pipeline = new SimulationPipelineIdentity(
                new SimulationPipelineId(reader.ReadString()),
                new SimulationPipelineRevision(reader.ReadString()),
                new SimulationPipelineSchemaVersion(reader.ReadInt32()),
                new SimulationPipelineHash(new StableHash(reader.ReadString())));
            SimulationComponentIdentity backend = ReadComponent(reader);
            ulong lastCompletedTick = reader.ReadUInt64();
            int count = reader.ReadInt32();
            if (count < 0 || count > 1000000)
                throw new InvalidDataException($"Pipeline snapshot participant count '{count}' is invalid.");
            var participants = new List<SimulationPipelinePassStateSnapshot>(count);
            for (int i = 0; i < count; i++)
            {
                var passId = new SimulationPipelinePassId(reader.ReadString());
                var implementationVersion = new SimulationPipelinePassImplementationVersion(reader.ReadString());
                string stateOwner = reader.ReadString();
                string schemaId = reader.ReadString();
                int schemaVersion = reader.ReadInt32();
                var stateHash = new StableHash(reader.ReadString());
                byte[] payload = reader.ReadBytes();
                participants.Add(new SimulationPipelinePassStateSnapshot(
                    passId,
                    implementationVersion,
                    stateOwner,
                    schemaId,
                    schemaVersion,
                    stateHash,
                    payload));
            }
            var snapshot = new SimulationPipelineStateSnapshot(pipeline, backend, lastCompletedTick, participants);
            if (!snapshot.SnapshotHash.Equals(expectedHash))
                throw new InvalidDataException($"Pipeline state snapshot hash mismatch. Expected '{expectedHash}', actual '{snapshot.SnapshotHash}'.");
            return snapshot;
        }

        static void WriteComponent(CanonicalWriter writer, SimulationComponentIdentity identity)
        {
            writer.WriteByte((byte)identity.Role);
            writer.WriteString(identity.ComponentId);
            writer.WriteString(identity.SemanticVersion);
            writer.WriteString(identity.ConfigurationHash.ToString());
        }

        static SimulationComponentIdentity ReadComponent(CanonicalReader reader)
        {
            return new SimulationComponentIdentity(
                (SimulationComponentRole)reader.ReadByte(),
                reader.ReadString(),
                reader.ReadString(),
                new StableHash(reader.ReadString()));
        }
    }
}
