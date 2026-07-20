using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonSimulation.DotRecast;
using ThirdPersonSimulation.ServerAuthoritative;

namespace ThirdPersonSimulation.DotRecastAuthority
{
    public static class DotRecastAuthoritySceneManifestCodec
    {
        public static byte[] Write(DotRecastAuthoritySceneManifest manifest)
        {
            if (manifest == null)
                throw new ArgumentNullException(nameof(manifest));
            byte[] payload = WritePayload(manifest);
            StableHash hash = SimulationCanonicalPayloadHash.Compute(payload);
            if (!hash.Equals(manifest.ManifestHash))
                throw new InvalidDataException("DotRecast Authority Scene manifest changed after construction.");
            using var writer = new CanonicalWriter();
            writer.WriteString(DotRecastAuthoritySceneManifest.Magic);
            writer.WriteInt32(DotRecastAuthoritySceneManifest.SchemaVersion);
            writer.WriteBytes(payload);
            writer.WriteString(hash.Value);
            return writer.ToArray();
        }

        public static DotRecastAuthoritySceneManifest Read(byte[] bytes)
        {
            var reader = new CanonicalReader(bytes ?? throw new ArgumentNullException(nameof(bytes)));
            if (!string.Equals(reader.ReadString(), DotRecastAuthoritySceneManifest.Magic, StringComparison.Ordinal) ||
                reader.ReadInt32() != DotRecastAuthoritySceneManifest.SchemaVersion)
            {
                throw new InvalidDataException("DotRecast Authority Scene manifest magic or schema is unsupported.");
            }
            byte[] payload = reader.ReadBytes();
            var expectedHash = new StableHash(reader.ReadString());
            reader.RequireComplete();
            StableHash actualHash = SimulationCanonicalPayloadHash.Compute(payload);
            if (!actualHash.Equals(expectedHash))
                throw new InvalidDataException("DotRecast Authority Scene manifest hash does not match its canonical payload.");

            var payloadReader = new CanonicalReader(payload);
            var hostProductId = new HostProductId(payloadReader.ReadString());
            string hostId = payloadReader.ReadString();
            DotRecastAuthoritySceneIdentity scene = ReadScene(payloadReader);
            var roomId = new ServerAuthoritativeRoomId(payloadReader.ReadString());
            DotRecastAuthorityEndpointDescriptor data = ReadEndpoint(payloadReader);
            DotRecastAuthorityProgramArtifactBinding program = ReadProgram(payloadReader);
            DotRecastAuthorityPipelineBinding pipeline = ReadPipeline(payloadReader);
            DotRecastAuthorityWorldBinding world = ReadWorld(payloadReader);
            DotRecastAuthorityRuntimeIdentitySet runtime = ReadRuntime(payloadReader);
            int actorCount = ReadCount(payloadReader, "roster", 1024);
            var actors = new DotRecastAuthorityActorBinding[actorCount];
            for (int i = 0; i < actorCount; i++)
                actors[i] = ReadActor(payloadReader);
            payloadReader.RequireComplete();
            var manifest = new DotRecastAuthoritySceneManifest(
                hostProductId,
                hostId,
                scene,
                roomId,
                data,
                program,
                pipeline,
                world,
                runtime,
                actors);
            if (!manifest.ManifestHash.Equals(expectedHash))
                throw new InvalidDataException("DotRecast Authority Scene manifest canonical reconstruction is unstable.");
            return manifest;
        }

        public static StableHash ComputeHash(DotRecastAuthoritySceneManifest manifest)
        {
            if (manifest == null)
                throw new ArgumentNullException(nameof(manifest));
            return SimulationCanonicalPayloadHash.Compute(WritePayload(manifest));
        }

        static byte[] WritePayload(DotRecastAuthoritySceneManifest manifest)
        {
            using var writer = new CanonicalWriter();
            writer.WriteString(manifest.HostProductId.Value);
            writer.WriteString(manifest.HostId);
            WriteScene(writer, manifest.Scene);
            writer.WriteString(manifest.RoomId.Value);
            WriteEndpoint(writer, manifest.DataEndpoint);
            WriteProgram(writer, manifest.Program);
            WritePipeline(writer, manifest.Pipeline);
            WriteWorld(writer, manifest.World);
            WriteRuntime(writer, manifest.Runtime);
            writer.WriteInt32(manifest.Roster.Count);
            for (int i = 0; i < manifest.Roster.Count; i++)
                WriteActor(writer, manifest.Roster[i]);
            return writer.ToArray();
        }

