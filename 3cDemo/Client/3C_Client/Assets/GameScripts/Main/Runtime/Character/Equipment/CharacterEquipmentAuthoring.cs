using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Equipment
{
    [Serializable]
    public sealed class EquipmentSlotDefinition
    {
        [SerializeField] string m_SlotId;
        [SerializeField] EquipmentSlotRequirement m_Requirement = EquipmentSlotRequirement.Required;

        public string SlotIdValue => Normalize(m_SlotId);
        public EquipmentSlotId SlotId => new EquipmentSlotId(SlotIdValue);
        public EquipmentSlotRequirement Requirement => m_Requirement;

        internal bool CollectConfigurationErrors(string owner, HashSet<string> identities, List<string> errors)
        {
            bool valid = RequireIdentity(SlotIdValue, $"{owner}: Equipment Slot", identities, errors);
            if (m_Requirement != EquipmentSlotRequirement.Required && m_Requirement != EquipmentSlotRequirement.Optional)
            {
                errors?.Add($"{owner}: Equipment Slot '{SlotIdValue}' has an unknown requirement.");
                valid = false;
            }
            return valid;
        }

        internal static bool RequireIdentity(string value, string owner, HashSet<string> identities, List<string> errors)
        {
            if (string.IsNullOrEmpty(value))
            {
                errors?.Add($"{owner} identity is missing.");
                return false;
            }
            if (identities != null && !identities.Add(value))
            {
                errors?.Add($"{owner} identity '{value}' is duplicated.");
                return false;
            }
            return true;
        }

        internal static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    [Serializable]
    public sealed class EquipmentActionRouteDefinition
    {
        [SerializeField] string m_RouteId;
        [SerializeField] string m_OwnerSlotId;
        [SerializeField] string m_InputRequestId;
        [SerializeField] EquipmentRouteRequestConsumption m_RequestConsumption = EquipmentRouteRequestConsumption.OnActivated;
        [SerializeField] EquipmentRouteMissingImplementation m_MissingImplementation = EquipmentRouteMissingImplementation.ReturnFailure;

        public string RouteIdValue => EquipmentSlotDefinition.Normalize(m_RouteId);
        public EquipmentActionRouteId RouteId => new EquipmentActionRouteId(RouteIdValue);
        public string OwnerSlotIdValue => EquipmentSlotDefinition.Normalize(m_OwnerSlotId);
        public EquipmentSlotId OwnerSlotId => new EquipmentSlotId(OwnerSlotIdValue);
        public string InputRequestId => EquipmentSlotDefinition.Normalize(m_InputRequestId);
        public EquipmentRouteRequestConsumption RequestConsumption => m_RequestConsumption;
        public EquipmentRouteMissingImplementation MissingImplementation => m_MissingImplementation;

        internal bool CollectConfigurationErrors(
            CharacterPipelineDefinition definition,
            HashSet<string> routeIds,
            HashSet<string> slotIds,
            List<string> errors)
        {
            bool valid = EquipmentSlotDefinition.RequireIdentity(RouteIdValue, $"{definition.name}: Equipment Route", routeIds, errors);
            if (!slotIds.Contains(OwnerSlotIdValue))
            {
                errors?.Add($"{definition.name}: Equipment Route '{RouteIdValue}' references unknown Slot '{OwnerSlotIdValue}'.");
                valid = false;
            }
            bool inputExists = definition.InputProfile && definition.InputProfile.ActionRequests.Any(
                value => value != null && string.Equals(value.RequestId, InputRequestId, StringComparison.Ordinal));
            if (!inputExists)
            {
                errors?.Add($"{definition.name}: Equipment Route '{RouteIdValue}' references unknown Input Request '{InputRequestId}'.");
                valid = false;
            }
            if (m_RequestConsumption != EquipmentRouteRequestConsumption.OnActivated &&
                m_RequestConsumption != EquipmentRouteRequestConsumption.Always)
            {
                errors?.Add($"{definition.name}: Equipment Route '{RouteIdValue}' has an unknown request consumption policy.");
                valid = false;
            }
            if (m_MissingImplementation != EquipmentRouteMissingImplementation.ReturnFailure &&
                m_MissingImplementation != EquipmentRouteMissingImplementation.RejectComposition)
            {
                errors?.Add($"{definition.name}: Equipment Route '{RouteIdValue}' has an unknown missing implementation policy.");
                valid = false;
            }
            return valid;
        }
    }

    [Serializable]
    public sealed class EquipmentParameterSchema
    {
        [SerializeField] string m_ParameterId;
        [SerializeField] EquipmentParameterValueKind m_ValueKind = EquipmentParameterValueKind.Scalar;
        [SerializeField] bool m_Required = true;

        public string ParameterIdValue => EquipmentSlotDefinition.Normalize(m_ParameterId);
        public EquipmentParameterId ParameterId => new EquipmentParameterId(ParameterIdValue);
        public EquipmentParameterValueKind ValueKind => m_ValueKind;
        public bool Required => m_Required;

        internal bool CollectConfigurationErrors(string owner, HashSet<string> ids, List<string> errors)
        {
            bool valid = EquipmentSlotDefinition.RequireIdentity(ParameterIdValue, $"{owner}: Equipment Parameter", ids, errors);
            if (!Enum.IsDefined(typeof(EquipmentParameterValueKind), m_ValueKind))
            {
                errors?.Add($"{owner}: Equipment Parameter '{ParameterIdValue}' has an unknown value kind.");
                valid = false;
            }
            return valid;
        }
    }

    [Serializable]
    public sealed class EquipmentParameterValue
    {
        [SerializeField] string m_ParameterId;
        [SerializeField] EquipmentParameterValueKind m_ValueKind = EquipmentParameterValueKind.Scalar;
        [SerializeField] bool m_Boolean;
        [SerializeField] int m_Int32;
        [SerializeField] ulong m_UInt64;
        [SerializeField] float m_Scalar;
        [SerializeField] Vector2 m_Vector2;
        [SerializeField] Vector3 m_Vector3;
        [SerializeField] float m_YawDegrees;
        [SerializeField] GameplayTagId m_GameplayTag;
        [SerializeField] GameplayEffectDefinition m_GameplayEffect;
        [SerializeField] string m_AnimationProducerId;
        [SerializeField] string m_Identity;

        public string ParameterIdValue => EquipmentSlotDefinition.Normalize(m_ParameterId);
        public EquipmentParameterId ParameterId => new EquipmentParameterId(ParameterIdValue);
        public EquipmentParameterValueKind ValueKind => m_ValueKind;
        public bool Boolean => m_Boolean;
        public int Int32 => m_Int32;
        public ulong UInt64 => m_UInt64;
        public float Scalar => m_Scalar;
        public Vector2 Vector2 => m_Vector2;
        public Vector3 Vector3 => m_Vector3;
        public float YawDegrees => m_YawDegrees;
        public GameplayTagId GameplayTag => m_GameplayTag;
        public GameplayEffectDefinition GameplayEffect => m_GameplayEffect;
        public string AnimationProducerId => EquipmentSlotDefinition.Normalize(m_AnimationProducerId);
        public string Identity => EquipmentSlotDefinition.Normalize(m_Identity);

        internal bool IsFinite()
        {
            return m_ValueKind switch
            {
                EquipmentParameterValueKind.Scalar => float.IsFinite(m_Scalar),
                EquipmentParameterValueKind.Vector2 => float.IsFinite(m_Vector2.x) && float.IsFinite(m_Vector2.y),
                EquipmentParameterValueKind.Vector3 => float.IsFinite(m_Vector3.x) && float.IsFinite(m_Vector3.y) && float.IsFinite(m_Vector3.z),
                EquipmentParameterValueKind.Yaw => float.IsFinite(m_YawDegrees),
                _ => true
            };
        }
    }

    [Serializable]
    public sealed class EquipmentLocalStateDeclaration
    {
        [SerializeField] string m_StateId;
        [SerializeField] ProgramStateValueKind m_ValueKind = ProgramStateValueKind.Boolean;
        [SerializeField] EquipmentParameterValue m_DefaultValue = new EquipmentParameterValue();

        public string StateIdValue => EquipmentSlotDefinition.Normalize(m_StateId);
        public EquipmentLocalStateId StateId => new EquipmentLocalStateId(StateIdValue);
        public ProgramStateValueKind ValueKind => m_ValueKind;
        public EquipmentParameterValue DefaultValue => m_DefaultValue;

        internal bool CollectConfigurationErrors(string owner, HashSet<string> ids, List<string> errors)
        {
            bool valid = EquipmentSlotDefinition.RequireIdentity(StateIdValue, $"{owner}: Equipment Local State", ids, errors);
            bool supported = m_ValueKind == ProgramStateValueKind.Boolean || m_ValueKind == ProgramStateValueKind.Int32 ||
                             m_ValueKind == ProgramStateValueKind.UInt64 || m_ValueKind == ProgramStateValueKind.Scalar ||
                             m_ValueKind == ProgramStateValueKind.Vector2 || m_ValueKind == ProgramStateValueKind.Vector3 ||
                             m_ValueKind == ProgramStateValueKind.Yaw || m_ValueKind == ProgramStateValueKind.Identity;
            if (!supported)
            {
                errors?.Add($"{owner}: Equipment Local State '{StateIdValue}' uses unsupported value kind '{m_ValueKind}'.");
                valid = false;
            }
            if (m_DefaultValue == null || !m_DefaultValue.IsFinite())
            {
                errors?.Add($"{owner}: Equipment Local State '{StateIdValue}' has an invalid default value.");
                valid = false;
            }
            return valid;
        }
    }

    [Serializable]
    public sealed class EquipmentFeatureRouteImplementation
    {
        [SerializeField] string m_RouteId;
        [SerializeField] ActionProfile m_ActionProfile;
        [SerializeReference] SubTree m_InlineGraph;
        [SerializeField] string[] m_RequiredParameterIds = Array.Empty<string>();
        [SerializeField] string[] m_RequiredProducerIds = Array.Empty<string>();

        public string RouteIdValue => EquipmentSlotDefinition.Normalize(m_RouteId);
        public EquipmentActionRouteId RouteId => new EquipmentActionRouteId(RouteIdValue);
        public ActionProfile ActionProfile => m_ActionProfile;
        public SubTree InlineGraph => m_InlineGraph;
        public IReadOnlyList<string> RequiredParameterIds => m_RequiredParameterIds ?? Array.Empty<string>();
        public IReadOnlyList<string> RequiredProducerIds => m_RequiredProducerIds ?? Array.Empty<string>();

        internal void BindSerializedOwner(CharacterEquipmentFeatureDefinition owner, int index)
        {
            if (m_InlineGraph == null)
                return;
            m_InlineGraph.BindSerializedOwner(owner, $"m_RouteImplementations.Array.data[{index}].m_InlineGraph");
        }

#if UNITY_EDITOR
        public void CreateInlineGraph(CharacterEquipmentFeatureDefinition owner, int index)
        {
            if (m_InlineGraph != null)
                throw new InvalidOperationException("Equipment Route inline graph already exists.");
            m_InlineGraph = CharacterEquipmentFeatureDefinition.CreateDefaultFeatureGraph("Equipment Route");
            BindSerializedOwner(owner, index);
        }
#endif
    }

    [CreateAssetMenu(fileName = "CharacterEquipmentFeatureDefinition", menuName = "3C/Character/Equipment Feature")]
    public sealed class CharacterEquipmentFeatureDefinition : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] string m_FeatureId;
        [SerializeField, Min(1)] ulong m_FeatureRevision = 1;
        [SerializeField] EquipmentParameterSchema[] m_Parameters = Array.Empty<EquipmentParameterSchema>();
        [SerializeField] EquipmentLocalStateDeclaration[] m_LocalStates = Array.Empty<EquipmentLocalStateDeclaration>();
        [SerializeField] GameplayTagId[] m_GrantedTags = Array.Empty<GameplayTagId>();
        [SerializeField] GameplayEffectDefinition[] m_PassiveEffects = Array.Empty<GameplayEffectDefinition>();
        [SerializeReference] SubTree m_PersistentGraph;
        [SerializeField] EquipmentFeatureRouteImplementation[] m_RouteImplementations = Array.Empty<EquipmentFeatureRouteImplementation>();
        [SerializeField] string[] m_RequiredGameplayCapabilities = Array.Empty<string>();
        [SerializeField] WorldCapability m_RequiredWorldCapabilities;

        public string FeatureIdValue => EquipmentSlotDefinition.Normalize(m_FeatureId);
        public EquipmentFeatureId FeatureId => new EquipmentFeatureId(FeatureIdValue);
        public EquipmentFeatureRevision FeatureRevision => new EquipmentFeatureRevision(Math.Max(1UL, m_FeatureRevision));
        public IReadOnlyList<EquipmentParameterSchema> Parameters => m_Parameters ?? Array.Empty<EquipmentParameterSchema>();
        public IReadOnlyList<EquipmentLocalStateDeclaration> LocalStates => m_LocalStates ?? Array.Empty<EquipmentLocalStateDeclaration>();
        public IReadOnlyList<GameplayTagId> GrantedTags => m_GrantedTags ?? Array.Empty<GameplayTagId>();
        public IReadOnlyList<GameplayEffectDefinition> PassiveEffects => m_PassiveEffects ?? Array.Empty<GameplayEffectDefinition>();
        public SubTree PersistentGraph => m_PersistentGraph;
        public IReadOnlyList<EquipmentFeatureRouteImplementation> RouteImplementations => m_RouteImplementations ?? Array.Empty<EquipmentFeatureRouteImplementation>();
        public IReadOnlyList<string> RequiredGameplayCapabilities => m_RequiredGameplayCapabilities ?? Array.Empty<string>();
        public WorldCapability RequiredWorldCapabilities => m_RequiredWorldCapabilities;

        void OnEnable() => BindGraphs();
        void OnValidate() => BindGraphs();
        public void OnBeforeSerialize() => BindGraphs();

        public void OnAfterDeserialize()
        {
            BindGraphs();
            m_PersistentGraph?.OnAfterDeserializeGraph();
            IReadOnlyList<EquipmentFeatureRouteImplementation> routes = RouteImplementations;
            for (int i = 0; i < routes.Count; i++)
                routes[i]?.InlineGraph?.OnAfterDeserializeGraph();
        }

        void BindGraphs()
        {
            m_PersistentGraph?.BindSerializedOwner(this, "m_PersistentGraph");
            IReadOnlyList<EquipmentFeatureRouteImplementation> routes = RouteImplementations;
            for (int i = 0; i < routes.Count; i++)
                routes[i]?.BindSerializedOwner(this, i);
        }

        public bool CollectConfigurationErrors(
            CharacterPipelineDefinition definition,
            CharacterEquipmentProfile equipmentProfile,
            List<string> errors)
        {
            bool valid = true;
            string owner = string.IsNullOrEmpty(FeatureIdValue) ? name : FeatureIdValue;
            if (string.IsNullOrEmpty(FeatureIdValue))
            {
                errors?.Add($"{name}: Equipment Feature identity is missing.");
                valid = false;
            }
            if (m_FeatureRevision == 0)
            {
                errors?.Add($"{name}: Equipment Feature revision must be positive.");
                valid = false;
            }
            var parameterIds = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<EquipmentParameterSchema> parameters = Parameters;
            for (int i = 0; i < parameters.Count; i++)
            {
                if (parameters[i] == null)
                {
                    errors?.Add($"{owner}: Equipment Parameter #{i} is missing.");
                    valid = false;
                }
                else valid &= parameters[i].CollectConfigurationErrors(owner, parameterIds, errors);
            }
            var stateIds = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<EquipmentLocalStateDeclaration> states = LocalStates;
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i] == null)
                {
                    errors?.Add($"{owner}: Equipment Local State #{i} is missing.");
                    valid = false;
                }
                else valid &= states[i].CollectConfigurationErrors(owner, stateIds, errors);
            }
            valid &= ValidateTagsAndEffects(definition, owner, errors);
            valid &= ValidateRoutes(equipmentProfile, owner, parameterIds, errors);
            var capabilities = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<string> requiredCapabilities = RequiredGameplayCapabilities;
            for (int i = 0; i < requiredCapabilities.Count; i++)
            {
                string capability = EquipmentSlotDefinition.Normalize(requiredCapabilities[i]);
                if (string.IsNullOrEmpty(capability) || !capabilities.Add(capability))
                {
                    errors?.Add($"{owner}: Gameplay capability #{i} is missing or duplicated.");
                    valid = false;
                }
            }
            return valid;
        }

        bool ValidateTagsAndEffects(CharacterPipelineDefinition definition, string owner, List<string> errors)
        {
            bool valid = true;
            GameplayTagCatalogRuntimeData tagCatalog = null;
            if (definition && definition.GameplayEffectProfile)
                definition.GameplayEffectProfile.CollectConfigurationErrors(out tagCatalog, null);
            var tags = new HashSet<GameplayTagId>();
            IReadOnlyList<GameplayTagId> grantedTags = GrantedTags;
            for (int i = 0; i < grantedTags.Count; i++)
            {
                GameplayTagId tag = grantedTags[i];
                if (!tag.IsValid || !tags.Add(tag) || tagCatalog == null || !tagCatalog.Contains(tag))
                {
                    errors?.Add($"{owner}: Granted Tag '{tag}' is missing, duplicated or absent from the Character catalog.");
                    valid = false;
                }
            }
            var effects = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<GameplayEffectDefinition> passiveEffects = PassiveEffects;
            for (int i = 0; i < passiveEffects.Count; i++)
            {
                GameplayEffectDefinition effect = passiveEffects[i];
                string effectId = effect ? effect.EffectId.Value : string.Empty;
                bool registered = definition && definition.GameplayEffectProfile &&
                    definition.GameplayEffectProfile.EffectDefinitions.Any(value => value == effect);
                if (string.IsNullOrEmpty(effectId) || !effects.Add(effectId) || !registered)
                {
                    errors?.Add($"{owner}: Passive Effect #{i} is missing, duplicated or absent from the Character catalog.");
                    valid = false;
                }
            }
            return valid;
        }

        bool ValidateRoutes(CharacterEquipmentProfile equipmentProfile, string owner, HashSet<string> parameterIds, List<string> errors)
        {
            bool valid = true;
            var routeIds = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<EquipmentFeatureRouteImplementation> routes = RouteImplementations;
            for (int i = 0; i < routes.Count; i++)
            {
                EquipmentFeatureRouteImplementation route = routes[i];
                if (route == null)
                {
                    errors?.Add($"{owner}: Route implementation #{i} is missing.");
                    valid = false;
                    continue;
                }
                string routeId = route.RouteIdValue;
                valid &= EquipmentSlotDefinition.RequireIdentity(routeId, $"{owner}: Route implementation", routeIds, errors);
                if (!equipmentProfile ||
                    !equipmentProfile.Routes.Any(value => value != null && string.Equals(value.RouteIdValue, routeId, StringComparison.Ordinal)))
                {
                    errors?.Add($"{owner}: Route implementation '{routeId}' is absent from the Character Equipment Profile.");
                    valid = false;
                }
                if (!route.ActionProfile)
                {
                    errors?.Add($"{owner}: Route implementation '{routeId}' has no ActionProfile.");
                    valid = false;
                }
                if (route.InlineGraph == null)
                {
                    errors?.Add($"{owner}: Route implementation '{routeId}' has no inline graph.");
                    valid = false;
                }
                else if (!ReferenceEquals(route.InlineGraph.SerializedOwner, this))
                {
                    errors?.Add($"{owner}: Route implementation '{routeId}' inline graph owner is invalid.");
                    valid = false;
                }
                for (int parameterIndex = 0; parameterIndex < route.RequiredParameterIds.Count; parameterIndex++)
                {
                    string parameterId = EquipmentSlotDefinition.Normalize(route.RequiredParameterIds[parameterIndex]);
                    if (!parameterIds.Contains(parameterId))
                    {
                        errors?.Add($"{owner}: Route implementation '{routeId}' requires unknown Parameter '{parameterId}'.");
                        valid = false;
                    }
                }
            }
            if (m_PersistentGraph != null && !ReferenceEquals(m_PersistentGraph.SerializedOwner, this))
            {
                errors?.Add($"{owner}: Persistent graph owner is invalid.");
                valid = false;
            }
            return valid;
        }

