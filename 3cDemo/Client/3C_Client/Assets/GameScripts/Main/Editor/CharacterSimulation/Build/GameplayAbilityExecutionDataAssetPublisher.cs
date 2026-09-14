using System;
using System.IO;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public static class GameplayAbilityExecutionDataAssetPublisher
    {
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
