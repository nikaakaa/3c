using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    public sealed class DeterministicCollisionWorldAuthoring : MonoBehaviour
    {
        [SerializeField] string m_MapId = "deterministic-rollback-demo";
        [SerializeField, Min(1)] int m_QuantizationUnitsPerMeter = 1000;
        [SerializeField] Vector3 m_WorldBoundsCenter;
        [SerializeField] Vector3 m_WorldBoundsSize = new Vector3(100f, 20f, 100f);
        [SerializeField] DeterministicCollisionWorldAsset m_Output;

        public string MapId => m_MapId;
        public int QuantizationUnitsPerMeter => m_QuantizationUnitsPerMeter;
        public Bounds WorldBounds => new Bounds(m_WorldBoundsCenter, m_WorldBoundsSize);
        public DeterministicCollisionWorldAsset Output => m_Output;
    }
}
