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

            IReadOnlyList<CharacterEquipmentFeatureDefinition> features = m_EquipmentProfile.Features;
            for (int featureIndex = 0; featureIndex < features.Count; featureIndex++)
            {
                CharacterEquipmentFeatureDefinition feature = features[featureIndex];
                if (!feature)
                    continue;
                IReadOnlyList<EquipmentFeatureRouteImplementation> routes = feature.RouteImplementations;
                for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
                {
                    EquipmentFeatureRouteImplementation route = routes[routeIndex];
                    if (route == null || string.IsNullOrEmpty(route.SkillIdValue))
                        continue;
                    if (!behaviorIds.Contains(route.SkillIdValue))
                    {
                        errors?.Add($"{name}: Equipment Route '{route.RouteIdValue}' references Skill '{route.SkillIdValue}' outside the Definition catalog.");
                        valid = false;
                    }
                }
            }
            return valid;
        }
    }
}
