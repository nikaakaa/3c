using System;
using ThirdPerson.NetworkTest.Contracts;
using System.Collections.Generic;
using System.IO;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal static class NetworkTestProductAdapterUtility
    {
        public static string CharacterContentIdentity(CharacterPipelineDefinition definition)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            GameplayAbilityExecutionDataSet<Float32GameplayAbilityExecutionData> abilities =
                definition.LoadFloat32AbilitySet();
            CharacterControlRuntimeBinding control = definition.BuildControlRuntimeBinding(
                CharacterControlRuntimeModuleCatalog.Create());
            CharacterBodyMotionBinding bodyMotion = definition.BuildBodyMotionRuntimeBinding();
            CharacterGameplayEffectRuntimeBinding gameplayEffects = RequiresCapability(abilities, "GameplayEffect")
                ? definition.BuildGameplayEffectRuntimeBinding()
                : null;
            CharacterEquipmentRuntimeBinding equipment = RequiresCapability(abilities, "Equipment")
                ? definition.BuildEquipmentRuntimeBinding()
                : null;
            var parts = new List<string>
            {
                "float32-character-content/1",
                control.BindingHash.ToString(),
                bodyMotion.BindingHash.ToString(),
                gameplayEffects?.BindingHash.ToString() ?? string.Empty,
                equipment?.BindingHash.ToString() ?? string.Empty
            };
            for (int i = 0; i < abilities.Data.Count; i++)
            {
                Float32GameplayAbilityExecutionData ability = abilities.Data[i];
                parts.Add(ability.AbilityId.Value);
                parts.Add(ability.ContentHash.ToString());
                parts.Add(ability.StateSchemaHash.ToString());
                parts.Add(ability.ExecutionIdentity);
            }
            return $"character-content={StableHash.Compute(parts.ToArray())}";
        }

        public static StableHash FixedCharacterContentHash(CharacterPipelineDefinition definition)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            GameplayAbilityExecutionDataSet<FixedGameplayAbilityExecutionData> abilities =
                definition.LoadFixedAbilitySet();
            CharacterControlRuntimeBinding control = definition.BuildControlRuntimeBinding(
                CharacterControlRuntimeModuleCatalog.Create());
            CharacterBodyMotionBinding bodyMotion = definition.BuildBodyMotionRuntimeBinding();
            CharacterGameplayEffectRuntimeBinding gameplayEffects = definition.BuildGameplayEffectRuntimeBinding();
            CharacterEquipmentRuntimeBinding equipment = definition.BuildEquipmentRuntimeBinding();
            return new ThirdPersonSimulation.Fixed.SimulationActorBinding(
                new ActorId("network-test-character"),
                "network-test-body",
                control,
                bodyMotion,
                gameplayEffects,
                equipment,
                abilities).GameplayContentHash;
        }

        public static StableHash FixedCharacterStateSchemaHash(CharacterPipelineDefinition definition)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            GameplayAbilityExecutionDataSet<FixedGameplayAbilityExecutionData> abilities =
                definition.LoadFixedAbilitySet();
            CharacterControlRuntimeBinding control = definition.BuildControlRuntimeBinding(
                CharacterControlRuntimeModuleCatalog.Create());
            CharacterBodyMotionBinding bodyMotion = definition.BuildBodyMotionRuntimeBinding();
            CharacterGameplayEffectRuntimeBinding gameplayEffects = definition.BuildGameplayEffectRuntimeBinding();
            CharacterEquipmentRuntimeBinding equipment = definition.BuildEquipmentRuntimeBinding();
            return new ThirdPersonSimulation.Fixed.SimulationActorBinding(
                new ActorId("network-test-character"),
                "network-test-body",
                control,
                bodyMotion,
                gameplayEffects,
                equipment,
                abilities).StateSchemaHash;
        }

        public static string FixedCharacterContentIdentity(CharacterPipelineDefinition definition) =>
            $"character-content={FixedCharacterContentHash(definition)}";

        static bool RequiresCapability(
            GameplayAbilityExecutionDataSet<Float32GameplayAbilityExecutionData> abilities,
            string capability)
        {
            for (int i = 0; i < abilities.Data.Count; i++)
                if (abilities.Data[i].Capabilities.HasGameplayCapability(capability))
                    return true;
            return false;
        }

        public static NetworkTestProductManifestField Field(string key, string value) =>
            new NetworkTestProductManifestField { key = key, value = value };

        public static NetworkTestSessionRoleManifest SessionRole(
            string roleId,
            string launchSourceKind,
            string launchSourceId,
            bool required,
            string visibility,
            string readyCondition,
            string[] dependsOnRoleIds,
            string[] endpointKeys,
            string windowRoleId = "") => new NetworkTestSessionRoleManifest
        {
            roleId = roleId,
            launchSourceKind = launchSourceKind,
            launchSourceId = launchSourceId,
            required = required,
            visibility = visibility,
            readyCondition = readyCondition,
            dependsOnRoleIds = dependsOnRoleIds ?? Array.Empty<string>(),
            endpointKeys = endpointKeys ?? Array.Empty<string>(),
            windowRoleId = windowRoleId ?? string.Empty
        };

        public static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            T value = AssetDatabase.LoadAssetAtPath<T>(path);
            return value ? value : throw new InvalidOperationException($"Network Test Product asset is missing: {path}");
        }

        public static void PublishFantasyConfig(string serverProject, string serverDirectory)
        {
            string source = Path.Combine(Path.GetDirectoryName(serverProject) ?? string.Empty, "Fantasy.config");
            string target = Path.Combine(serverDirectory, "Fantasy.config");
            File.Copy(source, target, true);
            NetworkTestArtifactFileUtility.RequireExactFile(source, target);
        }

        public static NetworkTestRuntimeArtifactManifest RequireManagedArtifact(
            NetworkTestProductBuildManifest manifest,
            string roleId,
            string productId,
            string productRoot)
        {
            NetworkTestRuntimeArtifactManifest artifact = NetworkTestProductBuildWorkflow.RequireArtifact(manifest, roleId);
            if (!string.Equals(artifact.kind, NetworkTestRuntimeArtifactKind.ManagedExecutable.ToString(), StringComparison.Ordinal) ||
                !string.Equals(artifact.productId, productId, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(artifact.manifestPath) ||
                string.IsNullOrWhiteSpace(artifact.manifestHash))
                throw new InvalidOperationException("Network Test Product managed artifact manifest identity is invalid.");
            string path = Path.Combine(productRoot, artifact.manifestPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path) || !string.Equals(
                    NetworkTestArtifactFileUtility.Sha256(path),
                    artifact.manifestHash,
                    StringComparison.Ordinal))
                throw new InvalidOperationException("Network Test Product managed artifact manifest hash is invalid.");
            return artifact;
        }
    }
}
