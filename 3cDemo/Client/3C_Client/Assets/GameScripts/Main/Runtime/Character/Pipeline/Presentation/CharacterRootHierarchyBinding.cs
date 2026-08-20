using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CharacterRootHierarchyBinding : MonoBehaviour
    {
        [SerializeField] Transform m_LogicRoot;
        [SerializeField] Transform m_VisualRoot;
        [SerializeField] Transform m_PoseRoot;

        public Transform LogicRoot => m_LogicRoot;
        public Transform VisualRoot => m_VisualRoot;
        public Transform PoseRoot => m_PoseRoot;

        public void Configure(Transform logicRoot, Transform visualRoot, Transform poseRoot)
        {
            m_LogicRoot = logicRoot;
            m_VisualRoot = visualRoot;
            m_PoseRoot = poseRoot;
            RequireValid();
        }

        public void RequireValid()
        {
            if (!m_LogicRoot || !m_VisualRoot || !m_PoseRoot)
                throw new InvalidOperationException("Character Root Hierarchy requires LogicRoot, VisualRoot, and PoseRoot.");
            if (m_LogicRoot != transform)
                throw new InvalidOperationException("Character Root Hierarchy binding must be attached to its LogicRoot.");
            if (m_VisualRoot == m_LogicRoot || m_VisualRoot.parent != m_LogicRoot)
                throw new InvalidOperationException("Character VisualRoot must be a direct child of LogicRoot.");
            if (m_PoseRoot == m_VisualRoot || m_PoseRoot == m_LogicRoot || m_PoseRoot.parent != m_VisualRoot)
                throw new InvalidOperationException("Character PoseRoot must be a direct child of VisualRoot.");
            RequireFinite(m_LogicRoot, nameof(LogicRoot));
            RequireFinite(m_VisualRoot, nameof(VisualRoot));
            RequireFinite(m_PoseRoot, nameof(PoseRoot));
            if (m_PoseRoot.localPosition.sqrMagnitude > 0.00000001f ||
                Quaternion.Angle(m_PoseRoot.localRotation, Quaternion.identity) > 0.001f ||
                (m_PoseRoot.localScale - Vector3.one).sqrMagnitude > 0.00000001f)
            {
                throw new InvalidOperationException("Character PoseRoot local transform must be identity.");
            }
        }

        public void ApplyLogicPose(Vector3 position, Quaternion rotation)
        {
            RequirePose(position, rotation, "LogicRoot");
            m_LogicRoot.SetPositionAndRotation(position, rotation.normalized);
        }

        public void ApplyVisualWorldPose(Vector3 position, Quaternion rotation)
        {
            RequirePose(position, rotation, "VisualRoot");
            Quaternion normalized = rotation.normalized;
            m_VisualRoot.SetLocalPositionAndRotation(
                m_LogicRoot.InverseTransformPoint(position),
                (Quaternion.Inverse(m_LogicRoot.rotation) * normalized).normalized);
        }

        static void RequireFinite(Transform value, string field)
        {
            if (!IsFinite(value.position) || !IsFinite(value.rotation) || !IsFinite(value.lossyScale))
                throw new InvalidOperationException($"Character {field} world transform must be finite.");
        }

        static void RequirePose(Vector3 position, Quaternion rotation, string field)
        {
            if (!IsFinite(position) || !IsFinite(rotation) || MagnitudeSquared(rotation) <= 0.00000001f)
                throw new InvalidOperationException($"Character {field} pose must be finite and normalized-capable.");
        }

        static bool IsFinite(Vector3 value) =>
            IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

        static bool IsFinite(Quaternion value) =>
            IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);

        static float MagnitudeSquared(Quaternion value) =>
            value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w;

        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
