using System;
using ThirdPersonSimulation.ServerAuthoritative;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonGameplay.Networking.ServerAuthoritative
{
    public enum ServerAuthoritativeTestScenarioId : byte
    {
        ServerAuthoritativeClient = 1,
        UnityAuthorityWorker = 2,
        DotRecastAuthorityClient = 3
    }

    [DisallowMultipleComponent]
    public sealed class ServerAuthoritativeNetworkTestBootstrap : MonoBehaviour
    {
        const string ScenarioArgument = "--network-test-scenario=";
        const string RoleArgument = "--server-authoritative-role=";
        const string PlayerArgument = "--server-authoritative-player-id=";
        const string ActorArgument = "--server-authoritative-actor-id=";

        [SerializeField] string m_ClientSceneName = string.Empty;
        [SerializeField] string m_AuthoritySceneName = string.Empty;
        [SerializeField] string m_DotRecastClientSceneName = string.Empty;
#if UNITY_EDITOR
        [SerializeField] ServerAuthoritativeTestScenarioId m_EditorScenario;
        [SerializeField] ServerAuthoritativeProcessRole m_EditorRole;
#endif

        void Awake()
        {
            ServerAuthoritativeTestScenarioId scenario = ResolveSelection(
                out ServerAuthoritativeProcessRole role,
                out string expectedPlayerId,
                out string expectedActorId);
            if (scenario == ServerAuthoritativeTestScenarioId.UnityAuthorityWorker)
            {
                SceneManager.LoadScene(RequireSceneName(m_AuthoritySceneName, "Authority Scene"), LoadSceneMode.Single);
                return;
            }
            ServerAuthoritativeSceneLaunchSelection.SelectClient(role, expectedPlayerId, expectedActorId);
            string clientScene = scenario == ServerAuthoritativeTestScenarioId.DotRecastAuthorityClient
                ? RequireSceneName(m_DotRecastClientSceneName, "DotRecast Client Scene")
                : RequireSceneName(m_ClientSceneName, "Client Scene");
            SceneManager.LoadScene(clientScene, LoadSceneMode.Single);
        }

        ServerAuthoritativeTestScenarioId ResolveSelection(
            out ServerAuthoritativeProcessRole role,
            out string expectedPlayerId,
            out string expectedActorId)
        {
#if UNITY_EDITOR
            ServerAuthoritativeTestScenarioId scenario = m_EditorScenario;
            role = m_EditorRole;
            expectedPlayerId = string.Empty;
            expectedActorId = string.Empty;
#else
            string scenarioValue = RequireArgument(ScenarioArgument);
            string roleValue = RequireArgument(RoleArgument);
            ServerAuthoritativeTestScenarioId scenario = scenarioValue switch
            {
                "server-authoritative-client" => ServerAuthoritativeTestScenarioId.ServerAuthoritativeClient,
                "unity-authority-worker" => ServerAuthoritativeTestScenarioId.UnityAuthorityWorker,
                "dotrecast-authority-client" => ServerAuthoritativeTestScenarioId.DotRecastAuthorityClient,
                _ => throw new InvalidOperationException(
                    $"Command line '{ScenarioArgument}' must name a registered ServerAuthoritative test scenario.")
            };
            role = roleValue switch
            {
                "authority" => ServerAuthoritativeProcessRole.AuthorityWorker,
                "client-a" => ServerAuthoritativeProcessRole.ClientA,
                "client-b" => ServerAuthoritativeProcessRole.ClientB,
                _ => throw new InvalidOperationException(
                    $"Command line requires exactly one '{RoleArgument}authority|client-a|client-b' argument.")
            };
            bool dotRecastClient = scenario == ServerAuthoritativeTestScenarioId.DotRecastAuthorityClient;
            expectedPlayerId = dotRecastClient ? RequireArgument(PlayerArgument) : string.Empty;
            expectedActorId = dotRecastClient ? RequireArgument(ActorArgument) : string.Empty;
#endif
            bool authority = scenario == ServerAuthoritativeTestScenarioId.UnityAuthorityWorker;
            if (!Enum.IsDefined(typeof(ServerAuthoritativeTestScenarioId), scenario) ||
                !Enum.IsDefined(typeof(ServerAuthoritativeProcessRole), role) ||
                authority != (role == ServerAuthoritativeProcessRole.AuthorityWorker))
            {
                throw new InvalidOperationException("Network Test Bootstrap scenario and process role do not form a valid launch pair.");
            }
            return scenario;
        }

#if !UNITY_EDITOR
        static string RequireArgument(string prefix)
        {
            string value = null;
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (!arguments[i].StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                if (value != null)
                    throw new InvalidOperationException($"Command line contains duplicate '{prefix}' arguments.");
                value = arguments[i].Substring(prefix.Length);
            }
            return value ?? throw new InvalidOperationException($"Command line requires exactly one '{prefix}' argument.");
        }
#endif

        static string RequireSceneName(string value, string field)
        {
            return string.IsNullOrWhiteSpace(value)
                ? throw new InvalidOperationException($"Network Test Bootstrap requires an explicit {field}.")
                : value.Trim();
        }
    }

    internal static class ServerAuthoritativeSceneLaunchSelection
    {
        static ServerAuthoritativeProcessRole s_Role;
        static string s_ExpectedPlayerId = string.Empty;
        static string s_ExpectedActorId = string.Empty;
        static bool s_Pending;

        public static void SelectClient(
            ServerAuthoritativeProcessRole role,
            string expectedPlayerId,
            string expectedActorId)
        {
            if (role != ServerAuthoritativeProcessRole.ClientA && role != ServerAuthoritativeProcessRole.ClientB)
                throw new ArgumentOutOfRangeException(nameof(role));
            if (s_Pending)
                throw new InvalidOperationException("A ServerAuthoritative client Scene launch is already pending.");
            s_Role = role;
            s_ExpectedPlayerId = expectedPlayerId ?? string.Empty;
            s_ExpectedActorId = expectedActorId ?? string.Empty;
            s_Pending = true;
        }

        public static ServerAuthoritativeProcessRole TakeClientRole(
            out string expectedPlayerId,
            out string expectedActorId)
        {
            if (!s_Pending)
                throw new InvalidOperationException("ServerAuthoritative Client Scene was entered without a Bootstrap launch selection.");
            ServerAuthoritativeProcessRole role = s_Role;
            expectedPlayerId = s_ExpectedPlayerId;
            expectedActorId = s_ExpectedActorId;
            s_Role = default;
            s_ExpectedPlayerId = string.Empty;
            s_ExpectedActorId = string.Empty;
            s_Pending = false;
            return role;
        }
    }
}
