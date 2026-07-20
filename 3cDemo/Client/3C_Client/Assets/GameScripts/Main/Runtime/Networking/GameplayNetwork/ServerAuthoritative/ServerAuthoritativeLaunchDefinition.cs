using System;
using System.Net;
using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;
using UnityEngine;

namespace ThirdPersonGameplay.Networking.ServerAuthoritative
{
    [CreateAssetMenu(
        fileName = "ServerAuthoritativeLaunch",
        menuName = "3C/Networking/Server Authoritative Launch")]
    public sealed class ServerAuthoritativeLaunchDefinition : ScriptableObject
    {
        [SerializeField] ServerAuthoritativeProcessRole m_Role;
        [SerializeField] string m_RoomId = string.Empty;
        [SerializeField] string m_WorkerId = string.Empty;
        [SerializeField] string m_PlayerId = string.Empty;
        [SerializeField] string m_OwnerActorId = string.Empty;
        [SerializeField] string m_AuthorityHostProductId = string.Empty;
        [SerializeField] ServerAuthoritativeAuthorityHostRouteKind m_AuthorityHostRouteKind;
        [SerializeField] string m_RemotePresentationBindingId = string.Empty;
        [SerializeField] string m_DataBindHost = string.Empty;
        [SerializeField, Min(0)] int m_DataBindPort;
        [SerializeField] string m_DataAdvertisedHost = string.Empty;

        public ServerAuthoritativeProcessRole Role => Enum.IsDefined(typeof(ServerAuthoritativeProcessRole), m_Role)
            ? m_Role
            : throw new InvalidOperationException($"Launch Definition '{name}' requires an explicit process role.");
        public string RemotePresentationBindingId
        {
            get
            {
                if (Role == ServerAuthoritativeProcessRole.AuthorityWorker)
                    throw new InvalidOperationException($"Authority launch '{name}' has no Remote Presentation binding.");
                return Require(m_RemotePresentationBindingId, "RemotePresentationBindingId");
            }
        }

        public HostProductId AuthorityHostProductId => new HostProductId(Require(m_AuthorityHostProductId, "AuthorityHostProductId"));
        public ServerAuthoritativeAuthorityHostRouteKind AuthorityHostRouteKind =>
            Enum.IsDefined(typeof(ServerAuthoritativeAuthorityHostRouteKind), m_AuthorityHostRouteKind)
                ? m_AuthorityHostRouteKind
                : throw new InvalidOperationException($"Launch Definition '{name}' requires an explicit Authority Host route kind.");

        public void RequireAuthorityHost(ServerAuthoritativeAuthorityHostIdentity host)
        {
            if (!host.IsValid || !host.RoomId.Equals(BuildProcessIdentity().RoomId) ||
                !host.HostProductId.Equals(AuthorityHostProductId) ||
                host.RouteKind != AuthorityHostRouteKind)
            {
                throw new InvalidOperationException($"Launch Definition '{name}' rejected Authority Host '{host}'.");
            }
        }

        public ServerAuthoritativeProcessIdentity BuildProcessIdentity()
        {
            var roomId = new ServerAuthoritativeRoomId(Require(m_RoomId, "RoomId"));
            if (Role == ServerAuthoritativeProcessRole.AuthorityWorker)
            {
                if (!string.IsNullOrWhiteSpace(m_PlayerId) || !string.IsNullOrWhiteSpace(m_OwnerActorId) ||
                    !string.IsNullOrWhiteSpace(m_RemotePresentationBindingId))
                {
                    throw new InvalidOperationException($"Authority launch '{name}' cannot own Player, Actor, or Remote Presentation identity.");
                }
                return new ServerAuthoritativeProcessIdentity(
                    Role,
                    roomId,
                    new ServerAuthoritativeWorkerId(Require(m_WorkerId, "WorkerId")),
                    default,
                    default);
            }
            if (!string.IsNullOrWhiteSpace(m_WorkerId))
                throw new InvalidOperationException($"Client launch '{name}' cannot own a WorkerId.");
            _ = RemotePresentationBindingId;
            return new ServerAuthoritativeProcessIdentity(
                Role,
                roomId,
                default,
                new ServerAuthoritativePlayerId(Require(m_PlayerId, "PlayerId")),
                new ActorId(Require(m_OwnerActorId, "OwnerActorId")));
        }

