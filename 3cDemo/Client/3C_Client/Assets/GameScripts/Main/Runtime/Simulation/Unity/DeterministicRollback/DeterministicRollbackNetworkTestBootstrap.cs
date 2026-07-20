using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback
{
    [DisallowMultipleComponent]
    public sealed class DeterministicRollbackNetworkTestBootstrap : MonoBehaviour
    {
        [SerializeField] string m_PeerSceneName = string.Empty;

        void Awake()
        {
            SceneManager.LoadScene(RequireScene(m_PeerSceneName), LoadSceneMode.Single);
        }

        static string RequireScene(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new System.InvalidOperationException("Rollback Bootstrap requires an explicit Peer Scene.")
                : value.Trim();
    }
}
