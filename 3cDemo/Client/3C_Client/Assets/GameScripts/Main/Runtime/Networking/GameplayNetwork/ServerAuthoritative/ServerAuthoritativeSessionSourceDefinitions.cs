using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;
using ThirdPersonSimulation.UnityAuthority;
using ThirdPersonSimulation.ServerAuthoritative.Transport;
using UnityEngine;

namespace ThirdPersonGameplay.Networking.ServerAuthoritative
{
    internal abstract class ServerAuthoritativeSourcePreparation : ISimulationSessionSourcePreparation
    {
        readonly ServerAuthoritativeHybridModelDefinition m_Model;
        readonly ServerAuthoritativeLaunchDefinition m_Launch;
        readonly ServerAuthoritativeProcessIdentity m_Process;
        readonly ServerAuthoritativeDataPlaneLaunch m_DataPlane;
        readonly StableHash m_ModelConfigurationHash;
        IServerAuthoritativeEndpointConnection m_Connection;
        ISimulationSessionPreparedSource m_Prepared;
        bool m_HandshakeTaken;
        bool m_Disposed;

        protected ServerAuthoritativeSourcePreparation(
            ServerAuthoritativeHybridModelDefinition model,
            ServerAuthoritativeLaunchDefinition launch,
            GameplayNetworkModelPreparationContext context,
            GameplayNetworkModelSourceRequirements requirements)
        {
            Model = m_Model = model ? model : throw new ArgumentNullException(nameof(model));
            m_Launch = launch ? launch : throw new ArgumentNullException(nameof(launch));
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Requirements = requirements ?? throw new ArgumentNullException(nameof(requirements));
            ServerAuthoritativeLaunchDefinition lockedLaunch = m_Launch;
            m_Process = lockedLaunch.BuildProcessIdentity();
            m_DataPlane = lockedLaunch.BuildDataPlaneLaunch();
            _ = lockedLaunch.BuildLaunchHash();
            m_ModelConfigurationHash = model.BuildModelIdentity().ConfigurationHash;
            if (context.TickRate != model.SimulationTickRate)
                throw new InvalidOperationException($"Session TickRate '{context.TickRate}' does not match Model SimulationTickRate '{model.SimulationTickRate}'.");
            Descriptor = BuildDescriptor(requirements);
        }

        protected ServerAuthoritativeHybridModelDefinition Model { get; }
        protected GameplayNetworkModelPreparationContext Context { get; }
        protected GameplayNetworkModelSourceRequirements Requirements { get; }
        protected ServerAuthoritativeProcessIdentity Process => m_Process;
        protected ServerAuthoritativePipelineCompatibilityIdentity Compatibility { get; private set; }
        protected ISimulationDiagnosticsSink Diagnostics { get; private set; }
        protected IServerAuthoritativeEndpointConnection Connection => m_Connection ?? throw new ObjectDisposedException(GetType().Name);

        protected void RequireExpectedAuthorityHost(ServerAuthoritativeEndpointHandshake handshake)
        {
            if (handshake == null)
                throw new ArgumentNullException(nameof(handshake));
            m_Launch.RequireAuthorityHost(handshake.AuthorityHost);
        }
        public SimulationSessionPreparationStatus Status { get; private set; } = SimulationSessionPreparationStatus.Pending;
        public SimulationSessionFailure Failure { get; private set; }
        public SimulationSessionSourceDescriptor Descriptor { get; }

        public SimulationSessionPreparationStatus Step(SimulationSessionLogicTickContext context)
        {
            ThrowIfDisposed();
            if (Status != SimulationSessionPreparationStatus.Pending)
                return Status;
            if (context.Source.Kind != Requirements.OuterTickKind ||
                !string.Equals(context.Source.ClockId, Context.SourceClockId.Value, StringComparison.Ordinal))
            {
                return Fail(
                    "server_authoritative_source_clock_mismatch",
                    $"ServerAuthoritative Source preparation requires its configured '{Requirements.OuterTickKind}' clock.");
            }
            try
            {
                Connection.Step(context);
                if (Connection.Status == ServerAuthoritativeEndpointConnectionStatus.Pending)
                    return Status;
                if (Connection.Status == ServerAuthoritativeEndpointConnectionStatus.Failed)
                {
                    ServerAuthoritativeEndpointFailure failure = Connection.Failure;
                    return Fail(
                        failure?.Code ?? "fantasy_endpoint_failed",
                        failure?.Message ?? "Fantasy Endpoint failed without structured diagnostics.");
                }
                if (m_HandshakeTaken)
                    return Fail("server_authoritative_handshake_duplicate", "Fantasy Endpoint produced a duplicate ready handshake.");
                ServerAuthoritativeEndpointHandshake handshake = Connection.TakeHandshake() ??
                    throw new InvalidOperationException("Fantasy Endpoint became Ready without a handshake.");
                m_HandshakeTaken = true;
                ValidateHandshake(handshake);
                m_Prepared = BuildPrepared(handshake, m_Connection);
                m_Connection = null;
                Status = SimulationSessionPreparationStatus.Ready;
                return Status;
            }
            catch (Exception exception)
            {
                return Fail("server_authoritative_source_preparation_failed", exception.Message);
            }
        }

