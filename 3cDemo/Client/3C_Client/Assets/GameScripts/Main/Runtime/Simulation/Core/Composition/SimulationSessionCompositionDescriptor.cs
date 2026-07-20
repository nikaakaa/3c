using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ThirdPersonSimulation
{
    public sealed class SimulationActorRosterDescriptor
    {
        readonly ReadOnlyCollection<ActorId> m_Actors;

        public SimulationActorRosterDescriptor(IEnumerable<ActorId> actors)
        {
            var values = actors == null ? new List<ActorId>() : new List<ActorId>(actors);
            values.Sort();
            if (values.Count == 0)
                throw new ArgumentException("Session roster must contain at least one Actor.", nameof(actors));
            var identities = new string[values.Count + 1];
            identities[0] = "simulation-roster/1";
            for (int i = 0; i < values.Count; i++)
            {
                if (!values[i].IsValid || i > 0 && values[i - 1].Equals(values[i]))
                    throw new ArgumentException("Session roster contains an invalid or duplicate ActorId.", nameof(actors));
                identities[i + 1] = values[i].Value;
            }
            m_Actors = values.AsReadOnly();
            RosterHash = StableHash.Compute(identities);
        }

        public IReadOnlyList<ActorId> Actors => m_Actors;
        public StableHash RosterHash { get; }
    }

    public sealed class SimulationSessionCompositionDescriptor
    {
        public SimulationSessionCompositionDescriptor(
            SimulationSessionId sessionId,
            SimulationWorldId worldId,
            SimulationSourceClockId sourceClockId,
            int tickRate,
            SimulationComponentIdentity programRuntime,
            NumericProfileId numericProfileId,
            TargetAbiVersion targetAbiVersion,
            OperationSetVersion operationSetVersion,
            SimulationComponentIdentity executionBackend,
            SimulationPipelineIdentity pipeline,
            ProgramCatalogHash programCatalogHash,
            SimulationActorRosterDescriptor roster,
            SimulationComponentIdentity sessionSource,
            SimulationComponentIdentity worldSolver,
            SolverImplementationId solverImplementationId,
            WorldCapability solverCapabilities,
            WorldFeature solverFeatures,
            SimulationComponentIdentity snapshotCodec,
            SimulationComponentIdentity committer,
            SimulationComponentIdentity? model = null,
            SimulationComponentIdentity? endpoint = null,
            SimulationProtocolIdentity? protocol = null)
        {
            if (!sessionId.IsValid || !worldId.IsValid || !sourceClockId.IsValid || tickRate <= 0)
                throw new ArgumentException("Session identity and TickRate are required.");
            RequireRole(programRuntime, SimulationComponentRole.ProgramRuntime, nameof(programRuntime));
            RequireRole(executionBackend, SimulationComponentRole.ExecutionBackend, nameof(executionBackend));
            RequireRole(sessionSource, SimulationComponentRole.SessionSource, nameof(sessionSource));
            RequireRole(worldSolver, SimulationComponentRole.WorldSolver, nameof(worldSolver));
            RequireRole(snapshotCodec, SimulationComponentRole.SnapshotCodec, nameof(snapshotCodec));
            RequireRole(committer, SimulationComponentRole.Committer, nameof(committer));
            if (!numericProfileId.IsValid || !targetAbiVersion.IsValid || !operationSetVersion.IsValid ||
                !pipeline.IsValid || !programCatalogHash.IsValid || roster == null || solverImplementationId.Equals(default))
            {
                throw new ArgumentException("Session composition identity is incomplete.");
            }
            if (model.HasValue)
                RequireRole(model.Value, SimulationComponentRole.Model, nameof(model));
            if (endpoint.HasValue)
                RequireRole(endpoint.Value, SimulationComponentRole.Endpoint, nameof(endpoint));
            bool hasNetworkModel = model.HasValue || endpoint.HasValue || protocol.HasValue;
            if (hasNetworkModel && (!model.HasValue || !endpoint.HasValue || !protocol.HasValue || !protocol.Value.IsValid))
                throw new ArgumentException("Network Model composition requires Model, Endpoint, and Protocol identities together.");

            SessionId = sessionId;
            WorldId = worldId;
            SourceClockId = sourceClockId;
            TickRate = tickRate;
            ProgramRuntime = programRuntime;
            NumericProfileId = numericProfileId;
            TargetAbiVersion = targetAbiVersion;
            OperationSetVersion = operationSetVersion;
            ExecutionBackend = executionBackend;
            Pipeline = pipeline;
            ProgramCatalogHash = programCatalogHash;
            Roster = roster;
            SessionSource = sessionSource;
            WorldSolver = worldSolver;
            SolverImplementationId = solverImplementationId;
            SolverCapabilities = solverCapabilities;
            SolverFeatures = solverFeatures;
            SnapshotCodec = snapshotCodec;
            Committer = committer;
            Model = model;
            Endpoint = endpoint;
            Protocol = protocol;
            Identity = ComputeIdentity();
        }

        public SimulationSessionCompositionIdentity Identity { get; }
        public SimulationSessionId SessionId { get; }
        public SimulationWorldId WorldId { get; }
        public SimulationSourceClockId SourceClockId { get; }
        public int TickRate { get; }
        public SimulationComponentIdentity ProgramRuntime { get; }
        public NumericProfileId NumericProfileId { get; }
        public TargetAbiVersion TargetAbiVersion { get; }
        public OperationSetVersion OperationSetVersion { get; }
        public SimulationComponentIdentity ExecutionBackend { get; }
        public SimulationPipelineIdentity Pipeline { get; }
        public ProgramCatalogHash ProgramCatalogHash { get; }
        public SimulationActorRosterDescriptor Roster { get; }
        public SimulationComponentIdentity SessionSource { get; }
        public SimulationComponentIdentity WorldSolver { get; }
        public SolverImplementationId SolverImplementationId { get; }
        public WorldCapability SolverCapabilities { get; }
        public WorldFeature SolverFeatures { get; }
        public SimulationComponentIdentity SnapshotCodec { get; }
        public SimulationComponentIdentity Committer { get; }
        public SimulationComponentIdentity? Model { get; }
        public SimulationComponentIdentity? Endpoint { get; }
        public SimulationProtocolIdentity? Protocol { get; }

        SimulationSessionCompositionIdentity ComputeIdentity()
        {
            return new SimulationSessionCompositionIdentity(StableHash.Compute(
                "simulation-session-composition/2",
                SessionId.Value,
                WorldId.Value,
                SourceClockId.Value,
                TickRate.ToString(CultureInfo.InvariantCulture),
                ProgramRuntime.ToString(),
                NumericProfileId.Value,
                TargetAbiVersion.ToString(),
                OperationSetVersion.Value,
                ExecutionBackend.ToString(),
                Pipeline.ToString(),
                ProgramCatalogHash.ToString(),
                Roster.RosterHash.ToString(),
                SessionSource.ToString(),
                WorldSolver.ToString(),
                SolverImplementationId.ToString(),
                Convert.ToUInt64(SolverCapabilities, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
                Convert.ToUInt64(SolverFeatures, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
                SnapshotCodec.ToString(),
                Committer.ToString(),
                Model?.ToString() ?? string.Empty,
                Endpoint?.ToString() ?? string.Empty,
                Protocol?.ToString() ?? string.Empty));
        }

        static void RequireRole(SimulationComponentIdentity identity, SimulationComponentRole role, string parameter)
        {
            if (!identity.IsValid || identity.Role != role)
                throw new ArgumentException($"Component role must be {role}.", parameter);
        }
    }
}
