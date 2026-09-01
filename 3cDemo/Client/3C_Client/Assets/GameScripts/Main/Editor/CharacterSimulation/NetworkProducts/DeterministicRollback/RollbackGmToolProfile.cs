using ThirdPerson.Development.Gm;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    [CreateAssetMenu(menuName = "3C/Development/Rollback GM Tool Profile")]
    public sealed class RollbackGmToolProfile : ScriptableObject
    {
        [SerializeField] int m_MaximumMessageBytes = 65536;
        [SerializeField] int m_MaximumServerRequests = 16;
        [SerializeField] int m_MaximumQueuedQueries = 32;
        [SerializeField] int m_MaximumQueriesPerPump = 2;
        [SerializeField] int m_RelayTimeoutMilliseconds = 2000;
        [SerializeField] int m_ServerTimeoutMilliseconds = 4000;
        [SerializeField] int m_ClientTimeoutMilliseconds = 5000;
        [SerializeField] int m_MaximumClientRequests = 8;
        [SerializeField] int m_HistoryCapacity = 32;
        [SerializeField] int m_OutputCapacity = 64;
        [SerializeField] int m_MaximumOutputCharacters = 4096;

        public void RequireValid()
        {
            BuildPolicy().RequireValid();
        }

        public GmToolPolicy BuildPolicy() => new GmToolPolicy
        {
            maximumMessageBytes = m_MaximumMessageBytes,
            maximumServerRequests = m_MaximumServerRequests,
            maximumQueuedQueries = m_MaximumQueuedQueries,
            maximumQueriesPerPump = m_MaximumQueriesPerPump,
            relayTimeoutMilliseconds = m_RelayTimeoutMilliseconds,
            serverTimeoutMilliseconds = m_ServerTimeoutMilliseconds,
            clientTimeoutMilliseconds = m_ClientTimeoutMilliseconds,
            maximumClientRequests = m_MaximumClientRequests,
            historyCapacity = m_HistoryCapacity,
            outputCapacity = m_OutputCapacity,
            maximumOutputCharacters = m_MaximumOutputCharacters
        };
    }
}
