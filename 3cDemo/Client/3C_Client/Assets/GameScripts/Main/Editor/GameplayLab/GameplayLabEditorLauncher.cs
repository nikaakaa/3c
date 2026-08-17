using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCamera;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Editor.ProductStartup;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.DeterministicRollback;
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
        const string PlayerPrefabPath = "Assets/Prefabs/Characters/RuntimeProfiles/Local/CorinStandalonePlayer.prefab";

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
            if (!player.GetComponentInChildren<CharacterWorldAwarePresentationBinding>(true))
            {
                throw new InvalidOperationException(
                    "Gameplay Lab player Prefab requires a World-Aware Presentation Binding.");
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
            if (bootstrap.Variants.Count != 3)
                throw new InvalidOperationException($"Gameplay Lab requires exactly Local Fixed, Local Float32 and Rollback Variants, found {bootstrap.Variants.Count}.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < bootstrap.Variants.Count; i++)
            {
                GameplayLabSessionVariantDefinition variant = bootstrap.Variants[i];
                if (!ids.Add(variant.VariantId))
                    throw new InvalidOperationException("Gameplay Lab Session Variants require distinct IDs.");
                if (!string.Equals(variant.NumericProfileId, "fixed-q32.32", StringComparison.Ordinal) &&
                    !string.Equals(variant.NumericProfileId, "float32-ieee754", StringComparison.Ordinal))
                    throw new InvalidOperationException($"Gameplay Lab Variant '{variant.VariantId}' uses an unsupported numeric profile.");
                ValidateRuntimeRoot(variant);
            }
            if (!ids.Contains("gameplay-lab.local-fixed-q32.32") ||
                !ids.Contains("gameplay-lab.local-float32") ||
                !ids.Contains("gameplay-lab.deterministic-rollback"))
                throw new InvalidOperationException("Gameplay Lab requires the exact Local Fixed, Local Float32 and Deterministic Rollback Variants.");
            GameplayLabSessionVariantDefinition localFixed = bootstrap.Variants.Single(
                value => string.Equals(value.VariantId, "gameplay-lab.local-fixed-q32.32", StringComparison.Ordinal));
            GameplayLabSessionVariantDefinition localFloat = bootstrap.Variants.Single(
                value => string.Equals(value.VariantId, "gameplay-lab.local-float32", StringComparison.Ordinal));
            GameplayLabSessionVariantDefinition rollback = bootstrap.Variants.Single(
                value => string.Equals(value.VariantId, "gameplay-lab.deterministic-rollback", StringComparison.Ordinal));
            if (localFixed.IsExternalLaunchVariant || localFloat.IsExternalLaunchVariant ||
                !rollback.IsExternalLaunchVariant)
                throw new InvalidOperationException("Only the Deterministic Rollback Variant may use external launch.");
            RequireSharedClosure(localFixed, rollback);
        }

        static void ValidateRuntimeRoot(GameplayLabSessionVariantDefinition variant)
        {
            GameObject root = variant.RuntimeRootPrefab;
            bool rollbackVariant = string.Equals(
                variant.VariantId,
                "gameplay-lab.deterministic-rollback",
                StringComparison.Ordinal);
            bool floatVariant = string.Equals(
                variant.NumericProfileId,
                "float32-ieee754",
                StringComparison.Ordinal);
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
            const int expectedActorCount = 2;
            if (actors.Length != expectedActorCount || distinctActorCount != expectedActorCount)
            {
                string actorSummary = actors.Length == 0
                    ? "none"
                    : string.Join(", ", actors.Select(actor => $"{actor.GetType().FullName}:{actor.SimulationActorId}"));
                throw new InvalidOperationException(
                    $"Gameplay Lab Variant '{variant.VariantId}' requires {expectedActorCount} distinct Session Actor(s). " +
                    $"Found={actors.Length}; Distinct={distinctActorCount}; Actors=[{actorSummary}].");
            }
            if (actors.Any(actor => !ReferenceEquals(actor.SessionHost, hosts[0])))
                throw new InvalidOperationException($"Gameplay Lab Variant '{variant.VariantId}' contains an Actor outside its Session Host.");
            if (!rollbackVariant)
            {
                int localOwners = root.GetComponentsInChildren<FixedCharacterHost>(true)
                    .Count(host => host.PresentationRole == CharacterPresentationRole.LocalOwner) +
                    root.GetComponentsInChildren<CharacterPipelineHost>(true)
                        .Count(host => host.PresentationRole == CharacterPresentationRole.LocalOwner);
                if (localOwners != 1)
                    throw new InvalidOperationException($"Gameplay Lab Variant '{variant.VariantId}' requires exactly one local owner.");
                SessionActorActionTargetInputProvider[] providers =
                    root.GetComponentsInChildren<SessionActorActionTargetInputProvider>(true);
                if (providers.Length != 1 || providers[0].Target == null)
                    throw new InvalidOperationException(
                        $"Gameplay Lab Variant '{variant.VariantId}' requires one bound committed Actor target provider.");
            }
            if (root.GetComponentsInChildren<ThirdPersonCameraController>(true).Length != 1)
                throw new InvalidOperationException($"Gameplay Lab Variant '{variant.VariantId}' requires exactly one gameplay camera controller.");
            int floatHosts = root.GetComponentsInChildren<CharacterPipelineHost>(true).Length;
            int fixedHosts = root.GetComponentsInChildren<FixedCharacterHost>(true).Length;
            int rollbackHosts = root.GetComponentsInChildren<DeterministicRollbackCharacterHost>(true).Length;
            if ((floatVariant && (floatHosts != 2 || fixedHosts != 0 || rollbackHosts != 0)) ||
                (!floatVariant && !rollbackVariant &&
                 (fixedHosts != 2 || rollbackHosts != 0 || floatHosts != 0)) ||
                (rollbackVariant && (rollbackHosts != 2 || fixedHosts != 0 || floatHosts != 0)))
            {
                throw new InvalidOperationException($"Gameplay Lab Variant '{variant.VariantId}' Actor host model does not match its numeric profile.");
            }
            if (!floatVariant && !rollbackVariant)
            {
                FixedCharacterHost[] characters = root.GetComponentsInChildren<FixedCharacterHost>(true);
                if (characters.Any(character => !character.ControlSource) ||
                    characters.Count(character => character.ControlSource is FixedPlayerCharacterControlSource) !=
                    1 ||
                    characters.Count(character => character.ControlSource is FixedNeutralCharacterControlSource) !=
                    1)
                {
                    throw new InvalidOperationException(
                        $"Gameplay Lab Variant '{variant.VariantId}' requires one persisted Fixed Player Control Source and one persisted Fixed Neutral Control Source.");
                }
            }
            if (floatVariant)
            {
                CharacterPipelineHost[] characters = root.GetComponentsInChildren<CharacterPipelineHost>(true);
                if (characters.Any(character => !character.ControlSource) ||
                    characters.Count(character => character.ControlSource is PlayerCharacterControlSource) != 1 ||
                    characters.Count(character => character.ControlSource is AICharacterControlSource) != 1)
                {
                    throw new InvalidOperationException($"Gameplay Lab Variant '{variant.VariantId}' requires one Float32 Player and one formal AI Control Source.");
                }
                CharacterPipelineHost enemy = characters.Single(
                    character => character.ControlSource is AICharacterControlSource);
                AICharacterControlSource ai = (AICharacterControlSource)enemy.ControlSource;
                if (enemy.ActorId != "gameplay-lab-target" ||
                    !ai.Controller ||
                    !ai.Controller.PerceptionProfile.CandidateActorIds.Contains("gameplay-lab-player"))
                {
                    throw new InvalidOperationException($"Gameplay Lab Variant '{variant.VariantId}' AI target closure is not bound to the Gameplay Lab player roster.");
                }
            }
            variant.ValidateComposition(hosts[0].Composition);
        }

        static void RequireSharedClosure(
            GameplayLabSessionVariantDefinition local,
            GameplayLabSessionVariantDefinition rollback)
        {
            if (!string.Equals(local.DefinitionGuid, rollback.DefinitionGuid, StringComparison.Ordinal) ||
                local.Program != rollback.Program ||
                local.PresentationProjection != rollback.PresentationProjection ||
                local.WorldSolver != rollback.WorldSolver ||
                local.CollisionWorld != rollback.CollisionWorld)
            {
                throw new InvalidOperationException(
                    "Gameplay Lab Local Fixed and Rollback Variants must share Definition, Program, Projection, KCC and Collision identities.");
            }
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

        [MenuItem("Tools/3C/Internal/Rebuild Gameplay Lab Assets")]
        static void RebuildAssets()
        {
            GameplayLabAssetBuilder.Rebuild();
        }

        [MenuItem("Tools/3C/Characters/Sync Training Enemy to Gameplay Lab Float32")]
        static void SyncTrainingEnemy()
        {
            GameplayLabAssetBuilder.SyncFloat32EnemyVariant();
        }

        sealed class Operations : IGameplayLabLauncherOperations
        {
            public GameplayLabLauncherState ReadState() => GameplayLabEditorLauncher.ReadLauncherState();
            public void Open() => GameplayLabEditorLauncher.Open();
            public void Play(int variantIndex) => GameplayLabEditorLauncher.Play(variantIndex);
            public void SyncAssets() => GameplayLabAssetBuilder.Rebuild();
        }
    }
}