        static void WriteScene(CanonicalWriter writer, DotRecastAuthoritySceneIdentity scene)
        {
            writer.WriteInt32(scene.ProcessConfigId);
            writer.WriteInt32(scene.SceneConfigId);
            writer.WriteString(scene.SceneType);
        }

        static DotRecastAuthoritySceneIdentity ReadScene(CanonicalReader reader) =>
            new DotRecastAuthoritySceneIdentity(
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadString());

        static void WriteEndpoint(CanonicalWriter writer, DotRecastAuthorityEndpointDescriptor endpoint)
        {
            writer.WriteString(endpoint.Host);
            writer.WriteInt32(endpoint.Port);
        }

        static DotRecastAuthorityEndpointDescriptor ReadEndpoint(CanonicalReader reader) =>
            new DotRecastAuthorityEndpointDescriptor(reader.ReadString(), reader.ReadInt32());

        static void WriteProgram(CanonicalWriter writer, DotRecastAuthorityProgramArtifactBinding program)
        {
            writer.WriteString(program.RelativePath);
            writer.WriteString(program.DefinitionGuid);
            writer.WriteString(program.ProgramId.Value);
            writer.WriteString(program.ProgramHash.ToString());
            writer.WriteString(program.LayoutHash.ToString());
            writer.WriteString(program.ArtifactBytesHash.Value);
            writer.WriteInt32(program.ArtifactByteLength);
            writer.WriteString(program.CompilerVersion);
            writer.WriteString(program.OperationSetVersion.Value);
            writer.WriteString(program.SourceRevision.Value);
            writer.WriteString(program.SemanticHash.ToString());
            writer.WriteString(program.NumericProfileId.Value);
            writer.WriteInt32(program.TargetAbiVersion.Value);
            writer.WriteUInt64((ulong)program.RequiredWorldCapabilities);
        }

        static DotRecastAuthorityProgramArtifactBinding ReadProgram(CanonicalReader reader)
        {
            return new DotRecastAuthorityProgramArtifactBinding(
                reader.ReadString(),
                reader.ReadString(),
                new ProgramId(reader.ReadString()),
                new ProgramHash(new StableHash(reader.ReadString())),
                new LayoutHash(new StableHash(reader.ReadString())),
                new StableHash(reader.ReadString()),
                reader.ReadInt32(),
                reader.ReadString(),
                new OperationSetVersion(reader.ReadString()),
                new ProgramRevision(reader.ReadString()),
                new SemanticHash(new StableHash(reader.ReadString())),
                new NumericProfileId(reader.ReadString()),
                new TargetAbiVersion(reader.ReadInt32()),
                (WorldCapability)reader.ReadUInt64());
        }

        static void WritePipeline(CanonicalWriter writer, DotRecastAuthorityPipelineBinding pipeline)
        {
            WritePipelineIdentity(writer, pipeline.PredictionIdentity);
            WritePipelineIdentity(writer, pipeline.Identity);
            writer.WriteString(pipeline.DescriptorHash.Value);
            WriteComponent(writer, pipeline.BackendIdentity);
            WriteSource(writer, pipeline.Source);
            WriteSourcePorts(writer, pipeline.SourcePorts);
            byte[] sourcePolicyBytes = ServerAuthoritativeAuthoritySourcePolicyCodec.Write(pipeline.SourcePolicy);
            writer.WriteBytes(sourcePolicyBytes);
            writer.WriteString(pipeline.SourcePolicy.ConfigurationHash.Value);
            ServerAuthoritativeReplicationPolicy replication = pipeline.ReplicationPolicy;
            writer.WriteUInt16((ushort)replication.ReliableGameplayFactKinds);
            writer.WriteInt32(replication.ReliableProducerIds.Count);
            for (int i = 0; i < replication.ReliableProducerIds.Count; i++)
                writer.WriteString(replication.ReliableProducerIds[i]);
            writer.WriteString(replication.ConfigurationHash.Value);
        }