        public ServerAuthoritativeDataPlaneLaunch BuildDataPlaneLaunch()
        {
            IPAddress bindAddress = ParseAddress(Require(m_DataBindHost, "DataBindHost"), "DataBindHost");
            if (Role == ServerAuthoritativeProcessRole.AuthorityWorker)
            {
                if (m_DataBindPort <= 0 || m_DataBindPort > 65535)
                    throw new InvalidOperationException($"Authority launch '{name}' requires an explicit UDP DataBindPort.");
                IPAddress advertised = ParseAddress(Require(m_DataAdvertisedHost, "DataAdvertisedHost"), "DataAdvertisedHost");
                return new ServerAuthoritativeDataPlaneLaunch(
                    new IPEndPoint(bindAddress, m_DataBindPort),
                    new IPEndPoint(advertised, m_DataBindPort));
            }
            if (m_DataBindPort != 0 || !string.IsNullOrWhiteSpace(m_DataAdvertisedHost))
                throw new InvalidOperationException($"Client launch '{name}' must use an ephemeral UDP bind and cannot advertise a worker endpoint.");
            return new ServerAuthoritativeDataPlaneLaunch(new IPEndPoint(bindAddress, 0), null);
        }

        public StableHash BuildLaunchHash()
        {
            ServerAuthoritativeProcessIdentity process = BuildProcessIdentity();
            ServerAuthoritativeDataPlaneLaunch dataPlane = BuildDataPlaneLaunch();
            _ = AuthorityHostProductId;
            _ = AuthorityHostRouteKind;
            return StableHash.Compute(
                "server-authoritative-launch/4",
                process.ToString(),
                dataPlane.BindEndPoint.ToString(),
                dataPlane.AdvertisedEndPoint?.ToString() ?? string.Empty,
                AuthorityHostProductId.Value,
                AuthorityHostRouteKind.ToString(),
                Role == ServerAuthoritativeProcessRole.AuthorityWorker ? string.Empty : RemotePresentationBindingId);
        }

        string Require(string value, string field) => string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Launch Definition '{name}' requires explicit {field}.")
            : value.Trim();

        IPAddress ParseAddress(string value, string field) => IPAddress.TryParse(value, out IPAddress address)
            ? address
            : throw new InvalidOperationException($"Launch Definition '{name}' has invalid {field} '{value}'.");

#if UNITY_EDITOR
        public void SetAuthoring(
            ServerAuthoritativeProcessRole role,
            string roomId,
            string workerId,
            string playerId,
            string ownerActorId,
            HostProductId authorityHostProductId,
            ServerAuthoritativeAuthorityHostRouteKind authorityHostRouteKind,
            string remotePresentationBindingId,
            string dataBindHost,
            int dataBindPort,
            string dataAdvertisedHost)
        {
            m_Role = role;
            m_RoomId = roomId ?? string.Empty;
            m_WorkerId = workerId ?? string.Empty;
            m_PlayerId = playerId ?? string.Empty;
            m_OwnerActorId = ownerActorId ?? string.Empty;
            m_AuthorityHostProductId = authorityHostProductId.IsValid ? authorityHostProductId.Value : string.Empty;
            m_AuthorityHostRouteKind = authorityHostRouteKind;
            m_RemotePresentationBindingId = remotePresentationBindingId ?? string.Empty;
            m_DataBindHost = dataBindHost ?? string.Empty;
            m_DataBindPort = dataBindPort;
            m_DataAdvertisedHost = dataAdvertisedHost ?? string.Empty;
            _ = BuildLaunchHash();
        }
#endif
    }

    public readonly struct ServerAuthoritativeDataPlaneLaunch
    {
        public ServerAuthoritativeDataPlaneLaunch(IPEndPoint bindEndPoint, IPEndPoint advertisedEndPoint)
        {
            BindEndPoint = bindEndPoint ?? throw new ArgumentNullException(nameof(bindEndPoint));
            if ((advertisedEndPoint == null) != (bindEndPoint.Port == 0))
                throw new ArgumentException("Authority and client data-plane endpoint ownership is inconsistent.");
            AdvertisedEndPoint = advertisedEndPoint;
        }

        public IPEndPoint BindEndPoint { get; }
        public IPEndPoint AdvertisedEndPoint { get; }
        public bool IsAuthority => AdvertisedEndPoint != null;
    }
}
