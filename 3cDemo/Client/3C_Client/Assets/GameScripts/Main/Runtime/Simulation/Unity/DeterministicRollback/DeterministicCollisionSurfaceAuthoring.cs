using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback
{
    public sealed class DeterministicCollisionSurfaceAuthoring : MonoBehaviour
    {
        [SerializeField] string m_SurfaceIdentity = "default";
        [SerializeField] string m_MaterialIdentity = "default";
        [SerializeField] bool m_Walkable = true;

        public string SurfaceIdentity => m_SurfaceIdentity;
        public string MaterialIdentity => m_MaterialIdentity;
        public bool Walkable => m_Walkable;
    }
}
