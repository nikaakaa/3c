using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CharacterWorldAwarePresentationBinding : MonoBehaviour
    {
        [SerializeField] Transform m_PresentationRoot;
        [SerializeField] Transform m_SelfColliderRoot;

        public Transform PresentationRoot => m_PresentationRoot;
        public Transform SelfColliderRoot => m_SelfColliderRoot;
        public int CharacterLayer => m_SelfColliderRoot ? m_SelfColliderRoot.gameObject.layer : -1;

        public void Configure(Transform presentationRoot, Transform selfColliderRoot)
        {
            m_PresentationRoot = presentationRoot;
            m_SelfColliderRoot = selfColliderRoot;
            RequireValid();
        }

        public void RequireValid()
        {
            if (!m_PresentationRoot || !m_SelfColliderRoot)
                throw new InvalidOperationException("World-Aware Presentation Binding requires Presentation Root and Self Collider Root.");
            if (m_SelfColliderRoot != m_PresentationRoot && !m_PresentationRoot.IsChildOf(m_SelfColliderRoot))
                throw new InvalidOperationException("World-Aware Presentation Root must belong to the Self Collider Root hierarchy.");
            if (!gameObject.scene.IsValid())
                throw new InvalidOperationException("World-Aware Presentation Binding requires a valid Scene world fixture.");
        }

        public bool IsSelfCollider(Collider collider) =>
            collider && collider.transform.IsChildOf(m_SelfColliderRoot);
    }
}
