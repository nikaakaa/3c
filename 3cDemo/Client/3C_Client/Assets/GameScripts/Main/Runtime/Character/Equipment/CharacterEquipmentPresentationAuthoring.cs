using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Equipment
{
    [Serializable]
    public sealed class EquipmentVisualBindingDefinition
    {
        [SerializeField] string m_VisualBindingId;
        [SerializeField] EquipmentVisualBindingKind m_Kind = EquipmentVisualBindingKind.ExistingRigObject;
        [SerializeField] string m_RigBindingId;
        [SerializeField] string[] m_RendererBindingIds = Array.Empty<string>();
        [SerializeField] GameObject m_VisualPrefab;
        [SerializeField] string m_SocketBindingId;
        [SerializeField] Vector3 m_LocalPosition;
        [SerializeField] Vector3 m_LocalEulerAngles;
        [SerializeField] Vector3 m_LocalScale = Vector3.one;
        [SerializeField] EquipmentVisualLifecyclePolicy m_LifecyclePolicy = EquipmentVisualLifecyclePolicy.KeepWhileEquipped;

        public string VisualBindingIdValue => EquipmentSlotDefinition.Normalize(m_VisualBindingId);
        public EquipmentVisualBindingId VisualBindingId => new EquipmentVisualBindingId(VisualBindingIdValue);
        public EquipmentVisualBindingKind Kind => m_Kind;
        public string RigBindingId => EquipmentSlotDefinition.Normalize(m_RigBindingId);
        public IReadOnlyList<string> RendererBindingIds => m_RendererBindingIds ?? Array.Empty<string>();
        public GameObject VisualPrefab => m_VisualPrefab;
        public string SocketBindingId => EquipmentSlotDefinition.Normalize(m_SocketBindingId);
        public Vector3 LocalPosition => m_LocalPosition;
        public Quaternion LocalRotation => Quaternion.Euler(m_LocalEulerAngles);
        public Vector3 LocalScale => m_LocalScale;
        public EquipmentVisualLifecyclePolicy LifecyclePolicy => m_LifecyclePolicy;

        internal bool CollectConfigurationErrors(HashSet<string> ids, List<string> errors, string owner)
        {
            bool valid = EquipmentSlotDefinition.RequireIdentity(
                VisualBindingIdValue,
                $"{owner}: Equipment Visual Binding",
                ids,
                errors);
            if (!Enum.IsDefined(typeof(EquipmentVisualBindingKind), m_Kind) ||
                !Enum.IsDefined(typeof(EquipmentVisualLifecyclePolicy), m_LifecyclePolicy))
            {
                errors?.Add($"{owner}: Equipment Visual Binding '{VisualBindingIdValue}' has an invalid policy.");
                valid = false;
            }
            if (!IsFinite(m_LocalPosition) || !IsFinite(m_LocalEulerAngles) || !IsFinite(m_LocalScale) ||
                m_LocalScale.x == 0f || m_LocalScale.y == 0f || m_LocalScale.z == 0f)
            {
                errors?.Add($"{owner}: Equipment Visual Binding '{VisualBindingIdValue}' has an invalid local pose.");
                valid = false;
            }
            if (m_Kind == EquipmentVisualBindingKind.ExistingRigObject)
            {
                if (string.IsNullOrEmpty(RigBindingId) || m_VisualPrefab || !string.IsNullOrEmpty(SocketBindingId))
                {
                    errors?.Add($"{owner}: ExistingRigObject '{VisualBindingIdValue}' requires only a Rig Binding identity.");
                    valid = false;
                }
                var rendererIds = new HashSet<string>(StringComparer.Ordinal);
                IReadOnlyList<string> bindings = RendererBindingIds;
                if (bindings.Count == 0)
                {
                    errors?.Add($"{owner}: ExistingRigObject '{VisualBindingIdValue}' has no Renderer Binding identities.");
                    valid = false;
                }
                for (int i = 0; i < bindings.Count; i++)
                {
                    string rendererId = EquipmentSlotDefinition.Normalize(bindings[i]);
                    if (string.IsNullOrEmpty(rendererId) || !rendererIds.Add(rendererId))
                    {
                        errors?.Add($"{owner}: ExistingRigObject '{VisualBindingIdValue}' Renderer Binding #{i} is missing or duplicated.");
                        valid = false;
                    }
                }
            }
            else if (m_Kind == EquipmentVisualBindingKind.SpawnedVisualAsset)
            {
                if (!m_VisualPrefab || string.IsNullOrEmpty(SocketBindingId) || !string.IsNullOrEmpty(RigBindingId) || RendererBindingIds.Count != 0)
                {
                    errors?.Add($"{owner}: SpawnedVisualAsset '{VisualBindingIdValue}' requires only a Prefab and Socket Binding identity.");
                    valid = false;
                }
            }
            return valid;
        }

        static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        }
    }

    [CreateAssetMenu(fileName = "CharacterEquipmentPresentationProfile", menuName = "3C/Character/Equipment Presentation Profile")]
    public sealed class CharacterEquipmentPresentationProfile : ScriptableObject
    {
        [SerializeField] EquipmentVisualBindingDefinition[] m_VisualBindings = Array.Empty<EquipmentVisualBindingDefinition>();

        public IReadOnlyList<EquipmentVisualBindingDefinition> VisualBindings =>
            m_VisualBindings ?? Array.Empty<EquipmentVisualBindingDefinition>();

        public bool TryGetBinding(EquipmentVisualBindingId bindingId, out EquipmentVisualBindingDefinition binding)
        {
            IReadOnlyList<EquipmentVisualBindingDefinition> bindings = VisualBindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                EquipmentVisualBindingDefinition candidate = bindings[i];
                if (candidate != null && candidate.VisualBindingId.Equals(bindingId))
                {
                    binding = candidate;
                    return true;
                }
            }
            binding = null;
            return false;
        }

        public bool CollectConfigurationErrors(CharacterEquipmentProfile gameplayProfile, List<string> errors)
        {
            bool valid = true;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<EquipmentVisualBindingDefinition> bindings = VisualBindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                EquipmentVisualBindingDefinition binding = bindings[i];
                if (binding == null)
                {
                    errors?.Add($"{name}: Equipment Visual Binding #{i} is missing.");
                    valid = false;
                }
                else
                {
                    valid &= binding.CollectConfigurationErrors(ids, errors, name);
                }
            }
            if (gameplayProfile == null)
            {
                errors?.Add($"{name}: matching Equipment Gameplay Profile is missing.");
                return false;
            }
            IReadOnlyList<EquipmentDefinition> equipment = gameplayProfile.Equipment;
            for (int i = 0; i < equipment.Count; i++)
            {
                EquipmentDefinition item = equipment[i];
                if (item && !bindings.Any(value => value != null && value.VisualBindingId.Equals(item.VisualBindingId)))
                {
                    errors?.Add($"{name}: Equipment '{item.EquipmentIdValue}' references unknown Visual Binding '{item.VisualBindingIdValue}'.");
                    valid = false;
                }
            }
            return valid;
        }
    }

    [Serializable]
    public sealed class EquipmentRigObjectBinding
    {
        [SerializeField] string m_RigBindingId;
        [SerializeField] string[] m_RendererBindingIds = Array.Empty<string>();
        [SerializeField] Renderer[] m_Renderers = Array.Empty<Renderer>();

        public string RigBindingId => EquipmentSlotDefinition.Normalize(m_RigBindingId);
        public IReadOnlyList<string> RendererBindingIds => m_RendererBindingIds ?? Array.Empty<string>();
        public IReadOnlyList<Renderer> Renderers => m_Renderers ?? Array.Empty<Renderer>();
    }

    [Serializable]
    public sealed class EquipmentSocketBinding
    {
        [SerializeField] string m_SocketBindingId;
        [SerializeField] Transform m_Socket;

        public string SocketBindingId => EquipmentSlotDefinition.Normalize(m_SocketBindingId);
        public Transform Socket => m_Socket;
    }

    public sealed class CharacterEquipmentRigBindingCatalog : MonoBehaviour
    {
        [SerializeField] EquipmentRigObjectBinding[] m_RigObjects = Array.Empty<EquipmentRigObjectBinding>();
        [SerializeField] EquipmentSocketBinding[] m_Sockets = Array.Empty<EquipmentSocketBinding>();

        public IReadOnlyList<EquipmentRigObjectBinding> RigObjects => m_RigObjects ?? Array.Empty<EquipmentRigObjectBinding>();
        public IReadOnlyList<EquipmentSocketBinding> Sockets => m_Sockets ?? Array.Empty<EquipmentSocketBinding>();

        public EquipmentRigObjectBinding RequireRigObject(string rigBindingId)
        {
            EquipmentRigObjectBinding binding = RigObjects.SingleOrDefault(value => value != null && string.Equals(value.RigBindingId, rigBindingId, StringComparison.Ordinal));
            return binding ?? throw new InvalidOperationException($"Equipment Rig Binding '{rigBindingId}' is absent.");
        }

        public Transform RequireSocket(string socketBindingId)
        {
            EquipmentSocketBinding binding = Sockets.SingleOrDefault(value => value != null && string.Equals(value.SocketBindingId, socketBindingId, StringComparison.Ordinal));
            if (binding == null || !binding.Socket)
                throw new InvalidOperationException($"Equipment Socket Binding '{socketBindingId}' is absent.");
            return binding.Socket;
        }
    }
}