#if UNITY_EDITOR
        public void CreatePersistentGraph()
        {
            if (m_PersistentGraph != null)
                throw new InvalidOperationException("Equipment Persistent graph already exists.");
            m_PersistentGraph = CreateDefaultFeatureGraph("Equipment Persistent");
            BindGraphs();
        }

        public static SubTree CreateDefaultFeatureGraph(string graphName)
        {
            var tree = new SubTree { name = graphName };
            RootNode root = tree.CreateNode(typeof(RootNode)) as RootNode;
            root.Position = Vector2.zero;
            tree.RootGUID = root.GUID;
            return tree;
        }
#endif
    }

    [CreateAssetMenu(fileName = "EquipmentDefinition", menuName = "3C/Character/Equipment Definition")]
    public sealed class EquipmentDefinition : ScriptableObject
    {
        [SerializeField] string m_EquipmentId;
        [SerializeField] string m_SlotId;
        [SerializeField] CharacterEquipmentFeatureDefinition m_Feature;
        [SerializeField] EquipmentParameterValue[] m_ParameterValues = Array.Empty<EquipmentParameterValue>();
        [SerializeField] string m_VisualBindingId;

        public string EquipmentIdValue => EquipmentSlotDefinition.Normalize(m_EquipmentId);
        public EquipmentId EquipmentId => new EquipmentId(EquipmentIdValue);
        public string SlotIdValue => EquipmentSlotDefinition.Normalize(m_SlotId);
        public EquipmentSlotId SlotId => new EquipmentSlotId(SlotIdValue);
        public CharacterEquipmentFeatureDefinition Feature => m_Feature;
        public IReadOnlyList<EquipmentParameterValue> ParameterValues => m_ParameterValues ?? Array.Empty<EquipmentParameterValue>();
        public string VisualBindingIdValue => EquipmentSlotDefinition.Normalize(m_VisualBindingId);
        public EquipmentVisualBindingId VisualBindingId => new EquipmentVisualBindingId(VisualBindingIdValue);

        public bool CollectConfigurationErrors(CharacterEquipmentProfile profile, List<string> errors)
        {
            bool valid = true;
            if (string.IsNullOrEmpty(EquipmentIdValue) || string.IsNullOrEmpty(SlotIdValue) || string.IsNullOrEmpty(VisualBindingIdValue) || !m_Feature)
            {
                errors?.Add($"{name}: Equipment identity, Slot, Feature and Visual Binding are required.");
                valid = false;
            }
            if (profile == null || !profile.Slots.Any(value => value != null && string.Equals(value.SlotIdValue, SlotIdValue, StringComparison.Ordinal)))
            {
                errors?.Add($"{name}: Equipment references unknown Slot '{SlotIdValue}'.");
                valid = false;
            }
            if (profile == null || !profile.Features.Contains(m_Feature))
            {
                errors?.Add($"{name}: Equipment Feature is absent from its Profile catalog.");
                valid = false;
            }
            var values = new Dictionary<string, EquipmentParameterValue>(StringComparer.Ordinal);
            IReadOnlyList<EquipmentParameterValue> parameterValues = ParameterValues;
            for (int i = 0; i < parameterValues.Count; i++)
            {
                EquipmentParameterValue value = parameterValues[i];
                if (value == null || string.IsNullOrEmpty(value.ParameterIdValue) || !values.TryAdd(value.ParameterIdValue, value) || !value.IsFinite())
                {
                    errors?.Add($"{name}: Equipment Parameter value #{i} is missing, duplicated or non-finite.");
                    valid = false;
                }
            }
            if (m_Feature)
            {
                IReadOnlyList<EquipmentParameterSchema> schemas = m_Feature.Parameters;
                for (int i = 0; i < schemas.Count; i++)
                {
                    EquipmentParameterSchema schema = schemas[i];
                    if (schema == null)
                        continue;
                    if (!values.TryGetValue(schema.ParameterIdValue, out EquipmentParameterValue value))
                    {
                        if (schema.Required)
                        {
                            errors?.Add($"{name}: required Equipment Parameter '{schema.ParameterIdValue}' is missing.");
                            valid = false;
                        }
                        continue;
                    }
                    if (value.ValueKind != schema.ValueKind)
                    {
                        errors?.Add($"{name}: Equipment Parameter '{schema.ParameterIdValue}' kind does not match Feature schema.");
                        valid = false;
                    }
                }
                foreach (string parameterId in values.Keys)
                {
                    if (!schemas.Any(value => value != null && string.Equals(value.ParameterIdValue, parameterId, StringComparison.Ordinal)))
                    {
                        errors?.Add($"{name}: Equipment Parameter '{parameterId}' is not declared by Feature '{m_Feature.FeatureIdValue}'.");
                        valid = false;
                    }
                }
            }
            return valid;
        }
    }

    [Serializable]
    public sealed class InitialEquipmentLoadoutEntry
    {
        [SerializeField] string m_SlotId;
        [SerializeField] EquipmentDefinition m_Equipment;

        public string SlotIdValue => EquipmentSlotDefinition.Normalize(m_SlotId);
        public EquipmentSlotId SlotId => new EquipmentSlotId(SlotIdValue);
        public EquipmentDefinition Equipment => m_Equipment;
    }

    [CreateAssetMenu(fileName = "CharacterEquipmentProfile", menuName = "3C/Character/Equipment Profile")]
    public sealed class CharacterEquipmentProfile : ScriptableObject
    {
        [SerializeField] EquipmentSlotDefinition[] m_Slots = Array.Empty<EquipmentSlotDefinition>();
        [SerializeField] EquipmentActionRouteDefinition[] m_Routes = Array.Empty<EquipmentActionRouteDefinition>();
        [SerializeField] CharacterEquipmentFeatureDefinition[] m_Features = Array.Empty<CharacterEquipmentFeatureDefinition>();
        [SerializeField] EquipmentDefinition[] m_Equipment = Array.Empty<EquipmentDefinition>();
        [SerializeField] InitialEquipmentLoadoutEntry[] m_InitialLoadout = Array.Empty<InitialEquipmentLoadoutEntry>();

        public IReadOnlyList<EquipmentSlotDefinition> Slots => m_Slots ?? Array.Empty<EquipmentSlotDefinition>();
        public IReadOnlyList<EquipmentActionRouteDefinition> Routes => m_Routes ?? Array.Empty<EquipmentActionRouteDefinition>();
        public IReadOnlyList<CharacterEquipmentFeatureDefinition> Features => m_Features ?? Array.Empty<CharacterEquipmentFeatureDefinition>();
        public IReadOnlyList<EquipmentDefinition> Equipment => m_Equipment ?? Array.Empty<EquipmentDefinition>();
        public IReadOnlyList<InitialEquipmentLoadoutEntry> InitialLoadout => m_InitialLoadout ?? Array.Empty<InitialEquipmentLoadoutEntry>();

        public bool CollectConfigurationErrors(CharacterPipelineDefinition definition, List<string> errors)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            bool valid = true;
            var slotIds = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<EquipmentSlotDefinition> slots = Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null)
                {
                    errors?.Add($"{name}: Equipment Slot #{i} is missing.");
                    valid = false;
                }
                else valid &= slots[i].CollectConfigurationErrors(name, slotIds, errors);
            }
            var routeIds = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<EquipmentActionRouteDefinition> routes = Routes;
            for (int i = 0; i < routes.Count; i++)
            {
                if (routes[i] == null)
                {
                    errors?.Add($"{name}: Equipment Route #{i} is missing.");
                    valid = false;
                }
                else valid &= routes[i].CollectConfigurationErrors(definition, routeIds, slotIds, errors);
            }
            var featureIds = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<CharacterEquipmentFeatureDefinition> features = Features;
            for (int i = 0; i < features.Count; i++)
            {
                CharacterEquipmentFeatureDefinition feature = features[i];
                if (!feature || !featureIds.Add(feature.FeatureIdValue))
                {
                    errors?.Add($"{name}: Equipment Feature #{i} is missing or duplicated.");
                    valid = false;
                    continue;
                }
                valid &= feature.CollectConfigurationErrors(definition, this, errors);
            }
            var equipmentIds = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<EquipmentDefinition> equipment = Equipment;
            for (int i = 0; i < equipment.Count; i++)
            {
                EquipmentDefinition item = equipment[i];
                if (!item || !equipmentIds.Add(item.EquipmentIdValue))
                {
                    errors?.Add($"{name}: Equipment item #{i} is missing or duplicated.");
                    valid = false;
                    continue;
                }
                valid &= item.CollectConfigurationErrors(this, errors);
            }
            valid &= ValidateInitialLoadout(slots, equipment, errors);
            valid &= ValidateRouteCoverage(routes, equipment, errors);
            return valid;
        }

        bool ValidateInitialLoadout(
            IReadOnlyList<EquipmentSlotDefinition> slots,
            IReadOnlyList<EquipmentDefinition> equipment,
            List<string> errors)
        {
            bool valid = true;
            var entries = new Dictionary<string, InitialEquipmentLoadoutEntry>(StringComparer.Ordinal);
            IReadOnlyList<InitialEquipmentLoadoutEntry> loadout = InitialLoadout;
            for (int i = 0; i < loadout.Count; i++)
            {
                InitialEquipmentLoadoutEntry entry = loadout[i];
                if (entry == null || string.IsNullOrEmpty(entry.SlotIdValue) || !entries.TryAdd(entry.SlotIdValue, entry))
                {
                    errors?.Add($"{name}: Initial Loadout entry #{i} is missing or duplicated.");
                    valid = false;
                    continue;
                }
                if (!slots.Any(value => value != null && string.Equals(value.SlotIdValue, entry.SlotIdValue, StringComparison.Ordinal)))
                {
                    errors?.Add($"{name}: Initial Loadout references unknown Slot '{entry.SlotIdValue}'.");
                    valid = false;
                }
                if (entry.Equipment && (!equipment.Contains(entry.Equipment) || !string.Equals(entry.Equipment.SlotIdValue, entry.SlotIdValue, StringComparison.Ordinal)))
                {
                    errors?.Add($"{name}: Initial Loadout Slot '{entry.SlotIdValue}' references an incompatible Equipment item.");
                    valid = false;
                }
            }
            for (int i = 0; i < slots.Count; i++)
            {
                EquipmentSlotDefinition slot = slots[i];
                if (slot == null)
                    continue;
                if (!entries.TryGetValue(slot.SlotIdValue, out InitialEquipmentLoadoutEntry entry))
                {
                    errors?.Add($"{name}: Initial Loadout has no explicit value for Slot '{slot.SlotIdValue}'.");
                    valid = false;
                }
                else if (slot.Requirement == EquipmentSlotRequirement.Required && !entry.Equipment)
                {
                    errors?.Add($"{name}: required Slot '{slot.SlotIdValue}' cannot be None.");
                    valid = false;
                }
            }
            return valid;
        }

        bool ValidateRouteCoverage(
            IReadOnlyList<EquipmentActionRouteDefinition> routes,
            IReadOnlyList<EquipmentDefinition> equipment,
            List<string> errors)
        {
            bool valid = true;
            for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
            {
                EquipmentActionRouteDefinition route = routes[routeIndex];
                if (route == null || route.MissingImplementation != EquipmentRouteMissingImplementation.RejectComposition)
                    continue;
                for (int itemIndex = 0; itemIndex < equipment.Count; itemIndex++)
                {
                    EquipmentDefinition item = equipment[itemIndex];
                    if (!item || !string.Equals(item.SlotIdValue, route.OwnerSlotIdValue, StringComparison.Ordinal))
                        continue;
                    bool implemented = item.Feature && item.Feature.RouteImplementations.Any(
                        value => value != null && string.Equals(value.RouteIdValue, route.RouteIdValue, StringComparison.Ordinal));
                    if (!implemented)
                    {
                        errors?.Add($"{name}: Equipment '{item.EquipmentIdValue}' does not implement required Route '{route.RouteIdValue}'.");
                        valid = false;
                    }
                }
            }
            return valid;
        }
    }
}
