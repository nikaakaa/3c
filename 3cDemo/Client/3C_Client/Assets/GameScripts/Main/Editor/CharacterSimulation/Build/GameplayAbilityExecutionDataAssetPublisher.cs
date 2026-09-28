using System;
using System.Collections.Generic;
using System.IO;
using BTSMTL.Timeline;
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
            public List<TimelineAsset> Timelines;
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
            var abilityTimelineIdentities = new HashSet<string>(StringComparer.Ordinal);
            var abilityTimelines = new List<TimelineAsset>();
            for (int i = 0; i < abilities.Count; i++)
            {
                CompiledAbility result = CompileAbility(definition, abilities[i], folder);
                for (int timelineIndex = 0; timelineIndex < result.Timelines.Count; timelineIndex++)
                {
                    TimelineAsset timeline = result.Timelines[timelineIndex];
                    if (abilityTimelineIdentities.Add(timeline.Data.AuthoringId))
                        abilityTimelines.Add(timeline);
                }
                compiled.Add(result);
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
            MergeTimelines(definition, abilityTimelines);
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            for (int i = 0; i < compiled.Count; i++)
            {
                AssetDatabase.ImportAsset(compiled[i].Float32Path, ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(compiled[i].FixedPath, ImportAssetOptions.ForceUpdate);
            }
        }

        public static void PublishSelected(
            CharacterPipelineDefinition definition,
            string outputFolder,
            params string[] abilityIds)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            if (abilityIds == null || abilityIds.Length == 0)
                throw new ArgumentException("At least one Ability Id is required.", nameof(abilityIds));
            string folder = RequireAssetFolder(outputFolder);
            var remaining = new HashSet<string>(abilityIds, StringComparer.Ordinal);
            if (remaining.Count != abilityIds.Length || remaining.Contains(string.Empty))
                throw new ArgumentException("Ability Ids must be distinct and non-empty.", nameof(abilityIds));
            var compiled = new List<CompiledAbility>(abilityIds.Length);
            for (int i = 0; i < definition.AbilityGrants.Count; i++)
            {
                GameplayAbilityDefinition ability = definition.AbilityGrants[i]?.Ability;
                if (ability && remaining.Remove(ability.AbilityId))
                    compiled.Add(CompileAbility(definition, ability, folder));
            }
            if (remaining.Count != 0)
                throw new InvalidOperationException($"Definition '{definition.name}' does not grant every selected Ability.");
            compiled.Sort((left, right) => string.CompareOrdinal(left.Float32Path, right.Float32Path));
            var abilityTimelines = new List<TimelineAsset>();
            for (int i = 0; i < compiled.Count; i++)
                abilityTimelines.AddRange(compiled[i].Timelines);

            var float32Assets = new GameplayAbilityDataAsset[compiled.Count];
            var fixedAssets = new FixedGameplayAbilityDataAsset[compiled.Count];
            for (int i = 0; i < compiled.Count; i++)
            {
                float32Assets[i] = AssetDatabase.LoadAssetAtPath<GameplayAbilityDataAsset>(compiled[i].Float32Path);
                fixedAssets[i] = AssetDatabase.LoadAssetAtPath<FixedGameplayAbilityDataAsset>(compiled[i].FixedPath);
                if (!float32Assets[i] || !fixedAssets[i] ||
                    !Contains(definition.Float32AbilityData, float32Assets[i]) ||
                    !Contains(definition.FixedAbilityData, fixedAssets[i]))
                    throw new InvalidOperationException($"Ability Data for '{compiled[i].Float32Path}' is not linked by Definition '{definition.name}'.");
            }
            for (int i = 0; i < compiled.Count; i++)
            {
                float32Assets[i].SetCompiledExecutionData(compiled[i].Float32);
                fixedAssets[i].SetCompiledExecutionData(compiled[i].Fixed.Data);
                EditorUtility.SetDirty(float32Assets[i]);
                EditorUtility.SetDirty(fixedAssets[i]);
                AssetDatabase.SaveAssetIfDirty(float32Assets[i]);
                AssetDatabase.SaveAssetIfDirty(fixedAssets[i]);
                AssetDatabase.ImportAsset(compiled[i].Float32Path, ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(compiled[i].FixedPath, ImportAssetOptions.ForceUpdate);
            }
            MergeTimelines(definition, abilityTimelines);
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssetIfDirty(definition);
        }

        static void MergeTimelines(CharacterPipelineDefinition definition, IReadOnlyList<TimelineAsset> abilityTimelines)
        {
            var configuredTimelines = definition.ControlMotionTimelines;
            var allTimelines = new List<TimelineAsset>(configuredTimelines.Count + abilityTimelines.Count);
            var timelineIdentities = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < abilityTimelines.Count; i++)
                if (timelineIdentities.Add(abilityTimelines[i].Data.AuthoringId))
                    allTimelines.Add(abilityTimelines[i]);
            for (int i = 0; i < configuredTimelines.Count; i++)
            {
                TimelineAsset timeline = configuredTimelines[i];
                if (!timeline || timeline.Data == null)
                    throw new InvalidOperationException($"Character Pipeline Definition '{definition.name}' has an invalid configured Timeline.");
                if (timelineIdentities.Add(timeline.Data.AuthoringId))
                    allTimelines.Add(timeline);
            }
            definition.SetControlMotionTimelines(allTimelines);
        }

        static CompiledAbility CompileAbility(
            CharacterPipelineDefinition definition,
            GameplayAbilityDefinition ability,
            string folder)
        {
            GameplayAbilitySemanticFrontendResult semantic =
                GameplayAbilitySemanticFrontendCompiler.Compile(
                    ability,
                    definition.SimulationTickRate,
                    CharacterSkillProviderOwners.Asset(
                        AssetDatabase.AssetPathToGUID(
                            AssetDatabase.GetAssetPath(definition.GameplayEffectProfile))));
            if (!semantic.IsValid)
                throw new InvalidOperationException(FormatReport(semantic.Report));
            var timelines = new List<TimelineAsset>();
            var timelineIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (BtsmtlSkillGraphOccurrence occurrence in semantic.CompilationModel.EntryGraph.EnumerateOccurrences())
            {
                foreach (BtsmtlSkillTimelineOccurrence timelineOccurrence in occurrence.Timelines)
                {
                    TimelineAsset timeline = timelineOccurrence.Node.TimelineAsset;
                    if (!timeline || timeline.Data == null)
                        throw new InvalidOperationException($"Ability '{ability.AbilityId}' has an invalid Timeline reference.");
                    if (timelineIds.Add(timeline.Data.AuthoringId))
                        timelines.Add(timeline);
                }
            }
            return new CompiledAbility
            {
                Float32 = GameplayAbilityTargetCompiler.CompileFloat32(semantic.Artifact),
                Fixed = GameplayAbilityTargetCompiler.CompileFixed(semantic.Artifact),
                Float32Path = RequireAssetPath($"{folder}/{ability.AbilityId}.Float32Data.asset", "float32AssetPath"),
                FixedPath = RequireAssetPath($"{folder}/{ability.AbilityId}.FixedData.asset", "fixedAssetPath"),
                Timelines = timelines
            };
        }

        static bool Contains<T>(IReadOnlyList<T> assets, UnityEngine.Object asset)
            where T : UnityEngine.Object
        {
            for (int i = 0; i < assets.Count; i++)
                if (assets[i] == asset)
                    return true;
            return false;
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

        static string FormatReport(SimulationCompileReport report)
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

