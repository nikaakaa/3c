using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.GameplayEffect;
using ThirdPersonGameplay.Tags;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring
{
    public static class CorinRushBadgeAuthoring
    {
        const string CatalogPath = "Assets/Configs/Character/Corin/Pipeline/GameplayEffect/CorinGameplayTagCatalog.asset";
        const string ProfilePath = "Assets/Configs/Character/Corin/Pipeline/GameplayEffect/CorinCharacterGameplayEffectProfile.asset";

        [MenuItem("3C/Character/Gameplay/Equip Corin Badge S01")]
        public static void Equip()
        {
            GameplayTagCatalog catalog = AssetDatabase.LoadAssetAtPath<GameplayTagCatalog>(CatalogPath);
            CharacterGameplayEffectProfile profile = AssetDatabase.LoadAssetAtPath<CharacterGameplayEffectProfile>(ProfilePath);
            if (!catalog || !profile || profile.TagCatalog != catalog)
                throw new InvalidOperationException("Corin Gameplay Tag catalog and profile are not bound.");

            var badge = new GameplayTagId("Badge_S01");
            if (!catalog.Tags.Any(tag => tag.TagId == badge))
            {
                var definition = new GameplayTagDefinition();
                definition.Configure(badge, "Badge S01", default, "Badge");
                catalog.ConfigureTags(catalog.Tags.Concat(new[] { definition }));
                EditorUtility.SetDirty(catalog);
            }
            if (!profile.InitialTags.Contains(badge))
            {
                profile.ConfigureInitialTags(profile.InitialTags.Concat(new[] { badge }));
                EditorUtility.SetDirty(profile);
            }

            var errors = new List<string>();
            if (!profile.CollectConfigurationErrors(out _, errors))
                throw new InvalidOperationException(string.Join(" ", errors));
            AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(CatalogPath));
            AssetDatabase.SaveAssetIfDirty(AssetDatabase.GUIDFromAssetPath(ProfilePath));
        }
    }
}
