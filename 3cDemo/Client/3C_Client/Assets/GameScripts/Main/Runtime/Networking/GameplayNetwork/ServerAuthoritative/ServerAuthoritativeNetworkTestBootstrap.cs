using System;
using ThirdPersonSimulation.ServerAuthoritative;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonGameplay.Networking.ServerAuthoritative
{
    public enum ServerAuthoritativeTestScenarioId : byte
    {
        ServerAuthoritativeClient = 1,
        UnityAuthorityWorker = 2
    }

    [DisallowMultipleComponent]
    public sealed class ServerAuthoritativeNetworkTestBootstrap : MonoBehaviour
    {
        const string ScenarioArgument = "--network-test-scenario=";
        const string RoleArgument = "--server-authoritative-role=";

        [SerializeField] string m_ClientSceneName = string.Empty;
        [SerializeField] string m_AuthoritySceneName = string.Empty;
#if UNITY_EDITOR
        [SerializeField] ServerAuthoritativeTestScenarioId m_EditorScenario;
        [SerializeField] ServerAuthoritativeProcessRole m_EditorRole;
#endif

        void Awake()
        {
            ServerAuthoritativeTestScenarioId scenario = ResolveSelection(out ServerAuthoritativeProcessRole role);
            if (scenario == ServerAuthoritativeTestScenarioId.UnityAuthorityWorker)
            {
                SceneManager.LoadScene(RequireSceneName(m_AuthoritySceneName, "Authority Scene"), LoadSceneMode.Single);
                return;
            }
            ServerAuthoritativeSceneLaunchSelection.SelectClient(role);
            SceneManager.LoadScene(RequireSceneName(m_ClientSceneName, "Client Scene"), LoadSceneMode.Single);
        }

        ServerAuthoritativeTestScenarioId ResolveSelection(out ServerAuthoritativeProcessRole role)
        {
#if UNITY_EDITOR
            ServerAuthoritativeTestScenarioId scenario = m_EditorScenario;
            role = m_EditorRole;
#else
            string scenarioValue = RequireArgument(ScenarioArgument);
            string roleValue = RequireArgument(RoleArgument);
            ServerAuthoritativeTestScenarioId scenario = scenarioValue switch
            {
                "server-authoritative-client" => ServerAuthoritativeTestScenarioId.ServerAuthoritativeClient,
                "unity-authority-worker" => ServerAuthoritativeTestScenarioId.UnityAuthorityWorker,
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
        static bool s_Pending;

        public static void SelectClient(ServerAuthoritativeProcessRole role)
        {
            if (role != ServerAuthoritativeProcessRole.ClientA && role != ServerAuthoritativeProcessRole.ClientB)
                throw new ArgumentOutOfRangeException(nameof(role));
            if (s_Pending)
                throw new InvalidOperationException("A ServerAuthoritative client Scene launch is already pending.");
            s_Role = role;
            s_Pending = true;
        }

        public static ServerAuthoritativeProcessRole TakeClientRole()
        {
            if (!s_Pending)
                throw new InvalidOperationException("ServerAuthoritative Client Scene was entered without a Bootstrap launch selection.");
            ServerAuthoritativeProcessRole role = s_Role;
            s_Role = default;
            s_Pending = false;
            return role;
        }
    }
}
