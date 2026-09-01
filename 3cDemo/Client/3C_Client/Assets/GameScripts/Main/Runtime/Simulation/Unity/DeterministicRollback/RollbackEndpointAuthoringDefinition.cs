using System;
using System.IO;
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
        string m_LocalAddress = string.Empty;
        int m_LocalPort;

        public string ProfileId => Require(m_ProfileId, nameof(m_ProfileId));
        public string PeerId => Require(m_PeerId, nameof(m_PeerId));
        public string PlayerId => Require(m_PlayerId, nameof(m_PlayerId));
        public ActorId ActorId => new ActorId(Require(m_ActorId, nameof(m_ActorId)));

        public RollbackRosterEntry BuildRosterEntry() => new RollbackRosterEntry(PeerId, PlayerId, ActorId);

        public RollbackPeerLaunchProfile WithEndpoint(string address, int port) => new RollbackPeerLaunchProfile
        {
            m_ProfileId = m_ProfileId,
            m_PeerId = m_PeerId,
            m_PlayerId = m_PlayerId,
            m_ActorId = m_ActorId,
            m_LocalAddress = address,
            m_LocalPort = port
        };

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

    [Serializable]
    public sealed class RollbackPeerRunManifest
    {
        public int schemaVersion;
        public string candidateId = string.Empty;
        public string runId = string.Empty;
        public string sessionId = string.Empty;
        public string profileId = string.Empty;
        public string relayAddress = string.Empty;
        public int relayPort;
        public string localAddress = string.Empty;
        public int localPort;

        public void RequireValid()
        {
            if (schemaVersion != 1 || string.IsNullOrWhiteSpace(candidateId) || string.IsNullOrWhiteSpace(runId) ||
                string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(profileId) ||
                !IPAddress.TryParse(relayAddress, out _) || relayPort <= 0 || relayPort > 65535 ||
                !IPAddress.TryParse(localAddress, out _) || localPort <= 0 || localPort > 65535 ||
                relayPort == localPort)
                throw new InvalidOperationException("Rollback Peer Run Manifest不完整或endpoint无效。");
        }
    }

    [CreateAssetMenu(fileName = "RollbackEndpoint", menuName = "3C/Simulation/Deterministic Rollback/Endpoint")]
    public sealed class RollbackEndpointAuthoringDefinition : ScriptableObject
    {
        [SerializeField] string m_RelayServerPeerId = "rollback-input-relay";
        [SerializeField, Range(256, 1200)] int m_MaximumDatagramBytes = 1200;
        [SerializeField, Min(1)] int m_MaximumQueuedMessages = 512;
        [SerializeField, Min(1)] int m_MaximumFragmentsPerMessage = 512;
        [SerializeField, Min(1)] int m_ReliableResendMilliseconds = 50;
        [SerializeField, Min(1)] int m_InputRedundancyCount = 4;
        [SerializeField, Min(1)] int m_MaximumPreparationTicks = 600;
        [SerializeField] RollbackPeerLaunchProfile[] m_PeerProfiles = Array.Empty<RollbackPeerLaunchProfile>();

        public string RelayServerPeerId => Require(m_RelayServerPeerId, nameof(m_RelayServerPeerId));
        public int MaximumDatagramBytes => m_MaximumDatagramBytes;
        public int MaximumQueuedMessages => m_MaximumQueuedMessages;
        public int MaximumFragmentsPerMessage => m_MaximumFragmentsPerMessage;
        public int ReliableResendMilliseconds => m_ReliableResendMilliseconds;
        public int InputRedundancyCount => m_InputRedundancyCount > 0
            ? m_InputRedundancyCount
            : throw new InvalidOperationException($"Rollback Endpoint '{name}' requires a positive input redundancy count.");
        public int MaximumPreparationTicks => m_MaximumPreparationTicks > 0
            ? m_MaximumPreparationTicks
            : throw new InvalidOperationException($"Rollback Endpoint '{name}' requires a positive preparation Tick limit.");

        public RollbackEndpointDefinition Build()
        {
            RollbackPeerRunManifest run = ReadRunManifest();
            return new RollbackEndpointDefinition(
                run.relayAddress,
                run.relayPort,
                run.sessionId,
                m_MaximumDatagramBytes,
                m_MaximumQueuedMessages,
                m_MaximumFragmentsPerMessage,
                m_ReliableResendMilliseconds);
        }

        public IPEndPoint BuildRelayServerEndPoint()
        {
            RollbackPeerRunManifest run = ReadRunManifest();
            return new IPEndPoint(IPAddress.Parse(run.relayAddress), run.relayPort);
        }

        public StableHash BuildConfigurationHash() => RollbackEndpointDefinition.ComputeConfigurationHash(
            m_MaximumDatagramBytes,
            m_MaximumQueuedMessages,
            m_MaximumFragmentsPerMessage,
            m_ReliableResendMilliseconds);

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
            RollbackPeerRunManifest run = ReadRunManifest();
            if (!string.Equals(run.profileId, profileId, StringComparison.Ordinal))
                throw new InvalidOperationException("Rollback Peer profile与Run Manifest不匹配。");
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
            return (match ?? throw new InvalidOperationException($"Rollback Endpoint '{name}' has no Peer profile '{profileId}'."))
                .WithEndpoint(run.localAddress, run.localPort);
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

        static RollbackPeerRunManifest ReadRunManifest()
        {
            const string prefix = "--deterministic-rollback-run-manifest=";
            string path = string.Empty;
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (!arguments[i].StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                if (path.Length != 0)
                    throw new InvalidOperationException("Rollback launch contains more than one Run Manifest argument.");
                path = arguments[i].Substring(prefix.Length);
            }
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new InvalidOperationException("Rollback Peer requires an existing --deterministic-rollback-run-manifest path.");
            RollbackPeerRunManifest manifest = JsonUtility.FromJson<RollbackPeerRunManifest>(File.ReadAllText(path));
            manifest?.RequireValid();
            return manifest ?? throw new InvalidOperationException("Rollback Peer Run Manifest JSON is invalid.");
        }

        static string Require(string value, string field)
        {
            if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException($"Rollback Endpoint requires an explicit {field}.");
            return value;
        }
    }
}
