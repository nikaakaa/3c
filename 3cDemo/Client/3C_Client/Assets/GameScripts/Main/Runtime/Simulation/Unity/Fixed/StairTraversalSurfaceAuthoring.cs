using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    [DisallowMultipleComponent]
    public sealed class StairTraversalSurfaceAuthoring : MonoBehaviour
    {
        [SerializeField] string m_StairIdentity;
        [SerializeField] BoxCollider m_TraversalRampCollider;
        [SerializeField] Transform m_FootSurfaceRoot;
        [SerializeField] Transform m_LowerTransition;
        [SerializeField] Transform m_UpperTransition;

        public string StairIdentity => m_StairIdentity;
        public BoxCollider TraversalRampCollider => m_TraversalRampCollider;
        public Transform FootSurfaceRoot => m_FootSurfaceRoot;
        public Transform LowerTransition => m_LowerTransition;
        public Transform UpperTransition => m_UpperTransition;
    }
}