        static DotRecastAuthorityPipelineBinding ReadPipeline(CanonicalReader reader)
        {
            SimulationPipelineIdentity predictionIdentity = ReadPipelineIdentity(reader);
            SimulationPipelineIdentity identity = ReadPipelineIdentity(reader);
            var descriptorHash = new StableHash(reader.ReadString());
            SimulationComponentIdentity backend = ReadComponent(reader);
            SimulationSessionSourceDescriptor source = ReadSource(reader);
            IReadOnlyList<SimulationPortDescriptor> sourcePorts = ReadSourcePorts(reader);
            byte[] sourcePolicyBytes = reader.ReadBytes();
            var expectedSourcePolicyHash = new StableHash(reader.ReadString());
            ServerAuthoritativeAuthoritySourcePolicy sourcePolicy = ServerAuthoritativeAuthoritySourcePolicyCodec.Read(sourcePolicyBytes);
            if (!sourcePolicy.ConfigurationHash.Equals(expectedSourcePolicyHash))
                throw new InvalidDataException("Authority Source policy hash does not match manifest identity.");
            var factKinds = (ServerAuthoritativeReliableGameplayFactKinds)reader.ReadUInt16();
            int producerCount = ReadCount(reader, "replication producer", 1000000);
            var producers = new string[producerCount];
            for (int i = 0; i < producers.Length; i++)
                producers[i] = reader.ReadString();
            var expectedReplicationHash = new StableHash(reader.ReadString());
            var replication = new ServerAuthoritativeReplicationPolicy(factKinds, producers);
            if (!replication.ConfigurationHash.Equals(expectedReplicationHash))
                throw new InvalidDataException("Authority replication policy hash does not match manifest identity.");
            return new DotRecastAuthorityPipelineBinding(
                predictionIdentity,
                identity,
                descriptorHash,
                backend,
                source,
                sourcePorts,
                sourcePolicy,
                replication);
        }

        static void WriteSource(CanonicalWriter writer, SimulationSessionSourceDescriptor source)
        {
            WriteComponent(writer, source.Identity);
            writer.WriteString(source.NumericProfileId.Value);
            writer.WriteInt32(source.TargetAbiVersion.Value);
            writer.WriteByte((byte)source.OuterTickKind);
            writer.WriteUInt64((ulong)source.ExecutionSupport);
            writer.WriteBoolean(source.Deterministic);
            writer.WriteString(source.RequiredBackendId);
            writer.WriteString(source.RequiredPipelineId.Value);
            WriteOptionalComponent(writer, source.Model);
            WriteOptionalComponent(writer, source.Endpoint);
            writer.WriteBoolean(source.Protocol.HasValue);
            if (source.Protocol.HasValue)
            {
                SimulationProtocolIdentity protocol = source.Protocol.Value;
                writer.WriteString(protocol.ProtocolId);
                writer.WriteString(protocol.SemanticVersion);
                writer.WriteString(protocol.SchemaHash.Value);
            }
            writer.WriteUInt64((ulong)source.RequiredSolverCapabilities);
            writer.WriteInt32(source.RequiredPipelinePasses.Count);
            for (int i = 0; i < source.RequiredPipelinePasses.Count; i++)
            {
                SimulationPipelinePassRequirement pass = source.RequiredPipelinePasses[i];
                writer.WriteString(pass.PassId.Value);
                writer.WriteString(pass.ImplementationVersion.Value);
                writer.WriteByte((byte)pass.Phase);
            }
            writer.WriteInt32(source.RequiredPipelineSourcePorts.Count);
            for (int i = 0; i < source.RequiredPipelineSourcePorts.Count; i++)
            {
                SimulationPipelinePortRequirement port = source.RequiredPipelineSourcePorts[i];
                writer.WriteByte((byte)port.Role);
                writer.WriteString(port.PortId);
                writer.WriteString(port.SchemaId);
                writer.WriteInt32(port.SchemaVersion);
                writer.WriteByte((byte)port.Direction);
            }
        }

