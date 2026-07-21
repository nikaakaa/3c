using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCamera;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Editor.ProductStartup;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation.FinalIK;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonGameplay.Lab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonGameplay.Editor.Lab
{
    internal static class GameplayLabEditorLauncher
    {
        public const string ScenePath = "Assets/Scenes/GameplayLab/GameplayLab.unity";
        const string PlayerPrefabPath = "Assets/Prefabs/Characters/RuntimeProfiles/CorinStandalonePlayer.prefab";

        internal static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            Validate();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        internal static GameplayLabLauncherState ReadLauncherState()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool closeScene = !scene.IsValid() || !scene.isLoaded;
            if (closeScene)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                GameplayLabBootstrap bootstrap = RequireBootstrap(scene);
                string[] labels = bootstrap.Variants
                    .Select(variant => variant.VariantId)
                    .ToArray();
                return new GameplayLabLauncherState(labels, bootstrap.StartupVariantIndex);
            }
            finally
            {
                if (closeScene && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        internal static void Play(int variantIndex)
        {
            Validate();
            EditorPlayModeSceneLauncher.Play(ScenePath, scene =>
            {
                GameplayLabBootstrap bootstrap = RequireBootstrap(scene);
                bootstrap.SetStartupVariantIndex(variantIndex);
            });
        }

        internal static void Validate()
        {
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))
                throw new InvalidOperationException($"Gameplay Lab scene is missing: {ScenePath}");
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool closeScene = !scene.IsValid() || !scene.isLoaded;
            if (closeScene)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                GameplayLabBootstrap bootstrap = RequireBootstrap(scene);
                SimulationSessionHost[] sessionHosts = roots
                    .SelectMany(root => root.GetComponentsInChildren<SimulationSessionHost>(true))
                    .ToArray();
                if (sessionHosts.Length != 0)
                    throw new InvalidOperationException("Gameplay Lab scene cannot contain a pre-instantiated SimulationSessionHost.");
                ValidatePlayerPresentation();
                ValidateVariants(bootstrap);
            }
            finally
            {
                if (closeScene && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        static void ValidatePlayerPresentation()
        {
            GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (!player)
                throw new InvalidOperationException($"Gameplay Lab player Prefab is missing: {PlayerPrefabPath}");
            if (!player.GetComponentInChildren<CharacterFootPlacementComposition>(true) ||
                !player.GetComponentInChildren<FinalIKLimbFootPlacementSolver>(true))
            {
                throw new InvalidOperationException(
                    "Gameplay Lab player Prefab requires Foot Placement composition and FinalIK solver.");
            }
        }

        static GameplayLabBootstrap RequireBootstrap(Scene scene)
        {
            GameplayLabBootstrap[] bootstraps = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<GameplayLabBootstrap>(true))
                .ToArray();
            if (bootstraps.Length != 1)
                throw new InvalidOperationException($"Gameplay Lab scene requires exactly one Bootstrap, found {bootstraps.Length}.");
            return bootstraps[0];
        }

        static void ValidateVariants(GameplayLabBootstrap bootstrap)
        {
            if (bootstrap.Variants.Count != 2)
                throw new InvalidOperationException($"Gameplay Lab requires exactly two local Session Variants, found {bootstrap.Variants.Count}.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var profiles = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < bootstrap.Variants.Count; i++)
            {
                GameplayLabSessionVariantDefinition variant = bootstrap.Variants[i];
                if (!ids.Add(variant.VariantId) || !profiles.Add(variant.NumericProfileId))
                    throw new InvalidOperationException("Gameplay Lab Session Variants require distinct IDs and numeric profiles.");
                ValidateRuntimeRoot(variant);
            }
            if (!profiles.Contains("float32-ieee754") || !profiles.Contains("fixed-q32.32"))
                throw new InvalidOperationException("Gameplay Lab requires Local Float32 and Local Fixed Q32.32 Variants.");
        }

        static void ValidateRuntimeRoot(GameplayLabSessionVariantDefinition variant)
        {
            GameObject root = variant.RuntimeRootPrefab;
            if (CountMissingScripts(root) != 0)
                throw new InvalidOperationException($"Gameplay Lab Variant '{variant.VariantId}' contains a missing MonoBehaviour script.");
            SimulationSessionHost[] hosts = root.GetComponentsInChildren<SimulationSessionHost>(true);
            if (hosts.Length != 1)
                throw new InvalidOperationException($"Gameplay Lab Variant '{variant.VariantId}' requires exactly one Session Host.");
            ISimulationSessionActorHost[] actors = root
                .GetComponentsInChildren<MonoBehaviour>(true)
                .OfType<ISimulationSessionActorHost>()
                .ToArray();
            int distinctActorCount = actors.Select(actor => actor.SimulationActorId).Distinct().Count();
            if (actors.Length != 2 || distinctActorCount != 2)
            {
                string actorSummary = actors.Length == 0
                    ? "none"
                    : string.Join(", ", actors.Select(actor => $"{actor.GetType().FullName}:{actor.SimulationActorId}"));
                throw new InvalidOperationException(
                    $"Gameplay Lab Variant '{variant.VariantId}' requires two distinct Session Actors. " +
                    $"Found={actors.Length}; Distinct={distinctActorCount}; Actors=[{actorSummary}].");
            }
            if (actors.Any(actor => !ReferenceEquals(actor.SessionHost, hosts[0])))
                throw new InvalidOperationException($"Gameplay Lab Variant '{variant.VariantId}' contains an Actor outside its Session Host.");
            int localOwners = root.GetComponentsInChildren<CharacterPipelineHost>(true)
                .Count(host => host.PresentationRole == CharacterPresentationRole.LocalOwner) +
                root.GetComponentsInChildren<FixedCharacterHost>(true)
                    .Count(host => host.PresentationRole == CharacterPresentationRole.LocalOwner);
            if (localOwners != 1)
                throw new InvalidOperationException($"Gameplay Lab Variant '{variant.VariantId}' requires exactly one local owner.");
            SessionActorActionTargetInputProvider[] providers =
                root.GetComponentsInChildren<SessionActorActionTargetInputProvider>(true);
            if (providers.Length != 1 || providers[0].Target == null)
                throw new InvalidOperationException($"Gameplay Lab Variant '{variant.VariantId}' requires one bound committed Actor target provider.");
            if (root.GetComponentsInChildren<ThirdPersonCameraController>(true).Length != 1)
                throw new InvalidOperationException($"Gameplay Lab Variant '{variant.VariantId}' requires exactly one gameplay camera controller.");
            bool fixedVariant = string.Equals(variant.NumericProfileId, "fixed-q32.32", StringComparison.Ordinal);
            int floatHosts = root.GetComponentsInChildren<CharacterPipelineHost>(true).Length;
            int fixedHosts = root.GetComponentsInChildren<FixedCharacterHost>(true).Length;
            if ((fixedVariant && (fixedHosts != 2 || floatHosts != 0)) ||
                (!fixedVariant && (floatHosts != 2 || fixedHosts != 0)))
            {
                throw new InvalidOperationException($"Gameplay Lab Variant '{variant.VariantId}' Actor host model does not match its numeric profile.");
            }
            if (fixedVariant)
            {
                FixedCharacterHost[] characters = root.GetComponentsInChildren<FixedCharacterHost>(true);
                if (characters.Any(character => !character.ControlSource) ||
                    characters.Count(character => character.ControlSource is FixedPlayerCharacterControlSource) != 1 ||
                    characters.Count(character => character.ControlSource is FixedNeutralCharacterControlSource) != 1)
                {
                    throw new InvalidOperationException($"Gameplay Lab Variant '{variant.VariantId}' requires one persisted Fixed Player Control Source and one persisted Fixed Neutral Control Source.");
                }
            }
            variant.ValidateComposition(hosts[0].Composition);
        }

        static int CountMissingScripts(GameObject root)
        {
            int count = 0;
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
                count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transforms[i].gameObject);
            return count;
        }
    }

    [InitializeOnLoad]
    internal static class GameplayLabLauncherRegistration
    {
        static GameplayLabLauncherRegistration()
        {
            GameplayLabLauncherRegistry.Register(new Operations());
        }

        sealed class Operations : IGameplayLabLauncherOperations
        {
            public GameplayLabLauncherState ReadState() => GameplayLabEditorLauncher.ReadLauncherState();
            public void Open() => GameplayLabEditorLauncher.Open();
            public void Play(int variantIndex) => GameplayLabEditorLauncher.Play(variantIndex);
            public void RebuildAssets() => GameplayLabAssetBuilder.Rebuild();
        }
    }
}
