using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonCharacter.Editor.ProductStartup
{
    [InitializeOnLoad]
    internal static class LocalGameplayPlayModeLauncher
    {
        const string StandaloneGameplay = "Assets/Scenes/Standalone/StandaloneGameplay.unity";
        const string CorinPrefab = "Assets/Prefabs/Characters/RuntimeProfiles/CorinStandalonePlayer.prefab";
        const string RestoreSceneKey = "ThirdPerson.LocalPlay.RestoreScene";
        const string RestorePendingKey = "ThirdPerson.LocalPlay.RestorePending";
        const string FootPlacementComposition = "ThirdPersonCharacter.Pipeline.Presentation.CharacterFootPlacementComposition";
        const string FinalIkSolver = "ThirdPersonCharacter.Pipeline.Presentation.FinalIK.FinalIKLimbFootPlacementSolver";

        static LocalGameplayPlayModeLauncher()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Tools/3C/Local Play/Standalone Gameplay")]
        internal static void PlayStandaloneGameplay()
        {
            Play(StandaloneGameplay);
        }

        [MenuItem("Tools/3C/Local Play/Validate Local Gameplay Scenes")]
        static void ValidateLocalGameplayScenes()
        {
            Validate(StandaloneGameplay);
            Debug.Log("Standalone Gameplay is ready for direct local play.");
        }

        [MenuItem("Tools/3C/Local Play/Standalone Gameplay", true)]
        static bool CanPlay()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        static void Play(string scenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Validate(scenePath);
            string previousScene = SceneManager.GetActiveScene().path;
            SessionState.SetString(RestoreSceneKey, previousScene ?? string.Empty);
            SessionState.SetBool(RestorePendingKey, true);
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        static void Validate(string scenePath)
        {
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath))
            {
                throw new InvalidOperationException($"Local Gameplay scene is missing: {scenePath}");
            }

            GameObject corin = AssetDatabase.LoadAssetAtPath<GameObject>(CorinPrefab);
            if (!corin)
            {
                throw new InvalidOperationException($"Corin prefab is missing: {CorinPrefab}");
            }

            string[] componentTypes = corin.GetComponentsInChildren<MonoBehaviour>(true)
                .Where(component => component)
                .Select(component => component.GetType().FullName)
                .ToArray();
            if (!componentTypes.Contains(FootPlacementComposition, StringComparer.Ordinal) ||
                !componentTypes.Contains(FinalIkSolver, StringComparer.Ordinal))
            {
                throw new InvalidOperationException("Corin local Gameplay prefab requires Foot Placement composition and FinalIK solver.");
            }

            ValidateSceneComposition(scenePath);
        }

        static void ValidateSceneComposition(string scenePath)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool closeScene = !scene.IsValid() || !scene.isLoaded;
            if (closeScene)
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }

            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                SimulationSessionHost[] sessionHosts = roots
                    .SelectMany(root => root.GetComponentsInChildren<SimulationSessionHost>(true))
                    .ToArray();
                if (sessionHosts.Length != 1)
                {
                    throw new InvalidOperationException(
                        $"Local Gameplay scene requires exactly one SimulationSessionHost, found {sessionHosts.Length}: {scenePath}");
                }

                SimulationSessionHost sessionHost = sessionHosts[0];
                if (!sessionHost.isActiveAndEnabled)
                {
                    throw new InvalidOperationException(
                        $"Local Gameplay SimulationSessionHost must be active: {sessionHost.name}");
                }

                CharacterPipelineHost[] characterHosts = roots
                    .SelectMany(root => root.GetComponentsInChildren<CharacterPipelineHost>(true))
                    .ToArray();
                if (characterHosts.Length == 0)
                {
                    throw new InvalidOperationException(
                        $"Local Gameplay scene requires at least one CharacterPipelineHost: {scenePath}");
                }

                var actorIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (CharacterPipelineHost characterHost in characterHosts)
                {
                    if (!characterHost.isActiveAndEnabled)
                    {
                        throw new InvalidOperationException(
                            $"Local Gameplay CharacterPipelineHost must be active: {characterHost.name}");
                    }
                    if (!characterHost.SessionHost)
                    {
                        throw new InvalidOperationException(
                            $"Local Gameplay CharacterPipelineHost '{characterHost.name}' requires an explicit SimulationSessionHost.");
                    }
                    if (characterHost.SessionHost != sessionHost)
                    {
                        throw new InvalidOperationException(
                            $"Local Gameplay CharacterPipelineHost '{characterHost.name}' references a different SimulationSessionHost.");
                    }
                    if (string.IsNullOrEmpty(characterHost.ActorId))
                    {
                        throw new InvalidOperationException(
                            $"Local Gameplay CharacterPipelineHost '{characterHost.name}' requires an explicit ActorId.");
                    }
                    if (!actorIds.Add(characterHost.ActorId))
                    {
                        throw new InvalidOperationException(
                            $"Local Gameplay scene contains duplicate ActorId '{characterHost.ActorId}'.");
                    }
                }
            }
            finally
            {
                if (closeScene && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(RestorePendingKey, false))
            {
                return;
            }

            SessionState.SetBool(RestorePendingKey, false);
            string previousScene = SessionState.GetString(RestoreSceneKey, string.Empty);
            SessionState.EraseString(RestoreSceneKey);
            if (!string.IsNullOrWhiteSpace(previousScene) &&
                AssetDatabase.LoadAssetAtPath<SceneAsset>(previousScene) &&
                !string.Equals(SceneManager.GetActiveScene().path, previousScene, StringComparison.Ordinal))
            {
                EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
            }
        }
    }
}