        static SimulationSessionSourceDescriptor ReadSource(CanonicalReader reader)
        {
            SimulationComponentIdentity identity = ReadComponent(reader);
            var numericProfileId = new NumericProfileId(reader.ReadString());
            var targetAbiVersion = new TargetAbiVersion(reader.ReadInt32());
            SimulationTickSourceKind tickKind = ReadEnum<SimulationTickSourceKind>(reader.ReadByte(), "source tick kind");
            var executionSupport = (SimulationPipelineExecutionSupport)reader.ReadUInt64();
            bool deterministic = reader.ReadBoolean();
            string requiredBackendId = reader.ReadString();
            var requiredPipelineId = new SimulationPipelineId(reader.ReadString());
            SimulationComponentIdentity? model = ReadOptionalComponent(reader);
            SimulationComponentIdentity? endpoint = ReadOptionalComponent(reader);
            SimulationProtocolIdentity? protocol = null;
            if (reader.ReadBoolean())
            {
                protocol = new SimulationProtocolIdentity(
                    reader.ReadString(),
                    reader.ReadString(),
                    new StableHash(reader.ReadString()));
            }
            var solverCapabilities = (WorldCapability)reader.ReadUInt64();
            int passCount = ReadCount(reader, "source pass", 1024);
            var passes = new SimulationPipelinePassRequirement[passCount];
            for (int i = 0; i < passes.Length; i++)
            {
                passes[i] = new SimulationPipelinePassRequirement(
                    new SimulationPipelinePassId(reader.ReadString()),
                    new SimulationPipelinePassImplementationVersion(reader.ReadString()),
                    ReadEnum<SimulationPipelinePhase>(reader.ReadByte(), "source pass phase"));
            }
            int portCount = ReadCount(reader, "source requirement port", 1024);
            var ports = new SimulationPipelinePortRequirement[portCount];
            for (int i = 0; i < ports.Length; i++)
            {
                ports[i] = new SimulationPipelinePortRequirement(
                    ReadEnum<SimulationPipelineBindingPortRole>(reader.ReadByte(), "source port role"),
                    reader.ReadString(),
                    reader.ReadString(),
                    reader.ReadInt32(),
                    ReadEnum<SimulationPortDirection>(reader.ReadByte(), "source port direction"));
            }
            return new SimulationSessionSourceDescriptor(
                identity,
                numericProfileId,
                targetAbiVersion,
                tickKind,
                executionSupport,
                deterministic,
                requiredBackendId,
                requiredPipelineId,
                model,
                endpoint,
                protocol,
                solverCapabilities,
                passes,
                ports);
        }

        static void WriteSourcePorts(CanonicalWriter writer, IReadOnlyList<SimulationPortDescriptor> ports)
        {
            writer.WriteInt32(ports.Count);
            for (int i = 0; i < ports.Count; i++)
            {
                SimulationPortDescriptor port = ports[i];
                writer.WriteString(port.PortId);
                writer.WriteString(port.SchemaId);
                writer.WriteInt32(port.SchemaVersion);
                writer.WriteByte((byte)port.Direction);
                writer.WriteString(port.OwnerComponentId);
                writer.WriteString(port.ConfigurationHash.Value);
            }
        }

        static IReadOnlyList<SimulationPortDescriptor> ReadSourcePorts(CanonicalReader reader)
        {
            int count = ReadCount(reader, "source runtime port", 1024);
            var ports = new SimulationPortDescriptor[count];
            for (int i = 0; i < ports.Length; i++)
            {
                ports[i] = new SimulationPortDescriptor(
                    reader.ReadString(),
                    reader.ReadString(),
                    reader.ReadInt32(),
                    ReadEnum<SimulationPortDirection>(reader.ReadByte(), "source runtime port direction"),
                    reader.ReadString(),
                    new StableHash(reader.ReadString()));
            }
            return ports;
        }

        static void WriteWorld(CanonicalWriter writer, DotRecastAuthorityWorldBinding world)
        {
            writer.WriteString(world.WorldId.Value);
            writer.WriteString(world.MapId);
            writer.WriteString(world.WorldRevision.Value);
            writer.WriteString(world.WorldConfigurationHash.Value);
            writer.WriteString(world.NavigationSurfaceConfigurationHash.Value);
            WriteWorldSolverDefinition(writer, world.SolverDefinition);
            writer.WriteString(world.NavigationSurfaceRelativePath);
            writer.WriteString(world.NavigationSurfaceContentHash.Value);
            writer.WriteString(world.NavigationSurfaceBytesHash.Value);
            writer.WriteInt32(world.NavigationSurfaceByteLength);
            writer.WriteString(world.QueryProfileHash.Value);
            WriteContactConfiguration(writer, world.ContactConfiguration);
        }

        static DotRecastAuthorityWorldBinding ReadWorld(CanonicalReader reader)
        {
            return new DotRecastAuthorityWorldBinding(
                new SimulationWorldId(reader.ReadString()),
                reader.ReadString(),
                new WorldRevision(reader.ReadString()),
                new StableHash(reader.ReadString()),
                new StableHash(reader.ReadString()),
                ReadWorldSolverDefinition(reader),
                reader.ReadString(),
                new StableHash(reader.ReadString()),
                new StableHash(reader.ReadString()),
                reader.ReadInt32(),
                new StableHash(reader.ReadString()),
                ReadContactConfiguration(reader));
        }

