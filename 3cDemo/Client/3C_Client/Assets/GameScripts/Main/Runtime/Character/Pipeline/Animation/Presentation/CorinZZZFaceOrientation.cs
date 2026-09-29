using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    [ExecuteAlways]
    public sealed class CorinZZZFaceOrientation : MonoBehaviour
    {
        private static readonly int HeadForwardId = Shader.PropertyToID("_HeadForward");
        private static readonly int HeadLeftId = Shader.PropertyToID("_HeadLeft");

        [SerializeField] private Transform m_headBone;
        [SerializeField] private Vector3 m_headForwardAxis = Vector3.up;
        [SerializeField] private Vector3 m_headLeftAxis = Vector3.forward;
        [SerializeField] private SkinnedMeshRenderer[] m_faceRenderers;

        private MaterialPropertyBlock m_propertyBlock;

        private void OnEnable()
        {
            UpdateOrientation();
        }

        private void LateUpdate()
        {
            UpdateOrientation();
        }

        private void UpdateOrientation()
        {
            if (m_headBone == null || m_faceRenderers == null)
            {
                return;
            }

            m_propertyBlock ??= new MaterialPropertyBlock();
            Vector3 headForward = m_headBone.TransformDirection(m_headForwardAxis).normalized;
            Vector3 headLeft = m_headBone.TransformDirection(m_headLeftAxis).normalized;

            foreach (SkinnedMeshRenderer faceRenderer in m_faceRenderers)
            {
                if (faceRenderer == null)
                {
                    continue;
                }

                faceRenderer.GetPropertyBlock(m_propertyBlock);
                m_propertyBlock.SetVector(HeadForwardId, headForward);
                m_propertyBlock.SetVector(HeadLeftId, headLeft);
                faceRenderer.SetPropertyBlock(m_propertyBlock);
            }
        }
    }
}
