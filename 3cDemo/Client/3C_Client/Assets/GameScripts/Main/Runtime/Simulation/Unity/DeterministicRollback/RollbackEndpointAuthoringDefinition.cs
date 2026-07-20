using System;
using System.Net;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DeterministicRollback;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback
{
    [Serializable]
    public sealed class RollbackPeerLaunchProfile
    {
        [SerializeField] string m_ProfileId = string.Empty;
        [SerializeField] string m_PeerId = string.Empty;
        [SerializeField] string m_PlayerId = string.Empty;
        [SerializeField] string m_ActorId = string.Empty;
        [SerializeField] string m_LocalAddress = "127.0.0.1";
        [SerializeField] int m_LocalPort;

        public string ProfileId => Require(m_ProfileId, nameof(m_ProfileId));
        public string PeerId => Require(m_PeerId, nameof(m_PeerId));
        public string PlayerId => Require(m_PlayerId, nameof(m_PlayerId));
        public ActorId ActorId => new ActorId(Require(m_ActorId, nameof(m_ActorId)));

        public RollbackRosterEntry BuildRosterEntry() => new RollbackRosterEntry(PeerId, PlayerId, ActorId);

        public IPEndPoint BuildLocalEndPoint()
        {
            if (!IPAddress.TryParse(m_LocalAddress, out IPAddress address) || m_LocalPort <= 0 || m_LocalPort > 65535)
                throw new InvalidOperationException($"Rollback launch profile '{ProfileId}' has an invalid local UDP endpoint.");
            return new IPEndPoint(address, m_LocalPort);
        }

        static string Require(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException($"Rollback launch profile requires an explicit {field}.");
            return value;
        }
    }

    [CreateAssetMenu(fileName = "RollbackEndpoint", menuName = "3C/Simulation/Deterministic Rollback/Endpoint")]
    public sealed class RollbackEndpointAuthoringDefinition : ScriptableObject
    {
        [SerializeField] string m_RelayServerAddress = "127.0.0.1";
        [SerializeField] int m_RelayServerPort = 24100;
        [SerializeField] string m_RelayServerPeerId = "rollback-input-relay";
        [SerializeField] string m_SessionId = string.Empty;
        [SerializeField, Range(256, 1200)] int m_MaximumDatagramBytes = 1200;
        [SerializeField, Min(1)] int m_MaximumQueuedMessages = 512;
        [SerializeField, Min(1)] int m_MaximumFragmentsPerMessage = 512;
        [SerializeField, Min(1)] int m_ReliableResendMilliseconds = 50;
        [SerializeField, Min(1)] int m_InputRedundancyCount = 4;
        [SerializeField, Min(1)] int m_MaximumPreparationTicks = 600;
        [SerializeField] RollbackPeerLaunchProfile[] m_PeerProfiles = Array.Empty<RollbackPeerLaunchProfile>();

        public string RelayServerPeerId => Require(m_RelayServerPeerId, nameof(m_RelayServerPeerId));
        public int InputRedundancyCount => m_InputRedundancyCount > 0
            ? m_InputRedundancyCount
            : throw new InvalidOperationException($"Rollback Endpoint '{name}' requires a positive input redundancy count.");
        public int MaximumPreparationTicks => m_MaximumPreparationTicks > 0
            ? m_MaximumPreparationTicks
            : throw new InvalidOperationException($"Rollback Endpoint '{name}' requires a positive preparation Tick limit.");

        public RollbackEndpointDefinition Build()
        {
            return new RollbackEndpointDefinition(
                Require(m_RelayServerAddress, nameof(m_RelayServerAddress)),
                m_RelayServerPort,
                Require(m_SessionId, nameof(m_SessionId)),
                m_MaximumDatagramBytes,
                m_MaximumQueuedMessages,
                m_MaximumFragmentsPerMessage,
                m_ReliableResendMilliseconds);
        }

        public IPEndPoint BuildRelayServerEndPoint()
        {
            RollbackEndpointDefinition definition = Build();
            return new IPEndPoint(definition.Address, definition.Port);
        }

        public RollbackPeerLaunchProfile ResolvePeerProfile()
        {
            const string prefix = "--deterministic-rollback-profile=";
            string profileId = string.Empty;
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (!arguments[i].StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                if (profileId.Length != 0)
                    throw new InvalidOperationException("Rollback launch contains more than one Peer profile argument.");
                profileId = Require(arguments[i].Substring(prefix.Length), "launch profile argument");
            }
            if (profileId.Length == 0)
                throw new InvalidOperationException("Rollback Peer requires --deterministic-rollback-profile=<ProfileId>.");
            RollbackPeerLaunchProfile match = null;
            for (int i = 0; i < m_PeerProfiles.Length; i++)
            {
                RollbackPeerLaunchProfile profile = m_PeerProfiles[i] ??
                    throw new InvalidOperationException($"Rollback Endpoint '{name}' contains a missing Peer profile.");
                if (!string.Equals(profile.ProfileId, profileId, StringComparison.Ordinal))
                    continue;
                if (match != null)
                    throw new InvalidOperationException($"Rollback Endpoint '{name}' contains duplicate profile '{profileId}'.");
                match = profile;
            }
            return match ?? throw new InvalidOperationException($"Rollback Endpoint '{name}' has no Peer profile '{profileId}'.");
        }

        public RollbackRoster BuildRoster()
        {
            if (m_PeerProfiles == null || m_PeerProfiles.Length == 0)
                throw new InvalidOperationException($"Rollback Endpoint '{name}' requires at least one Peer profile.");
            var entries = new RollbackRosterEntry[m_PeerProfiles.Length];
            for (int i = 0; i < m_PeerProfiles.Length; i++)
            {
                RollbackPeerLaunchProfile profile = m_PeerProfiles[i] ??
                    throw new InvalidOperationException($"Rollback Endpoint '{name}' contains a missing Peer profile.");
                entries[i] = profile.BuildRosterEntry();
            }
            return new RollbackRoster(1, entries);
        }

        static string Require(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException($"Rollback Endpoint requires an explicit {field}.");
            return value;
        }
    }
}
