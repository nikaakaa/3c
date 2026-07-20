using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;
using ThirdPersonSimulation.ServerAuthoritative.Transport;

namespace ThirdPersonGameplay.Networking.ServerAuthoritative
{
    internal enum ServerAuthoritativeEndpointConnectionStatus : byte
    {
        Pending = 1,
        Ready = 2,
        Failed = 3
    }

    internal sealed class ServerAuthoritativeEndpointFailure
    {
        public ServerAuthoritativeEndpointFailure(string code, string message)
        {
            Code = string.IsNullOrWhiteSpace(code) ? throw new ArgumentException("Endpoint failure code is required.", nameof(code)) : code.Trim();
            Message = string.IsNullOrWhiteSpace(message) ? throw new ArgumentException("Endpoint failure message is required.", nameof(message)) : message.Trim();
        }

        public string Code { get; }
        public string Message { get; }
    }

    internal sealed class ServerAuthoritativeEndpointHandshake
    {
        readonly ReadOnlyCollection<ServerAuthoritativeRosterEntry> m_Roster;

        public ServerAuthoritativeEndpointHandshake(
            ServerAuthoritativeProcessIdentity process,
            ServerAuthoritativeAuthorityHostIdentity authorityHost,
            ServerAuthoritativeWorldIdentity authorityWorld,
            ServerAuthoritativePipelineCompatibilityIdentity compatibility,
            IEnumerable<ServerAuthoritativeRosterEntry> roster)
        {
            if (!process.RoomId.IsValid || !authorityHost.IsValid || !authorityHost.RoomId.Equals(process.RoomId) || authorityWorld == null)
                throw new ArgumentException("ServerAuthoritative handshake identity is incomplete.");
            Process = process;
            AuthorityHost = authorityHost;
            AuthorityWorld = authorityWorld;
            Compatibility = compatibility ?? throw new ArgumentNullException(nameof(compatibility));
            var values = roster == null ? new List<ServerAuthoritativeRosterEntry>() : new List<ServerAuthoritativeRosterEntry>(roster);
            values.Sort();
            if (values.Count != 2)
                throw new ArgumentException("ServerAuthoritative demo handshake requires exactly two roster entries.", nameof(roster));
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0 && values[i - 1].ActorId.Equals(values[i].ActorId))
                    throw new ArgumentException("ServerAuthoritative handshake contains a duplicate ActorId.", nameof(roster));
            }
            m_Roster = values.AsReadOnly();
        }

        public ServerAuthoritativeProcessIdentity Process { get; }
        public ServerAuthoritativeAuthorityHostIdentity AuthorityHost { get; }
        public ServerAuthoritativeWorldIdentity AuthorityWorld { get; }
        public ServerAuthoritativePipelineCompatibilityIdentity Compatibility { get; }
        public IReadOnlyList<ServerAuthoritativeRosterEntry> Roster => m_Roster;
    }

    internal interface IServerAuthoritativeEndpointConnection : IDisposable
    {
        ServerAuthoritativeEndpointConnectionStatus Status { get; }
        ServerAuthoritativeEndpointFailure Failure { get; }
        void Step(SimulationSessionLogicTickContext context);
        ServerAuthoritativeEndpointHandshake TakeHandshake();
    }

    internal interface IServerAuthoritativePredictionEndpointConnection : IServerAuthoritativeEndpointConnection
    {
        AuthoritativeObservationBatch DrainObservations(SimulationTickSourceIdentity source);
        void AcknowledgeRemoteEvents(ulong eventHorizon);
        void Send(Float32SourceEgressRecord record);
    }

    internal interface IServerAuthoritativeAuthorityEndpointConnection :
        IServerAuthoritativeEndpointConnection,
        IServerAuthoritativeAuthorityControlTransport,
        IServerAuthoritativeAuthorityDataTransport
    {
        void AttachSourceRuntime(ServerAuthoritativeAuthoritySourceRuntime runtime);
    }
}