        static void WriteWorldSolverDefinition(
            CanonicalWriter writer,
            SimulationWorldSolverDefinitionDescriptor definition)
        {
            WriteComponent(writer, definition.Identity);
            writer.WriteString(definition.NumericProfileId.Value);
            writer.WriteInt32(definition.TargetAbiVersion.Value);
            writer.WriteString(definition.ImplementationId.Value);
            writer.WriteString(definition.ImplementationVersion);
            writer.WriteUInt64((ulong)definition.Capabilities);
            writer.WriteInt32((int)definition.Features);
            writer.WriteInt32((int)definition.ExecutionSupport);
            writer.WriteBoolean(definition.Deterministic);
        }

        static SimulationWorldSolverDefinitionDescriptor ReadWorldSolverDefinition(CanonicalReader reader) =>
            new SimulationWorldSolverDefinitionDescriptor(
                ReadComponent(reader),
                new NumericProfileId(reader.ReadString()),
                new TargetAbiVersion(reader.ReadInt32()),
                new SolverImplementationId(reader.ReadString()),
                reader.ReadString(),
                (WorldCapability)reader.ReadUInt64(),
                (WorldFeature)reader.ReadInt32(),
                (SimulationPipelineExecutionSupport)reader.ReadInt32(),
                reader.ReadBoolean());

        static void WriteRuntime(CanonicalWriter writer, DotRecastAuthorityRuntimeIdentitySet runtime)
        {
            writer.WriteString(runtime.SessionId.Value);
            writer.WriteString(runtime.SourceClockId.Value);
            WriteComponent(writer, runtime.SnapshotCodec);
            WriteComponent(writer, runtime.Committer);
            WriteComponent(writer, runtime.Transport);
            WriteComponent(writer, runtime.Diagnostics);
        }

        static DotRecastAuthorityRuntimeIdentitySet ReadRuntime(CanonicalReader reader)
        {
            return new DotRecastAuthorityRuntimeIdentitySet(
                new SimulationSessionId(reader.ReadString()),
                new SimulationSourceClockId(reader.ReadString()),
                ReadComponent(reader),
                ReadComponent(reader),
                ReadComponent(reader),
                ReadComponent(reader));
        }

        static void WriteActor(CanonicalWriter writer, DotRecastAuthorityActorBinding actor)
        {
            writer.WriteString(actor.Roster.PlayerId.Value);
            writer.WriteString(actor.Roster.ActorId.Value);
            writer.WriteByte((byte)actor.Roster.ClientRole);
            writer.WriteString(actor.WorldBodyBindingId);
            writer.WriteBytes(actor.CopyInitialCharacterStateBytes());
            writer.WriteString(actor.InitialCharacterStateHash.ToString());
            WriteBody(writer, actor.InitialBody);
            WriteContactShape(writer, actor.ContactShape);
            WriteOutputRoute(writer, actor.OutputRoute);
        }

        static DotRecastAuthorityActorBinding ReadActor(CanonicalReader reader)
        {
            var roster = new ServerAuthoritativeRosterEntry(
                new ServerAuthoritativePlayerId(reader.ReadString()),
                new ActorId(reader.ReadString()),
                ReadEnum<ServerAuthoritativeProcessRole>(reader.ReadByte(), "client role"));
            string binding = reader.ReadString();
            byte[] stateBytes = reader.ReadBytes();
            var stateHash = new CharacterStateHash(new StableHash(reader.ReadString()));
            WorldBodyState body = ReadBody(reader);
            ActorContactShape contactShape = ReadContactShape(reader);
            SimulationOutputRouteDescriptor route = ReadOutputRoute(reader);
            return new DotRecastAuthorityActorBinding(roster, binding, stateBytes, stateHash, body, contactShape, route);
        }

        static void WriteContactShape(CanonicalWriter writer, ActorContactShape shape)
        {
            writer.WriteUInt32(shape.Radius.Bits);
            writer.WriteUInt32(shape.Height.Bits);
            writer.WriteUInt32(shape.SkinWidth.Bits);
        }

        static ActorContactShape ReadContactShape(CanonicalReader reader) => new ActorContactShape(
            Float32Scalar.FromBits(reader.ReadUInt32()),
            Float32Scalar.FromBits(reader.ReadUInt32()),
            Float32Scalar.FromBits(reader.ReadUInt32()));

        static void WriteContactConfiguration(
            CanonicalWriter writer,
            ActorContactSolverConfiguration configuration)
        {
            writer.WriteInt32(configuration.IterationCount);
            writer.WriteUInt32(configuration.ContactTolerance.Bits);
            writer.WriteUInt32(configuration.MaximumDepenetrationDistance.Bits);
            writer.WriteByte((byte)configuration.ResponseKind);
        }

