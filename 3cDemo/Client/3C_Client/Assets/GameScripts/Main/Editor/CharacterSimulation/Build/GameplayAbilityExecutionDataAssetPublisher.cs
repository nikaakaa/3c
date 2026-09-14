using System;
using System.Collections.Generic;
using System.IO;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public static class GameplayAbilityExecutionDataAssetPublisher
    {
        sealed class CompiledAbility
        {
            public Float32GameplayAbilityExecutionCompilationResult Float32;
            public ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionCompilationResult Fixed;
            public string Float32Path;
            public string FixedPath;
        }

        public static void PublishDefinition(
            CharacterPipelineDefinition definition,
            string outputFolder)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            string folder = RequireAssetFolder(outputFolder);
            var abilities = new List<GameplayAbilityDefinition>();
            var identities = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < definition.AbilityGrants.Count; i++)
            {
                AbilityGrant grant = definition.AbilityGrants[i];
                GameplayAbilityDefinition ability = grant?.Ability;
                if (!ability || string.IsNullOrEmpty(ability.AbilityId) || !identities.Add(ability.AbilityId))
                    throw new InvalidOperationException($"Character Pipeline Definition '{definition.name}' has an invalid or duplicated Ability grant.");
                abilities.Add(ability);
            }
            abilities.Sort((left, right) => string.CompareOrdinal(left.AbilityId, right.AbilityId));

            var compiled = new List<CompiledAbility>(abilities.Count);
            for (int i = 0; i < abilities.Count; i++)
            {
                GameplayAbilityDefinition ability = abilities[i];
                GameplayAbilitySemanticFrontendResult semantic =
                    GameplayAbilitySemanticFrontendCompiler.Compile(ability);
                if (!semantic.IsValid)
                    throw new InvalidOperationException(FormatReport(semantic.Report));
                compiled.Add(new CompiledAbility
                {
                    Float32 = GameplayAbilityTargetCompiler.CompileFloat32(semantic.Artifact),
                    Fixed = GameplayAbilityTargetCompiler.CompileFixed(semantic.Artifact),
                    Float32Path = RequireAssetPath(
                        $"{folder}/{ability.AbilityId}.Float32Data.asset",
                        "float32AssetPath"),
                    FixedPath = RequireAssetPath(
                        $"{folder}/{ability.AbilityId}.FixedData.asset",
                        "fixedAssetPath")
                });
            }
            for (int i = 0; i < compiled.Count; i++)
            {
                ValidateAssetSlot<GameplayAbilityDataAsset>(compiled[i].Float32Path);
                ValidateAssetSlot<FixedGameplayAbilityDataAsset>(compiled[i].FixedPath);
            }

            var float32Assets = new GameplayAbilityDataAsset[compiled.Count];
            var fixedAssets = new FixedGameplayAbilityDataAsset[compiled.Count];
            for (int i = 0; i < compiled.Count; i++)
            {
                CompiledAbility ability = compiled[i];
                float32Assets[i] = PrepareAsset<GameplayAbilityDataAsset>(ability.Float32Path);
                fixedAssets[i] = PrepareAsset<FixedGameplayAbilityDataAsset>(ability.FixedPath);
                float32Assets[i].SetCompiledExecutionData(ability.Float32);
                fixedAssets[i].SetCompiledExecutionData(ability.Fixed.Data);
                EditorUtility.SetDirty(float32Assets[i]);
                EditorUtility.SetDirty(fixedAssets[i]);
            }
            definition.SetFloat32AbilityData(float32Assets);
            definition.SetFixedAbilityData(fixedAssets);
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            for (int i = 0; i < compiled.Count; i++)
            {
                AssetDatabase.ImportAsset(compiled[i].Float32Path, ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(compiled[i].FixedPath, ImportAssetOptions.ForceUpdate);
            }
        }

        public static void Publish(
            GameplayAbilityDefinition definition,
            string float32AssetPath,
            string fixedAssetPath)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            string float32Path = RequireAssetPath(float32AssetPath, nameof(float32AssetPath));
            string fixedPath = RequireAssetPath(fixedAssetPath, nameof(fixedAssetPath));
            if (string.Equals(float32Path, fixedPath, StringComparison.Ordinal))
                throw new ArgumentException("Float32 and Fixed Ability Data paths must be different.");

            GameplayAbilitySemanticFrontendResult semantic =
                GameplayAbilitySemanticFrontendCompiler.Compile(definition);
            if (!semantic.IsValid)
                throw new InvalidOperationException(FormatReport(semantic.Report));
            Float32GameplayAbilityExecutionCompilationResult float32 =
                GameplayAbilityTargetCompiler.CompileFloat32(semantic.Artifact);
            ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionCompilationResult fixedData =
                GameplayAbilityTargetCompiler.CompileFixed(semantic.Artifact);

            ValidateAssetSlot<GameplayAbilityDataAsset>(float32Path);
            ValidateAssetSlot<FixedGameplayAbilityDataAsset>(fixedPath);
            GameplayAbilityDataAsset float32Asset = PrepareAsset<GameplayAbilityDataAsset>(float32Path);
            FixedGameplayAbilityDataAsset fixedAsset = PrepareAsset<FixedGameplayAbilityDataAsset>(fixedPath);
            float32Asset.SetCompiledExecutionData(float32);
            fixedAsset.SetCompiledExecutionData(fixedData.Data);
            EditorUtility.SetDirty(float32Asset);
            EditorUtility.SetDirty(fixedAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(float32Path, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(fixedPath, ImportAssetOptions.ForceUpdate);
        }

        static TAsset PrepareAsset<TAsset>(string path)
            where TAsset : ScriptableObject
        {
            ValidateAssetSlot<TAsset>(path);
            UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(path);
            TAsset asset = existing as TAsset;
            if (asset)
                return asset;
            asset = ScriptableObject.CreateInstance<TAsset>();
            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void ValidateAssetSlot<TAsset>(string path)
            where TAsset : ScriptableObject
        {
            UnityEngine.Object existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing != null && existing is not TAsset)
                throw new InvalidOperationException(
                    $"Ability Data output '{path}' is occupied by '{existing.GetType().Name}'.");
            string folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
                throw new InvalidOperationException($"Ability Data output folder '{folder}' does not exist.");
        }

        static string RequireAssetFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Ability Data output folder is required.", nameof(path));
            string normalized = path.Trim().Replace('\\', '/').TrimEnd('/');
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal) ||
                !AssetDatabase.IsValidFolder(normalized))
            {
                throw new ArgumentException("Ability Data output folder must be an existing project-relative folder.", nameof(path));
            }
            return normalized;
        }

        static string RequireAssetPath(string path, string parameter)
        {
            if (string.IsNullOrWhiteSpace(path) ||
                !path.StartsWith("Assets/", StringComparison.Ordinal) ||
                !string.Equals(Path.GetExtension(path), ".asset", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Ability Data output must be an explicit project-relative .asset path.", parameter);
            }
            return path.Replace('\\', '/');
        }

        static string FormatReport(CharacterSimulationCompileReport report)
        {
            if (report == null || report.Messages.Count == 0)
                return "Gameplay Ability compilation failed without a diagnostic.";
            var messages = new string[report.Messages.Count];
            for (int i = 0; i < messages.Length; i++)
                messages[i] = report.Messages[i].ToString();
            return string.Join("\n", messages);
        }
    }
}