        public ISimulationSessionPreparedSource TakePreparedSource()
        {
            ThrowIfDisposed();
            if (Status != SimulationSessionPreparationStatus.Ready || m_Prepared == null)
                throw new InvalidOperationException("ServerAuthoritative prepared Source is unavailable.");
            ISimulationSessionPreparedSource prepared = m_Prepared;
            m_Prepared = null;
            return prepared;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Prepared?.Dispose();
            m_Prepared = null;
            DisposePendingResources();
            m_Connection?.Dispose();
            m_Connection = null;
        }

        protected abstract void ValidateHandshake(ServerAuthoritativeEndpointHandshake handshake);
        protected abstract ISimulationSessionPreparedSource BuildPrepared(
            ServerAuthoritativeEndpointHandshake handshake,
            IServerAuthoritativeEndpointConnection connection);
        protected virtual void DisposePendingResources() { }

        protected void Initialize(IReadOnlyList<IFloat32SimulationActorRegistration> registrations)
        {
            if (m_Connection != null || Compatibility != null)
                throw new InvalidOperationException("ServerAuthoritative Source preparation was initialized more than once.");
            if (registrations == null || registrations.Count == 0)
                throw new InvalidOperationException("ServerAuthoritative Source preparation has no locked Actor registrations.");
            Compatibility = BuildCompatibility(registrations);
            Diagnostics = new Float32SimulationDiagnosticsAggregate(registrations);
            m_Connection = Model.Endpoint.CreateConnection(
                m_Process,
                Compatibility,
                Model.Policy,
                registrations[0].Program,
                m_DataPlane,
                Context.WorldIdentity,
                m_ModelConfigurationHash,
                Diagnostics) ??
                throw new InvalidOperationException("Fantasy Endpoint returned no connection.");
        }

        ServerAuthoritativePipelineCompatibilityIdentity BuildCompatibility(
            IReadOnlyList<IFloat32SimulationActorRegistration> registrations)
        {
            ServerAuthoritativePipelineCompatibilityIdentity identity = Model.BuildCompatibility(
                registrations[0],
                Context.ProgramRuntime,
                Context.ExecutionBackend);
            for (int i = 1; i < registrations.Count; i++)
            {
                ServerAuthoritativePipelineCompatibilityIdentity candidate = Model.BuildCompatibility(
                    registrations[i],
                    Context.ProgramRuntime,
                    Context.ExecutionBackend);
                if (candidate.CompatibilityHash != identity.CompatibilityHash)
                    throw new InvalidOperationException("ServerAuthoritative Actor roster does not share one Program/Pipeline compatibility identity.");
            }
            return identity;
        }

        static SimulationSessionSourceDescriptor BuildDescriptor(GameplayNetworkModelSourceRequirements requirements)
        {
            var identity = new SimulationComponentIdentity(
                SimulationComponentRole.SessionSource,
                requirements.SourceComponentId,
                requirements.SourceSemanticVersion,
                requirements.RequirementsHash);
            return new SimulationSessionSourceDescriptor(
                identity,
                requirements.NumericProfileId,
                requirements.TargetAbiVersion,
                requirements.OuterTickKind,
                requirements.ExecutionSupport,
                requirements.Deterministic,
                requirements.RequiredBackendId,
                requirements.RequiredPipelineId,
                requirements.Model,
                requirements.Endpoint,
                requirements.Protocol,
                requirements.RequiredSolverCapabilities,
                requirements.RequiredPasses,
                requirements.RequiredSourcePorts);
        }

