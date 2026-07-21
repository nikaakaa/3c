using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonGameplay.Lab
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class GameplayLabBootstrap : MonoBehaviour
    {
        [SerializeField] GameplayLabSessionVariantDefinition[] m_Variants =
            Array.Empty<GameplayLabSessionVariantDefinition>();
        [SerializeField, Min(0)] int m_StartupVariantIndex;

        GameObject m_RuntimeRoot;
        SimulationSessionHost m_SessionHost;
        bool m_VariantLocked;

        public IReadOnlyList<GameplayLabSessionVariantDefinition> Variants =>
            m_Variants ?? Array.Empty<GameplayLabSessionVariantDefinition>();
        public int StartupVariantIndex => m_StartupVariantIndex;
        public GameplayLabSessionVariantDefinition Variant => RequireVariant();
        public SimulationSessionHost SessionHost => m_SessionHost;
        public bool VariantLocked => m_VariantLocked;

#if UNITY_EDITOR
        public void SetVariants(
            int startupVariantIndex,
            params GameplayLabSessionVariantDefinition[] variants)
        {
            m_Variants = variants == null
                ? throw new ArgumentNullException(nameof(variants))
                : (GameplayLabSessionVariantDefinition[])variants.Clone();
            m_StartupVariantIndex = startupVariantIndex;
            ValidateConfiguration();
        }

        public void SetStartupVariantIndex(int index)
        {
            if (m_VariantLocked)
                throw new InvalidOperationException("Gameplay Lab Variant is locked after Session startup.");
            m_StartupVariantIndex = index;
            _ = RequireVariant();
        }
#endif

        void Awake()
        {
            GameplayLabSessionVariantDefinition variant = RequireVariant();
            SimulationSessionHost[] existing = FindObjectsOfType<SimulationSessionHost>(true);
            if (existing.Length != 0)
                throw new InvalidOperationException("Gameplay Lab scene cannot contain a pre-instantiated SimulationSessionHost.");
            m_RuntimeRoot = Instantiate(variant.RuntimeRootPrefab);
            m_RuntimeRoot.name = variant.RuntimeRootPrefab.name;
            SimulationSessionHost[] hosts = m_RuntimeRoot.GetComponentsInChildren<SimulationSessionHost>(true);
            if (hosts.Length != 1)
                throw new InvalidOperationException($"Gameplay Lab runtime root '{m_RuntimeRoot.name}' must contain exactly one SimulationSessionHost.");
            m_SessionHost = hosts[0];
            variant.ValidateComposition(m_SessionHost.Composition);
            existing = FindObjectsOfType<SimulationSessionHost>(true);
            if (existing.Length != 1 || !ReferenceEquals(existing[0], m_SessionHost))
                throw new InvalidOperationException("Gameplay Lab runtime root created more than one Session Host.");
        }

        void Update()
        {
            if (!m_VariantLocked && m_SessionHost &&
                m_SessionHost.LifecycleState != SimulationSessionLifecycleState.Uninitialized)
            {
                m_VariantLocked = true;
            }
        }

        void OnDestroy()
        {
            if (m_RuntimeRoot)
                Destroy(m_RuntimeRoot);
            m_RuntimeRoot = null;
            m_SessionHost = null;
        }

        GameplayLabSessionVariantDefinition RequireVariant()
        {
            ValidateConfiguration();
            return m_Variants[m_StartupVariantIndex];
        }

        void ValidateConfiguration()
        {
            if (m_Variants == null || m_Variants.Length == 0)
                throw new InvalidOperationException("Gameplay Lab Bootstrap requires explicit Session Variants.");
            if (m_StartupVariantIndex < 0 || m_StartupVariantIndex >= m_Variants.Length)
                throw new InvalidOperationException("Gameplay Lab startup Variant index is outside the configured Variant list.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < m_Variants.Length; i++)
            {
                GameplayLabSessionVariantDefinition variant = m_Variants[i];
                if (!variant)
                    throw new InvalidOperationException($"Gameplay Lab Variant at index {i} is missing.");
                if (!ids.Add(variant.VariantId))
                    throw new InvalidOperationException($"Gameplay Lab Variant '{variant.VariantId}' is configured more than once.");
            }
        }
    }
}
