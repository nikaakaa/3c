using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Equipment;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class EquipmentVisualProjectionBinding
    {
        [SerializeField] string m_VisualBindingId;
        [SerializeField] EquipmentVisualBindingKind m_Kind;
        [SerializeField] string m_RigBindingId;
        [SerializeField] string[] m_RendererBindingIds = Array.Empty<string>();
        [SerializeField] GameObject m_VisualPrefab;
        [SerializeField] string m_SocketBindingId;
        [SerializeField] Vector3 m_LocalPosition;
        [SerializeField] Quaternion m_LocalRotation = Quaternion.identity;
        [SerializeField] Vector3 m_LocalScale = Vector3.one;
        [SerializeField] EquipmentVisualLifecyclePolicy m_LifecyclePolicy;

        internal EquipmentVisualProjectionBinding(EquipmentVisualBindingDefinition source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            m_VisualBindingId = source.VisualBindingId.Value;
            m_Kind = source.Kind;
            m_RigBindingId = source.RigBindingId;
            m_RendererBindingIds = source.RendererBindingIds
                .Select(EquipmentSlotDefinition.Normalize)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            m_VisualPrefab = source.VisualPrefab;
            m_SocketBindingId = source.SocketBindingId;
            m_LocalPosition = source.LocalPosition;
            m_LocalRotation = source.LocalRotation;
            m_LocalScale = source.LocalScale;
            m_LifecyclePolicy = source.LifecyclePolicy;
        }

        public EquipmentVisualBindingId VisualBindingId => new EquipmentVisualBindingId(m_VisualBindingId);
        public EquipmentVisualBindingKind Kind => m_Kind;
        public string RigBindingId => m_RigBindingId ?? string.Empty;
        public IReadOnlyList<string> RendererBindingIds => m_RendererBindingIds ?? Array.Empty<string>();
        public GameObject VisualPrefab => m_VisualPrefab;
        public string SocketBindingId => m_SocketBindingId ?? string.Empty;
        public Vector3 LocalPosition => m_LocalPosition;
        public Quaternion LocalRotation => m_LocalRotation;
        public Vector3 LocalScale => m_LocalScale;
        public EquipmentVisualLifecyclePolicy LifecyclePolicy => m_LifecyclePolicy;
    }

    public sealed partial class CharacterPresentationProjection
    {
        [SerializeField] string m_ProjectionRevision = string.Empty;
        [SerializeField] EquipmentVisualProjectionBinding[] m_EquipmentVisualBindings = Array.Empty<EquipmentVisualProjectionBinding>();

        public string ProjectionRevision => m_ProjectionRevision;
        public IReadOnlyList<EquipmentVisualProjectionBinding> EquipmentVisualBindings =>
            m_EquipmentVisualBindings ?? Array.Empty<EquipmentVisualProjectionBinding>();
        public bool TryGetEquipmentVisualBinding(
            EquipmentVisualBindingId bindingId,
            out EquipmentVisualProjectionBinding binding)
        {
            for (int i = 0; i < EquipmentVisualBindings.Count; i++)
            {
                EquipmentVisualProjectionBinding candidate = EquipmentVisualBindings[i];
                if (candidate != null && candidate.VisualBindingId == bindingId)
                {
                    binding = candidate;
                    return true;
                }
            }
            binding = null;
            return false;
        }

        internal void SetEquipmentProjection(
            string projectionRevision,
            EquipmentVisualProjectionBinding[] visualBindings)
        {
            m_ProjectionRevision = projectionRevision ?? string.Empty;
            m_EquipmentVisualBindings = visualBindings ?? Array.Empty<EquipmentVisualProjectionBinding>();
        }
    }
}