        static ActorContactSolverConfiguration ReadContactConfiguration(CanonicalReader reader) =>
            new ActorContactSolverConfiguration(
                reader.ReadInt32(),
                Float32Scalar.FromBits(reader.ReadUInt32()),
                Float32Scalar.FromBits(reader.ReadUInt32()),
                ReadEnum<ActorContactResponseKind>(reader.ReadByte(), "actor contact response"));

        static void WriteBody(CanonicalWriter writer, WorldBodyState body)
        {
            writer.WriteString(body.ActorId.Value);
            writer.WriteVector3(body.Position);
            writer.WriteYaw(body.Yaw);
            writer.WriteVector3(body.Velocity);
            writer.WriteScalar(body.VerticalVelocity);
            writer.WriteBoolean(body.Grounded);
            writer.WriteUInt32((uint)body.Collision);
        }

        static WorldBodyState ReadBody(CanonicalReader reader) => new WorldBodyState(
            new ActorId(reader.ReadString()),
            reader.ReadVector3(),
            reader.ReadYaw(),
            reader.ReadVector3(),
            reader.ReadScalar(),
            reader.ReadBoolean(),
            (WorldCollisionSummary)reader.ReadUInt32());

        static void WriteOutputRoute(CanonicalWriter writer, SimulationOutputRouteDescriptor route)
        {
            writer.WriteString(route.RouteId);
            writer.WriteString(route.SchemaId);
            writer.WriteInt32(route.SchemaVersion);
            writer.WriteString(route.ActorId.Value);
            writer.WriteString(route.ConfigurationHash.Value);
        }

        static SimulationOutputRouteDescriptor ReadOutputRoute(CanonicalReader reader) =>
            new SimulationOutputRouteDescriptor(
                reader.ReadString(),
                reader.ReadString(),
                reader.ReadInt32(),
                new ActorId(reader.ReadString()),
                new StableHash(reader.ReadString()));

        static void WritePipelineIdentity(CanonicalWriter writer, SimulationPipelineIdentity identity)
        {
            writer.WriteString(identity.Id.Value);
            writer.WriteString(identity.Revision.Value);
            writer.WriteInt32(identity.SchemaVersion.Value);
            writer.WriteString(identity.Hash.ToString());
        }

        static SimulationPipelineIdentity ReadPipelineIdentity(CanonicalReader reader) =>
            new SimulationPipelineIdentity(
                new SimulationPipelineId(reader.ReadString()),
                new SimulationPipelineRevision(reader.ReadString()),
                new SimulationPipelineSchemaVersion(reader.ReadInt32()),
                new SimulationPipelineHash(new StableHash(reader.ReadString())));

        static void WriteComponent(CanonicalWriter writer, SimulationComponentIdentity identity)
        {
            writer.WriteByte((byte)identity.Role);
            writer.WriteString(identity.ComponentId);
            writer.WriteString(identity.SemanticVersion);
            writer.WriteString(identity.ConfigurationHash.Value);
        }

        static SimulationComponentIdentity ReadComponent(CanonicalReader reader) =>
            new SimulationComponentIdentity(
                ReadEnum<SimulationComponentRole>(reader.ReadByte(), "component role"),
                reader.ReadString(),
                reader.ReadString(),
                new StableHash(reader.ReadString()));

        static void WriteOptionalComponent(
            CanonicalWriter writer,
            SimulationComponentIdentity? identity)
        {
            writer.WriteBoolean(identity.HasValue);
            if (identity.HasValue)
                WriteComponent(writer, identity.Value);
        }

        static SimulationComponentIdentity? ReadOptionalComponent(CanonicalReader reader) =>
            reader.ReadBoolean() ? ReadComponent(reader) : default(SimulationComponentIdentity?);

        static int ReadCount(CanonicalReader reader, string label, int maximum)
        {
            int count = reader.ReadInt32();
            if (count <= 0 || count > maximum)
                throw new InvalidDataException($"Manifest {label} count '{count}' is invalid.");
            return count;
        }

        static T ReadEnum<T>(byte value, string label) where T : struct
        {
            if (!Enum.IsDefined(typeof(T), value))
                throw new InvalidDataException($"Manifest {label} '{value}' is invalid.");
            return (T)Enum.ToObject(typeof(T), value);
        }
    }
}