        SimulationSessionPreparationStatus Fail(string code, string message)
        {
            Failure = new SimulationSessionFailure(
                SimulationSessionFailureStage.Preparation,
                code,
                message,
                Descriptor.Identity.ToString());
            Status = SimulationSessionPreparationStatus.Failed;
            return Status;
        }

        void ThrowIfDisposed()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(GetType().Name);
        }
    }

    internal sealed class ServerAuthoritativePredictionSourcePreparation : ServerAuthoritativeSourcePreparation
    {
        ILocalSimulationActorRegistration m_Owner;
        readonly string m_RemotePresentationBindingId;

        public ServerAuthoritativePredictionSourcePreparation(
            ServerAuthoritativeHybridModelDefinition model,
            ServerAuthoritativeLaunchDefinition launch,
            GameplayNetworkModelPreparationContext context,
            GameplayNetworkModelSourceRequirements requirements)
            : base(model, launch, context, requirements)
        {
            m_RemotePresentationBindingId = launch.RemotePresentationBindingId;
            Initialize(RequireRegistrations(context.Registrations));
        }

        IReadOnlyList<IFloat32SimulationActorRegistration> RequireRegistrations(
            IReadOnlyList<ISimulationActorRegistration> registrations)
        {
            if (registrations == null || registrations.Count != 1 || registrations[0] is not ILocalSimulationActorRegistration owner)
                throw new InvalidOperationException("Prediction Source requires exactly one local Float32 owner registration.");
            if (owner.ActorId != Process.ActorId || owner.LocalInput == null)
                throw new InvalidOperationException("Prediction Source owner registration does not match its launch ActorId or local input.");
            m_Owner = owner;
            return new IFloat32SimulationActorRegistration[] { owner };
        }

        protected override void ValidateHandshake(ServerAuthoritativeEndpointHandshake handshake)
        {
            RequireExpectedAuthorityHost(handshake);
            if (!handshake.Process.Equals(Process) || handshake.Compatibility.CompatibilityHash != Compatibility.CompatibilityHash)
                throw new InvalidOperationException("Prediction handshake identity does not match the locked launch/model identity.");
            int ownerCount = 0;
            for (int i = 0; i < handshake.Roster.Count; i++)
            {
                if (handshake.Roster[i].ActorId == Process.ActorId && handshake.Roster[i].PlayerId.Equals(Process.PlayerId))
                    ownerCount++;
            }
            if (ownerCount != 1)
                throw new InvalidOperationException("Prediction handshake roster does not contain the exact owner route.");
        }

        protected override ISimulationSessionPreparedSource BuildPrepared(
            ServerAuthoritativeEndpointHandshake handshake,
            IServerAuthoritativeEndpointConnection connection)
        {
            var prediction = connection as IServerAuthoritativePredictionEndpointConnection ??
                throw new InvalidOperationException("Prediction Source received an Authority Endpoint connection.");
            var input = new Float32LocalInputSourcePort(
                Descriptor.Identity,
                new[] { new LocalSimulationInputBinding(m_Owner.ActorId, m_Owner.LocalInput) });
            var observation = new ServerAuthoritativeObservationPort(Descriptor.Identity, prediction);
            var restore = new ServerAuthoritativePredictionRestorePort(Descriptor.Identity);
            ActorId remoteActorId = RequireRemoteActor(handshake);
            var state = new ServerAuthoritativePredictionStatePort(
                Descriptor.Identity,
                Model.Policy,
                restore,
                m_Owner.Program,
                Compatibility,
                handshake.AuthorityWorld,
                new[] { remoteActorId });
            var send = new ServerAuthoritativeNetworkSendPort(
                Descriptor.Identity,
                ServerAuthoritativeSourcePortContracts.PredictionSend,
                prediction.Send,
                prediction);
            ServerAuthoritativeRemotePresentationRegistration remote = null;
            try
            {
                remote = ServerAuthoritativeRemotePresentationSiteRegistry.Claim(
                    m_RemotePresentationBindingId,
                    remoteActorId,
                    m_Owner.Program,
                    Context.TickRate,
                    Diagnostics);
                var committedOutput = new ServerAuthoritativePredictionCommittedOutputPort(send, remote, prediction);
                var prepared = new ServerAuthoritativePreparedSource(
                    Descriptor,
                    new SimulationRuntimePortSet(new ISimulationRuntimePort[] { input, observation, restore, state, send }),
                    restore,
                    committedOutput,
                    new Float32StandardSessionRuntimeLauncher(Descriptor),
                    prediction,
                    remote);
                remote = null;
                return prepared;
            }
            finally
            {
                remote?.Dispose();
            }
        }

        ActorId RequireRemoteActor(ServerAuthoritativeEndpointHandshake handshake)
        {
            ActorId remote = default;
            for (int i = 0; i < handshake.Roster.Count; i++)
            {
                ActorId actorId = handshake.Roster[i].ActorId;
                if (actorId == m_Owner.ActorId)
                    continue;
                if (remote.IsValid)
                    throw new InvalidOperationException("Prediction handshake contains more than one remote Actor.");
                remote = actorId;
            }
            return remote.IsValid
                ? remote
                : throw new InvalidOperationException("Prediction handshake contains no remote Actor.");
        }
    }

    internal sealed class ServerAuthoritativeAuthoritySourcePreparation : ServerAuthoritativeSourcePreparation
    {
        IReadOnlyList<IFloat32SimulationActorRegistration> m_Roster;
        readonly ServerAuthoritativeAuthoritySourcePolicy m_SourcePolicy;
        ServerAuthoritativeAuthoritySourceRuntime m_Runtime;

        public ServerAuthoritativeAuthoritySourcePreparation(
            ServerAuthoritativeAuthoritySessionSourceDefinition definition,
            GameplayNetworkModelPreparationContext context,
            GameplayNetworkModelSourceRequirements requirements)
            : base(
                definition ? definition.Model : throw new ArgumentNullException(nameof(definition)),
                definition.Launch,
                context,
                requirements)
        {
            m_SourcePolicy = definition.BuildPolicy();
            UnityAuthorityHostProduct.Descriptor.RequireAuthoritySolver(context.WorldSolver);
            IReadOnlyList<IFloat32SimulationActorRegistration> roster = RequireRegistrations(context.Registrations);
            Initialize(roster);
            var authority = Connection as IServerAuthoritativeAuthorityEndpointConnection ??
                throw new InvalidOperationException("Authority Source received a Prediction Endpoint connection.");
            var actorIds = new ActorId[roster.Count];
            for (int i = 0; i < actorIds.Length; i++)
                actorIds[i] = roster[i].ActorId;
            m_Runtime = new ServerAuthoritativeAuthoritySourceRuntime(
                Descriptor,
                m_SourcePolicy,
                UnityAuthorityHostProduct.CreateWorkerHostIdentity(Process),
                actorIds,
                roster[0].Program,
                authority,
                authority,
                Diagnostics);
            authority.AttachSourceRuntime(m_Runtime);
        }

        IReadOnlyList<IFloat32SimulationActorRegistration> RequireRegistrations(
            IReadOnlyList<ISimulationActorRegistration> registrations)
        {
            if (registrations == null || registrations.Count != 2)
                throw new InvalidOperationException("Authority Source requires exactly two Float32 Actor registrations.");
            var values = new IFloat32SimulationActorRegistration[2];
            for (int i = 0; i < values.Length; i++)
                values[i] = registrations[i] as IFloat32SimulationActorRegistration ??
                    throw new InvalidOperationException("Authority Source roster contains a non-Float32 Actor registration.");
            Array.Sort(values, (left, right) => left.ActorId.CompareTo(right.ActorId));
            if (values[0].ActorId == values[1].ActorId)
                throw new InvalidOperationException("Authority Source roster contains duplicate ActorId.");
            m_Roster = values;
            return values;
        }

        protected override void ValidateHandshake(ServerAuthoritativeEndpointHandshake handshake)
        {
            RequireExpectedAuthorityHost(handshake);
            if (!handshake.Process.Equals(Process) ||
                !handshake.AuthorityHost.Equals(UnityAuthorityHostProduct.CreateWorkerHostIdentity(Process)) ||
                handshake.Compatibility.CompatibilityHash != Compatibility.CompatibilityHash)
            {
                throw new InvalidOperationException("Authority handshake identity does not match the locked launch/model identity.");
            }
            if (handshake.Roster.Count != m_Roster.Count)
                throw new InvalidOperationException("Authority handshake roster count does not match the canonical simulation roster.");
            for (int i = 0; i < m_Roster.Count; i++)
            {
                if (handshake.Roster[i].ActorId != m_Roster[i].ActorId)
                    throw new InvalidOperationException("Authority handshake roster does not match the canonical Actor order.");
            }
        }

        protected override ISimulationSessionPreparedSource BuildPrepared(
            ServerAuthoritativeEndpointHandshake handshake,
            IServerAuthoritativeEndpointConnection connection)
        {
            var authority = connection as IServerAuthoritativeAuthorityEndpointConnection ??
                throw new InvalidOperationException("Authority Source received a Prediction Endpoint connection.");
            ServerAuthoritativeAuthoritySourceRuntime runtime = m_Runtime ??
                throw new InvalidOperationException("Authority portable Source runtime is unavailable.");
            if (runtime.Policy.ConfigurationHash != m_SourcePolicy.ConfigurationHash || runtime.Roster.Count != handshake.Roster.Count)
                throw new InvalidOperationException("Authority portable Source identity changed during preparation.");
            for (int i = 0; i < runtime.Roster.Count; i++)
            {
                if (!runtime.Roster[i].Equals(handshake.Roster[i]))
                    throw new InvalidOperationException("Authority portable Source roster does not match the Fantasy handshake.");
            }
            ActorId[] lockedRoster = ActorIds(runtime.Roster);
            var prepared = new ServerAuthoritativePreparedSource(
                Descriptor,
                runtime.RuntimePorts,
                null,
                runtime.SourceEgress,
                new ServerAuthoritativeAuthoritySessionRuntimeLauncher(
                    Descriptor,
                    runtime.Policy,
                    Compatibility.AuthorityPipeline,
                    lockedRoster),
                runtime);
            m_Runtime = null;
            _ = authority;
            return prepared;
        }

        static ActorId[] ActorIds(IReadOnlyList<ServerAuthoritativeRosterEntry> roster)
        {
            var values = new ActorId[roster.Count];
            for (int i = 0; i < values.Length; i++)
                values[i] = roster[i].ActorId;
            return values;
        }

        protected override void DisposePendingResources()
        {
            m_Runtime?.Dispose();
            m_Runtime = null;
        }
    }

    internal class ServerAuthoritativePreparedSource : IFloat32SimulationSessionPreparedSource
    {
        readonly IDisposable[] m_Resources;
        bool m_Disposed;

        public ServerAuthoritativePreparedSource(
            SimulationSessionSourceDescriptor descriptor,
            SimulationRuntimePortSet runtimePorts,
            IFloat32SimulationRestoreSource restoreSource,
            IFloat32SourceEgressOutputPort sourceEgress,
            IFloat32SimulationSessionRuntimeLauncher runtimeLauncher,
            params IDisposable[] resources)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            RuntimePorts = runtimePorts ?? throw new ArgumentNullException(nameof(runtimePorts));
            RestoreSource = restoreSource;
            SourceEgress = sourceEgress ?? throw new ArgumentNullException(nameof(sourceEgress));
            RuntimeLauncher = runtimeLauncher ?? throw new ArgumentNullException(nameof(runtimeLauncher));
            if (resources == null || resources.Length == 0)
                throw new ArgumentException("ServerAuthoritative prepared Source requires owned resources.", nameof(resources));
            m_Resources = new IDisposable[resources.Length];
            for (int i = 0; i < resources.Length; i++)
                m_Resources[i] = resources[i] ?? throw new ArgumentException("ServerAuthoritative prepared Source contains a missing resource.", nameof(resources));
        }

        public SimulationSessionSourceDescriptor Descriptor { get; }
        public SimulationRuntimePortSet RuntimePorts { get; }
        public IFloat32SimulationSessionRuntimeLauncher RuntimeLauncher { get; }
        public IFloat32SimulationRestoreSource RestoreSource { get; }
        public IFloat32SourceEgressOutputPort SourceEgress { get; }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            var failures = new List<Exception>();
            for (int i = m_Resources.Length - 1; i >= 0; i--)
            {
                try
                {
                    m_Resources[i].Dispose();
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
            if (failures.Count != 0)
                throw new AggregateException("ServerAuthoritative prepared Source failed to release completely.", failures);
        }
    }

    internal sealed class ServerAuthoritativePredictionCommittedOutputPort : IFloat32SourceEgressOutputPort
    {
        readonly ServerAuthoritativeNetworkSendPort m_Network;
        readonly ServerAuthoritativeRemotePresentationRegistration m_Remote;
        readonly IServerAuthoritativePredictionEndpointConnection m_Prediction;

        public ServerAuthoritativePredictionCommittedOutputPort(
            ServerAuthoritativeNetworkSendPort network,
            ServerAuthoritativeRemotePresentationRegistration remote,
            IServerAuthoritativePredictionEndpointConnection prediction)
        {
            m_Network = network ?? throw new ArgumentNullException(nameof(network));
            m_Remote = remote ?? throw new ArgumentNullException(nameof(remote));
            m_Prediction = prediction ?? throw new ArgumentNullException(nameof(prediction));
        }

        public void Commit(Float32SourceEgressRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            if (string.Equals(record.ChannelId, ServerAuthoritativeEgressChannels.ClientInput, StringComparison.Ordinal))
            {
                RequireSchema(
                    record,
                    ServerAuthoritativeEgressChannels.ClientInputSchema,
                    ServerAuthoritativeEgressChannels.SchemaVersion);
                m_Network.Commit(record);
                return;
            }
            if (string.Equals(record.ChannelId, ServerAuthoritativeEgressChannels.RemotePresentation, StringComparison.Ordinal))
            {
                RequireSchema(
                    record,
                    ServerAuthoritativeEgressChannels.RemotePresentationSchema,
                    ServerAuthoritativeEgressChannels.RemotePresentationSchemaVersion);
                RemotePresentationBatch batch = ServerAuthoritativeEgressCodec.ReadRemotePresentation(record.CopyPayload());
                if (batch.ActorId != record.ActorId || batch.ActorId != m_Remote.ActorId)
                    throw new InvalidOperationException("Remote Presentation egress Actor identity does not match its registration.");
                m_Remote.Commit(batch);
                ulong eventHorizon = 0;
                for (int i = 0; i < batch.ReliableEvents.Count; i++)
                    eventHorizon = Math.Max(eventHorizon, batch.ReliableEvents[i].Header.Sequence);
                if (eventHorizon != 0)
                    m_Prediction.AcknowledgeRemoteEvents(eventHorizon);
                return;
            }
            throw new InvalidOperationException($"Prediction Source cannot commit unknown egress channel '{record.ChannelId}'.");
        }

        static void RequireSchema(Float32SourceEgressRecord record, string schemaId, int schemaVersion)
        {
            if (!string.Equals(record.SchemaId, schemaId, StringComparison.Ordinal) || record.SchemaVersion != schemaVersion)
                throw new InvalidOperationException($"Source egress channel '{record.ChannelId}' has an incompatible schema.");
        }
    }

    internal sealed class ServerAuthoritativeObservationPort : IServerAuthoritativeObservationSourcePort
    {
        readonly IServerAuthoritativePredictionEndpointConnection m_Connection;

        public ServerAuthoritativeObservationPort(
            SimulationComponentIdentity source,
            IServerAuthoritativePredictionEndpointConnection connection)
        {
            m_Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            Descriptor = SimulationPortDescriptor.CreateSource(
                ServerAuthoritativeSourcePortContracts.Observation,
                source);
        }

        public SimulationPortDescriptor Descriptor { get; }
        public AuthoritativeObservationBatch Drain(SimulationTickSourceIdentity source) => m_Connection.DrainObservations(source);
    }

    internal sealed class ServerAuthoritativeNetworkSendPort : IServerAuthoritativeNetworkSendPort
    {
        readonly Action<Float32SourceEgressRecord> m_Send;
        readonly IDisposable m_Owner;

        public ServerAuthoritativeNetworkSendPort(
            SimulationComponentIdentity source,
            SimulationPipelinePortRequirement requirement,
            Action<Float32SourceEgressRecord> send,
            IDisposable owner)
        {
            m_Send = send ?? throw new ArgumentNullException(nameof(send));
            m_Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Descriptor = SimulationPortDescriptor.CreateSource(
                requirement,
                source);
        }

        public SimulationPortDescriptor Descriptor { get; }

        public void Commit(Float32SourceEgressRecord record)
        {
            _ = m_Owner;
            m_Send(record ?? throw new ArgumentNullException(nameof(record)));
        }
    }
}
