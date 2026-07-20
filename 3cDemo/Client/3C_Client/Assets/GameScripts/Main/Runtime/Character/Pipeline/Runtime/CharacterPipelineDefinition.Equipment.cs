using System;
using System.Collections.Generic;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Equipment;
using ThirdPersonGameplay.Tags;

namespace ThirdPersonCharacter.Pipeline
{
    public sealed partial class CharacterPipelineDefinition
    {
        public IReadOnlyList<ActionProfile> BuildCompiledActionProfileCatalog()
        {
            var profiles = new List<ActionProfile>();
            IReadOnlyList<ActionProfile> coreProfiles = ActionProfiles;
            for (int i = 0; i < coreProfiles.Count; i++)
            {
                if (coreProfiles[i])
                    profiles.Add(coreProfiles[i]);
            }
            if (m_EquipmentCapabilityEnabled && m_EquipmentProfile)
            {
                IReadOnlyList<CharacterEquipmentFeatureDefinition> features = m_EquipmentProfile.Features;
                for (int featureIndex = 0; featureIndex < features.Count; featureIndex++)
                {
                    CharacterEquipmentFeatureDefinition feature = features[featureIndex];
                    if (!feature)
                        continue;
                    IReadOnlyList<EquipmentFeatureRouteImplementation> routes = feature.RouteImplementations;
                    for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
                    {
                        ActionProfile actionProfile = routes[routeIndex]?.ActionProfile;
                        if (actionProfile && !profiles.Contains(actionProfile))
                            profiles.Add(actionProfile);
                    }
                }
            }
            profiles.Sort((left, right) => string.CompareOrdinal(left.ActionId, right.ActionId));
            return profiles;
        }

        bool CollectEquipmentConfigurationErrors(
            HashSet<string> behaviorIds,
            GameplayTagCatalogRuntimeData gameplayTagCatalog,
            List<string> errors)
        {
            if (!m_EquipmentCapabilityEnabled)
            {
                if (m_EquipmentProfile || m_EquipmentPresentationProfile)
                {
                    errors?.Add($"{name}: Equipment profiles must be absent while Equipment capability is disabled.");
                    return false;
                }
                return true;
            }

            bool valid = true;
            if (!m_EquipmentProfile)
            {
                errors?.Add($"{name}: Equipment capability requires one Equipment Gameplay Profile.");
                valid = false;
            }
            else
            {
                valid &= m_EquipmentProfile.CollectConfigurationErrors(this, errors);
            }
            if (!m_EquipmentPresentationProfile)
            {
                errors?.Add($"{name}: Equipment capability requires one Equipment Presentation Profile.");
                valid = false;
            }
            else
            {
                valid &= m_EquipmentPresentationProfile.CollectConfigurationErrors(m_EquipmentProfile, errors);
            }
            if (!m_EquipmentProfile)
                return valid;

            var equipmentActionIds = new Dictionary<string, ActionProfile>(StringComparer.Ordinal);
            IReadOnlyList<CharacterEquipmentFeatureDefinition> features = m_EquipmentProfile.Features;
            for (int featureIndex = 0; featureIndex < features.Count; featureIndex++)
            {
                CharacterEquipmentFeatureDefinition feature = features[featureIndex];
                if (!feature)
                    continue;
                IReadOnlyList<EquipmentFeatureRouteImplementation> routes = feature.RouteImplementations;
                for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
                {
                    ActionProfile profile = routes[routeIndex]?.ActionProfile;
                    if (!profile)
                        continue;
                    valid &= profile.CollectConfigurationErrors(errors);
                    if (gameplayTagCatalog != null)
                        valid &= profile.CollectTagConfigurationErrors(gameplayTagCatalog, errors);
                    if (string.IsNullOrEmpty(profile.ActionId))
                        continue;
                    if (equipmentActionIds.TryGetValue(profile.ActionId, out ActionProfile existing))
                    {
                        if (existing != profile)
                        {
                            errors?.Add($"{name}: Equipment Features define duplicate ActionId '{profile.ActionId}' with different ActionProfile assets.");
                            valid = false;
                        }
                        continue;
                    }
                    equipmentActionIds.Add(profile.ActionId, profile);
                    if (!behaviorIds.Add(profile.ActionId) && !IsSharedCoreAction(profile))
                    {
                        errors?.Add($"{name}: duplicate core/Equipment behavior id '{profile.ActionId}'.");
                        valid = false;
                    }
                }
            }
            return valid;
        }

        bool IsSharedCoreAction(ActionProfile profile)
        {
            IReadOnlyList<ActionProfile> profiles = ActionProfiles;
            for (int i = 0; i < profiles.Count; i++)
            {
                if (profiles[i] == profile)
                    return true;
            }
            return false;
        }
    }
}
