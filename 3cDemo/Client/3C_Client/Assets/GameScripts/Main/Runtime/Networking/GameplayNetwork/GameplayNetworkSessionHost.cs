using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonGameplay.Networking
{
    [DisallowMultipleComponent]
    public sealed class GameplayNetworkSessionHost : MonoBehaviour
    {
        [SerializeField] GameplayNetworkModelDefinition m_ModelDefinition;

        readonly List<string> m_ConfigurationErrors = new List<string>();
        GameplayNetworkModelDefinition m_LockedDefinition;
        IGameplayNetworkModelSession m_Session;

        public GameplayNetworkModelDefinition ModelDefinition => m_ModelDefinition;
        public IGameplayNetworkModelSession Session => m_Session;
        public string ModelId => m_Session?.ModelId ?? string.Empty;
        public int BindingCount => m_Session?.BindingCount ?? 0;
        public IReadOnlyList<string> ConfigurationErrors => m_ConfigurationErrors;

        public bool EnsureSession()
        {
            if (m_Session != null)
            {
                if (m_ModelDefinition != m_LockedDefinition)
                    throw new InvalidOperationException("Gameplay network model definition cannot change while its session is active.");

                return true;
            }

            m_ConfigurationErrors.Clear();
            if (!m_ModelDefinition)
            {
                m_ConfigurationErrors.Add("GameplayNetworkSessionHost requires one model definition.");
                ReportConfigurationErrors();
                return false;
            }

            if (string.IsNullOrWhiteSpace(m_ModelDefinition.ModelId))
                m_ConfigurationErrors.Add($"{m_ModelDefinition.name}: model id is missing.");
            bool valid = m_ModelDefinition.CollectConfigurationErrors(m_ConfigurationErrors);
            if (!valid && m_ConfigurationErrors.Count == 0)
                m_ConfigurationErrors.Add($"{m_ModelDefinition.name}: model configuration is invalid.");
            if (m_ConfigurationErrors.Count != 0)
            {
                ReportConfigurationErrors();
                return false;
            }

            IGameplayNetworkModelSession session = m_ModelDefinition.CreateSession();
            if (session == null)
                throw new InvalidOperationException($"Network model '{m_ModelDefinition.ModelId}' returned no session.");
            if (!string.Equals(session.ModelId, m_ModelDefinition.ModelId, StringComparison.Ordinal))
            {
                session.Dispose();
                throw new InvalidOperationException(
                    $"Network model definition '{m_ModelDefinition.ModelId}' created session '{session.ModelId}'.");
            }

            m_LockedDefinition = m_ModelDefinition;
            m_Session = session;
            m_Session.LockConfiguration();
            return true;
        }

        public T RequireSession<T>() where T : class, IGameplayNetworkModelSession
        {
            if (!EnsureSession())
                throw new InvalidOperationException("Gameplay network session configuration is invalid.");
            if (m_Session is T typedSession)
                return typedSession;

            throw new InvalidOperationException(
                $"Gameplay network session '{m_Session.ModelId}' is not '{typeof(T).Name}'.");
        }

        void OnEnable()
        {
            EnsureSession();
        }

        void OnDisable()
        {
            DisposeSession();
        }

        void OnDestroy()
        {
            DisposeSession();
        }

        void DisposeSession()
        {
            m_Session?.Dispose();
            m_Session = null;
            m_LockedDefinition = null;
        }

        void ReportConfigurationErrors()
        {
            for (int i = 0; i < m_ConfigurationErrors.Count; i++)
                Debug.LogError(m_ConfigurationErrors[i], this);
        }
    }
}
