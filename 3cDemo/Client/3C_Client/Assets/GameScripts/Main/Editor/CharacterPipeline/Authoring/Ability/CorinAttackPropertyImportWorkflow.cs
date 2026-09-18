using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.GameplayEffect;
using ThirdPersonGameplay.Effects;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring
{
    public static class CorinAttackPropertyImportWorkflow
    {
        const string SourceJsonPath = @"D:\ZZZ_Dump\output\corin_replication\replication-guide\data\attack-properties.json";
        const string OutputFolder = "Assets/Configs/Character/Corin/Pipeline/GameplayEffect/AttackProperties";
        const string EffectProfilePath = "Assets/Configs/Character/Corin/Pipeline/GameplayEffect/CorinCharacterGameplayEffectProfile.asset";
        const string AbilityPath = "Assets/Configs/Character/Corin/Pipeline/Abilities/CorinAttackGameplayAbilityDefinition.asset";
        const string KeyPrefix = "Corin_Attack_Normal_0";

        [MenuItem("3C/Character/Gameplay/Import Corin Normal Attack Properties")]
        public static void Import()
        {
            if (!File.Exists(SourceJsonPath))
                throw new FileNotFoundException("Corin AttackProperty source is missing.", SourceJsonPath);
            JObject source = JObject.Parse(File.ReadAllText(SourceJsonPath, Encoding.UTF8));
            List<string> keys = source.Properties()
                .Select(property => property.Name)
                .Where(name => name.StartsWith(KeyPrefix, StringComparison.Ordinal))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
            if (keys.Count == 0)
                throw new InvalidOperationException("Corin AttackProperty source has no normal attack entries.");
            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                string parent = Path.GetDirectoryName(OutputFolder)?.Replace('\\', '/');
                string leaf = Path.GetFileName(OutputFolder);
                if (!AssetDatabase.IsValidFolder(parent))
                    throw new InvalidOperationException($"Corin AttackProperty output parent '{parent}' is missing.");
                AssetDatabase.CreateFolder(parent, leaf);
            }

            var effects = new List<GameplayEffectDefinition>();
            foreach (string key in keys)
                effects.Add(ImportEffect(source[key] as JObject ?? throw new InvalidOperationException($"AttackProperty '{key}' is not an object."), key));

            GameplayEffectDefinition profileEffect = AssetDatabase.LoadAssetAtPath<GameplayEffectDefinition>(EffectProfilePath);
            CharacterGameplayEffectProfile profile = AssetDatabase.LoadAssetAtPath<CharacterGameplayEffectProfile>(EffectProfilePath);
            if (!profileEffect && !profile)
                throw new InvalidOperationException("Corin Gameplay Effect profile is missing.");
            if (profile)
                RegisterProfileEffects(profile, effects);

            GameplayAbilityDefinition ability = AssetDatabase.LoadAssetAtPath<GameplayAbilityDefinition>(AbilityPath);
            if (!ability)
                throw new InvalidOperationException("Corin Attack Gameplay Ability is missing.");
            ability.ConfigureEffects(effects);
            EditorUtility.SetDirty(ability);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Imported {effects.Count} Corin normal attack AttackProperty effects.");
        }

        static GameplayEffectDefinition ImportEffect(JObject source, string key)
        {
            string assetPath = $"{OutputFolder}/{key}.asset";
            GameplayEffectDefinition effect = AssetDatabase.LoadAssetAtPath<GameplayEffectDefinition>(assetPath);
            if (!effect)
            {
                effect = ScriptableObject.CreateInstance<GameplayEffectDefinition>();
                AssetDatabase.CreateAsset(effect, assetPath);
            }
            else if (!string.Equals(effect.EffectId.Value, key, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Effect '{assetPath}' already owns identity '{effect.EffectId}'.");
            }

            SerializedObject serialized = new SerializedObject(effect);
            serialized.FindProperty("m_EffectId").boxedValue = new GameplayEffectId(key);
            serialized.FindProperty("m_DefinitionRevision").uintValue = 1u;
            serialized.FindProperty("m_DisplayName").stringValue = key;
            serialized.FindProperty("m_DebugCategory").stringValue = "Corin/AttackProperty";
            serialized.FindProperty("m_DurationPolicy").enumValueIndex = (int)GameplayEffectDurationPolicy.Instant;
            serialized.FindProperty("m_MaxStacks").intValue = 1;
            serialized.FindProperty("m_SetByCallerParameters").arraySize = 0;
            ApplyComponents(serialized, source, key);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(effect);
            return effect;
        }

        static void ApplyComponents(SerializedObject serialized, JObject source, string key)
        {
            JObject pattern = source.Value<JObject>("AttackPattern") ?? throw new InvalidOperationException($"AttackProperty '{key}' has no AttackPattern.");
            JObject property = source.Value<JObject>("AttackProperty") ?? throw new InvalidOperationException($"AttackProperty '{key}' has no AttackProperty payload.");
            var collision = new GameplayAttackCollisionComponentDefinition(
                ParseCollisionKind(pattern.Value<string>("$type")),
                Vector(pattern.Value<float>("CenterXOffset"), pattern.Value<float>("CenterYOffset"), pattern.Value<float>("CenterZOffset")),
                pattern.Value<float>("width"),
                pattern.Value<float>("height"),
                pattern.Value<float>("distance"),
                pattern.Value<float>("FanAngle"),
                pattern.Value<float>("Radius"),
                pattern.Value<float>("InvalidRadius"),
                pattern.Value<float>("InvalidAngle"),
                pattern.Value<int>("FollowAtkDirType"),
                pattern.Value<int>("AttackResultFilterCoreDistance"),
                pattern.Value<bool>("IsSubtractive"),
                pattern.Value<float>("hitInterval"),
                pattern.Value<int>("aliveMaxHitCnt"),
                pattern.Value<int>("unitMaxHitCnt"),
                pattern.Value<bool>("isFollowAttacker"));
            var attack = new GameplayAttackPropertyComponentDefinition(
                key,
                property.Value<int>("HitType"),
                property.Value<int>("HitStrenType"),
                Tokens(property["TagList"]),
                Magnitude(property["DamagePercentage"]),
                Magnitude(property["AddedDamageValue"]),
                Magnitude(property["BreakStunRatio"]),
                Magnitude(property["ElementAccumulationValue"]),
                Magnitude(property["ExhaustedAccumulationValue"]),
                Magnitude(property["ExhaustedChaseValue"]),
                property.Value<int>("DamageElement"),
                property.Value<int>("DamageHitType"),
                DynamicInt(property["DamageBreakLevel"]),
                Magnitude(property["DamageBreakLevelProbability"]),
                DynamicInt(property["TriggerBuffLevel"]),
                DynamicInt(property["DestructionClass"]),
                DynamicInt(property["DestructionDurability"]),
                DynamicInt(property["OverrideDamageStaggerLevel"]),
                property.Value<int>("DamageTextID"),
                property.Value<int>("DamageTextWaitTime"),
                DynamicInt(property["FrameHalt"]),
                DynamicInt(property["AttackerFrameHalt"]),
                HitEffectId(property["GroundHitEffect"]),
                HitEffectId(property["SkyHitEffect"]),
                HitEffectId(property["DownHitEffect"]),
                property.Value<string>("StandardConfigKey") ?? string.Empty,
                property.Value<string>("AbilityTargetKey") ?? string.Empty,
                property.Value<bool>("IsCauseStun"),
                property.Value<bool>("IsHeavyAttack"),
                property.Value<bool>("IsCauseExhausted"),
                property.Value<bool>("IsHeal"),
                property.Value<bool>("IsIndirect"),
                property.Value<bool>("Enemy"),
                property.Value<bool>("Allied"),
                property.Value<bool>("Neutral"),
                property.Value<bool>("IsUseAbilityTargetKey"),
                property.Value<bool>("BanDamage"));
            SerializedProperty components = serialized.FindProperty("m_Components");
            components.arraySize = 2;
            components.GetArrayElementAtIndex(0).managedReferenceValue = collision;
            components.GetArrayElementAtIndex(1).managedReferenceValue = attack;
        }

        static void RegisterProfileEffects(CharacterGameplayEffectProfile profile, List<GameplayEffectDefinition> effects)
        {
            SerializedObject serialized = new SerializedObject(profile);
            SerializedProperty definitions = serialized.FindProperty("m_EffectDefinitions");
            var existing = new HashSet<string>();
            for (int i = 0; i < definitions.arraySize; i++)
            {
                UnityEngine.Object reference = definitions.GetArrayElementAtIndex(i).objectReferenceValue;
                if (reference)
                    existing.Add(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(reference)));
            }
            foreach (GameplayEffectDefinition effect in effects)
            {
                string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(effect));
                if (existing.Add(guid))
                {
                    definitions.arraySize++;
                    definitions.GetArrayElementAtIndex(definitions.arraySize - 1).objectReferenceValue = effect;
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
        }

        static GameplayAttackCollisionKind ParseCollisionKind(string typeName)
        {
            if (string.Equals(typeName, "MoleMole.Config.BoxCollisionDetect, Logic", StringComparison.Ordinal))
                return GameplayAttackCollisionKind.Box;
            if (string.Equals(typeName, "MoleMole.Config.BoxCollisionContinuousDetect, Logic", StringComparison.Ordinal))
                return GameplayAttackCollisionKind.BoxContinuous;
            if (string.Equals(typeName, "MoleMole.Config.FanCollisionWithHeightDetect, Logic", StringComparison.Ordinal))
                return GameplayAttackCollisionKind.FanWithHeight;
            throw new NotSupportedException($"Attack collision type '{typeName}' is unsupported.");
        }

        static Vector3 Vector(float x, float y, float z) => new Vector3(x, y, z);

        static string[] Tokens(JToken token)
        {
            if (token is not JArray array)
                return Array.Empty<string>();
            return array.Select(value => value.Value<string>() ?? string.Empty).ToArray();
        }

        static GameplayMagnitudeDefinition Magnitude(JToken token)
        {
            JObject value = token as JObject ?? throw new InvalidOperationException("Attack magnitude is missing.");
            string dynamicKey = value.Value<string>("dynamicKey");
            bool dynamic = value.Value<bool?>("isDynamic") == true && !string.IsNullOrWhiteSpace(dynamicKey);
            return new GameplayMagnitudeDefinition(
                dynamic ? GameplayMagnitudeSource.SetByCaller : GameplayMagnitudeSource.Constant,
                value.Value<float?>("fixedValue") ?? 0f,
                dynamic ? dynamicKey : string.Empty,
                default,
                1f,
                0f);
        }

        static int DynamicInt(JToken token)
        {
            if (token is not JObject value)
                return 0;
            if (value.Value<bool?>("isDynamic") == true)
                throw new NotSupportedException("Dynamic integer AttackProperty values are not supported by the normal attack importer.");
            return value.Value<int?>("fixedValue") ?? 0;
        }

        static int HitEffectId(JToken token)
        {
            if (token is not JObject value)
                return 0;
            return value.Value<int?>("TargetHitEffect") ?? 0;
        }
    }
}
